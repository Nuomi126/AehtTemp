
using XCharts.Runtime.Treemaps;

namespace XCharts.Editor.Treemaps
{
    [SerieEditor(typeof(Treemap))]
    public class TreemapEditor : SerieEditor<Treemap>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Gap");
            PropertyField("m_Left");
            PropertyField("m_Right");
            PropertyField("m_Top");
            PropertyField("m_Bottom");
            PropertyField("m_LeafDepth");
            PropertyField("m_VisualDimension");
            PropertyField("m_DrillDownIcon");
            PropertyField("m_CustomGoUpLevelButton");
            PropertyField("m_LevelStyle");
            PropertyField("m_UpperLabel");
            PropertyField("m_ItemStyle");
        }
    }
}