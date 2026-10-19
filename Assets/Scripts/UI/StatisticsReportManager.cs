using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 添加UI命名空间用于Button
using XCharts.Runtime.UI;
using XCharts.Runtime;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.Networking;
using System;
using TMPro; // 引入TextMeshPro命名空间
using XCharts.UI.Demo; // 引入StatisticsReportTable所在的命名空间

#if dUI_TextMeshPro && UNITY_2021_2_OR_NEWER
using UnityEngine.TextCore.LowLevel; // TextMeshPro的GlyphRenderMode等类型
#endif

/// <summary>
/// 统计报表管理器
/// 负责获取统计报表数据并填充表格
/// Updated: 2026-01-14 - Enforced Frame 71 and Singleton
/// </summary>
public class StatisticsReportManager : MonoBehaviour
{
    #region 单例模式
    private static StatisticsReportManager _instance;
    public static StatisticsReportManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StatisticsReportManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("StatisticsReportManager");
                    _instance = go.AddComponent<StatisticsReportManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Inspector 配置
    [Header("表格组件引用")]
    [SerializeField] private UITable voltageTable;      // 电压表格
    [SerializeField] private UITable efficiencyTable;   // 效应表格
    [SerializeField] private UITable feedingTable;      // 下料表格
    [SerializeField] private UITable needleTable;       // 针摆表格
    [SerializeField] private UITable processTable;      // 工艺表格
    
    [Header("Tab按钮引用")]
    [SerializeField] private Button btnDianya;          // 电压按钮
    [SerializeField] private Button btnXiaoying;        // 效应按钮
    [SerializeField] private Button btnXialiao;         // 下料按钮
    [SerializeField] private Button btnZhenbai;         // 针摆按钮
    [SerializeField] private Button btnGongyi;          // 工艺按钮
    
    [Header("按钮颜色")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 1f);
    
    [Header("字体设置")]
    [SerializeField] public TMP_FontAsset tableFont;    // 用于显示中文的字体
    
    // 自动查找UI引用
    private void Awake()
    {
        // 优先使用配置好的 tongjiManager 作为单例
        if (gameObject.name == "tongjiManager")
        {
            // 如果已有其他实例，销毁旧实例，使用 tongjiManager
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[StatisticsReportManager] 销毁旧实例 ({_instance.gameObject.name})，使用 tongjiManager");
                Destroy(_instance.gameObject);
            }
            _instance = this;
            Debug.Log("[StatisticsReportManager] ✓ tongjiManager 成为单例实例");
        }
        else if (_instance == null)
        {
            // 如果还没有实例，当前对象成为单例
            _instance = this;
            Debug.LogWarning($"[StatisticsReportManager] {gameObject.name} 成为单例（警告：应该使用 tongjiManager）");
        }
        else
        {
            // 已有单例且当前不是 tongjiManager，销毁自己
            Debug.LogWarning($"[StatisticsReportManager] 检测到重复实例 ({gameObject.name})，正在销毁...");
            Destroy(gameObject);
            return;
        }
        
        // ✓ 移除 DontDestroyOnLoad，让Manager随场景加载/销毁，确保按钮引用始终正确
        // 打印当前表格引用状态
        Debug.Log($"[Awake] 表格引用状态检查:");
        Debug.Log($"  voltageTable: {(voltageTable != null ? voltageTable.name : "NULL")}");
        Debug.Log($"  efficiencyTable: {(efficiencyTable != null ? efficiencyTable.name : "NULL")}");
        Debug.Log($"  feedingTable: {(feedingTable != null ? feedingTable.name : "NULL")}");
        Debug.Log($"  needleTable: {(needleTable != null ? needleTable.name : "NULL")}");
        Debug.Log($"  processTable: {(processTable != null ? processTable.name : "NULL")}");
        
        // 自动查找缺失的表格引用
        if (voltageTable == null)
        {
            var go = GameObject.Find("UITable_Voltage");
            if (go == null) go = GameObject.Find("Table_dy");
            if (go != null)
            {
                voltageTable = go.GetComponent<UITable>();
                Debug.Log($"  ✓ 自动找到 voltageTable: {go.name}");
            }
        }
        
        if (efficiencyTable == null)
        {
            var go = GameObject.Find("UITable_Effect");
            if (go == null) go = GameObject.Find("Table_xy");
            if (go != null)
            {
                efficiencyTable = go.GetComponent<UITable>();
                Debug.Log($"  ✓ 自动找到 efficiencyTable: {go.name}");
            }
            else
            {
                Debug.LogError("  ✗ 找不到效应表格 UITable_Effect!");
            }
        }
        
        if (feedingTable == null)
        {
            var go = GameObject.Find("UITable_Feeding");
            if (go == null) go = GameObject.Find("Table_xl");
            if (go != null)
            {
                feedingTable = go.GetComponent<UITable>();
                Debug.Log($"  ✓ 自动找到 feedingTable: {go.name}");
            }
        }
        
        if (needleTable == null)
        {
            var go = GameObject.Find("UITable_Needle");
            if (go != null)
            {
                needleTable = go.GetComponent<UITable>();
                Debug.Log($"  ✓ 自动找到 needleTable: {go.name}");
            }
        }
        
        if (processTable == null)
        {
            var go = GameObject.Find("UITable_Process");
            if (go != null)
            {
                processTable = go.GetComponent<UITable>();
                Debug.Log($"  ✓ 自动找到 processTable: {go.name}");
            }
        }
        // 尝试从StatisticsReportTable获取字体
        var tableUI = FindObjectOfType<StatisticsReportTable>();
        if (tableUI != null)
        {
            // 通过反射获取私有字段 customTMPFont (如果需要)，或者假设它已经赋值给了我们
            // 这里我们尝试查找场景中所有字体，找到一个包含中文字符的
        }

        // 字体回退策略
        if (tableFont == null || tableFont.name.Contains("Alibaba") || tableFont.name.Contains("Liberation")) 
        {
            // 1. 尝试查找 FangZhengShuSong (方正书宋)
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in fonts)
            {
                if (font.name.Contains("FangZheng") || font.name.Contains("msyh") || font.name.Contains("Deng"))
                {
                    tableFont = font;
                    Debug.Log($"✓ 自动切换字体为: {font.name}");
                    break;
                }
            }
        }
        
        // 如果使用Alibaba字体，创建动态TMP字体来自动补字
#if dUI_TextMeshPro
        if (tableFont != null && tableFont.name.Contains("Alibaba"))
        {
            var runtimeFont = GetOrCreateRuntimeAlibabaTMPFont();
            if (runtimeFont != null)
            {
                tableFont = runtimeFont;
                Debug.Log("✓ StatisticsReportManager: 已切换到动态TMP字体（支持自动补字）");
            }
        }
#endif
    }
    
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string electrolyticCompanyId = "1033";
    [SerializeField] public string defaultTestSlot = "1001";  // 改为public以便其他组件访问

    [Header("表格样式配置")]
    [SerializeField] private Color32 headerBgColor = new Color32(70, 130, 180, 255);
    [SerializeField] private Color32 headerTextColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 dataTextColor = new Color32(50, 50, 50, 255);
    [SerializeField] private Color32 whiteRow = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 lightGreenRow = new Color32(240, 255, 240, 255);

    [Header("调试设置")]
    [SerializeField] private bool enableDetailedDebug = true;
    
    [Header("详情面板")]
    [SerializeField] private SlotDetailPanelManager slotDetailManager;
    [SerializeField] private string currentWorkAreaName = "1车间1工区";
    #endregion

    #region 数据存储
    private StatisticsReportResponse currentReportData;
    private string queryDate; // 查询日期
    
    private enum TabType
    {
        Voltage,    // 电压
        Effect,     // 效应
        Feeding,    // 下料
        Needle,     // 针摆
        Process     // 工艺
    }
    
    private TabType currentTab = TabType.Voltage;
    #endregion

    #region 数据结构定义
    [System.Serializable]
    public class StatisticsReportResponse
    {
        public int total;
        public ReportData[] rows;
        public int code;
        public string msg;
        public int type;
    }

    [System.Serializable]
    public class ReportData
    {
        // 通用字段
        public int caoh; // 槽号
        public string recordTime; // 记录时间/日期
        public int caokCaozht; // 槽状态
        
        // 电压相关
        public int sheddy; // 设定电压
        public int pingjdy; // 平均电压
        public int gongzdy; // 工作电压
        public int xiedl; // 系列电流
        public int biaozdy; // 标准电压（电阻）
        
        // 针摆相关
        public int zhenzhen; // 针振
        public int baidong; // 摆动
        public int zonggonghao; // 总功耗
        public int baidongchixu; // 摆动持续
        public int dianyayichangchixu; // 电压异常持续
        
        // 下料相关
        public int xialiaojiange; // 基准下料间隔
        public int xialiaocishu; // 下料次数
        public int xialiaoliang; // 下料量
        public int qianliang; // 欠量下料次数
        public int guoliang; // 过量下料次数
        public int fuyanxialiao; // 氟盐下料次数
        
        // 效应相关
        public int xiaoyingcishu; // 效应次数
        public int shanshuxiaoying; // 闪烁效应基准
        public int xiaoyingchixu; // 效应持续时间
        public int xiaoyingjuny; // 效应均压
        public int xiaoyinggonghao; // 效应功耗
        
        // 工艺相关（需要从其他帧获取，暂时预留）
        public float wendu; // 温度
        public float lvshupingmian; // 铝水平
        public float dianjiezhipingmian; // 电解质水平
        public float fenzibi; // 分子比
        public float tiehanliang; // 铁含量
        public float chulvliang; // 出铝量

        public string FormatTime(int seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        public string GetStatusText()
        {
             switch (caokCaozht)
             {
                 case 0: return "联机";
                 case 1: return "停槽";
                 case 2: return "降流";
                 case 3: return "启动";
                 default: return "未知";
             }
        }

        public string FormatVoltage(int value)
        {
             return (value / 1000.0f).ToString("F3");
        }
    }
    #endregion

    #region Unity 生命周期
    private void Start()
    {
        // 初始化
        if (string.IsNullOrEmpty(queryDate))
        {
            queryDate = DateTime.Now.ToString("yyyyMMdd");
        }
        
        // 配置所有表格的横向滚动
        ConfigureTableScroll();
        
        // 初始化Tab按钮（在最后执行）
        InitTabButtons();
        
        // 默认显示电压表格，但不填充数据（等待用户点击查询按钮）
        SwitchTab(TabType.Voltage, fillData: false);
        
        // 绑定表格点击事件
        BindTableClickEvents();
    }
    
    /// <summary>
    /// 绑定所有表格的点击事件
    /// </summary>
    private void BindTableClickEvents()
    {
        if (voltageTable != null)
            voltageTable.onTableClick = OnTableRowClicked;
        if (efficiencyTable != null)
            efficiencyTable.onTableClick = OnTableRowClicked;
        if (feedingTable != null)
            feedingTable.onTableClick = OnTableRowClicked;
        if (needleTable != null)
            needleTable.onTableClick = OnTableRowClicked;
        if (processTable != null)
            processTable.onTableClick = OnTableRowClicked;

        Debug.Log("[StatisticsReportManager] ✓ 表格点击事件已绑定");
    }

    /// <summary>
    /// 表格行点击回调
    /// </summary>
    private void OnTableRowClicked(int row, int column, TableCell cell)
    {
        // row为-1表示点击了表头，忽略
        if (row < 0)
        {
            Debug.Log("[StatisticsReportManager] 点击了表头，忽略");
            return;
        }

        // 检查数据是否存在
        if (currentReportData == null || currentReportData.rows == null)
        {
            Debug.LogWarning("[StatisticsReportManager] 没有数据！");
            return;
        }

        if (row >= currentReportData.rows.Length)
        {
            Debug.LogWarning($"[StatisticsReportManager] 无效的行索引: {row}");
            return;
        }

        // 获取点击行对应的槽数据
        var slotData = currentReportData.rows[row];
        Debug.Log($"[StatisticsReportManager] 点击了第 {row} 行，槽号: {slotData.caoh}");

        // 显示详情面板
        if (slotDetailManager != null)
        {
            // 格式化日期范围
            string dateRange = FormatDateRange(queryDate);
            slotDetailManager.ShowSlotDetail(slotData, currentWorkAreaName, dateRange);
        }
        else
        {
            Debug.LogError("[StatisticsReportManager] ✗ SlotDetailPanelManager 未配置！");
        }
    }

    /// <summary>
    /// 格式化日期显示
    /// </summary>
    private string FormatDateRange(string date)
    {
        if (string.IsNullOrEmpty(date) || date.Length < 8)
            return date;
        
        // 将 20240701 转换为 2024.07.01
        return $"{date.Substring(0, 4)}.{date.Substring(4, 2)}.{date.Substring(6, 2)}";
    }
    
    /// <summary>
    /// 配置表格横向滚动
    /// </summary>
    private void ConfigureTableScroll()
    {
        ConfigureSingleTable(voltageTable);
        ConfigureSingleTable(efficiencyTable);
        ConfigureSingleTable(feedingTable);
        ConfigureSingleTable(needleTable);
        ConfigureSingleTable(processTable);
    }
    
    /// <summary>
    /// 配置单个表格的滚动条
    /// </summary>
    private void ConfigureSingleTable(UITable table)
    {
        if (table == null) return;
        
        // 启用滚动支持
        table.supportScroll = true;
        
        // 配置滚动条样式
        if (table.scrollbarStyle != null)
        {
            table.scrollbarStyle.show = true;
            table.scrollbarStyle.horizontal = true;  // 启用横向滚动
            table.scrollbarStyle.vertical = true;    // 启用纵向滚动
            table.scrollbarStyle.width = 10f;        // 滚动条宽度
        }
        
        // 设置滚动灵敏度
        table.scrollSensitivity = 10f;
        
        // 设置固定列宽
        if (table.columnStyle != null)
        {
            if (table == efficiencyTable)
            {
                table.columnStyle.columnWidth = 120f; // 效应报表列宽设为120
            }
            else
            {
                table.columnStyle.columnWidth = 160f;
            }
        }
        
        Debug.Log($"✓ 已配置表格 {table.name} 的横向滚动和列宽(160)");
    }
    
    /// <summary>
    /// 根据当前Tab填充对应表格
    /// </summary>
    private void FillCurrentTabTable()
    {
        Debug.Log($"[FillCurrentTabTable] 根据当前Tab({currentTab})填充表格");
        
        switch (currentTab)
        {
            case TabType.Voltage:
                FillVoltageTable();
                UpdateTableFont(voltageTable);
                break;
            case TabType.Effect:
                FillEffectTable();
                UpdateTableFont(efficiencyTable);
                break;
            case TabType.Feeding:
                FillFeedingTable();
                UpdateTableFont(feedingTable);
                break;
            case TabType.Needle:
                FillNeedleTable(needleTable);
                UpdateTableFont(needleTable);
                break;
            case TabType.Process:
                FillProcessTable(processTable);
                UpdateTableFont(processTable);
                break;
        }
    }
    #endregion

    #region 字体处理 - 动态TMP字体
#if dUI_TextMeshPro
    private static TMP_FontAsset s_RuntimeAlibabaTMPFont;

    /// <summary>
    /// 创建动态TMP字体（自动补字）
    /// </summary>
    private static TMP_FontAsset CreateDynamicTMPFontFromTtf(Font sourceFont)
    {
        if (sourceFont == null) return null;

        // 90/10/4096 是生成 SDF 时的参数，保持一致
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

    /// <summary>
    /// 获取或创建运行时动态Alibaba TMP字体
    /// </summary>
    private TMP_FontAsset GetOrCreateRuntimeAlibabaTMPFont()
    {
        if (s_RuntimeAlibabaTMPFont != null) return s_RuntimeAlibabaTMPFont;

        // 关键：Dynamic 需要 sourceFontFile（也就是 ttf Font）才能在运行时补字
        var ttf = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular");
        if (ttf == null)
        {
            ttf = Resources.Load<Font>("Alibaba-PuHuiTi-Regular");
        }
        
        if (ttf == null)
        {
            Debug.LogWarning("⚠️ 找不到 Resources/Fonts/Alibaba-PuHuiTi-Regular.ttf，无法创建 Dynamic TMP 字体（可能仍会缺字）。");
            return null;
        }

        s_RuntimeAlibabaTMPFont = CreateDynamicTMPFontFromTtf(ttf);
        if (s_RuntimeAlibabaTMPFont != null)
        {
            Debug.Log("✅ StatisticsReportManager: 已创建运行时 Dynamic TMP 字体（自动补字）");
        }

        return s_RuntimeAlibabaTMPFont;
    }
#endif
    #endregion

    #region 公开方法 - API调用
    /// <summary>
    /// 设置查询日期
    /// </summary>
    public void SetQueryDate(string date)
    {
        queryDate = date;
    }

    /// <summary>
    /// 获取统计报表数据（Frame 71 - 历史曲线）
    /// </summary>
    /// <param name="slotNumber">槽号（可选）</param>
    public void FetchStatisticsReport(string slotNumber = null)
    {
        Debug.Log($"[FetchStatisticsReport] 方法被调用，槽号参数: {slotNumber ?? "null"}");
        StartCoroutine(FetchStatisticsReportAsync(slotNumber));
    }

    private System.Collections.IEnumerator FetchStatisticsReportAsync(string slotId)
    {
        Debug.Log($"[协程开始] 准备发起API请求...");
        
        string enterpriseId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? GlobalUserData.AlSerialPantId : electrolyticCompanyId;
        
        if (string.IsNullOrEmpty(queryDate))
        {
            queryDate = DateTime.Now.ToString("yyyyMMdd");
        }

        string slotNumber = !string.IsNullOrEmpty(slotId) ? slotId : defaultTestSlot;
        
        // Frame 71: 企业ID-71-槽号-日期
        string requestParam = $"{enterpriseId}-71-{slotNumber}-{queryDate}";
        
        string jsonData = $"\"{requestParam}\"";
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

        if (enableDetailedDebug)
        {
            Debug.Log($"=== 获取统计报表 (Frame 71) ===");
            Debug.Log($"请求参数: {requestParam}");
            Debug.Log($"JSON格式: {jsonData}");
        }

        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            
            if (enableDetailedDebug)
            {
                Debug.Log($"✓ HTTP响应码: {webRequest.responseCode}");
                Debug.Log($"✓ 响应长度: {responseText.Length} 字符");
                
                // 输出前200字符看看实际内容
                if (responseText.Length > 0)
                {
                    string preview = responseText.Length > 200 ? responseText.Substring(0, 200) + "..." : responseText;
                    Debug.Log($"响应内容预览: {preview}");
                }
                else
                {
                    Debug.LogError("⚠ 响应为空字符串！");
                }
            }
            
            try
            {
                byte[] data = ParseBase64Response(responseText);
                
                if (data != null && data.Length > 0)
                {
                    Debug.Log($"✓ Base64解码: {data.Length} 字节");
                    List<ReportData> reports = ParseFrame71Data(data);
                    
                    if (reports.Count > 0)
                    {
                        currentReportData = new StatisticsReportResponse
                        {
                            total = reports.Count,
                            rows = reports.ToArray(),
                            code = 200,
                            msg = "Success"
                        };
                        
                        Debug.Log($"✓ 解析完成: {reports.Count} 条记录");
                        
                        // 根据当前Tab填充对应表格
                        FillCurrentTabTable();
                    }
                    else
                    {
                        Debug.LogError("✗ 未解析到任何记录");
                    }
                }
                else
                {
                    Debug.LogError($"✗ Base64解码失败或数据为空，原始响应长度: {responseText.Length}");
                    if (responseText.Length <= 50)
                    {
                        Debug.LogError($"完整响应内容: [{responseText}]");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 解析异常: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"✗ 请求失败 - HTTP状态码: {webRequest.responseCode}");
            Debug.LogError($"✗ 错误信息: {webRequest.error}");
            Debug.LogError($"✗ 响应文本: {webRequest.downloadHandler?.text ?? "null"}");
        }
        
        webRequest.Dispose();
    }

    private byte[] ParseBase64Response(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogError("响应内容为空");
            return null;
        }

        try
        {
            string cleanResponse = response.Trim('"');
            
            if (enableDetailedDebug)
            {
                Debug.Log($"Base64字符串长度: {cleanResponse.Length}");
            }
            
            int paddingCount = cleanResponse.Length % 4;
            if (paddingCount > 0)
            {
                cleanResponse += new string('=', 4 - paddingCount);
            }

            return System.Convert.FromBase64String(cleanResponse);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Base64转换失败: {ex.Message}");
            return null;
        }
    }

    private List<ReportData> ParseFrame71Data(byte[] fullData)
    {
        List<ReportData> list = new List<ReportData>();
        
        if (fullData.Length < 14)
        {
            Debug.LogError($"数据帧长度不足：{fullData.Length} 字节");
            return list;
        }
        
        // 验证帧头 170, 85
        if (fullData[0] != 170 || fullData[1] != 85)
        {
            Debug.LogError($"帧头错误：[{fullData[0]},{fullData[1]}]，期望[170,85]");
            return list;
        }
        
        // 验证帧尾 165, 90
        if (fullData[fullData.Length - 2] != 165 || fullData[fullData.Length - 1] != 90)
        {
            Debug.LogError($"帧尾错误：[{fullData[fullData.Length - 2]},{fullData[fullData.Length - 1]}]，期望[165,90]");
            return list;
        }
        
        // 解析帧头信息
        int offset = 2;
        ushort companyId = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        ushort frameType = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        ushort cellNumber = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        uint dataLength = (uint)((fullData[offset] << 24) | (fullData[offset + 1] << 16) | (fullData[offset + 2] << 8) | fullData[offset + 3]);
        offset += 4;
        
        if (enableDetailedDebug)
        {
            Debug.Log($"✓ 帧头验证通过");
            Debug.Log($"  企业ID: {companyId}");
            Debug.Log($"  帧类型: {frameType}");
            Debug.Log($"  槽号: {cellNumber}");
            Debug.Log($"  数据长度: {dataLength} 字节");
        }
        
        // 提取有效数据（跳过帧尾2字节）
        byte[] validData = new byte[dataLength];
        Array.Copy(fullData, offset, validData, 0, (int)dataLength);
        
        // 解析Frame 71数据（112字节一条）
        int recordCount = validData.Length / 112;
        
        Debug.Log($"开始解析 {recordCount} 条记录...");
        
        for (int i = 0; i < recordCount; i++)
        {
            int recOffset = i * 112;
            ReportData report = new ReportData();
            
            try
            {
                // 调试：输出第一条记录的前30字节看看原始数据
                if (i == 0)
                {
                    string hexDump = "";
                    for (int j = 0; j < 30; j++)
                    {
                        hexDump += validData[recOffset + j].ToString("X2") + " ";
                    }
                    Debug.Log($"第1条记录前30字节(HEX): {hexDump}");
                    
                    // 输出各个位置的解析值
                    Debug.Log($"偏移0-1(槽号): {(validData[0] << 8) | validData[1]}");
                    Debug.Log($"偏移8-9(设定电压): {(validData[8] << 8) | validData[9]}");
                    Debug.Log($"偏移10-11(平均电压): {(validData[10] << 8) | validData[11]}");
                    Debug.Log($"偏移12-13(系列电流): {(validData[12] << 8) | validData[13]}");
                    Debug.Log($"偏移14-15(标准电压): {(validData[14] << 8) | validData[15]}");
                    Debug.Log($"偏移16-17(滤波电压): {(validData[16] << 8) | validData[17]}");
                }
                
                // 槽号 (0-1) - 大端序
                report.caoh = (validData[recOffset] << 8) | validData[recOffset + 1];
                
                // 设定电压 (8-9) - 大端序
                report.sheddy = (validData[recOffset + 8] << 8) | validData[recOffset + 9];
                
                // 平均电压 (10-11) - 大端序
                report.pingjdy = (validData[recOffset + 10] << 8) | validData[recOffset + 11];
                
                // 滤波电压作为工作电压 (14-15) - 大端序
                report.gongzdy = (validData[recOffset + 14] << 8) | validData[recOffset + 15];
                
                // 槽状态默认为联机
                report.caokCaozht = 0;
                
                if (enableDetailedDebug && i < 3)
                {
                    Debug.Log($"记录{i+1}: 槽号={report.caoh}, 设定电压={report.sheddy}, 平均电压={report.pingjdy}, 工作电压={report.gongzdy}");
                }
                
                list.Add(report);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"解析第 {i + 1} 条记录失败: {e.Message}");
            }
        }
        
        return list;
    }

    /// <summary>
    /// 解析Frame 72数据（单槽跨度日报信息 - 134字节/记录）
    /// 协议格式: 企业ID-72-槽号-起始日期-终止日期
    /// </summary>
    private List<ReportData> ParseFrame72Data(byte[] fullData)
    {
        List<ReportData> list = new List<ReportData>();
        
        if (fullData.Length < 14)
        {
            Debug.LogError($"[Frame72] 数据帧长度不足：{fullData.Length} 字节");
            return list;
        }
        
        // 验证帧头 170, 85
        if (fullData[0] != 170 || fullData[1] != 85)
        {
            Debug.LogError($"[Frame72] 帧头错误：[{fullData[0]},{fullData[1]}]，期望[170,85]");
            return list;
        }
        
        // 验证帧尾 165, 90
        if (fullData[fullData.Length - 2] != 165 || fullData[fullData.Length - 1] != 90)
        {
            Debug.LogError($"[Frame72] 帧尾错误：[{fullData[fullData.Length - 2]},{fullData[fullData.Length - 1]}]，期望[165,90]");
            return list;
        }
        
        // 解析帧头信息
        int offset = 2;
        ushort companyId = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        ushort frameType = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        ushort cellNumber = (ushort)((fullData[offset] << 8) | fullData[offset + 1]);
        offset += 2;
        uint dataLength = (uint)((fullData[offset] << 24) | (fullData[offset + 1] << 16) | (fullData[offset + 2] << 8) | fullData[offset + 3]);
        offset += 4;
        
        if (enableDetailedDebug)
        {
            Debug.Log($"[Frame72] ✓ 帧头验证通过");
            Debug.Log($"  企业ID: {companyId}");
            Debug.Log($"  帧类型: {frameType}");
            Debug.Log($"  槽号: {cellNumber}");
            Debug.Log($"  数据长度: {dataLength} 字节");
        }
        
        // 提取有效数据
        byte[] validData = new byte[dataLength];
        Array.Copy(fullData, offset, validData, 0, (int)dataLength);
        
        // Frame 72: 134字节一条记录
        int recordSize = 134;
        int recordCount = validData.Length / recordSize;
        
        Debug.Log($"[Frame72] 开始解析 {recordCount} 条记录（每条 {recordSize} 字节）...");
        
        for (int i = 0; i < recordCount; i++)
        {
            int recOffset = i * recordSize;
            ReportData report = new ReportData();
            
            try
            {
                // 调试：输出第一条记录的前40字节
                if (i == 0 && enableDetailedDebug)
                {
                    string hexDump = "";
                    for (int j = 0; j < Math.Min(40, validData.Length - recOffset); j++)
                    {
                        hexDump += validData[recOffset + j].ToString("X2") + " ";
                    }
                    Debug.Log($"[Frame72] 第1条记录前40字节(HEX): {hexDump}");
                }
                
                // Frame 72 数据结构 (134字节)详细解析:
                // 偏移 0-1:   记录号 (2字节)
                // 偏移 2-3:   槽号 (2字节) 
                // 偏移 4-7:   日期 (4字节)
                // 偏移 8-11:  时间 (4字节)
                // 偏移 12:    车间号 (1字节)
                // 偏移 13:    工区号 (1字节)
                // 偏移 14-15: 设定电压 (2字节)
                // 偏移 16-17: 平均电压 (2字节)
                // 偏移 18-19: 工作电压 (2字节)
                // 偏移 20-21: 系列电流 (2字节)
                // 偏移 22-23: 标准电压 (2字节)
                // 偏移 24-25: 针振 (2字节)
                // 偏移 26-27: 摆动 (2字节)
                // 偏移 28-31: 总功耗 (4字节)
                // 偏移 32-33: 下料次数 (2字节)
                // 偏移 34-35: 下料量 (2字节)
                // 偏移 59-60: 氟盐下料次数 (2字节)
                // 偏移 63-64: 基准下料间隔 (2字节)
                // 偏移 83-84: 电压异常持续 (2字节)
                // 偏移 85-86: 摆动持续 (2字节)
                // 偏移 91-92: 欠量下料次数 (2字节)
                // 偏移 93-94: 过量下料次数 (2字节)
                // 偏移 95-96: 闪烁效应基准 (2字节)
                // 偏移 106-107: 效应次数 (2字节)
                // 偏移 108-111: 效应持续时间 (4字节)
                // 偏移 116-119: 效应均压 (4字节)
                // 偏移 120-123: 效应功耗 (4字节)
                
                // 槽号 (2-3) - 大端序
                report.caoh = (validData[recOffset + 2] << 8) | validData[recOffset + 3];
                
                // 日期 (4-7)
                int dateValue = (validData[recOffset + 4] << 24) | (validData[recOffset + 5] << 16) | 
                                (validData[recOffset + 6] << 8) | validData[recOffset + 7];
                report.recordTime = dateValue.ToString();
                
                // 车间号 (12)、工区号 (13)
                byte workshopNum = validData[recOffset + 12];
                byte workAreaNum = validData[recOffset + 13];
                
                // 电压相关
                report.sheddy = (validData[recOffset + 14] << 8) | validData[recOffset + 15]; // 设定电压
                report.pingjdy = (validData[recOffset + 16] << 8) | validData[recOffset + 17]; // 平均电压
                report.gongzdy = (validData[recOffset + 18] << 8) | validData[recOffset + 19]; // 工作电压
                report.xiedl = (validData[recOffset + 20] << 8) | validData[recOffset + 21]; // 系列电流
                report.biaozdy = (validData[recOffset + 22] << 8) | validData[recOffset + 23]; // 标准电压
                
                // 针摆相关
                report.zhenzhen = (validData[recOffset + 24] << 8) | validData[recOffset + 25]; // 针振
                report.baidong = (validData[recOffset + 26] << 8) | validData[recOffset + 27]; // 摆动
                report.zonggonghao = (validData[recOffset + 28] << 24) | (validData[recOffset + 29] << 16) | 
                                      (validData[recOffset + 30] << 8) | validData[recOffset + 31]; // 总功耗
                report.baidongchixu = (validData[recOffset + 85] << 8) | validData[recOffset + 86]; // 摆动持续
                report.dianyayichangchixu = (validData[recOffset + 83] << 8) | validData[recOffset + 84]; // 电压异常持续
                
                // 下料相关
                report.xialiaocishu = (validData[recOffset + 32] << 8) | validData[recOffset + 33]; // 下料次数
                report.xialiaoliang = (validData[recOffset + 34] << 8) | validData[recOffset + 35]; // 下料量
                report.xialiaojiange = (validData[recOffset + 63] << 8) | validData[recOffset + 64]; // 基准下料间隔
                report.qianliang = (validData[recOffset + 91] << 8) | validData[recOffset + 92]; // 欠量次数
                report.guoliang = (validData[recOffset + 93] << 8) | validData[recOffset + 94]; // 过量次数
                report.fuyanxialiao = (validData[recOffset + 59] << 8) | validData[recOffset + 60]; // 氟盐下料次数
                
                // 效应相关
                report.xiaoyingcishu = (validData[recOffset + 106] << 8) | validData[recOffset + 107]; // 效应次数
                report.shanshuxiaoying = (validData[recOffset + 95] << 8) | validData[recOffset + 96]; // 闪烁效应基准
                report.xiaoyingchixu = (validData[recOffset + 108] << 24) | (validData[recOffset + 109] << 16) | 
                                        (validData[recOffset + 110] << 8) | validData[recOffset + 111]; // 效应持续时间
                report.xiaoyingjuny = (validData[recOffset + 116] << 24) | (validData[recOffset + 117] << 16) | 
                                       (validData[recOffset + 118] << 8) | validData[recOffset + 119]; // 效应均压
                report.xiaoyinggonghao = (validData[recOffset + 120] << 24) | (validData[recOffset + 121] << 16) | 
                                          (validData[recOffset + 122] << 8) | validData[recOffset + 123]; // 效应功耗
                
                // 槽状态默认为联机
                report.caokCaozht = 0;
                
                // 工艺相关字段暂时为0（需要从其他帧获取）
                report.wendu = 0;
                report.lvshupingmian = 0;
                report.dianjiezhipingmian = 0;
                report.fenzibi = 0;
                report.tiehanliang = 0;
                report.chulvliang = 0;
                
                if (enableDetailedDebug && i < 3)
                {
                    Debug.Log($"[Frame72] 记录{i+1}: 槽号={report.caoh}, 设定电压={report.sheddy}, " +
                              $"效应次数={report.xiaoyingcishu}, 下料次数={report.xialiaocishu}, 针振={report.zhenzhen}");
                }
                
                list.Add(report);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Frame72] 解析第 {i + 1} 条记录失败: {e.Message}");
            }
        }
        
        return list;
    }

    /// <summary>
    /// 获取Frame 72统计报表数据（按车间工区批量请求所有槽号）
    /// </summary>
    /// <param name="workshopNumber">车间号</param>
    /// <param name="workAreaNumber">工区号</param>
    /// <param name="startDate">起始日期 yyyyMMdd</param>
    /// <param name="endDate">终止日期 yyyyMMdd</param>
    public void FetchFrame72ReportByWorkArea(int workshopNumber, int workAreaNumber, string startDate, string endDate)
    {
        Debug.Log($"[FetchFrame72] 方法被调用 - 车间:{workshopNumber}, 工区:{workAreaNumber}, 日期:{startDate}-{endDate}");
        StartCoroutine(FetchFrame72ReportByWorkAreaAsync(workshopNumber, workAreaNumber, startDate, endDate));
    }

    private System.Collections.IEnumerator FetchFrame72ReportByWorkAreaAsync(int workshopNumber, int workAreaNumber, string startDate, string endDate)
    {
        Debug.Log($"[Frame72协程] 开始批量请求...");
        
        // 获取该车间工区下的所有槽号
        List<ushort> slotNumbers = new List<ushort>();

        // 先等待全局槽配置加载（避免场景初始化时序导致拿不到数据）
        float waitStart = Time.realtimeSinceStartup;
        while (GlobalElectrolyticCellDataManager.Instance == null || !GlobalElectrolyticCellDataManager.Instance.isDataLoaded)
        {
            if (Time.realtimeSinceStartup - waitStart > 3f)
            {
                break;
            }
            yield return null;
        }

        if (GlobalElectrolyticCellDataManager.Instance != null && GlobalElectrolyticCellDataManager.Instance.isDataLoaded)
        {
            slotNumbers = GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea(
                (byte)workshopNumber, (byte)workAreaNumber);
            Debug.Log($"[Frame72] 获取到 {slotNumbers.Count} 个槽号");
        }

        // 回退：若全局管理器未就绪或当前工区无数据，尝试从槽号选择器读取
        if (slotNumbers.Count == 0)
        {
            var selector = FindObjectOfType<SlotNumberSelector>();
            if (selector != null)
            {
                var rawSlots = selector.GetSlotNumberItems();
                foreach (var s in rawSlots)
                {
                    if (ushort.TryParse(s, out ushort sn) && !slotNumbers.Contains(sn))
                    {
                        slotNumbers.Add(sn);
                    }
                }
                if (slotNumbers.Count > 0)
                {
                    Debug.LogWarning($"[Frame72] 使用 SlotNumberSelector 回退槽号列表，数量: {slotNumbers.Count}");
                }
            }
        }
        
        if (slotNumbers.Count == 0)
        {
            Debug.LogWarning($"[Frame72] 无法获取槽号列表（GlobalElectrolyticCellDataManager未就绪或无数据，且选择器回退失败）");
            yield break;
        }
        
        // 第一步：先创建表格并填充槽号列表（显示空数据）
        List<ReportData> allReports = new List<ReportData>();
        foreach (ushort slotNumber in slotNumbers)
        {
            ReportData report = new ReportData();
            report.caoh = slotNumber;
            report.caokCaozht = 0; // 默认联机
            report.sheddy = 0;
            report.pingjdy = 0;
            report.gongzdy = 0;
            allReports.Add(report);
        }
        
        // 立即显示槽号列表
        currentReportData = new StatisticsReportResponse
        {
            total = allReports.Count,
            rows = allReports.ToArray(),
            code = 200,
            msg = "Loading..."
        };
        
        Debug.Log($"[Frame72] ✓ 先填充 {allReports.Count} 个槽号到表格");
        FillCurrentTabTable();
        
        yield return null; // 等待一帧让表格显示
        
        // 第二步：异步请求每个槽号的数据
        string enterpriseId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId) ? GlobalUserData.AlSerialPantId : electrolyticCompanyId;
        
        int successCount = 0;
        // 遍历每个槽号发起请求
        for (int i = 0; i < slotNumbers.Count; i++)
        {
            ushort slotNumber = slotNumbers[i];
            
            // Frame 72: 企业ID-72-槽号-起始日期-终止日期
            string requestParam = $"{enterpriseId}-72-{slotNumber}-{startDate}-{endDate}";
            string jsonData = $"\"{requestParam}\"";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
            
            Debug.Log($"[Frame72] 请求槽号 {slotNumber}: {requestParam}");
            
            UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler.text;
                
                try
                {
                    byte[] data = ParseBase64Response(responseText);
                    
                    if (data != null && data.Length > 0)
                    {
                        List<ReportData> reports = ParseFrame72Data(data);
                        
                        if (reports.Count > 0)
                        {
                            // 更新对应槽号的数据
                            var reportData = reports[0]; // 取第一条记录
                            allReports[i] = reportData;
                            successCount++;
                            Debug.Log($"[Frame72] 槽号 {slotNumber} 解析完成: 设定电压={reportData.sheddy}, 平均电压={reportData.pingjdy}");
                        }
                        else
                        {
                            Debug.LogWarning($"[Frame72] 槽号 {slotNumber} 返回数据为空");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Frame72] 槽号 {slotNumber} Base64解析后长度为0，可能当天没有数据");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Frame72] 槽号 {slotNumber} 解析异常: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[Frame72] 槽号 {slotNumber} 请求失败: {webRequest.error}");
            }
            
            webRequest.Dispose();
            
            // 每10个槽号更新一次表格
            if ((i + 1) % 10 == 0 || i == slotNumbers.Count - 1)
            {
                currentReportData.rows = allReports.ToArray();
                FillCurrentTabTable();
                Debug.Log($"[Frame72] 已处理 {i + 1}/{slotNumbers.Count} 个槽号，更新表格");
            }
            
            // 添加小延迟避免请求过快
            yield return new WaitForSeconds(0.05f);
        }
        
        // 最终更新
        currentReportData.rows = allReports.ToArray();
        currentReportData.msg = $"Success ({successCount}/{slotNumbers.Count} slots)";
        
        Debug.Log($"[Frame72] ✓ 批量请求完成，共 {allReports.Count} 个槽号，{successCount} 个有数据");
        
        FillCurrentTabTable();
    }
    #endregion

    #region 公开方法 - 填充表格
    /// <summary>
    /// 更新表格字体（包括表头和数据单元格）
    /// </summary>
    private void UpdateTableFont(UITable table)
    {
        if (table == null)
        {
            Debug.LogWarning("UpdateTableFont: table为null");
            return;
        }
        
        if (tableFont == null)
        {
            Debug.LogWarning("UpdateTableFont: tableFont为null");
            return;
        }
        
        // 获取所有TextMeshProUGUI组件（包括未激活的）
        var texts = table.GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"UpdateTableFont: 找到 {texts.Length} 个TextMeshProUGUI组件");
        
        int headerCount = 0;
        int dataCount = 0;
        
        foreach (var t in texts)
        {
            if (t.font != tableFont)
            {
                t.font = tableFont;
                
                // 判断是否是表头（通常包含"Header"或"Column"）
                if (t.gameObject.name.Contains("Header") || t.gameObject.name.Contains("Column"))
                {
                    headerCount++;
                    Debug.Log($"✓ 更新表头字体: {t.gameObject.name} -> {tableFont.name}");
                }
                else
                {
                    dataCount++;
                }
            }
        }
        
        Debug.Log($"UpdateTableFont 完成: 表头 {headerCount} 个, 数据 {dataCount} 个");
        
        // 强制刷新表格
        table.SetAllDirty();
        table.RefreshGraph();
    }

    /// <summary>
    /// 初始化表格样式 - 简化版：只设置列数为5
    /// </summary>
    private void InitializeTableStyle(UITable table)
    {
        if (table == null)
        {
            Debug.LogError("InitializeTableStyle: table为null");
            return;
        }

        try
        {
            Debug.Log("StatisticsReportManager: 设置表格为5列");
            
            // 设置5列表头
            table.SetColumns(new List<string> { "槽号", "槽状态", "设定电压", "平均电压", "工作电压" });
            
            Debug.Log("✓ 表格列数设置完成：5列");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InitializeTableStyle错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 填充电压表格
    /// </summary>
    public void FillVoltageTable(UITable table = null)
    {
        UITable targetTable = table ?? voltageTable;
        if (targetTable == null)
        {
            Debug.LogError("电压表格组件未设置！");
            return;
        }

        if (currentReportData == null || currentReportData.rows == null || currentReportData.rows.Length == 0)
        {
            Debug.LogWarning("当前无数据可填充");
            return;
        }

        try
        {
            InitializeTableStyle(targetTable);
            targetTable.ClearData();

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                targetTable.AddRow(new string[5]);
            }

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                var report = currentReportData.rows[i];
                var row = targetTable.data[i];

                while (row.data.Count < 5)
                {
                    row.data.Add(new TableCell());
                }

                // 第一列：如果是Frame 71 (历史曲线)，显示时间；否则显示槽号
                if (!string.IsNullOrEmpty(report.recordTime))
                {
                    row.data[0].stringValue = report.recordTime;
                    row.data[0].type = DataType.String;
                }
                else
                {
                    row.data[0].doubleValue = report.caoh;
                    row.data[0].type = DataType.Double;
                }

                row.data[1].stringValue = report.GetStatusText();
                row.data[1].type = DataType.String;

                row.data[2].doubleValue = report.sheddy / 1000.0;
                row.data[2].type = DataType.Double;

                row.data[3].doubleValue = report.pingjdy / 1000.0;
                row.data[3].type = DataType.Double;

                row.data[4].doubleValue = report.gongzdy / 1000.0;
                row.data[4].type = DataType.Double;
            }

            targetTable.RefreshGraph();
            targetTable.RefreshTable();
            
            // 刷新表格后应用字体
            UpdateTableFont(targetTable);

            Debug.Log($" 电压表格填充完成，共 {currentReportData.rows.Length} 条记录");
        }
        catch (System.Exception e)
        {
            Debug.LogError($" 填充电压表格失败: {e.Message}");
        }
    }

    /// <summary>
    /// 填充效应表格
    /// </summary>
    public void FillEffectTable(UITable table = null)
    {
        UITable targetTable = table ?? efficiencyTable;
        if (targetTable == null)
        {
            Debug.LogError("效应表格组件未设置！");
            return;
        }

        if (currentReportData == null || currentReportData.rows == null || currentReportData.rows.Length == 0)
        {
            Debug.LogWarning("当前无数据可填充");
            return;
        }

        try
        {
            // 设置6列表头
            targetTable.SetColumns(new List<string> { "槽号", "效应次数", "闪烁效应次数", "持续时间", "均压", "功耗" });
            targetTable.ClearData();

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                targetTable.AddRow(new string[6]);
            }

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                var report = currentReportData.rows[i];
                var row = targetTable.data[i];

                while (row.data.Count < 6)
                {
                    row.data.Add(new TableCell());
                }

                row.data[0].doubleValue = report.caoh;
                row.data[0].type = DataType.Double;

                row.data[1].doubleValue = report.xiaoyingcishu;
                row.data[1].type = DataType.Double;

                row.data[2].doubleValue = report.shanshuxiaoying;
                row.data[2].type = DataType.Double;

                row.data[3].stringValue = report.FormatTime(report.xiaoyingchixu);
                row.data[3].type = DataType.String;

                row.data[4].doubleValue = report.xiaoyingjuny / 1000.0;
                row.data[4].type = DataType.Double;

                row.data[5].doubleValue = report.xiaoyinggonghao / 1000.0;
                row.data[5].type = DataType.Double;
            }

            targetTable.RefreshGraph();
            targetTable.RefreshTable();
            UpdateTableFont(targetTable);

            Debug.Log($"✓ 效应表格填充完成，共 {currentReportData.rows.Length} 条记录");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ 填充效应表格失败: {e.Message}");
        }
    }

    // 保持向后兼容
    public void FillEfficiencyTable(UITable table = null)
    {
        FillEffectTable(table);
    }

    /// <summary>
    /// 填充下料表格
    /// </summary>
    public void FillFeedingTable(UITable table = null)
    {
        UITable targetTable = table ?? feedingTable;
        if (targetTable == null)
        {
            Debug.LogError("下料表格组件未设置！");
            return;
        }

        if (currentReportData == null || currentReportData.rows == null || currentReportData.rows.Length == 0)
        {
            Debug.LogWarning("当前无数据可填充");
            return;
        }

        try
        {
            // 设置7列表头
            targetTable.SetColumns(new List<string> { "槽号", "下料间隔", "总次数", "欠量次数", "过量次数", "下料总量", "氟盐量" });
            targetTable.ClearData();

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                targetTable.AddRow(new string[7]);
            }

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                var report = currentReportData.rows[i];
                var row = targetTable.data[i];

                while (row.data.Count < 7)
                {
                    row.data.Add(new TableCell());
                }

                row.data[0].doubleValue = report.caoh;
                row.data[0].type = DataType.Double;

                row.data[1].doubleValue = report.xialiaojiange;
                row.data[1].type = DataType.Double;

                row.data[2].doubleValue = report.xialiaocishu;
                row.data[2].type = DataType.Double;

                row.data[3].doubleValue = report.qianliang;
                row.data[3].type = DataType.Double;

                row.data[4].doubleValue = report.guoliang;
                row.data[4].type = DataType.Double;

                row.data[5].doubleValue = report.xialiaoliang;
                row.data[5].type = DataType.Double;

                row.data[6].doubleValue = report.fuyanxialiao;
                row.data[6].type = DataType.Double;
            }

            targetTable.RefreshGraph();
            targetTable.RefreshTable();
            UpdateTableFont(targetTable);

            Debug.Log($"✓ 下料表格填充完成，共 {currentReportData.rows.Length} 条记录");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ 填充下料表格失败: {e.Message}");
        }
    }

    /// <summary>
    /// 填充针摆表格
    /// </summary>
    public void FillNeedleTable(UITable table)
    {
        if (table == null)
        {
            Debug.LogError("针摆表格组件未设置！");
            return;
        }

        if (currentReportData == null || currentReportData.rows == null || currentReportData.rows.Length == 0)
        {
            Debug.LogWarning("当前无数据可填充");
            return;
        }

        try
        {
            // 设置6列表头
            table.SetColumns(new List<string> { "槽号", "针振", "摆动", "总功耗", "摆动持续", "异常持续" });
            table.ClearData();

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                table.AddRow(new string[6]);
            }

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                var report = currentReportData.rows[i];
                var row = table.data[i];

                while (row.data.Count < 6)
                {
                    row.data.Add(new TableCell());
                }

                row.data[0].doubleValue = report.caoh;
                row.data[0].type = DataType.Double;

                row.data[1].doubleValue = report.zhenzhen;
                row.data[1].type = DataType.Double;

                row.data[2].doubleValue = report.baidong;
                row.data[2].type = DataType.Double;

                row.data[3].doubleValue = report.zonggonghao / 1000.0;
                row.data[3].type = DataType.Double;

                row.data[4].doubleValue = report.baidongchixu;
                row.data[4].type = DataType.Double;

                row.data[5].doubleValue = report.dianyayichangchixu;
                row.data[5].type = DataType.Double;
            }

            table.RefreshGraph();
            table.RefreshTable();
            UpdateTableFont(table);

            Debug.Log($"✓ 针摆表格填充完成，共 {currentReportData.rows.Length} 条记录");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ 填充针摆表格失败: {e.Message}");
        }
    }

    /// <summary>
    /// 填充工艺表格
    /// </summary>
    public void FillProcessTable(UITable table)
    {
        if (table == null)
        {
            Debug.LogError("工艺表格组件未设置！");
            return;
        }

        if (currentReportData == null || currentReportData.rows == null || currentReportData.rows.Length == 0)
        {
            Debug.LogWarning("当前无数据可填充");
            return;
        }

        try
        {
            // 设置7列表头
            table.SetColumns(new List<string> { "槽号", "温度", "铝水平", "电解质水平", "分子比", "铁含量", "出铝量" });
            table.ClearData();

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                table.AddRow(new string[7]);
            }

            for (int i = 0; i < currentReportData.rows.Length; i++)
            {
                var report = currentReportData.rows[i];
                var row = table.data[i];

                while (row.data.Count < 7)
                {
                    row.data.Add(new TableCell());
                }

                row.data[0].doubleValue = report.caoh;
                row.data[0].type = DataType.Double;

                // 工艺数据暂时显示为0（需要从其他帧获取）
                row.data[1].doubleValue = report.wendu;
                row.data[1].type = DataType.Double;

                row.data[2].doubleValue = report.lvshupingmian;
                row.data[2].type = DataType.Double;

                row.data[3].doubleValue = report.dianjiezhipingmian;
                row.data[3].type = DataType.Double;

                row.data[4].doubleValue = report.fenzibi;
                row.data[4].type = DataType.Double;

                row.data[5].doubleValue = report.tiehanliang;
                row.data[5].type = DataType.Double;

                row.data[6].doubleValue = report.chulvliang;
                row.data[6].type = DataType.Double;
            }

            table.RefreshGraph();
            table.RefreshTable();
            UpdateTableFont(table);

            Debug.Log($"✓ 工艺表格填充完成，共 {currentReportData.rows.Length} 条记录（工艺数据需要从其他帧获取）");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ 填充工艺表格失败: {e.Message}");
        }
    }
    #endregion

    #region 公开方法 - 数据访问
    public StatisticsReportResponse GetCurrentReportData()
    {
        return currentReportData;
    }

    public int GetTotalRecords()
    {
        return currentReportData?.total ?? 0;
    }
    #endregion

    #region 调试方法
    [ContextMenu("测试-获取统计数据")]
    public void TestFetchData()
    {
        FetchStatisticsReport();
    }

    [ContextMenu("打印当前数据")]
    public void PrintCurrentData()
    {
        if (currentReportData == null)
        {
            Debug.Log("当前无数据");
            return;
        }

        Debug.Log($"=== 当前统计数据 ===");
        Debug.Log($"总记录数: {currentReportData.total}");
        Debug.Log($"实际行数: {currentReportData.rows?.Length ?? 0}");

        if (currentReportData.rows != null && currentReportData.rows.Length > 0)
        {
            var firstRow = currentReportData.rows[0];
            Debug.Log("\n第一条记录示例:");
            Debug.Log($"  槽号: {firstRow.caoh}");
            Debug.Log($"  槽状态: {firstRow.GetStatusText()}");
            Debug.Log($"  设定电压: {firstRow.FormatVoltage(firstRow.sheddy)}V");
            Debug.Log($"  平均电压: {firstRow.FormatVoltage(firstRow.pingjdy)}V");
            Debug.Log($"  工作电压: {firstRow.FormatVoltage(firstRow.gongzdy)}V");
            Debug.Log($"  效应次数: {firstRow.xiaoyingcishu}");
        }
    }
    #endregion
    
    #region Tab切换逻辑
    private void InitTabButtons()
    {
        Debug.Log("=== 开始初始化Tab按钮 ===");
        
        if (btnDianya != null)
        {
            btnDianya.onClick.RemoveAllListeners();
            btnDianya.onClick.AddListener(() => {
                Debug.Log(">>> 电压按钮被点击！");
                SwitchTab(TabType.Voltage);
            });
            Debug.Log($"✓ 电压按钮已绑定: {btnDianya.name}");
        }
        else
        {
            Debug.LogError("✗ btnDianya 为 null!");
        }
        
        if (btnXiaoying != null)
        {
            btnXiaoying.onClick.RemoveAllListeners();
            btnXiaoying.onClick.AddListener(() => {
                Debug.Log(">>> 效应按钮被点击！");
                SwitchTab(TabType.Effect);
            });
            Debug.Log($"✓ 效应按钮已绑定: {btnXiaoying.name}");
        }
        else
        {
            Debug.LogError("✗ btnXiaoying 为 null!");
        }
        
        if (btnXialiao != null)
        {
            btnXialiao.onClick.RemoveAllListeners();
            btnXialiao.onClick.AddListener(() => {
                Debug.Log(">>> 下料按钮被点击！");
                SwitchTab(TabType.Feeding);
            });
            Debug.Log($"✓ 下料按钮已绑定: {btnXialiao.name}");
        }
        else
        {
            Debug.LogError("✗ btnXialiao 为 null!");
        }
        
        if (btnZhenbai != null)
        {
            btnZhenbai.onClick.RemoveAllListeners();
            btnZhenbai.onClick.AddListener(() => {
                Debug.Log(">>> 针摆按钮被点击！");
                SwitchTab(TabType.Needle);
            });
            Debug.Log($"✓ 针摆按钮已绑定: {btnZhenbai.name}");
        }
        else
        {
            Debug.LogError("✗ btnZhenbai 为 null!");
        }
        
        if (btnGongyi != null)
        {
            btnGongyi.onClick.RemoveAllListeners();
            btnGongyi.onClick.AddListener(() => {
                Debug.Log(">>> 工艺按钮被点击！");
                SwitchTab(TabType.Process);
            });
            Debug.Log($"✓ 工艺按钮已绑定: {btnGongyi.name}");
        }
        else
        {
            Debug.LogError("✗ btnGongyi 为 null!");
        }
        
        Debug.Log("=== Tab按钮初始化完成 ===");
    }
    
    /// <summary>
    /// 切换Tab，可选择是否立即填充数据
    /// </summary>
    private void SwitchTab(TabType tabType, bool fillData = true)
    {
        currentTab = tabType;
        Debug.Log($"=== 切换到Tab: {tabType}，填充数据: {fillData} ===");
        
        // 隐藏所有表格
        if (voltageTable != null) voltageTable.gameObject.SetActive(false);
        if (efficiencyTable != null) efficiencyTable.gameObject.SetActive(false);
        if (feedingTable != null) feedingTable.gameObject.SetActive(false);
        if (needleTable != null) needleTable.gameObject.SetActive(false);
        if (processTable != null) processTable.gameObject.SetActive(false);
        
        // 重置按钮颜色
        SetButtonColor(btnDianya, false);
        SetButtonColor(btnXiaoying, false);
        SetButtonColor(btnXialiao, false);
        SetButtonColor(btnZhenbai, false);
        SetButtonColor(btnGongyi, false);
        
        // 显示对应表格和高亮按钮
        switch (tabType)
        {
            case TabType.Voltage:
                if (voltageTable != null) voltageTable.gameObject.SetActive(true);
                SetButtonColor(btnDianya, true);
                if (fillData && currentReportData != null) FillVoltageTable();
                break;
            case TabType.Effect:
                if (efficiencyTable != null) efficiencyTable.gameObject.SetActive(true);
                SetButtonColor(btnXiaoying, true);
                if (fillData && currentReportData != null) FillEffectTable();
                break;
            case TabType.Feeding:
                if (feedingTable != null) feedingTable.gameObject.SetActive(true);
                SetButtonColor(btnXialiao, true);
                if (fillData && currentReportData != null) FillFeedingTable();
                break;
            case TabType.Needle:
                if (needleTable != null) needleTable.gameObject.SetActive(true);
                SetButtonColor(btnZhenbai, true);
                if (fillData && currentReportData != null) FillNeedleTable(needleTable);
                break;
            case TabType.Process:
                if (processTable != null) processTable.gameObject.SetActive(true);
                SetButtonColor(btnGongyi, true);
                if (fillData && currentReportData != null) FillProcessTable(processTable);
                break;
        }
    }
    
    private void SetButtonColor(Button btn, bool isSelected)
    {
        if (btn == null) return;
        var image = btn.GetComponent<Image>();
        if (image != null)
        {
            image.color = isSelected ? selectedColor : normalColor;
        }
    }
    #endregion
}
