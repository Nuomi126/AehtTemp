using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Gauges
{
    /// <summary>
    /// Gauge chart.
    /// ||仪表盘。
    /// </summary>
    [AddComponentMenu("XCharts/GaugeChart", 19)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [HelpURL("https://xcharts-team.github.io/docs/gauge")]
    public class GaugeChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForSerie(typeof(Gauge))] private List<Gauge> m_SerieGauges = new List<Gauge>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            Gauge.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate progress gauge chart.
        /// ||生成进度仪表盘。 
        /// </summary>
        public void DefaultProgressGaugeChart()
        {
            CheckChartInit();
            var serie = GetSerie<Gauge>(0);
            serie.gaugeAxis.type = GaugeAxisType.Progress;
        }

        /// <summary>
        /// generate section gauge chart.
        /// ||生成分段仪表盘。
        /// </summary>
        public void DefaultSectionGaugeChart()
        {
            CheckChartInit();
            var serie = GetSerie<Gauge>(0);
            serie.gaugeAxis.type = GaugeAxisType.Section;
        }

        /// <summary>
        /// generate multiple gauge chart.
        /// ||生成多仪表盘。
        /// </summary>
        public void DefaultMultipleGaugeChart()
        {
            DefaultProgressGaugeChart();
            ClearData();
            var serie = GetSerie<Gauge>(0);
            serie.gaugeAxis.type = GaugeAxisType.Progress;
            serie.startAngle = 0;
            serie.endAngle = 360;
            serie.gaugePointer.show = false;
            serie.gaugeAnchor.show = false;

            serie.gaugeAxis.overlap = false;
            serie.gaugeAxis.axisTick.show = false;
            serie.gaugeAxis.axisLabel.show = false;
            serie.gaugeAxis.splitLine.show = false;
            serie.gaugeAxis.axisLine.lineStyle.width = 20;

            serie.radius[0] = 0.3f;
            serie.center[1] = 0.45f;

            serie.label.show = false;
            serie.titleStyle.show = false;
            serie.roundCap = true;

            AddData(0, UnityEngine.Random.Range(10, 90), "ring1");
            AddData(0, UnityEngine.Random.Range(10, 90), "ring2");
            AddData(0, UnityEngine.Random.Range(10, 90), "ring3");
        }

        /// <summary>
        /// generate barometer gauge chart.
        /// ||生成气压计仪表盘。
        /// </summary>
        public void DefaultBarometerGaugeChart()
        {
            DefaultProgressGaugeChart();
            Gauge.AddDefaultSerie(this, GenerateDefaultSerieName());
            var serie = GetSerie<Gauge>(0);
            serie.gaugeAxis.type = GaugeAxisType.None;
            serie.radius[0] = 0.35f;
            serie.min = 0;
            serie.max = 100;
            serie.titleStyle.show = false;
            serie.label.show = false;
            serie.splitNumber = 10;

            var lineColor1 = Color.red;
            var lineColor2 = Color.black;
            var lineWidth = 1.6f;
            var splitLength = 15f;

            serie.gaugeAxis.axisLine.backgroundColor = lineColor1;
            serie.gaugeAxis.axisLine.lineStyle.color = lineColor1;
            serie.gaugeAxis.axisLine.lineStyle.width = lineWidth * 2;

            serie.gaugeAxis.splitLine.lineStyle.color = lineColor1;
            serie.gaugeAxis.splitLine.lineStyle.width = lineWidth;
            serie.gaugeAxis.splitLine.lineStyle.length = splitLength;
            serie.gaugeAxis.splitLine.distance = -splitLength;

            serie.gaugeAxis.axisTick.lineStyle.color = lineColor1;
            serie.gaugeAxis.axisTick.lineStyle.width = lineWidth * 0.4f;
            serie.gaugeAxis.axisTick.lineStyle.length = splitLength * 0.7f;
            serie.gaugeAxis.axisTick.distance = -splitLength * 0.7f;

            serie.gaugeAxis.axisLabel.distance = -28f;

            serie.gaugePointer.length = 1.14f;
            serie.gaugePointer.backLength = 0.1f;
            serie.gaugePointer.backExtraLength = 0.15f;
            serie.gaugePointer.topLeftLength = 0.009f;
            serie.gaugePointer.topRightLength = 0.009f;
            serie.gaugePointer.bottomLeftLength = 0.02f;
            serie.gaugePointer.bottomRightLength = 0.02f;
            serie.gaugePointer.color = lineColor2;

            serie.gaugeAnchor.show = true;
            serie.gaugeAnchor.type = SymbolType.EmptyCircle;
            serie.gaugeAnchor.size = 10;
            serie.gaugeAnchor.itemStyle.color = lineColor2;

            var serie2 = GetSerie<Gauge>(1);
            serie2.gaugeAxis.type = GaugeAxisType.None;
            serie2.radius[0] = 0.31f;
            serie2.min = 0;
            serie2.max = 60;
            serie2.splitNumber = 6;
            serie2.gaugeAxis.axisLine.backgroundColor = lineColor2;
            serie2.gaugeAxis.axisLine.lineStyle.color = lineColor2;
            serie2.gaugeAxis.axisLine.lineStyle.width = lineWidth * 2;

            serie2.gaugeAxis.splitLine.lineStyle.color = lineColor2;
            serie2.gaugeAxis.splitLine.lineStyle.width = lineWidth;
            serie2.gaugeAxis.splitLine.lineStyle.length = splitLength;
            serie2.gaugeAxis.splitLine.distance = -3f;

            serie2.gaugeAxis.axisTick.lineStyle.color = lineColor2;
            serie2.gaugeAxis.axisTick.lineStyle.width = lineWidth * 0.4f;
            serie2.gaugeAxis.axisTick.lineStyle.length = splitLength * 0.7f;
            serie2.gaugeAxis.axisTick.distance = -3f;

            serie2.gaugeAxis.axisLabel.distance = 28f;
            serie2.gaugePointer.show = false;
            serie2.titleStyle.show = false;
            serie2.label.show = true;
            serie2.itemStyle.color = lineColor2;

            serie2.gaugeAnchor.show = true;
            serie2.gaugeAnchor.type = SymbolType.Circle;
            serie2.gaugeAnchor.size = 5;
            serie2.gaugeAnchor.itemStyle.color = lineColor2;
        }

        /// <summary>
        /// generate clock gauge chart.
        /// ||生成时钟仪表盘。
        /// </summary>
        public void DefaultClockGaugeChart()
        {
            CheckChartInit();
            RemoveData();
            Gauge.AddDefaultSerie(this, "Hour");
            Gauge.AddDefaultSerie(this, "Minute");
            Gauge.AddDefaultSerie(this, "Second");

            var serieHour = GetSerie<Gauge>(0);
            var serieMinute = GetSerie<Gauge>(1);
            var serieSecond = GetSerie<Gauge>(2);

            var timeOfDay = System.DateTime.Now.TimeOfDay;

            serieHour.startAngle = 0;
            serieHour.endAngle = 360;
            serieHour.center[1] = 0.45f;
            serieHour.radius[0] = 0.35f;
            serieHour.splitNumber = 12;
            serieHour.min = 0;
            serieHour.max = 12;
            serieHour.titleStyle.show = false;
            serieHour.label.show = false;
            serieHour.gaugeAxis.axisLabel.distance = 30;
            serieHour.gaugeAxis.axisLabel.showStartLabel = false;
            serieHour.gaugeAxis.splitLine.distance = 5;
            serieHour.gaugeAxis.axisTick.distance = 5;
            serieHour.gaugePointer.length = 0.5f;
            serieHour.gaugePointer.backLength = 0.08f;
            serieHour.gaugePointer.backExtraLength = 0f;
            serieHour.gaugePointer.topLeftLength = 0.025f;
            serieHour.gaugePointer.topRightLength = 0.025f;
            serieHour.gaugePointer.bottomLeftLength = 0.025f;
            serieHour.gaugePointer.bottomRightLength = 0.025f;
            serieHour.gaugePointer.autoColor = true;
            serieHour.gaugePointer.roundCap = true;
            serieHour.gaugeAnchor.type = SymbolType.EmptyCircle;
            serieHour.gaugeAnchor.size = 0;
            serieHour.gaugeAnchor.gap = 0;
            serieHour.ClearData();
            serieHour.AddYData(timeOfDay.TotalHours % 12, "Hour");

            serieMinute.startAngle = 0;
            serieMinute.endAngle = 360;
            serieMinute.center[1] = 0.45f;
            serieMinute.radius[0] = 0.35f;
            serieMinute.max = 60;
            serieMinute.titleStyle.show = false;
            serieMinute.label.show = false;
            serieMinute.gaugeAxis.show = false;
            serieMinute.gaugePointer.length = 0.7f;
            serieMinute.gaugePointer.backLength = 0.08f;
            serieMinute.gaugePointer.backExtraLength = 0f;
            serieMinute.gaugePointer.topLeftLength = 0.02f;
            serieMinute.gaugePointer.topRightLength = 0.02f;
            serieMinute.gaugePointer.bottomLeftLength = 0.02f;
            serieMinute.gaugePointer.bottomRightLength = 0.02f;
            serieMinute.gaugePointer.autoColor = true;
            serieMinute.gaugePointer.roundCap = true;
            serieMinute.gaugeAnchor.show = false;
            serieMinute.ClearData();
            serieMinute.AddYData(timeOfDay.TotalMinutes % 60, "Minute");

            serieSecond.startAngle = 0;
            serieSecond.endAngle = 360;
            serieSecond.center[1] = 0.45f;
            serieSecond.radius[0] = 0.35f;
            serieSecond.max = 60;
            serieSecond.titleStyle.show = false;
            serieSecond.label.show = false;
            serieSecond.gaugeAxis.show = false;
            serieSecond.gaugePointer.length = 0.85f;
            serieSecond.gaugePointer.backLength = 0.08f;
            serieSecond.gaugePointer.backExtraLength = 0f;
            serieSecond.gaugePointer.topLeftLength = 0.01f;
            serieSecond.gaugePointer.topRightLength = 0.01f;
            serieSecond.gaugePointer.bottomLeftLength = 0.01f;
            serieSecond.gaugePointer.bottomRightLength = 0.01f;
            serieSecond.gaugePointer.autoColor = true;
            serieSecond.gaugePointer.roundCap = true;
            serieSecond.gaugeAnchor.type = SymbolType.EmptyCircle;
            serieSecond.gaugeAnchor.size = 6.39f;
            serieSecond.gaugeAnchor.gap = 0;
            serieSecond.ClearData();
            serieSecond.AddYData(timeOfDay.Seconds % 60, "Second");
        }

        public void DefaultRangeClockGaugeChart()
        {
            CheckChartInit();
            ClearData();

            var serieHour = GetSerie<Gauge>(0);

            serieHour.startAngle = 0;
            serieHour.endAngle = 360;
            serieHour.center[1] = 0.45f;
            serieHour.radius[0] = 0.35f;
            serieHour.splitNumber = 24;
            serieHour.min = 0;
            serieHour.max = 24;
            serieHour.loop = true;
            serieHour.rangeDataMode = true;
            serieHour.titleStyle.show = false;
            serieHour.label.show = false;
            serieHour.gaugeAxis.axisLabel.distance = 17;
            serieHour.gaugeAxis.axisLabel.textStyle.fontSize = 14;
            serieHour.gaugeAxis.axisLabel.showStartLabel = false;
            serieHour.gaugeAxis.splitLine.distance = 0;
            serieHour.gaugeAxis.splitLine.lineStyle.width = 1;
            serieHour.gaugeAxis.splitLine.lineStyle.length = 7;
            serieHour.gaugeAxis.axisTick.show = false;
            serieHour.gaugeAxis.axisTick.distance = 5;

            AddData(0, 22, 8, "range1");
            AddData(0, 8, 11, "range2");
            AddData(0, 11, 13, "range3");
            AddData(0, 13, 17, "range4");
            AddData(0, 17, 22, "range5");
        }

        /// <summary>
        /// update clock time.
        /// ||更新时钟时间。
        /// </summary>
        public void UpdateClockTime()
        {
            var timeOfDay = System.DateTime.Now.TimeOfDay;
            var hour = timeOfDay.TotalHours % 12;
            var minute = timeOfDay.TotalMinutes % 60;
            var second = timeOfDay.Seconds % 60;
            UpdateClockTime(hour, minute, second);
        }

        /// <summary>
        /// update clock time.
        /// ||更新时钟时间。
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public void UpdateClockTime(double hour, double minute, double second)
        {
            UpdateData(0, 0, hour);
            UpdateData(1, 0, minute);
            UpdateData(2, 0, second);
        }

        /// <summary>
        /// update pointer value.
        /// ||更新指针值。
        /// </summary>
        /// <param name="value"></param>
        public void UpdatePointerValue(double value)
        {
            var serie = GetSerie<Gauge>(0);
            if (serie != null)
            {
                serie.gaugePointer.value = value;
                RefreshChart();
            }
        }
    }
}
