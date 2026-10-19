using UnityEngine;
using UnityEngine.UI;
using XUGL;
using System.Collections.Generic;

namespace XCharts.Runtime.Line3Ds
{
    /// <summary>
    /// Line3D chart component. It is used for showing the relationship between nodes.
    /// ||关系图，用于展现节点以及节点之间的关系数据。
    /// </summary>
    [System.Serializable]
    [SerieHandler(typeof(Line3DHandler), true)]
    [SerieComponent(typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(AreaStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle))]
    [SerieDataExtraField()]
    public class Line3D : Serie, INeedSerieContainer
    {
        public int containerIndex { get; internal set; }
        public int containterInstanceId { get; internal set; }

        public static Serie AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Line3D>(serieName);

            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Circle;

            serie.showDataName = true;
            serie.showDataDimension = 2;

            for (int i = 0; i < 5; i++)
            {
                var x = i;
                var y = i * 5;
                var z = i * 10;
                chart.AddData(serie.index, x, y, z);
            }
            return serie;
        }
    }
}