
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.Gauges
{
    [System.Serializable]
    public class GaugeAnchor : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private bool m_ShowAbove = true;
        [SerializeField] private SymbolType m_Type = SymbolType.EmptyCircle;
        [SerializeField] private float m_Size = 6;
        [SerializeField] private float m_Gap = 0;
        [SerializeField] private Sprite m_Image;
        [SerializeField] private Image.Type m_ImageType;
        [SerializeField] private Vector2 m_OffsetCenter = new Vector2(0, 0);
        [SerializeField] private ItemStyle m_ItemStyle = new ItemStyle();

        /// <summary>
        /// Whether to display the anchor.
        /// ||是否显示指针固定点。
        /// </summary>
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// 固定点是否显示在指针上面。
        /// </summary>
        public bool showAbove
        {
            get { return m_ShowAbove; }
            set { if (PropertyUtil.SetStruct(ref m_ShowAbove, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the type of symbol.
        /// ||标记类型。
        /// </summary>
        public SymbolType type
        {
            get { return m_Type; }
            set { if (PropertyUtil.SetStruct(ref m_Type, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// 自定义的标记图形。
        /// </summary>
        public Sprite image
        {
            get { return m_Image; }
            set { if (PropertyUtil.SetClass(ref m_Image, value)) SetAllDirty(); }
        }
        public Image.Type imageType
        {
            get { return m_ImageType; }
            set { if (PropertyUtil.SetStruct(ref m_ImageType, value)) SetAllDirty(); }
        }
        /// <summary>
        /// The offset position relative to the center.
        /// ||相对于中心的偏移位置。
        /// </summary>
        public Vector2 offsetCenter
        {
            get { return m_OffsetCenter; }
            set { if (PropertyUtil.SetStruct(ref m_OffsetCenter, value)) SetComponentDirty(); }
        }
        /// <summary>
        /// The style of data item.
        /// ||图形样式。
        /// </summary>
        public ItemStyle itemStyle
        {
            get { return m_ItemStyle; }
            set { if (PropertyUtil.SetClass(ref m_ItemStyle, value, true)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the size of symbol.
        /// ||标记的大小。
        /// </summary>
        public float size
        {
            get { return m_Size; }
            set { if (PropertyUtil.SetStruct(ref m_Size, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the gap of symbol and line segment.
        /// ||图形标记和线条的间隙距离。
        /// </summary>
        public float gap
        {
            get { return m_Gap; }
            set { if (PropertyUtil.SetStruct(ref m_Gap, value)) SetVerticesDirty(); }
        }

        public Vector3 GetPosition(Vector3 center, float radius)
        {
            var offsetX = m_OffsetCenter.x >= -1f && m_OffsetCenter.x <= 1f ? m_OffsetCenter.x * radius : m_OffsetCenter.x;
            var offsetY = m_OffsetCenter.y >= -1f && m_OffsetCenter.y <= 1f ? m_OffsetCenter.y * radius : m_OffsetCenter.y;
            return center + new Vector3(offsetX, offsetY);
        }
    }
}