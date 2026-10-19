using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TextDecrementer : MonoBehaviour
{
    // 要减少数值的TextMeshPro文本组件
    public TextMeshProUGUI targetText;
    
    // 减少数值的按钮
    public Button decrementButton;
    
    // 引用SlotNumberSelector组件
    public SlotNumberSelector slotSelector;
    
    // 添加一个UnityEvent，可以在Inspector中配置要执行的方法
    [System.Serializable]
    public class OnValueChangedEvent : UnityEngine.Events.UnityEvent { }
    
    [Header("事件回调")]
    public OnValueChangedEvent onValueChanged;
    
    void Start()
    {
        // 检查组件是否已分配
        if (targetText == null)
        {
            Debug.LogError("目标文本组件未分配！");
            return;
        }
        
        if (decrementButton == null)
        {
            Debug.LogError("减少按钮未分配！");
            return;
        }
        
        if (slotSelector == null)
        {
            Debug.LogError("SlotNumberSelector组件未分配！");
            return;
        }
        
        // 添加按钮点击事件
        decrementButton.onClick.AddListener(DecrementValue);

		// 异步加载实际数据 20250804
		StartCoroutine(slotSelector.LoadActualDataAsync());
	}
    
    // 减少数值的方法 - 切换到上一个选项
    public void DecrementValue()
    {
        if (slotSelector == null) return;
        
        // 获取当前可用的槽号列表
        var availableSlots = slotSelector.GetSlotNumberItems();
        if (availableSlots == null || availableSlots.Count == 0)
        {
            Debug.LogWarning("没有可用的槽号，可能数据还未加载完成");
            return;
        }
        
        // 找到当前文本在列表中的位置
        string currentText = targetText.text;
        int currentIndex = slotSelector.GetSlotNumberIndex(currentText);
        
        if (currentIndex == -1)
        {
            // 如果当前文本不在列表中，设置为最后一个选项
            currentIndex = availableSlots.Count - 1;
        }
        else
        {
            // 移动到上一个选项，如果已经是第一个则循环到最后一个
            currentIndex = currentIndex - 1;
            if (currentIndex < 0)
            {
                currentIndex = availableSlots.Count - 1;
            }
        }
        
        // 更新文本显示
        targetText.text = availableSlots[currentIndex];
        
        Debug.Log($"槽号递减: {currentText} -> {availableSlots[currentIndex]}");
        
        // 触发回调事件
        onValueChanged?.Invoke();
    }
    
    // 在组件销毁时移除事件监听
    private void OnDestroy()
    {
        if (decrementButton != null)
        {
            decrementButton.onClick.RemoveListener(DecrementValue);
        }
    }
}