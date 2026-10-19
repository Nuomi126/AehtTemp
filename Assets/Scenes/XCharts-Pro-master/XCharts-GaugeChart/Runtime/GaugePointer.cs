using UnityEngine;

namespace XCharts.Runtime.Gauges
{
    /// <summary>
    /// Settings related to gauge pointer.
    /// ||仪表盘指针相关设置。
    /// </summary>
    [System.Serializable]
    public class GaugePointer : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private float m_Length = 0.6f;
        [SerializeField] private float m_BackLength = 0.05f;
        [SerializeField] private float m_BackExtraLength = 0.05f;
        [SerializeField] private float m_TopLeftLength = 0f;
        [SerializeField] private float m_TopRightLength = 0f;
        [SerializeField] private float m_BottomLeftLength = 0.025f;
        [SerializeField] private float m_BottomRightLength = 0.025f;
        [SerializeField] private float m_Distance;
        [SerializeField] private bool m_RoundCap;
        [SerializeField] private bool m_AutoColor = true;
        [SerializeField] private Color32 m_Color;
        [SerializeField] private Color32 m_ToColor;
        [SerializeField] private float[] m_CornerRadius = new float[] { 0, 0, 0, 0 };
        [SerializeField][Since("v3.6.0")] private double m_Value;

        /// <summary>
        /// Whether to display a pointer.
        /// ||是否显示指针。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float length
        {
            get { return m_Length; }
            set { if (PropertyUtil.SetStruct(ref m_Length, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer back length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针后凸出的长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float backLength
        {
            get { return m_BackLength; }
            set { if (PropertyUtil.SetStruct(ref m_BackLength, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer back extra length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针后额外的长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float backExtraLength
        {
            get { return m_BackExtraLength; }
            set { if (PropertyUtil.SetStruct(ref m_BackExtraLength, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针顶部左边凸起长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float topLeftLength
        {
            get { return m_TopLeftLength; }
            set { if (PropertyUtil.SetStruct(ref m_TopLeftLength, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针顶部右边凸起长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float topRightLength
        {
            get { return m_TopRightLength; }
            set { if (PropertyUtil.SetStruct(ref m_TopRightLength, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针底部左边凸起长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float bottomLeftLength
        {
            get { return m_BottomLeftLength; }
            set { if (PropertyUtil.SetStruct(ref m_BottomLeftLength, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Pointer length. It can be an absolute value, or it can be a percentage relative to the radius (0-1).
        /// ||指针底部右边凸起长度。可以是绝对值，也可以是相对于半径的百分比（0-1的浮点数）。
        /// </summary>
        public float bottomRightLength
        {
            get { return m_BottomRightLength; }
            set { if (PropertyUtil.SetStruct(ref m_BottomRightLength, value)) SetVerticesDirty(); }
        }
        public bool autoColor
        {
            get { return m_AutoColor; }
            set { if (PropertyUtil.SetStruct(ref m_AutoColor, value)) SetVerticesDirty(); }
        }
        public bool roundCap
        {
            get { return m_RoundCap; }
            set { if (PropertyUtil.SetStruct(ref m_RoundCap, value)) SetVerticesDirty(); }
        }
        public Color32 color
        {
            get { return m_Color; }
            set { if (PropertyUtil.SetStruct(ref m_Color, value)) SetVerticesDirty(); }
        }
        public Color32 toColor
        {
            get { return m_ToColor; }
            set { if (PropertyUtil.SetStruct(ref m_ToColor, value)) SetVerticesDirty(); }
        }
        public float distance
        {
            get { return m_Distance; }
            set { if (PropertyUtil.SetStruct(ref m_Distance, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// The radius of rounded corner. Its unit is px. Use array to respectively specify the 4 corner radiuses((clockwise upper left, upper right, bottom right and bottom left)).
        /// ||圆角半径。用数组分别指定4个圆角半径（顺时针左上，右上，右下，左下）。
        /// </summary>
        public float[] cornerRadius
        {
            get { return m_CornerRadius; }
            set { if (PropertyUtil.SetClass(ref m_CornerRadius, value, true)) SetVerticesDirty(); }
        }
        /// <summary>
        /// The value of pointer. When it is a clock gauge, the value can be specified to control the position of the pointer.
        /// ||指针的值。当为时钟仪表盘时，可通过指定value值来控制指针的位置。
        /// </summary>
        public double value
        {
            get { return m_Value; }
            set { if (PropertyUtil.SetStruct(ref m_Value, value)) SetVerticesDirty(); }
        }
    }
}