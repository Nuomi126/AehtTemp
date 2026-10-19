using UnityEngine;

namespace XCharts.Runtime.Gantts
{
    [System.Serializable]
    [SerieHandler(typeof(GanttHandler), true)]
    [RequireComponent(typeof(GanttStyle))]
    [DefaultTooltip(Tooltip.Type.None, Tooltip.Trigger.Item)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField()]
    public class Gantt : Serie
    {
        [SerializeField] private bool m_DisableDrag;

        public bool disableDrag
        {
            get { return m_DisableDrag; }
            set { if (PropertyUtil.SetStruct(ref m_DisableDrag, value)) { SetVerticesDirty(); } }
        }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Gantt>(serieName);
            serie.showDataName = true;
            serie.showDataDimension = 2;
            var timestamp = DateTimeUtil.GetTimestamp();
            for (int i = 0; i < 5; i++)
            {
                var start = timestamp + Random.Range(1, 6) * 3600 * 24;
                var end = start + Random.Range(1, 10) * 3600 * 24;
                serie.AddXYData(start, end, "task-" + (i + 1));
            }
        }

        public static void AddDefaultValueSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Gantt>(serieName);
            serie.showDataName = true;
            serie.showDataDimension = 2;
            for (int i = 0; i < 5; i++)
            {
                var start = Random.Range(0, 6);
                var end = start + Random.Range(1, 10);
                serie.AddXYData(start, end, "task-" + (i + 1));
            }
        }

        public static void AddDefaultCategorySerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Gantt>(serieName);
            serie.showDataName = true;
            serie.showDataDimension = 2;
            for (int i = 0; i < 5; i++)
            {
                var start = Random.Range(0, 6);
                var end = start + Random.Range(1, 10 - start);
                serie.AddXYData(start, end, "task-" + (i + 1));
            }
        }
    }
}