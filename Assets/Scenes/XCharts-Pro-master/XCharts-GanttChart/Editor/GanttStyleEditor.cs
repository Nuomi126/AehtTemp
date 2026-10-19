
using UnityEditor;
using XCharts.Runtime.Gantts;

namespace XCharts.Editor.Gantts
{
    [ComponentEditor(typeof(GanttStyle))]
    public class GanttStyleEditor : MainComponentEditor<GanttStyle>
    {
        public override void OnInspectorGUI()
        {
            ++EditorGUI.indentLevel;
            PropertyField("m_GanttType");
            PropertyField("m_Invert");
            PropertyField("m_DragEnable");
            PropertyField("m_DragSensitivity");
            PropertyField("m_DragRangeMin");
            PropertyField("m_DragRangeMax");
            PropertyField("m_AutoAdsorbAlignline");
            PropertyField("m_AlignlineDataIndex");
            PropertyField("m_AliginLine");
            PropertyField("m_SplitArea");
            //PropertyField("m_MarkLines");
            --EditorGUI.indentLevel;
        }
    }
}