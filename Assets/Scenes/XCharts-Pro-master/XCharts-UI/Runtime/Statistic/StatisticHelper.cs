using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    public static class StatisticHelper
    {
        public static string GetTitleContent(UIStatistic statistic, double value)
        {
            if (string.IsNullOrEmpty(statistic.titleLabelStyle.formatter))
                return statistic.title == null ? "" : statistic.title;
            else
            {
                var content = statistic.titleLabelStyle.formatter;
                var numericFormatter = GetNumericFormatter(statistic, statistic.titleLabelStyle);
                FormatterHelper.ReplaceAxisLabelContent(ref content, numericFormatter, value);
                content = content.Replace("{title}", statistic.title == null ? "" : statistic.title);
                return content;
            }
        }

        public static string GetValueContent(UIStatistic statistic, double value)
        {
            var numericFormatter = GetNumericFormatter(statistic, statistic.valueLabelStyle);
            if (statistic.type == UIStatistic.Type.Countdown)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(statistic.context.value);
                if (string.IsNullOrEmpty(statistic.valueLabelStyle.formatter))
                {
                    return string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else
                {
                    var content = statistic.valueLabelStyle.formatter;
                    FormatterHelper.ReplaceAxisLabelContent(ref content, numericFormatter, value);
                    content = content.Replace("{dd}", timeSpan.Days.ToString("D2"));
                    content = content.Replace("{hh}", timeSpan.Hours.ToString("D2"));
                    content = content.Replace("{mm}", timeSpan.Minutes.ToString("D2"));
                    content = content.Replace("{ss}", timeSpan.Seconds.ToString("D2"));
                    content = content.Replace("{fff}", timeSpan.Milliseconds.ToString("D3"));
                    content = content.Replace("{d}", timeSpan.Days.ToString());
                    content = content.Replace("{h}", timeSpan.Hours.ToString());
                    content = content.Replace("{m}", timeSpan.Minutes.ToString());
                    content = content.Replace("{s}", timeSpan.Seconds.ToString());
                    content = content.Replace("{f}", timeSpan.Milliseconds.ToString());
                    content = content.Replace("{title}", statistic.title == null ? "" : statistic.title);
                    return content;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(statistic.valueLabelStyle.formatter))
                {
                    return ChartCached.NumberToStr(value, numericFormatter);
                }
                else
                {
                    var content = statistic.valueLabelStyle.formatter;
                    FormatterHelper.ReplaceAxisLabelContent(ref content, numericFormatter, value);
                    content = content.Replace("{title}", statistic.title == null ? "" : statistic.title);
                    return content;
                }
            }
        }

        public static Vector3 GetContentPosition(UIStatistic statistic)
        {
            return statistic.graphPosition + statistic.location.GetPosition(statistic.graphWidth, statistic.graphHeight);
        }

        private static string GetNumericFormatter(UIStatistic statistic, LabelStyle labelStyle)
        {
            return string.IsNullOrEmpty(labelStyle.numericFormatter)
                    ? (statistic.context.valueIsInteger ? "0" : "0.00")
                    : labelStyle.numericFormatter;
        }
    }
}