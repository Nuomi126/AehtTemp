using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MultiSensorChartManager : MonoBehaviour
{
    [Header("组件引用")]
    public GetTemperature temperatureManager;
    public GameObject singleChartPrefab; // 单个图表的预制体
    public Transform chartsContainer; // 放置图表的容器
    
    [Header("数据设置")]
    public TextMeshProUGUI potNameText; // 电解槽文本
    //public TextMeshProUGUI dateText; // 日期文本
    public Button applyFilterButton; // 应用筛选按钮
    
    [Header("按钮控制")]
    public Button startButton;
    public Button stopButton;
    public Button clearButton;
    
    [Header("API参数")]
    public string companyIndex = "1033";
    public string caoHao = "1001";
    
    [Header("图表布局")]
    public int columns = 4; // 每行显示的图表数量
    public Vector2 chartSize = new Vector2(300, 200);
    public Vector2 spacing = new Vector2(20, 20);
    
    [Header("数据设置")]
    public bool autoRefresh = false;
    public float refreshInterval = 2f; // 2秒刷新一次
    
    [Header("颜色配置")]
    public Color[] sensorColors = new Color[240]; // 支持更多测温点
    
    private List<SingleSensorChartController> chartControllers = new List<SingleSensorChartController>();
    private List<int> sensorIndexes = new List<int>(); // 实际的测温点索引
    private float timer = 0f;
    
    void Start()
    {
        InitializeColors();
        SetupButtons();
        
        // 注册应用筛选按钮点击事件
        if (applyFilterButton != null)
        {
            applyFilterButton.onClick.AddListener(OnApplyFilter);
        }
    }
    
    void Update()
    {
        if (autoRefresh)
        {
            timer += Time.deltaTime;
            if (timer >= refreshInterval)
            {
                timer = 0f;
                RefreshAllData();
            }
        }
    }
    
    // 应用筛选按钮点击事件
    private void OnApplyFilter()
    {
        Debug.Log("应用筛选按钮被点击");
        
        // 获取测温点索引并创建图表
        FetchSensorIndexes();
    }
    
    // 获取测温点索引
    private void FetchSensorIndexes()
    {
        // 清除现有索引
        sensorIndexes.Clear();
        
        // 启动协程获取数据
        StartCoroutine(GetSensorIndexes());
    }
    
    // 获取测温点索引的协程
    private IEnumerator GetSensorIndexes()
    {
        Debug.Log("开始获取测温点索引");
        
        if (temperatureManager == null)
        {
            Debug.LogError("GetTemperature组件未设置！");
            yield break;
        }
        
        // 先获取一次数据来确定有多少个测温点
        bool dataReceived = false;
        List<List<int>> tempMatrix = null;
        
        // 使用临时回调获取数据
        System.Action<List<List<int>>> tempCallback = (matrix) => {
            tempMatrix = matrix;
            dataReceived = true;
        };
        
        // 发起数据请求
        StartCoroutine(temperatureManager.WebAPIPostHelperAsync(companyIndex, caoHao, tempCallback));
        
        // 等待数据返回
        float timeout = 10f;
        float elapsed = 0f;
        while (!dataReceived && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (dataReceived && tempMatrix != null && tempMatrix.Count > 0)
        {
            // 根据返回的数据确定测温点数量
            for (int i = 0; i < tempMatrix.Count; i++)
            {
                sensorIndexes.Add(i);
            }
            
            Debug.Log($"获取到 {sensorIndexes.Count} 个测温点索引");
            
            // 创建图表
            CreateCharts();
            
            // 等待一帧，确保图表已创建
            yield return null;
            
            Debug.Log("图表创建完成");
        }
        else
        {
            Debug.LogWarning("获取测温点索引失败或数据为空");
        }
    }
    
    // 初始化颜色
    private void InitializeColors()
    {
        for (int i = 0; i < sensorColors.Length; i++)
        {
            float hue = (float)i / sensorColors.Length;
            sensorColors[i] = Color.HSVToRGB(hue, 0.8f, 0.9f);
        }
    }
    
    // 设置按钮事件
    private void SetupButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartMonitoring);
        
        if (stopButton != null)
            stopButton.onClick.AddListener(StopMonitoring);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAllData);
    }
    
    // 创建所有图表
    private void CreateCharts()
    {
        // 清除现有图表
        foreach (Transform child in chartsContainer)
        {
            Destroy(child.gameObject);
        }
        chartControllers.Clear();
        
        // 根据实际的测温点数量创建图表
        for (int i = 0; i < sensorIndexes.Count; i++)
        {
            CreateSingleChart(i, sensorIndexes[i]);
        }
        
        // 调整容器大小
        AdjustContainerSize();
        
        Debug.Log($"[MultiSensorChartManager] 创建了{sensorIndexes.Count}个图表");
    }
    
    // 创建单个图表
    private void CreateSingleChart(int index, int sensorIndex)
    {
        // 计算位置
        int row = index / columns;
        int col = index % columns;
        float x = col * (chartSize.x + spacing.x);
        float y = -row * (chartSize.y + spacing.y);
        
        // 实例化图表
        GameObject chartObj = Instantiate(singleChartPrefab, chartsContainer);
        chartObj.name = $"Chart_Sensor_{sensorIndex + 1}";
        
        // 设置位置和大小
        RectTransform rectTransform = chartObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = chartSize;
        
        // 获取控制器组件
        SingleSensorChartController controller = chartObj.GetComponent<SingleSensorChartController>();
        if (controller != null)
        {
            // 设置测温点信息
            string sensorName = $"测温点{sensorIndex + 1}";
            Color sensorColor = sensorIndex < sensorColors.Length ? sensorColors[sensorIndex] : Color.red;
            controller.SetSensorInfo(sensorIndex, sensorName, sensorColor);
            
            // 添加到控制器列表
            chartControllers.Add(controller);
        }
    }
    
    // 调整容器大小
    private void AdjustContainerSize()
    {
        if (sensorIndexes.Count == 0) return;
        
        int totalRows = (sensorIndexes.Count + columns - 1) / columns;
        // 计算实际需要的列数（可能小于设置的columns）
        int actualColumns = Mathf.Min(columns, sensorIndexes.Count);
        
        // 计算容器高度和宽度
        float containerHeight = totalRows * (chartSize.y + spacing.y);
        float containerWidth = actualColumns * (chartSize.x + spacing.x);
        
        // 如果只有一行且图表数量少于列数，调整宽度
        if (totalRows == 1 && sensorIndexes.Count < columns)
        {
            containerWidth = sensorIndexes.Count * (chartSize.x + spacing.x) - spacing.x;
        }
        
        Debug.Log($"调整容器大小: 宽度={containerWidth}, 高度={containerHeight}, 行数={totalRows}, 列数={actualColumns}");
        
        RectTransform containerRect = chartsContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(containerWidth, containerHeight);
    }
    
    // 开始监控
    public void StartMonitoring()
    {
        if (chartControllers.Count == 0)
        {
            Debug.LogWarning("请先点击应用筛选按钮创建图表");
            return;
        }
        
        autoRefresh = true;
        timer = 0f;
        
        // 立即获取一次数据
        RefreshAllData();
        
        // 更新按钮状态
        if (startButton != null) startButton.interactable = false;
        if (stopButton != null) stopButton.interactable = true;
        
        Debug.Log("[MultiSensorChartManager] 开始监控");
    }
    
    // 停止监控
    public void StopMonitoring()
    {
        autoRefresh = false;
        
        // 更新按钮状态
        if (startButton != null) startButton.interactable = true;
        if (stopButton != null) stopButton.interactable = false;
        
        Debug.Log("[MultiSensorChartManager] 停止监控");
    }
    
    // 清除所有数据
    public void ClearAllData()
    {
        foreach (var controller in chartControllers)
        {
            controller.ClearData();
        }
        
        Debug.Log("[MultiSensorChartManager] 清除所有图表数据");
    }
    
    // 刷新所有数据
    private void RefreshAllData()
    {
        if (temperatureManager == null)
        {
            Debug.LogError("[MultiSensorChartManager] GetTemperature组件未设置！");
            return;
        }
        
        // 使用改进的API获取数据
        StartCoroutine(temperatureManager.WebAPIPostHelperAsync(companyIndex, caoHao, OnMatrixDataReceived));
    }
    
    // 矩阵数据接收回调
    private void OnMatrixDataReceived(List<List<int>> temperatureMatrix)
    {
        if (temperatureMatrix == null || temperatureMatrix.Count == 0)
        {
            Debug.LogWarning("[MultiSensorChartManager] 接收到空数据");
            return;
        }
        
        Debug.Log($"[MultiSensorChartManager] 接收到{temperatureMatrix.Count}个测温点的数据");
        
        // 为每个图表添加对应的数据
        for (int i = 0; i < Math.Min(chartControllers.Count, temperatureMatrix.Count); i++)
        {
            if (temperatureMatrix[i].Count > 0)
            {
                // 取最新的温度数据
                float temperature = temperatureMatrix[i][temperatureMatrix[i].Count - 1];
                chartControllers[i].AddTemperatureData(temperature);
            }
        }
    }
    
    // 兼容原有API的回调
    private void OnDataReceived(List<int> temperatures)
    {
        if (temperatures == null || temperatures.Count == 0)
        {
            Debug.LogWarning("[MultiSensorChartManager] 接收到空数据");
            return;
        }
        
        Debug.Log($"[MultiSensorChartManager] 接收到{temperatures.Count}个温度数据点");
        
        // 为每个图表添加对应的数据
        for (int i = 0; i < Math.Min(chartControllers.Count, temperatures.Count); i++)
        {
            chartControllers[i].AddTemperatureData(temperatures[i]);
        }
    }
}