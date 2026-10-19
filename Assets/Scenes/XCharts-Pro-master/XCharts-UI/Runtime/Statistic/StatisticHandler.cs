using System;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    internal static class StatisticHandler
    {
        public static void InitLabel(UIStatistic statistic)
        {
            statistic.location.OnChanged();
            var anchorMin = statistic.location.runtimeAnchorMin;
            var anchorMax = statistic.location.runtimeAnchorMax;
            var pivot = statistic.location.runtimePivot;
            var contentObject = ChartHelper.AddObject("content", statistic.transform, anchorMin, anchorMax,
                pivot, statistic.graphSizeDelta, -1, statistic.childrenNodeNames);

            anchorMin = statistic.location.runtimeAnchorMin;
            anchorMax = statistic.location.runtimeAnchorMax;
            pivot = statistic.location.runtimePivot;
            ChartHelper.UpdateRectTransform(contentObject, anchorMin, anchorMax, pivot, new Vector2(statistic.graphWidth, statistic.graphHeight));
            var titleFontSize = statistic.titleLabelStyle.textStyle.GetFontSize(statistic.theme.sharedTheme.title);
            var valueFontSize = statistic.valueLabelStyle.textStyle.GetFontSize(statistic.theme.sharedTheme.title);
            var contentPosition = StatisticHelper.GetContentPosition(statistic);
            contentObject.transform.localPosition = contentPosition;
            contentObject.hideFlags = statistic.chartHideFlags;

            var titleContent = StatisticHelper.GetTitleContent(statistic, statistic.value);
            var titleLabel = ChartHelper.AddChartLabel("title", contentObject.transform, statistic.titleLabelStyle,
                statistic.theme.sharedTheme.axis, titleContent, Color.clear, statistic.location.runtimeTextAlignment);
            ChartHelper.SetActive(titleLabel, statistic.titleLabelStyle.show);

            var valueContent = StatisticHelper.GetValueContent(statistic, statistic.value);
            var valueLabel = ChartHelper.AddChartLabel("value", contentObject.transform, statistic.valueLabelStyle,
                statistic.theme.sharedTheme.title, valueContent, Color.clear, statistic.location.runtimeTextAlignment);
            ChartHelper.SetActive(valueLabel, statistic.valueLabelStyle.show);
            statistic.context.valueLabel = valueLabel;
            statistic.context.titleLabel = titleLabel;
            statistic.context.value = statistic.value;

            if (statistic.location.IsTop())
            {
                titleLabel.transform.localPosition = statistic.titleLabelStyle.offset;
                valueLabel.transform.localPosition = -new Vector3(0, titleFontSize + statistic.itemGap) + statistic.valueLabelStyle.offset;
            }
            else if (statistic.location.IsBottom())
            {
                titleLabel.transform.localPosition = new Vector3(0, valueFontSize + statistic.itemGap) + statistic.titleLabelStyle.offset;
                valueLabel.transform.localPosition = statistic.valueLabelStyle.offset;
            }
            else
            {
                var total = (titleFontSize + valueFontSize + statistic.itemGap) / 2;
                var titlePos = new Vector3(0, total - titleFontSize / 2);
                var valuePos = titlePos - new Vector3(0, titleFontSize / 2 + statistic.itemGap + valueFontSize / 2);
                titleLabel.transform.localPosition = titlePos + statistic.titleLabelStyle.offset;
                valueLabel.transform.localPosition = valuePos + statistic.valueLabelStyle.offset;
            }
        }

        public static void UpdateAnimation(UIStatistic statistic)
        {
            if (statistic.context.valueUpdate)
            {
                var currentValue = statistic.GetCurrentValue(statistic.animationStyle.GetChangeDuration());
                UpdateValueText(statistic, currentValue);
                if (statistic.onChange != null)
                {
                    statistic.onChange.Invoke(currentValue);
                }
            }
        }

        public static void UpdateCountdown(UIStatistic statistic)
        {
            if (statistic.type != UIStatistic.Type.Countdown) return;
            if (statistic.context.countdownFinish) return;
            statistic.context.value -= Time.deltaTime;
            if (statistic.context.value < 0)
            {
                statistic.context.value = 0;
                statistic.context.countdownFinish = true;
                UpdateValueText(statistic, statistic.context.value);
                if (statistic.onFinish != null)
                {
                    statistic.onFinish.Invoke(statistic.context.value);
                }
            }
            else
            {
                UpdateValueText(statistic, statistic.context.value);
                if (statistic.onChange != null)
                {
                    statistic.onChange.Invoke(statistic.context.value);
                }
            }
        }

        public static void UpdateValueText(UIStatistic statistic, double value)
        {
            if (statistic.context.valueLabel != null)
            {
                statistic.context.valueLabel.SetText(StatisticHelper.GetValueContent(statistic, value));
            }
            if (statistic.context.titleLabel != null)
            {
                statistic.context.titleLabel.SetText(StatisticHelper.GetTitleContent(statistic, value));
            }
        }
    }
}