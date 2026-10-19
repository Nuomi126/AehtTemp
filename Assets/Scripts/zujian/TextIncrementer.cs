using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class TextIncrementer : MonoBehaviour
{
    // 要增加数值的TextMeshPro文本组件
    public TextMeshProUGUI targetText;
    
    // 增加数值的按钮
    public Button incrementButton;
    
    // 引用SlotNumberSelector组件
    public SlotNumberSelector slotSelector;
    
    // 数值改变后的回调事件
    [Header("事件回调")]
    public UnityEvent onValueChanged;
    
    void Start()
    {
        // 检查组件是否已分配
        if (targetText == null)
        {
            Debug.LogError("目标文本组件未分配！");
            return;
        }
        
        if (incrementButton == null)
        {
            Debug.LogError("增加按钮未分配！");
            return;
        }
        
        if (slotSelector == null)
        {
            Debug.LogError("SlotNumberSelector组件未分配！");
            return;
        }
        
        // 添加按钮点击事件
        incrementButton.onClick.AddListener(IncrementValue);

		// 异步加载实际数据 20250804
		StartCoroutine(slotSelector.LoadActualDataAsync());
	}
    
    // 增加数值的方法 - 切换到下一个选项
    public void IncrementValue()
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
            // 如果当前文本不在列表中，设置为第一个选项
            currentIndex = 0;
        }
        else
        {
            // 移动到下一个选项，如果已经是最后一个则循环到第一个
            currentIndex = (currentIndex + 1) % availableSlots.Count;
        }
        
        // 更新文本显示
        targetText.text = availableSlots[currentIndex];
        
        Debug.Log($"槽号递增: {currentText} -> {availableSlots[currentIndex]}");
        
        // 触发回调事件
        onValueChanged?.Invoke();
    }
    
    // 清理事件监听
    void OnDestroy()
    {
        if (incrementButton != null)
        {
            incrementButton.onClick.RemoveListener(IncrementValue);
        }
    }
}