
using XCharts.Runtime.Graphs;

namespace XCharts.Editor.Graphs
{
    [SerieEditor(typeof(Graph))]
    public class GraphEditor : SerieEditor<Graph>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Left");
            PropertyField("m_Right");
            PropertyField("m_Top");
            PropertyField("m_Bottom");
            PropertyField("m_Symbol");
            PropertyField("m_LevelStyle");
            PropertyField("m_LineStyle");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
            PropertyField("m_Force");
        }

        public override void OnEndCustomInspectorGUI()
        {
            PropertyFieldLinks();
        }
    }
}