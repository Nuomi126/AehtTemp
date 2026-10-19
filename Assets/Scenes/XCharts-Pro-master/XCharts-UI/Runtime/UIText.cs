using System;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// ui component of text. 
    /// UI文本组件。
    /// </summary>
    [Serializable]
    [HelpURL("https://xcharts-team.github.io/docs/ui/ui_text")]
    public class UIText : UIComponent
    {
        [SerializeField] private string m_Text = "Text";
        [SerializeField] private TextStyle m_TextStyle = new TextStyle() { autoAlign = false };
        private ChartText m_TextObject;

        /// <summary>
        /// The text content.
        /// ||文本内容。
        /// </summary>
        public string text
        {
            get { return m_Text; }
            set
            {
                m_Text = value;
                if (m_TextObject != null) m_TextObject.SetText(m_Text);
                else SetAllComponentDirty();
            }
        }
        /// <summary>
        /// The text style.
        /// ||文本样式。
        /// </summary>
        public TextStyle textStyle
        {
            get { return m_TextStyle; }
            set { m_TextStyle = value; SetVerticesDirty(); }
        }

        protected override void Update()
        {
            base.Update();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            raycastTarget = false;
            SetSize(100, 20);
            m_Text = "Text";
            SetAllComponentDirty();
        }
#endif

        protected override void InitPainter()
        {
            if (background.show)
            {
                base.InitPainter();
            }
            else
            {
                ChartHelper.DestoryGameObject(transform, "painter_b");
            }
        }

        protected override void InitComponent()
        {
            base.InitComponent();

            Vector2 anchorMin, anchorMax, pivot;
            var sizeDelta = new Vector2(m_GraphWidth, m_GraphHeight);
            var autoAlignment = TextAnchor.MiddleCenter;
            var alignment = m_TextStyle.GetAlignment(autoAlignment);
            ChartHelper.UpdateAnchorAndPivotByTextAlignment(alignment, out anchorMin, out anchorMax, out pivot);
            m_TextObject = ChartHelper.AddTextObject("Text", gameObject.transform, anchorMin, anchorMax, pivot,
                sizeDelta, textStyle, m_Theme.sharedTheme.common, Color.clear, autoAlignment);
            m_TextObject.gameObject.hideFlags = chartHideFlags;
            m_TextObject.SetText(m_Text);
        }
    }
}