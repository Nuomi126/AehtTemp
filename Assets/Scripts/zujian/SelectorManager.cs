using UnityEngine;

// 静态管理器类，用于管理所有选择器
public static class SelectorManager
{
    // 当前激活的选择器
    private static GameObject activeSelector;
    
    // 设置当前激活的选择器
    public static void SetActiveSelector(GameObject selector)
    {
        // 如果有其他选择器已激活，先关闭它
        if (activeSelector != null && activeSelector != selector)
        {
            activeSelector.SetActive(false);
            Debug.Log($"关闭选择器: {activeSelector.name}");
        }
        
        // 更新当前激活的选择器
        activeSelector = selector;
        Debug.Log($"设置当前激活选择器: {selector.name}");
    }
    
    // 关闭当前激活的选择器
    public static void CloseActiveSelector()
    {
        if (activeSelector != null)
        {
            activeSelector.SetActive(false);
            Debug.Log($"关闭选择器: {activeSelector.name}");
            activeSelector = null;
        }
    }
}