using UnityEngine;
using UnityEngine.Serialization;

namespace XCharts.Runtime.Bar3Ds
{
    [System.Serializable]
    public class Bar3DStyle : ChildComponent
    {
        [SerializeField] protected bool m_Show = true;
        [SerializeField] protected bool m_VerticalArrange = false;
        [SerializeField] protected Vector3 m_VerticalArrangeDire = new Vector3(-1, 1);
        [SerializeField] protected float m_VerticalArrangeGap = 0.5f;
        [SerializeField] protected Vector3 m_BarOffset = Vector3.zero;
        [SerializeField][Range(0, 2)] protected float m_WidthRate = 0.5f;
        [SerializeField][Range(0, 2)][FormerlySerializedAs("m_TopColorBurnRate")] protected float m_TopColorRate = 0.8f;
        [SerializeField][Range(0, 2)] protected float m_BottomColorRate = 1f;
        [SerializeField][Range(0, 2)][FormerlySerializedAs("m_SideColorBurnRate")] protected float m_SideColorRate = 0.7f;
        [SerializeField][FormerlySerializedAs("m_Dire")] protected Vector3 m_CubeDire = new Vector3(1, 1);
        [SerializeField][Range(0, 2)][Since("v3.1.0")] protected float m_CylinderRate = 0.5f;
        [SerializeField][Since("v3.6.0")] protected Color32 m_TopColor;
        [SerializeField][Since("v3.6.0")] protected Color32 m_BottomColor;
        [SerializeField][Since("v3.6.0")] protected Color32 m_SideColor;
        [SerializeField][Since("v3.10.0")] protected bool m_OccludedAreaBorder;
        [SerializeField][Since("v3.10.0")] protected bool m_OccludedAreaBorderDash;
        [SerializeField][Since("v3.10.0")] protected bool m_BorderDash;

        public bool show
        {
            get { return m_Show; }
            set { m_Show = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// Vertical arrange or not. 
        /// ||是否垂直排列。
        /// </summary>
        public bool verticalArrange
        {
            get { return m_VerticalArrange; }
            set { m_VerticalArrange = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The direction vector of vertical.
        /// ||垂直排列时的方向。
        /// </summary>
        public Vector3 verticalArrangeDire
        {
            get { return m_VerticalArrangeDire; }
            set { m_VerticalArrangeDire = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The gap of vertical arrange bar3d.
        /// ||垂直排列时两个柱子间的间隙。
        /// </summary>
        public float verticalArrangeGap
        {
            get { return m_VerticalArrangeGap; }
            set { m_VerticalArrangeGap = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The offset of bar.
        /// ||柱子偏移。
        /// </summary>
        public Vector3 barOffset
        {
            get { return m_BarOffset; }
            set { m_BarOffset = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The rate of side width.
        /// ||方形柱侧边宽度相对正面的比例。
        /// </summary>
        public float widthRate
        {
            get { return m_WidthRate; }
            set { m_WidthRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The color rate of top.
        /// ||柱子顶部相对正面的颜色比例。
        /// </summary>
        public float topColorRate
        {
            get { return m_TopColorRate; }
            set { m_TopColorRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The color rate of bottom.
        /// ||柱子底部相对正面的颜色比例。
        /// </summary>
        public float bottomColorRate
        {
            get { return m_BottomColorRate; }
            set { m_BottomColorRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The color rate of side.
        /// ||方形柱侧边相对正面的颜色比例。
        /// </summary>
        public float sideColorRate
        {
            get { return m_SideColorRate; }
            set { m_SideColorRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The direction vector of bar3d.
        /// ||方形柱形成3D效果的方向。
        /// </summary>
        public Vector3 cubeDire
        {
            get { return m_CubeDire; }
            set { m_CubeDire = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// The rate of top ellipse.
        /// ||圆柱的顶部椭圆比列。
        /// </summary>
        public float cylinderRate
        {
            get { return m_CylinderRate; }
            set { m_CylinderRate = value; SetVerticesDirty(); }
        }
        public Color32 topColor
        {
            get { return m_TopColor; }
            set { m_TopColor = value; SetVerticesDirty(); }
        }

        public Color32 bottomColor
        {
            get { return m_BottomColor; }
            set { m_BottomColor = value; SetVerticesDirty(); }
        }

        public Color32 sideColor
        {
            get { return m_SideColor; }
            set { m_SideColor = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// Whether to show the border of occluded area.
        /// ||是否显示遮挡区域的边框。
        /// </summary>
        public bool occludedAreaBorder
        {
            get { return m_OccludedAreaBorder; }
            set { m_OccludedAreaBorder = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// Whether to show dash border of occluded area.
        /// ||是否显示遮挡区域的虚线边框。
        /// </summary>
        public bool occludedAreaBorderDash
        {
            get { return m_OccludedAreaBorderDash; }
            set { m_OccludedAreaBorderDash = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// wherther to show dash border.
        /// ||是否显示虚线边框。 
        /// </summary>
        public bool borderDash
        {
            get { return m_BorderDash; }
            set { m_BorderDash = value; SetVerticesDirty(); }
        }
    }
}