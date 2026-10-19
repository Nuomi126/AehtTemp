using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableColumnStyle), true)]
    public class TableColumnStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Column Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_ColumnWidth");
                PropertyField(prop, "m_MinColumnWidth");
                PropertyField(prop, "m_DefaultLabelStyle");
                PropertyField(prop, "m_ColumnBackgroundColors");
                --EditorGUI.indentLevel;
            }
        }
    }
}