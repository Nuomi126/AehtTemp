
using XCharts.Runtime.Pyramids;

namespace XCharts.Editor.Pyramids
{
    [SerieEditor(typeof(Pyramid))]
    public class PyramidEditor : SerieEditor<Pyramid>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_Gap");
            PropertyField("m_Left");
            PropertyField("m_Right");
            PropertyField("m_Top");
            PropertyField("m_Bottom");
            PropertyField("m_ItemStyle");
            PropertyField("m_PyramidStyle");
        }
    }
}