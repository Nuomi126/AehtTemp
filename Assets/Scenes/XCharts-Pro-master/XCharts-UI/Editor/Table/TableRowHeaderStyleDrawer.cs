using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableRowHeaderStyle), true)]
    public class TableRowHeaderStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Row Header Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Width");
                PropertyField(prop, "m_BackgroundColor");
                PropertyField(prop, "m_LabelStyle");
                --EditorGUI.indentLevel;
            }
        }
    }
}