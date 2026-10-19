

using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Gantts;

namespace XCharts.Example.Gantts
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_Gantt_Category : MonoBehaviour
    {
        private GanttChart chart;
        private float updateTime;
        public int dayCount = 10;
        public int taskCount = 5;

        void Awake()
        {
            chart = gameObject.GetComponent<GanttChart>();
            if (chart == null)
            {
                chart = gameObject.AddComponent<GanttChart>();
            }
            GenerateCategoryData();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddData();
            }
        }

        void AddData()
        {
            for (int i = 0; i < taskCount; i++)
            {
                var startIndex = Random.Range(0, (int)(dayCount * 2.0f / 3));
                var endIndex = Random.Range(startIndex, dayCount);
                chart.UpdateData(0, i, 0, startIndex);
                chart.UpdateData(0, i, 1, endIndex);
            }
        }

        void GenerateCategoryData()
        {
            chart.RemoveData();

            var grid = chart.GetChartComponent<GridCoord>();
            grid.left = 100;

            var xAxis0 = chart.GetChartComponent<Axis>();
            xAxis0.type = Axis.AxisType.Category;
            xAxis0.boundaryGap = false;
            xAxis0.splitNumber = dayCount;

            var yAxis0 = chart.GetChartComponent<YAxis>();
            yAxis0.type = Axis.AxisType.Category;
            yAxis0.boundaryGap = true;
            yAxis0.splitNumber = 0;

            for (int i = 0; i < dayCount; i++)
            {
                chart.AddXAxisData("day" + (i + 1));
            }

            chart.AddSerie<Gantt>("任务进度表");
            for (int i = 0; i < taskCount; i++)
            {
                var taskName = "task-" + (i + 1);
                var startIndex = Random.Range(0, (int)(dayCount * 2.0f / 3));
                var endIndex = Random.Range(startIndex, dayCount);
                chart.AddData(0, startIndex, endIndex, taskName);
            }
        }
    }
}