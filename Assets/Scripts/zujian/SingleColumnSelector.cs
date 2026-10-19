using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.Security.Cryptography.X509Certificates;

public class SingleColumnSelector : MonoBehaviour
{
    // 辅助类：接受所有证书
    private class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // 接受所有证书
        }
    }
    
    // UI组件
    public GameObject selectorPanel;
    public Button cancelButton;
    public Button confirmButton;
    public ScrollRect itemScrollRect;
    public TextMeshProUGUI selectedDisplayText; // 主UI上显示选中项的文本

    // API地址
    public string apiBaseUrl = "http://183.214.222.76:9000/api/Home/GetPlantProperty"; // 工区API基础地址

    // 数据存储
    private List<string> items = new List<string>(); // 选项列表

    private string selectedItem;
    private float itemHeight = 100f; // 列表中每个项目的高度
    private RectTransform itemContentRect;

    void Start()
    {
        // 设置UI行为
        selectorPanel.SetActive(false);
        cancelButton.onClick.AddListener(CancelSelection);
        confirmButton.onClick.AddListener(ConfirmSelection);

        // 设置内容矩形以便于引用
        itemContentRect = itemScrollRect.content.GetComponent<RectTransform>();

        // 添加滚动位置变化的监听器
        itemScrollRect.onValueChanged.AddListener(OnItemScrollChanged);
        
        // 不在Start中调用FetchDataFromAPI，而是在打开面板时调用
    }

    // 添加静态引用，用于跟踪当前激活的选择器
    private static GameObject currentActiveSelector;
    
    // 触发面板显示的方法
    public void OpenSelectorPanel()
    {
        // 如果有其他选择器处于激活状态，先关闭它
        if (currentActiveSelector != null && currentActiveSelector != selectorPanel)
        {
            currentActiveSelector.SetActive(false);
        }
        
        // 显示选择器面板
        selectorPanel.SetActive(true);
        
        // 更新当前激活的选择器引用
        currentActiveSelector = selectorPanel;
        
        // 在打开面板时获取数据
        Debug.Log("OpenSelectorPanel被调用，准备获取API数据");
        FetchDataFromAPI();
    }
    
    // 从API获取数据
    public void FetchDataFromAPI()
    {
        // 确保GameObject是激活的
        if (gameObject.activeInHierarchy)
        {
            Debug.Log("开始获取API数据");
            StartCoroutine(FetchWorkAreaData());
        }
        else
        {
            Debug.LogWarning("无法启动协程，GameObject未激活。将使用默认数据。");
            // 使用默认数据
            items = new List<string> { "1工区", "2工区", "3工区", "4工区", "5工区", "6工区" };
            UpdateUI();
        }
    }

    private IEnumerator FetchWorkAreaData()
    {
        // 从全局变量获取NetCommIndex
        int netCommIndex = GlobalUserData.NetCommIndex;
        string parameter = $"{netCommIndex}-3";
        
        Debug.Log($"开始获取工区数据，API地址: {apiBaseUrl}，参数: {parameter}");
        
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            Debug.LogWarning("API地址未设置，使用默认数据");
            items = new List<string> { "1工区", "2工区", "3工区", "4工区", "5工区", "6工区" };
            UpdateUI();
            yield break;
        }

        // 构建完整的URL，包含参数
        string fullUrl = $"{apiBaseUrl}?para={parameter}";
        Debug.Log($"完整API请求URL: {fullUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            // 允许不安全连接（HTTP而非HTTPS）
            request.certificateHandler = new AcceptAllCertificates();
            request.timeout = 10; // 设置10秒超时
            
            Debug.Log("发送工区数据请求...");
            yield return request.SendWebRequest();
            Debug.Log("工区数据请求完成，状态: " + request.result);

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("API原始响应: " + response);
                
                items = ParseApiResponse(response);
                Debug.Log("解析后的工区数据: " + string.Join(", ", items) + "，数量: " + items.Count);
            }
            else
            {
                Debug.LogError("获取工区数据失败: " + request.error + "，响应码: " + request.responseCode);
                items = new List<string> { "1工区", "2工区", "3工区", "4工区", "5工区", "6工区" };
                Debug.Log("使用默认工区数据: " + string.Join(", ", items));
            }
            
            UpdateUI();
        }
    }

    // 解析API响应，格式为"1工区-2工区-3工区"
    private List<string> ParseApiResponse(string response)
    {
        Debug.Log("开始解析API响应: " + response);
        
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("API响应为空");
            return new List<string>();
        }

        // 去除整个响应字符串首尾的引号
        response = response.Trim('"');
        Debug.Log("去除引号后的响应: " + response);
        
        // 按"-"分割字符串
        string[] items = response.Split('-');
        Debug.Log("解析结果，项目数量: " + items.Length);
        
        // 创建一个新列表来存储清理后的项目
        List<string> cleanedItems = new List<string>();
        
        for (int i = 0; i < items.Length; i++)
        {
            // 去除每个项目中的逗号和可能的引号
            string cleanedItem = items[i].Replace(",", "").Trim('"');
            cleanedItems.Add(cleanedItem);
            Debug.Log("  项目[" + i + "]: " + cleanedItem);
        }
        
        return cleanedItems;
    }

    // 添加静态数据共享
    private static List<string> sharedSlotData = new List<string>();
    private static System.Action<List<string>> onDataUpdated;
    
    // 在UpdateUI方法中添加数据共享
    private void UpdateUI()
    {
        Debug.Log("开始更新UI，选项数量: " + items.Count);

        // 如果没有数据，添加默认选项
        if (items.Count == 0)
        {
            Debug.LogError("选项列表为空，添加默认选项");
            items.Add("默认选项");
        }

        // 初始化选择
        selectedItem = items[0];
        Debug.Log("已设置选中项: " + selectedItem);

        // 填充ScrollRect选项
        PopulateScrollRect(itemScrollRect, items);
        
        // 初始化显示文本
        UpdateDisplayText();
        
        // 新增：更新共享数据
        UpdateSharedData();
    }
    
    // 更新共享数据
    private void UpdateSharedData()
    {
        sharedSlotData = new List<string>(items);
        onDataUpdated?.Invoke(sharedSlotData);
        Debug.Log($"共享槽号数据已更新，共{sharedSlotData.Count}个选项");
    }
    
    // 静态方法：获取共享的槽号数据
    public static List<string> GetSharedSlotData()
    {
        return new List<string>(sharedSlotData);
    }
    
    // 静态方法：订阅数据更新事件
    public static void SubscribeToDataUpdates(System.Action<List<string>> callback)
    {
        onDataUpdated += callback;
    }
    
    // 静态方法：取消订阅数据更新事件
    public static void UnsubscribeFromDataUpdates(System.Action<List<string>> callback)
    {
        onDataUpdated -= callback;
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
            optionText.color = item == selectedItem ? Color.red : Color.black; // 如果是当前选中项则显示为红色

            // 设置Text元素的大小
            RectTransform rectTransform = option.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, itemHeight); // 调整高度
            rectTransform.localScale = Vector3.one;

            // 为每个选项添加Button
            Button button = option.AddComponent<Button>();
            button.onClick.AddListener(() => OnItemSelected(item));

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

    void OnItemSelected(string item)
    {
        selectedItem = item;
        UpdateDisplayText();
        UpdateItemColors(); // 更新所有选项的颜色
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
                optionText.color = optionText.text == selectedItem ? Color.red : Color.black;
            }
        }
    }

    void OnItemScrollChanged(Vector2 scrollPosition)
    {
        // 滚动区域的滚动位置变化逻辑
        string previousSelected = selectedItem;
        SelectItemInScroll(itemContentRect, ref selectedItem);
        
        // 如果选择发生变化，更新颜色
        if (previousSelected != selectedItem)
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

    void UpdateDisplayText()
    {
        // 只在选择器面板中更新预览文本，不更改主UI上的显示文本
        // 这里可以添加一个预览文本组件，如果需要的话
    }

    void CancelSelection()
    {
        selectorPanel.SetActive(false); // 隐藏选择面板
    }

    void ConfirmSelection()
    {
        // 只有在确认时才更新主UI上的显示文本
        selectedDisplayText.text = selectedItem;
        selectorPanel.SetActive(false); // 确认后隐藏选择面板
        
        // 这里可以添加代码，将选择结果传递给其他系统
    }
    
    // 设置API地址的方法
    public void SetApiUrl(string apiUrl)
    {
        Debug.Log($"设置API地址: {apiUrl}");
        this.apiBaseUrl = apiUrl;
        
        // 如果选择器面板当前是激活状态，则立即刷新数据
        if (selectorPanel != null && selectorPanel.activeInHierarchy)
        {
            FetchDataFromAPI();
        }
    }
    
    // 获取当前选择的项目
    public string GetSelectedItem()
    {
        return selectedItem;
    }
    
    // 设置可选项列表
    public void SetItems(List<string> newItems)
    {
        if (newItems != null && newItems.Count > 0)
        {
            items = new List<string>(newItems);
            selectedItem = items[0];
            PopulateScrollRect(itemScrollRect, items);
            UpdateDisplayText();
        }
    }
    
    // 在SingleColumnSelector类中添加以下方法
    
    // 获取当前可用的选项列表
    public List<string> GetItems()
    {
        return new List<string>(items);
    }
    
    // 检查某个项目是否在可用列表中
    public bool ContainsItem(string item)
    {
        return items.Contains(item);
    }
    
    // 获取某个项目在列表中的索引
    public int GetItemIndex(string item)
    {
        return items.IndexOf(item);
    }
}