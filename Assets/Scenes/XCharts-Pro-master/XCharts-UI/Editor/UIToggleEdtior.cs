using System;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UIToggle), false)]
    public class UIToggleEditor : UIComponentEditor
    {

        [MenuItem("XCharts/UI/UIToggle", priority = 201)]
        [MenuItem("GameObject/UI/XCharts/UI/UIToggle", priority = 201)]
        public static void AddUIToggle()
        {
            XChartsEditor.AddGraph<UIToggle>("UIToggle");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_IsOn");
            PropertyField("m_Interactable");
            PropertyField("m_Text");
            PropertyField("m_OnColor");
            PropertyField("m_OffColor");
            PropertyField("m_MarkGap");
            PropertyField("m_MarkColor");
            PropertyField("m_Theme");
            PropertyField("m_BorderStyle");
            PropertyField("m_LabelStyle");
        }
    }
}