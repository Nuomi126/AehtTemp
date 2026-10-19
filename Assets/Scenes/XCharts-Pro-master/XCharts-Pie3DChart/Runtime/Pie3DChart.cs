using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Pie3Ds
{
    /// <summary>
    /// 3D pie.
    /// ||3D饼图。
    /// </summary>
    [HelpURL("https://xcharts-team.github.io/docs/pie3d")]
    public class Pie3DChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForSerie(typeof(Pie3D))] private List<Pie3D> m_SerieBar3Ds = new List<Pie3D>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            var legend = EnsureChartComponent<Legend>();
            legend.show = true;

            RemoveData();
            Pie3D.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default 3d donut pie chart.
        /// || 生成默认3D甜甜圈饼图。
        /// </summary>
        public void DefaultDonutPie3DChart()
        {
            CheckChartInit();
            var serie = GetSerie(0);
            serie.radius[0] = 0.20f;
        }

        /// <summary>
        /// generate default 2d ellipse pie chart.
        /// || 生成默认2D椭圆饼图。
        /// </summary>
        public void DefaultEllipsePie3DChart()
        {
            CheckChartInit();
            var serie = GetSerie<Pie3D>(0);
            serie.pie3dStyle.pieMaxHeight = 0;
        }

        /// <summary>
        /// generate default 3d auto height pie chart.
        /// || 生成默认3D自动高度饼图。
        /// </summary>
        public void DefaultAutoHeightPie3DChart()
        {
            CheckChartInit();
            var serie = GetSerie<Pie3D>(0);
            serie.pie3dStyle.pieAutoHeight = true;
        }
    }
}