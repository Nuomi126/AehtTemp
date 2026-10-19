using UnityEngine;
using TMPro;
using XCharts.Runtime;
using XCharts.Runtime.UI;

namespace XCharts.UI.Demo
{
    /// <summary>
    /// 统计报表表格UI组件
    /// 挂载到UITable GameObject上，负责显示统计数据
    /// </summary>
    public class StatisticsReportTable : MonoBehaviour
    {
        [Header("表格组件")]
        [SerializeField] private UITable m_Table;

        [Header("字体设置")]
        [SerializeField] private TMP_FontAsset customTMPFont;
        
        [Header("表格颜色配置")]
        [SerializeField] private Color32 headerBgColor = new Color32(70, 130, 180, 255);      // 表头背景色（钢青色）
        [SerializeField] private Color32 headerTextColor = new Color32(255, 255, 255, 255);  // 表头文字色（白色）
        [SerializeField] private Color32 dataTextColor = new Color32(50, 50, 50, 255);       // 数据文字色（深灰）
        [SerializeField] private Color32 whiteRow = new Color32(255, 255, 255, 255);         // 白色行
        [SerializeField] private Color32 lightGreenRow = new Color32(240, 255, 240, 255);    // 淡绿色行

        [Header("选择器组件（可选，用于获取参数）")]
        [SerializeField] private GameObject workshopSelector;  // 车间/工区选择器
        [SerializeField] private GameObject slotSelector;      // 槽号选择器
        [SerializeField] private riqSelector dateSelector;     // 日期选择器

        private SlotNumberSelector slotNumberSelector;
        private WorkAreaSelector workAreaSelector;

        void Awake()
        {
            // 获取UITable组件
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
            }

            // 设置中文字体支持
            if (customTMPFont != null)
            {
                SetupTMPFont();
            }
            
            // 查找日期选择器
            if (dateSelector == null)
            {
                dateSelector = FindObjectOfType<riqSelector>();
                if (dateSelector != null)
                {
                    Debug.Log($"✓ 自动找到 DateSelector: {dateSelector.name}");
                }
            }

            // 监听日期确认事件
            if (dateSelector != null)
            {
                dateSelector.OnDateConfirmed.AddListener(OnDateSelected);
            }

            // 查找选择器组件（即使对象未激活也能找到）
            if (slotSelector != null)
            {
                slotNumberSelector = slotSelector.GetComponentInChildren<SlotNumberSelector>(true);
            }
            
            // 如果未找到，尝试全局查找
            if (slotNumberSelector == null)
            {
                slotNumberSelector = FindObjectOfType<SlotNumberSelector>();
                if (slotNumberSelector != null)
                {
                    Debug.Log($"✓ 自动找到 SlotNumberSelector: {slotNumberSelector.name}");
                }
                else
                {
                     Debug.LogWarning("未找到 SlotNumberSelector 组件！");
                }
            }
            
            // 监听槽号确认事件
            if (slotNumberSelector != null)
            {
                slotNumberSelector.OnSlotConfirmed.AddListener(OnSlotSelected);
                Debug.Log("✓ 已监听槽号选择事件");
            }
            
            if (workshopSelector != null)
            {
                workAreaSelector = workshopSelector.GetComponentInChildren<WorkAreaSelector>(true);
            }

            // 如果未找到，尝试全局查找
            if (workAreaSelector == null)
            {
                workAreaSelector = FindObjectOfType<WorkAreaSelector>();
                if (workAreaSelector != null)
                {
                    Debug.Log($"✓ 自动找到 WorkAreaSelector: {workAreaSelector.name}");
                }
                else
                {
                     Debug.LogWarning("未找到 WorkAreaSelector 组件！");
                }
            }
            
            // 监听车间工区选择事件
            if (workAreaSelector != null)
            {
                workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnWorkAreaSelected);
                Debug.Log("✓ 已监听车间工区选择事件");
            }
            
            // 设置主题字体
            if (m_Table != null && m_Table.theme != null && m_Table.theme.sharedTheme != null && customTMPFont != null)
            {
                m_Table.theme.sharedTheme.tmpFont = customTMPFont;
                if (m_Table.theme.sharedTheme.title != null) m_Table.theme.sharedTheme.title.fontSize = 30;
                if (m_Table.theme.sharedTheme.common != null) m_Table.theme.sharedTheme.common.fontSize = 30;
                m_Table.theme.sharedTheme.SyncTMPFontToSubComponent();
                m_Table.SetAllDirty();
                Debug.Log("👉 已设置统计报表表格主题字体");
            }
        }

        void Start()
        {
            // 初始化表格样式
            InitializeTableStyle();
            
            // 延迟设置默认值，确保选择器的Start已执行
            Invoke(nameof(SetDefaultValuesToSelectors), 0.5f);
            
            // 延迟加载，使用Frame 72按车间工区加载所有槽号
            Invoke(nameof(AutoLoadFrame72Data), 1.5f);
        }
        
        /// <summary>
        /// 车间工区选择回调
        /// </summary>
        private void OnWorkAreaSelected(string workAreaText)
        {
            Debug.Log($"=== 收到车间工区选择事件: {workAreaText} ===");
            // 使用Frame 72重新加载该车间工区的所有槽号数据
            LoadFrame72DataByWorkArea();
        }
        
        /// <summary>
        /// 自动加载Frame 72数据（入口加载）
        /// </summary>
        private void AutoLoadFrame72Data()
        {
            Debug.Log("=== 统计报表 Frame 72 自动加载 ===");
            LoadFrame72DataByWorkArea();
        }
        
        /// <summary>
        /// 根据当前选中的车间工区加载Frame 72数据
        /// </summary>
        private void LoadFrame72DataByWorkArea()
        {
            if (StatisticsReportManager.Instance == null)
            {
                Debug.LogError("✗ StatisticsReportManager实例未找到！");
                return;
            }
            
            // 获取车间号和工区号
            int workshopNumber = 1;
            int workAreaNumber = 1;
            
            if (workAreaSelector != null)
            {
                workshopNumber = workAreaSelector.GetSelectedWorkshopNumber();
                workAreaNumber = workAreaSelector.GetSelectedWorkAreaNumber();
                Debug.Log($"✓ 从WorkAreaSelector获取: 车间{workshopNumber} 工区{workAreaNumber}");
            }
            else
            {
                Debug.LogWarning("✗ WorkAreaSelector未找到，使用默认车间1工区1");
            }
            
            // 获取日期
            string queryDate = System.DateTime.Now.ToString("yyyyMMdd");
            if (dateSelector != null && dateSelector.selectedDisplayText != null)
            {
                // 尝试解析日期选择器的日期
                string dateText = dateSelector.selectedDisplayText.text;
                var match = System.Text.RegularExpressions.Regex.Match(dateText, @"(\d{4})\u5e74(\d{2})\u6708(\d{2})\u65e5");
                if (match.Success)
                {
                    queryDate = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;
                }
            }
            
            Debug.Log($"→ 加载Frame 72统计报表: 车间{workshopNumber} 工区{workAreaNumber} 日期{queryDate}");
            
            // 调用Frame 72批量请求
            StatisticsReportManager.Instance.FetchFrame72ReportByWorkArea(
                workshopNumber, 
                workAreaNumber, 
                queryDate, 
                queryDate  // 起始和终止日期相同
            );
            
            // 延迟填充数据（等待API返回）
            Invoke(nameof(FillVoltageTable), 5f);
        }
        
        /// <summary>
        /// 设置默认值到选择器UI
        /// </summary>
        private void SetDefaultValuesToSelectors()
        {
            Debug.Log("=== 开始设置默认值到选择器UI ===");
            
            // 设置默认槽号显示
            if (slotNumberSelector != null && StatisticsReportManager.Instance != null)
            {
                string defaultSlot = StatisticsReportManager.Instance.defaultTestSlot;
                if (slotNumberSelector.selectedDisplayText != null)
                {
                    slotNumberSelector.selectedDisplayText.text = defaultSlot;
                    Debug.Log($"✓ 设置默认槽号显示: {defaultSlot}");
                }
                else
                {
                    Debug.LogWarning("✗ slotNumberSelector.selectedDisplayText为null");
                }
            }
            else
            {
                Debug.LogWarning($"✗ 槽号设置失败 - slotNumberSelector: {slotNumberSelector != null}, Manager: {StatisticsReportManager.Instance != null}");
            }
            
            // 设置默认日期显示
            if (dateSelector != null)
            {
                string today = System.DateTime.Now.ToString("yyyyMMdd");
                if (dateSelector.selectedDisplayText != null)
                {
                    dateSelector.selectedDisplayText.text = System.DateTime.Now.ToString("yyyy年MM月dd日");
                    Debug.Log($"✓ 设置默认日期显示: {today}");
                }
                else
                {
                    Debug.LogWarning("✗ dateSelector.selectedDisplayText为null");
                }
            }
            else
            {
                Debug.LogWarning("✗ dateSelector为null");
            }
            
            // 设置默认车间工区显示
            Debug.Log($"workAreaSelector状态: {workAreaSelector != null}");
            if (workAreaSelector != null)
            {
                Debug.Log($"workAreaSelector.selectedDisplayText状态: {workAreaSelector.selectedDisplayText != null}");
                if (workAreaSelector.selectedDisplayText != null)
                {
                    workAreaSelector.selectedDisplayText.text = "1车间1工区";
                    Debug.Log("✓ 设置默认车间工区显示: 1车间1工区");
                }
                else
                {
                    Debug.LogWarning("✗ workAreaSelector.selectedDisplayText为null");
                }
            }
            else
            {
                Debug.LogWarning("✗ workAreaSelector为null - 尝试全局查找...");
                // 尝试重新查找
                workAreaSelector = FindObjectOfType<WorkAreaSelector>();
                if (workAreaSelector != null)
                {
                    Debug.Log($"✓ 重新找到WorkAreaSelector: {workAreaSelector.name}");
                    if (workAreaSelector.selectedDisplayText != null)
                    {
                        workAreaSelector.selectedDisplayText.text = "1车间1工区";
                        Debug.Log("✓ 设置默认车间工区显示: 1车间1工区");
                    }
                }
                else
                {
                    Debug.LogError("✗ 场景中未找到WorkAreaSelector组件 - 请在Inspector中设置引用");
                }
            }
            
            Debug.Log("=== 默认值设置完成 ===");
        }
        
        /// <summary>
        /// 初始化表格样式 - 简化版：只设置列数
        /// </summary>
        private void InitializeTableStyle()
        {
            if (m_Table == null)
            {
                Debug.LogError("m_Table为null，无法初始化表格");
                return;
            }
            
            try
            {
                Debug.Log("初始化统计报表表格 - 设置5列");
                
                // 设置5列表头
                m_Table.SetColumns(new System.Collections.Generic.List<string> { "槽号", "槽状态", "设定电压", "平均电压", "工作电压" });
                
                // 刷新表格
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                Debug.Log("✓ 统计报表表格列数设置完成：5列");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"初始化表格时出错: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 日期选择回调
        /// </summary>
        private void OnDateSelected(string date)
        {
            Debug.Log($"=== 收到日期选择事件: {date} ===");
            if (StatisticsReportManager.Instance != null)
            {
                StatisticsReportManager.Instance.SetQueryDate(date);
                // 使用Frame 72重新加载数据
                LoadFrame72DataByWorkArea();
            }
        }

        /// <summary>
        /// 槽号选择回调 - 已禁用，统计报表现在按车间工区加载所有槽号
        /// </summary>
        private void OnSlotSelected(string slotNumber)
        {
            Debug.Log($"=== 收到槽号选择事件: {slotNumber} (已禁用单槽加载) ===");
            // 统计报表现在不需要单独绑定槽号，而是按车间工区加载所有槽
            // 如果需要单独槽号操作，可以在这里添加逻辑
        }

        /// <summary>
        /// 自动加载数据（启动时调用）- 保留用于兼容
        /// </summary>
        private void AutoLoadData()
        {
            Debug.Log("=== 统计报表自动加载 (兼容方法) ===");
            LoadFrame72DataByWorkArea();
        }

        public void RefreshData()
        {
            Debug.Log("=== 手动刷新统计报表 ===");
            LoadFrame72DataByWorkArea();
        }

        /// <summary>
        /// 设置TextMeshPro字体以支持中文显示
        /// </summary>
        private void SetupTMPFont()
        {
            if (m_Table == null) return;

            // 遍历表格的所有TextMeshPro组件并设置字体
            var tmpComponents = m_Table.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmpComponents)
            {
                tmp.font = customTMPFont;
            }
        }

        /// <summary>
        /// 从选择器获取参数并加载电压数据
        /// </summary>
        private void LoadVoltageDataWithParams()
        {
            string slotNumber = null;

            // 从槽号选择器获取槽号
            if (slotNumberSelector != null)
            {
                Debug.Log($"✓ 找到SlotNumberSelector: {slotNumberSelector.name}");
                
                int val = slotNumberSelector.GetSelectedSlotNumberValue();
                if (val > 0)
                {
                    slotNumber = val.ToString();
                    Debug.Log($"✓ 获取到选中的槽号: {slotNumber}");
                }
                else
                {
                    Debug.LogWarning("✗ 未选中有效槽号，将使用默认槽号");
                }
            }
            else
            {
                Debug.LogWarning("✗ 槽号选择器未设置！");
            }

            // 如果没有获取到槽号，使用Manager的默认槽号
            if (string.IsNullOrEmpty(slotNumber) && StatisticsReportManager.Instance != null)
            {
                slotNumber = StatisticsReportManager.Instance.defaultTestSlot;
                Debug.Log($"→ 使用默认槽号: {slotNumber}");
            }

            Debug.Log($"→ 加载统计报表参数: 槽号={slotNumber ?? "无"}");

            if (StatisticsReportManager.Instance == null)
            {
                Debug.LogError("✗ StatisticsReportManager实例未找到！");
                return;
            }

            // 发起API请求 (传入槽号)
            StatisticsReportManager.Instance.FetchStatisticsReport(slotNumber);

            // 延迟3秒后填充数据（等待API返回）
            Invoke(nameof(FillVoltageTable), 3f);
        }

        /// <summary>
        /// 加载电压统计数据（公开方法，可绑定到按钮）
        /// </summary>
        public void LoadVoltageData()
        {
            LoadVoltageDataWithParams();
        }

        /// <summary>
        /// 加载电流效率统计数据
        /// </summary>
        public void LoadEfficiencyData()
        {
            if (StatisticsReportManager.Instance == null)
            {
                Debug.LogError("StatisticsReportManager实例未找到！");
                return;
            }

            string slotNumber = null;
            
            if (slotNumberSelector != null)
            {
                int val = slotNumberSelector.GetSelectedSlotNumberValue();
                if (val > 0) slotNumber = val.ToString();
            }

            StatisticsReportManager.Instance.FetchStatisticsReport(slotNumber);
            Invoke(nameof(FillEfficiencyTable), 3f);
        }

        /// <summary>
        /// 加载下料统计数据
        /// </summary>
        public void LoadFeedingData()
        {
            if (StatisticsReportManager.Instance == null)
            {
                Debug.LogError("StatisticsReportManager实例未找到！");
                return;
            }

            string slotNumber = null;
            
            if (slotNumberSelector != null)
            {
                int val = slotNumberSelector.GetSelectedSlotNumberValue();
                if (val > 0) slotNumber = val.ToString();
            }

            StatisticsReportManager.Instance.FetchStatisticsReport(slotNumber);
            Invoke(nameof(FillFeedingTable), 3f);
        }

        /// <summary>
        /// 填充电压表格
        /// </summary>
        private void FillVoltageTable()
        {
            if (m_Table != null)
            {
                StatisticsReportManager.Instance.FillVoltageTable(m_Table);
                
                // 更新字体设置
                if (customTMPFont != null)
                {
                    SetupTMPFont();
                }
            }
        }

        /// <summary>
        /// 填充电流效率表格
        /// </summary>
        private void FillEfficiencyTable()
        {
            if (m_Table != null)
            {
                StatisticsReportManager.Instance.FillEfficiencyTable(m_Table);
                
                if (customTMPFont != null)
                {
                    SetupTMPFont();
                }
            }
        }

        /// <summary>
        /// 填充下料表格
        /// </summary>
        private void FillFeedingTable()
        {
            if (m_Table != null)
            {
                StatisticsReportManager.Instance.FillFeedingTable(m_Table);
                
                if (customTMPFont != null)
                {
                    SetupTMPFont();
                }
            }
        }
        void OnDestroy()
        {
            if (dateSelector != null)
            {
                dateSelector.OnDateConfirmed.RemoveListener(OnDateSelected);
            }
            
            if (slotNumberSelector != null)
            {
                slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotSelected);
            }
            
            if (workAreaSelector != null)
            {
                workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaSelected);
            }
        }
    }
}
