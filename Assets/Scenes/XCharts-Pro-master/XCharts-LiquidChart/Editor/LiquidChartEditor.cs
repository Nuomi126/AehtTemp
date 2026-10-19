
using UnityEditor;
using XCharts.Runtime.Liquids;

namespace XCharts.Editor.Liquids
{
    [CustomEditor(typeof(LiquidChart), false)]
    public class LiquidChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/LiquidChart/Basic Liquid", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/LiquidChart/Basic Liquid", priority = 104)]
        public static void AddLiquidChart()
        {
            XChartsEditor.AddChart<LiquidChart>("LiquidChart");
        }

        [MenuItem("XCharts/LiquidChart/Rect Liquid", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/LiquidChart/Rect Liquid", priority = 104)]
        public static void AddLiquidChart_Rect()
        {
            var chart = XChartsEditor.AddChart<LiquidChart>("LiquidChart");
            chart.DefaultRectLiquidChart();
        }

        [MenuItem("XCharts/LiquidChart/Round Rect Liquid", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/LiquidChart/Round Rect Liquid", priority = 104)]
        public static void AddLiquidChart_RoundRect()
        {
            var chart = XChartsEditor.AddChart<LiquidChart>("LiquidChart");
            chart.DefaultRoundRectLiquidChart();
        }

        [MenuItem("XCharts/LiquidChart/Capsule Liquid", priority = 104)]
        [MenuItem("GameObject/UI/XCharts/LiquidChart/Capsule Liquid", priority = 104)]
        public static void AddLiquidChart_Capsule()
        {
            var chart = XChartsEditor.AddChart<LiquidChart>("LiquidChart");
            chart.DefaultCapsuleLiquidChart();
        }
    }
}