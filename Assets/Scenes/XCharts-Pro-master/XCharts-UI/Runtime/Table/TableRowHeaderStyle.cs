using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// the style of the table row header.
    /// ||行表头样式。
    /// </summary>
    [Serializable]
    public class TableRowHeaderStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private float m_Width = 100f;
        [SerializeField] private Color32 m_BackgroundColor;
        [SerializeField] private LabelStyle m_LabelStyle = new LabelStyle()
        {
            show = true,
            formatter = "{index}"
        };
        /// <summary>
        /// whether the row header is visible.
        /// ||是否显示行表头。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the width of the row header.
        /// ||宽度。
        /// </summary>
        public float width
        {
            get { return m_Width; }
            set { if (PropertyUtil.SetStruct(ref m_Width, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the background color of the row header.
        /// ||背景色。
        /// </summary>
        public Color32 backgroundColor
        {
            get { return m_BackgroundColor; }
            set { if (PropertyUtil.SetStruct(ref m_BackgroundColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the style of the row header label.
        /// ||文本样式。
        /// </summary>
        public LabelStyle labelStyle
        {
            get { return m_LabelStyle; }
            set { if (PropertyUtil.SetClass(ref m_LabelStyle, value)) SetComponentDirty(); }
        }
    }
}