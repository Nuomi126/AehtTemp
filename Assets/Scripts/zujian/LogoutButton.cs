using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogoutButton : MonoBehaviour
{
    [Header("退出按钮配置")]
    [SerializeField] private Button logoutButton;
    
    private void Start()
    {
        // 如果没有手动分配按钮，尝试获取当前GameObject上的Button组件
        if (logoutButton == null)
        {
            logoutButton = GetComponent<Button>();
        }
        
        // 为按钮添加点击事件
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);
        }
        else
        {
            Debug.LogError("LogoutButton: 未找到Button组件，请确保脚本挂载在有Button组件的GameObject上或手动分配logoutButton字段");
        }
    }
    
    /// <summary>
    /// 退出按钮点击事件处理
    /// </summary>
    public void OnLogoutButtonClicked()
    {
        Debug.Log("用户点击退出按钮，开始执行退出流程...");
        
        // 1. 清空全局用户数据
        ClearUserData();
        
        // 2. 跳转到登录页面（System场景）
        LoadLoginScene();
    }
    
    /// <summary>
    /// 清空用户临时数据
    /// </summary>
    private void ClearUserData()
    {
        try
        {
            // 调用GlobalUserData的清空方法
            GlobalUserData.ClearUserData();
            
            Debug.Log("用户数据已清空：");
            Debug.Log($"- UserId: {GlobalUserData.UserId}");
            Debug.Log($"- AlSerialPantId: '{GlobalUserData.AlSerialPantId}'");
            Debug.Log($"- AlSerialPantIdName: '{GlobalUserData.AlSerialPantIdName}'");
            Debug.Log($"- WorkshopList Count: {GlobalUserData.WorkshopList?.Count ?? 0}");
            Debug.Log($"- WorkAreaList Count: {GlobalUserData.WorkAreaList?.Count ?? 0}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"清空用户数据时发生错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 跳转到登录场景
    /// </summary>
    private void LoadLoginScene()
    {
        try
        {
            Debug.Log("正在跳转到System场景（登录页面）...");
            SceneManager.LoadScene("System");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"跳转到登录场景时发生错误: {ex.Message}");
            Debug.LogError("请确保Scene Build Settings中包含名为'System'的场景");
        }
    }
    
    /// <summary>
    /// 公共方法：供其他脚本调用的退出功能
    /// </summary>
    public static void Logout()
    {
        Debug.Log("执行静态退出方法...");
        
        // 清空用户数据
        GlobalUserData.ClearUserData();
        
        // 跳转到登录场景
        SceneManager.LoadScene("System");
    }
    
    private void OnDestroy()
    {
        // 清理事件监听，防止内存泄漏
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnLogoutButtonClicked);
        }
    }
}