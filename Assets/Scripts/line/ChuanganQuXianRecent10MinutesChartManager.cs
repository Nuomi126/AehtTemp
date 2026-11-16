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
    /// 阳极电流曲线 - 最近10分钟视图。
    /// Frame 6 每2秒返回一个时间点，每个时间点包含48路阳极电流。
    /// </summary>
    [DisallowMultipleComponent]
    public class ChuanganQuXianRecent10MinutesChartManager : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int frameType = 6;
        [SerializeField] private int connectionTimeout = 30;

        [Header("查询条件")]
        public TextMeshProUGUI potNameText;
        public Button applyFilterButton;
        [SerializeField] private WorkAreaSelector workAreaSelector;
        [SerializeField] private SlotNumberSelector slotNumberSelector;

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
        [SerializeField] private bool useMockDataFallback = true;

        [Header("绘图设置")]
        [SerializeField] private int valueCountPerRecord = 48;
        [SerializeField] private int valuesPerChart = 4;
        [SerializeField] private int expectedBytesPerRecord = 96;
        [SerializeField] private int recentMinutes = 10;
        [SerializeField] private int sampleIntervalSeconds = 2;

        private readonly List<LineChart> chartInstances = new List<LineChart>();
        private readonly List<ParsedFrame> parsedFrames = new List<ParsedFrame>();
        private Coroutine bootstrapRoutine;
        private bool isLoading;
        private bool reloadRequested;
        private string selectedPotName;
        private readonly List<string> timeLabels = new List<string>();
        private readonly List<DateTime> timePoints = new List<DateTime>();

        private const int FrameHeaderSize = 12;
        private const int FrameTailSize = 2;

        [Serializable]
        private class ParsedFrame
        {
            public int enterpriseId;
            public int parsedFrameType;
            public int slot;
            public int dataLength;
            public bool isMock;
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
            if (slotNumberSelector == null)
            {
                slotNumberSelector = FindSceneComponent<SlotNumberSelector>();
            }
            if (workAreaSelector == null)
            {
                workAreaSelector = FindSceneComponent<WorkAreaSelector>();
            }

            if (slotNumberSelector != null && slotNumberSelector.OnSlotConfirmed != null)
            {
                slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
                slotNumberSelector.OnSlotConfirmed.AddListener(OnSlotConfirmed);
            }
            if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null)
            {
                workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);
                workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnWorkAreaConfirmed);
            }
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveListener(OnApplyFilter);
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

        private void OnSlotConfirmed(string slotNumber)
        {
            if (!string.IsNullOrWhiteSpace(slotNumber))
            {
                selectedPotName = slotNumber.Trim();
            }
            LoadTemperatureData();
        }

        private void OnWorkAreaConfirmed(string workArea)
        {
            if (slotNumberSelector == null) return;

            // 工区文本已由WorkAreaSelector更新，此时按新工区重建槽号列表。
            slotNumberSelector.RefreshSlotNumberData();
            string firstSlot = slotNumberSelector.GetSelectedSlotNumber();
            if (!string.IsNullOrWhiteSpace(firstSlot) && Regex.IsMatch(firstSlot, @"^\d+$"))
            {
                selectedPotName = firstSlot;
                if (potNameText != null) potNameText.text = firstSlot;
                LoadTemperatureData();
            }
        }

        private void OnDestroy()
        {
            if (slotNumberSelector != null && slotNumberSelector.OnSlotConfirmed != null)
            {
                slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
            }
            if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null)
            {
                workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);
            }
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveListener(OnApplyFilter);
            }
        }

        private void LoadTemperatureData()
        {
            if (isLoading)
            {
                reloadRequested = true;
                return;
            }
            if (!gameObject.activeInHierarchy)
            {
                ReportStatus("对象未激活，无法加载", true);
                return;
            }

            selectedPotName = ResolveSelectedSlotNumber();
            ClearCharts();
            parsedFrames.Clear();

            // 演示时先显示模拟曲线；真实接口返回有效非零数据后会自动替换。
            if (useMockDataFallback)
            {
                RenderMockData();
            }
            StartCoroutine(FetchAndRender());
        }

        private IEnumerator FetchAndRender()
        {
            isLoading = true;
            ShowLoading(true, $"正在读取最近{recentMinutes}分钟数据...");

            string requestCompanyId = !string.IsNullOrWhiteSpace(GlobalUserData.AlSerialPantId)
                ? GlobalUserData.AlSerialPantId
                : companyId;
            string requestParam = $"{requestCompanyId}-{frameType}-{selectedPotName}";
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
                    FinishLoading(useMockDataFallback ? null : "阳极电流数据请求失败");
                    yield break;
                }

                if (!TryParseResponse(responseText, out var frame))
                {
                    ReportStatus("响应解析失败，停止绘制", true);
                    FinishLoading(useMockDataFallback ? null : "暂无可用的阳极电流数据");
                    yield break;
                }

                bool hasRealValues = frame.records.Any(values => values != null && values.Any(value => value != 0));
                if (hasRealValues || !useMockDataFallback)
                {
                    parsedFrames.Clear();
                    parsedFrames.Add(frame);
                    ReportStatus($"真实帧解析完成: 记录数={frame.records.Count}, dataLength={frame.dataLength}");
                    RenderChartsFromFrames();
                }
                else
                {
                    Debug.LogWarning($"[ChuanganQuXianRecent10MinutesChartManager] Frame 6 返回全零值，保留模拟曲线，槽号={selectedPotName}");
                }
            }

            FinishLoading();
        }

        private void RenderMockData()
        {
            int pointCount = GetMaximumPointCount();
            int slot = 1001;
            int.TryParse(selectedPotName, out slot);
            int seed = Mathf.Abs(slot * 397) % 997;

            var frame = new ParsedFrame
            {
                enterpriseId = 0,
                parsedFrameType = frameType,
                slot = slot,
                dataLength = pointCount * expectedBytesPerRecord,
                isMock = true
            };

            for (int point = 0; point < pointCount; point++)
            {
                int[] values = new int[valueCountPerRecord];
                float elapsedSeconds = point * Mathf.Max(1, sampleIntervalSeconds);
                for (int sensor = 0; sensor < valueCountPerRecord; sensor++)
                {
                    int localSeries = sensor % valuesPerChart;
                    bool isBGroup = sensor >= valueCountPerRecord / 2;
                    float[] aBases = { 500f, 535f, 590f, 430f };
                    float[] bBases = { 430f, 410f, 375f, 525f };
                    float baseCurrent = (isBGroup ? bBases : aBases)[localSeries];
                    baseCurrent += ((sensor / valuesPerChart) % 6) * 5f;

                    float phase = sensor * 0.37f + seed * 0.01f;
                    float slowWave = Mathf.Sin(elapsedSeconds * Mathf.PI * 2f / 300f + phase) * 32f;
                    float mediumWave = Mathf.Sin(elapsedSeconds * Mathf.PI * 2f / 90f + sensor * 0.21f) * 12f;
                    float fineWave = Mathf.Sin(elapsedSeconds * Mathf.PI * 2f / 35f + sensor * 0.53f) * 4f;
                    values[sensor] = Mathf.Clamp(
                        Mathf.RoundToInt(baseCurrent + slowWave + mediumWave + fineWave),
                        280,
                        780);
                }
                frame.records.Add(values);
            }

            parsedFrames.Clear();
            parsedFrames.Add(frame);
            RenderChartsFromFrames();
            ReportStatus($"已生成阳极电流模拟曲线：槽号={slot}，时间点={pointCount}");
        }

        private void FinishLoading(string message = null)
        {
            ShowLoading(!string.IsNullOrEmpty(message), message);
            isLoading = false;
            ReportStatus($"绘图完成: {chartInstances.Count} 张图");

            if (reloadRequested)
            {
                reloadRequested = false;
                LoadTemperatureData();
            }
        }

        private T FindSceneComponent<T>() where T : Component
        {
            return FindObjectsOfType<T>(true)
                .FirstOrDefault(component => component != null && component.gameObject.scene == gameObject.scene);
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

            if (parsedFrameType != frameType)
            {
                ReportStatus($"帧类型校验失败: 实际={parsedFrameType}, 期望={frameType}", true);
                return false;
            }

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
                dataLength = dataLength
            };

            if (expectedBytesPerRecord != valueCountPerRecord * 2 || dataLength % expectedBytesPerRecord != 0)
            {
                ReportStatus($"Frame 6 数据长度不符合预期: dataLength={dataLength}, pointSize={expectedBytesPerRecord}", true);
                return false;
            }

            int recordCount = dataLength / expectedBytesPerRecord;
            ReportStatus($"开始逐条解析真实记录，recordCount={recordCount}");
            for (int recordIndex = 0; recordIndex < recordCount; recordIndex++)
            {
                int recordOffset = offset + recordIndex * expectedBytesPerRecord;
                if (recordOffset + expectedBytesPerRecord > offset + dataLength) break;

                int[] values = new int[valueCountPerRecord];
                for (int i = 0; i < valueCountPerRecord; i++)
                {
                    values[i] = ReadUInt16(frameData, recordOffset + i * 2);
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
            int chartCount = Mathf.CeilToInt((float)valueCountPerRecord / valuesPerChart);
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

            BuildTimeLabels(frame.records.Count);
            ReportStatus($"总时间点={frame.records.Count}，本次绘制={timeLabels.Count}点，采样间隔={sampleIntervalSeconds}秒");
            ReportStatus($"图表数量={chartCount}，每图曲线数={valuesPerChart}，总值数={valueCountPerRecord}");
            if (timeLabels.Count > 0)
            {
                ReportStatus($"绘制时间范围：{timeLabels[0]} ~ {timeLabels[timeLabels.Count - 1]}");
            }

            for (int i = 0; i < chartCount; i++)
            {
                var meta = GetGroupMeta(i);
                ReportStatus($"准备绘制图{i + 1}/{chartCount}: {GetBusinessChartTitle(i)}，曲线={GetSeriesName(meta.groupPrefix, meta.groupIndex, 1)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 2)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 3)}, {GetSeriesName(meta.groupPrefix, meta.groupIndex, 4)}");
                CreateSingleChart(frame, i);
            }
        }

        /// <summary>
        /// 生成最近N分钟的X轴标签，最后一个时间点对应当前时间。
        /// </summary>
        private void BuildTimeLabels(int totalRecordCount)
        {
            timeLabels.Clear();
            timePoints.Clear();
            int count = Mathf.Min(GetMaximumPointCount(), totalRecordCount);
            if (count <= 0) return;

            DateTime now = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                DateTime t = now.AddSeconds(-(count - 1 - i) * sampleIntervalSeconds);
                timeLabels.Add(t.ToString("HH:mm:ss"));
                timePoints.Add(t);
            }
        }

        private int GetMaximumPointCount()
        {
            return Mathf.Max(1, recentMinutes * 60 / Mathf.Max(1, sampleIntervalSeconds));
        }

        /// <summary>本次实际绘制的数据点数。</summary>
        private int GetPointCount(ParsedFrame frame)
        {
            return Mathf.Min(GetMaximumPointCount(), frame.records.Count);
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

            // ---- X轴：使用真实时间轴，数据点按时间戳定位 ----
            var xAxis = chart.EnsureChartComponent<XAxis>();
            if (xAxis != null)
            {
                xAxis.type = Axis.AxisType.Time;
                xAxis.axisLine.show = true;
                xAxis.axisTick.show = true;
                xAxis.boundaryGap = false;
                xAxis.splitNumber = 5;
                if (xAxis.axisLabel != null)
                {
                    xAxis.axisLabel.show = true;
                    xAxis.axisLabel.interval = 0;
                    xAxis.axisLabel.numericFormatter = "date:HH:mm";
                    xAxis.axisLabel.showStartLabel = true;
                    xAxis.axisLabel.showEndLabel = true;
                    xAxis.axisLabel.rotate = 0;
                    if (xAxis.axisLabel.textStyle != null)
                    {
                        xAxis.axisLabel.textStyle.color = Color.white;
                        xAxis.axisLabel.textStyle.fontSize = 14;
                        xAxis.axisLabel.textStyle.fontStyle = FontStyle.Bold;
                    }
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
                if (frame.isMock)
                {
                    yAxis.min = 0;
                    yAxis.max = 900;
                    yAxis.splitNumber = 9;
                }
                else
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
                }

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
                tooltip.trigger = Tooltip.Trigger.Axis;
                tooltip.triggerOn = Tooltip.TriggerOn.MouseMove;
                tooltip.showContent = true;
                // 当前图表字体不含中文提示文字，标题直接显示时间，避免出现方框乱码。
                tooltip.titleFormatter = "{b}";
                // 使用单列内容，避免三列布局在窄屏上把数值挤出背景框。
                tooltip.itemFormatter = "{.} {a}  {c}";
                tooltip.numericFormatter = "F0";
                tooltip.backgroundColor = new Color32(255, 255, 255, 248);
                tooltip.borderColor = new Color32(170, 170, 170, 255);
                tooltip.borderWidth = 1f;
                tooltip.fixedWidth = 0f;
                tooltip.minWidth = 140f;
                tooltip.paddingLeftRight = 8;
                tooltip.paddingTopBottom = 6;
                tooltip.titleHeight = 19f;
                tooltip.itemHeight = 19f;
                if (tooltip.titleLabelStyle != null && tooltip.titleLabelStyle.textStyle != null)
                {
                    tooltip.titleLabelStyle.textStyle.color = new Color32(40, 40, 40, 255);
                    tooltip.titleLabelStyle.textStyle.fontSize = 13;
                    tooltip.titleLabelStyle.textStyle.alignment = TextAnchor.MiddleLeft;
                }
                if (tooltip.contentLabelStyles != null)
                {
                    foreach (var labelStyle in tooltip.contentLabelStyles)
                    {
                        if (labelStyle != null && labelStyle.textStyle != null)
                        {
                            labelStyle.textStyle.color = new Color32(40, 40, 40, 255);
                            labelStyle.textStyle.fontSize = 13;
                            labelStyle.textStyle.alignment = TextAnchor.MiddleLeft;
                        }
                    }
                }
                if (tooltip.lineStyle != null)
                {
                    tooltip.lineStyle.show = true;
                    tooltip.lineStyle.width = 2f;
                }
            }

            Color[] colors =
            {
                new Color32(50, 105, 255, 255),
                new Color32(65, 190, 90, 255),
                new Color32(255, 174, 45, 255),
                new Color32(235, 65, 220, 255)
            };

            int pointCount = GetPointCount(frame);
            int startIndex = frame.records.Count - pointCount;

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
                    serie.lineStyle.width = 2f;
                }

                if (serie.itemStyle != null)
                {
                    serie.itemStyle.color = colors[s % colors.Length];
                }

                serie.lineType = LineType.Normal;

                if (serie.symbol != null)
                {
                    serie.symbol.show = false;
                }

                if (serie.areaStyle != null)
                {
                    serie.areaStyle.show = false;
                }

                serie.large = false;

                // 不聚合，完整保留Frame 6的每个2秒采样点。
                for (int i = 0; i < pointCount; i++)
                {
                    var pointValues = frame.records[startIndex + i];
                    DateTime pointTime = i < timePoints.Count
                        ? timePoints[i]
                        : DateTime.Now.AddSeconds(-(pointCount - 1 - i) * sampleIntervalSeconds);
                    string pointName = pointTime.ToString("HH:mm:ss");
                    if (pointValues != null && pointValues.Length > globalSensorIndex)
                    {
                        chart.AddData(s, pointTime, pointValues[globalSensorIndex], pointName);
                    }
                    else
                    {
                        chart.AddData(s, pointTime, 0, pointName);
                    }
                }
            }

            chart.RefreshChart();
            chart.SetAllDirty();
        }

        private List<double> ExtractValuesForChart(ParsedFrame frame, int startSensorIndex, int endSensorIndex)
        {
            var result = new List<double>();
            if (frame == null || frame.records == null || frame.records.Count == 0) return result;

            startSensorIndex = Mathf.Clamp(startSensorIndex, 0, valueCountPerRecord - 1);
            endSensorIndex = Mathf.Clamp(endSensorIndex, startSensorIndex, valueCountPerRecord - 1);

            // 仅提取当前绘制范围内的数据。
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
            int chartStart = chartIndex * valuesPerChart;
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

        private int ReadInt32(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
