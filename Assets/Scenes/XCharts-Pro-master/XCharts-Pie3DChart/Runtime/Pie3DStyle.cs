using UnityEngine;

namespace XCharts.Runtime.Pie3Ds
{
    /// <summary>
    /// Pie 3d style.
    /// ||3D饼图样式。
    /// </summary>
    [System.Serializable]
    public class Pie3DStyle : ChildComponent
    {
        [SerializeField] protected bool m_Show = true;
        [SerializeField] protected float m_EllipseMinorAxis = 0.65f;
        [SerializeField] protected float m_PieMaxHeight = 20f;
        [SerializeField] protected bool m_PieAutoHeight = false;
        [SerializeField] protected Vector2 m_SelectedOffset = new Vector2(0.1f, 0.5f);
        [SerializeField] [Range(0, 1)] protected float m_PeripherySideColorRate = 0.8f;
        [SerializeField] [Range(0, 1)] protected float m_StartSideColorRate = 0.7f;
        [SerializeField] [Range(0, 1)] protected float m_EndSideColorRate = 0.65f;
        [SerializeField] [Since("v3.10.0")] protected bool m_DrawSplitSide = true;

        public bool show
        {
            get { return m_Show; }
            set { m_Show = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The minor axis length of ellipse.
        /// ||椭圆的短轴长度。当为0-1的浮点数时为长轴的百分比。
        /// </summary>
        public float ellipseMinorAxis { get { return m_EllipseMinorAxis; } set { m_EllipseMinorAxis = value; SetVerticesDirty(); } }
        /// <summary>
        /// The max height of 3d pie.
        /// ||饼图厚度。
        /// </summary>
        public float pieMaxHeight { get { return m_PieMaxHeight; } set { m_PieMaxHeight = value; SetVerticesDirty(); } }
        /// <summary>
        /// Whether auto set 3d pie height by data rate.
        /// ||是否自动根据比例计算厚度。
        /// </summary>
        public bool pieAutoHeight { get { return m_PieAutoHeight; } set { m_PieAutoHeight = value; SetVerticesDirty(); } }
        /// <summary>
        /// The periphery side color rate of 3d pie. 
        /// ||饼图切块的侧面颜色。
        /// </summary>
        public float peripherySideColorRate { get { return m_PeripherySideColorRate; } set { m_PeripherySideColorRate = value; SetVerticesDirty(); } }
        /// <summary>
        /// The start side color rate of 3d pie.
        /// ||饼图切块的起始面颜色（顺时针）。
        /// </summary>
        public float startSideColorRate { get { return m_StartSideColorRate; } set { m_StartSideColorRate = value; SetVerticesDirty(); } }
        /// <summary>
        /// The end side color rate of 3d pie.
        /// ||饼图切块的结束面颜色（顺时针）。
        /// </summary>
        public float endSideColorRate { get { return m_EndSideColorRate; } set { m_EndSideColorRate = value; SetVerticesDirty(); } }
        /// <summary>
        /// The offset of selected pie slice.
        /// ||被选中状态的饼图块的额外偏移。
        /// </summary>
        public Vector2 selectedOffset { get { return m_SelectedOffset; } set { m_SelectedOffset = value; SetVerticesDirty(); } }
        /// <summary>
        /// whether draw split side. When pieAutoHeight is true, this value is invalid.
        /// ||是否绘制分割面。当pieAutoHeight为true时，该值无效。
        /// </summary>
        public bool drawSplitSide { get { return m_DrawSplitSide; } set { m_DrawSplitSide = value; SetVerticesDirty(); } }
    }
}