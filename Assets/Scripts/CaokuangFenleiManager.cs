using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using XCharts.Runtime;
using XCharts.Runtime.UI;
using System;
using TMPro; // 引入用于显示日期的文本组件空间

public class CaokuangFenleiManager : MonoBehaviour
{
    [Header("大面板引用")]
    [Tooltip("历史趋势面板（外层之一）")]
    public GameObject lishiqushiPanel;
    [Tooltip("当日槽况面板（外层之二，用于彻底隐藏防止遮挡）")]
    public GameObject dangricaokuangPanel;
    [Tooltip("历史分析面板（内层详情）")]
    public GameObject lishifenxiPanel;

    [Header("顶部外层筛选组件(工区/起止日期)")]
    public WorkAreaSelector workAreaSelector;
    public GameObject workAreaDisplayText; // 添加工区的普通Text供托拽
    
    public GameObject startDateSelector; // 起始日期选择器 (可选绑)
    public GameObject startDateDisplayText; // 起始日期显示的文本 (支持 Text 或 TMP_Text)
    
    public GameObject endDateSelector; // 终止日期选择器 (可选绑)
    public GameObject endDateDisplayText; // 终止日期显示的文本 (支持 Text 或 TMP_Text)

    [Header("历史趋势(外层)组件")]
    public UITable lishiqushiTable; // 在外层的UITable，用于点击跳转

    [Header("历史分析(内层)基础组件")]
    public Button backButton; // 返回按钮 (back_b)
    public Toggle toggleTongJi; // 统计数据Toggle
    public Toggle toggleQuXian; // 曲线Toggle

    [Header("历史分析(内层)切换面板")]
    public GameObject panelTongJi; // 包含UITable的节点
    public GameObject panelQuXian; // 包含曲线图的节点

    [Header("历史分析(内层)图表组件")]
    public UITable tongjiTable; // 内部的7列UITable
    public LineChart quxianChart; // 带有LineChart的组件

    void Start()
    {
        // === 0. 顶部筛选器初始默认值设置 ===
        InitDefaultFilters();

        // 1. 绑定返回按钮
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        // 2. 绑定 Toggle 切换事件
        if (toggleTongJi != null)
        {
            toggleTongJi.onValueChanged.AddListener(isOn => { if (isOn) ShowPanel(true); });
        }
        if (toggleQuXian != null)
        {
            toggleQuXian.onValueChanged.AddListener(isOn => { if (isOn) ShowPanel(false); });
        }

        // 3. 监听外层表格的点击事件
        if (lishiqushiTable != null)
        {
            // 开启表格的射线检测，以支持点击
            lishiqushiTable.raycastTarget = true;
            lishiqushiTable.supportSelect = true;
            lishiqushiTable.onTableClick = OnLishiqushiTableClicked;
        }

        // 初始状态：显示外层，隐藏内层
        if (lishifenxiPanel != null) lishifenxiPanel.SetActive(false);
        if (lishiqushiPanel != null) lishiqushiPanel.SetActive(true);
        if (dangricaokuangPanel != null) dangricaokuangPanel.SetActive(true);

        // 为了能测试点击，给最外层的历史趋势UITable填充几行临时测试数据
        MockLishiqushiTableData();
    }

    // 辅助方法：给未知的 UI 文本对象赋值
    private void SetTextContent(GameObject textObj, string content)
    {
        if (textObj == null) return;

        // 尝试设置为普通的 UnityEngine.UI.Text (包含子物体情况)
        var normalText = textObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (normalText != null)
        {
            normalText.text = content;
            return;
        }

        // 尝试设置为 TextMeshProUGUI (包含子物体情况)
        var tmpText = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = content;
        }
    }

    // 辅助方法：获取未知的 UI 文本对象的文本
    private string GetTextContent(GameObject textObj)
    {
        if (textObj == null) return "";

        var normalText = textObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (normalText != null) return normalText.text;

        var tmpText = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null) return tmpText.text;

        return "";
    }

    // 初始化默认的筛选条件
    private void InitDefaultFilters()
    {
        // 1. 设置工区为“1车间1工区”
        if (workAreaSelector != null && workAreaSelector.selectedDisplayText != null)
        {
            workAreaSelector.selectedDisplayText.text = "1车间1工区";
        }
        if (workAreaDisplayText != null)
        {
            SetTextContent(workAreaDisplayText, "1车间1工区");
        }

        // 2. 设置起始日期(一月前) 和 终止日期(今天)
        string defaultStartDateStr = DateTime.Today.AddDays(-30).ToString("yyyyMMdd");
        string defaultEndDateStr = DateTime.Today.ToString("yyyyMMdd");
        
        // 绑定起始日期
        if (startDateSelector != null)
        {
            var selector = startDateSelector.GetComponent<riqSelector>();
            if (selector != null && selector.selectedDisplayText != null)
            {
                selector.selectedDisplayText.text = defaultStartDateStr;
            }
        }
        if (startDateDisplayText != null)
        {
            SetTextContent(startDateDisplayText, defaultStartDateStr);
        }

        // 绑定终止日期
        if (endDateSelector != null)
        {
            var selector = endDateSelector.GetComponent<riqSelector>();
            if (selector != null && selector.selectedDisplayText != null)
            {
                selector.selectedDisplayText.text = defaultEndDateStr;
            }
        }
        if (endDateDisplayText != null)
        {
            SetTextContent(endDateDisplayText, defaultEndDateStr);
        }
    }

    // 填充外层表格用于测试点击
    private void MockLishiqushiTableData()
    {
        if (lishiqushiTable == null) return;
        lishiqushiTable.ClearData();
        lishiqushiTable.AddRow("2024.12.01", "3001", "83.8", "B类槽", "上升 ↑");
        lishiqushiTable.AddRow("2024.12.02", "3002", "85.7", "A类槽", "上升 ↑");
        lishiqushiTable.AddRow("2024.12.03", "3003", "79.0", "C类槽", "下降 ↓");
        lishiqushiTable.AddRow("2024.12.04", "3004", "88.9", "A类槽", "上升 ↑");
        lishiqushiTable.AddRow("2024.12.05", "3005", "84.1", "B类槽", "下降 ↓");
    }

    // 外层表格行点击回调
    private void OnLishiqushiTableClicked(int row, int column, TableCell cell)
    {
        // 确保点击有效行
        if (row < 0 || lishiqushiTable == null || row >= lishiqushiTable.data.Count) return;

        // 从被点击行的数据中，读取该行的第1列（槽号字段）来作为请求参数
        // 根据你前面填充的："2024.12.01", "3001", "83.8"，索引 1 即为 "3001"
        string clickedSlotNumber = "";
        try {
            clickedSlotNumber = lishiqushiTable.data[row].data[1].GetContent();
        } catch {
            clickedSlotNumber = "3001"; // 获取失败时的默认容错
        }

        // 获取用户目前选择的日期，如果没选或获取失败，自动 fallback 为默认的一个月区间
        string sDateText = GetTextContent(startDateDisplayText);
        string sDate = !string.IsNullOrEmpty(sDateText) ? sDateText : DateTime.Today.AddDays(-30).ToString("yyyyMMdd");
            
        string eDateText = GetTextContent(endDateDisplayText);
        string eDate = !string.IsNullOrEmpty(eDateText) ? eDateText : DateTime.Today.ToString("yyyyMMdd");

        // 触发跳转操作，将真实的槽号以及动态获取的起始时间传给下一级的接口调用和生成逻辑
        OpenLishiFenxi(clickedSlotNumber, sDate, eDate);
    }

    private void OnBackClicked()
    {
        if (lishifenxiPanel != null) lishifenxiPanel.SetActive(false);
        if (lishiqushiPanel != null) lishiqushiPanel.SetActive(true);
        if (dangricaokuangPanel != null) dangricaokuangPanel.SetActive(true); // 或者根据你原来的逻辑恢复
    }

    // 切换 统计数据 / 曲线 面板
    private void ShowPanel(bool isShowTongJi)
    {
        if (panelTongJi != null) panelTongJi.SetActive(isShowTongJi);
        if (panelQuXian != null) panelQuXian.SetActive(!isShowTongJi);
    }

    // 打开内层面板并生成数据（改为接收日期等接口属性）
    public void OpenLishiFenxi(string slotNumber, string startDate, string endDate)
    {
        if (lishiqushiPanel != null) lishiqushiPanel.SetActive(false);
        if (dangricaokuangPanel != null) dangricaokuangPanel.SetActive(false); // 关键：进详情时把另一个外层也隐藏
        if (lishifenxiPanel != null) lishifenxiPanel.SetActive(true);

        // 默认激活统计数据Toggle
        if (toggleTongJi != null) toggleTongJi.isOn = true;
        ShowPanel(true);

        // -- API 预留口：日后你可以在这里发起网络请求 --
        // StartCoroutine(FetchDataFromAPI(slotNumber, startDate, endDate));

        // -- 目前暂时通过提取方法来生成模拟的图表数据 --
        GenerateMockRenderData(slotNumber, startDate, endDate);
    }

    // 解析出供渲染的假数据并调用图表更新
    private void GenerateMockRenderData(string slotNumber, string startDate, string endDate)
    {
        // 注意：以后你在真实API拿到 List<Date>, List<Score> 后，也是分别传给下面两个渲染方法
        
        List<string> dates = new List<string> { "12.01", "12.02", "12.03", "12.04", "12.05", "12.06", "12.07", "12.08" };
        List<float> scores = new List<float> { 78.5f, 79.8f, 77.5f, 81.5f, 83.2f, 83.8f, 84.1f, 92.5f };

        MockTableData(slotNumber, dates, scores);
        RenderChartData(slotNumber, dates, scores);
    }

    // 生成内层明细表格
    private void MockTableData(string slotNumber, List<string> dates, List<float> scores)
    {
        if (tongjiTable == null) return;

        tongjiTable.ClearData(); // 清空原有数据
        
        // 遍历我们的日期并动态注入（这里顺便模拟了一下涨跌判定）
        for (int i = 0; i < dates.Count; i++)
        {
            string dateStr = "2024." + dates[i];
            float s = scores[i];
            string trendStr = (i == 0 || s >= scores[i - 1]) ? "<color=#32B16C>上升 ↑</color>" : "<color=#EB5A5A>下降 ↓</color>";
            string levelStr = s > 85 ? "A类槽" : (s > 80 ? "B类槽" : "C类槽");

            tongjiTable.AddRow(dateStr, slotNumber, s.ToString("F1"), levelStr, "80", "85", trendStr);
        }
    }

    // 生成 XCharts 曲线图数据（修正版）
    private void RenderChartData(string slotNumber, List<string> dates, List<float> scores)
    {
        if (quxianChart == null) return;
        
        // 彻底清空原有系列与数据缓冲，避免XCharts组件缓存引发的不绘制或错乱
        quxianChart.ClearData();
        quxianChart.RemoveData();

        // 如果旧版需要可以手动清空：
        quxianChart.series.Clear();

        // 1. 设置标题在左上角
        var title = quxianChart.EnsureChartComponent<Title>();
        if (title != null)
        {
            title.show = true;
            title.text = $"{slotNumber}号槽槽况趋势";
            title.location.align = Location.Align.TopLeft;
        }

        // 2. 清理并设置 X 轴日期
        var xAxis = quxianChart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Category;
        xAxis.splitLine.show = true;
        xAxis.splitLine.lineStyle.color = new Color32(230, 230, 230, 255);
        
        // 3. 配置 Y 轴动态范围
        var yAxis = quxianChart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom; 
        yAxis.min = 70;
        yAxis.max = 95;
        yAxis.splitLine.show = true;

        // 4. 新增线条并设置指定颜色 (#049151 -> RGB 4, 145, 81)
        var serie = quxianChart.AddSerie<Line>("当日评分");
        
        serie.symbol.show = true;
        serie.symbol.type = SymbolType.EmptyCircle;
        
        Color32 themeColor = new Color32(4, 145, 81, 255);
        serie.itemStyle.color = themeColor;
        serie.lineStyle.color = themeColor;
        
        // 5. 设置底部渐变色的面积背景
        // 对于面积背景，XCharts 需要 EnsureComponent 来创建
        var areaStyle = serie.EnsureComponent<AreaStyle>();
        areaStyle.show = true;
        areaStyle.color = new Color32(4, 145, 81, 80);  // 顶部带透明度的绿
        areaStyle.toColor = new Color32(4, 145, 81, 5); // 底部更透明，形成渐变

        // ====== 关键修复部分：向图表内注入数据 ======
        for (int i = 0; i < dates.Count; i++)
        {
            // 通过接口追加 X轴分类名，再将其对应的值添加到第0个Series中
            quxianChart.AddXAxisData(dates[i]);
            quxianChart.AddData(0, scores[i]);
        }

        // 添加并展示十字光标提示框(Tooltip)
        var tooltip = quxianChart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        tooltip.type = Tooltip.Type.Line;
        
        // 手动唤醒图表刷新渲染
        quxianChart.RefreshChart();
    }
}
