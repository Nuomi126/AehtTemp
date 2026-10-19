using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Treemaps
{
    [HelpURL("https://xcharts-team.github.io/docs/treemap")]
    public class TreemapChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField] [ListForSerie(typeof(Treemap))] private List<Treemap> m_SerieTreemaps = new List<Treemap>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            Treemap.AddDefaultSerie(this, GenerateDefaultSerieName());
        }
    }
}