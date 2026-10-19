
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Pie3Ds
{
    internal static class Pie3DSlicePool
    {
        private static readonly ObjectPool<Pie3DSlice> s_ListPool = new ObjectPool<Pie3DSlice>(OnGet, null);

        static void OnGet(Pie3DSlice serieData)
        {
            serieData.sidePolygons.Clear();
            serieData.topPolygons.Clear();
            serieData.startOrEndPolygons.Clear();
        }

        static void OnClear(Pie3DSlice serieData)
        {
        }

        public static Pie3DSlice Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(Pie3DSlice toRelease)
        {
            s_ListPool.Release(toRelease);
        }

        public static void Release(List<Pie3DSlice> toReleases)
        {
            foreach (var item in toReleases)
                s_ListPool.Release(item);
            toReleases.Clear();
        }
    }
}
