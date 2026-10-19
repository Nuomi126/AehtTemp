using XCharts.Runtime.Pie3Ds;

namespace XCharts.Editor.Pie3Ds
{
    [SerieEditor(typeof(Pie3D))]
    public class Pie3DEditor : SerieEditor<Pie3D>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_RoseType");
            PropertyField("m_Gap");
            PropertyTwoFiled("m_Center");
            PropertyTwoFiled("m_Radius");
            //PropertyField("m_StartAngle");
            PropertyField("m_AvoidLabelOverlap");
            PropertyFiledMore(() =>
            {
                PropertyField("m_MinAngle");
                PropertyField("m_RoundCap");
                PropertyField("m_Ignore");
                PropertyField("m_IgnoreValue");
                PropertyField("m_MinShowLabel");
                PropertyField("m_MinShowLabelValue");
            });
            PropertyField("m_Pie3DStyle");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
        }
    }
}