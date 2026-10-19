using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableRow), true)]
    public class TableRowDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Row"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Name");
                PropertyField(prop, "m_Height");
                PropertyListField(prop, "m_Data");
                --EditorGUI.indentLevel;
            }
        }
    }
}