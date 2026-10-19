using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// The style of table column.
    /// ||列样式。
    /// </summary>
    [Serializable]
    public class TableColumnStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private float m_ColumnWidth = 0f;
        [SerializeField] private float m_MinColumnWidth = 100f;
        [SerializeField] private LabelStyle m_DefaultLabelStyle = new LabelStyle() { show = true };
        [SerializeField] private List<Color32> m_ColumnBackgroundColors = new List<Color32> { };

        /// <summary>
        /// whether the column is visible.
        /// ||是否显示列。 
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the width of column. default 0, auto width. if set, it will be used first.
        /// ||列宽。默认0，自适应。Columns中设置有时优先用Columns中的值，没有时用这个值。
        /// </summary>
        public float columnWidth
        {
            get { return m_ColumnWidth; }
            set { if (PropertyUtil.SetStruct(ref m_ColumnWidth, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the min width of column.
        /// ||最小列宽。
        /// </summary>
        public float minColumnWidth
        {
            get { return m_MinColumnWidth; }
            set { if (PropertyUtil.SetStruct(ref m_MinColumnWidth, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the default style of the column label.
        /// ||默认的列数据文本样式。
        /// </summary>
        public LabelStyle defaultLabelStyle
        {
            get { return m_DefaultLabelStyle; }
            set { if (PropertyUtil.SetClass(ref m_DefaultLabelStyle, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the background color of column.
        /// ||列背景颜色列表。
        /// </summary>
        public List<Color32> columnBackgroundColors { get { return m_ColumnBackgroundColors; } set { m_ColumnBackgroundColors = value; } }
    }
}