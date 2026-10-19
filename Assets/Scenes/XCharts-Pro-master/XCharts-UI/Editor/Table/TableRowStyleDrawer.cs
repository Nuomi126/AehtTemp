using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableRowStyle), true)]
    public class TableRowStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Row Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_RowHeight");
                PropertyField(prop, "m_RowGap");
                PropertyField(prop, "m_MinRowHeight");
                PropertyField(prop, "m_BorderStyle");
                PropertyField(prop, "m_RowBackgroundColors");
                --EditorGUI.indentLevel;
            }
        }
    }
}