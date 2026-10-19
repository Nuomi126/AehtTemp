using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// the style of the table column header.
    /// ||列表头样式。
    /// </summary>
    [Serializable]
    public class TableColumnHeaderStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private float m_Height = 50f;
        [SerializeField] private Color32 m_BackgroundColor;
        [SerializeField][Since("v3.10.0")] private bool m_Opaque = true;
        [SerializeField] private ImageStyle m_Background = new ImageStyle() { show = false };
        [SerializeField] private LabelStyle m_DefaultLabelStyle = new LabelStyle() { show = true };
        [SerializeField][Since("v3.10.0")] private BorderStyle m_BorderStyle = new BorderStyle();

        /// <summary>
        /// whether the column header is visible.
        /// ||是否显示列表头。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the height of the column header.
        /// ||列表头高度。
        /// </summary>
        public float height { get { return m_Height; } set { m_Height = value; } }
        /// <summary>
        /// the background color of the column header.
        /// ||列表头背景色。
        /// </summary>
        public Color32 backgroundColor { get { return m_BackgroundColor; } set { m_BackgroundColor = value; } }
        /// <summary>
        /// the background style of the column header. 
        /// ||背景图片样式。
        /// </summary>
        public ImageStyle background { get { return m_Background; } set { m_Background = value; } }
        /// <summary>
        /// the default style of the column header label.
        /// ||默认的列表头文本样式。
        /// </summary>
        public LabelStyle defalutLabelStyle
        {
            get { return m_DefaultLabelStyle; }
            set { if (PropertyUtil.SetClass(ref m_DefaultLabelStyle, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the border style of the column header.
        /// ||列表头边框样式。
        /// </summary>
        public BorderStyle borderStyle
        {
            get { return m_BorderStyle; }
            set { if (PropertyUtil.SetClass(ref m_BorderStyle, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// whether the column header is opaque.
        /// ||列表头是否不透明。
        /// </summary>
        public bool opaque { get { return m_Opaque; } set { m_Opaque = value; } }
    }
}