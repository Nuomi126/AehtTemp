using System;
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UITable), false)]
    public class UITableEditor : UIComponentEditor
    {
        static class Styles
        {
            public static readonly GUIContent btnRandomData = new GUIContent("Generate Default Data", "");
        }
        private bool m_BaseFoldout;

        [MenuItem("XCharts/UI/UITable", priority = 202)]
        [MenuItem("GameObject/UI/XCharts/UI/UITable", priority = 202)]
        public static void AddUITable()
        {
            AddUIComponent<UITable>("UITable");
        }

        protected override void OnStartInspectorGUI()
        {
            m_BaseFoldout = ChartEditorHelper.DrawHeader("Base", m_BaseFoldout, false, null, null);
            if (m_BaseFoldout)
            {
                PropertyField("m_SupportDrag");
                PropertyField("m_SupportScroll");
                PropertyField("m_SupportSelect");
                PropertyField("m_ScrollSensitivity");
                PropertyField("m_SelectColor");
                PropertyField("m_SelectHeaderColor");
            }
            PropertyField("m_Theme");
            PropertyField("m_Background");
            PropertyField("m_BorderStyle");
            PropertyField("m_SeparatorStyle");
            PropertyField("m_ScrollbarStyle");
            PropertyField("m_CarouselStyle");
            PropertyField("m_ColumnHeaderStyle");
            PropertyField("m_ColumnStyle");
            PropertyField("m_RowHeaderStyle");
            PropertyField("m_RowStyle");
            PropertyListField("m_Columns");
            PropertyListField("m_Data");
        }

        protected override void OnDebugStartInspectorGUI()
        {
            if (GUILayout.Button(Styles.btnRandomData))
            {
                TableGenDefaultDataEditor.ShowWindow();
                TableGenDefaultDataEditor.UpdateData(target as UITable);
            }
        }
    }
}