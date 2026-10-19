

using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Gantts;

namespace XCharts.Example.Gantts
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_Gantt_DataZoom : MonoBehaviour
    {
        [SerializeField] private int s_MaxShowTask = 5;
        [SerializeField] private float s_TaskWidth = 40f;

        private GanttChart chart;
        private float updateTime;
        private int taskCount = 5;
        private int taskNowCount = 0;
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
                AddSerie();
            }
        }

        void AddSerie()
        {
            if (taskNowCount < s_MaxShowTask)
            {
                chart.GetSerie(taskNowCount).show = true;
                taskNowCount++;
            }
            else
            {
                AddSerie(taskNowCount, true);
            }
            var grid = chart.GetChartComponent<GridCoord>();
            var dataZoom = chart.GetChartComponent<DataZoom>(1);
            var yAxis = chart.GetChartComponent<YAxis>();
            var serieCount = chart.series.Count < s_MaxShowTask ? s_MaxShowTask : chart.series.Count;
            var eachWidth = grid.context.height / s_MaxShowTask;
            var totalHeight = serieCount * eachWidth;
            var rate = grid.context.height / totalHeight * 100;

            if (dataZoom.end > rate)
            {
                dataZoom.start = dataZoom.end - rate;
            }
            else
            {
                dataZoom.start = 0;
                dataZoom.end = rate;
            }
            yAxis.SetComponentDirty();
            chart.RefreshChart();
        }

        void GenerateTimeData()
        {
            var style = chart.EnsureChartComponent<GanttStyle>();
            style.ganttType = GanttType.Multinomial;
            chart.RemoveData();

            var grid = chart.GetChartComponent<GridCoord>();
            grid.left = 100;

            var xAxis0 = chart.GetChartComponent<XAxis>();
            xAxis0.type = Axis.AxisType.Time;
            xAxis0.boundaryGap = false;
            xAxis0.splitNumber = 5;
            xAxis0.axisLabel.numericFormatter = "MM-dd HH:mm:ss";

            var yAxis0 = chart.GetChartComponent<YAxis>();
            yAxis0.type = Axis.AxisType.Category;
            yAxis0.boundaryGap = true;
            yAxis0.splitNumber = 0;

            var dataZoom0 = chart.GetChartComponent<DataZoom>(0);
            dataZoom0.enable = true;
            dataZoom0.zoomLock = false;
            dataZoom0.supportInside = true;
            dataZoom0.supportInsideScroll = true;
            dataZoom0.supportInsideDrag = false;
            dataZoom0.supportSlider = true;
            dataZoom0.start = 0;
            dataZoom0.end = 100;

            var dataZoom1 = chart.GetChartComponent<DataZoom>(0);
            dataZoom1.enable = true;
            dataZoom1.zoomLock = true;
            dataZoom1.supportInside = false;
            dataZoom1.supportSlider = true;
            dataZoom1.start = 0;
            dataZoom1.end = 100;

            for (int i = 0; i < s_MaxShowTask; i++)
            {
                AddSerie(i, i == 0);
            }
        }

        void AddSerie(int index, bool show)
        {
            var serie = chart.AddSerie<Gantt>("Task" + (index + 1));
            serie.show = show;
            serie.barWidth = s_TaskWidth;
            serie.clip = true;
            if (serie.show) taskNowCount++;
            var nowTimestamp = DateTimeUtil.GetTimestamp();
            for (int j = 0; j < taskCount; j++)
            {
                var taskName = "Stage" + (j + 1);
                var endTimestamp = nowTimestamp + Random.Range(2, 4) * 3600 * 24;
                chart.AddData(index, nowTimestamp, endTimestamp, taskName);
                nowTimestamp = endTimestamp;
            }
        }
    }
}