using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using XCharts.UI.Demo;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 单槽分析页 —— 两个折线图渲染器
/// Chart 1：多指标（工作电压/分子比/铝水平/电解质水平/下料量/出铝量/总功耗）
/// Chart 2：槽温变化（单线）
/// 用法：在 Inspector 拖入两个 LineChart 组件引用，
///       由 SingleSlotInfoRangeManager 在查询完成后调用 SetRows()。
/// </summary>
[DisallowMultipleComponent]
public class SingleSlotInfoCharts : MonoBehaviour
{
    [Header("图表组件（在场景中拖入）")]
    [Tooltip("第一个图：工作电压/分子比/铝水平/电解质水平/下料量/出铝量/总功耗 7条折线")]
    [SerializeField] private LineChart multiSeriesChart;

    [Tooltip("第二个图：槽温变化 单条折线")]
    [SerializeField] private LineChart temperatureChart;

    [Header("字体（可选，不填则自动查找）")]
    [SerializeField] private TMP_FontAsset customTMPFont;
    [SerializeField] private bool enforceFontEveryFrame = true;

    [Header("背景颜色（可选调整）")]
    [SerializeField] private Color32 chartBgColor   = new Color32(55,  59,  71,  255); // #373B47
    [SerializeField] private Color32 gridBgColor    = new Color32(40,  47,  57,  255); // #282F39
    [SerializeField] private Color32 gridBorderColor = new Color32(100, 100, 100, 255);

    // ── 多指标折线颜色（7条）──────────────────────────────────────────────
    private static readonly Color32[] MultiSeriesColors =
    {
        new Color32( 84, 179, 229, 255),  // 工作电压  - 蓝
        new Color32(143, 193, 108, 255),  // 分子比    - 绿
        new Color32(255, 191, 107, 255),  // 铝水平    - 橙
        new Color32(255, 128, 128, 255),  // 电解质水平 - 红
        new Color32(195, 144, 232, 255),  // 下料量    - 紫
        new Color32(102, 214, 214, 255),  // 出铝量    - 青
        new Color32(255, 224, 102, 255),  // 总功耗    - 黄
    };

    private static readonly string[] MultiSeriesNames =
    {
        "工作电压", "分子比", "铝水平", "电解质水平", "下料量", "出铝量", "总功耗"
    };

    // ────────────────────────────────────────────────────────────────────────
    //  公共接口
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>用查询结果填充两张图表。</summary>
    public void SetRows(List<SingleSlotInfoDailyTable.DailySlotInfoRow> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            ClearCharts();
            return;
        }

        if (multiSeriesChart != null)
            BuildMultiSeriesChart(rows);

        if (temperatureChart != null)
            BuildTemperatureChart(rows);
    }

    /// <summary>清空两张图表（无数据时调用）。</summary>
    public void ClearCharts()
    {
        if (multiSeriesChart != null)
        {
            multiSeriesChart.ClearData();
            multiSeriesChart.RemoveAllSerie();
        }

        if (temperatureChart != null)
        {
            temperatureChart.ClearData();
            temperatureChart.RemoveAllSerie();
        }
    }

    private void LateUpdate()
    {
        if (!enforceFontEveryFrame)
        {
            return;
        }

        var font = GetOrLoadTMPFont();
        if (font == null)
        {
            return;
        }

        if (multiSeriesChart != null)
        {
            ForceApplyFontToChartTexts(multiSeriesChart, font);
        }

        if (temperatureChart != null)
        {
            ForceApplyFontToChartTexts(temperatureChart, font);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  图表构建
    // ────────────────────────────────────────────────────────────────────────

    private void BuildMultiSeriesChart(List<SingleSlotInfoDailyTable.DailySlotInfoRow> rows)
    {
        ConfigureChartFrame(multiSeriesChart, "单槽多指标趋势", showLegend: true);

        // X 轴：按日期排好序的日期标签
        foreach (var row in rows)
            multiSeriesChart.AddXAxisData(FormatDateLabel(row.FullDate));

        // 添加 7 条折线系列
        for (int s = 0; s < MultiSeriesNames.Length; s++)
        {
            var serie = multiSeriesChart.AddSerie<Line>(MultiSeriesNames[s]);
            Color32 c = MultiSeriesColors[s];
            serie.lineStyle.color    = c;
            serie.itemStyle.color    = c;
            serie.lineStyle.width    = 2f;
            serie.symbol.show        = true;
            serie.symbol.size        = 5f;
            serie.lineType           = LineType.Smooth;
            serie.EnsureComponent<AreaStyle>().show = false;
            serie.AnimationEnable(false);
        }

        // 按 serieIndex 逐点添加数据
        foreach (var row in rows)
        {
            multiSeriesChart.AddData(0, row.WorkVoltage);
            multiSeriesChart.AddData(1, SafeVal(row.MoleRatio));
            multiSeriesChart.AddData(2, SafeVal(row.AluminumLevel));
            multiSeriesChart.AddData(3, SafeVal(row.ElectrolyteLevel));
            multiSeriesChart.AddData(4, row.FeedingAmount);
            multiSeriesChart.AddData(5, SafeVal(row.AluminumOutput));
            multiSeriesChart.AddData(6, row.TotalPower);
        }

        multiSeriesChart.RefreshChart();
    }

    private void BuildTemperatureChart(List<SingleSlotInfoDailyTable.DailySlotInfoRow> rows)
    {
        ConfigureChartFrame(temperatureChart, "槽温变化", showLegend: false);

        foreach (var row in rows)
            temperatureChart.AddXAxisData(FormatDateLabel(row.FullDate));

        // 单条紫色折线，和截图样式一致
        Color32 purple = new Color32(160, 96, 255, 255); // #A060FF
        var serie = temperatureChart.AddSerie<Line>("槽温");
        serie.lineStyle.color    = purple;
        serie.itemStyle.color    = purple;
        serie.lineStyle.width    = 2.5f;
        serie.symbol.show        = true;
        serie.symbol.size        = 6f;
        serie.lineType           = LineType.Smooth;
        serie.EnsureComponent<AreaStyle>().show = false;
        serie.AnimationEnable(false);

        foreach (var row in rows)
        {
            float temp = row.Temperature;
            double val = (float.IsNaN(temp) || temp <= 0f) ? 0.0 : temp;
            var sd = temperatureChart.AddData(0, val);

            // 每个有效数据点显示白色数值标签（与截图一致）
            if (!float.IsNaN(temp) && temp > 0f)
            {
                var lbl = sd.EnsureComponent<LabelStyle>();
                lbl.show                      = true;
                lbl.position                  = LabelStyle.Position.Top;
                lbl.offset                    = new Vector3(0, 10, 0);
                lbl.formatter                 = "{c}";
                lbl.textStyle.color           = Color.white;
                lbl.textStyle.fontStyle       = FontStyle.Bold;
            }
        }

        temperatureChart.RefreshChart();
    }

    private TMP_FontAsset GetOrLoadTMPFont()
    {
        if (customTMPFont != null) return customTMPFont;

        customTMPFont = Resources.Load<TMP_FontAsset>("Fonts/Alibaba-PuHuiTi-Regular SDF");

        if (customTMPFont == null)
        {
            var ttf = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular");
            if (ttf == null)
            {
                ttf = Resources.Load<Font>("Alibaba-PuHuiTi-Regular");
            }

            if (ttf != null)
            {
                customTMPFont = TMP_FontAsset.CreateFontAsset(
                    ttf,
                    90,
                    10,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic);

                if (customTMPFont != null)
                {
                    customTMPFont.isMultiAtlasTexturesEnabled = true;
                }
            }
        }

        if (customTMPFont == null) customTMPFont = TMP_Settings.defaultFontAsset;
        return customTMPFont;
    }

    private static void ForceApplyFontToChartTexts(LineChart chart, TMP_FontAsset font)
    {
        if (chart == null || font == null)
        {
            return;
        }

        var labels = chart.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].font != font)
            {
                labels[i].font = font;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  公用配置
    // ────────────────────────────────────────────────────────────────────────

    private void ConfigureChartFrame(LineChart chart, string titleText, bool showLegend)
    {
        chart.ClearData();
        chart.RemoveAllSerie();

        // 标题
        var title = chart.EnsureChartComponent<Title>();
        title.show  = true;
        title.text  = titleText;
        title.labelStyle.textStyle.color = Color.white;
        title.location.align = Location.Align.TopCenter;

        // X 轴
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.type                          = Axis.AxisType.Category;
        xAxis.boundaryGap                   = true;
        xAxis.axisLabel.textStyle.color     = Color.white;
        xAxis.axisLabel.textStyle.fontSize  = 14;
        xAxis.axisLine.show                 = true;
        xAxis.axisTick.show                 = true;
        xAxis.splitLine.show                = false;

        // Y 轴（自动范围）
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type                              = Axis.AxisType.Value;
        yAxis.minMaxType                        = Axis.AxisMinMaxType.Default;
        yAxis.axisLabel.show                    = true;
        yAxis.axisLabel.textStyle.color         = Color.white;
        yAxis.splitLine.lineStyle.type          = LineStyle.Type.Dashed;
        yAxis.splitLine.lineStyle.color         = new Color32(80, 80, 80, 255);

        // Tooltip
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show    = true;
        tooltip.trigger = Tooltip.Trigger.Axis;

        // Legend
        var legend = chart.EnsureChartComponent<Legend>();
        legend.show                = showLegend;
        legend.labelStyle.textStyle.color = Color.white;

        // 主题/背景
        chart.theme.enableCustomTheme    = true;
        chart.theme.customBackgroundColor = chartBgColor;

        // TMP 字体（必须设置，否则图表文字不显示）
        var tmFont = GetOrLoadTMPFont();
        if (tmFont != null && chart.theme.sharedTheme != null)
        {
            chart.theme.sharedTheme.tmpFont = tmFont;
            chart.theme.sharedTheme.SyncTMPFontToSubComponent();
        }

        // Grid
        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.backgroundColor = gridBgColor;
        grid.showBorder      = true;
        grid.borderWidth     = 1f;
        grid.borderColor     = gridBorderColor;
        grid.left            = 60;
        grid.right           = 20;
        grid.bottom          = 50;
        grid.top             = showLegend ? 80 : 60;

        ForceApplyFontToChartTexts(chart, tmFont);
        chart.RefreshChart();
        ForceApplyFontToChartTexts(chart, tmFont);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  工具方法
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>将 yyyyMMdd 转换为 MM-dd 显示在 X 轴。</summary>
    private static string FormatDateLabel(string fullDate)
    {
        if (string.IsNullOrEmpty(fullDate) || fullDate.Length < 8)
            return fullDate ?? string.Empty;
        return fullDate.Substring(4, 2) + "-" + fullDate.Substring(6, 2);
    }

    /// <summary>将 NaN/Infinity 替换为 0，防止 XCharts 绘图异常。</summary>
    private static double SafeVal(float v)
    {
        return float.IsNaN(v) || float.IsInfinity(v) ? 0.0 : v;
    }
}
