using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableScrollbarStyle), true)]
    public class TableScrollbarStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Scorllbar Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Horizontal");
                PropertyField(prop, "m_Vertical");
                PropertyField(prop, "m_Width");
                PropertyField(prop, "m_BackgroundColor");
                PropertyField(prop, "m_Color");
                PropertyField(prop, "m_BorderStyle");
                --EditorGUI.indentLevel;
            }
        }
    }
}