
using UnityEditor;
using XCharts.Runtime.Graphs;

namespace XCharts.Editor.Graphs
{
    [CustomEditor(typeof(GraphChart), false)]
    public class GraphChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/GraphChart/Graph", priority = 109)]
        [MenuItem("GameObject/UI/XCharts/GraphChart/Graph", priority = 109)]
        public static void AddGraphChart()
        {
            XChartsEditor.AddChart<GraphChart>("GraphChart");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(target == null) return;
            m_Chart = (GraphChart)target;
        }
    }
}