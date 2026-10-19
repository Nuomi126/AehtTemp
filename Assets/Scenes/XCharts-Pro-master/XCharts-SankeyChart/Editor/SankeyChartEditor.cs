
using UnityEditor;
using XCharts.Runtime.Sankeys;

namespace XCharts.Editor.Sankeys
{
    [CustomEditor(typeof(SankeyChart), false)]
    public class SankeyChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/SankeyChart/Sankey", priority = 108)]
        [MenuItem("GameObject/UI/XCharts/SankeyChart/Sankey", priority = 108)]
        public static void AddSankeyChart()
        {
            XChartsEditor.AddChart<SankeyChart>("SankeyChart");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(target == null) return;
            m_Chart = (SankeyChart)target;
        }
    }
}