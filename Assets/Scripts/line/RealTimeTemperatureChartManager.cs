using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;

namespace XCharts.Example
{
    [DisallowMultipleComponent]
    public class RealTimeTemperatureChartManager : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int connectionTimeout = 30;
        
        [Header("筛选设置")]
        public TextMeshProUGUI potNameText;
        
        public Button applyFilterButton;
        
        [Header("加载提示")]
        public TextMeshProUGUI loadingText; // 加载提示文本
        
        [Header("多图表布局设置")]
        public GameObject chartPrefab; // LineChart预制体
        public Transform chartsContainer; // 放置图表的容器
        public ScrollRect scrollRect; // 滚动视图
        public int columns = 4; // 每行显示的图表数量
        public Vector2 chartSize = new Vector2(300, 200); // 每个图表的大小
        public Vector2 spacing = new Vector2(20, 20); // 图表之间的间距
        
        [Header("传感器类别选择")]
        public Button categoryButton阴极A;
        public Button categoryButton阴极B;
        public Button categoryButton钢窗A;
        public Button categoryButton钢窗B;
        public Button categoryButton端部;
        public Button categoryButton底部;
        
        [Header("自动更新设置")]
        [SerializeField] private bool enableAutoUpdate = true; // 是否启用自动更新
        [SerializeField] private float updateInterval = 600f; // 更新间隔（秒）

        // 自动更新控制
        private Coroutine autoUpdateCoroutine;
        private bool isUpdating = false;
        private bool isRendering = false;
        
        // 或者使用按钮数组的方式
        // public Button[] categoryButtons; // 按顺序：A面阴极, B面阴极, A面钢窗, B面钢窗, 端口, 底部
        
        // 筛选条件 - 改为属性，从UI组件读取
        private string selectedPotName => potNameText != null ? potNameText.text : "槽号选择";
        // private string selectedDate = "20250622"; // 实时数据不需要日期参数
        
        private string selectedCategory = "A面阴极"; 
        
        // 数据缓存
        private List<SensorTemperatureData> allSensorData = new List<SensorTemperatureData>();
        private List<LineChart> chartInstances = new List<LineChart>();
        private List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> currentHistoryData = new List<GlobalTemperatureSensorDataManager.HistoryTemperatureData>();
        
        //程序是不是第一次运行
        private bool isFirstRun = true;
		float nextExecutionTime = 0; // 下次执行的时间点

		// 添加实时温度数据结构
		[System.Serializable]
        public class RealTimeTemperatureData
        {
            public int timestamp; // 时间戳（秒）
            public List<int> temperatureValues; // 240个测温点的温度值 - 改为int
            
            public RealTimeTemperatureData()
            {
                temperatureValues = new List<int>();
            }
        }
        
        // 本地实时数据存储
        private List<RealTimeTemperatureData> currentRealTimeData;
        
        // 传感器温度数据结构
        [System.Serializable]
        public class SensorTemperatureData
        {
            public int sensorId;
            public string category;
            public int sampleIndex; // 对应API返回的采样编号
            public List<int> temperatureValues; // 300个时间点的温度值 - 改为int
            public List<string> timeLabels; // 对应的时间标签
            public TemperatureSensorConfigManager.TemperatureSensorConfig config; // 添加传感器配置字段
            public TemperatureSensorConfigManager.TemperatureSensorConfig sensorConfig; // 优化后的传感器配置字段
            
            public SensorTemperatureData()
            {
                temperatureValues = new List<int>();
                timeLabels = new List<string>();
            }
            
            public SensorTemperatureData(TemperatureSensorConfigManager.TemperatureSensorConfig sensorConfig)
            {
                temperatureValues = new List<int>();
                timeLabels = new List<string>();
                config = sensorConfig;
                sampleIndex = sensorConfig.sampleindex;
            }
        }
        
        void Awake()
        {
            InitUI();
            InitCategoryButtons();
        }
        
        void Start()
        {
            // 检查全局传感器数据是否已加载
            if (GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                UnityEngine.Debug.Log("传感器配置数据已加载，可以开始获取温度数据");
                // 添加数据验证
                ValidateSensorData();

                // 首次加载数据
                LoadTemperatureData();                
                
                // 启动自动更新
                // 统一将刷新间隔提升为10分钟，避免场景序列化保留旧值导致频繁刷新
                updateInterval = 600f;
                if (enableAutoUpdate)
                {
                    StartAutoUpdate();
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("传感器配置数据未加载，请先运行AutoTemperatureSensorDataLoader");
            }

			isFirstRun = true;

            // 加载配置

            //加载实时温度数据
		}

        private void Update()
        {
            // 原有实时曲线逻辑
            if (Time.time >= nextExecutionTime)
            {
                nextExecutionTime = Time.time + 2f; // 每2秒拉一次数据
                if (isFirstRun)
                {
                    if (GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
                    {
                        if (chartsContainer.childCount == 0)
                            OnApplyFilter();
                        else
                            isFirstRun = false;
                    }
                }
            }

            // 每10分钟刷新X轴（动态到当前时间）
            if (Time.time - lastXAxisUpdateTime > 600f)
            {
                foreach (var chart in chartInstances)
                {
                    if (chart != null)
                    {
                        ConfigureXAxisOptimized(chart);
                        chart.RefreshChart();
                    }
                }
                lastXAxisUpdateTime = Time.time;
            }
        }


        private void InitUI()
        {
            if (potNameText != null)
                //potNameText.text = "1001"; // 设置默认值
            
            //if (dateText != null)
                //dateText.text = selectedDate;
            
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveAllListeners();
                applyFilterButton.onClick.AddListener(OnApplyFilter); // 启用筛选功能
            }
        }
        
        // 添加筛选按钮的处理方法
        public void OnApplyFilter()
        {
            UnityEngine.Debug.Log($"应用筛选: 槽号={selectedPotName}, 类别={selectedCategory}");
            
            // 停止当前的自动更新
            StopAutoUpdate();
            
            // 重新加载数据
            LoadTemperatureData();
            
            // 重新启动自动更新
            if (enableAutoUpdate)
            {
                StartAutoUpdate();
            }
        }
        
  
        
        private void InitCategoryButtons()
        {
            UnityEngine.Debug.Log("初始化类别按钮");
            
            if (categoryButton阴极A != null)
            {
                categoryButton阴极A.onClick.RemoveAllListeners();
                categoryButton阴极A.onClick.AddListener(() => OnCategoryButtonClicked("A面阴极"));
            }
            
            if (categoryButton阴极B != null)
            {
                categoryButton阴极B.onClick.RemoveAllListeners();
                categoryButton阴极B.onClick.AddListener(() => OnCategoryButtonClicked("B面阴极"));
            }
            
            if (categoryButton钢窗A != null)
            {
                categoryButton钢窗A.onClick.RemoveAllListeners();
                categoryButton钢窗A.onClick.AddListener(() => OnCategoryButtonClicked("A面钢窗"));
            }
            
            if (categoryButton钢窗B != null)
            {
                categoryButton钢窗B.onClick.RemoveAllListeners();
                categoryButton钢窗B.onClick.AddListener(() => OnCategoryButtonClicked("B面钢窗"));
            }
            
            if (categoryButton端部 != null)
            {
                categoryButton端部.onClick.RemoveAllListeners();
                categoryButton端部.onClick.AddListener(() => OnCategoryButtonClicked("端部"));
            }
            
            if (categoryButton底部 != null)
            {
                categoryButton底部.onClick.RemoveAllListeners();
                categoryButton底部.onClick.AddListener(() => OnCategoryButtonClicked("底部"));
            }
            
            UpdateCategoryButtonVisualState();
        }
        
   
        
        private void OnCategoryButtonClicked(string category)
        {
            selectedCategory = category;
            UnityEngine.Debug.Log($"选择类别: {selectedCategory}");
            
            // 更新按钮视觉状态
            UpdateCategoryButtonVisualState();
            
            // 停止当前的自动更新
            StopAutoUpdate();
            
            // 立即重新加载数据
            LoadTemperatureData();
            
            // 重新启动自动更新
            if (enableAutoUpdate)
            {
                StartAutoUpdate();
                UnityEngine.Debug.Log($"重新启动自动更新 - 类别: {selectedCategory}");
            }
        }
        
        private void UpdateCategoryButtonVisualState()
        {
            // 选中色 #049151，未选中为透明
            Color selectedColor = new Color32(4, 145, 81, 255); // #049151
            Color normalColor = Color.clear; // 透明
            
            SetButtonColor(categoryButton阴极A, selectedCategory == "A面阴极", selectedColor, normalColor);
            SetButtonColor(categoryButton阴极B, selectedCategory == "B面阴极", selectedColor, normalColor);
            SetButtonColor(categoryButton钢窗A, selectedCategory == "A面钢窗", selectedColor, normalColor);
            SetButtonColor(categoryButton钢窗B, selectedCategory == "B面钢窗", selectedColor, normalColor);
            SetButtonColor(categoryButton端部, selectedCategory == "端部", selectedColor, normalColor);
            SetButtonColor(categoryButton底部, selectedCategory == "底部", selectedColor, normalColor);
        }

        /// <summary>
        /// 设置按钮颜色，优先设置子物体visual的颜色，否则设置根节点Image
        /// </summary>
        private void SetButtonColor(Button btn, bool isSelected, Color selectedColor, Color normalColor)
        {
            if (btn == null) return;
            // 优先查找子物体 visual 的 Image
            var visualTransform = btn.transform.Find("visual");
            var targetImg = visualTransform != null ? visualTransform.GetComponent<Image>() : btn.GetComponent<Image>();
            if (targetImg != null)
            {
                targetImg.color = isSelected ? selectedColor : normalColor;
            }
        }
        
        
        
        public void LoadTemperatureData()
        {
            if (!gameObject.activeInHierarchy)
            {
                UnityEngine.Debug.LogWarning("游戏对象未激活，无法启动协程");
                return;
            }
            
            if (!GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                UnityEngine.Debug.LogError("传感器配置数据未加载，无法获取温度数据");
                return;
            }
            
            if (isUpdating)
            {
                UnityEngine.Debug.Log("正在更新中，跳过本次请求");
                return;
            }
            
            // 如果槽号为空，则不加载数据 20250813
            if (selectedPotName == "请先选择车间工区" || selectedPotName == "槽号选择" || selectedPotName.Length == 0)
            {
				UnityEngine.Debug.Log("槽号为空，跳过本次请求");
				return;
            }

            isUpdating = true;

            // 显示加载提示
			if (selectedPotName != null && selectedPotName.Length > 0 && selectedPotName != "槽号选择" && selectedPotName != "请先选择车间工区")
            {
				ShowLoadingText(true);
			}				
            
            // 清空现有数据和图表
            allSensorData.Clear();
            ClearCharts();
            
            UnityEngine.Debug.Log("开始加载温度数据...");
            
            // 使用优化的批量数据获取
            StartCoroutine(FetchAllSensorDataOptimized());
        }
/// <summary>
/// 首先获取历史温度数据
/// </summary>
private IEnumerator FetchHistoryTemperatureDataFirst()
{
    // 使用新的API调用格式：企业标识-帧类型-槽号
    string requestData = $"{companyId}-8-{selectedPotName}";
    string jsonData = $"\"{requestData}\"";
    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
    
    using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = connectionTimeout;
        
        UnityEngine.Debug.Log($"获取实时温度数据: {requestData}");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            UnityEngine.Debug.Log($"实时温度数据API响应成功");
            
            // 解析实时温度数据
            var realTimeData = ParseRealTimeTemperatureResponse(responseText);
            if (realTimeData != null && realTimeData.Count > 0)
            {
                UnityEngine.Debug.Log($"解析到实时温度数据: {realTimeData.Count} 条记录");
                currentRealTimeData = realTimeData;
            }
        }
        else
        {
            UnityEngine.Debug.LogError($"获取实时温度数据失败: {request.error}");
            ShowLoadingText(false); // 在错误时隐藏加载提示
        }
    }
    
    // 继续获取传感器数据 
    //yield return StartCoroutine(FetchTemperatureDataFromAPI()); 20250807
}
        private void ClearCharts()
        {
            // 检查游戏对象是否激活，如果未激活则直接同步清理（避免协程错误）
            if (!gameObject.activeInHierarchy)
            {
                // 同步清理图表
                foreach (var chart in chartInstances)
                {
                    if (chart != null)
                        Destroy(chart.gameObject);
                }
                chartInstances.Clear();

                // 清空容器
                if (chartsContainer != null)
                {
                    foreach (Transform child in chartsContainer)
                    {
                        Destroy(child.gameObject);
                    }
                }
                return;
            }
            
            // 对象激活时使用协程分帧销毁
            StartCoroutine(ClearChartsCoroutine());
		}
        
        /// <summary>
        /// 验证传感器配置数据是否符合预期
        /// </summary>
        private void ValidateSensorData()
        {
            if (!GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                UnityEngine.Debug.LogError("传感器配置数据未加载！");
                return;
            }
            
            var amianyinjiCount = GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs().Count;
            var bmianyinjiCount = GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs().Count;
            var amiangangchuangCount = GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs().Count;
            var bmiangangchuangCount = GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs().Count;
            var duanbuCount = GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs().Count;
            var dibuCount = GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs().Count;
            
            UnityEngine.Debug.Log($"=== 传感器配置数据验证 ===");
            UnityEngine.Debug.Log($"A面阴极: {amianyinjiCount} (预期: 56)");
            UnityEngine.Debug.Log($"B面阴极: {bmianyinjiCount} (预期: 56)");
            UnityEngine.Debug.Log($"A面钢窗: {amiangangchuangCount} (预期: 28)");
            UnityEngine.Debug.Log($"B面钢窗: {bmiangangchuangCount} (预期: 28)");
            UnityEngine.Debug.Log($"端部: {duanbuCount} (预期: 18)");
            UnityEngine.Debug.Log($"底部: {dibuCount} (预期: 28)");
            
            // 检查是否符合预期
            bool isValid = true;
            //if (amianyinjiCount != 56) { UnityEngine.Debug.LogWarning($"A面阴极数量不符合预期: {amianyinjiCount} != 56"); isValid = false; }
            //if (bmianyinjiCount != 56) { UnityEngine.Debug.LogWarning($"B面阴极数量不符合预期: {bmianyinjiCount} != 56"); isValid = false; }
            //if (amiangangchuangCount != 28) { UnityEngine.Debug.LogWarning($"A面钢窗数量不符合预期: {amiangangchuangCount} != 28"); isValid = false; }
            //if (bmiangangchuangCount != 28) { UnityEngine.Debug.LogWarning($"B面钢窗数量不符合预期: {bmiangangchuangCount} != 28"); isValid = false; }
            //if (duanbuCount != 18) { UnityEngine.Debug.LogWarning($"端部数量不符合预期: {duanbuCount} != 18"); isValid = false; }
            //if (dibuCount != 28) { UnityEngine.Debug.LogWarning($"底部数量不符合预期: {dibuCount} != 28"); isValid = false; }
            
            if (isValid)
            {
                UnityEngine.Debug.Log("✅ 传感器配置数据验证通过！");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ 传感器配置数据验证失败！请检查数据加载逻辑。");
            }
        }
        /// <summary>
        /// 优化的批量数据获取方法
        /// </summary>
        private IEnumerator FetchAllSensorDataOptimized()
        {
			// 创建并启动计时器 20250807
			Stopwatch stopwatch = Stopwatch.StartNew();
			
			// 首先获取实时温度数据
			yield return StartCoroutine(FetchHistoryTemperatureDataFirst());
			// 停止计时器 20250807
			stopwatch.Stop();
			// 输出结果 20250807
			UnityEngine.Debug.Log($"获取实时温度数据代码执行时间: {stopwatch.Elapsed.TotalSeconds} 秒");

			// 获取选定类别的传感器配置
			var sensorConfigs = GetSensorConfigsByCategory(selectedCategory);
	
			if (sensorConfigs.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"未找到类别 {selectedCategory} 的传感器配置");
                ShowLoadingText(false); // 隐藏加载提示
                isUpdating = false;
                yield break;
            }
            
            UnityEngine.Debug.Log($"找到 {sensorConfigs.Count} 个 {selectedCategory} 传感器");

			// 创建并启动计时器 20250807
			stopwatch = Stopwatch.StartNew();
			// 批量处理传感器数据
			yield return StartCoroutine(ProcessSensorDataBatch(sensorConfigs));
			// 停止计时器 20250807
			stopwatch.Stop();
			// 输出结果 20250807
			UnityEngine.Debug.Log($"批量处理传感器数据代码执行时间: {stopwatch.Elapsed.TotalSeconds} 秒");

			// 创建图表	
			CreateMultipleCharts();

			// 隐藏加载提示
			ShowLoadingText(false);
            
            isUpdating = false;
        }
        
        /// <summary>
        /// 批量处理传感器数据
        /// </summary>
        private IEnumerator ProcessSensorDataBatch(List<TemperatureSensorConfigManager.TemperatureSensorConfig> configs)
        {
            const int batchSize = 10; // 每批处理10个传感器
                                      //const int batchSize = 5; // 每批处理10个传感器 20250807

            for (int i = 0; i < configs.Count; i += batchSize)
            {
                var batch = configs.Skip(i).Take(batchSize).ToList();
                
                // 并行处理当前批次
                var tasks = new List<Coroutine>();
                foreach (var config in batch)
                {
                    tasks.Add(StartCoroutine(ProcessSingleSensorOptimized(config)));
                }
                
                // 等待当前批次完成
                foreach (var task in tasks)
                {
                    yield return task;
                }
                
                // 每批处理完后让出一帧，避免卡顿
                yield return null;
                
                UnityEngine.Debug.Log($"已处理 {Mathf.Min(i + batchSize, configs.Count)}/{configs.Count} 个传感器");
            }
        }
        
        /// <summary>
        /// 优化的单个传感器处理
        /// </summary>
        private IEnumerator ProcessSingleSensorOptimized(TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            // 检查缓存 - 实时数据不需要日期参数
            string cacheKey = $"{config.sn}_{selectedPotName}";
            if (TryGetCachedSensorData(cacheKey, out SensorTemperatureData cachedData))
            {
                allSensorData.Add(cachedData);
                yield break;
            }
            
            // 如果有实时数据，直接处理
            if (currentRealTimeData != null && currentRealTimeData.Count > 0)
            {
                ProcessRealTimeTemperatureData(currentRealTimeData, config);
                
                // 缓存处理结果
                if (allSensorData.Count > 0)
                {
                    var lastData = allSensorData[allSensorData.Count - 1];
                    CacheSensorData(cacheKey, lastData);
                }
            }
            else
            {
                // 生成测试数据
                GenerateTestDataForSensor(config);
            }
            
            yield return null;
        }
        
        private IEnumerator FetchTemperatureDataFromAPI()
        {
            // 获取选定类别的传感器配置
            var sensorConfigs = GetSensorConfigsByCategory(selectedCategory);
            
            if (sensorConfigs.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"未找到类别 {selectedCategory} 的传感器配置");
                ShowLoadingText(false); // 隐藏加载提示
                yield break;
            }
            
            UnityEngine.Debug.Log($"找到 {sensorConfigs.Count} 个 {selectedCategory} 传感器");
            
            // 为每个传感器获取温度数据
            foreach (var config in sensorConfigs)
            {
                yield return StartCoroutine(FetchSensorTemperatureData(config));
            }
            
            // 创建图表 
            CreateMultipleCharts();
            
            // 隐藏加载提示
            ShowLoadingText(false);
            
            isUpdating = false;
        }
        
private List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetSensorConfigsByCategory(string category)
        {
            var configs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
            
            // 添加调试信息，显示实际的传感器数量
            var stats = GlobalTemperatureSensorDataManager.Instance.GetDataStatistics();
            UnityEngine.Debug.Log($"全局传感器数据统计: {stats}");
            
            switch (category)
            {
                case "A面阴极":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs());
                    break;
                case "B面阴极":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs());
                    break;
                case "A面钢窗":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs());
                    break;
                case "B面钢窗":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs());
                    break;
                case "端部":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs());
                    break;
                case "底部":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs());
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"未知的传感器类别: {category}");
                    break;
            }
            
            return configs;
        }
            
        /// <summary>
/// 解析实时温度数据响应
/// </summary>
private List<RealTimeTemperatureData> ParseRealTimeTemperatureResponse(string responseText)
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
        UnityEngine.Debug.LogError($"Base64解码失败: {ex.Message}");
        return null;
    }
    
    UnityEngine.Debug.Log($"解码后数据长度: {frameData.Length} 字节");
    
    // 解析新的帧格式
    if (!ParseRealTimeFrameData(frameData, out List<RealTimeTemperatureData> realTimeData))
    {
        UnityEngine.Debug.LogError("实时帧数据解析失败");
        return null;
    }
    
    return realTimeData;
}

/// <summary>
/// 解析实时温度帧数据
/// </summary>
private bool ParseRealTimeFrameData(byte[] frameData, out List<RealTimeTemperatureData> realTimeData)
{
    realTimeData = new List<RealTimeTemperatureData>();
    
    if (frameData.Length < 14) // 最小帧长度
    {
        UnityEngine.Debug.LogError("帧数据长度不足");
        return false;
    }
    
    int offset = 0;
    
    // 验证帧头：170, 85
    if (frameData[offset] != 170 || frameData[offset + 1] != 85)
    {
        UnityEngine.Debug.LogError($"帧头验证失败: {frameData[offset]}, {frameData[offset + 1]} (期望: 170, 85)");
        return false;
    }
    offset += 2;
    
    // 读取服务电解企业标识 (2字节)
    int enterpriseId = (frameData[offset] << 8) | frameData[offset + 1];
    offset += 2;
    
    // 读取帧类型 (2字节) - 应该是8
    int frameType = (frameData[offset] << 8) | frameData[offset + 1];
    offset += 2;
    
    // 读取槽号 (2字节)
    int slotNumber = (frameData[offset] << 8) | frameData[offset + 1];
    offset += 2;
    
    // 读取有效数据长度 (4字节) - 应该是144000字节
    int dataLength = (frameData[offset] << 24) | (frameData[offset + 1] << 16) | 
                    (frameData[offset + 2] << 8) | frameData[offset + 3];
    offset += 4;
    
    UnityEngine.Debug.Log($"企业标识: {enterpriseId}, 帧类型: {frameType}, 槽号: {slotNumber}, 数据长度: {dataLength}");
    
    // 验证帧类型
    if (frameType != 8)
    {
        UnityEngine.Debug.LogError($"帧类型验证失败: {frameType} (期望: 8)");
        return false;
    }
    
    // 验证数据长度
    if (offset + dataLength + 2 > frameData.Length)
    {
        UnityEngine.Debug.LogError($"数据长度验证失败: 期望 {offset + dataLength + 2}, 实际 {frameData.Length}");
        return false;
    }
    
    // 验证帧尾
    int tailOffset = offset + dataLength;
    if (frameData[tailOffset] != 165 || frameData[tailOffset + 1] != 90)
    {
        UnityEngine.Debug.LogError($"帧尾验证失败: {frameData[tailOffset]}, {frameData[tailOffset + 1]} (期望: 165, 90)");
        return false;
    }
    
    // 解析实时温度数据
        // 每个点480字节，对应240个测温点，每2秒一个点，共300个点（10分钟）
        int pointSize = 480; // 每个时间点的数据大小
        int pointCount = dataLength / pointSize; // 应该是300个点
        int sensorsPerPoint = 240; // 每个点240个传感器
        
        UnityEngine.Debug.Log($"开始解析实时数据: 总长度={frameData.Length}, 数据长度={dataLength}, 点数={pointCount}");
        UnityEngine.Debug.Log($"时间点数量: {pointCount}, 每点传感器数: {sensorsPerPoint}");
        UnityEngine.Debug.Log($"原始帧数据前50字节: {string.Join(", ", frameData.Take(50).Select(b => b.ToString()))}");
        UnityEngine.Debug.Log($"数据部分前20字节: {string.Join(", ", frameData.Skip(offset).Take(20).Select(b => b.ToString()))}");
        
        for (int i = 0; i < pointCount; i++)
        {
            var timePointData = new RealTimeTemperatureData();
            timePointData.timestamp = i * 2; // 每2秒一个点
            
            int pointOffset = offset + i * pointSize;
            
            // 解析240个传感器的温度值
            for (int j = 0; j < sensorsPerPoint; j++)
            {
                // 假设每个温度值占2字节（可能需要根据实际格式调整）
                int tempOffset = pointOffset + j * 2;
                if (tempOffset + 1 < frameData.Length)
                {
                    // 读取温度值（大端序）
                    int tempValue = (frameData[tempOffset] << 8) | frameData[tempOffset + 1];
                    // 转换为整数温度值
                    int temperature = tempValue;
                    timePointData.temperatureValues.Add(temperature);
                    
                    // 只打印前几个数据点的详细信息
                    if (i < 3 && j < 5)
                    {
                        UnityEngine.Debug.Log($"时间点{i}, 传感器{j}: 原始值={tempValue}, 温度={temperature}");
                    }
                }
                
              
            }
            
            realTimeData.Add(timePointData);
            
            // 打印每个时间点的数据统计
            if (i < 5)
            {
                UnityEngine.Debug.Log($"时间点{i}: 获取到{timePointData.temperatureValues.Count}个温度值");
            }
        }
    
    UnityEngine.Debug.Log($"解析完成: 共{realTimeData.Count}个时间点");
    return true;
}
private IEnumerator FetchSensorTemperatureData(TemperatureSensorConfigManager.TemperatureSensorConfig config)
{
    // 构建新的API请求参数：企业标识-帧类型-槽号
    string requestParam = $"{companyId}-8-{selectedPotName}";
    string jsonData = $"\"{requestParam}\"";
    byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
    
    using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(byteArray);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = connectionTimeout;
        
        UnityEngine.Debug.Log($"请求传感器 {config.sampleindex} 的实时温度数据: {requestParam}");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            ParseSensorTemperatureResponse(responseText, config);
        }
        else
        {
            UnityEngine.Debug.LogError($"获取传感器 {config.sampleindex} 数据失败: {request.error}");
            //GenerateTestDataForSensor(config);
        }
    }
}
        
        private void ParseSensorTemperatureResponse(string responseText, TemperatureSensorConfigManager.TemperatureSensorConfig config)
{
    try
    {
        UnityEngine.Debug.Log($"直接解析实时API响应数据");
        
        // 如果有本地实时数据，直接使用
        if (currentRealTimeData != null && currentRealTimeData.Count > 0)
        {
            UnityEngine.Debug.Log($"使用本地实时数据处理传感器 {config.sampleindex}");
            ProcessRealTimeTemperatureData(currentRealTimeData, config);
            return;
        }
        
        // 如果没有本地数据，解析API响应
        var realTimeData = ParseRealTimeTemperatureResponse(responseText);
        if (realTimeData != null && realTimeData.Count > 0)
        {
            UnityEngine.Debug.Log($"直接处理解析的实时温度数据");
            ProcessRealTimeTemperatureData(realTimeData, config);
        }
        else
        {
            UnityEngine.Debug.LogWarning($"未能解析到有效的实时温度数据，为传感器 {config.sampleindex} 生成测试数据");
            //GenerateTestDataForSensor(config);
        }
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogError($"解析传感器 {config.sampleindex} 数据时出错: {ex.Message}");
        //GenerateTestDataForSensor(config);
    }
}

        /// <summary>
        /// 处理实时温度数据
        /// </summary>
        /// <summary>
        /// 处理实时温度数据
        /// </summary>
        private void ProcessRealTimeTemperatureData(List<RealTimeTemperatureData> realTimeData, TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            if (realTimeData == null || realTimeData.Count == 0)
            {
                UnityEngine.Debug.LogWarning("实时温度数据为空");
                return;
            }

            var sensorData = new SensorTemperatureData
            {
                sensorId = config.sn,
                category = selectedCategory,
                sampleIndex = config.sampleindex,
                config = config
            };

            // 计算数据的时间范围（从最早的时间点到当前时间）
            DateTime currentTime = DateTime.Now;
            DateTime startTime = currentTime.AddSeconds(-(realTimeData.Count * 2)); // 根据数据点数量计算开始时间

            UnityEngine.Debug.Log($"数据处理: 从 {startTime:HH:mm:ss} 到 {currentTime:HH:mm:ss}, 共 {realTimeData.Count} 个数据点");

            // 从实时数据中提取指定传感器的温度值
            for (int index = 0; index < realTimeData.Count; index++)
            {
                var timePoint = realTimeData[index];
                if (config.sampleindex < timePoint.temperatureValues.Count)
                {
                    int temperature = timePoint.temperatureValues[config.sampleindex];
                    sensorData.temperatureValues.Add(temperature);

                    // 生成实际时间标签（基于数据的时间顺序）
                    DateTime pointTime = startTime.AddSeconds(index * 2);
                    sensorData.timeLabels.Add(pointTime.ToString("HH:mm:ss"));
                }
            }

            if (sensorData.temperatureValues.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"未找到传感器 {config.sampleindex} 对应的温度数据");
            }
            else
            {
                allSensorData.Add(sensorData);
                UnityEngine.Debug.Log($"成功为传感器 {config.sampleindex} 加载了 {sensorData.temperatureValues.Count} 个温度数据点，时间范围: {sensorData.timeLabels.First()} 到 {sensorData.timeLabels.Last()}");
            }
        }
        /// <summary>
        /// 解析历史温度数据响应（复用HistoryTemperatureTest的逻辑）
        /// </summary>
        private List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> ParseHistoryTemperatureResponse(string responseText)
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
        UnityEngine.Debug.LogError($"Base64解码失败: {ex.Message}");
        return null;
    }
    
    UnityEngine.Debug.Log($"解码后数据长度: {frameData.Length} 字节");
    
    // 解析帧格式
    if (!ParseFrameData(frameData, out List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> historyData))
    {
        UnityEngine.Debug.LogError("帧数据解析失败");
        return null;
    }
    
    return historyData;
}

/// <summary>
/// 解析帧数据（复用HistoryTemperatureTest的逻辑）
/// </summary>
private bool ParseFrameData(byte[] frameData, out List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> historyData)
{
    historyData = new List<GlobalTemperatureSensorDataManager.HistoryTemperatureData>();
    
    if (frameData.Length < 14) // 最小帧长度
    {
        UnityEngine.Debug.LogError("帧数据长度不足");
        return false;
    }
    
    int offset = 0;
    
    // 验证帧头：170, 85
    if (frameData[offset] != 170 || frameData[offset + 1] != 85)
    {
        UnityEngine.Debug.LogError($"帧头验证失败: {frameData[offset]}, {frameData[offset + 1]} (期望: 170, 85)");
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
    
    UnityEngine.Debug.Log($"企业标识: {enterpriseId}, 帧类型: {frameType}, 槽号: {slotNumber}, 数据长度: {dataLength}");
    
    // 验证数据长度
    if (offset + dataLength + 2 > frameData.Length)
    {
        UnityEngine.Debug.LogError($"数据长度验证失败: 期望 {offset + dataLength + 2}, 实际 {frameData.Length}");
        return false;
    }
    
    // 验证帧尾
    int tailOffset = offset + dataLength;
    if (frameData[tailOffset] != 165 || frameData[tailOffset + 1] != 90)
    {
        UnityEngine.Debug.LogError($"帧尾验证失败: {frameData[tailOffset]}, {frameData[tailOffset + 1]} (期望: 165, 90)");
        return false;
    }
    
    // 解析历史温度数据
    int recordSize = 512;
    int recordCount = dataLength / recordSize;
    
    UnityEngine.Debug.Log($"历史记录数量: {recordCount}");
    
    for (int i = 0; i < recordCount; i++)
    {
        int recordOffset = offset + i * recordSize;
        
        var historyRecord = new GlobalTemperatureSensorDataManager.HistoryTemperatureData();
        historyRecord.companyIndex = companyId;
        historyRecord.slotNumber = slotNumber;
        //historyRecord.queryDate = selectedDate;
        
        // 读取日期 (4字节)
        int dateValue = (frameData[recordOffset] << 24) | (frameData[recordOffset + 1] << 16) |
                       (frameData[recordOffset + 2] << 8) | frameData[recordOffset + 3];
        
        try
        {
            historyRecord.date = DateTimeOffset.FromUnixTimeSeconds(dateValue).DateTime;
        }
        catch
        {
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

/// <summary>
/// 处理缓存的温度数据
/// </summary>
private void ProcessCachedTemperatureData(List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> historyData, TemperatureSensorConfigManager.TemperatureSensorConfig config)
{
    // 优先使用本地历史数据
    var dataToUse = currentHistoryData.Count > 0 ? currentHistoryData : historyData;
    
    if (dataToUse == null || dataToUse.Count == 0)
    {
        UnityEngine.Debug.LogWarning($"没有可用的温度数据，为传感器 {config.sampleindex} 生成测试数据");
        //GenerateTestDataForSensor(config);
        return;
    }
    
    var sensorData = new SensorTemperatureData
    {
        sensorId = config.sn,
        category = selectedCategory,
        sampleIndex = config.sampleindex,
        config = config
    };
    
    // 从历史数据中提取温度值
    // 这里需要根据传感器的sampleindex找到对应的采样点数据
    foreach (var record in dataToUse)
    {
        if (record.temperatureValues.Count > config.sampleindex)
        {
            // 将253个采样点的数据转换为720个时间点的数据
            // 可以通过插值或重复采样来实现
            ConvertSampleDataToTimeSeriesData(record.temperatureValues, sensorData, config.sampleindex);
            break;
        }
    }
    
    if (sensorData.temperatureValues.Count == 0)
    {
        UnityEngine.Debug.LogWarning($"未找到传感器 {config.sampleindex} 对应的温度数据，生成测试数据");
        //GenerateTestDataForSensor(config);
    }
    else
    {
        allSensorData.Add(sensorData);
        UnityEngine.Debug.Log($"成功为传感器 {config.sampleindex} 加载了 {sensorData.temperatureValues.Count} 个温度数据点");
    }
}

/// <summary>
        /// 将采样点数据转换为时间序列数据
        /// </summary>
        private void ConvertSampleDataToTimeSeriesData(List<int> sampleData, SensorTemperatureData sensorData, int sampleIndex)
        {
            // 如果sampleIndex超出范围，使用最后一个值
            int targetSampleIndex = Mathf.Min(sampleIndex, sampleData.Count - 1);
            int baseTemperature = sampleData[targetSampleIndex];
            
            // 生成300个时间点的数据（10分钟，每2秒一个点）
            for (int i = 0; i < 300; i++)
            {
                // 添加一些随机变化来模拟真实的温度波动
                int variation = UnityEngine.Random.Range(-2, 3);
                int temperature = baseTemperature + variation;
                
                sensorData.temperatureValues.Add(temperature);
                
                // 生成时间标签（分:秒格式）
                int minutes = (i * 2) / 60;
                int seconds = (i * 2) % 60;
                sensorData.timeLabels.Add($"{minutes:D2}:{seconds:D2}");
            }
        }
        
        /// <summary>
        /// 优化数据转换，减少内存分配
        /// </summary>
        private SensorTemperatureData ConvertSampleDataToTimeSeriesData(List<int> sampleData, TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            var data = new SensorTemperatureData
            {
                sensorId = config.sn,
                category = selectedCategory,
                sampleIndex = config.sampleindex,
                config = config,
                temperatureValues = new List<int>(sampleData.Count),
                timeLabels = new List<string>(sampleData.Count)
            };
            
            // 预分配容量，避免动态扩容
            var timeSpan = TimeSpan.FromMinutes(2); // 每2分钟一个数据点
            var startTime = DateTime.Today;
            
            for (int i = 0; i < sampleData.Count; i++)
            {
                data.temperatureValues.Add(sampleData[i]);
                data.timeLabels.Add((startTime + timeSpan * i).ToString("HH:mm"));
            }
            
            return data;
        }
        private SensorTemperatureData ParseTemperatureData(byte[] data, TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            // 实现温度数据解析逻辑
            // 这里需要根据您的API数据格式来实现
            var sensorData = new SensorTemperatureData
            {
                sensorId = config.sn,
                category = selectedCategory,
                sampleIndex = config.sampleindex,
                config = config
            };
            
            // 解析720个温度值
            // TODO: 根据实际API数据格式实现解析逻辑
            
            return sensorData;
        }
        
        private void GenerateTestDataForSensor(TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            var sensorData = new SensorTemperatureData
            {
                sensorId = config.sn,
                category = selectedCategory,
                sampleIndex = config.sampleindex,
                config = config
            };
            
            // 生成300个测试温度数据点（10分钟，每2秒一个点）
            int baseTemp = UnityEngine.Random.Range(800, 1001);
            
            for (int i = 0; i < 300; i++)
            {
                // 模拟温度变化
                float timeSeconds = i * 2f; // 每2秒一个点
                int tempVariation = Mathf.RoundToInt(Mathf.Sin(timeSeconds * Mathf.PI / 300f) * 20f); // 10分钟周期变化
                int noise = UnityEngine.Random.Range(-5, 6);
                int temperature = baseTemp + tempVariation + noise;
                
                sensorData.temperatureValues.Add(temperature);
                
                // 生成时间标签（分:秒格式）
                int minutes = (i * 2) / 60;
                int seconds = (i * 2) % 60;
                sensorData.timeLabels.Add($"{minutes:D2}:{seconds:D2}");
            }
            
            allSensorData.Add(sensorData);
            UnityEngine.Debug.Log($"为传感器 {config.sampleindex} 生成了 {sensorData.temperatureValues.Count} 个测试数据点");
        }
        
private void CreateMultipleCharts()
{
    if (chartPrefab == null || chartsContainer == null)
    {
        UnityEngine.Debug.LogError("图表预制体或容器未设置");
        return;
    }
    
    UnityEngine.Debug.Log($"开始创建 {allSensorData.Count} 个图表");
    
    // 计算布局
    int totalCharts = allSensorData.Count;
    int rows = Mathf.CeilToInt((float)totalCharts / columns);
    
    // 调整容器大小 - 让容器和图表一样宽，不要过大
    float containerWidth = chartSize.x; // 单个图表的宽度
    float containerHeight = rows * (chartSize.y + spacing.y) - spacing.y;
    
    if (chartsContainer.GetComponent<RectTransform>() != null)
    {
        chartsContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(containerWidth, containerHeight);
    }
    
    // 移除ScrollRect的水平滚动
    if (scrollRect != null)
    {
        scrollRect.horizontal = false; // 禁用水平滚动
        scrollRect.vertical = true;    // 保持垂直滚动
    }
    
    // 分批创建图表，避免一次性创建过多导致卡顿
    isRendering = true;
    StartCoroutine(CreateChartsInBatches());
}

/// <summary>
/// 协程分批创建图表
/// </summary>
private IEnumerator CreateMultipleChartsCoroutine(List<SensorTemperatureData> allSensorData)
{
    if (allSensorData == null || allSensorData.Count == 0)
    {
        UnityEngine.Debug.LogWarning("没有传感器数据可显示");
        yield break;
    }
    
    // 计算布局
    int chartCount = allSensorData.Count;
    float containerHeight = chartCount * (chartSize.y + spacing.y);
    
    // 调整容器大小
    if (chartsContainer != null)
    {
        var rectTransform = chartsContainer.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, containerHeight);
    }
    
    // 分批创建图表，每帧最多创建2个
    const int chartsPerFrame = 2;
    
    for (int i = 0; i < allSensorData.Count; i += chartsPerFrame)
    {
        int endIndex = Mathf.Min(i + chartsPerFrame, allSensorData.Count);
        
        for (int j = i; j < endIndex; j++)
        {
            CreateSingleChart(allSensorData[j], j);
        }
        
        // 等待一帧
        yield return null;
    }
    
    UnityEngine.Debug.Log($"完成创建 {chartCount} 个图表");
}

/// <summary>
/// 公共方法：创建多个图表
/// </summary>
    public void CreateMultipleCharts(List<SensorTemperatureData> allSensorData)
    {
        isRendering = true;
        StartCoroutine(CreateMultipleChartsCoroutine(allSensorData));
    }

/// <summary>
/// 分批创建图表
/// </summary>
private IEnumerator CreateChartsInBatches()
{
    const int batchSize = 2; // 每批创建2个图表，减少每帧的负载
    
    for (int i = 0; i < allSensorData.Count; i += batchSize)
    {
        int endIndex = Mathf.Min(i + batchSize, allSensorData.Count);
        
        // 创建当前批次的图表
        for (int j = i; j < endIndex; j++)
        {
            CreateSingleChartOptimized(allSensorData[j], j);
        }
        
        // 每批创建完后让出一帧
        yield return null;
        
        UnityEngine.Debug.Log($"已创建 {endIndex}/{allSensorData.Count} 个图表");
    }
    
    UnityEngine.Debug.Log($"成功创建了 {chartInstances.Count} 个图表");
    
    // 确保隐藏加载提示
    ShowLoadingText(false);
    isRendering = false;
}
        
        private void CreateSingleChart(SensorTemperatureData sensorData, int index)
{
    // 修改为单列布局
    float x = 0; // 所有图表都在同一列
    float y = -index * (chartSize.y + spacing.y); // 垂直排列
    
    // 实例化图表
    GameObject chartObj = Instantiate(chartPrefab, chartsContainer);
    chartObj.name = $"Chart_Sensor_{sensorData.sampleIndex}";
    
    // 设置位置和大小
    RectTransform rectTransform = chartObj.GetComponent<RectTransform>();
    rectTransform.anchoredPosition = new Vector2(x, y);
    rectTransform.sizeDelta = chartSize;
    
    // 获取LineChart组件
    LineChart lineChart = chartObj.GetComponent<LineChart>();
    if (lineChart != null)
    {
        ConfigureSingleChart(lineChart, sensorData);
        chartInstances.Add(lineChart);
    }
    else
    {
        UnityEngine.Debug.LogError($"图表预制体缺少LineChart组件");
    }
}

/// <summary>
/// 优化的单个图表创建方法
/// </summary>
private void CreateSingleChartOptimized(SensorTemperatureData sensorData, int index)
{
    // 修改为单列布局
    float x = 0; // 所有图表都在同一列
    float y = -index * (chartSize.y + spacing.y); // 垂直排列
    
    // 实例化图表
    GameObject chartObj = Instantiate(chartPrefab, chartsContainer);
    chartObj.name = $"Chart_Sensor_{sensorData.sampleIndex}";
    
    // 设置位置和大小
    RectTransform rectTransform = chartObj.GetComponent<RectTransform>();
    rectTransform.anchoredPosition = new Vector2(x, y);
    rectTransform.sizeDelta = chartSize;
    
    // 获取LineChart组件
    LineChart lineChart = chartObj.GetComponent<LineChart>();
    if (lineChart != null)
    {
        ConfigureSingleChartOptimized(lineChart, sensorData);
        chartInstances.Add(lineChart);
    }
    else
    {
        UnityEngine.Debug.LogError($"图表预制体缺少LineChart组件");
    }
}

        private void ConfigureSingleChart(LineChart chart, SensorTemperatureData sensorData)
        {
            // 清空现有数据
            chart.ClearData();

            // 完全关闭动画
            chart.AnimationEnable(false);

            // 设置图表标题 - 白色文字，放在左上角
            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            string sensorDisplayName = GetSensorDisplayName(sensorData.config);
            title.text = sensorDisplayName;
            title.labelStyle.textStyle.color = Color.white;
            title.location.align = Location.Align.TopLeft;
            title.location.left = 0.03f;
            title.location.top = 0.03f;

            // ===== 配置 X 轴：从 0:00 到当前时间 =====
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.axisLabel.interval = 0;
            xAxis.axisLabel.textStyle.fontSize = 14;
            xAxis.axisLabel.textStyle.color = Color.white; // 横坐标文字白色
            xAxis.axisLine.show = true;
            xAxis.axisTick.show = true;
            xAxis.boundaryGap = true;

            // 兼容所有版本的XCharts：安全清空X轴标签
            try
            {
                var xAxis0 = chart.GetChartComponent<XAxis>(0);
                if (xAxis0 != null && xAxis0.data != null)
                {
                    xAxis0.data.Clear();
                }
            }
            catch { }

            DateTime now = DateTime.Now;
            DateTime startTime = now.Date; // 今天0点
            TimeSpan timeSinceMidnight = now - startTime;

            // 每2分钟一个点，24小时共720点
            int totalMinutes = (int)timeSinceMidnight.TotalMinutes;
            int pointCount = Mathf.Clamp(totalMinutes / 2, 1, 720);

            for (int i = 0; i < pointCount; i++)
            {
                DateTime pointTime = startTime.AddMinutes(i * 2);
                chart.AddXAxisData(pointTime.ToString("HH:mm"));
            }

            xAxis.SetAllDirty();
            chart.RefreshChart();

            // ===== 配置 Y 轴 =====
            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.min = 200;
            yAxis.max = 320;
            yAxis.position = Axis.AxisPosition.Left;
            yAxis.axisLabel.show = true;
            yAxis.axisLabel.textStyle.color = Color.white; // 纵坐标文字白色
            yAxis.splitLine.show = false; // 去掉横线
            yAxis.SetAllDirty();

            // 如果你需要副轴可以保留下面（否则可注释）
            var yAxes = chart.GetChartComponents<YAxis>();
            if (yAxes == null || yAxes.Count < 2)
            {
                chart.AddChartComponent<YAxis>();
            }
            var yAxis1 = chart.GetChartComponent<YAxis>(1);
            if (yAxis1 != null)
            {
                yAxis1.type = Axis.AxisType.Value;
                yAxis1.minMaxType = Axis.AxisMinMaxType.Custom;
                yAxis1.min = 200;
                yAxis1.max = 320;
                yAxis1.position = Axis.AxisPosition.Right;
                yAxis1.axisLabel.show = true;
                yAxis1.axisLabel.textStyle.color = Color.white; // 右侧纵坐标文字白色
                yAxis1.splitLine.show = false; // 去掉横线
                yAxis1.SetAllDirty();
            }

            // ===== 设置背景与网格边框样式（与MultiSensorTemperatureChartManager一致）=====
            chart.theme.enableCustomTheme = true;
            chart.theme.transparentBackground = false;
            chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255); // #373B47 外部背景
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.backgroundColor = new Color32(40, 47, 57, 255); // #282F39 表格内部背景
            grid.showBorder = true;
            grid.borderWidth = 2f;
            grid.borderColor = new Color32(255, 255, 255, 255); // 白色边框

            // 刷新图表以应用轴配置
            chart.RefreshChart();

            // ===== 清除现有系列并重新添加（紫色折线，无渐变填充） =====
            chart.RemoveAllSerie();
            var serie = chart.AddSerie<Line>("温度数据");
            serie.AnimationEnable(false);

            Color32 purple = new Color32(160, 96, 255, 255); // #A060FF 紫色折线
            serie.lineStyle.color = purple;
            serie.itemStyle.color = purple;
            serie.lineStyle.width = 2f;
            // 全局隐藏符号，改为仅在刻度点单独开启
            serie.symbol.show = false;
            serie.symbol.size = 4f;
            // 关闭大数据模式，确保绘制标签
            serie.large = false;
            serie.animation.enable = false;

            // 关闭渐变填充区域
            var area = serie.EnsureComponent<AreaStyle>();
            area.show = false;

            // ===== 添加数据点，仅在有X轴标签的位置显示白色数值 =====
            int dataCount = Mathf.Min(sensorData.temperatureValues.Count, pointCount);
            for (int i = 0; i < dataCount; i++)
            {
                var serieData = chart.AddData(0, sensorData.temperatureValues[i]);
                bool show = ShouldShowLabelAtIndex(chart, i);
                var label = serieData.EnsureComponent<LabelStyle>();
                label.show = show;
                if (show)
                {
                    label.position = LabelStyle.Position.Top;
                    label.formatter = "{c}";
                    label.textStyle.autoColor = false;
                    label.textStyle.color = Color.white; // 数值标签白色
                    label.textStyle.fontSize = 16;
                    label.textStyle.fontStyle = FontStyle.Bold;
                    label.offset = new Vector3(0, 20, 0);
                    // 仅在刻度点显示一个菱形符号点
                    var symbol = serieData.EnsureComponent<SerieSymbol>();
                    symbol.show = true;
                    symbol.type = SymbolType.Diamond;
                    symbol.size = 5;
                }
            }

            // 图例与 tooltip - 图例关闭（去掉"温度数据"那一行）
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = false;
            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;

            chart.AnimationEnable(false);
            chart.RefreshChart();
            chart.SetAllDirty();
        }

        private float lastXAxisUpdateTime = 0f;

        // 每60秒刷新一次X轴，动态延长到当前时间
        /// <summary>
        /// 优化的图表配置方法
        /// </summary>
        private void ConfigureSingleChartOptimized(LineChart chart, SensorTemperatureData sensorData)
        {
            // 清空现有数据
            chart.ClearData();
            chart.AnimationEnable(false);

            // 标题 - 白色文字，放在左上角
            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            title.text = GetSensorDisplayName(sensorData.config);
            title.labelStyle.textStyle.color = Color.white;
            title.location.align = Location.Align.TopLeft;
            title.location.left = 0.03f;
            title.location.top = 0.03f;

            // ===== 设置背景与网格边框样式（与MultiSensorTemperatureChartManager一致）=====
            chart.theme.enableCustomTheme = true;
            chart.theme.transparentBackground = false;
            chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255); // #373B47 外部背景
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.backgroundColor = new Color32(40, 47, 57, 255); // #282F39 表格内部背景
            grid.showBorder = true;
            grid.borderWidth = 2f;
            grid.borderColor = new Color32(255, 255, 255, 255); // 白色边框

            // 配置 X / Y 轴 + 数据 + 组件
            ConfigureXAxisOptimized(chart);
            ConfigureYAxisOptimized(chart);
            AddDataSeriesOptimized(chart, sensorData);
            ConfigureChartComponents(chart);

            chart.AnimationEnable(false);
            chart.RefreshChart();
            chart.SetAllDirty();
        }

        /// <summary>
        /// 优化的X轴配置
        /// </summary>
        /// <summary>
        /// 优化的X轴配置 - 使用实际数据的时间范围
        /// </summary>
        private void ConfigureXAxisOptimized(LineChart chart)
        {
            // 获取或创建X轴
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.axisLine.show = true;
            xAxis.axisTick.show = true;
            xAxis.boundaryGap = true;

            // 字体样式（白色 + 大字号）
            xAxis.axisLabel.interval = 0;
            xAxis.axisLabel.textStyle.fontSize = 14;
            xAxis.axisLabel.textStyle.color = Color.white;
            xAxis.axisLabel.textStyle.fontStyle = FontStyle.Normal;

            // 清空旧标签
            var xAxis0 = chart.GetChartComponent<XAxis>(0);
            if (xAxis0 != null && xAxis0.data != null)
                xAxis0.data.Clear();

            // === 生成最近10分钟（300个点）时间轴：每30个点一个非空标签，其余为空 ===
            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddMinutes(-10);
            for (int i = 0; i < 300; i++)
            {
                DateTime t = startTime.AddSeconds(i * 2);
                if (i % 30 == 0 || i == 299)
                    chart.AddXAxisData(t.ToString("HH:mm"));
                else
                    chart.AddXAxisData("");
            }

            xAxis.SetAllDirty();
            chart.RefreshChart();

            UnityEngine.Debug.Log("X轴已更新：最近10分钟，每分钟一个刻度（含空标签占位）");
        }



        /// <summary>
        /// 创建时间标签数组
        /// </summary>
        private string[] CreateTimeLabels()
        {
            string[] timeLabels = new string[300];
            
            // 获取当前时间
            DateTime currentTime = DateTime.Now;
            // 计算10分钟前的时间作为起始时间
            DateTime startTime = currentTime.AddMinutes(-10);
            
            // 为每个数据点生成实际时间标签
            for (int i = 0; i < timeLabels.Length; i++)
            {
                // 每个数据点间隔2秒
                DateTime pointTime = startTime.AddSeconds(i * 2);
                timeLabels[i] = pointTime.ToString("HH:mm:ss");
            }
            
            return timeLabels;
        }

        /// <summary>
        /// 优化的Y轴配置
        /// </summary>
        private void ConfigureYAxisOptimized(LineChart chart)
        {
            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.min = 200;
            yAxis.max = 320;
            yAxis.position = Axis.AxisPosition.Left;
            yAxis.axisLabel.show = true;
            yAxis.axisLabel.textStyle.color = Color.white; // 纵坐标文字白色
            yAxis.splitLine.show = false; // 去掉横线
            yAxis.SetAllDirty();
            
            // 添加右侧Y轴
            var yAxes = chart.GetChartComponents<YAxis>();
            if (yAxes == null || yAxes.Count < 2)
            {
                chart.AddChartComponent<YAxis>();
            }
            var yAxis1 = chart.GetChartComponent<YAxis>(1);
            if (yAxis1 != null)
            {
                yAxis1.type = Axis.AxisType.Value;
                yAxis1.minMaxType = Axis.AxisMinMaxType.Custom;
                yAxis1.min = 200;
                yAxis1.max = 320;
                yAxis1.position = Axis.AxisPosition.Right;
                yAxis1.axisLabel.show = true;
                yAxis1.axisLabel.textStyle.color = Color.white; // 右侧纵坐标文字白色
                yAxis1.splitLine.show = false; // 去掉横线
                yAxis1.SetAllDirty();
            }
        }

        // 仅在X轴有非空刻度标签的位置显示数值标签
        private bool ShouldShowLabelAtIndex(LineChart chart, int xIndex)
        {
            try
            {
                var xAxis0 = chart.GetChartComponent<XAxis>(0);
                if (xAxis0 != null && xAxis0.data != null && xIndex >= 0 && xIndex < xAxis0.data.Count)
                {
                    string label = xAxis0.data[xIndex];
                    return !string.IsNullOrEmpty(label);
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 优化的数据系列添加
        /// </summary>
        /// <summary>
        /// 优化的数据系列添加
        /// </summary>
        private void AddDataSeriesOptimized(LineChart chart, SensorTemperatureData sensorData)
{
    chart.RemoveAllSerie();

    // 创建折线系列 - 紫色折线
    var serie = chart.AddSerie<Line>("温度数据");
    Color32 purple = new Color32(160, 96, 255, 255); // #A060FF 紫色
    serie.lineStyle.color = purple;
    serie.itemStyle.color = purple;
    serie.lineStyle.width = 2f;
    // 全局隐藏符号，仅在刻度点单独显示
    serie.symbol.show = false;
    serie.symbol.size = 5f;
    // 关闭大数据模式，确保标签与符号正常绘制
    serie.large = false;
    serie.animation.enable = false;

    // 关闭渐变阴影区域
    var area = serie.EnsureComponent<AreaStyle>();
    area.show = false;

    // 系列标签样式（全局默认）
    var label = serie.EnsureComponent<LabelStyle>();
    // 关闭整条线的全局标签，避免与点标签重复
    label.show = false;

    // === 仅取最近10分钟数据 ===
    int totalCount = sensorData.temperatureValues.Count;
    int maxPoints = 300; // 10分钟，每2秒一个点
    int startIndex = Mathf.Max(0, totalCount - maxPoints);

    // 智能间隔：防止标签过密
    int showInterval = 1;
    if (totalCount > 100 && totalCount <= 300)
        showInterval = 2;
    else if (totalCount > 300 && totalCount <= 600)
        showInterval = 4;
    else if (totalCount > 600)
        showInterval = 8;

    // === 添加温度数据 ===
    for (int i = startIndex; i < totalCount; i++)
    {
        var yValue = sensorData.temperatureValues[i];
        var serieData = chart.AddData(0, yValue);

        // 仅在X轴有非空标签的位置显示白色数值
        int xIndex = i - startIndex;
        var pointLabel = serieData.EnsureComponent<LabelStyle>();
        bool show = ShouldShowLabelAtIndex(chart, xIndex);
        pointLabel.show = show;
        if (show)
        {
            pointLabel.position = LabelStyle.Position.Top;
            pointLabel.formatter = "{c}";
            pointLabel.textStyle.autoColor = false;
            pointLabel.textStyle.color = Color.white; // 白色数值标签
            pointLabel.textStyle.fontSize = 16;
            pointLabel.textStyle.fontStyle = FontStyle.Bold;
            pointLabel.offset = new Vector3(0, 20, 0);

            // 仅在刻度点显示一个菱形符号点
            var symbol = serieData.EnsureComponent<SerieSymbol>();
            symbol.show = true;
            symbol.type = SymbolType.Diamond;
            symbol.size = 5;
        }
    }

    UnityEngine.Debug.Log($"绘制温度曲线：显示最近10分钟，共 {totalCount - startIndex} 个点，标签间隔 {showInterval}");
    chart.RefreshChart();
}

        /// <summary>
        /// 准备数据点数组
        /// </summary>
        private int[] PrepareDataPoints(SensorTemperatureData sensorData)
        {
            int[] dataPoints = new int[300];
            
            for (int i = 0; i < 300; i++)
            {
                if (i < sensorData.temperatureValues.Count)
                {
                    dataPoints[i] = sensorData.temperatureValues[i];
                }
                else
                {
                    dataPoints[i] = sensorData.temperatureValues.Count > 0 ? 
                        sensorData.temperatureValues[sensorData.temperatureValues.Count - 1] : 0;
                }
            }
            
            return dataPoints;
        }
        
        /// <summary>
        /// 添加数据点高亮
        /// </summary>
        private void AddDataPointHighlight(SerieData serieData)
        {
            // 添加标签
            var labelStyle = serieData.EnsureComponent<LabelStyle>();
            labelStyle.show = true;
            labelStyle.position = LabelStyle.Position.Top;
            labelStyle.offset = new Vector3(0, 20, 0);
            labelStyle.formatter = "{c}";
            labelStyle.numericFormatter = "F1";
            labelStyle.textStyle.color = Color.red;
            labelStyle.textStyle.fontStyle = FontStyle.Bold;
            
            // 添加符号
            var symbol = serieData.EnsureComponent<SerieSymbol>();
            symbol.show = true;
            symbol.type = SymbolType.Diamond;
            symbol.size = 5;
        }

        /// <summary>
        /// 配置图表组件
        /// </summary>
        private void ConfigureChartComponents(LineChart chart)
        {
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = false;

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;

            chart.RefreshChart();
        }


        private Color GetCategoryColor(string category)
        {
            var categoryColors = new Dictionary<string, Color>
            {
                { "A面阴极", Color.red },
                { "B面阴极", Color.blue },
                { "A面钢窗", Color.green },
                { "B面钢窗", Color.yellow },
                { "端部", Color.magenta },
                { "底部", Color.cyan }
            };
            
            return categoryColors.ContainsKey(category) ? categoryColors[category] : Color.gray;
        }
        
        // 公共方法
        public void RefreshData()
        {
            LoadTemperatureData();
        }
        
        // 添加获取传感器显示名称的方法
        private string GetSensorDisplayName(TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            string categoryName = GetCategoryName(config);
            
            switch (categoryName)
            {
                case "A面阴极":
                    return $"A面阴极A{config.nodeindex}-{config.directionindex}";
                    
                case "B面阴极":
                    return $"B面阴极B{config.nodeindex}-{config.directionindex}";
                    
                case "A面钢窗":
                    return $"A面钢窗WA{config.nodeindex}";
                    
                case "B面钢窗":
                    return $"B面钢窗WB{config.nodeindex}";
                    
                case "端部":
                    if (config.posindex == 2)
                        return $"出铝端T{config.nodeindex}";
                    else if (config.posindex == 3)
                        return $"烟道端G{config.nodeindex}";
                    else
                        return $"端部{config.nodeindex}";
                        
                case "底部":
                    return $"F{config.nodeindex}";
                        
                default:
                    return $"{categoryName}{config.nodeindex}";
            }
        }

        private string GetCategoryName(TemperatureSensorConfigManager.TemperatureSensorConfig config)
{
    // 根据directiontype和posindex确定类别
    if (config.directiontype == 1) // A面
    {
        if (config.posindex == 4) return "A面阴极";
        if (config.posindex == 1) return "A面钢窗";
    }
    else if (config.directiontype == 2) // B面
    {
        if (config.posindex == 4) return "B面阴极";
        if (config.posindex == 1) return "B面钢窗";
    }
    else if (config.directiontype == 0)
    {
        if (config.posindex == 2 || config.posindex == 3) return "端部";
    }
    // 修正底部判断条件：使用directionindex而不是directiontype
    if (config.directionindex == 0 && config.posindex == 5)
    {
        return "底部";
    }
    
    return "未知类型";
}
        // 添加位置文本获取方法
        private string GetPositionText(ushort position)
        {
            return position switch
            {
                1 => "钢窗",
                2 => "出铝端",
                3 => "烟道端",
                4 => "阴极",
                5 => "底部",
                _ => "未知位置"
            };
        }
        // 添加方向文本获取方法
        private string GetDirectionText(ushort directiontype, ushort directionindex)
        {
            string direction = directiontype switch
            {
                1 => "A面",
                2 => "B面",
                _ => ""
            };
            
            return direction;
        }

        
        public int GetChartCount()
        {
            return chartInstances.Count;
        }
        
        public List<SensorTemperatureData> GetSensorData()
        {
            return new List<SensorTemperatureData>(allSensorData);
        }
        
        /// <summary>
        /// 启动自动更新
        /// </summary>
        public void StartAutoUpdate()
        {
            if (autoUpdateCoroutine != null)
            {
                StopCoroutine(autoUpdateCoroutine);
            }
            
            autoUpdateCoroutine = StartCoroutine(AutoUpdateCoroutine());
            UnityEngine.Debug.Log($"启动自动更新，间隔: {updateInterval}秒");
        }
        
        /// <summary>
        /// 停止自动更新
        /// </summary>
        public void StopAutoUpdate()
        {
            if (autoUpdateCoroutine != null)
            {
                StopCoroutine(autoUpdateCoroutine);
                autoUpdateCoroutine = null;
            }
            UnityEngine.Debug.Log("停止自动更新");
        }
        
        /// <summary>
        /// 自动更新协程
        /// </summary>
        private IEnumerator AutoUpdateCoroutine()
        {
            while (enableAutoUpdate)
            {
                yield return new WaitForSeconds(updateInterval);
                
                if (!isUpdating && !isRendering && gameObject.activeInHierarchy)
                {
                    UnityEngine.Debug.Log($"[{System.DateTime.Now:HH:mm:ss}] 执行自动更新...");
                    RefreshTemperatureData();
                }
                else
                {
                    UnityEngine.Debug.Log($"[{System.DateTime.Now:HH:mm:ss}] 跳过更新 - isUpdating: {isUpdating}, isRendering: {isRendering}, activeInHierarchy: {gameObject.activeInHierarchy}");
                }
            }
        }
        
        /// <summary>
        /// 刷新温度数据（用于自动更新）
        /// </summary>
        private void RefreshTemperatureData()
        {
            if (isUpdating || isRendering)
            {
                UnityEngine.Debug.Log("正在更新中，跳过本次更新");
                return;
            }
            
            if (!GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                UnityEngine.Debug.Log("传感器配置数据未加载，无法刷新数据");
                return;
            }
            
            isUpdating = true;
            
            // 清空现有数据
            allSensorData.Clear();
            currentRealTimeData = null;
            
            // 重新获取数据
            StartCoroutine(RefreshDataCoroutine());
        }
        
        /// <summary>
        /// 刷新数据协程
        /// </summary>
        private IEnumerator RefreshDataCoroutine()
        {
            try
            {
                // 获取实时温度数据
                yield return StartCoroutine(FetchRealTimeTemperatureData());
                
                // 更新图表 - 默认只更新数据，不重建
                if (currentRealTimeData != null && currentRealTimeData.Count > 0)
                {
                    yield return StartCoroutine(UpdateChartsWithNewData());
                }
                else
                {
                    UnityEngine.Debug.LogWarning("未获取到有效的实时数据");
                }
            }
            finally
            {
                isUpdating = false;
            }
        }
        
        /// <summary>
        /// 获取实时温度数据
        /// </summary>
        private IEnumerator FetchRealTimeTemperatureData()
        {
            // 使用新的API调用格式：企业标识-帧类型-槽号
            string requestData = $"{companyId}-8-{selectedPotName}";
            string jsonData = $"\"{requestData}\"";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;
                
                UnityEngine.Debug.Log($"刷新实时温度数据: {requestData}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    UnityEngine.Debug.Log($"实时温度数据刷新成功");
                    
                    // 解析实时温度数据
                    var realTimeData = ParseRealTimeTemperatureResponse(responseText);
                    if (realTimeData != null && realTimeData.Count > 0)
                    {
                        currentRealTimeData = realTimeData;
                        UnityEngine.Debug.Log($"刷新获取到实时温度数据: {realTimeData.Count} 条记录");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"刷新实时温度数据失败: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// 使用新数据更新图表
        /// </summary>
        private IEnumerator UpdateChartsWithNewData()
        {
            var configs = GetSensorConfigsByCategory(selectedCategory);
            
            foreach (var config in configs)
            {
                // 处理每个传感器的数据
                if (currentRealTimeData != null && currentRealTimeData.Count > 0)
                {
                    ProcessRealTimeTemperatureData(currentRealTimeData, config);
                }
                
                // 每处理几个传感器后让出一帧，避免卡顿
                yield return null;
            }
            
            // 更新图表显示
            UpdateChartsDisplay(false); // false 表示不强制重建
        }
        
        /// <summary>
        /// 更新图表显示
        /// </summary>
        private void UpdateChartsDisplay(bool forceRecreate = false)
        {
            if (forceRecreate || chartInstances.Count == 0 || chartInstances.Count != allSensorData.Count)
            {
                // 只有在强制重建或图表数量不匹配时才重新创建
                ClearCharts();
                CreateMultipleCharts();
                UnityEngine.Debug.Log($"图表重新创建完成，共 {allSensorData.Count} 个传感器数据");
            }
            else
            {
                // 仅更新现有图表的数据
                UpdateExistingChartsData();
                UnityEngine.Debug.Log($"图表数据更新完成，共 {allSensorData.Count} 个传感器数据");
            }
        }
        
        /// <summary>
        /// 设置自动更新状态
        /// </summary>
        public void SetAutoUpdate(bool enabled)
        {
            enableAutoUpdate = enabled;
            
            if (enabled)
            {
                StartAutoUpdate();
            }
            else
            {
                StopAutoUpdate();
            }
        }
        
        /// <summary>
        /// 设置更新间隔
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(1f, interval); // 最小1秒间隔
            
            if (enableAutoUpdate)
            {
                StartAutoUpdate(); // 重启自动更新以应用新间隔
            }
        }
        
        /// <summary>
        /// 仅更新现有图表的数据，不重新创建图表
        /// </summary>
        private void UpdateExistingChartsData()
        {
            for (int i = 0; i < chartInstances.Count && i < allSensorData.Count; i++)
            {
                var chart = chartInstances[i];
                var sensorData = allSensorData[i];
                
                if (chart != null)
                {
                    UpdateSingleChartData(chart, sensorData);
                }
            }
        }
        
        /// <summary>
        /// 更新单个图表的数据，保持原有样式
        /// </summary>
        private void UpdateSingleChartData(LineChart chart, SensorTemperatureData sensorData)
        {
            try
            {
                // 确保图表存在且有系列
                if (chart.series.Count == 0)
                {
                    UnityEngine.Debug.LogWarning("图表没有系列，跳过更新");
                    return;
                }
                
                var serie = chart.series[0];
                
                // 保存当前样式配置
                var originalLineColor = serie.lineStyle.color;
                var originalItemColor = serie.itemStyle.color;
                var originalLineWidth = serie.lineStyle.width;
                var originalSymbolShow = serie.symbol.show;
                var originalLarge = serie.large;
                
                // 清除数据但保持系列结构
                serie.data.Clear();
                
                // 重新添加数据，保持原有样式
                for (int i = 0; i < 300; i++)
                {
                    int temperature;
                    if (i < sensorData.temperatureValues.Count)
                    {
                        temperature = sensorData.temperatureValues[i];
                    }
                    else
                    {
                        // 如果数据不足300个，用最后一个值填充
                        temperature = sensorData.temperatureValues.Count > 0 ? 
                            sensorData.temperatureValues[sensorData.temperatureValues.Count - 1] : 0;
                    }
                    
                    var serieData = chart.AddData(0, temperature);
                    
                    // 每隔50个数据点显示红色数值标签和菱形符号
                    if (i % 50 == 0 || i == 299)
                    {
                        // 为数据点添加标签组件
                        var labelStyle = serieData.EnsureComponent<LabelStyle>();
                        labelStyle.show = true;
                        labelStyle.position = LabelStyle.Position.Top;
                        labelStyle.offset = new Vector3(0, 20, 0);
                        labelStyle.formatter = "{c}";
                        labelStyle.numericFormatter = "F1";
                        labelStyle.textStyle.color = Color.red;
                        labelStyle.textStyle.fontStyle = FontStyle.Bold;
                        
                        // 为数据点添加符号组件
                        var symbol = serieData.EnsureComponent<SerieSymbol>();
                        symbol.show = true;
                        symbol.type = SymbolType.Diamond;
                        symbol.size = 5;
                    }
                }
                
                // 恢复原有样式配置
                serie.lineStyle.color = originalLineColor;
                serie.itemStyle.color = originalItemColor;
                serie.lineStyle.width = originalLineWidth;
                serie.symbol.show = originalSymbolShow;
                serie.large = originalLarge;
                
                // 确保动画关闭
                serie.AnimationEnable(false);
                chart.AnimationEnable(false);
                
                // 更新图表标题
                var title = chart.EnsureChartComponent<Title>();
                if (title != null)
                {
                    string sensorDisplayName = GetSensorDisplayName(sensorData.config);
                    title.text = sensorDisplayName;
                }
                
                // 强制刷新图表
                chart.RefreshChart();
                chart.SetAllDirty();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"更新图表数据时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据传感器索引获取系列颜色
        /// </summary>
        private Color GetSerieColor(int sampleIndex)
        {
            // 根据传感器索引生成不同的颜色
            float hue = (sampleIndex * 0.618033988749895f) % 1.0f; // 黄金角度
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
        
        // 数据缓存相关方法
        private Dictionary<string, SensorTemperatureData> sensorDataCache = new Dictionary<string, SensorTemperatureData>();
        private const int MAX_CACHE_SIZE = 1000;
        
        /// <summary>
        /// 尝试从缓存获取传感器数据
        /// </summary>
        private bool TryGetCachedSensorData(string cacheKey, out SensorTemperatureData cachedData)
        {
            return sensorDataCache.TryGetValue(cacheKey, out cachedData);
        }
        
        /// <summary>
        /// 缓存传感器数据
        /// </summary>
        private void CacheSensorData(string cacheKey, SensorTemperatureData data)
        {
            if (sensorDataCache.Count >= MAX_CACHE_SIZE)
            {
                // 清理最旧的缓存项
                var firstKey = sensorDataCache.Keys.First();
                sensorDataCache.Remove(firstKey);
            }
            
            sensorDataCache[cacheKey] = data;
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        private void ClearCache()
        {
            sensorDataCache.Clear();
        }
        
        void OnDestroy()
        {
            // 停止自动更新
            StopAutoUpdate();
            
            // 清理缓存
            ClearCache();
            
            // 直接清理图表资源，不使用协程（因为对象可能已经非激活）
            foreach (var chart in chartInstances)
            {
                if (chart != null)
                    Destroy(chart.gameObject);
            }
            chartInstances.Clear();
            
            if (chartsContainer != null)
            {
                foreach (Transform child in chartsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        void OnDisable()
        {
            StopAutoUpdate();
        }
        
        /// <summary>
        /// 强制重建所有图表（在切换类别或初始化时使用）
        /// </summary>
        public void ForceRecreateCharts()
        {
            UpdateChartsDisplay(true);
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
                    loadingText.text = "正在读取实时数据...";
                }
            }
        }

        // 分帧销毁图表 20250808
        private IEnumerator ClearChartsCoroutine()
        {
            isRendering = true;
            int destroyPerFrame = 5; // 每帧销毁5个图表
            int index = 0;
            while (index < chartInstances.Count)
            {
                Destroy(chartInstances[index].gameObject);
                index++;
                if (index % destroyPerFrame == 0) yield return null; // 每销毁5个歇一帧
            }
            chartInstances.Clear();

			// 清空容器
            if (chartsContainer != null)
            {
                foreach (Transform child in chartsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            isRendering = false;
        }

        // 加载配置数据，加载实时温度数据
        //private IEnumerator LoadConfigAndInitCoroutine()
        //{ 
        //    //yield return StartCoroutine(AutoTemperatureSensorDataLoader.TemperatureSensorConfig.GetTemperatureSensorConfig(companyId));
        //}
	}
}
