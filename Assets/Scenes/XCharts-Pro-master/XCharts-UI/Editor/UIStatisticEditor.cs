using System;
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UIStatistic), false)]
    public class UIStatisticEditor : UIComponentEditor
    {
        private bool m_BaseFoldout;

        [MenuItem("XCharts/UI/UIStatistic", priority = 201)]
        [MenuItem("GameObject/UI/XCharts/UI/UIStatistic", priority = 201)]
        public static void AddUIStatistic()
        {
            AddUIComponent<UIStatistic>("UIStatistic");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Type");
            PropertyField("m_Title");
            PropertyField("m_Value");
            PropertyField("m_ItemGap");
            PropertyField("m_Location");
            PropertyField("m_Theme");
            PropertyField("m_Background");
            PropertyField("m_TitleLabelStyle");
            PropertyField("m_ValueLabelStyle");
            PropertyField("m_AnimationStyle");
        }
    }
}