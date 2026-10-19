namespace XCharts.Runtime.PictorialBars
{
    [System.Serializable]
    [SerieHandler(typeof(PictorialBarHandler), true)]
    [DefaultAnimation(AnimationType.BottomToTop)]
    [DefaultTooltip(Tooltip.Type.None, Tooltip.Trigger.Axis)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle),typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Ignore")]
    public class PictorialBar : Serie, INeedSerieContainer
    {
        public int containerIndex { get; internal set; }
        public int containterInstanceId { get; internal set; }
        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<PictorialBar>(serieName);
            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Custom;
            for (int i = 0; i < 5; i++)
            {
                chart.AddData(serie.index, UnityEngine.Random.Range(10, 90), "data" + i);
            }
        }

        public static PictorialBar ConvertSerie(Serie serie)
        {
            var newSerie = SerieHelper.CloneSerie<PictorialBar>(serie);
            return newSerie;
        }
    }
}