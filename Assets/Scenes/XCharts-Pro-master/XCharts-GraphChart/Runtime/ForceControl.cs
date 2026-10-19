using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace XCharts.Runtime.Graphs
{
    internal class ForceControl
    {
        private float minX, maxX, minY, maxY;
        private int iterations;
        public bool finished = false;

        public bool CheckNeedUpdate(Graph serie, GraphData graph)
        {
            if (serie == null || graph == null) return true;
            if (serie.dataCount != graph.nodes.Count) return true;
            if (serie.links.Count != graph.edges.Count) return true;
            for (int i = 0; i < serie.dataCount; i++)
            {
                var serieData = serie.data[i];
                var node = graph.nodes[i];
                if (node.value != serieData.GetData(1)) return true;
            }
            return false;
        }

        public void Init(Graph serie, GraphData graph)
        {
            ResetInterations(serie, graph);
            ResetDefaultPosition(serie, graph);
        }

        public void ResetInterations(Graph serie, GraphData graph)
        {
            if (serie == null || graph == null) return;
            iterations = 0;
            finished = false;
            serie.force.currentIterations = 0;
            minX = serie.context.rect.x;
            maxX = serie.context.rect.x + serie.context.rect.width;
            minY = serie.context.rect.y;
            maxY = serie.context.rect.y + serie.context.rect.height;
        }

        public void ResetDefaultPosition(Graph serie, GraphData graph)
        {
            var nodeCount = graph.nodes.Count;
            var center = serie.context.center;
            var radius = 10f;
            var diff = 360f / (nodeCount - 1);
            var angle = 0f;
            var force = serie.force;
            var centerIndex = force.centerDataIndex >= 0 && force.centerDataIndex < nodeCount ?
                force.centerDataIndex :
                0;
            for (int i = 0; i < nodeCount; i++)
            {
                var node = graph.nodes[i];
                node.weight = force.repulsion;
                node.repulsion = force.repulsion;
                if (node.dataIndex == centerIndex)
                {
                    node.position = center;
                }
                else
                {
                    node.position = ChartHelper.GetPos(center, radius, angle, true);
                    angle += diff;
                }
            }
        }

        public void Update(Graph serie, GraphData graph)
        {
            if (finished) return;
            if (graph.IsAllNodeInZeroPosition())
            {
                Init(serie, graph);
            }
            if (serie.animation.IsFinish())
            {
                var iterations = serie.force.iterations / 2;
                for (int i = 0; i < iterations; i++)
                {
                    Step(serie, graph);
                }
            }
            else
            {
                for (int i = 0; i < serie.force.stepCount; i++)
                    Step(serie, graph);
            }
        }

        public void Step(Graph serie, GraphData graph)
        {
            var k = Mathf.Sqrt(serie.context.width * serie.context.height / (float)graph.nodes.Count);
            var nodeCount = graph.nodes.Count;
            float distX, distY, dist;
            var force = serie.force;
            var edgeLength = force.edgeLength;
            var repulsion = force.repulsion;
            for (int i = 0; i < nodeCount; i++)
            {
                var node1 = graph.nodes[i];
                node1.pp.x = 0;
                node1.pp.y = 0;
                for (int j = 0; j < nodeCount; j++)
                {
                    if (j != i)
                    {
                        var node2 = graph.nodes[j];
                        distX = node1.position.x - node2.position.x;
                        distY = node1.position.y - node2.position.y;
                        dist = Mathf.Sqrt(distX * distX + distY * distY);
                        if (dist > 0 && dist < edgeLength)
                        {
                            node1.pp.x = node1.pp.x + distX / dist * k * k / dist * repulsion;
                            node1.pp.y = node1.pp.y + distY / dist * k * k / dist * repulsion;
                        }
                    }
                }
            }

            for (int i = 0; i < graph.edges.Count; i++)
            {
                var edge = graph.edges[i];
                var node1 = edge.node1;
                var node2 = edge.node2;

                distX = node1.position.x - node2.position.x;
                distY = node1.position.y - node2.position.y;
                dist = Mathf.Sqrt(distX * distX + distY * distY);
                var diff = dist / k * force.gravity;
                node1.pp.x = node1.pp.x - distX * diff;
                node1.pp.y = node1.pp.y - distY * diff;
                node2.pp.x = node2.pp.x + distX * diff;
                node2.pp.y = node2.pp.y + distY * diff;
            }

            // int maxt = 4, maxty = 3;
            var stepFactor = force.stepFactor;
            var centerIndex = force.centerDataIndex >= 0 && force.centerDataIndex < nodeCount ?
                force.centerDataIndex :
                -1;
            for (int i = 0; i < nodeCount; i++)
            {
                var node = graph.nodes[i];
                var dx = node.pp.x;
                var dy = node.pp.y;

                if (dx < -stepFactor) dx = -stepFactor;
                if (dx > stepFactor) dx = stepFactor;
                if (dy < -stepFactor) dy = -stepFactor;
                if (dy > stepFactor) dy = stepFactor;

                var x = (node.position.x + dx) >= maxX || (node.position.x + dx) <= minX ? node.position.x - dx : node.position.x + dx;
                var y = (node.position.y + dy) >= maxY || (node.position.y + dy <= minY) ? node.position.y - dy : node.position.y + dy;

                if (node.dataIndex != centerIndex)
                {
                    if (x < minX) x = minX;
                    if (x > maxX) x = maxX;
                    if (y < minY) y = minY;
                    if (y > maxY) y = maxY;
                    node.position.x = x;
                    node.position.y = y;
                }

                var serieData = serie.data[node.dataIndex];
                serieData.context.position = node.position;
                serieData.context.labelPosition = serieData.context.position;
            }
            iterations++;
            finished = iterations >= force.iterations;
            serie.force.currentIterations = iterations;
        }
    }
}