using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Bar3Ds
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class Bar3DHandler : SerieHandler<Bar3D>
    {
        List<List<SerieData>> m_StackSerieData = new List<List<SerieData>>();
        private GridCoord m_SerieGrid;

        public override void DrawSerie(VertexHelper vh)
        {
            if (serie.dataCount == 0) return;
            DrawBar3D(vh, serie);
        }
        public override void Update()
        {
            base.Update();
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateCoordSerieParams(ref paramList, ref title, dataIndex, showCategory, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override Vector3 GetSerieDataLabelPosition(SerieData serieData, LabelStyle label)
        {
            switch (label.position)
            {
                case LabelStyle.Position.Bottom:
                    var center = serieData.context.rect.center;
                    return new Vector3(center.x, center.y - serieData.context.rect.height / 2);
                case LabelStyle.Position.Center:
                case LabelStyle.Position.Inside:
                    return serieData.context.rect.center;
                default:
                    return serieData.context.position;
            }
        }

        public override void UpdateSerieContext()
        {
            if (m_SerieGrid == null)
                return;

            var needCheck = (chart.isPointerInChart && m_SerieGrid.IsPointerEnter()) || m_LegendEnter;
            var needInteract = false;
            Color32 color, toColor;
            if (!needCheck)
            {
                if (m_LastCheckContextFlag != needCheck)
                {
                    m_LastCheckContextFlag = needCheck;
                    serie.context.pointerItemDataIndex = -1;
                    serie.context.pointerEnter = false;

                    foreach (var serieData in serie.data)
                    {
                        SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme);
                        serieData.interact.SetColor(ref needInteract, color, toColor);
                    }
                    if (needInteract)
                    {
                        chart.RefreshPainter(serie);
                    }
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;
            if (m_LegendEnter)
            {
                serie.context.pointerEnter = true;
                foreach (var serieData in serie.data)
                {
                    SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme);
                    serieData.interact.SetColor(ref needInteract, color, toColor);
                }
            }
            else
            {
                serie.context.pointerItemDataIndex = -1;
                serie.context.pointerEnter = false;
                foreach (var serieData in serie.data)
                {
                    if (serieData.context.rect.Contains(chart.pointerPos))
                    {
                        serie.context.pointerItemDataIndex = serieData.index;
                        serie.context.pointerEnter = true;
                        serieData.context.highlight = true;
                        SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, SerieState.Emphasis);
                        serieData.interact.SetColor(ref needInteract, color, toColor);
                    }
                    else
                    {
                        serieData.context.highlight = false;
                        SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, SerieState.Normal);
                        serieData.interact.SetColor(ref needInteract, color, toColor);
                    }
                }
            }
            if (needInteract)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void DrawBar3D(VertexHelper vh, Bar3D serie)
        {
            if (!serie.show) return;
            if (serie.animation.HasFadeOut()) return;
            XAxis xAxis;
            YAxis yAxis;
            if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) return;
            if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) return;
            if (!chart.TryGetChartComponent<GridCoord>(out m_SerieGrid, xAxis.gridIndex)) return;
            var bar3dStyle = GetBar3DStyle();
            var dataZoom = chart.GetDataZoomOfAxis(xAxis);
            var showData = serie.GetDataList(dataZoom);
            var colorIndex = serie.context.colorIndex;
            var isStack = SeriesHelper.IsStack<Bar3D>(chart.series, serie.stack);
            var vertical = bar3dStyle.verticalArrange;
            m_StackSerieData.Clear();
            if (isStack) SeriesHelper.UpdateStackDataList(chart.series, serie, dataZoom, m_StackSerieData);
            float categoryWidth = AxisHelper.GetDataWidth(xAxis, m_SerieGrid.context.width, showData.Count, dataZoom);
            float barGap = chart.GetSerieBarGap<Bar3D>();

            var barCount = chart.GetSerieBarRealCount<Bar3D>();
            float totalBarWidth = chart.GetSerieTotalWidth<Bar3D>(categoryWidth, barGap, barCount);
            float barWidth = serie.GetBarWidth(categoryWidth, barCount, 0.4f);
            float offset = (categoryWidth - totalBarWidth) / 2;
            var serieReadIndex = chart.GetSerieIndexIfStack<Bar3D>(serie);
            float gap = serie.barGap == -1 ? offset : offset + chart.GetSerieTotalGap<Bar3D>(categoryWidth, barGap, serieReadIndex);
            int maxCount = serie.maxShow > 0 ?
                (serie.maxShow > showData.Count ? showData.Count : serie.maxShow) :
                showData.Count;

            var isPercentStack = SeriesHelper.IsPercentStack<Bar3D>(chart.series, serie.stack);
            var dataChanging = false;
            var dataChangeDuration = serie.animation.GetChangeDuration();
            var dataAddDuration = serie.animation.GetAdditionDuration();
            var unscaledTime = serie.animation.unscaledTime;
            var yMinValue = yAxis.context.minValue;
            var yMaxValue = yAxis.context.maxValue;

            serie.containerIndex = m_SerieGrid.index;
            serie.containterInstanceId = m_SerieGrid.instanceId;
            serie.animation.InitProgress(m_SerieGrid.context.x, m_SerieGrid.context.x + m_SerieGrid.context.width);
            Color32 areaColor, areaToColor;
            for (int i = serie.minShow; i < maxCount; i++)
            {
                var serieData = showData[i];
                if (serie.IsIgnoreValue(serieData, serieData.GetData(1)))
                {
                    serie.context.dataPoints.Add(Vector3.zero);
                    serie.context.dataIndexs.Add(serieData.index);
                    continue;
                }
                if (serie.colorByData)
                {
                    colorIndex = i;
                }
                var highlight = serie.data[i].context.highlight || serie.highlight;
                var itemStyle = SerieHelper.GetItemStyle(serie, serieData);
                var value = serieData.GetCurrData(1, dataAddDuration, dataChangeDuration, yAxis.inverse, 0, 0, unscaledTime);
                float borderWidth = value == 0 ? 0 : itemStyle.borderWidth;
                if (serieData.IsDataChanged()) dataChanging = true;
                float pX = m_SerieGrid.context.x + i * categoryWidth + bar3dStyle.barOffset.x;
                float zeroY = m_SerieGrid.context.y + yAxis.context.offset;
                if (!xAxis.boundaryGap) pX -= categoryWidth / 2;
                float axisLineWidth = value == 0 ? 0 :
                    ((value < 0 ? -1 : 1) * xAxis.axisLine.GetWidth(chart.theme.axis.lineWidth));
                float pY = zeroY + axisLineWidth + bar3dStyle.barOffset.y;
                if (isStack)
                {
                    for (int n = 0; n < m_StackSerieData.Count - 1; n++)
                    {
                        pY += m_StackSerieData[n][i].context.stackHeight;
                    }
                }

                var barHig = 0f;
                var valueTotal = 0d;
                if (isPercentStack)
                {
                    valueTotal = chart.GetSerieSameStackTotalValue<Bar3D>(serie.stack, i);
                    barHig = valueTotal != 0 ? ((float)(value / valueTotal) * m_SerieGrid.context.height) : 0;
                }
                else
                {
                    valueTotal = yMaxValue - yMinValue;
                    if (valueTotal != 0)
                    {
                        barHig = (float)((yMinValue > 0 ? value - yMinValue : value) / valueTotal) * m_SerieGrid.context.height;
                    }
                }
                serieData.context.stackHeight = barHig;
                float currHig = AnimationStyleHelper.CheckDataAnimation(chart, serie, i, barHig);
                Vector3 plb, plt, prt, prb, top;
                if (value < 0)
                {
                    plb = new Vector3(pX + gap + borderWidth, pY - borderWidth);
                    plt = new Vector3(pX + gap + borderWidth, pY + currHig + borderWidth);
                    prt = new Vector3(pX + gap + barWidth - borderWidth, pY + currHig + borderWidth);
                    prb = new Vector3(pX + gap + barWidth - borderWidth, pY - borderWidth);
                }
                else
                {
                    plb = new Vector3(pX + gap + borderWidth, pY + borderWidth);
                    plt = new Vector3(pX + gap + borderWidth, pY + currHig - borderWidth);
                    prt = new Vector3(pX + gap + barWidth - borderWidth, pY + currHig - borderWidth);
                    prb = new Vector3(pX + gap + barWidth - borderWidth, pY + borderWidth);
                }
                top = new Vector3(pX + gap + barWidth / 2, pY + currHig - borderWidth);
                if (serie.clip)
                {
                    plb = chart.ClampInGrid(m_SerieGrid, plb);
                    plt = chart.ClampInGrid(m_SerieGrid, plt);
                    prt = chart.ClampInGrid(m_SerieGrid, prt);
                    prb = chart.ClampInGrid(m_SerieGrid, prb);
                    top = chart.ClampInGrid(m_SerieGrid, top);
                }
                serieData.context.position = top;
                serie.context.dataPoints.Add(top);
                serie.context.dataIndexs.Add(serieData.index);
                if (serie.show && currHig != 0)
                {
                    if (!ChartHelper.IsClearColor(itemStyle.backgroundColor))
                    {
                        var plt2 = new Vector3(pX + gap + borderWidth, pY + m_SerieGrid.context.height - borderWidth);
                        var prt2 = new Vector3(pX + gap + barWidth - borderWidth, pY + m_SerieGrid.context.height - borderWidth);
                        DrawNormalBar(vh, serie, serieData, itemStyle, bar3dStyle, colorIndex, highlight, gap, barWidth,
                            pX, pY, plb, plt2, prt2, prb, false, m_SerieGrid, vertical, itemStyle.backgroundColor, itemStyle.backgroundColor);
                    }
                    SerieHelper.GetItemColor(out areaColor, out areaToColor, serie, serieData, chart.theme, colorIndex);
                    DrawNormalBar(vh, serie, serieData, itemStyle, bar3dStyle, colorIndex, highlight, gap, barWidth,
                        pX, pY, plb, plt, prt, prb, false, m_SerieGrid, vertical, areaColor, areaToColor);

                }
                if (serie.animation.CheckDetailBreak(top, false))
                {
                    break;
                }
            }
            if (!serie.animation.IsFinish())
            {
                serie.animation.CheckProgress();
                chart.RefreshPainter(serie);
            }
            if (dataChanging)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void DrawNormalBar(VertexHelper vh, Bar3D serie, SerieData serieData, ItemStyle itemStyle,
            Bar3DStyle bar3DStyle, int colorIndex, bool highlight, float gap, float barWidth, float pX,
            float pY, Vector3 plb, Vector3 plt, Vector3 prt, Vector3 prb, bool isYAxis, GridCoord grid,
            bool verticalArrange, Color32 areaColor, Color32 areaToColor)
        {
            if (serie.clip)
            {
                prb = chart.ClampInGrid(grid, prb);
                plb = chart.ClampInGrid(grid, plb);
                plt = chart.ClampInGrid(grid, plt);
                prt = chart.ClampInGrid(grid, prt);
            }
            if (verticalArrange)
            {
                var arrangeGap = ChartHelper.GetActualValue(bar3DStyle.verticalArrangeGap, barWidth);
                var diff = bar3DStyle.verticalArrangeDire.normalized * (colorIndex * arrangeGap);
                prb += diff;
                plb += diff;
                plt += diff;
                prt += diff;
            }
            serieData.context.rect = Rect.MinMaxRect(plb.x, plb.y, prb.x, prt.y);
            var itemWidth = Mathf.Abs(prt.x - plb.x);
            var itemHeight = Mathf.Abs(plt.y - prb.y);
            if (itemWidth > 0 && itemHeight > 0)
            {
                var bottomColor = ChartHelper.IsClearColor(bar3DStyle.bottomColor) ?
                    ChartHelper.GetHighlightColor(areaColor, bar3DStyle.bottomColorRate) :
                    bar3DStyle.bottomColor;
                var topColor = ChartHelper.IsClearColor(bar3DStyle.topColor) ?
                    ChartHelper.GetHighlightColor(areaToColor, bar3DStyle.topColorRate) :
                    bar3DStyle.topColor;
                switch (serie.bar3dType)
                {
                    case Bar3DType.Cube:
                        DrawCube(vh, bar3DStyle, plb, plt, prt, prb, areaColor, areaToColor, bottomColor, topColor, itemStyle.borderWidth, itemStyle.borderColor);
                        break;
                    case Bar3DType.Cylinder:
                        var center = (plb + prb) / 2;
                        var ellipseRadius = itemWidth * bar3DStyle.cylinderRate / 2;
                        center.y += ellipseRadius;
                        DrawCylinder(vh, center, itemWidth / 2, ellipseRadius, itemHeight, areaColor, areaToColor, bottomColor, topColor);
                        break;
                }
            }
        }

        private Bar3DStyle GetBar3DStyle()
        {
            if (serie.bar3dStyle.show) return serie.bar3dStyle;
            for (int i = chart.series.Count - 1; i >= 0; i--)
            {
                var tempSerie = chart.series[i];
                if (tempSerie is Bar3D)
                {
                    var bar3d = tempSerie as Bar3D;
                    if (bar3d.bar3dStyle.show) return bar3d.bar3dStyle;
                }
            }
            return serie.bar3dStyle;
        }

        private void DrawCube(VertexHelper vh, Bar3DStyle bar3DStyle, Vector3 plb, Vector3 plt, Vector3 prt, Vector3 prb,
            Color32 areaColor, Color32 areaToColor, Color32 bottomColor, Color32 topColor, float borderWidth, Color32 borderColor)
        {
            var diff = bar3DStyle.cubeDire.normalized * (prb.x - plb.x) * bar3DStyle.widthRate;
            var sideColor = ChartHelper.IsClearColor(bar3DStyle.sideColor) ?
                ChartHelper.GetHighlightColor(areaColor, bar3DStyle.sideColorRate)
                : bar3DStyle.sideColor;
            var sideToColor = ChartHelper.IsClearColor(bar3DStyle.sideColor) ?
                ChartHelper.GetHighlightColor(areaToColor, bar3DStyle.sideColorRate) :
                bar3DStyle.sideColor;
            var plt2 = plt + diff;
            var prt2 = prt + diff;
            var prb2 = prb + diff;
            var plb2 = plb + diff;

            if (ChartHelper.IsClearColor(borderColor))
            {
                borderColor = sideColor;
                borderColor.a = (byte)(sideColor.a * 1.2f);
            }

            if (bar3DStyle.occludedAreaBorder && borderWidth > 0)
            {
                if (bar3DStyle.occludedAreaBorderDash)
                {
                    UGL.DrawDashLine(vh, plb, plb2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plb2, prb2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plb2, plt2, borderWidth, borderColor, borderColor);
                }
                else
                {
                    UGL.DrawLine(vh, plb, plb2, borderWidth, borderColor);
                    UGL.DrawLine(vh, plb2, prb2, borderWidth, borderColor);
                    UGL.DrawLine(vh, plb2, plt2, borderWidth, borderColor);
                }
            }

            var cvc = vh.currentVertCount;

            vh.AddVert(plb, sideColor, Vector2.zero); //0
            vh.AddVert(plb, areaColor, Vector2.zero); //1
            vh.AddVert(plb, bottomColor, Vector2.zero); //2

            vh.AddVert(plb2, areaColor, Vector2.zero); //3
            vh.AddVert(plb2, sideColor, Vector2.zero); //4
            vh.AddVert(plb2, bottomColor, Vector2.zero); //5

            vh.AddVert(prb2, sideColor, Vector2.zero); //6
            vh.AddVert(prb2, areaColor, Vector2.zero); //7
            vh.AddVert(prb2, bottomColor, Vector2.zero); //8

            vh.AddVert(prb, areaColor, Vector2.zero); //9
            vh.AddVert(prb, sideColor, Vector2.zero); //10
            vh.AddVert(prb, bottomColor, Vector2.zero); //11

            vh.AddVert(plt, sideToColor, Vector2.zero); //12
            vh.AddVert(plt, areaToColor, Vector2.zero); //13
            vh.AddVert(plt, topColor, Vector2.zero); //14

            vh.AddVert(plt2, areaToColor, Vector2.zero); //15
            vh.AddVert(plt2, sideToColor, Vector2.zero); //16
            vh.AddVert(plt2, topColor, Vector2.zero); //17

            vh.AddVert(prt2, sideToColor, Vector2.zero); //18
            vh.AddVert(prt2, areaToColor, Vector2.zero); //19
            vh.AddVert(prt2, topColor, Vector2.zero); //20

            vh.AddVert(prt, areaToColor, Vector2.zero); //21
            vh.AddVert(prt, sideToColor, Vector2.zero); //22
            vh.AddVert(prt, topColor, Vector2.zero); //23

            vh.AddTriangle(cvc + 2, cvc + 5, cvc + 11);
            vh.AddTriangle(cvc + 11, cvc + 5, cvc + 8);

            vh.AddTriangle(cvc + 3, cvc + 15, cvc + 7);
            vh.AddTriangle(cvc + 7, cvc + 15, cvc + 19);

            vh.AddTriangle(cvc + 0, cvc + 12, cvc + 4);
            vh.AddTriangle(cvc + 4, cvc + 12, cvc + 16);

            vh.AddTriangle(cvc + 13, cvc + 17, cvc + 23);
            vh.AddTriangle(cvc + 23, cvc + 17, cvc + 20);

            vh.AddTriangle(cvc + 2, cvc + 13, cvc + 9);
            vh.AddTriangle(cvc + 9, cvc + 13, cvc + 21);

            vh.AddTriangle(cvc + 10, cvc + 22, cvc + 6);
            vh.AddTriangle(cvc + 6, cvc + 22, cvc + 18);

            if (borderWidth > 0)
            {
                if (bar3DStyle.borderDash)
                {
                    UGL.DrawDashLine(vh, plt2, prt2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plt, plt2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plt, prt, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plt, plb, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, plb, prb, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, prb, prt, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, prb, prb2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, prt, prt2, borderWidth, borderColor, borderColor);
                    UGL.DrawDashLine(vh, prt2, prb2, borderWidth, borderColor, borderColor);
                }
                else
                {
                    UGL.DrawLine(vh, plt2, prt2, borderWidth, borderColor);
                    UGL.DrawLine(vh, plt, plt2, borderWidth, borderColor);
                    UGL.DrawLine(vh, plt, prt, borderWidth, borderColor);
                    UGL.DrawLine(vh, plt, plb, borderWidth, borderColor);
                    UGL.DrawLine(vh, plb, prb, borderWidth, borderColor);
                    UGL.DrawLine(vh, prb, prt, borderWidth, borderColor);
                    UGL.DrawLine(vh, prb, prb2, borderWidth, borderColor);
                    UGL.DrawLine(vh, prt, prt2, borderWidth, borderColor);
                    UGL.DrawLine(vh, prt2, prb2, borderWidth, borderColor);
                }
            }
        }

        private void DrawCylinder(VertexHelper vh, Vector3 center, float outw, float outh, float hig,
            Color32 color, Color32 toColor, Color32 bottomColor, Color32 topColor)
        {
            var smoothness = chart.settings.cicleSmoothness;
            DrawEllipse(vh, center, 180, 360, outw, outh, hig, color, toColor, bottomColor, topColor, smoothness);
            DrawEllipse(vh, center, 0, 180, outw, outh, hig, color, toColor, bottomColor, topColor, smoothness);
        }

        private void DrawEllipse(VertexHelper vh, Vector3 center, float startAngle, float endAngle,
            float outw, float outh, float hig, Color32 color, Color32 toColor, Color32 bottomColor,
            Color32 topColor, float smoothness)
        {
            if (outw < 0) outw = 0;
            if (outh < 0) outh = 0;
            if (smoothness < 0.5f) smoothness = 0.5f;

            var count = Mathf.CeilToInt((endAngle - startAngle) / smoothness);
            var diff = (endAngle - startAngle) / count;
            var currentVertCount = vh.currentVertCount;
            var pcb = center;
            var pct = new Vector3(center.x, center.y + hig);
            var index = 0;
            vh.AddVert(pcb, bottomColor, Vector2.zero); // 0
            vh.AddVert(pct, topColor, Vector2.zero); // 1
            for (int i = 0; i <= count; i++)
            {
                var angle = startAngle + i * diff;
                var rad = (360 - angle) * Mathf.Deg2Rad;
                var x = center.x + outw * Mathf.Cos(rad);
                var y = center.y + outh * Mathf.Sin(rad);
                var pb = new Vector3(x, y);
                var pt = new Vector2(x, y + hig);

                vh.AddVert(pb, color, Vector2.zero); //2
                vh.AddVert(pb, bottomColor, Vector2.zero); //3
                vh.AddVert(pt, toColor, Vector2.zero); //4
                vh.AddVert(pt, topColor, Vector2.zero); //5
            }
            for (int i = 1; i <= count; i++)
            {
                index = currentVertCount + (i * 4) + 2;
                var lastIndex = index - 4;
                vh.AddTriangle(lastIndex + 1, currentVertCount, index + 1);
            }
            for (int i = 1; i <= count; i++)
            {
                index = currentVertCount + (i * 4) + 2;
                var lastIndex = index - 4;
                vh.AddTriangle(lastIndex, index, lastIndex + 2);
                vh.AddTriangle(index, index + 2, lastIndex + 2);
            }
            for (int i = 1; i <= count; i++)
            {
                index = currentVertCount + (i * 4) + 2;
                var lastIndex = index - 4;
                vh.AddTriangle(lastIndex + 3, currentVertCount + 1, index + 3);
            }
        }
    }
}