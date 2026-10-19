using UnityEngine;

namespace XCharts.Runtime.Bar3Ds
{
    /// <summary>
    /// The type of Bar3D.
    /// ||3D柱图类型。分为立方柱和圆柱。
    /// </summary>
    public enum Bar3DType
    {
        /// <summary>
        /// Cube.
        /// ||立方柱。
        /// </summary>
        Cube,
        /// <summary>
        /// Cylinder.
        /// ||圆柱。
        /// </summary>
        Cylinder
    }

    /// <summary>
    /// Bar3D.
    /// ||3D柱图。支持立方柱和圆柱。可水平排列和垂直排列。
    /// </summary>
    [System.Serializable]
    [SerieHandler(typeof(Bar3DHandler), true)]
    [RequireComponent(typeof(Bar3DStyle))]
    [DefaultAnimation(AnimationType.BottomToTop)]
    [DefaultTooltip(Tooltip.Type.Shadow, Tooltip.Trigger.Axis)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Ignore")]
    public class Bar3D : Serie, INeedSerieContainer
    {
        [SerializeField][Since("v3.1.0")] private Bar3DType m_Bar3DType = new Bar3DType();
        [SerializeField] private Bar3DStyle m_Bar3DStyle = new Bar3DStyle();

        public int containerIndex { get; internal set; }
        public int containterInstanceId { get; internal set; }

        /// <summary>
        /// The type of Bar3D.
        /// ||3D柱图的类型。
        /// </summary>
        public Bar3DType bar3dType
        {
            get { return m_Bar3DType; }
            set { if (PropertyUtil.SetStruct(ref m_Bar3DType, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// The style of Bar3D.
        /// ||3D柱图的样式。
        /// </summary>
        public Bar3DStyle bar3dStyle
        {
            get { return m_Bar3DStyle; }
            set { if (PropertyUtil.SetClass(ref m_Bar3DStyle, value, true)) SetVerticesDirty(); }
        }

        public static Bar3D AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Bar3D>(serieName);
            for (int i = 0; i < 5; i++)
            {
                chart.AddData(serie.index, UnityEngine.Random.Range(10, 90));
            }
            return serie;
        }

        public static Bar3D ConvertSerie(Serie serie)
        {
            var newSerie = SerieHelper.CloneSerie<Bar3D>(serie);
            return newSerie;
        }
    }
}