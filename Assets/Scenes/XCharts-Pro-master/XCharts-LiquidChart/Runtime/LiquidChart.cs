using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Liquids
{
    /// <summary>
    /// 水位图
    /// </summary>
    [AddComponentMenu("XCharts/LiquidChart", 22)]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [HelpURL("https://xcharts-team.github.io/docs/liquid")]
    public class LiquidChart : BaseChart
    {

#pragma warning disable 0414
        [SerializeField][ListForComponent(typeof(Vessel))] private List<Vessel> m_Vessels = new List<Vessel>();
        [SerializeField][ListForSerie(typeof(Liquid))] private List<Liquid> m_SerieLiquids = new List<Liquid>();
#pragma warning restore 0414

        protected override void DefaultChart()
        {
            GetChartComponent<Tooltip>().type = Tooltip.Type.Line;
            RemoveData();

            Liquid.AddDefaultSerie(this, GenerateDefaultSerieName());
        }

        /// <summary>
        /// generate default rect liquid chart.
        /// 生成默认的矩形水位图。
        /// </summary>
        public void DefaultRectLiquidChart()
        {
            CheckChartInit();
            var vessel = GetChartComponent<Vessel>();
            vessel.shape = Vessel.Shape.Rect;
        }

        /// <summary>
        /// generate default round rect liquid chart.
        /// 生成默认的圆角矩形水位图。
        /// </summary>
        public void DefaultRoundRectLiquidChart()
        {
            CheckChartInit();
            var vessel = GetChartComponent<Vessel>();
            vessel.shape = Vessel.Shape.Rect;
            vessel.cornerRadius = new float[] { 10, 10, 10, 10 };
        }

        /// <summary>
        /// generate default capsule liquid chart.
        /// 生成默认的胶囊形水位图。
        /// </summary>
        public void DefaultCapsuleLiquidChart()
        {
            CheckChartInit();
            var vessel = GetChartComponent<Vessel>();
            vessel.shape = Vessel.Shape.Rect;
            vessel.cornerRadius = new float[] { 1, 1, 1, 1 };
            vessel.width = 0.3f;

            var serie = GetSerie<Liquid>(0);
            serie.waveLength = 8f;
            serie.waveHeight = 4f;
            serie.label.textStyle.fontSize = 30;
        }
    }
}