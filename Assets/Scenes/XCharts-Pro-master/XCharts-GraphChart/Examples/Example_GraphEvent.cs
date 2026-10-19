using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Graphs;

namespace XCharts.Example.Graphs
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_GraphEvent : MonoBehaviour
    {
        private GraphChart chart;

        void Awake()
        {
            chart = gameObject.GetComponent<GraphChart>();
            chart.onSerieClick = (eventData) =>
            {
                Debug.Log("onSerieClick serieIndex: " + eventData.serieIndex + " dataIndex: " + eventData.dataIndex + " dimension: " + eventData.dimension);
            };
            var symbol = chart.GetSerie(0).symbol;
            symbol.sizeType = SymbolSizeType.Function;
            //symbol.sizeFunction = (defaultSize, serieData) =>
            //{
                //if(serieData.index == 0) return defaultSize*2;
                //else return defaultSize;
            //};
        }
    }
}