using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XCharts.Runtime.UI;
using XCharts.Runtime;
using System.Linq;

#if dUI_TextMeshPro
using TMPro;
#if UNITY_2021_2_OR_NEWER
using UnityEngine.TextCore.LowLevel;
#endif
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XCharts.UI.Demo
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class SingleSlotTemperatureAlarmTable : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private SingleSlotTemperatureAlarmHistoryManager singleSlotAlarmManager; // 单槽预警历史管理器
        [SerializeField] private UITable m_Table; // XChart表格组件

    #if dUI_TextMeshPro
        [Header("字体（TMP）")]
        [SerializeField] private TMP_FontAsset customTMPFont; // Alibaba-PuHuiTi-Regular SDF.asset

        private static TMP_FontAsset s_RuntimeAlibabaTMPFont;
    #else
        private Font customFont; // 阿里巴巴普惠体字体（代码自动加载）
    #endif
        
        private bool isInitialized = false;
        
        // 三个数组存储解析好的单槽预警信息
        private List<string> slotNumbers = new List<string>();      // 槽号数组
        private List<string> occurTimes = new List<string>();       // 发生时间数组
        private List<string> messageContents = new List<string>();  // 信息内容数组

        void Awake()
        {
#if dUI_TextMeshPro
            // TMP 模式：优先使用 TMP_FontAsset（SDF）
            if (customTMPFont == null)
            {
                customTMPFont = Resources.Load<TMP_FontAsset>("Fonts/Alibaba-PuHuiTi-Regular SDF");
            }
            if (customTMPFont == null)
            {
                customTMPFont = Resources.Load<TMP_FontAsset>("Alibaba-PuHuiTi-Regular SDF");
            }
#if UNITY_EDITOR
            if (customTMPFont == null)
            {
                customTMPFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Alibaba-PuHuiTi-Regular SDF.asset");
            }
#endif

            if (customTMPFont == null)
            {
                Debug.LogError("❌ 未找到 TMP 字体资产：Alibaba-PuHuiTi-Regular SDF.asset");
                Debug.LogError("请在 Inspector 把 customTMPFont 指向 Assets/Fonts/Alibaba-PuHuiTi-Regular SDF.asset，或放到 Resources/Fonts 目录下以便自动加载。");
            }
            else
            {
                Debug.Log($"✅ 成功加载 TMP 字体资产：{customTMPFont.name}");
            }

            // 运行时兜底：如果静态 SDF 资产缺字，创建 Dynamic TMP 字体用于自动补字
            var runtimeFont = GetOrCreateRuntimeAlibabaTMPFont();
            if (runtimeFont != null)
            {
                customTMPFont = runtimeFont;
            }
#else
            // 非 TMP 模式：使用传统 Font
            customFont = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular");
            if (customFont == null)
            {
                customFont = Resources.Load<Font>("Alibaba-PuHuiTi-Regular");
            }

            if (customFont == null)
            {
                Debug.LogError("❌ 未找到字体文件：Alibaba-PuHuiTi-Regular");
                Debug.LogError("请确保字体文件在：Assets/Resources/Fonts/Alibaba-PuHuiTi-Regular.ttf");
            }
            else
            {
                Debug.Log($"✅ 成功加载字体：{customFont.name}");
            }
#endif
            
            // 获取UITable组件
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
            }
            
            // ✅✅✅ 关键！设置主题字体（这才是真正控制UITable字体的地方）
            if (m_Table != null && m_Table.theme != null && m_Table.theme.sharedTheme != null)
            {
                // 设置表头字体（title theme）
#if dUI_TextMeshPro
                if (customTMPFont != null)
                {
                    m_Table.theme.sharedTheme.tmpFont = customTMPFont;
                    if (m_Table.theme.sharedTheme.title != null) m_Table.theme.sharedTheme.title.fontSize = 30;
                    if (m_Table.theme.sharedTheme.common != null) m_Table.theme.sharedTheme.common.fontSize = 30;
                    m_Table.theme.sharedTheme.SyncTMPFontToSubComponent();
                    m_Table.SetAllDirty();
                    Debug.Log("👉 已设置主题 TMP 字体并重建表格！");
                }
#else
                if (customFont != null)
                {
                    if (m_Table.theme.sharedTheme.title != null)
                    {
                        m_Table.theme.sharedTheme.title.font = customFont;
                        m_Table.theme.sharedTheme.title.fontSize = 30;
                    }
                    if (m_Table.theme.sharedTheme.common != null)
                    {
                        m_Table.theme.sharedTheme.common.font = customFont;
                        m_Table.theme.sharedTheme.common.fontSize = 30;
                    }
                    m_Table.theme.sharedTheme.SyncFontToSubComponent();
                    m_Table.SetAllDirty();
                    Debug.Log("👉 主题字体配置完成！");
                }
#endif
            }
            
            // 获取单槽预警历史管理器
            if (singleSlotAlarmManager == null)
            {
                singleSlotAlarmManager = FindObjectOfType<SingleSlotTemperatureAlarmHistoryManager>();
            }
        }

#if dUI_TextMeshPro
        private static TMP_FontAsset CreateDynamicTMPFontFromTtf(Font sourceFont)
        {
            if (sourceFont == null) return null;

            // 90/10/4096 是你生成 SDF 时的参数，保持一致
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                10,
                GlyphRenderMode.SDFAA,
                4096,
                4096,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null) return null;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            return fontAsset;
        }

        private TMP_FontAsset GetOrCreateRuntimeAlibabaTMPFont()
        {
            if (s_RuntimeAlibabaTMPFont != null) return s_RuntimeAlibabaTMPFont;

            // 关键：Dynamic 需要 sourceFontFile（也就是 ttf Font）才能在运行时补字
            var ttf = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular");
            if (ttf == null)
            {
                Debug.LogWarning("⚠️ 找不到 Resources/Fonts/Alibaba-PuHuiTi-Regular.ttf，无法创建 Dynamic TMP 字体（可能仍会缺字）。");
                return null;
            }

            s_RuntimeAlibabaTMPFont = CreateDynamicTMPFontFromTtf(ttf);
            if (s_RuntimeAlibabaTMPFont != null)
            {
                Debug.Log("✅ 已创建运行时 Dynamic TMP 字体（自动补字）");
            }

            return s_RuntimeAlibabaTMPFont;
        }
#endif
        
        void Start()
        {
            // 初始化表格
            InitEmptyTable();
            
            // 延迟刷新确保字体应用
            StartCoroutine(RefreshTableAfterDelay());
        }
        
        private IEnumerator RefreshTableAfterDelay()
        {
            yield return new WaitForSeconds(0.1f);
            if (m_Table != null)
            {
                m_Table.RefreshGraph();
                Debug.Log("🔄 表格已刷新，字体应已生效");
            }
        }
        
        // 初始化空表格
        private void InitEmptyTable()
        {
            // 检查表格组件是否存在
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
                if (m_Table == null)
                {
                    Debug.LogError("无法获取UITable组件");
                    return;
                }
            }
            
            // 检查columns是否存在，确保至少有3列
            if (m_Table.columns == null || m_Table.columns.Count < 3)
            {
                Debug.LogError("表格列数不足，需要至少3列");
                return;
            }
            
            try
            {
                // ==================== 表格样式配置 ====================
                // 颜色定义
                Color32 headerBgColor = new Color32(237, 248, 243, 255);  // 淡绿色 #EDF8F3 表头背景
                Color32 headerTextColor = new Color32(51, 51, 51, 255);   // 深灰色 #333333 表头文字
                Color32 lightGreenRow = new Color32(237, 248, 243, 255);  // 淡绿色 #EDF8F3 隔行背景
                Color32 whiteRow = new Color32(255, 255, 255, 255);       // 白色 #FFFFFF 隔行背景
                Color32 dataTextColor = new Color32(51, 51, 51, 255);     // 深灰色 #333333 数据文字
                
                // ==================== 设置表头 ====================
                // 第1列：槽号 - 使用默认样式
                m_Table.columns[0].content = "槽号";
                m_Table.columns[0].width = 100;

                // ✅ 强制表头加粗（避免被默认/列样式覆盖）
                m_Table.columns[0].headerLabelStyle.show = true;
                m_Table.columns[0].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[0].headerLabelStyle.textStyle.fontSize = 30;
                m_Table.columns[0].headerLabelStyle.textStyle.color = headerTextColor;
#if dUI_TextMeshPro
                m_Table.columns[0].headerLabelStyle.textStyle.tmpFont = customTMPFont;
                m_Table.columns[0].headerLabelStyle.textStyle.tmpFontStyle = FontStyles.Bold;
#else
                m_Table.columns[0].headerLabelStyle.textStyle.font = customFont;
                m_Table.columns[0].headerLabelStyle.textStyle.fontStyle = FontStyle.Bold;
#endif
                
                // 第2列：预警时间 - 使用默认样式
                m_Table.columns[1].content = "预警时间";
                m_Table.columns[1].width = 180;

                // ✅ 强制表头加粗（避免被默认/列样式覆盖）
                m_Table.columns[1].headerLabelStyle.show = true;
                m_Table.columns[1].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[1].headerLabelStyle.textStyle.fontSize = 30;
                m_Table.columns[1].headerLabelStyle.textStyle.color = headerTextColor;
#if dUI_TextMeshPro
                m_Table.columns[1].headerLabelStyle.textStyle.tmpFont = customTMPFont;
                m_Table.columns[1].headerLabelStyle.textStyle.tmpFontStyle = FontStyles.Bold;
#else
                m_Table.columns[1].headerLabelStyle.textStyle.font = customFont;
                m_Table.columns[1].headerLabelStyle.textStyle.fontStyle = FontStyle.Bold;
#endif
                
                // 第3列：预警信息 - 左对齐+偏移
                m_Table.columns[2].content = "预警信息";
                m_Table.columns[2].width = 700;
                m_Table.columns[2].headerLabelStyle.show = true;
                m_Table.columns[2].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleLeft;
                m_Table.columns[2].headerLabelStyle.textStyle.fontSize = 30;
                m_Table.columns[2].headerLabelStyle.textStyle.fontStyle = FontStyle.Bold;
                m_Table.columns[2].headerLabelStyle.textStyle.color = headerTextColor;
#if dUI_TextMeshPro
                m_Table.columns[2].headerLabelStyle.textStyle.tmpFont = customTMPFont;
                m_Table.columns[2].headerLabelStyle.textStyle.tmpFontStyle = FontStyles.Bold;
#else
                m_Table.columns[2].headerLabelStyle.textStyle.font = customFont;
#endif
                m_Table.columns[2].headerLabelStyle.offset = new Vector3(200, 0, 0); // 向右偏移200像素
                
                // 隐藏多余的列
                for (int i = 3; i < m_Table.columns.Count; i++)
                {
                    m_Table.columns[i].headerLabelStyle.show = false;
                    m_Table.columns[i].width = 0;
                }
                
                // ==================== 设置表头背景和高度 ====================
                if (m_Table.columnHeaderStyle != null)
                {
                    m_Table.columnHeaderStyle.show = true;
                    m_Table.columnHeaderStyle.height = 75; // 增加到1.5倍（50 * 1.5 = 75）
                    m_Table.columnHeaderStyle.backgroundColor = headerBgColor;
                    
                    // 🔥 设置默认表头样式（关键！）
                    m_Table.columnHeaderStyle.defalutLabelStyle.show = true;
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.fontSize = 30;
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.fontStyle = FontStyle.Bold;
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.color = headerTextColor;
#if dUI_TextMeshPro
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.tmpFont = customTMPFont;
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.tmpFontStyle = FontStyles.Bold;
#else
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.font = customFont;
#endif
                    m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                }
                
                // ==================== 设置数据列样式 ====================
                // 🔥 设置默认数据样式
                if (m_Table.columnStyle != null)
                {
                    m_Table.columnStyle.defaultLabelStyle.show = true;
                    m_Table.columnStyle.defaultLabelStyle.textStyle.fontSize = 30;
                    m_Table.columnStyle.defaultLabelStyle.textStyle.color = dataTextColor;
#if dUI_TextMeshPro
                    m_Table.columnStyle.defaultLabelStyle.textStyle.tmpFont = customTMPFont;
                    m_Table.columnStyle.defaultLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                    m_Table.columnStyle.defaultLabelStyle.textStyle.font = customFont;
#endif
                    m_Table.columnStyle.defaultLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                }
                
                // 只对预警信息列设置特殊样式
                for (int i = 0; i < 3; i++)
                {
                    var column = m_Table.columns[i];
                    if (column == null) continue;
                    
                    if (i == 2) // 预警信息列左对齐（不偏移）
                    {
                        column.dataLabelStyle.show = true;
                        column.dataLabelStyle.textStyle.alignment = TextAnchor.MiddleLeft;
                        column.dataLabelStyle.textStyle.fontSize = 30;
                        column.dataLabelStyle.textStyle.color = dataTextColor;
#if dUI_TextMeshPro
                        column.dataLabelStyle.textStyle.tmpFont = customTMPFont;
                        column.dataLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                        column.dataLabelStyle.textStyle.font = customFont;
#endif
                        // 数据行不偏移，只有表头“预警信息”四个字偏移
                    }
                }
                
                // ==================== 设置行样式（隔行变色）====================
                if (m_Table.rowStyle != null)
                {
                    m_Table.rowStyle.rowHeight = 90;  // 行高增加到3倍
                    m_Table.rowStyle.minRowHeight = 90;
                    
                    // 设置隔行背景颜色：白色和淡绿色交替
                    m_Table.rowStyle.rowBackgroundColors.Clear();
                    m_Table.rowStyle.rowBackgroundColors.Add(whiteRow);       // 第1行白色
                    m_Table.rowStyle.rowBackgroundColors.Add(lightGreenRow); // 第2行淡绿色
                }
                
                // ==================== 设置分隔线 ====================
                if (m_Table.separatorStyle != null)
                {
                    m_Table.separatorStyle.show = true;
                    m_Table.separatorStyle.horizontal = true;
                    m_Table.separatorStyle.horizontalWidth = 1;
                    m_Table.separatorStyle.horizontalColor = new Color32(230, 230, 230, 255);
                    m_Table.separatorStyle.vertical = true;
                    m_Table.separatorStyle.verticalWidth = 1;
                    m_Table.separatorStyle.verticalColor = new Color32(230, 230, 230, 255);
                }
                
                // ==================== 设置边框（圆角）====================
                if (m_Table.borderStyle != null)
                {
                    m_Table.borderStyle.show = true;
                    m_Table.borderStyle.borderWidth = 2;
                    m_Table.borderStyle.borderColor = new Color32(220, 220, 220, 255);
                    m_Table.borderStyle.roundedCorner = true; // 启用圆角
                    m_Table.borderStyle.cornerRadius = new float[] { 25, 25, 25, 25 }; // 圆角半径25px（更圆）
                }
                
                // 清空所有数据行
                m_Table.ClearData();
                
                // 刷新表格
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                isInitialized = true;
                Debug.Log("单槽温度预警表格初始化成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"初始化单槽温度预警表格时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 从单槽预警历史管理器加载数据
        // 从单槽预警历史管理器加载数据
        public void LoadSingleSlotAlarmDataFromManager()
        {
            Debug.Log("=== LoadSingleSlotAlarmDataFromManager 开始执行 ===");
            
            // 确保表格已初始化
            if (!isInitialized)
            {
                Debug.Log("表格未初始化，开始初始化...");
                InitEmptyTable();
            }
            
            if (singleSlotAlarmManager == null)
            {
                Debug.LogError("单槽预警历史管理器未设置");
                // 尝试重新查找
                singleSlotAlarmManager = FindObjectOfType<SingleSlotTemperatureAlarmHistoryManager>();
                if (singleSlotAlarmManager == null)
                {
                    Debug.LogError("场景中找不到 SingleSlotTemperatureAlarmHistoryManager 组件");
                    ShowNoDataMessage();
                    return;
                }
                else
                {
                    Debug.Log("重新找到了 SingleSlotTemperatureAlarmHistoryManager 组件");
                    // 设置双向引用
                    singleSlotAlarmManager.SetSingleSlotAlarmTable(this);
                }
            }
            
            Debug.Log($"singleSlotAlarmManager 状态: {singleSlotAlarmManager != null}");
            
            // 检查是否有数据
            if (!singleSlotAlarmManager.HasAlarmData())
            {
                Debug.LogWarning("没有可用的单槽预警历史数据");
                ShowNoDataMessage();
                return;
            }
            
            // 获取表格格式的数据
            var tableData = singleSlotAlarmManager.GetTableDataForSingleSlot();
            
            if (tableData == null || tableData.Count == 0)
            {
                Debug.LogWarning("表格数据为空");
                ShowNoDataMessage();
                return;
            }
            
            // 填充表格
            FillTableWithAlarmData(tableData);
        }
        // 新增：清除表格数据的方法


        // 解析数据并填充表格
        // 新增：使用AlarmTableInfo数据填充表格
private void FillTableWithAlarmData(List<SingleSlotTemperatureAlarmHistoryManager.AlarmTableInfo> alarmData)
{
    Debug.Log($"开始填充表格，数据条数: {alarmData.Count}");
    
    // 清空现有数据
    m_Table.ClearData();
    
    // 添加数据行
    foreach (var alarm in alarmData)
    {
        // 添加新行
        m_Table.AddRow(new string[3]);
        
        // 获取最新添加的行
        var row = m_Table.data[m_Table.data.Count - 1];
        
        // 确保行中有足够的单元格
        while (row.data.Count < 3)
        {
            row.data.Add(new TableCell());
        }
        
        // 填充数据
        row.data[0].stringValue = alarm.slotNumber;    // 槽号
        row.data[1].stringValue = alarm.occurTime;     // 发生时间
        row.data[2].stringValue = alarm.messageContent; // 信息内容
    }
    
    // 刷新表格显示
    m_Table.RefreshGraph();
    m_Table.RefreshTable();
    
    Debug.Log($"表格填充完成，共 {alarmData.Count} 条数据");
}
        
        // 构建信息内容
        private string BuildMessageContent(SingleSlotTemperatureAlarmHistoryManager.TemperatureAlarmHistoryInfo alarmInfo)
        {
            string location = alarmInfo.GetLocationDescription();
            string nodeDesc = alarmInfo.nodeIndex > 0 ? $"的{alarmInfo.nodeIndex}号节点" : "";
            string reason = alarmInfo.GetAlarmReasonDescription();
            
            return $"{location}{nodeDesc}{reason}";
        }
        
        // 将时间戳转换为时间字符串
        private string ConvertTimeToString(int timeValue)
        {
            try
            {
                // 假设时间格式为 HHMMSS
                int hours = timeValue / 10000;
                int minutes = (timeValue % 10000) / 100;
                int seconds = timeValue % 100;
                
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            catch
            {
                return timeValue.ToString();
            }
        }
        
        // 填充表格数据
        private void FillTableWithData()
        {
            Debug.Log("开始填充表格数据");
            
            try
            {
                // 清空现有数据
                m_Table.ClearData();
                
                // 添加数据行
                for (int i = 0; i < slotNumbers.Count; i++)
                {
                    // 改为正确的方式：
                    m_Table.AddRow(new string[3]);
                    var row = m_Table.data[m_Table.data.Count - 1];
                    
                    // 确保有足够的单元格
                    while (row.data.Count < 3)
                    {
                        row.data.Add(new TableCell());
                    }
                    
                    // 填充数据 - 使用正确的变量名
                    row.data[0].stringValue = slotNumbers[i];
                    row.data[1].stringValue = occurTimes[i];
                    row.data[2].stringValue = messageContents[i];
                    
                    Debug.Log($"添加行数据: [{slotNumbers[i]}] [{occurTimes[i]}] [{messageContents[i]}]");
                }
                
                // 刷新表格显示
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                Debug.Log($"表格填充完成，共 {slotNumbers.Count} 行数据");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"填充表格数据时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 显示无数据消息
        private void ShowNoDataMessage()
        {
            Debug.Log("显示无数据消息");
            
            try
            {
                // 清空现有数据
                m_Table.ClearData();
                
                // 添加一行提示信息 - 使用正确的AddRow方法
                m_Table.AddRow(new string[3]);
                var row = m_Table.data[0];
                
                // 确保有足够的单元格
                while (row.data.Count < 3)
                {
                    row.data.Add(new TableCell());
                }
                
                // 填充无数据提示
                row.data[0].stringValue = "";
                row.data[1].stringValue = "暂无数据";
                row.data[2].stringValue = "请检查查询条件或稍后重试";
                
                // 刷新表格显示
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                Debug.Log("无数据消息显示完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示无数据消息时出错: {e.Message}");
            }
        }
        
        // 清空表格数据
        public void ClearTableData()
        {
            Debug.Log("清空表格数据");
            
            try
            {
                // 清空数据数组
                slotNumbers.Clear();
                occurTimes.Clear();
                messageContents.Clear();
                
                // 清空表格
                if (m_Table != null)
                {
                    m_Table.ClearData();
                    m_Table.RefreshGraph();
                    m_Table.RefreshTable();
                }
                
                Debug.Log("表格数据清空完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"清空表格数据时出错: {e.Message}");
            }
        }
        
        // 获取当前表格数据（供外部调用）
        public (List<string> slots, List<string> times, List<string> contents) GetCurrentTableData()
        {
            return (new List<string>(slotNumbers), new List<string>(occurTimes), new List<string>(messageContents));
        }
        
        // 手动刷新表格（供外部调用）
        public void RefreshTable()
        {
            if (m_Table != null)
            {
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                Debug.Log("手动刷新表格完成");
            }
        }
        
        // 设置单槽预警历史管理器引用（供外部调用）
        public void SetSingleSlotAlarmManager(SingleSlotTemperatureAlarmHistoryManager manager)
        {
            singleSlotAlarmManager = manager;
            Debug.Log("设置单槽预警历史管理器引用完成");
        }
        
        // 获取表格组件引用（供外部调用）
        public UITable GetTableComponent()
        {
            return m_Table;
        }
        
        // 在编辑器中的调试方法
        [ContextMenu("测试加载单槽数据")]
        private void TestLoadSingleSlotData()
        {
            LoadSingleSlotAlarmDataFromManager();
        }
        
        [ContextMenu("测试清空表格")]
        private void TestClearTable()
        {
            ClearTableData();
        }
    }
}