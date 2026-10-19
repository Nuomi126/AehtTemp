
using UnityEditor;
using XCharts.Runtime.Line3Ds;

namespace XCharts.Editor.Line3Ds
{
    [CustomEditor(typeof(Line3DChart), false)]
    public class Line3DChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/Line3DChart/Line3D", priority = 110)]
        [MenuItem("GameObject/UI/XCharts/Line3DChart/Line3D", priority = 110)]
        public static void AddLine3DChart()
        {
            XChartsEditor.AddChart<Line3DChart>("Line3DChart");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(target == null) return;
            m_Chart = (Line3DChart)target;
        }
    }
}