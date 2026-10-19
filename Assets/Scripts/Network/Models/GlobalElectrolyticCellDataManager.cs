using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalElectrolyticCellDataManager : MonoBehaviour
{
    private static GlobalElectrolyticCellDataManager _instance;
    public static GlobalElectrolyticCellDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GlobalElectrolyticCellDataManager");
                _instance = go.AddComponent<GlobalElectrolyticCellDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("全局电解槽配置数据")]
    [SerializeField] private List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> allConfigs = new List<ElectrolyticCellConfigManager.ElectrolyticCellConfig>();
    
    [Header("数据状态")]
    public bool isDataLoaded = false;
    public DateTime lastUpdateTime;
    public int totalConfigCount = 0;

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
    /// 存储所有电解槽配置数据
    /// </summary>
    /// <param name="configs">配置列表</param>
    public void StoreAllConfigs(List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> configs)
    {
        allConfigs.Clear();
        allConfigs.AddRange(configs);
        
        isDataLoaded = true;
        lastUpdateTime = DateTime.Now;
        totalConfigCount = allConfigs.Count;
        
        Debug.Log($"全局数据管理器已存储 {totalConfigCount} 个电解槽配置");
        
        // 延迟触发事件，确保所有组件都已初始化
        StartCoroutine(TriggerDataUpdateEvent());
    }
    
    /// <summary>
    /// 延迟触发数据更新事件
    /// </summary>
    private System.Collections.IEnumerator TriggerDataUpdateEvent()
    {
        // 等待一帧，确保所有组件都已初始化
        yield return null;
        
        // 触发数据更新事件
        OnDataUpdated?.Invoke();
        
        // 尝试自动刷新SlotNumberSelector的默认值
        try
        {
            var selector = FindObjectOfType<SlotNumberSelector>();
            if (selector != null)
            {
                Debug.Log("[GlobalElectrolyticCellDataManager] 数据加载完成，自动刷新槽号选择器默认值");
                selector.StartCoroutine(selector.InitializeDefaultsFromData());
            }
            else
            {
                Debug.LogWarning("[GlobalElectrolyticCellDataManager] 未找到 SlotNumberSelector 实例，跳过自动刷新");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GlobalElectrolyticCellDataManager] 自动刷新 SlotNumberSelector 失败: {ex.Message}");
        }
        
        Debug.Log("数据更新事件已触发");
    }

    /// <summary>
    /// 根据车间号筛选配置
    /// </summary>
    /// <param name="workshopNumber">车间号</param>
    /// <returns>筛选后的配置列表</returns>
    public List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> GetConfigsByWorkshop(byte workshopNumber)
    {
        return allConfigs.Where(config => config.workshopNumber == workshopNumber).ToList();
    }

    /// <summary>
    /// 根据工区号筛选配置
    /// </summary>
    /// <param name="workAreaNumber">工区号</param>
    /// <returns>筛选后的配置列表</returns>
    public List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> GetConfigsByWorkArea(byte workAreaNumber)
    {
        return allConfigs.Where(config => config.workAreaNumber == workAreaNumber).ToList();
    }

    /// <summary>
    /// 根据车间号和工区号筛选配置
    /// </summary>
    /// <param name="workshopNumber">车间号</param>
    /// <param name="workAreaNumber">工区号</param>
    /// <returns>筛选后的配置列表</returns>
    public List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> GetConfigsByWorkshopAndArea(byte workshopNumber, byte workAreaNumber)
    {
        return allConfigs.Where(config => 
            config.workshopNumber == workshopNumber && 
            config.workAreaNumber == workAreaNumber).ToList();
    }

    /// <summary>
    /// 获取所有车间号
    /// </summary>
    /// <returns>车间号列表</returns>
    public List<byte> GetAllWorkshopNumbers()
    {
        return allConfigs.Select(config => config.workshopNumber).Distinct().OrderBy(x => x).ToList();
    }

    /// <summary>
    /// 获取指定车间的所有工区号
    /// </summary>
    /// <param name="workshopNumber">车间号</param>
    /// <returns>工区号列表</returns>
    public List<byte> GetWorkAreaNumbersByWorkshop(byte workshopNumber)
    {
        return allConfigs
            .Where(config => config.workshopNumber == workshopNumber)
            .Select(config => config.workAreaNumber)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    /// <summary>
    /// 获取指定车间和工区的所有槽号
    /// </summary>
    /// <param name="workshopNumber">车间号</param>
    /// <param name="workAreaNumber">工区号</param>
    /// <returns>槽号列表</returns>
    public List<ushort> GetSlotNumbersByWorkshopAndArea(byte workshopNumber, byte workAreaNumber)
    {
        return allConfigs
            .Where(config => config.workshopNumber == workshopNumber && config.workAreaNumber == workAreaNumber)
            .Select(config => config.slotNumber)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    /// <summary>
    /// 根据槽号获取配置
    /// </summary>
    /// <param name="slotNumber">槽号</param>
    /// <returns>配置对象，如果未找到返回null</returns>
    public ElectrolyticCellConfigManager.ElectrolyticCellConfig GetConfigBySlotNumber(ushort slotNumber)
    {
        return allConfigs.FirstOrDefault(config => config.slotNumber == slotNumber);
    }

    /// <summary>
    /// 获取所有配置数据的副本
    /// </summary>
    /// <returns>所有配置的副本</returns>
    public List<ElectrolyticCellConfigManager.ElectrolyticCellConfig> GetAllConfigs()
    {
        return new List<ElectrolyticCellConfigManager.ElectrolyticCellConfig>(allConfigs);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void ClearAllData()
    {
        allConfigs.Clear();
        isDataLoaded = false;
        totalConfigCount = 0;
        Debug.Log("全局电解槽配置数据已清空");
        
        OnDataCleared?.Invoke();
    }

    /// <summary>
    /// 获取数据统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetDataStatistics()
    {
        if (!isDataLoaded || allConfigs.Count == 0)
        {
            return "暂无数据";
        }

        var workshopCount = GetAllWorkshopNumbers().Count;
        var workAreaCount = allConfigs.Select(c => c.workAreaNumber).Distinct().Count();
        var slotCount = allConfigs.Select(c => c.slotNumber).Distinct().Count();

        return $"总配置数: {totalConfigCount}\n" +
               $"车间数: {workshopCount}\n" +
               $"工区数: {workAreaCount}\n" +
               $"槽数: {slotCount}\n" +
               $"最后更新: {lastUpdateTime:yyyy-MM-dd HH:mm:ss}";
    }

    // 事件
    public System.Action OnDataUpdated;
    public System.Action OnDataCleared;
}