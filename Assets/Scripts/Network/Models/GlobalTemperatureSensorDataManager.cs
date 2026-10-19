using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalTemperatureSensorDataManager : MonoBehaviour
{
    private static GlobalTemperatureSensorDataManager _instance;
    public static GlobalTemperatureSensorDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GlobalTemperatureSensorDataManager");
                _instance = go.AddComponent<GlobalTemperatureSensorDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("全局温度传感器配置数据")]
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> allConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    
    // 分类存储的数据
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> amianyinjiConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> bmianyinjiConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> amiangangchuangConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> bmiangangchuangConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> duanbuConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    [SerializeField] private List<TemperatureSensorConfigManager.TemperatureSensorConfig> dibuConfigs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
    
    [Header("数据状态")]
    public bool isDataLoaded = false;
    public DateTime lastUpdateTime;
    public int totalConfigCount = 0;
    
    // 数据更新事件
    public System.Action OnDataUpdated;
    [Header("全局温度数据存储")]
    [SerializeField] private Dictionary<string, List<HistoryTemperatureData>> temperatureDataCache = new Dictionary<string, List<HistoryTemperatureData>>();
    
    [System.Serializable]
    public class HistoryTemperatureData
    {
        public DateTime date;
        public int recordNumber;
        public List<int> temperatureValues; // 253个采样点的温度数据
        public string companyIndex;
        public int slotNumber;
        public string queryDate;
        
        public HistoryTemperatureData()
        {
            temperatureValues = new List<int>();
        }
    }
    
    /// <summary>
    /// 存储温度数据到全局缓存
    /// </summary>
    public void StoreTemperatureData(string key, List<HistoryTemperatureData> data)
    {
        if (temperatureDataCache.ContainsKey(key))
        {
            temperatureDataCache[key] = data;
        }
        else
        {
            temperatureDataCache.Add(key, data);
        }
        
        Debug.Log($"已存储温度数据到全局缓存: {key}, 记录数: {data.Count}");
    }
    
    /// <summary>
    /// 从全局缓存获取温度数据
    /// </summary>
    public List<HistoryTemperatureData> GetTemperatureData(string key)
    {
        if (temperatureDataCache.ContainsKey(key))
        {
            return new List<HistoryTemperatureData>(temperatureDataCache[key]);
        }
        return new List<HistoryTemperatureData>();
    }
    
    /// <summary>
    /// 检查是否存在指定的温度数据
    /// </summary>
    public bool HasTemperatureData(string key)
    {
        return temperatureDataCache.ContainsKey(key) && temperatureDataCache[key].Count > 0;
    }
    
    /// <summary>
    /// 生成温度数据缓存键
    /// </summary>
    public static string GenerateTemperatureDataKey(string companyIndex, int slotNumber, string date)
    {
        return $"{companyIndex}-{slotNumber}-{date}";
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 存储所有温度传感器配置数据
    /// </summary>
    public void StoreAllConfigs(List<TemperatureSensorConfigManager.TemperatureSensorConfig> configs)
    {
        allConfigs.Clear();
        allConfigs.AddRange(configs);
        
        // 重新分类数据
        ClassifyConfigs();
        
        isDataLoaded = true;
        lastUpdateTime = DateTime.Now;
        totalConfigCount = allConfigs.Count;
        
        Debug.Log($"全局温度传感器数据管理器已存储 {totalConfigCount} 个配置");
        
        // 触发数据更新事件
        OnDataUpdated?.Invoke();
    }
    
    /// <summary>
    /// 存储分类后的配置数据
    /// </summary>
    public void StoreClassifiedConfigs(
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> amianyinji,
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> bmianyinji,
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> amiangangchuang,
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> bmiangangchuang,
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> duanbu,
        List<TemperatureSensorConfigManager.TemperatureSensorConfig> dibu)
    {
        amianyinjiConfigs.Clear();
        amianyinjiConfigs.AddRange(amianyinji);
        
        bmianyinjiConfigs.Clear();
        bmianyinjiConfigs.AddRange(bmianyinji);
        
        amiangangchuangConfigs.Clear();
        amiangangchuangConfigs.AddRange(amiangangchuang);
        
        bmiangangchuangConfigs.Clear();
        bmiangangchuangConfigs.AddRange(bmiangangchuang);
        
        duanbuConfigs.Clear();
        duanbuConfigs.AddRange(duanbu);
        
        dibuConfigs.Clear();
        dibuConfigs.AddRange(dibu);
        
        // 合并所有分类数据到总列表
        allConfigs.Clear();
        allConfigs.AddRange(amianyinjiConfigs);
        allConfigs.AddRange(bmianyinjiConfigs);
        allConfigs.AddRange(amiangangchuangConfigs);
        allConfigs.AddRange(bmiangangchuangConfigs);
        allConfigs.AddRange(duanbuConfigs);
        allConfigs.AddRange(dibuConfigs);
        
        isDataLoaded = true;
        lastUpdateTime = DateTime.Now;
        totalConfigCount = allConfigs.Count;
        
        Debug.Log($"全局温度传感器数据管理器已存储分类数据，总计 {totalConfigCount} 个配置");
        
        OnDataUpdated?.Invoke();
    }

    /// <summary>
    /// 对配置进行分类
    /// </summary>
    private void ClassifyConfigs()
    {
        amianyinjiConfigs.Clear();
        bmianyinjiConfigs.Clear();
        amiangangchuangConfigs.Clear();
        bmiangangchuangConfigs.Clear();
        duanbuConfigs.Clear();
        dibuConfigs.Clear();
        
        foreach (var config in allConfigs)
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
    }

    // 公共访问方法
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetAllConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(allConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetAmianyinjiConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(amianyinjiConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetBmianyinjiConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(bmianyinjiConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetAmiangangchuangConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(amiangangchuangConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetBmiangangchuangConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(bmiangangchuangConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetDuanbuConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(duanbuConfigs);
    }
    
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetDibuConfigs()
    {
        return new List<TemperatureSensorConfigManager.TemperatureSensorConfig>(dibuConfigs);
    }
    
    /// <summary>
    /// 根据分类名称获取配置
    /// </summary>
    public List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetConfigsByCategory(string category)
    {
        return category switch
        {
            "A面阴极" => GetAmianyinjiConfigs(),
            "B面阴极" => GetBmianyinjiConfigs(),
            "A面钢窗" => GetAmiangangchuangConfigs(),
            "B面钢窗" => GetBmiangangchuangConfigs(),
            "端部" => GetDuanbuConfigs(),
            "底部" => GetDibuConfigs(),
            _ => new List<TemperatureSensorConfigManager.TemperatureSensorConfig>()
        };
    }
    
    /// <summary>
    /// 获取数据统计信息
    /// </summary>
    public string GetDataStatistics()
    {
        return $"总配置数: {totalConfigCount}, A面阴极: {amianyinjiConfigs.Count}, B面阴极: {bmianyinjiConfigs.Count}, A面钢窗: {amiangangchuangConfigs.Count}, B面钢窗: {bmiangangchuangConfigs.Count}, 端部: {duanbuConfigs.Count}, 底部: {dibuConfigs.Count}, 最后更新: {lastUpdateTime:yyyy-MM-dd HH:mm:ss}";
    }
}