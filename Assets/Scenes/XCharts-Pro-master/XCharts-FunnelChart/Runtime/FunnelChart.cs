

using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Funnels
{
    /// <summary>
    /// Funnel chart. Suitable for flow analysis.
    /// || 漏斗图。适用于流程流量分析。
    /// </summary>
    [AddComponentMenu("XCharts/FunnelChart", 22)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [HelpURL("https://xcharts-team.github.io/docs/funnel")]
    public class FunnelChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForSerie(typeof(Funnel))] private List<Funnel> m_SerieFunnels = new List<Funnel>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            Funnel.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default horizontal funnel chart.
        /// || 生成默认的横向漏斗图。
        /// </summary>
        public void DefaultHorizonalFunnelChart()
        {
            CheckChartInit();
            var serie = GetSerie<Funnel>(0);
            serie.orient = Orient.Horizonal;
        }
    }
}
