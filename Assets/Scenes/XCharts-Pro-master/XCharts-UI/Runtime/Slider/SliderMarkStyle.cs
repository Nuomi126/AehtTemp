using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// Style of mark.
    /// ||标记样式。
    /// </summary>
    [Serializable]
    public class SliderMarkStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private bool m_Included = true;
        [SerializeField] private LabelStyle m_Label = new LabelStyle();
        [SerializeField] private SymbolStyle m_Symbol = new SymbolStyle();

        /// <summary>
        /// Whether to show mark.
        /// ||是否显示标记。
        /// </summary>
        public bool show { get { return m_Show; } set { m_Show = value; SetAllDirty(); } }
        /// <summary>
        /// Whether to include in the range. When true, the mark is displayed as the progress bar color, otherwise it is displayed as the background color.
        /// ||是否包含在范围内。为true时，标记显示为进度条颜色，否则显示为背景颜色。
        /// </summary>
        public bool included { get { return m_Included; } set { m_Included = value; SetAllDirty(); } }
        /// <summary>
        /// Style of symbol.
        /// ||图形样式。
        /// </summary> 
        public SymbolStyle symbol { get { return m_Symbol; } set { m_Symbol = value; SetVerticesDirty(); } }
        /// <summary>
        /// Style of label.
        /// ||文本样式。
        /// </summary>
        public LabelStyle label { get { return m_Label; } set { m_Label = value; SetAllDirty(); } }
    }
}