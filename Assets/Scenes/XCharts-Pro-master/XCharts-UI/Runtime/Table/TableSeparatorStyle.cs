using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// the style of the table separator.
    /// ||表格分割线样式。
    /// </summary>
    [Serializable]
    public class TableSeparatorStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private bool m_Vertical = true;
        [SerializeField] private bool m_VerticalColumn = true;
        [SerializeField] private float m_VerticalWidth = 1f;
        [SerializeField] private Color32 m_VerticalColor = Color.clear;
        [SerializeField] private bool m_Horizontal = true;
        [SerializeField] private bool m_HorizontalColumn = true;
        [SerializeField] private float m_HorizontalWidth = 1f;
        [SerializeField] private Color32 m_HorizontalColor = Color.clear;

        /// <summary>
        /// whether to show the separator.
        /// ||是否显示分割线。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the vertical separator.
        /// ||是否显示垂直分割线。
        /// </summary>
        public bool vertical
        {
            get { return m_Vertical; }
            set { if (PropertyUtil.SetStruct(ref m_Vertical, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the vertical separator of the header.
        /// ||是否显示列表头的垂直分割线。
        /// </summary>
        public bool verticalColumn
        {
            get { return m_VerticalColumn; }
            set { if (PropertyUtil.SetStruct(ref m_VerticalColumn, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the width of vertical separator.
        /// ||垂直分割线的宽度。
        /// </summary>
        public float verticalWidth
        {
            get { return m_VerticalWidth; }
            set { if (PropertyUtil.SetStruct(ref m_VerticalWidth, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the color of vertical separator.
        /// ||垂直分割线的颜色。
        /// </summary>
        public Color32 verticalColor
        {
            get { return m_VerticalColor; }
            set { if (PropertyUtil.SetColor(ref m_VerticalColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the horizontal separator.
        /// ||是否显示水平分割线。
        /// </summary>
        public bool horizontal
        {
            get { return m_Horizontal; }
            set { if (PropertyUtil.SetStruct(ref m_Horizontal, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the horizontal separator of the header.
        /// ||是否显示列表头的水平分割线。
        /// </summary>
        public bool horizontalColumn
        {
            get { return m_HorizontalColumn; }
            set { if (PropertyUtil.SetStruct(ref m_HorizontalColumn, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the width of horizontal separator.
        /// ||水平分割线的宽度。 
        /// </summary>
        public float horizontalWidth
        {
            get { return m_HorizontalWidth; }
            set { if (PropertyUtil.SetStruct(ref m_HorizontalWidth, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the color of horizontal separator.
        /// ||水平分割线的颜色。
        /// </summary>
        public Color32 horizontalColor
        {
            get { return m_HorizontalColor; }
            set { if (PropertyUtil.SetColor(ref m_HorizontalColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to need to draw the horizontal separator.
        /// ||是否需要绘制水平分割线。
        /// </summary>
        public bool IsNeedDrawHorizontalSeparator()
        {
            return show && horizontal && horizontalWidth > 0;
        }

        /// <summary>
        /// whether to need to draw the vertical separator.
        /// ||是否需要绘制垂直分割线。
        /// </summary>
        public bool IsNeedDrawVerticalSeparator()
        {
            return show && vertical && verticalWidth > 0;
        }
    }
}