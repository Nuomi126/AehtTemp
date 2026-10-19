
using UnityEditor;
using XCharts.Runtime.Treemaps;

namespace XCharts.Editor.Treemaps
{
    [CustomEditor(typeof(TreemapChart), false)]
    public class TreemapChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/TreemapChart/Treemap", priority = 106)]
        [MenuItem("GameObject/UI/XCharts/TreemapChart/Treemap", priority = 106)]
        public static void AddTreemapChart()
        {
            XChartsEditor.AddChart<TreemapChart>("TreemapChart");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(target == null) return;
            m_Chart = (TreemapChart)target;
        }
    }
}