using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XCharts.Example;

public class TodayButtonController : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Button todayButton; // 当日按钮
    [SerializeField] private TextMeshProUGUI dateText; // 日期文本框
    [SerializeField] private MultiSensorTemperatureChartManager chartManager; // 图表管理器
    
    void Start()
    {
        InitializeButton();
    }
    
    private void InitializeButton()
    {
        if (todayButton != null)
        {
            todayButton.onClick.RemoveAllListeners();
            todayButton.onClick.AddListener(OnTodayButtonClicked);
        }
        else
        {
            Debug.LogWarning("当日按钮未分配！请在Inspector中分配todayButton");
        }
    }
    
    /// <summary>
    /// 当日按钮点击事件处理
    /// </summary>
    private void OnTodayButtonClicked()
    {
        Debug.Log("点击了当日按钮");
        
        // 1. 设置日期文本框为当前日期
        SetCurrentDate();
        
        // 2. 执行当前筛选类别的画图功能
        ExecuteCurrentCategoryChart();
    }
    
    /// <summary>
    /// 设置日期文本框为当前日期
    /// </summary>
    private void SetCurrentDate()
    {
        if (dateText != null)
        {
            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            dateText.text = currentDate;
            Debug.Log($"日期已设置为当前日期: {currentDate}");
        }
        else
        {
            Debug.LogWarning("日期文本框未分配！请在Inspector中分配dateText");
        }
    }
    
    /// <summary>
    /// 执行当前筛选类别的画图功能
    /// </summary>
    private void ExecuteCurrentCategoryChart()
    {
        if (chartManager != null)
        {
            // 更新图表管理器的日期（如果需要）
            UpdateChartManagerDate();
            
            // 触发数据加载和图表绘制
            chartManager.LoadTemperatureData();
            
            Debug.Log($"已触发当前筛选类别的画图功能");
        }
        else
        {
            Debug.LogWarning("图表管理器未分配！请在Inspector中分配chartManager");
        }
    }
    
    /// <summary>
    /// 更新图表管理器的日期设置
    /// </summary>
    private void UpdateChartManagerDate()
    {
        if (chartManager != null && dateText != null)
        {
            // 通过反射或公共方法更新图表管理器的选中日期
            // 由于selectedDate是私有字段，我们需要通过dateText来同步
            var dateTextComponent = chartManager.dateText;
            if (dateTextComponent != null)
            {
                dateTextComponent.text = dateText.text;
            }
        }
    }
    
    /// <summary>
    /// 手动设置组件引用（如果需要通过代码设置）
    /// </summary>
    public void SetReferences(Button button, TextMeshProUGUI dateTextComponent, MultiSensorTemperatureChartManager manager)
    {
        todayButton = button;
        dateText = dateTextComponent;
        chartManager = manager;
        
        InitializeButton();
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        if (todayButton != null)
        {
            todayButton.onClick.RemoveAllListeners();
        }
    }
}