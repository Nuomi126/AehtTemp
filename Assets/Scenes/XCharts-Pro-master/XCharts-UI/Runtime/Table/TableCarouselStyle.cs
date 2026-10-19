using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// table carousel type.
    /// ||表格轮播类型。
    /// </summary> 
    public enum TableCarouselType
    {
        /// <summary>
        /// pixel carousel.
        /// ||像素轮播。
        /// </summary>
        Pixel,
        /// <summary>
        /// single row carousel.
        /// ||单行轮播。
        /// </summary>
        Row,
        /// <summary>
        /// page carousel.
        /// ||分页轮播。
        /// </summary>
        Page
    }

    /// <summary>
    /// the play mode of carousel.
    /// ||轮播播放模式。 
    /// </summary>
    public enum TableCarouselMode
    {
        /// <summary>
        /// play loop.
        /// ||循环播放。 
        /// </summary>
        Loop
    }

    public class TableCarouselContext
    {
        public float progress = 0;
        public int currentRow = -1;
        public bool isEnd = false;
        internal bool pause = false;
        internal float height = 50;
        internal float totalHeight = 0;
        internal float currentHeight = 0;
        internal bool startWait = false;
        internal float startWaitTime = 0;
    }

    /// <summary>
    /// The table carousel style.
    /// ||表格轮播样式。
    /// </summary>
    [Serializable]
    public class TableCarouselStyle : ChildComponent
    {
        [SerializeField] private bool m_Show = true;
        [SerializeField] private TableCarouselType m_Type = TableCarouselType.Row;
        [SerializeField] private TableCarouselMode m_Mode = TableCarouselMode.Loop;
        [SerializeField] private int m_WaitTime = 2000;
        [SerializeField] private float m_Speed = 100;
        [SerializeField] private bool m_Pause = false;
        [SerializeField] private bool m_HoverPause = true;

        /// <summary>
        /// Whether to enable carousel.
        /// ||是否启用轮播。
        public bool show
        {
            get { return m_Show; }
            set { if (PropertyUtil.SetStruct(ref m_Show, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// The carousel type.
        /// ||轮播类型。
        /// </summary>   
        public TableCarouselType type
        {
            get { return m_Type; }
            set { if (PropertyUtil.SetStruct(ref m_Type, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// The play mode of carousel.
        /// ||轮播播放模式。
        /// </summary>
        public TableCarouselMode mode
        {
            get { return m_Mode; }
            set { if (PropertyUtil.SetStruct(ref m_Mode, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// wait time of carousel (ms).
        /// ||轮播等待时间（ms）。
        /// </summary> 
        public int waitTime
        {
            get { return m_WaitTime; }
            set { if (PropertyUtil.SetStruct(ref m_WaitTime, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// The carousel speed.
        /// ||轮播速度。 
        /// </summary>  
        public float speed
        {
            get { return m_Speed; }
            set { if (PropertyUtil.SetStruct(ref m_Speed, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// pause carousel.
        /// ||暂停轮播。 
        /// </summary>
        public bool pause
        {
            get { return m_Pause; }
            set { if (PropertyUtil.SetStruct(ref m_Pause, value)) SetVerticesDirty(); }
        }

        /// <summary>
        /// Whether to pause when hover.
        /// ||鼠标悬停时是否暂停。
        /// </summary>
        public bool hoverPause
        {
            get { return m_HoverPause; }
            set { if (PropertyUtil.SetStruct(ref m_HoverPause, value)) SetVerticesDirty(); }
        }
    }
}