
using UnityEditor;
using XCharts.Runtime.PictorialBars;

namespace XCharts.Editor.PictorialBars
{
    [CustomEditor(typeof(PictorialBarChart), false)]
    public class PictorialBarChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/PictorialBarChart/PictorialBar", priority = 105)]
        [MenuItem("GameObject/UI/XCharts/PictorialBarChart/PictorialBar", priority = 105)]
        public static void AddPictorialBarChart()
        {
            XChartsEditor.AddChart<PictorialBarChart>("PictorialBarChart");
        }
    }
}