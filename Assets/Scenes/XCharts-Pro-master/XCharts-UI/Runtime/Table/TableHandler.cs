using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 表格处理类。
    /// </summary>
    internal static class TableHandler
    {
        private static Dictionary<int, string> s_CellIndexNames = new Dictionary<int, string>();

        public static void InitViewport(UITable table)
        {
            if (table.context.viewport != null) return;
            var viewport = ChartHelper.AddObject("Viewport", table.transform, new Vector2(0, 0),
                new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), -1, table.childrenNodeNames);
            ChartHelper.EnsureComponent<RectMask2D>(viewport);
            viewport.hideFlags = table.chartHideFlags;
            table.context.viewport = viewport;
        }

        public static void InitTableHeader(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var verticalScrollbarWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var sizeDelta = new Vector2(table.context.width - rowHeaderWidth - verticalScrollbarWidth, columnHeaderHeight);
            var vector2 = new Vector2(0, 1);
            var headerGameObject = ChartHelper.AddObject("ColumnHeader", table.context.viewport.transform, vector2,
                vector2, vector2, sizeDelta);
            var rectTransfrom = headerGameObject.GetComponent<RectTransform>();
            headerGameObject.SetActive(TableHelper.IsNeedDrawColumnHeader(table));

            rectTransfrom.anchoredPosition3D = new Vector3(rowHeaderWidth, 0, 0);

            if (table.supportDrag || table.supportScroll)
                ChartHelper.EnsureComponent<RectMask2D>(headerGameObject);
            else
                ChartHelper.RemoveComponent<RectMask2D>(headerGameObject);

            var vector0_5 = new Vector2(0.5f, 0.5f);
            var headerBackgroundObject = ChartHelper.AddObject("Background", headerGameObject.transform, vector0_5,
                vector0_5, vector0_5, sizeDelta);
            var backgroundImage = ChartHelper.EnsureComponent<Image>(headerBackgroundObject);
            ChartHelper.UpdateRectTransform(headerBackgroundObject, table.graphMinAnchor,
                table.graphMaxAnchor, table.graphPivot, sizeDelta);
            ChartHelper.SetBackground(backgroundImage, table.columnHeaderStyle.background);
            backgroundImage.transform.SetSiblingIndex(0);
            if (table.columns.Count < table.context.columnHeaderLabels.Count)
            {
                foreach (var label in table.context.columnHeaderLabels)
                {
                    if (label != null)
                        label.SetActive(false);
                }
            }
            table.context.columnHeaderLabels.Clear();
            for (int i = 0; i < table.columns.Count; i++)
            {
                var colWidth = TableHelper.GetColumnWidth(table, i);
                var labelStyle = TableHelper.GetHeaderLabelStyle(table, i);
                var label = ChartHelper.AddChartLabel2("header" + i, headerGameObject.transform, labelStyle,
                    table.theme.sharedTheme.title, table.columns[i].content, Color.clear, labelStyle.textStyle.alignment);
                table.context.columnHeaderLabels.Add(label);
                label.SetActive(labelStyle.show);
                label.SetSize(colWidth, columnHeaderHeight);

            }
            UpdateColumnHeaderPosition(table);
        }

        public static void UpdateColumnHeaderPosition(UITable table)
        {
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var currentTotalWidth = -table.context.scrollHorizonal;
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderStartX = table.context.x + rowHeaderWidth;
            var columnHeaderStartY = table.context.y + table.context.height - columnHeaderHeight;
            for (int i = 0; i < table.context.columnHeaderLabels.Count; i++)
            {
                var colWidth = TableHelper.GetColumnWidth(table, i);
                var label = table.context.columnHeaderLabels[i];
                var labelStyle = TableHelper.GetHeaderLabelStyle(table, i);
                var pos = new Vector3(currentTotalWidth + colWidth / 2, -columnHeaderHeight / 2) + labelStyle.offset;
                var rectX = columnHeaderStartX + currentTotalWidth;
                currentTotalWidth += colWidth;
                if (rectX > table.context.x - colWidth && rectX < table.context.x + table.context.width + colWidth)
                {
                    label.SetActive(true);
                    label.SetPosition(pos);
                    label.rect = new Rect(rectX, columnHeaderStartY, label.GetWidth(), label.GetHeight());
                }
                else
                {
                    label.SetActive(false);
                }
            }
        }

        public static void InitRowHeader(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var horizontalScrollbarWidth = TableHelper.GetScrollbarHorizontalWidth(table);
            var sizeDelta = new Vector2(rowHeaderWidth, table.context.height - columnHeaderHeight - horizontalScrollbarWidth);
            var vector2 = new Vector2(0, 1);
            var rowHeaderObject = ChartHelper.AddObject("RowHeader", table.context.viewport.transform, vector2,
                vector2, vector2, sizeDelta);
            var rectTransfrom = rowHeaderObject.GetComponent<RectTransform>();
            rowHeaderObject.SetActive(rowHeaderWidth > 0);
            rectTransfrom.anchoredPosition3D = new Vector3(0, -columnHeaderHeight, 0);

            if (table.supportDrag || table.supportScroll)
                ChartHelper.EnsureComponent<RectMask2D>(rowHeaderObject);
            else
                ChartHelper.RemoveComponent<RectMask2D>(rowHeaderObject);

            var labelStyle = table.rowHeaderStyle.labelStyle;
            if (table.data.Count < table.context.rowHeaderLabels.Count)
            {
                foreach (var label in table.context.rowHeaderLabels)
                {
                    if (label != null)
                        label.SetActive(false);
                }
            }
            table.context.rowHeaderLabels.Clear();
            for (int i = 0; i < table.data.Count; i++)
            {
                var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                var content = TableHelper.GetRowHeaderContent(table, i);
                var labelName = ChartCached.GetString("header", i);
                var label = ChartHelper.AddChartLabel2(labelName, rowHeaderObject.transform, labelStyle,
                    table.theme.sharedTheme.title, content, Color.clear, labelStyle.textStyle.alignment);
                table.context.rowHeaderLabels.Add(label);
                label.SetActive(labelStyle.show);
                label.SetSize(rowHeaderWidth, rowHeight);
            }
            UpdateRowHeaderPosition(table);
        }

        public static void UpdateRowHeaderPosition(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            var carouselHeight = TableHelper.GetCarouselProgress(table);
            var labelStyle = table.rowHeaderStyle.labelStyle;
            var rowHeaderStartX = table.context.x;
            var rowHeaderStartY = table.context.y + table.context.height - columnHeaderHeight + table.context.scrollVertical + carouselHeight;
            var rowTotalHeight = 0f;
            var rowGap = table.rowStyle.rowGap;

            var anyActive = false;
            var lastY = 0f;
            table.context.unvisibleIndexList.Clear();
            for (int i = 0; i < table.context.rowHeaderLabels.Count; i++)
            {
                var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                var label = table.context.rowHeaderLabels[i];
                var rowY = -rowTotalHeight - 0.5f * rowHeight;
                rowY += carouselHeight;
                var pos = new Vector3(rowHeaderWidth / 2, rowY + table.context.scrollVertical);

                rowTotalHeight += rowHeight + rowGap;
                var rectY = rowHeaderStartY - rowTotalHeight;
                if (rectY > table.context.y - rowHeight && rectY < table.context.y + table.context.rowHeight)
                {
                    lastY = rowY;
                    anyActive = true;
                    label.SetActive(true);
                    label.SetPosition(pos + labelStyle.offset);
                    label.rect = new Rect(rowHeaderStartX, rectY, label.GetWidth(), label.GetHeight());
                }
                else
                {
                    if (!anyActive && carouselHeight != 0 && !table.context.unvisibleIndexList.Contains(i))
                    {
                        table.context.unvisibleIndexList.Add(i);
                    }
                    label.SetActive(false);
                }
            }
            if (carouselHeight != 0)
            {
                foreach (var i in table.context.unvisibleIndexList)
                {
                    var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                    var label = table.context.rowHeaderLabels[i];
                    var rowY = lastY - rowHeight - table.rowStyle.rowGap;
                    rowTotalHeight += rowHeight + rowGap;
                    var pos = new Vector3(rowHeaderWidth / 2, rowY + table.context.scrollVertical);
                    var rectY = rowHeaderStartY - rowTotalHeight;
                    if (rectY > table.context.y - rowHeight && rectY < table.context.y + table.context.height + rowHeight)
                    {
                        label.SetActive(true);
                        label.SetPosition(pos + labelStyle.offset);
                        label.rect = new Rect(rowHeaderStartX, rectY, label.GetWidth(), label.GetHeight());
                        lastY = rowY;
                    }
                    else
                    {
                        label.SetActive(false);
                    }
                }
            }
        }

        public static void InitDataCell(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var verticalScrollbarWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var horizontalScrollbarWidth = TableHelper.GetScrollbarHorizontalWidth(table);
            var sizeDelta = new Vector2(table.context.width - rowHeaderWidth - verticalScrollbarWidth,
                table.context.height - columnHeaderHeight - horizontalScrollbarWidth);
            var vector2 = new Vector2(0, 1);
            var cellObject = ChartHelper.AddObject("Cell", table.context.viewport.transform, vector2,
                vector2, vector2, sizeDelta);
            var rectTransfrom = cellObject.GetComponent<RectTransform>();
            rectTransfrom.anchoredPosition3D = new Vector3(rowHeaderWidth, -columnHeaderHeight, 0);

            if (table.supportDrag || table.supportScroll)
                ChartHelper.EnsureComponent<RectMask2D>(cellObject);
            else
                ChartHelper.RemoveComponent<RectMask2D>(cellObject);

            if (table.data.Count < table.context.dataLabels.Count)
            {
                foreach (var label in table.context.dataLabels)
                {
                    if (label != null)
                        label.SetActive(false);
                }
            }
            table.context.dataLabels.Clear();
            for (int i = 0; i < table.data.Count; i++)
            {
                var rowData = table.data[i];
                var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                for (int j = 0; j < table.columns.Count; j++)
                {
                    var cellData = j < rowData.data.Count ? rowData.data[j] : null;
                    var colWidth = TableHelper.GetColumnWidth(table, j);
                    var labelStyle = TableHelper.GetDataLabelStyle(table, j);
                    var content = j < rowData.data.Count ? cellData.GetContent() : "";
                    var label = ChartHelper.AddChartLabel2(GetCellIndexName(i, j), cellObject.transform, labelStyle,
                        table.theme.sharedTheme.common, content, Color.clear, labelStyle.textStyle.alignment);
                    if (cellData != null && cellData.type == DataType.Sprite)
                    {
                        label.SetIconActive(true);
                        label.SetIconSprite(cellData.spriteValue);
                    }
                    table.context.dataLabels.Add(label);
                    label.SetActive(labelStyle.show);
                    label.SetSize(colWidth, rowHeight);
                }
            }
            UpdateDataCellPosition(table);
        }

        private static string GetCellIndexName(int i, int j)
        {
            var index = i * 1000000 + j;
            if (s_CellIndexNames.ContainsKey(index))
                return s_CellIndexNames[index];
            else
            {
                var name = "cell_" + i + "_" + j;
                s_CellIndexNames.Add(index, name);
                return name;
            }
        }

        public static void UpdateDataCellPosition(UITable table)
        {
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            var columnEachWidth = TableHelper.GetColumnAutoEachWidth(table);
            var columnHeaderStartX = table.context.x + TableHelper.GetRowHeaderWidth(table);
            var carouselHeight = TableHelper.GetCarouselProgress(table);
            var rowHeaderStartY = table.context.y + table.context.height - TableHelper.GetColumnHeaderHeight(table) + table.context.scrollVertical;
            var rowTotalHeight = 0f;
            var rowGap = table.rowStyle.rowGap;
            var anyActive = false;
            var lastY = 0f;
            table.context.unvisibleIndexList.Clear();
            for (int i = 0; i < table.data.Count; i++)
            {
                var rowData = table.data[i];
                var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                var currentTotalWidth = -table.context.scrollHorizonal;
                var rowY = -rowTotalHeight - rowHeight * 0.5f + table.context.scrollVertical;
                rowY += carouselHeight;
                rowTotalHeight += rowHeight + rowGap;
                for (int j = 0; j < table.columns.Count; j++)
                {
                    var colWidth = TableHelper.GetColumnWidth(table, j, columnEachWidth);
                    var label = table.context.dataLabels[i * table.columns.Count + j];
                    var labelStyle = TableHelper.GetDataLabelStyle(table, j);
                    var rectX = columnHeaderStartX + currentTotalWidth;
                    var rectY = rowHeaderStartY - rowTotalHeight + rowGap;
                    rectY += carouselHeight;
                    var yInRange = rectY > table.context.y - rowHeight && rectY < table.context.y + table.context.rowHeight;
                    var xInRange = rectX > table.context.x - colWidth && rectX < table.context.x + table.context.width + colWidth;
                    if (xInRange && yInRange)
                    {
                        anyActive = true;
                        label.SetActive(true);
                        label.SetPosition(new Vector3(currentTotalWidth + colWidth / 2, rowY) + labelStyle.offset);
                        label.rect = new Rect(rectX, rectY, colWidth, rowHeight);
                    }
                    else
                    {
                        if (!anyActive && carouselHeight != 0 && !table.context.unvisibleIndexList.Contains(i))
                        {
                            table.context.unvisibleIndexList.Add(i);
                        }
                        label.SetActive(false);
                    }
                    currentTotalWidth += colWidth;
                    lastY = rowY;
                }
            }
            if (carouselHeight != 0)
            {
                foreach (var i in table.context.unvisibleIndexList)
                {
                    var rowData = table.data[i];
                    var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                    var currentTotalWidth = -table.context.scrollHorizonal;
                    var rowY = lastY - rowHeight - rowGap;
                    rowTotalHeight += rowHeight + rowGap;
                    for (int j = 0; j < table.columns.Count; j++)
                    {
                        var label = table.context.dataLabels[i * table.columns.Count + j];
                        var colWidth = TableHelper.GetColumnWidth(table, j, columnEachWidth);
                        var labelStyle = TableHelper.GetDataLabelStyle(table, j);
                        var rectX = columnHeaderStartX + currentTotalWidth;
                        var rectY = rowHeaderStartY - rowTotalHeight + rowGap;
                        rectY += carouselHeight;
                        var yInRange = rectY > table.context.y - rowHeight && rectY < table.context.y + table.context.rowHeight;
                        if (yInRange)
                        {
                            label.SetActive(true);
                            label.SetPosition(new Vector3(currentTotalWidth + colWidth / 2, rowY) + labelStyle.offset);
                            label.rect = new Rect(rectX, rectY, colWidth, rowHeight);
                            currentTotalWidth += colWidth;
                            lastY = rowY;
                        }
                        else
                        {
                            label.SetActive(false);
                        }
                    }
                }
            }
        }

        public static void UpdateRuntimeData(UITable table, float graphX, float graphY, float graphWidth, float graphHeight)
        {
            var context = table.context;
            context.left = 0;
            context.bottom = 0;
            context.top = 0;
            context.right = 0;
            context.x = graphX + context.left;
            context.y = graphY + context.bottom;
            context.width = graphWidth - context.left - context.right;
            context.height = graphHeight - context.top - context.bottom;
            context.position = new Vector3(context.x, context.y);
            context.center = new Vector3(context.x + context.width / 2, context.y + context.height / 2);

            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var scrollbarWidth = TableHelper.GetScrollbarHorizontalWidth(table);
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            context.rowWidth = context.width - rowHeaderWidth - scrollbarWidth;
            context.rowHeight = context.height - columnHeaderHeight - scrollbarWidth;
        }

        public static void OnBeginDrag(UITable table, PointerEventData eventData)
        {
            table.context.isDragingHorizontalScrollbar = false;
            table.context.isDragingVerticalScrollbar = false;
            if (table.scrollbarStyle.show)
            {
                if (table.scrollbarStyle.horizontal || table.scrollbarStyle.vertical)
                {
                    Vector2 localPos;
                    if (!table.ScreenPointToChartPoint(eventData.position, out localPos))
                        return;
                    if (table.scrollbarStyle.horizontal && table.context.horizontalScrollbarRect.Contains(localPos))
                    {
                        table.context.isDragingHorizontalScrollbar = true;
                        return;
                    }
                    if (table.scrollbarStyle.vertical && table.context.verticalScrollbarRect.Contains(localPos))
                    {
                        table.context.isDragingVerticalScrollbar = true;
                        return;
                    }
                }
            }
        }

        public static void OnDrag(UITable table, PointerEventData eventData)
        {
            if (table.context.isDragingHorizontalScrollbar)
            {
                CheckScrollHorizontalRange(table, eventData.delta.x);
            }
            else if (table.context.isDragingVerticalScrollbar)
            {
                CheckScrollVerticalRange(table, eventData.delta.y);
            }
            else
            {
                if (!table.supportDrag) return;
                CheckScrollHorizontalRange(table, eventData.delta.x);
                CheckScrollVerticalRange(table, -eventData.delta.y);
            }
        }

        public static void OnEndDrag(UITable table, PointerEventData eventData)
        {
            table.context.isDragingHorizontalScrollbar = false;
            table.context.isDragingVerticalScrollbar = false;
        }

        public static void OnScroll(UITable table, PointerEventData eventData)
        {
            if (!table.supportScroll) return;
            CheckScrollHorizontalRange(table, eventData.scrollDelta.x * table.scrollSensitivity);
            CheckScrollVerticalRange(table, eventData.scrollDelta.y * table.scrollSensitivity);
        }

        public static void OnPointerEnter(UITable table)
        {
            TableCarouselHandler.OnPointerEnter(table);
        }

        public static void OnPointerExit(UITable table)
        {
            TableCarouselHandler.OnPointerExit(table);
        }

        public static void OnPointerClick(UITable table, PointerEventData eventData)
        {
            if (table.context.isDraging) return;
            if (!table.supportSelect && table.onTableClick == null) return;
            if (Input.touchCount > 1)
                return;

            Vector2 localPos;
            if (!table.ScreenPointToChartPoint(eventData.position, out localPos))
                return;

            var row = -1;
            var column = -1;
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderStartY = table.context.y + table.context.height - columnHeaderHeight;
            var rowHeaderStartX = table.context.x + rowHeaderWidth;
            if (localPos.x >= rowHeaderStartX && localPos.y >= columnHeaderStartY)
            {
                for (int i = 0; i < table.context.columnHeaderLabels.Count; i++)
                {
                    var label = table.context.columnHeaderLabels[i];
                    if (label.rect.Contains(localPos))
                    {
                        column = i;
                        break;
                    }
                }
            }
            if (localPos.x <= rowHeaderStartX && localPos.y <= columnHeaderStartY)
            {
                for (int i = 0; i < table.context.rowHeaderLabels.Count; i++)
                {
                    var label = table.context.rowHeaderLabels[i];
                    if (label.rect.Contains(localPos))
                    {
                        row = i;
                        break;
                    }
                }
            }
            if (localPos.x >= rowHeaderStartX && localPos.y <= columnHeaderStartY)
            {
                for (int i = 0; i < table.data.Count; i++)
                {
                    var found = false;
                    for (int j = 0; j < table.columns.Count; j++)
                    {
                        var label = table.context.dataLabels[i * table.columns.Count + j];
                        if (label.rect.Contains(localPos))
                        {
                            row = i;
                            column = j;
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }
            if (table.supportSelect)
            {
                if (row != table.context.selectRow || column != table.context.selectColumn)
                {
                    table.context.selectRow = row;
                    table.context.selectColumn = column;
                    table.RefreshGraph();
                }
            }
            if (table.onTableClick != null)
            {
                table.onTableClick.Invoke(row, column, table.GetData(row, column));
            }
        }

        private static void CheckScrollHorizontalRange(UITable table, float delta)
        {
            var last = table.context.scrollHorizonal;
            last -= delta;
            var max = TableHelper.GetScrollbarHorizontalRange(table);
            if (last > max) last = max;
            if (last < 0) last = 0;
            if (last != table.context.scrollHorizonal)
            {
                table.context.scrollHorizonal = last;
                table.RefreshGraph();
                table.RefreshPosition();
            }
        }

        private static void CheckScrollVerticalRange(UITable table, float delta)
        {
            var last = table.context.scrollVertical;
            last -= delta;
            var max = TableHelper.GetScrollbarVerticalRange(table);
            if (last > max) last = max;
            if (last < 0) last = 0;
            if (last != table.context.scrollVertical)
            {
                table.context.scrollVertical = last;
                table.RefreshGraph();
                table.RefreshPosition();
            }
        }

        public static void DrawTableStyle(VertexHelper vh, UITable table)
        {
            DrawTableRow(vh, table);
            DrawTableColumnHeader(vh, table);
            DrawTableSeparator(vh, table);
        }

        public static void DrawTableTop(VertexHelper vh, UITable table)
        {
            DrawTableBorder(vh, table);
            DrawTableScrollbar(vh, table);
        }

        private static void DrawTableRow(VertexHelper vh, UITable table)
        {
            var context = table.context;
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var carouselHeight = TableHelper.GetCarouselProgress(table);
            var scrollbarVerticalWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var selectColor = TableHelper.GetSelectColor(table);
            var selectHeaderColor = TableHelper.GetSelectHeaderColor(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            //draw row
            var maxY = context.y + context.height - columnHeaderHeight;
            var rowTotalHeight = 0f;
            var rowGap = table.rowStyle.rowGap;
            var anyActive = false;
            var lastY = 0f;
            var lastIndex = 0;
            var cornerRadius = table.rowStyle.borderStyle.GetRuntimeCornerRadius();
            var borderWidth = table.rowStyle.borderStyle.GetRuntimeBorderWidth();
            var borderColor = table.rowStyle.borderStyle.GetRuntimeBorderColor();
            Vector3 p1, p2, p4;
            table.context.unvisibleIndexList.Clear();
            for (int i = 0; i < table.data.Count; i++)
            {
                var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                var rowY = context.y + context.height - columnHeaderHeight - rowTotalHeight;
                rowY += table.context.scrollVertical;
                rowY += carouselHeight;
                rowTotalHeight += rowHeight + rowGap;
                var rowB = rowY - rowHeight;
                if (rowB > maxY)
                {
                    if (carouselHeight != 0 && !anyActive && !table.context.unvisibleIndexList.Contains(i))
                    {
                        table.context.unvisibleIndexList.Add(i);
                    }
                    continue;
                }
                if (rowY < table.context.y) continue;
                lastY = rowY;
                if (rowY > maxY)
                    rowY = maxY;
                anyActive = true;

                var rowColor = TableHelper.GetRowBackgroundColor(table, i);
                var rect = new Rect(context.x + borderWidth,
                        rowB + borderWidth,
                        context.width - scrollbarVerticalWidth - 2 * borderWidth,
                        rowHeight - 2 * borderWidth);
                if (table.context.selectColumn == -1 && table.context.selectRow >= 0 && table.context.selectRow == i)
                {
                    UGL.DrawRoundRectangleWithBorder(vh, rect, selectColor, selectColor, cornerRadius, borderWidth, borderColor);
                }
                else
                {
                    UGL.DrawRoundRectangleWithBorder(vh, rect, rowColor, rowColor, cornerRadius, borderWidth, borderColor);
                }
                if (rowHeaderWidth > 0)
                {
                    var bgColor = TableHelper.GetRowHeaderBgColor(table, i);
                    var headRect = new Rect(context.x + borderWidth, rowB + borderWidth,
                        rowHeaderWidth - 2 * borderWidth, rowHeight - 2 * borderWidth);
                    if (table.context.selectRow >= 0 && table.context.selectRow == i)
                    {
                        UGL.DrawRoundRectangle(vh, headRect, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
                    }
                    else if (table.context.selectColumn >= 0 && table.context.selectRow == -1)
                    {
                        UGL.DrawRoundRectangle(vh, headRect, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
                    }
                    else
                    {
                        UGL.DrawRoundRectangle(vh, headRect, bgColor, bgColor, 0, cornerRadius);
                    }
                }
                lastIndex = i;
            }
            if (carouselHeight != 0)
            {
                foreach (var i in table.context.unvisibleIndexList)
                {
                    var rowColor = TableHelper.GetRowBackgroundColor(table, lastIndex + 1);
                    var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                    var rowY = lastY - rowHeight - rowGap;
                    var rowB = rowY - rowHeight;
                    if (rowB > maxY) continue;
                    if (rowY < table.context.y) continue;
                    if (rowY > maxY)
                        rowY = maxY;
                    lastY = rowY;

                    p1 = new Vector3(context.x, rowY);
                    p2 = new Vector3(context.x + context.width - scrollbarVerticalWidth, rowY);
                    p4 = new Vector3(context.x, rowB);
                    var rect = new Rect(p1.x, p4.y, p2.x - p1.x, p1.y - p4.y);

                    if (table.context.selectColumn == -1 && table.context.selectRow >= 0 && table.context.selectRow == i)
                    {
                        UGL.DrawRoundRectangleWithBorder(vh, rect, selectColor, selectColor, cornerRadius, borderWidth, borderColor);
                    }
                    else
                    {
                        UGL.DrawRoundRectangleWithBorder(vh, rect, rowColor, rowColor, cornerRadius, borderWidth, borderColor);
                    }
                    if (rowHeaderWidth > 0)
                    {
                        var bgColor = TableHelper.GetRowHeaderBgColor(table, lastIndex + 1);
                        var headRect = new Rect(context.x + borderWidth, rowB + borderWidth,
                        rowHeaderWidth - 2 * borderWidth, rowHeight - 2 * borderWidth);
                        if (table.context.selectRow >= 0 && table.context.selectRow == i)
                        {
                            UGL.DrawRoundRectangle(vh, headRect, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
                        }
                        else if (table.context.selectColumn >= 0 && table.context.selectRow == -1)
                        {
                            UGL.DrawRoundRectangle(vh, headRect, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
                        }
                        else
                        {
                            UGL.DrawRoundRectangle(vh, headRect, bgColor, bgColor, 0, cornerRadius);
                        }
                    }
                    lastIndex++;
                }
            }
            if (table.context.selectColumn >= 0 && table.context.selectRow == -1)
            {
                var rectMinX = table.context.x + rowHeaderWidth;
                for (int i = 0; i < table.data.Count; i++)
                {
                    var label = table.context.dataLabels[i * table.columns.Count + table.context.selectColumn];
                    if (!label.IsActiveByScale()) continue;
                    var rectX = label.rect.x;
                    var rectMaxX = rectX + label.rect.width;
                    if (rectX < rectMinX) rectX = rectMinX;
                    if (rectMaxX < rectMinX) rectMaxX = rectMinX;
                    var rect = new Rect(rectX, label.rect.y, rectMaxX - rectX, label.rect.height);
                    UGL.DrawRoundRectangle(vh, rect, selectColor, selectColor, 0, cornerRadius);
                }
            }
            else if (table.context.selectColumn >= 0 && table.context.selectRow >= 0)
            {
                var rectMinX = table.context.x + rowHeaderWidth;
                var rectMaxY = table.context.y + table.context.height - columnHeaderHeight;
                var label = table.context.dataLabels[table.context.selectRow * table.columns.Count + table.context.selectColumn];
                var rectX = label.rect.x;
                var rectX2 = rectX + label.rect.width;
                if (rectX < rectMinX) rectX = rectMinX;
                if (rectX2 < rectMinX) rectX2 = rectMinX;

                var rectY = label.rect.y;
                var rectY2 = rectY + label.rect.height;
                if (rectY > rectMaxY) rectY = rectMaxY;
                if (rectY2 > rectMaxY) rectY2 = rectMaxY;
                var rect = new Rect(rectX, rectY, rectX2 - rectX, rectY2 - rectY);
                UGL.DrawRoundRectangle(vh, rect, selectColor, selectColor, 0, cornerRadius);
            }
        }

        private static void DrawTableSeparator(VertexHelper vh, UITable table)
        {
            var context = table.context;
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var carouselHeight = TableHelper.GetCarouselProgress(table);
            var scrollbarVerticalWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);

            Vector3 ps, pe;
            var rowTotalHeight = 0f;
            var rowGap = table.rowStyle.rowGap;
            var anyActive = false;
            var lastY = 0f;
            // draw separator
            if (table.separatorStyle.IsNeedDrawHorizontalSeparator())
            {
                var separatorColor = TableHelper.GetHorizontalSeparatorColor(table);
                // row line
                if (table.separatorStyle.horizontal)
                {
                    if (table.separatorStyle.horizontalColumn && table.columnHeaderStyle.show)
                    {
                        var rowY = context.y + context.height - columnHeaderHeight;
                        ps = new Vector3(context.x, rowY);
                        pe = new Vector3(context.x + context.width - scrollbarVerticalWidth, rowY);
                        UGL.DrawLine(vh, ps, pe, table.separatorStyle.horizontalWidth, separatorColor);
                    }
                    rowTotalHeight = 0;
                    anyActive = false;
                    lastY = 0f;
                    table.context.unvisibleIndexList.Clear();
                    for (int i = 0; i < table.data.Count; i++)
                    {
                        var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                        rowTotalHeight += rowHeight + rowGap;
                        var rowY = context.y + context.height - columnHeaderHeight - rowTotalHeight;
                        rowY += table.context.scrollVertical;
                        rowY += carouselHeight;
                        if (rowY < table.context.y || (i == table.data.Count - 1 && carouselHeight == 0))
                        {
                            continue;
                        }
                        if (rowY <= context.y + context.height - columnHeaderHeight)
                        {
                            anyActive = true;
                            lastY = rowY;
                            ps = new Vector3(context.x, rowY);
                            pe = new Vector3(context.x + context.width - scrollbarVerticalWidth, rowY);
                            UGL.DrawLine(vh, ps, pe, table.separatorStyle.horizontalWidth, separatorColor);
                        }
                        else if (carouselHeight > 0 && !anyActive && !table.context.unvisibleIndexList.Contains(i))
                        {
                            table.context.unvisibleIndexList.Add(i);
                        }
                    }
                    if (carouselHeight != 0)
                    {
                        foreach (var i in table.context.unvisibleIndexList)
                        {
                            var rowHeight = TableHelper.GetRowHeight(table, i, rowEachHeight);
                            var rowY = lastY - rowHeight - rowGap;
                            rowTotalHeight += rowHeight + rowGap;
                            lastY = rowY;
                            ps = new Vector3(context.x, rowY);
                            pe = new Vector3(context.x + context.width - scrollbarVerticalWidth, rowY);
                            UGL.DrawLine(vh, ps, pe, table.separatorStyle.horizontalWidth, separatorColor);
                        }
                    }
                }
            }
            if (table.separatorStyle.IsNeedDrawVerticalSeparator())
            {
                // column line
                var separatorColor = TableHelper.GetVerticalSeparatorColor(table);
                if (table.separatorStyle.vertical)
                {
                    var lineLength = table.separatorStyle.verticalColumn ?
                        context.height :
                        context.height - columnHeaderHeight;
                    var allRowAndGapHeight = table.separatorStyle.verticalColumn ?
                        TableHelper.GetAllRowAndGapHeight(table) + columnHeaderHeight :
                        TableHelper.GetAllRowAndGapHeight(table);
                    if (rowHeaderWidth > 0)
                    {
                        ps = new Vector3(context.x + rowHeaderWidth, context.y + lineLength);
                        pe = new Vector3(context.x + rowHeaderWidth, context.y + lineLength - allRowAndGapHeight);
                        UGL.DrawLine(vh, ps, pe, table.separatorStyle.verticalWidth, separatorColor);
                    }

                    var currentTotalWidth = 0f;
                    for (int i = 0; i <= table.columns.Count; i++)
                    {
                        var colWidth = TableHelper.GetColumnWidth(table, i);
                        if (i == 0 && rowHeaderWidth <= 0)
                        {
                            currentTotalWidth += colWidth;
                            continue;
                        }
                        if (i == table.columns.Count &&
                            currentTotalWidth + colWidth >= context.width - rowHeaderWidth - scrollbarVerticalWidth - 1)
                        {
                            currentTotalWidth += colWidth;
                            continue;
                        }
                        var colX = context.x + rowHeaderWidth + currentTotalWidth;
                        currentTotalWidth += colWidth;
                        colX -= table.context.scrollHorizonal;
                        if (colX >= context.x + rowHeaderWidth && colX <= context.x + context.width)
                        {
                            ps = new Vector3(colX, context.y + lineLength);
                            pe = new Vector3(colX, context.y + lineLength - allRowAndGapHeight);
                            UGL.DrawLine(vh, ps, pe, table.separatorStyle.verticalWidth, separatorColor);
                        }
                    }
                }
            }
        }

        private static void DrawTableColumnHeader(VertexHelper vh, UITable table)
        {
            if (!TableHelper.IsNeedDrawColumnHeader(table)) return;
            var context = table.context;
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var scrollbarVerticalWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var selectHeaderColor = TableHelper.GetSelectHeaderColor(table);
            var minColumnX = context.x + rowHeaderWidth;
            var cornerRadius = table.columnHeaderStyle.borderStyle.GetRuntimeCornerRadius();
            var borderWidth = table.columnHeaderStyle.borderStyle.GetRuntimeBorderWidth();

            var rect = new Rect(context.x + borderWidth, context.y + context.height - columnHeaderHeight + borderWidth,
                context.width - scrollbarVerticalWidth - 2 * borderWidth, columnHeaderHeight - 2 * borderWidth);

            if (table.columnHeaderStyle.opaque)
            {
                selectHeaderColor.a = 255;
            }

            if (table.context.selectColumn == -1 && table.context.selectRow >= 0)
            {
                UGL.DrawRoundRectangle(vh, rect, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
            }
            else
            {
                var backgroundColor = TableHelper.GetColumnHeaderBackgroundColor(table);
                if (table.columnHeaderStyle.opaque)
                {
                    backgroundColor.a = 255;
                }
                UGL.DrawRoundRectangle(vh, rect, backgroundColor, backgroundColor, 0, cornerRadius);
            }
            if (borderWidth > 0)
            {
                var borderColor = table.columnHeaderStyle.borderStyle.GetRuntimeBorderColor();
                UGL.DrawBorder(vh, rect, borderWidth, borderColor, 0, cornerRadius);
            }
            if (table.context.selectColumn >= 0 && table.context.selectRow == -1)
            {
                var label = table.context.columnHeaderLabels[table.context.selectColumn];
                var p1 = new Vector3(label.rect.x, label.rect.y);
                var p2 = new Vector3(label.rect.x + label.rect.width, label.rect.y);
                var p3 = new Vector3(label.rect.x + label.rect.width, label.rect.y + label.rect.height);
                var p4 = new Vector3(label.rect.x, label.rect.y + label.rect.height);
                TableHelper.ClampVectorMin(minColumnX, ref p1, ref p2, ref p3, ref p4);
                var rect2 = new Rect(p1.x, p1.y, p2.x - p1.x, p3.y - p2.y);
                UGL.DrawRoundRectangle(vh, rect2, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
            }
            else if (table.context.selectColumn >= 0 && table.context.selectRow >= 0)
            {
                var label = table.context.columnHeaderLabels[table.context.selectColumn];

                var p1 = new Vector3(label.rect.x, label.rect.y);
                var p2 = new Vector3(label.rect.x + label.rect.width, label.rect.y);
                var p3 = new Vector3(label.rect.x + label.rect.width, label.rect.y + label.rect.height);
                var p4 = new Vector3(label.rect.x, label.rect.y + label.rect.height);
                TableHelper.ClampVectorMin(minColumnX, ref p1, ref p2, ref p3, ref p4);
                var rect2 = new Rect(p1.x, p1.y, p2.x - p1.x, p3.y - p2.y);
                UGL.DrawRoundRectangle(vh, rect2, selectHeaderColor, selectHeaderColor, 0, cornerRadius);
            }
        }

        private static void DrawTableBorder(VertexHelper vh, UITable table)
        {
            var borderWidth = table.borderStyle.GetRuntimeBorderWidth();
            if (borderWidth <= 0) return;
            var rect = new Rect(table.context.x, table.context.y, table.context.width, table.context.height);
            var cornerRadius = table.borderStyle.GetRuntimeCornerRadius();
            var borderColor = table.borderStyle.GetRuntimeBorderColor();
            UGL.DrawBorder(vh, rect, borderWidth, borderColor, 0, cornerRadius);
        }

        private static void DrawTableScrollbar(VertexHelper vh, UITable table)
        {
            if (!table.scrollbarStyle.show) return;
            Vector3 p1, p2;
            var scrollbarColor = TableHelper.GetScrollbarColor(table);
            var scrollbarBackgroundColor = TableHelper.GetScrollbarBackgroundColor(table);
            var borderWidth = table.scrollbarStyle.borderStyle.GetRuntimeBorderWidth();
            var borderColor = table.scrollbarStyle.borderStyle.GetRuntimeBorderColor();
            var cornerRadius = table.scrollbarStyle.borderStyle.GetRuntimeCornerRadius();
            var halfWidth = table.scrollbarStyle.width * 0.5f;
            if (table.scrollbarStyle.vertical)
            {
                var px = table.context.x + table.context.width - halfWidth;
                p1 = new Vector3(px, table.context.y + table.context.height);
                p2 = new Vector3(px, table.context.y);
                UGL.DrawLine(vh, p1, p2, halfWidth, scrollbarBackgroundColor);

                var rate = TableHelper.GetScrollbarVerticalRate(table);
                var yRate = table.context.scrollVertical / TableHelper.GetScrollbarVerticalRange(table);

                var barLength = table.context.height * rate;
                if (table.scrollbarStyle.horizontal)
                {
                    barLength -= table.scrollbarStyle.width;
                }
                var offset = (table.context.height - barLength) * yRate;
                p1 = new Vector3(px, table.context.y + table.context.height - offset);
                p2 = new Vector3(px, table.context.y + table.context.height - offset - barLength);
                var rect = new Rect(p1.x - halfWidth, p2.y, table.scrollbarStyle.width, barLength);
                table.context.verticalScrollbarRect = rect;
                UGL.DrawRoundRectangleWithBorder(vh, rect, scrollbarColor, scrollbarColor, cornerRadius, borderWidth, borderColor, 0.5f);
            }
            if (table.scrollbarStyle.horizontal)
            {
                var py = table.context.y + halfWidth;
                p1 = new Vector3(table.context.x, py);
                p2 = new Vector3(table.context.x + table.context.width - table.scrollbarStyle.width, py);
                UGL.DrawLine(vh, p1, p2, halfWidth, scrollbarBackgroundColor);

                var rate = TableHelper.GetScrollbarHorizontalRate(table);
                var range = TableHelper.GetScrollbarHorizontalRange(table);
                var xRate = range == 0 ? 0 : table.context.scrollHorizonal / range;
                var barLength = table.context.width * rate;
                if (table.scrollbarStyle.vertical)
                {
                    barLength -= table.scrollbarStyle.width;
                }
                var offset = (table.context.width - barLength) * xRate;
                var p2x = table.context.x + barLength;
                p1 = new Vector3(table.context.x + offset, py - halfWidth);
                p2 = new Vector3(p2x + offset, py);
                var rect = new Rect(p1.x, p1.y, barLength, table.scrollbarStyle.width);
                table.context.horizontalScrollbarRect = rect;
                UGL.DrawRoundRectangleWithBorder(vh, rect, scrollbarColor, scrollbarColor, cornerRadius, borderWidth, borderColor, 1);
            }
        }
    }
}