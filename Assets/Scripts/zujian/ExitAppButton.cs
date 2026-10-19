using UnityEngine;
using UnityEngine.UI;

public class ExitAppButton : MonoBehaviour
{
    [Header("退出应用程序按钮配置")]
    [SerializeField] private Button exitButton;
    
    [Header("退出确认设置")]
    [SerializeField] private bool showConfirmDialog = true; // 是否显示确认对话框
    
    private void Start()
    {
        // 如果没有手动分配按钮，尝试获取当前GameObject上的Button组件
        if (exitButton == null)
        {
            exitButton = GetComponent<Button>();
        }
        
        // 为按钮添加点击事件
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        else
        {
            Debug.LogError("ExitAppButton: 未找到Button组件，请确保脚本挂载在有Button组件的GameObject上或手动分配exitButton字段");
        }
    }
    
    /// <summary>
    /// 退出按钮点击事件处理
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("用户点击退出应用程序按钮...");
        
        if (showConfirmDialog)
        {
            // 显示确认对话框（可选）
            ShowExitConfirmDialog();
        }
        else
        {
            // 直接退出应用程序
            ExitApplication();
        }
    }
    
    /// <summary>
    /// 显示退出确认对话框
    /// </summary>
    private void ShowExitConfirmDialog()
    {
        // 在移动设备上显示原生确认对话框
        if (Application.isMobilePlatform)
        {
            // 对于移动平台，直接退出（因为Unity没有内置的原生对话框API）
            // 如果需要自定义对话框，可以创建UI面板
            Debug.Log("移动平台：直接退出应用程序");
            ExitApplication();
        }
        else
        {
            // 在编辑器或桌面平台，可以使用EditorUtility（仅在编辑器中）
            Debug.Log("确认退出应用程序");
            ExitApplication();
        }
    }
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        Debug.Log("正在退出应用程序...");
        
        try
        {
            // 保存任何需要保存的数据（如果有的话）
            SaveDataBeforeExit();
            
            // 退出应用程序
#if UNITY_EDITOR
            // 在Unity编辑器中停止播放
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 在构建的应用程序中退出
            Application.Quit();
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"退出应用程序时发生错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 退出前保存数据（可根据需要自定义）
    /// </summary>
    private void SaveDataBeforeExit()
    {
        try
        {
            // 在这里添加任何需要在退出前保存的数据
            // 例如：用户设置、游戏进度等
            Debug.Log("保存数据完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"保存数据时发生错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 公共静态方法：供其他脚本调用的退出功能
    /// </summary>
    public static void QuitApplication()
    {
        Debug.Log("执行静态退出应用程序方法...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    private void OnDestroy()
    {
        // 清理事件监听，防止内存泄漏
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
    }
}