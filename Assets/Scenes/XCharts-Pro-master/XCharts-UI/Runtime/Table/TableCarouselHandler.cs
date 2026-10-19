using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    internal static class TableCarouselHandler
    {
        public static void OnPointerEnter(UITable table)
        {
            if (!Application.isPlaying) return;
            var carouselStyle = table.carouselStyle;
            if (!carouselStyle.show) return;
            carouselStyle.pause = true;
        }

        public static void OnPointerExit(UITable table)
        {
            if (!Application.isPlaying) return;
            var carouselStyle = table.carouselStyle;
            if (!carouselStyle.show) return;
            carouselStyle.pause = false;
        }
        
        public static void UdateCarousel(UITable table)
        {
            if (!Application.isPlaying) return;
            var carouselStyle = table.carouselStyle;
            if (!carouselStyle.show)
            {
                CarouselEnd(table);
                return;
            }
            if (carouselStyle.pause || table.context.carousel.pause)
            {
                return;
            }
            var allRowHeight = TableHelper.GetAllRowHeight(table);
            var rowEachHeight = TableHelper.GetRowAutoEachHeight(table);
            switch (carouselStyle.type)
            {
                case TableCarouselType.Pixel:
                    table.context.carousel.progress += carouselStyle.speed * Time.deltaTime;
                    if (table.context.carousel.progress > allRowHeight)
                    {
                        table.context.carousel.progress %= allRowHeight;
                    }
                    break;
                case TableCarouselType.Row:
                    if (table.context.carousel.startWait)
                    {
                        if (Time.time - table.context.carousel.startWaitTime > carouselStyle.waitTime / 1000f)
                        {
                            table.context.carousel.startWait = false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    var currentRow = table.context.carousel.currentRow;
                    if (currentRow < 0)
                    {
                        currentRow = 0;
                        var rowHeight = TableHelper.GetRowHeight(table, currentRow, rowEachHeight);
                        table.context.carousel.currentHeight = rowHeight + table.rowStyle.rowGap;
                        table.context.carousel.totalHeight += rowHeight + table.rowStyle.rowGap;
                    }
                    else
                    {
                        table.context.carousel.progress += carouselStyle.speed * Time.deltaTime;
                        table.context.carousel.currentHeight -= carouselStyle.speed * Time.deltaTime;
                    }
                    if (table.context.carousel.currentHeight <= 0)
                    {
                        currentRow++;
                        table.context.carousel.startWait = true;
                        table.context.carousel.startWaitTime = Time.time;

                        if (currentRow >= table.data.Count)
                        {
                            currentRow = 0;
                            table.context.carousel.progress = 0;
                            table.context.carousel.totalHeight = 0;
                        }
                        else
                        {
                            table.context.carousel.progress = table.context.carousel.totalHeight;
                        }
                        var rowHeight = TableHelper.GetRowHeight(table, currentRow, rowEachHeight);
                        var rowGap = currentRow == table.data.Count - 1 ? 0 : table.rowStyle.rowGap;
                        table.context.carousel.currentHeight = rowHeight + rowGap;
                        table.context.carousel.totalHeight += rowHeight + rowGap;
                    }
                    table.context.carousel.currentRow = currentRow;
                    break;
                case TableCarouselType.Page:
                    break;
            }

            table.RefreshPosition();
            table.RefreshTable();
        }

        private static void CarouselEnd(UITable table)
        {
            if (table.context.carousel.isEnd) return;
            table.context.carousel.isEnd = true;
            table.context.carousel.currentRow = -1;
        }
    }
}