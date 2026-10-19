using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleTemperatureTest : MonoBehaviour
{
    [Header("UI组件")]
    public Button testButton;
    public TextMeshProUGUI resultText;
    
    [Header("API管理器")]
    public TemperatureAPIManager temperatureAPI;
    
    void Start()
    {
        // 绑定按钮点击事件
        if (testButton != null)
        {
            testButton.onClick.AddListener(OnTestButtonClicked);
        }
        
        // 检查组件
        if (temperatureAPI == null)
        {
            temperatureAPI = FindObjectOfType<TemperatureAPIManager>();
        }
        
        if (resultText != null)
        {
            resultText.text = "点击按钮获取原始温度数据...";
        }
    }
    
    /// <summary>
    /// 测试按钮点击事件
    /// </summary>
    public void OnTestButtonClicked()
    {
        if (temperatureAPI == null)
        {
            UpdateResultText("错误：未找到TemperatureAPIManager组件！");
            return;
        }
        
        UpdateResultText("正在请求API数据...");
        
        // 调用API获取数据
        // temperatureAPI.GetTemperatureData(
        //     onSuccess: OnAPISuccess,
        //     onError: OnAPIError
        //);
    }
    
    /// <summary>
    /// API成功回调 - 直接显示原始数组
    /// </summary>
    private void OnAPISuccess(List<float> temperatureData)
    {
        if (temperatureData == null || temperatureData.Count == 0)
        {
            UpdateResultText("API返回成功，但数据为空！");
            return;
        }
        
        // 直接输出原始数组内容
        string arrayString = "原始温度数据数组：\n[";
        
        for (int i = 0; i < temperatureData.Count; i++)
        {
            arrayString += temperatureData[i].ToString();
            if (i < temperatureData.Count - 1)
            {
                arrayString += ", ";
            }
        }
        
        arrayString += "]";
        
        UpdateResultText(arrayString);
        
        // 同时在Console中输出，方便复制
        Debug.Log("原始温度数据数组：");
        Debug.Log(arrayString);
    }
    
    /// <summary>
    /// API错误回调
    /// </summary>
    private void OnAPIError(string errorMessage)
    {
        UpdateResultText($"API调用失败：{errorMessage}");
    }
    
    /// <summary>
    /// 更新结果显示文本
    /// </summary>
    private void UpdateResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
        }
        Debug.Log(text);
    }
}