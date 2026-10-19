using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.PictorialBars
{
    [HelpURL("https://xcharts-team.github.io/docs/pictorialbar")]
    public class PictorialBarChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField] [ListForSerie(typeof(PictorialBar))] private List<PictorialBar> m_SeriePictorialBars = new List<PictorialBar>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            EnsureChartComponent<GridCoord>();
            EnsureChartComponent<XAxis>();
            EnsureChartComponent<YAxis>();
            RemoveData();
            PictorialBar.AddDefaultSerie(this, GenerateDefaultSerieName());
            for (int i = 0; i < 5; i++)
            {
                AddXAxisData("x" + i);
            }
        }
    }
}