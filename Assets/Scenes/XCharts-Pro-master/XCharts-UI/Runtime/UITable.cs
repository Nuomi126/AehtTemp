using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 表格UI组件。
    /// 用于展示行列数据。
    /// </summary>
    [Serializable]
    [HelpURL("https://xcharts-team.github.io/docs/ui/ui_table")]
    public partial class UITable : UIComponent
    {
        [SerializeField] private bool m_SupportScroll = true;
        [SerializeField] private bool m_SupportDrag = false;
        [SerializeField] private bool m_SupportSelect = true;
        [SerializeField][Range(1f, 20f)] private float m_ScrollSensitivity = 10f;
        [SerializeField] private Color32 m_SelectColor;
        [SerializeField] private Color32 m_SelectHeaderColor;
        [SerializeField] private BorderStyle m_BorderStyle = new BorderStyle();
        [SerializeField] private TableSeparatorStyle m_SeparatorStyle = new TableSeparatorStyle();
        [SerializeField] private TableScrollbarStyle m_ScrollbarStyle = new TableScrollbarStyle();
        [SerializeField] private TableColumnHeaderStyle m_ColumnHeaderStyle = new TableColumnHeaderStyle();
        [SerializeField] private TableColumnStyle m_ColumnStyle = new TableColumnStyle();
        [SerializeField] private TableRowHeaderStyle m_RowHeaderStyle = new TableRowHeaderStyle() { show = true };
        [SerializeField] private TableRowStyle m_RowStyle = new TableRowStyle();
        [SerializeField] private TableCarouselStyle m_CarouselStyle = new TableCarouselStyle() { show = false };
        [SerializeField] private List<TableColumn> m_Columns = new List<TableColumn>();
        [SerializeField] private List<TableRow> m_Data = new List<TableRow>();

        public TableContext context = new TableContext();
        protected Painter m_TopPainter;
        protected bool m_NeedUpdatePosition;

        /// <summary>
        /// whether the table support scroll.
        /// ||是否支持滚动。
        /// </summary>
        public bool supportScroll
        {
            get { return m_SupportScroll; }
            set { if (PropertyUtil.SetStruct(ref m_SupportScroll, value)) SetAllDirty(); }
        }
        /// <summary>
        /// whether the table support drag. 
        /// ||是否支持拖拽。
        /// </summary>
        public bool supportDrag
        {
            get { return m_SupportDrag; }
            set { if (PropertyUtil.SetStruct(ref m_SupportDrag, value)) SetAllDirty(); }
        }
        /// <summary>
        /// whether the table support select. select row, column, cell.
        /// ||是否支持选中。选中行，列，单元格。
        /// </summary>
        public bool supportSelect
        {
            get { return m_SupportSelect; }
            set { if (PropertyUtil.SetStruct(ref m_SupportSelect, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the color of selected row, column, cell.
        /// ||选中时的颜色。
        /// </summary>
        public Color32 selectColor
        {
            get { return m_SelectColor; }
            set { if (PropertyUtil.SetColor(ref m_SelectColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the color of selected header.
        /// ||选中时表头的颜色。
        /// </summary>
        public Color32 selectHeaderColor
        {
            get { return m_SelectHeaderColor; }
            set { if (PropertyUtil.SetColor(ref m_SelectHeaderColor, value)) SetVerticesDirty(); }
        }
        /// <summary>
        /// the scroll sensitivity.
        /// ||滚动灵敏度。
        /// </summary>
        public float scrollSensitivity
        {
            get { return m_ScrollSensitivity; }
            set { if (PropertyUtil.SetStruct(ref m_ScrollSensitivity, value)) SetAllDirty(); }
        }
        /// <summary>
        /// the style of row header.
        /// ||行表头样式。
        /// </summary>
        public TableRowHeaderStyle rowHeaderStyle { get { return m_RowHeaderStyle; } set { m_RowHeaderStyle = value; } }
        /// <summary>
        /// the style of row.
        /// ||行样式。
        /// </summary>
        public TableRowStyle rowStyle { get { return m_RowStyle; } set { m_RowStyle = value; } }
        /// <summary>
        /// the style of column header. 
        /// ||列表头样式。
        /// </summary>
        public List<TableColumn> columns { get { return m_Columns; } set { m_Columns = value; } }
        /// <summary>
        /// the data of table.
        /// ||表格数据。
        /// </summary>
        public List<TableRow> data { get { return m_Data; } set { m_Data = value; } }
        /// <summary>
        /// the border style of table.
        /// ||表格边框样式。
        /// </summary>
        public BorderStyle borderStyle { get { return m_BorderStyle; } set { m_BorderStyle = value; } }
        /// <summary>
        /// the default style of column header.
        /// ||默认的列头样式。
        /// </summary>
        public TableColumnHeaderStyle columnHeaderStyle { get { return m_ColumnHeaderStyle; } set { m_ColumnHeaderStyle = value; } }
        /// <summary>
        /// the column style of table.
        /// ||列样式。
        /// </summary>
        public TableColumnStyle columnStyle { get { return m_ColumnStyle; } set { m_ColumnStyle = value; } }
        /// <summary>
        /// the style of separator.
        /// ||分割线样式。
        /// </summary>
        public TableSeparatorStyle separatorStyle { get { return m_SeparatorStyle; } set { m_SeparatorStyle = value; } }
        /// <summary>
        /// the style of scrollbar.
        /// ||滚动条样式。
        /// </summary>
        public TableScrollbarStyle scrollbarStyle { get { return m_ScrollbarStyle; } set { m_ScrollbarStyle = value; } }
        /// <summary>
        /// the style of carousel.
        /// ||轮播样式。
        /// </summary>
        public TableCarouselStyle carouselStyle { get { return m_CarouselStyle; } set { m_CarouselStyle = value; } }

        public override void RefreshGraph()
        {
            base.RefreshGraph();
            m_TopPainter.Refresh();
        }

        public virtual void RefreshPosition()
        {
            m_NeedUpdatePosition = true;
        }

        protected override void Update()
        {
            base.Update();
            TableCarouselHandler.UdateCarousel(this);
            if (m_NeedUpdatePosition)
            {
                m_NeedUpdatePosition = false;
                TableHandler.UpdateColumnHeaderPosition(this);
                TableHandler.UpdateRowHeaderPosition(this);
                TableHandler.UpdateDataCellPosition(this);
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            SetSize(800, 500);
            base.Reset();
            m_Columns.Clear();
            m_Columns.Add(new TableColumn() { content = "Column1" });
            m_Columns.Add(new TableColumn() { content = "Column2" });
            m_Columns.Add(new TableColumn() { content = "Column3" });
            m_Columns.Add(new TableColumn() { content = "Column4" });
            m_Columns.Add(new TableColumn() { content = "Column5" });

            m_Data.Clear();
            for (int i = 0; i < 10; i++)
            {
                var data = new List<TableCell>();
                for (int j = 0; j < 5; j++)
                {
                    data.Add(new TableCell() { stringValue = "cell_" + i + "_" + j });
                }
                m_Data.Add(new TableRow() { data = data });
            }
        }
#endif

        protected override void CheckRefreshPainter()
        {
            base.CheckRefreshPainter();
            m_TopPainter.CheckRefresh();
        }

        protected override void InitComponent()
        {
            base.InitComponent();
            TableHandler.UpdateRuntimeData(this, m_GraphX, m_GraphY, m_GraphWidth, m_GraphHeight);
            TableHandler.InitViewport(this);
            TableHandler.InitTableHeader(this);
            TableHandler.InitRowHeader(this);
            TableHandler.InitDataCell(this);
            InitTopPainter();
            RefreshGraph();
        }

        protected override void DataDirty()
        {
            TableHandler.InitTableHeader(this);
            TableHandler.InitRowHeader(this);
            TableHandler.InitDataCell(this);
        }

        protected override void InitPainter()
        {
            TableHandler.InitViewport(this);
            if (context.viewport == null) return;
            m_Painter = ChartHelper.AddPainterObject("Painter", context.viewport.transform, m_GraphMinAnchor,
                m_GraphMaxAnchor, m_GraphPivot, new Vector2(m_GraphWidth, m_GraphHeight), chartHideFlags, 1, m_ChildNodeNames);
            m_Painter.type = Painter.Type.Base;
            m_Painter.onPopulateMesh = OnDrawPainterBase;
            m_Painter.transform.SetSiblingIndex(0);
        }

        protected virtual void InitTopPainter()
        {
            m_TopPainter = ChartHelper.AddPainterObject("Top", transform, m_GraphMinAnchor,
                m_GraphMaxAnchor, m_GraphPivot, new Vector2(m_GraphWidth, m_GraphHeight), chartHideFlags, 1, m_ChildNodeNames);
            m_TopPainter.type = Painter.Type.Top;
            m_TopPainter.onPopulateMesh = OnDrawPainterTop;
            m_TopPainter.transform.SetSiblingIndex(transform.childCount - 1);
        }

        protected override void OnDrawPainterBase(VertexHelper vh, Painter painter)
        {
            base.OnDrawPainterBase(vh, painter);
            TableHandler.DrawTableStyle(vh, this);
        }

        protected virtual void OnDrawPainterTop(VertexHelper vh, Painter painter)
        {
            vh.Clear();
            TableHandler.DrawTableTop(vh, this);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            TableHandler.UpdateRuntimeData(this, m_GraphX, m_GraphY, m_GraphWidth, m_GraphHeight);
            TableHandler.InitViewport(this);
            TableHandler.InitTableHeader(this);
            TableHandler.InitRowHeader(this);
            TableHandler.InitDataCell(this);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            context.isDraging = true;
            TableHandler.OnBeginDrag(this, eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            TableHandler.OnDrag(this, eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            context.isDraging = false;
            TableHandler.OnEndDrag(this, eventData);
        }

        public override void OnScroll(PointerEventData eventData)
        {
            base.OnScroll(eventData);
            TableHandler.OnScroll(this, eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (isDragingClick) return;
            TableHandler.OnPointerClick(this, eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            TableHandler.OnPointerEnter(this);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            TableHandler.OnPointerExit(this);
        }
    }
}