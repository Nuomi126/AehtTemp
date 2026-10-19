using UnityEditor;
using UnityEngine;
using XCharts.Runtime.Graphs;

namespace XCharts.Editor.Graphs
{
    [CustomPropertyDrawer(typeof(Force), true)]
    public class ForceDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Force"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            if (MakeComponentFoldout(prop, "m_Show", true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, "m_Repulsion");
                PropertyField(prop, "m_Gravity");
                PropertyField(prop, "m_StepFactor");
                PropertyField(prop, "m_EdgeLength");
                PropertyField(prop, "m_Iterations");
                PropertyField(prop, "m_CenterDataIndex");
                --EditorGUI.indentLevel;
            }
        }
    }
}