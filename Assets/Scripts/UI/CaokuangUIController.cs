using UnityEngine;
using UnityEngine.UI;

public class CaokuangUIController : MonoBehaviour
{
    [Header("顶部切换按钮")]
    public Button dangriTabBtn; // 当日报表按钮
    public Button lishiTabBtn;  // 历史趋势按钮

    [Header("功能面板")]
    public GameObject dangriPanel; // dangribaobiao 面板
    public GameObject lishiPanel;  // lishiqushi 面板

    void Start()
    {
        // 绑定按钮点击事件
        if (dangriTabBtn != null) 
            dangriTabBtn.onClick.AddListener(ShowDangriPanel);
            
        if (lishiTabBtn != null) 
            lishiTabBtn.onClick.AddListener(ShowLishiPanel);
        
        // 默认进入场景时显示“当日报表”
        ShowDangriPanel();
    }

    private void ShowDangriPanel()
    {
        if (dangriPanel != null) dangriPanel.SetActive(true);
        if (lishiPanel != null) lishiPanel.SetActive(false);
    }

    private void ShowLishiPanel()
    {
        if (dangriPanel != null) dangriPanel.SetActive(false);
        if (lishiPanel != null) lishiPanel.SetActive(true);
    }
    
    private void OnDestroy()
    {
        // 移除事件防止内存泄漏
        if (dangriTabBtn != null) dangriTabBtn.onClick.RemoveListener(ShowDangriPanel);
        if (lishiTabBtn != null) lishiTabBtn.onClick.RemoveListener(ShowLishiPanel);
    }
}