using UnityEngine;

namespace XCharts.Runtime.Gantts
{
    public enum GanttType
    {
        /// <summary>
        /// 单项甘特图。每个数据项都是单独的一条进度。
        /// </summary>
        Monomial,
        /// <summary>
        /// 多项甘特图。每个数据项只是一条进度里的一段。
        /// </summary>
        Multinomial
    }

    [System.Serializable]
    [ComponentHandler(typeof(GanttStyleHandler), true)]
    public class GanttStyle : MainComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private GanttType m_GanttType = GanttType.Monomial;
        [SerializeField] private bool m_Invert = true;
        [SerializeField] private bool m_DragEnable = true;
        [SerializeField] private float m_DragSensitivity = 1f;
        [SerializeField] private double m_DragRangeMin = 0;
        [SerializeField] private double m_DragRangeMax = 0;

        [SerializeField] private bool m_AutoAdsorbAlignline = true;
        [SerializeField] private int m_AlignlineDataIndex = 1;
        [SerializeField] private float m_ShowAlignlineRange = 10f;
        [SerializeField] private LineStyle m_AliginLine = new LineStyle();
        [SerializeField] private AxisSplitArea m_SplitArea = AxisSplitArea.defaultSplitArea;

        public bool show
        {
            get { return m_Show; }
            set { m_Show = value; }
        }
        /// <summary>
        /// The type of gantt.
        /// ||甘特图类型。
        /// </summary>
        public GanttType ganttType
        {
            get { return m_GanttType; }
            set { m_GanttType = value; }
        }
        public bool invert
        {
            get { return m_Invert; }
            set { m_Invert = value; }
        }
        /// <summary>
        /// 是否可以拖拽甘特图。
        /// </summary>
        public bool dragEnable
        {
            get { return m_DragEnable; }
            set { m_DragEnable = value; }
        }
        /// <summary>
        /// 拖拽的灵敏度。
        /// </summary>
        public float dragSensitivity
        {
            get { return m_DragSensitivity; }
            set { m_DragSensitivity = value; }
        }
        /// <summary>
        /// 拖拽最小值限制。
        /// </summary>
        public double dragRangeMin
        {
            get { return m_DragRangeMin; }
            set { m_DragRangeMin = value; }
        }
        /// <summary>
        /// 拖拽最大值限制。
        /// </summary>
        public double dragRangeMax
        {
            get { return m_DragRangeMax; }
            set { m_DragRangeMax = value; }
        }
        /// <summary>
        /// 拖拽结束后是否自动吸附到对齐线。
        /// </summary>
        public bool autoAdsorbAlignline
        {
            get { return m_AutoAdsorbAlignline; }
            set { m_AutoAdsorbAlignline = value; }
        }
        /// <summary>
        /// 显示对齐线的数据项索引。
        /// </summary>
        public int alignlineDataIndex
        {
            get { return m_AlignlineDataIndex; }
            set { m_AlignlineDataIndex = value; }
        }
        /// <summary>
        /// 显示对齐线的范围。
        /// </summary>
        public float showAlignlineRange
        {
            get { return m_ShowAlignlineRange; }
            set { m_ShowAlignlineRange = value; }
        }
        /// <summary>
        /// 对齐线样式。
        /// </summary>
        public LineStyle aliginLine
        {
            get { return m_AliginLine; }
            set { m_AliginLine = value; }
        }
        public AxisSplitArea splitArea
        {
            get { return m_SplitArea; }
            set { m_SplitArea = value; }
        }
    }

    [UnityEngine.Scripting.Preserve]
    internal sealed class GanttStyleHandler : MainComponentHandler<GanttStyle>
    {
    }
}