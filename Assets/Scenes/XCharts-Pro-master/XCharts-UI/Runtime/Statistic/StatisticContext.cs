using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    public sealed class StatisticContext
    { 
        public double value;
        public bool countdownFinish;
        public ChartLabel valueLabel;
        public ChartLabel titleLabel;
        internal bool valueUpdate;
        internal bool valueIsInteger;
        internal float valueUpdateTime;
        internal double previousValue;
    }
}