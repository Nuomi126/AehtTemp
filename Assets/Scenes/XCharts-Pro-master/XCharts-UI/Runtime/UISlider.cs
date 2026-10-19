using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// ui component of slider. 
    /// ||UI滑动型输入组件，展示当前值和可选范围。
    /// </summary>
    [Serializable]
    [HelpURL("https://xcharts-team.github.io/docs/ui/ui_slider")]
    public class UISlider : UIComponent
    {
        /// <summary>
        /// Type of value.
        /// ||值类型。
        /// </summary> 
        public enum ValueType
        {
            /// <summary>
            /// Default value.
            /// ||默认值。
            /// </summary>
            Default,
            /// <summary>
            /// Range value.
            /// ||范围值。
            /// </summary> 
            Range,
            /// <summary>
            /// Enum value.
            /// ||枚举值。
            /// </summary> 
            Enum
        }

        public class Context
        {
            public bool isMinDraging;
            public bool isMaxDraging;
            public Rect drawRect;
            public float fillRate0;
            public float fillRate1;
            public Vector3 startLogicPos;
            public Vector3 endLogicPos;
            public Vector3 minHandlePos;
            public Vector3 maxHandlePos;
            public float handleSize;
            public float markSize;
            public List<ChartLabel> markLabels = new List<ChartLabel>();
            public List<Vector3> markPos = new List<Vector3>();
        }

        /// <summary>
        /// Style of handle.
        /// ||滑块样式。
        /// </summary>
        [Serializable]
        public class HandleStyle : ChildComponent
        {
            [SerializeField] private bool m_Show = true;
            [SerializeField] private bool m_GradientColor = false;
            [SerializeField] private SymbolStyle m_Symbol = new SymbolStyle();

            /// <summary>
            /// Whether to show handle.
            /// ||是否显示滑块。
            /// </summary> 
            public bool show { get { return m_Show; } set { m_Show = value; SetAllDirty(); } }
            /// <summary>
            /// Whether to use gradient color.
            /// ||是否使用渐变颜色。滑动条设置渐变颜色时才生效。
            /// </summary>
            public bool gradientColor { get { return m_GradientColor; } set { m_GradientColor = value; SetVerticesDirty(); } }
            /// <summary>
            /// Style of symbol.
            /// ||图形样式。
            /// </summary> 
            public SymbolStyle symbol { get { return m_Symbol; } set { m_Symbol = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Style of mark.
        /// ||标记样式。
        /// </summary>
        [Serializable]
        public class MarkStyle : ChildComponent
        {
            [SerializeField] private bool m_Show = true;
            [SerializeField] private bool m_Included = true;
            [SerializeField] private LabelStyle m_Label = new LabelStyle();
            [SerializeField] private SymbolStyle m_Symbol = new SymbolStyle();

            /// <summary>
            /// Whether to show mark.
            /// ||是否显示标记。
            /// </summary>
            public bool show { get { return m_Show; } set { m_Show = value; SetAllDirty(); } }
            /// <summary>
            /// Whether to include in the range. When true, the mark is displayed as the progress bar color, otherwise it is displayed as the background color.
            /// ||是否包含在范围内。为true时，标记显示为进度条颜色，否则显示为背景颜色。
            /// </summary>
            public bool included { get { return m_Included; } set { m_Included = value; SetAllDirty(); } }
            /// <summary>
            /// Style of symbol.
            /// ||图形样式。
            /// </summary> 
            public SymbolStyle symbol { get { return m_Symbol; } set { m_Symbol = value; SetVerticesDirty(); } }
            /// <summary>
            /// Style of label.
            /// ||文本样式。
            /// </summary>
            public LabelStyle label { get { return m_Label; } set { m_Label = value; SetAllDirty(); } }
        }

        [SerializeField] private bool m_Interactable = true;
        [SerializeField] private float m_Min = 0;
        [SerializeField] private float m_Max = 100;
        [SerializeField] private float m_Step = 1;
        [SerializeField] private float[] m_Value = new float[] { 0, 50 };
        [SerializeField] private ValueType m_ValueType = ValueType.Default;
        [SerializeField] private List<float> m_MarkValues = new List<float>();
        [SerializeField] private List<string> m_MarkNames = new List<string>();
        [SerializeField] private FillStyle m_FillStyle = new FillStyle();
        [SerializeField] private MarkStyle m_MarkStyle = new MarkStyle();
        [SerializeField] private HandleStyle m_HandleStyle = new HandleStyle();

        private Action<float> m_OnValueChanged;
        private Action<float> m_OnValueChangedComplete;
        private Action<float, float> m_OnRangeChanged;
        private Action<float, float> m_OnRangeChangedComplete;

        public Context context = new Context();
        private bool m_ValueLabelDirty;
        /// <summary>
        /// Whether the slider is interactable.
        /// ||是否可交互。
        /// </summary>
        public bool interactable { get { return m_Interactable; } set { m_Interactable = value; SetVerticesDirty(); } }
        /// <summary>
        /// Type of value. Support default value, range value, enum value.
        /// ||值类型。支持默认值、范围值、枚举值。
        /// </summary> 
        public ValueType valueType { get { return m_ValueType; } set { m_ValueType = value; SetAllDirty(); } }
        /// <summary>
        /// Values of marks. When specified with mark names, the mark and text are displayed at the specified position.
        /// ||标记值。当指定有标志值时，会在指定位置显示标记和文本。
        /// </summary>
        public List<float> markValues { get { return m_MarkValues; } set { m_MarkValues = value; SetAllDirty(); } }
        /// <summary>
        /// Names of marks. When specified with mark values, the mark and text are displayed at the specified position. 
        /// ||标志名称。当指定有标志名称时，标记的文本会尝试从这里取。
        /// </summary>
        public List<string> markNames { get { return m_MarkNames; } set { m_MarkNames = value; SetAllDirty(); } }
        /// <summary>
        /// Minimum value. The slider value cannot be less than the minimum value.
        /// ||最小值。滑块值不能小于最小值。
        /// </summary> 
        public float min { get { return m_Min; } set { m_Min = value; SetVerticesDirty(); } }
        /// <summary>
        /// Maximum value. The slider value cannot be greater than the maximum value.
        /// ||最大值。滑块值不能大于最大值。
        /// </summary>
        public float max { get { return m_Max; } set { m_Max = value; SetVerticesDirty(); } }
        /// <summary>
        /// Step size. The value of the slider will change by step size.
        /// ||步长。滑块值会按照步长变化。
        /// </summary>
        public float step { get { return m_Step; } set { m_Step = value; SetVerticesDirty(); } }
        /// <summary>
        /// Current value of the slider. This is a two-dimensional array. When it is a range value type, 
        /// the two values represent the minimum and maximum values respectively, and when it is other types, 
        /// only the second value is used.
        /// ||滑块当前值。这是一个二维数组，当为范围值类型时，两个值分别表示最小值和最大值，其他类型时只用第二个值。
        /// </summary>
        public float[] value { get { return m_Value; } set { m_Value = value; SetVerticesDirty(); } }
        /// <summary>
        /// Style of fill.
        /// ||填充样式。
        /// </summary> 
        public FillStyle fillStyle { get { return m_FillStyle; } set { m_FillStyle = value; SetAllDirty(); } }
        /// <summary>
        /// Style of mark.
        /// ||标记样式。
        /// </summary> 
        public MarkStyle markStyle { get { return m_MarkStyle; } set { m_MarkStyle = value; SetAllDirty(); } }
        /// <summary>
        /// Style of handle.
        /// ||滑块样式。
        /// </summary> 
        public HandleStyle handleStyle { get { return m_HandleStyle; } set { m_HandleStyle = value; SetAllDirty(); } }
        /// <summary>
        /// Callback when the value of the slider changes.
        /// ||滑块值改变时的回调。
        /// </summary> 
        public Action<float> onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }
        /// <summary>
        /// Callback when the value of the slider changes is completed.
        /// ||滑块值改变完成时的回调。
        /// </summary>
        public Action<float> onValueChangedComplete { get { return m_OnValueChangedComplete; } set { m_OnValueChangedComplete = value; } }
        /// <summary>
        /// Callback when the range of the slider changes.
        /// ||滑块范围改变时的回调。
        /// </summary>
        public Action<float, float> onRangeChanged { get { return m_OnRangeChanged; } set { m_OnRangeChanged = value; } }
        /// <summary>
        /// Callback when the range of the slider changes is completed.
        /// ||滑块范围改变完成时的回调。
        /// </summary>
        public Action<float, float> onRangeChangedComplete { get { return m_OnRangeChangedComplete; } set { m_OnRangeChangedComplete = value; } }

        /// <summary>
        /// Set the value of the slider.
        /// ||设置当前值。
        /// </summary> 
        public void SetValue(float value)
        {
            m_Value[1] = value;
            CheckValueLimit();
            SetVerticesDirty();
        }

        /// <summary>
        /// Set the range value of the slider.
        /// ||设置当前范围值。
        /// </summary>
        public void SetValue(float rangeMin, float rangeMax)
        {
            m_Value[0] = rangeMin;
            m_Value[1] = rangeMax;
            CheckValueLimit();
            SetVerticesDirty();
        }

        /// <summary>
        /// Set the value and the maximum and minimum values.
        /// ||设置当前值和最大最小值。
        /// </summary>
        public void SetValue(float value, float min, float max)
        {
            m_Min = min;
            m_Max = max;
            m_Value[1] = value;
            CheckValueLimit();
            SetVerticesDirty();
        }

        /// <summary>
        /// Set the range value and the maximum and minimum values.
        /// ||设置当前范围值和最大最小值。
        /// </summary>
        public void SetValue(float rangeMin, float rangeMax, float min, float max)
        {
            m_Min = min;
            m_Max = max;
            m_Value[0] = rangeMin;
            m_Value[1] = rangeMax;
            CheckValueLimit();
            SetVerticesDirty();
        }

        /// <summary>
        /// Set the current enum value and the enum type.
        /// ||设置当前枚举值和枚举类型。 
        /// </summary>
        public void SetValue(float value, Type enumType)
        {
            if (!enumType.IsEnum) return;
            m_ValueType = ValueType.Enum;
            m_Value[0] = 0;
            m_Value[1] = value;
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            m_MarkValues.Clear();
            m_MarkNames.Clear();
            foreach (var field in fields)
            {
                var v = (int)field.GetValue(null);
                if (m_Min > v) m_Min = v;
                if (m_Max < v) m_Max = v;
                m_MarkValues.Add(v);
                m_MarkNames.Add(field.Name);
            }
            SetAllDirty();
            InitValueLabel();
        }

        private void CheckValueLimit()
        {
            if (m_Value == null || m_Value.Length == 0)
            {
                m_Value = new float[] { 0, 0 };
                return;
            }
            if (m_Value.Length >= 1)
            {
                if (m_Value[0] < m_Min) m_Value[0] = m_Min;
                if (m_Value[0] > m_Max) m_Value[0] = m_Max;
            }
            if (m_Value.Length >= 2)
            {
                if (m_Value[1] < m_Min) m_Value[1] = m_Min;
                if (m_Value[1] > m_Max) m_Value[1] = m_Max;
                if (m_Value[1] < m_Value[0]) m_Value[1] = m_Value[0];
                if (m_Value[0] > m_Value[1]) m_Value[0] = m_Value[1];
            }
            if (m_Step > 0)
            {
                m_Value[0] = ChartHelper.GetMinCeilRate(m_Value[0], m_Step);
                m_Value[1] = ChartHelper.GetMaxCeilRate(m_Value[1], m_Step);
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSize(580, 70);
            m_Background.show = false;
            m_FillStyle.height = 5f;
            m_FillStyle.background.show = true;
            m_FillStyle.background.autoColor = false;
            m_FillStyle.background.imageColor = new Color32(235, 235, 235, 255);
            m_FillStyle.background.borderStyle.show = true;
            m_FillStyle.background.borderStyle.cornerRadius = new float[] { 1, 1, 1, 1 };
            context.handleSize = m_HandleStyle.symbol.size > 0 ? m_HandleStyle.symbol.size : Mathf.Min(20, graphHeight / 2);
            m_MarkStyle.label.offset = new Vector3(0, -context.handleSize - 5);
        }
#endif

        protected override void Update()
        {
            base.Update();
            if (m_ValueLabelDirty)
            {
                UpdateValueLabel();
                m_ValueLabelDirty = false;
            }
        }

        protected override void InitComponent()
        {
            base.InitComponent();
            InitValueLabel();
        }

        protected void UpdateContextData()
        {
            CheckValueLimit();
            m_FillStyle.location.OnChanged();
            m_FillStyle.location.UpdateRuntimeData(graphWidth, graphHeight);
            context.handleSize = m_HandleStyle.symbol.size > 0 ? m_HandleStyle.symbol.size : Mathf.Min(20, graphHeight / 2);
            context.markSize = m_MarkStyle.symbol.size > 0 ? m_MarkStyle.symbol.size : context.handleSize / 3;
            context.drawRect = m_FillStyle.location.GetRect(graphX, graphY, graphWidth, graphHeight, m_FillStyle.width, m_FillStyle.height);
            if (m_FillStyle.autoOffset)
            {
                var maxSize = Mathf.Max(context.handleSize, context.markSize);

                if (context.drawRect.x - maxSize < graphX)
                {
                    context.drawRect.x = graphX + maxSize;
                }
                if (context.drawRect.x + context.drawRect.width + maxSize > graphX + graphWidth)
                {
                    context.drawRect.width = graphWidth - (context.drawRect.x - graphX) - maxSize;
                }
            }
            m_FillStyle.background.rect = context.drawRect;

            var offsetX = 0f;
            var posY = context.drawRect.y + context.drawRect.height / 2;
            // switch (m_ValueType)
            // {
            //     case ValueType.Default:
            //     case ValueType.Range:
            //         offsetX = m_MarkStyle.show && m_MarkStyle.symbol.show && markValues.Count > 0 ? context.markSize : context.handleSize;
            //         break;
            //     case ValueType.Enum:
            //         offsetX = context.handleSize;
            //         break;
            // }
            context.startLogicPos = new Vector3(context.drawRect.x + offsetX, posY);
            context.endLogicPos = new Vector3(context.drawRect.x + context.drawRect.width - offsetX, posY);
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            base.OnDrawPainterBase(vh, painter);
            UpdateContextData();
            var background = m_FillStyle.background;
            var drawRect = context.drawRect;
            var smoothness = Mathf.Clamp(context.drawRect.height / 10, 0.2f, 5f);
            var cornerRadius = background.borderStyle.GetRuntimeCornerRadius();
            UIHelper.DrawBackground(vh, background, theme.GetBackgroundColor(background), smoothness);
            if (m_ValueType == ValueType.Enum)
            {
                var count = m_MarkValues.Count;
                var tick = drawRect.width / count;
                UGL.DrawRoundRectangle(vh, drawRect, m_FillStyle.color, m_FillStyle.color, 0, cornerRadius, true);
                DrawMark(vh);

                var index = m_MarkValues.IndexOf(m_Value[1]);
                if (index >= 0 && index < count)
                {
                    context.fillRate1 = (float)((m_Value[1] - m_Min) / (m_Max - m_Min));
                    context.maxHandlePos = new Vector3(drawRect.x + tick * (index + 0.5f), drawRect.y + drawRect.height / 2);
                    DrawHandle(vh);
                }
            }
            else if (m_ValueType == ValueType.Range)
            {
                context.fillRate0 = (float)((m_Value[0] - m_Min) / (m_Max - m_Min));
                context.fillRate1 = (float)((m_Value[1] - m_Min) / (m_Max - m_Min));
                var width0 = drawRect.width * context.fillRate0;
                var width1 = drawRect.width * context.fillRate1;
                var logicWidth = context.endLogicPos.x - context.startLogicPos.x;
                context.minHandlePos = new Vector3(context.startLogicPos.x + logicWidth * context.fillRate0, context.startLogicPos.y);
                context.maxHandlePos = new Vector3(context.startLogicPos.x + logicWidth * context.fillRate1, context.startLogicPos.y);
                var fillRect = new Rect(drawRect.x + width0, drawRect.y, width1 - width0, drawRect.height);
                var fillColor1 = ChartHelper.IsClearColor(m_FillStyle.toColor) ?
                    m_FillStyle.color : Color32.Lerp(m_FillStyle.color, m_FillStyle.toColor, context.fillRate0);
                var fillColor2 = ChartHelper.IsClearColor(m_FillStyle.toColor) ?
                    m_FillStyle.color : Color32.Lerp(m_FillStyle.color, m_FillStyle.toColor, context.fillRate1);
                UGL.DrawRoundRectangle(vh, fillRect, fillColor1, fillColor2, 0, cornerRadius, true);
                DrawMark(vh);
                DrawHandle(vh);
            }
            else
            {
                var value = m_Value.Length > 1 ? m_Value[1] : m_Value[0];
                context.fillRate1 = (float)((value - m_Min) / (m_Max - m_Min));
                var width = drawRect.width * context.fillRate1;
                var width2 = (context.endLogicPos.x - context.startLogicPos.x) * context.fillRate1;
                context.maxHandlePos = new Vector3(context.startLogicPos.x + width2, context.startLogicPos.y);
                var needFill = !m_MarkStyle.show || !m_MarkStyle.symbol.show || (m_MarkStyle.show && m_MarkStyle.symbol.show && m_MarkStyle.included);
                if (needFill)
                {
                    var fillRect = new Rect(drawRect.x, drawRect.y, width, drawRect.height);
                    var toColor = ChartHelper.IsClearColor(m_FillStyle.toColor) ?
                        m_FillStyle.color : Color32.Lerp(m_FillStyle.color, m_FillStyle.toColor, context.fillRate1);
                    UGL.DrawRoundRectangle(vh, fillRect, m_FillStyle.color, toColor, 0, cornerRadius, true);
                }
                DrawMark(vh);
                DrawHandle(vh);
            }
            m_ValueLabelDirty = true;
        }

        private void DrawMark(VertexHelper vh)
        {
            if (!m_MarkStyle.show || !m_MarkStyle.symbol.show) return;
            var included = m_MarkStyle.included;
            var symbol = m_MarkStyle.symbol;
            var isClearColor = ChartHelper.IsClearColor(symbol.color);
            var isClearEmptyColor = ChartHelper.IsClearColor(symbol.emptyColor);
            var symbolColor = isClearColor ? m_FillStyle.color : symbol.color;
            var emptyColor = isClearEmptyColor ? ColorUtil.white : symbol.emptyColor;
            var backgroundColor = theme.GetBackgroundColor(m_FillStyle.background);
            var count = m_MarkValues.Count;
            var offset = m_MarkStyle.symbol.offset;
            var borderWidth = m_MarkStyle.symbol.borderWidth;

            UpdateMarkPos();
            for (int i = 0; i < count; i++)
            {
                var pos = context.markPos[i];
                pos.x += offset.x;
                pos.y += offset.y;
                var inRange = (m_ValueType == ValueType.Enum) || (included && IsInRange(pos));
                if (inRange)
                {
                    ChartDrawer.DrawSymbol(vh, symbol.type, context.markSize, borderWidth, pos, symbolColor, symbolColor, symbol.gap,
                        null, emptyColor, backgroundColor, symbolColor, 2, pos, symbol.size2);
                }
                else
                {
                    var symbolColor1 = isClearColor ? backgroundColor : symbol.color;
                    ChartDrawer.DrawSymbol(vh, symbol.type, context.markSize, borderWidth, pos, symbolColor1, symbolColor1, symbol.gap,
                        null, emptyColor, backgroundColor, symbolColor, 2, pos, symbol.size2);
                }
            }
        }

        private bool IsInRange(Vector3 pos)
        {
            if (m_ValueType == ValueType.Range)
            {
                return pos.x >= context.minHandlePos.x && pos.x <= context.maxHandlePos.x;
            }
            else
            {
                return pos.x <= context.maxHandlePos.x;
            }
        }

        private void UpdateMarkPos()
        {
            var count = m_MarkValues.Count;
            var tick = context.drawRect.width / count;
            context.markPos.Clear();
            for (int i = 0; i < count; i++)
            {
                Vector3 pos;
                if (m_ValueType == ValueType.Enum)
                {
                    pos = new Vector3(context.drawRect.x + tick * (i + 0.5f), context.drawRect.y + context.drawRect.height / 2);
                }
                else
                {
                    var value = m_MarkValues[i];
                    var rate = (float)((value - m_Min) / (m_Max - m_Min));
                    var width = context.drawRect.width * rate;
                    pos = new Vector3(context.drawRect.x + width, context.drawRect.y + context.drawRect.height / 2);
                }
                context.markPos.Add(pos);
            }
        }

        private void DrawHandle(VertexHelper vh)
        {
            if (!m_HandleStyle.show) return;
            var symbol = m_HandleStyle.symbol;
            var size = context.handleSize;
            var borderWidth = m_HandleStyle.symbol.borderWidth;
            var symbolColor = ChartHelper.IsClearColor(m_HandleStyle.symbol.color) ? m_FillStyle.color : m_HandleStyle.symbol.color;
            var emptyColor = ChartHelper.IsClearColor(m_HandleStyle.symbol.emptyColor) ? ColorUtil.white : m_HandleStyle.symbol.emptyColor;
            var backgroundColor = emptyColor;
            var isGradientColor = !ChartHelper.IsClearColor(m_FillStyle.toColor) && m_HandleStyle.gradientColor;

            if (m_ValueType == ValueType.Range)
            {
                var color1 = isGradientColor ? Color32.Lerp(m_FillStyle.color, m_FillStyle.toColor, context.fillRate0) : symbolColor;
                ChartDrawer.DrawSymbol(vh, symbol.type, size, borderWidth, context.minHandlePos, color1, color1, symbol.gap,
                    null, emptyColor, backgroundColor, symbolColor, 2, context.minHandlePos, symbol.size2);
            }
            var color2 = isGradientColor ? Color32.Lerp(m_FillStyle.color, m_FillStyle.toColor, context.fillRate1) : symbolColor;
            ChartDrawer.DrawSymbol(vh, symbol.type, size, borderWidth, context.maxHandlePos, color2, color2, symbol.gap,
                null, emptyColor, backgroundColor, symbolColor, 2, context.maxHandlePos, symbol.size2);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            if (!CanDrag()) return;
            Vector2 localPos;
            if (!ScreenPointToChartPoint(eventData.position, out localPos)) return;
            context.isMinDraging = false;
            context.isMaxDraging = false;
            if (m_ValueType == ValueType.Range)
            {
                var dist = Vector2.Distance(localPos, context.minHandlePos);
                if (dist < context.handleSize * 2)
                {
                    context.isMinDraging = true;
                }
                dist = Vector2.Distance(localPos, context.maxHandlePos);
                if (dist < context.handleSize * 2)
                {
                    context.isMaxDraging = true;
                }
            }
            else
            {
                var dist = Vector2.Distance(localPos, context.maxHandlePos);
                if (dist < context.handleSize * 2)
                {
                    context.isMaxDraging = true;
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            if (!CanDrag()) return;
            if (!context.isMinDraging && !context.isMaxDraging) return;
            if (m_ValueType == ValueType.Enum)
            {
                CheckEnum(eventData);
            }
            else
            {
                CheckValue(eventData, context.isMinDraging);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (!context.isMinDraging && !context.isMaxDraging) return;
            var isMinDraging = context.isMinDraging;
            context.isMinDraging = false;
            context.isMaxDraging = false;
            if (!CanDrag()) return;
            if (Check(eventData, isMinDraging))
            {
                if (m_OnValueChangedComplete != null) m_OnValueChangedComplete(m_Value[1]);
                if (m_OnRangeChangedComplete != null) m_OnRangeChangedComplete(m_Value[0], m_Value[1]);
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (isDragingClick) return;
            if (!m_Interactable || context.isMinDraging || context.isMaxDraging) return;
            bool isMinDraging = false;
            if (m_ValueType == ValueType.Range)
            {
                Vector2 localPos;
                if (!ScreenPointToChartPoint(eventData.position, out localPos)) return;
                var centerX = (context.minHandlePos.x + context.maxHandlePos.x) / 2;
                if (localPos.x >= context.drawRect.x && localPos.x <= centerX)
                {
                    isMinDraging = true;
                }
            }
            if (Check(eventData, isMinDraging))
            {
                if (m_OnValueChangedComplete != null) m_OnValueChangedComplete(m_Value[1]);
                if (m_OnRangeChangedComplete != null) m_OnRangeChangedComplete(m_Value[0], m_Value[1]);
            }
        }

        private bool CanDrag()
        {
            return m_Interactable && m_HandleStyle.show;// && m_ValueType != ValueType.Enum;
        }

        private bool Check(PointerEventData eventData, bool isMinDraging)
        {
            bool flag = false;
            switch (m_ValueType)
            {
                case ValueType.Default:
                    flag = CheckValue(eventData, isMinDraging);
                    break;
                case ValueType.Range:
                    flag = CheckValue(eventData, isMinDraging);
                    break;
                case ValueType.Enum:
                    flag = CheckEnum(eventData);
                    break;
            }
            return flag;
        }

        private bool CheckValue(PointerEventData eventData, bool value0)
        {
            Vector2 localPos;
            if (!ScreenPointToChartPoint(eventData.position, out localPos)) return false;
            var rate = (localPos.x - context.startLogicPos.x) / (context.endLogicPos.x - context.startLogicPos.x);
            if (value0)
            {
                m_Value[0] = ChartHelper.GetMinCeilRate(m_Min + (m_Max - m_Min) * rate, m_Step);
            }
            else
            {
                m_Value[1] = ChartHelper.GetMaxCeilRate(m_Min + (m_Max - m_Min) * rate, m_Step);
            }
            if (m_OnValueChanged != null) m_OnValueChanged(m_Value[1]);
            if (m_OnRangeChanged != null) m_OnRangeChanged(m_Value[0], m_Value[1]);
            SetVerticesDirty();
            return true;
        }

        private bool CheckEnum(PointerEventData eventData)
        {
            Vector2 localPos;
            if (!ScreenPointToChartPoint(eventData.position, out localPos)) return false;
            var rate = (localPos.x - context.drawRect.x) / context.drawRect.width;
            var count = m_MarkValues.Count;
            var index = (int)(count * rate);
            if (index < 0) index = 0;
            if (index >= count) index = count - 1;
            m_Value[1] = m_MarkValues[index];
            if (m_OnValueChanged != null) m_OnValueChanged(m_Value[1]);
            if (m_OnRangeChanged != null) m_OnRangeChanged(m_Value[0], m_Value[1]);
            SetVerticesDirty();
            return true;
        }

        private void InitValueLabel()
        {
            var labelObject = ChartHelper.AddObject("valuelabel", transform, graphMinAnchor, graphMaxAnchor, graphPivot, graphSizeDelta, -1, childrenNodeNames);
            labelObject.hideFlags = chartHideFlags;

            var count = m_MarkValues.Count;
            var labelStyle = m_MarkStyle.label;
            var formatter = labelStyle.formatter;
            var numericFormatter = labelStyle.numericFormatter;
            if (count > 20) count = 20;
            context.markLabels.Clear();
            UpdateMarkPos();
            ChartHelper.HideAllObject(labelObject);
            for (int i = 0; i < count; i++)
            {
                var value = m_MarkValues[i];
                var pos = context.markPos[i];
                var text = i < m_MarkNames.Count ? m_MarkNames[i] : GetFormatterContent(formatter, numericFormatter, value);
                var labelTheme = theme.sharedTheme.axis;
                var label = ChartHelper.AddChartLabel("label" + i, labelObject.transform, labelStyle, labelTheme, text, Color.clear, TextAnchor.MiddleCenter);
                label.SetPosition(pos + labelStyle.offset);
                label.SetActive(m_MarkStyle.show && labelStyle.show, true);
                context.markLabels.Add(label);
            }
        }

        private string GetFormatterContent(string formatter, string numericFormatter, float value)
        {
            if (string.IsNullOrEmpty(formatter))
                return ChartCached.FloatToStr(value, numericFormatter);
            else
            {
                var content = formatter;
                FormatterHelper.ReplaceAxisLabelContent(ref content, numericFormatter, value);
                return content;
            }
        }

        private void UpdateValueLabel()
        {
            var count = m_MarkValues.Count;
            for (int i = 0; i < count; i++)
            {
                if (i >= context.markLabels.Count) break;
                var pos = context.markPos[i];
                var label = context.markLabels[i];
                label.SetPosition(pos + m_MarkStyle.label.offset);
            }
        }
    }
}