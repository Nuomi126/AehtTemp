using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlwitDataExample : MonoBehaviour
{
    [Header("UI引用")]
    public TextMeshProUGUI dataDisplayText;
    public TextMeshProUGUI voltageText;
    public TextMeshProUGUI currentText;
    
    [Header("API参数")]
    public string enterpriseId = "1033";
    public string tankId = "1001";
    
    [Header("状态")]
    public bool autoRefresh = false;
    public float refreshInterval = 5f;
    
    private AlwitDataService dataService;
    private float refreshTimer = 0f;
    private bool isRequestInProgress = false;
    
    void Start()
    {
        // 检查UI组件
        if (dataDisplayText == null)
        {
            Debug.LogWarning("数据显示文本未设置");
        }
        
        if (voltageText == null)
        {
            Debug.LogWarning("电压显示文本未设置");
        }
        
        if (currentText == null)
        {
            Debug.LogWarning("电流显示文本未设置");
        }
        
        // 创建服务实例
        try
        {
            dataService = new AlwitDataService();
            
            // 注册事件
            dataService.OnDataReceived += OnDataReceived;
            dataService.OnError += OnApiError;
            
            Debug.Log("AlwitDataService 初始化成功");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"初始化 AlwitDataService 失败: {ex.Message}");
        }
    }
    
    void Update()
    {
        // 如果启用了自动刷新，则定时获取数据
        if (autoRefresh && !isRequestInProgress)
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                GetMainInfo();
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消注册事件
        if (dataService != null)
        {
            dataService.OnDataReceived -= OnDataReceived;
            dataService.OnError -= OnApiError;
        }
    }
    
    // 获取主界面实时信息
    public void GetMainInfo()
    {
        if (isRequestInProgress)
        {
            Debug.Log("已有请求正在进行中，请稍后再试");
            return;
        }
        
        try
        {
            isRequestInProgress = true;
            
            if (dataDisplayText != null)
            {
                dataDisplayText.text = "正在获取数据...";
            }
            
            dataService.GetAlwitData(enterpriseId, AlwitDataService.MAIN_INFO, tankId);
        }
        catch (System.Exception ex)
        {
            isRequestInProgress = false;
            Debug.LogError($"发送请求时出错: {ex.Message}");
            
            if (dataDisplayText != null)
            {
                dataDisplayText.text = $"请求错误: {ex.Message}";
            }
        }
    }
    
    // 获取单槽温度信息
    public void GetTankTemperature()
    {
        if (isRequestInProgress)
        {
            Debug.Log("已有请求正在进行中，请稍后再试");
            return;
        }
        
        try
        {
            isRequestInProgress = true;
            
            if (dataDisplayText != null)
            {
                dataDisplayText.text = "正在获取温度数据...";
            }
            
            dataService.GetAlwitData(enterpriseId, AlwitDataService.TANK_TEMP_INFO, tankId);
        }
        catch (System.Exception ex)
        {
            isRequestInProgress = false;
            Debug.LogError($"发送请求时出错: {ex.Message}");
            
            if (dataDisplayText != null)
            {
                dataDisplayText.text = $"请求错误: {ex.Message}";
            }
        }
    }
    
    // 数据接收处理
    private void OnDataReceived(byte[] data)
    {
        isRequestInProgress = false;
        
        // 显示原始数据
        if (dataDisplayText != null)
        {
            dataDisplayText.text = AlwitDataService.ByteArrayToString(data);
        }
        
        // 提取并显示电压
        if (voltageText != null)
        {
            float voltage = AlwitDataService.ExtractVoltage(data);
            voltageText.text = $"电压: {voltage:F1} V";
        }
        
        // 提取并显示电流
        if (currentText != null)
        {
            float current = AlwitDataService.ExtractCurrent(data);
            currentText.text = $"电流: {current:F1} kA";
        }
    }
    
    // 错误处理
    private void OnApiError(string errorMessage)
    {
        isRequestInProgress = false;
        Debug.LogError($"API错误: {errorMessage}");
        
        if (dataDisplayText != null)
        {
            dataDisplayText.text = $"获取数据失败: {errorMessage}";
        }
    }
    
    // 切换自动刷新
    public void ToggleAutoRefresh()
    {
        autoRefresh = !autoRefresh;
        Debug.Log($"自动刷新已{(autoRefresh ? "开启" : "关闭")}");
    }
}