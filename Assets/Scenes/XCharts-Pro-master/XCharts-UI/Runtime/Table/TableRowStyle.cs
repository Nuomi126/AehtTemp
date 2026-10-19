using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// The style of table row.
    /// ||行样式。
    /// </summary>
    [Serializable]
    public class TableRowStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private float m_RowHeight = 0f;
        [SerializeField] private float m_RowGap = 0f;
        [SerializeField] private float m_MinRowHeight = 30f;
        [SerializeField][Since("v3.10.0")] private BorderStyle m_BorderStyle = new BorderStyle();
        [SerializeField] private List<Color32> m_RowBackgroundColors = new List<Color32> { };

        /// <summary>
        /// whether the row is visible.
        /// ||是否显示行。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the height of row.
        /// ||行高。
        /// </summary>
        public float rowHeight
        {
            get { return m_RowHeight; }
            set { if (PropertyUtil.SetStruct(ref m_RowHeight, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the gap of row. 
        /// ||行间距。
        /// </summary>
        public float rowGap
        {
            get { return m_RowGap; }
            set { if (PropertyUtil.SetStruct(ref m_RowGap, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the min height of row. 
        /// ||最小行高。
        /// </summary>
        public float minRowHeight
        {
            get { return m_MinRowHeight; }
            set { if (PropertyUtil.SetStruct(ref m_MinRowHeight, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the border style of row.
        /// ||行边框样式。
        /// </summary>
        public BorderStyle borderStyle
        {
            get { return m_BorderStyle; }
            set { if (PropertyUtil.SetClass(ref m_BorderStyle, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the background color of row.
        /// ||行背景颜色列表。
        /// </summary>
        public List<Color32> rowBackgroundColors { get { return m_RowBackgroundColors; } set { m_RowBackgroundColors = value; } }
    }
}