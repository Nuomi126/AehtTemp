using System;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UIImage), false)]
    public class UIImageEditor : UIComponentEditor
    {

        [MenuItem("XCharts/UI/UIImage", priority = 201)]
        [MenuItem("GameObject/UI/XCharts/UI/UIImage", priority = 201)]
        public static void AddUIStatistic()
        {
            XChartsEditor.AddGraph<UIImage>("UIImage");
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Theme");
            PropertyField("m_Background");
        }
    }
}