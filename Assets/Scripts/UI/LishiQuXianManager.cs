using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;
using XUGL;

namespace Aeht.UI
{
    /// <summary>
    /// 历史曲线：查询 Frame 71 并按旧版的电压、电流、下料和作业状态绘图。
    /// </summary>
    public class LishiQuXianManager : MonoBehaviour
    {
        [Header("图表")]
        [SerializeField] private LineChart historyChart;
        [SerializeField] private Transform chartContainer;
        [SerializeField] private GameObject chartPrefab;

        [Header("字体")]
        [SerializeField] private TMP_FontAsset chineseFontAsset;

        [Header("历史数据")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int requestTimeoutSeconds = 30;
        [SerializeField] private riqSelector dateSelector;
        [SerializeField] private SlotNumberSelector slotSelector;
        [SerializeField] private WorkAreaSelector workAreaSelector;
        [SerializeField] private bool useMockData = false;

        private Coroutine loadRoutine;
        private Coroutine filterRoutine;
        private TextMeshProUGUI statusText;
        private GlobalElectrolyticCellDataManager slotConfigManager;

        // 整图缩放/拖拽状态（Update 中处理）
        [Header("整图缩放")]
        [SerializeField] private float minScale = 0.5f;   // 最小缩放
        [SerializeField] private float maxScale = 3f;     // 最大缩放
        [SerializeField] private float scrollWheelSpeed = 0.1f; // 滚轮每格缩放幅度(10%)
        [SerializeField] private float rmbDragZoomSpeed = 0.006f; // 右键拖动每像素缩放幅度
        private bool m_IsPanning;
        private bool m_IsRmbZooming;
        private Vector2 m_LastMousePos;

        private void Start()
        {
            if (chartContainer == null)
                chartContainer = transform;

            if (historyChart == null && chartPrefab != null)
            {
                var go = Instantiate(chartPrefab, chartContainer);
                go.name = "HistoryChart";
                historyChart = go.GetComponent<LineChart>();
            }

            if (historyChart == null)
            {
                Debug.LogError("[LishiQuXianManager] historyChart 未绑定，且无法自动创建");
                return;
            }

            dateSelector = dateSelector != null ? dateSelector : FindObjectOfType<riqSelector>();
            slotSelector = slotSelector != null ? slotSelector : FindObjectOfType<SlotNumberSelector>();
            workAreaSelector = workAreaSelector != null ? workAreaSelector : FindObjectOfType<WorkAreaSelector>();
            if (dateSelector != null && dateSelector.OnDateConfirmed != null) dateSelector.OnDateConfirmed.AddListener(OnFilterChanged);
            if (slotSelector != null && slotSelector.OnSlotConfirmed != null) slotSelector.OnSlotConfirmed.AddListener(OnFilterChanged);
            if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null) workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnFilterChanged);
            slotConfigManager = GlobalElectrolyticCellDataManager.Instance;
            if (slotConfigManager != null) slotConfigManager.OnDataUpdated += OnSlotConfigurationUpdated;

            EnsureStatusText();
            if (!useMockData) historyChart.ClearData();
            if (useMockData) RenderMockData();
            else StartCoroutine(LoadAfterSelectors());

            // 创建右下角浮动缩放按钮（不依赖鼠标焦点，UGUI 按钮 100% 可靠）
            EnsureZoomControls();
        }

        private void OnDestroy()
        {
            if (dateSelector != null && dateSelector.OnDateConfirmed != null) dateSelector.OnDateConfirmed.RemoveListener(OnFilterChanged);
            if (slotSelector != null && slotSelector.OnSlotConfirmed != null) slotSelector.OnSlotConfirmed.RemoveListener(OnFilterChanged);
            if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null) workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnFilterChanged);
            if (slotConfigManager != null) slotConfigManager.OnDataUpdated -= OnSlotConfigurationUpdated;
        }

        private void OnFilterChanged(string _) => ScheduleRefresh();

        private void OnSlotConfigurationUpdated() => ScheduleRefresh();

        private void ScheduleRefresh()
        {
            if (!isActiveAndEnabled || useMockData) return;
            if (filterRoutine != null) StopCoroutine(filterRoutine);
            filterRoutine = StartCoroutine(RefreshAfterFilterFrame());
        }

        private IEnumerator RefreshAfterFilterFrame()
        {
            yield return null; // allow work-area selection to update the slot selector
            filterRoutine = null;
            RefreshHistory();
        }

        private IEnumerator LoadAfterSelectors()
        {
            yield return null;
            RefreshHistory();
        }

        public void RefreshHistory()
        {
            if (!isActiveAndEnabled || useMockData || historyChart == null) return;
            if (loadRoutine != null) StopCoroutine(loadRoutine);
            loadRoutine = StartCoroutine(FetchHistory());
        }

        private IEnumerator FetchHistory()
        {
            int slot = slotSelector != null ? slotSelector.GetSelectedSlotNumberValue() : -1;
            if (slot <= 0 || slot > ushort.MaxValue)
            {
                historyChart.ClearData();
                ShowStatus("请选择有效槽号");
                yield break;
            }

            string date = dateSelector != null && dateSelector.selectedDisplayText != null
                ? dateSelector.selectedDisplayText.text.Trim().Replace("-", "").Replace("/", "")
                : DateTime.Today.ToString("yyyyMMdd");
            if (!DateTime.TryParseExact(date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
            {
                historyChart.ClearData();
                ShowStatus("请选择有效日期");
                yield break;
            }

            ShowStatus("正在加载历史曲线…");
            string enterprise = !string.IsNullOrWhiteSpace(GlobalUserData.AlSerialPantId)
                ? GlobalUserData.AlSerialPantId.Trim() : companyId;
            string parameter = $"{enterprise}-71-{slot}-{date}";
            using (var request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes($"\"{parameter}\""));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = requestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    historyChart.ClearData();
                    ShowStatus($"历史数据请求失败：{request.error}");
                }
                else if (!HistoryCurveFrame71.TryParse(request.downloadHandler.text, slot, out var samples))
                {
                    historyChart.ClearData();
                    ShowStatus("该槽号与日期暂无历史曲线数据");
                }
                else
                {
                    RenderHistory(samples);
                    ShowStatus(string.Empty);
                }
            }
            loadRoutine = null;
        }

        private void EnsureStatusText()
        {
            if (chartContainer == null) return;
            var go = new GameObject("HistoryDataStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(chartContainer, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.20f, 0.42f);
            rect.anchorMax = new Vector2(0.80f, 0.58f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            statusText = go.GetComponent<TextMeshProUGUI>();
            statusText.font = chineseFontAsset;
            statusText.fontSize = 26;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            statusText.raycastTarget = false;
        }

        private void ShowStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        // ==================== 浮动缩放按钮（保证可用） ====================
        private void EnsureZoomControls()
        {
            if (chartContainer == null) return;
            // 防止重复创建（HUD 开/关、热重载时）
            if (chartContainer.Find("LishiZoomPanel") != null) return;

            // 确保场景里有 EventSystem（按钮需要）
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                es.transform.SetParent(null, false);
            }

            // 容器：右上角竖排按钮组（这里用 chartContainer 作锚，跟随 chartContainer 大小）
            var panel = new GameObject("LishiZoomPanel", typeof(RectTransform));
            panel.transform.SetParent(chartContainer, false);
            var panelRT = (RectTransform)panel.transform;
            panelRT.anchorMin = new Vector2(1, 1);
            panelRT.anchorMax = new Vector2(1, 1);
            panelRT.pivot = new Vector2(1, 1);
            panelRT.anchoredPosition = new Vector2(-10, -10);
            panelRT.sizeDelta = new Vector2(56, 180);

            AddZoomButton(panel.transform, "+", 0, () =>
            {
                if (historyChart == null || historyChart.rectTransform == null) return;
                ScaleChartAroundCenter(historyChart.rectTransform, 1.2f);
            });
            AddZoomButton(panel.transform, "-", 1, () =>
            {
                if (historyChart == null || historyChart.rectTransform == null) return;
                ScaleChartAroundCenter(historyChart.rectTransform, 0.83f);
            });
            AddZoomButton(panel.transform, "R", 2, () =>
            {
                if (historyChart == null || historyChart.rectTransform == null) return;
                historyChart.rectTransform.localScale = Vector3.one;
                historyChart.rectTransform.anchoredPosition = Vector2.zero;
            });
        }

        private void AddZoomButton(Transform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -index * 60);
            rt.sizeDelta = new Vector2(-4, 52); // 左右各缩 2px 间距

            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // 文字
            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRT = (RectTransform)textGo.transform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // The Java version uses 720 two-minute positions, with gaps where a
        // sample was not recorded. Keep that scale so the date and event flags
        // line up with the original chart.
        private void RenderHistory(List<HistoryCurveFrame71.Sample> samples)
        {
            var byIndex = new Dictionary<int, HistoryCurveFrame71.Sample>();
            foreach (var sample in samples) byIndex[sample.recordIndex] = sample;

            var chart = historyChart;
            chart.RemoveData();
            chart.RemoveAllSerie();
            chart.RemoveChartComponents<YAxis>();
            chart.RemoveChartComponents<GridCoord>();
            chart.RemoveChartComponents<Legend>();

            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;
            legend.location.align = Location.Align.TopCenter;
            legend.location.top = 0.01f;
            legend.orient = Orient.Horizonal;
            legend.selectedMode = Legend.SelectedMode.Multiple;
            legend.data = new List<string> { "工作电压", "电压上限", "电压下限", "系列电流", "实际下料", "基准下料", "作业状态" };
            legend.labelStyle.textStyle.color = Color.white;
            legend.labelStyle.textStyle.fontSize = 13;
            ApplyFont(legend.labelStyle.textStyle);

            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.show = true;
            grid.left = 60;
            grid.right = 65;
            grid.top = 68;
            grid.bottom = 65;
            grid.borderWidth = 1f;
            grid.borderColor = new Color32(130, 130, 130, 160);

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.show = true;
            xAxis.type = Axis.AxisType.Category;
            xAxis.boundaryGap = false;
            xAxis.axisLabel.show = true;
            xAxis.axisLabel.interval = 59;
            xAxis.axisLabel.textStyle.color = Color.white;
            xAxis.axisLabel.textStyle.fontSize = 13;
            ApplyFont(xAxis.axisLabel.textStyle);
            xAxis.splitLine.show = true;
            xAxis.splitLine.interval = 59;

            float average = (float)samples.Average(s => s.voltage);
            float voltageMin = Mathf.Floor((average - 600f) / 100f) * 100f;
            float voltageMax = voltageMin + 1000f;
            var leftAxis = chart.EnsureChartComponent<YAxis>();
            leftAxis.show = true;
            leftAxis.type = Axis.AxisType.Value;
            leftAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            leftAxis.min = voltageMin;
            leftAxis.max = voltageMax;
            leftAxis.splitNumber = 10;
            leftAxis.axisLabel.show = true;
            leftAxis.axisLabel.textStyle.color = Color.white;
            ApplyFont(leftAxis.axisLabel.textStyle);

            var rightAxis = chart.AddChartComponent<YAxis>();
            rightAxis.show = true;
            rightAxis.type = Axis.AxisType.Value;
            rightAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            rightAxis.min = 0;
            float maxSecondary = samples.Max(s => Mathf.Max(s.seriesCurrent / 10f,
                Mathf.Max(s.actualFeedingInterval, s.baseFeedingInterval)));
            rightAxis.max = Mathf.Ceil(Mathf.Max(50f, maxSecondary + 50f) / 50f) * 50f;
            rightAxis.splitNumber = 5;
            rightAxis.position = Axis.AxisPosition.Right;
            rightAxis.axisLabel.show = true;
            rightAxis.axisLabel.textStyle.color = Color.white;
            ApplyFont(rightAxis.axisLabel.textStyle);

            AddHistoryLine(chart, "工作电压", 0, new Color32(255, 90, 105, 255), 2f);
            AddHistoryLine(chart, "电压上限", 0, new Color32(255, 188, 64, 255), 1f);
            AddHistoryLine(chart, "电压下限", 0, new Color32(255, 188, 64, 255), 1f);
            AddHistoryLine(chart, "系列电流", 1, new Color32(86, 205, 255, 255), 1.5f);
            AddHistoryLine(chart, "实际下料", 1, new Color32(175, 124, 255, 255), 1.5f);
            AddHistoryLine(chart, "基准下料", 1, new Color32(128, 224, 129, 255), 1f);
            var events = chart.AddSerie<Scatter>("作业状态");
            events.yAxisIndex = 0;
            events.symbol.show = false;
            events.animation.enable = false;

            uint lastSpecial = 0, lastFeeding = 0, lastAnode = 0;
            for (int record = 1; record <= 720; record++)
            {
                int minutes = (record - 1) * 2;
                chart.AddXAxisData($"{minutes / 60:D2}:{minutes % 60:D2}");
                if (!byIndex.TryGetValue(record, out var sample))
                {
                    for (int series = 0; series < 7; series++) chart.AddData(series, 0).ignore = true;
                    continue;
                }

                chart.AddData(0, sample.voltage);
                chart.AddData(1, sample.voltageUpper);
                chart.AddData(2, sample.voltageLower);
                chart.AddData(3, sample.seriesCurrent / 10f);
                chart.AddData(4, sample.actualFeedingInterval);
                chart.AddData(5, sample.baseFeedingInterval);

                string eventLabel = GetEventLabel(sample, lastSpecial, lastFeeding, lastAnode);
                lastSpecial = sample.specialState;
                lastFeeding = sample.feedingState;
                lastAnode = sample.anodeMoveState;
                if (eventLabel.Length == 0) chart.AddData(6, 0).ignore = true;
                else
                {
                    var point = chart.AddData(6, voltageMin + 80f);
                    var label = point.EnsureComponent<LabelStyle>();
                    label.show = true;
                    label.formatter = eventLabel;
                    label.textStyle.color = new Color32(116, 235, 150, 255);
                    label.textStyle.fontSize = 11;
                    ApplyFont(label.textStyle);
                }
            }
            xAxis.SetAllDirty();
            chart.onDrawAfterSerie = null;
            chart.RefreshChart();
        }

        private static string GetEventLabel(HistoryCurveFrame71.Sample sample,
            uint previousSpecial, uint previousFeeding, uint previousAnode)
        {
            // Bit meanings follow hiscurvetools.js: special, feeding, anode move.
            uint special = sample.specialState & ~previousSpecial;
            uint feeding = sample.feedingState & ~previousFeeding;
            uint anode = sample.anodeMoveState & ~previousAnode;
            if ((special & 1) != 0) return "AE";
            if ((special & 2) != 0) return "出铝";
            if ((special & 4) != 0) return "换极";
            if ((special & 16) != 0) return "母线";
            if ((special & 32) != 0) return "手动";
            if ((feeding & 2) != 0) return "AEB";
            if ((feeding & 1) != 0) return "(AEB)";
            if ((feeding & 32) != 0) return "AlF";
            if ((feeding & 16) != 0) return "(AlF)";
            if ((anode & 2) != 0) return "自动↑";
            if ((anode & 1) != 0) return "手动↑";
            if ((anode & 8) != 0) return "自动↓";
            if ((anode & 4) != 0) return "手动↓";
            return string.Empty;
        }

        private static void AddHistoryLine(LineChart chart, string title, int axis, Color color, float width)
        {
            var series = chart.AddSerie<Line>(title);
            series.yAxisIndex = axis;
            series.lineStyle.width = width;
            series.lineStyle.color = color;
            series.itemStyle.color = color;
            series.symbol.show = false;
            series.animation.enable = false;
        }

        // ==================== 模拟数据 ====================
        private void RenderMockData()
        {
            int pointCount = 24; // 24小时
            var voltageData = new List<float>();    // 红色折线：工作电压 ~4300
            var tempData = new List<float>();       // 紫色折线：槽温 ~4000 波动
            var stateData = new List<float>();      // 白色折线：状态 ~3600-3700 方波
            var abnormalIndices = new List<int>();  // 异常点索引
            var afLabels = new List<(int index, string text, Color color)>(); // AF标记
            var controlStates = new List<(int index, bool automatic, bool upward)>();
            var bar1Data = new List<float>();       // 底部紫色条
            var bar2Data = new List<float>();       // 底部红色条（出铝）

            System.Random rnd = new System.Random(42);

            for (int i = 0; i < pointCount; i++)
            {
                // 工作电压：4300 附近，微波动
                voltageData.Add(4300f + (float)(rnd.NextDouble() * 20 - 10));

                // 槽温：4000 附近，大波动
                float temp = 4000f + (float)(rnd.NextDouble() * 100 - 50);
                tempData.Add(temp);

                // 异常点：随机在槽温上标记几个低点
                if (rnd.NextDouble() < 0.15)
                {
                    abnormalIndices.Add(i);
                    tempData[i] -= 80f; // 让异常点更低
                }

                // 状态：3600-3700 方波
                stateData.Add((i % 3 == 0) ? 3700f : 3600f);

                // 仅在4个代表性时间点显示状态箭头，避免24个箭头挤在一起。
                if (i >= 2 && (i - 2) % 6 == 0)
                {
                    int stateIndex = (i - 2) / 6;
                    controlStates.Add((i, stateIndex < 2, stateIndex % 2 == 0));
                }

                // AF 标记：随机绿色/红色 AF
                if (rnd.NextDouble() < 0.4)
                {
                    bool isRed = rnd.NextDouble() < 0.3;
                    afLabels.Add((i, "AF", isRed ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f)));
                }

// 底部条带（出铝固定值，一条水平虚线）
            bar1Data.Add(50f);
            bar2Data.Add(8f);       // 出铝：固定值8，画水平虚线
            }

            RenderChart(voltageData, tempData, stateData, abnormalIndices, afLabels, controlStates, bar1Data, bar2Data);
        }

        // ==================== 核心渲染 ====================
        private void RenderChart(
            List<float> voltageData,
            List<float> tempData,
            List<float> stateData,
            List<int> abnormalIndices,
            List<(int index, string text, Color color)> afLabels,
            List<(int index, bool automatic, bool upward)> controlStates,
            List<float> bar1Data,
            List<float> bar2Data)
        {
            var chart = historyChart;
            chart.RemoveData();
            chart.RemoveAllSerie();
            chart.RemoveChartComponents<YAxis>();
            chart.RemoveChartComponents<GridCoord>();
            chart.RemoveChartComponents<Legend>();

            // ========== 0. 原曲线图例 ==========
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;
            legend.location.align = Location.Align.TopCenter;
            legend.location.top = 0.01f;
            legend.orient = Orient.Horizonal;
            legend.selectedMode = Legend.SelectedMode.Multiple;
            legend.itemWidth = 18f;
            legend.itemHeight = 10f;
            legend.itemGap = 10f;
            legend.data = new List<string> { "工作电压", "槽温", "异常点", "状态", "出铝", "AF标记" };
            legend.textLimit.enable = false;
            if (legend.labelStyle.textStyle == null)
                legend.labelStyle.textStyle = new TextStyle();
            legend.labelStyle.textStyle.color = Color.white;
            legend.labelStyle.textStyle.fontSize = 13;
            ApplyFont(legend.labelStyle.textStyle);

            // 状态图例独立显示，不覆盖原来的曲线图例。
            var statusLegend = chart.AddChartComponent<Legend>();
            statusLegend.show = true;
            statusLegend.location.align = Location.Align.TopLeft;
            statusLegend.location.left = 0.02f;
            statusLegend.location.top = 0.065f;
            statusLegend.orient = Orient.Horizonal;
            statusLegend.selectedMode = Legend.SelectedMode.None;
            statusLegend.itemWidth = 12f;
            statusLegend.itemHeight = 10f;
            statusLegend.itemGap = 8f;
            statusLegend.data = new List<string> { "自动↑", "自动↓", "手动↑", "手动↓" };
            statusLegend.textLimit.enable = true;
            statusLegend.textLimit.maxWidth = 52f;
            statusLegend.textLimit.suffix = string.Empty;
            if (statusLegend.labelStyle.textStyle == null)
                statusLegend.labelStyle.textStyle = new TextStyle();
            statusLegend.labelStyle.textStyle.color = Color.white;
            statusLegend.labelStyle.textStyle.fontSize = 12;
            ApplyFont(statusLegend.labelStyle.textStyle);

            // ========== 1. Grid ==========
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.show = true;
            grid.left = 60;
            grid.right = 60;
            grid.top = 72;
            grid.bottom = 80;
            grid.borderWidth = 1f;
            grid.borderColor = new Color32(120, 120, 120, 150);

            // ========== 2. X轴 ==========
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.show = true;
            xAxis.type = Axis.AxisType.Category;
            xAxis.splitNumber = 12;
            xAxis.boundaryGap = true;
            xAxis.axisLabel.show = true;
            xAxis.axisLabel.interval = 1;
            if (xAxis.axisLabel.textStyle == null)
                xAxis.axisLabel.textStyle = new TextStyle();
            xAxis.axisLabel.textStyle.color = Color.white;
            xAxis.axisLabel.textStyle.fontSize = 16;
            ApplyFont(xAxis.axisLabel.textStyle);

            xAxis.splitLine.show = true;
            xAxis.splitLine.interval = 0;
            if (xAxis.splitLine.lineStyle == null)
                xAxis.splitLine.lineStyle = new LineStyle();
            xAxis.splitLine.lineStyle.color = new Color32(120, 120, 120, 150);
            xAxis.splitLine.lineStyle.width = 0.5f;

            for (int i = 0; i < 24; i++)
            {
                chart.AddXAxisData($"{i:D2}:00");
            }
            xAxis.SetAllDirty();

            // ========== 3. 左 Y 轴 ==========
            var yAxis1 = chart.EnsureChartComponent<YAxis>();
            yAxis1.show = true;
            yAxis1.type = Axis.AxisType.Value;
            yAxis1.min = 3400;
            yAxis1.max = 4400;
            yAxis1.splitNumber = 10;
            yAxis1.axisLabel.show = true;
            if (yAxis1.axisLabel.textStyle == null)
                yAxis1.axisLabel.textStyle = new TextStyle();
            yAxis1.axisLabel.textStyle.color = Color.white;
            yAxis1.axisLabel.textStyle.fontSize = 16;
            ApplyFont(yAxis1.axisLabel.textStyle);
            yAxis1.splitLine.show = true;
            if (yAxis1.splitLine.lineStyle == null)
                yAxis1.splitLine.lineStyle = new LineStyle();
            yAxis1.splitLine.lineStyle.color = new Color32(120, 120, 120, 150);
            yAxis1.splitLine.lineStyle.width = 0.5f;

            // ========== 4. 右 Y 轴 ==========
            var yAxis2 = chart.AddChartComponent<YAxis>();
            yAxis2.show = true;
            yAxis2.type = Axis.AxisType.Value;
            yAxis2.minMaxType = Axis.AxisMinMaxType.Custom; // 固定 0-30，不自动缩放
            yAxis2.min = 0;
            yAxis2.max = 30;
            yAxis2.splitNumber = 5;
            yAxis2.axisLabel.show = true;
            if (yAxis2.axisLabel.textStyle == null)
                yAxis2.axisLabel.textStyle = new TextStyle();
            yAxis2.axisLabel.textStyle.color = Color.white;
            yAxis2.axisLabel.textStyle.fontSize = 16;
            ApplyFont(yAxis2.axisLabel.textStyle);
            yAxis2.position = Axis.AxisPosition.Right;

            // ========== 5. 整图缩放说明 ==========
            // 不做 DataZoom 数据窗口缩放。整张 LineChart 的放大/缩小/平移
            // 由 Update() 手动控制 localScale / anchoredPosition（见下方 Update）。
            // 缩放中心 = 鼠标位置；左键拖拽 = 平移；滚轮 = 缩放。

            // ========== 6. 红色折线：工作电压 ==========
            var serieVoltage = chart.AddSerie<Line>("工作电压");
            serieVoltage.yAxisIndex = 0;
            serieVoltage.lineStyle.width = 1.5f;
            serieVoltage.lineStyle.color = new Color(1f, 0.35f, 0.5f); // 粉红/红
            serieVoltage.itemStyle.color = serieVoltage.lineStyle.color;
            serieVoltage.symbol.show = false; // 不显示标记点
            serieVoltage.animation.enable = false;
            for (int i = 0; i < voltageData.Count; i++)
            {
                chart.AddData(0, voltageData[i]);
            }

            // ========== 6. 紫色折线：槽温（关闭内置符号，用 onDrawAfterSerie 自绘大头针） ==========
            var serieTemp = chart.AddSerie<Line>("槽温");
            serieTemp.yAxisIndex = 0;
            serieTemp.lineStyle.width = 2f;
            serieTemp.lineStyle.color = new Color(0.6f, 0.4f, 1f); // 紫色
            serieTemp.itemStyle.color = serieTemp.lineStyle.color;
            serieTemp.symbol.show = false; // 关闭内置符号（EmptyTriangle 是硬编码等边三角）
            serieTemp.animation.enable = false;
            for (int i = 0; i < tempData.Count; i++)
            {
                chart.AddData(1, tempData[i]);
            }

            // ========== 7. 亮蓝色 Plus 散点：异常点 ==========
            var serieAbnormal = chart.AddSerie<Scatter>("异常点");
            serieAbnormal.yAxisIndex = 0;
            serieAbnormal.symbol.show = true;
            serieAbnormal.symbol.type = SymbolType.Plus;
            serieAbnormal.symbol.size = 12f;
            serieAbnormal.symbol.color = new Color32(41, 182, 246, 255); // 亮蓝色，与红绿箭头区分
            serieAbnormal.itemStyle.color = new Color32(41, 182, 246, 255);
            serieAbnormal.symbol.borderWidth = 2f;
            for (int i = 0; i < 24; i++)
            {
                if (abnormalIndices.Contains(i))
                    chart.AddData(2, tempData[i]);
                else
                {
                    var sd = chart.AddData(2, 0);
                    sd.ignore = true;
                }
            }

            // ========== 8. 白色折线：状态 ==========
            var serieState = chart.AddSerie<Line>("状态");
            serieState.yAxisIndex = 0;
            serieState.lineStyle.width = 1.5f;
            serieState.lineStyle.color = Color.white;
            serieState.itemStyle.color = Color.white;
            serieState.symbol.show = false;
            serieState.animation.enable = false;
            for (int i = 0; i < stateData.Count; i++)
            {
                chart.AddData(3, stateData[i]);
            }

            // ========== 9. 红色虚线（出铝） ==========
            var serieBar2 = chart.AddSerie<Line>("出铝");
            serieBar2.yAxisIndex = 1; // 右Y轴
            serieBar2.lineStyle.width = 2.5f;
            serieBar2.lineStyle.type = LineStyle.Type.Dashed;
            serieBar2.lineStyle.color = new Color(1f, 0.3f, 0.3f, 1f); // 红色，不透明
            serieBar2.itemStyle.color = serieBar2.lineStyle.color;
            serieBar2.symbol.show = false; // 不显示数据点
            serieBar2.lineType = LineType.Smooth; // 平滑曲线
            serieBar2.animation.enable = false;
            for (int i = 0; i < bar2Data.Count; i++)
            {
                chart.AddData(4, bar2Data[i]);
            }

            // ========== 10. AF 标记（Scatter + Label） ==========
            var serieAF = chart.AddSerie<Scatter>("AF标记");
            serieAF.yAxisIndex = 0;
            serieAF.symbol.show = false; // 不显示图形，只显示文字
            serieAF.itemStyle.color = new Color32(70, 210, 95, 255);
            for (int i = 0; i < 24; i++)
            {
                var af = afLabels.FirstOrDefault(a => a.index == i);
                if (af.text != null)
                {
                    var sd = chart.AddData(5, 3450f); // 放在底部
                    var label = sd.EnsureComponent<LabelStyle>();
                    label.show = true;
                    label.formatter = af.text;
                    label.textStyle.color = af.color;
                    label.textStyle.fontSize = 12;
                    ApplyFont(label.textStyle);
                    label.offset = new Vector3(0, 0, 0);
                }
                else
                {
                    var sd = chart.AddData(5, 0);
                    sd.ignore = true;
                }
            }

            Color autoColor = new Color32(70, 210, 95, 255);
            Color manualColor = new Color32(245, 75, 75, 255);
            AddStatusLegendSerie(chart, "自动↑", autoColor);
            AddStatusLegendSerie(chart, "自动↓", autoColor);
            AddStatusLegendSerie(chart, "手动↑", manualColor);
            AddStatusLegendSerie(chart, "手动↓", manualColor);

            // ========== 12. 自定义绘制：槽温曲线的状态竖线和方向箭头 ==========
            chart.onDrawAfterSerie = (vh, serie) =>
            {
                if (serie.serieName != "槽温") return;
                float headHalfWidth = 7.5f;
                float headHeight = 10f;
                float shaftHalfWidth = 2.5f;
                float shaftLength = 12f;

                foreach (var marker in controlStates)
                {
                    if (marker.index < 0 || marker.index >= serie.data.Count) continue;
                    var sd = serie.data[marker.index];
                    var pt = sd.context.position;
                    if (pt == Vector3.zero) continue;

                    Color markerColor = marker.automatic ? autoColor : manualColor;
                    // 只绘制箭头本身：尾杆端点直接落在折线数据点，不再增加连接线。
                    if (marker.upward)
                    {
                        float baseY = pt.y + shaftLength;
                        float tipY = baseY + headHeight;
                        UGL.DrawQuadrilateral(vh,
                            new Vector3(pt.x - shaftHalfWidth, pt.y, pt.z),
                            new Vector3(pt.x + shaftHalfWidth, pt.y, pt.z),
                            new Vector3(pt.x + shaftHalfWidth, baseY, pt.z),
                            new Vector3(pt.x - shaftHalfWidth, baseY, pt.z),
                            markerColor);
                        UGL.DrawTriangle(vh,
                            new Vector3(pt.x - headHalfWidth, baseY, pt.z),
                            new Vector3(pt.x + headHalfWidth, baseY, pt.z),
                            new Vector3(pt.x, tipY, pt.z),
                            markerColor);
                    }
                    else
                    {
                        float baseY = pt.y - shaftLength;
                        float tipY = baseY - headHeight;
                        UGL.DrawQuadrilateral(vh,
                            new Vector3(pt.x - shaftHalfWidth, pt.y, pt.z),
                            new Vector3(pt.x + shaftHalfWidth, pt.y, pt.z),
                            new Vector3(pt.x + shaftHalfWidth, baseY, pt.z),
                            new Vector3(pt.x - shaftHalfWidth, baseY, pt.z),
                            markerColor);
                        UGL.DrawTriangle(vh,
                            new Vector3(pt.x - headHalfWidth, baseY, pt.z),
                            new Vector3(pt.x + headHalfWidth, baseY, pt.z),
                            new Vector3(pt.x, tipY, pt.z),
                            markerColor);
                    }
                }
            };

            // 回调设置完成后再刷新，确保首帧就能绘制状态标识。
            chart.RefreshChart();
        }

        private static void AddStatusLegendSerie(LineChart chart, string name, Color color)
        {
            var serie = chart.AddSerie<Scatter>(name);
            serie.yAxisIndex = 0;
            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Arrow;
            serie.symbol.size = 10f;
            serie.symbol.color = color;
            serie.itemStyle.color = color;
            serie.animation.enable = false;
        }

        private void ApplyFont(TextStyle textStyle)
        {
            if (chineseFontAsset != null && textStyle != null)
            {
#if dUI_TextMeshPro
                textStyle.tmpFont = chineseFontAsset;
#else
                textStyle.font = chineseFontAsset.sourceFontFile;
#endif
            }
        }

        // ==================== 整图缩放/平移 ====================
        // PC：鼠标滚轮 = 缩放（以鼠标位置为中心），左键按住拖动 = 平移
        // 手机：双指捏合 = 缩放（以图表中心），单指拖动 = 平移
        private void Update()
        {
            if (historyChart == null || historyChart.rectTransform == null) return;
            var rt = historyChart.rectTransform;
            var parent = rt.parent as RectTransform;

            if (Application.isMobilePlatform)
            {
                UpdateMobileZoomPan(rt, parent);
            }
            else
            {
                UpdatePcZoomPan(rt, parent);
            }
        }

        // ---------- PC：滚轮缩放（以鼠标为中心）+ 左键拖拽平移 + 键盘缩放兜底 ----------
        private void UpdatePcZoomPan(RectTransform rt, RectTransform parent)
        {
            // 通道 1：Input.mouseScrollDelta（旧版 Input Manager）
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float factor = 1f + scroll / 120f * scrollWheelSpeed;
                Debug.Log($"[LishiQuXian] mouseScrollDelta={scroll} factor={factor}");
                ScaleChartAroundPointer(rt, factor);
                return;
            }
            // 通道 2：Input.GetAxis("Mouse ScrollWheel")
            float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollAxis) > 0.001f)
            {
                float factor = 1f + scrollAxis * scrollWheelSpeed * 50f;
                Debug.Log($"[LishiQuXian] GetAxis={scrollAxis} factor={factor}");
                ScaleChartAroundPointer(rt, factor);
                return;
            }
            // 通道 3：键盘兜底（绝对可靠）：+ 放大，- 缩小，= 重置
            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            {
                ScaleChartAroundCenter(rt, 1.1f);
                return;
            }
            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            {
                ScaleChartAroundCenter(rt, 0.9f);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                return;
            }

            // 通道 4：右键按住上下拖动 = 缩放（和平移同一套已验证可用的鼠标管线）
            // 上拖放大、下拖缩小，以鼠标位置为中心。滚轮被 Editor 窗口吞掉时用这个。
            if (Input.GetMouseButtonDown(1) && IsPointerInChart(rt))
            {
                m_IsRmbZooming = true;
                m_LastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1) && m_IsRmbZooming)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - m_LastMousePos;
                m_LastMousePos = cur;
                float factor = 1f + delta.y * rmbDragZoomSpeed;
                Debug.Log($"[LishiQuXian] RMB-zoom delta.y={delta.y:F0} factor={factor}");
                ScaleChartAroundPointer(rt, factor);
                return;
            }
            else
            {
                m_IsRmbZooming = false;
            }

            if (Input.GetMouseButtonDown(0) && IsPointerInChart(rt))
            {
                m_IsPanning = true;
                m_LastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && m_IsPanning)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - m_LastMousePos;
                m_LastMousePos = cur;
                PanChart(rt, parent, delta);
            }
            else
            {
                m_IsPanning = false;
            }
        }

        // OnGUI：输入诊断 HUD + 兜底滚轮（仅当 Update 的 raw Input 都没读到滚动时才启用，
        // 避免同一帧被 Update 和 OnGUI 双重缩放）
        [SerializeField] private bool showInputDebug = true; // 输入诊断 HUD 开关（调试完可关）
        private bool m_DiagOnGuiOnce = true;
        private void OnGUI()
        {
            if (m_DiagOnGuiOnce)
            {
                m_DiagOnGuiOnce = false;
                Debug.Log($"[LishiQuXian] OnGUI running");
            }
            if (Event.current == null) return;

            // 不可见全屏 Box：Unity 只给"包含 GUI 控件"的 OnGUI 派发事件，必须放一个控件
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", GUIStyle.none);

            // 兜底滚轮：仅当 Update 的 raw Input 都没读到滚动时启用，避免同帧双重缩放
            bool updateSawScroll = Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f ||
                                   Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.001f;
            if (Event.current.type == EventType.ScrollWheel &&
                !updateSawScroll &&
                historyChart != null && historyChart.rectTransform != null)
            {
                var rt = historyChart.rectTransform;
                float factor = 1f + Event.current.delta.y / 120f * scrollWheelSpeed;
                Debug.Log($"[LishiQuXian] OnGUI scroll dy={Event.current.delta.y} factor={factor}");
                ScaleChartAroundPointer(rt, factor);
                Event.current.Use();
            }

            // 输入诊断 HUD：滚动/右键拖动时看屏幕左上角，确认输入和缩放状态
            if (showInputDebug)
            {
                var pos = Input.mousePosition;
                float scale = historyChart != null && historyChart.rectTransform != null
                    ? historyChart.rectTransform.localScale.x
                    : 1f;
                string msg = string.Format(
                    "[InputDebug] scale={0:F2} mouse=({1:F0},{2:F0}) scrollDelta={3:F1} scrollAxis={4:F3}",
                    scale, pos.x, pos.y,
                    Input.mouseScrollDelta.y,
                    Input.GetAxis("Mouse ScrollWheel"));
                GUI.Label(new Rect(10, 10, Screen.width - 20, 24), msg);
            }
        }

        // ---------- 手机：双指捏合缩放（围绕中心）+ 单指拖拽平移 ----------
        private float m_PinchLastDist;
        private bool m_TouchPanning;
        private Vector2 m_LastTouchPos;

        private void UpdateMobileZoomPan(RectTransform rt, RectTransform parent)
        {
            if (Input.touchCount == 2)
            {
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);
                float dist = Vector2.Distance(t0.position, t1.position);
                if (m_PinchLastDist > 1f && dist > 1f)
                {
                    float factor = dist / m_PinchLastDist;
                    ScaleChartAroundCenter(rt, factor);
                }
                m_PinchLastDist = dist;
                m_TouchPanning = false;
            }
            else
            {
                m_PinchLastDist = 0f;
                if (Input.touchCount == 1)
                {
                    var t = Input.GetTouch(0);
                    if (t.phase == TouchPhase.Began)
                    {
                        m_TouchPanning = true;
                        m_LastTouchPos = t.position;
                    }
                    else if (t.phase == TouchPhase.Moved && m_TouchPanning)
                    {
                        Vector2 delta = t.position - m_LastTouchPos;
                        m_LastTouchPos = t.position;
                        PanChart(rt, parent, delta);
                    }
                }
                else
                {
                    m_TouchPanning = false;
                }
            }
        }

        // ---------- 缩放/平移核心 ----------
        /// <summary>以鼠标屏幕位置为中心缩放（缩放后鼠标指向的曲线点不动）</summary>
        private void ScaleChartAroundPointer(RectTransform rt, float factor)
        {
            var cam = rt.GetComponentInParent<Canvas>() != null &&
                      rt.GetComponentInParent<Canvas>().renderMode != RenderMode.ScreenSpaceOverlay
                ? rt.GetComponentInParent<Canvas>().worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, Input.mousePosition, cam, out Vector3 worldPt))
                return;

            // 缩放前：鼠标位置在图表本地坐标中的点
            Vector3 localPt = rt.InverseTransformPoint(worldPt);
            float oldScale = rt.localScale.x;
            float newScale = Mathf.Clamp(oldScale * factor, minScale, maxScale);
            if (Mathf.Approximately(newScale, oldScale)) return;

            rt.localScale = Vector3.one * newScale;

            // 让 localPt 的世界位置保持不变（抵消缩放中心漂移）
            var parent = rt.parent as RectTransform;
            if (parent != null)
            {
                Vector3 newWorld = rt.TransformPoint(localPt);
                Vector3 offset = worldPt - newWorld;
                rt.anchoredPosition += (Vector2)parent.InverseTransformDirection(offset);
            }
        }

        /// <summary>以图表中心为中心缩放</summary>
        private void ScaleChartAroundCenter(RectTransform rt, float factor)
        {
            float oldScale = rt.localScale.x;
            float newScale = Mathf.Clamp(oldScale * factor, minScale, maxScale);
            if (Mathf.Approximately(newScale, oldScale)) return;
            rt.localScale = Vector3.one * newScale;
        }

        /// <summary>按屏幕像素增量平移图表（转成父级本地坐标单位）</summary>
        private static void PanChart(RectTransform rt, RectTransform parent, Vector2 screenDelta)
        {
            if (parent == null || parent.lossyScale.x <= 0.0001f) return;
            float factor = 1f / parent.lossyScale.x;
            rt.anchoredPosition += new Vector2(screenDelta.x * factor, screenDelta.y * factor);
        }

        /// <summary>鼠标位置是否在图表矩形内</summary>
        private static bool IsPointerInChart(RectTransform rt)
        {
            var cam = rt.GetComponentInParent<Canvas>() != null &&
                      rt.GetComponentInParent<Canvas>().renderMode != RenderMode.ScreenSpaceOverlay
                ? rt.GetComponentInParent<Canvas>().worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
        }
    }
}
