
using XCharts.Runtime;
using XCharts.Runtime.Line3Ds;

namespace XCharts.Editor.Line3Ds
{
    [SerieEditor(typeof(Line3D))]
    public class Line3DEditor : SerieEditor<Line3D>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_LineType");
            if (serie.lineType == LineType.Smooth)
            {
                PropertyField("m_SmoothLimit");
            }
            PropertyField("m_Symbol");
            PropertyField("m_LineStyle");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
        }

        public override void OnEndCustomInspectorGUI()
        {
        }
    }
}