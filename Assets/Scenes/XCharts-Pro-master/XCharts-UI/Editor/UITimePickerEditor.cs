using System;
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UITimePicker), false)]
    public class UITimePickerEditor : UIComponentEditor
    {
        private bool m_BaseFoldout;

        //[MenuItem("XCharts/UI/UITimePicker", priority = 203)]
        //[MenuItem("GameObject/UI/XCharts/UI/UITimePicker", priority = 203)]
        public static void AddUITimePicker()
        {
            AddUIComponent<UITimePicker>("UITimePicker");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Theme");
            PropertyField("m_Background");
        }
    }
}