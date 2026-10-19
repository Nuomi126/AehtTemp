using System.Collections.Generic;
using UnityEngine;
using XUGL;

namespace XCharts.Runtime.Pie3Ds
{
    /// <summary>
    /// Slice of pie.
    /// ||3D饼图的切块。
    /// </summary>
    public class Pie3DSlice
    {
        public int serieDataIndex;
        public float startAngle;
        public float endAngle;
        public float cos;
        public bool showStartSide;
        public bool showEndSide;
        public float hig;
        public bool forword;
        public List<Vector3> sidePolygons = new List<Vector3>();
        public List<Vector3> topPolygons = new List<Vector3>();
        public List<Vector3> startOrEndPolygons = new List<Vector3>();

        public Pie3DSlice()
        {
        }

        public void UpdateValue(int index, float start, float end, bool startSide, bool endSide, float maxHig)
        {
            serieDataIndex = index;
            startAngle = start;
            endAngle = end;
            showStartSide = startSide;
            showEndSide = endSide;
            forword = startAngle < 180;
            this.hig = maxHig;
            var cosS = Mathf.Cos((startAngle + 90) * Mathf.Deg2Rad);
            var cosE = Mathf.Cos((endAngle + 90) * Mathf.Deg2Rad);
            if (forword)
            {
                if (startAngle < 90 && endAngle > 90)
                    cos = -1;
                else
                    cos = Mathf.Max(cosS, cosE);
            }
            else
            {
                if (startAngle < 270 && endAngle > 270)
                    cos = 1;
                else
                    cos = Mathf.Max(cosS, cosE);
            }
        }

        public bool Contains(float angle)
        {
            return angle >= startAngle && angle < endAngle;
        }

        public bool IsInPolygon(Vector2 p)
        {
            return IsInSidePolygon(p) || IsInTopPolygon(p) || IsInStartOrEndSide(p);
        }

        public bool IsInStartOrEndSide(Vector2 p)
        {
            return UGLHelper.IsPointInPolygon(p, startOrEndPolygons);
        }

        public bool IsInSidePolygon(Vector2 p)
        {
            return UGLHelper.IsPointInPolygon(p, sidePolygons);
        }

        public bool IsInTopPolygon(Vector2 p)
        {
            return UGLHelper.IsPointInPolygon(p, topPolygons);
        }
    }
}