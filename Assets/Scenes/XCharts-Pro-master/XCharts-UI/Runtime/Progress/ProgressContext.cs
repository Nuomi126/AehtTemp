using UnityEngine;

namespace XCharts.Runtime.UI
{
    public class ProgressContext
    {
        public Rect drawRect;
        public Rect fillRect;
        public float fillRate;
        public float fillTickness;
        public Vector3 fillCenter;
        public float fillInnerRadius;
        public float fillOuterRadius;
        public float fillStartAngle;
        public float fillAngle;
        public Vector3 labelPos;
        public ChartLabel label;
    }
}