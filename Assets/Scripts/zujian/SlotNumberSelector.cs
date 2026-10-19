using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events; // Add this

public class SlotNumberSelector : MonoBehaviour
{
    // UI组件
    public GameObject selectorPanel;
    public Button cancelButton;
    public Button confirmButton;
    public ScrollRect itemScrollRect;
    public TextMeshProUGUI selectedDisplayText; // 主UI上显示选中项的文本
    
    // 用于获取车间号和工区号的文本框引用（现在只需要一个）
    public TextMeshProUGUI workshopWorkAreaDisplayText; // 车间工区显示文本（格式："1车间1工区"）

    // 事件
    public UnityEvent<string> OnSlotConfirmed; // Add this event

    // 数据存储
    private List<string> slotNumberItems = new List<string>(); // 槽号选项列表
    private string selectedSlotNumber;
    private float itemHeight = 100f; // 列表中每个项目的高度
    private RectTransform itemContentRect;
    
    // 当前筛选条件
    private int currentWorkshopNumber = -1;
    private int currentWorkAreaNumber = -1;

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
        //selectedDisplayText.text = "12345";

        // 设置内容矩形以便于引用
        if (itemScrollRect != null && itemScrollRect.content != null)
        {
            itemContentRect = itemScrollRect.content.GetComponent<RectTransform>();
        }
        //LoadSlotNumberDataFromGlobal();
        //string temp = $"{currentWorkshopNumber}车间{currentWorkAreaNumber}工区";
        //UpdateDisplayText(temp, slotNumberItems[0]);
        // 关键改进：预设默认数据，避免第一次点击空白
        InitializeDefaultData();
        // 可能在上面把自身面板隐藏了，隐藏后不能再启动协程
        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            StartCoroutine(InitializeDefaultsFromData());  // 数据准备好后设置真正默认值
        }
		//selectedDisplayText.text = "请先选择车间工区";

		// 异步加载实际数据
		//StartCoroutine(LoadActualDataAsync());
		
		// 如果数据已经加载，直接刷新
		if (GlobalElectrolyticCellDataManager.Instance != null && 
			GlobalElectrolyticCellDataManager.Instance.isDataLoaded)
		{
			RefreshData();
		}
		else
		{
			// 如果数据未加载，监听数据加载完成事件
			if (GlobalElectrolyticCellDataManager.Instance != null)
			{
				GlobalElectrolyticCellDataManager.Instance.OnDataUpdated += OnGlobalDataLoaded;
			}
		}
	}

	// 补充刷新方法，用于重载槽号数据并更新UI
	private void RefreshData()
	{
		LoadSlotNumberDataFromGlobal();
	}
	
	// 数据加载完成时的回调
	private void OnGlobalDataLoaded()
	{
		// 移除监听器
		if (GlobalElectrolyticCellDataManager.Instance != null)
		{
			GlobalElectrolyticCellDataManager.Instance.OnDataUpdated -= OnGlobalDataLoaded;
		}
		
		// 重新初始化并刷新数据
		InitializeDefaultData();
		RefreshData();
		
		Debug.Log("全局数据加载完成，槽号选择器已自动刷新");
	}

    // 新增方法：初始化默认数据
    public void InitializeDefaultData()
    {
        // 设置默认提示数据
        slotNumberItems.Clear();
        
        // 检查是否有全局数据可用
        if (GlobalElectrolyticCellDataManager.Instance != null && 
            GlobalElectrolyticCellDataManager.Instance.isDataLoaded &&
            GlobalElectrolyticCellDataManager.Instance.GetAllConfigs().Count > 0)
        {
            // 如果有数据，尝试设置默认值
            var allWorkshops = GlobalElectrolyticCellDataManager.Instance.GetAllWorkshopNumbers();
            if (allWorkshops.Count > 0)
            {
                currentWorkshopNumber = allWorkshops[0];
                var workAreas = GlobalElectrolyticCellDataManager.Instance.GetWorkAreaNumbersByWorkshop((byte)currentWorkshopNumber);
                if (workAreas.Count > 0)
                {
                    currentWorkAreaNumber = workAreas[0];
                    var slots = GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea(
                        (byte)currentWorkshopNumber, (byte)currentWorkAreaNumber);
                    if (slots.Count > 0)
                    {
                        selectedSlotNumber = slots[0].ToString();
                        Debug.Log($"初始化默认槽号: {currentWorkshopNumber}车间{currentWorkAreaNumber}工区 槽号{selectedSlotNumber}");
                    }
                    else
                    {
                        Debug.LogWarning($"工区{currentWorkshopNumber}车间{currentWorkAreaNumber}工区没有找到槽号");
                        selectedSlotNumber = "无槽号";
                    }
                }
                else
                {
                    Debug.LogWarning($"车间{currentWorkshopNumber}没有找到工区");
                    selectedSlotNumber = "无工区";
                }
            }
            else
            {
                Debug.LogWarning("没有找到任何车间");
                selectedSlotNumber = "无车间";
            }
            
            // 更新显示文本
            UpdateDisplayText($"{currentWorkshopNumber}车间{currentWorkAreaNumber}工区", selectedSlotNumber);
        }
        else
        {
            // 如果没有数据，设置默认提示
            slotNumberItems.Add("数据加载中...");
            selectedSlotNumber = "无数据";
            UpdateDisplayText("无车间工区", "无槽号");
            Debug.Log("没有可用的全局数据，显示默认提示");
        }
        
        // 预填充UI
        if (itemScrollRect != null)
        {
            PopulateScrollRect(itemScrollRect, slotNumberItems);
        }
    }
    public IEnumerator InitializeDefaultsFromData()
    {
        // 等待全局数据加载完成
        yield return new WaitUntil(() =>
            GlobalElectrolyticCellDataManager.Instance != null &&
            GlobalElectrolyticCellDataManager.Instance.isDataLoaded &&
            GlobalElectrolyticCellDataManager.Instance.GetAllConfigs().Count > 0);

        // 如果之前没有成功设置过有效槽号，则获取真实数据
        if (currentWorkshopNumber <= 0 || string.IsNullOrEmpty(selectedSlotNumber) || selectedSlotNumber == "无数据" || selectedSlotNumber == "无槽号")
        {
            var allWorkshops = GlobalElectrolyticCellDataManager.Instance.GetAllWorkshopNumbers();
            if (allWorkshops.Count > 0)
            {
                currentWorkshopNumber = allWorkshops[0];
                var workAreas = GlobalElectrolyticCellDataManager.Instance.GetWorkAreaNumbersByWorkshop((byte)currentWorkshopNumber);
                if (workAreas.Count > 0)
                {
                    currentWorkAreaNumber = workAreas[0];
                    var slots = GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea((byte)currentWorkshopNumber, (byte)currentWorkAreaNumber);
                    if (slots.Count > 0)
                    {
                        selectedSlotNumber = slots[0].ToString();
                    }
                }
            }
        }
        
        // 刷新UI
        UpdateDisplayText($"{currentWorkshopNumber}车间{currentWorkAreaNumber}工区", selectedSlotNumber);

        Debug.Log($"默认初始化完成: {currentWorkshopNumber}车间{currentWorkAreaNumber}工区, 槽号: {selectedSlotNumber}");
    }

    // 新增公开方法：同步确保默认车间、工区与槽号，并立即刷新UI
    public void EnsureDefaultWorkshopAreaAndSlot()
    {
        // 若全局电解槽数据已加载，优先使用真实数据的第一个项作为默认
        bool hasGlobal = GlobalElectrolyticCellDataManager.Instance != null &&
                         GlobalElectrolyticCellDataManager.Instance.isDataLoaded &&
                         GlobalElectrolyticCellDataManager.Instance.GetAllConfigs().Count > 0;

        if (hasGlobal)
        {
            // 如果当前文本未设置或当前索引非法，则抓取第一个车间/工区/槽号
            var allWorkshops = GlobalElectrolyticCellDataManager.Instance.GetAllWorkshopNumbers();
            if (allWorkshops != null && allWorkshops.Count > 0)
            {
                currentWorkshopNumber = allWorkshops[0];
                var workAreas = GlobalElectrolyticCellDataManager.Instance.GetWorkAreaNumbersByWorkshop((byte)currentWorkshopNumber);
                if (workAreas != null && workAreas.Count > 0)
                {
                    currentWorkAreaNumber = workAreas[0];
                    var slots = GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea((byte)currentWorkshopNumber, (byte)currentWorkAreaNumber);
                    if (slots != null && slots.Count > 0)
                    {
                        selectedSlotNumber = slots[0].ToString();
                    }
                    else if (string.IsNullOrEmpty(selectedSlotNumber))
                    {
                        selectedSlotNumber = "1001"; // 槽号兜底
                    }
                }
            }

            // 更新车间工区显示文本
            string waText = $"{currentWorkshopNumber}车间{currentWorkAreaNumber}工区";
            UpdateDisplayText(waText, selectedSlotNumber);

            // 用筛选得到的槽号重建列表
            slotNumberItems.Clear();
            var filteredConfigs = GlobalElectrolyticCellDataManager.Instance.GetConfigsByWorkshopAndArea((byte)currentWorkshopNumber, (byte)currentWorkAreaNumber);
            if (filteredConfigs != null && filteredConfigs.Count > 0)
            {
                var slotNumbers = filteredConfigs
                    .Select(cfg => cfg.slotNumber.ToString())
                    .OrderBy(s => { if (int.TryParse(s, out int n)) return n; return 0; })
                    .ToList();
                slotNumberItems.AddRange(slotNumbers);
            }
            else
            {
                slotNumberItems.Add(selectedSlotNumber ?? "1001");
            }

            // 列表刷新
            if (itemScrollRect != null)
            {
                PopulateScrollRect(itemScrollRect, slotNumberItems);
            }
        }
        else
        {
            // 无全局数据：使用硬默认并刷新UI与列表
            currentWorkshopNumber = currentWorkshopNumber <= 0 ? 1 : currentWorkshopNumber;
            currentWorkAreaNumber = currentWorkAreaNumber <= 0 ? 1 : currentWorkAreaNumber;
            if (string.IsNullOrEmpty(selectedSlotNumber)) selectedSlotNumber = "1001";

            UpdateDisplayText($"{currentWorkshopNumber}车间{currentWorkAreaNumber}工区", selectedSlotNumber);

            slotNumberItems.Clear();
            slotNumberItems.Add(selectedSlotNumber);
            if (itemScrollRect != null)
            {
                PopulateScrollRect(itemScrollRect, slotNumberItems);
            }
        }

        Debug.Log($"EnsureDefaultWorkshopAreaAndSlot -> {currentWorkshopNumber}车间{currentWorkAreaNumber}工区, 槽号: {selectedSlotNumber}");
    }


    // 简化OpenSelectorPanel方法
    public void OpenSelectorPanel()
    {
        // 强制确保面板处于正确的初始状态
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
        
        // 如果有其他选择器处于激活状态，先关闭它
        if (currentActiveSelector != null && currentActiveSelector != selectorPanel)
        {
            currentActiveSelector.SetActive(false);
        }
        
        // 立即激活面板
        if (selectorPanel != null)
        {
            selectorPanel.SetActive(true);
            currentActiveSelector = selectorPanel;
        }
        
        // 异步加载实际数据
        StartCoroutine(LoadActualDataAsync());
    }

    // 新增协程：异步加载实际数据
    public IEnumerator LoadActualDataAsync()
    {
        yield return null; // 等待一帧确保面板激活
        
        // 加载实际槽号数据
        LoadSlotNumberDataFromGlobal();
    }

    // 添加静态引用，用于跟踪当前激活的选择器
    private static GameObject currentActiveSelector;
    
    // 触发面板显示的方法

    // 延迟槽号数据加载协程
    private IEnumerator DelayedSlotDataLoading()
    {
        // 等待一帧，确保面板完全激活
        yield return null;
        
        // 从文本框获取车间号和工区号，然后加载对应的槽号信息
        Debug.Log("OpenSelectorPanel被调用，开始加载槽号信息");
        LoadSlotNumberDataFromGlobal();
    }
    
    // 从全局数据加载槽号信息（根据车间号和工区号筛选）
    // 修改 LoadSlotNumberDataFromGlobal 方法
    public void LoadSlotNumberDataFromGlobal()
    {
        Debug.Log("开始从全局数据加载槽号信息");
        
        slotNumberItems.Clear();
        
        // 检查全局电解槽数据管理器是否存在且已加载数据
        if (GlobalElectrolyticCellDataManager.Instance == null)
        {
            Debug.LogWarning("GlobalElectrolyticCellDataManager实例不存在");
            ShowDefaultData("管理器未初始化");
            return;
        }
        
        if (!GlobalElectrolyticCellDataManager.Instance.isDataLoaded)
        {
            Debug.LogWarning("全局电解槽数据尚未加载完成");
            //ShowDefaultData("数据加载中...");
            Debug.LogWarning("数据加载中...");
            return;
        }
        
        // 检查总数据量
        var allConfigs = GlobalElectrolyticCellDataManager.Instance.GetAllConfigs();
        Debug.Log($"全局数据管理器中总配置数量: {allConfigs.Count}");
        
        if (allConfigs.Count == 0)
        {
            Debug.LogWarning("全局数据管理器中没有配置数据");
            ShowDefaultData("暂无配置数据");
            return;
        }
        
        // 从文本框获取车间号和工区号
        if (!ExtractWorkshopAndWorkAreaNumbers())
        {
            Debug.LogWarning("无法从文本框中提取车间号和工区号");
            ShowDefaultData("请先选择车间工区");
            return;
        }
        
        Debug.Log($"提取到的筛选条件 - 车间号: {currentWorkshopNumber}, 工区号: {currentWorkAreaNumber}");
        
        // 根据车间号和工区号筛选电解槽配置
        var filteredConfigs = GlobalElectrolyticCellDataManager.Instance.GetConfigsByWorkshopAndArea(
            (byte)currentWorkshopNumber, (byte)currentWorkAreaNumber);
        
        Debug.Log($"筛选后的配置数量: {filteredConfigs.Count}");
        
        if (filteredConfigs != null && filteredConfigs.Count > 0)
        {
            // 提取槽号并排序
            var slotNumbers = filteredConfigs
                .Select(config => config.slotNumber.ToString())
                .OrderBy(slot => {
                    if (int.TryParse(slot, out int num))
                        return num;
                    return 0;
                })
                .ToList();
            
            foreach (string slotNumber in slotNumbers)
            {
                string displayText = $"{slotNumber}";
                slotNumberItems.Add(displayText);
                Debug.Log($"添加槽号: {displayText}");
            }
            
            Debug.Log($"槽号数据加载完成，总数: {slotNumberItems.Count}");
        }
        else
        {
            Debug.LogWarning($"未找到车间{currentWorkshopNumber}工区{currentWorkAreaNumber}的电解槽配置");
            ShowDefaultData($"车间{currentWorkshopNumber}工区{currentWorkAreaNumber}暂无槽号");
            return;
        }
        
        UpdateUI();
    }
    
    // 添加显示默认数据的辅助方法
    private void ShowDefaultData(string message)
    {
        slotNumberItems.Clear();
        slotNumberItems.Add(message);
        UpdateUI();
    }
    
    // 修改 ExtractWorkshopAndWorkAreaNumbers 方法，增加更详细的调试信息
    private bool ExtractWorkshopAndWorkAreaNumbers()
    {
        try
        {
            if (workshopWorkAreaDisplayText == null)
            {
                Debug.LogError("workshopWorkAreaDisplayText 引用为空，请在Inspector中设置");
                return false;
            }
            
            if (string.IsNullOrEmpty(workshopWorkAreaDisplayText.text))
            {
                Debug.LogWarning("车间工区显示文本为空");
                return false;
            }
            
            string text = workshopWorkAreaDisplayText.text.Trim();
            Debug.Log($"正在解析文本: '{text}'");
            
            // 使用正则表达式匹配格式："数字车间数字工区"
            Match match = Regex.Match(text, @"(\d+)车间(\d+)工区");
            if (match.Success)
            {
                currentWorkshopNumber = int.Parse(match.Groups[1].Value);
                currentWorkAreaNumber = int.Parse(match.Groups[2].Value);
                Debug.Log($"成功提取 - 车间号: {currentWorkshopNumber}, 工区号: {currentWorkAreaNumber}");
                return true;
            }
            else
            {
                Debug.LogWarning($"文本格式不匹配，期望格式：'数字车间数字工区'，实际文本：'{text}'");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"提取车间号和工区号时发生错误: {ex.Message}");
            return false;
        }
    }
    
    // 修改 UpdateUI 方法，增加更好的错误处理
    private void UpdateUI()
    {
        Debug.Log($"开始更新UI，槽号选项数量: {slotNumberItems.Count}");
    
        // 如果没有数据，不进行UI更新
        if (slotNumberItems.Count == 0)
        {
            Debug.LogError("槽号选项列表为空，无法更新UI");
            return;
        }
    
        // 初始化选择
        selectedSlotNumber = slotNumberItems[0];
        Debug.Log($"已设置选中槽号: {selectedSlotNumber}");
    
        // 填充ScrollRect选项
        if (itemScrollRect != null)
        {
            PopulateScrollRect(itemScrollRect, slotNumberItems);
            Debug.Log("ScrollRect已更新");
        }
        else
        {
            Debug.LogError("itemScrollRect 引用为空，请在Inspector中设置");
        }
        
        // 初始化显示文本
        UpdateDisplayText();
    }

    void PopulateScrollRect(ScrollRect scrollRect, List<string> data)
    {
        if (scrollRect == null || scrollRect.content == null) return;
        
        // 获取ScrollRect的内容区域
        Transform content = scrollRect.content;
        
        // 清除之前的项目
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(content.GetChild(i).gameObject);
        }

        // 为每个选项创建UI项目
        foreach (string item in data)
        {
            GameObject option = new GameObject("SlotNumberOption");
            RectTransform rectTransform = option.AddComponent<RectTransform>();

            // 添加Text组件以显示文本
            Text optionText = option.AddComponent<Text>();
            optionText.text = item;
            optionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            optionText.fontSize = 40;
            optionText.alignment = TextAnchor.MiddleCenter;
            optionText.color = item == selectedSlotNumber ? Color.red : Color.black;

            // 设置Text元素的大小
            rectTransform.sizeDelta = new Vector2(400, itemHeight);
            rectTransform.localScale = Vector3.one;

            // 为每个选项添加Button
            Button button = option.AddComponent<Button>();
            string capturedItem = item; // 捕获循环变量
            button.onClick.AddListener(() => OnSlotNumberSelected(capturedItem));

            // 将选项设置为ScrollRect内容的子项
            option.transform.SetParent(content, false);
        }

        // 添加LayoutGroup进行间距设置
        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10;
        }

        // 确保内容区域正确调整大小
        ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void OnSlotNumberSelected(string slotNumber)
    {
        selectedSlotNumber = slotNumber;
        UpdateDisplayText(null,slotNumber);
        UpdateItemColors();
        Debug.Log($"选择了槽号: {slotNumber}");
    }

    // 更新所有选项的颜色
    void UpdateItemColors()
    {
        if (itemScrollRect == null || itemScrollRect.content == null) return;
        
        // 遍历所有选项并更新颜色
        foreach (Transform child in itemScrollRect.content)
        {
            Text optionText = child.GetComponent<Text>();
            if (optionText != null)
            {
                optionText.color = optionText.text == selectedSlotNumber ? Color.red : Color.black;
            }
        }
    }

    // 删除此方法
    // void OnItemScrollChanged(Vector2 scrollPosition)
    // {
    //     if (itemContentRect == null) return;
    //     
    //     // 滚动区域的滚动位置变化逻辑
    //     string previousSelected = selectedSlotNumber;
    //     SelectItemInScroll(itemContentRect, ref selectedSlotNumber);
    //     
    //     // 如果选择发生变化，更新颜色
    //     if (previousSelected != selectedSlotNumber)
    //     {
    //         UpdateItemColors();
    //     }
    //     
    //     UpdateDisplayText();
    // }

    // 删除此方法
    // void SelectItemInScroll(RectTransform contentRect, ref string selectedItem)
    // {
    //     if (contentRect == null) return;
    //     
    //     // 计算滚动区域的中心
    //     float contentHeight = contentRect.rect.height;
    //     float centerY = contentHeight / 2;

    //     // 遍历项目并找到最接近中心的项目
    //     foreach (Transform child in contentRect)
    //     {
    //         RectTransform itemRect = child.GetComponent<RectTransform>();
    //         if (itemRect != null)
    //         {
    //             float itemCenterY = itemRect.anchoredPosition.y + itemRect.rect.height / 2;

    //             // 如果项目最接近中心，则选择它
    //             if (Mathf.Abs(itemCenterY - centerY) < itemHeight / 2)
    //             {
    //                 Text textComponent = child.GetComponent<Text>();
    //                 if (textComponent != null)
    //                 {
    //                     selectedItem = textComponent.text;
    //                     break;
    //                 }
    //             }
    //         }
    //     }
    // }

    void UpdateDisplayText(string workshopAreaText = null, string slotText = null)
    {
        // 车间工区：仅在传入非空时更新
        if (workshopWorkAreaDisplayText != null && !string.IsNullOrEmpty(workshopAreaText))
        {
            workshopWorkAreaDisplayText.text = workshopAreaText;
        }

        // 槽号：优先使用传入值，否则使用当前选中值
        if (selectedDisplayText != null)
        {
            selectedDisplayText.text = !string.IsNullOrEmpty(slotText) ? slotText : selectedSlotNumber;
        }
    }

    void CancelSelection()
    {
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
        Debug.Log("取消槽号选择");
    }

    public void ConfirmSelection()
    {
        // 只有在确认时才更新主UI上的显示文本
        if (selectedDisplayText != null)
        {
            selectedDisplayText.text = selectedSlotNumber;
        }
        
        Debug.Log($"确认选择槽号: {selectedSlotNumber}");
        
        // 触发确认事件
        OnSlotConfirmed?.Invoke(selectedSlotNumber);

        // 尝试更新 phoneTableScript (如果存在)
        // 直接调用而不是使用协程，因为面板即将关闭
        phoneTableScript phoneScript = FindObjectOfType<phoneTableScript>();
        if (phoneScript != null)
        {
            phoneScript.OnUpdataButtonClick();
        }

        if (selectorPanel != null)
            selectorPanel.SetActive(false);
            
        // 这里可以添加代码，将选择结果传递给其他系统
        OnSlotNumberConfirmed(selectedSlotNumber);
    }
    // 新增协程方法 - 已废弃，改为直接调用
    // private System.Collections.IEnumerator DelayedOnUpdataButtonClick() ...
    
    // 槽号确认选择的回调方法
    private void OnSlotNumberConfirmed(string slotNumber)
    {
        // 可以在这里添加槽号选择确认后的逻辑
        Debug.Log($"槽号选择已确认: {slotNumber}");
    }
    
    // 获取当前选择的槽号
    public string GetSelectedSlotNumber()
    {
        return selectedSlotNumber;
    }
    
    // 获取当前选择的槽号数字（去掉"槽号"前缀）
    public int GetSelectedSlotNumberValue()
    {
        if (string.IsNullOrEmpty(selectedSlotNumber))
            return -1;
            
        Match match = Regex.Match(selectedSlotNumber, @"(\d+)");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return -1;
    }
    
    // 获取当前选择的电解槽配置（修复：类型引用）
    public ElectrolyticCellConfigManager.ElectrolyticCellConfig GetSelectedSlotConfig()
    {
        int slotNumber = GetSelectedSlotNumberValue();
        if (slotNumber == -1 || GlobalElectrolyticCellDataManager.Instance == null)
            return null;
            
        return GlobalElectrolyticCellDataManager.Instance.GetConfigBySlotNumber((ushort)slotNumber);
    }
    
    // 手动刷新槽号数据（如果全局数据或车间工区选择更新了）
    public void RefreshSlotNumberData()
    {
        Debug.Log("手动刷新槽号数据");
        LoadSlotNumberDataFromGlobal();
    }
    
    // 设置车间工区文本框引用（如果需要动态设置）
    public void SetWorkshopWorkAreaTextReference(TextMeshProUGUI workshopWorkAreaText)
    {
        workshopWorkAreaDisplayText = workshopWorkAreaText;
        Debug.Log("已设置车间工区文本框引用");
    }
    
    // 设置槽号选项列表（备用方法）
    public void SetSlotNumberItems(List<string> newItems)
    {
        if (newItems != null && newItems.Count > 0)
        {
            slotNumberItems = new List<string>(newItems);
            selectedSlotNumber = slotNumberItems[0];
            if (itemScrollRect != null)
            {
                PopulateScrollRect(itemScrollRect, slotNumberItems);
            }
            UpdateDisplayText(null, selectedSlotNumber);
            Debug.Log($"手动设置槽号选项，数量: {newItems.Count}");
        }
    }
    
    // 添加获取槽号选项列表的公开方法
    public List<string> GetSlotNumberItems()
    {
        return new List<string>(slotNumberItems);
    }
    
    // 添加获取指定槽号在列表中索引的方法
    public int GetSlotNumberIndex(string slotNumber)
    {
        if (string.IsNullOrEmpty(slotNumber) || slotNumberItems == null)
            return -1;
            
        return slotNumberItems.IndexOf(slotNumber);
    }
}