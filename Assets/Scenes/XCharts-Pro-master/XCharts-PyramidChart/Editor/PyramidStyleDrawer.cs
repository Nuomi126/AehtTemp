using UnityEditor;
using UnityEngine;
using XCharts.Runtime.Pyramids;

namespace XCharts.Editor.Pyramids
{
    [CustomPropertyDrawer(typeof(PyramidStyle), true)]
    public class PyramidStyleDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "PyramidStyle"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_3D");
                PropertyField(prop, "m_DrawTop");
                PropertyField(prop, "m_BottomPointRate");
                PropertyField(prop, "m_LeftPointRate");
                PropertyField(prop, "m_RightPointRate");
                PropertyField(prop, "m_LeftColorOpacity");
                PropertyField(prop, "m_RightColorOpacity");
                PropertyField(prop, "m_LabelLineMargin");
                --EditorGUI.indentLevel;
            }
        }
    }
}