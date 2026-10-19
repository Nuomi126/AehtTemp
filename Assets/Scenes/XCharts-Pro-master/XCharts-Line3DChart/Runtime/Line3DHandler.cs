using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using XUGL;

namespace XCharts.Runtime.Line3Ds
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class Line3DHandler : SerieHandler<Line3D>
    {
        List<List<SerieData>> m_StackSerieData = new List<List<SerieData>>();
        private GridCoord3D m_SerieGrid;

        public override void InitComponent()
        {
            base.InitComponent();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override void UpdateSerieContext()
        {
            var needCheck = m_LegendEnter || (chart.isPointerInChart && serie.context.rect.Contains(chart.pointerPos));
            //var needInteract = false;
            if (!needCheck)
            {
                if (m_LastCheckContextFlag != needCheck)
                {
                    m_LastCheckContextFlag = needCheck;
                    serie.context.pointerItemDataIndex = -1;
                    serie.context.pointerEnter = false;
                    foreach (var serieData in serie.data)
                    {
                        serieData.context.highlight = false;
                    }
                    chart.RefreshPainter(serie);
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;

            //TODO:
        }

        public override void DrawSerie(VertexHelper vh)
        {
            // SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            // m_SerieGrid = chart.GetChartComponent<GridCoord3D>(0);

            // var interacting = false;
            // var lineWidth = serie.lineStyle.GetWidth(chart.theme.serie.lineWidth);
            // var lineColor = SerieHelper.GetLineColor(serie, null, chart.theme, serie.context.colorIndex);

            // var xAxis = chart.GetChartComponent<XAxis3D>(0);
            // var yAxis = chart.GetChartComponent<YAxis3D>(0);
            // var zAxis = chart.GetChartComponent<ZAxis3D>(0);

            // serie.context.dataPoints.Clear();
            // foreach (var serieData in serie.data)
            // {
            //     var x = serieData.GetData(0);
            //     var y = serieData.GetData(1);
            //     var z = serieData.GetData(2);
            //     var pos = Axis3DHelper.Get3DGridPosition(m_SerieGrid, xAxis, yAxis, zAxis, x, y, z);
            //     serieData.context.position = pos;
            //     serieData.context.labelPosition = pos;
            //     serie.context.dataPoints.Add(pos);
            // }

            // UGL.DrawLine(vh, serie.context.dataPoints, lineWidth, lineColor, false);

            if (serie.animation.HasFadeOut())
                return;

            var xAxis = chart.GetChartComponent<XAxis3D>(serie.xAxisIndex);
            var yAxis = chart.GetChartComponent<YAxis3D>(serie.yAxisIndex);
            var zAxis = chart.GetChartComponent<ZAxis3D>(0);

            if (xAxis == null)
                return;

            m_SerieGrid = chart.GetChartComponent<GridCoord3D>(xAxis.gridIndex);
            if (m_SerieGrid == null)
                return;

            var isY = false;
            var visualMap = chart.GetVisualMapOfSerie(serie);
            var dataZoom = chart.GetDataZoomOfAxis(xAxis);
            var showData = serie.GetDataList(dataZoom);

            if (showData.Count <= 0)
                return;

            var axisLength = xAxis.context.length;
            int maxCount = serie.maxShow > 0 ?
                (serie.maxShow > showData.Count ? showData.Count : serie.maxShow) :
                showData.Count;
            maxCount -= serie.context.dataZoomStartIndexOffset;
            var scaleWid = AxisHelper.GetDataWidth(xAxis, axisLength, maxCount, dataZoom);
            int rate = LineHelper.GetDataAverageRate(serie, axisLength, maxCount, false);
            var dataChanging = false;
            var unscaledTime = serie.animation.unscaledTime;

            var interacting = false;
            var lineWidth = LineHelper.GetLineWidth(ref interacting, serie, chart.theme.serie.lineWidth);

            xAxis.context.scaleWidth = scaleWid;
            serie.containerIndex = m_SerieGrid.index;
            serie.containterInstanceId = m_SerieGrid.instanceId;

            Serie lastSerie = null;
            var isStack = SeriesHelper.IsStack<Line>(chart.series, serie.stack);
            if (isStack)
            {
                lastSerie = SeriesHelper.GetLastStackSerie(chart.series, serie);
                SeriesHelper.UpdateStackDataList(chart.series, serie, dataZoom, m_StackSerieData);
            }
            var lp = Vector3.zero;
            for (int i = serie.minShow; i < showData.Count; i += rate)
            {
                var serieData = showData[i];
                var isIgnore = serie.IsIgnoreValue(serieData);
                if (isIgnore)
                {
                    serieData.context.stackHeight = 0;
                    serieData.context.position = Vector3.zero;
                    if (serie.ignoreLineBreak && serie.context.dataIgnores.Count > 0)
                    {
                        serie.context.dataIgnores[serie.context.dataIgnores.Count - 1] = true;
                    }
                }
                else
                {
                    var x = serieData.GetData(0);
                    var y = serieData.GetData(1);
                    var z = serieData.GetData(2);
                    var np = Axis3DHelper.Get3DGridPosition(m_SerieGrid, xAxis, yAxis, zAxis, x, y, z);
                    serieData.context.isClip = false;
                    if (serie.clip && !m_SerieGrid.Contains(np))
                    {
                        //if (m_SerieGrid.BoundaryPoint(lp, np, ref np))
                        {
                            serieData.context.isClip = true;
                        }
                    }
                    serie.context.dataIgnores.Add(false);
                    serieData.context.position = np;
                    serie.context.dataPoints.Add(np);
                    serie.context.dataIndexs.Add(serieData.index);
                    lp = np;
                }
            }

            if (dataChanging || interacting)
                chart.RefreshPainter(serie);

            if (serie.context.dataPoints.Count <= 0)
                return;

            serie.animation.InitProgress(serie.context.dataPoints, isY);

            VisualMapHelper.AutoSetLineMinMax(visualMap, serie, isY, xAxis, zAxis);
            Line3DHelper.UpdateSerieDrawPoints(serie, chart.settings, chart.theme, visualMap, lineWidth, isY, m_SerieGrid);
            Line3DHelper.DrawSerieLineArea(vh, serie, lastSerie, chart.theme, visualMap, isY, m_SerieGrid, xAxis, yAxis, zAxis);
            Line3DHelper.DrawSerieLine(vh, chart.theme, serie, visualMap, m_SerieGrid, xAxis, zAxis, lineWidth);

            serie.context.vertCount = vh.currentVertCount;

            if (!serie.animation.IsFinish())
            {
                serie.animation.CheckProgress();
                serie.animation.CheckSymbol(serie.symbol.GetSize(null, chart.theme.serie.lineSymbolSize));
                chart.RefreshPainter(serie);
            }
        }
    }
}