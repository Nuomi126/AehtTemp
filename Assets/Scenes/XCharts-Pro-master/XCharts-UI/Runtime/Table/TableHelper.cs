using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 表格帮助类。
    /// </summary>
    public static class TableHelper
    {
        public static string GetRowHeaderContent(UITable table, int i)
        {
            var labelStyle = table.rowHeaderStyle.labelStyle;
            var content = labelStyle.formatter;
            if (string.IsNullOrEmpty(content))
            {
                return ChartCached.IntToStr(i + 1);
            }
            else
            {
                var row = table.data[i];
                content = content.Replace("{name}", row.name);
                content = content.Replace("{index}", ChartCached.IntToStr(i + 1));
                return content;
            }
        }

        public static Color32 GetSelectColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.selectColor))
                return table.selectColor;
            else
                return table.theme.sharedTheme.dataZoom.fillerColor;
        }

        public static Color32 GetSelectHeaderColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.selectHeaderColor))
                return table.selectHeaderColor;
            else
                return ChartHelper.GetHighlightColor(table.theme.sharedTheme.dataZoom.fillerColor, 1.2f);
        }

        public static Color32 GetBackgroundColor(UITable table)
        {
            if (table.background.show && !ChartHelper.IsClearColor(table.background.imageColor))
                return table.background.imageColor;
            else
                return table.theme.backgroundColor;
        }

        public static Color32 GetRowBackgroundColor(UITable table, int index)
        {
            if (index < 0) return Color.clear;
            var list = table.rowStyle.rowBackgroundColors.Count > 0 ? table.rowStyle.rowBackgroundColors : table.theme.sharedTheme.axis.splitAreaColors;
            index = index % list.Count;
            return list[index];
        }

        public static Color32 GetHorizontalSeparatorColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.separatorStyle.horizontalColor))
                return table.separatorStyle.horizontalColor;
            else
                return table.theme.sharedTheme.axis.splitLineColor;
        }

        public static Color32 GetVerticalSeparatorColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.separatorStyle.verticalColor))
                return table.separatorStyle.verticalColor;
            else
                return table.theme.sharedTheme.axis.splitLineColor;
        }

        public static Color32 GetColumnHeaderBackgroundColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.columnHeaderStyle.backgroundColor))
                return table.columnHeaderStyle.backgroundColor;
            else
                return GetRowBackgroundColor(table, 1);
        }

        public static LabelStyle GetDataLabelStyle(UITable table, int index)
        {
            if (table.columns.Count > 0)
            {
                index = index % table.columns.Count;
                var label = table.columns[index].dataLabelStyle;
                if (label.show)
                    return label;
            }
            return table.columnStyle.defaultLabelStyle;
        }

        public static LabelStyle GetHeaderLabelStyle(UITable table, int index)
        {
            if (table.columns.Count > 0)
            {
                index = index % table.columns.Count;
                var label = table.columns[index].headerLabelStyle;
                if (label.show)
                    return label;
            }
            return table.columnHeaderStyle.defalutLabelStyle;
        }

        public static float GetRowHeaderWidth(UITable table)
        {
            if (table.rowHeaderStyle.show)
            {
                return table.rowHeaderStyle.width > 0 ? table.rowHeaderStyle.width : table.context.width / (table.columns.Count + 1);
            }
            else
            {
                return 0;
            }
        }

        public static float GetAllRowHeight(UITable table)
        {
            var customHeight = 0f;
            var customCount = 0;
            for (int i = 0; i < table.data.Count; i++)
            {
                var height = table.data[i].height;
                if (height > 0)
                {
                    customHeight += height + table.rowStyle.rowGap;
                    customCount++;
                }
            }
            if (customHeight > 0 || (customHeight == 0 && table.rowStyle.rowHeight <= 0))
            {
                customHeight -= table.rowStyle.rowGap;
                var residualCount = table.data.Count - customCount;
                var residualHeight = table.context.rowHeight - table.rowStyle.rowGap * (residualCount - 1) - customHeight;
                var eachHeight = residualHeight / residualCount;
                if (eachHeight < table.rowStyle.minRowHeight && table.rowStyle.minRowHeight > 0)
                {
                    eachHeight = table.rowStyle.minRowHeight;
                }
                return eachHeight * residualCount + customHeight;
            }
            else
            {
                return table.rowStyle.rowHeight * table.data.Count;
            }
        }

        public static float GetRowAutoEachHeight(UITable table)
        {
            var customHeight = 0f;
            var customCount = 0;
            for (int i = 0; i < table.data.Count; i++)
            {
                var height = table.data[i].height;
                if (height > 0)
                {
                    customHeight += height + table.rowStyle.rowGap;
                    customCount++;
                }
            }
            if (customHeight > 0)
            {
                customHeight -= table.rowStyle.rowGap;
                var residualCount = table.data.Count - customCount;
                var residualHeight = table.context.rowHeight - table.rowStyle.rowGap * (residualCount - 1) - customHeight;
                var eachHeight = residualHeight / residualCount;
                if (eachHeight < table.rowStyle.minRowHeight && table.rowStyle.minRowHeight > 0)
                {
                    eachHeight = table.rowStyle.minRowHeight;
                }
                return eachHeight;
            }
            else
            {
                if (table.rowStyle.rowHeight > 0)
                    return table.rowStyle.rowHeight;
                else
                {
                    var eachHeight = (table.context.rowHeight - table.rowStyle.rowGap * (table.data.Count - 1)) / table.data.Count;
                    if (eachHeight < table.rowStyle.minRowHeight && table.rowStyle.minRowHeight > 0)
                    {
                        eachHeight = table.rowStyle.minRowHeight;
                    }
                    return eachHeight;
                }
            }
        }

        public static float GetRowHeight(UITable table, int rowIndex, float eachHeight = 0)
        {
            if (rowIndex >= 0 && rowIndex < table.data.Count)
            {
                var row = table.data[rowIndex];
                if (row.height > 0)
                {
                    return row.height;
                }
            }
            if (table.rowStyle.rowHeight > 0)
            {
                return table.rowStyle.rowHeight;
            }
            else
            {
                return eachHeight == 0 ? GetRowAutoEachHeight(table) : eachHeight;
            }
        }

        public static float GetColumnHeaderHeight(UITable table)
        {
            if (table.columnHeaderStyle.show)
            {
                return table.columnHeaderStyle.height;
            }
            else
            {
                return 0;
            }
        }

        public static float GetAllColumnWidth(UITable table)
        {
            var width = 0f;
            for (int i = 0; i < table.columns.Count; i++)
            {
                width += GetColumnWidth(table, i);
            }
            return width;
        }

        public static float GetColumnWidth(UITable table, int columnIndex, float eachWidth = 0)
        {
            if (columnIndex < 0 || columnIndex >= table.columns.Count)
            {
                return 0;
            }
            var header = table.columns[columnIndex];
            if (header.width > 0)
            {
                return header.width;
            }
            else if (table.columnStyle.columnWidth > 0)
            {
                return table.columnStyle.columnWidth;
            }
            else
            {
                return eachWidth == 0 ? GetColumnAutoEachWidth(table) : eachWidth;
            }
        }

        public static float GetColumnAutoEachWidth(UITable table)
        {
            var customTotalWidth = 0f;
            var customCount = 0;
            foreach (var info in table.columns)
            {
                if (info.width > 0)
                {
                    customTotalWidth += info.width;
                    customCount++;
                }
            }
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var scrollbarWidth = TableHelper.GetScrollbarVerticalWidth(table);
            var each = (table.context.width - rowHeaderWidth - scrollbarWidth - customTotalWidth) / (table.columns.Count - customCount);
            if (table.columnStyle.minColumnWidth > 0 && each < table.columnStyle.minColumnWidth)
            {
                return table.columnStyle.minColumnWidth;
            }
            else
            {
                return each;
            }
        }

        public static Color32 GetRowHeaderBgColor(UITable table, int rowIndex)
        {
            if (!ChartHelper.IsClearColor(table.rowHeaderStyle.backgroundColor))
            {
                return table.rowHeaderStyle.backgroundColor;
            }
            else
            {
                return GetRowBackgroundColor(table, rowIndex);
            }
        }

        public static float GetAllRowAndGapHeight(UITable table)
        {
            return GetAllRowHeight(table) + table.rowStyle.rowGap * (table.data.Count - 1);
        }

        public static bool IsNeedDrawRowHeader(UITable table)
        {
            return table.rowHeaderStyle.show && table.rowHeaderStyle.width > 0;
        }

        public static bool IsNeedDrawColumnHeader(UITable table)
        {
            return table.columnHeaderStyle.show && table.columnHeaderStyle.height > 0;
        }

        public static Color32 GetScrollbarBackgroundColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.scrollbarStyle.backgroundColor))
                return table.scrollbarStyle.backgroundColor;
            else
                return table.theme.backgroundColor;
        }

        public static Color32 GetScrollbarColor(UITable table)
        {
            if (!ChartHelper.IsClearColor(table.scrollbarStyle.color))
                return table.scrollbarStyle.color;
            else
                return table.theme.sharedTheme.axis.lineColor;
        }

        public static float GetScrollbarHorizontalWidth(UITable table)
        {
            if (table.scrollbarStyle.show && table.scrollbarStyle.horizontal)
            {
                return table.scrollbarStyle.width;
            }
            else
            {
                return 0;
            }
        }

        public static float GetScrollbarVerticalWidth(UITable table)
        {
            if (table.scrollbarStyle.show && table.scrollbarStyle.vertical)
            {
                return table.scrollbarStyle.width;
            }
            else
            {
                return 0;
            }
        }

        public static float GetScrollbarHorizontalRate(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var total = GetAllColumnWidth(table);
            var current = table.context.width - rowHeaderWidth;
            return Mathf.Clamp01(current / total);
        }

        public static float GetScrollbarHorizontalRange(UITable table)
        {
            var rowHeaderWidth = TableHelper.GetRowHeaderWidth(table);
            var total = GetAllColumnWidth(table);
            return total - table.context.width + rowHeaderWidth + GetScrollbarHorizontalWidth(table);
        }

        public static float GetScrollbarVerticalRate(UITable table)
        {
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var total = GetAllRowAndGapHeight(table);
            var current = table.context.height - columnHeaderHeight;
            return Mathf.Clamp01(current / total);
        }

        public static float GetScrollbarVerticalRange(UITable table)
        {
            var columnHeaderHeight = TableHelper.GetColumnHeaderHeight(table);
            var total = GetAllRowAndGapHeight(table);
            var offset = total - table.context.height + columnHeaderHeight + GetScrollbarHorizontalWidth(table);
            return offset;
        }

        public static void ClampVectorMin(float minX, ref Vector3 p1, ref Vector3 p2, ref Vector3 p3, ref Vector3 p4)
        {
            if (p1.x < minX) p1.x = minX;
            if (p2.x < minX) p2.x = minX;
            if (p3.x < minX) p3.x = minX;
            if (p4.x < minX) p4.x = minX;
        }

        public static float GetCarouselProgress(UITable table)
        {
            return table.carouselStyle.show ? table.context.carousel.progress : 0;
        }
    }
}