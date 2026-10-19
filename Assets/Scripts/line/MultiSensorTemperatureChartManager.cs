using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using XCharts.Runtime;
using System.Text.RegularExpressions;

namespace XCharts.Example
{
    [DisallowMultipleComponent]
    public class MultiSensorTemperatureChartManager : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int connectionTimeout = 30;
        
        [Header("筛选设置")]
        public TextMeshProUGUI potNameText;
        public TextMeshProUGUI dateText;
        public Button applyFilterButton;
        
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
        
        [Header("UI提示设置")]
        public TextMeshProUGUI loadingText; // 加载提示文本

        // 或者使用按钮数组的方式
        // public Button[] categoryButtons; // 按顺序：A面阴极, B面阴极, A面钢窗, B面钢窗, 端口, 底部

        // 筛选条件 - 移除硬编码的默认值，改为从UI读取
        //20251103,cxt
        //private string selectedPotName => potNameText != null ? potNameText.text : "槽号选择";
        private string selectedPotName;
        
        private string selectedDate => GetValidatedDate();

        // 校验并获取有效日期（期望格式：yyyyMMdd），否则回退到当天
        private string GetValidatedDate()
        {
            try
            {
                var txt = dateText != null ? (dateText.text ?? string.Empty).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(txt) && Regex.IsMatch(txt, "^\\d{8}$"))
                {
                    return txt;
                }
            }
            catch { }
            var fallback = DateTime.Now.ToString("yyyyMMdd");
            Debug.LogWarning($"日期无效或未设置，使用当天作为查询日期: {fallback}");
            return fallback;
        }
        
        private string selectedCategory = "A面阴极";

		//程序是不是第一次运行 20250812
		private bool isFirstRun = true;
		float nextExecutionTime = 0; // 下次执行的时间点

		// 数据缓存
		private List<SensorTemperatureData> allSensorData = new List<SensorTemperatureData>();
        private List<LineChart> chartInstances = new List<LineChart>();

        // 程序第一次启动标志
        private bool isFirstStart = true;
        
        // 传感器温度数据结构
        [System.Serializable]
        public class SensorTemperatureData
        {
            public int sensorId;
            public string category;
            public int sampleIndex; // 对应API返回的采样编号
            public List<int> temperatureValues; // 720个时间点的温度值
            public List<string> timeLabels; // 对应的时间标签
            public TemperatureSensorConfigManager.TemperatureSensorConfig config; // 添加传感器配置字段
            
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
                Debug.Log("传感器配置数据已加载，可以开始获取温度数据");
                // 添加初始数据加载
                // 添加数据验证
                ValidateSensorData();
                //LoadTemperatureData();
            }
            else
            {
                Debug.Log("传感器配置数据未加载，请先运行AutoTemperatureSensorDataLoader");
            }

			isFirstStart = true;

            companyId = GlobalUserData.AlSerialPantId;
            // 统一解析槽号，移除硬编码
            selectedPotName = ResolveSelectedSlotNumber();
            Debug.Log($"初始化槽号为: {selectedPotName}");
        }
		

        void Update()
        {			
            if (Time.time >= nextExecutionTime)
			{
				nextExecutionTime = Time.time + 2; // 更新下次执行的时间点为当前时间加5秒 20250812
				UnityEngine.Debug.Log($"历史曲线更新 isFirstRun = {isFirstRun}, GlobalTemperatureSensorDataManager.Instance.isDataLoaded = {GlobalTemperatureSensorDataManager.Instance.isDataLoaded}");
                if (isFirstStart)
                {                    
                    // 第一次启动，且传感器配置数据已加载，则加载数据}
                    if (GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
                    {
                        if (chartsContainer.childCount == 0)
                        {
                            Debug.Log("测试测试，chartsContainer为空");
                            LoadTemperatureData();
                        }
						else
						{
							isFirstRun = false;
						}
					}
                }
			}			
        }
        private void InitUI()
        {
            // 设置默认值到UI组件
            if (potNameText != null)
            {
                // 如果UI未设置槽号，则根据解析结果填充，避免硬编码
                var resolved = ResolveSelectedSlotNumber();
                if (string.IsNullOrEmpty(potNameText.text))
                {
                    potNameText.text = resolved;
                }
            }


            if (dateText != null)
                dateText.text = System.DateTime.Now.ToString("yyyyMMdd");
            
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveAllListeners();
                applyFilterButton.onClick.AddListener(OnApplyFilter); // 启用这个功能
            }
        }
        
        // 添加筛选按钮的处理方法
        public void OnApplyFilter()
        {
            // 每次应用筛选前解析一次槽号，防止UI与内部不一致
            selectedPotName = ResolveSelectedSlotNumber();
            Debug.Log($"应用筛选: 槽号={selectedPotName}, 日期={selectedDate}, 类别={selectedCategory}");
            LoadTemperatureData();
        }
        
  
        
        private void InitCategoryButtons()
        {
            Debug.Log("初始化类别按钮");
            
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
            
            // 初始化按钮视觉状态
            UpdateCategoryButtonVisualState();
        }
        
   
        
        private void OnCategoryButtonClicked(string category)
        {
            selectedCategory = category;
            Debug.Log($"选择类别: {selectedCategory}");
            
            // 更新按钮视觉状态
            UpdateCategoryButtonVisualState();
            
            // 立即重新加载数据
            LoadTemperatureData(); // 取消注释这行
        }
        
        private void UpdateCategoryButtonVisualState()
        {
            // 定义选中和未选中的颜色
            Color selectedColor = new Color32(4, 145, 81, 255); // #049151
            Color normalColor = Color.clear;
            SetButtonColor(categoryButton阴极A, selectedCategory == "A面阴极", selectedColor, normalColor);
            SetButtonColor(categoryButton阴极B, selectedCategory == "B面阴极", selectedColor, normalColor);
            SetButtonColor(categoryButton钢窗A, selectedCategory == "A面钢窗", selectedColor, normalColor);
            SetButtonColor(categoryButton钢窗B, selectedCategory == "B面钢窗", selectedColor, normalColor);
            SetButtonColor(categoryButton端部, selectedCategory == "端部", selectedColor, normalColor);
            SetButtonColor(categoryButton底部, selectedCategory == "底部", selectedColor, normalColor);
        }

        private void SetButtonColor(Button btn, bool isSelected, Color selectedColor, Color normalColor)
        {
            if (btn == null) return;
            var tf = btn.transform.Find("visual");
            var targetImg = tf != null ? tf.GetComponent<Image>() : btn.GetComponent<Image>();
            if (targetImg != null)
            {
                targetImg.color = isSelected ? selectedColor : normalColor;
            }
        }
        
        
        public void LoadTemperatureData()
        {
            if (!gameObject.activeInHierarchy)
            {
                Debug.Log("游戏对象未激活，无法启动协程");
                return;
            }
            
            if (!GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                Debug.Log("传感器配置数据未加载，无法获取温度数据");
                return;
            }
            
            // 显示加载提示
            ShowLoadingText(true);
            
            // 清空现有数据和图表
            allSensorData.Clear();
            ClearCharts();
            
            Debug.Log("开始加载温度数据...");

            // 首先获取历史温度数据（如果还没有的话）
            // 统一使用真实的企业与槽号生成缓存键，避免与请求不一致
            string dataKey = GlobalTemperatureSensorDataManager.GenerateTemperatureDataKey(companyId, int.Parse(selectedPotName), selectedDate);
            Debug.Log($"检查历史温度缓存键: {dataKey}");

            if (!GlobalTemperatureSensorDataManager.Instance.HasTemperatureData(dataKey))
            {
                Debug.Log("全局缓存中没有温度数据，先获取历史温度数据");
                StartCoroutine(FetchHistoryTemperatureDataFirst());
            }
            else
            {
                Debug.Log("全局缓存中已有温度数据，直接使用");
                StartCoroutine(FetchTemperatureDataFromAPI());
            }
        }
        
        /// <summary>
        /// 显示或隐藏加载提示文本
        /// </summary>
        /// <param name="show">是否显示</param>
        private void ShowLoadingText(bool show)
        {
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(show);
                if (show)
                {
                    loadingText.text = "正在读取历史数据...";
                }
            }
        }
/// <summary>
/// 首先获取历史温度数据
        /// </summary>
        private IEnumerator FetchHistoryTemperatureDataFirst()
        {
            // 使用与HistoryTemperatureTest相同的API调用
            //20251102,cxt,设定companyId
            //string companyId = GlobalUserData.AlSerialPantId;
            string resolvedSlot = ResolveSelectedSlotNumber();
            string resolvedDate = GetValidatedDate();
            string requestData = $"{companyId}-67-{resolvedSlot}-{resolvedDate}";
            string jsonData = $"\"{requestData}\"";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;
                
                Debug.Log($"获取历史温度数据: {requestData}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"历史温度数据API响应成功");
                    
                    // 解析并存储历史温度数据
                    var historyData = ParseHistoryTemperatureResponse(responseText);
                    if (historyData != null && historyData.Count > 0)
                    {
                        string dataKey = GlobalTemperatureSensorDataManager.GenerateTemperatureDataKey(companyId, int.Parse(resolvedSlot), resolvedDate);
                        GlobalTemperatureSensorDataManager.Instance.StoreTemperatureData(dataKey, historyData);
                        Debug.Log($"历史温度数据已存储到全局缓存: {historyData.Count} 条记录");
                    }
                }
                else
                {
                    Debug.LogError($"获取历史温度数据失败: {request.error}");
                    // 出错时也要隐藏加载提示
                    ShowLoadingText(false);
                }
            }
            
            // 继续获取传感器数据
            yield return StartCoroutine(FetchTemperatureDataFromAPI());
        }
        private void ClearCharts()
        {
            // 销毁现有图表
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
        }
        
        /// <summary>
        /// 验证传感器配置数据是否符合预期
        /// </summary>
        private void ValidateSensorData()
        {
            if (!GlobalTemperatureSensorDataManager.Instance.isDataLoaded)
            {
                Debug.LogError("传感器配置数据未加载！");
                return;
            }
            
            var amianyinjiCount = GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs().Count;
            var bmianyinjiCount = GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs().Count;
            var amiangangchuangCount = GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs().Count;
            var bmiangangchuangCount = GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs().Count;
            var duanbuCount = GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs().Count;
            var dibuCount = GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs().Count;
            
            //Debug.Log($"=== 传感器配置数据验证 ===");
            //Debug.Log($"A面阴极: {amianyinjiCount} (预期: 56)");
            //Debug.Log($"B面阴极: {bmianyinjiCount} (预期: 56)");
            //Debug.Log($"A面钢窗: {amiangangchuangCount} (预期: 28)");
            //Debug.Log($"B面钢窗: {bmiangangchuangCount} (预期: 28)");
            //Debug.Log($"端部: {duanbuCount} (预期: 18)");
            //Debug.Log($"底部: {dibuCount} (预期: 28)");
            
            // 检查是否符合预期
            bool isValid = true;
            //if (amianyinjiCount != 56) { Debug.LogWarning($"A面阴极数量不符合预期: {amianyinjiCount} != 56"); isValid = false; }
            //if (bmianyinjiCount != 56) { Debug.LogWarning($"B面阴极数量不符合预期: {bmianyinjiCount} != 56"); isValid = false; }
            //if (amiangangchuangCount != 28) { Debug.LogWarning($"A面钢窗数量不符合预期: {amiangangchuangCount} != 28"); isValid = false; }
            //if (bmiangangchuangCount != 28) { Debug.LogWarning($"B面钢窗数量不符合预期: {bmiangangchuangCount} != 28"); isValid = false; }
            //if (duanbuCount != 18) { Debug.LogWarning($"端部数量不符合预期: {duanbuCount} != 18"); isValid = false; }
            //if (dibuCount != 28) { Debug.LogWarning($"底部数量不符合预期: {dibuCount} != 28"); isValid = false; }
            
            if (isValid)
            {
                Debug.Log("✅ 传感器配置数据验证通过！");
            }
            else
            {
                Debug.LogError("❌ 传感器配置数据验证失败！请检查数据加载逻辑。");
            }
        }
        private IEnumerator FetchTemperatureDataFromAPI()
        {
            // 获取选定类别的传感器配置
            var sensorConfigs = GetSensorConfigsByCategory(selectedCategory);
            
            if (sensorConfigs.Count == 0)
            {
                Debug.LogWarning($"未找到类别 {selectedCategory} 的传感器配置");
                yield break;
            }
            
            Debug.Log($"找到 {sensorConfigs.Count} 个 {selectedCategory} 传感器");
            
            // 优化：批量获取数据而不是逐个请求
            yield return StartCoroutine(FetchBatchSensorTemperatureData(sensorConfigs));
            
            // 创建图表
            CreateMultipleCharts();
        }
        
        // 新增：批量获取传感器数据
        private IEnumerator FetchBatchSensorTemperatureData(List<TemperatureSensorConfigManager.TemperatureSensorConfig> configs)
        {
            // 检查缓存，避免重复请求
            string resolvedSlot = ResolveSelectedSlotNumber();
            string resolvedDate = GetValidatedDate();
            string dataKey = GlobalTemperatureSensorDataManager.GenerateTemperatureDataKey(companyId, int.Parse(resolvedSlot), resolvedDate);
            
            if (GlobalTemperatureSensorDataManager.Instance.HasTemperatureData(dataKey))
            {
                Debug.Log("使用缓存数据，跳过API请求");
                var cachedData = GlobalTemperatureSensorDataManager.Instance.GetTemperatureData(dataKey);
                
                // 并行处理所有传感器数据
                foreach (var config in configs)
                {
                    ProcessCachedTemperatureData(cachedData, config);
                }
                yield break;
            }
            
            // 只发送一次API请求获取所有数据
            yield return StartCoroutine(FetchSingleAPIRequest(configs));
        }
        
        private IEnumerator FetchSingleAPIRequest(List<TemperatureSensorConfigManager.TemperatureSensorConfig> configs)
        {
            string resolvedSlot = ResolveSelectedSlotNumber();
            string resolvedDate = GetValidatedDate();
            string requestParam = $"{companyId}-67-{resolvedSlot}-{resolvedDate}";
            string jsonData = $"\"{requestParam}\"";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(byteArray);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;
                
                Debug.Log($"批量请求传感器数据: {requestParam}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"历史API响应文本长度: {responseText?.Length ?? 0}, 内容前20: {((responseText != null) ? responseText.Substring(0, Math.Min(20, responseText.Length)) : string.Empty)}");
                    var historyData = ParseHistoryTemperatureResponse(responseText);
                    
                    if (historyData != null && historyData.Count > 0)
                    {
                        // 存储到缓存
                        string dataKey = GlobalTemperatureSensorDataManager.GenerateTemperatureDataKey(companyId, int.Parse(resolvedSlot), resolvedDate);
                        GlobalTemperatureSensorDataManager.Instance.StoreTemperatureData(dataKey, historyData);
                        
                        // 并行处理所有传感器
                        foreach (var config in configs)
                        {
                            ProcessCachedTemperatureData(historyData, config);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("批量请求成功但未解析到有效历史数据。已为所选传感器生成测试数据，请核查日期/槽号或服务端响应是否存在历史数据。");
                        // 调试时使用测试数据，在无历史数据时为每个传感器生成简单测试数据，避免空图
                        foreach (var config in configs)
                        {
                            GenerateTestDataForSensor(config);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"批量获取数据失败: {request.error}。不使用测试数据兜底。");
                }
            }
        }
        
        private List<TemperatureSensorConfigManager.TemperatureSensorConfig> GetSensorConfigsByCategory(string category)
        {
            var configs = new List<TemperatureSensorConfigManager.TemperatureSensorConfig>();
            
            // 添加调试信息，显示实际的传感器数量
            var stats = GlobalTemperatureSensorDataManager.Instance.GetDataStatistics();
            Debug.Log($"全局传感器数据统计: {stats}");
            if (category == "A面阴极") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs()); return configs; }
            if (category == "B面阴极") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs()); return configs; }
            if (category == "A面钢窗") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs()); return configs; }
            if (category == "B面钢窗") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs()); return configs; }
            if (category == "端部") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs()); return configs; }
            if (category == "底部") { configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs()); return configs; }
            
            switch (category)
            {
                case "阴极":
                    // 合并A面阴极和B面阴极
                    var amianyinjiConfigs = GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs();
                    var bmianyinjiConfigs = GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs();
                    configs.AddRange(amianyinjiConfigs);
                    configs.AddRange(bmianyinjiConfigs);
                    Debug.Log($"阴极类别 - A面阴极: {amianyinjiConfigs.Count}, B面阴极: {bmianyinjiConfigs.Count}, 总计: {configs.Count}");
                    break;
                    
                case "钢窗":
                    // 合并A面钢窗和B面钢窗
                    var amiangangchuangConfigs = GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs();
                    var bmiangangchuangConfigs = GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs();
                    configs.AddRange(amiangangchuangConfigs);
                    configs.AddRange(bmiangangchuangConfigs);
                    Debug.Log($"钢窗类别 - A面钢窗: {amiangangchuangConfigs.Count}, B面钢窗: {bmiangangchuangConfigs.Count}, 总计: {configs.Count}");
                    break;
                    
                case "端部底部":
                    // 合并端部和底部
                    var duanbuConfigs = GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs();
                    var dibuConfigs = GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs();
                    configs.AddRange(duanbuConfigs);
                    configs.AddRange(dibuConfigs);
                    Debug.Log($"端部底部类别 - 端部: {duanbuConfigs.Count}, 底部: {dibuConfigs.Count}, 总计: {configs.Count}");
                    break;
                    
                // 保留原有的单独类别支持（用于调试和验证）
                case "A面阴极":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmianyinjiConfigs());
                    Debug.Log($"A面阴极: {configs.Count} 个传感器");
                    break;
                case "B面阴极":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmianyinjiConfigs());
                    Debug.Log($"B面阴极: {configs.Count} 个传感器");
                    break;
                case "A面钢窗":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetAmiangangchuangConfigs());
                    Debug.Log($"A面钢窗: {configs.Count} 个传感器");
                    break;
                case "B面钢窗":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetBmiangangchuangConfigs());
                    Debug.Log($"B面钢窗: {configs.Count} 个传感器");
                    break;
                case "端部":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDuanbuConfigs());
                    Debug.Log($"端部: {configs.Count} 个传感器");
                    break;
                case "底部":
                    configs.AddRange(GlobalTemperatureSensorDataManager.Instance.GetDibuConfigs());
                    Debug.Log($"底部: {configs.Count} 个传感器");
                    break;
                    
                default:
                    Debug.LogWarning($"未知的传感器类别: {category}");
                    break;
            }
            
            return configs;
        }
            
        
        private IEnumerator FetchSensorTemperatureData(TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            // 构建API请求参数
            string requestParam = $"{companyId}-67-{selectedPotName}-{selectedDate}";
            string jsonData = $"\"{requestParam}\"";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(byteArray);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;
                
                Debug.Log($"请求传感器 {config.sampleindex} 的温度数据: {requestParam}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    ParseSensorTemperatureResponse(responseText, config);
                }
                else
                {
                    Debug.LogError($"获取传感器 {config.sampleindex} 数据失败: {request.error}");
                    // 生成测试数据
                    //GenerateTestDataForSensor(config);
                }
            }
        }
        
        private void ParseSensorTemperatureResponse(string responseText, TemperatureSensorConfigManager.TemperatureSensorConfig config)
        {
            try
            {
                // 使用与HistoryTemperatureTest相同的解析逻辑
                string dataKey = GlobalTemperatureSensorDataManager.GenerateTemperatureDataKey(companyId, int.Parse(selectedPotName), selectedDate);
                
                // 检查全局缓存中是否已有数据
                if (GlobalTemperatureSensorDataManager.Instance.HasTemperatureData(dataKey))
                {
                    Debug.Log($"从全局缓存获取温度数据: {dataKey}");
                    var cachedData = GlobalTemperatureSensorDataManager.Instance.GetTemperatureData(dataKey);
                    ProcessCachedTemperatureData(cachedData, config);
                    return;
                }
                
                // 如果缓存中没有数据，解析API响应
                var historyData = ParseHistoryTemperatureResponse(responseText);
                if (historyData != null && historyData.Count > 0)
                {
                    // 存储到全局缓存
                    GlobalTemperatureSensorDataManager.Instance.StoreTemperatureData(dataKey, historyData);
                    
                    // 处理数据
                    ProcessCachedTemperatureData(historyData, config);
                }
                else
                {
                    Debug.LogWarning($"未能解析到有效的温度数据，为传感器 {config.sampleindex} 生成测试数据");
                    //GenerateTestDataForSensor(config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析传感器 {config.sampleindex} 数据时出错: {ex.Message}");
                //GenerateTestDataForSensor(config);
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
        Debug.LogError($"Base64解码失败: {ex.Message}");
        return null;
    }
    
    Debug.Log($"解码后数据长度: {frameData.Length} 字节");
    
    // 解析帧格式
    if (!ParseFrameData(frameData, out List<GlobalTemperatureSensorDataManager.HistoryTemperatureData> historyData))
    {
        Debug.LogError("帧数据解析失败");
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
        Debug.LogError("帧数据长度不足");
        return false;
    }
    
    int offset = 0;
    
    // 验证帧头：170, 85
    if (frameData[offset] != 170 || frameData[offset + 1] != 85)
    {
        Debug.LogError($"帧头验证失败: {frameData[offset]}, {frameData[offset + 1]} (期望: 170, 85)");
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
    
    Debug.Log($"企业标识: {enterpriseId}, 帧类型: {frameType}, 槽号: {slotNumber}, 数据长度: {dataLength}");
    
    // 验证数据长度
    if (offset + dataLength + 2 > frameData.Length)
    {
        Debug.LogError($"数据长度验证失败: 期望 {offset + dataLength + 2}, 实际 {frameData.Length}");
        return false;
    }
    
    // 验证帧尾
    int tailOffset = offset + dataLength;
    if (frameData[tailOffset] != 165 || frameData[tailOffset + 1] != 90)
    {
        Debug.LogError($"帧尾验证失败: {frameData[tailOffset]}, {frameData[tailOffset + 1]} (期望: 165, 90)");
        return false;
    }
    
    // 解析历史温度数据
    int recordSize = 512;
    int recordCount = dataLength / recordSize;
    
    Debug.Log($"历史记录数量: {recordCount}");
    
    for (int i = 0; i < recordCount; i++)
    {
        int recordOffset = offset + i * recordSize;
        
        var historyRecord = new GlobalTemperatureSensorDataManager.HistoryTemperatureData();
        historyRecord.companyIndex = companyId;
        historyRecord.slotNumber = slotNumber;
        historyRecord.queryDate = selectedDate;
        
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
            int rawTemperatureValue = (frameData[sampleOffset] << 8) | frameData[sampleOffset + 1];
            // 直接使用整数值，不进行浮点数转换
            int temperatureValue = rawTemperatureValue;
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
    if (historyData == null || historyData.Count == 0)
    {
        Debug.LogWarning($"没有可用的温度数据，为传感器 {config.sampleindex} 生成测试数据");
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

    // 仅使用第一条包含有效采样索引的历史记录，避免重复叠加造成数据异常
    for (int i = 0; i < historyData.Count; i++)
    {
        var record = historyData[i];
        Debug.Log($"历史记录 {i}: 日期: {record.date:yyyy-MM-dd HH:mm:ss}, 温度值数量: {record.temperatureValues.Count}");
        if (record.temperatureValues.Count > config.sampleindex)
        {
            ConvertSampleDataToTimeSeriesData(record.temperatureValues, sensorData, config.sampleindex);
            Debug.Log($"成功为传感器 {config.sampleindex} 生成时间序列数据，来源记录索引: {i}");
            break;
        }
        else
        {
            Debug.LogWarning($"历史记录 {i} 的温度值数量 {record.temperatureValues.Count} 小于传感器索引 {config.sampleindex}");
        }
    }

    // 若仍未生成数据，尝试使用第一条记录兜底
    if (sensorData.temperatureValues.Count == 0 && historyData.Count > 0 && historyData[0].temperatureValues.Count > 0)
    {
        Debug.LogWarning($"未找到传感器 {config.sampleindex} 对应的有效温度数据，使用第一条记录兜底");
        ConvertSampleDataToTimeSeriesData(historyData[0].temperatureValues, sensorData, config.sampleindex);
    }

    if (sensorData.temperatureValues.Count > 0)
    {
        allSensorData.Add(sensorData);
        float minTemp = sensorData.temperatureValues.Min();
        float maxTemp = sensorData.temperatureValues.Max();
        float avgTemp = (float)sensorData.temperatureValues.Average();
        Debug.Log($"传感器 {config.sampleindex} 温度数据统计 - 点数: {sensorData.temperatureValues.Count}, 最小值: {minTemp}, 最大值: {maxTemp}, 平均值: {avgTemp:F1}");
    }
    else
    {
        Debug.LogError($"传感器 {config.sampleindex} 没有生成任何温度数据点");
    }
}

/// <summary>
/// 将采样点数据转换为时间序列数据（恢复原可绘制版本逻辑）
/// 使用指定采样点作为基准温度，叠加小幅平滑变化生成720个时间点
/// </summary>
private void ConvertSampleDataToTimeSeriesData(List<int> sampleData, SensorTemperatureData sensorData, int sampleIndex)
{
    // 基准温度取指定采样点；越界则取最后一个
    int targetSampleIndex = Mathf.Min(sampleIndex, sampleData.Count - 1);
    int baseTemperature = sampleData[Mathf.Max(0, targetSampleIndex)];

    // 预计算时间标签（只做一次）
    if (sensorData.timeLabels.Count == 0)
    {
        for (int i = 0; i < 720; i++)
        {
            int hours = i * 2 / 60;
            int minutes = (i * 2) % 60;
            sensorData.timeLabels.Add($"{hours:D2}:{minutes:D2}");
        }
    }

    // 预计算变化（整数），保证平滑且性能稳定
    int[] variations = new int[720];
    for (int i = 0; i < 720; i++)
    {
        float timeHour = i * 2f / 60f;
        float sinV = Mathf.Sin(timeHour * Mathf.PI / 12f) * 1f;
        float cosV = Mathf.Cos(timeHour * Mathf.PI / 8f) * 0.5f;
        variations[i] = Mathf.RoundToInt(sinV + cosV);
    }

    // 生成时间序列数据（720点）
    sensorData.temperatureValues.Capacity = Math.Max(sensorData.temperatureValues.Count, 720);
    for (int i = 0; i < 720; i++)
    {
        int temperature = baseTemperature + variations[i];
        sensorData.temperatureValues.Add(temperature);
    }

    int minVal = sensorData.temperatureValues.Count > 0 ? sensorData.temperatureValues.Min() : 0;
    int maxVal = sensorData.temperatureValues.Count > 0 ? sensorData.temperatureValues.Max() : 0;
    Debug.Log($"已生成时间序列数据：采样索引 {targetSampleIndex}，范围 {minVal}-{maxVal}");
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
            
            // 生成720个测试温度数据点（24小时，每2分钟一个点）
            float baseTemp = UnityEngine.Random.Range(240f, 300f);
            
            for (int i = 0; i < 720; i++)
            {
                // 模拟温度变化
                float timeHour = i * 2f / 60f; // 每2分钟一个点
                float tempVariation = Mathf.Sin(timeHour * Mathf.PI / 12f) * 50f; // 12小时周期变化
                float noise = UnityEngine.Random.Range(-10f, 10f);
                float temperature = baseTemp + tempVariation + noise;
                sensorData.temperatureValues.Add(Mathf.RoundToInt(temperature));
                
                // 生成时间标签
                int hours = i * 2 / 60;
                int minutes = (i * 2) % 60;
                sensorData.timeLabels.Add($"{hours:D2}:{minutes:D2}");
            }
            
            allSensorData.Add(sensorData);
            Debug.Log($"为传感器 {config.sampleindex} 生成了 {sensorData.temperatureValues.Count} 个测试数据点");
        }
        
private void CreateMultipleCharts()
{
    if (chartPrefab == null || chartsContainer == null)
    {
        Debug.LogError("图表预制体或容器未设置");
        return;
    }
    
    // 数据验证检查
    if (allSensorData == null || allSensorData.Count == 0)
    {
        Debug.LogWarning("没有可用的传感器数据，无法创建图表（可能为所选日期无历史数据）。");
        ShowLoadingText(false);
        return;
    }
    
    Debug.Log($"开始分批创建 {allSensorData.Count} 个图表");
    
    // 启动分批创建协程
    StartCoroutine(CreateChartsInBatches());
    
    // 注意：不要在这里隐藏加载提示，应该在协程完成后隐藏
    // ShowLoadingText(false);
    
    Debug.Log($"图表创建协程已启动");
}

private IEnumerator CreateChartsInBatches()
{
    const int batchSize = 5; // 每批创建5个图表
    int totalCharts = allSensorData.Count;
    
    // 预先计算布局
    int rows = Mathf.CeilToInt((float)totalCharts / 1); // 单列布局
    float containerWidth = chartSize.x;
    float containerHeight = rows * (chartSize.y + spacing.y) - spacing.y;
    
    if (chartsContainer.GetComponent<RectTransform>() != null)
    {
        chartsContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(containerWidth, containerHeight);
    }
    
    if (scrollRect != null)
    {
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    }
    
    // 分批创建图表
    for (int i = 0; i < totalCharts; i += batchSize)
    {
        int endIndex = Mathf.Min(i + batchSize, totalCharts);
        
        // 创建当前批次的图表
        for (int j = i; j < endIndex; j++)
        {
            CreateSingleChart(allSensorData[j], j);
        }
        
        // 等待一帧，避免卡顿
        yield return null;
        
        Debug.Log($"已创建 {endIndex}/{totalCharts} 个图表");
    }
    
    // 所有图表创建完成后隐藏加载提示
    ShowLoadingText(false);
    
    Debug.Log($"所有图表创建完成，共创建了 {chartInstances.Count} 个图表");
    
    Debug.Log($"所有图表创建完成，共 {chartInstances.Count} 个");
    
    // 隐藏加载提示
    ShowLoadingText(false);
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
        Debug.LogError($"图表预制体缺少LineChart组件");
    }
}
        
        private void ConfigureSingleChart(LineChart chart, SensorTemperatureData sensorData)
        {
            // 清空现有数据
            chart.ClearData();

            // 设置图表标题
            //20251102,cxt
            //var title = chart.GetOrAddChartComponent<Title>();
            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            string sensorDisplayName = GetSensorDisplayName(sensorData.config);
            title.text = sensorDisplayName;
            title.labelStyle.textStyle.color = Color.white;
            title.location.align = Location.Align.TopLeft;
            title.location.left = 0.03f;
            title.location.top = 0.03f;
            
            // 配置X轴（时间）- 参考Line_LocalDB.cs的样式
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.splitNumber = 6; // 7个标签点：00:00, 04:00, 08:00, 12:00, 16:00, 20:00, 24:00
            xAxis.axisLabel.interval = 0; // 显示所有非空标签
            xAxis.axisLabel.textStyle.fontSize = 14;
            xAxis.axisLabel.textStyle.color = Color.white;
            xAxis.axisLine.show = true;
            xAxis.axisTick.show = true;
            xAxis.boundaryGap = true;
            
            // 固定写死X轴标签 - 720个数据点对应720个标签（索引0-719）
            string[] timeLabels = new string[720]; // 修改为720个标签
            for (int i = 0; i < timeLabels.Length; i++)
            {
                timeLabels[i] = ""; // 默认所有标签为空
            }
            
            // 设置特定位置的标签（注意索引范围是0-719）
            timeLabels[0] = "00:00";
            timeLabels[120] = "04:00";
            timeLabels[240] = "08:00";
            timeLabels[360] = "12:00";
            timeLabels[480] = "16:00";
            timeLabels[600] = "20:00";
            if (timeLabels.Length > 719) // 确保不越界
            {
                timeLabels[719] = "24:00"; // 最后一个数据点标记为24:00
            }
            
            // 添加所有标签到X轴
            foreach (string label in timeLabels)
            {
                chart.AddXAxisData(label);
            }
            xAxis.SetAllDirty();
            chart.RefreshChart();
            
            // 配置Y轴（温度）- 动态范围
            int ymin = 0, ymax = 1000;
            if (sensorData.temperatureValues != null && sensorData.temperatureValues.Count > 0)
            {
                ymin = sensorData.temperatureValues.Min();
                ymax = sensorData.temperatureValues.Max();
                // 加边距，避免贴边
                int padding = Math.Max(2, (int)((ymax - ymin) * 0.05f));
                ymin = Math.Max(0, ymin - padding);
                ymax = ymax + padding;
                // 防止范围过小
                if (ymax - ymin < 10)
                {
                    ymax = ymin + 10;
                }
            }

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.min = ymin;
            yAxis.max = ymax;
            yAxis.position = Axis.AxisPosition.Left;
            yAxis.axisLabel.show = true;
            yAxis.axisLabel.textStyle.color = Color.white;
            yAxis.splitLine.show = false;
            yAxis.SetAllDirty();
            
            // 添加第二个Y轴组件并保持一致
            chart.AddChartComponent<YAxis>();
            var yAxis1 = chart.GetChartComponent<YAxis>(1);
            yAxis1.type = Axis.AxisType.Value;
            yAxis1.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis1.min = ymin;
            yAxis1.max = ymax;
            yAxis1.position = Axis.AxisPosition.Right;
            yAxis1.axisLabel.show = true;
            yAxis1.axisLabel.textStyle.color = Color.white;
            yAxis1.splitLine.show = false;
            yAxis1.SetAllDirty();
            
            // 设置背景与网格边框样式
            chart.theme.enableCustomTheme = true;
            chart.theme.transparentBackground = false;
            chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255); // #373B47
            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.backgroundColor = new Color32(40, 47, 57, 255); // #282F39
            grid.showBorder = true;
            grid.borderWidth = 2f;
            grid.borderColor = new Color32(255, 255, 255, 255); // 白色边框
            
            // 刷新图表以应用轴配置
            chart.RefreshChart();
            
            // 清除现有系列并重新添加
            chart.RemoveAllSerie();
            
            // 添加数据系列（紫色折线，无渐变填充）
            var serie = chart.AddSerie<Line>("温度数据");
            Color32 purple = new Color32(160, 96, 255, 255); // #A060FF
            serie.AnimationEnable(false);
            serie.lineStyle.color = purple;
            serie.itemStyle.color = purple;
            serie.lineStyle.width = 2f;
            serie.symbol.show = false; // 不显示数据点符号，数据太多会影响性能
            serie.large = false;

            // 关闭渐变填充区域
            var area = serie.EnsureComponent<AreaStyle>();
            area.show = false;
            
            // 添加温度数据 - 确保数据点数量为720个，移除重复添加
            int dataCount = Mathf.Min(sensorData.temperatureValues.Count, 720);
            
            // 只添加一次数据，并在特定时间点添加红色标签
            for (int i = 0; i < 720; i++)
            {
                int temperature;
                if (i < sensorData.temperatureValues.Count)
                {
                    temperature = sensorData.temperatureValues[i];
                }
                else
                {
                    // 如果数据不足720个，用最后一个值填充
                    temperature = sensorData.temperatureValues.Count > 0 ? 
                        sensorData.temperatureValues[sensorData.temperatureValues.Count - 1] : 0;
                }
                
                // 直接添加整数值到图表，不需要格式化
                var serieData = chart.AddData(0, temperature);
                
                // 只在特定时间点（0点、4点、8点、12点、16点、20点、24点）显示红色数值标签
                if (i == 0 || i == 120 || i == 240 || i == 360 || i == 480 || i == 600 || i == 719) // 最后一个点改为719
                {
                    // 为数据点添加标签组件
                    var labelStyle = serieData.EnsureComponent<LabelStyle>();
                    labelStyle.show = true;
                    labelStyle.position = LabelStyle.Position.Top;
                    labelStyle.offset = new Vector3(0, 20, 0);
                    labelStyle.formatter = "{c}"; // 显示数值
                    //labelStyle.numericFormatter = "F1"; // 保留一位小数
                    labelStyle.textStyle.autoColor = false;
                    labelStyle.textStyle.color = Color.white; // 设置颜色为白色
                    labelStyle.textStyle.fontStyle = FontStyle.Bold; // 设置字体加粗
                    
                    // 为数据点添加符号组件
                    var symbol = serieData.EnsureComponent<SerieSymbol>();
                    symbol.show = true;
                    symbol.type = SymbolType.Diamond; // 菱形符号
                    symbol.size = 5; // 设置符号大小
                }
            }
            
            // 配置图例
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = false;
            
            // 配置工具提示
            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;
            
            // 移除数据缩放功能（左右滑动）
            var dataZoom = chart.GetChartComponent<DataZoom>();
            if (dataZoom != null)
            {
                dataZoom.enable = false;
                dataZoom.supportInside = false;
                dataZoom.supportSlider = false;
            }
            
            // 最终刷新图表
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
                { "端口", Color.magenta },
                { "底部", Color.cyan }
            };
            
            return categoryColors.ContainsKey(category) ? categoryColors[category] : Color.gray;
        }
        
        // 公共方法
        public void RefreshData()
        {
            LoadTemperatureData();
        }
        
        // 统一解析槽号，来源优先级：UI文本 -> 槽号选择器 -> 全局数据 -> 默认1001
        private string ResolveSelectedSlotNumber()
        {
            try
            {
                if (potNameText != null)
                {
                    var txt = (potNameText.text ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(txt) && Regex.IsMatch(txt, @"^\d+$"))
                        return txt;
                }

                var selector = FindObjectOfType<SlotNumberSelector>();
                if (selector != null)
                {
                    var slot = selector.GetSelectedSlotNumber();
                    if (!string.IsNullOrEmpty(slot) && Regex.IsMatch(slot, @"^\d+$"))
                        return slot;
                }

                if (GlobalElectrolyticCellDataManager.Instance != null)
                {
                    var configs = GlobalElectrolyticCellDataManager.Instance.GetAllConfigs();
                    if (configs != null && configs.Count > 0)
                        return configs[0].slotNumber.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析槽号时出现异常: {ex.Message}");
            }
            // 回退默认
            return "1001";
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
	}   
  }
