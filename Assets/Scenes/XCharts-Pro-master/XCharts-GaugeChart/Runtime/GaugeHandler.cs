using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Gauges
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class GaugeHandler : SerieHandler<Gauge>
    {
        private static readonly string s_SerieLabelObjectName = "label";
        private static readonly string s_AxisLabelObjectName = "axis_label";
        private bool m_UpdateLabelText = false;

        public override void InitComponent()
        {
            base.InitComponent();
            InitAxisLabel(serie);
        }

        public override void Update()
        {
            base.Update();
            if (m_UpdateLabelText)
            {
                m_UpdateLabelText = false;
                SerieLabelHelper.SetGaugeLabelText(serie);
                UpdateAxisLabel(serie);
            }
            serie.context.pointerEnter = false;
            serie.context.pointerItemDataIndex = -1;
            foreach (var serieData in serie.data)
            {
                if (serieData.IsInPolygon(chart.pointerPos))
                {
                    serie.context.pointerEnter = true;
                    serie.context.pointerItemDataIndex = serieData.index;
                }
            }
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override void DrawSerie(VertexHelper vh)
        {
            if (!serie.show) return;
            DrawGauge(vh, serie);
        }

        public override Vector3 GetSerieDataLabelPosition(SerieData serieData, LabelStyle label)
        {
            if (serieData.context.position == Vector3.zero)
            {
                UpdateSerieDataContext(serie, false);
            }
            if (serie.rangeDataMode)
            {
                serieData.context.labelPosition = ChartHelper.GetPosition(serie.context.center, serieData.context.angle, serie.context.insideRadius / 2);
                return serieData.context.labelPosition;
            }
            else
            {
                return serieData.context.position;
            }
        }

        public override Vector3 GetSerieDataTitlePosition(SerieData serieData, TitleStyle titleStyle)
        {
            if (serieData.context.position == Vector3.zero)
            {
                UpdateSerieDataContext(serie, false);
            }
            return serieData.context.position;
        }

        public override Color GetSerieDataAutoColor(SerieData serieData)
        {
            if (serie.gaugeAxis.type == GaugeAxisType.Section)
                return serie.gaugeAxis.GetStageColorByAngle(serieData.context.angle);
            else
                return base.GetSerieDataAutoColor(serieData);
        }

        private void InitAxisLabel(Gauge serie)
        {
            var labelObject = ChartHelper.AddObject(s_AxisLabelObjectName + serie.index, chart.transform,
                chart.chartMinAnchor, chart.chartMaxAnchor, chart.chartPivot, chart.chartSizeDelta);
            labelObject.hideFlags = chart.chartHideFlags;
            SerieLabelPool.ReleaseAll(labelObject.transform);

            var axisLabel = serie.gaugeAxis.axisLabel;
            var count = serie.splitNumber > 36 ? 36 : (serie.splitNumber + 1);
            var startAngle = serie.startAngle;

            serie.gaugeAxis.ClearLabelObject();
            UpdateSerieDataContext(serie, false);

            for (int j = 0; j < count; j++)
            {
                var textName = ChartCached.GetSerieLabelName(s_SerieLabelObjectName, serie.index, j);
                var label = ChartHelper.AddChartLabel(textName, labelObject.transform, axisLabel, chart.theme.common,
                    "", Color.clear, TextAnchor.MiddleCenter);
                label.SetActive(serie.gaugeAxis.show && axisLabel.show && axisLabel.IsNeedShowLabel(j, count), true);
                serie.gaugeAxis.AddLabelObject(label);
            }
            UpdateAxisLabel(serie);
        }

        private void UpdateAxisLabel(Gauge serie)
        {
            var show = serie.gaugeAxis.show && serie.gaugeAxis.axisLabel.show;
            serie.gaugeAxis.SetLabelObjectActive(show);
            if (!show)
            {
                return;
            }
            var count = serie.splitNumber > 36 ? 36 : serie.splitNumber;
            var startAngle = serie.startAngle;
            var totalAngle = serie.endAngle - serie.startAngle;
            var totalValue = serie.max - serie.min;
            var diffAngle = totalAngle / count;
            var diffValue = totalValue / count;
            var radius = serie.context.insideRadius - serie.gaugeAxis.axisLabel.distance;
            var serieData = serie.GetSerieData(0);
            var customLabelText = serie.gaugeAxis.axisLabelText;
            var axisLabel = serie.gaugeAxis.axisLabel;
            var textColor = axisLabel.textStyle.GetColor(chart.theme.axis.textColor);
            for (int j = 0; j <= count; j++)
            {
                var angle = serie.startAngle + j * diffAngle;
                var value = serie.min + j * diffValue;
                var pos = ChartHelper.GetPosition(serie.context.center, angle, radius);
                var text = customLabelText != null && j < customLabelText.Count ? customLabelText[j] :
                    SerieLabelHelper.GetFormatterContent(serie, serieData, value, totalValue,
                        serie.gaugeAxis.axisLabel, Color.clear);
                var label = serie.gaugeAxis.GetLabelObject(j);
                label.SetText(text);
                label.SetActive(serie.gaugeAxis.show && axisLabel.show && axisLabel.IsNeedShowLabel(j, count + 1));
                label.SetPosition(pos + serie.gaugeAxis.axisLabel.offset);
                label.SetTextColor(axisLabel.textStyle.autoColor ?
                    (Color)serie.gaugeAxis.GetStageColorByAngle(angle) : textColor);
            }
        }

        private void DrawGauge(VertexHelper vh, Gauge serie)
        {
            UpdateSerieDataContext(serie);
            DrawProgressBar(vh, serie);
            DrawStageColor(vh, serie);
            DrawSplitLine(vh, serie);
            DrawAxisTick(vh, serie);
            DrawClockData(vh, serie);
            if (serie.gaugeAnchor.showAbove)
            {
                DrawPointer(vh, serie);
                DrawAnchor(vh, serie);
            }
            else
            {
                DrawAnchor(vh, serie);
                DrawPointer(vh, serie);
            }
            if (!serie.animation.context.isAllItemAnimationEnd)
            {
                chart.RefreshPainter(serie);
            }
            else if (NeedRefresh(serie))
            {
                chart.RefreshPainter(serie);
            }
        }

        private void UpdateSerieDataContext(Gauge serie, bool checkAnimation = true)
        {
            SerieHelper.UpdateCenter(serie, chart);
            serie.context.insideRadius = serie.context.insideRadius - serie.gaugeAxis.distance;

            var isClock = serie.rangeDataMode;
            var clockValue = GetPointerValue();
            for (int i = 0; i < serie.dataCount; i++)
            {
                var serieData = serie.data[i];
                if (serie.label != null)
                    serieData.context.labelPosition = serie.context.center + serie.label.offset;
                else
                    serieData.context.labelPosition = serie.context.center;
                double value = 0;
                if (isClock)
                {
                    var svalue = serieData.GetCurrData(0, serie.animation);
                    var mvalue = serieData.GetCurrData(1, serie.animation);
                    if (svalue > mvalue)
                    {
                        value = mvalue / 2 - (serie.max - svalue) / 2;
                        serieData.context.selected = clockValue >= svalue || clockValue < mvalue;
                        serieData.context.highlight = serieData.context.selected;
                    }
                    else
                    {
                        value = (mvalue + svalue) / 2;
                        serieData.context.selected = clockValue >= svalue && clockValue < mvalue;
                        serieData.context.highlight = serieData.context.selected;
                    }
                }
                else
                {
                    value = serieData.GetCurrData(1, serie.animation, false, serie.min, serie.max, serie.loop);
                    value = MathUtil.Clamp(value, serie.min, serie.max);
                }
                var angle = GetValueAngle(value);
                var currAngle = checkAnimation ?
                    AnimationStyleHelper.CheckDataAnimation(chart, serie, i, angle, serie.startAngle) :
                    angle;
                serieData.context.angle = currAngle;
                serieData.context.position = serie.context.center;
            }
        }

        private float GetValueAngle(double value)
        {
            var rangeValue = serie.max - serie.min;
            var rangeAngle = serie.endAngle - serie.startAngle;
            var angle = serie.startAngle;
            if (rangeValue > 0)
            {
                angle += rangeAngle * (float)(value - serie.min) / rangeValue;
            }
            return angle;
        }

        private void DrawProgressBar(VertexHelper vh, Gauge serie)
        {
            var backgroundColor = serie.gaugeAxis.GetAxisLineBackgroundColor(chart.theme, serie.index);
            var lineWidth = serie.gaugeAxis.axisLine.GetWidth(chart.theme.axis.lineWidth);
            var outsideRadius = serie.context.insideRadius + lineWidth;
            if (!ChartHelper.IsClearColor(backgroundColor))
            {
                var borderWidth = serie.itemStyle.borderWidth;
                var borderColor = serie.itemStyle.borderColor;
                UGL.DrawDoughnut(vh, serie.context.center, serie.context.insideRadius, outsideRadius,
                    backgroundColor, backgroundColor, Color.clear, serie.startAngle, serie.endAngle, 0, Color.clear,
                    0, chart.settings.cicleSmoothness, serie.roundCap);
            }
            Color32 itemColor, itemToColor;
            if (serie.gaugeAxis.type == GaugeAxisType.Progress)
            {
                var range = serie.max - serie.min;
                if (serie.gaugeAxis.overlap)
                {
                    SerieHelper.UpdateSerieRuntimeFilterData(serie);
                    foreach (var serieData in serie.context.sortedData)
                    {
                        var currAngle = serieData.context.angle;
                        SerieHelper.GetItemColor(out itemColor, out itemToColor, serie, serieData, chart.theme, serieData.index);
                        var currColor = Color32.Lerp(itemColor, itemToColor, (float)((serieData.GetData(1) - serie.min) / range));
                        serieData.context.color = currColor;
                        if (!serie.gaugeAxis.show || !serie.gaugeAxis.axisLine.show) continue;
                        UGL.DrawDoughnut(vh, serie.context.center, serie.context.insideRadius, outsideRadius,
                            itemColor, currColor, Color.clear, serie.startAngle, currAngle, 0, Color.clear,
                            0, chart.settings.cicleSmoothness, serie.roundCap);
                    }
                }
                else
                {
                    var eachRadius = lineWidth / serie.dataCount;
                    var insideRadius = outsideRadius - eachRadius;
                    for (int i = 0; i < serie.dataCount; i++)
                    {
                        var serieData = serie.data[i];
                        var currAngle = serieData.context.angle;
                        SerieHelper.GetItemColor(out itemColor, out itemToColor, serie, serieData, chart.theme, serieData.index);
                        var currColor = Color32.Lerp(itemColor, itemToColor, (float)((serieData.GetData(1) - serie.min) / range));
                        serieData.context.color = currColor;
                        if (serie.gaugeAxis.show && serie.gaugeAxis.axisLine.show)
                        {
                            UGL.DrawDoughnut(vh, serie.context.center, insideRadius, outsideRadius,
                                itemColor, currColor, Color.clear, serie.startAngle, currAngle, 0, Color.clear,
                                0, chart.settings.cicleSmoothness, serie.roundCap);
                        }
                        outsideRadius -= eachRadius;
                        insideRadius -= eachRadius;
                    }
                }
            }
            else
            {
                for (int i = 0; i < serie.dataCount; i++)
                {
                    var serieData = serie.data[i];
                    SerieHelper.GetItemColor(out itemColor, out itemToColor, serie, serieData, chart.theme, serieData.index);
                    serieData.context.color = itemColor;
                }
            }
        }

        private void DrawClockData(VertexHelper vh, Gauge serie)
        {
            if (!serie.rangeDataMode) return;
            var lineWidth = serie.gaugeAxis.axisLine.GetWidth(chart.theme.axis.lineWidth);
            var outsideRadius = serie.context.insideRadius + lineWidth;
            Color32 color, toColor;
            serie.gaugeAxis.runtimeStageAngle.Clear();
            foreach (var serieData in serie.data)
            {
                var itemStyle = SerieHelper.GetItemStyle(serie, serieData);
                SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, serieData.index);
                var startValue = serieData.GetData(0);
                var endValue = serieData.GetData(1);
                var startAngle = GetValueAngle(startValue);
                var endAngle = GetValueAngle(endValue);
                if (startAngle > endAngle)
                {
                    startAngle = -(360 - startAngle);
                }
                var outRadius = outsideRadius;
                Color32 bcolor;
                if (ChartHelper.IsClearColor(itemStyle.backgroundColor))
                {
                    bcolor = color;
                    bcolor.a = 35;
                }
                else
                {
                    bcolor = itemStyle.backgroundColor;
                }
                if (serieData.selected || serieData.context.selected)
                {
                    bcolor = ChartHelper.GetHighlightColor(bcolor);
                    color = ChartHelper.GetHighlightColor(color);
                    if (serieData.radius > 0)
                        outRadius += serieData.radius;
                    else
                        outRadius += lineWidth * 0.5f;
                }
                UGL.DrawSector(vh, serie.context.center, serie.context.insideRadius,
                    bcolor, startAngle, endAngle, chart.settings.cicleSmoothness);

                if (itemStyle.borderWidth != 0)
                {
                    var epos = ChartHelper.GetPosition(serie.context.center, startAngle, serie.context.insideRadius);
                    UGL.DrawLine(vh, serie.context.center, epos, itemStyle.borderWidth, itemStyle.borderColor);
                }
                UGL.DrawDoughnut(vh, serie.context.center, serie.context.insideRadius, outRadius,
                    color, toColor, Color.clear, startAngle, endAngle, 0, Color.clear,
                    0, chart.settings.cicleSmoothness, serie.roundCap);

                serie.gaugeAxis.runtimeStageAngle.Add(startAngle);
            }
        }

        private void DrawStageColor(VertexHelper vh, Gauge serie)
        {
            if (serie.rangeDataMode) return;
            if (serie.gaugeAxis.type != GaugeAxisType.Section) return;
            if (!serie.gaugeAxis.show || !serie.gaugeAxis.axisLine.show) return;
            var totalAngle = serie.endAngle - serie.startAngle;
            var tempStartAngle = serie.startAngle;
            var tempEndAngle = serie.startAngle;
            var lineWidth = serie.gaugeAxis.axisLine.GetWidth(chart.theme.axis.lineWidth);
            var outsideRadius = serie.context.insideRadius + lineWidth;
            serie.gaugeAxis.runtimeStageAngle.Clear();
            if (serie.gaugeAxis.axisLine.stageGradient)
            {
                serie.gaugeAxis.runtimeStageAngle.Add(tempStartAngle);
                for (int i = 1; i < serie.gaugeAxis.axisLine.stageColor.Count; i++)
                {
                    var stageColor = serie.gaugeAxis.axisLine.stageColor[i - 1];
                    var stageToColor = serie.gaugeAxis.axisLine.stageColor[i];
                    tempEndAngle = serie.startAngle + totalAngle * stageToColor.percent;
                    serie.gaugeAxis.runtimeStageAngle.Add(tempEndAngle);
                    UGL.DrawDoughnut(vh, serie.context.center, serie.context.insideRadius, outsideRadius,
                        stageColor.color, stageToColor.color, Color.clear, tempStartAngle, tempEndAngle, 0, Color.clear,
                        0, chart.settings.cicleSmoothness, serie.roundCap);
                    tempStartAngle = tempEndAngle;
                }
            }
            else
            {
                for (int i = 0; i < serie.gaugeAxis.axisLine.stageColor.Count; i++)
                {
                    var stageColor = serie.gaugeAxis.axisLine.stageColor[i];
                    tempEndAngle = serie.startAngle + totalAngle * stageColor.percent;
                    serie.gaugeAxis.runtimeStageAngle.Add(tempEndAngle);
                    UGL.DrawDoughnut(vh, serie.context.center, serie.context.insideRadius, outsideRadius,
                        stageColor.color, stageColor.color, Color.clear, tempStartAngle, tempEndAngle, 0, Color.clear,
                        0, chart.settings.cicleSmoothness, serie.roundCap);
                    tempStartAngle = tempEndAngle;
                }
            }
        }


        private void DrawPointer(VertexHelper vh, Gauge serie)
        {
            if (!serie.gaugePointer.show) return;
            var isClock = serie.rangeDataMode;
            foreach (var serieData in serie.data)
            {
                var currAngle = isClock ? GetValueAngle(GetPointerValue()) : serieData.context.angle;
                if (chart.customDrawGaugePointerFunction != null)
                {
                    chart.customDrawGaugePointerFunction(vh, serie.index, serieData.index, currAngle);
                    continue;
                }
                var pointerColor = serie.GetPointerColor(chart.theme, serieData.index, currAngle, serieData.context.color);
                var pointerToColor = !ChartHelper.IsClearColor(serie.gaugePointer.toColor) ? serie.gaugePointer.toColor : pointerColor;
                var len = ChartHelper.GetActualValue(serie.gaugePointer.length, serie.context.insideRadius);
                var backLen = ChartHelper.GetActualValue(serie.gaugePointer.backLength, serie.context.insideRadius);
                var extraLen = ChartHelper.GetActualValue(serie.gaugePointer.backExtraLength, serie.context.insideRadius);
                var leftLen = ChartHelper.GetActualValue(serie.gaugePointer.bottomLeftLength, serie.context.insideRadius);
                var rightLen = ChartHelper.GetActualValue(serie.gaugePointer.bottomRightLength, serie.context.insideRadius);
                var topLeftLen = ChartHelper.GetActualValue(serie.gaugePointer.topLeftLength, serie.context.insideRadius);
                var topRightLen = ChartHelper.GetActualValue(serie.gaugePointer.topRightLength, serie.context.insideRadius);

                var offsetY = ChartHelper.GetActualValue(serie.gaugePointer.distance, serie.context.insideRadius);
                var center = ChartHelper.GetPosition(serie.context.center, currAngle, offsetY);
                var posTopCenter = ChartHelper.GetPosition(center, currAngle, len);
                var posTopLeft = ChartHelper.GetPosition(posTopCenter, currAngle - 90, topLeftLen);
                var posTopRight = ChartHelper.GetPosition(posTopCenter, currAngle + 90, topRightLen);
                var posBack = ChartHelper.GetPosition(center, currAngle + 180, backLen + extraLen);
                var posBottomLeft = ChartHelper.GetPosition(center, currAngle - 90, leftLen);
                var posBottomRight = ChartHelper.GetPosition(center, currAngle + 90, rightLen);

                if (backLen != 0)
                {
                    posBottomLeft = posBottomLeft + (posBottomLeft - posTopLeft).normalized * backLen;
                    posBottomRight = posBottomRight + (posBottomRight - posTopRight).normalized * backLen;
                }

                UGL.DrawTriangle(vh, posBack, posBottomLeft, posTopLeft, pointerColor, pointerColor, pointerToColor);
                UGL.DrawTriangle(vh, posBottomRight, posBack, posTopRight, pointerColor, pointerColor, pointerToColor);
                UGL.DrawTriangle(vh, posBack, posTopLeft, posTopRight, pointerColor, pointerToColor, pointerToColor);

                if (serie.gaugePointer.roundCap)
                {
                    UGL.DrawCricle(vh, (posTopLeft + posTopRight) / 2,
                        Vector3.Distance(posTopLeft, posTopRight) / 2, pointerToColor);
                    if (extraLen == 0)
                    {
                        UGL.DrawCricle(vh, (posBottomLeft + posBottomRight) / 2,
                            Vector3.Distance(posBottomLeft, posBottomRight) / 2, pointerColor);
                    }
                }
                serieData.SetPolygon(posBack, posBottomLeft, posTopLeft, posTopRight, posBottomRight);
                if (isClock)
                    break;
            }
        }

        private double GetPointerValue()
        {
            return serie.gaugePointer.value % serie.max;
        }

        private void DrawAnchor(VertexHelper vh, Gauge serie)
        {
            var serieData = serie.GetSerieData(0);
            if (serie.gaugeAnchor == null || !serie.gaugeAnchor.show || serieData == null) return;
            float symbolBorder = 0f;
            float[] cornerRadius = null;
            Color32 symbolEmptyColor, borderColor;
            var currAngle = serieData.context.angle;
            var pointerColor = serie.GetPointerColor(chart.theme, serieData.index, currAngle, serieData.context.color);

            var itemStyle = serie.gaugeAnchor.itemStyle;
            symbolEmptyColor = itemStyle.backgroundColor;
            borderColor = itemStyle.borderColor;
            symbolBorder = itemStyle.borderWidth != 0 ? itemStyle.borderWidth : serie.lineStyle.GetWidth(chart.theme.serie.lineWidth);

            var pos = serie.gaugeAnchor.GetPosition(serie.context.center, serie.context.insideRadius);
            chart.DrawSymbol(vh, serie.gaugeAnchor.type, serie.gaugeAnchor.size, symbolBorder, pos, pointerColor,
                pointerColor, symbolEmptyColor, borderColor, serie.gaugeAnchor.gap, cornerRadius);
        }

        private void DrawSplitLine(VertexHelper vh, Gauge serie)
        {
            if (!serie.gaugeAxis.show || !serie.gaugeAxis.splitLine.show) return;
            if (serie.splitNumber <= 0) return;
            var splitLine = serie.gaugeAxis.splitLine;
            var totalAngle = serie.endAngle - serie.startAngle;
            var diffAngle = totalAngle / serie.splitNumber;
            var splitLineWidth = splitLine.GetWidth(chart.theme.axis.splitLineWidth);
            var splitLineLength = splitLine.GetLength(chart.theme.axis.splitLineLength);
            var outsideRadius = serie.context.insideRadius - serie.gaugeAxis.splitLine.distance;
            var insideRadius = outsideRadius - splitLineLength;
            for (int i = 0; i < serie.splitNumber + 1; i++)
            {
                var rate = i * diffAngle / totalAngle;
                var angle = serie.startAngle + i * diffAngle;
                var lineColor = serie.gaugeAxis.GetSplitLineColor(chart.theme.axis.splitLineColor, serie.index, angle, rate);
                var p1 = ChartHelper.GetPosition(serie.context.center, angle, insideRadius);
                var p2 = ChartHelper.GetPosition(serie.context.center, angle, outsideRadius);
                UGL.DrawLine(vh, p1, p2, splitLineWidth, lineColor);
            }
        }

        private void DrawAxisTick(VertexHelper vh, Gauge serie)
        {
            if (!serie.gaugeAxis.show || !serie.gaugeAxis.axisTick.show) return;
            if (serie.splitNumber <= 0) return;
            var axisTick = serie.gaugeAxis.axisTick;
            var totalAngle = serie.endAngle - serie.startAngle;
            var diffAngle = totalAngle / serie.splitNumber;
            var lineWidth = serie.gaugeAxis.axisLine.GetWidth(chart.theme.axis.lineWidth);
            var tickWidth = axisTick.GetWidth(chart.theme.axis.tickWidth);
            var tickLength = axisTick.GetLength(chart.theme.axis.tickLength);
            var outsideRadius = serie.context.insideRadius - serie.gaugeAxis.axisTick.distance;

            var insideRadius = outsideRadius - (tickLength < 1 ? lineWidth * tickLength : tickLength);
            for (int i = 0; i < serie.splitNumber; i++)
            {
                for (int j = 1; j < axisTick.splitNumber; j++)
                {
                    var angle = i * diffAngle + j * (diffAngle / axisTick.splitNumber);
                    var rate = angle / totalAngle;
                    var nowAngle = serie.startAngle + angle;
                    var lineColor = serie.gaugeAxis.GetAxisTickColor(chart.theme.axis.tickColor, serie.index, nowAngle, rate);
                    var p1 = ChartHelper.GetPosition(serie.context.center, nowAngle, insideRadius);
                    var p2 = ChartHelper.GetPosition(serie.context.center, nowAngle, outsideRadius);
                    UGL.DrawLine(vh, p1, p2, tickWidth, lineColor);
                }
            }
        }

        private bool NeedRefresh(Serie serie)
        {
            foreach (var serieData in serie.data)
            {
                if (serieData.IsDataChanged())
                    return true;
            }
            return false;
        }
    }
}