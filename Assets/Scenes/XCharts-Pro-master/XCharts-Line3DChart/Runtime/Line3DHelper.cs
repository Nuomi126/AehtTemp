using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using XUGL;

namespace XCharts.Runtime.Line3Ds
{
    public static class Line3DHelper
    {
        private static List<Vector3> s_CurvesPosList = new List<Vector3>();

        public static void DrawSerieLine(VertexHelper vh, ThemeStyle theme, Serie serie, VisualMap visualMap,
            GridCoord3D grid, Axis axis, Axis relativedAxis, float lineWidth)
        {
            if (!serie.lineStyle.show || serie.lineStyle.type == LineStyle.Type.None)
                return;

            var datas = serie.context.drawPoints;

            var dataCount = datas.Count;
            if (dataCount < 2)
                return;

            var ltp = Vector3.zero;
            var lbp = Vector3.zero;
            var ntp = Vector3.zero;
            var nbp = Vector3.zero;
            var itp = Vector3.zero;
            var ibp = Vector3.zero;
            var clp = Vector3.zero;
            var crp = Vector3.zero;

            var isBreak = false;
            var isY = axis is YAxis;
            var isVisualMapGradient = VisualMapHelper.IsNeedLineGradient(visualMap);
            var isLineStyleGradient = serie.lineStyle.IsNeedGradient();
            var lineColor = SerieHelper.GetLineColor(serie, null, theme, serie.context.colorIndex);

            var lastDataIsIgnore = datas[0].isIgnoreBreak;
            var firstInGridPointIndex = serie.clip ? -1 : 1;
            var segmentCount = 0;
            var dashLength = serie.lineStyle.dashLength;
            var gapLength = serie.lineStyle.gapLength;
            var dotLength = serie.lineStyle.dotLength;
            for (int i = 1; i < dataCount; i++)
            {
                var cdata = datas[i];
                var ldata = datas[i - 1];
                var xValue = cdata.xValue;
                var yValue = cdata.yValue;
                var zValue = cdata.zValue;
                var isIgnore = cdata.isIgnoreBreak;
                var cp = cdata.position;
                var lp = ldata.position;

                var ndata = i == dataCount - 1 ? cdata : datas[i + 1];
                var np = ndata.position;
                if (serie.animation.CheckDetailBreak(cp, isY))
                {
                    isBreak = true;
                    var ip = Vector3.zero;
                    var progress = serie.animation.GetCurrDetail();
                    var rate = 0f;
                    if (AnimationStyleHelper.GetAnimationPosition(serie.animation, isY, lp, cp, progress, ref ip, ref rate))
                    {
                        cp = np = ip;
                        xValue = ldata.xValue + rate * (xValue - ldata.xValue);
                        yValue = ldata.yValue + rate * (yValue - ldata.yValue);
                        zValue = ldata.zValue + rate * (zValue - ldata.zValue);
                    }
                }
                serie.context.lineEndPostion = cp;
                serie.context.lineEndValueX = xValue;
                serie.context.lineEndValueY = yValue;
                serie.context.lineEndValueZ = zValue;
                var handled = false;
                var isClip = false;
                if (serie.clip)
                {
                    if (!grid.Contains(cp))
                        isClip = true;
                    else if (firstInGridPointIndex <= 0)
                        firstInGridPointIndex = i;
                    if (isClip) isIgnore = true;
                }
                if (serie.lineStyle.type == LineStyle.Type.None)
                {
                    handled = true;
                    break;
                }
                {
                    segmentCount++;
                    var index = 0f;
                    switch (serie.lineStyle.type)
                    {
                        case LineStyle.Type.Dashed:
                            index = segmentCount % (dashLength + gapLength);
                            if (index >= dashLength)
                                isIgnore = true;
                            break;
                        case LineStyle.Type.Dotted:
                            index = segmentCount % (dotLength + gapLength);
                            if (index >= dotLength)
                                isIgnore = true;
                            break;
                        case LineStyle.Type.DashDot:
                            index = segmentCount % (dashLength + dotLength + 2 * gapLength);
                            if (index >= dashLength && index < dashLength + gapLength)
                                isIgnore = true;
                            else if (index >= dashLength + gapLength + dotLength)
                                isIgnore = true;
                            break;
                        case LineStyle.Type.DashDotDot:
                            index = segmentCount % (dashLength + 2 * dotLength + 3 * gapLength);
                            if (index >= dashLength && index < dashLength + gapLength)
                                isIgnore = true;
                            else if (index >= dashLength + gapLength + dotLength && index < dashLength + dotLength + 2 * gapLength)
                                isIgnore = true;
                            else if (index >= dashLength + 2 * gapLength + 2 * dotLength)
                                isIgnore = true;
                            break;
                    }
                }

                if (handled)
                {
                    lastDataIsIgnore = isIgnore;
                    if (isBreak)
                        break;
                    else
                        continue;
                }
                bool bitp = true, bibp = true;
                UGLHelper.GetLinePoints(lp, cp, np, lineWidth,
                    ref ltp, ref lbp,
                    ref ntp, ref nbp,
                    ref itp, ref ibp,
                    ref clp, ref crp,
                    ref bitp, ref bibp, i);

                if (i == 1)
                {
                    if (isClip) lastDataIsIgnore = true;
                    AddLineVertToVertexHelper(vh, ltp, lbp, lineColor, isVisualMapGradient, isLineStyleGradient,
                        visualMap, serie.lineStyle, false, lastDataIsIgnore, isIgnore, cdata);
                    if (dataCount == 2 || isBreak)
                    {
                        AddLineVertToVertexHelper(vh, clp, crp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                        serie.context.lineEndPostion = cp;
                        serie.context.lineEndValueZ = 0;
                        break;
                    }
                }

                if (bitp == bibp)
                {
                    if (bitp)
                        AddLineVertToVertexHelper(vh, itp, ibp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                    else
                    {
                        AddLineVertToVertexHelper(vh, ltp, clp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                        AddLineVertToVertexHelper(vh, ltp, crp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                    }
                }
                else
                {
                    if (bitp)
                    {
                        AddLineVertToVertexHelper(vh, itp, clp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                        AddLineVertToVertexHelper(vh, itp, crp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                    }
                    else if (bibp)
                    {
                        AddLineVertToVertexHelper(vh, clp, ibp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                        AddLineVertToVertexHelper(vh, crp, ibp, lineColor, isVisualMapGradient, isLineStyleGradient,
                            visualMap, serie.lineStyle, true, lastDataIsIgnore, isIgnore, cdata);
                    }
                }
                lastDataIsIgnore = isIgnore;
                if (isBreak)
                    break;
            }
        }

        private static void AddLineVertToVertexHelper(VertexHelper vh, Vector3 tp, Vector3 bp,
            Color32 lineColor, bool visualMapGradient, bool lineStyleGradient, VisualMap visualMap,
            LineStyle lineStyle, bool needTriangle, bool lastIgnore, bool ignore, PointInfo pointInfo)
        {
            if (lastIgnore && needTriangle)
                UGL.AddVertToVertexHelper(vh, tp, bp, ColorUtil.clearColor32, true);

            if (visualMapGradient)
            {
                var color1 = visualMap.GetColor(pointInfo.xValue, pointInfo.yValue, pointInfo.zValue);
                UGL.AddVertToVertexHelper(vh, tp, bp, color1, color1, needTriangle);
            }
            // else if (lineStyleGradient)
            // {
            //     var color1 = VisualMapHelper.GetLineStyleGradientColor(lineStyle, tp, grid, axis, lineColor);
            //     var color2 = VisualMapHelper.GetLineStyleGradientColor(lineStyle, bp, grid, axis, lineColor);
            //     UGL.AddVertToVertexHelper(vh, tp, bp, color1, color2, needTriangle);
            // }
            else
            {
                UGL.AddVertToVertexHelper(vh, tp, bp, lineColor, needTriangle);
            }
            if (lastIgnore && !needTriangle)
            {
                UGL.AddVertToVertexHelper(vh, tp, bp, ColorUtil.clearColor32, false);
            }
            if (ignore && needTriangle)
            {
                UGL.AddVertToVertexHelper(vh, tp, bp, ColorUtil.clearColor32, false);
            }
        }

        internal static void UpdateSerieDrawPoints(Serie serie, Settings setting, ThemeStyle theme, VisualMap visualMap,
            float lineWidth, bool isY, GridCoord3D grid)
        {
            serie.context.drawPoints.Clear();
            var last = Vector3.zero;
            switch (serie.lineType)
            {
                case LineType.Smooth:
                    UpdateSmoothLineDrawPoints(serie, setting, isY);
                    break;
                case LineType.StepStart:
                case LineType.StepMiddle:
                case LineType.StepEnd:
                    UpdateStepLineDrawPoints(serie, setting, theme, isY, lineWidth);
                    break;
                default:
                    UpdateNormalLineDrawPoints(serie, setting, visualMap, grid);
                    break;
            }
        }

        private static void UpdateNormalLineDrawPoints(Serie serie, Settings setting, VisualMap visualMap, GridCoord3D grid)
        {
            var isVisualMapGradient = VisualMapHelper.IsNeedGradient(visualMap);
            if (isVisualMapGradient || serie.clip || (serie.lineStyle.IsNotSolidLine()))
            {
                var dataPoints = serie.context.dataPoints;
                var dataIndexs = serie.context.dataIndexs;
                if (dataPoints.Count > 1)
                {
                    var sp = dataPoints[0];
                    var spIndex = dataIndexs[0];
                    var ip = Vector3.zero;
                    for (int i = 1; i < dataPoints.Count; i++)
                    {
                        var ep = dataPoints[i];
                        var ignore = serie.context.dataIgnores[i];
                        // if (serie.clip && grid.NotAnyIntersect(sp, ep))
                        // {
                        //     sp = ep;
                        //     spIndex = dataIndexs[i];
                        //     continue;
                        // }
                        var dir = (ep - sp).normalized;
                        var dist = Vector3.Distance(sp, ep);
                        var segment = (int)(dist / setting.lineSegmentDistance);
                        var spData = serie.GetSerieData(dataIndexs[spIndex]);
                        var xValue = spData.GetData(0);
                        var yValue = spData.GetData(1);
                        var zValue = spData.GetData(2);
                        serie.context.drawPoints.Add(new PointInfo(sp, ignore, xValue, yValue, zValue));

                        var epData = serie.GetSerieData(dataIndexs[i]);
                        var xValue2 = epData.GetData(0);
                        var yValue2 = epData.GetData(1);
                        var zValue2 = epData.GetData(2);
                        for (int j = 1; j < segment; j++)
                        {
                            var np = sp + dir * dist * j / segment;
                            var xNowValue = xValue + (xValue2 - xValue) * j / segment;
                            var yNowValue = yValue + (yValue2 - yValue) * j / segment;
                            var zNowValue = zValue + (zValue2 - zValue) * j / segment;
                            serie.context.drawPoints.Add(new PointInfo(np, ignore, xNowValue, yNowValue, zNowValue));
                        }
                        sp = ep;
                        spIndex = dataIndexs[i];
                        if (i == dataPoints.Count - 1)
                        {
                            serie.context.drawPoints.Add(new PointInfo(ep, ignore, xValue2, yValue2, zValue2));
                        }
                    }
                }
                else
                {
                    var dataIndex = serie.context.dataIndexs[0];
                    var serieData = serie.GetSerieData(dataIndex);
                    var x = serieData.GetData(0);
                    var y = serieData.GetData(1);
                    var z = serieData.GetData(2);
                    serie.context.drawPoints.Add(new PointInfo(dataPoints[0], serie.context.dataIgnores[0], x, y, z));
                }
            }
            else
            {
                for (int i = 0; i < serie.context.dataPoints.Count; i++)
                {
                    var dataIndex = serie.context.dataIndexs[i];
                    var serieData = serie.GetSerieData(dataIndex);
                    var x = serieData.GetData(0);
                    var y = serieData.GetData(1);
                    var z = serieData.GetData(2);
                    serie.context.drawPoints.Add(new PointInfo(serie.context.dataPoints[i], serie.context.dataIgnores[i], x, y, z));
                }
            }
        }

        private static void UpdateSmoothLineDrawPoints(Serie serie, Settings setting, bool isY)
        {
            var points = serie.context.dataPoints;
            float smoothness = setting.lineSmoothness;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var sp = points[i];
                var ep = points[i + 1];
                var lsp = i > 0 ? points[i - 1] : sp;
                var nep = i < points.Count - 2 ? points[i + 2] : ep;
                var ignore = serie.context.dataIgnores[i];
                if (isY)
                    UGLHelper.GetBezierListVertical(ref s_CurvesPosList, sp, ep, smoothness, setting.lineSmoothStyle);
                else
                    UGLHelper.GetBezierList(ref s_CurvesPosList, sp, ep, lsp, nep, smoothness, setting.lineSmoothStyle, serie.smoothLimit);
                for (int j = 1; j < s_CurvesPosList.Count; j++)
                {
                    serie.context.drawPoints.Add(new PointInfo(s_CurvesPosList[j], ignore));
                }
            }
        }

        private static void UpdateStepLineDrawPoints(Serie serie, Settings setting, ThemeStyle theme, bool isY, float lineWidth)
        {
            var points = serie.context.dataPoints;
            var lp = points[0];
            serie.context.drawPoints.Clear();
            serie.context.drawPoints.Add(new PointInfo(lp, serie.context.dataIgnores[0]));
            for (int i = 1; i < points.Count; i++)
            {
                var cp = points[i];
                var ignore = serie.context.dataIgnores[i];
                if ((isY && Mathf.Abs(lp.x - cp.x) <= lineWidth) ||
                    (!isY && Mathf.Abs(lp.y - cp.y) <= lineWidth))
                {
                    serie.context.drawPoints.Add(new PointInfo(cp, ignore));
                    lp = cp;
                    continue;
                }
                switch (serie.lineType)
                {
                    case LineType.StepStart:
                        serie.context.drawPoints.Add(new PointInfo(isY ?
                            new Vector3(cp.x, lp.y) :
                            new Vector3(lp.x, cp.y), ignore));
                        break;
                    case LineType.StepMiddle:
                        serie.context.drawPoints.Add(new PointInfo(isY ?
                            new Vector3(lp.x, (lp.y + cp.y) / 2) :
                            new Vector3((lp.x + cp.x) / 2, lp.y), ignore));
                        serie.context.drawPoints.Add(new PointInfo(isY ?
                            new Vector3(cp.x, (lp.y + cp.y) / 2) :
                            new Vector3((lp.x + cp.x) / 2, cp.y), ignore));
                        break;
                    case LineType.StepEnd:
                        serie.context.drawPoints.Add(new PointInfo(isY ?
                            new Vector3(lp.x, cp.y) :
                            new Vector3(cp.x, lp.y), ignore));
                        break;
                }
                serie.context.drawPoints.Add(new PointInfo(cp, ignore));
                lp = cp;
            }
        }

        public static void DrawSerieLineArea(VertexHelper vh, Serie serie, Serie lastStackSerie,
            ThemeStyle theme, VisualMap visualMap, bool isY, GridCoord3D grid, XAxis3D xAxis, YAxis3D yAxis, ZAxis3D zAxis)
        {
            Color32 areaColor, areaToColor;
            bool innerFill, toTop;
            if (!SerieHelper.GetAreaColor(out areaColor, out areaToColor, out innerFill, out toTop, serie, null, theme, serie.context.colorIndex))
            {
                return;
            }
            if (innerFill)
            {
                UGL.DrawPolygon(vh, serie.context.dataPoints, areaColor);
                return;
            }
            if (lastStackSerie == null)
            {
                DrawSerieLineNormalArea(vh, serie,
                    areaColor,
                    areaToColor,
                    visualMap,
                    grid, xAxis, yAxis, zAxis,
                    toTop);
            }
            else
            {
                // DrawSerieLineStackArea(vh, serie, lastStackSerie, isY,
                //     gridXY + relativedAxis.context.offset,
                //     gridXY,
                //     gridXY + (isY ? grid.context.width : grid.context.height),
                //     areaColor,
                //     areaToColor,
                //     visualMap,
                //     toTop);
            }
        }

        private static void DrawSerieLineNormalArea(VertexHelper vh, Serie serie,
            Color32 areaColor, Color32 areaToColor, VisualMap visualMap, GridCoord3D grid, XAxis3D xAxis, YAxis3D yAxis, ZAxis3D zAxis, bool toTop)
        {
            var points = serie.context.drawPoints;
            var count = points.Count;
            if (count < 2)
                return;

            var isBreak = false;
            var isVisualMapGradient = VisualMapHelper.IsNeedAreaGradient(visualMap);
            var areaLerp = !ChartHelper.IsValueEqualsColor(areaColor, areaToColor);
            var lastDataIsIgnore = false;
            Vector3 tp, zp;
            for (int i = 0; i < points.Count; i++)
            {
                var pdata = points[i];
                var xValue = pdata.xValue;
                var yValue = pdata.yValue;
                var zValue = pdata.zValue;
                tp = pdata.position;
                if (serie.animation.CheckDetailBreak(tp, false))
                {
                    isBreak = true;
                    tp = serie.context.lineEndPostion;
                    xValue = serie.context.lineEndValueX;
                    yValue = serie.context.lineEndValueY;
                    zValue = serie.context.lineEndValueZ;
                }
                zp = Axis3DHelper.Get3DGridPosition(grid, xAxis, yAxis, xValue, yValue);
                if (serie.clip)
                {
                    grid.Clamp(ref tp);
                }
                var isIgnore = pdata.isIgnoreBreak;
                var color = areaColor;
                var toColor = areaToColor;
                var lerp = areaLerp;


                if (isVisualMapGradient)
                {
                    color = visualMap.GetColor(xValue, yValue, 0, areaColor.a);
                    toColor = visualMap.GetColor(xValue, yValue, zValue, areaToColor.a);
                    lerp = true;
                }

                if (lerp)
                {
                    if (toTop)
                    {
                        var range = zAxis.context.minMaxRange;
                        var color1 = Color32.Lerp(color, toColor, (float)((zValue - zAxis.context.minValue) / range));
                        UGL.AddVertToVertexHelper(vh, tp, zp, color1, color, i > 0);
                    }
                    else
                    {
                        UGL.AddVertToVertexHelper(vh, tp, zp, toColor, color, i > 0);
                    }
                }
                else
                {
                    if (lastDataIsIgnore)
                        UGL.AddVertToVertexHelper(vh, tp, zp, ColorUtil.clearColor32, true);

                    UGL.AddVertToVertexHelper(vh, tp, zp, toColor, color, i > 0);

                    if (isIgnore)
                        UGL.AddVertToVertexHelper(vh, tp, zp, ColorUtil.clearColor32, true);
                }
                lastDataIsIgnore = isIgnore;
                if (isBreak)
                    break;
            }
        }

        private static void DrawSerieLineStackArea(VertexHelper vh, Serie serie, Serie lastStackSerie, bool isY,
            float zero, float min, float max, Color32 color, Color32 toColor, VisualMap visualMap, bool toTop)
        {
            if (lastStackSerie == null)
                return;

            var upPoints = serie.context.drawPoints;
            var downPoints = lastStackSerie.context.drawPoints;
            var upCount = upPoints.Count;
            var downCount = downPoints.Count;

            if (upCount <= 0 || downCount <= 0)
                return;

            var lerp = !ChartHelper.IsValueEqualsColor(color, toColor);
            var ltp = upPoints[0].position;
            var lbp = downPoints[0].position;

            if (lerp)
                UGL.AddVertToVertexHelper(vh, ltp, lbp, toColor, color, false);
            else
                UGL.AddVertToVertexHelper(vh, ltp, lbp, color, false);

            int u = 1, d = 1;
            var isBreakTop = false;
            var isBreakBottom = false;

            while ((u < upCount || d < downCount))
            {
                var tp = u < upCount ? upPoints[u].position : upPoints[upCount - 1].position;
                var bp = d < downCount ? downPoints[d].position : downPoints[downCount - 1].position;

                var tnp = (u + 1) < upCount ? upPoints[u + 1].position : upPoints[upCount - 1].position;
                var bnp = (d + 1) < downCount ? downPoints[d + 1].position : downPoints[downCount - 1].position;

                if (serie.animation.CheckDetailBreak(tp, isY))
                {
                    isBreakTop = true;

                    var progress = serie.animation.GetCurrDetail();
                    var ip = Vector3.zero;

                    if (UGLHelper.GetIntersection(ltp, tp,
                            new Vector3(progress, -10000),
                            new Vector3(progress, 10000), ref ip))
                        tp = ip;
                    else
                        tp = new Vector3(progress, tp.y);
                }
                if (serie.animation.CheckDetailBreak(bp, isY))
                {
                    isBreakBottom = true;

                    var progress = serie.animation.GetCurrDetail();
                    var ip = Vector3.zero;

                    if (UGLHelper.GetIntersection(lbp, bp,
                            new Vector3(progress, -10000),
                            new Vector3(progress, 10000), ref ip))
                        bp = ip;
                    else
                        bp = new Vector3(progress, bp.y);
                }

                if (lerp)
                    UGL.AddVertToVertexHelper(vh, tp, bp, toColor, color, true);
                else
                    UGL.AddVertToVertexHelper(vh, tp, bp, color, true);
                u++;
                d++;
                if (bp.x < tp.x && bnp.x < tp.x)
                    u--;
                if (tp.x < bp.x && tnp.x < bp.x)
                    d--;

                ltp = tp;
                lbp = bp;
                if (isBreakTop && isBreakBottom)
                    break;
            }
        }
    }
}