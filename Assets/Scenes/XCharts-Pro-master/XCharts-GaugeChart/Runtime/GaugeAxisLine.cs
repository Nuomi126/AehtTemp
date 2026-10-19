using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Gauges
{
    [System.Serializable]
    public class GaugeAxisLine : BaseLine
    {
        [SerializeField] private Color32 m_BackgroundColor = new Color32(230, 235, 248, 255);
        [SerializeField]
        private List<StageColor> m_StageColor = new List<StageColor>()
        {
            new StageColor(0, new Color32(103, 224, 227, 255)),
            new StageColor(0.2f, new Color32(103, 224, 227, 255)),
            new StageColor(0.8f, new Color32(55, 162, 218, 255)),
            new StageColor(1.0f, new Color32(253, 102, 109, 255)),
        };
        [SerializeField][Since("v3.4.0")] private bool m_StageGradient;

        /// <summary>
        /// The background color of gauge axis.
        /// ||进度条背景颜色。
        /// </summary>
        public Color32 backgroundColor { get { return m_BackgroundColor; } set { m_BackgroundColor = value; } }
        /// <summary>
        /// The colors of stage.
        /// ||阶段颜色。
        /// </summary>
        public List<StageColor> stageColor { get { return m_StageColor; } set { m_StageColor = value; } }
        /// <summary>
        /// Whether to use gradients for transitions.
        /// ||阶段颜色是否渐变过渡。
        /// </summary>
        public bool stageGradient { get { return m_StageGradient; } set { m_StageGradient = value; SetVerticesDirty(); } }

        public GaugeAxisLine(bool show) : base(show)
        {
            m_LineStyle.width = 10f;
        }
    }
}