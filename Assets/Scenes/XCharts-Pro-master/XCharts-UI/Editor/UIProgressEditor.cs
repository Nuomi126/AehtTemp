using UnityEditor;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomEditor(typeof(UIProgress), false)]
    public class UIProgressEditor : UIComponentEditor
    {
        [MenuItem("XCharts/UI/UIProgress", priority = 200)]
        [MenuItem("GameObject/UI/XCharts/UI/UIProgress", priority = 200)]
        public static void AddUIProgress()
        {
            AddUIComponent<UIProgress>("UIProgress");
        }

        [MenuItem("XCharts/UI/UIProgress - Circle", priority = 200)]
        [MenuItem("GameObject/UI/XCharts/UI/UIProgress - Circle", priority = 200)]
        public static void AddUIProgress_Circle()
        {
            var progress = AddUIComponent<UIProgress>("UIProgress");
            progress.DefaultCircle();
        }

        protected override void OnStartInspectorGUI()
        {
            PropertyField("m_Type");
            PropertyField("m_Min");
            PropertyField("m_Max");
            PropertyField("m_Step");
            PropertyField("m_Value");
            PropertyField("m_FillStyle");
            PropertyField("m_LabelStyle");
        }
    }
}