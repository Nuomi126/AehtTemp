using UnityEditor;
using XCharts.Runtime.Bar3Ds;

namespace XCharts.Editor.Bar3Ds
{
    [CustomEditor(typeof(Bar3DChart), false)]
    public class BarChart3DEditor : BaseChartEditor
    {
        [MenuItem("XCharts/Bar3DChart/Cube", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Bar3DChart/Cube", priority = 101)]
        public static void AddBar3DChart()
        {
            XChartsEditor.AddChart<Bar3DChart>("Bar3DChart");
        }

        [MenuItem("XCharts/Bar3DChart/Grouped Cube", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Bar3DChart/Grouped Cube", priority = 101)]
        public static void AddBar3DChart_GroupedCube()
        {
            var chart = XChartsEditor.AddChart<Bar3DChart>("Bar3DChart");
            chart.DefaultGroupedCubeChart();
        }

        [MenuItem("XCharts/Bar3DChart/Stacked Cube", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Bar3DChart/Stacked Cube", priority = 101)]
        public static void AddBar3DChart_StackedCube()
        {
            var chart = XChartsEditor.AddChart<Bar3DChart>("Bar3DChart");
            chart.DefaultStackedCubeChart();
        }
        [MenuItem("XCharts/Bar3DChart/Cylinder", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Bar3DChart/Cylinder", priority = 101)]
        public static void AddBar3DChart_Cylinder()
        {
            var chart = XChartsEditor.AddChart<Bar3DChart>("Bar3DChart");
            chart.DefaultCylinderChart();
        }
    }
}