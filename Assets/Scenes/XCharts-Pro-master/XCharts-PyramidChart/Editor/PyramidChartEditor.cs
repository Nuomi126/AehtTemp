
using UnityEditor;
using XCharts.Runtime.Pyramids;

namespace XCharts.Editor.Pyramids
{
    [CustomEditor(typeof(PyramidChart), false)]
    public class PyramidChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/PyramidChart/3D Pyramid", priority = 106)]
        [MenuItem("GameObject/UI/XCharts/PyramidChart/3D Pyramid", priority = 106)]
        public static void AddPyramidChart()
        {
            XChartsEditor.AddChart<PyramidChart>("PyramidChart");
        }

        [MenuItem("XCharts/PyramidChart/2D Pyramid", priority = 106)]
        [MenuItem("GameObject/UI/XCharts/PyramidChart/2D Pyramid", priority = 106)]
        public static void AddPyramidChart_2D()
        {
            var chart = XChartsEditor.AddChart<PyramidChart>("PyramidChart");
            chart.Default2DPyramidChart();
        }
    }
}