using UnityEditor;
using XCharts.Runtime.Pie3Ds;

namespace XCharts.Editor.Pie3Ds
{
    [CustomEditor(typeof(Pie3DChart), false)]
    public class Pie3DChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/Pie3DChart/3D Pie", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Pie3DChart/3D Pie", priority = 101)]
        public static void AddPie3DChart()
        {
            XChartsEditor.AddChart<Pie3DChart>("Pie3DChart");
        }

        [MenuItem("XCharts/Pie3DChart/3D Donut", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Pie3DChart/3D Donut", priority = 101)]
        public static void AddPie3DChart_Donut()
        {
            var chart = XChartsEditor.AddChart<Pie3DChart>("Pie3DChart");
            chart.DefaultDonutPie3DChart();
        }

        [MenuItem("XCharts/Pie3DChart/2D Ellipse", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Pie3DChart/2D Ellipse", priority = 101)]
        public static void AddPie3DChart_Ellipse()
        {
            var chart = XChartsEditor.AddChart<Pie3DChart>("Pie3DChart");
            chart.DefaultEllipsePie3DChart();
        }

        [MenuItem("XCharts/Pie3DChart/3D Auto Height Pie", priority = 101)]
        [MenuItem("GameObject/UI/XCharts/Pie3DChart/3D Auto Height Pie", priority = 101)]
        public static void AddPie3DChart_AutoHeightPie()
        {
            var chart = XChartsEditor.AddChart<Pie3DChart>("Pie3DChart");
            chart.DefaultAutoHeightPie3DChart();
        }
    }
}