using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class UIProgress : UIComponent
    {
        public class Context
        {
            public Rect drawRect;
            public Rect fillRect;
            public float fillRate;
            public float fillTickness;
            public Vector3 fillCenter;
            public float fillInnerRadius;
            public float fillOuterRadius;
            public float fillStartAngle;
            public float fillAngle;
            public Vector3 labelPos;
            public ChartLabel label;
        }
        /// <summary>
        /// The type of progress.
        /// ||进度条类型。
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Horizontal progress bar.
            /// ||水平进度条。
            /// </summary>
            Line,
            /// <summary>
            /// Circle progress bar.
            /// ||圆形进度条。
            /// </summary> 
            Circle
        }
        [SerializeField] private Type m_Type = Type.Line;
        [SerializeField] private float m_Min = 0;
        [SerializeField] private float m_Max = 100f;
        [SerializeField] private float m_Step = 0f;
        [SerializeField] private float m_Value = 50f;
        [SerializeField] private FillStyle m_FillStyle = new FillStyle();
        [SerializeField] private LabelStyle m_LabelStyle = new LabelStyle();

        public Context context = new Context();
        private bool m_UpdateLabel = false;

        /// <summary>
        /// The type of progress.
        /// ||进度条类型。
        /// </summary> 
        public Type type { get { return m_Type; } set { m_Type = value; SetVerticesDirty(); } }
        /// <summary>
        /// The minimum value of progress.
        /// ||进度条最小值。
        /// </summary>
        public float min { get { return m_Min; } set { m_Min = value; SetVerticesDirty(); } }
        /// <summary>
        /// The maximum value of progress.
        /// ||进度条最大值。
        ///  <summary> 
        public float max { get { return m_Max; } set { m_Max = value; SetVerticesDirty(); } }
        /// <summary>
        /// The step of progress.
        /// ||进度条步长。
        /// </summary>
        public float step { get { return m_Step; } set { m_Step = value; SetVerticesDirty(); } }
        /// <summary>
        /// The value of progress.
        /// ||进度条当前值。
        /// </summary> 
        public float value { get { return m_Value; } set { m_Value = value; SetVerticesDirty(); } }
        /// <summary>
        /// The style of progress bar fill.
        /// ||进度条填充样式。
        /// </summary>
        public FillStyle fillStyle { get { return m_FillStyle; } set { m_FillStyle = value; SetVerticesDirty(); } }
        /// <summary>
        /// The style of progress bar label.
        /// ||进度条标签样式。
        /// </summary>
        public LabelStyle labelStyle { get { return m_LabelStyle; } set { m_LabelStyle = value; SetVerticesDirty(); } }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSize(580, 4);
            m_Background.show = false;
            m_FillStyle.height = 10f;
            m_FillStyle.background.show = true;
            m_FillStyle.background.autoColor = false;
            m_FillStyle.background.imageColor = new Color32(235, 235, 235, 255);
            m_FillStyle.background.borderStyle.show = true;
            m_FillStyle.background.borderStyle.cornerRadius = new float[] { 1, 1, 1, 1 };
            m_LabelStyle.show = true;
            m_LabelStyle.position = LabelStyle.Position.Right;
            m_LabelStyle.formatter = "{value}%";
            m_FillStyle.location.right = 0f;
            m_FillStyle.color = new Color32(22, 119, 255, 255);
            m_LabelStyle.textStyle.autoAlign = false;
            m_LabelStyle.textStyle.alignment = TextAnchor.MiddleRight;
        }
#endif

        public void DefaultCircle()
        {
            SetSize(200, 200);
            m_Type = Type.Circle;
            m_LabelStyle.position = LabelStyle.Position.Center;
            m_LabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
        } 

        protected override void Update()
        {
            base.Update();
            if (m_UpdateLabel)
            {
                UpdateLabel();
                m_UpdateLabel = false;
            }
        }

        protected override void InitComponent()
        {
            base.InitComponent();
            InitLabel();
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            base.OnDrawPainterBase(vh, painter);
            UpdateContextData();
            var lastValue = m_Value;
            var background = m_FillStyle.background;
            var backgroundColor = theme.GetBackgroundColor(background);
            var fillColor = m_FillStyle.color;
            var toColor = ChartHelper.IsClearColor(m_FillStyle.toColor) ?
                fillColor : Color32.Lerp(fillColor, m_FillStyle.toColor, context.fillRate);
            float smoothness;
            var cornerRadius = background.borderStyle.GetRuntimeCornerRadius();
            switch (m_Type)
            {
                case Type.Line:
                    smoothness = Mathf.Clamp(context.fillRect.height / 10, 0.2f, 5f);
                    UIHelper.DrawBackground(vh, background, backgroundColor, smoothness);
                    UGL.DrawRoundRectangle(vh, context.fillRect, fillColor, toColor, 0, cornerRadius, true, smoothness);
                    break;
                case Type.Circle:
                    var startDegree = 0;
                    var toDegree = 360;
                    var borderWidth = 0;
                    var borderColor = Color.clear;
                    var roundCap = context.fillRate < 1 && m_FillStyle.roundCap;
                    var clockwise = true;
                    var radiusGradient = false;
                    smoothness = Mathf.Clamp(context.fillTickness / 10, 0.2f, 5f);

                    UGL.DrawDoughnut(vh, context.fillCenter, context.fillInnerRadius, context.fillOuterRadius, backgroundColor, backgroundColor,
                        Color.clear, 0, toDegree, borderWidth, borderColor, 0, smoothness,
                        false, clockwise, radiusGradient);

                    UGL.DrawDoughnut(vh, context.fillCenter, context.fillInnerRadius, context.fillOuterRadius, fillColor, toColor,
                        Color.clear, startDegree, context.fillAngle, borderWidth, borderColor, 0, smoothness,
                        roundCap, clockwise, radiusGradient);
                    break;
            }
            if (!m_UpdateLabel || lastValue != m_Value)
            {
                m_UpdateLabel = true;
            }
        }

        private void UpdateContextData()
        {
            if (m_Min == m_Max) m_Max = m_Min + 1;

            m_Value = ChartHelper.GetMinCeilRate(Mathf.Clamp(m_Value, m_Min, m_Max), m_Step);
            m_FillStyle.location.OnChanged();
            m_FillStyle.location.UpdateRuntimeData(graphWidth, graphHeight);
            context.drawRect = m_FillStyle.location.GetRect(graphX, graphY, graphWidth, graphHeight, m_FillStyle.width, m_FillStyle.height);
            if (m_FillStyle.autoOffset && m_Type == Type.Line && m_LabelStyle.show && context.label != null)
            {
                var labelWidth = context.label.GetWidth();
                if (context.drawRect.x + context.drawRect.width + labelWidth >= graphX + graphWidth)
                {
                    context.drawRect.width = graphWidth - (context.drawRect.x - graphX) - labelWidth;
                }
            }
            m_FillStyle.background.rect = context.drawRect;

            context.fillTickness = context.drawRect.height;
            context.fillRate = (m_Value - m_Min) / (m_Max - m_Min);
            context.fillRect = new Rect(context.drawRect.x, context.drawRect.y, context.drawRect.width * context.fillRate, context.drawRect.height);
            context.fillCenter = new Vector3(context.drawRect.x + context.drawRect.width / 2, context.drawRect.y + context.drawRect.height / 2);
            context.fillStartAngle = 0;
            context.fillAngle = 360 * context.fillRate;

            var rect = graphRect;
            var isCircle = m_Type == Type.Circle;
            if (isCircle)
            {
                var tempRadius = m_FillStyle.radius <= 0 ?
                    Mathf.Min(graphWidth, graphHeight) / 2 - context.fillTickness :
                    m_FillStyle.radius;
                context.fillInnerRadius = tempRadius;
                context.fillOuterRadius = tempRadius + context.fillTickness;
                var midRadius = (context.fillOuterRadius + context.fillInnerRadius) / 2 + m_LabelStyle.distance;
                var px1 = 0f;
                var py1 = 0f;
                switch (m_LabelStyle.position)
                {
                    case LabelStyle.Position.Start:
                        px1 = Mathf.Sin(context.fillStartAngle * Mathf.Deg2Rad) * midRadius;
                        py1 = Mathf.Cos(context.fillStartAngle * Mathf.Deg2Rad) * midRadius;
                        context.labelPos = new Vector3(context.fillCenter.x + px1, context.fillCenter.y + py1);
                        break;
                    case LabelStyle.Position.End:
                        px1 = Mathf.Sin((context.fillStartAngle + context.fillAngle) * Mathf.Deg2Rad) * midRadius;
                        py1 = Mathf.Cos((context.fillStartAngle + context.fillAngle) * Mathf.Deg2Rad) * midRadius;
                        context.labelPos = new Vector3(context.fillCenter.x + px1, context.fillCenter.y + py1);
                        break;
                    default:
                        context.labelPos = context.fillCenter;
                        break;
                }
            }
            else
            {
                context.fillInnerRadius = 0;
                context.fillOuterRadius = 0;
                switch (m_LabelStyle.position)
                {
                    case LabelStyle.Position.Bottom:
                        context.labelPos = new Vector3(rect.x + rect.width / 2, rect.y);
                        break;
                    case LabelStyle.Position.Top:
                        context.labelPos = new Vector3(rect.x + rect.width / 2, rect.y + rect.height);
                        break;
                    case LabelStyle.Position.Left:
                    case LabelStyle.Position.Start:
                        context.labelPos = new Vector3(rect.x, rect.y + rect.height / 2);
                        break;
                    case LabelStyle.Position.Right:
                    case LabelStyle.Position.End:
                        context.labelPos = new Vector3(rect.x + rect.width, rect.y + rect.height / 2);
                        break;
                    case LabelStyle.Position.Inside:
                    case LabelStyle.Position.Outside:
                        rect = context.fillRect;
                        context.labelPos = new Vector3(rect.x + rect.width, rect.y + rect.height / 2);
                        break;
                    default:
                        context.labelPos = new Vector3(rect.x + rect.width / 2, rect.y + rect.height / 2);
                        break;
                }
            }
        }

        private void InitLabel()
        {
            UpdateContextData();
            var labelTheme = theme.sharedTheme.axis;
            var text = m_LabelStyle.GetFormatterContent(0, m_Value, m_Min, m_Max);

            var labelObject = ChartHelper.AddObject("label", transform, graphMinAnchor, graphMaxAnchor, graphPivot, graphSizeDelta, -1, childrenNodeNames);
            labelObject.hideFlags = chartHideFlags;

            context.label = ChartHelper.AddChartLabel("label", labelObject.transform, m_LabelStyle, labelTheme, text, Color.clear, TextAnchor.MiddleCenter);
            context.label.SetPosition(context.labelPos + labelStyle.offset);
            context.label.SetActive(m_LabelStyle.show, true);
        }

        private void UpdateLabel()
        {
            if (context.label == null)
            {
                InitLabel();
            }
            else
            {
                var text = m_LabelStyle.GetFormatterContent(0, m_Value, m_Min, m_Max);
                context.label.SetText(text);
                context.label.SetPosition(context.labelPos + labelStyle.offset);
            }
        }
    }
}