using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Pyramids
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class PyramidHandler : SerieHandler<Pyramid>
    {
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
            var dataIndex = GetPyramidPosIndex(serie, chart.pointerPos);
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

        public override void DrawSerie(VertexHelper vh)
        {
            if (serie.dataCount == 0) return;
            SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            var pyramidStyle = serie.pyramidStyle;
            if (pyramidStyle.draw3d)
            {
                Draw3DPyramid(vh, serie, pyramidStyle);
            }
            else
            {
                Draw2DPyramid(vh, serie, pyramidStyle);
            }
            DrawLabelLine(vh, serie);
        }

        private void Draw2DPyramid(VertexHelper vh, Serie serie, PyramidStyle style)
        {
            var data = serie.data;
            var dataCount = data.Count;
            var pyramidWidth = serie.context.width;
            var pyramidHeight = serie.context.height - (dataCount - 1) * serie.gap;
            var total = serie.GetDataTotal(1);
            if (total == 0) total = 1;
            var startX = serie.context.x;
            var startY = serie.context.y;
            var tan = serie.context.height / (pyramidWidth / 2);

            var pdl = new Vector3(startX, startY);
            var pdr = new Vector3(startX + pyramidWidth, startY);
            var pdt = new Vector3(startX + pyramidWidth / 2, startY + serie.context.height);
            for (int i = 0; i < dataCount; i++)
            {
                var serieData = data[i];
                var nowHeight = pyramidHeight * (float)(serieData.GetData(1) / total);
                var diff = nowHeight / tan;
                var pul = new Vector3(pdl.x + diff, pdl.y + nowHeight);
                var pur = new Vector3(pdr.x - diff, pul.y);
                var color = chart.theme.GetColor(i);
                color.a = (byte)(color.a * style.leftColorOpacity);
                if (serieData.context.highlight)
                {
                    color = ChartHelper.GetHighlightColor(color);
                }
                if (i == dataCount - 1)
                {
                    UGL.DrawTriangle(vh, pdl, pdt, pdr, color);
                    serieData.context.position = (pur + pdr) / 2 + Vector3.left * style.labelLineMargin;
                    serieData.SetPolygon(pdl, pdt, pdr);
                }
                else
                {
                    serieData.context.position = (pur + pdr) / 2 + Vector3.left * style.labelLineMargin;
                    serieData.SetPolygon(pul, pur, pdr, pdl);
                    UGL.DrawQuadrilateral(vh, pul, pur, pdr, pdl, color);
                    pdl = pul;
                    pdr = pur;
                    if (serie.gap > 0)
                    {
                        diff = serie.gap / tan;
                        pdl = new Vector3(pdl.x + diff, pdl.y + serie.gap);
                        pdr = new Vector3(pdr.x - diff, pdl.y);
                    }
                }
            }
        }
        private void Draw3DPyramid(VertexHelper vh, Serie serie, PyramidStyle style)
        {
            var data = serie.data;
            var dataCount = data.Count;
            var pyramidWidth = serie.context.width;
            var pyramidHeight = serie.context.height - (dataCount - 1) * serie.gap;
            var total = serie.GetDataTotal(1);
            if (total == 0) total = 1;
            var startX = serie.context.x;
            var startY = serie.context.y;
            var tan = serie.context.height / (pyramidWidth / 2);

            var pdl = new Vector3(startX, startY);
            var pdr = new Vector3(startX + pyramidWidth, startY);
            var pdt = new Vector3(startX + pyramidWidth / 2, startY + serie.context.height);

            var dhc = pyramidWidth * style.bottomPointRate;
            var pbc = new Vector3(pdl.x + dhc, pdl.y);

            var dhl = serie.context.height * style.leftPointRate;
            var pbl = new Vector3(pdl.x + dhl / tan, pdl.y + dhl);

            var dhr = serie.context.height * style.rightPointRate;
            var pbr = new Vector3(pdr.x - dhr / tan, pdr.y + dhr);

            var nowHigLeft = dhl;
            var nowHigRight = dhr;
            var nowHigCenter = 0f;
            var lpdl = pbl;
            var lpdr = pbr;
            var lpdc = pbc;
            var ctdire = (pdt - pbc).normalized;
            for (int i = 0; i < dataCount; i++)
            {
                var serieData = data[i];
                var rate = (float)(serieData.GetData(1) / total);
                nowHigLeft += (pyramidHeight - dhl) * rate;
                nowHigRight += (pyramidHeight - dhr) * rate;
                nowHigCenter += pyramidHeight * rate;

                var pul = new Vector3(pdl.x + nowHigLeft / tan, pdl.y + nowHigLeft);
                var puc = pbc + ctdire * nowHigCenter;
                var pur = new Vector3(pdr.x - nowHigRight / tan, pdr.y + nowHigRight);
                var pui = pul + (pur - puc);
                var color = chart.theme.GetColor(i);
                var lcolor = color;
                lcolor.a = (byte)(lcolor.a * style.leftColorOpacity);
                var rcolor = color;
                rcolor.a = (byte)(rcolor.a * style.rightColorOpacity);
                var tcolor = color;
                tcolor.a = (byte)(tcolor.a * style.topColorOpacity);
                if (serieData.context.highlight)
                {
                    lcolor = ChartHelper.GetHighlightColor(lcolor);
                    rcolor = ChartHelper.GetHighlightColor(rcolor);
                }
                if (i == dataCount - 1)
                {
                    UGL.DrawTriangle(vh, lpdl, pdt, lpdc, lcolor);
                    UGL.DrawTriangle(vh, lpdc, pdt, lpdr, rcolor);
                    serieData.context.position = (pur + lpdr) / 2 + Vector3.left * style.labelLineMargin;
                    serieData.SetPolygon(lpdl, pdt, lpdr, lpdc);
                }
                else
                {
                    UGL.DrawQuadrilateral(vh, pul, puc, lpdc, lpdl, lcolor);
                    UGL.DrawQuadrilateral(vh, puc, pur, lpdr, lpdc, rcolor);
                    if (style.drawTop) UGL.DrawQuadrilateral(vh, pul, pui, pur, puc, tcolor);
                    serieData.context.position = (pur + lpdr) / 2 + Vector3.left * style.labelLineMargin;
                    serieData.SetPolygon(lpdl, pul, puc, pur, lpdr, lpdc);
                    if (serie.gap > 0)
                    {
                        nowHigLeft += serie.gap;
                        nowHigRight += serie.gap;
                        nowHigCenter += serie.gap;
                        lpdl = new Vector3(pdl.x + nowHigLeft / tan, pdl.y + nowHigLeft);
                        lpdc = pbc + ctdire * nowHigCenter;
                        lpdr = new Vector3(pdr.x - nowHigRight / tan, pdr.y + nowHigRight);
                    }
                    else
                    {
                        lpdl = pul;
                        lpdr = pur;
                        lpdc = puc;
                    }
                }
            }
        }

        public override void RefreshLabelInternal()
        {
            if (!serie.show) return;
            var data = serie.data;
            for (int n = 0; n < data.Count; n++)
            {
                var serieData = data[n];
                if (!serieData.context.canShowLabel || serie.IsIgnoreValue(serieData))
                {
                    serieData.SetLabelActive(false);
                    continue;
                }
                if (!serieData.show) continue;
                Color color = chart.theme.GetColor(n);
                DrawPieLabel(serie, n, serieData, color);
            }
        }

        private void DrawPieLabel(Serie serie, int dataIndex, SerieData serieData, Color serieColor)
        {
            if (serieData.labelObject == null) return;
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            var showLabel = ((serieLabel != null && serieLabel.show) && serieData.context.canShowLabel);
            if (showLabel)
            {
                serieData.SetLabelActive(showLabel);
                var color = SerieLabelHelper.GetLabelColor(serie, chart.theme, dataIndex);
                var fontSize = serieLabel.textStyle.GetFontSize(chart.theme.common);
                var fontStyle = serieLabel.textStyle.fontStyle;
                serieData.labelObject.text.SetColor(color);
                serieData.labelObject.text.SetFontSize(fontSize);
                serieData.labelObject.text.SetFontStyle(fontStyle);
                serieData.labelObject.SetTextRotate(serieLabel.rotate);
                if (!string.IsNullOrEmpty(serieLabel.formatter))
                {
                    var value = serieData.data[1];
                    var total = serie.yTotal;
                    var content = SerieLabelHelper.GetFormatterContent(serie, serieData, value, total,
                        serieLabel, serieColor);
                    if (serieData.labelObject.SetText(content)) chart.RefreshPainter(serie);
                }
                else
                {
                    if (serieData.labelObject.SetText(serieData.name)) chart.RefreshPainter(serie);
                }
                serieData.context.labelPosition = serieData.context.position +
                    Vector3.right * (labelLine.lineLength2 + serieData.labelObject.GetTextWidth() / 2);
                serieData.labelObject.SetPosition(serieData.context.labelPosition + serieLabel.offset);
            }
            else
            {
                serieData.SetLabelActive(false);
            }
        }

        private void DrawLabelLine(VertexHelper vh, Serie serie)
        {
            foreach (var serieData in serie.data)
            {
                var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
                if (SerieLabelHelper.CanShowLabel(serie, serieData, serieLabel, 1))
                {
                    Color color = chart.theme.GetColor(serieData.index);
                    if (serieData.context.highlight)
                    {
                        color = ChartHelper.GetHighlightColor(color);
                    }
                    DrawLabelLine(vh, serie, serieData, color);
                }
            }
        }

        private void DrawLabelLine(VertexHelper vh, Serie serie, SerieData serieData, Color color)
        {
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            if (serieLabel.show
                //&& serieLabel.position == SerieLabel.Position.Outside
                &&
                labelLine.show)
            {
                if (!ChartHelper.IsClearColor(labelLine.lineColor)) color = labelLine.lineColor;
                else if (labelLine.lineType == LabelLine.LineType.HorizontalLine) color *= color;
                var ep = serieData.context.position + Vector3.right * labelLine.lineLength2;
                var sp = serieData.context.position + new Vector3(-1, -0.8f) * labelLine.lineLength1;

                switch (labelLine.lineType)
                {
                    case LabelLine.LineType.BrokenLine:
                        UGL.DrawLine(vh, sp, serieData.context.position, ep, labelLine.lineWidth, color);
                        break;
                    case LabelLine.LineType.Curves:
                        UGL.DrawCurves(vh, sp, ep, serieData.context.position, ep, labelLine.lineWidth, color,
                            chart.settings.lineSmoothness);
                        break;
                    case LabelLine.LineType.HorizontalLine:
                        UGL.DrawLine(vh, serieData.context.position, ep, labelLine.lineWidth, color);
                        break;
                }
            }
        }

        private int GetPyramidPosIndex(Serie serie, Vector2 local)
        {
            var data = serie.data;
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
    }
}