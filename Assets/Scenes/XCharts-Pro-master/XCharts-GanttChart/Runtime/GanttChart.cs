

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XCharts.Runtime.Gantts
{
    [AddComponentMenu("XCharts/GanttChart", 22)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [HelpURL("https://xcharts-team.github.io/docs/gantt")]
    public partial class GanttChart : BaseChart
    {
#pragma warning disable 0414
        [SerializeField][ListForComponent(typeof(GanttStyle))] private List<GanttStyle> m_GanttStyles = new List<GanttStyle>();
        [SerializeField][ListForSerie(typeof(Gantt))] private List<Gantt> m_SerieGantts = new List<Gantt>();
#pragma warning restore 0414

        protected System.Action<PointerEventData, int, int> m_OnPointerClickGantt;
        protected System.Action<PointerEventData, int, int> m_OnDragGanttEnd;

        /// <summary>
        /// the callback function of click gantt.
        /// ||点击甘特图柱条回调。参数：PointerEventData，SerieIndex，SerieDataIndex
        /// </summary>
        public System.Action<PointerEventData, int, int> onPointerClickGantt
        {
            set { m_OnPointerClickGantt = value; m_ForceOpenRaycastTarget = true; }
            get { return m_OnPointerClickGantt; }
        }
        public System.Action<PointerEventData, int, int> onDragGanttEnd
        {
            set { m_OnDragGanttEnd = value; m_ForceOpenRaycastTarget = true; }
            get { return m_OnDragGanttEnd; }
        }

        public bool runtimeBeginDrag { get; internal set; }
        public bool runtimeIsDragSerie { get; internal set; }
        public bool runtimeDragLeft { get; internal set; }
        public Serie runtimeDragSerie { get; internal set; }
        public SerieData runtimeDragSerieData { get; internal set; }
        public bool runtimeIsAlgining { get; internal set; }
        public double aliginingValue { get; internal set; }

        protected override void DefaultChart()
        {
            GetChartComponent<Title>().text = "GanttChart";
            var xCount = 5;

            EnsureChartComponent<GridCoord>();

            var xAxis = EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Time;
            xAxis.boundaryGap = false;
            xAxis.splitNumber = xCount;
            xAxis.animation.show = false;

            var yAxis = EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Category;
            yAxis.boundaryGap = true;
            yAxis.splitNumber = 0;

            EnsureChartComponent<GanttStyle>();

            RemoveData();
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default value axis gantt chart.
        /// || 生成默认的数值轴甘特图。
        /// </summary>
        public void DefaultValueAxisGanttChart()
        {
            CheckChartInit();
            var xAxis = EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Value;

            RemoveData();
            Gantt.AddDefaultValueSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default category axis gantt chart.
        /// || 生成默认的类目轴甘特图。
        /// </summary>
        public void DefaultCategoryAxisGanttChart()
        {
            CheckChartInit();
            var xAxis = EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            RemoveData();
            for (int i = 0; i < 10; i++)
            {
                xAxis.AddData("day" + i);
            }
            Gantt.AddDefaultCategorySerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default monomial group gantt chart.
        /// || 生成默认的单维度分组甘特图。
        /// </summary>
        public void DefaultMonomialGroupGanttChart()
        {
            CheckChartInit();
            var ganttStyle = GetChartComponent<GanttStyle>();
            ganttStyle.ganttType = GanttType.Monomial;
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default multinomial group gantt chart.
        /// || 生成默认的多维度分组甘特图。
        /// </summary>
        public void DefaultMultinomialGroupGanttChart()
        {
            CheckChartInit();
            var ganttStyle = GetChartComponent<GanttStyle>();
            ganttStyle.ganttType = GanttType.Multinomial;
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
            Gantt.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        public override void InitAxisRuntimeData(Axis axis)
        {
            if (axis.type != Axis.AxisType.Category) return;
            if (axis.data.Count > 0) return;
            axis.context.runtimeData.Clear();
            var serieCount = m_Series.Count;
            var style = GetChartComponent<GanttStyle>();
            for (int i = serieCount - 1; i >= 0; i--)
            {
                var serie = m_Series[i];
                if (!(serie is Gantt)) continue;
                if (serie.yAxisIndex != axis.index) continue;
                switch (style.ganttType)
                {
                    case GanttType.Monomial:
                        for (int j = serie.data.Count - 1; j >= 0; j--)
                        {
                            var serieData = serie.data[j];
                            axis.context.runtimeData.Add(serieData.show ? serieData.name : "");
                            axis.SetNeedUpdateFilterData();
                        }
                        break;
                    case GanttType.Multinomial:
                        axis.context.runtimeData.Add(serie.show ? serie.serieName : "");
                        axis.SetNeedUpdateFilterData();
                        break;
                }
            }
        }

        public override void GetSeriesMinMaxValue(Axis axis, int axisIndex, out double tempMinValue, out double tempMaxValue)
        {

            DataZoomHelper.UpdateDataZoomRuntimeStartEndValue<Gantt>(this);
            tempMinValue = float.MaxValue;
            tempMaxValue = float.MinValue;
            DataZoom xDataZoom = null, yDataZoom = null;
            foreach (var serie in m_Series)
            {
                if (!(serie is Gantt)) continue;
                if (serie.xAxisIndex != axis.index) continue;
                double xData, yData;
                GetDataZoomOfSerie(serie, out xDataZoom, out yDataZoom);
                if (axis.minMaxType == Axis.AxisMinMaxType.Custom && xDataZoom != null)
                {
                    xData = xDataZoom.context.startValue;
                    yData = xDataZoom.context.endValue;
                    if (xData < tempMinValue) tempMinValue = xData;
                    if (yData > tempMaxValue) tempMaxValue = yData;
                }
                else
                {
                    foreach (var serieData in serie.data)
                    {
                        if (serieData.data.Count >= 2)
                        {
                            if (xDataZoom != null)
                            {
                                xData = serieData.GetData(0, xDataZoom.context.startValue, xDataZoom.context.endValue);
                                yData = serieData.GetData(1, xDataZoom.context.startValue, xDataZoom.context.endValue);
                            }
                            else
                            {
                                xData = serieData.data[0];
                                yData = serieData.data[1];
                            }
                            if (xData < tempMinValue) tempMinValue = xData;
                            if (yData > tempMaxValue) tempMaxValue = yData;
                        }
                    }
                }
            }
            if (tempMinValue == float.MaxValue) tempMinValue = 0;
            if (tempMaxValue == float.MinValue) tempMaxValue = 0;
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            var style = GetChartComponent<GanttStyle>();
            if (!style.dragEnable || Input.touchCount > 1) return;
            runtimeBeginDrag = true;
            runtimeIsDragSerie = false;
            runtimeDragSerie = null;
            runtimeDragSerieData = null;
            foreach (var component in m_Components)
            {
                if (component is GridCoord)
                {
                    var grid = component as GridCoord;
                    if (grid.Contains(pointerPos))
                    {
                        foreach (var serie in m_Series)
                        {
                            if (!(serie is Gantt)) continue;
                            if ((serie as Gantt).disableDrag) continue;
                            if (!serie.show) continue;
                            foreach (var serieData in serie.data)
                            {
                                if (serieData.context.rect.Contains(pointerPos))
                                {
                                    switch (style.ganttType)
                                    {
                                        case GanttType.Monomial:
                                            runtimeIsDragSerie = false;
                                            runtimeDragSerie = serie;
                                            runtimeDragSerieData = serieData;
                                            break;
                                        case GanttType.Multinomial:
                                            runtimeIsDragSerie = true;
                                            runtimeDragSerie = serie;
                                            runtimeDragSerieData = serieData;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            var style = GetChartComponent<GanttStyle>();
            if (!style.dragEnable) return;
            if (runtimeDragSerie == null) return;
            XAxis xAxis;
            GridCoord grid;
            if (!TryGetChartComponent<XAxis>(out xAxis, runtimeDragSerie.xAxisIndex)) return;
            if (!TryGetChartComponent<GridCoord>(out grid, xAxis.gridIndex)) return;
            float deltaX = eventData.delta.x;
            float deltaPercent = deltaX / grid.context.width * 100;
            var value = xAxis.context.minMaxRange * deltaPercent;
            value *= style.dragSensitivity / 100;
            runtimeDragLeft = value < 0;
            if (runtimeIsDragSerie)
            {
                if (style.dragRangeMin > 0)
                {
                    var currValue = runtimeDragSerie.data[0].data[0];
                    if (currValue + value < style.dragRangeMin) value = style.dragRangeMin - currValue;
                }
                if (style.dragRangeMax > 0)
                {
                    var currValue = runtimeDragSerie.data[runtimeDragSerie.dataCount - 1].data[1];
                    if (currValue + value > style.dragRangeMax) value = style.dragRangeMax - currValue;
                }
                foreach (var serieData in runtimeDragSerie.data)
                {
                    for (int i = 0; i < serieData.data.Count; i++)
                    {
                        serieData.data[i] += value;
                    }
                }
            }
            else
            {
                if (style.dragRangeMin > 0)
                {
                    var currValue = runtimeDragSerieData.data[0];
                    if (currValue + value < style.dragRangeMin) value = style.dragRangeMin - currValue;
                }
                if (style.dragRangeMax > 0)
                {
                    var currValue = runtimeDragSerieData.data[1];
                    if (currValue + value > style.dragRangeMax) value = style.dragRangeMax - currValue;
                }
                for (int i = 0; i < runtimeDragSerieData.data.Count; i++)
                {
                    runtimeDragSerieData.data[i] += value;
                }
            }
            if (value != 0) RefreshPainter(runtimeDragSerie);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            var style = GetChartComponent<GanttStyle>();
            if (!style.dragEnable) return;
            if (style.autoAdsorbAlignline && runtimeIsAlgining && runtimeDragSerie != null && aliginingValue > 0)
            {
                if (runtimeIsDragSerie)
                {
                    var diff = aliginingValue - GetDragSerieData(style).data[0];
                    foreach (var serieData in runtimeDragSerie.data)
                    {
                        for (int i = 0; i < serieData.data.Count; i++)
                        {
                            serieData.data[i] += diff;
                        }
                    }
                }
                else
                {
                    var diff = aliginingValue - runtimeDragSerieData.data[0];
                    for (int i = 0; i < runtimeDragSerieData.data.Count; i++)
                    {
                        runtimeDragSerieData.data[i] += diff;
                    }
                }
                RefreshPainter(runtimeDragSerie);
            }
            if (m_OnDragGanttEnd != null && runtimeDragSerie != null)
            {
                m_OnDragGanttEnd(eventData, runtimeDragSerie.index, runtimeDragSerieData.index);
            }
            runtimeBeginDrag = false;
            runtimeIsDragSerie = false;
            runtimeDragSerie = null;
            runtimeDragSerieData = null;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (m_OnPointerClickGantt == null) return;

            foreach (var component in m_Components)
            {
                if (component is GridCoord)
                {
                    var grid = component as GridCoord;
                    if (grid.Contains(pointerPos))
                    {
                        foreach (var serie in m_Series)
                        {
                            if (!(serie is Gantt)) continue;
                            if (!serie.show) continue;
                            foreach (var serieData in serie.data)
                            {
                                if (serieData.context.rect.Contains(pointerPos))
                                {
                                    m_OnPointerClickGantt(eventData, serie.index, serieData.index);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void DrawPainterUpper(VertexHelper vh)
        {
            base.DrawPainterUpper(vh);
            DrawAlginLine(vh);
        }

        private void DrawAlginLine(VertexHelper vh)
        {
            var style = GetChartComponent<GanttStyle>();
            if (!style.dragEnable || !runtimeBeginDrag) return;
            var dragedSerieData = GetDragSerieData(style);
            runtimeIsAlgining = false;
            aliginingValue = 0;
            foreach (var serie in m_Series)
            {
                if (!(serie is Gantt)) continue;
                if (!serie.show) continue;
                var dataCount = serie.dataCount;
                var xAxis = GetChartComponent<XAxis>(serie.xAxisIndex);
                var grid = GetChartComponent<GridCoord>(xAxis.gridIndex);
                for (int i = 0; i < dataCount; i++)
                {
                    var item = serie.data[i];
                    if (DrawLine(vh, style, grid, item, dragedSerieData, i == dataCount - 1))
                    {
                        runtimeIsAlgining = true;
                        return;
                    }
                }
            }
        }

        private SerieData GetDragSerieData(GanttStyle style)
        {
            if (runtimeIsDragSerie)
            {
                for (int i = style.alignlineDataIndex; i >= 0; i--)
                {
                    var data = runtimeDragSerie.GetSerieData(i);
                    if (data != null) return data;
                }
                return null;
            }
            else
            {
                return runtimeDragSerieData;
            }
        }

        private bool DrawLine(VertexHelper vh, GanttStyle style, GridCoord grid, SerieData currData, SerieData dragData, bool isEndData)
        {
            if (currData == null || dragData == null) return false;
            var diff = Mathf.Abs(currData.context.rect.x - dragData.context.rect.x);
            if (dragData != currData && diff <= style.showAlignlineRange)
            {
                SetAliginingValue(style, currData.GetData(0));
                var sp = new Vector3(currData.context.rect.x, grid.context.y);
                var ep = new Vector3(currData.context.rect.x, grid.context.y + grid.context.height);
                if (style.aliginLine.show && grid.Contains(sp))
                {
                    ChartDrawer.DrawLineStyle(vh, style.aliginLine, sp, ep, theme.axis.splitLineColor,
                    theme.axis.splitLineWidth, theme.axis.splitLineType);
                }
                return true;
            }
            if (isEndData)
            {
                var alginX = currData.context.rect.xMax;
                diff = Mathf.Abs(alginX - dragData.context.rect.x);
                if (dragData != currData && diff <= style.showAlignlineRange)
                {
                    SetAliginingValue(style, currData.GetData(1));
                    var sp = new Vector3(alginX, grid.context.y);
                    var ep = new Vector3(alginX, grid.context.y + grid.context.height);
                    if (style.aliginLine.show && grid.Contains(sp))
                    {
                        ChartDrawer.DrawLineStyle(vh, style.aliginLine, sp, ep, theme.axis.splitLineColor,
                            theme.axis.splitLineWidth, theme.axis.splitLineType);
                    }
                    return true;
                }
            }
            return false;
        }

        private void SetAliginingValue(GanttStyle style, double value)
        {
            if (style.dragRangeMin != 0 || style.dragRangeMax != 0)
            {
                if (style.dragRangeMin != 0 && value > style.dragRangeMin) aliginingValue = value;
                if (style.dragRangeMax != 0 && value < style.dragRangeMax) aliginingValue = value;
            }
            else
            {
                aliginingValue = value;
            }
        }
    }
}