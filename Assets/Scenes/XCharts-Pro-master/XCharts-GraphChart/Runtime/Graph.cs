using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Graphs
{
    /// <summary>
    /// Graph chart component. It is used for showing the relationship between nodes.
    /// ||关系图，用于展现节点以及节点之间的关系数据。
    /// </summary>
    [System.Serializable]
    [SerieHandler(typeof(GraphHandler), true)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle))]
    [SerieDataExtraField("m_Id")]
    public class Graph : Serie
    {
        [SerializeField] private LevelStyle m_LevelStyle = new LevelStyle();
        [SerializeField] private Force m_Force = new Force();

        internal GraphData m_GraphData = new GraphData(false);
        internal bool m_ClearDataFlag = false;

        /// <summary>
        /// the style of level.
        /// ||层级样式。
        /// </summary>
        public LevelStyle levelStyle { get { return m_LevelStyle; } }
        /// <summary>
        /// the force layout of the graph.
        /// ||图表的力引导布局。
        /// </summary>
        public Force force { get { return m_Force; } }

        public GraphData graphData { get { return m_GraphData; } }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public override void ClearData()
        {
            base.ClearData();
            m_ClearDataFlag = true;
        }

        public override void AddSerieData(SerieData serieData)
        {
            base.AddSerieData(serieData);
            var nodeId = string.IsNullOrEmpty(serieData.id) ? serieData.name : serieData.id;
            var node = graphData.AddNode(nodeId, serieData.name, serieData.index, serieData.GetData(1));
            serieData.context.graphNodeId = node.id;
        }

        public override SerieDataLink AddLink(string sourceId, string targetId, double value = 0)
        {
            var dataLink = base.AddLink(sourceId, targetId, value);
            if (dataLink != null)
            {
                var edge = graphData.AddEdge(dataLink.source, dataLink.target, dataLink.value);
                if (edge != null)
                {
                    edge.distance = m_Force.edgeLength;
                }
            }
            return dataLink;
        }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Graph>(serieName);

            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Circle;

            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.position = LabelStyle.Position.Outside;
            label.textStyle.autoAlign = false;
            label.textStyle.alignment = TextAnchor.MiddleCenter;
            label.formatter = "{b}";

            serie.lineStyle.show = true;

            serie.left = 0.1f;
            serie.right = 0.1f;
            serie.top = 0.2f;
            serie.bottom = 0.1f;

            serie.showDataName = true;
            serie.showDataDimension = 1;

            chart.AddData(0, Random.Range(5, 10), "a", "a");
            chart.AddData(0, Random.Range(5, 10), "b", "b");
            chart.AddData(0, Random.Range(5, 10), "a1", "a1");
            chart.AddData(0, Random.Range(5, 10), "a2", "a2");
            chart.AddData(0, Random.Range(5, 10), "b1", "b1");
            chart.AddData(0, Random.Range(5, 10), "c", "c");

            chart.AddLink(0, "a", "a1");
            chart.AddLink(0, "a", "a2");
            chart.AddLink(0, "b", "b1");
            chart.AddLink(0, "a", "b1");
            chart.AddLink(0, "b1", "a1");
            chart.AddLink(0, "b1", "c");
        }

        public ItemStyle GetLevelItemStyle(int depth)
        {
            if (m_LevelStyle.show)
            {
                var allDepthZero = true;
                foreach (var level in m_LevelStyle.levels)
                {
                    if (level.depth > 0) allDepthZero = false;
                    if (level.depth == depth) return level.itemStyle;
                }
                if (allDepthZero && depth < m_LevelStyle.levels.Count) return m_LevelStyle.levels[depth].itemStyle;
            }
            return itemStyle;
        }

        public LabelStyle GetLevelLabel(int depth)
        {
            if (m_LevelStyle.show)
            {
                var allDepthZero = true;
                foreach (var level in m_LevelStyle.levels)
                {
                    if (level.depth > 0) allDepthZero = false;
                    if (level.depth == depth) return level.label;
                }
                if (allDepthZero && depth < m_LevelStyle.levels.Count) return m_LevelStyle.levels[depth].label;
            }
            return label;
        }

        public LineStyle GetLevelLineStyle(int depth)
        {
            if (m_LevelStyle.show)
            {
                var allDepthZero = true;
                foreach (var level in m_LevelStyle.levels)
                {
                    if (level.depth > 0) allDepthZero = false;
                    if (level.depth == depth) return level.lineStyle;
                }
                if (allDepthZero && depth < m_LevelStyle.levels.Count) return m_LevelStyle.levels[depth].lineStyle;
            }
            return lineStyle;
        }
    }
}