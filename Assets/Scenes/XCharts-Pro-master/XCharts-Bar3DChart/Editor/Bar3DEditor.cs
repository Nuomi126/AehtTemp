using XCharts.Runtime.Bar3Ds;

namespace XCharts.Editor.Bar3Ds
{
    [SerieEditor(typeof(Bar3D))]
    public class Bar3DEditor : SerieEditor<Bar3D>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_ColorBy");
            PropertyField("m_Stack");
            PropertyField("m_XAxisIndex");
            PropertyField("m_YAxisIndex");
            PropertyField("m_Bar3DType");
            PropertyField("m_BarPercentStack");
            PropertyField("m_BarWidth");
            PropertyField("m_BarGap");
            PropertyField("m_BarMaxWidth");
            PropertyFiledMore(() =>
            {
                PropertyFieldLimitMin("m_MinShow", 0);
                PropertyFieldLimitMin("m_MaxShow", 0);
                PropertyFieldLimitMin("m_MaxCache", 0);
                PropertyField("m_Ignore");
                PropertyField("m_IgnoreValue");
                PropertyField("m_IgnoreLineBreak");
                PropertyField("m_ShowAsPositiveNumber");
                PropertyField("m_Large");
                PropertyField("m_LargeThreshold");
                PropertyField("m_Clip");
            });
            PropertyField("m_ItemStyle");
            PropertyField("m_Bar3DStyle");
            PropertyField("m_Animation");
        }
    }
}