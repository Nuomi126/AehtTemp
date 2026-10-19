using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class WorkAreaSelector : MonoBehaviour
{
    // UI组件
    public GameObject selectorPanel;
    public Button cancelButton;
    public Button confirmButton;
    public ScrollRect itemScrollRect;
    public TextMeshProUGUI selectedDisplayText; // 主UI上显示选中项的文本
    
    // 公开事件：车间工区选择确认时触发
    public UnityEvent<string> OnWorkAreaConfirmedEvent;

    // 数据存储
    private List<string> workAreaItems = new List<string>(); // 工区选项列表
    private string selectedWorkArea;
    private float itemHeight = 100f; // 列表中每个项目的高度
    private RectTransform itemContentRect;

    void Start()
    {
		// 获取当前激活场景的名称
		string sceneName = SceneManager.GetActiveScene().name;
        //UnityEngine.Debug.Log("当前场景名称: " + sceneName);
        if (sceneName != "yujingxingxi" && selectorPanel != null)
        { 
             // 设置UI行为
             selectorPanel.SetActive(false);
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelection);
        
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        // 设置内容矩形以便于引用
        if (itemScrollRect != null && itemScrollRect.content != null)
        {
            itemContentRect = itemScrollRect.content.GetComponent<RectTransform>();
        }
    
        // 关键改进：在Start中就预加载数据和初始化UI
        PreloadDataAndInitializeUI();

        //设置默认工区号
        selectedWorkArea = "1车间1工区";

        if (selectedDisplayText != null)
        {
            selectedDisplayText.text = selectedWorkArea;
        }
    }
    

    // 添加静态引用，用于跟踪当前激活的选择器
    private static GameObject currentActiveSelector;
    
    // 添加面板状态检查
    private bool isPanelInitialized = false;
    private bool isDataLoaded = false;
    private void PreloadDataAndInitializeUI()
    {
        LoadWorkAreaDataFromGlobal();
    
        if (workAreaItems.Count > 0)
        {
            selectedWorkArea = workAreaItems[0];
            PopulateScrollRect(itemScrollRect, workAreaItems);
            isPanelInitialized = true;
            isDataLoaded = true;
        }
    }
    // 触发面板显示的方法
    public void OpenSelectorPanel()
    {
	    // 创建Stopwatch实例并开始计时
	    Stopwatch stopwatch = Stopwatch.StartNew();
		// 如果有其他选择器处于激活状态，先关闭它
		if (currentActiveSelector != null && currentActiveSelector != selectorPanel)
        {
            currentActiveSelector.SetActive(false);
        }
    
        // 检查面板是否已经激活，避免重复点击
        if (selectorPanel != null && selectorPanel.activeInHierarchy)
        {
		    UnityEngine.Debug.Log("工区选择器面板已经激活，忽略重复点击");
            return;
        }
    
        // 如果数据未加载，重新加载
        if (!isDataLoaded)
        {
            PreloadDataAndInitializeUI();
        }
    
        // 激活面板
        if (selectorPanel != null)
        {
            selectorPanel.SetActive(true);
            currentActiveSelector = selectorPanel;
        }

	    UnityEngine.Debug.Log("工区选择器面板已激活");

	    // 停止计时
	    stopwatch.Stop();
	    // 输出执行时间（毫秒）
	    UnityEngine.Debug.Log($"WorkAreaSelector OpenSelectorPanel()执行时间: {stopwatch.Elapsed.TotalSeconds} 秒");
    }
    
    // 修改数据加载方法，移除协程
    private void LoadWorkAreaDataFromGlobal()
    {
		UnityEngine.Debug.Log("开始从全局数据加载工区信息");
        
        workAreaItems.Clear();
        
        // 检查全局工区数据是否存在
        if (GlobalUserData.WorkAreaList != null && GlobalUserData.WorkAreaList.Count > 0)
        {
			UnityEngine.Debug.Log($"从GlobalUserData.WorkAreaList加载工区数据，数量: {GlobalUserData.WorkAreaList.Count}");
            
            foreach (var workArea in GlobalUserData.WorkAreaList)
            {
                string displayText = workArea.WorkAreaName;
                workAreaItems.Add(displayText);
				UnityEngine.Debug.Log($"添加工区: {displayText}");
            }
        }
        else
        {
			UnityEngine.Debug.LogWarning("全局工区数据为空，使用默认数据");
            workAreaItems = new List<string> { "1车间1工区", "1车间2工区", "1车间3工区", "2车间1工区", "2车间2工区", "2车间3工区" };
        }

		UnityEngine.Debug.Log($"工区数据加载完成，总数: {workAreaItems.Count}");
    }

    // 更新UI
    private void UpdateUI()
    {
		UnityEngine.Debug.Log("开始更新UI，工区选项数量: " + workAreaItems.Count);

        // 如果没有数据，添加默认选项
        if (workAreaItems.Count == 0)
        {
			UnityEngine.Debug.LogError("工区选项列表为空，添加默认选项");
            workAreaItems.Add("默认工区");
        }

        // 初始化选择
        selectedWorkArea = workAreaItems[0];
		UnityEngine.Debug.Log("已设置选中工区: " + selectedWorkArea);

        // 填充ScrollRect选项
        PopulateScrollRect(itemScrollRect, workAreaItems);
        
        // 初始化显示文本
        UpdateDisplayText();
    }

    void PopulateScrollRect(ScrollRect scrollRect, List<string> data)
    {
        // 获取ScrollRect的内容区域
        Transform content = scrollRect.content;
        foreach (Transform child in content)
        {
            Destroy(child.gameObject); // 清除之前的项目
        }

        // 为每个选项创建UI项目
        foreach (string item in data)
        {
            GameObject option = new GameObject("WorkAreaOption");
            option.AddComponent<RectTransform>(); // 确保它有一个RectTransform用于布局

            // 添加Text组件以显示文本
            Text optionText = option.AddComponent<Text>();
            optionText.text = item;
            optionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 传统字体
            optionText.fontSize = 40;
            optionText.alignment = TextAnchor.MiddleCenter;
            optionText.color = item == selectedWorkArea ? Color.red : Color.black; // 如果是当前选中项则显示为红色

            // 设置Text元素的大小
            RectTransform rectTransform = option.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, itemHeight); // 调整高度
            rectTransform.localScale = Vector3.one;

            // 为每个选项添加Button
            Button button = option.AddComponent<Button>();
            button.onClick.AddListener(() => OnWorkAreaSelected(item));

            // 将选项设置为ScrollRect内容的子项
            option.transform.SetParent(content, false);

            // 添加LayoutGroup进行间距设置
            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.spacing = 10;
            }
        }

        // 确保内容区域正确调整大小
        ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void OnWorkAreaSelected(string workArea)
    {
        selectedWorkArea = workArea;
        UpdateDisplayText();
        UpdateItemColors(); // 更新所有选项的颜色
		UnityEngine.Debug.Log($"选择了工区: {workArea}");
    }

    // 更新所有选项的颜色
    void UpdateItemColors()
    {
        // 遍历所有选项并更新颜色
        foreach (Transform child in itemScrollRect.content)
        {
            Text optionText = child.GetComponent<Text>();
            if (optionText != null)
            {
                optionText.color = optionText.text == selectedWorkArea ? Color.red : Color.black;
            }
        }
    }

    // 移除滚动选择相关方法
    /*
    void OnItemScrollChanged(Vector2 scrollPosition)
    {
        // 滚动区域的滚动位置变化逻辑
        string previousSelected = selectedWorkArea;
        SelectItemInScroll(itemContentRect, ref selectedWorkArea);
        
        // 如果选择发生变化，更新颜色
        if (previousSelected != selectedWorkArea)
        {
            UpdateItemColors();
        }
        
        UpdateDisplayText();
    }

    void SelectItemInScroll(RectTransform contentRect, ref string selectedItem)
    {
        // 计算滚动区域的中心
        float contentHeight = contentRect.rect.height;
        float centerY = contentHeight / 2;

        // 遍历项目并找到最接近中心的项目
        foreach (Transform child in contentRect)
        {
            RectTransform itemRect = child.GetComponent<RectTransform>();
            float itemCenterY = itemRect.anchoredPosition.y + itemRect.rect.height / 2;

            // 如果项目最接近中心，则选择它
            if (Mathf.Abs(itemCenterY - centerY) < itemHeight / 2)
            {
                selectedItem = child.GetComponent<Text>().text;
                break;
            }
        }
    }
    */

    void UpdateDisplayText()
    {
        // 只在选择器面板中更新预览文本，不更改主UI上的显示文本
        // 这里可以添加一个预览文本组件，如果需要的话
    }

    void CancelSelection()
    {
        selectorPanel.SetActive(false); // 隐藏选择面板
		UnityEngine.Debug.Log("取消工区选择");
    }

    void ConfirmSelection()
    {
        // 只有在确认时才更新主UI上的显示文本
        if (selectedDisplayText != null)
        {
            selectedDisplayText.text = selectedWorkArea;
        }
        selectorPanel.SetActive(false); // 确认后隐藏选择面板

		UnityEngine.Debug.Log($"确认选择工区: {selectedWorkArea}");
        
        // 这里可以添加代码，将选择结果传递给其他系统
        // 例如：通知其他组件工区已选择
        OnWorkAreaConfirmed(selectedWorkArea);
    }
    
    // 工区确认选择的回调方法
    private void OnWorkAreaConfirmed(string workArea)
    {
		// 可以在这里添加工区选择确认后的逻辑
		// 例如：保存到PlayerPrefs、通知其他系统等
		UnityEngine.Debug.Log($"工区选择已确认: {workArea}");
		
		// 触发公开事件，通知其他组件
		OnWorkAreaConfirmedEvent?.Invoke(workArea);
    }
    
    /// <summary>
    /// 获取当前选中的车间号
    /// </summary>
    public int GetSelectedWorkshopNumber()
    {
        if (string.IsNullOrEmpty(selectedWorkArea)) return 1;
        
        // 解析格式："1车间1工区"
        var match = System.Text.RegularExpressions.Regex.Match(selectedWorkArea, @"(\d+)车间");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int workshopNum))
        {
            return workshopNum;
        }
        return 1;
    }
    
    /// <summary>
    /// 获取当前选中的工区号
    /// </summary>
    public int GetSelectedWorkAreaNumber()
    {
        if (string.IsNullOrEmpty(selectedWorkArea)) return 1;
        
        // 解析格式："1车间1工区"
        var match = System.Text.RegularExpressions.Regex.Match(selectedWorkArea, @"(\d+)工区");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int workAreaNum))
        {
            return workAreaNum;
        }
        return 1;
    }
    
    // 获取当前选择的工区
    public string GetSelectedWorkArea()
    {
        return selectedWorkArea;
    }
    
    // 手动刷新工区数据（如果全局数据更新了）
    public void RefreshWorkAreaData()
    {
		UnityEngine.Debug.Log("手动刷新工区数据");
        LoadWorkAreaDataFromGlobal();
    }
    
    // 设置工区选项列表（备用方法）
    public void SetWorkAreaItems(List<string> newItems)
    {
        if (newItems != null && newItems.Count > 0)
        {
            workAreaItems = new List<string>(newItems);
            selectedWorkArea = workAreaItems[0];
            PopulateScrollRect(itemScrollRect, workAreaItems);
            UpdateDisplayText();
			UnityEngine.Debug.Log($"手动设置工区选项，数量: {newItems.Count}");
        }
    }
}