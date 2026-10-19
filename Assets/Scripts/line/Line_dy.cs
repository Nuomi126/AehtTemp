using UnityEngine;
#if INPUT_SYSTEM_ENABLED
using Input = XCharts.Runtime.InputHelper;
#endif
using XCharts.Runtime;

namespace XCharts.Example
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Line_dy: MonoBehaviour
    {
        void Awake()
        {
            // 初始化图表
            InitChart();
            
            // 注册数据更新事件
            if (DailyReportManager.Instance != null)
            {
                DailyReportManager.Instance.OnDataUpdated.AddListener(UpdateChartWithApiData);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 手动请求数据
                if (DailyReportManager.Instance != null)
                {
                    DailyReportManager.Instance.FetchDailyReports();
                }
            }
        }

        // 初始化图表
        void InitChart()
        {
            var chart = gameObject.GetComponent<LineChart>();
            if (chart == null)
            {
                chart = gameObject.AddComponent<LineChart>();
                chart.Init();
            }
            chart.EnsureChartComponent<Title>().show = true;
            chart.EnsureChartComponent<Title>().text = "电压数据图表";

            chart.EnsureChartComponent<Tooltip>().show = true;
            chart.EnsureChartComponent<Legend>().show = true;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            var yAxis = chart.EnsureChartComponent<YAxis>();
            xAxis.show = true;
            yAxis.show = true;
            xAxis.type = Axis.AxisType.Category;
            yAxis.type = Axis.AxisType.Value;

            xAxis.splitNumber = 10;
            xAxis.boundaryGap = true;

            chart.RemoveData();
          
            chart.AddSerie<Line>("工作电压");
           
        }
        
        // 使用API数据更新图表
        void UpdateChartWithApiData(DailyReportResponse data)
        {
            if (data == null || data.rows == null || data.rows.Length == 0)
            {
                Debug.LogError("API返回的数据为空");
                return;
            }
            
            var chart = gameObject.GetComponent<LineChart>();
            if (chart == null)
            {
                Debug.LogError("找不到LineChart组件");
                return;
            }
            
            try
            {
                // 清除现有数据
                chart.ClearData();
                
                // 确保有正确的系列
                if (chart.series.Count < 3)
                {
                    chart.RemoveData();
                    
                    chart.AddSerie<Line>("工作电压");
                    
                }
                
                // 添加数据
                for (int i = 0; i < data.rows.Length; i++)
                {
                    var report = data.rows[i];
                    
                    // 使用槽号作为X轴标签
                    chart.AddXAxisData(report.caoh.ToString());
                    
                    // 添加三种电压数据
                    
                    chart.AddData(0, report.gongzdy); // 工作电压
                    
                }
                
                // 刷新图表
                chart.RefreshChart();
                
                Debug.Log($"图表已更新，共 {data.rows.Length} 条数据");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"更新图表时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 手动加载数据的方法（可通过按钮调用）
        public void LoadData()
        {
            if (DailyReportManager.Instance != null)
            {
                DailyReportManager.Instance.FetchDailyReports();
            }
        }
    }
}