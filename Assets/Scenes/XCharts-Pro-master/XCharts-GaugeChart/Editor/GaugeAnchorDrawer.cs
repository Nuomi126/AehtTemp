
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.Gauges;

namespace XCharts.Editor.Gauges
{

    [CustomPropertyDrawer(typeof(GaugeAnchor), true)]
    public class GaugeAnchorDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Gauge Anchor"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_ShowAbove");
                PropertyField(prop, "m_Type");
                PropertyField(prop, "m_Size");
                PropertyField(prop, "m_Gap");
                PropertyField(prop, "m_ItemStyle");
                --EditorGUI.indentLevel;
            }
        }
    }
}