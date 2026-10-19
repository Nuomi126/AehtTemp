using UnityEngine;
using UnityEngine.UI;

public abstract class UIController<T, S> : MonoBehaviour 
    where T : class 
    where S : ApiService<T>, new()
{
    // API服务
    protected S apiService;
    
    // 刷新按钮
    [SerializeField] protected Button refreshButton;
    
    // 加载指示器
    [SerializeField] protected GameObject loadingIndicator;
    
    // 错误提示文本
    [SerializeField] protected Text errorText;
    
    protected virtual void Awake()
    {
        apiService = new S();
    }
    
    protected virtual void Start()
    {
        // 注册事件
        apiService.OnDataReceived += UpdateUI;
        apiService.OnError += ShowError;
        
        // 注册按钮点击事件
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }
        
        // 初始加载数据
        FetchData();
    }
    
    protected virtual void OnDestroy()
    {
        // 取消注册事件
        apiService.OnDataReceived -= UpdateUI;
        apiService.OnError -= ShowError;
    }
    
    // 刷新按钮点击事件
    protected virtual void OnRefreshButtonClicked()
    {
        FetchData();
    }
    
    // 获取数据
    protected virtual void FetchData()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        if (errorText != null)
            errorText.gameObject.SetActive(false);
            
        apiService.FetchData();
    }
    
    // 显示错误
    protected virtual void ShowError(string errorMessage)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        if (errorText != null)
        {
            errorText.text = $"错误: {errorMessage}";
            errorText.gameObject.SetActive(true);
        }
        
        Debug.LogError($"API错误: {errorMessage}");
    }
    
    // 更新UI
    protected abstract void UpdateUI(T data);
}