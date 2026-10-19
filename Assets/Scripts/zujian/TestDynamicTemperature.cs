using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestDynamicTemperature : MonoBehaviour
{
    [Header("UI 组件")]
    public Button testButton;
    public TextMeshProUGUI resultText;
    public ScrollRect scrollRect; // 可选：用于滚动查看长文本
    
    [Header("温度数据获取")]
    public GetDynamicTemperature temperatureScript;
    
    [Header("参数输入组件")]
    [Tooltip("公司索引输入框（可选）")]
    public TextMeshProUGUI companyIndexInputField;
    [Tooltip("炉号输入框（可选）")]
    public TextMeshProUGUI caohaoInputField;
    
    [Header("默认参数值")]
    [Tooltip("默认公司索引")]
    public string defaultCompanyIndex = "1";
    [Tooltip("默认炉号")]
    public int defaultCaohao = 1;
    
    void Start()
    {
        // 绑定按钮点击事件
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestTemperatureAPI);
        }
        
        // 初始化文本
        if (resultText != null)
        {
            resultText.text = "点击按钮测试温度数据获取...";
        }
        
        // 如果没有设置温度脚本，尝试获取
        if (temperatureScript == null)
        {
            temperatureScript = FindObjectOfType<GetDynamicTemperature>();
            if (temperatureScript == null)
            {
                // 动态添加组件
                temperatureScript = gameObject.AddComponent<GetDynamicTemperature>();
            }
        }
        
        // 初始化输入框的默认值
        InitializeInputFields();
    }
    
    private void InitializeInputFields()
    {
        // 如果输入框存在且为空，设置默认值
        if (companyIndexInputField != null && string.IsNullOrEmpty(companyIndexInputField.text))
        {
            companyIndexInputField.text = defaultCompanyIndex;
        }
        
        if (caohaoInputField != null && string.IsNullOrEmpty(caohaoInputField.text))
        {
            caohaoInputField.text = defaultCaohao.ToString();
        }
    }
    
    /// <summary>
    /// 获取公司索引值，优先从输入框获取，否则使用默认值
    /// </summary>
    private string GetCompanyIndex()
    {
        if (companyIndexInputField != null && !string.IsNullOrEmpty(companyIndexInputField.text))
        {
            return companyIndexInputField.text.Trim();
        }
        return defaultCompanyIndex;
    }
    
    /// <summary>
    /// 获取炉号值，优先从输入框获取，否则使用默认值
    /// </summary>
    private int GetCaohao()
    {
        if (caohaoInputField != null && !string.IsNullOrEmpty(caohaoInputField.text))
        {
            if (int.TryParse(caohaoInputField.text.Trim(), out int inputValue))
            {
                return inputValue;
            }
            else
            {
                Debug.LogWarning($"炉号输入框的值 '{caohaoInputField.text}' 不是有效的整数，使用默认值 {defaultCaohao}");
            }
        }
        return defaultCaohao;
    }
    
    public void TestTemperatureAPI()
    {
        if (temperatureScript == null)
        {
            UpdateResultText("错误：未找到 GetDynamicTemperature 脚本！");
            return;
        }
        
        // 获取参数值
        string companyIndex = GetCompanyIndex();
        int caohao = GetCaohao();
        
        if (resultText != null)
        {
            resultText.text = $"正在获取温度数据，请稍候...\n公司索引: {companyIndex}\n炉号: {caohao}";
        }
        
        // 调用API，使用获取到的参数值
        temperatureScript.GetTemperatureData(
            companyIndex,  // 从输入框或默认值获取的公司索引
            caohao,        // 从输入框或默认值获取的炉号
            OnTemperatureDataSuccess,  // 成功回调
            OnTemperatureDataError     // 错误回调
        );
    }
    
    private void OnTemperatureDataSuccess(List<List<int>> temperatureData)
    {
        if (temperatureData == null || temperatureData.Count == 0)
        {
            UpdateResultText("获取成功，但数据为空！");
            return;
        }
        
        // 构建显示文本
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"温度数据获取成功！");
        sb.AppendLine($"公司索引: {GetCompanyIndex()}");
        sb.AppendLine($"炉号: {GetCaohao()}");
        sb.AppendLine($"通道数量: {temperatureData.Count}");
        sb.AppendLine($"数据时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("\n=== 数据详情 ===");
        
        // 显示前10个通道的数据作为示例
        int channelsToShow = Mathf.Min(10, temperatureData.Count);
        for (int i = 0; i < channelsToShow; i++)
        {
            var channelData = temperatureData[i];
            sb.AppendLine($"\n通道 {i + 1}: {channelData.Count} 个数据点");
            
            // 显示前5个数据点
            int pointsToShow = Mathf.Min(5, channelData.Count);
            for (int j = 0; j < pointsToShow; j++)
            {
                sb.Append($"{channelData[j]} ");
            }
            if (channelData.Count > pointsToShow)
            {
                sb.Append($"... (还有 {channelData.Count - pointsToShow} 个数据点)");
            }
        }
        
        if (temperatureData.Count > channelsToShow)
        {
            sb.AppendLine($"\n... (还有 {temperatureData.Count - channelsToShow} 个通道)");
        }
        
        // 统计信息
        int totalDataPoints = 0;
        int minValue = int.MaxValue;
        int maxValue = int.MinValue;
        
        foreach (var channel in temperatureData)
        {
            totalDataPoints += channel.Count;
            foreach (var value in channel)
            {
                if (value < minValue) minValue = value;
                if (value > maxValue) maxValue = value;
            }
        }
        
        sb.AppendLine($"\n=== 统计信息 ===");
        sb.AppendLine($"总数据点数: {totalDataPoints}");
        sb.AppendLine($"数值范围: {minValue} - {maxValue}");
        
        UpdateResultText(sb.ToString());
    }
    
    private void OnTemperatureDataError(string error)
    {
        UpdateResultText($"获取温度数据失败！\n公司索引: {GetCompanyIndex()}\n炉号: {GetCaohao()}\n错误信息: {error}\n时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
    
    private void UpdateResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
            
            // 如果有滚动视图，滚动到顶部
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        // 同时在控制台输出
        Debug.Log($"[温度数据测试] {text}");
    }
    
    // 清空结果的方法（可选）
    public void ClearResult()
    {
        UpdateResultText("结果已清空，点击按钮重新测试...");
    }
    
    /// <summary>
    /// 重置输入框为默认值
    /// </summary>
    public void ResetToDefaults()
    {
        if (companyIndexInputField != null)
        {
            companyIndexInputField.text = defaultCompanyIndex;
        }
        
        if (caohaoInputField != null)
        {
            caohaoInputField.text = defaultCaohao.ToString();
        }
        
        UpdateResultText("参数已重置为默认值");
    }
}