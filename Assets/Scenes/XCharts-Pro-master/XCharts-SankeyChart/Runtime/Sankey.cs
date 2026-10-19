using UnityEngine;
using UnityEngine.UI;
using XUGL;
using System.Collections.Generic;

namespace XCharts.Runtime.Sankeys
{
    /// <summary>
    /// Sankey diagram is a special flow diagram, which is mainly used to express how the raw materials, energy and so on flow from the initial form to the final state through the intermediate process.
    /// ||桑基图是一种特殊的流图（可以看作是有向无环图）。 它主要用来表示原材料、能量等如何从最初形式经过中间过程的加工或转化达到最终状态。
    /// </summary>
    [System.Serializable]
    [SerieHandler(typeof(SankeyHandler), true)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle))]
    [SerieDataExtraField()]
    public class Sankey : Serie
    {
        /// <summary>
        /// the align of node.
        /// ||节点对齐方式。
        /// </summary>
        public enum NodeAlign
        {
            /// <summary>
            /// top align.
            /// ||顶部对齐
            /// </summary>
            Top,
            /// <summary>
            /// center align.
            /// ||居中对齐 
            /// </summary>
            Center,
            /// <summary>
            /// bottom align.
            /// ||底部对齐 
            /// </summary>
            Bottom
        }
        [SerializeField] private NodeAlign m_NodeAlign = NodeAlign.Center;
        [SerializeField] private float m_NodeWidth = 20;
        [SerializeField] private float m_NodeGap = 8;
        [SerializeField] private bool m_Draggable;
        [SerializeField][Range(0, 1f)] private float m_Curveness = 0.5f;
        [SerializeField][Since("v3.11.0")] private bool m_Collapse = false;
        [SerializeField][Since("v3.11.0")] private bool m_CollapseMutex = true;
        [SerializeField][Since("v3.11.0")] private int m_CollapseLevel = -1;
        [SerializeField][Since("v3.11.0")] private int m_DefaultExpandDataIndex = -1;
        [SerializeField][Since("v3.11.0")] private bool m_LineGradient = true;
        [SerializeField] private LevelStyle m_LevelStyle = new LevelStyle();

        /// <summary>
        /// the align of node.
        /// ||节点对齐方式。
        /// </summary>
        public NodeAlign nodeAlign
        {
            get { return m_NodeAlign; }
            set { m_NodeAlign = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the width of node.
        /// ||节点的宽度。
        /// </summary>
        public float nodeWidth
        {
            get { return m_NodeWidth; }
            set { m_NodeWidth = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the gap of node.
        /// ||节点的间距。
        /// </summary> 
        public float nodeGap
        {
            get { return m_NodeGap; }
            set { m_NodeGap = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// whether draggable.
        /// ||是否可拖拽。 
        /// </summary>
        public bool draggable
        {
            get { return m_Draggable; }
            set { m_Draggable = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the curveness of line.
        /// ||线条的弯曲程度。
        /// </summary>
        public float curveness
        {
            get { return m_Curveness; }
            set { m_Curveness = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// whether collapse.
        /// ||是否开启折叠。
        /// </summary>
        public bool collapse
        {
            get { return m_Collapse; }
            set { m_Collapse = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// whether collapse mutex.
        /// ||是否开启折叠互斥。
        /// </summary>
        public bool collapseMutex
        {
            get { return m_CollapseMutex; }
            set { m_CollapseMutex = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the level of collapse. The level starts from 0, and -1 means no limit.
        /// ||折叠限制的层级。层级从0开始，-1表示不限制。
        /// </summary>
        /// <value></value>
        public int collapseLevel
        {
            get { return m_CollapseLevel; }
            set { m_CollapseLevel = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the default expand data index.
        /// ||默认展开的数据索引。
        /// </summary>
        public int defaultExpandDataIndex
        {
            get { return m_DefaultExpandDataIndex; }
            set { m_DefaultExpandDataIndex = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// whether line gradient. If enabled, the color of lineStyle will not work.
        /// ||是否开启线条渐变。开启后lineStyle的color将不生效。
        /// </summary>
        public bool lineGradient
        {
            get { return m_LineGradient; }
            set { m_LineGradient = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// the style of level.
        /// ||层级样式。
        /// </summary>
        public LevelStyle levelStyle { get { return m_LevelStyle; } }
        public override SerieColorBy defaultColorBy { get { return SerieColorBy.Data; } }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Sankey>(serieName);

            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.position = LabelStyle.Position.Outside;
            label.textStyle.autoAlign = false;
            label.textStyle.alignment = TextAnchor.MiddleLeft;
            label.formatter = "{b}";

            serie.lineStyle.show = true;
            serie.lineStyle.opacity = 0.2f;

            serie.left = 0.1f;
            serie.right = 0.1f;
            serie.top = 0.2f;
            serie.bottom = 0.1f;

            serie.showDataName = true;
            serie.showDataDimension = 1;

            chart.AddData(0, 0, "a");
            chart.AddData(0, 0, "b");
            chart.AddData(0, 0, "a1");
            chart.AddData(0, 0, "a2");
            chart.AddData(0, 0, "b1");
            chart.AddData(0, 0, "c");

            chart.AddLink(0, "a", "a1", 5);
            chart.AddLink(0, "a", "a2", 3);
            chart.AddLink(0, "b", "b1", 8);
            chart.AddLink(0, "a", "b1", 3);
            chart.AddLink(0, "b1", "a1", 1);
            chart.AddLink(0, "b1", "c", 2);
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