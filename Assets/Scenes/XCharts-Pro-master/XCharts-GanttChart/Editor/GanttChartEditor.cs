
using UnityEditor;
using XCharts.Runtime.Gantts;

namespace XCharts.Editor.Gantts
{
    [CustomEditor(typeof(GanttChart), false)]
    public class GanttChartEditor : BaseChartEditor
    {
        [MenuItem("XCharts/GanttChart/Time Gantt", priority = 103)]
        [MenuItem("GameObject/UI/XCharts/GanttChart/Time Gantt", priority = 103)]
        public static void AddGanttChart()
        {
            XChartsEditor.AddChart<GanttChart>("GanttChart");
        }

        [MenuItem("XCharts/GanttChart/Value Gantt", priority = 103)]
        [MenuItem("GameObject/UI/XCharts/GanttChart/Value Gantt", priority = 103)]
        public static void AddGanttChart_ValueAxis()
        {
            var chart = XChartsEditor.AddChart<GanttChart>("GanttChart");
            chart.DefaultValueAxisGanttChart();
        }

        [MenuItem("XCharts/GanttChart/Category Gantt", priority = 103)]
        [MenuItem("GameObject/UI/XCharts/GanttChart/Category Gantt", priority = 103)]
        public static void AddGanttChart_CategoryAxis()
        {
            var chart = XChartsEditor.AddChart<GanttChart>("GanttChart");
            chart.DefaultCategoryAxisGanttChart();
        }

        [MenuItem("XCharts/GanttChart/Monomial Group Gantt", priority = 103)]
        [MenuItem("GameObject/UI/XCharts/GanttChart/Monomial Group Gantt", priority = 103)]
        public static void AddGanttChart_MonomialGroupGantt()
        {
            var chart = XChartsEditor.AddChart<GanttChart>("GanttChart");
            chart.DefaultMonomialGroupGanttChart();
        }

        [MenuItem("XCharts/GanttChart/Multinomial Group Gantt", priority = 103)]
        [MenuItem("GameObject/UI/XCharts/GanttChart/Multinomial Group Gantt", priority = 103)]
        public static void AddGanttChart_MultinomialGroupGantt()
        {
            var chart = XChartsEditor.AddChart<GanttChart>("GanttChart");
            chart.DefaultMultinomialGroupGanttChart();
        }
    }
}