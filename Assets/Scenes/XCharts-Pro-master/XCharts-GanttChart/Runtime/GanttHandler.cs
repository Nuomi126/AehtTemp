using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Gantts
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class GanttHandler : SerieHandler<Gantt>
    {
        private GridCoord m_SerieGrid;

        public override void UpdateSerieContext()
        {
            if (m_SerieGrid == null)
                return;

            var needCheck = (chart.isPointerInChart && m_SerieGrid.IsPointerEnter() && !serie.placeHolder) || m_LegendEnter;
            var needInteract = false;
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
            Color32 color, toColor;
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

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            var colorIndex = serie.context.colorIndex;
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category, marker, itemFormatter,
                numericFormatter, ignoreDataDefaultContent, 1, colorIndex);
        }

        public override void DrawSerie(VertexHelper vh)
        {
            if (serie.dataCount == 0) return;
            DrawGanttSerie(vh, serie);
        }

        private void DrawGanttSerie(VertexHelper vh, Serie serie)
        {
            if (serie.animation.HasFadeOut()) return;
            XAxis xAxis;
            YAxis yAxis;
            GanttStyle style;
            DataZoom xDataZoom, yDataZoom;
            if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) return;
            if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) return;
            if (!chart.TryGetChartComponent<GridCoord>(out m_SerieGrid, xAxis.gridIndex)) return;
            if (!chart.TryGetChartComponent<GanttStyle>(out style)) return;
            chart.GetDataZoomOfSerie(serie, out xDataZoom, out yDataZoom);
            if (yDataZoom != null)
            {
                yDataZoom.context.invert = style.invert;
                if (!yDataZoom.IsInSelectedZoom(chart.series.Count, serie.index, style.invert)) return;
            }
            var showData = serie.GetDataList(null);
            var xCategoryWidth = AxisHelper.GetDataWidth(xAxis, m_SerieGrid.context.width, showData.Count, xDataZoom);
            var yCategoryWidth = AxisHelper.GetDataWidth(yAxis, m_SerieGrid.context.height, showData.Count, yDataZoom);
            var barWidth = serie.GetBarWidth(yCategoryWidth);
            var gap = (yCategoryWidth - barWidth) / 2;
            var dataChanging = false;
            var minValue = xAxis.context.minValue;
            var maxValue = xAxis.context.maxValue;
            var pX = m_SerieGrid.context.x + (xAxis.boundaryGap ? xCategoryWidth / 2 : 0);
            var pY = 0f;
            var startY = m_SerieGrid.context.y - (yAxis.boundaryGap ? 0 : yCategoryWidth / 2);
            var isTimeOrValue = xAxis.IsTime() || xAxis.IsValue();
            var serieCount = 0;
            var categoryIndex = 0;
            GetGanttSerieCategoryIndex(serie, m_SerieGrid.index, out serieCount, out categoryIndex);
            var dataCount = serie.data.Count;
            var colorIndex = serie.context.colorIndex;
            if (style.ganttType == GanttType.Multinomial)
            {
                var stackIndex = GetGanttSerieStackIndex(serie, m_SerieGrid.index);
                DrawBackground(vh, stackIndex, serie, m_SerieGrid, style, pX, startY + (serieCount - categoryIndex - 1) * yCategoryWidth,
                    yCategoryWidth);
            }
            if (!serie.show) return;
            for (int i = 0; i < dataCount; i++)
            {
                var serieData = serie.data[i];
                switch (style.ganttType)
                {
                    case GanttType.Monomial:
                        pY = startY + (categoryIndex - i) * yCategoryWidth;
                        break;
                    case GanttType.Multinomial:
                        pY = startY + (serieCount - categoryIndex - 1) * yCategoryWidth;
                        colorIndex = i;
                        break;
                }
                DrawSerieData(vh, m_SerieGrid, serie, serieData, colorIndex, pX, pY, gap, barWidth, isTimeOrValue, minValue,
                    maxValue, xCategoryWidth);
            }
            if (dataChanging)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void DrawBackground(VertexHelper vh, int categoryIndex, Serie serie, GridCoord grid, GanttStyle style, float pX, float pY,
            float yCategoryWidth)
        {
            if (!style.splitArea.show) return;
            var plb = new Vector3(grid.context.x, pY + yCategoryWidth);
            var plt = new Vector3(grid.context.x, pY);
            var prt = new Vector3(grid.context.x + grid.context.width, pY);
            var prb = new Vector3(grid.context.x + grid.context.width, pY + yCategoryWidth);
            var color = ChartHelper.IsClearColor(serie.itemStyle.backgroundColor) ?
                style.splitArea.GetColor(categoryIndex, chart.theme.axis) :
                serie.itemStyle.backgroundColor;
            UGL.DrawQuadrilateral(vh, plb, plt, prt, prb, color);
        }

        private void DrawSerieData(VertexHelper vh, GridCoord grid, Serie serie, SerieData serieData, int colorIndex,
            float pX, float pY, float gap, float barWidth, bool isTime, double minValue, double maxValue,
            float xCategoryWidth)
        {
            var xStart = 0f;
            var xEnd = 0f;
            var xActualStart = 0f;
            var xActualEnd = 0f;
            int start, end, actualStart, actualEnd;
            start = (int)serieData.GetData(0);
            end = (int)serieData.GetData(1);
            actualStart = (int)serieData.GetData(2);
            actualEnd = (int)serieData.GetData(3);
            var enableActual = actualStart > 0 && actualEnd > 0;
            if (isTime)
            {
                var valueTotal = maxValue - minValue;
                xStart = pX + (float)((start - minValue) / valueTotal * grid.context.width);
                xEnd = pX + (float)((end - minValue) / valueTotal * grid.context.width);
                if (enableActual)
                {
                    xActualStart = pX + (float)((actualStart - minValue) / valueTotal * grid.context.width);
                    xActualEnd = pX + (float)((actualEnd - minValue) / valueTotal * grid.context.width);
                }
            }
            else
            {
                xStart = pX + ((int)start) * xCategoryWidth;
                xEnd = pX + ((int)end) * xCategoryWidth;
                if (enableActual)
                {
                    xActualStart = pX + ((int)actualStart) * xCategoryWidth;
                    xActualEnd = pX + ((int)actualEnd) * xCategoryWidth;
                }
            }
            var itemStyle = SerieHelper.GetItemStyle(serie, serieData);
            var color = chart.GetItemColor(serie, serieData, colorIndex);
            var borderWidth = itemStyle.borderWidth;
            var rect = DrawGanttBar(vh, grid, serie, serieData, itemStyle, color, pY, pY, gap, barWidth, xStart, xEnd);
            if (enableActual)
            {
                var defaultActualColor = SerieHelper.GetItemColor(serie, serieData, chart.theme, colorIndex, SerieState.Emphasis);
                var actualColor = itemStyle.GetColor0(defaultActualColor);
                var rect2 = DrawGanttBar(vh, grid, serie, serieData, itemStyle, actualColor, pY, pY, gap, barWidth,
                    xActualStart, xActualEnd);
                var rect3X = Mathf.Min(rect.x, rect2.x);
                var rect3Width = Mathf.Max(rect.x + rect.width, rect2.x + rect2.width) - rect3X;
                var rect3 = new Rect(rect3X, rect.y, rect3Width, rect.height);
                serie.context.dataPoints.Add(rect3.center);
                serie.context.dataIndexs.Add(serieData.index);
                serieData.context.position = rect3.center;
                serieData.context.labelPosition = rect3.center;
                serieData.context.rect = rect3;
            }
            else
            {
                serie.context.dataPoints.Add(rect.center);
                serie.context.dataIndexs.Add(serieData.index);
                serieData.context.position = rect.center;
                serieData.context.labelPosition = rect.center;
                serieData.context.rect = rect;
            }
        }

        private Rect DrawGanttBar(VertexHelper vh, GridCoord grid, Serie serie, SerieData serieData, ItemStyle itemStyle,
            Color32 color, float pX, float pY, float gap, float barWidth, float xStart, float xEnd)
        {

            var borderWidth = itemStyle.borderWidth;
            var plb = new Vector3(xStart + borderWidth, pY + gap + borderWidth);
            var plt = new Vector3(xStart + borderWidth, pY + gap + barWidth - borderWidth);
            var prt = new Vector3(xEnd - borderWidth, pY + gap + barWidth - borderWidth);
            var prb = new Vector3(xEnd - borderWidth, pY + gap + borderWidth);
            var center = new Vector3((plb.x + prt.x) / 2, (plt.y + prb.y) / 2);
            var itemWidth = Mathf.Abs(prt.x - plb.x);
            var itemHeight = Mathf.Abs(plt.y - prb.y);
            var rect = new Rect(plb.x, plb.y, xEnd - xStart, barWidth);
            if (serie.clip)
            {
                plb = chart.ClampInGrid(grid, plb);
                plt = chart.ClampInGrid(grid, plt);
                prt = chart.ClampInGrid(grid, prt);
                prb = chart.ClampInGrid(grid, prb);
                center = chart.ClampInGrid(grid, center);
            }
            if (itemStyle.IsNeedCorner())
            {
                UGL.DrawRoundRectangle(vh, center, itemWidth, itemHeight, color, color, 0,
                    itemStyle.cornerRadius, true, 0.5f);
            }
            else
            {
                chart.DrawClipPolygon(vh, ref prb, ref plb, ref plt, ref prt, color, color,
                    serie.clip, grid);
            }
            if (borderWidth != 0)
            {
                UGL.DrawBorder(vh, center, itemWidth, itemHeight, borderWidth, itemStyle.borderColor,
                    itemStyle.borderToColor, 0, itemStyle.cornerRadius, true, 0.5f);
            }
            return rect;
        }

        private void GetGanttSerieCategoryIndex(Serie currSerie, int gridIndex, out int serieCount, out int categoryIndex)
        {
            var count = chart.series.Count;
            var found = false;
            XAxis xAxis;
            YAxis yAxis;
            GridCoord grid;
            GanttStyle style;
            DataZoom xDataZoom, yDataZoom;
            serieCount = 0;
            categoryIndex = -1;
            for (int i = 0; i < count; i++)
            {
                var serie = chart.GetSerie(i);
                if (!(serie is Gantt)) continue;
                if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) continue;
                if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) continue;
                if (!chart.TryGetChartComponent<GridCoord>(out grid, xAxis.gridIndex)) continue;
                if (!chart.TryGetChartComponent<GanttStyle>(out style)) continue;
                chart.GetDataZoomOfSerie(serie, out xDataZoom, out yDataZoom);
                if (yDataZoom != null)
                {
                    if (!yDataZoom.IsInSelectedZoom(count, i, style.invert)) continue;
                }
                serieCount++;
                if (style.ganttType == GanttType.Monomial)
                {
                    foreach (var serieData in serie.data)
                    {
                        if (!found) categoryIndex++;
                    }
                }
                else
                {
                    if (!found) categoryIndex++;
                }
                if (serie.index == currSerie.index)
                {
                    found = true;
                }
            }
        }

        private List<string> m_StackList = new List<string>();
        private int GetGanttSerieStackIndex(Serie currSerie, int gridIndex)
        {
            XAxis xAxis;
            YAxis yAxis;
            GridCoord grid;
            m_StackList.Clear();
            var count = chart.series.Count;
            for (int i = 0; i < count; i++)
            {
                var serie = chart.GetSerie(i);
                if (!(serie is Gantt)) continue;
                if (!chart.TryGetChartComponent<XAxis>(out xAxis, serie.xAxisIndex)) continue;
                if (!chart.TryGetChartComponent<YAxis>(out yAxis, serie.yAxisIndex)) continue;
                if (!chart.TryGetChartComponent<GridCoord>(out grid, xAxis.gridIndex)) continue;
                if (grid.index != gridIndex) continue;
                var stack = string.IsNullOrEmpty(serie.stack) ?
                    ChartCached.IntToStr(serie.index) :
                    serie.stack;
                if (!m_StackList.Contains(stack))
                {
                    m_StackList.Add(stack);
                }
            }
            var nowstack = string.IsNullOrEmpty(currSerie.stack) ?
                ChartCached.IntToStr(currSerie.index) :
                currSerie.stack;
            return m_StackList.IndexOf(nowstack);
        }
    }
}