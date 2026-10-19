using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using XCharts.Example;

public class MultiChartManager : MonoBehaviour
{
    [Header("图表设置")]
    public GameObject chartPrefab; // LineChart预制体
    public Transform chartsContainer; // 放置图表的容器
    public int columns = 1; // 每行显示的图表数量
    public Vector2 chartSize = new Vector2(400, 300); // 每个图表的大小
    public Vector2 spacing = new Vector2(20, 20); // 图表之间的间距
    
    [Header("数据设置")]
    public TextMeshProUGUI potNameText; // 电解槽文本
    public TextMeshProUGUI dateText; // 日期文本
    public Button applyFilterButton; // 应用筛选按钮
    
    private List<Line_LocalDB> chartComponents = new List<Line_LocalDB>();
    private List<int> positionIndexes = new List<int>();
    
    void Start()
    {
        // 注册按钮点击事件
        if (applyFilterButton != null)
        {
            applyFilterButton.onClick.AddListener(OnApplyFilter);
        }
    }
    
    // 应用筛选按钮点击事件
    private void OnApplyFilter()
    {
        Debug.Log("应用筛选按钮被点击");
        
        // 直接调用 FetchPositionIndexes 方法
        // 注意：CreateCharts 和 LoadData 将在协程中调用
        FetchPositionIndexes();
    }
    
    // 获取位置索引列表（示例方法，实际应从数据库获取）
    // 获取位置索引列表
    private void FetchPositionIndexes()
    {
        // 清除现有索引
        positionIndexes.Clear();
        
        // 从Line_LocalDB获取位置索引
        if (chartPrefab != null)
        {
            Line_LocalDB tempLineDB = chartPrefab.GetComponent<Line_LocalDB>();
            if (tempLineDB != null)
            {
                // 创建临时实例获取位置索引
                GameObject tempObj = Instantiate(chartPrefab);
                Line_LocalDB lineDB = tempObj.GetComponent<Line_LocalDB>();
                
                // 设置文本引用
                lineDB.potNameText = potNameText;
                lineDB.dateText = dateText;
                
                // 获取位置索引（这会触发数据库查询）
                StartCoroutine(GetPositionIndexes(lineDB, tempObj));
            }
        }
    }
    
    // 获取位置索引的协程
    private System.Collections.IEnumerator GetPositionIndexes(Line_LocalDB lineDB, GameObject tempObj)
    {
        Debug.Log("开始获取位置索引");
        // 激活临时对象，确保它能正常加载数据
        tempObj.SetActive(true);
        // 等待数据加载完成
        lineDB.LoadData();
        
        // 增加等待时间，确保数据完全加载
        yield return new WaitForSeconds(2.0f);
        
        // 获取位置索引
        positionIndexes = lineDB.GetSampleIndexes();
        Debug.Log($"MultiChartManager获取到 {positionIndexes.Count} 个位置索引");
        
        // 检查索引数量是否正确
        if (positionIndexes.Count <= 5 && lineDB.GetType().GetField("sampleIndexes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(lineDB) is List<int> originalIndexes)
        {
            Debug.LogWarning($"可能存在数据传递问题：Line_LocalDB内部有 {originalIndexes.Count} 个索引，但只返回了 {positionIndexes.Count} 个");
        }
        
        // 销毁临时对象
        Destroy(tempObj);
        
        // 确保有位置索引
        if (positionIndexes.Count > 0)
        {
            // 创建图表
            CreateCharts();
            
            // 等待一帧，确保图表已创建
            yield return null;
            
            // 加载数据
            LoadData();
            
            Debug.Log("图表创建和数据加载完成");
        }
        else
        {
            Debug.LogWarning("没有获取到位置索引，无法创建图表");
        }
    }
    
    // 创建图表
    private void CreateCharts()
    {
        // 清除现有图表
        foreach (Transform child in chartsContainer)
        {
            Destroy(child.gameObject);
        }
        chartComponents.Clear();
        
        // 创建新图表
        for (int i = 0; i < positionIndexes.Count; i++)
        {
            // 计算位置
            int row = i / columns;
            int col = i % columns;
            float x = col * (chartSize.x + spacing.x);
            float y = -row * (chartSize.y + spacing.y);
            
            // 实例化图表预制体
            GameObject chartObj = Instantiate(chartPrefab, chartsContainer);
            chartObj.name = $"Chart_Position_{positionIndexes[i]}";
            
            // 设置位置和大小
            RectTransform rectTransform = chartObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = chartSize;
            
            // 获取Line_LocalDB组件
            Line_LocalDB lineDB = chartObj.GetComponent<Line_LocalDB>();
            if (lineDB != null)
            {
                // 设置目标位置
                lineDB.SetTargetPosition(positionIndexes[i]);
                
                // 设置文本引用
                lineDB.potNameText = potNameText;
                lineDB.dateText = dateText;
                
                // 添加到列表
                chartComponents.Add(lineDB);
            }
        }
        
        // 调整容器大小
        int totalRows = (positionIndexes.Count + columns - 1) / columns;
        // 计算实际需要的列数（可能小于设置的columns）
        int actualColumns = Mathf.Min(columns, positionIndexes.Count);
        
        // 计算容器高度和宽度
        float containerHeight = totalRows * (chartSize.y + spacing.y);
        //float containerWidth = actualColumns * (chartSize.x + spacing.x);
        float containerWidth = 750;
        
        // 如果只有一行且图表数量少于列数，减去多余的间距
        if (totalRows == 1 && positionIndexes.Count < columns)
        {
            containerWidth -= spacing.x;
        }
        
        Debug.Log($"调整容器大小: 宽度={containerWidth}, 高度={containerHeight}, 行数={totalRows}, 列数={actualColumns}");
        
        RectTransform containerRect = chartsContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(containerWidth, containerHeight);
    }
    
    // 加载数据
    private void LoadData()
    {
        foreach (var chart in chartComponents)
        {
            chart.LoadData();
        }
    }
}