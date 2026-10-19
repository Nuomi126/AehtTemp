using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    [CustomPropertyDrawer(typeof(TableCell), true)]
    public class TableCellDrawer : BasePropertyDrawer
    {
        public override string ClassName { get { return "Cell"; } }
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            base.OnGUI(pos, prop, label);
            var m_Type = prop.FindPropertyRelative("m_Type");
            if (MakeComponentFoldout(prop, GetValuePropertyField(m_Type.enumValueIndex), true))
            {
                ++EditorGUI.indentLevel;
                PropertyField(prop, m_Type);
                switch (m_Type.enumValueIndex)
                {
                    case (int)DataType.GameObject:
                        PropertyField(prop, "m_GameObjectValue");
                        break;
                    case (int)DataType.String:
                        PropertyField(prop, "m_StringValue");
                        break;
                    case (int)DataType.Double:
                        PropertyField(prop, "m_DoubleValue");
                        break;
                    case (int)DataType.Bool:
                        PropertyField(prop, "m_BoolValue");
                        break;
                    case (int)DataType.Sprite:
                        PropertyField(prop, "m_SpriteValue");
                        break;
                }
                --EditorGUI.indentLevel;
            }
        }

        private static string GetValuePropertyField(int typeIndex)
        {
            switch (typeIndex)
            {
                case (int)DataType.GameObject:
                    return "m_GameObjectValue";
                case (int)DataType.String:
                    return "m_StringValue";
                case (int)DataType.Double:
                    return "m_DoubleValue";
                case (int)DataType.Bool:
                    return "m_BoolValue";
                case (int)DataType.Sprite:
                    return "m_SpriteValue";
                default:
                    return "m_StringValue";
            }
        }
    }
}