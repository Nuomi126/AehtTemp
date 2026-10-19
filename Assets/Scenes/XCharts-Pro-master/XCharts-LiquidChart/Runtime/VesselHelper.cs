
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Liquids
{
    internal static class VesselHelper
    {
        public static Color32 GetColor(Vessel vessel, Serie serie, ThemeStyle theme, int colorIndex)
        {
            if (serie != null && vessel.autoColor)
            {
                return SerieHelper.GetItemColor(serie, null, theme, colorIndex, SerieState.Normal, false);
            }
            else
            {
                return vessel.color;
            }
        }

        public static Color32 GetColor(Vessel vessel, Serie serie, Color32 serieColor)
        {
            if (serie != null && vessel.autoColor)
            {
                return serieColor;
            }
            else
            {
                return vessel.color;
            }
        }

        public static Color32 GetBorderColor(Vessel vessel, Serie serie, ThemeStyle theme, int colorIndex)
        {
            if (serie != null && vessel.autoColor)
            {
                return SerieHelper.GetItemColor(serie, null, theme, colorIndex, SerieState.Normal, false);
            }
            else
            {
                return vessel.borderColor;
            }
        }

        public static Color32 GetBorderColor(Vessel vessel, Serie serie, Color32 serieColor)
        {
            if (serie != null && vessel.autoColor)
            {
                return serieColor;
            }
            else
            {
                return vessel.borderColor;
            }
        }
    }
}