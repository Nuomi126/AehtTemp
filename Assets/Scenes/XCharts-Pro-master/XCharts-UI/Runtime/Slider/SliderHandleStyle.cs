using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// Style of handle.
    /// ||滑块样式。
    /// </summary>
    [Serializable]
    public class SliderHandleStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private bool m_GradientColor = false;
        [SerializeField] private SymbolStyle m_Symbol = new SymbolStyle();

        /// <summary>
        /// Whether to show handle.
        /// ||是否显示滑块。
        /// </summary> 
        public bool show { get { return m_Show; } set { m_Show = value; SetAllDirty(); } }
        /// <summary>
        /// Whether to use gradient color.
        /// ||是否使用渐变颜色。滑动条设置渐变颜色时才生效。
        /// </summary>
        public bool gradientColor { get { return m_GradientColor; } set { m_GradientColor = value; SetVerticesDirty(); } }
        /// <summary>
        /// Style of symbol.
        /// ||图形样式。
        /// </summary> 
        public SymbolStyle symbol { get { return m_Symbol; } set { m_Symbol = value; SetVerticesDirty(); } }
    }
}