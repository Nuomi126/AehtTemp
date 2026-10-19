using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using System;
using UnityEngine.UI;

public class SingleSensorChartController : MonoBehaviour
{
    [Header("图表组件")]
    public LineChart lineChart;
    
    [Header("测温点设置")]
    public int sensorIndex; // 该图表对应的测温点索引（0-239）
    public string sensorName; // 测温点名称
    
    [Header("数据设置")]
    private List<float> temperatureHistory = new List<float>(); // 温度历史数据
    private List<string> timeLabels = new List<string>(); // 时间标签
    private int maxDataPoints = 300; // 10分钟 * 30个点/分钟 = 300个数据点
    
    [Header("显示设置")]
    public Color lineColor = Color.red;
    public float lineWidth = 2f;
    
    void Start()
    {
        InitializeChart();
    }
    
    // 初始化图表
    private void InitializeChart()
    {
        if (lineChart == null)
        {
            lineChart = GetComponent<LineChart>();
        }
        
        if (lineChart == null)
        {
            Debug.LogError($"[SingleSensorChart] 测温点{sensorIndex}：未找到LineChart组件！");
            return;
        }
        
        // 清除现有数据
        lineChart.ClearData();
        
        // 创建单个数据系列
        var serie = lineChart.AddSerie<Line>(sensorName);
        serie.lineStyle.color = lineColor;
        serie.lineStyle.width = lineWidth;
        serie.symbol.show = true;
        serie.symbol.size = 4;
        serie.animation.enable = true;
        
        // 配置坐标轴
        var xAxis = lineChart.EnsureChartComponent<XAxis>();
        xAxis.show = true;
        xAxis.type = Axis.AxisType.Category;
        xAxis.splitNumber = 10;
        
        var yAxis = lineChart.EnsureChartComponent<YAxis>();
        yAxis.show = true;
        yAxis.type = Axis.AxisType.Value;
        yAxis.splitNumber = 8;
        
        // 添加标题
        var title = lineChart.EnsureChartComponent<Title>();
        title.show = true;
        title.text = $"{sensorName} - 10分钟温度曲线";
        
        // 添加提示工具
        var tooltip = lineChart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        
        // 添加DataZoom组件
        var dataZoom = lineChart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = false; // 单个图表不需要滑块
        dataZoom.start = 0;
        dataZoom.end = 100;
        
        Debug.Log($"[SingleSensorChart] 测温点{sensorIndex}图表初始化完成");
    }
    
    // 添加新的温度数据点
    public void AddTemperatureData(float temperature)
    {
        if (lineChart == null) return;
        
        // 添加时间标签
        string currentTime = DateTime.Now.ToString("HH:mm:ss");
        timeLabels.Add(currentTime);
        temperatureHistory.Add(temperature);
        
        // 限制数据点数量（保持10分钟的数据）
        if (temperatureHistory.Count > maxDataPoints)
        {
            temperatureHistory.RemoveAt(0);
            timeLabels.RemoveAt(0);
        }
        
        // 更新图表
        UpdateChart();
        
        Debug.Log($"[SingleSensorChart] 测温点{sensorIndex}添加数据：{temperature}°C，当前数据点数：{temperatureHistory.Count}");
    }
    
    // 更新图表显示
    private void UpdateChart()
    {
        if (lineChart == null) return;
        
        // 清除现有数据
        lineChart.RemoveData();
        
        // 添加X轴标签
        foreach (string timeLabel in timeLabels)
        {
            lineChart.AddXAxisData(timeLabel);
        }
        
        // 添加温度数据
        foreach (float temp in temperatureHistory)
        {
            lineChart.AddData(0, temp); // 系列索引为0
        }
        
        // 刷新图表
        lineChart.RefreshChart();
    }
    
    // 清除历史数据
    public void ClearData()
    {
        temperatureHistory.Clear();
        timeLabels.Clear();
        if (lineChart != null)
        {
            lineChart.RemoveData();
        }
        Debug.Log($"[SingleSensorChart] 测温点{sensorIndex}数据已清除");
    }
    
    // 设置测温点信息
    public void SetSensorInfo(int index, string name, Color color)
    {
        sensorIndex = index;
        sensorName = name;
        lineColor = color;
        
        // 重新初始化图表
        InitializeChart();
    }
}