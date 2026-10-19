using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ThreeColumnSelector : MonoBehaviour
{
    // UI components
    public GameObject selectorPanel;
    public Button cancelButton;
    public Button confirmButton;
    public ScrollRect workshopScrollRect;
    public ScrollRect workAreaScrollRect;
    public ScrollRect caohScrollRect;
    public TextMeshProUGUI selectedDisplayText; // 主UI上显示选中区域的按钮

    // 模拟数据
    private List<string> workshops = new List<string> { "1车间", "2车间", "3车间", "4车间", "5车间" };
    private List<string> workAreas = new List<string> { "1工区", "2工区", "3工区", "4工区", "5工区" };
    private List<string> caohs = new List<string> { "3101", "3102", "3103", "3104" };

    private string selectedWorkshop;
    private string selectedWorkArea;
    private string selectedCaoh;

    private float itemHeight = 100f; // 列表中每个项目的高度
    private RectTransform workshopContentRect;
    private RectTransform workAreaContentRect;
    private RectTransform caohContentRect;

    void Start()
    {
        // 设置UI行为
        selectorPanel.SetActive(false);
        cancelButton.onClick.AddListener(CancelSelection);
        confirmButton.onClick.AddListener(ConfirmSelection);

        // 初始化选择
        selectedWorkshop = workshops[0];
        selectedWorkArea = workAreas[0];
        selectedCaoh = caohs[0];

        // 填充ScrollRect选项
        PopulateScrollRect(workshopScrollRect, workshops);
        PopulateScrollRect(workAreaScrollRect, workAreas);
        PopulateScrollRect(caohScrollRect, caohs);

        // 设置内容矩形以便于引用
        workshopContentRect = workshopScrollRect.content.GetComponent<RectTransform>();
        workAreaContentRect = workAreaScrollRect.content.GetComponent<RectTransform>();
        caohContentRect = caohScrollRect.content.GetComponent<RectTransform>();

        // 添加滚动位置变化的监听器
        workshopScrollRect.onValueChanged.AddListener(OnWorkshopScrollChanged);
        workAreaScrollRect.onValueChanged.AddListener(OnWorkAreaScrollChanged);
        caohScrollRect.onValueChanged.AddListener(OnCaohScrollChanged);
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
            optionText.color = Color.black; // 设置颜色使文本可见

            // 设置Text元素的大小
            RectTransform rectTransform = option.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, itemHeight); // 调整高度
            rectTransform.localScale = Vector3.one;

            // 为每个选项添加Button（可选，如果需要点击行为）
            Button button = option.AddComponent<Button>();
            button.onClick.AddListener(() => OnItemSelected(item, scrollRect));

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

    void OnItemSelected(string selectedItem, ScrollRect scrollRect)
    {
        if (scrollRect == workshopScrollRect)
        {
            selectedWorkshop = selectedItem;
        }
        else if (scrollRect == workAreaScrollRect)
        {
            selectedWorkArea = selectedItem;
        }
        else if (scrollRect == caohScrollRect)
        {
            selectedCaoh = selectedItem;
        }
    }

    void OnWorkshopScrollChanged(Vector2 scrollPosition)
    {
        // 车间滚动区域的滚动位置变化逻辑
        SelectItemInScroll(workshopContentRect, ref selectedWorkshop);
        UpdateDisplayText();
    }

    void OnWorkAreaScrollChanged(Vector2 scrollPosition)
    {
        // 工区滚动区域的滚动位置变化逻辑
        SelectItemInScroll(workAreaContentRect, ref selectedWorkArea);
        UpdateDisplayText();
    }

    void OnCaohScrollChanged(Vector2 scrollPosition)
    {
        // 槽号滚动区域的滚动位置变化逻辑
        SelectItemInScroll(caohContentRect, ref selectedCaoh);
        UpdateDisplayText();
    }

    void SelectItemInScroll(RectTransform contentRect, ref string selectedItem)
    {
        // 计算滚动区域的中心
        float contentHeight = contentRect.rect.height;
        float scrollHeight = contentHeight; // 可滚动内容区域
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

    void UpdateDisplayText()
    {
        // 更新显示文本
        selectedDisplayText.text = $"{selectedWorkshop} - {selectedWorkArea} - {selectedCaoh}";
    }

    void CancelSelection()
    {
        selectorPanel.SetActive(false); // 隐藏选择面板
    }

    void ConfirmSelection()
    {
        // 只显示槽号
        selectedDisplayText.text = selectedCaoh;
        selectorPanel.SetActive(false); // 确认后隐藏选择面板
        
        // 这里可以添加代码，将选择结果传递给其他系统
        // 例如：通知DailyReportManager使用这些选择作为参数
    }

    // 触发面板显示的方法
    public void OpenSelectorPanel()
    {
        selectorPanel.SetActive(true);
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