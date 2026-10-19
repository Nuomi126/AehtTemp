using System;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 时间选择UI组件。
    /// 输入或选择时间的控件。当用户需要输入一个时间，可以点击标准输入框，弹出时间面板进行选择。
    /// </summary>
    [Serializable]
    public class UITimePicker : UIComponent
    {

        protected override void Update()
        {
            base.Update();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSize(400, 200);
        }
#endif

        protected override void InitComponent()
        {
            base.InitComponent();
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            base.OnDrawPainterBase(vh, painter);
        }
    }
}