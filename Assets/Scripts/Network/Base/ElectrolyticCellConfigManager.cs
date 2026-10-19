using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement; // 添加场景管理命名空间

public class ElectrolyticCellConfigManager : MonoBehaviour
{
    [Header("UI组件")]
    public Button getConfigButton; // 保留按钮，但不是必需的
    public TextMeshProUGUI displayText;
    public TMP_InputField companyIdInput; // 可选：让用户输入企业标识
    
    [Header("API设置")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    public string defaultCompanyId = "1033"; // 默认企业标识
    
    [Header("自动加载设置")]
    public bool autoLoadOnStart = true; // 是否在Start时自动加载
    public float autoLoadDelay = 1.0f; // 自动加载延迟时间（秒）
    
    [Header("调试选项")]
    public bool enableDebugLog = true;
    
    [Header("场景切换限制设置")]
    public bool preventSceneSwitchingWhenLoading = true; // 是否在加载时阻止场景切换
    public string loadingMessage = "电解槽配置数据正在加载中，请稍候..."; // 加载时的提示信息
    
    // 数据加载状态标志
    private bool isDataLoading = false;
    private bool isDataLoadCompleted = false;
    
    // 静态实例，用于全局访问
    private static ElectrolyticCellConfigManager _instance;
    public static ElectrolyticCellConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ElectrolyticCellConfigManager>();
            }
            return _instance;
        }
    }
    
    // 电解槽配置数据结构（每20字节一组）
    [System.Serializable]
    public class ElectrolyticCellConfig
    {
        public byte[] rawData; // 原始13字节数据
        public string configInfo; // 解析后的配置信息
        
        // 帧信息
        public ushort frameCompanyId;    // 帧中的企业标识
        public ushort frameType;         // 帧类型
        public ushort frameSlotNumber;   // 帧中的槽号
        
        // 解析后的具体字段（根据13字节数据调整）
        public ushort slotNumber;           // 槽号 (2字节)
        public ushort internalId;           // 内部唯一号ID (2字节)
        public byte connectionStatus;       // 联机状态 (1字节)
        public byte workshopNumber;         // 车间号 (1字节)
        public byte workAreaNumber;         // 工区号 (1字节)
        public byte slotAge;                // 槽龄朝代 (1字节)
        public byte anodeCurrentFlag;       // 阳极电流标志 (1字节)
        public byte cathodeCurrentFlag;     // 阴极电流标志 (1字节)
        public byte temperatureFlag;        // 温度标志 (1字节)
        public byte controlFlag;            // 控制标志 (1字节)
        public byte extraByte;              // 第13字节
        
        public ElectrolyticCellConfig(byte[] data)
        {
            int dataLength = Math.Min(data.Length, 13);
            rawData = new byte[dataLength];
            Array.Copy(data, rawData, dataLength);
            ParseFields(rawData);
            configInfo = GenerateConfigInfo();
        }
        
        private void ParseFields(byte[] data)
        {
            if (data.Length < 13)
            {
                Debug.LogWarning($"数据长度不足13字节，实际长度: {data.Length}");
                return;
            }
            
            int offset = 0;
            
            // 槽号 (2字节) - 大端序
            slotNumber = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            
            // 内部唯一号ID (2字节) - 大端序  
            internalId = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            
            // 联机状态 (1字节)
            connectionStatus = data[offset];
            offset += 1;
            
            // 车间号 (1字节)
            workshopNumber = data[offset];
            offset += 1;
            
            // 工区号 (1字节)
            workAreaNumber = data[offset];
            offset += 1;
            
            // 槽龄朝代 (1字节)
            slotAge = data[offset];
            offset += 1;
            
            // 阳极电流标志 (1字节)
            anodeCurrentFlag = data[offset];
            offset += 1;
            
            // 阴极电流标志 (1字节)
            cathodeCurrentFlag = data[offset];
            offset += 1;
            
            // 温度标志 (1字节)
            temperatureFlag = data[offset];
            offset += 1;
            
            // 控制标志 (1字节)
            controlFlag = data[offset];
            offset += 1;
            
            // 第13字节
            extraByte = data[offset];
        }
        
        private string GenerateConfigInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== 电解槽配置信息 ===");
            sb.AppendLine($"槽号: {slotNumber}");
            sb.AppendLine($"内部唯一号(ID): {internalId}");
            sb.AppendLine($"联机状态: {GetConnectionStatusText(connectionStatus)}");
            sb.AppendLine($"车间号: {workshopNumber}");
            sb.AppendLine($"工区号: {workAreaNumber}");
            sb.AppendLine($"槽龄朝代: {slotAge}");
            sb.AppendLine($"阳极电流标志: {anodeCurrentFlag}");
            sb.AppendLine($"阴极电流标志: {cathodeCurrentFlag}");
            sb.AppendLine($"温度标志: {temperatureFlag}");
            sb.AppendLine($"控制标志: {controlFlag}");
            sb.AppendLine($"额外字节: {extraByte}");
            sb.AppendLine();
            sb.AppendLine("原始字节数据: " + string.Join(" ", rawData.Select(b => b.ToString("X2"))));
            
            return sb.ToString();
        }
        
        private string GetConnectionStatusText(byte status)
        {
            switch (status)
            {
                case 0: return "联机";
                case 1: return "脱机";
                case 2: return "停槽";
                case 3: return "焙烧";
                default: return $"未知状态({status})";
            }
        }
    }
    
    private List<ElectrolyticCellConfig> configList = new List<ElectrolyticCellConfig>();
    
    void Start()
    {
        // 设置单例实例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 防止场景切换时销毁
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 注册场景切换事件
        if (preventSceneSwitchingWhenLoading)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        // 设置按钮事件（如果按钮存在）
        if (getConfigButton != null)
        {
            getConfigButton.onClick.AddListener(OnGetConfigButtonClick);
        }
        
        // 初始化显示文本
        if (displayText != null)
        {
            displayText.text = "正在初始化电解槽配置管理器...";
        }
        
        // 设置默认企业ID
        if (companyIdInput != null)
        {
            companyIdInput.text = defaultCompanyId;
        }
        
        // 自动触发获取配置数据
        if (autoLoadOnStart)
        {
            StartCoroutine(AutoLoadConfigData());
        }
    }
    
    void OnDestroy()
    {
        // 取消注册场景切换事件
        if (preventSceneSwitchingWhenLoading)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    /// <summary>
    /// 场景加载完成时的回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isDataLoading && enableDebugLog)
        {
            Debug.Log($"场景 {scene.name} 已加载，但电解槽配置数据仍在加载中");
        }
    }
    
    /// <summary>
    /// 检查是否可以进行场景切换
    /// </summary>
    /// <returns>如果可以切换场景返回true，否则返回false</returns>
    public bool CanSwitchScene()
    {
        if (!preventSceneSwitchingWhenLoading)
        {
            return true; // 如果没有启用限制，总是允许切换
        }
        
        return !isDataLoading;
    }
    
    /// <summary>
    /// 尝试切换场景（带检查）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <returns>如果成功开始切换返回true，否则返回false</returns>
    public bool TrySwitchScene(string sceneName)
    {
        if (CanSwitchScene())
        {
            SceneManager.LoadScene(sceneName);
            return true;
        }
        else
        {
            ShowLoadingWarning();
            return false;
        }
    }
    
    /// <summary>
    /// 显示加载中的警告信息
    /// </summary>
    private void ShowLoadingWarning()
    {
        if (enableDebugLog)
        {
            Debug.LogWarning("场景切换被阻止：" + loadingMessage);
        }
        
        // 如果有显示文本组件，临时显示警告信息
        if (displayText != null)
        {
            string originalText = displayText.text;
            displayText.text = loadingMessage;
            
            // 2秒后恢复原始文本
            StartCoroutine(RestoreDisplayTextAfterDelay(originalText, 2.0f));
        }
    }
    
    /// <summary>
    /// 延迟恢复显示文本
    /// </summary>
    private IEnumerator RestoreDisplayTextAfterDelay(string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (displayText != null)
        {
            displayText.text = originalText;
        }
    }
    
    /// <summary>
    /// 获取数据加载状态
    /// </summary>
    public bool IsDataLoading => isDataLoading;
    
    /// <summary>
    /// 获取数据是否加载完成
    /// </summary>
    public bool IsDataLoadCompleted => isDataLoadCompleted;
    
    /// <summary>
    /// 自动加载配置数据的协程
    /// </summary>
    private IEnumerator AutoLoadConfigData()
    {
        // 设置加载状态
        isDataLoading = true;
        isDataLoadCompleted = false;
        
        // 等待指定的延迟时间
        yield return new WaitForSeconds(autoLoadDelay);
        //20251102,cxt
        //string companyId = companyIdInput != null ? companyIdInput.text : defaultCompanyId;
        string companyId = GlobalUserData.AlSerialPantId;

        if (string.IsNullOrEmpty(companyId))
        {
            companyId = defaultCompanyId;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"自动开始获取电解槽配置数据，企业ID: {companyId}");
        }
        
        if (displayText != null)
        {
            displayText.text = "自动获取电解槽配置信息中...";
        }
        
        // 开始获取配置数据
        yield return StartCoroutine(GetElectrolyticCellConfig(companyId));
        
        // 数据加载完成
        isDataLoading = false;
        isDataLoadCompleted = true;
        
        if (enableDebugLog)
        {
            Debug.Log("电解槽配置数据加载完成，场景切换限制已解除");
        }
    }
    
    private void OnGetConfigButtonClick()
    {
        string companyId = companyIdInput != null ? companyIdInput.text : defaultCompanyId;
        
        if (string.IsNullOrEmpty(companyId))
        {
            UpdateDisplayText("错误：企业标识不能为空");
            return;
        }
        
        // 设置加载状态
        isDataLoading = true;
        isDataLoadCompleted = false;
        
        StartCoroutine(GetElectrolyticCellConfigWithCallback(companyId));
    }
    
    /// <summary>
    /// 获取配置数据的协程（带回调）
    /// </summary>
    private IEnumerator GetElectrolyticCellConfigWithCallback(string companyId)
    {
        yield return StartCoroutine(GetElectrolyticCellConfig(companyId));
        
        // 数据加载完成
        isDataLoading = false;
        isDataLoadCompleted = true;
        
        if (enableDebugLog)
        {
            Debug.Log("手动获取的电解槽配置数据加载完成");
        }
    }
    
    private IEnumerator GetElectrolyticCellConfig(string companyId)
    {
        // 构建请求参数：企业标识 + "-65"
        string requestParam = $"{companyId}-65";
        
        if (enableDebugLog)
        {
            Debug.Log($"开始获取电解槽配置信息，参数: {requestParam}");
        }
        
        UpdateDisplayText("正在获取电解槽配置信息...");
        
        // 按照现有API模式创建请求
        string jsonData = $"\"{requestParam}\"";
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
        
        // 创建UnityWebRequest
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        if (enableDebugLog)
        {
            Debug.Log($"发送请求到: {apiUrl}");
            Debug.Log($"请求参数: {jsonData}");
        }
        
        // 记录请求开始时间
        DateTime requestStartTime = DateTime.Now;
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        // 记录请求结束时间
        DateTime requestEndTime = DateTime.Now;
        TimeSpan requestDuration = requestEndTime - requestStartTime;
        
        if (enableDebugLog)
        {
            Debug.Log($"请求完成，耗时: {requestDuration.TotalMilliseconds:F2} 毫秒");
            Debug.Log($"请求状态: {webRequest.result}");
        }
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            
            if (enableDebugLog)
            {
                Debug.Log($"API响应: {responseText}");
            }
            
            // 解析响应数据
            ParseElectrolyticCellConfigResponse(responseText);
        }
        else
        {
            string errorMsg = $"请求失败: {webRequest.error}";
            if (enableDebugLog)
            {
                Debug.LogError(errorMsg);
            }
            UpdateDisplayText(errorMsg);
        }
        
        // 清理资源
        webRequest.Dispose();
    }
    
    private void ParseElectrolyticCellConfigResponse(string responseText)
    {
        try
        {
            configList.Clear();
            
            // 去除可能的引号
            string cleanResponse = responseText;
            if (responseText.StartsWith("\"") && responseText.EndsWith("\""))
            {
                cleanResponse = responseText.Substring(1, responseText.Length - 2);
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"清理后的响应: {cleanResponse}");
            }
            
            // 处理Base64填充
            string tmpText = cleanResponse;
            int paddingCount = tmpText.Length % 4;
            if (paddingCount > 0)
            {
                for (int i = 0; i < 4 - paddingCount; i++)
                {
                    tmpText += "=";
                }
            }
            
            // 将Base64字符串转换为字节数组
            byte[] responseData = Convert.FromBase64String(tmpText);
            
            if (enableDebugLog)
            {
                Debug.Log($"解码后数据长度: {responseData.Length} 字节");
                Debug.Log($"原始字节数据(十六进制): {BitConverter.ToString(responseData)}");
                Debug.Log($"原始字节数据(十进制): [{string.Join(", ", responseData.Select(b => b.ToString()))}]");
                
               
            }
            
            // 按照Java方法解析数据
            ParseDataLikeJava(responseData);
        }
        catch (Exception ex)
        {
            string errorMsg = $"解析配置数据时出错: {ex.Message}";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
        }
    }
    
    private void ParseDataLikeJava(byte[] tmpBuffer)
    {
        List<int> list = new List<int>();
        List<int> result = new List<int>();
        List<List<int>> re = new List<List<int>>();
        
        if (tmpBuffer != null && tmpBuffer.Length > 0)
        {
            int jk = tmpBuffer.Length / 32;
            int offset = 0;
            
            // 按32字节分组处理
            for (int j = 0; j < jk; j++)
            {
                for (int jt = 0; jt < 32; jt++)
                {
                    offset = j * 32 + jt;
                    int unsignedByte = tmpBuffer[offset] & 0xFF;
                    list.Add(unsignedByte);
                }
                
                list.Add(-1); // 使用-1代替Java中的null作为分隔符
            }
            
            // 处理剩余字节
            offset = jk * 32;
            jk = tmpBuffer.Length % 32;
            if (jk != 0)
            {
                for (int j = 0; j < jk; j++)
                {
                    int unsignedByte = tmpBuffer[offset + j] & 0xFF;
                    list.Add(unsignedByte);
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"处理后list大小: {list.Count}");
            }
            
            // 过滤掉分隔符，创建result列表
            foreach (int value in list)
            {
                if (value != -1)
                {
                    result.Add(value);
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"过滤后result大小: {result.Count}");
            }
            
            // 修改解析逻辑：跳过前12个字节的标识信息，然后按20字节为一组解析电解槽信息
            for (int i = 12; i < result.Count; i += 20)
            {
                List<int> li = new List<int>();
                for (int j = i; j < i + 20 && j < result.Count; j++)
                {
                    li.Add(result[j]);
                }
                if (li.Count == 20) // 确保有完整的20字节
                {
                    re.Add(li);
                }
            }
            
            if (enableDebugLog)
            {
                //Debug.Log($"最终解析出 {re.Count} 组配置数据");
                for (int i = 0; i < re.Count; i++)
                {
                    //Debug.Log($"配置组 {i + 1}: [{string.Join(", ", re[i])}]");
                }
            }
            
            // 转换为ElectrolyticCellConfig对象
            ConvertToConfigObjects(re);
        }
    }
    
    private void ConvertToConfigObjects(List<List<int>> configGroups)
    {
        configList.Clear();
        
        foreach (var group in configGroups)
        {
            if (group.Count >= 20)
            {
                // 将int列表转换为byte数组（恢复为20字节）
                byte[] configBytes = new byte[20];
                for (int i = 0; i < 20; i++)
                {
                    configBytes[i] = (byte)group[i];
                }
                
                // 创建配置对象（恢复为20字节）
                ElectrolyticCellConfig config = new ElectrolyticCellConfig(configBytes);
                configList.Add(config);
            }
        }
        
        // 更新显示
        UpdateDisplayWithConfigs();
    }
    
    private void UpdateDisplayWithConfigs()
    {
        if (configList.Count == 0)
        {
            UpdateDisplayText("未获取到配置数据");
            return;
        }
        
        StringBuilder displayContent = new StringBuilder();
        displayContent.AppendLine($"=== 电解槽配置信息 ===");
        displayContent.AppendLine($"获取时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        displayContent.AppendLine($"配置数量: {configList.Count}");
        displayContent.AppendLine();
        
        for (int i = 0; i < configList.Count; i++)
        {
            displayContent.AppendLine($"--- 配置 {i + 1} ---");
            displayContent.AppendLine(configList[i].configInfo);
            displayContent.AppendLine();
        }
        
        UpdateDisplayText(displayContent.ToString());
        
        if (enableDebugLog)
        {
            Debug.Log($"成功解析 {configList.Count} 个电解槽配置");
        }
        
        // 存储数据到全局管理器
        StoreDataToGlobalManager();
    }
    
    /// <summary>
    /// 存储数据到全局管理器
    /// </summary>
    private void StoreDataToGlobalManager()
    {
        try
        {
            GlobalElectrolyticCellDataManager.Instance.StoreAllConfigs(configList);
            
            if (enableDebugLog)
            {
                Debug.Log($"已将 {configList.Count} 个电解槽配置存储到全局数据管理器");
                Debug.Log($"数据统计: {GlobalElectrolyticCellDataManager.Instance.GetDataStatistics()}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"存储数据到全局管理器时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取全局数据管理器实例
    /// </summary>
    public GlobalElectrolyticCellDataManager GetGlobalDataManager()
    {
        return GlobalElectrolyticCellDataManager.Instance;
    }
    private void UpdateDisplayText(string content)
    {
        if (displayText != null)
        {
            displayText.text = content;
        }
    }
    
    // 公共方法：获取解析后的配置列表
    public List<ElectrolyticCellConfig> GetConfigList()
    {
        return new List<ElectrolyticCellConfig>(configList);
    }
    
    // 公共方法：手动触发获取配置
    public void GetConfigManually(string companyId)
    {
        // 设置加载状态
        isDataLoading = true;
        isDataLoadCompleted = false;
        
        StartCoroutine(GetElectrolyticCellConfigWithCallback(companyId));
    }
}