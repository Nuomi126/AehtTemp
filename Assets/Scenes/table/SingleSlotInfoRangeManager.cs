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
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;
using XCharts.Runtime.UI;
using XCharts.UI.Demo;

public class SingleSlotInfoRangeManager : MonoBehaviour
{
    [Header("输入与按钮")]
    [SerializeField] private TextMeshProUGUI startDateInputText;
    [SerializeField] private TextMeshProUGUI endDateInputText;
    [SerializeField] private TextMeshProUGUI slotInputText;
    [Tooltip("可选：若页面没有查询按钮，可不绑定")]
    [SerializeField] private Button queryButton;

    [Header("选择器（可选，进入页面自动同步）")]
    [SerializeField] private riqSelector startDateSelector;
    [SerializeField] private riqSelector endDateSelector;
    [SerializeField] private SlotNumberSelector slotNumberSelector;
    [SerializeField] private WorkAreaSelector workAreaSelector;

    [Header("页面显示")]
    [SerializeField] private TextMeshProUGUI dateRangeDisplayText;
    [SerializeField] private TextMeshProUGUI slotDisplayText;

    [Header("顶部6个横幅值")]
    [SerializeField] private TextMeshProUGUI bannerSlotValueText;
    [SerializeField] private TextMeshProUGUI bannerSetVoltageValueText;
    [SerializeField] private TextMeshProUGUI bannerAvgVoltageValueText;
    [SerializeField] private TextMeshProUGUI bannerWorkVoltageValueText;
    [SerializeField] private TextMeshProUGUI bannerFeedingValueText;
    [SerializeField] private TextMeshProUGUI bannerTotalPowerValueText;

    [Header("表格")]
    [SerializeField] private SingleSlotInfoDailyTable dailyTable;

    [Header("折线图（可选）")]
    [SerializeField] private SingleSlotInfoCharts dailyCharts;

    [Header("详情面板（可选）")]
    [SerializeField] private SlotDetailPanelManager detailPanelManager;

    [Header("API设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string electrolyticCompanyId = "1033";

    [Header("Frame 73（工艺列）")]
    [SerializeField] private bool queryFrame73 = true;
    [SerializeField] private Frame73Mapping frame73Mapping = new Frame73Mapping();

    [Header("调试")]
    [SerializeField] private bool autoQueryOnEnable = true;
    [SerializeField] private bool autoQueryOnSelectorConfirmed = true;
    [SerializeField] private bool syncDateFromSelectors = true;
    [SerializeField] private bool enableDetailedDebug = true;

    private readonly List<SingleSlotInfoDailyTable.DailySlotInfoRow> _currentRows = new List<SingleSlotInfoDailyTable.DailySlotInfoRow>();

    [Serializable]
    private class Frame73Mapping
    {
        [Header("记录结构")]
        public int recordSize = 70;
        public int dateOffset = 4;

        [Header("温度/分子比/铝水平/电解质水平/出铝量 偏移（2字节，大端；-1表示未配置）")]
        public int temperatureOffset = 16;      // 槽温
        public int moleRatioOffset = 10;        // 分子比
        public int aluminumLevelOffset = 18;    // 铝水平
        public int electrolyteLevelOffset = 20; // 电解质水平
        public int aluminumOutputOffset = 46;   // 出铝总量
        public bool aluminumOutputIsUInt32 = true;

        [Header("缩放（最终值=原始值*scale）")]
        public float temperatureScale = 1f;
        public float moleRatioScale = 0.01f;
        public float aluminumLevelScale = 1f;
        public float electrolyteLevelScale = 1f;
        public float aluminumOutputScale = 1f;
    }

    private void Start()
    {
        ResolveSelectorsIfNeeded();
        InitDefaults();
        BindSelectorEvents();

        if (queryButton != null)
        {
            queryButton.onClick.RemoveListener(OnQueryButtonClick);
            queryButton.onClick.AddListener(OnQueryButtonClick);
        }

        BindDailyTableClickEvents();

        RefreshSelectionDisplay();
    }

    private void OnDestroy()
    {
        if (dailyTable != null)
        {
            dailyTable.onCellClicked -= OnDailyTableCellClicked;
            var table = dailyTable.GetTable();
            if (table != null)
            {
                table.onTableClick = null;
            }
        }
    }

    private void BindDailyTableClickEvents()
    {
        if (dailyTable == null)
        {
            return;
        }

        // 兼容两种点击链路：
        // 1) 本脚本直接绑定 UITable.onTableClick（与 tongjibaobiao 同模式）
        // 2) SingleSlotInfoDailyTable 转发 onCellClicked
        dailyTable.onCellClicked -= OnDailyTableCellClicked;
        dailyTable.onCellClicked += OnDailyTableCellClicked;

        var table = dailyTable.GetTable();
        if (table != null)
        {
            table.raycastTarget = true;
            table.supportSelect = true;
            table.onTableClick = OnDailyTableRawTableClicked;
            table.onPointerClick = OnDailyTablePointerClick;
            Debug.Log("[SingleSlotInfoRangeManager] 已绑定 UITable.onTableClick（行点击跳转已启用）");
        }
    }

    private void OnDailyTablePointerClick(PointerEventData eventData, BaseGraph graph)
    {
        var table = dailyTable != null ? dailyTable.GetTable() : null;
        if (table == null)
        {
            return;
        }

        Vector2 localPos;
        if (!table.ScreenPointToChartPoint(eventData.position, out localPos))
        {
            return;
        }

        int row = -1;
        int column = -1;
        int colCount = table.columns != null ? table.columns.Count : 0;
        if (colCount <= 0 || table.context == null || table.context.dataLabels == null)
        {
            return;
        }

        for (int i = 0; i < table.data.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < colCount; j++)
            {
                int idx = i * colCount + j;
                if (idx < 0 || idx >= table.context.dataLabels.Count)
                {
                    continue;
                }

                var label = table.context.dataLabels[idx];
                if (label != null && label.rect.Contains(localPos))
                {
                    row = i;
                    column = j;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        if (row >= 0 && _currentRows != null && row < _currentRows.Count)
        {
            Debug.Log($"[SingleSlotInfoRangeManager] onPointerClick 命中行: row={row}, col={column}");
            OpenDetailByRow(_currentRows[row]);
        }
    }

    private void OnDailyTableRawTableClicked(int row, int column, TableCell cell)
    {
        if (row < 0)
        {
            return;
        }

        if (_currentRows == null || row >= _currentRows.Count)
        {
            Debug.LogWarning($"[SingleSlotInfoRangeManager] 点击行超出范围: row={row}, rows={(_currentRows == null ? 0 : _currentRows.Count)}");
            return;
        }

        OpenDetailByRow(_currentRows[row]);
    }

    private void OnDailyTableCellClicked(int columnIndex, SingleSlotInfoDailyTable.DailySlotInfoRow row)
    {
        // 现在支持点击整行跳转，不再限制只点日期列。
        OpenDetailByRow(row);
    }

    private void OpenDetailByRow(SingleSlotInfoDailyTable.DailySlotInfoRow row)
    {
        if (row == null)
        {
            return;
        }

        if (detailPanelManager != null)
        {
            // 创建一个虚假的 ReportData 传递给 detailPanelManager
            var dummyData = new StatisticsReportManager.ReportData
            {
                caoh = row.SlotNumber
            };
            
            // 将 "yyyyMMdd" 日期转换为 "yyyy-MM-dd" 如果有可能的话，主要用于显示在明细面板上。
            string displayDate = FormatDateForUi(row.FullDate);
            
            // 调用时，可以传入 "-" 作为工区名
            detailPanelManager.ShowSlotDetail(dummyData, "-", displayDate);
            Debug.Log($"[SingleSlotInfoRangeManager] 已打开详情：槽号={row.SlotNumber} 日期={displayDate}");
        }
        else
        {
            Debug.Log($"[SingleSlotInfoRangeManager] 点击了行：槽号={row.SlotNumber} 日期={row.FullDate}，但未绑定 SlotDetailPanelManager。");
        }
    }

    private void OnEnable()
    {
        ResolveSelectorsIfNeeded();
        BindDailyTableClickEvents();
        BindSelectorEvents();
        InitDefaults();
        RefreshSelectionDisplay();

        if (autoQueryOnEnable)
        {
            OnQueryButtonClick();
        }
    }

    private void OnDisable()
    {
        UnbindSelectorEvents();
    }

    private void InitDefaults()
    {
        SyncInputFromSelectors();

        string today = DateTime.Today.ToString("yyyyMMdd");
        string last30Days = DateTime.Today.AddDays(-30).ToString("yyyyMMdd");

        string startStr = NormalizeDate(startDateInputText != null ? startDateInputText.text : null);
        if (startDateInputText != null && string.IsNullOrEmpty(startStr))
    {
        startDateInputText.text = last30Days;  // 👈 改这里
        if (startDateSelector != null && startDateSelector.selectedDisplayText != null)
        {
            startDateSelector.selectedDisplayText.text = last30Days;  // 👈 改这里
        }
    }

        string endStr = NormalizeDate(endDateInputText != null ? endDateInputText.text : null);
        if (endDateInputText != null && string.IsNullOrEmpty(endStr))
        {
            endDateInputText.text = today;
            if (endDateSelector != null && endDateSelector.selectedDisplayText != null)
            {
                endDateSelector.selectedDisplayText.text = today;
            }
        }

        // Slot Input default
        string slotStr = NormalizeSlot(slotInputText != null ? slotInputText.text : null);
        if (slotInputText != null && string.IsNullOrEmpty(slotStr))
        {
            int selectedSlot = slotNumberSelector != null ? slotNumberSelector.GetSelectedSlotNumberValue() : -1;
            slotInputText.text = selectedSlot > 0 ? selectedSlot.ToString() : "1001";
            if (slotNumberSelector != null && slotNumberSelector.selectedDisplayText != null)
            {
                slotNumberSelector.selectedDisplayText.text = slotInputText.text;
            }
        }

        // WorkArea Input default (if any)
        if (workAreaSelector != null && workAreaSelector.selectedDisplayText != null)
        {
            if (workAreaSelector.selectedDisplayText.text.Contains("选择") || string.IsNullOrWhiteSpace(workAreaSelector.selectedDisplayText.text))
            {
                workAreaSelector.selectedDisplayText.text = "1车间1工区";
            }
        }
    }

    private void ResolveSelectorsIfNeeded()
    {
        if (startDateSelector == null)
        {
            startDateSelector = FindObjectOfType<riqSelector>();
        }

        if (slotNumberSelector == null)
        {
            slotNumberSelector = FindObjectOfType<SlotNumberSelector>();
        }

        if (workAreaSelector == null)
        {
            workAreaSelector = FindObjectOfType<WorkAreaSelector>();
        }

        if (detailPanelManager == null)
        {
            detailPanelManager = FindObjectOfType<SlotDetailPanelManager>();
            if (detailPanelManager == null)
            {
                var all = Resources.FindObjectsOfTypeAll<SlotDetailPanelManager>();
                if (all != null && all.Length > 0)
                {
                    detailPanelManager = all[0];
                }
            }
        }
    }

    private void BindSelectorEvents()
    {
        if (!autoQueryOnSelectorConfirmed)
        {
            return;
        }

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

    private void UnbindSelectorEvents()
    {
        if (startDateSelector != null)
        {
            startDateSelector.OnDateConfirmed.RemoveListener(OnStartDateConfirmed);
        }

        if (endDateSelector != null && endDateSelector != startDateSelector)
        {
            endDateSelector.OnDateConfirmed.RemoveListener(OnEndDateConfirmed);
        }

        if (slotNumberSelector != null)
        {
            slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
        }

        if (workAreaSelector != null)
        {
            workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);
        }
    }

    private void OnStartDateConfirmed(string _)
    {
        SyncInputFromSelectors();
        TryAutoQueryFromSelection();
    }

    private void OnEndDateConfirmed(string _)
    {
        SyncInputFromSelectors();
        TryAutoQueryFromSelection();
    }

    private void OnSlotConfirmed(string _)
    {
        SyncInputFromSelectors();
        TryAutoQueryFromSelection();
    }

    private void OnWorkAreaConfirmed(string _)
    {
        // 切换车间工区后，默认选择该区域的第一个槽号，或直接同步槽号
        SyncInputFromSelectors();
        TryAutoQueryFromSelection();
    }

    private void TryAutoQueryFromSelection()
    {
        string startDate = NormalizeDate(startDateInputText != null ? startDateInputText.text : null);
        string endDate = NormalizeDate(endDateInputText != null ? endDateInputText.text : null);
        string slot = NormalizeSlot(slotInputText != null ? slotInputText.text : null);

        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || string.IsNullOrEmpty(slot))
        {
            if (enableDetailedDebug)
            {
                Debug.Log("[SingleSlotInfoRangeManager] 自动查询跳过：槽号或日期尚未准备好。");
            }
            return;
        }

        if (string.CompareOrdinal(startDate, endDate) > 0)
        {
            if (enableDetailedDebug)
            {
                Debug.Log($"[SingleSlotInfoRangeManager] 自动查询跳过：起始日期大于终止日期 {startDate}>{endDate}");
            }
            return;
        }

        StartCoroutine(QueryRangeAsync(startDate, endDate, slot));
    }

    private void SyncInputFromSelectors()
    {
        if (syncDateFromSelectors && startDateInputText != null && startDateSelector != null && startDateSelector.selectedDisplayText != null)
        {
            string startText = NormalizeDate(startDateSelector.selectedDisplayText.text);
            if (!string.IsNullOrEmpty(startText))
            {
                startDateInputText.text = startText;
            }
        }

        if (syncDateFromSelectors && endDateInputText != null && endDateSelector != null && endDateSelector.selectedDisplayText != null)
        {
            // 避免同一个日期选择器在一次确认中同时覆盖开始和结束日期
            if (endDateSelector != startDateSelector)
            {
                string endText = NormalizeDate(endDateSelector.selectedDisplayText.text);
                if (!string.IsNullOrEmpty(endText))
                {
                    endDateInputText.text = endText;
                }
            }
        }

        if (slotInputText != null && slotNumberSelector != null)
        {
            int selectedSlot = slotNumberSelector.GetSelectedSlotNumberValue();
            if (selectedSlot > 0)
            {
                slotInputText.text = selectedSlot.ToString();
            }
        }
    }

    public void OnQueryButtonClick()
    {
        SyncInputFromSelectors();

        string startDate = NormalizeDate(startDateInputText != null ? startDateInputText.text : null);
        string endDate = NormalizeDate(endDateInputText != null ? endDateInputText.text : null);
        string slot = NormalizeSlot(slotInputText != null ? slotInputText.text : null);

        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
        {
            Debug.LogWarning("[SingleSlotInfoRangeManager] 日期格式无效，请使用 yyyyMMdd 或 yyyy-MM-dd。");
            ClearUi("日期格式无效");
            return;
        }

        if (string.IsNullOrEmpty(slot))
        {
            Debug.LogWarning("[SingleSlotInfoRangeManager] 槽号无效。");
            ClearUi("槽号无效");
            return;
        }

        if (string.CompareOrdinal(startDate, endDate) > 0)
        {
            Debug.LogWarning($"[SingleSlotInfoRangeManager] 起始日期大于终止日期: {startDate} > {endDate}");
            ClearUi("起始日期不能大于终止日期");
            return;
        }

        StartCoroutine(QueryRangeAsync(startDate, endDate, slot));
    }

    private IEnumerator QueryRangeAsync(string startDate, string endDate, string slot)
    {
        _currentRows.Clear();
        RefreshSelectionDisplay(startDate, endDate, slot);

        string companyId = !string.IsNullOrEmpty(GlobalUserData.AlSerialPantId)
            ? GlobalUserData.AlSerialPantId
            : electrolyticCompanyId;

        Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate = new Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow>();

        int before72 = rowByDate.Count;
        yield return QueryAndParseFrame72(companyId, slot, startDate, endDate, rowByDate);
        int after72 = rowByDate.Count;

        if (queryFrame73)
        {
            int merged73 = 0;
            yield return QueryAndMergeFrame73(companyId, slot, startDate, endDate, rowByDate);
            merged73 = CountRowHasProcessFields(rowByDate);
            Debug.Log($"[SingleSlotInfoRangeManager] 结果统计：Frame72日期记录={after72 - before72}，Frame73已合并工艺列={merged73}");
        }
        else
        {
            Debug.Log($"[SingleSlotInfoRangeManager] 结果统计：Frame72日期记录={after72 - before72}，Frame73未启用");
        }

        _currentRows.AddRange(rowByDate.Values);
        _currentRows.Sort((a, b) => string.CompareOrdinal(a.FullDate, b.FullDate));

        if (_currentRows.Count == 0)
        {
            ClearUi("无数据");
            yield break;
        }

        FillBannerAverages(_currentRows);

        if (dailyTable != null)
        {
            dailyTable.SetRows(_currentRows);
        }

        if (dailyCharts != null)
        {
            dailyCharts.SetRows(_currentRows);
        }
    }

    private IEnumerator QueryAndParseFrame72(
        string companyId,
        string slot,
        string startDate,
        string endDate,
        Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate)
    {
        string requestParam = $"{companyId}-72-{slot}-{startDate}-{endDate}";
        byte[] decoded = null;

        yield return PostFrameRequest(requestParam, data => decoded = data);

        if (decoded == null || decoded.Length == 0)
        {
            Debug.LogWarning("[SingleSlotInfoRangeManager] Frame72返回空数据（可能该日期范围无日报）。");
            yield break;
        }

        ParseFrame72Payload(decoded, rowByDate);
    }

    private IEnumerator QueryAndMergeFrame73(
        string companyId,
        string slot,
        string startDate,
        string endDate,
        Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate)
    {
        string requestParam = $"{companyId}-73-{slot}-{startDate}-{endDate}";
        byte[] decoded = null;

        yield return PostFrameRequest(requestParam, data => decoded = data);

        if (decoded == null || decoded.Length == 0)
        {
            Debug.LogWarning("[SingleSlotInfoRangeManager] Frame73返回空数据（工艺列将显示 --）。");
            yield break;
        }

        ParseFrame73Payload(decoded, rowByDate);
    }

    private IEnumerator PostFrameRequest(string requestParam, Action<byte[]> onDecoded)
    {
        string jsonData = JsonConvert.SerializeObject(requestParam);
        byte[] body = Encoding.UTF8.GetBytes(jsonData);

        if (enableDetailedDebug)
        {
            Debug.Log($"[SingleSlotInfoRangeManager] 请求参数: {requestParam}");
        }

        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SingleSlotInfoRangeManager] 请求失败: {webRequest.error}, HTTP={webRequest.responseCode}, param={requestParam}");
                onDecoded?.Invoke(null);
                yield break;
            }

            string responseText = webRequest.downloadHandler.text;
            byte[] decoded = DecodeBase64Payload(responseText);
            if (decoded == null && enableDetailedDebug)
            {
                string preview = string.IsNullOrEmpty(responseText)
                    ? "<empty>"
                    : (responseText.Length > 80 ? responseText.Substring(0, 80) + "..." : responseText);
                Debug.LogWarning($"[SingleSlotInfoRangeManager] 响应解码失败，原始预览: {preview}");
            }
            onDecoded?.Invoke(decoded);
        }
    }

    private static int CountRowHasProcessFields(Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate)
    {
        int count = 0;
        foreach (var kv in rowByDate)
        {
            var r = kv.Value;
            if (!float.IsNaN(r.Temperature) || !float.IsNaN(r.MoleRatio) || !float.IsNaN(r.AluminumLevel) ||
                !float.IsNaN(r.ElectrolyteLevel) || !float.IsNaN(r.AluminumOutput))
            {
                count++;
            }
        }
        return count;
    }

    private void ParseFrame72Payload(byte[] allBytes, Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate)
    {
        int offset = 0;

        while (offset + 14 <= allBytes.Length)
        {
            if (allBytes[offset] != 170 || allBytes[offset + 1] != 85)
            {
                offset++;
                continue;
            }

            int frameStart = offset;
            offset += 2;

            ushort companyId = ReadUInt16BE(allBytes, ref offset);
            ushort frameType = ReadUInt16BE(allBytes, ref offset);
            ushort slot = ReadUInt16BE(allBytes, ref offset);
            uint dataLength = ReadUInt32BE(allBytes, ref offset);

            if (offset + dataLength + 2 > allBytes.Length)
            {
                Debug.LogWarning($"[SingleSlotInfoRangeManager] 帧数据长度异常: frameType={frameType}, dataLength={dataLength}");
                break;
            }

            int dataStart = offset;
            int dataEnd = offset + (int)dataLength;
            offset = dataEnd;

            byte tail1 = allBytes[offset];
            byte tail2 = allBytes[offset + 1];
            offset += 2;

            if (tail1 != 165 || tail2 != 90)
            {
                Debug.LogWarning($"[SingleSlotInfoRangeManager] 帧尾异常: [{tail1},{tail2}]，frameType={frameType}");
                continue;
            }

            if (frameType != 72)
            {
                continue;
            }

            int recordSize = 134;
            int recordCount = ((int)dataLength) / recordSize;

            if (enableDetailedDebug)
            {
                Debug.Log($"[SingleSlotInfoRangeManager] 解析Frame72: company={companyId}, slot={slot}, records={recordCount}, frameStart={frameStart}");
            }

            for (int i = 0; i < recordCount; i++)
            {
                int recOffset = dataStart + i * recordSize;
                if (recOffset + recordSize > allBytes.Length)
                {
                    break;
                }

                var row = ParseFrame72Record(allBytes, recOffset);
                rowByDate[row.FullDate] = row;
            }
        }
    }

    private void ParseFrame73Payload(byte[] allBytes, Dictionary<string, SingleSlotInfoDailyTable.DailySlotInfoRow> rowByDate)
    {
        if (frame73Mapping == null || frame73Mapping.recordSize <= 0)
        {
            Debug.LogWarning("[SingleSlotInfoRangeManager] Frame73已启用，但recordSize未配置，跳过Frame73解析。");
            return;
        }

        int offset = 0;

        while (offset + 14 <= allBytes.Length)
        {
            if (allBytes[offset] != 170 || allBytes[offset + 1] != 85)
            {
                offset++;
                continue;
            }

            offset += 2;
            ushort companyId = ReadUInt16BE(allBytes, ref offset);
            ushort frameType = ReadUInt16BE(allBytes, ref offset);
            ReadUInt16BE(allBytes, ref offset); // 帧头槽号，按日期分组时可不使用
            uint dataLength = ReadUInt32BE(allBytes, ref offset);

            if (offset + dataLength + 2 > allBytes.Length)
            {
                break;
            }

            int dataStart = offset;
            offset += (int)dataLength;

            byte tail1 = allBytes[offset];
            byte tail2 = allBytes[offset + 1];
            offset += 2;

            if (tail1 != 165 || tail2 != 90 || frameType != 73)
            {
                continue;
            }

            int recordCount = ((int)dataLength) / frame73Mapping.recordSize;

            if (enableDetailedDebug)
            {
                Debug.Log($"[SingleSlotInfoRangeManager] 解析Frame73: company={companyId}, records={recordCount}");
            }

            for (int i = 0; i < recordCount; i++)
            {
                int recOffset = dataStart + i * frame73Mapping.recordSize;
                if (recOffset + frame73Mapping.recordSize > allBytes.Length)
                {
                    break;
                }

                string date = ParseDateFromRecordAt(allBytes, recOffset + frame73Mapping.dateOffset);
                if (string.IsNullOrEmpty(date))
                {
                    continue;
                }

                if (!rowByDate.TryGetValue(date, out var row))
                {
                    continue;
                }

                AssignFrame73Field(allBytes, recOffset, frame73Mapping.temperatureOffset, frame73Mapping.temperatureScale, v => row.Temperature = v);
                AssignFrame73Field(allBytes, recOffset, frame73Mapping.moleRatioOffset, frame73Mapping.moleRatioScale, v => row.MoleRatio = v);
                AssignFrame73Field(allBytes, recOffset, frame73Mapping.aluminumLevelOffset, frame73Mapping.aluminumLevelScale, v => row.AluminumLevel = v);
                AssignFrame73Field(allBytes, recOffset, frame73Mapping.electrolyteLevelOffset, frame73Mapping.electrolyteLevelScale, v => row.ElectrolyteLevel = v);
                if (frame73Mapping.aluminumOutputIsUInt32)
                {
                    AssignFrame73FieldUInt32(allBytes, recOffset, frame73Mapping.aluminumOutputOffset, frame73Mapping.aluminumOutputScale, v => row.AluminumOutput = v);
                }
                else
                {
                    AssignFrame73Field(allBytes, recOffset, frame73Mapping.aluminumOutputOffset, frame73Mapping.aluminumOutputScale, v => row.AluminumOutput = v);
                }
            }
        }
    }

    private static void AssignFrame73Field(byte[] data, int recOffset, int fieldOffset, float scale, Action<float> setter)
    {
        if (fieldOffset < 0)
        {
            return;
        }

        int abs = recOffset + fieldOffset;
        if (abs + 1 >= data.Length)
        {
            return;
        }

        int raw = (data[abs] << 8) | data[abs + 1];
        setter?.Invoke(raw * scale);
    }

    private static void AssignFrame73FieldUInt32(byte[] data, int recOffset, int fieldOffset, float scale, Action<float> setter)
    {
        if (fieldOffset < 0)
        {
            return;
        }

        int abs = recOffset + fieldOffset;
        if (abs + 3 >= data.Length)
        {
            return;
        }

        uint raw = (uint)((data[abs] << 24) |
                          (data[abs + 1] << 16) |
                          (data[abs + 2] << 8) |
                          data[abs + 3]);
        setter?.Invoke(raw * scale);
    }

    private static string ParseDateFromRecordAt(byte[] data, int dateAbsOffset)
    {
        if (dateAbsOffset + 3 >= data.Length)
        {
            return null;
        }

        uint rawDate = (uint)((data[dateAbsOffset] << 24) |
                              (data[dateAbsOffset + 1] << 16) |
                              (data[dateAbsOffset + 2] << 8) |
                              data[dateAbsOffset + 3]);

        return ParseDateFromRaw(rawDate);
    }

    private SingleSlotInfoDailyTable.DailySlotInfoRow ParseFrame72Record(byte[] data, int recOffset)
    {
        ushort slot = (ushort)((data[recOffset + 2] << 8) | data[recOffset + 3]);

        uint rawDate = (uint)((data[recOffset + 4] << 24) |
                              (data[recOffset + 5] << 16) |
                              (data[recOffset + 6] << 8) |
                              data[recOffset + 7]);

        int setVoltageMv = (data[recOffset + 14] << 8) | data[recOffset + 15];
        int avgVoltageMv = (data[recOffset + 16] << 8) | data[recOffset + 17];
        int workVoltageMv = (data[recOffset + 18] << 8) | data[recOffset + 19];

        int totalPower = (data[recOffset + 28] << 24) |
                         (data[recOffset + 29] << 16) |
                         (data[recOffset + 30] << 8) |
                         data[recOffset + 31];

        int feedingAmount = (data[recOffset + 34] << 8) | data[recOffset + 35];

        return new SingleSlotInfoDailyTable.DailySlotInfoRow
        {
            SlotNumber = slot,
            FullDate = ParseDateFromRaw(rawDate),
            SetVoltage = setVoltageMv / 1000f,
            AvgVoltage = avgVoltageMv / 1000f,
            WorkVoltage = workVoltageMv / 1000f,
            Temperature = float.NaN,
            MoleRatio = float.NaN,
            AluminumLevel = float.NaN,
            ElectrolyteLevel = float.NaN,
            FeedingAmount = feedingAmount,
            AluminumOutput = float.NaN,
            TotalPower = totalPower
        };
    }

    private void FillBannerAverages(List<SingleSlotInfoDailyTable.DailySlotInfoRow> rows)
    {
        int count = rows.Count;
        if (count == 0)
        {
            ClearBannerValues();
            return;
        }

        float avgSetVoltage = rows.Average(r => r.SetVoltage);
        float avgAvgVoltage = rows.Average(r => r.AvgVoltage);
        float avgWorkVoltage = rows.Average(r => r.WorkVoltage);
        float avgFeeding = rows.Average(r => r.FeedingAmount);
        float avgTotalPower = rows.Average(r => r.TotalPower);

        if (bannerSlotValueText != null)
        {
            bannerSlotValueText.text = rows[0].SlotNumber.ToString();
        }

        if (bannerSetVoltageValueText != null)
        {
            bannerSetVoltageValueText.text = avgSetVoltage.ToString("F3", CultureInfo.InvariantCulture);
        }

        if (bannerAvgVoltageValueText != null)
        {
            bannerAvgVoltageValueText.text = avgAvgVoltage.ToString("F3", CultureInfo.InvariantCulture);
        }

        if (bannerWorkVoltageValueText != null)
        {
            bannerWorkVoltageValueText.text = avgWorkVoltage.ToString("F3", CultureInfo.InvariantCulture);
        }

        if (bannerFeedingValueText != null)
        {
            bannerFeedingValueText.text = avgFeeding.ToString("F0", CultureInfo.InvariantCulture);
        }

        if (bannerTotalPowerValueText != null)
        {
            bannerTotalPowerValueText.text = avgTotalPower.ToString("F0", CultureInfo.InvariantCulture);
        }
    }

    private void RefreshSelectionDisplay(string startDate = null, string endDate = null, string slot = null)
    {
        string finalStart = string.IsNullOrEmpty(startDate)
            ? NormalizeDate(startDateInputText != null ? startDateInputText.text : null)
            : startDate;

        string finalEnd = string.IsNullOrEmpty(endDate)
            ? NormalizeDate(endDateInputText != null ? endDateInputText.text : null)
            : endDate;

        string finalSlot = string.IsNullOrEmpty(slot)
            ? NormalizeSlot(slotInputText != null ? slotInputText.text : null)
            : slot;

        if (dateRangeDisplayText != null)
        {
            string from = FormatDateForUi(finalStart);
            string to = FormatDateForUi(finalEnd);
            dateRangeDisplayText.text = $"{from} - {to}";
        }

        if (slotDisplayText != null)
        {
            slotDisplayText.text = finalSlot;
        }
    }

    private void ClearUi(string reason)
    {
        ClearBannerValues();

        if (dailyTable != null)
        {
            dailyTable.ClearAndShowMessage(reason);
        }

        if (dailyCharts != null)
        {
            dailyCharts.ClearCharts();
        }
    }

    private void ClearBannerValues()
    {
        SetText(bannerSlotValueText, "--");
        SetText(bannerSetVoltageValueText, "--");
        SetText(bannerAvgVoltageValueText, "--");
        SetText(bannerWorkVoltageValueText, "--");
        SetText(bannerFeedingValueText, "--");
        SetText(bannerTotalPowerValueText, "--");
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static string NormalizeSlot(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string trimmed = raw.Trim();
        if (int.TryParse(trimmed, out int slot) && slot > 0)
        {
            return slot.ToString();
        }

        return null;
    }

    private static string NormalizeDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string text = raw.Trim();

        // 优先提取连续8位数字，兼容“2026-03-07”“2026年03月07日”等文本
        Match m = Regex.Match(text, @"(\d{4})\D?(\d{2})\D?(\d{2})");
        if (m.Success)
        {
            string compact = m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value;
            if (DateTime.TryParseExact(compact, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d0))
            {
                return d0.ToString("yyyyMMdd");
            }
        }

        string[] formats = { "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd" };

        foreach (string fmt in formats)
        {
            if (DateTime.TryParseExact(text, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                return dt.ToString("yyyyMMdd");
            }
        }

        if (DateTime.TryParse(text, out DateTime parsed))
        {
            return parsed.ToString("yyyyMMdd");
        }

        return null;
    }

    private static string ParseDateFromRaw(uint rawDate)
    {
        if (rawDate >= 20000101 && rawDate <= 21001231)
        {
            string s = rawDate.ToString();
            return s.Length == 8 ? s : DateTime.Today.ToString("yyyyMMdd");
        }

        int year = (int)((rawDate >> 16) & 0xFFFF);
        int month = (int)((rawDate >> 8) & 0xFF);
        int day = (int)(rawDate & 0xFF);

        if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
        {
            return $"{year:D4}{month:D2}{day:D2}";
        }

        return DateTime.Today.ToString("yyyyMMdd");
    }

    private static string FormatDateForUi(string yyyymmdd)
    {
        if (string.IsNullOrEmpty(yyyymmdd) || yyyymmdd.Length != 8)
        {
            return "----.--.--";
        }

        return $"{yyyymmdd.Substring(0, 4)}.{yyyymmdd.Substring(4, 2)}.{yyyymmdd.Substring(6, 2)}";
    }

    private static byte[] DecodeBase64Payload(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            return null;
        }

        try
        {
            string clean = responseText.Trim().Trim('"');
            int mod = clean.Length % 4;
            if (mod > 0)
            {
                clean += new string('=', 4 - mod);
            }

            return Convert.FromBase64String(clean);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static ushort ReadUInt16BE(byte[] data, ref int offset)
    {
        ushort v = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        return v;
    }

    private static uint ReadUInt32BE(byte[] data, ref int offset)
    {
        uint v = (uint)((data[offset] << 24) |
                        (data[offset + 1] << 16) |
                        (data[offset + 2] << 8) |
                        data[offset + 3]);
        offset += 4;
        return v;
    }
}
