using XCharts.Runtime.PictorialBars;

namespace XCharts.Editor.PictorialBars
{
    [SerieEditor(typeof(PictorialBar))]
    public class PictorialBarEditor : SerieEditor<PictorialBar>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyField("m_ColorBy");
            PropertyField("m_XAxisIndex");
            PropertyField("m_YAxisIndex");
            PropertyFieldLimitMin("m_MinShow", 0);
            PropertyFieldLimitMin("m_MaxShow", 0);
            PropertyFieldLimitMin("m_MaxCache", 0);
            PropertyField("m_BarWidth");
            PropertyField("m_BarGap");
            PropertyField("m_BarMaxWidth");
            PropertyField("m_Clip");
            PropertyField("m_Ignore");
            PropertyField("m_IgnoreValue");

            PropertyField("m_Symbol");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
        }
    }
}