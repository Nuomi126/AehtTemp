using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Graphs
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class GraphHandler : SerieHandler<Graph>
    {
        private ForceControl m_ForceControl = new ForceControl();

        public override void InitComponent()
        {
            base.InitComponent();
            InitAndUpdateGraph();
        }

        public override void Update()
        {
            base.Update();
            if (serie.m_ClearDataFlag)
            {
                InitAndUpdateGraph();
                serie.m_ClearDataFlag = false;
            }
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
            var param = paramList[0];
            var itemStyle = serie.GetLevelItemStyle(param.serieData.context.level);
            SerieHelper.GetColor(ref param.color, itemStyle.color, itemStyle.color, itemStyle.opacity, chart.theme, param.serieData.index);
        }

        public override void UpdateSerieContext()
        {
            var needCheck = m_LegendEnter || (chart.isPointerInChart && serie.context.rect.Contains(chart.pointerPos));
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
                    }
                    chart.RefreshPainter(serie);
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;
            serie.context.pointerEnter = false;
            var lastPointerItemDataIndex = serie.context.pointerItemDataIndex;
            var dataIndex = GetPointerItemDataIndex(chart.pointerPos);
            for (int i = 0; i < serie.data.Count; i++)
            {
                var serieData = serie.data[i];
                if (dataIndex == i || (m_LegendEnter && m_LegendEnterIndex == i))
                {
                    serie.context.pointerItemDataIndex = i;
                    serieData.context.highlight = true;
                    serie.context.pointerEnter = true;
                }
                else
                {
                    serieData.context.highlight = false;
                }
            }

            if (lastPointerItemDataIndex != serie.context.pointerItemDataIndex)
            {
                var serieData = serie.GetSerieData(serie.context.pointerItemDataIndex);
                var node = serie.m_GraphData.GetNode(serieData.context.graphNodeId);
                foreach (var edge in node.edges)
                {
                    var node1 = edge.node1;
                    var node2 = edge.node2;
                    var serieData1 = serie.GetSerieData(node1.dataIndex);
                    var serieData2 = serie.GetSerieData(node2.dataIndex);
                    serieData1.context.highlight = true;
                    serieData2.context.highlight = true;
                }
                needInteract = true;
            }
            if (needInteract)
            {
                chart.RefreshPainter(serie);
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
        }

        public override void OnDataUpdate()
        {
            m_ForceControl.ResetInterations(serie, serie.m_GraphData);
            chart.RefreshPainter(serie);
        }

        private void InitAndUpdateGraph()
        {
            //if (m_ForceControl.CheckNeedUpdate(serie, serie.m_GraphData))
            {
                serie.m_GraphData.Clear();
                foreach (var serieData in serie.data)
                {
                    var nodeId = string.IsNullOrEmpty(serieData.id) ? serieData.name : serieData.id;
                    var node = serie.m_GraphData.AddNode(nodeId, serieData.name, serieData.index, serieData.GetData(1));
                    serieData.context.graphNodeId = node.id;
                }
                var edgeLength = serie.force.edgeLength;
                foreach (var dataLink in serie.links)
                {
                    var edge = serie.m_GraphData.AddEdge(dataLink.source, dataLink.target, dataLink.value);
                    if (edge != null)
                    {
                        edge.distance = edgeLength;
                    }
                }
                m_ForceControl.Init(serie, serie.m_GraphData);
            }
        }

        public override void DrawSerie(VertexHelper vh)
        {
            SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            m_ForceControl.Update(serie, serie.m_GraphData);
            if (!ChartHelper.IsClearColor(serie.itemStyle.backgroundColor))
            {
                var color = serie.itemStyle.backgroundColor;
                var isYAxis = false;
                UGL.DrawRoundRectangle(vh, serie.context.rect, color, color, 0,
                    serie.itemStyle.cornerRadius, isYAxis, chart.settings.cicleSmoothness, false);
            }

            if (serie.lineStyle.show && serie.lineStyle.type != LineStyle.Type.None)
            {
                var lineType = serie.lineStyle.type;
                var lineWidth = serie.lineStyle.GetWidth(chart.theme.serie.lineWidth / 2);
                var lineColor = SerieHelper.GetLineColor(serie, null, chart.theme, serie.context.colorIndex);
                foreach (var edge in serie.m_GraphData.edges)
                {
                    var node1 = edge.node1;
                    var node2 = edge.node2;
                    var serieData1 = serie.GetSerieData(node1.dataIndex);
                    var serieData2 = serie.GetSerieData(node2.dataIndex);
                    var highlight = serieData1.context.highlight || serieData2.context.highlight;
                    if (highlight)
                    {
                        var lineColor2 = ChartHelper.GetHighlightColor(lineColor);
                        ChartDrawer.DrawLineStyle(vh, lineType, lineWidth, node1.position, node2.position, lineColor2,
                            lineColor);
                    }
                    else
                    {
                        ChartDrawer.DrawLineStyle(vh, lineType, lineWidth, node1.position, node2.position, lineColor,
                            lineColor);
                    }
                }
            }

            var itemStyle = serie.itemStyle;
            var maxCount = serie.dataCount;
            float symbolBorder = 0f;
            float[] cornerRadius = null;
            var emptyColor = itemStyle.backgroundColor;
            foreach (var node in serie.m_GraphData.nodes)
            {
                var serieData = serie.data[node.dataIndex];
                var color = ColorUtil.clearColor32;
                var toColor = ColorUtil.clearColor32;

                var borderColor = ColorUtil.clearColor32;

                var symbol = SerieHelper.GetSerieSymbol(serie, serieData);
                if (!symbol.ShowSymbol(serieData.index, maxCount))
                    continue;

                var state = SerieHelper.GetSerieState(serie, serieData, true);
                if (serieData.show && node.IsAnyInEdgesExpanded())
                {
                    serieData.context.canShowLabel = true;
                    SerieHelper.GetColor(ref color, itemStyle.color, itemStyle.color, itemStyle.opacity, chart.theme, serieData.index);
                    SerieHelper.GetColor(ref toColor, itemStyle.toColor, color, itemStyle.opacity, chart.theme, serieData.index);
                    SerieHelper.GetSymbolInfo(out borderColor, out symbolBorder, out cornerRadius, serie, serieData, chart.theme, state);
                    if (serieData.context.highlight)
                    {
                        color = ChartHelper.GetHighlightColor(color);
                        toColor = ChartHelper.GetHighlightColor(toColor);
                    }
                }
                else
                {
                    serieData.context.canShowLabel = false;
                }
                var symbolSize = SerieHelper.GetSysmbolSize(serie, serieData, chart.theme.serie.scatterSymbolSize, state);
                serieData.context.outsideRadius = symbolSize;
                chart.DrawSymbol(vh, symbol.type, symbolSize, symbolBorder, serieData.context.position,
                    color, toColor, emptyColor, borderColor, symbol.gap, cornerRadius);
            }

            if (!m_ForceControl.finished)
            {
                chart.RefreshPainter(serie);
            }
        }

        private int GetPointerItemDataIndex(Vector2 local)
        {
            foreach (var serieData in serie.data)
            {
                var dist = Vector2.Distance(local, serieData.context.position);
                if (dist < serieData.context.outsideRadius)
                {
                    return serieData.index;
                }
            }
            return -1;
        }
    }
}