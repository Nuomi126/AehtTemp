using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// the row data of the table.
    /// ||表格的行数据。
    /// </summary>
    [Serializable]
    public class TableRow : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private string m_Name;
        [SerializeField] private float m_Height;
        [SerializeField] private List<TableCell> m_Data = new List<TableCell>();
        /// <summary>
        /// whether the row is visible.
        /// ||是否显示当前行。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the name of the row. can be used to display in the row header, use wildcard {name}.
        /// ||行名称。可用于显示在行表头，用通配符{name}。
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set { if (PropertyUtil.SetClass(ref m_Name, value)) SetComponentDirty(); }
        }
        /// <summary>
        /// the height of the row. default 0, auto height.
        /// ||当前行的自定义高度。默认为0不自定义。
        /// </summary>
        public float height
        {
            get { return m_Height; }
            set { if (PropertyUtil.SetStruct(ref m_Height, value)) SetComponentDirty(); }
        }
        /// <summary>
        /// the data list of the row.
        /// ||行数据列表。
        /// </summary>
        public List<TableCell> data
        {
            get { return m_Data; }
            set { if (PropertyUtil.SetClass(ref m_Data, value)) SetAllDirty(); }
        }
    }
}