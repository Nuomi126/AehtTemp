using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableColumnHeaderStyle), true)]
    public class TableColumnHeaderStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Column Header Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Height");
                PropertyField(prop, "m_BackgroundColor");
                PropertyField(prop, "m_Opaque");
                PropertyField(prop, "m_Background");
                PropertyField(prop, "m_DefaultLabelStyle");
                PropertyField(prop, "m_BorderStyle");
                --EditorGUI.indentLevel;
            }
        }
    }
}