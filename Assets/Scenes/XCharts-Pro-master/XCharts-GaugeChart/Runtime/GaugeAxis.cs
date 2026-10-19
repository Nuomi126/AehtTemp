using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Gauges
{
    /// <summary>
    /// 仪表盘类型
    /// </summary>
    public enum GaugeAxisType
    {
        None,
        /// <summary>
        /// 分段
        /// </summary>
        Section,
        /// <summary>
        /// 进度
        /// </summary>
        Progress
    }
    /// <summary>
    /// Settings related to gauge axis line.
    /// ||仪表盘轴线相关设置。
    /// </summary>
    [System.Serializable]
    public class GaugeAxis : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private GaugeAxisType m_Type = GaugeAxisType.None;
        [SerializeField] private bool m_Overlap = true;
        [SerializeField] private float m_Distance;
        [SerializeField] private GaugeAxisLine m_AxisLine = new GaugeAxisLine(true);
        [SerializeField] private AxisSplitLine m_SplitLine = AxisSplitLine.defaultSplitLine;
        [SerializeField] private AxisTick m_AxisTick = AxisTick.defaultTick;
        [SerializeField] private AxisLabel m_AxisLabel = new AxisLabel();
        [SerializeField] private List<string> m_AxisLabelText = new List<string>();

        public bool show { get { return m_Show; } set { m_Show = value; SetVerticesDirty(); } }
        public bool overlap { get { return m_Overlap; } set { m_Overlap = value; SetVerticesDirty(); } }
        public float distance { get { return m_Distance; } set { m_Distance = value; SetVerticesDirty(); } }
        public GaugeAxisType type { get { return m_Type; } set { m_Type = value; SetVerticesDirty(); } }
        /// <summary>
        /// axis line style.
        /// ||仪表盘轴线样式。
        /// </summary>
        public GaugeAxisLine axisLine { get { return m_AxisLine; } set { m_AxisLine = value; SetVerticesDirty(); } }
        /// <summary>
        /// slit line style.
        /// ||分割线。
        /// </summary>
        public AxisSplitLine splitLine { get { return m_SplitLine; } set { m_SplitLine = value; SetVerticesDirty(); } }
        /// <summary>
        /// axis tick style.
        /// ||刻度。
        /// </summary>
        public AxisTick axisTick { get { return m_AxisTick; } set { m_AxisTick = value; SetVerticesDirty(); } }
        /// <summary>
        /// axis label style.
        /// ||文本标签。
        /// </summary>
        public AxisLabel axisLabel { get { return m_AxisLabel; } set { m_AxisLabel = value; SetComponentDirty(); } }
        /// <summary>
        /// Coordinate axis scale label custom content. When the content is empty, 
        /// `axisLabel` automatically displays the content according to the scale; otherwise, 
        /// the content is taken from the list definition.
        /// ||
        /// 自定义Label的内容。
        /// </summary>
        public List<string> axisLabelText { get { return m_AxisLabelText; } set { m_AxisLabelText = value; } }

        public List<float> runtimeStageAngle = new List<float>();
        public List<Vector3> runtimeLabelPosition = new List<Vector3>();
        private List<ChartLabel> m_RuntimeLabelList = new List<ChartLabel>();

        public GaugeAxis()
        {
            m_AxisLabel.distance = -40f;
            m_SplitLine.show = true;
            m_SplitLine.distance = 10;
            m_SplitLine.lineStyle.width = 1.5f;
            m_SplitLine.lineStyle.length = 10f;
            m_SplitLine.lineStyle.color = new Color32(90, 103, 122, 255);

            m_AxisTick.show = true;
            m_AxisTick.distance = 10;
            m_AxisTick.splitNumber = 5;
            m_AxisTick.lineStyle.length = 6f;
        }

        internal Color32 GetAxisLineBackgroundColor(ThemeStyle theme, int index)
        {
            var color = !ChartHelper.IsClearColor(axisLine.backgroundColor) ?
                axisLine.backgroundColor : ChartConst.greyColor32;
            ChartHelper.SetColorOpacity(ref color, axisLine.lineStyle.opacity);
            return color;
        }

        internal Color32 GetSplitLineColor(Color32 themeColor, int serieIndex, float angle, float rate)
        {
            if (splitLine.autoColor)
                return GetStageColor(rate);
            Color32 color;
            if (!ChartHelper.IsClearColor(splitLine.lineStyle.color))
            {
                color = splitLine.lineStyle.color;
                ChartHelper.SetColorOpacity(ref color, splitLine.lineStyle.opacity);
                return color;
            }
            color = themeColor;
            ChartHelper.SetColorOpacity(ref color, splitLine.lineStyle.opacity);
            return color;
        }

        internal Color32 GetAxisTickColor(Color32 themeColor, int serieIndex, float angle, float rate)
        {
            if (splitLine.autoColor)
                return GetStageColor(rate);
            Color32 color;
            if (!ChartHelper.IsClearColor(axisTick.lineStyle.color))
            {
                color = axisTick.lineStyle.color;
                ChartHelper.SetColorOpacity(ref color, axisTick.lineStyle.opacity);
                return color;
            }
            color = themeColor;
            ChartHelper.SetColorOpacity(ref color, axisTick.lineStyle.opacity);
            return color;
        }

        public void ClearLabelObject()
        {
            m_RuntimeLabelList.Clear();
        }

        public void AddLabelObject(ChartLabel label)
        {
            m_RuntimeLabelList.Add(label);
        }

        public ChartLabel GetLabelObject(int index)
        {
            if (index >= 0 && index < m_RuntimeLabelList.Count)
            {
                return m_RuntimeLabelList[index];
            }
            return null;
        }

        public void SetLabelObjectPosition(int index, Vector3 pos)
        {
            if (index >= 0 && index < m_RuntimeLabelList.Count)
            {
                m_RuntimeLabelList[index].SetPosition(pos);
            }
        }

        public void SetLabelObjectText(int index, string text)
        {
            if (index >= 0 && index < m_RuntimeLabelList.Count)
            {
                m_RuntimeLabelList[index].SetText(text);
            }
        }

        public void SetLabelObjectActive(bool flag)
        {
            foreach (var label in m_RuntimeLabelList)
            {
                label.SetActive(flag);
            }
        }

        public Color32 GetStageColor(float rate)
        {
            var count = m_AxisLine.stageColor.Count;
            if (count == 0)
                return ColorUtil.clearColor32;
            var stageIndex = count - 1;
            var stageRate = 1f;
            for (int i = 0; i < m_AxisLine.stageColor.Count; i++)
            {
                var stage = m_AxisLine.stageColor[i];
                if (rate <= stage.percent)
                {
                    var lastStage = i > 0 ? m_AxisLine.stageColor[i - 1] : stage;
                    stageIndex = i;
                    stageRate = (rate - lastStage.percent) / (stage.percent - lastStage.percent);
                    break;
                }
            }
            if (m_AxisLine.stageGradient)
            {
                return Color32.Lerp(m_AxisLine.stageColor[stageIndex - 1].color,
                    m_AxisLine.stageColor[stageIndex].color, stageRate);
            }
            else
            {
                return m_AxisLine.stageColor[stageIndex].color;
            }
        }

        public Color32 GetStageColorByAngle(float angle)
        {
            var count = runtimeStageAngle.Count;
            if (count == 0)
                return ColorUtil.clearColor32;
            var stageIndex = count - 1;
            var stageRate = 1f;
            var startIndex = m_AxisLine.stageGradient?1 : 0;
            for (int i = startIndex; i < runtimeStageAngle.Count; i++)
            {
                if (angle <= runtimeStageAngle[i])
                {
                    stageIndex = i;
                    if (m_AxisLine.stageGradient)
                        stageRate = (angle - runtimeStageAngle[i - 1]) / (runtimeStageAngle[i] - runtimeStageAngle[i - 1]);
                    break;
                }
            }
            if (m_AxisLine.stageGradient)
            {
                return Color32.Lerp(m_AxisLine.stageColor[stageIndex - 1].color,
                    m_AxisLine.stageColor[stageIndex].color, stageRate);
            }
            else
            {
                return m_AxisLine.stageColor[stageIndex].color;
            }
        }
    }
}