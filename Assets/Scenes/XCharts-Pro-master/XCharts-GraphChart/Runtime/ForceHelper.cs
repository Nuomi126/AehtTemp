using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace XCharts.Runtime.Graphs
{
    internal class ForceHelper
    {
        // private double k = 0;
        private float initialFriction;
        private float friction;
        private bool inited = false;
        internal bool finished = false;
        //private Dictionary<string, Vector3> preservedPoints = new Dictionary<string, Vector3>();

        public ForceHelper()
        {
        }

        public void Init(Graph serie, GraphData graph)
        {
            if (graph == null) return;
            var width = serie.context.width;
            var height = serie.context.height;
            var nodeCount = graph.nodes.Count;
            var center = serie.context.center;

            // k = Math.Sqrt(width * height / (double)nodeCount);
            initialFriction = serie.force.friction <= 0 ? 0.6f : serie.force.friction;
            friction = initialFriction;
            finished = false;

            var rangeMinX = center.x - width * 0.4f;
            var rangeMaxX = center.x + width * 0.4f;
            var rangeMinY = center.y - height * 0.4f;
            var rangeMaxY = center.y + height * 0.4f;
            for (int i = 0; i < nodeCount; i++)
            {
                var node = graph.nodes[i];
                node.weight = serie.force.repulsion;
                node.repulsion = serie.force.repulsion;
                if (i == 0)
                {
                    node.position = center;
                }
                else
                {
                    node.position.x = UnityEngine.Random.Range(rangeMinX, rangeMaxX);
                    node.position.y = UnityEngine.Random.Range(rangeMinY, rangeMaxY);
                    //node.position = center;
                }
                node.pp = node.position;
            }
            foreach (var edge in graph.edges)
            {
                edge.distance = serie.force.edgeLength;
            }
            inited = true;
        }

        public void Step(Graph serie, GraphData graph)
        {
            if (finished) return;
            if (graph == null || !inited) return;
            var center = serie.context.center;
            var gravity = serie.force.gravity;
            var nodeCount = graph.nodes.Count;
            var v12 = Vector3.zero;
            BeforceStep(serie, graph);
            for (int i = 0; i < graph.edges.Count; i++)
            {
                var edge = graph.edges[i];
                var node1 = edge.node1;
                var node2 = edge.node2;
                v12 = node1.position - node2.position;
                var d = v12.magnitude - edge.distance;
                var wt = node1.weight + node2.weight;
                var w = wt == 0 ? 0 : node2.weight / wt;
                v12 = v12.normalized;
                d /= 2f;
                node1.position = node1.position + v12 * w * d * friction;
                node2.position = node2.position + v12 * (-(1 - w) * d * friction);
            }

            for (int i = 0; i < nodeCount; i++)
            {
                var node = graph.nodes[i];
                v12 = node.position - center;
                node.position = node.position + v12 * (gravity * friction);
            }

            for (int i = 0; i < nodeCount; i++)
            {
                var node1 = graph.nodes[i];
                for (int j = i + 1; j < nodeCount; j++)
                {
                    var node2 = graph.nodes[j];
                    v12 = node1.position - node2.position;
                    var d = v12.magnitude;
                    if (d == 0)
                    {
                        v12 = new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), 0);
                        d = 1;
                    }
                    var repFact = (node1.repulsion + node2.repulsion) / d / d;
                    node1.pp = node1.pp + v12 * repFact;
                    node2.pp = node2.pp - v12 * repFact;
                }
            }

            for (int i = 0; i < nodeCount; i++)
            {
                var node = graph.nodes[i];
                v12 = node.pp - node.position;
                node.position = node.position + v12 * friction;
                node.pp = node.position;
            }

            friction *= 0.992f;
            finished = friction < 0.01f;
            AfterStep(serie, graph);
        }

        private void BeforceStep(Graph serie, GraphData graph)
        {

        }

        private void AfterStep(Graph serie, GraphData graph)
        {
            // for (int i = 0; i < graph.edges.Count; i++)
            // {
            //     var edge = graph.edges[i];
            //     var node1 = edge.node1;
            //     var node2 = edge.node2;
            // }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="var"></param>
        /// <param name="domainStart"></param>
        /// <param name="domainEnd"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="clamp"></param>
        /// <returns></returns> <summary>
        public static float LinearMap(float val, float domainStart, float domainEnd, float rangeStart, float rangeEnd, bool clamp = false)
        {
            var subDomain = domainEnd - domainStart;
            var subRange = rangeEnd - rangeStart;
            if (subDomain == 0)
            {
                return subRange == 0 ? rangeStart : (rangeStart + rangeEnd) / 2;
            }
            if (clamp)
            {
                if (subDomain > 0)
                {
                    if (val <= domainStart)
                    {
                        return rangeStart;
                    }
                    else if (val >= domainEnd)
                    {
                        return rangeEnd;
                    }
                }
                else
                {
                    if (val >= domainStart)
                    {
                        return rangeStart;
                    }
                    else if (val <= domainEnd)
                    {
                        return rangeEnd;
                    }
                }
            }
            else
            {
                if (val == domainStart)
                {
                    return rangeStart;
                }
                if (val == domainEnd)
                {
                    return rangeEnd;
                }
            }

            return (val - domainStart) / subDomain * subRange + rangeStart;
        }
    }
}