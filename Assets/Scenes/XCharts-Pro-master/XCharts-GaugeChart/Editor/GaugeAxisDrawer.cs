
using UnityEditor;
using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Gauges;

namespace XCharts.Editor.Gauges
{
    [CustomPropertyDrawer(typeof(GaugeAxis), true)]
    public class GaugeAxisDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Gauge Axis"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Type");
                PropertyField(prop, "m_Overlap");
                PropertyField(prop, "m_Distance");
                PropertyField(prop, "m_AxisLine");
                PropertyField(prop, "m_SplitLine");
                PropertyField(prop, "m_AxisTick");
                PropertyField(prop, "m_AxisLabel");
                PropertyField(prop, "m_AxisLabelText");
                --EditorGUI.indentLevel;
            }
        }
    }


    [CustomPropertyDrawer(typeof(StageColor), true)]
    public class GaugeAxisLineStageColorDrawer : BasePropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            Rect drawRect = pos;
            drawRect.height = EditorGUIUtility.singleLineHeight;
            SerializedProperty m_Percent = prop.FindPropertyRelative("m_Percent");
            SerializedProperty m_Color = prop.FindPropertyRelative("m_Color");

            ChartEditorHelper.MakeTwoField(ref drawRect, drawRect.width, m_Percent, m_Color, "Stage");
            drawRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return 1 * EditorGUIUtility.singleLineHeight + 1 * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [CustomPropertyDrawer(typeof(GaugePointer), true)]
    public class GaugePointerDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Gauge Pointer"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Value");
                PropertyField(prop, "m_Length");
                PropertyField(prop, "m_BackLength");
                PropertyField(prop, "m_BackExtraLength");
                PropertyField(prop, "m_TopLeftLength");
                PropertyField(prop, "m_TopRightLength");
                PropertyField(prop, "m_BottomLeftLength");
                PropertyField(prop, "m_BottomRightLength");
                PropertyField(prop, "m_Distance");
                PropertyField(prop, "m_RoundCap");
                PropertyField(prop, "m_AutoColor");
                PropertyField(prop, "m_Color");
                PropertyField(prop, "m_ToColor");
                --EditorGUI.indentLevel;
            }
        }
    }

    [CustomPropertyDrawer(typeof(GaugeAxisLine), true)]
    public class GaugeAxisLineDrawer : BaseLineDrawer
    {
        public override string ClassName { get { return "Axis Line"; } }
        protected override void DrawExtendeds(SerializedProperty prop)
        {
            base.DrawExtendeds(prop);
            PropertyField(prop, "m_BackgroundColor");
            PropertyField(prop, "m_StageGradient");
            PropertyListField(prop, "m_StageColor");
        }
    }
}