
using XCharts.Runtime.Sankeys;

namespace XCharts.Editor.Sankeys
{
    [SerieEditor(typeof(Sankey))]
    public class SankeyEditor : SerieEditor<Sankey>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Left");
            PropertyField("m_Right");
            PropertyField("m_Top");
            PropertyField("m_Bottom");
            PropertyField("m_NodeAlign");
            PropertyField("m_NodeWidth");
            PropertyField("m_NodeGap");
            PropertyField("m_Curveness");
            PropertyField("m_Draggable");
            PropertyField("m_Collapse");
            PropertyField("m_CollapseMutex");
            PropertyField("m_CollapseLevel");
            PropertyField("m_DefaultExpandDataIndex");
            PropertyField("m_LineGradient");
            PropertyField("m_LevelStyle");
            PropertyField("m_LineStyle");
            PropertyField("m_ItemStyle");
        }

        public override void OnEndCustomInspectorGUI()
        {
            PropertyFieldLinks();
        }
    }
}