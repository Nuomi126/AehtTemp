using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Funnels
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class FunnelHandler : SerieHandler<Funnel>
    {
        public override void DrawSerie(VertexHelper vh)
        {
            DrawFunnel(vh, serie);
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override void UpdateSerieContext()
        {
            var needCheck = chart.isPointerInChart || m_LegendEnter;
            if (!needCheck)
            {
                if (m_LastCheckContextFlag != needCheck)
                {
                    m_LastCheckContextFlag = needCheck;
                    serie.context.pointerItemDataIndex = -1;
                    serie.context.pointerEnter = false;
                    foreach (var serieData in serie.data)
                    {
                        serieData.context.highlight = false;
                    }
                    chart.RefreshPainter(serie);
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;

            serie.context.pointerEnter = false;
            var lastDataIndex = serie.context.pointerItemDataIndex;
            var dataIndex = GetFunnelPosIndex(serie, chart.pointerPos);
            if (dataIndex >= 0)
            {
                if (lastDataIndex >= 0)
                    serie.GetSerieData(lastDataIndex).context.highlight = false;
                if (lastDataIndex != dataIndex)
                    chart.RefreshPainter(serie);
                serie.GetSerieData(dataIndex).context.highlight = true;
                serie.context.pointerItemDataIndex = dataIndex;
                serie.context.pointerEnter = true;
            }
            else
            {
                if (lastDataIndex >= 0)
                {
                    serie.GetSerieData(lastDataIndex).context.highlight = false;
                    chart.RefreshPainter(serie);
                }
                serie.context.pointerItemDataIndex = -1;
            }
        }

        public override Vector3 GetSerieDataLabelPosition(SerieData serieData, LabelStyle label)
        {
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            var offset = (labelLine != null && labelLine.show) ? labelLine.lineLength2 : 0;
            switch (label.position)
            {
                case LabelStyle.Position.Inside:
                case LabelStyle.Position.Center:
                case LabelStyle.Position.Top:
                case LabelStyle.Position.Bottom:
                    return serieData.context.labelPosition;
                case LabelStyle.Position.Outside:
                case LabelStyle.Position.Right:
                    offset += serieData.labelObject.GetTextWidth() / 2;
                    return serieData.context.labelPosition + new Vector3(offset, 0, 0);
                case LabelStyle.Position.Left:
                    offset += serieData.labelObject.GetTextWidth() / 2;
                    return serieData.context.labelPosition + new Vector3(-offset, 0, 0);
            }
            return serieData.context.labelPosition;
        }

        private void DrawFunnel(VertexHelper vh, Funnel serie)
        {
            if (!serie.show) return;
            SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            SerieHelper.UpdateSerieRuntimeFilterData(serie);

            var maxValue = 0d;
            if (serie.max == 0 && serie.min == 0)
            {
                var min = 0d;
                var max = 0d;
                SerieHelper.GetMinMaxData(serie, 1, out min, out max, null);
                maxValue = max;
            }
            else
            {
                maxValue = serie.max - serie.min;
            }
            switch (serie.orient)
            {
                case Orient.Horizonal:
                    DrawHorizonalFunnel(vh, serie, maxValue);
                    DrawFunnelLabelLine(vh, serie);
                    break;
                case Orient.Vertical:
                    DrawVerticalFunnel(vh, serie, maxValue);
                    DrawFunnelLabelLine(vh, serie);
                    break;
            }
        }

        private void DrawVerticalFunnel(VertexHelper vh, Funnel serie, double maxValue)
        {
            var data = serie.context.sortedData;
            var dataCount = data.Count;
            var maxWidth = serie.context.width;
            var maxHeight = serie.context.height;
            var minSize = serie.minSize > 1 ? serie.minSize : serie.minSize * serie.context.width;
            var maxSize = serie.maxSize > 1 ? serie.maxSize : serie.maxSize * serie.context.width;
            var eachHeight = (serie.context.height - ((dataCount - 1) * serie.gap)) / dataCount;
            var startY = serie.context.y + serie.context.height;
            var currentY = startY;
            var startX = 0f;
            var totalValue = serie.dynamicHeight?serie.yTotal : 0;
            var minWidth = ChartHelper.GetActualValue(serie.minSize, maxWidth, 1);
            var tan = maxHeight / (maxWidth / 2 - minWidth / 2);
            for (int i = 0; i < dataCount; i++)
            {
                var nowWidth = 0f;
                var nexWidth = 0f;
                var nowHeight = eachHeight;
                Vector3 pul, pur, pdl, pdr;
                var serieData = data[i];
                if (!serieData.show) continue;
                var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                var color = chart.GetItemColor(serie, serieData, colorIndex);

                if (serie.dynamicHeight)
                {
                    nowHeight = (float) (maxHeight * serieData.GetData(1) / totalValue);
                    switch (serie.dataSortType)
                    {
                        case SerieDataSortType.Ascending:
                            nowWidth = (2 * (startY - currentY) / tan);
                            nexWidth = (2 * (startY - currentY + nowHeight) / tan);
                            break;
                        case SerieDataSortType.Descending:
                        case SerieDataSortType.None:
                            nowWidth = maxWidth - (2 * (startY - currentY) / tan);
                            nexWidth = maxWidth - (2 * (startY - currentY + nowHeight) / tan);
                            break;
                    }
                }
                else
                {
                    switch (serie.dataSortType)
                    {
                        case SerieDataSortType.Ascending:
                            nexWidth = i == dataCount - 1 ? maxWidth : (float) (maxWidth * serieData.GetData(1) / maxValue);
                            nowWidth = i == 0 ? (serie.rectEnd?nexWidth : 0) : maxWidth * (float) (data[i - 1].GetData(1) / maxValue);
                            break;
                        case SerieDataSortType.Descending:
                        case SerieDataSortType.None:
                            nowWidth = maxWidth * (float) (serieData.GetData(1) / maxValue);
                            nexWidth = i == dataCount - 1 ? (serie.rectEnd?nowWidth : 0) : maxWidth * (float) (data[i + 1].GetData(1) / maxValue);
                            break;
                    }
                }
                if (nowWidth < minSize) nowWidth = minSize;
                if (nowWidth > maxSize) nowWidth = maxSize;
                if (nexWidth < minSize) nexWidth = minSize;
                if (nexWidth > maxSize) nexWidth = maxSize;
                switch (serie.align)
                {
                    case Align.Left:
                        startX = serie.context.x;
                        pul = new Vector3(startX, currentY);
                        pur = new Vector3(pul.x + nowWidth, pul.y);
                        pdl = new Vector3(startX, pul.y - nowHeight);
                        pdr = new Vector3(pdl.x + nexWidth, pdl.y);
                        break;
                    case Align.Right:
                        startX = serie.context.x + maxWidth;
                        pul = new Vector3(startX - nowWidth, currentY);
                        pur = new Vector3(startX, pul.y);
                        pdl = new Vector3(startX - nexWidth, pul.y - nowHeight);
                        pdr = new Vector3(startX, pdl.y);
                        break;
                    default:
                        startX = serie.context.x + maxWidth / 2;
                        pul = new Vector3(startX - nowWidth / 2, currentY);
                        pur = new Vector3(pul.x + nowWidth, pul.y);
                        pdl = new Vector3(startX - nexWidth / 2, pul.y - nowHeight);
                        pdr = new Vector3(pdl.x + nexWidth, pdl.y);
                        break;
                }
                currentY -= (nowHeight + serie.gap);
                UGL.DrawQuadrilateral(vh, pul, pur, pdr, pdl, color);
                UpdateSerieLabelPosition(serie, serieData, pul, pur, pdr, pdl);
            }
        }

        private void DrawHorizonalFunnel(VertexHelper vh, Funnel serie, double maxValue)
        {
            var data = serie.context.sortedData;
            var dataCount = data.Count;
            var maxWidth = serie.context.width;
            var maxHeight = serie.context.height;
            var minSize = serie.minSize > 1 ? serie.minSize : serie.minSize * serie.context.height;
            var maxSize = serie.maxSize > 1 ? serie.maxSize : serie.maxSize * serie.context.height;
            var eachHeight = (serie.context.width - ((dataCount - 1) * serie.gap)) / dataCount;
            var startX = serie.context.x;
            var currentX = startX;
            var startY = 0f;

            var totalValue = serie.dynamicHeight?serie.yTotal : 0;
            var minHeight = ChartHelper.GetActualValue(serie.minSize, maxHeight, 1);
            var tan = maxWidth / (maxHeight / 2 - minHeight / 2);
            for (int i = 0; i < dataCount; i++)
            {
                var nowWidth = 0f;
                var nexWidth = 0f;
                var nowHeight = eachHeight;
                Vector3 pul, pur, pdl, pdr;
                var serieData = data[i];
                var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                var color = chart.GetItemColor(serie, serieData, colorIndex);
                if (serie.dynamicHeight)
                {
                    nowHeight = (float) (maxWidth * serieData.GetData(1) / totalValue);
                    switch (serie.dataSortType)
                    {
                        case SerieDataSortType.Ascending:
                            nowWidth = (2 * (currentX - startX) / tan);
                            nexWidth = (2 * (currentX - startX + nowHeight) / tan);
                            break;
                        case SerieDataSortType.Descending:
                        case SerieDataSortType.None:
                            nowWidth = maxHeight - (2 * (currentX - startX) / tan);
                            nexWidth = maxHeight - (2 * (currentX - startX + nowHeight) / tan);
                            break;
                    }
                }
                else
                {
                    switch (serie.dataSortType)
                    {
                        case SerieDataSortType.Ascending:
                            nexWidth = i == dataCount - 1 ? maxHeight : maxHeight * (float) (serieData.GetData(1) / maxValue);
                            nowWidth = i == 0 ? (serie.rectEnd? nexWidth : 0) : maxHeight * (float) (data[i - 1].GetData(1) / maxValue);
                            break;
                        case SerieDataSortType.Descending:
                        case SerieDataSortType.None:
                            nowWidth = maxHeight * (float) (serieData.GetData(1) / maxValue);
                            nexWidth = i == dataCount - 1 ? (serie.rectEnd?nowWidth : 0) : maxHeight * (float) (data[i + 1].GetData(1) / maxValue);
                            break;
                    }
                }
                if (nowWidth < minSize) nowWidth = minSize;
                if (nowWidth > maxSize) nowWidth = maxSize;
                if (nexWidth < minSize) nexWidth = minSize;
                if (nexWidth > maxSize) nexWidth = maxSize;
                switch (serie.align)
                {
                    case Align.Left:
                        startY = serie.context.y + serie.context.height;
                        pul = new Vector3(currentX, startY);
                        pur = new Vector3(pul.x + nowHeight, startY);
                        pdl = new Vector3(pul.x, startY - nowWidth);
                        pdr = new Vector3(pur.x, startY - nexWidth);
                        break;
                    case Align.Right:
                        startY = serie.context.y;
                        pul = new Vector3(currentX, startY + nowWidth);
                        pur = new Vector3(pul.x + nowHeight, startY + nexWidth);
                        pdl = new Vector3(pul.x, startY);
                        pdr = new Vector3(pur.x, startY);
                        break;
                    default:
                        startY = serie.context.y + maxHeight / 2;
                        pul = new Vector3(currentX, startY + nowWidth / 2);
                        pur = new Vector3(pul.x + nowHeight, startY + nexWidth / 2);
                        pdl = new Vector3(pul.x, startY - nowWidth / 2);
                        pdr = new Vector3(pur.x, startY - nexWidth / 2);
                        break;
                }
                currentX += (nowHeight + serie.gap);
                UGL.DrawQuadrilateral(vh, pul, pur, pdr, pdl, color);
                UpdateSerieLabelPosition(serie, serieData, pul, pur, pdr, pdl);
            }
        }

        private void UpdateSerieLabelPosition(Serie serie, SerieData serieData, Vector3 pul, Vector3 pur,
            Vector3 pdr, Vector3 pdl)
        {
            serieData.SetPolygon(pul, pur, pdr, pdl);
            serieData.context.position = new Vector3((pul.x + pur.x) / 2, (pdl.y + pul.y) / 2);
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            if (serieLabel == null || !serieLabel.show) return;
            switch (serieLabel.position)
            {
                case LabelStyle.Position.Inside:
                case LabelStyle.Position.Center:
                    serieData.context.labelPosition = serieData.context.position;
                    break;
                case LabelStyle.Position.Outside:
                case LabelStyle.Position.Right:
                    serieData.context.labelPosition = new Vector3((pdr.x + pur.x) / 2, (pdr.y + pur.y) / 2);
                    break;
                case LabelStyle.Position.Left:
                    serieData.context.labelPosition = new Vector3((pdl.x + pul.x) / 2, (pdl.y + pul.y) / 2);
                    break;
                case LabelStyle.Position.Top:
                    break;
                case LabelStyle.Position.Bottom:
                    break;
            }
        }

        private int GetFunnelPosIndex(Serie serie, Vector2 local)
        {
            if (!(serie is Funnel)) return -1;
            var data = serie.context.sortedData;
            for (int i = 0; i < data.Count; i++)
            {
                var serieData = data[i];
                if (serieData.IsInPolygon(local))
                {
                    return i;
                }
            }
            return -1;
        }

        private void DrawFunnelLabelLine(VertexHelper vh, Serie serie)
        {
            foreach (var serieData in serie.data)
            {
                var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
                if (SerieLabelHelper.CanShowLabel(serie, serieData, serieLabel, 1))
                {
                    int colorIndex = chart.GetLegendRealShowNameIndex(serieData.name);
                    var color = chart.GetItemColor(serie, serieData, colorIndex);;
                    DrawFunnelLabelLine(vh, serie, serieData, color);
                }
            }
        }

        private void DrawFunnelLabelLine(VertexHelper vh, Serie serie, SerieData serieData, Color color)
        {
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            if (serieLabel == null || labelLine == null)
                return;
            if (serieLabel.show && labelLine.show)
            {
                switch (serieLabel.position)
                {
                    case LabelStyle.Position.Inside:
                    case LabelStyle.Position.Center:
                        break;
                    case LabelStyle.Position.Outside:
                    case LabelStyle.Position.Right:
                        var sp = serieData.context.labelPosition + new Vector3(labelLine.lineGap, 0);
                        var ep = serieData.context.labelPosition + new Vector3(labelLine.lineLength2 - labelLine.lineGap, 0);
                        UGL.DrawLine(vh, sp, ep, labelLine.lineWidth, color);
                        break;
                    case LabelStyle.Position.Left:
                        sp = serieData.context.labelPosition - new Vector3(labelLine.lineGap, 0);
                        ep = serieData.context.labelPosition - new Vector3(labelLine.lineLength2 - labelLine.lineGap, 0);
                        UGL.DrawLine(vh, sp, ep, labelLine.lineWidth, color);
                        break;
                    case LabelStyle.Position.Top:
                        break;
                    case LabelStyle.Position.Bottom:
                        break;
                }
            }
        }
    }
}