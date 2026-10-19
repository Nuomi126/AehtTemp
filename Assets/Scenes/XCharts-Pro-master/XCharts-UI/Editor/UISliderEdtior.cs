using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(UISlider.HandleStyle), true)]
    public class HandleStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "HandleStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_GradientColor");
                PropertyField(prop, "m_Symbol");
                --EditorGUI.indentLevel;
            }
        }
    }

    [CustomPropertyDrawer(typeof(UISlider.MarkStyle), true)]
    public class MarkStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "MarkStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Included");
                PropertyField(prop, "m_Symbol");
                PropertyField(prop, "m_Label");
                --EditorGUI.indentLevel;
            }
        }
    }

    [CustomEditor(typeof(UISlider), false)]
    public class UISliderEditor : UIComponentEditor
    {

        [MenuItem("XCharts/UI/UISlider", priority = 201)]
        [MenuItem("GameObject/UI/XCharts/UI/UISlider", priority = 201)]
        public static void AddUISlider()
        {
            XChartsEditor.AddGraph<UISlider>("UISlider");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Interactable");
            PropertyField("m_ValueType");
            PropertyField("m_Min");
            PropertyField("m_Max");
            PropertyField("m_Step");
            PropertyTwoFiled("m_Value");
            PropertyListField("m_MarkValues");
            PropertyListField("m_MarkNames");
            PropertyField("m_Background");
            PropertyField("m_FillStyle");
            PropertyField("m_MarkStyle");
            PropertyField("m_HandleStyle");
        }
    }
}