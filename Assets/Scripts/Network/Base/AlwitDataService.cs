using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 用于处理来自Alwit系统的数据服务
/// </summary>
public class AlwitDataService : ApiService<byte[]>
{
    // API端点
    protected override string Endpoint => "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    
    // 数据类型常量
    public const string MAIN_INFO = "1";           // 主界面实时信息
    public const string SECTION_MONITOR = "2";     // 分区监控实时信息
    public const string SINGLE_TANK_INFO = "3";    // 单槽实时信息
    public const string TANK_CURVE_10MIN = "4";    // 单槽10分钟动态曲线
    public const string ANODE_CURRENT_INFO = "5";  // 单槽阳极电流实时信息
    public const string ANODE_CURVE_10MIN = "6";   // 单槽10分钟阳极电流动态曲线
    public const string TANK_TEMP_INFO = "7";      // 单槽全槽温度实时信息
    public const string TEMP_CURVE_10MIN = "8";    // 单槽10分钟全槽温度动态曲线
    
    // 声明自己的事件，避免直接访问基类事件
    public new event Action<byte[]> OnDataReceived;
    public new event Action<string> OnError;
    
    // 请求完成回调重写
    protected override void OnRequestComplete(string response, bool success)
    {
        if (success)
        {
            try
            {
                // 解析Base64编码的数据
                byte[] data = ParseBase64Response(response);
                if (data != null && data.Length > 0)
                {
                    // 记录日志
                    Debug.Log($"AlwitDataService接收到数据，长度: {data.Length}字节");
                    
                    // 触发事件 - 只触发自己的事件，不尝试调用基类事件
                    OnDataReceived?.Invoke(data);
                }
                else
                {
                    string errorMsg = "数据解析失败或为空";
                    Debug.LogError($"AlwitDataService错误: {errorMsg}");
                    
                    // 只触发自己的事件
                    OnError?.Invoke(errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"数据解析异常: {ex.Message}";
                Debug.LogError($"AlwitDataService错误: {errorMsg}");
                
                // 只触发自己的事件
                OnError?.Invoke(errorMsg);
            }
        }
        else
        {
            Debug.LogError($"AlwitDataService错误: {response}");
            
            // 只触发自己的事件
            OnError?.Invoke(response);
        }
    }
    
    /// <summary>
    /// 解析Base64编码的响应数据
    /// </summary>
    private byte[] ParseBase64Response(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("响应为空");
            return null;
        }
        
        try
        {
            // 检查响应是否以引号开始
            if (response.StartsWith("\"") && response.EndsWith("\""))
            {
                // 去掉字符串开始和结尾的引号
                string base64Text = response.Substring(1, response.Length - 2);
                
                // 将Base64字符串转换为字节数组
                return Convert.FromBase64String(base64Text);
            }
            else if (response.Contains("\""))
            {
                // 尝试提取引号内的内容
                int startPos = response.IndexOf('\"');
                int endPos = response.LastIndexOf('\"');
                
                if (startPos >= 0 && endPos > startPos)
                {
                    string base64Text = response.Substring(startPos + 1, endPos - startPos - 1);
                    return Convert.FromBase64String(base64Text);
                }
            }
            
            // 尝试直接解析为Base64
            try
            {
                return Convert.FromBase64String(response);
            }
            catch
            {
                Debug.LogWarning("响应不是有效的Base64字符串: " + response);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析Base64响应时出错: {ex.Message}, 响应: {response}");
        }
        
        Debug.LogWarning("响应格式不符合预期: " + response);
        return null;
    }
    
    /// <summary>
    /// 获取特定类型的数据
    /// </summary>
    /// <param name="enterpriseId">电解企业标识</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="tankId">槽号</param>
    public void GetAlwitData(string enterpriseId, string dataType, string tankId)
    {
        // 参数验证
        if (string.IsNullOrEmpty(enterpriseId))
        {
            Debug.LogError("企业ID不能为空");
            OnError?.Invoke("企业ID不能为空");
            return;
        }
        
        if (string.IsNullOrEmpty(dataType))
        {
            Debug.LogError("数据类型不能为空");
            OnError?.Invoke("数据类型不能为空");
            return;
        }
        
        // 构建请求参数: 电解企业标识 + "-" + 帧类型 + "-" + 槽号
        string requestParam = $"{enterpriseId}-{dataType}-{tankId}";
        
        // 创建参数字典
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "data", requestParam }
        };
        
        Debug.Log($"发送Alwit数据请求: {requestParam}");
        
        // 发送请求
        FetchData(parameters);
    }
    
    /// <summary>
    /// 将字节数组转换为可读的字符串
    /// </summary>
    public static string ByteArrayToString(byte[] data, int maxRows = 30)
    {
        if (data == null || data.Length == 0)
            return "空数据";
            
        StringBuilder sb = new StringBuilder();
        int bytesPerRow = 32;
        int completeRows = Math.Min(data.Length / bytesPerRow, maxRows);
        bool truncated = completeRows < data.Length / bytesPerRow;
        
        // 处理完整行
        for (int row = 0; row < completeRows; row++)
        {
            for (int col = 0; col < bytesPerRow; col++)
            {
                int offset = row * bytesPerRow + col;
                sb.Append(data[offset].ToString()).Append(",");
            }
            sb.AppendLine();
        }
        
        // 处理最后一行（如果数据没有被截断且有剩余字节）
        if (!truncated)
        {
            int remainingBytes = data.Length % bytesPerRow;
            if (remainingBytes > 0)
            {
                int offset = completeRows * bytesPerRow;
                for (int i = 0; i < remainingBytes; i++)
                {
                    sb.Append(data[offset + i].ToString()).Append(",");
                }
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 从字节数组中提取电压值
    /// </summary>
    public static float ExtractVoltage(byte[] data)
    {
        if (data != null && data.Length >= 20)
        {
            float value = data[18];
            value = (value * 256 + data[19]) / 10;
            return value;
        }
        return 0;
    }
    
    /// <summary>
    /// 从字节数组中提取电流值
    /// </summary>
    public static float ExtractCurrent(byte[] data)
    {
        if (data != null && data.Length >= 22)
        {
            float value = data[20];
            value = (value * 256 + data[21]) / 10;
            return value;
        }
        return 0;
    }
}