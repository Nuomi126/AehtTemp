
using UnityEditor;
using XCharts.Runtime.Gauges;

namespace XCharts.Editor.Gauges
{
    /// <summary>
    /// Editor class used to edit UI GaugeChart.
    /// </summary>
    [CustomEditor(typeof(GaugeChart), false)]
    public class GaugeChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/GaugeChart/Basic Gauge", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Basic Gauge", priority = 104)]
        public static void AddGaugeChart()
        {
            XChartsEditor.AddChart<GaugeChart>("GaugeChart");
        }

        [MenuItem("XCharts/GaugeChart/Progress Gauge", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Progress Gauge", priority = 104)]
        public static void AddGaugeChart_Progress()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart");
            chart.DefaultProgressGaugeChart();
        }

        [MenuItem("XCharts/GaugeChart/Section Gauge", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Section Gauge", priority = 104)]
        public static void AddGaugeChart_Section()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart");
            chart.DefaultSectionGaugeChart();
        }

        [MenuItem("XCharts/GaugeChart/Multi Gauge", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Multi Gauge", priority = 104)]
        public static void AddGaugeChart_Multi()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart");
            chart.DefaultMultipleGaugeChart();
        }

        [MenuItem("XCharts/GaugeChart/Barometer", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Barometer", priority = 104)]
        public static void AddGaugeChart_Barometer()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart", "Barometer");
            chart.DefaultBarometerGaugeChart();
        }

        [MenuItem("XCharts/GaugeChart/Clock", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Clock", priority = 104)]
        public static void AddGaugeChart_Clock()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart", "Clock");
            chart.DefaultClockGaugeChart();
        }

        [MenuItem("XCharts/GaugeChart/Range Clock", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/GaugeChart/Range Clock", priority = 104)]
        public static void AddGaugeChart_RangeClock()
        {
            var chart = XChartsEditor.AddChart<GaugeChart>("GaugeChart", "Clock");
            chart.DefaultRangeClockGaugeChart();
        }
    }
}