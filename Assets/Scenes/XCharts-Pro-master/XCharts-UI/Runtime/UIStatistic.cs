using System;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 统计数值UI组件。
    /// 用于展示统计数值。当需要突出某个或某组数字时，或展示带描述的统计类数据时使用。
    /// </summary>
    [Serializable]
    [HelpURL("https://xcharts-team.github.io/docs/ui/ui_statistic")]
    public class UIStatistic : UIComponent
    {
        public enum Type { Default, Countdown }
        [SerializeField] private Type m_Type = Type.Default;
        [SerializeField] private string m_Title = "";
        [SerializeField] private double m_Value;
        [SerializeField] private float m_ItemGap = 5;

        [SerializeField] private Location m_Location = Location.defaultTop;
        [SerializeField] private LabelStyle m_TitleLabelStyle = new LabelStyle() { textStyle = new TextStyle() { fontSize = 22 } };
        [SerializeField] private LabelStyle m_ValueLabelStyle = new LabelStyle() { textStyle = new TextStyle() { fontSize = 38 } };
        [SerializeField] private AnimationStyle m_AnimationStyle = new AnimationStyle();

        protected Action<double> m_OnFinish;
        protected Action<double> m_OnChange;
        public StatisticContext context = new StatisticContext();

        /// <summary>
        /// Callback function when the animation is finished.
        /// ||动画结束时的回调函数。
        /// </summary>
        public Action<double> onFinish
        {
            get { return m_OnFinish; }
            set { m_OnFinish = value; }
        }
        /// <summary>
        /// Callback function when the value is changed.
        /// ||数值变更时的回调函数。
        /// </summary>
        public Action<double> onChange
        {
            get { return m_OnChange; }
            set { m_OnChange = value; }
        }
        /// <summary>
        /// The type of statistic.
        /// ||统计数值类型。
        /// </summary>
        public Type type
        {
            get { return m_Type; }
            set { if (PropertyUtil.SetStruct(ref m_Type, value)) SetAllComponentDirty(); }
        }
        /// <summary>
        /// The title of statistic.
        /// ||统计数值标题。
        /// </summary>
        public string title
        {
            get { return m_Title; }
            set { if (PropertyUtil.SetClass(ref m_Title, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// value of statistic.
        /// ||统计数值。或者秒数。
        /// </summary>
        public double value
        {
            get { return m_Value; }
            set
            {
                context.previousValue = m_Value;
                if (PropertyUtil.SetStruct(ref m_Value, value))
                {
                    context.value = m_Value;
                    context.valueIsInteger = Math.Abs(m_Value % 1) <= (Double.Epsilon * 100);
                    if (m_AnimationStyle.GetChangeDuration() > 0)
                    {
                        context.valueUpdateTime = Time.time;
                        context.valueUpdate = true;
                    }
                    StatisticHandler.UpdateValueText(this, m_Value);
                }
            }
        }
        /// <summary>
        /// The gap between the title and value.
        /// ||标题和数值文本之间的间距。
        /// </summary>
        public float itemGap
        {
            get { return m_ItemGap; }
            set { if (PropertyUtil.SetStruct(ref m_ItemGap, value)) SetAllComponentDirty(); }
        }
        /// <summary>
        /// The style of title label.
        /// ||标题文本样式。
        /// </summary>
        public LabelStyle titleLabelStyle
        {
            get { return m_TitleLabelStyle; }
            set { m_TitleLabelStyle = value; }
        }
        /// <summary>
        /// The style of value label.
        /// ||数值文本样式。
        /// </summary>
        public LabelStyle valueLabelStyle
        {
            get { return m_ValueLabelStyle; }
            set { m_ValueLabelStyle = value; }
        }
        /// <summary>
        /// The start animation.
        /// ||起始动画。
        /// </summary>
        public AnimationStyle animationStyle
        {
            get { return m_AnimationStyle; }
            set { if (PropertyUtil.SetClass(ref m_AnimationStyle, value, true)) SetVerticesDirty(); }
        }
        /// <summary>
        /// The location of text.
        /// ||文本显示位置。
        /// </summary>
        public Location location
        {
            get { return m_Location; }
            set { if (PropertyUtil.SetClass(ref m_Location, value)) SetAllComponentDirty(); }
        }

        protected override void Update()
        {
            base.Update();
            StatisticHandler.UpdateCountdown(this);
            StatisticHandler.UpdateAnimation(this);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSize(180, 100);
            m_Title = "Title";
            m_Value = UnityEngine.Random.Range(112000, 119999);
            m_Location = Location.defaultLeft;
            SetAllComponentDirty();
        }
#endif

        protected override void InitComponent()
        {
            base.InitComponent();
            StatisticHandler.InitLabel(this);
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            base.OnDrawPainterBase(vh, painter);
        }

        /// <summary>
        /// 获取当前时刻的数值。当有变更动画时，返回动画过程中的数值。
        /// </summary>
        /// <param name="animationDuration">设定的变更动画时长，单位：毫秒</param>
        /// <returns>当前值</returns>
        public double GetCurrentValue(float animationDuration)
        {
            if (context.valueUpdate && animationDuration > 0)
            {
                var time = Time.time - context.valueUpdateTime;
                var total = animationDuration / 1000;

                var rate = time / total;
                if (rate > 1) rate = 1;
                if (rate < 1)
                {
                    return MathUtil.Lerp(context.previousValue, context.value, rate);
                }
                else
                {
                    context.valueUpdate = false;
                    return context.value;
                }
            }
            else
            {
                return context.value;
            }
        }
    }
}