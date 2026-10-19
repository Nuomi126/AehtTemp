using UnityEditor;
using UnityEngine;
using XCharts.Runtime.Bar3Ds;

namespace XCharts.Editor.Bar3Ds
{
    [CustomPropertyDrawer(typeof(Bar3DStyle), true)]
    public class Bar3DStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Bar3DStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_VerticalArrange");
                PropertyField(prop, "m_VerticalArrangeGap");
                PropertyField(prop, "m_VerticalArrangeDire");
                PropertyField(prop, "m_BarOffset");
                PropertyField(prop, "m_WidthRate");
                PropertyField(prop, "m_TopColorRate");
                PropertyField(prop, "m_BottomColorRate");
                PropertyField(prop, "m_SideColorRate");
                PropertyField(prop, "m_TopColor");
                PropertyField(prop, "m_BottomColor");
                PropertyField(prop, "m_SideColor");
                PropertyField(prop, "m_CubeDire");
                PropertyField(prop, "m_CylinderRate");
                PropertyField(prop, "m_OccludedAreaBorder");
                PropertyField(prop, "m_OccludedAreaBorderDash");
                PropertyField(prop, "m_BorderDash");
                --EditorGUI.indentLevel;
            }
        }
    }
}