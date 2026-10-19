using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.UI;
//using UnityEngine.UI.Extensions;
using XCharts.UI.Demo; // 新增：引用TemperatureAlarmTable所在的命名空间

public class TemperatureAlarmHistoryManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI displayText; // 显示文本组件
    [SerializeField] private Button getDataButton; // 获取数据按钮
    [SerializeField] private TemperatureAlarmTable alarmTable; // 新增：预警表格组件
    [SerializeField] private TextMeshProUGUI dateDisplayText; // 新增：日期显示文本
    [SerializeField] private TextMeshProUGUI dateInputText;
    [SerializeField] private TextMeshProUGUI loadingText; // 加载提示文本
    
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string electrolyticCompanyId = "1033";
    
    [Header("调试设置")]
    [SerializeField] private bool enableDetailedDebug = true; // 启用详细调试
    [SerializeField] private bool showRawBytes = true; // 显示原始字节数据
    [SerializeField] private bool showProtocolParsing = true; // 显示协议解析调试信息
    [ContextMenu("手动获取数据")]
    public void ManualGetTemperatureAlarmHistory()
    {
        Debug.Log("=== 手动触发数据获取 ===");
        GetTemperatureAlarmHistory();
    }

    [ContextMenu("测试企业标识")]
    public void TestCompanyId()
    {
        Debug.Log($"当前企业标识: '{GlobalUserData.AlSerialPantId}'");
        Debug.Log($"企业标识是否为空: {string.IsNullOrEmpty(GlobalUserData.AlSerialPantId)}");
    }
     private string GetQueryDateFromText()
{
    // 直接返回文本框中的内容，去除首尾空格
    return dateInputText.text.Trim();
}
    // 当前预警历史数据存储
    private TemperatureAlarmHistoryData currentAlarmHistoryData;
    
    // 温度语音报警历史信息数据结构
    [System.Serializable]
    public class TemperatureAlarmHistoryData
    {
        public List<TemperatureAlarmHistoryInfo> alarmHistories;
        
        public TemperatureAlarmHistoryData()
        {
            alarmHistories = new List<TemperatureAlarmHistoryInfo>();
        }
    }
    // 在 GetTemperatureAlarmHistory() 方法中构建参数
    private string BuildRequestParameter()
    {
        string inputDate = GetQueryDateFromText();
        string standardizedDate = ConvertToStandardDateFormat(inputDate);
        
        // 如果日期转换失败，直接返回空或抛出异常
        if (string.IsNullOrEmpty(standardizedDate))
        {
            Debug.LogError($"日期格式转换失败: {inputDate}");
            return null; // 或者可以抛出异常
        }
        
        // 优先使用GlobalUserData中的企业标识，如果为空则使用默认值
        string companyId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? GlobalUserData.AlSerialPantId : electrolyticCompanyId;
        
        // 添加详细的调试信息
        Debug.Log($"=== GlobalUserData状态检查 ===");
        Debug.Log($"GlobalUserData.AlSerialPantId: '{GlobalUserData.AlSerialPantId}'");
        Debug.Log($"GlobalUserData.AlSerialPantId是否为空: {string.IsNullOrEmpty(GlobalUserData.AlSerialPantId)}");
        Debug.Log($"electrolyticCompanyId: '{electrolyticCompanyId}'");
        Debug.Log($"最终使用的companyId: '{companyId}'");
        
        Debug.Log($"使用企业标识: {companyId} (来源: {(!string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? "GlobalUserData" : "默认值")})");
        Debug.Log($"原始日期: {inputDate}, 标准化日期: {standardizedDate}");
        
        // 格式：电解企业标识 + "-69-0-" + 标准化日期
        return $"{companyId}-69-0-{standardizedDate}";
    }
    
    // 修改：将各种日期格式转换为标准格式 (YYYYMMDD)，不提供默认值
    private string ConvertToStandardDateFormat(string inputDate)
    {
        if (string.IsNullOrEmpty(inputDate?.Trim()))
        {
            Debug.LogError("输入日期为空");
            return null; // 不提供默认日期
        }
        
        string trimmedInput = inputDate.Trim();
        
        // 支持的输入格式
        string[] supportedFormats = { "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy" };
        
        foreach (string format in supportedFormats)
        {
            if (DateTime.TryParseExact(trimmedInput, format, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyyMMdd"); // 统一转换为 YYYYMMDD 格式
            }
        }
        
        // 尝试通用日期解析
        if (DateTime.TryParse(trimmedInput, out DateTime generalParsedDate))
        {
            return generalParsedDate.ToString("yyyyMMdd");
        }
        
        // 如果都解析失败，返回null而不是默认日期
        Debug.LogError($"无法解析日期格式: {inputDate}");
        return null;
    }
    // 表格显示用的预警信息结构
    [System.Serializable]
    public class AlarmTableInfo
    {
        public string slotNumber;      // 槽号（如："1001"）
        public string occurTime;       // 发生时间（如："15:33:54"）
        public string messageContent;  // 信息内容（如："烟道端的9号节点最高温度判断成立"）
        
        public AlarmTableInfo(string slot, string time, string content)
        {
            slotNumber = slot;
            occurTime = time;
            messageContent = content;
        }
    }
    
    // 温度语音报警历史信息结构（只保留一个定义）
    [System.Serializable]
    public class TemperatureAlarmHistoryInfo
    {
        public int slotNumber;          // 槽号（2字节）
        public int date;                // 日期（4字节）
        public int time;                // 时间（4字节）
        public int positionIndex;       // 位置编号（2字节）
        public int directionType;       // 方向类型（2字节）
        public int directionIndex;      // 方向编号（2字节）
        public int nodeIndex;           // 节点号（2字节）
        public int alarmReason;         // 报警故障原因（2字节）
        
        public string GetPositionDescription()
        {
            switch (positionIndex)
            {
                case 1: return "钢窗";
                case 2: return "出铝端";
                case 3: return "烟道端";
                case 4: return "阴极钢棒";
                case 5: return "炉底";
                default: return $"位置{positionIndex}";
            }
        }
        
        public string GetDirectionTypeDescription()
        {
            switch (directionType)
            {
                case 1: return "A面";
                case 2: return "B面";
                case 0: return "";
                default: return $"方向{directionType}";
            }
        }
        
        public string GetDirectionIndexDescription()
        {
            if (positionIndex == 4) // 阴极钢棒
            {
                switch (directionIndex)
                {
                    case 1: return "-1";
                    case 2: return "-2";
                    case 0: return "";
                    default: return $"-{directionIndex}";
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
            
            string dirTypeDesc = GetDirectionTypeDescription();
            if (!string.IsNullOrEmpty(dirTypeDesc))
            {
                sb.Append(dirTypeDesc);
            }
            
            string dirIndexDesc = GetDirectionIndexDescription();
            if (!string.IsNullOrEmpty(dirIndexDesc))
            {
                sb.Append(dirIndexDesc);
            }
            
            if (nodeIndex > 0)
            {
                sb.Append($"-{nodeIndex}");
            }
            
            return sb.ToString();
        }
        
       
    public string GetTimeOnly()
{
    try
    {
        // 如果time是4字节打包的时间数据，需要解包
        // 假设格式为：高字节到低字节 = 小时、分钟、秒、毫秒（或其他格式）
        
        // 方法1：按字节位解包（需要根据实际协议确定）
        int hour = (time >> 24) & 0xFF;   // 最高字节
        int minute = (time >> 16) & 0xFF; // 次高字节
        int second = (time >> 8) & 0xFF;  // 次低字节
        // int millisecond = time & 0xFF;  // 最低字节（如果需要）
        
        // 验证时间范围
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            Debug.LogWarning($"时间数据异常: 原始值={time}, 解析结果={hour:D2}:{minute:D2}:{second:D2}");
            return "--:--:--";
        }
        
        return $"{hour:D2}:{minute:D2}:{second:D2}";
    }
    catch (Exception ex)
    {
        Debug.LogError($"时间解析失败: {ex.Message}, 原始时间值: {time}");
        return "--:--:--";
    }
}

public string GetDateTimeString()
{
    try
    {
        // 日期解析（假设date是4字节打包数据）
        int year = (date >> 16) & 0xFFFF;  // 高16位为年份
        int month = (date >> 8) & 0xFF;    // 次高8位为月份
        int day = date & 0xFF;             // 低8位为日期
        
        // 时间解析
        int hour = (time >> 24) & 0xFF;
        int minute = (time >> 16) & 0xFF;
        int second = (time >> 8) & 0xFF;
        
        // 验证数据范围
        if (year < 2000 || year > 2100 || month < 1 || month > 12 || 
            day < 1 || day > 31 || hour < 0 || hour > 23 || 
            minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            Debug.LogWarning($"日期时间数据异常: date={date}, time={time}");
            return "----/--/-- --:--:--";
        }
        
        return $"{year:D4}/{month:D2}/{day:D2} {hour:D2}:{minute:D2}:{second:D2}";
    }
    catch (Exception ex)
    {
        Debug.LogError($"日期时间解析失败: {ex.Message}");
        return "----/--/-- --:--:--";
    }
}
        
        public string GetDetailedMessageContent()
        {
            StringBuilder sb = new StringBuilder();
            
            string position = GetPositionDescription();
            string direction = GetDirectionTypeDescription();
            string directionIndex = GetDirectionIndexDescription();
            
            if (!string.IsNullOrEmpty(direction))
            {
                sb.Append(direction);
            }
            
            sb.Append(position);
            
            if (!string.IsNullOrEmpty(directionIndex))
            {
                sb.Append(directionIndex);
            }
            
            if (nodeIndex > 0)
            {
                sb.Append($"的{nodeIndex}号节点");
            }
            
            sb.Append(GetAlarmReasonDescription());
            
            return sb.ToString();
        }
        
        public AlarmTableInfo ToTableInfo()
        {
            return new AlarmTableInfo(
                slotNumber.ToString(),
                GetTimeOnly(),
                GetDetailedMessageContent()
            );
        }
    }
    
    // 获取表格格式的预警信息列表
    public List<AlarmTableInfo> GetAlarmTableInfoList()
    {
        List<AlarmTableInfo> tableInfoList = new List<AlarmTableInfo>();
        
        if (currentAlarmHistoryData != null && currentAlarmHistoryData.alarmHistories != null)
        {
            foreach (var alarm in currentAlarmHistoryData.alarmHistories)
            {
                tableInfoList.Add(alarm.ToTableInfo());
            }
        }
        
        return tableInfoList;
    }
    
    // 获取表格数据的公共方法（供外部调用）
    public List<AlarmTableInfo> GetCurrentAlarmTableData()
    {
        return GetAlarmTableInfoList();
    }
    
    // 获取当前报警历史数据的公共方法（供表格组件调用）
    public TemperatureAlarmHistoryData GetCurrentAlarmHistoryData()
    {
        return currentAlarmHistoryData;
    }
    
    // 打印表格格式数据（用于调试）
    [ContextMenu("打印表格格式数据")]
    public void PrintTableFormatData()
    {
        List<AlarmTableInfo> tableData = GetCurrentAlarmTableData();
        
        Debug.Log("=== 表格格式的预警数据 ===");
        Debug.Log("槽号\t时间\t\t信息内容");
        
        foreach (var info in tableData)
        {
            Debug.Log($"{info.slotNumber}\t{info.occurTime}\t{info.messageContent}");
        }
    }
    
    private void Start()
    {
        
        
        // 更新日期显示文本
        UpdateDateDisplay();
        
        // 绑定按钮点击事件
        if (getDataButton != null)
        {
            getDataButton.onClick.AddListener(OnGetDataButtonClick);
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("TemperatureAlarmHistoryManager enabled");
        // 重置状态
        currentAlarmHistoryData = null;
        if (alarmTable != null)
        {
            alarmTable.ClearTableData();
        }
        if (dateInputText != null && !IsValidDate(dateInputText.text))
        {
            dateInputText.text = DateTime.Today.ToString("yyyyMMdd");
        }
        UpdateDateDisplay();
        StartCoroutine(DelayedInitialQuery());
    }

    private void OnDisable()
    {
        Debug.Log("TemperatureAlarmHistoryManager disabled");
        // 清理状态，停止正在进行的协程
        StopAllCoroutines();
        currentAlarmHistoryData = null;
    }
    
    // 新增：更新日期显示
    private void UpdateDateDisplay()
    {
        if (dateDisplayText != null)
        {
            // 格式化日期显示（YYYY-MM-DD格式更易读）
            string formattedDate = FormatDateForDisplay(dateInputText.text);
            dateDisplayText.text = $"查询日期: {formattedDate}";
        }
    }
    
    // 新增：格式化日期显示
    private string FormatDateForDisplay(string dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length != 8)
        {
            return dateString;
        }
        
        try
        {
            string year = dateString.Substring(0, 4);
            string month = dateString.Substring(4, 2);
            string day = dateString.Substring(6, 2);
            return $"{year}-{month}-{day}";
        }
        catch
        {
            return dateString;
        }
    }
    public void SetDateAndQuery(string newDate)
{
    Debug.Log($"=== SetDateAndQuery 开始 ===");
    Debug.Log($"接收到的日期: {newDate}");
    
    if (ValidateDateFormat(newDate))
    {
        Debug.Log("日期格式验证通过");
        
        if (dateInputText != null)
        {
            string formattedDate = FormatDateForDisplay(newDate);
            dateInputText.text = formattedDate;
            Debug.Log($"文本框已更新为: {formattedDate}");
        }
        else
        {
            Debug.LogWarning("dateInputText 为 null");
        }
        
        UpdateDateDisplay();
        Debug.Log($"日期选择器设置日期: {newDate}");
        
        // 立即执行查询
        if (ValidateDateFormat(newDate))
        {
            Debug.Log("=== 日期选择器自动执行查询 ===");
            GetTemperatureAlarmHistory();
        }
        else
        {
            Debug.LogWarning($"查询前日期验证失败: {newDate}");
        }
    }
    else
    {
        Debug.LogWarning($"日期格式无效: {newDate}，请使用YYYYMMDD格式（如：20250622）");
    }
    
    Debug.Log($"=== SetDateAndQuery 结束 ===");
}
    // 新增：设置查询日期（供外部调用）
    public void SetQueryDate(string newDate)
    {
        if (ValidateDateFormat(newDate))
        {
            if (dateInputText != null)
            {
                dateInputText.text = FormatDateForDisplay(newDate);
            }
            UpdateDateDisplay();
            Debug.Log($"查询日期已更新为: {newDate}");
        }
        else
        {
            Debug.LogWarning($"日期格式无效: {newDate}，请使用YYYYMMDD格式（如：20250622）");
        }
    }
    
    
    // 新增：日期格式验证
    private bool ValidateDateFormat(string dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length != 8)
        {
            return false;
        }
        
        // 检查是否全为数字
        if (!System.Text.RegularExpressions.Regex.IsMatch(dateString, @"^\d{8}$"))
        {
            return false;
        }
        
        // 尝试解析日期
        try
        {
            int year = int.Parse(dateString.Substring(0, 4));
            int month = int.Parse(dateString.Substring(4, 2));
            int day = int.Parse(dateString.Substring(6, 2));
            
            DateTime testDate = new DateTime(year, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    // 修改：按钮点击事件处理
    public void OnGetDataButtonClick()
    {
        // 检查文本框中的日期是否有效
        if (!IsValidDate(dateInputText?.text))
        {
            Debug.LogWarning("文本框中的日期无效，无法执行查询操作");
            return; // 直接返回，不执行后续操作
        }
        
        // 新增：在查询前先清空表格
        if (alarmTable != null)
        {
            Debug.Log("清空表格数据");
            alarmTable.ClearTableData();
        }
        else
        {
            Debug.LogWarning("alarmTable 为空，无法清空表格");
        }
        
        string queryDate = GetQueryDateFromText();
        Debug.Log("=== 按钮点击获取数据 ===");
        Debug.Log($"从文本框读取的查询日期: {queryDate}");
        
        // 显示加载提示
        ShowLoadingText(true);
        
        // 只有日期有效时才执行查询
        GetTemperatureAlarmHistory();
    }
    
    // 获取温度语音报警历史信息
   public void GetTemperatureAlarmHistory(string date = null)
    {
        if (!string.IsNullOrEmpty(date) && dateInputText != null)
        {
            dateInputText.text = FormatDateForDisplay(date);
        }
        
        // 显示加载提示
        ShowLoadingText(true);
        
        StartCoroutine(GetTemperatureAlarmHistoryAsync());
    }
    
    public IEnumerator GetTemperatureAlarmHistoryAsync()
    {
        // 在查询开始时清空当前数据
        currentAlarmHistoryData = null;
        
        // 优先使用GlobalUserData中的企业标识
        string companyId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? GlobalUserData.AlSerialPantId : electrolyticCompanyId;
        
        if (string.IsNullOrEmpty(companyId))
        {
            Debug.LogWarning("企业标识为空，无法获取温度报警历史数据");
            ShowLoadingText(false);
            yield break;
        }
        
        // 使用新的参数构建方法
        string requestParam = BuildRequestParameter();
        
        // 检查参数是否构建成功
        if (string.IsNullOrEmpty(requestParam))
        {
            Debug.LogError("请求参数构建失败，可能是日期格式无效");
            ShowLoadingText(false);
            UpdateUIWithError("日期格式无效，请检查输入的日期格式");
            yield break;
        }
        
        Debug.Log($"=== 开始获取温度语音报警历史信息 ===");
        Debug.Log($"使用企业标识: {companyId} (来源: {(!string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? "GlobalUserData" : "硬编码")})");
        Debug.Log($"查询日期: {dateInputText.text}");
        Debug.Log($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
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
            TemperatureAlarmHistoryData parsedData = ParseTemperatureAlarmHistoryData(responseText);
            
            if (parsedData != null && parsedData.alarmHistories != null && parsedData.alarmHistories.Count > 0)
            {
                // 更新UI显示
                UpdateUI(parsedData);
                Debug.Log($"✅ 成功获取并解析了 {parsedData.alarmHistories.Count} 条温度报警历史记录");
            }
            else
            {
                Debug.LogWarning("❌ 解析数据失败或数据为空");
                // 确保currentAlarmHistoryData为空
                currentAlarmHistoryData = new TemperatureAlarmHistoryData();
                // 显示无数据的UI
                UpdateUIWithNoData();
            }
            
            // 隐藏加载提示
            ShowLoadingText(false);
        }
        else
        {
            Debug.LogError($"❌ 获取温度报警历史数据失败: {webRequest.error}");
            Debug.LogError($"HTTP响应码: {webRequest.responseCode}");
            
            if (webRequest.downloadHandler != null)
            {
                string errorResponse = webRequest.downloadHandler.text;
                Debug.LogError($"错误响应内容: '{errorResponse}'");
                Debug.LogError($"错误响应长度: {errorResponse.Length} 字符");
            }
            
            // 错误情况下也要清空数据
            currentAlarmHistoryData = new TemperatureAlarmHistoryData();
            
            // 显示错误的UI
            UpdateUIWithError(webRequest.error);
            
            // 隐藏加载提示
            ShowLoadingText(false);
        }
        
        // 清理资源
        webRequest.Dispose();
        Debug.Log($"=== 温度报警历史数据获取完成 ===");
    }
    
    private TemperatureAlarmHistoryData ParseTemperatureAlarmHistoryData(string responseText)
    {
        try
        {
            Debug.Log($"🔍 开始解析温度报警历史响应数据...");
            TemperatureAlarmHistoryData result = new TemperatureAlarmHistoryData();
            
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
                Debug.Log($"🔧 添加了 {paddingNeeded} 个Base64填充字符");
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
            
            if (showRawBytes)
            {
                LogRawBytes(decodedBytes);
            }
            
            // 按照协议格式解析数据帧
            int offset = 0;
            int totalAlarmRecords = 0;
            
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
                
                // 检查数据长度是否为20的整数倍
                if (dataLength % 20 != 0)
                {
                    Debug.LogWarning($"⚠️ 数据长度 {dataLength} 不是20的整数倍，可能存在数据问题");
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
                
                // 解析帧数据内容（按20字节分组）
                int frameAlarmRecords = ParseTemperatureAlarmFrameData(frameData, frameType, slotNumber, result);
                totalAlarmRecords += frameAlarmRecords;
            }
            
            Debug.Log($"✅ 总计解析出 {result.alarmHistories.Count} 条温度报警历史记录");
            Debug.Log($"✅ 总计 {totalAlarmRecords} 个报警记录");
            
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析温度报警历史数据时出错: {ex.Message}");
            Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            return null;
        }
    }
    
    private int ParseTemperatureAlarmFrameData(byte[] frameData, int frameType, int slotNumber, TemperatureAlarmHistoryData result)
    {
        try
        {
            int alarmRecordCount = 0;
            
            // 按20字节分组解析
            for (int i = 0; i < frameData.Length; i += 20)
            {
                if (i + 20 > frameData.Length)
                {
                    Debug.LogWarning($"⚠️ 剩余数据不足20字节，跳过 {frameData.Length - i} 字节");
                    break;
                }
                
                // 提取20字节数据
                byte[] recordData = new byte[20];
                Array.Copy(frameData, i, recordData, 0, 20);
                
                // 解析温度报警历史记录
                TemperatureAlarmHistoryInfo alarmInfo = ParseTemperatureAlarmRecord(recordData);
                if (alarmInfo != null)
                {
                    result.alarmHistories.Add(alarmInfo);
                    alarmRecordCount++;
                    
                    if (showProtocolParsing)
                    {
                        Debug.Log($"✅ 解析到温度报警记录: 槽号{alarmInfo.slotNumber}, {alarmInfo.GetLocationDescription()}, {alarmInfo.GetAlarmReasonDescription()}, {alarmInfo.GetDateTimeString()}");
                    }
                }
            }
            
            return alarmRecordCount;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析温度报警帧数据失败: {ex.Message}");
            return 0;
        }
    }
    
    private TemperatureAlarmHistoryInfo ParseTemperatureAlarmRecord(byte[] recordData)
    {
        if (recordData == null || recordData.Length != 20)
        {
            Debug.LogWarning($"⚠️ 温度报警记录数据长度不正确: {recordData?.Length ?? 0}");
            return null;
        }
        
        try
        {
            TemperatureAlarmHistoryInfo alarmInfo = new TemperatureAlarmHistoryInfo();
            
            int offset = 0;
            
            // 槽号（2字节）
            alarmInfo.slotNumber = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            // 日期（4字节）
            alarmInfo.date = (recordData[offset] << 24) | (recordData[offset + 1] << 16) | 
                           (recordData[offset + 2] << 8) | recordData[offset + 3];
            offset += 4;
            
            // 时间（4字节）
            alarmInfo.time = (recordData[offset] << 24) | (recordData[offset + 1] << 16) | 
                           (recordData[offset + 2] << 8) | recordData[offset + 3];
            offset += 4;
            
            // 位置编号（2字节）
            alarmInfo.positionIndex = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            // 方向类型（2字节）
            alarmInfo.directionType = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            // 方向编号（2字节）
            alarmInfo.directionIndex = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            // 节点号（2字节）
            alarmInfo.nodeIndex = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            // 报警故障原因（2字节）
            alarmInfo.alarmReason = (recordData[offset] << 8) | recordData[offset + 1];
            offset += 2;
            
            return alarmInfo;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析温度报警记录失败: {ex.Message}");
            return null;
        }
    }
    
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
    
    // 在 UpdateUI 方法中添加调试信息
    private void UpdateUI(TemperatureAlarmHistoryData data)
    {
        Debug.Log($"UpdateUI 被调用，数据条数: {data?.alarmHistories?.Count ?? 0}");
        
        // 存储当前数据
        currentAlarmHistoryData = data;
        Debug.Log($"currentAlarmHistoryData 已更新: {currentAlarmHistoryData != null}");
        
        if (displayText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"🔄 温度语音报警历史信息 (查询时间: {DateTime.Now:HH:mm:ss})");
            sb.AppendLine($"🏢 企业: {GlobalUserData.AlSerialPantIdName}");
            sb.AppendLine($"📅 查询日期: {dateInputText.text}");
            sb.AppendLine($"📊 报警记录数量: {data.alarmHistories.Count}");
            sb.AppendLine();
            
            if (data.alarmHistories.Count > 0)
            {
                // 按时间排序（最新的在前）
                data.alarmHistories.Sort((a, b) => 
                {
                    int dateCompare = b.date.CompareTo(a.date);
                    if (dateCompare != 0) return dateCompare;
                    return b.time.CompareTo(a.time);
                });
                
                // 显示报警信息
                sb.AppendLine("槽号\t时间\t\t信息内容");
                sb.AppendLine("────────────────────────────────────────");
                
                foreach (var alarm in data.alarmHistories)
                {
                    AlarmTableInfo tableInfo = alarm.ToTableInfo();
                    sb.AppendLine($"{tableInfo.slotNumber}\t{tableInfo.occurTime}\t{tableInfo.messageContent}");
                }
            }
            else
            {
                sb.AppendLine("ℹ️ 该日期没有温度语音报警历史记录");
            }
            
            displayText.text = sb.ToString();
        }
        
        // 新增：更新表格数据
        if (alarmTable != null)
        {
            Debug.Log("开始调用 alarmTable.LoadAlarmDataFromManager()");
            alarmTable.LoadAlarmDataFromManager();
            Debug.Log("alarmTable.LoadAlarmDataFromManager() 调用完成");
        }
        else
        {
            Debug.LogWarning("alarmTable 为空，无法更新表格");
        }
        
        // 确保隐藏加载提示
        ShowLoadingText(false);
    }
    
    private void UpdateUIWithNoData()
    {
        // 确保currentAlarmHistoryData为空的数据对象
        currentAlarmHistoryData = new TemperatureAlarmHistoryData();
        
        if (displayText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"🔄 温度语音报警历史信息 (查询时间: {DateTime.Now:HH:mm:ss})");
            sb.AppendLine($"🏢 企业: {GlobalUserData.AlSerialPantIdName}");
            sb.AppendLine($"📅 查询日期: {dateInputText.text}");
            sb.AppendLine($"📊 报警记录数量: 0");
            sb.AppendLine();
            sb.AppendLine($"ℹ️ 该日期没有温度语音报警历史记录");
            
            displayText.text = sb.ToString();
        }
        
        // 修改：只清空表格，不调用LoadAlarmDataFromManager
        if (alarmTable != null)
        {
            alarmTable.ClearTableData();
        }
        
        ShowLoadingText(false);
    }

    private IEnumerator DelayedInitialQuery()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);
        if (!IsValidDate(dateInputText?.text))
        {
            dateInputText.text = DateTime.Today.ToString("yyyyMMdd");
            UpdateDateDisplay();
        }
        GetTemperatureAlarmHistory();
    }
    // 验证日期是否有效
private bool IsValidDate(string dateText)
{
    if (string.IsNullOrEmpty(dateText?.Trim()))
    {
        return false;
    }
    
    string inputText = dateText.Trim();
    
    // 支持多种日期格式验证
    string[] supportedFormats = { "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy" };
    
    foreach (string format in supportedFormats)
    {
        if (DateTime.TryParseExact(inputText, format, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            return true;
        }
    }
    
    // 尝试通用日期解析
    if (DateTime.TryParse(inputText, out DateTime generalParsedDate))
    {
        return true;
    }
    
    return false;
}
    private void UpdateUIWithError(string error)
    {
        // 确保currentAlarmHistoryData为空的数据对象
        currentAlarmHistoryData = new TemperatureAlarmHistoryData();
        
        if (displayText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"❌ 温度语音报警历史信息获取失败 (时间: {DateTime.Now:HH:mm:ss})");
            sb.AppendLine($"🏢 企业: {GlobalUserData.AlSerialPantIdName}");
            sb.AppendLine($"📅 查询日期: {dateInputText.text}");
            sb.AppendLine();
            sb.AppendLine($"错误信息: {error}");
            
            displayText.text = sb.ToString();
        }
        
        // 清空表格数据
        if (alarmTable != null)
        {
            alarmTable.ClearTableData();
        }
        
        ShowLoadingText(false);
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

/// <summary>
/// 显示或隐藏加载提示文本
/// </summary>
private void ShowLoadingText(bool show)
{
    if (loadingText != null)
    {
        loadingText.gameObject.SetActive(show);
        if (show)
        {
            loadingText.text = "正在读取预警数据...";
        }
    }
}
}


