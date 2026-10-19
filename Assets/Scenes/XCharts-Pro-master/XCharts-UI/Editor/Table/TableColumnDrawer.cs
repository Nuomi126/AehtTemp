using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableColumn), true)]
    public class TableColumnDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Column"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Content", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Width");
                PropertyField(prop, "m_HeaderLabelStyle");
                PropertyField(prop, "m_DataLabelStyle");
                --EditorGUI.indentLevel;
            }
        }
    }
}