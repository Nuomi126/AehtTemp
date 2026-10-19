using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class RawDataTest : MonoBehaviour
{
    [Header("UI 组件")]
    public Button testButton;
    public TextMeshProUGUI resultText;
    public ScrollRect scrollRect;
    
    [Header("温度数据获取")]
    public GetDynamicTemperature temperatureScript;
    
    [Header("默认参数值")]
    public string defaultCompanyIndex = "1033";
    public int defaultCaohao = 1001;
    
    void Start()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestRawData);
        }
        
        if (resultText != null)
        {
            resultText.text = "点击按钮查看原始数组数据...";
        }
        
        if (temperatureScript == null)
        {
            temperatureScript = FindObjectOfType<GetDynamicTemperature>();
            if (temperatureScript == null)
            {
                temperatureScript = gameObject.AddComponent<GetDynamicTemperature>();
            }
        }
    }
    
    public void TestRawData()
    {
        if (temperatureScript == null)
        {
            UpdateResultText("错误：未找到 GetDynamicTemperature 脚本！");
            return;
        }
        
        UpdateResultText("正在获取原始数据，请稍候...");
        
        temperatureScript.GetTemperatureData(
            defaultCompanyIndex,
            defaultCaohao,
            OnRawDataSuccess,
            OnRawDataError
        );
    }
    
    private void OnRawDataSuccess(List<List<int>> rawData)
    {
        if (rawData == null || rawData.Count == 0)
        {
            UpdateResultText("数据为空！");
            return;
        }
        
        // 直接输出原始数组结构
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 原始数组数据 ===");
        sb.AppendLine($"外层数组长度: {rawData.Count}");
        
        // 显示数组结构
        sb.AppendLine("[");
        
        for (int i = 0; i < rawData.Count; i++)
        {
            var innerArray = rawData[i];
            sb.Append($"  [{i}]: [");
            
            if (innerArray != null && innerArray.Count > 0)
            {
                // 显示前10个数据，如果数据太多就截断
                int showCount = Mathf.Min(10, innerArray.Count);
                for (int j = 0; j < showCount; j++)
                {
                    sb.Append(innerArray[j]);
                    if (j < showCount - 1) sb.Append(", ");
                }
                
                if (innerArray.Count > showCount)
                {
                    sb.Append($", ... (共{innerArray.Count}个数据)");
                }
            }
            
            sb.Append("]");
            if (i < rawData.Count - 1) sb.Append(",");
            sb.AppendLine();
            
            // 如果传感器太多，只显示前20个
            if (i >= 19 && rawData.Count > 20)
            {
                sb.AppendLine($"  ... (还有{rawData.Count - 20}个传感器)");
                break;
            }
        }
        
        sb.AppendLine("]");
        
        // 添加一些统计信息
        sb.AppendLine();
        sb.AppendLine("=== 统计信息 ===");
        sb.AppendLine($"传感器数量: {rawData.Count}");
        
        if (rawData.Count > 0 && rawData[0] != null)
        {
            sb.AppendLine($"第一个传感器数据点数: {rawData[0].Count}");
        }
        
        // 检查所有传感器的数据点数是否一致
        if (rawData.Count > 1)
        {
            bool allSameLength = true;
            int firstLength = rawData[0]?.Count ?? 0;
            
            for (int i = 1; i < rawData.Count; i++)
            {
                if (rawData[i]?.Count != firstLength)
                {
                    allSameLength = false;
                    break;
                }
            }
            
            sb.AppendLine($"所有传感器数据点数一致: {(allSameLength ? "是" : "否")}");
        }
        
        UpdateResultText(sb.ToString());
    }
    
    private void OnRawDataError(string error)
    {
        UpdateResultText($"获取原始数据失败！\n错误信息: {error}");
    }
    
    private void UpdateResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
            
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        Debug.Log($"[原始数据测试] {text}");
    }
    
    public void ClearResult()
    {
        UpdateResultText("结果已清空，点击按钮重新测试...");
    }
}