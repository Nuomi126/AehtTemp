using UnityEngine;

namespace XCharts.Runtime.Gauges
{
    /// <summary>
    /// serie of gauge chart.
    /// ||仪表盘系列数据。
    /// </summary>
    [System.Serializable]
    [SerieHandler(typeof(GaugeHandler), true)]
    [DefaultAnimation(AnimationType.BottomToTop)]
    [SerieComponent(typeof(LabelStyle), typeof(TitleStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(TitleStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Selected", "m_Radius")]
    public class Gauge : Serie
    {
        [SerializeField][Since("v3.6.0")] private bool m_RangeDataMode = false;
        [SerializeField][Since("v3.6.0")] private bool m_Loop = false;
        [SerializeField] private GaugeAxis m_GaugeAxis = new GaugeAxis();
        [SerializeField] private GaugePointer m_GaugePointer = new GaugePointer();
        [SerializeField] private GaugeAnchor m_GaugeAnchor = new GaugeAnchor();

        /// <summary>
        /// Whether the value is range data. When this mode is enabled, each value in data is an array, the first value of the array is the starting value, and the second value is the ending value. The pointer has only one, and the data is drawn as a ring.
        /// ||数值是否为范围数据。启用该模式后，data中的每个值都是一个数组，数组中的第一个值为起始值，第二个值为结束值。指针只有一个，数据绘制为环形。
        /// </summary>
        public bool rangeDataMode
        {
            get { return m_RangeDataMode; }
            set { if (PropertyUtil.SetStruct(ref m_RangeDataMode, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the value is loop.
        /// ||数值是否循环。可用在钟表图。
        /// </summary>
        public bool loop
        {
            get { return m_Loop; }
            set { if (PropertyUtil.SetStruct(ref m_Loop, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// 仪表盘轴线。
        /// </summary>
        public GaugeAxis gaugeAxis
        {
            get { return m_GaugeAxis; }
            set { if (PropertyUtil.SetClass(ref m_GaugeAxis, value, true)) SetVerticesDirty(); }
        }
        /// <summary>
        /// 仪表盘指针。
        /// </summary>
        public GaugePointer gaugePointer
        {
            get { return m_GaugePointer; }
            set { if (PropertyUtil.SetClass(ref m_GaugePointer, value, true)) SetVerticesDirty(); }
        }
        /// <summary>
        /// 仪表盘指针固定点。
        /// </summary>
        public GaugeAnchor gaugeAnchor
        {
            get { return m_GaugeAnchor; }
            set { if (PropertyUtil.SetClass(ref m_GaugeAnchor, value, true)) SetVerticesDirty(); }
        }
        public override SerieColorBy defaultColorBy { get { return SerieColorBy.Data; } }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Gauge>(serieName);
            serie.min = 0;
            serie.max = 100;
            serie.startAngle = -135;
            serie.endAngle = 135;
            serie.center[0] = 0.5f;
            serie.center[1] = 0.4f;
            serie.radius[0] = 0.4f;
            serie.splitNumber = 5;

            serie.EnsureComponent<LabelStyle>();
            serie.label.show = true;
            serie.label.position = LabelStyle.Position.Center;
            serie.label.offset = new Vector3(0, -0.45f);
            serie.label.textStyle.autoColor = true;
            serie.label.textStyle.fontSize = 24;

            serie.EnsureComponent<TitleStyle>();
            serie.titleStyle.show = true;
            serie.titleStyle.textStyle.fontSize = 22;
            serie.titleStyle.offset = new Vector3(0, -0.24f, 0);

            serie.itemStyle.show = true;
            serie.gaugeAxis.axisLabel.show = true;
            serie.gaugeAxis.axisLabel.distance = 40;
            chart.AddData(serie.index, UnityEngine.Random.Range(10, 90), "title");
        }

        public override bool vertsDirty
        {
            get
            {
                return base.vertsDirty ||
                    gaugeAxis.vertsDirty ||
                    gaugePointer.vertsDirty;
            }
        }

        public override void ClearVerticesDirty()
        {
            base.ClearVerticesDirty();
            gaugeAxis.ClearVerticesDirty();
            gaugePointer.ClearVerticesDirty();
        }

        public override void ClearComponentDirty()
        {
            base.ClearComponentDirty();
            gaugeAxis.ClearComponentDirty();
            gaugePointer.ClearComponentDirty();
        }

        public Color32 GetPointerColor(ThemeStyle theme, int colorIndex, float angle, Color32 dataColor)
        {
            if (m_GaugeAxis.type == GaugeAxisType.Section && m_GaugePointer.autoColor)
            {
                return m_GaugeAxis.GetStageColorByAngle(angle);
            }
            if (!ChartHelper.IsClearColor(m_GaugePointer.color))
                return m_GaugePointer.color;
            return dataColor;
        }
    }
}