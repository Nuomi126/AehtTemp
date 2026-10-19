using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Bar3Ds
{
    [HelpURL("https://xcharts-team.github.io/docs/bar3d")]
    public class Bar3DChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForSerie(typeof(Bar3D))] private List<Bar3D> m_SerieBar3Ds = new List<Bar3D>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            EnsureChartComponent<GridCoord>();
            EnsureChartComponent<XAxis>();
            EnsureChartComponent<YAxis>();

            RemoveData();
            var serie = Bar3D.AddDefaultSerie(this, GenerateDefaultSerieName());
            for (int i = 0; i < serie.dataCount; i++)
            {
                AddXAxisData("x" + i);
            }
        }

        /// <summary>
        /// default grouped 3d column chart.
        /// || 默认分组3D柱状图。
        /// </summary>
        public void DefaultGroupedCubeChart()
        {
            CheckChartInit();
            Bar3D.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// default stacked 3d column chart.
        /// || 默认堆叠分组3D柱状图。
        /// </summary>
        public void DefaultStackedCubeChart()
        {
            CheckChartInit();
            var serie1 = GetSerie(0);
            serie1.stack = "stack1";
            var serie2 = Bar3D.AddDefaultSerie(this, GenerateDefaultSerieName());
            serie2.stack = "stack1";
        }

        /// <summary>
        /// generate default cylinder chart.
        /// || 生成默认的圆柱体图。
        /// </summary>
        public void DefaultCylinderChart()
        {
            CheckChartInit();
            var serie = GetSerie<Bar3D>(0);
            serie.bar3dType = Bar3DType.Cylinder;
        }
    }
}