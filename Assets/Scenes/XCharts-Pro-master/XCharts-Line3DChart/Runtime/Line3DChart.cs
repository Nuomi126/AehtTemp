using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Line3Ds
{
    [HelpURL("https://xcharts-team.github.io/docs/line3d")]
    public class Line3DChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForComponent(typeof(GridCoord3D))] private List<GridCoord3D> m_GridCoord3Ds = new List<GridCoord3D>();
        [SerializeField][ListForComponent(typeof(XAxis3D))] private List<XAxis3D> m_XAxis3Ds = new List<XAxis3D>();
        [SerializeField][ListForComponent(typeof(YAxis3D))] private List<YAxis3D> m_YAxis3Ds = new List<YAxis3D>();
        [SerializeField][ListForComponent(typeof(ZAxis3D))] private List<ZAxis3D> m_ZAxis3Ds = new List<ZAxis3D>();
        [SerializeField][ListForSerie(typeof(Line3D))] private List<Line3D> m_SerieLine3Ds = new List<Line3D>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            RemoveData();
            EnsureChartComponent<GridCoord3D>();
            var xAxis = EnsureChartComponent<XAxis3D>();
            xAxis.type = Axis.AxisType.Value;
            xAxis.splitLine.show = true;

            var yAxis = EnsureChartComponent<YAxis3D>();
            yAxis.position = Axis.AxisPosition.Right;

            EnsureChartComponent<YAxis3D>();
            EnsureChartComponent<ZAxis3D>();

            Line3D.AddDefaultSerie(this, GenerateDefaultSerieName());
            for (int i = 0; i < 5; i++)
            {
                AddXAxisData("x" + (i + 1));
            }
        }
    }
}