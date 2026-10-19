using UnityEngine;

namespace XCharts.Runtime.Funnels
{
    [System.Serializable]
    [SerieHandler(typeof(FunnelHandler), true)]
    [SerieComponent(typeof(LabelStyle), typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Ignore")]
    public class Funnel : Serie
    {
        [SerializeField][Since("v3.3.0")] private bool m_RectEnd;
        [SerializeField][Since("v3.3.0")] private bool m_DynamicHeight;

        /// <summary>
        /// Whether it ends with a rectangle. The default false ends with a triangle.
        /// ||是否最后以矩形结束。默认false以三角形结束。
        /// </summary>
        public bool rectEnd
        {
            get { return m_RectEnd; }
            set { if (PropertyUtil.SetStruct(ref m_RectEnd, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// Whether the height is dynamic. When set to true, the height of each elemnet in funnel plot is directly proportional to the corresponding value of yField.
        /// ||是否映射为动态高度。当设置为 'true' 时，漏斗图每个条目（图表元素）的高度和 y 轴字段对应数值成正比。
        /// </summary>
        public bool dynamicHeight
        {
            get { return m_DynamicHeight; }
            set { if (PropertyUtil.SetStruct(ref m_DynamicHeight, value)) SetVerticesDirty(); }
        }

        public override SerieColorBy defaultColorBy { get { return SerieColorBy.Data; } }
        public override bool useSortData { get { return true; } }
        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Funnel>(serieName);
            serie.dataSortType = SerieDataSortType.Descending;
            serie.left = 0.2f;
            serie.right = 0.2f;
            serie.top = 0.25f;
            serie.bottom = 0.1f;
            for (int i = 0; i < 5; i++)
            {
                chart.AddData(serie.index, (i + 1) * 20, "data" + (i + 1));
            }
        }
    }
}