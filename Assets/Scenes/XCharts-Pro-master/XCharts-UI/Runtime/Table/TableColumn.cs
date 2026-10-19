using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 列表头内容和样式。
    /// </summary>
    [Serializable]
    public class TableColumn : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private string m_Content = "";
        [SerializeField] private float m_Width = 0f;
        [SerializeField] private LabelStyle m_DataLabelStyle = new LabelStyle() { show = false };
        [SerializeField] private LabelStyle m_HeaderLabelStyle = new LabelStyle() { show = false };
        /// <summary>
        /// whether the column is visible.
        /// ||是否显示列表头。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the content of the column header.
        /// ||列表头内容。
        /// </summary>
        public string content
        {
            get { return m_Content; }
            set { if (PropertyUtil.SetClass(ref m_Content, value)) SetComponentDirty(); }
        }
        /// <summary>
        /// the width of the column. when width is 0, the width is average.
        /// ||列宽。默认为0时列宽平分。
        /// </summary>
        public float width
        {
            get { return m_Width; }
            set { if (PropertyUtil.SetStruct(ref m_Width, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the style of the column header. default not enabled, but use the defaultHeaderLabelStyle of TableColumnHeaderStyle.
        /// ||当前列的数据文本样式。默认不启用，而是共用TableColumnHeaderStyle的defaultDataLabelStyle。
        /// </summary>
        public LabelStyle dataLabelStyle
        {
            get { return m_DataLabelStyle; }
            set { if (PropertyUtil.SetClass(ref m_DataLabelStyle, value)) SetComponentDirty(); }
        }
        /// <summary>
        /// the style of the column header. default not enabled, but use the defaultHeaderLabelStyle of TableColumnHeaderStyle.
        /// ||当前列的列表头文本样式。默认不启用，而是共用TableColumnHeaderStyle的defalutHeaderLabelStyle。
        /// </summary>
        public LabelStyle headerLabelStyle
        {
            get { return m_HeaderLabelStyle; }
            set { if (PropertyUtil.SetClass(ref m_HeaderLabelStyle, value)) SetComponentDirty(); }
        }
    }
}