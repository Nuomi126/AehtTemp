using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using XUGL;

namespace XCharts.Runtime.Sankeys
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class SankeyHandler : SerieHandler<Sankey>
    {
        private GraphData m_Graph;
        private List<List<GraphNode>> m_DepthNodes;
        private bool m_ExpandFlag = false;

        public override void InitComponent()
        {
            base.InitComponent();
            m_ExpandFlag = false;
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
            var param = paramList[0];
            var itemStyle = GetItemStyle(param.serieData.context.level);
            SerieHelper.GetColor(ref param.color, itemStyle.color, itemStyle.color, itemStyle.opacity, chart.theme, param.serieData.index);
        }

        public override void UpdateSerieContext()
        {
            var needCheck = m_LegendEnter || chart.isPointerInChart;
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
                }
                else
                {
                    serieData.context.highlight = false;
                }
            }
            if (m_Graph != null)
            {
                var lastHighlight = false;
                foreach (var edge in m_Graph.edges)
                {
                    if (edge.highlight)
                    {
                        if (edge.IsPointInEdge(chart.pointerPos))
                        {
                            lastHighlight = true;
                            needInteract = true;
                        }
                        else
                        {
                            edge.highlight = false;
                            needInteract = true;
                        }
                        break;
                    }
                }
                if (!lastHighlight)
                {
                    foreach (var edge in m_Graph.edges)
                    {
                        if (edge.IsPointInEdge(chart.pointerPos))
                        {
                            edge.highlight = true;
                            needInteract = true;
                        }
                        else
                        {
                            edge.highlight = false;
                        }
                    }
                }
            }

            if (lastPointerItemDataIndex != serie.context.pointerItemDataIndex)
            {
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
            if (!serie.collapse) return;
            var serieData = serie.GetSerieData(serie.context.pointerItemDataIndex);
            if (serieData != null)
            {
                var nodeLevel = serieData.context.level;
                if (serie.collapseLevel >= 0 && nodeLevel != serie.collapseLevel) return;
                m_ExpandFlag = true;
                var node = m_Graph.GetNode(serieData.context.graphNodeId);
                if (node != null)
                {
                    if (serie.collapseMutex)
                    {
                        m_Graph.ExpandAllNodes(false, nodeLevel);
                        node.Expand(true);
                    }
                    else
                    {
                        node.Expand(!node.expand);
                    }
                }
            }
        }

        public override void OnDataUpdate()
        {
            InitAndUpdateGraph();
        }

        private void InitAndUpdateGraph(bool firstInit = false)
        {
            if (m_Graph == null)
            {
                m_Graph = new GraphData(true);
            }
            else if (firstInit)
            {
                return;
            }
            m_Graph.Clear();
            foreach (var serieData in serie.data)
            {
                var node = m_Graph.AddNode(serieData.name, serieData.name, serieData.index, serieData.GetData(1));
                serieData.context.graphNodeId = node.id;
            }
            foreach (var dataLink in serie.links)
            {
                m_Graph.AddEdge(dataLink.source, dataLink.target, dataLink.value);
            }
            foreach (var node in m_Graph.nodes)
            {
                serie.UpdateYData(node.dataIndex, node.totalValues);
            }
            m_DepthNodes = m_Graph.GetDepthNodes();
        }

        private void InitDefaultExpandNode()
        {
            if (m_ExpandFlag) return;
            if (serie.defaultExpandDataIndex >= 0 && serie.collapse && serie.collapseMutex)
            {
                var serieData = serie.GetSerieData(serie.defaultExpandDataIndex);
                if (serieData != null)
                {
                    var nodeLevel = serieData.context.level;
                    m_Graph.ExpandAllNodes(false, nodeLevel);
                    m_Graph.ExpandNode(serieData.context.graphNodeId, true);
                }
            }
        }

        public override void DrawSerie(VertexHelper vh)
        {
            SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            if (!ChartHelper.IsClearColor(serie.itemStyle.backgroundColor))
            {
                var color = serie.itemStyle.backgroundColor;
                var cornerRadius = serie.itemStyle.cornerRadius;
                var isYAxis = false;
                UGL.DrawRoundRectangle(vh, serie.context.rect, color, color, 0,
                    cornerRadius, isYAxis, chart.settings.cicleSmoothness, false);
            }
            InitAndUpdateGraph(true);
            var maxDepth = m_DepthNodes.Count;
            var eachColumnDiff = (serie.context.width - maxDepth * serie.nodeWidth) / (maxDepth - 1);

            //set node position
            var currentX = serie.context.x;
            var nodeCount = 0;
            var eachValueLength = 0f;
            for (int i = 0; i < maxDepth; i++)
            {
                var nodeList = m_DepthNodes[i];
                var expandNodeCount = GraphData.GetExpandedNodesCount(nodeList);
                var totalValue = GraphData.GetNodesTotalValue(nodeList);
                var totalHeight = (float)totalValue * eachValueLength;
                var currentY = serie.context.y + serie.context.height;
                if (i == 0)
                {
                    eachValueLength = (float)((serie.context.height - (expandNodeCount - 1) * serie.nodeGap) / totalValue);
                }
                else
                {
                    switch (serie.nodeAlign)
                    {
                        case Sankey.NodeAlign.Top:
                            break;
                        case Sankey.NodeAlign.Bottom:
                            currentY -= serie.context.height - totalHeight - (expandNodeCount - 1) * serie.nodeGap;
                            break;
                        case Sankey.NodeAlign.Center:
                            currentY -= (serie.context.height - totalHeight - (expandNodeCount - 1) * serie.nodeGap) / 2;
                            break;
                    }
                }
                for (int j = 0; j < nodeList.Count; j++)
                {
                    var node = nodeList[j];
                    node.level = i;
                    var isExpanded = node.IsAnyInEdgesExpanded();

                    var nodeHight = isExpanded ? (float)node.totalValues * eachValueLength : 0;
                    var rect = new Rect(currentX, currentY - nodeHight, serie.nodeWidth, nodeHight);

                    currentY -= nodeHight + (isExpanded ? serie.nodeGap : 0);
                    nodeCount++;

                    var serieData = serie.data[node.dataIndex];
                    serieData.context.level = i;
                    serieData.context.inTotalValue = 0;
                    serieData.context.outTotalValue = 0;
                    serieData.context.rect = rect;
                    serieData.context.position = new Vector3(rect.x + rect.width / 2, rect.y + rect.height / 2);
                    serieData.context.labelPosition = serieData.context.position + new Vector3(rect.width / 2, 0);
                }
                currentX += eachColumnDiff + serie.nodeWidth;
            }
            InitDefaultExpandNode();

            //draw line

            for (int i = 0; i < maxDepth; i++)
            {
                var nodeList = m_DepthNodes[i];
                var lineStyle = serie.GetLevelLineStyle(i);
                for (int j = 0; j < nodeList.Count; j++)
                {
                    var sourceNode = nodeList[j];
                    var nodeTotalValue = sourceNode.totalValues;
                    var sourceSerieData = serie.data[sourceNode.dataIndex];
                    var itemStyle = GetItemStyle(i, sourceSerieData);
                    foreach (var edge in sourceNode.outEdges)
                    {
                        var targetNode = edge.node2;
                        var targetSerieData = serie.data[targetNode.dataIndex];
                        var color = ColorUtil.clearColor32;
                        var toColor = ColorUtil.clearColor32;
                        Vector3 startTopPos, startBotPos, endTopPos, endBotPos;
                        GetOutTopAndBotPosition(edge.value, sourceSerieData, eachValueLength, out startTopPos, out startBotPos);
                        GetInTopAndBotPosition(edge.value, targetSerieData, eachValueLength, out endTopPos, out endBotPos);
                        if (sourceSerieData.show && edge.expand)
                        {
                            sourceSerieData.context.canShowLabel = true;
                            var defaultColor = ChartHelper.IsClearColor(itemStyle.color) ?
                                    chart.theme.GetColor(sourceSerieData.index) : itemStyle.color;
                            if (serie.lineGradient)
                            {
                                color = defaultColor;
                                var targetItemStyle = GetItemStyle(targetSerieData.context.level, targetSerieData);
                                toColor = ChartHelper.IsClearColor(targetItemStyle.color) ?
                                    chart.theme.GetColor(targetSerieData.index) : targetItemStyle.color;
                                ChartHelper.SetColorOpacity(ref color, lineStyle.opacity);
                                ChartHelper.SetColorOpacity(ref toColor, lineStyle.opacity);
                            }
                            else
                            {
                                color = lineStyle.GetColor(defaultColor);
                                toColor = color;
                            }
                            if (edge.highlight || IsEdgeHighlight(edge))
                            {
                                color = ChartHelper.GetHighlightColor(color);
                                toColor = ChartHelper.GetHighlightColor(toColor);
                            }
                        }
                        else
                        {
                            sourceSerieData.context.canShowLabel = false;
                        }

                        UpdateBezierPoints(ref edge.upPoints, startTopPos, endTopPos, serie.curveness, chart.settings.lineSmoothness);
                        UpdateBezierPoints(ref edge.downPoints, startBotPos, endBotPos, serie.curveness, chart.settings.lineSmoothness);

                        UGL.DrawEdge(vh, edge.upPoints, edge.downPoints, color, toColor, UGL.Direction.XAxis);
                    }
                }
            }

            //draw node
            for (int i = 0; i < maxDepth; i++)
            {
                var nodeList = m_DepthNodes[i];
                for (int j = 0; j < nodeList.Count; j++)
                {
                    var sourceNode = nodeList[j];
                    var serieData = serie.data[sourceNode.dataIndex];
                    var color = ColorUtil.clearColor32;
                    var toColor = ColorUtil.clearColor32;
                    var itemStyle = GetItemStyle(i, serieData);
                    if (serieData.show && sourceNode.IsAnyInEdgesExpanded())
                    {
                        serieData.context.canShowLabel = true;
                        serieData.context.level = i;
                        SerieHelper.GetColor(ref color, itemStyle.color, itemStyle.color, itemStyle.opacity, chart.theme, serieData.index);
                        SerieHelper.GetColor(ref toColor, itemStyle.toColor, color, itemStyle.opacity, chart.theme, serieData.index);
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
                    UGL.DrawRoundRectangle(vh, serieData.context.rect, color, toColor, 0,
                        serie.itemStyle.cornerRadius, false, chart.settings.cicleSmoothness, false);
                }
            }
        }

        private static void UpdateBezierPoints(ref List<Vector3> posList, Vector3 startTopPos, Vector3 endTopPos, float curveness, float lineSmoothness)
        {
            var diff = (endTopPos.x - startTopPos.x) * (curveness == 0 ? 0.5f : curveness);
            var cp1 = startTopPos + new Vector3(diff, 0);
            var cp2 = endTopPos + new Vector3(-diff, 0);

            var dist = Vector3.Distance(startTopPos, endTopPos);
            var segment = (int)(dist / (lineSmoothness <= 0 ? 2f : lineSmoothness));
            UGLHelper.GetBezierList2(ref posList, startTopPos, endTopPos, segment, cp1, cp2);
        }

        private bool IsEdgeHighlight(GraphEdge edge)
        {
            var serieData = serie.data[edge.node1.dataIndex];
            var serieData2 = serie.data[edge.node2.dataIndex];
            return serieData.context.highlight || serieData2.context.highlight;
        }

        private void GetOutTopAndBotPosition(double value, SerieData serieData, float eachValueHeight, out Vector3 top, out Vector3 bot)
        {
            top = new Vector3(serieData.context.rect.x + serieData.context.rect.width,
                serieData.context.rect.y + serieData.context.rect.height - (float)serieData.context.outTotalValue * eachValueHeight);
            bot = new Vector3(serieData.context.rect.x + serieData.context.rect.width,
                serieData.context.rect.y + serieData.context.rect.height - (float)(serieData.context.outTotalValue + value) * eachValueHeight);
            serieData.context.outTotalValue += value;
        }

        private void GetInTopAndBotPosition(double value, SerieData serieData, float eachValueHeight, out Vector3 top, out Vector3 bot)
        {
            top = new Vector3(serieData.context.rect.x,
                serieData.context.rect.y + serieData.context.rect.height - (float)serieData.context.inTotalValue * eachValueHeight);
            bot = new Vector3(serieData.context.rect.x,
                serieData.context.rect.y + serieData.context.rect.height - (float)(serieData.context.inTotalValue + value) * eachValueHeight);
            serieData.context.inTotalValue += value;
        }

        private int GetPointerItemDataIndex(Vector2 local)
        {
            foreach (var serieData in serie.data)
            {
                if (serieData.context.rect.Contains(local))
                {
                    return serieData.index;
                }
            }
            return -1;
        }

        private static bool IsPointInCurveRect(List<Vector3> curves, float curveWidth, Vector2 local)
        {
            for (int i = 0; i < curves.Count - 1; i++)
            {
                var p1 = curves[i];
                var p2 = curves[i + 1];
                if (local.x > p1.x && local.x < p2.x && local.y > p1.y - curveWidth && local.y < p2.y + curveWidth)
                {
                    return true;
                }
            }
            return false;
        }

        private ItemStyle GetItemStyle(int level, SerieData serieData = null)
        {
            if (serieData != null && serieData.itemStyle != null)
            {
                return serieData.itemStyle;
            }
            return serie.GetLevelItemStyle(level);
        }
    }
}