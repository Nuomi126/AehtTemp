// TempLineChartUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XCharts.Example;

public class TempLineChartUIController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button tempLineButton; // 温度曲线按钮
    
    [Header("曲线图引用")]
    [SerializeField] private GameObject tempLineObject; // 温度曲线对象
    
    [Header("筛选控件")]
    [SerializeField] private TextMeshProUGUI potNameText; // 电解槽文本
    [SerializeField] private TextMeshProUGUI dateText;    // 日期文本
    
    // 曲线图组件引用 - 修改为使用MultiSensorTemperatureChartManager
    private MultiSensorTemperatureChartManager tempLineChart;
    
    private void Start()
    {
        // 获取曲线图组件引用
        if (tempLineObject != null)
            tempLineChart = tempLineObject.GetComponent<MultiSensorTemperatureChartManager>();
        
        // 初始隐藏温度曲线图
        if (tempLineObject != null)
            tempLineObject.SetActive(false);
        
        // 注册按钮点击事件
        if (tempLineButton != null)
            tempLineButton.onClick.AddListener(OnTempLineButtonClicked);
            
        // 初始化文本引用
        InitTextReferences();
    }
    
    // 初始化文本引用
    private void InitTextReferences()
    {
        // 如果温度曲线组件存在，则传递文本引用
        if (tempLineChart != null)
        {
           // 将控制器中的文本引用传递给温度曲线组件
            if (potNameText != null && tempLineChart.potNameText == null)
                tempLineChart.potNameText = potNameText;
                
            if (dateText != null && tempLineChart.dateText == null)
                tempLineChart.dateText = dateText;
        }
    }
    
    private void OnDestroy()
    {
        // 取消注册按钮事件
        if (tempLineButton != null)
            tempLineButton.onClick.RemoveListener(OnTempLineButtonClicked);
    }
    
    // 温度曲线按钮点击事件 - 修改为使用MultiSensorTemperatureChartManager
    private void OnTempLineButtonClicked()
    {
        // 显示温度曲线
        if (tempLineObject != null)
        {
            tempLineObject.SetActive(true);
            
            // 加载数据
            if (tempLineChart != null)
            {
                // 添加短暂延迟确保对象已完全激活后加载数据
                StartCoroutine(LoadDataAfterDelay());
            }
        }
    }
    
    // 添加延迟加载数据的协程
    private System.Collections.IEnumerator LoadDataAfterDelay()
    {
        // 等待一帧，确保对象已完全激活
        yield return null;
        
        // 再次确认对象是激活的
        if (tempLineObject != null && tempLineObject.activeInHierarchy && tempLineChart != null)
        {
            // 调用加载数据方法
            tempLineChart.LoadTemperatureData();
        }
        else
        {
            Debug.LogWarning("温度曲线对象未激活或组件为空，无法加载数据");
        }
    }
    
    // 公共方法，可以从其他脚本调用来显示温度曲线并加载数据
    public void ShowTempLineChart()
    {
        OnTempLineButtonClicked();
    }
    
    // 公共方法，可以从其他脚本调用来刷新温度曲线数据
    public void RefreshTempLineChart()
    {
        // 确保温度曲线对象是激活的
        if (tempLineObject != null)
        {
            if (!tempLineObject.activeInHierarchy)
                tempLineObject.SetActive(true);
                
            StartCoroutine(LoadDataAfterDelay());
        }
    }
}