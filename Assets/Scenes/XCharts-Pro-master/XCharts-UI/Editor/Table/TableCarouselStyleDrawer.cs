using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableCarouselStyle), true)]
    public class TableCarouselStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Carousel Style"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Type");
                PropertyField(prop, "m_Mode");
                PropertyField(prop, "m_Speed");
                PropertyField(prop, "m_WaitTime");
                PropertyField(prop, "m_Pause");
                PropertyField(prop, "m_HoverPause");
                --EditorGUI.indentLevel;
            }
        }
    }
}