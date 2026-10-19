using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class TemperatureSensorConfigManager : MonoBehaviour
{
    [Header("UI组件")]
    public Button getConfigButton;
    public TextMeshProUGUI displayText;
    public TMP_InputField companyIdInput; // 可选：让用户输入企业标识
    
    [Header("API设置")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    public string defaultCompanyId = "1033"; // 默认企业标识
    
    [Header("调试选项")]
    public bool enableDebugLog = true;
    
    // 温度传感器配置数据结构（每12字节一组）
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
    
    void Start()
    {
        // 初始化UI组件
        if (getConfigButton != null)
        {
            getConfigButton.onClick.AddListener(OnGetConfigButtonClick);
        }
        
        if (displayText != null)
        {
            displayText.text = "点击按钮获取温度传感器配置信息";
        }
    }
    
    private void OnGetConfigButtonClick()
    {
        //20251102,cxt
        //string companyId = !string.IsNullOrEmpty(companyIdInput?.text) ? companyIdInput.text : defaultCompanyId;
        string companyId = GlobalUserData.AlSerialPantId;
        StartCoroutine(GetTemperatureSensorConfig(companyId));
    }
    
    private IEnumerator GetTemperatureSensorConfig(string companyId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"开始获取温度传感器配置，企业ID: {companyId}");
        }
        
        // 构建请求参数：企业标识 + "-66"
        string requestParam = $"{companyId}-66";
        
        if (enableDebugLog)
        {
            Debug.Log($"开始获取温度传感器配置信息，参数: {requestParam}");
        }
        
        UpdateDisplayText("正在获取温度传感器配置信息...");
        
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
                Debug.Log($"发送请求到: {apiUrl}");
                Debug.Log($"请求参数: {jsonData}");
            }
            
            // 发送请求
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"请求失败: {request.error}";
                Debug.LogError(errorMsg);
                UpdateDisplayText(errorMsg);
                yield break;
            }
            
            string responseText = request.downloadHandler.text;
            if (enableDebugLog)
            {
                Debug.Log($"响应内容: {responseText}");
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
                Debug.Log($"解码后数据长度: {responseData.Length} 字节");
                Debug.Log($"原始字节数据: {BitConverter.ToString(responseData)}");
                
                // 按16字节一行显示，便于查看
                for (int i = 0; i < responseData.Length; i += 16)
                {
                    int length = Math.Min(16, responseData.Length - i);
                    byte[] chunk = new byte[length];
                    Array.Copy(responseData, i, chunk, 0, length);
                    Debug.Log($"偏移 {i:D4}: {BitConverter.ToString(chunk)}");
                }
            }
            
            // 解析完整数据帧
            ParseDataFrame(responseData);
        }
        catch (Exception ex)
        {
            string errorMsg = $"解析温度传感器配置数据时出错: {ex.Message}";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
        }
    }

    private void ParseDataFrame(byte[] frameData)
    {
        if (frameData.Length < 14)
        {
            string errorMsg = $"数据帧长度不足：{frameData.Length} 字节，最少需要14字节";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
            return;
        }
        
        // 验证帧头：170, 85
        if (frameData[0] != 170 || frameData[1] != 85)
        {
            string errorMsg = $"帧头错误：期望[170,85]，实际[{frameData[0]},{frameData[1]}]";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
            return;
        }
        
        // 验证帧尾：165, 90
        if (frameData[frameData.Length - 2] != 165 || frameData[frameData.Length - 1] != 90)
        {
            string errorMsg = $"帧尾错误：期望[165,90]，实际[{frameData[frameData.Length - 2]},{frameData[frameData.Length - 1]}]";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
            return;
        }
        
        // 解析帧信息（大端序）
        ushort companyId = (ushort)((frameData[2] << 8) | frameData[3]);
        ushort frameType = (ushort)((frameData[4] << 8) | frameData[5]);
        ushort cellNumber = (ushort)((frameData[6] << 8) | frameData[7]);
        uint dataLength = (uint)((frameData[8] << 24) | (frameData[9] << 16) | (frameData[10] << 8) | frameData[11]);
        
        if (enableDebugLog)
        {
            Debug.Log($"帧头验证通过: [170, 85]");
            Debug.Log($"企业标识: {companyId}");
            Debug.Log($"帧类型: {frameType}");
            Debug.Log($"槽号: {cellNumber}");
            Debug.Log($"有效数据长度: {dataLength} 字节");
            Debug.Log($"帧尾验证通过: [165, 90]");
        }
        
        // 验证数据长度
        uint expectedDataLength = (uint)(frameData.Length - 14); // 总长度 - 帧头帧尾和标识信息
        if (dataLength != expectedDataLength)
        {
            string errorMsg = $"数据长度不匹配：声明{dataLength}字节，实际{expectedDataLength}字节";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
            return;
        }
        
        // 提取有效数据内容（跳过前12字节的帧头和标识信息，去掉最后2字节的帧尾）
        byte[] validData = new byte[dataLength];
        Array.Copy(frameData, 12, validData, 0, (int)dataLength);
        
        if (enableDebugLog)
        {
            Debug.Log($"提取有效数据: {validData.Length} 字节");
            Debug.Log($"有效数据内容: {BitConverter.ToString(validData)}");
        }
        
        // 解析温度传感器配置数据
        ParseTemperatureSensorData(validData);
    }

    private void ParseTemperatureSensorData(byte[] data)
    {
        temperatureSensorConfigs.Clear();
        
        if (data.Length % 12 != 0)
        {
            string errorMsg = $"有效数据长度错误：{data.Length} 字节，应为12的整数倍";
            Debug.LogError(errorMsg);
            UpdateDisplayText(errorMsg);
            return;
        }
        
        int configCount = data.Length / 12;
        
        if (enableDebugLog)
        {
            Debug.Log($"解析到 {configCount} 个温度传感器配置");
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
                    Debug.Log($"配置 {i + 1}: 序号={config.sn}, 采样编号={config.sampleindex}, 位置={config.posindex}, 方向类型={config.directiontype}, 方向编号={config.directionindex}, 节点号={config.nodeindex}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析第 {i + 1} 个配置时出错: {ex.Message}");
            }
        }
        
        // 更新显示
        UpdateDisplayWithConfigs();
    }
    
    // 添加分类数组
    [Header("分类结果")]
    public List<TemperatureSensorConfig> amianyinjiConfigs = new List<TemperatureSensorConfig>(); // A面阴极
    public List<TemperatureSensorConfig> bmianyinjiConfigs = new List<TemperatureSensorConfig>(); // B面阴极
    public List<TemperatureSensorConfig> amiangangchuangConfigs = new List<TemperatureSensorConfig>(); // A面钢窗
    public List<TemperatureSensorConfig> bmiangangchuangConfigs = new List<TemperatureSensorConfig>(); // B面钢窗
    public List<TemperatureSensorConfig> duanbuConfigs = new List<TemperatureSensorConfig>(); // 端口
    public List<TemperatureSensorConfig> dibuConfigs = new List<TemperatureSensorConfig>(); // 底部
    
    private void UpdateDisplayWithConfigs()
    {
        if (displayText == null) return;
        
        // 执行分类
        ClassifyTemperatureSensorConfigs();
        
        // 存储到全局管理器
        StoreToGlobalManager();
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"温度传感器配置信息 (共 {temperatureSensorConfigs.Count} 个)");
        sb.AppendLine("=" + new string('=', 50));
        
        // 显示分类统计
        sb.AppendLine("\n=== 分类统计 ===");
        sb.AppendLine($"A面阴极: {amianyinjiConfigs.Count} 个");
        sb.AppendLine($"B面阴极: {bmianyinjiConfigs.Count} 个");
        sb.AppendLine($"A面钢窗: {amiangangchuangConfigs.Count} 个");
        sb.AppendLine($"B面钢窗: {bmiangangchuangConfigs.Count} 个");
        sb.AppendLine($"端口: {duanbuConfigs.Count} 个");
        sb.AppendLine($"底部: {dibuConfigs.Count} 个");
        sb.AppendLine();
        
        // 显示详细配置信息
        foreach (var config in temperatureSensorConfigs)
        {
            sb.Append(config.ToString());
        }
        
        displayText.text = sb.ToString();
        
        if (enableDebugLog)
        {
            Debug.Log($"显示了 {temperatureSensorConfigs.Count} 个温度传感器配置");
            Debug.Log($"分类结果 - A面阴极:{amianyinjiConfigs.Count}, B面阴极:{bmianyinjiConfigs.Count}, A面钢窗:{amiangangchuangConfigs.Count}, B面钢窗:{bmiangangchuangConfigs.Count}, 端口:{duanbuConfigs.Count}, 底部:{dibuConfigs.Count}");
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
            // 端口: (directiontype=0 AND posindex=3) or (directiontype=0 AND posindex=2)
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
            int result = a.directionindex.CompareTo(b.directionindex);
            return result != 0 ? result : a.sampleindex.CompareTo(b.sampleindex);
        });
        
        // B面阴极: ORDER BY directionindex, sampleindex ASC
        bmianyinjiConfigs.Sort((a, b) => 
        {
            int result = a.directionindex.CompareTo(b.directionindex);
            return result != 0 ? result : a.sampleindex.CompareTo(b.sampleindex);
        });
        
        // A面钢窗: ORDER BY sampleindex ASC
        amiangangchuangConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // B面钢窗: ORDER BY sampleindex ASC
        bmiangangchuangConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // 端口: ORDER BY sampleindex ASC
        duanbuConfigs.Sort((a, b) => a.sampleindex.CompareTo(b.sampleindex));
        
        // 底部: ORDER BY nodeindex ASC
        dibuConfigs.Sort((a, b) => a.nodeindex.CompareTo(b.nodeindex));
    }
    
    // 公共方法：获取各分类的配置列表
    public List<TemperatureSensorConfig> GetAmianyinjiConfigs() => new List<TemperatureSensorConfig>(amianyinjiConfigs);
    public List<TemperatureSensorConfig> GetBmianyinjiConfigs() => new List<TemperatureSensorConfig>(bmianyinjiConfigs);
    public List<TemperatureSensorConfig> GetAmiangangchuangConfigs() => new List<TemperatureSensorConfig>(amiangangchuangConfigs);
    public List<TemperatureSensorConfig> GetBmiangangchuangConfigs() => new List<TemperatureSensorConfig>(bmiangangchuangConfigs);
    public List<TemperatureSensorConfig> GetDuanbuConfigs() => new List<TemperatureSensorConfig>(duanbuConfigs);
    public List<TemperatureSensorConfig> GetDibuConfigs() => new List<TemperatureSensorConfig>(dibuConfigs);
    
    /// <summary>
    /// 获取指定分类的配置数量
    /// </summary>
    public Dictionary<string, int> GetClassificationCounts()
    {
        return new Dictionary<string, int>
        {
            { "A面阴极", amianyinjiConfigs.Count },
            { "B面阴极", bmianyinjiConfigs.Count },
            { "A面钢窗", amiangangchuangConfigs.Count },
            { "B面钢窗", bmiangangchuangConfigs.Count },
            { "端口", duanbuConfigs.Count },
            { "底部", dibuConfigs.Count }
        };
    }
    
    /// <summary>
    /// 根据分类名称获取对应的配置列表
    /// </summary>
    public List<TemperatureSensorConfig> GetConfigsByCategory(string category)
    {
        return category switch
        {
            "A面阴极" => GetAmianyinjiConfigs(),
            "B面阴极" => GetBmianyinjiConfigs(),
            "A面钢窗" => GetAmiangangchuangConfigs(),
            "B面钢窗" => GetBmiangangchuangConfigs(),
            "端口" => GetDuanbuConfigs(),
            "底部" => GetDibuConfigs(),
            _ => new List<TemperatureSensorConfig>()
        };
    }
    
    // 公共方法：手动触发获取配置
    public void GetTemperatureSensorConfigManually(string companyId = null)
    {
        //20251102,cxt
        //string id = companyId ?? defaultCompanyId;
        string id = GlobalUserData.AlSerialPantId;
        StartCoroutine(GetTemperatureSensorConfig(id));
    }
    
    // 公共方法：获取配置列表
    public List<TemperatureSensorConfig> GetTemperatureSensorConfigList()
    {
        return new List<TemperatureSensorConfig>(temperatureSensorConfigs);
    }
    
    /// <summary>
    /// 更新显示文本
    /// </summary>
    private void UpdateDisplayText(string content)
    {
        if (displayText != null)
        {
            displayText.text = content;
        }
    }
    
    /// <summary>
    /// 将分类后的数据存储到全局管理器
    /// </summary>
    private void StoreToGlobalManager()
    {
        // 将分类后的数据存储到全局管理器
        GlobalTemperatureSensorDataManager.Instance.StoreClassifiedConfigs(
            amianyinjiConfigs,
            bmianyinjiConfigs,
            amiangangchuangConfigs,
            bmiangangchuangConfigs,
            duanbuConfigs,
            dibuConfigs
        );
        
        if (enableDebugLog)
        {
            Debug.Log($"温度传感器配置数据已存储到全局管理器: {GlobalTemperatureSensorDataManager.Instance.GetDataStatistics()}");
        }
    }
}