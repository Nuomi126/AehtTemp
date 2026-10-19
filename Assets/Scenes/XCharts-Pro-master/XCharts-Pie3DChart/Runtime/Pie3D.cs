using UnityEngine;

namespace XCharts.Runtime.Pie3Ds
{
    /// <summary>
    /// serie of 3D pie.
    /// ||3D饼图系列。
    /// </summary>
    [System.Serializable]
    [SerieConvert(typeof(Line), typeof(Bar), typeof(Pie))]
    [SerieHandler(typeof(Pie3DHandler), true)]
    [DefaultAnimation(AnimationType.BottomToTop)]
    [SerieComponent(typeof(LabelStyle), typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle),typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Ignore", "m_Selected", "m_Radius")]
    public class Pie3D : Serie
    {
        [SerializeField] private Pie3DStyle m_Pie3DStyle = new Pie3DStyle();

        /// <summary>
        /// Pie 3d style. 
        /// ||3D相关的样式。
        /// </summary>
        public Pie3DStyle pie3dStyle
        {
            get { return m_Pie3DStyle; }
            set { if (PropertyUtil.SetClass(ref m_Pie3DStyle, value, true)) SetVerticesDirty(); }
        }

        public override SerieColorBy defaultColorBy { get { return SerieColorBy.Data; } }

        public static Serie AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Pie3D>(serieName);
            serie.center[1] = 0.4f;
            serie.radius[1] = 0.3f;

            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.position = LabelStyle.Position.Middle;
            label.autoOffset = true;
            label.offset = new Vector3(10, 0, 0);

            var labelLine = serie.EnsureComponent<LabelLine>();
            labelLine.lineAngle = 60;
            labelLine.lineLength1 = 0.6f;
            labelLine.lineLength2 = 1f;

            chart.AddData(serie.index, 10, "pie1");
            chart.AddData(serie.index, 25, "pie2");
            chart.AddData(serie.index, 65, "pie3");
            return serie;
        }

        public static Pie3D ConvertSerie(Serie serie)
        {
            var newSerie = SerieHelper.CloneSerie<Pie3D>(serie);
            return newSerie;
        }
    }
}