
using XCharts.Editor;
using XCharts.Runtime.Funnels;

namespace XCharts.Editor.Funnels
{
    [SerieEditor(typeof(Funnel))]
    public class FunnelEditor : SerieEditor<Funnel>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Min");
            PropertyField("m_Max");
            PropertyField("m_MinSize");
            PropertyField("m_MaxSize");
            PropertyField("m_Orient");
            PropertyField("m_DataSortType");
            PropertyField("m_Align");
            PropertyField("m_Gap");
            PropertyField("m_Left");
            PropertyField("m_Right");
            PropertyField("m_Top");
            PropertyField("m_Bottom");
            PropertyField("m_RectEnd");
            PropertyField("m_DynamicHeight");
            PropertyField("m_ItemStyle");
        }
    }
}