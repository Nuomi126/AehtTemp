using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // 新增：用于ScrollRect

public class RealTimeDataManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private ScrollRect displayScrollView; // 修改：主显示改为滚动视图
    [SerializeField] private TextMeshProUGUI displayText; // 保留：滚动视图内的文本组件
    [SerializeField] private ScrollRect sensorAlarmScrollView; // 修改：传感器预警改为滚动视图
    [SerializeField] private TextMeshProUGUI sensorAlarmText; // 保留：滚动视图内的文本组件
    [SerializeField] private ScrollRect controllerFaultScrollView; // 修改：槽控机故障改为滚动视图
    [SerializeField] private TextMeshProUGUI controllerFaultText; // 保留：滚动视图内的文本组件
    
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    
    [Header("更新设置")]
    [SerializeField] private float updateInterval = 2f; // 2秒更新间隔
    
    [Header("调试设置")]
    [SerializeField] private bool enableDetailedDebug = true; // 启用详细调试
    [SerializeField] private bool showRawBytes = true; // 显示原始字节数据
    [SerializeField] private bool showAllDataGroups = false; // 显示所有数据组
    [SerializeField] private bool showProtocolParsing = true; // 新增：显示协议解析调试信息
    
    private Coroutine dataUpdateCoroutine;
    private List<string> alStr = new List<string>(); // 预警信息字符串列表
    private int alarmInfoIndex = 0;
	private float timer = 0f;
	private float interval = 1.5f; // 预警信息轮播间隔时间（秒）

	// 数据结构
	[System.Serializable]
    public class RealTimeData
    {
        public List<List<int>> dataGroups;
        public List<SensorAlarmInfo> sensorAlarms; // 新增：传感器预警信息
        public List<ControllerFaultInfo> controllerFaults; // 新增：槽控机故障信息
        
        public RealTimeData()
        {
            dataGroups = new List<List<int>>();
            sensorAlarms = new List<SensorAlarmInfo>();
            controllerFaults = new List<ControllerFaultInfo>();
        }
    }
    
    // 新增：传感器预警信息结构
    [System.Serializable]
    public class SensorAlarmInfo
    {
        public int slotNumber;      // 槽号（2字节）
        public int alarmType;       // 报警类型（1字节）
        public int position;        // 部位（1字节）
        public int direction;       // 方向A/B（1字节）
        public int cathode;         // 阴极1/2（1字节）
        public int nodeNumber;      // 节点号（1字节）
        public int alarmReason;     // 报警原因（1字节）
        public int month;           // 月（1字节）
        public int day;             // 日（1字节）
        public int hour;            // 时（1字节）
        public int minute;          // 分（1字节）
        
        public string GetAlarmTypeDescription()
        {
            if (alarmType == 9009)
            {
                return "全槽温度预警";
            }
            else if (alarmType > 9000)
            {
                return "传感器预警";
            }
            return $"类型{alarmType}";
        }
        
        public string GetDirectionDescription()
        {
            switch (direction)
            {
                case 1: return "A面";
                case 2: return "B面";
                case 0: return "";
                default: return $"方向{direction}";
            }
        }
        
        public string GetPositionDescription()
        {
            switch (position)
            {
                case 1: return "钢窗";
                case 2: return "出铝端";
                case 3: return "烟道端";
                case 4: return "阴极钢棒";
                case 5: return "炉底";
                default: return $"部位{position}";
            }
        }
        
        public string GetCathodeDescription()
        {
            if (position == 4) // 阴极钢棒
            {
                switch (cathode)
                {
                    case 1: return "-1";
                    case 2: return "-2";
                    case 0: return "";
                    default: return $"-{cathode}";
                }
            }
            return "";
        }
        
        public string GetAlarmReasonDescription()
        {
            switch (alarmReason)
            {
                case 1: return "绝对与爬升温度判断成立";
                case 2: return "绝对或爬升温度判断成立";
                case 3: return "槽底绝对或爬升温度判断成立";
                case 4: return "最高温度判断成立";
                case 5: return "同组温差异常判断成立";
                case 6: return "相邻温差异常判断成立";
                default: return $"原因{alarmReason}";
            }
        }
        
        public string GetLocationDescription()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetPositionDescription());
            
            string dirDesc = GetDirectionDescription();
            if (!string.IsNullOrEmpty(dirDesc))
            {
                sb.Append(dirDesc);
            }
            
            string cathodeDesc = GetCathodeDescription();
            if (!string.IsNullOrEmpty(cathodeDesc))
            {
                sb.Append(cathodeDesc);
            }
            
            if (nodeNumber > 0)
            {
                sb.Append($"-{nodeNumber}");
            }
            
            return sb.ToString();
        }
        
        public string GetTimeString()
        {
            return $"{month:D2}/{day:D2}-{hour:D2}:{minute:D2}";
        }
    }
    
    // 槽控机故障信息结构 - 更新描述方法
    [System.Serializable]
    public class ControllerFaultInfo
    {
        public int slotNumber;      // 槽号（2字节）
        public int faultType;       // 故障类型（1字节）
        public int month;           // 月（1字节）
        public int day;             // 日（1字节）
        public int hour;            // 时（1字节）
        public int minute;          // 分（1字节）
        
        public string GetFaultTypeDescription()
        {
            switch (faultType)
            {
                // 第一类故障代码
                case 1: return "主粘连";
                case 2: return "正粘连";
                case 3: return "反粘连";
                case 4: return "主未合";
                case 5: return "正未合";
                case 6: return "反未合";
                case 8: return "电源跳";
                case 10: return "定时超";
                case 13: return "母线上限";
                case 14: return "母线下限";
                case 17: return "缺相";
                case 40: return "纯手动";
                case 60: return "按钮锁";
                
                // 第二类故障代码
                case 121: return "控制版故障";
                case 122: return "槽控机门打开";
                case 124: return "纯手动阳降";
                case 125: return "纯手动阳升";
                case 126: return "纯手动下料";
                case 127: return "纯手动打壳";
                case 128: return "不保护/不定时";
                case 133: return "下料故障";
                
                // 第三类故障代码
                case 618: return "电流异常";
                case 619: return "通信断";
                case 620: return "电压异常";
                
                default: return $"故障类型{faultType}";
            }
        }
        
        public string GetTimeString()
        {
            return $"{month:D2}/{day:D2}-{hour:D2}:{minute:D2}";
        }
    }

    private void Start()
    {
        // 开始定时获取数据
        StartRealTimeDataUpdate();
    }

    private void Update()
    {
        int numOfPage = 9; // 每页显示9条预警信息
        // 累加每帧的时间
		timer += Time.deltaTime;

		// 当累积时间达到间隔时间时执行操作
		if (timer >= interval)
		{
			string alStrTmp = "";
			// 如果告警信息数量少于一页，退出轮播
			if (alStr.Count > 0 && alStr.Count <= numOfPage)
            {
                if (sensorAlarmText.text == "")
                {
					for (int i = 0; i < alStr.Count; i++)
					{						
						alStrTmp = alStrTmp + alStr[i] + "\n";
					}
					sensorAlarmText.text = alStrTmp;
					alStrTmp = "";
				}
            }
            else if (alStr.Count > 0 && alStr.Count > (alarmInfoIndex * numOfPage - 1) && Input.touchCount <= 0)
            {
                sensorAlarmText.text = "";
                // 每页9条预警信息，轮播显示 2025.07.29
                for (int j = alarmInfoIndex * numOfPage; j < alStr.Count; j++)
                {
                    if (j >= (alarmInfoIndex + 1) * numOfPage)
                    {
                        break;
                    }
                    alStrTmp = alStrTmp + alStr[j] + "\n";
                }
                sensorAlarmText.text = alStrTmp;
                alarmInfoIndex++;
                alStrTmp = "";
            }
            else if (alarmInfoIndex * numOfPage - 1 > alStr.Count)
            {
                alarmInfoIndex = 0;
            }
			// 重置计时器（也可以减去间隔时间，避免时间误差累积）
			timer = 0f;
		}		
	}

	private void OnDestroy()
    {
        // 停止数据更新
        StopRealTimeDataUpdate();
    }
    
    public void StartRealTimeDataUpdate()
    {
        if (dataUpdateCoroutine != null)
        {
            StopCoroutine(dataUpdateCoroutine);
        }
        
        dataUpdateCoroutine = StartCoroutine(UpdateDataPeriodically());
        Debug.Log("开始实时数据更新");
    }
    
    public void StopRealTimeDataUpdate()
    {
        if (dataUpdateCoroutine != null)
        {
            StopCoroutine(dataUpdateCoroutine);
            dataUpdateCoroutine = null;
        }
        
        Debug.Log("停止实时数据更新");
    }
    
    private IEnumerator UpdateDataPeriodically()
    {
        while (true)
        {
            yield return StartCoroutine(GetRealTimeDataAsync());
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    public IEnumerator GetRealTimeDataAsync()
    {
        // 检查是否有企业标识
        if (string.IsNullOrEmpty(GlobalUserData.AlSerialPantId))
        {
            Debug.LogWarning("企业标识为空，无法获取实时数据");
            yield break;
        }
        
        string companyIndex = GlobalUserData.AlSerialPantId;
        
        Debug.Log($"=== 开始获取实时数据 ===");
        Debug.Log($"企业标识: {companyIndex}");
        Debug.Log($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

        // 构建请求参数：企业标识 + "-1-1001"
        // 帧类型1：主界面实时信息（2秒）
        //20251103,cxt
        //帧类型1的槽号可以设置为0;
        string cellid = "0";
        string requestParam = $"{companyIndex}-1-{cellid}";
        Debug.Log($"请求参数: {requestParam}");
        
        // 创建JSON数据
        string jsonData = JsonConvert.SerializeObject(requestParam);
        Debug.Log($"JSON数据: {jsonData}");
        Debug.Log($"JSON数据长度: {jsonData.Length} 字符");
        
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
        Debug.Log($"请求字节数组长度: {byteArray.Length} 字节");
        
        if (enableDetailedDebug)
        {
            Debug.Log($"请求字节数组内容: {BitConverter.ToString(byteArray)}");
        }
        
        // 创建UnityWebRequest
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        Debug.Log($"发送API请求到: {apiUrl}");
        Debug.Log($"请求方法: POST");
        Debug.Log($"Content-Type: application/json");
        
        // 记录请求开始时间
        DateTime requestStartTime = DateTime.Now;
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        // 记录请求结束时间
        DateTime requestEndTime = DateTime.Now;
        TimeSpan requestDuration = requestEndTime - requestStartTime;
        
        Debug.Log($"请求完成，耗时: {requestDuration.TotalMilliseconds:F2} 毫秒");
        Debug.Log($"请求状态: {webRequest.result}");
        Debug.Log($"HTTP响应码: {webRequest.responseCode}");
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"原始响应长度: {responseText.Length} 字符");
            Debug.Log($"原始响应内容: '{responseText}'");
            
            if (enableDetailedDebug)
            {
                // 显示响应的前100个字符（如果太长的话）
                string previewText = responseText.Length > 100 ? responseText.Substring(0, 100) + "..." : responseText;
                Debug.Log($"响应预览: {previewText}");
                
                // 检查响应是否包含特殊字符
                bool hasSpecialChars = responseText.Contains("\n") || responseText.Contains("\r") || responseText.Contains("\t");
                Debug.Log($"响应包含特殊字符: {hasSpecialChars}");
            }
            
            // 解析响应数据
            RealTimeData parsedData = ParseRealTimeData(responseText);
            
            if (parsedData != null && parsedData.dataGroups.Count > 0)
            {
                // 更新UI显示
                UpdateUI(parsedData);
                Debug.Log($"✅ 成功获取并解析了 {parsedData.dataGroups.Count} 组数据");
                
                if (enableDetailedDebug)
                {
                    LogDataGroupsSummary(parsedData);
                }
            }
            else
            {
                Debug.LogWarning("❌ 解析数据失败或数据为空");
            }
        }
        else
        {
            Debug.LogError($"❌ 获取实时数据失败: {webRequest.error}");
            Debug.LogError($"HTTP响应码: {webRequest.responseCode}");
            
            if (webRequest.downloadHandler != null)
            {
                string errorResponse = webRequest.downloadHandler.text;
                Debug.LogError($"错误响应内容: '{errorResponse}'");
                Debug.LogError($"错误响应长度: {errorResponse.Length} 字符");
            }
        }
        
        // 清理资源
        webRequest.Dispose();
        Debug.Log($"=== 实时数据获取完成 ===");
    }
    
    private RealTimeData ParseRealTimeData(string responseText)
    {
        try
        {
            Debug.Log($"🔍 开始解析响应数据...");
            RealTimeData result = new RealTimeData();
            
            // 去除首尾双引号
            string cleanResponse = responseText;
            bool hadQuotes = responseText.StartsWith("\"") && responseText.EndsWith("\"");
            
            if (hadQuotes)
            {
                cleanResponse = responseText.Substring(1, responseText.Length - 2);
                Debug.Log($"✂️ 去除了首尾双引号");
            }
            
            // Base64解码
            string base64Text = cleanResponse;
            int paddingCount = base64Text.Length % 4;
            if (paddingCount > 0)
            {
                int paddingNeeded = 4 - paddingCount;
                for (int i = 0; i < paddingNeeded; i++)
                {
                    base64Text += "=";
                }
            }
            
            byte[] decodedBytes;
            try
            {
                decodedBytes = Convert.FromBase64String(base64Text);
                Debug.Log($"✅ Base64解码成功，数据长度: {decodedBytes.Length} 字节");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Base64解码失败: {ex.Message}");
                return null;
            }
            
            // 按照协议格式解析数据帧
            int offset = 0;
            int protocolGroups = 0; // 添加protocolGroups变量定义
            
            while (offset < decodedBytes.Length)
            {
                // 检查是否有足够的字节来读取帧头
                if (offset + 14 > decodedBytes.Length)
                {
                    Debug.LogWarning($"⚠️ 剩余数据不足以构成完整帧，跳过 {decodedBytes.Length - offset} 字节");
                    break;
                }
                
                // 检查帧头 (170, 85)
                if (decodedBytes[offset] != 170 || decodedBytes[offset + 1] != 85)
                {
                    Debug.LogWarning($"⚠️ 帧头不匹配，期望[170,85]，实际[{decodedBytes[offset]},{decodedBytes[offset + 1]}]，跳过此字节");
                    offset++;
                    continue;
                }
                
                // 读取帧信息
                int frameStart = offset;
                offset += 2; // 跳过帧头
                
                // 读取服务电解企业标识 (2字节)
                int companyId = (decodedBytes[offset] << 8) | decodedBytes[offset + 1];
                offset += 2;
                
                // 读取帧类型 (2字节)
                int frameType = (decodedBytes[offset] << 8) | decodedBytes[offset + 1];
                offset += 2;
                
                // 读取槽号 (2字节)
                int slotNumber = (decodedBytes[offset] << 8) | decodedBytes[offset + 1];
                offset += 2;
                
                // 读取有效数据长度 (4字节)
                int dataLength = (decodedBytes[offset] << 24) | (decodedBytes[offset + 1] << 16) | 
                               (decodedBytes[offset + 2] << 8) | decodedBytes[offset + 3];
                offset += 4;
                
                // 检查数据长度是否合理
                if (dataLength < 0 || offset + dataLength + 2 > decodedBytes.Length)
                {
                    Debug.LogError($"❌ 数据长度异常: {dataLength}，剩余字节: {decodedBytes.Length - offset}");
                    offset++;
                    continue;
                }
                
                // 读取数据内容
                byte[] frameData = new byte[dataLength];
                Array.Copy(decodedBytes, offset, frameData, 0, dataLength);
                offset += dataLength;
                
                // 检查帧尾 (165, 90)
                if (offset + 2 > decodedBytes.Length || 
                    decodedBytes[offset] != 165 || decodedBytes[offset + 1] != 90)
                {
                    Debug.LogWarning($"⚠️ 帧尾不匹配，期望[165,90]，实际[{decodedBytes[offset]},{decodedBytes[offset + 1]}]");
                    offset++;
                    continue;
                }
                offset += 2; // 跳过帧尾
                
                Debug.Log($"✅ 解析到完整数据帧: 企业ID={companyId}, 帧类型={frameType}, 槽号={slotNumber}, 数据长度={dataLength}");
                
                // 解析帧数据内容（按13字节分组）
                int frameProtocolGroups = ParseFrameData(frameData, frameType, slotNumber, result);
                protocolGroups += frameProtocolGroups; // 累加协议组数量
            }
            
            Debug.Log($"✅ 总计解析出 {result.dataGroups.Count} 个数据组");
            Debug.Log($"✅ 解析出 {result.sensorAlarms.Count} 个传感器预警");
            Debug.Log($"✅ 解析出 {result.controllerFaults.Count} 个槽控机故障");
            Debug.Log($"✅ 总计 {protocolGroups} 个协议数据组");
            
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析实时数据时出错: {ex.Message}");
            Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            return null;
        }
    }
    
    // 修改ParseFrameData方法，返回解析的协议组数量
    private int ParseFrameData(byte[] frameData, int frameType, int slotNumber, RealTimeData result)
    {
        try
        {
            int protocolGroupCount = 0;
            
            // 将字节数组转换为整数列表
            List<int> allData = new List<int>();
            for (int i = 0; i < frameData.Length; i++)
            {
                allData.Add(frameData[i] & 0xFF);
            }
            
            // 按13字节分组解析
            for (int i = 0; i < allData.Count - 12; i += 13)
            {
                List<int> group = new List<int>();
                for (int j = i; j < i + 13 && j < allData.Count; j++)
                {
                    group.Add(allData[j]);
                }
                
                if (group.Count == 13)
                {
                    result.dataGroups.Add(group);
                    
                    // 根据T_1值解析不同类型的数据
                    int t1 = group[0];
                    
                    if (t1 == 253) // 传感器预警信息
                    {
                        SensorAlarmInfo alarm = ParseSensorAlarm(group);
                        if (alarm != null)
                        {
                            result.sensorAlarms.Add(alarm);
                            protocolGroupCount++;
                            if (showProtocolParsing)
                            {
                                Debug.Log($"✅ 解析到传感器预警: 槽号{alarm.slotNumber}, {alarm.GetAlarmTypeDescription()}");
                            }
                        }
                    }
                    else if (t1 == 251) // 槽控机故障信息
                    {
                        ControllerFaultInfo fault = ParseControllerFault(group);
                        if (fault != null)
                        {
                            result.controllerFaults.Add(fault);
                            protocolGroupCount++;
                            if (showProtocolParsing)
                            {
                                Debug.Log($"✅ 解析到槽控机故障: 槽号{fault.slotNumber}, {fault.GetFaultTypeDescription()}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"⚠️ 跳过不完整的组，只有 {group.Count} 个元素");
                }
            }
            
            return protocolGroupCount; // 返回解析的协议组数量
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析帧数据失败: {ex.Message}");
            return 0; // 发生异常时返回0
        }
    }
    
    // 新增：检查字符串是否为有效的Base64
    private bool IsValidBase64String(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
            
        // Base64字符集
        foreach (char c in s)
        {
            if (!char.IsLetterOrDigit(c) && c != '+' && c != '/' && c != '=')
                return false;
        }
        
        return true;
    }
    
    // 新增：记录原始字节数据
    private void LogRawBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return;
            
        Debug.Log($"📊 原始字节数据 (前50字节):");
        
        StringBuilder hexBuilder = new StringBuilder();
        StringBuilder decBuilder = new StringBuilder();
        
        int displayCount = Mathf.Min(50, bytes.Length);
        
        for (int i = 0; i < displayCount; i++)
        {
            hexBuilder.Append($"{bytes[i]:X2} ");
            decBuilder.Append($"{bytes[i]:D3} ");
            
            // 每16个字节换行
            if ((i + 1) % 16 == 0)
            {
                hexBuilder.AppendLine();
                decBuilder.AppendLine();
            }
        }
        
        if (bytes.Length > displayCount)
        {
            hexBuilder.Append("...");
            decBuilder.Append("...");
        }
        
        Debug.Log($"十六进制:\n{hexBuilder}");
        Debug.Log($"十进制:\n{decBuilder}");
    }
    
    // 新增：记录所有数据的预览
    private void LogAllDataPreview(List<int> allData)
    {
        if (allData == null || allData.Count == 0)
            return;
            
        Debug.Log($"📈 所有解析数据预览 (前50个):");
        
        StringBuilder sb = new StringBuilder();
        int displayCount = Mathf.Min(50, allData.Count);
        
        for (int i = 0; i < displayCount; i++)
        {
            sb.Append($"{allData[i]:D3} ");
            
            // 每10个数字换行
            if ((i + 1) % 10 == 0)
            {
                sb.AppendLine();
            }
        }
        
        if (allData.Count > displayCount)
        {
            sb.Append("...");
        }
        
        Debug.Log(sb.ToString());
        
        // 显示关键位置的数据
        if (allData.Count > 12)
        {
            Debug.Log($"索引12处的数据 (分组起始): {allData[12]}");
        }
    }
    
    // 新增：记录数据组摘要
    private void LogDataGroupsSummary(RealTimeData data)
    {
        if (data == null || data.dataGroups.Count == 0)
            return;
            
        Debug.Log($"📋 数据组摘要:");
        
        int displayCount = showAllDataGroups ? data.dataGroups.Count : Mathf.Min(5, data.dataGroups.Count);
        
        for (int i = 0; i < displayCount; i++)
        {
            List<int> group = data.dataGroups[i];
            Debug.Log($"组 {i + 1}: [{string.Join(", ", group)}]");
        }
        
        if (!showAllDataGroups && data.dataGroups.Count > 5)
        {
            Debug.Log($"... 还有 {data.dataGroups.Count - 5} 个数据组 (设置showAllDataGroups=true查看全部)");
        }
    }
    
    private void UpdateUI(RealTimeData data)
    {
        // 更新主显示文本
        if (displayText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"🔄 实时数据 (更新时间: {DateTime.Now:HH:mm:ss})");
            sb.AppendLine($"🏢 企业: {GlobalUserData.AlSerialPantIdName}");
            sb.AppendLine($"📊 数据组数量: {data.dataGroups.Count}");
            sb.AppendLine($"⚠️ 传感器预警: {data.sensorAlarms.Count}");
            sb.AppendLine($"🔧 槽控机故障: {data.controllerFaults.Count}");
            sb.AppendLine();
            
            // 显示前几组原始数据作为示例
            int displayCount = Mathf.Min(3, data.dataGroups.Count);
            for (int i = 0; i < displayCount; i++)
            {
                sb.AppendLine($"📈 数据组 {i + 1}:");
                List<int> group = data.dataGroups[i];
                
                // 显示完整的13个数据
                for (int j = 0; j < group.Count; j++)
                {
                    sb.Append($"{group[j]:D3} ");
                    
                    // 每5个数字换行
                    if ((j + 1) % 5 == 0 && j < group.Count - 1)
                    {
                        sb.AppendLine();
                        sb.Append("     "); // 缩进
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine();
            }
            
            if (data.dataGroups.Count > displayCount)
            {
                sb.AppendLine($"... 还有 {data.dataGroups.Count - displayCount} 组数据");
            }
            
            // 添加调试信息
            if (enableDetailedDebug)
            {
                sb.AppendLine();
                sb.AppendLine($"🔧 调试信息:");
                sb.AppendLine($"   详细调试: {enableDetailedDebug}");
                sb.AppendLine($"   显示原始字节: {showRawBytes}");
                sb.AppendLine($"   显示所有组: {showAllDataGroups}");
                sb.AppendLine($"   协议解析调试: {showProtocolParsing}");
            }
            
            displayText.text = sb.ToString();
            
            // 动态设置content高度
            SetContentHeight(displayScrollView, displayText);
        }
        
        // 更新传感器预警信息显示
        UpdateSensorAlarmUI(data.sensorAlarms);
        
        // 更新槽控机故障信息显示
        UpdateControllerFaultUI(data.controllerFaults);
    }
    
    // 新增：解析传感器预警信息
    private SensorAlarmInfo ParseSensorAlarm(List<int> data)
    {
        if (data == null || data.Count != 13 || data[0] != 253)
        {
            return null;
        }
        
        try
        {
            SensorAlarmInfo alarm = new SensorAlarmInfo();
            
            // 槽号（第2-3字节，高字节H和低字节L，值=H*256+L）
            alarm.slotNumber = data[1] * 256 + data[2];
            
            // 报警类型（第4字节）
            alarm.alarmType = data[3];
            
            // 报警位置信息（第5-8字节）
            alarm.position = data[4];      // 部位
            alarm.direction = data[5];     // 方向（A/B）
            alarm.cathode = data[6];       // 阴极（1/2）
            alarm.nodeNumber = data[7];    // 节点号
            
            // 报警原因（第9字节）
            alarm.alarmReason = data[8];
            
            // 报警时间（第10-13字节）
            alarm.month = data[9];
            alarm.day = data[10];
            alarm.hour = data[11];
            alarm.minute = data[12];
            
            return alarm;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析传感器预警信息失败: {ex.Message}");
            return null;
        }
    }
    
    // 新增：解析槽控机故障信息
    private ControllerFaultInfo ParseControllerFault(List<int> data)
    {
        if (data == null || data.Count != 13 || data[0] != 251)
        {
            return null;
        }
        
        try
        {
            ControllerFaultInfo fault = new ControllerFaultInfo();
            
            // 槽号（第2-3字节，高字节H和低字节L，值=H*256+L）
            fault.slotNumber = data[1] * 256 + data[2];
            
            // 故障类型（第4字节）
            fault.faultType = data[3];
            
            // 第5-8字节为0（保留字段）
            
            // 报警时间（第10-13字节）
            fault.month = data[9];
            fault.day = data[10];
            fault.hour = data[11];
            fault.minute = data[12];
            
            return fault;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析槽控机故障信息失败: {ex.Message}");
            return null;
        }
    }
    
    // 新增：更新传感器预警信息UI
    private void UpdateSensorAlarmUI(List<SensorAlarmInfo> alarms)
    {
        alStr.Clear();

		if (sensorAlarmText == null)
        {
            Debug.LogWarning("传感器预警文本组件未设置");
            return;
        }
        
        StringBuilder sb = new StringBuilder();        
        
        if (alarms.Count == 0)
        {
            sb.AppendLine("暂无传感器预警信息");
        }
        else
        {
            for (int i = 0; i < alarms.Count; i++)
            {
                SensorAlarmInfo alarm = alarms[i];
                string alarmText = $"{alarm.slotNumber}号槽：{alarm.GetTimeString()} 【{alarm.GetLocationDescription()}】{alarm.GetAlarmTypeDescription()} ({alarm.GetAlarmReasonDescription()})";
                sb.AppendLine(alarmText);
                alStr.Add(alarmText);
				//alStr.Add(alarmText);
            }
        }
		
		//sensorAlarmText.text = sb.ToString();

		// 动态设置content高度
		SetContentHeight(sensorAlarmScrollView, sensorAlarmText);
    }
    
    // 新增：更新槽控机故障信息UI
    private void UpdateControllerFaultUI(List<ControllerFaultInfo> faults)
    {
        if (controllerFaultText == null)
        {
            Debug.LogWarning("槽控机故障文本组件未设置");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        List<string> alStr = new List<string>();
        
        if (faults.Count == 0)
        {
            sb.AppendLine("暂无槽控机故障信息");
        }
        else
        {
            for (int i = 0; i < faults.Count; i++)
            {
                ControllerFaultInfo fault = faults[i];
                string faultText = $"{fault.slotNumber}号槽：{fault.GetTimeString()} {fault.GetFaultTypeDescription()}";
                sb.AppendLine(faultText);
                alStr.Add(faultText);
            }
        }
        
        controllerFaultText.text = sb.ToString();
        
        Debug.Log("UpdateControllerFaultUI 故障信息输出测试" + alStr.ToString());
        
        // 动态设置content高度
        SetContentHeight(controllerFaultScrollView, controllerFaultText);
    }
    // 新增：动态设置ScrollRect的Content高度
    private void SetContentHeight(ScrollRect scrollRect, TextMeshProUGUI textComponent)
    {
        if (scrollRect == null || textComponent == null)
            return;
            
        // 强制更新文本布局
        textComponent.ForceMeshUpdate();
        
        // 获取文本的实际高度
        float textHeight = textComponent.preferredHeight;
        
        // 获取ScrollRect的高度
        float scrollRectHeight = scrollRect.GetComponent<RectTransform>().rect.height;
        
        // 设置Content的高度，确保至少等于ScrollRect的高度
        float contentHeight = Mathf.Max(textHeight, scrollRectHeight);
        
        // 获取Content的RectTransform
        RectTransform contentRect = scrollRect.content;
        if (contentRect != null)
        {
            // 设置Content的高度
            Vector2 sizeDelta = contentRect.sizeDelta;
            sizeDelta.y = contentHeight;
            contentRect.sizeDelta = sizeDelta;
            
            // 强制刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }
    // 手动触发数据更新（用于测试）
    [ContextMenu("手动获取数据")]
    public void ManualUpdateData()
    {
        StartCoroutine(GetRealTimeDataAsync());
    }
    
    // 切换详细调试模式
    [ContextMenu("切换详细调试")]
    public void ToggleDetailedDebug()
    {
        enableDetailedDebug = !enableDetailedDebug;
        Debug.Log($"详细调试模式: {enableDetailedDebug}");
    }
    
    // 设置更新间隔
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
        Debug.Log($"更新间隔设置为: {interval}秒");
    }
}