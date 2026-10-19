using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 表格运行时上下文。
    /// </summary>
    public class TableContext
    {
        public GameObject viewport;
        /// <summary>
        /// 左下角坐标x
        /// </summary>
        public float x;
        /// <summary>
        /// 左下角坐标y
        /// </summary>
        public float y;
        /// <summary>
        /// 宽
        /// </summary>
        public float width;
        /// <summary>
        /// 高
        /// </summary>
        public float height;
        /// <summary>
        /// 行的总宽（扣除行表头）
        /// </summary>
        public float rowWidth;
        /// <summary>
        /// 行的总高（扣除列表头）
        /// </summary>
        public float rowHeight;
        /// <summary>
        /// 左下角坐标
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// 中心坐标
        /// </summary>
        public Vector3 center;
        public float left;
        public float right;
        public float bottom;
        public float top;
        /// <summary>
        /// 水平滑动偏移
        /// </summary>
        public float scrollHorizonal;
        /// <summary>
        /// 垂直滑动偏移
        /// </summary>
        public float scrollVertical;
        /// <summary>
        /// 选择的行
        /// </summary>
        public int selectRow = -1;
        /// <summary>
        /// 选中的列
        /// </summary>
        public int selectColumn = -1;
        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool isDraging = false;
        public bool isDragingHorizontalScrollbar = false;
        public bool isDragingVerticalScrollbar = false;
        public Rect horizontalScrollbarRect;
        public Rect verticalScrollbarRect;
        /// <summary>
        /// 列表头的Label
        /// </summary>
        public List<ChartLabel> columnHeaderLabels = new List<ChartLabel>();
        /// <summary>
        /// 行表头的Label
        /// </summary>
        public List<ChartLabel> rowHeaderLabels = new List<ChartLabel>();
        /// <summary>
        /// 数据的Label
        /// </summary>
        public List<ChartLabel> dataLabels = new List<ChartLabel>();

        public TableCarouselContext carousel = new TableCarouselContext();

        internal List<int> unvisibleIndexList = new List<int>();
    }
}