using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// Style of fill.
    /// ||填充样式。
    /// </summary>
    [Serializable]
    public class FillStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private Color32 m_Color = new Color32(145, 202, 255, 255);
        [SerializeField] private Color32 m_ToColor = new Color32(0, 0, 0, 0);
        [SerializeField] private float m_Width;
        [SerializeField] private float m_Height = 5f;
        [SerializeField] private float m_Radius = 0f;
        [SerializeField] private bool m_RoundCap = true;
        [SerializeField] private bool m_AutoOffset = true;
        [SerializeField] private Location m_Location = new Location() { align = Location.Align.Center };
        [SerializeField] private Background m_Background = new Background();

        /// <summary>
        /// Whether to show fill.
        /// ||是否显示填充。
        /// </summary>
        public bool show { get { return m_Show; } set { m_Show = value; SetVerticesDirty(); } }
        /// <summary>
        /// Fill color.
        /// ||填充颜色。
        /// </summary>
        public Color32 color { get { return m_Color; } set { m_Color = value; SetVerticesDirty(); } }
        /// <summary>
        /// To color.
        /// ||渐变结束颜色。
        /// </summary>
        public Color32 toColor { get { return m_ToColor; } set { m_ToColor = value; SetVerticesDirty(); } }
        /// <summary>
        /// Width of fill.
        /// ||填充宽度。优先级比left和right高。
        /// </summary> 
        public float width { get { return m_Width; } set { m_Width = value; SetVerticesDirty(); } }
        /// <summary>
        /// Height of fill. Higher priority than top and bottom. Represents the width of the ring in the circular progress bar.
        /// ||填充高度。优先级比top和bottom高。在环形进度条中表示环的宽度。
        /// </summary> 
        public float height { get { return m_Height; } set { m_Height = value; SetVerticesDirty(); } }
        /// <summary>
        /// Fill radius. Only valid in circular progress bar.
        /// ||填充半径。只在环形进度条中有效。
        /// </summary>
        public float radius { get { return m_Radius; } set { m_Radius = value; SetVerticesDirty(); } }
        /// <summary>
        /// Whether to use round cap. Only valid in circular progress bar.
        /// ||是否使用圆角。只在环形进度条中有效。
        /// </summary> 
        public bool roundCap { get { return m_RoundCap; } set { m_RoundCap = value; SetVerticesDirty(); } }
        /// <summary>
        /// Whether to automatically offset the fill position. When true, it will automatically indent left and right according to the displayed text or slider size.
        /// ||是否自动偏移填充位置。当为true时，会自动根据显示的文本或滑块大小自动进行左右缩进。
        /// </summary>
        public bool autoOffset { get { return m_AutoOffset; } set { m_AutoOffset = value; SetVerticesDirty(); } }
        /// <summary>
        /// Location of fill.
        /// ||填充位置。
        /// </summary> 
        public Location location { get { return m_Location; } set { m_Location = value; SetVerticesDirty(); } }
        /// <summary>
        /// Background style.
        /// ||背景样式。
        /// </summary>
        public Background background { get { return m_Background; } set { m_Background = value; SetVerticesDirty(); } }
    }
}