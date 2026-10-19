using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using UnityEngine.Networking;

public class HistoryTemperatureTest : MonoBehaviour
{
    [Header("UI 组件")]
    public Button testButton;
    public TextMeshProUGUI resultText;
    public ScrollRect scrollRect;
    public TMP_InputField companyIndexInput;
    public TMP_InputField slotNumberInput;
    public TMP_InputField dateInput;
    
    [Header("默认参数值")]
    public string defaultCompanyIndex = "1033";
    public int defaultSlotNumber = 1001;
    public string defaultDate = "20250622";
    
    [Header("API设置")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    
    [System.Serializable]
    public class HistoryTemperatureData
    {
        public DateTime date;
        public int recordNumber;
        public List<int> temperatureValues; // 253个采样点的温度数据
        
        public HistoryTemperatureData()
        {
            temperatureValues = new List<int>();
        }
    }
    
    void Start()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestHistoryTemperatureData);
        }
        
        if (resultText != null)
        {
            resultText.text = "点击按钮获取单槽温度历史数据...";
        }
        
        // 初始化输入框默认值
        if (companyIndexInput != null)
            companyIndexInput.text = defaultCompanyIndex;
        if (slotNumberInput != null)
            slotNumberInput.text = defaultSlotNumber.ToString();
        if (dateInput != null)
            dateInput.text = defaultDate;
    }
    
    public void TestHistoryTemperatureData()
    {
        // 获取输入参数
        string companyIndex = companyIndexInput != null ? companyIndexInput.text : defaultCompanyIndex;
        string slotNumberStr = slotNumberInput != null ? slotNumberInput.text : defaultSlotNumber.ToString();
        string date = dateInput != null ? dateInput.text : defaultDate;
        
        if (!int.TryParse(slotNumberStr, out int slotNumber))
        {
            UpdateResultText("错误：槽号必须是数字！");
            return;
        }
        
        UpdateResultText("正在获取单槽温度历史数据，请稍候...");
        
        StartCoroutine(GetHistoryTemperatureData(companyIndex, slotNumber, date));
    }
    
    private IEnumerator GetHistoryTemperatureData(string companyIndex, int slotNumber, string date)
    {
        // 构建请求参数：电解企业标识 + "-67-" + 槽号 + "-" + 日期
        string requestData = $"{companyIndex}-67-{slotNumber}-{date}";
        string jsonData = $"\"{requestData}\"";
        
        Debug.Log($"请求参数: {requestData}");
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"API响应: {responseText}");
                
                try
                {
                    ProcessHistoryTemperatureResponse(responseText, companyIndex, slotNumber, date);
                }
                catch (System.Exception ex)
                {
                    UpdateResultText($"数据处理失败！\n错误信息: {ex.Message}");
                    Debug.LogError($"数据处理异常: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                UpdateResultText($"API请求失败！\n错误信息: {request.error}");
                Debug.LogError($"API请求失败: {request.error}");
            }
        }
    }
    
    private void ProcessHistoryTemperatureResponse(string responseText, string companyIndex, int slotNumber, string date)
    {
        // 去掉响应字符串的引号
        string base64Data = responseText.Trim();
        if (base64Data.StartsWith("\"") && base64Data.EndsWith("\""))
        {
            base64Data = base64Data.Substring(1, base64Data.Length - 2);
        }
        
        // Base64填充处理
        int paddingCount = base64Data.Length % 4;
        if (paddingCount > 0)
        {
            base64Data += new string('=', 4 - paddingCount);
        }
        
        // Base64解码
        byte[] frameData;
        try
        {
            frameData = Convert.FromBase64String(base64Data);
        }
        catch (FormatException ex)
        {
            UpdateResultText($"Base64解码失败！\n错误信息: {ex.Message}");
            return;
        }
        
        Debug.Log($"解码后数据长度: {frameData.Length} 字节");
        
        // 解析帧格式
        if (!ParseFrameData(frameData, out List<HistoryTemperatureData> historyData))
        {
            UpdateResultText("帧数据解析失败！");
            return;
        }
        
        // 显示解析结果
        DisplayHistoryTemperatureData(historyData, companyIndex, slotNumber, date);
    }
    
    private bool ParseFrameData(byte[] frameData, out List<HistoryTemperatureData> historyData)
    {
        historyData = new List<HistoryTemperatureData>();
        
        if (frameData.Length < 14) // 最小帧长度：帧头(2) + 企业标识(2) + 帧类型(2) + 槽号(2) + 数据长度(4) + 帧尾(2)
        {
            Debug.LogError("帧数据长度不足");
            return false;
        }
        
        int offset = 0;
        
        // 验证帧头：170, 85
        if (frameData[offset] != 170 || frameData[offset + 1] != 85)
        {
            Debug.LogError($"帧头验证失败: {frameData[offset]}, {frameData[offset + 1]} (期望: 170, 85)");
            return false;
        }
        offset += 2;
        
        // 读取服务电解企业标识 (2字节)
        int enterpriseId = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;
        
        // 读取帧类型 (2字节)
        int frameType = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;
        
        // 读取槽号 (2字节)
        int slotNumber = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;
        
        // 读取有效数据长度 (4字节)
        int dataLength = (frameData[offset] << 24) | (frameData[offset + 1] << 16) | 
                        (frameData[offset + 2] << 8) | frameData[offset + 3];
        offset += 4;
        
        Debug.Log($"企业标识: {enterpriseId}, 帧类型: {frameType}, 槽号: {slotNumber}, 数据长度: {dataLength}");
        
        // 验证数据长度
        if (offset + dataLength + 2 > frameData.Length)
        {
            Debug.LogError($"数据长度验证失败: 期望 {offset + dataLength + 2}, 实际 {frameData.Length}");
            return false;
        }
        
        // 验证帧尾
        int tailOffset = offset + dataLength;
        if (frameData[tailOffset] != 165 || frameData[tailOffset + 1] != 90)
        {
            Debug.LogError($"帧尾验证失败: {frameData[tailOffset]}, {frameData[tailOffset + 1]} (期望: 165, 90)");
            return false;
        }
        
        // 解析历史温度数据
        // 数据长度为512的整数倍，每512字节包含：日期(4字节) + 记录号(2字节) + 253个采样点数据(每个2字节)
        int recordSize = 512;
        int recordCount = dataLength / recordSize;
        
        Debug.Log($"历史记录数量: {recordCount}");
        
        for (int i = 0; i < recordCount; i++)
        {
            int recordOffset = offset + i * recordSize;
            
            var historyRecord = new HistoryTemperatureData();
            
            // 读取日期 (4字节) - 假设为Unix时间戳
            int dateValue = (frameData[recordOffset] << 24) | (frameData[recordOffset + 1] << 16) |
                           (frameData[recordOffset + 2] << 8) | frameData[recordOffset + 3];
            
            // 将日期值转换为DateTime（这里需要根据实际格式调整）
            try
            {
                historyRecord.date = DateTimeOffset.FromUnixTimeSeconds(dateValue).DateTime;
            }
            catch
            {
                // 如果不是Unix时间戳，使用其他方式解析
                historyRecord.date = DateTime.Now;
            }
            
            // 读取记录号 (2字节)
            historyRecord.recordNumber = (frameData[recordOffset + 4] << 8) | frameData[recordOffset + 5];
            
            // 读取253个采样点数据 (每个2字节)
            for (int j = 0; j < 253; j++)
            {
                int sampleOffset = recordOffset + 6 + j * 2;
                int temperatureValue = (frameData[sampleOffset] << 8) | frameData[sampleOffset + 1];
                historyRecord.temperatureValues.Add(temperatureValue);
            }
            
            historyData.Add(historyRecord);
        }
        
        return true;
    }
    
    private void DisplayHistoryTemperatureData(List<HistoryTemperatureData> historyData, string companyIndex, int slotNumber, string date)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 单槽温度历史数据 ===");
        sb.AppendLine($"企业标识: {companyIndex}");
        sb.AppendLine($"槽号: {slotNumber}");
        sb.AppendLine($"查询日期: {date}");
        sb.AppendLine($"历史记录数量: {historyData.Count}");
        sb.AppendLine();
        
        for (int i = 0; i < historyData.Count; i++) // 移除 && i < 10 限制，显示所有记录
        {
            var record = historyData[i];
            sb.AppendLine($"记录 {i + 1}:");
            sb.AppendLine($"  日期: {record.date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  记录号: {record.recordNumber}");
            sb.AppendLine($"  采样点数量: {record.temperatureValues.Count}");
            
            if (record.temperatureValues.Count > 0)
            {
                // 显示前5个和后5个采样点数据
                sb.Append("  前5个采样点: ");
                for (int j = 0; j < Math.Min(5, record.temperatureValues.Count); j++)
                {
                    sb.Append($"{record.temperatureValues[j]}");
                    if (j < Math.Min(4, record.temperatureValues.Count - 1)) sb.Append(", ");
                }
                
                if (record.temperatureValues.Count > 10)
                {
                    sb.Append(" ... ");
                    int startIndex = Math.Max(0, record.temperatureValues.Count - 5);
                    for (int j = startIndex; j < record.temperatureValues.Count; j++)
                    {
                        sb.Append($"{record.temperatureValues[j]}");
                        if (j < record.temperatureValues.Count - 1) sb.Append(", ");
                    }
                }
                sb.AppendLine();
                
                // 计算温度统计信息
                
            }
            sb.AppendLine();
        }
        
        // 移除这个条件判断，因为现在显示所有数据
        // if (historyData.Count > 10)
        // {
        //     sb.AppendLine($"... 还有 {historyData.Count - 10} 条记录未显示");
        // }
        
        UpdateResultText(sb.ToString());
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
        
        Debug.Log($"[历史温度数据测试] {text}");
    }
    
    public void ClearResult()
    {
        UpdateResultText("结果已清空，点击按钮重新测试...");
    }
    
    public void SetCurrentDate()
    {
        if (dateInput != null)
        {
            dateInput.text = DateTime.Now.ToString("yyyyMMdd");
        }
    }
}