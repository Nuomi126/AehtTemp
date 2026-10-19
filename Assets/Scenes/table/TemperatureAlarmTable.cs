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
    public class TemperatureAlarmTable : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private TemperatureAlarmHistoryManager alarmManager; // 预警历史管理器
        [SerializeField] private UITable m_Table; // XChart表格组件

    #if dUI_TextMeshPro
        [Header("字体（TMP）")]
        [SerializeField] private TMP_FontAsset customTMPFont; // Alibaba-PuHuiTi-Regular SDF.asset

        private static TMP_FontAsset s_RuntimeAlibabaTMPFont;
    #else
        private Font customFont; // 阿里巴巴普惠体字体（代码自动加载）
    #endif
        
        private bool isInitialized = false;
        
        // 三个数组存储解析好的预警信息
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
            
            // 获取预警历史管理器
            if (alarmManager == null)
            {
                alarmManager = FindObjectOfType<TemperatureAlarmHistoryManager>();
            }
        }

#if dUI_TextMeshPro
        private static TMP_FontAsset CreateDynamicTMPFontFromTtf(Font sourceFont)
        {
            if (sourceFont == null) return null;

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
                    m_Table.columnHeaderStyle.height = 83;  // 55 * 1.5 ≈ 83
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
                
                // ==================== 设置行样式（隔行变色、增加行距）====================
                if (m_Table.rowStyle != null)
                {
                    m_Table.rowStyle.rowHeight = 90;  // 行高增加到3倍
                    m_Table.rowStyle.minRowHeight = 90;
                    m_Table.rowStyle.rowGap = 0;  // 行间距由行高控制
                    
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
                Debug.Log("温度预警表格初始化成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"初始化温度预警表格时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 从预警历史管理器加载数据
        public void LoadAlarmDataFromManager()
        {
            Debug.Log("=== LoadAlarmDataFromManager 开始执行 ===");
            
            // 确保表格已初始化
            if (!isInitialized)
            {
                Debug.Log("表格未初始化，开始初始化...");
                InitEmptyTable();
            }
            
            if (alarmManager == null)
            {
                Debug.LogError("预警历史管理器未设置");
                // 尝试重新查找
                alarmManager = FindObjectOfType<TemperatureAlarmHistoryManager>();
                if (alarmManager == null)
                {
                    Debug.LogError("场景中找不到 TemperatureAlarmHistoryManager 组件");
                    return;
                }
                else
                {
                    Debug.Log("重新找到了 TemperatureAlarmHistoryManager 组件");
                }
            }
            
            Debug.Log($"alarmManager 状态: {alarmManager != null}");
            
            // 通过公共方法获取当前的报警历史数据
            var currentData = alarmManager.GetCurrentAlarmHistoryData();
            
            Debug.Log($"获取到的数据: {currentData != null}");
            if (currentData != null)
            {
                Debug.Log($"alarmHistories: {currentData.alarmHistories != null}");
                if (currentData.alarmHistories != null)
                {
                    Debug.Log($"数据条数: {currentData.alarmHistories.Count}");
                }
            }
            
            if (currentData == null || currentData.alarmHistories == null || currentData.alarmHistories.Count == 0)
            {
                Debug.LogWarning("没有预警数据可显示");
                // 清空表格并显示无数据提示
                m_Table.ClearData();
                AddNoDataRow();
                return;
            }
            
            try
            {
                Debug.Log($"开始加载预警数据，共 {currentData.alarmHistories.Count} 条记录");
                
                // 第一步：将解析好的预警信息放在三个数组里
                PrepareAlarmDataArrays(currentData);
                
                // 第二步：使用数组数据正确填充表格
                FillTableWithArrayData();
                
                Debug.Log($"表格更新完成，共 {m_Table.data.Count} 行，{m_Table.columns.Count} 列");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载预警数据到表格时出错: {e.Message}\n{e.StackTrace}");
                
                // 出错时显示无数据
                m_Table.ClearData();
                AddNoDataRow();
            }
            
            Debug.Log("=== LoadAlarmDataFromManager 执行完成 ===");
        }
        
        // 第一步：准备三个数组的数据
        private void PrepareAlarmDataArrays(TemperatureAlarmHistoryManager.TemperatureAlarmHistoryData currentData)
        {
            // 清空之前的数组数据
            slotNumbers.Clear();
            occurTimes.Clear();
            messageContents.Clear();
            
            Debug.Log("开始准备预警数据数组...");

            //获取的预警信息已经按发生时间排序，不需要再次排序 20250801
            var sortedAlarms = currentData.alarmHistories;
			//// 按槽号从小到大排序
			//var sortedAlarms = currentData.alarmHistories
			//    .OrderBy(alarm => alarm.slotNumber)
			//    .ToList();

			Debug.Log($"排序完成，共 {sortedAlarms.Count} 条记录");
            
            // 遍历排序后的预警记录，填充三个数组
            foreach (var alarm in sortedAlarms)
            {
                try
                {
                    // 槽号
                    string slotNumber = alarm.slotNumber.ToString();
                    slotNumbers.Add(slotNumber);
                    
                    // 发生时间
                    string occurTime = alarm.GetTimeOnly();
                    occurTimes.Add(occurTime);
                    
                    // 信息内容
                    string messageContent = alarm.GetDetailedMessageContent();
                    messageContents.Add(messageContent);
                    
                    Debug.Log($"数组数据 - 槽号: {slotNumber}, 时间: {occurTime}, 内容: {messageContent}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"准备数组数据时出错: {e.Message}");
                    // 添加默认值以保持数组长度一致
                    slotNumbers.Add("--");
                    occurTimes.Add("--:--:--");
                    messageContents.Add("数据解析错误");
                }
            }
            
            Debug.Log($"数组准备完成 - 槽号数组: {slotNumbers.Count}, 时间数组: {occurTimes.Count}, 内容数组: {messageContents.Count}");
            
            // 验证三个数组长度是否一致
            if (slotNumbers.Count != occurTimes.Count || occurTimes.Count != messageContents.Count)
            {
                Debug.LogError($"数组长度不一致！槽号: {slotNumbers.Count}, 时间: {occurTimes.Count}, 内容: {messageContents.Count}");
            }
        }
        
        // 第二步：使用数组数据填充表格
        private void FillTableWithArrayData()
        {
            Debug.Log("开始使用数组数据填充表格...");
            
            // 清空现有数据
            m_Table.ClearData();
            
            // 检查数组是否有数据
            if (slotNumbers.Count == 0)
            {
                Debug.LogWarning("数组为空，无法填充表格");
                AddNoDataRow();
                return;
            }
            
            // 创建所有需要的行
            int rowCount = slotNumbers.Count;
            for (int i = 0; i < rowCount; i++)
            {
                m_Table.AddRow(new string[3]);
            }
            
            Debug.Log($"已创建 {rowCount} 行，开始填充数据...");
            
            // 逐行填充数据
            for (int i = 0; i < rowCount; i++)
            {
                try
                {
                    var row = m_Table.data[i];
                    
                    // 确保每行有足够的单元格（3列）
                    while (row.data.Count < 3)
                    {
                        row.data.Add(new TableCell());
                    }
                    
                    // 从数组中获取数据并填充
                    string slotNumber = i < slotNumbers.Count ? slotNumbers[i] : "--";
                    string occurTime = i < occurTimes.Count ? occurTimes[i] : "--:--:--";
                    string messageContent = i < messageContents.Count ? messageContents[i] : "数据缺失";
                    
                    // 使用stringValue填充数据
                    row.data[0].stringValue = slotNumber;      // 槽号
                    row.data[1].stringValue = occurTime;       // 发生时间
                    row.data[2].stringValue = messageContent;  // 信息内容
                    
                    Debug.Log($"第 {i + 1} 行填充完成: [{slotNumber}] [{occurTime}] [{messageContent}]");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"填充第 {i + 1} 行数据时出错: {e.Message}");
                    
                    // 填充默认值
                    var row = m_Table.data[i];
                    while (row.data.Count < 3)
                    {
                        row.data.Add(new TableCell());
                    }
                    row.data[0].stringValue = "--";
                    row.data[1].stringValue = "--:--:--";
                    row.data[2].stringValue = "填充错误";
                }
            }
            
            // 刷新表格
            Debug.Log("开始刷新表格显示...");
            m_Table.RefreshGraph();
            m_Table.RefreshTable();
            
            // 强制刷新Canvas
            Canvas.ForceUpdateCanvases();
            
            Debug.Log($"表格填充完成！总行数: {m_Table.data.Count}");
        }
        
        // 添加无数据提示行
        private void AddNoDataRow()
        {
            try
            {
                m_Table.AddRow(new string[0]);
                var row = m_Table.data[0];
                
                // 确保有足够的单元格
                while (row.data.Count < 3)
                {
                    row.data.Add(new TableCell());
                }
                
                row.data[0].stringValue = "--";
                row.data[1].stringValue = "--:--:--";
                row.data[2].stringValue = "暂无预警数据";
                
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"添加无数据提示行时出错: {e.Message}");
            }
        }
        
        // 手动刷新数据（可通过按钮调用）
        public void RefreshAlarmData()
        {
            Debug.Log("手动刷新预警表格数据");
            LoadAlarmDataFromManager();
        }
        
        // 清空表格数据
        public void ClearTableData()
        {
            if (m_Table != null)
            {
                m_Table.ClearData();
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                // 同时清空数组
                slotNumbers.Clear();
                occurTimes.Clear();
                messageContents.Clear();
                
                Debug.Log("已清空预警表格数据和数组");
            }
        }
        
        // 调试方法：打印数组内容
        [ContextMenu("打印数组内容")]
        public void PrintArrayContents()
        {
            Debug.Log("=== 预警数据数组内容 ===");
            Debug.Log($"数组长度 - 槽号: {slotNumbers.Count}, 时间: {occurTimes.Count}, 内容: {messageContents.Count}");
            
            for (int i = 0; i < slotNumbers.Count; i++)
            {
                string slot = i < slotNumbers.Count ? slotNumbers[i] : "N/A";
                string time = i < occurTimes.Count ? occurTimes[i] : "N/A";
                string content = i < messageContents.Count ? messageContents[i] : "N/A";
                
                Debug.Log($"第 {i + 1} 条: [{slot}] [{time}] [{content}]");
            }
        }
    }
}