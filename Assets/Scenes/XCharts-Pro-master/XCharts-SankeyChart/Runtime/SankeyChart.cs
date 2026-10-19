using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Sankeys
{
    [HelpURL("https://xcharts-team.github.io/docs/sankey")]
    public class SankeyChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField] [ListForSerie(typeof(Sankey))] private List<Sankey> m_SerieSankeys = new List<Sankey>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            m_Settings.lineSmoothness = 5f;
            var tooltip = EnsureChartComponent<Tooltip>();
            tooltip.titleFormatter = "-";
            Sankey.AddDefaultSerie(this, GenerateDefaultSerieName());
        }
    }
}