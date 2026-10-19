using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Pie3Ds
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class Pie3DHandler : SerieHandler<Pie3D>
    {
        private List<Pie3DSlice> m_Slices = new List<Pie3DSlice>();

        public override void Update()
        {
            base.Update();
        }

        public override void DrawSerie(VertexHelper vh)
        {
            UpdateRuntimeData(serie);
            DrawPie(vh, serie);
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override Vector3 GetSerieDataLabelPosition(SerieData serieData, LabelStyle label)
        {
            return serieData.context.labelPosition;
        }

        public override Vector3 GetSerieDataLabelOffset(SerieData serieData, LabelStyle label)
        {
            var offset = label.GetOffset(serie.context.insideRadius);
            if (label.autoOffset)
            {
                var currAngle = serieData.context.halfAngle;
                var isRight = currAngle <= 90 || currAngle >= 270;
                if (isRight) return offset;
                else return new Vector3(-offset.x, offset.y, offset.z);
            }
            else
            {
                return offset;
            }
        }

        public override void OnLegendButtonClick(int index, string legendName, bool show)
        {
            if (!serie.IsLegendName(legendName))
                return;
            LegendHelper.CheckDataShow(serie, legendName, show);
            chart.UpdateLegendColor(legendName, show);
            chart.RefreshPainter(serie);
        }

        public override void OnLegendButtonEnter(int index, string legendName)
        {
            if (!serie.IsLegendName(legendName))
                return;
            LegendHelper.CheckDataHighlighted(serie, legendName, true);
            chart.RefreshPainter(serie);
        }

        public override void OnLegendButtonExit(int index, string legendName)
        {
            if (!serie.IsLegendName(legendName))
                return;
            LegendHelper.CheckDataHighlighted(serie, legendName, false);
            chart.RefreshPainter(serie);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            var index = GetPiePosIndex(serie, chart.pointerPos);
            if (index >= 0)
            {
                for (int j = 0; j < serie.data.Count; j++)
                {
                    if (j == index) serie.data[j].context.selected = !serie.data[j].context.selected;
                    else serie.data[j].context.selected = false;
                }
                chart.RefreshChart(serie);
            }
            base.OnPointerDown(eventData);
        }

        public override int GetPointerItemDataIndex()
        {
            return GetPiePosIndex(serie, chart.pointerPos);
        }

        public override void UpdateSerieContext()
        {
            var needCheck = m_LegendEnter || chart.isPointerInChart;
            var needInteract = false;
            Color32 color, toColor;
            if (!needCheck)
            {
                if (m_LastCheckContextFlag != needCheck)
                {
                    m_LastCheckContextFlag = needCheck;
                    serie.context.pointerItemDataIndex = -1;
                    serie.context.pointerEnter = false;
                    foreach (var serieData in serie.data)
                    {
                        var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                        SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, colorIndex, SerieState.Normal);
                        serieData.context.highlight = false;
                        serieData.interact.SetValueAndColor(ref needInteract, serieData.context.outsideRadius, color, toColor);
                    }
                    if (needInteract)
                    {
                        chart.RefreshPainter(serie);
                    }
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;
            var lastPointerItemDataIndex = serie.context.pointerItemDataIndex;
            var dataIndex = GetPiePosIndex(serie, chart.pointerPos);
            serie.context.pointerItemDataIndex = -1;
            serie.context.pointerEnter = false;
            for (int i = 0; i < serie.dataCount; i++)
            {
                var serieData = serie.data[i];
                if (dataIndex == i || (m_LegendEnter && m_LegendEnterIndex == i))
                {
                    serie.context.pointerEnter = true;
                    serie.context.pointerItemDataIndex = i;
                    serieData.context.highlight = true;

                    var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                    SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, colorIndex, SerieState.Emphasis);
                    var value = serie.animation.GetInteractionRadius(serieData.context.outsideRadius);
                    serieData.interact.SetValueAndColor(ref needInteract, value, color, toColor);
                }
                else
                {
                    serieData.context.highlight = false;
                    var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                    SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, colorIndex, SerieState.Normal);
                    serieData.interact.SetValueAndColor(ref needInteract, serieData.context.outsideRadius, color, toColor);
                }
            }
            if (lastPointerItemDataIndex != serie.context.pointerItemDataIndex)
            {
                needInteract = true;
            }
            if (needInteract)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void UpdateRuntimeData(Pie3D serie)
        {
            var data = serie.data;
            serie.context.dataMax = serie.yMax;
            var runtimePieDataTotal = serie.yTotal;

            SerieHelper.UpdateCenter(serie, chart);
            float totalDegree = 0;
            float startDegree = GetStartAngle(serie);
            float zeroReplaceValue = 0;
            int showdataCount = 0;
            foreach (var sd in serie.data)
            {
                if (sd.show && serie.pieRoseType == RoseType.Area) showdataCount++;
                sd.context.canShowLabel = false;
            }
            bool isAllZeroValue = SerieHelper.IsAllZeroValue(serie, 1);
            var dataTotalFilterMinAngle = runtimePieDataTotal;
            if (isAllZeroValue)
            {
                totalDegree = 360;
                zeroReplaceValue = totalDegree / data.Count;
                serie.context.dataMax = zeroReplaceValue;
                runtimePieDataTotal = 360;
                dataTotalFilterMinAngle = 360;
            }
            else
            {
                dataTotalFilterMinAngle = GetTotalAngle(serie, runtimePieDataTotal, ref totalDegree);
            }
            if (dataTotalFilterMinAngle == 0)
            {
                dataTotalFilterMinAngle = 360;
            }
            for (int n = 0; n < data.Count; n++)
            {
                var serieData = data[n];
                var value = isAllZeroValue ? zeroReplaceValue : serieData.GetCurrData(1, serie.animation);
                serieData.context.startAngle = startDegree;
                serieData.context.toAngle = startDegree;
                serieData.context.halfAngle = startDegree;
                serieData.context.currentAngle = startDegree;
                if (!serieData.show)
                {
                    continue;
                }
                float degree = (float)(totalDegree * value / dataTotalFilterMinAngle);
                if (serie.minAngle > 0 && degree < serie.minAngle) degree = serie.minAngle;
                serieData.context.toAngle = startDegree + degree;
                serieData.context.currentAngle = serieData.context.toAngle;
                serieData.context.halfAngle = startDegree + (serieData.context.toAngle - startDegree) / 2;
                serieData.context.offsetCenter = serie.context.center;
                serieData.context.insideRadius = serie.context.insideRadius;
                serieData.context.outsideRadius = serie.context.outsideRadius;
                if (serie.pie3dStyle.selectedOffset != Vector2.zero && (serieData.selected || serieData.context.selected))
                {
                    var majorAxis = serieData.context.outsideRadius;
                    var minorAxis = ChartHelper.GetActualValue(serie.pie3dStyle.ellipseMinorAxis, majorAxis);
                    var currRad = -serieData.context.halfAngle * Mathf.Deg2Rad;
                    var offsetX = ChartHelper.GetActualValue(serie.pie3dStyle.selectedOffset[0], serie.context.outsideRadius);
                    var offsetY = ChartHelper.GetActualValue(serie.pie3dStyle.selectedOffset[1], serie.pie3dStyle.pieMaxHeight);
                    var w = offsetX;
                    var h = w * minorAxis / majorAxis;

                    var x = serie.context.center.x + w * Mathf.Cos(currRad);
                    var y = serie.context.center.y + h * Mathf.Sin(currRad);

                    serieData.context.offsetCenter = new Vector3(x, y + offsetY);
                }
                serieData.context.canShowLabel = !serie.IsMinShowLabelValue(value);
                startDegree = serieData.context.toAngle;
                UpdatePieLabelPosition(serie, serieData);
            }
            AvoidLabelOverlap(serie, chart.theme.common);
        }

        private double GetTotalAngle(Serie serie, double dataTotal, ref float totalAngle)
        {
            totalAngle = 360f;
            if (serie.minAngle > 0)
            {
                var rate = serie.minAngle / 360;
                var minAngleValue = dataTotal * rate;
                foreach (var serieData in serie.data)
                {
                    var value = serieData.GetData(1);
                    if (value < minAngleValue)
                    {
                        totalAngle -= serie.minAngle;
                        dataTotal -= value;
                    }
                }
                return dataTotal;
            }
            else
            {
                return dataTotal;
            }
        }

        private void DrawPieCenter(VertexHelper vh, Serie serie, ItemStyle itemStyle, float insideRadius)
        {
            if (!ChartHelper.IsClearColor(itemStyle.centerColor))
            {
                var radius = insideRadius - itemStyle.centerGap;
                UGL.DrawCricle(vh, serie.context.center, radius, itemStyle.centerColor, chart.settings.cicleSmoothness);
            }
        }

        private Dictionary<int, float> m_SliceHigDict = new Dictionary<int, float>();
        private void DrawPie(VertexHelper vh, Pie3D serie)
        {
            if (!serie.show || serie.animation.HasFadeOut())
            {
                return;
            }
            UpdateSerieRuntimeFilterData(serie);
            var dataChanging = false;
            Color32 color, toColor;
            m_SliceHigDict.Clear();
            for (int n = 0; n < m_Slices.Count; n++)
            {
                var slice = m_Slices[n];
                var serieData = serie.GetSerieData(slice.serieDataIndex);
                if (!serieData.show)
                    continue;
                if (serieData.IsDataChanged())
                    dataChanging = true;
                var colorIndex = chart.GetLegendRealShowNameIndex(serieData.legendName);
                var state = SerieHelper.GetSerieState(serie, serieData);
                SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, colorIndex, state);
                var currHig = 0f;
                if(m_SliceHigDict.ContainsKey(slice.serieDataIndex))
                {
                    currHig = m_SliceHigDict[slice.serieDataIndex];
                }
                else
                {
                    currHig = AnimationStyleHelper.CheckDataAnimation(chart, serie, n, slice.hig);
                    m_SliceHigDict[slice.serieDataIndex] = currHig;
                }
                var inMajorAxis = serieData.context.insideRadius;
                var inMinorAxis = ChartHelper.GetActualValue(serie.pie3dStyle.ellipseMinorAxis, inMajorAxis);
                var outMajorAxis = serieData.context.outsideRadius;
                var outMinorAxis = ChartHelper.GetActualValue(serie.pie3dStyle.ellipseMinorAxis, outMajorAxis);

                var center = serieData.context.offsetCenter;
                DrawEllipse(vh, center, inMajorAxis, inMinorAxis, outMajorAxis, outMinorAxis, currHig, color,
                    chart.settings.cicleSmoothness, slice);

                serieData.context.areaCenter = GetEllipseAreaCenter(center, inMajorAxis, inMinorAxis, outMajorAxis, outMinorAxis,
                    currHig, serieData.context.startAngle, serieData.context.toAngle);
                serieData.context.position = GetEllipseSideCenter(center, inMajorAxis, inMinorAxis, outMajorAxis, outMinorAxis,
                    currHig, serieData.context.startAngle, serieData.context.toAngle);

                var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
                if (SerieLabelHelper.CanShowLabel(serie, serieData, serieLabel, 1))
                {
                    DrawPieLabelLine(vh, serie, serieData, color);
                }
            }
            if (!serie.animation.IsFinish())
            {
                serie.animation.CheckProgress();
                chart.RefreshPainter(serie);
            }
            if (dataChanging)
            {
                chart.RefreshPainter(serie);
            }
        }

        private void UpdateSerieRuntimeFilterData(Pie3D serie, bool filterInvisible = true)
        {
            Pie3DSlicePool.Release(m_Slices);
            var data = serie.data;
            var dataTotal = serie.yTotal;
            var dataMax = serie.yMax;
            var totalTickness = serie.pie3dStyle.pieMaxHeight * dataTotal / dataMax;
            var drawSide = serie.pie3dStyle.drawSplitSide || serie.pie3dStyle.pieAutoHeight;
            for (int i = 0; i < serie.data.Count; i++)
            {
                var serieData = serie.data[i];
                if (!filterInvisible || (filterInvisible && serieData.show))
                {
                    var startAngle = serieData.context.startAngle;
                    var endAngle = serieData.context.currentAngle;
                    var hig = serie.pie3dStyle.pieAutoHeight && dataMax != 0 ?
                        (float)((serieData.context.toAngle - serieData.context.startAngle) / 360f * totalTickness) :
                        (serieData.radius != 0 ? serieData.radius : serie.pie3dStyle.pieMaxHeight);
                    var is360 = endAngle - startAngle >= 360;
                    if (endAngle > 180 && startAngle < 180)
                    {
                        var slice1 = Pie3DSlicePool.Get();
                        slice1.UpdateValue(serieData.index, startAngle, 180, !is360 && drawSide, false, hig);
                        m_Slices.Add(slice1);

                        var slice2 = Pie3DSlicePool.Get();
                        slice2.UpdateValue(serieData.index, 180, endAngle, false, !is360 && drawSide, hig);
                        m_Slices.Add(slice2);
                    }
                    else
                    {
                        var slice1 = Pie3DSlicePool.Get();
                        slice1.UpdateValue(serieData.index, startAngle, endAngle, !is360 && drawSide, !is360 && drawSide, hig);
                        m_Slices.Add(slice1);
                    }
                }
            }
            m_Slices.Sort(delegate (Pie3DSlice p1, Pie3DSlice p2)
            {
                return p2.cos.CompareTo(p1.cos);
            });
        }

        private Vector3 GetEllipseAreaCenter(Vector3 center, float inw, float inh, float outw, float outh, float hig,
            float startAngle, float endAngle)
        {
            var w = (inw + outw) / 2;
            var h = (inh + outh) / 2;
            var angle = startAngle + (endAngle - startAngle) / 2;
            var rad = (360 - angle) * Mathf.Deg2Rad;
            var x = center.x + w * Mathf.Cos(rad);
            var y = center.y + h * Mathf.Sin(rad);
            return new Vector2(x, y + hig);
        }

        private Vector3 GetEllipseSideCenter(Vector3 center, float inw, float inh, float outw, float outh, float hig,
            float startAngle, float endAngle)
        {
            var w = outw;
            var h = outh;
            var angle = startAngle + (endAngle - startAngle) / 2;
            var rad = (360 - angle) * Mathf.Deg2Rad;
            var x = center.x + w * Mathf.Cos(rad);
            var y = center.y + h * Mathf.Sin(rad);
            return new Vector2(x, y + hig);
        }

        private void DrawEllipse(VertexHelper vh, Vector3 center, float inw, float inh, float outw, float outh, float hig,
            Color32 color, float smoothness, Pie3DSlice slice)
        {
            float startAngle = slice.startAngle;
            float endAngle = slice.endAngle;
            bool startSide = slice.showStartSide;
            bool endSide = slice.showEndSide;

            if (endAngle < startAngle)
                endAngle += 360;
            if (endAngle <= startAngle)
                return;
            if (inw < 0) inw = 0;
            if (inh < 0) inh = 0;
            if (outw < 0) outw = 0;
            if (outh < 0) outh = 0;
            if (inw > outw) inw = outw;
            if (inh > outh) inh = outh;
            if (smoothness < 0.5f) smoothness = 0.5f;

            var count = Mathf.CeilToInt((endAngle - startAngle) / smoothness);
            var diff = (endAngle - startAngle) / count;
            var currentVertCount = vh.currentVertCount;
            var pcb = center;
            var pct = new Vector3(center.x, center.y + hig);
            var sideColor = ChartHelper.GetHighlightColor(color, serie.pie3dStyle.peripherySideColorRate);
            var startSideColor = ChartHelper.GetHighlightColor(color, serie.pie3dStyle.startSideColorRate);
            var endSideColor = ChartHelper.GetHighlightColor(color, serie.pie3dStyle.endSideColorRate);
            var index = 0;
            var isLeft = false;
            var needBorder = serie.itemStyle.borderWidth > 0;
            var ptList = ListPool<Vector3>.Get();
            var pbList = ListPool<Vector3>.Get();
            if (inw == 0 && inh == 0)
            {
                vh.AddVert(pcb, startSideColor, Vector2.zero); // 0
                vh.AddVert(pct, startSideColor, Vector2.zero); // 1
                vh.AddVert(pcb, endSideColor, Vector2.zero); // 2
                vh.AddVert(pct, endSideColor, Vector2.zero); // 3
                vh.AddVert(pct, color, Vector2.zero); // 4
                slice.topPolygons.Add(pct);
                for (int i = 0; i <= count; i++)
                {
                    var angle = startAngle + i * diff;
                    var rad = (360 - angle) * Mathf.Deg2Rad;
                    var x = center.x + outw * Mathf.Cos(rad);
                    var y = center.y + outh * Mathf.Sin(rad);
                    var pb = new Vector3(x, y);
                    var pt = new Vector2(x, y + hig);
                    isLeft = pb.x < center.x;
                    if (needBorder)
                    {
                        ptList.Add(pt);
                        if (angle == 0) pbList.Add(pt);
                        if (angle >= 0 && angle <= 180) pbList.Add(pb);
                        if (angle == 180) pbList.Add(pt);
                    }
                    if (i == 0)
                    {
                        vh.AddVert(pb, startSideColor, Vector2.zero); // 5
                        vh.AddVert(pt, startSideColor, Vector2.zero); // 6
                        slice.sidePolygons.Add(pb);
                        slice.sidePolygons.Add(pt);
                        slice.topPolygons.Add(pt);
                    }
                    vh.AddVert(pb, sideColor, Vector2.zero);
                    vh.AddVert(pt, sideColor, Vector2.zero);
                    vh.AddVert(pt, color, Vector2.zero);
                    index = currentVertCount + (i + 2) * 3 + 1;
                    if (i == 0)
                    {
                        if (isLeft && startSide)
                        {
                            slice.startOrEndPolygons.Add(pcb);
                            slice.startOrEndPolygons.Add(pct);
                            slice.startOrEndPolygons.Add(pt);
                            slice.startOrEndPolygons.Add(pb);

                            vh.AddTriangle(currentVertCount, currentVertCount + 5, currentVertCount + 1); // startSide: 0 5 1
                            vh.AddTriangle(currentVertCount + 1, currentVertCount + 5, currentVertCount + 6); //startSide: 1 5 6
                        }
                    }
                    else
                    {
                        vh.AddTriangle(currentVertCount + 4, index - 1, index + 2); // top: 4 12 9
                        if (angle >= 0 && angle <= 180)
                        {
                            vh.AddTriangle(index, index - 3, index + 1); // side: 10 7 11
                            vh.AddTriangle(index - 3, index + 1, index - 2); // side: 7 11 8
                        }
                    }
                    if (i == count)
                    {
                        slice.sidePolygons.Add(pt);
                        slice.sidePolygons.Add(pb);
                        slice.topPolygons.Add(pt);
                        if (!isLeft && endSide)
                        {
                            slice.startOrEndPolygons.Add(pcb);
                            slice.startOrEndPolygons.Add(pct);
                            slice.startOrEndPolygons.Add(pt);
                            slice.startOrEndPolygons.Add(pb);
                            vh.AddVert(pb, endSideColor, Vector2.zero); // 5
                            vh.AddVert(pt, endSideColor, Vector2.zero); // 6
                            vh.AddTriangle(currentVertCount + 2, currentVertCount + 3, index + 3); // endSide: 2 3 16
                            vh.AddTriangle(currentVertCount + 3, index + 3, index + 4); // endSide: 3 16 17
                        }
                    }
                }

            }
            else
            {
                var ptList2 = ListPool<Vector3>.Get();
                for (int i = 0; i <= count; i++)
                {
                    var angle = startAngle + i * diff;
                    var rad = (360 - angle) * Mathf.Deg2Rad;
                    var sin = Mathf.Sin(rad);
                    var cos = Mathf.Cos(rad);
                    var ix = center.x + inw * cos;
                    var iy = center.y + inh * sin;
                    var ox = center.x + outw * cos;
                    var oy = center.y + outh * sin;
                    var ipb = new Vector3(ix, iy);
                    var ipt = new Vector2(ix, iy + hig);
                    var opb = new Vector3(ox, oy);
                    var opt = new Vector2(ox, oy + hig);
                    isLeft = opb.x < center.x;
                    if (needBorder)
                    {
                        ptList.Add(opt);
                        ptList2.Add(ipt);
                        if (angle == 0) pbList.Add(opt);
                        if (angle >= 0 && angle <= 180) pbList.Add(opb);
                        if (angle == 180) pbList.Add(opt);
                    }
                    if (i == 0)
                    {
                        slice.topPolygons.Add(ipt);
                        slice.topPolygons.Add(opt);
                        slice.sidePolygons.Add(opb);
                        slice.sidePolygons.Add(opt);
                        vh.AddVert(ipb, startSideColor, Vector2.zero); //0
                        vh.AddVert(ipt, startSideColor, Vector2.zero); // 1
                        vh.AddVert(opb, startSideColor, Vector2.zero); // 2
                        vh.AddVert(opt, startSideColor, Vector2.zero); // 3
                    }
                    vh.AddVert(ipb, sideColor, Vector2.zero); //4
                    vh.AddVert(ipt, sideColor, Vector2.zero); //5
                    vh.AddVert(ipt, color, Vector2.zero); //6

                    vh.AddVert(opb, sideColor, Vector2.zero); //7
                    vh.AddVert(opt, sideColor, Vector2.zero); //8
                    vh.AddVert(opt, color, Vector2.zero); //9
                    index = currentVertCount + (i * 6) + 4;
                    if (i == 0)
                    {
                        if (isLeft && startSide)
                        {
                            slice.startOrEndPolygons.Add(ipb);
                            slice.startOrEndPolygons.Add(ipt);
                            slice.startOrEndPolygons.Add(opt);
                            slice.startOrEndPolygons.Add(opb);
                            vh.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 3); // startSide: 0 1 3
                            vh.AddTriangle(currentVertCount, currentVertCount + 3, currentVertCount + 2); //startSide: 1 5 6
                        }
                    }
                    else
                    {
                        //inside
                        if (angle >= 180 && angle <= 360)
                        {
                            vh.AddTriangle(index, index + 1, index - 5); // inside: 10 11 5
                            vh.AddTriangle(index, index - 5, index - 6); // inside: 10 5 4
                        }
                        //top
                        vh.AddTriangle(index - 4, index - 1, index + 2); // top: 6 9 12
                        vh.AddTriangle(index + 2, index - 1, index + 5); // top: 12 9 15
                        // //outside
                        if (angle >= 0 && angle <= 180)
                        {
                            vh.AddTriangle(index + 3, index + 4, index - 2); // outside: 13 14 8
                            vh.AddTriangle(index + 3, index - 2, index - 3); // outside: 13 8 7
                        }
                    }
                    if (i == count)
                    {
                        slice.topPolygons.Add(opt);
                        slice.topPolygons.Add(ipt);
                        slice.sidePolygons.Add(opt);
                        slice.sidePolygons.Add(opb);
                        if (!isLeft && endSide)
                        {
                            slice.startOrEndPolygons.Add(ipb);
                            slice.startOrEndPolygons.Add(ipt);
                            slice.startOrEndPolygons.Add(opt);
                            slice.startOrEndPolygons.Add(opb);
                            vh.AddVert(ipb, endSideColor, Vector2.zero); //22
                            vh.AddVert(ipt, endSideColor, Vector2.zero); //23
                            vh.AddVert(opb, endSideColor, Vector2.zero); //24
                            vh.AddVert(opt, endSideColor, Vector2.zero); //25
                            vh.AddTriangle(index + 6, index + 7, index + 9); // endSide: 22 23 25
                            vh.AddTriangle(index + 6, index + 9, index + 8); // endSide: 22 25 24
                        }
                    }
                }
                if (needBorder)
                {
                    UGL.DrawLine(vh, ptList2, serie.itemStyle.borderWidth, serie.itemStyle.borderColor, false);
                }
                ListPool<Vector3>.Release(ptList2);
            }
            if (needBorder)
            {
                UGL.DrawLine(vh, pbList, serie.itemStyle.borderWidth, serie.itemStyle.borderColor, false);
                UGL.DrawLine(vh, ptList, serie.itemStyle.borderWidth, serie.itemStyle.borderColor, false);
            }
            ListPool<Vector3>.Release(ptList);
            ListPool<Vector3>.Release(pbList);
        }

        private bool IsAnyPieClickOffset()
        {
            foreach (var serie in chart.series)
            {
                if (serie is Pie && serie.pieClickOffset)
                    return true;
            }
            return false;
        }

        private bool IsAnyPieDataHighlight()
        {
            foreach (var serie in chart.series)
            {
                if (serie is Pie)
                {
                    foreach (var serieData in serie.data)
                    {
                        if (serieData.context.highlight)
                            return true;
                    }
                }
            }
            return false;
        }

        private void DrawPieLabelLine(VertexHelper vh, Serie serie)
        {
            foreach (var serieData in serie.data)
            {
                var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
                if (SerieLabelHelper.CanShowLabel(serie, serieData, serieLabel, 1))
                {
                    var color = (Color)chart.GetItemColor(serie, serieData);
                    DrawPieLabelLine(vh, serie, serieData, color);
                }
            }
        }

        private void DrawPieLabelLine(VertexHelper vh, Serie serie, SerieData serieData, Color32 defaltColor)
        {
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            if (serieLabel != null && serieLabel.show &&
                labelLine != null && labelLine.show)
            {
                var color = ChartHelper.IsClearColor(labelLine.lineColor) ?
                    ChartHelper.GetHighlightColor(defaltColor, 0.9f) :
                    labelLine.lineColor;
                switch (labelLine.lineType)
                {
                    case LabelLine.LineType.BrokenLine:
                        UGL.DrawLine(vh, serieData.context.labelLinePosition, serieData.context.labelLinePosition2,
                            serieData.context.labelPosition, labelLine.lineWidth, color);
                        break;
                    case LabelLine.LineType.Curves:
                        if (serieData.context.labelLinePosition2 == serieData.context.labelPosition)
                        {
                            UGL.DrawCurves(vh, serieData.context.labelLinePosition, serieData.context.labelPosition,
                            serieData.context.labelLinePosition, (serieData.context.labelLinePosition + serieData.context.labelPosition) * 0.6f,
                            labelLine.lineWidth, color, chart.settings.lineSmoothness);
                        }
                        else
                        {
                            UGL.DrawCurves(vh, serieData.context.labelLinePosition, serieData.context.labelPosition,
                            serieData.context.labelLinePosition, serieData.context.labelLinePosition2,
                            labelLine.lineWidth, color, chart.settings.lineSmoothness);
                        }
                        break;
                    case LabelLine.LineType.HorizontalLine:
                        UGL.DrawLine(vh, serieData.context.labelLinePosition, serieData.context.labelPosition, labelLine.lineWidth, color);
                        break;
                }
                if (labelLine.startSymbol != null && labelLine.startSymbol.show)
                {
                    DrawSymbol(vh, labelLine.startSymbol, serieData.context.labelLinePosition, color);
                }
                if (labelLine.endSymbol != null && labelLine.endSymbol.show)
                {
                    DrawSymbol(vh, labelLine.endSymbol, serieData.context.labelPosition, color);
                }
            }
        }

        private void DrawSymbol(VertexHelper vh, SymbolStyle symbol, Vector3 pos, Color32 defaultColor)
        {
            var color = symbol.GetColor(defaultColor);
            chart.DrawSymbol(vh, symbol.type, symbol.size, 1, pos,
                color, color, ColorUtil.clearColor32, color, symbol.gap, null, symbol.size2);
        }

        private int GetPiePosIndex(Pie3D serie, Vector2 local)
        {
            var b = serie.context.outsideRadius;
            var a = ChartHelper.GetActualValue(serie.pie3dStyle.ellipseMinorAxis, b);

            var center1 = serie.context.center;
            var centerTop = new Vector2(center1.x, center1.y + serie.pie3dStyle.pieMaxHeight);

            if (local.x < center1.x - b || local.x > center1.x + b ||
                local.y < center1.y - a || local.y > centerTop.y + a)
                return -1;

            var isInBottomEllipse = IsInEllipse(local, center1, b, a);
            var inInTopEllipse = IsInEllipse(local, centerTop, b, a);
            if (!isInBottomEllipse &&
                !inInTopEllipse &&
                (local.x < center1.x - b || local.x > center1.x + b || local.y < center1.y || local.y > centerTop.y))
                return -1;
            var currAngle = 0f;
            for (int i = m_Slices.Count - 1; i >= 0; i--)
            {
                var slice = m_Slices[i];
                if (slice.IsInPolygon(local))
                    return slice.serieDataIndex;
            }
            if (serie.pie3dStyle.pieAutoHeight)
            {
                for (int i = m_Slices.Count - 1; i >= 0; i--)
                {
                    var slice = m_Slices[i];
                    var center = new Vector3(center1.x, center1.y + slice.hig);
                    if (IsInEllipse(local, center, b, a) && slice.Contains(GetPointerAngle(local, center)))
                        return slice.serieDataIndex;
                }
            }
            else
            {
                currAngle = GetPointerAngle(local, centerTop);
                for (int i = m_Slices.Count - 1; i >= 0; i--)
                {
                    var slice = m_Slices[i];
                    if (inInTopEllipse && slice.Contains(currAngle))
                        return slice.serieDataIndex;
                }
            }
            currAngle = GetPointerAngle(local, center1);
            for (int i = m_Slices.Count - 1; i >= 0; i--)
            {
                var slice = m_Slices[i];
                if (isInBottomEllipse && slice.Contains(currAngle))
                    return slice.serieDataIndex;
            }
            return -1;
        }

        private static float GetPointerAngle(Vector2 local, Vector3 center)
        {
            var x = local.x - center.x;
            var y = local.y - center.y;
            var currAngle = Mathf.Atan2(y, x);
            if (currAngle < 0)
                currAngle += 2 * Mathf.PI;
            var currDegree = 360 - currAngle * 180 / Mathf.PI;
            return currDegree;
        }

        private bool IsInEllipse(Vector2 pos, Vector3 center, float w, float h)
        {
            var v = Mathf.Pow(center.x - pos.x, 2) / Mathf.Pow(w, 2) + Mathf.Pow(center.y - pos.y, 2) / Mathf.Pow(h, 2);
            return v < 1;
        }

        private bool IsInPieSlice(float x, float y, Vector3 center, float a, float b, float angle, float angleDegree, Pie3DSlice slice)
        {
            if (angleDegree >= slice.startAngle && angleDegree <= slice.endAngle)
            {
                var r = Mathf.Sqrt(y * y + x * x);
                var a2 = a * a;
                var b2 = b * b;
                var cosFi = Mathf.Cos(angle);
                var sinFi = Mathf.Sin(angle);
                var ellipseRadius = (b * a) / Mathf.Sqrt(b2 * cosFi * cosFi + a2 * sinFi * sinFi);

                if (ellipseRadius >= r)
                {
                    return true;
                }
            }
            return false;
        }

        private float GetStartAngle(Serie serie)
        {
            return serie.clockwise ? serie.startAngle : 360 - serie.startAngle;
        }

        private float GetToAngle(Serie serie, float angle)
        {
            var toAngle = angle + serie.startAngle;
            if (!serie.clockwise)
            {
                toAngle = 360 - angle - serie.startAngle;
            }
            if (!serie.animation.IsFinish())
            {
                var currAngle = serie.animation.GetCurrDetail();
                if (serie.clockwise)
                {
                    toAngle = toAngle > currAngle ? currAngle : toAngle;
                }
                else
                {
                    toAngle = toAngle < 360 - currAngle ? 360 - currAngle : toAngle;
                }
            }
            return toAngle;
        }

        public static void UpdatePieLabelPosition(Serie serie, SerieData serieData)
        {
            if (serieData.labelObject == null) return;
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            if (serieLabel == null) return;
            var currAngle = serieData.context.halfAngle;
            var labelLine = SerieHelper.GetSerieLabelLine(serie, serieData);
            var isRight = currAngle <= 90 || currAngle >= 270;
            switch (serieLabel.position)
            {
                case LabelStyle.Position.Center:
                    UpdateLabelPosition(serie, serieData, labelLine, serie.context.center, isRight);
                    break;
                case LabelStyle.Position.Outside:
                case LabelStyle.Position.Top:
                case LabelStyle.Position.End:
                    UpdateLabelPosition(serie, serieData, labelLine, serieData.context.position, isRight);
                    break;
                default:
                    UpdateLabelPosition(serie, serieData, labelLine, serieData.context.areaCenter, isRight);
                    break;
            }
        }

        private static void UpdateLabelPosition(Serie serie, SerieData serieData, LabelLine labelLine, Vector3 startPosition, bool isRight)
        {
            serieData.context.labelLinePosition = startPosition;
            if (labelLine == null || !labelLine.show)
            {
                serieData.context.labelPosition = startPosition;
                return;
            }
            var dire = isRight ? Vector3.right : Vector3.left;
            var rad = Mathf.Deg2Rad * (isRight ? labelLine.lineAngle : 180 - labelLine.lineAngle);
            var lineLength1 = ChartHelper.GetActualValue(labelLine.lineLength1, serie.context.outsideRadius);
            var lineLength2 = ChartHelper.GetActualValue(labelLine.lineLength2, serie.context.outsideRadius);
            var pos1 = startPosition;
            var pos2 = pos1 + new Vector3(Mathf.Cos(rad) * lineLength1, Mathf.Sin(rad) * lineLength1);
            var pos5 = labelLine.lineType == LabelLine.LineType.HorizontalLine
                ? pos1 + dire * (lineLength1 + lineLength2) + labelLine.GetEndSymbolOffset()
                : pos2 + dire * lineLength2 + labelLine.GetEndSymbolOffset();
            if (labelLine.lineEndX != 0)
            {
                pos5.x = serie.context.center.x + (isRight ? Mathf.Abs(labelLine.lineEndX) : -Mathf.Abs(labelLine.lineEndX));
            }
            serieData.context.labelLinePosition2 = pos2;
            serieData.context.labelPosition = pos5;
        }

        public static void AvoidLabelOverlap(Serie serie, ComponentTheme theme)
        {
            if (!serie.avoidLabelOverlap) return;
            var data = serie.data;
            if (data.Count == 0) return;
            var lastCheckPos = Vector3.zero;
            var m1 = -1;
            var m2 = -1;
            for (int n = 0; n < data.Count; n++)
            {
                var serieData = data[n];
                if (serieData.context.labelPosition.x == 0) continue;
                if (serieData.context.labelPosition.x < serie.context.center.x)
                {
                    if (m1 < 0) m1 = n;
                }
                else if (m1 >= 0)
                {
                    m2 = n;
                    break;
                }
            }
            for (int n = m1 - 1; n >= 0; n--)
                CheckSerieDataLabel(serie, data[n], ref lastCheckPos);
            if (m2 > 0)
            {
                for (int n = data.Count - 1; n >= m2; n--)
                    CheckSerieDataLabel(serie, data[n], ref lastCheckPos);
            }
            if (m1 >= 0)
            {
                lastCheckPos = Vector3.zero;
                var size = m2 >= 0 ? m2 : data.Count;
                for (int n = m1; n < size; n++)
                {
                    CheckSerieDataLabel(serie, data[n], ref lastCheckPos);
                }
            }
        }

        private static void CheckSerieDataLabel(Serie serie, SerieData serieData, ref Vector3 lastCheckPos)
        {
            if (serieData.labelObject == null) return;
            if (!serieData.context.canShowLabel)
            {
                serieData.SetLabelActive(false);
                return;
            }
            if (!serieData.show) return;
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            var isOutside = serieLabel.position == LabelStyle.Position.Outside ||
                serieLabel.position == LabelStyle.Position.Default;
            if (!serieLabel.show) return;
            if (!isOutside) return;
            var fontSize = serieData.labelObject.GetHeight();
            if (lastCheckPos == Vector3.zero)
            {
                lastCheckPos = serieData.context.labelPosition;
            }
            else if (serieData.context.labelPosition.x != 0)
            {
                if (serieData.context.labelPosition.y - lastCheckPos.y < fontSize)
                {
                    var y1 = lastCheckPos.y + fontSize;
                    var newPos = new Vector3(serieData.context.labelPosition.x, y1);
                    serieData.context.labelPosition = newPos;
                }
                lastCheckPos = serieData.context.labelPosition;
            }

        }
    }
}