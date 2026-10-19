using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Pyramids
{
    [System.Serializable]
    [SerieHandler(typeof(PyramidHandler), true)]
    [RequireComponent(typeof(PyramidStyle))]
    [SerieComponent(typeof(LabelStyle), typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(LabelLine), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField()]
    public class Pyramid : Serie
    {
        [SerializeField] private PyramidStyle m_PyramidStyle = new PyramidStyle();

        public PyramidStyle pyramidStyle
        {
            get { return m_PyramidStyle; }
            set { if (PropertyUtil.SetClass(ref m_PyramidStyle, value, true)) SetVerticesDirty(); }
        }

        public override SerieColorBy defaultColorBy { get { return SerieColorBy.Data; } }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Pyramid>(serieName);

            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.position = LabelStyle.Position.Outside;

            var labelLine = serie.EnsureComponent<LabelLine>();
            labelLine.show = true;
            labelLine.lineType = LabelLine.LineType.HorizontalLine;
            labelLine.lineLength1 = 10;
            labelLine.lineLength2 = 100;

            serie.left = 0.2f;
            serie.right = 0.35f;
            serie.top = 0.25f;
            serie.bottom = 0.1f;

            for (int i = 0; i < 5; i++)
            {
                chart.AddData(serie.index, UnityEngine.Random.Range(10, 90), "data" + i);
            }
        }
    }
}