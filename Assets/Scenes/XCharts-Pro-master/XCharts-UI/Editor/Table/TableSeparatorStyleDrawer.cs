using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableSeparatorStyle), true)]
    public class TableSeparatorStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Separator Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Vertical");
                PropertyField(prop, "m_VerticalColumn");
                PropertyField(prop, "m_VerticalWidth");
                PropertyField(prop, "m_VerticalColor");
                PropertyField(prop, "m_Horizontal");
                PropertyField(prop, "m_HorizontalColumn");
                PropertyField(prop, "m_HorizontalWidth");
                PropertyField(prop, "m_HorizontalColor");
                --EditorGUI.indentLevel;
            }
        }
    }
}