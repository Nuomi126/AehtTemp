using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Graphs
{
    [HelpURL("https://xcharts-team.github.io/docs/graph")]
    public class GraphChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField] [ListForSerie(typeof(Graph))] private List<Graph> m_SerieGraphs = new List<Graph>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            m_Settings.lineSmoothness = 5f;
            var tooltip = EnsureChartComponent<Tooltip>();
            tooltip.titleFormatter = "-";
            Graph.AddDefaultSerie(this, GenerateDefaultSerieName());
        }
    }
}