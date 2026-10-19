using UnityEngine;

namespace XCharts.Runtime.Pyramids
{
    [System.Serializable]
    public class PyramidStyle : ChildComponent
    {
        [SerializeField] protected bool m_Show = true;
        [SerializeField] protected bool m_3D = true;
        [SerializeField] protected bool m_DrawTop = true;
        [SerializeField] [Range(0, 1)] protected float m_BottomPointRate = 0.5f;
        [SerializeField] [Range(0, 1)] protected float m_LeftPointRate = 0.1f;
        [SerializeField] [Range(0, 1)] protected float m_RightPointRate = 0.1f;
        [SerializeField] [Range(0, 1)] protected float m_LeftColorOpacity = 0.9f;
        [SerializeField] [Range(0, 1)] protected float m_RightColorOpacity = 0.7f;
        [SerializeField] [Range(0, 1)] protected float m_TopColorOpacity = 0.99f;
        [SerializeField] protected float m_LabelLineMargin = 10f;

        /// <summary>
        /// 3D金字塔
        /// </summary>
        public bool draw3d
        {
            get { return m_3D; }
            set { m_3D = value; SetVerticesDirty(); }
        }
        public bool drawTop { get { return m_DrawTop; } set { m_DrawTop = value; SetVerticesDirty(); } }
        /// <summary>
        /// 3D模式下，底部中点占金字塔宽度的比例
        /// </summary>
        public float bottomPointRate
        {
            get { return m_BottomPointRate; }
            set { m_BottomPointRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// 3D模式下，底部左边点占金字塔高度的比例
        /// </summary>
        public float leftPointRate
        {
            get { return m_LeftPointRate; }
            set { m_LeftPointRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// 3D模式下，底部右边点占金字塔高度的比例
        /// </summary>
        public float rightPointRate
        {
            get { return m_RightPointRate; }
            set { m_RightPointRate = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// 3D模式下，金字塔左边的颜色透明度
        /// </summary>
        public float leftColorOpacity
        {
            get { return m_LeftColorOpacity; }
            set { m_LeftColorOpacity = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// 3D模式下，金字塔右边的颜色透明度
        /// </summary>
        public float rightColorOpacity
        {
            get { return m_RightColorOpacity; }
            set { m_RightColorOpacity = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// 3D模式下，金字塔顶部的颜色透明度
        /// </summary>
        public float topColorOpacity
        {
            get { return m_TopColorOpacity; }
            set { m_TopColorOpacity = value; SetVerticesDirty(); }
        }
        /// <summary>
        /// Label标签距离右边的距离
        /// </summary>
        public float labelLineMargin
        {
            get { return m_LabelLineMargin; }
            set { m_LabelLineMargin = value; SetVerticesDirty(); }
        }
    }
}