using UnityEditor;
using UnityEngine;
using XCharts.Runtime.Pie3Ds;

namespace XCharts.Editor.Pie3Ds
{
    [CustomPropertyDrawer(typeof(Pie3DStyle), true)]
    public class Pie3DStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Pie3DStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", false))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_EllipseMinorAxis");
                PropertyField(prop, "m_PieMaxHeight");
                PropertyField(prop, "m_PieAutoHeight");
                PropertyField(prop, "m_SelectedOffset");
                PropertyField(prop, "m_PeripherySideColorRate");
                PropertyField(prop, "m_StartSideColorRate");
                PropertyField(prop, "m_EndSideColorRate");
                PropertyField(prop, "m_DrawSplitSide");
                --EditorGUI.indentLevel;
            }
        }
    }
}