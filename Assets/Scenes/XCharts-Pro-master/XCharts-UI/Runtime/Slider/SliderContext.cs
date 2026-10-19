using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.UI
{
    public class SliderContext
    {
        public bool isMinDraging;
        public bool isMaxDraging;
        public Rect drawRect;
        public float fillRate0;
        public float fillRate1;
        public Vector3 startLogicPos;
        public Vector3 endLogicPos;
        public Vector3 minHandlePos;
        public Vector3 maxHandlePos;
        public float handleSize;
        public float markSize;
        public List<ChartLabel> markLabels = new List<ChartLabel>();
        public List<Vector3> markPos = new List<Vector3>();
    }
}