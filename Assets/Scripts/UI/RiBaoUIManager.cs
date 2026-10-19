using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 日报界面管理器 - 用于控制“当日报表”和“历史报表”面板的显示与切换
/// </summary>
public class RiBaoUIManager : MonoBehaviour
{
    [Header("切换按钮 (Buttons)")]
    [Tooltip("切换到当日报表的按钮")]
    public Button dangriBtn;    
    
    [Tooltip("切换到历史报表的按钮")]
    public Button lishiBtn;     

    [Header("对应的界面面板 (Panels)")]
    [Tooltip("当日报表的UI根节点")]
    public GameObject dangriPanel;  
    
    [Tooltip("历史报表的UI根节点")]
    public GameObject lishiPanel;   

    private void Start()
    {
        // 绑定按钮点击事件
        if (dangriBtn != null)
        {
            dangriBtn.onClick.AddListener(ShowDangriPanel);
        }
        else
        {
            Debug.LogWarning("[RiBaoUIManager] 当日报表按钮(dangriBtn)未赋值！");
        }
            
        if (lishiBtn != null)
        {
            lishiBtn.onClick.AddListener(ShowLishiPanel);
        }
        else
        {
            Debug.LogWarning("[RiBaoUIManager] 历史报表按钮(lishiBtn)未赋值！");
        }

        // 默认初始化时显示“当日报表”面板，隐藏“历史报表”
        ShowDangriPanel();
    }

    /// <summary>
    /// 显示当日报表
    /// </summary>
    public void ShowDangriPanel()
    {
        if (dangriPanel != null) dangriPanel.SetActive(true);
        if (lishiPanel != null) lishiPanel.SetActive(false);
        
        Debug.Log("[RiBaoUIManager] 切换到了：当日报表界面");
        
        // TODO: 如果需要，可以在这里触发获取当日数据的接口
        // if (RiBaoManager.Instance != null) RiBaoManager.Instance.RequestDailyReport(...);
    }

    /// <summary>
    /// 显示历史报表
    /// </summary>
    public void ShowLishiPanel()
    {
        if (dangriPanel != null) dangriPanel.SetActive(false);
        if (lishiPanel != null) lishiPanel.SetActive(true);
        
        Debug.Log("[RiBaoUIManager] 切换到了：历史报表界面");
        
        // TODO: 如果需要，可以在这里触发获取历史数据的接口
    }

    private void OnDestroy()
    {
        // 组件销毁时移除监听，防止内存泄漏
        if (dangriBtn != null) dangriBtn.onClick.RemoveListener(ShowDangriPanel);
        if (lishiBtn != null) lishiBtn.onClick.RemoveListener(ShowLishiPanel);
    }
}
