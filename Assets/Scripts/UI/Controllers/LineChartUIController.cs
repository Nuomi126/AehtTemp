// LineChartUIController.cs
using UnityEngine;
using UnityEngine.UI;

public class LineChartUIController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button voltageLineButton; // 电压曲线按钮
    [SerializeField] private Button effectLineButton;  // 效应曲线按钮
    
    [Header("曲线图引用")]
    [SerializeField] private GameObject voltageLineObject; // 电压曲线对象
    [SerializeField] private GameObject effectLineObject;  // 效应曲线对象

    [SerializeField] private TMPro.TextMeshProUGUI gongchangText;
    [SerializeField] private TMPro.TextMeshProUGUI gongquText;
    
    // 曲线图组件引用
    // 修改为正确的类型名称，根据您的项目可能是LineChart或其他类型
    private MonoBehaviour voltageLine;
    private MonoBehaviour effectLine;
    
    private void Start()
    {
        // 获取曲线图组件引用，使用GetComponent<>()但不指定具体类型
        if (voltageLineObject != null)
            voltageLine = voltageLineObject.GetComponent<MonoBehaviour>();
            
        if (effectLineObject != null)
            effectLine = effectLineObject.GetComponent<MonoBehaviour>();
        
        // 初始隐藏所有曲线图
        if (voltageLineObject != null)
            voltageLineObject.SetActive(false);
            
        if (effectLineObject != null)
            effectLineObject.SetActive(false);
        
        // 注册按钮点击事件
        if (voltageLineButton != null)
            voltageLineButton.onClick.AddListener(OnVoltageLineButtonClicked);
            
        if (effectLineButton != null)
            effectLineButton.onClick.AddListener(OnEffectLineButtonClicked);
            
        // 添加搜索按钮事件监听
        AddSearchButtonListener();
    }
    
    private void OnSearchButtonClicked()
    {
        DailyReportManager.Instance.FetchDailyReportsWithTMPParams(gongchangText, gongquText);
    }
    
    private void OnDestroy()
    {
        // 取消注册按钮事件
        if (voltageLineButton != null)
            voltageLineButton.onClick.RemoveListener(OnVoltageLineButtonClicked);
            
        if (effectLineButton != null)
            effectLineButton.onClick.RemoveListener(OnEffectLineButtonClicked);
            
        if (searchButton != null)
            searchButton.onClick.RemoveListener(OnSearchButtonClicked);
    }
    
    // 电压曲线按钮点击事件
    private void OnVoltageLineButtonClicked()
    {
        // 先获取最新数据
        if (DailyReportManager.Instance != null)
        {
            DailyReportManager.Instance.FetchDailyReports();
        }
        
        // 显示电压曲线，隐藏效应曲线
        if (voltageLineObject != null)
        {
            voltageLineObject.SetActive(true);
            // 如果需要刷新曲线数据，可以使用SendMessage调用刷新方法
            voltageLineObject.SendMessage("RefreshChart", null, SendMessageOptions.DontRequireReceiver);
        }
            
        if (effectLineObject != null)
            effectLineObject.SetActive(false);
    }
    
    // 效应曲线按钮点击事件
    private void OnEffectLineButtonClicked()
    {
        // 先获取最新数据
        if (DailyReportManager.Instance != null)
        {
            DailyReportManager.Instance.FetchDailyReports();
        }
        
        // 显示效应曲线，隐藏电压曲线
        if (effectLineObject != null)
        {
            effectLineObject.SetActive(true);
            // 如果需要刷新曲线数据，可以使用SendMessage调用刷新方法
            effectLineObject.SendMessage("RefreshChart", null, SendMessageOptions.DontRequireReceiver);
        }
            
        if (voltageLineObject != null)
            voltageLineObject.SetActive(false);
    }
    
    // 添加搜索按钮
    [SerializeField] private Button searchButton;
    
    // 在Start方法中添加搜索按钮事件监听
    private void AddSearchButtonListener()
    {
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchButtonClicked);
        }
    }
}