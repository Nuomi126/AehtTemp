using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.PictorialBars
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class PictorialBarHandler : SerieHandler<PictorialBar>
    {
        private bool m_RefreshSymbol;
        private GridCoord m_SerieGrid;
        private int m_LastSymbolCount;
        List<List<SerieData>> m_StackSerieData = new List<List<SerieData>>();

        public override void InitComponent()
        {
            base.InitComponent();
            var painter = chart.GetPainter(serie.index);
            if (painter != null)
            {
                ChartHelper.HideAllObject(painter.gameObject);
                if (serie.symbol.show && serie.symbol.type == SymbolType.Custom)
                {
                    InitSymbolImage(serie, painter);
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_RefreshSymbol)
            {
                RefreshSymbol();
                m_RefreshSymbol = false;
            }
        }

        public override void DrawSerie(VertexHelper vh)
        {
            DrawSerie(vh, serie);
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
                        serieData.context.highlight = false;
                        serieData.interact.Reset();
                    }
                    chart.RefreshPainter(serie);
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
                    if (serie.context.pointerAxisDataIndexs.Contains(serieData.index) ||
                        serieData.context.rect.Contains(chart.pointerPos))
                    {
                        serie.context.pointerItemDataIndex = serieData.index;
                        serie.context.pointerEnter = true;
                        serieData.context.highlight = true;
                    }
                    else
                    {
                        serieData.context.highlight = false;
                    }
                    var state = SerieHelper.GetSerieState(serie, serieData, true);
                    SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, state);
                    serieData.interact.SetColor(ref needInteract, color, toColor);
                }
            }
            if (needInteract)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void DrawSerie(VertexHelper vh, Serie serie)
        {
            if (!serie.show) return;
            if (serie.animation.HasFadeOut()) return;
            XAxis xAxis;
            YAxis yAxis;
            if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) return;
            if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) return;
            if (!chart.TryGetChartComponent<GridCoord>(out m_SerieGrid, xAxis.gridIndex)) return;
            var dataZoom = chart.GetDataZoomOfAxis(xAxis);
            var showData = serie.GetDataList(dataZoom);
            var isStack = SeriesHelper.IsStack<PictorialBar>(chart.series, serie.stack);
            m_StackSerieData.Clear();
            if (isStack) SeriesHelper.UpdateStackDataList(chart.series, serie, dataZoom, m_StackSerieData);
            float categoryWidth = AxisHelper.GetDataWidth(xAxis, m_SerieGrid.context.width, showData.Count, dataZoom);
            float barGap = chart.GetSerieBarGap<PictorialBar>();

            var barCount = chart.GetSerieBarRealCount<PictorialBar>();
            float totalBarWidth = chart.GetSerieTotalWidth<PictorialBar>(categoryWidth, barGap, barCount);
            float barWidth = serie.GetBarWidth(categoryWidth, barCount);
            float offset = (categoryWidth - totalBarWidth) / 2;
            var serieReadIndex = chart.GetSerieIndexIfStack<PictorialBar>(serie);
            float gap = serie.barGap == -1 ? offset : offset + chart.GetSerieTotalGap<PictorialBar>(categoryWidth, barGap, serieReadIndex);
            int maxCount = serie.maxShow > 0 ?
                (serie.maxShow > showData.Count ? showData.Count : serie.maxShow) :
                showData.Count;

            var isPercentStack = SeriesHelper.IsPercentStack<PictorialBar>(chart.series, serie.stack);
            var dataChanging = false;
            var dataChangeDuration = serie.animation.GetChangeDuration();
            var dataAddDuration = serie.animation.GetAdditionDuration();
            var yMinValue = yAxis.context.minValue;
            var yMaxValue = yAxis.context.maxValue;
            serie.animation.InitProgress(m_SerieGrid.context.y, m_SerieGrid.context.y + m_SerieGrid.context.height);
            for (int i = serie.minShow; i < maxCount; i++)
            {
                var serieData = showData[i];
                if (serie.IsIgnoreValue(serieData, serieData.GetData(1)))
                {
                    serie.context.dataPoints.Add(Vector3.zero);
                    serie.context.dataIndexs.Add(serieData.index);
                    continue;
                }
                var itemStyle = SerieHelper.GetItemStyle(serie, serieData);
                var value = serieData.GetCurrData(1, dataAddDuration, dataChangeDuration, yAxis.inverse, 0, 0, serie.animation.unscaledTime);
                float borderWidth = value == 0 ? 0 : itemStyle.borderWidth;
                if (serieData.IsDataChanged()) dataChanging = true;
                float pX = m_SerieGrid.context.x + i * categoryWidth;
                float zeroY = m_SerieGrid.context.y + yAxis.context.offset;
                if (!xAxis.boundaryGap) pX -= categoryWidth / 2;
                float axisLineWidth = value == 0 ? 0 :
                    ((value < 0 ? -1 : 1) * xAxis.axisLine.GetWidth(chart.theme.axis.lineWidth));
                float pY = zeroY + axisLineWidth;
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
                    valueTotal = chart.GetSerieSameStackTotalValue<PictorialBar>(serie.stack, i);
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
                serie.context.dataPoints.Add(top);
                serie.context.dataIndexs.Add(serieData.index);
                serieData.context.position = top;
                serieData.context.rect = Rect.MinMaxRect(plb.x, plb.y, prb.x, prt.y);
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
            m_RefreshSymbol = true;
        }

        private void InitSymbolImage(Serie serie, Painter painter)
        {
            Color32 areaColor, areaToColor;
            for (int i = 0; i < serie.data.Count; i++)
            {
                var serieData = serie.data[i];
                var symbol = SerieHelper.GetSerieSymbol(serie, serieData);
                var objName = "symbol_" + i;
                var imageType = symbol.repeat ? Image.Type.Tiled : symbol.imageType;
                var image = ChartHelper.AddIcon(objName, painter.transform, symbol.width,
                    symbol.height, symbol.image, imageType);
                image.raycastTarget = false;
                SerieHelper.GetItemColor(out areaColor, out areaToColor, serie, serieData, chart.theme);
                image.color = areaColor;
                serieData.context.symbol = image;
            }
            m_LastSymbolCount = serie.data.Count;
            m_RefreshSymbol = true;
        }

        private void RefreshSymbol()
        {
            if (serie.symbol.type != SymbolType.Custom) return;
            XAxis xAxis;
            YAxis yAxis;
            GridCoord grid;
            if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) return;
            if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) return;
            if (!chart.TryGetChartComponent<GridCoord>(out grid, xAxis.gridIndex)) return;
            var dataZoom = chart.GetDataZoomOfAxis(xAxis);
            var showData = serie.GetDataList(dataZoom);
            var categoryWidth = AxisHelper.GetDataWidth(xAxis, grid.context.width, showData.Count, dataZoom);
            var barWidth = serie.GetBarWidth(categoryWidth);

            Color32 areaColor, areaToColor;
            var painter = chart.GetPainter(serie.index);
            if (m_LastSymbolCount != serie.data.Count)
            {
                ChartHelper.HideAllObject(painter.transform);
                InitSymbolImage(serie, painter);
            }
            for (int i = 0; i < serie.data.Count; i++)
            {
                var serieData = serie.data[i];
                if (i >= serie.context.dataPoints.Count) continue;
                if (serieData.context.symbol == null) continue;
                ChartHelper.SetActive(serieData.context.symbol, serie.show && serieData.show);
                var symbol = SerieHelper.GetSerieSymbol(serie, serieData);
                var epos = serie.context.dataPoints[i];
                var spos = new Vector3(epos.x, grid.context.y);
                var cpos = (spos + epos) / 2;
                var wid = barWidth;
                var hig = epos.y - spos.y;
                var tran = serieData.context.symbol.transform;
                tran.localPosition = cpos + symbol.offset3;
                var rect = tran.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(wid, hig);

                SerieHelper.GetItemColor(out areaColor, out areaToColor, serie, serieData, chart.theme);
                serieData.context.symbol.color = areaColor;
            }
        }
    }
}