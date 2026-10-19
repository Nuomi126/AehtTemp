using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// the style of the table carousel.
    /// ||表格滚动条样式。
    /// </summary>
    [Serializable]
    public class TableScrollbarStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = false;
        [SerializeField] private bool m_Horizontal = false;
        [SerializeField] private bool m_Vertical = true;
        [SerializeField] private float m_Width = 10;
        [SerializeField] private Color32 m_BackgroundColor;
        [SerializeField] private Color32 m_Color;
        [SerializeField][Since("v3.10.0")] private BorderStyle m_BorderStyle = new BorderStyle();

        /// <summary>
        /// whether the scrollbar is enable.
        /// ||是否开启滚动条。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the width of the scrollbar.
        /// ||滚动条宽度。
        /// </summary>
        public float width
        {
            get { return m_Width; }
            set { if (PropertyUtil.SetStruct(ref m_Width, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the horizontal scrollbar. 
        /// ||是否显示水平滚动条。
        /// </summary>
        public bool horizontal
        {
            get { return m_Horizontal; }
            set { if (PropertyUtil.SetStruct(ref m_Horizontal, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether to show the vertical scrollbar.
        /// ||是否显示垂直滚动条。
        /// </summary>
        public bool vertical
        {
            get { return m_Vertical; }
            set { if (PropertyUtil.SetStruct(ref m_Vertical, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the background color of the scrollbar.
        /// ||滚动条背景色。
        /// </summary>
        public Color32 backgroundColor
        {
            get { return m_BackgroundColor; }
            set { if (PropertyUtil.SetStruct(ref m_BackgroundColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the color of the scrollbar.
        /// ||滚动条颜色。
        /// </summary>
        public Color32 color
        {
            get { return m_Color; }
            set { if (PropertyUtil.SetStruct(ref m_Color, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the border style of the scrollbar.
        /// ||滚动条的边框样式。
        /// </summary>
        public BorderStyle borderStyle
        {
            get { return m_BorderStyle; }
            set { if (PropertyUtil.SetClass(ref m_BorderStyle, value)) SetVerticesDirty(); }
        }
    }
}