using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;

namespace XCharts.Example
{
    /// <summary>
    /// 传感器曲线 - 最近10分钟视图。
    /// 基于 ChuanganQuXianTemperatureChartManager 修改：
    /// X轴显示最近 recentMinutes 分钟（每分钟一个数据点），而非24小时按小时聚合。
    /// </summary>
    [DisallowMultipleComponent]
    public class ChuanganQuXianRecent10MinutesChartManager : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int frameType = 8;
        [SerializeField] private int connectionTimeout = 30;
        [SerializeField] private string queryDate = "20250622";

        [Header("查询条件")]
        public TextMeshProUGUI potNameText;
        public Button applyFilterButton;

        [Header("图表布局")]
        public GameObject chartPrefab;
        public Transform chartsContainer;
        public ScrollRect scrollRect;
        public Vector2 chartSize = new Vector2(0, 400);
        public float chartSpacing = 0f;
        public Vector2 chartPadding = new Vector2(24, 56);
        private float _runtimeChartWidth; // 运行时和 Content/viewport 一致的图表宽度

        [Header("UI提示")]
        public TextMeshProUGUI loadingText;

        [Header("运行控制")]
        [SerializeField] private bool autoLoadOnStart = false;
        [SerializeField] private bool showStatusOnScreen = false;

        [Header("绘图设置")]
        [SerializeField] private int valueCountPerRecord = 48;
        [SerializeField] private int valuesPerChart = 4;
        [SerializeField] private int expectedBytesPerRecord = 102;
        [SerializeField] private int recentMinutes = 10; // ★ 最近N分钟（每分钟一条记录）

        private readonly List<LineChart> chartInstances = new List<LineChart>();
        private readonly List<ParsedFrame> parsedFrames = new List<ParsedFrame>();
        private Coroutine bootstrapRoutine;
        private bool isLoading;
        private string selectedPotName;
        private List<string> minuteLabels = new List<string>(); // 本次渲染共享的X轴标签

        private const int FrameHeaderSize = 14;
        private const int FrameTailSize = 2;
        private const int DateBytes = 4;
        private const int RecordNoBytes = 2;
        private const int ValueBytes = 96;

        [Serializable]
        private class ParsedFrame
        {
            public int enterpriseId;
            public int parsedFrameType;
            public int slot;
            public int dataLength;
            public string dateText;
            public int recordNo;
            public List<int[]> records = new List<int[]>();
        }

        [Serializable]
        private class GroupMeta
        {
            public string groupPrefix;
            public int groupIndex;
            public int startSensorIndex;
            public int endSensorIndex;
        }

        private void Awake()
        {
            AutoBindSceneReferences();
            InitUI();
            ReportStatus("Awake");
        }

        private void Start()
        {
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveAllListeners();
                applyFilterButton.onClick.AddListener(OnApplyFilter);
            }

            if (autoLoadOnStart)
            {
                if (bootstrapRoutine != null) StopCoroutine(bootstrapRoutine);
                bootstrapRoutine = StartCoroutine(DelayedBootstrap());
            }
        }

        private IEnumerator DelayedBootstrap()
        {
            yield return null;
            LoadTemperatureData();
        }

        private void InitUI()
        {
            selectedPotName = ResolveSelectedSlotNumber();
            if (potNameText != null && string.IsNullOrWhiteSpace(potNameText.text))
            {
                potNameText.text = selectedPotName;
            }
        }

        public void OnApplyFilter()
        {
            LoadTemperatureData();
        }

        private void LoadTemperatureData()
        {
            if (isLoading) return;
            if (!gameObject.activeInHierarchy)
            {
                ReportStatus("对象未激活，无法加载", true);
                return;
            }

            selectedPotName = ResolveSelectedSlotNumber();
            ClearCharts();
            parsedFrames.Clear();
            StartCoroutine(FetchAndRender());
        }

        private IEnumerator FetchAndRender()
        {
            isLoading = true;
            ShowLoading(true, $"正在读取最近{recentMinutes}分钟数据...");

            string requestParam = $"{companyId}-{frameType}-{selectedPotName}-{queryDate}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes($"\"{requestParam}\"");
            ReportStatus($"请求参数: {requestParam}");
            ReportStatus($"请求地址: {apiUrl}");
            ReportStatus("开始发送请求");

            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                ReportStatus($"请求结果={request.result}, code={request.responseCode}");
                ReportStatus($"原始响应长度={(responseText != null ? responseText.Length : 0)}");

                if (request.result != UnityWebRequest.Result.Success || string.IsNullOrWhiteSpace(responseText))
                {
                    ReportStatus($"请求失败或为空: {request.error}", true);
                    FinishLoading();
                    yield break;
                }

                if (!TryParseResponse(responseText, out var frame))
                {
                    ReportStatus("响应解析失败，停止绘制", true);
                    FinishLoading();
                    yield break;
                }

                parsedFrames.Clear();
                parsedFrames.Add(frame);
                ReportStatus($"真实帧解析完成: 记录数={frame.records.Count}, dataLength={frame.dataLength}");
                RenderChartsFromFrames();
            }

            FinishLoading();
        }

        private void FinishLoading()
        {
            ShowLoading(false);
            isLoading = false;
            ReportStatus($"绘图完成: {chartInstances.Count} 张图");
        }

        private bool TryParseResponse(string responseText, out ParsedFrame frame)
        {
            frame = null;

            string base64Data = responseText.Trim().Trim('"');
            while (base64Data.Length % 4 != 0)
            {
                base64Data += "=";
            }

            byte[] frameData;
            try
            {
                frameData = Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                ReportStatus($"Base64 解码失败: {ex.Message}", true);
                return false;
            }

            if (frameData.Length < FrameHeaderSize + FrameTailSize)
            {
                ReportStatus("帧长度不足", true);
                return false;
            }

            int offset = 0;
            if (frameData[offset] != 170 || frameData[offset + 1] != 85)
            {
                ReportStatus("帧头校验失败", true);
                return false;
            }
            offset += 2;

            int enterpriseId = ReadUInt16(frameData, offset); offset += 2;
            int parsedFrameType = ReadUInt16(frameData, offset); offset += 2;
            int slot = ReadUInt16(frameData, offset); offset += 2;
            int dataLength = ReadInt32(frameData, offset); offset += 4;

            ReportStatus($"响应帧信息 enterprise={enterpriseId}, frameType={parsedFrameType}, slot={slot}, dataLength={dataLength}");

            if (offset + dataLength + FrameTailSize > frameData.Length)
            {
                ReportStatus("数据长度校验失败", true);
                return false;
            }

            if (frameData[offset + dataLength] != 165 || frameData[offset + dataLength + 1] != 90)
            {
                ReportStatus("帧尾校验失败", true);
                return false;
            }

            frame = new ParsedFrame
            {
                enterpriseId = enterpriseId,
                parsedFrameType = parsedFrameType,
                slot = slot,
                dataLength = dataLength,
                dateText = ReadDateText(frameData, offset),
                recordNo = ReadUInt16(frameData, offset + DateBytes)
            };

            int recordCount = dataLength / expectedBytesPerRecord;
            ReportStatus($"开始逐条解析真实记录，recordCount={recordCount}");
            for (int recordIndex = 0; recordIndex < recordCount; recordIndex++)
            {
                int recordOffset = offset + recordIndex * expectedBytesPerRecord;
                if (recordOffset + expectedBytesPerRecord > offset + dataLength) break;

                int[] values = new int[valueCountPerRecord];
                int valueOffset = recordOffset + DateBytes + RecordNoBytes;
                for (int i = 0; i < valueCountPerRecord; i++)
                {
                    values[i] = ReadUInt16(frameData, valueOffset + i * 2);
                }
                frame.records.Add(values);
                if (recordIndex < 3 || recordIndex == recordCount - 1)
                {
                    ReportStatus($"记录{recordIndex + 1}/{recordCount} 解析完成，首值={values[0]}, 末值={values[valueCountPerRecord - 1]}");
                }
            }

            ReportStatus($"成功解析记录数: {frame.records.Count}");
            return frame.records.Count > 0;
        }

        private void RenderChartsFromFrames()
        {
            ClearCharts();
            if (chartsContainer == null) AutoBindSceneReferences();
            if (chartsContainer == null)
            {
                ReportStatus("chartsContainer 未找到", true);
                return;
            }

            if (parsedFrames.Count == 0)
            {
                ReportStatus("没有可绘制的真实数据", true);
                return;
            }

            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }

            if (chartSize.y > 1000f || chartSize.x > 1000f)
            {
                Debug.LogWarning($"[ChuanganQuXianRecent10MinutesChartManager] 检测到 chartSize 脏值={chartSize}，自动重置为 (0, 320)");
                chartSize = new Vector2(0, 320);
            }
            if (chartSpacing > 50f)
            {
                Debug.LogWarning($"[ChuanganQuXianRecent10MinutesChartManager] 检测到 chartSpacing 脏值={chartSpacing}，自动重置为 0");
                chartSpacing = 0f;
            }

            var frame = parsedFrames[0];
            int chartCount = 12;
            Debug.Log($"[ChuanganQuXianRecent10MinutesChartManager] ★ 当前 chartSize={chartSize}, chartSpacing={chartSpacing}");
            float totalHeight = chartCount * (chartSize.y + chartSpacing) + chartPadding.y;

            // === 强制 Content 宽度 = Viewport 宽度 ===
            if (chartsContainer is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();

                float viewportWidth = 0f;
                var scrollRT = scrollRect != null ? scrollRect.GetComponent<RectTransform>() : null;
                float srWidth = scrollRT != null ? scrollRT.rect.width : 0f;
                float vpWidth = (scrollRect != null && scrollRect.viewport != null)
                    ? scrollRect.viewport.rect.width : 0f;
                float parentWidth = 0f;
                if (scrollRT != null && scrollRT.parent is RectTransform parentRT)
                    parentWidth = parentRT.rect.width;

                viewportWidth = srWidth > 1f ? srWidth
                    : (vpWidth > 1f ? vpWidth
                    : (parentWidth > 1f ? parentWidth : Screen.width));

                _runtimeChartWidth = viewportWidth;

                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(viewportWidth, totalHeight);

                Debug.Log($"[Content尺寸] srWidth={srWidth}, vpWidth={vpWidth}, 最终Content宽={viewportWidth}, 高={totalHeight}");
            }

            // ★ 生成本次渲染共享的X轴标签：最近 recentMinutes 分钟（以当前时间为锚点）
            BuildMinuteLabels(frame.records.Count);
            ReportStatus($"总记录数={frame.records.Count}，本次绘制最近 {minuteLabels.Count} 分钟");
            ReportStatus($"图表数量={chartCount}，每图曲线数={valuesPerChart}，总值数={valueCountPerRecord}");
            if (minuteLabels.Count > 0)
            {
                ReportStatus($"绘制时间范围：{minuteLabels[0]} ~ {minuteLabels[minuteLabels.Count - 1]}，每分钟一个横坐标");
            }

            for (int i = 0; i < chartCount; i++)
            {
                var meta = GetGroupMeta(i);
                ReportStatus($"准备绘制图{i + 1}/12: {GetBusinessChartTitle(i)}，曲线={GetSeriesName(meta.groupPrefix, meta.groupIndex, 1)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 2)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 3)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 4)}");
                CreateSingleChart(frame, i);
            }
        }

        /// <summary>
        /// 生成最近N分钟的X轴标签（HH:mm），锚点为当前时间。
        /// 记录按分钟等间隔存储，最后一条记录对应"现在"。
        /// </summary>
        private void BuildMinuteLabels(int totalRecordCount)
        {
            minuteLabels.Clear();
            int count = Mathf.Clamp(Mathf.Min(recentMinutes, totalRecordCount), 0, recentMinutes);
            if (count <= 0) return;

            DateTime now = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                DateTime t = now.AddMinutes(-(count - 1 - i));
                minuteLabels.Add(t.ToString("HH:mm"));
            }
        }

        /// <summary>本次实际绘制的数据条数（不超过 recentMinutes）</summary>
        private int GetPointCount(ParsedFrame frame)
        {
            return Mathf.Clamp(Mathf.Min(recentMinutes, frame.records.Count), 0, recentMinutes);
        }

        private void CreateSingleChart(ParsedFrame frame, int chartIndex)
        {
            GameObject chartObj = CreateChartObject();
            if (chartObj == null)
            {
                ReportStatus("无法创建图表对象", true);
                return;
            }

            var meta = GetGroupMeta(chartIndex);
            string titleText = GetBusinessChartTitle(chartIndex);
            chartObj.name = titleText;
            chartObj.transform.SetParent(chartsContainer, false);

            if (chartObj.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1f);
                rect.anchoredPosition = new Vector2(0, -chartIndex * (chartSize.y + chartSpacing));
                rect.sizeDelta = new Vector2(_runtimeChartWidth, chartSize.y + chartPadding.y);
            }

            var lineChart = chartObj.GetComponent<LineChart>();
            if (lineChart == null)
            {
                ReportStatus("图表对象缺少 LineChart 组件", true);
                return;
            }

            ConfigureChart(lineChart, frame, chartIndex, titleText, meta);
            chartInstances.Add(lineChart);
        }

        private GameObject CreateChartObject()
        {
            if (chartPrefab != null)
            {
                return Instantiate(chartPrefab);
            }

            var go = new GameObject("RuntimeLineChart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LineChart));
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color32(55, 59, 71, 255);
            }
            return go;
        }

        private void ConfigureChart(LineChart chart, ParsedFrame frame, int chartIndex, string titleText, GroupMeta meta)
        {
            chart.ClearData();
            chart.RemoveAllSerie();

            chart.SetSize(_runtimeChartWidth, chartSize.y + chartPadding.y);

            if (chart.theme != null)
            {
                chart.theme.enableCustomTheme = true;
                chart.theme.transparentBackground = false;
                chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255);
            }

            // ---- Grid ----
            var grid = chart.EnsureChartComponent<GridCoord>();
            if (grid != null)
            {
                grid.backgroundColor = new Color32(40, 47, 57, 255);
                grid.showBorder = true;
                grid.borderWidth = 2f;
                grid.borderColor = Color.white;
                grid.left = 60f;
                grid.right = 20f;
                grid.top = 50f;
                grid.bottom = 90f;
            }

            // ---- Title ----
            var title = chart.EnsureChartComponent<Title>();
            if (title != null)
            {
                title.show = true;
                title.text = titleText;
                if (title.labelStyle != null && title.labelStyle.textStyle != null)
                {
                    title.labelStyle.textStyle.color = Color.white;
                    title.labelStyle.textStyle.fontSize = 22;
                    title.labelStyle.textStyle.fontStyle = FontStyle.Bold;
                }
                title.location.align = Location.Align.TopLeft;
                title.location.left = 0.03f;
                title.location.top = 0.03f;
            }

            // ---- X轴：最近N分钟，每分钟一个横坐标 ----
            var xAxis = chart.EnsureChartComponent<XAxis>();
            if (xAxis != null)
            {
                xAxis.type = Axis.AxisType.Category;
                xAxis.axisLine.show = true;
                xAxis.axisTick.show = true;
                xAxis.boundaryGap = true;
                if (xAxis.axisLabel != null)
                {
                    xAxis.axisLabel.show = true;
                    xAxis.axisLabel.interval = 0;
                    xAxis.axisLabel.rotate = 0;
                    if (xAxis.axisLabel.textStyle != null)
                    {
                        xAxis.axisLabel.textStyle.color = Color.white;
                        xAxis.axisLabel.textStyle.fontSize = 18;
                        xAxis.axisLabel.textStyle.fontStyle = FontStyle.Bold;
                    }
                }
                // 每分钟一个标签（数据点少，全部显示）
                for (int i = 0; i < minuteLabels.Count; i++)
                {
                    chart.AddXAxisData(minuteLabels[i]);
                }
                xAxis.SetAllDirty();
            }

            // ---- Y轴 ----
            var yAxis = chart.EnsureChartComponent<YAxis>();
            if (yAxis != null)
            {
                yAxis.type = Axis.AxisType.Value;
                yAxis.position = Axis.AxisPosition.Left;
                yAxis.axisLine.show = true;
                yAxis.axisTick.show = true;
                yAxis.axisLabel.show = true;
                yAxis.splitLine.show = false;
                if (yAxis.splitLine != null)
                {
                    yAxis.splitLine.lineStyle.show = false;
                }
                if (yAxis.axisLabel != null)
                {
                    yAxis.axisLabel.numericFormatter = "f0";
                    if (yAxis.axisLabel.textStyle != null)
                    {
                        yAxis.axisLabel.textStyle.color = Color.white;
                        yAxis.axisLabel.textStyle.fontSize = 18;
                        yAxis.axisLabel.textStyle.fontStyle = FontStyle.Bold;
                    }
                }
            }

            // ---- 计算Y轴范围（仅基于最近N分钟的数据） ----
            var values = ExtractValuesForChart(frame, meta.startSensorIndex, meta.endSensorIndex);
            if (values.Count > 0 && yAxis != null)
            {
                double min = values.Min();
                double max = values.Max();
                double range = max - min;
                double padding;
                if (range < 1)
                    padding = 5;
                else if (range < 10)
                    padding = range * 0.5;
                else
                    padding = range * 0.25;

                yAxis.min = Math.Max(0, min - padding);
                yAxis.max = max + padding;

                if (yAxis.max - yAxis.min < 20)
                {
                    double center = (yAxis.min + yAxis.max) / 2.0;
                    yAxis.min = Math.Max(0, center - 10);
                    yAxis.max = center + 10;
                }

                yAxis.splitNumber = 6;
                yAxis.SetAllDirty();
            }

            // ---- DataZoom：禁用，显示全部最近N分钟数据 ----
            var dataZoom = chart.EnsureChartComponent<DataZoom>();
            if (dataZoom != null)
            {
                dataZoom.enable = false;
                dataZoom.supportInside = false;
                dataZoom.supportSlider = false;
                dataZoom.start = 0;
                dataZoom.end = 100;
            }

            var legend = chart.EnsureChartComponent<Legend>();
            if (legend != null)
            {
                legend.show = true;
                legend.location.align = Location.Align.TopRight;
                legend.location.top = 0.05f;
                legend.location.right = 0.02f;
                legend.selectedMode = Legend.SelectedMode.Multiple;
                legend.itemGap = 16f;
                if (legend.labelStyle != null && legend.labelStyle.textStyle != null)
                {
                    legend.labelStyle.textStyle.color = Color.white;
                    legend.labelStyle.textStyle.fontSize = 20;
                    legend.labelStyle.textStyle.fontStyle = FontStyle.Bold;
                }
            }

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            if (tooltip != null)
            {
                tooltip.show = true;
                tooltip.type = Tooltip.Type.Line;
                if (tooltip.lineStyle != null)
                {
                    tooltip.lineStyle.show = true;
                    tooltip.lineStyle.width = 2f;
                }
            }

            Color[] colors =
            {
                new Color32(160, 96, 255, 255),
                new Color32(84, 183, 98, 255),
                new Color32(240, 198, 84, 255),
                new Color32(233, 76, 126, 255)
            };

            int pointCount = GetPointCount(frame);
            int startIndex = frame.records.Count - pointCount; // 最近N条记录的起始索引

            for (int s = 0; s < valuesPerChart; s++)
            {
                int globalSensorIndex = meta.startSensorIndex + s;
                if (globalSensorIndex > meta.endSensorIndex || globalSensorIndex >= valueCountPerRecord) break;

                string seriesName = GetSeriesName(meta.groupPrefix, meta.groupIndex, s + 1);
                var serie = chart.AddSerie<Line>(seriesName);
                if (serie == null) continue;

                if (serie.lineStyle != null)
                {
                    serie.lineStyle.color = colors[s % colors.Length];
                    serie.lineStyle.width = 3f;
                }

                if (serie.symbol != null)
                {
                    serie.symbol.show = false;
                }

                if (serie.areaStyle != null)
                {
                    serie.areaStyle.show = false;
                }

                serie.large = false;

                // ★ 不做小时聚合：每条记录（每分钟）直接作为一个数据点
                for (int i = 0; i < pointCount; i++)
                {
                    var pointValues = frame.records[startIndex + i];
                    if (pointValues != null && pointValues.Length > globalSensorIndex)
                    {
                        chart.AddData(s, pointValues[globalSensorIndex]);
                    }
                    else
                    {
                        chart.AddData(s, 0);
                    }
                }
            }

            chart.RefreshChart();
        }

        private List<double> ExtractValuesForChart(ParsedFrame frame, int startSensorIndex, int endSensorIndex)
        {
            var result = new List<double>();
            if (frame == null || frame.records == null || frame.records.Count == 0) return result;

            startSensorIndex = Mathf.Clamp(startSensorIndex, 0, valueCountPerRecord - 1);
            endSensorIndex = Mathf.Clamp(endSensorIndex, startSensorIndex, valueCountPerRecord - 1);

            // ★ 仅提取最近N分钟（与数据添加逻辑保持一致）
            int pointCount = GetPointCount(frame);
            int startIndex = frame.records.Count - pointCount;
            for (int i = startIndex; i < frame.records.Count; i++)
            {
                var pointValues = frame.records[i];
                if (pointValues == null) continue;
                for (int sensor = startSensorIndex; sensor <= endSensorIndex; sensor++)
                {
                    if (pointValues.Length > sensor)
                    {
                        result.Add(pointValues[sensor]);
                    }
                }
            }

            return result;
        }

        private string GetBusinessChartTitle(int chartIndex)
        {
            char prefix = chartIndex < 6 ? 'A' : 'B';
            int localIndex = (chartIndex % 6) + 1;
            int start = (localIndex - 1) * valuesPerChart + 1;
            int end = start + valuesPerChart - 1;
            return $"{prefix}{start}-{prefix}{end}";
        }

        private GroupMeta GetGroupMeta(int chartIndex)
        {
            int localIndex = chartIndex % 6;
            int chartStart = localIndex * valuesPerChart;
            return new GroupMeta
            {
                groupPrefix = chartIndex < 6 ? "A" : "B",
                groupIndex = localIndex + 1,
                startSensorIndex = chartStart,
                endSensorIndex = Mathf.Min(chartStart + valuesPerChart - 1, valueCountPerRecord - 1)
            };
        }

        private string GetSeriesName(string groupPrefix, int groupIndex, int sensorIndexInChart)
        {
            int sensorNumber = ((groupIndex - 1) * valuesPerChart) + sensorIndexInChart;
            return $"{groupPrefix}{sensorNumber}";
        }

        private void ClearCharts()
        {
            foreach (var chart in chartInstances)
            {
                if (chart != null)
                {
                    Destroy(chart.gameObject);
                }
            }
            chartInstances.Clear();

            if (chartsContainer == null) return;
            for (int i = chartsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(chartsContainer.GetChild(i).gameObject);
            }
        }

        private void AutoBindSceneReferences()
        {
            if (chartsContainer == null)
            {
                var content = FindTransformByName("Content", "content");
                if (content != null)
                {
                    chartsContainer = content;
                    scrollRect = chartsContainer.GetComponentInParent<ScrollRect>(true);
                }
            }

            if (scrollRect == null)
            {
                var scrolls = FindObjectsOfType<ScrollRect>(true);
                foreach (var sr in scrolls)
                {
                    if (sr != null && sr.content != null)
                    {
                        scrollRect = sr;
                        if (chartsContainer == null) chartsContainer = sr.content;
                        break;
                    }
                }
            }
        }

        private Transform FindTransformByName(params string[] keywords)
        {
            var transforms = FindObjectsOfType<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr == null) continue;
                string name = tr.name.ToLowerInvariant();
                foreach (var kw in keywords)
                {
                    if (!string.IsNullOrEmpty(kw) && name.Contains(kw.ToLowerInvariant())) return tr;
                }
            }
            return null;
        }

        private void ShowLoading(bool show, string text = null)
        {
            if (loadingText == null) return;
            loadingText.gameObject.SetActive(show);
            if (show && !string.IsNullOrWhiteSpace(text))
            {
                loadingText.text = text;
            }
        }

        private void ReportStatus(string message, bool warning = false)
        {
            string fullMessage = $"[ChuanganQuXianRecent10MinutesChartManager] {message}";
            if (warning) Debug.LogWarning(fullMessage);
            else Debug.Log(fullMessage);

            if (showStatusOnScreen && loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = message;
            }
        }

        private string ResolveSelectedSlotNumber()
        {
            if (potNameText != null)
            {
                string txt = (potNameText.text ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(txt) && Regex.IsMatch(txt, @"^\d+$")) return txt;
            }
            return "1001";
        }

        private int ReadUInt16(byte[] data, int offset)
        {
            return (data[offset] << 8) | data[offset + 1];
        }

        private string ReadDateText(byte[] data, int offset)
        {
            int year = 2000 + data[offset];
            int month = data[offset + 1];
            int day = data[offset + 2];
            int hour = data[offset + 3];
            return $"{year:0000}{month:00}{day:00}{hour:00}";
        }

        private int ReadInt32(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
