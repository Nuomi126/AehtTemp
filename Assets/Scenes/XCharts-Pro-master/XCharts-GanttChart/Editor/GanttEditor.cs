

using XCharts.Runtime.Gantts;

namespace XCharts.Editor.Gantts
{
    [SerieEditor(typeof(Gantt))]
    public class GanttEditor : SerieEditor<Gantt>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Stack");
            PropertyField("m_XAxisIndex");
            PropertyField("m_YAxisIndex");
            PropertyField("m_BarWidth");
            PropertyField("m_BarGap");
            PropertyField("m_Clip");
            PropertyField("m_Large");
            PropertyField("m_LargeThreshold");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
        }
    }
}