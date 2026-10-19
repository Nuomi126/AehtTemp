using UnityEngine;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Liquids
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class LiquidHandler : SerieHandler<Liquid>
    {
        private bool m_UpdateLabelText = false;
        private float m_WaveSpeed;

        public override void Update()
        {
            base.Update();
            if (m_UpdateLabelText)
            {
                m_UpdateLabelText = false;
                RefreshLabelInternal();
            }
        }

        public override void RefreshLabelNextFrame() { }

        public override Color GetSerieDataAutoColor(SerieData serieData)
        {
            Color32 color, toColor;
            GetLiquidColor(serie, false, out color, out toColor);
            return color;
        }

        public override void DrawSerie(VertexHelper vh)
        {
            UpdateRuntimeData();
            DrawVesselBackground(vh);
            DrawLiquid(vh);
            DrawVessel(vh);
        }

        private void UpdateRuntimeData()
        {
            Vessel vessel;
            if (chart.TryGetChartComponent<Vessel>(out vessel, serie.vesselIndex))
            {
                vessel.UpdateRuntimeData(chart);
            }
        }

        private void DrawVesselBackground(VertexHelper vh)
        {
            var vessel = chart.GetChartComponent<Vessel>(serie.vesselIndex);
            if (vessel != null)
            {
                if (vessel.backgroundColor.a != 0)
                {
                    switch (vessel.shape)
                    {
                        case Vessel.Shape.Circle:
                            var cenPos = vessel.context.center;
                            var radius = vessel.context.radius;
                            UGL.DrawCricle(vh, cenPos, vessel.context.innerRadius + vessel.gap, vessel.backgroundColor,
                                chart.settings.cicleSmoothness);
                            UGL.DrawDoughnut(vh, cenPos, vessel.context.innerRadius, vessel.context.innerRadius + vessel.gap,
                                vessel.backgroundColor, Color.clear, chart.settings.cicleSmoothness);
                            break;
                        case Vessel.Shape.Rect:
                            UGL.DrawRoundRectangle(vh, vessel.context.center, vessel.context.width, vessel.context.height,
                                vessel.backgroundColor, vessel.backgroundColor, 0,
                                vessel.cornerRadius, false, chart.settings.cicleSmoothness, false);
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        private void DrawVessel(VertexHelper vh)
        {
            var vessel = chart.GetChartComponent<Vessel>(serie.vesselIndex);
            if (vessel != null && vessel.show)
            {
                switch (vessel.shape)
                {
                    case Vessel.Shape.Circle:
                        DrawCirleVessel(vh, vessel);
                        break;
                    case Vessel.Shape.Rect:
                        DrawRectVessel(vh, vessel);
                        break;
                    default:
                        DrawCirleVessel(vh, vessel);
                        break;
                }
            }
        }

        private void DrawCirleVessel(VertexHelper vh, Vessel vessel)
        {
            var cenPos = vessel.context.center;
            var radius = vessel.context.radius;
            Color32 color, toColor;
            GetLiquidColor(serie, false, out color, out toColor);
            var vesselColor = VesselHelper.GetColor(vessel, serie, color);
            if (vessel.gap != 0)
            {
                UGL.DrawDoughnut(vh, cenPos, vessel.context.innerRadius, vessel.context.innerRadius + vessel.gap,
                    vessel.backgroundColor, Color.clear, chart.settings.cicleSmoothness);
            }
            UGL.DrawDoughnut(vh, cenPos, radius - vessel.shapeWidth, radius, vesselColor, Color.clear,
                chart.settings.cicleSmoothness);
            if (vessel.borderWidth > 0)
            {
                var outsideRadius = vessel.context.innerRadius;
                var insideRadius = outsideRadius - vessel.borderWidth;
                var borderColor = VesselHelper.GetBorderColor(vessel, serie, color);
                UGL.DrawDoughnut(vh, cenPos, insideRadius, outsideRadius, borderColor, Color.clear,
                    chart.settings.cicleSmoothness);
            }
        }

        private void DrawRectVessel(VertexHelper vh, Vessel vessel)
        {
            Color32 color, toColor;
            GetLiquidColor(serie, false, out color, out toColor);
            var vesselColor = VesselHelper.GetColor(vessel, serie, color);
            if (vessel.gap != 0)
            {
                UGL.DrawBorder(vh, vessel.context.center, vessel.context.width,
                    vessel.context.height, vessel.gap, vessel.backgroundColor, 0, vessel.cornerRadius);
            }
            UGL.DrawBorder(vh, vessel.context.center, vessel.context.width, vessel.context.height,
                vessel.shapeWidth, vesselColor, 0, vessel.cornerRadius, false, 1, false, vessel.gap);
            if (vessel.borderWidth > 0)
            {
                var borderColor = VesselHelper.GetBorderColor(vessel, serie, color);
                UGL.DrawBorder(vh, vessel.context.center, vessel.context.width, vessel.context.height,
                    vessel.borderWidth, borderColor, 0, vessel.cornerRadius, false, 1, false);
            }
        }

        private void DrawLiquid(VertexHelper vh)
        {
            if (!serie.show) return;
            if (serie.animation.HasFadeOut()) return;
            var vessel = chart.GetChartComponent<Vessel>(serie.vesselIndex);
            if (vessel == null) return;
            switch (vessel.shape)
            {
                case Vessel.Shape.Circle:
                    DrawCirleLiquid(vh, vessel);
                    break;
                case Vessel.Shape.Rect:
                    DrawRectLiquid(vh, vessel);
                    break;
                default:
                    DrawCirleLiquid(vh, vessel);
                    break;
            }
        }

        private void DrawCirleLiquid(VertexHelper vh, Vessel vessel)
        {
            var cenPos = vessel.context.center;
            var radius = vessel.context.innerRadius;
            var serieData = serie.GetSerieData(0);
            if (serieData == null) return;
            var value = serieData.GetCurrData(1, serie.animation);
            if (serie.context.checkValue != value)
            {
                serie.context.checkValue = value;
                m_UpdateLabelText = true;
            }
            if (serieData.context.labelPosition != cenPos)
            {
                serieData.context.labelPosition = cenPos;
                m_UpdateLabelText = true;
            }
            if (value <= 0) return;
            var realHig = (float)((value - serie.min) / (serie.max - serie.min) * radius * 2);
            serie.animation.InitProgress(0, realHig);

            var hig = serie.animation.IsFinish() ? realHig : serie.animation.GetCurrDetail();
            var a = Mathf.Abs(radius - hig + (hig > radius ? serie.waveHeight : -serie.waveHeight));
            var diff = Mathf.Sqrt(radius * radius - Mathf.Pow(a, 2));

            Color32 color, toColor;
            GetLiquidColor(serie, true, out color, out toColor);

            var isNeedGradient = !ChartHelper.IsValueEqualsColor(color, toColor);
            var isFull = hig >= 2 * radius;
            if (hig >= 2 * radius) hig = 2 * radius;
            if (isFull && !isNeedGradient)
            {
                UGL.DrawCricle(vh, cenPos, radius, toColor, chart.settings.cicleSmoothness);
            }
            else
            {
                var startY = cenPos.y - radius + hig;
                var waveStartPos = new Vector3(cenPos.x - diff, startY);
                var waveEndPos = new Vector3(cenPos.x + diff, startY);
                var startX = hig > radius ? cenPos.x - radius : waveStartPos.x;
                var endX = hig > radius ? cenPos.x + radius : waveEndPos.x;

                var step = vessel.smoothness;
                if (step < 0.5f) step = 0.5f;
                var lup = hig > radius ? new Vector3(cenPos.x - radius, cenPos.y) : waveStartPos;
                var ldp = lup;
                var nup = Vector3.zero;
                var ndp = Vector3.zero;
                var angle = 0f;
                m_WaveSpeed += serie.waveSpeed * Time.deltaTime;
                var isStarted = false;
                var isEnded = false;
                var waveHeight = isFull ? 0 : serie.waveHeight;
                while (startX < endX)
                {
                    startX += step;
                    if (startX > endX) startX = endX;
                    if (startX > waveStartPos.x && !isStarted)
                    {
                        startX = waveStartPos.x;
                        isStarted = true;
                    }
                    if (startX > waveEndPos.x && !isEnded)
                    {
                        startX = waveEndPos.x;
                        isEnded = true;
                    }
                    var py = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(Mathf.Abs(cenPos.x - startX), 2));
                    if (startX < waveStartPos.x || startX > waveEndPos.x)
                    {
                        nup = new Vector3(startX, cenPos.y + py);
                    }
                    else
                    {
                        var py2 = waveHeight * Mathf.Sin(1 / serie.waveLength * angle + m_WaveSpeed + serie.waveOffset);
                        var nupY = waveStartPos.y + py2;
                        if (nupY > cenPos.y + py) nupY = cenPos.y + py;
                        else if (nupY < cenPos.y - py) nupY = cenPos.y - py;
                        nup = new Vector3(startX, nupY);
                        angle += step;
                    }
                    ndp = new Vector3(startX, cenPos.y - py);
                    if (!ChartHelper.IsValueEqualsColor(color, toColor))
                    {
                        var colorMin = cenPos.y - radius;
                        var colorMax = startY + serie.waveHeight;
                        var tcolor1 = Color32.Lerp(color, toColor, 1 - (lup.y - colorMin) / (colorMax - colorMin));
                        var tcolor2 = Color32.Lerp(color, toColor, 1 - (ldp.y - colorMin) / (colorMax - colorMin));
                        UGL.DrawQuadrilateral(vh, lup, nup, ndp, ldp, tcolor1, tcolor2);
                    }
                    else
                    {
                        UGL.DrawQuadrilateral(vh, lup, nup, ndp, ldp, color);
                    }
                    lup = nup;
                    ldp = ndp;
                }
            }

            if (serie.waveSpeed != 0 && Application.isPlaying && !isFull)
            {
                chart.RefreshPainter(serie);
            }
            if (!serie.animation.IsFinish())
            {
                serie.animation.CheckProgress(realHig);
                chart.RefreshPainter(serie);
            }
        }

        private void DrawRectLiquid(VertexHelper vh, Vessel vessel)
        {
            var cenPos = vessel.context.center;
            var serieData = serie.GetSerieData(0);
            if (serieData == null) return;
            var value = serieData.GetCurrData(1, serie.animation);
            if (serie.context.checkValue != value)
            {
                serie.context.checkValue = value;
                m_UpdateLabelText = true;
            }
            if (serieData.context.labelPosition != cenPos)
            {
                serieData.context.labelPosition = cenPos;
                m_UpdateLabelText = true;
            }
            if (value <= 0) return;
            var realHig = (value - serie.min) / (serie.max - serie.min) * vessel.context.height;
            serie.animation.InitProgress(0, (float)realHig);
            var hig = serie.animation.IsFinish() ? realHig : serie.animation.GetCurrDetail();
            Color32 color, toColor;
            GetLiquidColor(serie, true, out color, out toColor);
            var isNeedGradient = !ChartHelper.IsValueEqualsColor(color, toColor);
            var isFull = hig >= vessel.context.height;

            float brLt = 0f, brRt = 0f, brRb = 0f, brLb = 0f;
            var needRound = false;
            var roundLt = Vector3.zero;
            var roundRt = Vector3.zero;
            var roundRb = Vector3.zero;
            var roundLb = Vector3.zero;

            UGL.InitCornerRadius(vessel.cornerRadius, vessel.context.width, vessel.context.height,
                false, false, ref brLt, ref brRt, ref brRb, ref brLb, ref needRound);
            if (needRound)
            {
                var center = vessel.context.center;
                var halfWid = vessel.context.width * 0.5f;
                var halfHig = vessel.context.height * 0.5f;
                roundLt = new Vector3(center.x - halfWid + brLt, center.y + halfHig - brLt);
                roundRt = new Vector3(center.x + halfWid - brRt, center.y + halfHig - brRt);
                roundRb = new Vector3(center.x + halfWid - brRb, center.y - halfHig + brRb);
                roundLb = new Vector3(center.x - halfWid + brLb, center.y - halfHig + brLb);
            }

            if (hig >= vessel.context.height)
                hig = vessel.context.height;

            if (isFull && !isNeedGradient && !needRound)
            {
                UGL.DrawRectangle(vh, cenPos, vessel.context.width / 2, vessel.context.height / 2, toColor);
            }
            else
            {
                var startY = (float)(cenPos.y - vessel.context.height / 2 + hig);
                var waveStartPos = new Vector3(cenPos.x - vessel.context.width / 2, startY);
                var waveEndPos = new Vector3(cenPos.x + vessel.context.width / 2, startY);
                var startX = waveStartPos.x;
                var endX = waveEndPos.x;

                var step = vessel.smoothness;
                if (step < 0.5f) step = 0.5f;
                var lup = waveStartPos;
                var ldp = new Vector3(startX, vessel.context.center.y - vessel.context.height / 2);
                var nup = Vector3.zero;
                var ndp = Vector3.zero;
                var angle = 0f;
                var isStarted = false;
                var isEnded = false;
                var waveHeight = isFull ? 0 : serie.waveHeight;
                var deltaTime = serie.animation.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                m_WaveSpeed += serie.waveSpeed * deltaTime;
                while (startX < endX)
                {
                    startX += step;
                    if (startX > endX) startX = endX;
                    if (startX > waveStartPos.x && !isStarted)
                    {
                        startX = waveStartPos.x;
                        isStarted = true;
                    }
                    if (startX > waveEndPos.x && !isEnded)
                    {
                        startX = waveEndPos.x;
                        isEnded = true;
                    }
                    var py2 = waveHeight * Mathf.Sin(1 / serie.waveLength * angle + m_WaveSpeed + serie.waveOffset);
                    var nupY = waveStartPos.y + py2;
                    nup = new Vector3(startX, nupY);
                    angle += step;

                    ndp = new Vector3(startX, cenPos.y - vessel.context.height / 2);
                    if (nup.y > cenPos.y + vessel.context.height / 2)
                    {
                        nup.y = cenPos.y + vessel.context.height / 2;
                    }
                    if (nup.y < cenPos.y - vessel.context.height / 2)
                    {
                        nup.y = cenPos.y - vessel.context.height / 2;
                    }
                    if (needRound)
                    {
                        var currRoundHig = 0f;
                        if (brLb > 0)
                        {
                            currRoundHig = Mathf.Sqrt(Mathf.Pow(brLb, 2) - Mathf.Pow(Mathf.Abs(roundLb.x - startX), 2));
                            if (startX < roundLb.x && ndp.y < roundLb.y - currRoundHig)
                                ndp.y = roundLb.y - currRoundHig;
                            if (ndp.y > nup.y)
                            {
                                lup = nup;
                                ldp = ndp;
                                continue;
                            }
                        }
                        if (brLt > 0)
                        {
                            currRoundHig = Mathf.Sqrt(Mathf.Pow(brLt, 2) - Mathf.Pow(Mathf.Abs(roundLt.x - startX), 2));
                            if (startX < roundLt.x && nup.y > roundLt.y + currRoundHig)
                                nup.y = roundLt.y + currRoundHig;
                        }
                        if (brRt > 0)
                        {
                            currRoundHig = Mathf.Sqrt(Mathf.Pow(brRt, 2) - Mathf.Pow(Mathf.Abs(roundRt.x - startX), 2));
                            if (startX > roundRt.x && nup.y > roundRt.y + currRoundHig)
                                nup.y = roundRt.y + currRoundHig;
                        }
                        if (brRb > 0)
                        {
                            currRoundHig = Mathf.Sqrt(Mathf.Pow(brRb, 2) - Mathf.Pow(Mathf.Abs(roundRb.x - startX), 2));
                            if (startX > roundRb.x && ndp.y < roundRb.y - currRoundHig)
                                ndp.y = roundRb.y - currRoundHig;
                            if (nup.y <= ndp.y)
                                break;
                        }
                    }
                    if (!ChartHelper.IsValueEqualsColor(color, toColor))
                    {
                        var colorMin = cenPos.y - vessel.context.height;
                        var colorMax = startY + serie.waveHeight;
                        var tcolor1 = Color32.Lerp(color, toColor, 1 - (lup.y - colorMin) / (colorMax - colorMin));
                        var tcolor2 = Color32.Lerp(color, toColor, 1 - (ldp.y - colorMin) / (colorMax - colorMin));
                        UGL.DrawQuadrilateral(vh, lup, nup, ndp, ldp, tcolor1, tcolor2);
                    }
                    else
                    {
                        UGL.DrawQuadrilateral(vh, lup, nup, ndp, ldp, color);
                    }
                    lup = nup;
                    ldp = ndp;
                }
            }
            if (serie.waveSpeed != 0 && Application.isPlaying && !isFull)
            {
                chart.RefreshPainter(serie);
            }
            if (!serie.animation.IsFinish())
            {
                serie.animation.CheckProgress(realHig);
                chart.RefreshPainter(serie);
            }
        }

        private void GetLiquidColor(Liquid serie, bool opacity, out Color32 color, out Color32 toColor)
        {
            var visualMap = chart.GetVisualMapOfSerie(serie);
            var serieData = serie.GetSerieData(0); ;
            if (visualMap != null && visualMap.show)
            {
                if (serieData != null)
                {
                    var itemStyle = SerieHelper.GetItemStyle(serie, serieData);
                    color = visualMap.GetColor(serieData.GetCurrData(1, serie.animation));
                    if (opacity)
                        ChartHelper.SetColorOpacity(ref color, itemStyle.opacity);
                    toColor = color;
                    return;
                }
            }
            SerieHelper.GetItemColor(out color, out toColor, serie, serieData, chart.theme, serie.context.colorIndex, SerieState.Normal, opacity);
        }
    }
}