using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;

/// <summary>
/// 工艺曲线管理器
/// 挂载到 gongyiManager 空物体上。
/// 
/// 功能：
/// 1. 选取车间/工区（WorkAreaSelector）、槽号（SlotNumberSelector）、
///    起始日期（riqSelector_Start）、终止日期（riqSelector_End）后点击查询。
/// 2. 调用 API 20（72号帧）获取工作电压。
/// 3. 调用 API 21（73号帧）获取槽温、铝水平、电解质水平、吸出铝量、钙含量。
/// 4. 在 6 个 LineChart 上绘制每日折线图。
/// 
/// Inspector 绑定清单（全部 [SerializeField]，在编辑器中拖拽赋值）：
///   - workAreaSelector          : WorkAreaSelector 组件
///   - slotNumberSelector        : SlotNumberSelector 组件
///   - startDateSelector         : riqSelector 组件（起始日期）
///   - endDateSelector           : riqSelector 组件（终止日期）
///   - queryButton               : 查询 Button
///   - loadingText               : 加载提示 TextMeshProUGUI（可选）
///   - chartsContainer           : Scroll View → Viewport → Content 的 Transform
///   - scrollRect                : ScrollRect 组件
///   - chartPrefab               : 带 LineChart 组件的预制体（可选，不绑则自动创建）
///   - chart_dianya              : "工作电压" 的 LineChart（LineChart_dianya）
///   - chart_caowen              : "槽温" 的 LineChart（LineChart_caowen）
///   - chart_lvshuiping          : "铝水平" 的 LineChart（LineChart_lvshuiping）
///   - chart_dianjiezhi          : "电解质水平" 的 LineChart（LineChart_dianjiezhi）
///   - chart_xichulv             : "吸出铝量" 的 LineChart（LineChart_xichulv）
///   - chart_gaihanliang         : "钙含量" 的 LineChart（LineChart_gaihanliang）
/// </summary>
[DisallowMultipleComponent]
public class GongyiCurveManager : MonoBehaviour
{
    // ==================== 选择器 ====================
    [Header("选择器")]
    [SerializeField] private WorkAreaSelector workAreaSelector;
    [SerializeField] private SlotNumberSelector slotNumberSelector;
    [SerializeField] private riqSelector startDateSelector;
    [SerializeField] private riqSelector endDateSelector;

    // ==================== 按钮 ====================
    [Header("按钮")]
    [SerializeField] private Button queryButton;

    // ==================== 提示 ====================
    [Header("提示")]
    [SerializeField] private TextMeshProUGUI loadingText;

    // ==================== 图表父节点 & ScrollRect ====================
    [Header("图表容器")]
    [SerializeField] private Transform chartsContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject chartPrefab; // 可选

    // ==================== 6 个 LineChart 引用 ====================
    [Header("图表引用（拖拽场景中的 LineChart）")]
    [SerializeField] private LineChart chart_dianya;      // 工作电压
    [SerializeField] private LineChart chart_caowen;      // 槽温
    [SerializeField] private LineChart chart_lvshuiping;   // 铝水平
    [SerializeField] private LineChart chart_dianjiezhi;   // 电解质水平
    [SerializeField] private LineChart chart_xichulv;      // 吸出铝量
    [SerializeField] private LineChart chart_gaihanliang;  // 钙含量

    // ==================== API ====================
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string electrolyticCompanyId = "1033";

    // ==================== 运行时 ====================
    [Header("运行控制")]
    [SerializeField] private bool autoQueryOnEnable = false;
    [SerializeField] private bool autoQueryOnSelectorConfirmed = true;
    [SerializeField] private bool enableDetailedDebug = true;

    // 图表布局参数
    [Header("图表布局")]
    [SerializeField] private float chartHeight = 420f;
    [SerializeField] private float chartSpacing = 0f;

    [Header("字体")]
    [SerializeField] private TMP_FontAsset chineseFontAsset;

    private LineChart[] _allCharts;   // [0]=dianya, [1]=caowen, [2]=lvshuiping, [3]=dianjiezhi, [4]=xichulv, [5]=gaihanliang
    private string[] _chartTitles;    // 对应标题
    private List<DailyDataPoint> _currentData = new List<DailyDataPoint>();
    private bool _isLoading;

    // ==================== 数据模型 ====================
    [Serializable]
    private class DailyDataPoint
    {
        public string Date;            // yyyyMMdd
        public float WorkVoltage;      // 工作电压（÷1000）
        public float Caowen;           // 槽温
        public float AluminumLevel;    // 铝水平
        public float ElectrolyteLevel; // 电解质水平
        public float AluminumOutput;   // 吸出铝量
        public float CalciumContent;   // 钙含量

        public bool Has72Data;         // 72 帧数据是否存在
        public bool Has73Data;         // 73 帧数据是否存在
    }

    // ==================== 生命周期 ====================
    private void Start()
    {
        // 强制覆盖 Inspector 中可能存在的旧值，确保图表高度和间距生效
        chartHeight = 420f;
        chartSpacing = 0f;

        AutoLoadChineseFont();
        AutoBindReferences();
        InitDefaults();
        BindEvents();

        if (autoQueryOnEnable)
        {
            OnQueryButtonClick();
        }
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    // ==================== 自动绑定 / 初始化 ====================
    private void AutoBindReferences()
    {
        if (workAreaSelector == null)
            workAreaSelector = FindObjectOfType<WorkAreaSelector>();

        if (slotNumberSelector == null)
            slotNumberSelector = FindObjectOfType<SlotNumberSelector>();

        if (scrollRect == null)
        {
            var scrolls = FindObjectsOfType<ScrollRect>(true);
            foreach (var sr in scrolls)
            {
                if (sr != null && sr.content != null && sr.content.name.ToLowerInvariant().Contains("content"))
                {
                    scrollRect = sr;
                    if (chartsContainer == null) chartsContainer = sr.content;
                    break;
                }
            }
        }

        if (chartsContainer == null && scrollRect != null)
            chartsContainer = scrollRect.content;

        // 构建图表数组
        _allCharts = new LineChart[] { chart_dianya, chart_caowen, chart_lvshuiping, chart_dianjiezhi, chart_xichulv, chart_gaihanliang };
        _chartTitles = new string[] { "工作电压", "槽温", "铝水平", "电解质水平", "吸出铝量", "钙含量" };
    }

    private void InitDefaults()
    {
        // 默认起始日期：30 天前
        string today = DateTime.Today.ToString("yyyyMMdd");
        string thirtyDaysAgo = DateTime.Today.AddDays(-30).ToString("yyyyMMdd");

        if (startDateSelector != null && startDateSelector.selectedDisplayText != null)
        {
            string cur = NormalizeDate(startDateSelector.selectedDisplayText.text);
            if (string.IsNullOrEmpty(cur))
                startDateSelector.selectedDisplayText.text = thirtyDaysAgo;
        }

        if (endDateSelector != null && endDateSelector.selectedDisplayText != null)
        {
            string cur = NormalizeDate(endDateSelector.selectedDisplayText.text);
            if (string.IsNullOrEmpty(cur))
                endDateSelector.selectedDisplayText.text = today;
        }
    }

    private void BindEvents()
    {
        if (queryButton != null)
        {
            queryButton.onClick.RemoveListener(OnQueryButtonClick);
            queryButton.onClick.AddListener(OnQueryButtonClick);
        }

        // 选择器确认事件
        if (startDateSelector != null)
        {
            startDateSelector.OnDateConfirmed.RemoveListener(OnStartDateConfirmed);
            startDateSelector.OnDateConfirmed.AddListener(OnStartDateConfirmed);
        }

        if (endDateSelector != null && endDateSelector != startDateSelector)
        {
            endDateSelector.OnDateConfirmed.RemoveListener(OnEndDateConfirmed);
            endDateSelector.OnDateConfirmed.AddListener(OnEndDateConfirmed);
        }

        if (slotNumberSelector != null)
        {
            slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
            slotNumberSelector.OnSlotConfirmed.AddListener(OnSlotConfirmed);
        }

        if (workAreaSelector != null)
        {
            workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);
            workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnWorkAreaConfirmed);
        }
    }

    private void UnbindEvents()
    {
        if (queryButton != null) queryButton.onClick.RemoveListener(OnQueryButtonClick);
        if (startDateSelector != null) startDateSelector.OnDateConfirmed.RemoveListener(OnStartDateConfirmed);
        if (endDateSelector != null && endDateSelector != startDateSelector) endDateSelector.OnDateConfirmed.RemoveListener(OnEndDateConfirmed);
        if (slotNumberSelector != null) slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
        if (workAreaSelector != null) workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);
    }

    // ==================== 选择器事件 ====================
    private void OnStartDateConfirmed(string _)
    {
        if (autoQueryOnSelectorConfirmed) TryAutoQuery();
    }

    private void OnEndDateConfirmed(string _)
    {
        if (autoQueryOnSelectorConfirmed) TryAutoQuery();
    }

    private void OnSlotConfirmed(string _)
    {
        if (autoQueryOnSelectorConfirmed) TryAutoQuery();
    }

    private void OnWorkAreaConfirmed(string _)
    {
        if (autoQueryOnSelectorConfirmed) TryAutoQuery();
    }

    private void TryAutoQuery()
    {
        string startDate = GetDateFromSelector(startDateSelector);
        string endDate = GetDateFromSelector(endDateSelector);
        string slot = GetSlotNumber();

        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || string.IsNullOrEmpty(slot))
            return;

        if (string.CompareOrdinal(startDate, endDate) > 0)
            return;

        StartCoroutine(QueryAndRenderCharts());
    }

    // ==================== 查询按钮 ====================
    public void OnQueryButtonClick()
    {
        string startDate = GetDateFromSelector(startDateSelector);
        string endDate = GetDateFromSelector(endDateSelector);
        string slot = GetSlotNumber();

        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
        {
            Debug.LogWarning("[GongyiCurveManager] 请选择起始日期和终止日期");
            ShowLoading("请选择起止日期", true);
            return;
        }

        if (string.IsNullOrEmpty(slot))
        {
            Debug.LogWarning("[GongyiCurveManager] 槽号无效");
            ShowLoading("槽号无效", true);
            return;
        }

        if (string.CompareOrdinal(startDate, endDate) > 0)
        {
            Debug.LogWarning("[GongyiCurveManager] 起始日期不能大于终止日期");
            ShowLoading("起始日期不能大于终止日期", true);
            return;
        }

        StartCoroutine(QueryAndRenderCharts());
    }

    // ==================== 主协程：查询 & 绘图 ====================
    private IEnumerator QueryAndRenderCharts()
    {
        if (_isLoading) yield break;
        _isLoading = true;
        ShowLoading("正在查询数据...");

        string companyId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId)
            ? GlobalUserData.AlSerialPantId
            : electrolyticCompanyId;

        string slot = GetSlotNumber();
        string startDate = GetDateFromSelector(startDateSelector);
        string endDate = GetDateFromSelector(endDateSelector);

        // 按日期索引的数据点
        Dictionary<string, DailyDataPoint> dataByDate = new Dictionary<string, DailyDataPoint>();

        // ---- Step 1: 查询 Frame72（工作电压）----
        yield return QueryFrame72(companyId, slot, startDate, endDate, dataByDate);

        // ---- Step 2: 查询 Frame73（5 个工艺字段）----
        yield return QueryFrame73(companyId, slot, startDate, endDate, dataByDate);

        // ---- Step 3: 按日期排序 ----
        _currentData = dataByDate.Values
            .OrderBy(d => d.Date)
            .ToList();

        // ---- Step 4: 补全缺失日期的空数据点 ----
        FillMissingDates(startDate, endDate, _currentData);

        // ---- Step 5: 排序确保 X 轴连续 ----
        _currentData = _currentData.OrderBy(d => d.Date).ToList();

        if (_currentData.Count == 0)
        {
            ShowLoading("无数据", true);
            _isLoading = false;
            yield break;
        }

        Debug.Log($"[GongyiCurveManager] 数据查询完成，共 {_currentData.Count} 天数据");

        // ---- Step 6: 刷新 6 个图表 ----
        yield return null; // 等一帧让 UI 刷新
        Canvas.ForceUpdateCanvases();
        RenderAllCharts();

        ShowLoading("", false);
        _isLoading = false;
    }

    /// <summary>
    /// 补全起止日期之间缺失的日期，用 0 值填充，保证 X 轴连续
    /// </summary>
    private void FillMissingDates(string startDate, string endDate, List<DailyDataPoint> existingData)
    {
        if (!DateTime.TryParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start))
            return;
        if (!DateTime.TryParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            return;

        var existingDateSet = new HashSet<string>(existingData.Select(d => d.Date));

        for (DateTime d = start; d <= end; d = d.AddDays(1))
        {
            string ds = d.ToString("yyyyMMdd");
            if (!existingDateSet.Contains(ds))
            {
                existingData.Add(new DailyDataPoint
                {
                    Date = ds,
                    WorkVoltage = 0f,
                    Caowen = 0f,
                    AluminumLevel = 0f,
                    ElectrolyteLevel = 0f,
                    AluminumOutput = 0f,
                    CalciumContent = 0f,
                    Has72Data = false,
                    Has73Data = false
                });
            }
        }
    }

    // ==================== Frame72 查询 ====================
    private IEnumerator QueryFrame72(string companyId, string slot, string startDate, string endDate,
        Dictionary<string, DailyDataPoint> dataByDate)
    {
        string param = $"{companyId}-72-{slot}-{startDate}-{endDate}";
        byte[] decoded = null;
        yield return PostFrameRequest(param, data => decoded = data);

        if (decoded == null || decoded.Length == 0)
        {
            Debug.LogWarning("[GongyiCurveManager] Frame72 返回空数据");
            yield break;
        }

        ParseFrame72Payload(decoded, dataByDate);
    }

    private void ParseFrame72Payload(byte[] allBytes, Dictionary<string, DailyDataPoint> dataByDate)
    {
        int offset = 0;
        const int recordSize = 134;

        while (offset + 14 <= allBytes.Length)
        {
            // 扫描帧头 AA55
            if (allBytes[offset] != 0xAA || allBytes[offset + 1] != 0x55)
            {
                offset++;
                continue;
            }

            int frameStart = offset;
            offset += 2;
            ushort coId = ReadUInt16BE(allBytes, offset); offset += 2;
            ushort frameType = ReadUInt16BE(allBytes, offset); offset += 2;
            ushort frameSlot = ReadUInt16BE(allBytes, offset); offset += 2;
            uint dataLength = ReadUInt32BE(allBytes, offset); offset += 4;

            if (offset + dataLength + 2 > allBytes.Length)
            {
                Debug.LogWarning($"[GongyiCurveManager] Frame72 数据长度异常: {dataLength}");
                break;
            }

            int dataStart = offset;
            offset += (int)dataLength;

            // 校验帧尾 A55A
            if (allBytes[offset] != 0xA5 || allBytes[offset + 1] != 0x5A)
            {
                Debug.LogWarning("[GongyiCurveManager] Frame72 帧尾校验失败");
                offset += 2;
                continue;
            }
            offset += 2;

            if (frameType != 72) continue;

            int recordCount = ((int)dataLength) / recordSize;
            if (enableDetailedDebug)
                Debug.Log($"[GongyiCurveManager] Frame72: slot={frameSlot}, records={recordCount}");

            for (int i = 0; i < recordCount; i++)
            {
                int recOff = dataStart + i * recordSize;
                if (recOff + recordSize > allBytes.Length) break;

                string date = ParseDateFromRecord(allBytes, recOff + 4);
                if (string.IsNullOrEmpty(date)) continue;

                // 工作电压 offset = 18（2 字节，大端）
                int workVoltageRaw = (allBytes[recOff + 18] << 8) | allBytes[recOff + 19];
                float workVoltage = workVoltageRaw / 1000f;

                if (!dataByDate.TryGetValue(date, out var dp))
                {
                    dp = new DailyDataPoint { Date = date };
                    dataByDate[date] = dp;
                }
                dp.WorkVoltage = workVoltage;
                dp.Has72Data = true;
            }
        }
    }

    // ==================== Frame73 查询 ====================
    private IEnumerator QueryFrame73(string companyId, string slot, string startDate, string endDate,
        Dictionary<string, DailyDataPoint> dataByDate)
    {
        string param = $"{companyId}-73-{slot}-{startDate}-{endDate}";
        byte[] decoded = null;
        yield return PostFrameRequest(param, data => decoded = data);

        if (decoded == null || decoded.Length == 0)
        {
            Debug.LogWarning("[GongyiCurveManager] Frame73 返回空数据");
            yield break;
        }

        ParseFrame73Payload(decoded, dataByDate);
    }

    private void ParseFrame73Payload(byte[] allBytes, Dictionary<string, DailyDataPoint> dataByDate)
    {
        int offset = 0;
        const int recordSize = 70;

        while (offset + 14 <= allBytes.Length)
        {
            if (allBytes[offset] != 0xAA || allBytes[offset + 1] != 0x55)
            {
                offset++;
                continue;
            }

            offset += 2;
            ushort coId = ReadUInt16BE(allBytes, offset); offset += 2;
            ushort frameType = ReadUInt16BE(allBytes, offset); offset += 2;
            ushort frameSlot = ReadUInt16BE(allBytes, offset); offset += 2;
            uint dataLength = ReadUInt32BE(allBytes, offset); offset += 4;

            if (offset + dataLength + 2 > allBytes.Length) break;

            int dataStart = offset;
            offset += (int)dataLength;

            if (allBytes[offset] != 0xA5 || allBytes[offset + 1] != 0x5A)
            {
                offset += 2;
                continue;
            }
            offset += 2;

            if (frameType != 73) continue;

            int recordCount = ((int)dataLength) / recordSize;
            if (enableDetailedDebug)
                Debug.Log($"[GongyiCurveManager] Frame73: slot={frameSlot}, records={recordCount}");

            for (int i = 0; i < recordCount; i++)
            {
                int recOff = dataStart + i * recordSize;
                if (recOff + recordSize > allBytes.Length) break;

                string date = ParseDateFromRecord(allBytes, recOff + 4);
                if (string.IsNullOrEmpty(date)) continue;

                // 字段偏移（均 2 字节，大端，除非注明）:
                // 槽温 offset=16, 铝水平 offset=18, 电解质水平 offset=20
                // 吸出铝量 offset=24（4字节 uint32）, 钙含量 offset=40
                int caowen = (allBytes[recOff + 16] << 8) | allBytes[recOff + 17];
                int lvshuiping = (allBytes[recOff + 18] << 8) | allBytes[recOff + 19];
                int dianjiezhi = (allBytes[recOff + 20] << 8) | allBytes[recOff + 21];
                uint xichulv = (uint)((allBytes[recOff + 24] << 24) | (allBytes[recOff + 25] << 16) |
                                       (allBytes[recOff + 26] << 8) | allBytes[recOff + 27]);
                int gaihanliang = (allBytes[recOff + 40] << 8) | allBytes[recOff + 41];

                if (!dataByDate.TryGetValue(date, out var dp))
                {
                    dp = new DailyDataPoint { Date = date };
                    dataByDate[date] = dp;
                }
                dp.Caowen = caowen;
                dp.AluminumLevel = lvshuiping;
                dp.ElectrolyteLevel = dianjiezhi;
                dp.AluminumOutput = xichulv;
                dp.CalciumContent = gaihanliang;
                dp.Has73Data = true;
            }
        }
    }

    // ==================== 通用 POST 请求 ====================
    private IEnumerator PostFrameRequest(string requestParam, Action<byte[]> onDecoded)
    {
        string jsonBody = JsonConvert.SerializeObject(requestParam);
        byte[] body = Encoding.UTF8.GetBytes(jsonBody);

        if (enableDetailedDebug)
            Debug.Log($"[GongyiCurveManager] 请求: {requestParam}");

        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[GongyiCurveManager] 请求失败: {webRequest.error} code={webRequest.responseCode}");
                onDecoded?.Invoke(null);
                yield break;
            }

            string responseText = webRequest.downloadHandler.text;
            byte[] decoded = DecodeBase64Payload(responseText);

            if (decoded == null && enableDetailedDebug)
            {
                string preview = string.IsNullOrEmpty(responseText) ? "<empty>"
                    : (responseText.Length > 80 ? responseText.Substring(0, 80) + "..." : responseText);
                Debug.LogWarning($"[GongyiCurveManager] Base64 解码失败，预览: {preview}");
            }

            onDecoded?.Invoke(decoded);
        }
    }

    // ==================== 图表渲染 ====================
    private void RenderAllCharts()
    {
        // 确保图表数组有效
        AutoBindReferences();
        if (_allCharts == null || _allCharts.Length != 6)
        {
            Debug.LogError("[GongyiCurveManager] 图表引用未正确绑定，请检查 Inspector");
            return;
        }

        // 再次确保字体已加载（Start 时可能还未就绪）
        if (chineseFontAsset == null)
        {
            AutoLoadChineseFont();
            if (chineseFontAsset == null)
                Debug.LogError("[GongyiCurveManager] ❌ 字体加载失败！请拖拽 TMP_FontAsset 到 Inspector → Chinese Font Asset");
        }

        // ★ 在配置图表前，统一对所有图表设置主题字体并重建对象
        foreach (var chart in _allCharts)
        {
            if (chart == null) continue;
            SetupChartFont(chart);
        }

        // 设置 Content 宽度 = Viewport 宽度
        if (chartsContainer is RectTransform rt && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            float vpWidth = scrollRect.GetComponent<RectTransform>().rect.width;
            if (vpWidth < 1f && scrollRect.viewport != null)
                vpWidth = scrollRect.viewport.rect.width;

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            float totalHeight = 6 * (chartHeight + chartSpacing);
            rt.sizeDelta = new Vector2(vpWidth, totalHeight);

            if (enableDetailedDebug)
                Debug.Log($"[GongyiCurveManager] Content: width={vpWidth}, totalHeight={totalHeight}");
        }

        // 字段提取函数映射
        Func<DailyDataPoint, float>[] valueExtractors =
        {
            dp => dp.WorkVoltage,      // 工作电压
            dp => dp.Caowen,           // 槽温
            dp => dp.AluminumLevel,    // 铝水平
            dp => dp.ElectrolyteLevel, // 电解质水平
            dp => dp.AluminumOutput,   // 吸出铝量
            dp => dp.CalciumContent    // 钙含量
        };

        string[] yUnitSuffixes = { "V", "℃", "cm", "cm", "kg", "%" };

        for (int i = 0; i < 6; i++)
        {
            var chart = _allCharts[i];
            if (chart == null)
            {
                Debug.LogWarning($"[GongyiCurveManager] 图表 [{_chartTitles[i]}] 未绑定");
                continue;
            }

            ConfigureChart(chart, i, _chartTitles[i], valueExtractors[i], yUnitSuffixes[i], _currentData);

            // 设置图表位置（在 Content 内从上到下排列）
            if (chart.TryGetComponent<RectTransform>(out var crt))
            {
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(0, 1);
                crt.pivot = new Vector2(0, 1);
                crt.anchoredPosition = new Vector2(0, -i * (chartHeight + chartSpacing));
                crt.sizeDelta = new Vector2(
                    (chartsContainer is RectTransform crt2) ? crt2.sizeDelta.x : 760f,
                    chartHeight);
            }

            chart.RefreshChart();

            // ★ 兜底：遍历所有子 TextMeshProUGUI，强制覆盖字体（解放之舟可能被保留在缓存中）
            ForceApplyFontToChartTexts(chart);
        }

        // 启用垂直滚动
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }
    }

    /// <summary>在配置前设置主题字体并销毁旧文本对象</summary>
    private void SetupChartFont(LineChart chart)
    {
        if (chineseFontAsset == null) return;
        if (chart == null || chart.theme == null) return;

        // 路径 1：设置 sharedTheme（如果有的话），tmpFont setter 会自动调用 SyncTMPFontToSubComponent
        if (chart.theme.sharedTheme != null)
        {
            chart.theme.sharedTheme.tmpFont = chineseFontAsset;
            // Sync 之后所有 sub-theme (common/axis/title/legend/tooltip 等) 都有正确字体
            Debug.Log($"[GongyiCurveManager] SetupChartFont: sharedTheme({chart.theme.sharedTheme.name}) ← {chineseFontAsset.name}");
        }
        else
        {
            // 没有 sharedTheme：XCharts 内部自动创建了一个默认 Theme，但它的子属性访问 sharedTheme 会 crash
            // 这种情况下，textStyle.tmpFont 直接赋值是唯一可行路径（在 ApplyFontToTextStyle 中处理）
            Debug.LogWarning($"[GongyiCurveManager] {chart.name}.sharedTheme 为空，仅通过 TextStyle 设置字体");
        }

        // 不再在此处 Rebuild，避免 Painter 引用丢失；
        // 字体兜底由 ForceApplyFontToChartTexts 在 ConfigureChart 后统一处理
    }

    /// <summary>兜底：遍历图表所有子 TextMeshProUGUI，强制设置为中文字体</summary>
    private void ForceApplyFontToChartTexts(LineChart chart)
    {
        if (chineseFontAsset == null || chart == null) return;

        var allTmp = chart.GetComponentsInChildren<TextMeshProUGUI>(true);
        int count = 0;
        foreach (var tmp in allTmp)
        {
            if (tmp.font != chineseFontAsset)
            {
                tmp.font = chineseFontAsset;
                count++;
            }
        }

        if (count > 0 && enableDetailedDebug)
            Debug.Log($"[GongyiCurveManager] ForceApplyFont: {chart.name} → {count}/{allTmp.Length} 个 TMP 字体已修正");
    }

    private void ConfigureChart(LineChart chart, int index, string title,
        Func<DailyDataPoint, float> valueExtractor, string unitSuffix,
        List<DailyDataPoint> data)
    {
        chart.ClearData();
        chart.RemoveAllSerie();

        // ---- 主题 ----
        if (chart.theme != null)
        {
            chart.theme.enableCustomTheme = true;
            chart.theme.transparentBackground = false;
            chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255);
        }

        // ---- Grid ----
        var grid = chart.EnsureChartComponent<GridCoord>();
        if (grid != null)
        {
            grid.backgroundColor = new Color32(40, 47, 57, 255);
            grid.showBorder = true;
            grid.borderWidth = 2f;
            grid.borderColor = new Color32(120, 120, 120, 150); // 灰灰色
            grid.left = 60f;
            grid.right = 20f;
            grid.top = 50f;
            grid.backgroundColor = new Color32(30, 34, 45, 120); // 半透明深色背景
            grid.bottom = 50f;
        }

        // ---- 标题 ----
        var titleComp = chart.EnsureChartComponent<Title>();
        if (titleComp != null)
        {
            titleComp.show = false; // 隐藏内置标题，改用自定义悬浮标签
            titleComp.text = title;
            if (titleComp.labelStyle != null && titleComp.labelStyle.textStyle != null)
            {
                titleComp.labelStyle.textStyle.color = Color.white;
                titleComp.labelStyle.textStyle.fontSize = 24;
                titleComp.labelStyle.textStyle.fontStyle = FontStyle.Bold;
                ApplyFontToTextStyle(titleComp.labelStyle.textStyle);
            }
            titleComp.location.align = Location.Align.TopLeft;
            titleComp.location.left = 0.03f;
            titleComp.location.top = 0.02f;
        }

        // ---- X 轴（日期）----
        var xAxis = chart.EnsureChartComponent<XAxis>();
        if (xAxis != null)
        {
            xAxis.type = Axis.AxisType.Category;
            xAxis.axisLine.show = true;
            xAxis.axisTick.show = true;
            xAxis.boundaryGap = true;
            xAxis.splitNumber = 15; // ★ 固定 15 个 X 轴分割，让竖线数量可控

            if (xAxis.axisLabel != null)
            {
                xAxis.axisLabel.show = true;
                xAxis.axisLabel.rotate = 0;
                if (xAxis.axisLabel.textStyle != null)
                {
                    xAxis.axisLabel.textStyle.color = Color.white;
                    xAxis.axisLabel.textStyle.fontSize = 14;
                    xAxis.axisLabel.textStyle.fontStyle = FontStyle.Bold;
                    ApplyFontToTextStyle(xAxis.axisLabel.textStyle);
                }
                xAxis.axisLabel.interval = 0; // 每个刻度都显示标签
            }

            // X轴网格线（固定 14 格竖线）
            xAxis.splitLine.show = true;
            xAxis.splitLine.interval = 0;
            if (xAxis.splitLine.lineStyle == null)
                xAxis.splitLine.lineStyle = new LineStyle();
            xAxis.splitLine.lineStyle.show = true;
            xAxis.splitLine.lineStyle.type = LineStyle.Type.Solid;
            xAxis.splitLine.lineStyle.color = new Color32(120, 120, 120, 150);
            xAxis.splitLine.lineStyle.width = 1f;

            // 补足到 15 个 X 轴数据点，确保始终画 15 条竖线
            int targetXCount = Mathf.Max(data.Count, 15);
            for (int i = 0; i < targetXCount; i++)
            {
                string label = (i < data.Count) ? FormatDateShort(data[i].Date) : "";
                chart.AddXAxisData(label);
            }
            xAxis.SetAllDirty();
        }

        // ---- Y 轴 ----
        var yAxis = chart.EnsureChartComponent<YAxis>();
        if (yAxis != null)
        {
            yAxis.type = Axis.AxisType.Value;
            yAxis.position = Axis.AxisPosition.Left;
            yAxis.axisLine.show = true;
            yAxis.axisTick.show = true;
            yAxis.axisLabel.show = false; // 隐藏Y轴数值标签，让格子更干净
            yAxis.splitLine.show = true;
            if (yAxis.splitLine != null)
            {
                if (yAxis.splitLine.lineStyle == null)
                    yAxis.splitLine.lineStyle = new LineStyle();
                yAxis.splitLine.lineStyle.show = true;
                yAxis.splitLine.lineStyle.type = LineStyle.Type.Solid;
                yAxis.splitLine.lineStyle.color = new Color32(120, 120, 120, 150);
                yAxis.splitLine.lineStyle.width = 1f;
            }
            if (yAxis.axisLabel != null)
            {
                yAxis.axisLabel.numericFormatter = "f0";
                if (yAxis.axisLabel.textStyle != null)
                {
                    yAxis.axisLabel.textStyle.color = Color.white;
                    yAxis.axisLabel.textStyle.fontSize = 16;
                    ApplyFontToTextStyle(yAxis.axisLabel.textStyle);
                }
            }

            // 计算 Y 轴范围（取整，避免小范围时 splitNumber 固定为 5 导致刻度重复）
            var values = data.Where(d => d.Has72Data || d.Has73Data)
                             .Select(dp => valueExtractor(dp))
                             .Where(v => v > 0 || (valueExtractor is not null))
                             .ToList();

            if (values.Count > 0)
            {
                int minInt = Mathf.FloorToInt(values.Min());
                int maxInt = Mathf.CeilToInt(values.Max());
                if (maxInt <= minInt) maxInt = minInt + 1;

                yAxis.min = minInt;
                yAxis.max = maxInt;

                // 固定约 10 条 Y 轴横线（尝试 splitNumber=11 让 XCharts 内部产生 10 条）
                yAxis.splitNumber = 11;
            }

            yAxis.SetAllDirty();
        }

        // ---- DataZoom 禁用 ----
        var dataZoom = chart.EnsureChartComponent<DataZoom>();
        if (dataZoom != null)
        {
            dataZoom.enable = false;
            dataZoom.supportInside = false;
            dataZoom.supportSlider = false;
        }

        // ---- Legend ----
        var legend = chart.EnsureChartComponent<Legend>();
        if (legend != null)
        {
            legend.show = true;
            legend.location.align = Location.Align.TopRight;
            legend.location.top = 0.02f;
            legend.location.right = 0.02f;
            legend.selectedMode = Legend.SelectedMode.Multiple;
            legend.itemGap = 12f;
            if (legend.labelStyle != null && legend.labelStyle.textStyle != null)
            {
                legend.labelStyle.textStyle.color = Color.white;
                legend.labelStyle.textStyle.fontSize = 16;
                ApplyFontToTextStyle(legend.labelStyle.textStyle);
            }
        }

        // ---- Tooltip ----
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        if (tooltip != null)
        {
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;
            if (tooltip.lineStyle != null)
            {
                tooltip.lineStyle.show = true;
                tooltip.lineStyle.width = 2f;
            }

        }

        // ===== Grid背景色 =====
        var gridBg = chart.EnsureChartComponent<GridCoord>();
        if (gridBg != null)
        {
            gridBg.backgroundColor = new Color32(30, 34, 45, 120);
        }

        // ===== 添加折线系列 =====
        Color serieColor = GetChartColor(index);
        var serie = chart.AddSerie<Line>(title);
        if (serie != null)
        {
            if (serie.lineStyle != null)
            {
                serie.lineStyle.color = serieColor;
                serie.lineStyle.width = 2.5f;
            }
            if (serie.symbol != null)
            {
                serie.symbol.show = true;
                serie.symbol.type = SymbolType.Circle;
                serie.symbol.size = 4f;
            }
            if (serie.areaStyle != null)
            {
                serie.areaStyle.show = true; // 开启区域半透明填充
                serie.areaStyle.color = new Color(serieColor.r, serieColor.g, serieColor.b, 0.15f);
                serie.areaStyle.opacity = 0.15f;
            }
            serie.large = false;

            // 添加数据（只添加实际数据点，不补 NaN，曲线正常绘制）
            for (int i = 0; i < data.Count; i++)
            {
                float val = valueExtractor(data[i]);
                var serieData = chart.AddData(0, val);

                // 数值标签（超过30个点自动隐藏，避免拥挤，显示整数）
                var labelStyle = serieData.EnsureComponent<LabelStyle>();
                labelStyle.show = (data.Count <= 30) && (i < data.Count);
                labelStyle.position = LabelStyle.Position.Top;
                labelStyle.offset = new Vector3(0, 8, 0);
                labelStyle.numericFormatter = "F0";
                labelStyle.textStyle.color = serieColor;
                labelStyle.textStyle.fontSize = 14;
                ApplyFontToTextStyle(labelStyle.textStyle);
            }
        }

        // 创建悬浮标题（左上角标签样式）
        CreateOrUpdateFloatingTitle(chart, title);

        // 统一刷新，确保所有配置一次性生效，避免多次重建导致 Painter 引用丢失
        chart.RefreshChart();
    }

    // ==================== 辅助方法 ====================
    private Color GetChartColor(int index)
    {
        Color[] colors =
        {
            new Color32(76, 175, 80, 255),    // 绿色 - 工作电压
            new Color32(244, 67, 54, 255),    // 红色 - 槽温
            new Color32(33, 150, 243, 255),   // 蓝色 - 铝水平
            new Color32(255, 152, 0, 255),    // 橙色 - 电解质水平
            new Color32(156, 39, 176, 255),   // 紫色 - 吸出铝量
            new Color32(0, 188, 212, 255)     // 青色 - 钙含量
        };
        return colors[index % colors.Length];
    }

    private string GetDateFromSelector(riqSelector selector)
    {
        if (selector == null || selector.selectedDisplayText == null) return null;
        return NormalizeDate(selector.selectedDisplayText.text);
    }

    private string GetSlotNumber()
    {
        if (slotNumberSelector != null)
        {
            int slot = slotNumberSelector.GetSelectedSlotNumberValue();
            if (slot > 0) return slot.ToString();
        }
        return null;
    }

    private static string ParseDateFromRecord(byte[] data, int offset)
    {
        if (offset + 3 >= data.Length) return null;
        uint raw = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                          (data[offset + 2] << 8) | data[offset + 3]);

        // 直接 8 位数字
        if (raw >= 20000101 && raw <= 21001231)
            return raw.ToString();

        // 拆开解析
        int year = (int)((raw >> 16) & 0xFFFF);
        int month = (int)((raw >> 8) & 0xFF);
        int day = (int)(raw & 0xFF);
        if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
            return $"{year:D4}{month:D2}{day:D2}";

        return null;
    }

    private static string FormatDateShort(string yyyymmdd)
    {
        if (string.IsNullOrEmpty(yyyymmdd) || yyyymmdd.Length != 8) return yyyymmdd;
        return yyyymmdd.Substring(6, 2); // 只显示日
    }

    private static string NormalizeDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string text = raw.Trim();

        Match m = Regex.Match(text, @"(\d{4})\D?(\d{2})\D?(\d{2})");
        if (m.Success)
        {
            string compact = m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value;
            if (DateTime.TryParseExact(compact, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return compact;
        }

        string[] formats = { "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd" };
        foreach (string fmt in formats)
        {
            if (DateTime.TryParseExact(text, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt.ToString("yyyyMMdd");
        }

        if (DateTime.TryParse(text, out DateTime parsed))
            return parsed.ToString("yyyyMMdd");

        return null;
    }

    private static byte[] DecodeBase64Payload(string responseText)
    {
        if (string.IsNullOrEmpty(responseText)) return null;
        try
        {
            string clean = responseText.Trim().Trim('"');
            int mod = clean.Length % 4;
            if (mod > 0) clean += new string('=', 4 - mod);
            return Convert.FromBase64String(clean);
        }
        catch { return null; }
    }

    private static ushort ReadUInt16BE(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32BE(byte[] data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                      (data[offset + 2] << 8) | data[offset + 3]);
    }

    private void AutoLoadChineseFont()
    {
        if (chineseFontAsset != null) return;

        // 策略1：从已加载内存中搜索
        var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in allFonts)
        {
            string n = f.name.ToLowerInvariant();
            // 优先匹配中文字体关键词，排除 LiberationSans（默认英文）
            if ((n.Contains("fangzheng") || n.Contains("puhuiti") || n.Contains("alibaba") || n.Contains("chinese"))
                && !n.Contains("liberation"))
            {
                chineseFontAsset = f;
                Debug.Log($"[GongyiCurveManager] AutoLoad (FindObjects): {f.name}");
                return;
            }
        }
        // 宽松回退：名字含 SDF（但不含 liberation）
        foreach (var f in allFonts)
        {
            string n = f.name.ToLowerInvariant();
            if (n.Contains("sdf") && !n.Contains("liberation") && !n.Contains("default"))
            {
                chineseFontAsset = f;
                Debug.Log($"[GongyiCurveManager] AutoLoad (FindObjects-fallback): {f.name}");
                return;
            }
        }

        // 策略2：Resources.Load
        chineseFontAsset = Resources.Load<TMP_FontAsset>("Fonts/FangZhengShuSong-GBK-1 SDF");
        if (chineseFontAsset == null)
            chineseFontAsset = Resources.Load<TMP_FontAsset>("font/FangZhengShuSong-GBK-1 SDF");
        if (chineseFontAsset == null)
            chineseFontAsset = Resources.Load<TMP_FontAsset>("FangZhengShuSong-GBK-1 SDF");
        if (chineseFontAsset != null)
        {
            Debug.Log($"[GongyiCurveManager] AutoLoad (Resources): {chineseFontAsset.name}");
            return;
        }

#if UNITY_EDITOR
        // 策略3：Editor 下用 AssetDatabase 直接扫描（100% 能找到）
        var guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset FangZhengShuSong");
        if (guids.Length == 0)
            guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset SDF");
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            string n = font.name.ToLowerInvariant();
            // 跳过默认英文字体
            if (n.Contains("liberation") || font.name == "LiberationSans SDF") continue;
            if (font != null)
            {
                chineseFontAsset = font;
                Debug.Log($"[GongyiCurveManager] AutoLoad (AssetDatabase): {font.name} @ {path}");
                return;
            }
        }
#endif

        Debug.LogWarning("[GongyiCurveManager] ❌ 未找到任何中文字体！请拖拽 TMP_FontAsset 到 Inspector → Chinese Font Asset");
    }

    private void ApplyFontToTextStyle(TextStyle textStyle)
    {
        if (textStyle == null) return;
        if (chineseFontAsset != null)
            textStyle.tmpFont = chineseFontAsset;
    }

    /// <summary>
    /// 为图表创建或更新悬浮标题（左上角标签样式）
    /// </summary>
    private void CreateOrUpdateFloatingTitle(LineChart chart, string title)
    {
        if (chart == null) return;

        Transform titleTrans = chart.transform.Find("FloatingTitle");
        RectTransform titleRT;
        TextMeshProUGUI titleText;
        Image titleBg;

        if (titleTrans == null)
        {
            GameObject titleObj = new GameObject("FloatingTitle");
            titleObj.transform.SetParent(chart.transform, false);
            titleObj.transform.SetAsFirstSibling();

            titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(0, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.anchoredPosition = new Vector2(14, -10);

            titleBg = titleObj.AddComponent<Image>();
            titleBg.color = new Color32(46, 125, 50, 255);

            GameObject textObj = new GameObject("TitleText");
            textObj.transform.SetParent(titleObj.transform, false);
            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 4);
            textRT.offsetMax = new Vector2(-10, -4);

            titleText = textObj.AddComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 18;
            titleText.color = Color.white;
            if (chineseFontAsset != null)
                titleText.font = chineseFontAsset;
        }
        else
        {
            titleRT = titleTrans.GetComponent<RectTransform>();
            titleBg = titleTrans.GetComponent<Image>();
            titleText = titleTrans.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        }

        if (titleText != null)
        {
            titleText.text = title;
            if (chineseFontAsset != null)
                titleText.font = chineseFontAsset;

            float preferredWidth = titleText.GetPreferredValues(title).x;
            if (titleRT != null)
                titleRT.sizeDelta = new Vector2(preferredWidth + 20, 34);
        }
    }

    private void ShowLoading(string msg, bool visible = true)
    {
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(visible);
            if (visible && !string.IsNullOrEmpty(msg))
                loadingText.text = msg;
        }
    }
}
