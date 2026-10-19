using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AutoTemperatureSensorDataLoader : MonoBehaviour
{
    [Header("API设置")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    public string defaultCompanyId = "1033"; // 默认企业标识
    
    [Header("自动执行设置")]
    public bool autoLoadOnStart = true; // 启动时自动加载
    public float autoLoadDelay = 1f; // 延迟执行时间（秒）
    public bool enablePeriodicUpdate = false; // 是否启用定期更新
    public float updateInterval = 300f; // 更新间隔（秒，默认5分钟）
    
    [Header("调试选项")]
    public bool enableDebugLog = true;
    
    // 温度传感器配置数据结构（复用原有结构）
    [System.Serializable]
    public class TemperatureSensorConfig
    {
        public byte[] rawData; // 原始12字节数据
        public ushort sn; // 序号
        public ushort sampleindex; // 采样编号
        public ushort posindex; // 位置编号
        public ushort directiontype; // 方向类型
        public ushort directionindex; // 方向编号
        public ushort nodeindex; // 节点号
        
        public TemperatureSensorConfig(byte[] data)
        {
            if (data.Length != 12)
            {
                throw new ArgumentException("温度传感器配置数据必须为12字节");
            }
            
            rawData = new byte[12];
            Array.Copy(data, rawData, 12);
            
            ParseFields();
        }
        
        private void ParseFields()
        {
            // 解析各个字段（大端序）
            sn = (ushort)((rawData[0] << 8) | rawData[1]);
            sampleindex = (ushort)((rawData[2] << 8) | rawData[3]);
            posindex = (ushort)((rawData[4] << 8) | rawData[5]);
            directiontype = (ushort)((rawData[6] << 8) | rawData[7]);
            directionindex = (ushort)((rawData[8] << 8) | rawData[9]);
            nodeindex = (ushort)((rawData[10] << 8) | rawData[11]);
        }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== 温度传感器配置 {sn} ===");
            sb.AppendLine($"序号: {sn}");
            sb.AppendLine($"采样编号: {sampleindex}");
            sb.AppendLine($"位置编号: {posindex} ({GetPositionText(posindex)})");
            sb.AppendLine($"方向类型: {directiontype} ({GetdirectiontypeText(directiontype)})");
            sb.AppendLine($"方向编号: {directionindex} ({GetdirectionindexText(directionindex)})");
            sb.AppendLine($"节点号: {nodeindex}");
            sb.AppendLine($"原始数据: {BitConverter.ToString(rawData)}");
            sb.AppendLine();
            
            return sb.ToString();
        }
        
        private string GetPositionText(ushort position)
        {
            return position switch
            {
                1 => "钢窗",
                2 => "出铝端",
                3 => "烟道端",
                4 => "阴极钢棒",
                5 => "炉底",
                _ => "未知位置"
            };
        }
        
        private string GetdirectiontypeText(ushort directiontype)
        {
            return directiontype switch
            {
                0 => "无A/B面分别位置",
                1 => "A面",
                2 => "B面",
                _ => "未知方向类型"
            };
        }
        
        private string GetdirectionindexText(ushort directionindex)
        {
            return directionindex switch
            {
                0 => "只有一个测量点",
                1 => "阴极钢棒-1位置点",
                2 => "阴极钢棒-2位置点",
                _ => "未知方向编号"
            };
        }
    }
    
    private List<TemperatureSensorConfig> temperatureSensorConfigs = new List<TemperatureSensorConfig>();
    
    // 分类数组
    private List<TemperatureSensorConfig> amianyinjiConfigs = new List<TemperatureSensorConfig>(); // A面阴极
    private List<TemperatureSensorConfig> bmianyinjiConfigs = new List<TemperatureSensorConfig>(); // B面阴极
    private List<TemperatureSensorConfig> amiangangchuangConfigs = new List<TemperatureSensorConfig>(); // A面钢窗
    private List<TemperatureSensorConfig> bmiangangchuangConfigs = new List<TemperatureSensorConfig>(); // B面钢窗
    private List<TemperatureSensorConfig> duanbuConfigs = new List<TemperatureSensorConfig>(); // 端部
    private List<TemperatureSensorConfig> dibuConfigs = new List<TemperatureSensorConfig>(); // 底部
    
    private Coroutine periodicUpdateCoroutine;
    
    void Start()
    {
        if (autoLoadOnStart)
        {
            // 延迟执行，确保其他组件初始化完成
            StartCoroutine(DelayedAutoLoad());
        }
        
        if (enablePeriodicUpdate)
        {
            StartPeriodicUpdate();
        }
    }
    
    private IEnumerator DelayedAutoLoad()
    {
        yield return new WaitForSeconds(autoLoadDelay);
        LoadTemperatureSensorData();
    }
    
    /// <summary>
    /// 开始定期更新
    /// </summary>
    public void StartPeriodicUpdate()
    {
        if (periodicUpdateCoroutine != null)
        {
            StopCoroutine(periodicUpdateCoroutine);
        }
        periodicUpdateCoroutine = StartCoroutine(PeriodicUpdateCoroutine());
    }
    
    /// <summary>
    /// 停止定期更新
    /// </summary>
    public void StopPeriodicUpdate()
    {
        if (periodicUpdateCoroutine != null)
        {
            StopCoroutine(periodicUpdateCoroutine);
            periodicUpdateCoroutine = null;
        }
    }
    
    private IEnumerator PeriodicUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            LoadTemperatureSensorData();
        }
    }
    
    /// <summary>
    /// 手动加载温度传感器数据
    /// </summary>
    public void LoadTemperatureSensorData(string companyId = null)
    {
        //20251102,cxt
        //string id = companyId ?? defaultCompanyId;
        string id = GlobalUserData.AlSerialPantId;
        StartCoroutine(GetTemperatureSensorConfig(id));
    }
    
    public IEnumerator GetTemperatureSensorConfig(string companyId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 开始获取温度传感器配置，企业ID: {companyId}");
        }
        
        // 构建请求参数：企业标识 + "-66"
        string requestParam = $"{companyId}-66";
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 开始获取温度传感器配置信息，参数: {requestParam}");
        }
        
        // 按照现有API模式创建请求 - 直接用双引号包装字符串
        string jsonData = $"\"{requestParam}\"";
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
        
        // 创建UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(byteArray);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (enableDebugLog)
            {
                Debug.Log($"[AutoLoader] 发送请求到: {apiUrl}");
                Debug.Log($"[AutoLoader] 请求参数: {jsonData}");
            }
            
            // 发送请求
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"[AutoLoader] 请求失败: {request.error}";
                Debug.LogError(errorMsg);
                yield break;
            }
            
            string responseText = request.downloadHandler.text;
            if (enableDebugLog)
            {
                Debug.Log($"[AutoLoader] 响应内容: {responseText}");
            }
            
            // 解析响应
            ParseTemperatureSensorConfigResponse(responseText);
        }
    }
    
    private void ParseTemperatureSensorConfigResponse(string responseText)
    {
        try
        {
            // 去除可能的引号
            string tmpText = responseText.Trim('\"');
            
            // 处理转义字符
            if (tmpText.Contains("\\\""))
            {
                tmpText = tmpText.Replace("\\\"", "\"");
            }
            
            // 将Base64字符串转换为字节数组
            byte[] responseData = Convert.FromBase64String(tmpText);
            
            if (enableDebugLog)
            {
                Debug.Log($"[AutoLoader] 解码后数据长度: {responseData.Length} 字节");
                Debug.Log($"[AutoLoader] 原始字节数据: {BitConverter.ToString(responseData)}");
            }
            
            // 解析完整数据帧
            ParseDataFrame(responseData);
        }
        catch (Exception ex)
        {
            string errorMsg = $"[AutoLoader] 解析温度传感器配置数据时出错: {ex.Message}";
            Debug.LogError(errorMsg);
        }
    }

    private void ParseDataFrame(byte[] frameData)
    {
        if (frameData.Length < 14)
        {
            string errorMsg = $"[AutoLoader] 数据帧长度不足：{frameData.Length} 字节，最少需要14字节";
            Debug.LogError(errorMsg);
            return;
        }
        
        // 验证帧头：170, 85
        if (frameData[0] != 170 || frameData[1] != 85)
        {
            string errorMsg = $"[AutoLoader] 帧头错误：期望[170,85]，实际[{frameData[0]},{frameData[1]}]";
            Debug.LogError(errorMsg);
            return;
        }
        
        // 验证帧尾：165, 90
        if (frameData[frameData.Length - 2] != 165 || frameData[frameData.Length - 1] != 90)
        {
            string errorMsg = $"[AutoLoader] 帧尾错误：期望[165,90]，实际[{frameData[frameData.Length - 2]},{frameData[frameData.Length - 1]}]";
            Debug.LogError(errorMsg);
            return;
        }
        
        // 解析帧信息（大端序）
        ushort companyId = (ushort)((frameData[2] << 8) | frameData[3]);
        ushort frameType = (ushort)((frameData[4] << 8) | frameData[5]);
        ushort cellNumber = (ushort)((frameData[6] << 8) | frameData[7]);
        uint dataLength = (uint)((frameData[8] << 24) | (frameData[9] << 16) | (frameData[10] << 8) | frameData[11]);
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 帧头验证通过: [170, 85]");
            Debug.Log($"[AutoLoader] 企业标识: {companyId}");
            Debug.Log($"[AutoLoader] 帧类型: {frameType}");
            Debug.Log($"[AutoLoader] 槽号: {cellNumber}");
            Debug.Log($"[AutoLoader] 有效数据长度: {dataLength} 字节");
            Debug.Log($"[AutoLoader] 帧尾验证通过: [165, 90]");
        }
        
        // 验证数据长度
        uint expectedDataLength = (uint)(frameData.Length - 14); // 总长度 - 帧头帧尾和标识信息
        if (dataLength != expectedDataLength)
        {
            string errorMsg = $"[AutoLoader] 数据长度不匹配：声明{dataLength}字节，实际{expectedDataLength}字节";
            Debug.LogError(errorMsg);
            return;
        }
        
        // 提取有效数据内容（跳过前12字节的帧头和标识信息，去掉最后2字节的帧尾）
        byte[] validData = new byte[dataLength];
        Array.Copy(frameData, 12, validData, 0, (int)dataLength);
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 提取有效数据: {validData.Length} 字节");
            Debug.Log($"[AutoLoader] 有效数据内容: {BitConverter.ToString(validData)}");
        }
        
        // 解析温度传感器配置数据
        ParseTemperatureSensorData(validData);
    }

    private void ParseTemperatureSensorData(byte[] data)
    {
        temperatureSensorConfigs.Clear();
        
        if (data.Length % 12 != 0)
        {
            string errorMsg = $"[AutoLoader] 有效数据长度错误：{data.Length} 字节，应为12的整数倍";
            Debug.LogError(errorMsg);
            return;
        }
        
        int configCount = data.Length / 12;
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 解析到 {configCount} 个温度传感器配置");
        }
        
        // 按12字节一组解析配置数据
        for (int i = 0; i < configCount; i++)
        {
            byte[] configData = new byte[12];
            Array.Copy(data, i * 12, configData, 0, 12);
            
            try
            {
                TemperatureSensorConfig config = new TemperatureSensorConfig(configData);
                temperatureSensorConfigs.Add(config);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[AutoLoader] 配置 {i + 1}: 序号={config.sn}, 采样编号={config.sampleindex}, 位置={config.posindex}, 方向类型={config.directiontype}, 方向编号={config.directionindex}, 节点号={config.nodeindex}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoLoader] 解析第 {i + 1} 个配置时出错: {ex.Message}");
            }
        }
        
        // 处理数据并存储到全局管理器
        ProcessAndStoreData();
    }
    
    /// <summary>
    /// 处理数据并存储到全局管理器
    /// </summary>
    private void ProcessAndStoreData()
    {
        // 执行分类
        ClassifyTemperatureSensorConfigs();
        
        // 存储到全局管理器
        StoreToGlobalManager();
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 数据处理完成，共 {temperatureSensorConfigs.Count} 个配置");
            Debug.Log($"[AutoLoader] 分类结果 - A面阴极:{amianyinjiConfigs.Count}, B面阴极:{bmianyinjiConfigs.Count}, A面钢窗:{amiangangchuangConfigs.Count}, B面钢窗:{bmiangangchuangConfigs.Count}, 端部:{duanbuConfigs.Count}, 底部:{dibuConfigs.Count}");
        }
    }
    
    /// <summary>
    /// 根据Java SQL查询规则对温度传感器配置进行分类
    /// </summary>
    private void ClassifyTemperatureSensorConfigs()
    {
        // 清空之前的分类结果
        amianyinjiConfigs.Clear();
        bmianyinjiConfigs.Clear();
        amiangangchuangConfigs.Clear();
        bmiangangchuangConfigs.Clear();
        duanbuConfigs.Clear();
        dibuConfigs.Clear();
        
        foreach (var config in temperatureSensorConfigs)
        {
            // A面阴极: directiontype=1 AND posindex=4
            if (config.directiontype == 1 && config.posindex == 4)
            {
                amianyinjiConfigs.Add(config);
            }
            // B面阴极: directiontype=2 AND posindex=4
            else if (config.directiontype == 2 && config.posindex == 4)
            {
                bmianyinjiConfigs.Add(config);
            }
            // A面钢窗: directiontype=1 AND posindex=1
            else if (config.directiontype == 1 && config.posindex == 1)
            {
                amiangangchuangConfigs.Add(config);
            }
            // B面钢窗: directiontype=2 AND posindex=1
            else if (config.directiontype == 2 && config.posindex == 1)
            {
                bmiangangchuangConfigs.Add(config);
            }
            // 端部: (directiontype=0 AND posindex=3) or (directiontype=0 AND posindex=2)
            else if (config.directiontype == 0 && (config.posindex == 3 || config.posindex == 2))
            {
                duanbuConfigs.Add(config);
            }
            // 底部: directionindex=0 AND posindex=5
            else if (config.directionindex == 0 && config.posindex == 5)
            {
                dibuConfigs.Add(config);
            }
        }
        
        // 按照SQL中的ORDER BY规则对各分类进行排序
        SortClassifiedConfigs();
    }
    
    /// <summary>
    /// 按照SQL查询中的ORDER BY规则对分类后的配置进行排序
    /// </summary>
    private void SortClassifiedConfigs()
    {
        // A面阴极: ORDER BY directionindex, sampleindex ASC
        amianyinjiConfigs.Sort((a, b) => 
        {
            int result = a.nodeindex.CompareTo(b.nodeindex);
            return result != 0 ? result : a.sampleindex.CompareTo(b.sampleindex);
        });
        
        // B面阴极: ORDER BY directionindex, sampleindex ASC
        bmianyinjiConfigs.Sort((a, b) => 
        {
            int result = a.nodeindex.CompareTo(b.nodeindex);
            return result != 0 ? result : a.sampleindex.CompareTo(b.sampleindex);
        });
        
        // A面钢窗: ORDER BY sampleindex ASC
        amiangangchuangConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // B面钢窗: ORDER BY sampleindex ASC
        bmiangangchuangConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // 端部: ORDER BY sampleindex ASC
        duanbuConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // 底部: ORDER BY nodeindex ASC
        dibuConfigs.Sort((a, b) => a.nodeindex.CompareTo(b.nodeindex));
    }
    
    /// <summary>
    /// 将分类后的数据存储到全局管理器
    /// </summary>
    private void StoreToGlobalManager()
    {
        // 转换数据类型到原始TemperatureSensorConfigManager.TemperatureSensorConfig
        var convertedAmianyinji = ConvertToOriginalType(amianyinjiConfigs);
        var convertedBmianyinji = ConvertToOriginalType(bmianyinjiConfigs);
        var convertedAmiangangchuang = ConvertToOriginalType(amiangangchuangConfigs);
        var convertedBmiangangchuang = ConvertToOriginalType(bmiangangchuangConfigs);
        var convertedDuanbu = ConvertToOriginalType(duanbuConfigs);
        var convertedDibu = ConvertToOriginalType(dibuConfigs);
        
        // 将分类后的数据存储到全局管理器
        GlobalTemperatureSensorDataManager.Instance.StoreClassifiedConfigs(
            convertedAmianyinji,
            convertedBmianyinji,
            convertedAmiangangchuang,
            convertedBmiangangchuang,
            convertedDuanbu,
            convertedDibu
        );
        
        if (enableDebugLog)
        {
            Debug.Log($"[AutoLoader] 温度传感器配置数据已存储到全局管理器: {GlobalTemperatureSensorDataManager.Instance.GetDataStatistics()}");
        }
    }
    
    /// <summary>
    /// 转换数据类型到原始的TemperatureSensorConfigManager.TemperatureSensorConfig
    /// </summary>
    private List<TemperatureSensorConfigManager.TemperatureSensorConfig> ConvertToOriginalType(List<TemperatureSensorConfig> configs)
    {
        var result = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
        
        foreach (var config in configs)
        {
            try
            {
                var originalConfig = new TemperatureSensorConfigManager.TemperatureSensorConfig(config.rawData);
                result.Add(originalConfig);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoLoader] 转换配置数据时出错: {ex.Message}");
            }
        }
        
        return result;
    }
    
    // 公共方法：获取当前加载的配置数据
    public List<TemperatureSensorConfig> GetLoadedConfigs()
    {
        return new List<TemperatureSensorConfig>(temperatureSensorConfigs);
    }
    
    // 公共方法：获取分类统计
    public Dictionary<string, int> GetClassificationCounts()
    {
        return new Dictionary<string, int>
        {
            { "A面阴极", amianyinjiConfigs.Count },
            { "B面阴极", bmianyinjiConfigs.Count },
            { "A面钢窗", amiangangchuangConfigs.Count },
            { "B面钢窗", bmiangangchuangConfigs.Count },
            { "端部", duanbuConfigs.Count },
            { "底部", dibuConfigs.Count }
        };
    }
    
    // 公共方法：检查数据是否已加载
    public bool IsDataLoaded()
    {
        return temperatureSensorConfigs.Count > 0;
    }
    
    void OnDestroy()
    {
        StopPeriodicUpdate();
    }
}