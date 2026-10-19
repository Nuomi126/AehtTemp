using System;
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UIText), false)]
    public class UITextEditor : UIComponentEditor
    {
        private bool m_BaseFoldout;

        [MenuItem("XCharts/UI/UIText", priority = 201)]
        [MenuItem("GameObject/UI/XCharts/UI/UIText", priority = 201)]
        public static void AddUIText()
        {
            AddUIComponent<UIText>("UIText");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Text");
            PropertyField("m_RaycastTarget");
            PropertyField("m_Background");
            PropertyField("m_TextStyle");
        }
    }
}