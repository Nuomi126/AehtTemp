using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XCharts.Runtime.UI
{
    public enum DataType
    {
        GameObject,
        String,
        Double,
        Bool,
        Sprite,
    }
    /// <summary>
    /// 表格的单元格数据。
    /// </summary>
    [Serializable]
    public class TableCell : ChildComponent, ITableCell
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private DataType m_Type = DataType.String;
        [SerializeField][FormerlySerializedAs("m_Value")] private string m_StringValue;
        [SerializeField] private double m_DoubleValue;
        [SerializeField] private bool m_BoolValue;
        [SerializeField] private GameObject m_GameObjectValue;
        [SerializeField][FormerlySerializedAs("m_Sprite")] private Sprite m_SpriteValue;
        /// <summary>
        /// whether the cell is visible.
        /// ||是否显示当前数据。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the type of the cell data.
        /// ||当前数据的类型。
        /// </summary>
        public DataType type
        {
            get { return m_Type; }
            set { if (PropertyUtil.SetStruct(ref m_Type, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the string data. when type is String.
        /// ||当前数据的字符串值。当type为String时有效。
        /// </summary>
        public string stringValue
        {
            get { return m_StringValue; }
            set { if (PropertyUtil.SetClass(ref m_StringValue, value)) { type = DataType.String; SetAllDirty(); } }
        }
        /// <summary>
        /// the double data. when type is Double.
        /// ||当前数据的双精度值。当type为Double时有效。
        /// </summary> 
        public double doubleValue
        {
            get { return m_DoubleValue; }
            set { if (PropertyUtil.SetStruct(ref m_DoubleValue, value)) { type = DataType.Double; SetAllDirty(); } }
        }
        /// <summary>
        /// the bool data. when type is Bool.
        /// ||当前数据的布尔值。当type为Bool时有效。 
        /// </summary>
        public bool boolValue
        {
            get { return m_BoolValue; }
            set { if (PropertyUtil.SetStruct(ref m_BoolValue, value)) { type = DataType.Bool; SetAllDirty(); } }
        }
        /// <summary>
        /// the gameobject data. when type is GameObject.
        /// ||当前数据的GameObject值。当type为GameObject时有效。
        /// </summary>
        public GameObject gameObjectValue
        {
            get { return m_GameObjectValue; }
            set { if (PropertyUtil.SetClass(ref m_GameObjectValue, value)) { type = DataType.GameObject; SetAllDirty(); } }
        }
        /// <summary>
        /// the sprite data. when type is Sprite.
        /// ||当前数据的Sprite值。当type为Sprite时有效。
        /// </summary>
        public Sprite spriteValue
        {
            get { return m_SpriteValue; }
            set { if (PropertyUtil.SetClass(ref m_SpriteValue, value)) { type = DataType.Sprite; SetAllDirty(); } }
        }
        /// <summary>
        /// get the content of the cell.
        /// ||获取当前数据的内容。
        /// </summary>
        public string GetContent()
        {
            switch (m_Type)
            {
                case DataType.String:
                    return m_StringValue;
                case DataType.Double:
                    return m_DoubleValue.ToString();
                case DataType.Bool:
                    return m_BoolValue.ToString();
                case DataType.Sprite:
                    return "";
                default:
                    return m_StringValue;
            }
        }
    }
}