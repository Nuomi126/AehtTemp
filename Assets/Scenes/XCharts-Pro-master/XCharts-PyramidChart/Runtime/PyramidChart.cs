using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Pyramids
{
    [HelpURL("https://xcharts-team.github.io/docs/pyramid")]
    public class PyramidChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForSerie(typeof(Pyramid))] private List<Pyramid> m_SeriePyramids = new List<Pyramid>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            Pyramid.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default 2d pyramid chart.
        /// ||生成默认的2D金字塔图。
        /// </summary>
        public void Default2DPyramidChart()
        {
            CheckChartInit();
            var serie = GetSerie<Pyramid>(0);
            serie.pyramidStyle.draw3d = false;
        }
    }
}