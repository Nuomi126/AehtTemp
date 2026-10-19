using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// ui component of toggle. 
    /// UI开关组件。
    /// </summary>
    [Serializable]
    [HelpURL("https://xcharts-team.github.io/docs/ui/ui_toggle")]
    public class UIToggle : UIComponent
    {
        [SerializeField] private bool m_IsOn = false;
        [SerializeField] private bool m_Interactable = true;
        [SerializeField] private string m_Text = "";
        [SerializeField] private Color32 m_OnColor = new Color32(83, 158, 241, 255);
        [SerializeField] private Color32 m_OffColor = new Color32(198, 198, 198, 255);
        [SerializeField] private Color32 m_MarkColor = new Color32(255, 255, 255, 255);
        [SerializeField] private float m_MarkGap = 5f;
        [SerializeField] private BorderStyle m_BorderStyle = new BorderStyle() { show = true, cornerRadius = new float[] { 1, 1, 1, 1 } };
        [SerializeField] private LabelStyle m_LabelStyle = new LabelStyle();
        private Action<bool> m_OnValueChanged;

        /// <summary>
        /// Whether the toggle is on.
        /// ||是否开启。
        /// </summary>
        public bool isOn { get { return m_IsOn; } set { m_IsOn = value; SetVerticesDirty(); } }
        /// <summary>
        /// Whether the toggle is interactable.
        /// ||是否可交互。
        /// </summary>
        public bool interactable { get { return m_Interactable; } set { m_Interactable = value; SetVerticesDirty(); } }
        /// <summary>
        /// The text of toggle.
        /// ||开关文本。
        /// </summary> 
        public string text { get { return m_Text; } set { m_Text = value; SetAllDirty(); } }
        /// <summary>
        /// The color of toggle when it is on.
        /// ||开启时的颜色。
        /// </summary>
        public Color32 onColor { get { return m_OnColor; } set { m_OnColor = value; SetVerticesDirty(); } }
        /// <summary>
        /// The color of toggle when it is off.
        /// ||关闭时的颜色。
        /// <summary> 
        public Color32 offColor { get { return m_OffColor; } set { m_OffColor = value; SetVerticesDirty(); } }
        /// <summary>
        /// The color of mark.
        /// ||标记颜色。
        /// </summary>
        public Color32 markColor { get { return m_MarkColor; } set { m_MarkColor = value; SetVerticesDirty(); } }
        /// <summary>
        /// The gap between mark and border.
        /// ||标记与边框的间隔。
        public float markGap { get { return m_MarkGap; } set { m_MarkGap = value; SetVerticesDirty(); } }
        /// <summary>
        /// The border style of toggle.
        /// ||边框样式。
        /// </summary>
        public BorderStyle borderStyle { get { return m_BorderStyle; } set { m_BorderStyle = value; SetVerticesDirty(); } }
        /// <summary>
        /// The text style of toggle.
        /// ||文本样式。
        /// </summary> 
        public LabelStyle labelStyle { get { return m_LabelStyle; } set { m_LabelStyle = value; SetAllDirty(); } }

        /// <summary>
        /// The callback function when the value of toggle changed.
        /// ||开关值改变时的回调函数。
        /// </summary>
        public Action<bool> onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSize(160, 80);
            m_Background.show = false;
            m_LabelStyle.textStyle.autoAlign = false;
        }
#endif

        protected override void InitComponent()
        {
            base.InitComponent();
            ChartHelper.AddChartLabel("Text", transform, labelStyle, theme.sharedTheme.common,
                m_Text, Color.clear, TextAnchor.MiddleCenter);
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            var backgroundColor = m_IsOn ? m_OnColor : m_OffColor;
            var borderWidth = m_BorderStyle.GetRuntimeBorderWidth();
            var borderColor = m_BorderStyle.GetRuntimeBorderColor();
            var cornerRadius = m_BorderStyle.GetRuntimeCornerRadius();
            UGL.DrawRoundRectangleWithBorder(vh, graphRect, backgroundColor, backgroundColor, cornerRadius,
                borderWidth, borderColor);

            var radius = Mathf.Min(m_GraphWidth, m_GraphHeight) / 2 - m_MarkGap;
            var pos = m_IsOn
                ? new Vector3(m_GraphX + m_GraphWidth - radius - m_MarkGap, m_GraphY + m_GraphHeight / 2)
                : new Vector3(m_GraphX + radius + m_MarkGap, m_GraphY + m_GraphHeight / 2);

            if (m_BorderStyle.IsCricle())
            {
                UGL.DrawCricle(vh, pos, radius, m_MarkColor);
            }
            else
            {
                UGL.DrawRoundRectangle(vh, pos, radius * 2, radius * 2, m_MarkColor, m_MarkColor, 0, cornerRadius);
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (m_Interactable)
            {
                m_IsOn = !m_IsOn;
                SetVerticesDirty();
                if (m_OnValueChanged != null) m_OnValueChanged(m_IsOn);
            }
        }
    }
}