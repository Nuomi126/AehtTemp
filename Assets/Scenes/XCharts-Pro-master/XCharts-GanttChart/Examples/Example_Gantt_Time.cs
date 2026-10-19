

using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Gantts;

namespace XCharts.Example.Gantts
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_Gantt_Time : MonoBehaviour
    {
        private GanttChart chart;
        private float updateTime;
        public int taskCount = 5;

        void Awake()
        {
            chart = gameObject.GetComponent<GanttChart>();
            if (chart == null)
            {
                chart = gameObject.AddComponent<GanttChart>();
            }
            GenerateTimeData();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddData();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                UpdateData();
            }
        }

        void UpdateData()
        {
            var serieIndex = Random.Range(0, chart.series.Count);
            var serie = chart.series[serieIndex];
            var dataIndex = Random.Range(0, serie.dataCount);
            var nowTimestamp = DateTimeUtil.GetTimestamp();
            var startTimestamp = nowTimestamp + Random.Range(1, 6) * 3600 * 24;
            var endTimestamp = startTimestamp + Random.Range(1, 10) * 3600 * 24;
            chart.UpdateData(serieIndex, dataIndex, startTimestamp, endTimestamp);
        }

        void AddData()
        {
            chart.ClearData();
            taskCount = Random.Range(2, 6);
            for (int i = 0; i < taskCount; i++)
            {
                var taskName = "张三-任务-" + (i + 1);
                var nowTimestamp = DateTimeUtil.GetTimestamp();
                var startTimestamp = nowTimestamp + Random.Range(1, 6) * 3600 * 24;
                var endTimestamp = startTimestamp + Random.Range(1, 10) * 3600 * 24;
                chart.AddData(0, startTimestamp, endTimestamp, taskName);
            }
            for (int i = 0; i < taskCount; i++)
            {
                var taskName = "李四-任务-" + (i + 1);
                var nowTimestamp = DateTimeUtil.GetTimestamp();
                var startTimestamp = nowTimestamp + Random.Range(1, 6) * 3600 * 24;
                var endTimestamp = startTimestamp + Random.Range(1, 10) * 3600 * 24;
                chart.AddData(1, startTimestamp, endTimestamp, taskName);
            }
        }

        void GenerateTimeData()
        {
            chart.RemoveData();

            var grid = chart.GetChartComponent<GridCoord>();
            grid.left = 100;

            var xAxis0 = chart.GetChartComponent<XAxis>();
            xAxis0.type = Axis.AxisType.Time;
            xAxis0.boundaryGap = false;
            xAxis0.splitNumber = 5;
            // xAxis0.axisLabel.numericFormatter = "HH:mm:ss";

            var yAxis0 = chart.GetChartComponent<YAxis>();
            yAxis0.type = Axis.AxisType.Category;
            yAxis0.boundaryGap = true;
            yAxis0.splitNumber = 0;

            var serie1 = chart.AddSerie<Gantt>("张三");
            serie1.clip = true;
            // serie1.EnsureComponent<LabelStyle>();
            // serie1.label.show = true;
            taskCount = Random.Range(2, 6);
            for (int i = 0; i < taskCount; i++)
            {
                var taskName = "张三-任务-" + (i + 1);
                var nowTimestamp = DateTimeUtil.GetTimestamp();
                var startTimestamp = nowTimestamp + Random.Range(1, 6) * 3600 * 24;
                var endTimestamp = startTimestamp + Random.Range(1, 10) * 3600 * 24;
                chart.AddData(0, startTimestamp, endTimestamp, taskName);
            }
            var serie2 = chart.AddSerie<Gantt>("李四");
            serie2.clip = true;
            for (int i = 0; i < taskCount; i++)
            {
                var taskName = "李四-任务-" + (i + 1);
                var nowTimestamp = DateTimeUtil.GetTimestamp();
                var startTimestamp = nowTimestamp + Random.Range(1, 6) * 3600 * 24;
                var endTimestamp = startTimestamp + Random.Range(1, 10) * 3600 * 24;
                chart.AddData(1, startTimestamp, endTimestamp, taskName);
            }
        }
    }
}