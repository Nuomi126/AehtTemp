using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(FillStyle), true)]
    public class FillStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "FillStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Color");
                PropertyField(prop, "m_ToColor");
                PropertyField(prop, "m_Width");
                PropertyField(prop, "m_Height");
                PropertyField(prop, "m_Radius");
                PropertyField(prop, "m_RoundCap");
                PropertyField(prop, "m_AutoOffset");
                PropertyField(prop, "m_Location");
                PropertyField(prop, "m_Background");
                --EditorGUI.indentLevel;
            }
        }
    }
}