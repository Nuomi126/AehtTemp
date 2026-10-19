using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class riqSelector : MonoBehaviour
{
    // UI components
    public GameObject selectorPanel;
    public Button cancelButton;
    public Button confirmButton;
    public ScrollRect workshopScrollRect;
    public ScrollRect workAreaScrollRect;
    public ScrollRect caohScrollRect;
    public TextMeshProUGUI selectedDisplayText; // 主UI上显示选中区域的按钮

    // 选中项的颜色
    public Color selectedColor = Color.red;
    public Color normalColor = Color.black;

    // 模拟数据
     private List<string> year;
    //private List<string> year = new List<string> { "2023", "2024", "2025" };
    private List<string> month = new List<string> { "1月","2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" };
    private List<string> day = new List<string> { "1日", "2日","3日", "4日", "5日", "6日", "7日", "8日", "9日", "10日", "11日", "12日", "13日", "14日", "15日", "16日", "17日", "18日", "19日", "20日", "21日", "22日", "23日", "24日", "25日", "26日", "27日", "28日", "29日", "30日", "31日"};

    private string selectedWorkshop;
    private string selectedWorkArea;
    private string selectedCaoh;

    private float itemHeight = 100f; // 列表中每个项目的高度
    private RectTransform workshopContentRect;
    private RectTransform workAreaContentRect;
    private RectTransform caohContentRect;

    // 保存当前选中的Text组件引用
    private Text currentSelectedWorkshopText;
    private Text currentSelectedWorkAreaText;
    private Text currentSelectedCaohText;
    private void Awake()
    {
        // 初始化年份列表，包含当前年份及前三年
        InitializeYearList();
    }
    
    // 初始化年份列表
    private void InitializeYearList()
{
    year = new List<string>();
    int currentYear = System.DateTime.Now.Year;
    
    // 添加当前年份及前三年，从大到小排序（最新年份在前）
    for (int i = 0; i <= 3; i++)
    {
        year.Add((currentYear - i).ToString());
    }
}
    void Start()
    {
		// 获取当前激活场景的名称
		string sceneName = SceneManager.GetActiveScene().name;
		//UnityEngine.Debug.Log("当前场景名称: " + sceneName);
		if (sceneName != "yujingxingxi")
		{
			// 设置UI行为
			selectorPanel.SetActive(false);
		}
		
		cancelButton.onClick.AddListener(CancelSelection);
        confirmButton.onClick.AddListener(ConfirmSelection);

        // 获取当前日期并设置为默认选择
        System.DateTime currentDate = System.DateTime.Now;
        selectedWorkshop = currentDate.Year.ToString();
        selectedWorkArea = currentDate.Month.ToString() + "月";
        selectedCaoh = currentDate.Day.ToString() + "日";

        // 填充ScrollRect选项
        PopulateScrollRect(workshopScrollRect, year);
        PopulateScrollRect(workAreaScrollRect, month);
        PopulateScrollRect(caohScrollRect, day);

        // 设置内容矩形以便于引用
        workshopContentRect = workshopScrollRect.content.GetComponent<RectTransform>();
        workAreaContentRect = workAreaScrollRect.content.GetComponent<RectTransform>();
        caohContentRect = caohScrollRect.content.GetComponent<RectTransform>();

        // 确保初始选中项颜色正确
        InitializeSelectedItems();

        if (selectedDisplayText != null)
        {
            string yearValue = selectedWorkshop;
            string monthValue = selectedWorkArea.Replace("月", "");
            if (monthValue.Length == 1) monthValue = "0" + monthValue;
            string dayValue = selectedCaoh.Replace("日", "");
            if (dayValue.Length == 1) dayValue = "0" + dayValue;
            selectedDisplayText.text = $"{yearValue}{monthValue}{dayValue}";
        }
    }
    
    // 新增方法：确保初始选中项颜色正确
    private void InitializeSelectedItems()
    {
        // 遍历年份选项，找到并设置选中项
        foreach (Transform child in workshopContentRect)
        {
            Text itemText = child.GetComponent<Text>();
            if (itemText != null && itemText.text == selectedWorkshop)
            {
                currentSelectedWorkshopText = itemText;
                itemText.color = selectedColor;
                break;
            }
        }
        
        // 遍历月份选项，找到并设置选中项
        foreach (Transform child in workAreaContentRect)
        {
            Text itemText = child.GetComponent<Text>();
            if (itemText != null && itemText.text == selectedWorkArea)
            {
                currentSelectedWorkAreaText = itemText;
                itemText.color = selectedColor;
                break;
            }
        }
        
        // 遍历日期选项，找到并设置选中项
        foreach (Transform child in caohContentRect)
        {
            Text itemText = child.GetComponent<Text>();
            if (itemText != null && itemText.text == selectedCaoh)
            {
                currentSelectedCaohText = itemText;
                itemText.color = selectedColor;
                break;
            }
        }
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
            GameObject option = new GameObject("Option");
            option.AddComponent<RectTransform>(); // 确保它有一个RectTransform用于布局

            // 添加Text组件以显示文本
            Text optionText = option.AddComponent<Text>();
            optionText.text = item;
            optionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 传统字体
            optionText.fontSize = 40;
            optionText.alignment = TextAnchor.MiddleCenter;
            optionText.color = normalColor; // 默认使用正常颜色
            
            // 如果是当前选中的项目，设置为选中颜色
            if ((scrollRect == workshopScrollRect && item == selectedWorkshop) ||
                (scrollRect == workAreaScrollRect && item == selectedWorkArea) ||
                (scrollRect == caohScrollRect && item == selectedCaoh))
            {
                optionText.color = selectedColor;
                
                // 保存选中项的引用
                if (scrollRect == workshopScrollRect)
                    currentSelectedWorkshopText = optionText;
                else if (scrollRect == workAreaScrollRect)
                    currentSelectedWorkAreaText = optionText;
                else if (scrollRect == caohScrollRect)
                    currentSelectedCaohText = optionText;
            }

            // 设置Text元素的大小
            RectTransform rectTransform = option.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, itemHeight); // 调整高度
            rectTransform.localScale = Vector3.one;

            // 为每个选项添加Button（可选，如果需要点击行为）
            Button button = option.AddComponent<Button>();
            button.onClick.AddListener(() => OnItemSelected(item, scrollRect, optionText));

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

    void OnItemSelected(string selectedItem, ScrollRect scrollRect, Text selectedText)
    {
        // 更新选中项的颜色
        if (scrollRect == workshopScrollRect)
        {
            // 恢复之前选中项的颜色
            if (currentSelectedWorkshopText != null)
                currentSelectedWorkshopText.color = normalColor;
            
            selectedWorkshop = selectedItem;
            currentSelectedWorkshopText = selectedText;
        }
        else if (scrollRect == workAreaScrollRect)
        {
            // 恢复之前选中项的颜色
            if (currentSelectedWorkAreaText != null)
                currentSelectedWorkAreaText.color = normalColor;
                
            selectedWorkArea = selectedItem;
            currentSelectedWorkAreaText = selectedText;
        }
        else if (scrollRect == caohScrollRect)
        {
            // 恢复之前选中项的颜色
            if (currentSelectedCaohText != null)
                currentSelectedCaohText.color = normalColor;
                
            selectedCaoh = selectedItem;
            currentSelectedCaohText = selectedText;
        }
        
        // 设置当前选中项的颜色
        selectedText.color = selectedColor;
        
        // 更新显示文本
        UpdateDisplayText();
    }


    void UpdateDisplayText()
    {
        // 更新显示文本
        selectedDisplayText.text = $"{selectedWorkshop} - {selectedWorkArea} - {selectedCaoh}";
    }

    void CancelSelection()
    {
        selectorPanel.SetActive(false); // 隐藏选择面板
    }

    [Header("事件回调")]
    [SerializeField] public UnityEvent<string> OnDateConfirmed; // 日期确认事件
    
    public void ConfirmSelection()
    {
        // 提取年份数字
        string yearValue = selectedWorkshop;
        
        // 提取月份数字（去掉"月"字）
        string monthValue = selectedWorkArea.Replace("月", "");
        // 确保月份是两位数
        if (monthValue.Length == 1)
            monthValue = "0" + monthValue;
        
        // 提取日期数字（去掉"日"字）
        string dayValue = selectedCaoh.Replace("日", "");
        // 确保日期是两位数
        if (dayValue.Length == 1)
            dayValue = "0" + dayValue;
        
        // 格式化为 YYYYMMDD
        string formattedDate = $"{yearValue}{monthValue}{dayValue}";
        
        // 更新显示文本
        selectedDisplayText.text = formattedDate;
        
        // 隐藏选择面板
        selectorPanel.SetActive(false);
        
        // 触发事件，传递格式化的日期
        OnDateConfirmed?.Invoke(formattedDate);
        
        Debug.Log($"日期选择器确认日期: {formattedDate}");
    }
    
    // 添加静态引用，用于跟踪当前激活的选择器
    private static GameObject currentActiveSelector;
    
    // 触发面板显示的方法
    public void OpenSelectorPanel()
    {
        // 强制确保面板处于正确的初始状态
        selectorPanel.SetActive(false);
        
        // 如果有其他选择器处于激活状态，先关闭它
        if (currentActiveSelector != null && currentActiveSelector != selectorPanel)
        {
            currentActiveSelector.SetActive(false);
        }
        
        // 激活面板
        selectorPanel.SetActive(true);
        
        // 更新当前激活的选择器引用
        currentActiveSelector = selectorPanel;
        
        // 延迟执行滚动位置设置和UI初始化，确保面板完全激活后再执行
        StartCoroutine(DelayedInitialization());
    }
    
    // 延迟初始化协程
    private IEnumerator DelayedInitialization()
    {
        // 等待一帧，确保面板完全激活
        yield return null;
        
        // 设置滚动位置到初始选择
        ResetScrollPositions();
        
        // 确保选中项颜色正确
        InitializeSelectedItems();
    }
    
    // 重置滚动位置到当前选择
    private void ResetScrollPositions()
    {
        // 预先计算并设置滚动位置，避免打开后的计算延迟
        SetScrollToSelection(workshopScrollRect, year, selectedWorkshop);
        SetScrollToSelection(workAreaScrollRect, month, selectedWorkArea);
        SetScrollToSelection(caohScrollRect, day, selectedCaoh);
    }
    
    // 设置滚动位置到指定选项
    private void SetScrollToSelection(ScrollRect scrollRect, List<string> dataList, string selectedValue)
    {
        int index = dataList.IndexOf(selectedValue);
        if (index >= 0)
        {
            float normalizedPosition = 1f - ((float)index / (dataList.Count - 1));
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        }
    }
    
    // 获取当前选择的车间
    public string GetSelectedWorkshop()
    {
        return selectedWorkshop;
    }
    
    // 获取当前选择的工区
    public string GetSelectedWorkArea()
    {
        return selectedWorkArea;
    }
    
    // 获取当前选择的槽号
    public string GetSelectedCaoh()
    {
        return selectedCaoh;
    }
}