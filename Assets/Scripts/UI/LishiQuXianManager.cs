using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;
using XUGL;

namespace Aeht.UI
{
    /// <summary>
    /// 历史曲线 Manager：在一个大 LineChart 中绘制多条曲线（双 Y 轴 + Line/Scatter/Bar 混合）
    /// </summary>
    public class LishiQuXianManager : MonoBehaviour
    {
        [Header("图表")]
        [SerializeField] private LineChart historyChart;
        [SerializeField] private Transform chartContainer;
        [SerializeField] private GameObject chartPrefab;

        [Header("字体")]
        [SerializeField] private TMP_FontAsset chineseFontAsset;

        [Header("模拟数据")]
        [SerializeField] private bool useMockData = true;

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

            if (useMockData)
                RenderMockData();

            // 创建右下角浮动缩放按钮（不依赖鼠标焦点，UGUI 按钮 100% 可靠）
            EnsureZoomControls();
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

        // ==================== 模拟数据 ====================
        private void RenderMockData()
        {
            int pointCount = 24; // 24小时
            var voltageData = new List<float>();    // 红色折线：工作电压 ~4300
            var tempData = new List<float>();       // 紫色折线：槽温 ~4000 波动
            var stateData = new List<float>();      // 白色折线：状态 ~3600-3700 方波
            var abnormalIndices = new List<int>();  // 异常点索引
            var afLabels = new List<(int index, string text, Color color)>(); // AF标记
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

            RenderChart(voltageData, tempData, stateData, abnormalIndices, afLabels, bar1Data, bar2Data);
        }

        // ==================== 核心渲染 ====================
        private void RenderChart(
            List<float> voltageData,
            List<float> tempData,
            List<float> stateData,
            List<int> abnormalIndices,
            List<(int index, string text, Color color)> afLabels,
            List<float> bar1Data,
            List<float> bar2Data)
        {
            var chart = historyChart;
            chart.RemoveData();
            chart.RemoveAllSerie();
            chart.RemoveChartComponents<YAxis>();
            chart.RemoveChartComponents<GridCoord>();

            // ========== 0. Legend 图例 ==========
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;
            legend.location.align = Location.Align.TopCenter;
            legend.location.top = 0.02f; // 靠近顶部
            legend.itemWidth = 20f;
            legend.itemHeight = 10f;
            legend.itemGap = 15f;
            if (legend.labelStyle.textStyle == null)
                legend.labelStyle.textStyle = new TextStyle();
            legend.labelStyle.textStyle.color = Color.white;
            legend.labelStyle.textStyle.fontSize = 14;
            ApplyFont(legend.labelStyle.textStyle);

            // ========== 1. Grid ==========
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.show = true;
            grid.left = 60;
            grid.right = 60;
            grid.top = 40;
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
            serieTemp.symbol.show = false; // 关闭内置符号（EmptyTriangle 是硬编码等边三角）
            serieTemp.animation.enable = false;
            for (int i = 0; i < tempData.Count; i++)
            {
                chart.AddData(1, tempData[i]);
            }

            // ========== 7. 红色 Plus 散点：异常点 ==========
            var serieAbnormal = chart.AddSerie<Scatter>("异常点");
            serieAbnormal.yAxisIndex = 0;
            serieAbnormal.symbol.show = true;
            serieAbnormal.symbol.type = SymbolType.Plus;
            serieAbnormal.symbol.size = 12f;
            serieAbnormal.symbol.color = new Color(1f, 0.2f, 0.2f); // 红色
            serieAbnormal.itemStyle.color = new Color(1f, 0.2f, 0.2f);
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

            chart.RefreshChart();

            // ========== 12. 自定义绘制：槽温的"三角形+竖线"大头针（锐角三角形） ==========
            // EmptyTriangle 是硬编码等边三角，这里用 UGL.DrawLine 画 3 条线，组成高而尖的锐角等腰三角形
            chart.onDrawAfterSerie = (vh, serie) =>
            {
                if (serie.serieName != "槽温") return;
                var triColor = new Color32(102, 255, 102, 255); // 绿色
                float triHalfW = 8f;   // 三角形半宽（底边总宽 16）
                float triH = 20f;      // 三角形高（高而尖，锐角）
                float pinLen = 28f;    // 竖线长度（向下）
                float lineW = 2f;      // 线宽

                for (int i = 0; i < serie.data.Count; i++)
                {
                    var sd = serie.data[i];
                    var pt = sd.context.position;
                    if (pt == Vector3.zero) continue; // 无效数据

                    // 三个顶点（Y 向下：-Y = 上，+Y = 下）
                    var top  = pt + new Vector3(0,            -triH / 2f, 0); // 顶点（向上）
                    var left = pt + new Vector3(-triHalfW,    +triH / 2f, 0); // 左下
                    var rght = pt + new Vector3(+triHalfW,    +triH / 2f, 0); // 右下

                    // 空心三角形 = 画 3 条线
                    UGL.DrawLine(vh, top, left, lineW, triColor); // 左斜边
                    UGL.DrawLine(vh, top, rght, lineW, triColor); // 右斜边
                    UGL.DrawLine(vh, left, rght, lineW, triColor); // 底边

                    // 竖线：从底边中点向下，紧贴底边不超出
                    var lineTop    = pt + new Vector3(0, +triH / 2f, 0);     // 底边中点
                    var lineBottom = lineTop + new Vector3(0, pinLen, 0);    // 向下延伸
                    UGL.DrawLine(vh, lineTop, lineBottom, lineW, triColor);
                }
            };
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
