using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GetDynamicTemperature : MonoBehaviour
{
    [System.Serializable]
    public class TemperatureData
    {
        public List<List<int>> temperatureChannels;
        
        public TemperatureData()
        {
            temperatureChannels = new List<List<int>>();
        }
    }
    
    /// <summary>
    /// 获取动态温度数据的主要方法
    /// </summary>
    /// <param name="companyIndex">公司索引</param>
    /// <param name="caohao">炉号</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="onError">错误回调</param>
    public void GetTemperatureData(string companyIndex, int caohao, 
        System.Action<List<List<int>>> onComplete, 
        System.Action<string> onError = null)
    {
        StartCoroutine(WebAPIPostHelper(companyIndex, caohao, onComplete, onError));
    }
    
    private IEnumerator WebAPIPostHelper(string companyIndex, int caohao, 
        System.Action<List<List<int>>> onComplete, 
        System.Action<string> onError)
    {
        Debug.Log($"Company Index: {companyIndex}");
        Debug.Log($"Caohao: {caohao}");
        
        List<int> list = new List<int>();
        List<int> result1 = new List<int>();
        List<List<int>> tem = new List<List<int>>();
        
        // API URL - 修复为正确的API地址
        string url = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        
        // 构建请求参数 - 修复JSON格式
        string requestData = $"{companyIndex}-8-{caohao}";
        string jsonData = $"\"{requestData}\""; // 直接发送字符串而不是对象
        byte[] bodyRaw = null;
        
        try
        {
            bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        }
        catch (System.Exception ex)
        {
            string errorMsg = $"Failed to encode request data: {ex.Message}";
            Debug.LogError(errorMsg);
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        Debug.Log($"Request data: {jsonData}");
        
        // 创建UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // 发送请求 - 移出try-catch块
            yield return request.SendWebRequest();
            
            // 检查请求结果
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Response: {responseText}");
                
                // 处理响应数据
                try
                {
                    if (ProcessResponseData(responseText, list, result1, tem))
                    {
                        Debug.Log($"Successfully processed {tem.Count} temperature channels");
                        onComplete?.Invoke(tem);
                    }
                    else
                    {
                        onError?.Invoke("Failed to process response data");
                    }
                }
                catch (System.Exception ex)
                {
                    string errorMsg = $"Exception during data processing: {ex.Message}";
                    Debug.LogError(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
            else
            {
                string errorMsg = $"Request failed: {request.error}";
                Debug.LogError(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }
    
    private bool ProcessResponseData(string responseText, List<int> list, List<int> result1, List<List<int>> tem)
    {
        // 检查响应是否为空
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("Response text is null or empty");
            return false;
        }
        
        // 去掉字符串开始和结尾的引号
        string tmpText = responseText.Trim();
        if (tmpText.StartsWith("\"") && tmpText.EndsWith("\""))
        {
            tmpText = tmpText.Substring(1, tmpText.Length - 2);
        }
        
        // 检查Base64字符串是否有效
        if (string.IsNullOrEmpty(tmpText))
        {
            Debug.LogError("Base64 string is null or empty after processing");
            return false;
        }
        
        // Base64填充处理
        int paddingCount = tmpText.Length % 4;
        if (paddingCount > 0)
        {
            tmpText += new string('=', 4 - paddingCount);
        }
        
        Debug.Log($"Processing Base64 string of length: {tmpText.Length}");
        
        // Base64解码
        byte[] tmpBuffer;
        try
        {
            tmpBuffer = Convert.FromBase64String(tmpText);
        }
        catch (FormatException ex)
        {
            Debug.LogError($"Invalid Base64 format: {ex.Message}");
            return false;
        }
        
        Debug.Log($"Buffer length: {tmpBuffer.Length}");
        
        if (tmpBuffer != null && tmpBuffer.Length > 0)
        {
            // 按32字节分组处理
            int jk = tmpBuffer.Length / 32;
            int offset = 0;
            
            for (int j = 0; j < jk; j++)
            {
                for (int jt = 0; jt < 32; jt++)
                {
                    offset = j * 32 + jt;
                    int unsignedByte = tmpBuffer[offset] & 0xFF;
                    list.Add(unsignedByte);
                }
                list.Add(-1); // 使用-1代替null作为分隔符
            }
            
            // 处理剩余字节
            offset = jk * 32;
            int remainingBytes = tmpBuffer.Length % 32;
            if (remainingBytes != 0)
            {
                for (int j = 0; j < remainingBytes; j++)
                {
                    int unsignedByte = tmpBuffer[offset + j] & 0xFF;
                    list.Add(unsignedByte);
                }
            }
            
            // 过滤掉分隔符
            foreach (int value in list)
            {
                if (value != -1)
                {
                    result1.Add(value);
                }
            }
            
            Debug.Log($"Processed data length: {result1.Count}");
            
            // 重组温度数据 - 修正为时间序列格式
            // 假设数据格式：每个时间点包含240个传感器的数据，每个传感器数据占2字节
            int sensorsCount = 240;
            int bytesPerSensor = 2;
            int totalDataPoints = result1.Count / (sensorsCount * bytesPerSensor);
            
            Debug.Log($"Total data points per sensor: {totalDataPoints}");
            
            // 初始化240个传感器的数据列表
            for (int i = 0; i < sensorsCount; i++)
            {
                tem.Add(new List<int>());
            }
            
            // 按时间点遍历数据
            for (int timePoint = 0; timePoint < totalDataPoints; timePoint++)
            {
                // 每个时间点处理240个传感器的数据
                for (int sensorIndex = 0; sensorIndex < sensorsCount; sensorIndex++)
                {
                    int dataIndex = timePoint * sensorsCount * bytesPerSensor + sensorIndex * bytesPerSensor;
                    
                    // 确保不会越界
                    if (dataIndex + 1 < result1.Count)
                    {
                        // 将两个字节组合成一个温度值
                        int temperatureValue = (result1[dataIndex] << 8) + result1[dataIndex + 1];
                        tem[sensorIndex].Add(temperatureValue);
                    }
                }
            }
            
            Debug.Log($"Generated {tem.Count} temperature channels with {(tem.Count > 0 ? tem[0].Count : 0)} data points each");
            return true;
        }
        else
        {
            Debug.LogError("Decoded buffer is null or empty");
            return false;
        }
    }
}