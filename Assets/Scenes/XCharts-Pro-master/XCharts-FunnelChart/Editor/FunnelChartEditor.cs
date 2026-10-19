
using UnityEditor;
using XCharts.Runtime.Funnels;

namespace XCharts.Editor.Funnels
{
    /// <summary>
    /// Editor class used to edit UI FunnelChart.
    /// ||</summary>
    [CustomEditor(typeof(FunnelChart), false)]
    public class FunnelChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/FunnelChart/Funnel", priority = 102)]
        [MenuItem("GameObject/UI/XCharts/FunnelChart/Funnel", priority = 102)]
        public static void AddFunnelChart()
        {
            XChartsEditor.AddChart<FunnelChart>("FunnelChart");
        }

        [MenuItem("XCharts/FunnelChart/Horizonal Funnel", priority = 102)]
        [MenuItem("GameObject/UI/XCharts/FunnelChart/Horizonal Funnel", priority = 102)]
        public static void AddFunnelChart_Horizonal()
        {
            var chart = XChartsEditor.AddChart<FunnelChart>("FunnelChart");
            chart.DefaultHorizonalFunnelChart();
        }
    }
}