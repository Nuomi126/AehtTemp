using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using XCharts.Runtime.UI;
using System.Text.RegularExpressions;

public class EfficiencyReportManager : MonoBehaviour
{
    [Header("API 设定")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    public string enterpriseId = "1033";
    
    [Header("筛选条件 (工区/日期)")]
    public string currentSlotOrArea = "1001"; 
    public string currentDate = "20241201";

    [Header("当日报表 - 表格")]
    public UITable dangri_UITable_Efficient;
    public UITable dangri_UITable_Summary;

    [Header("历史统计 - 表格")]
    public UITable lishi_UITable_Efficient;
    public UITable lishi_UITable_Summary;

    [Header("详情跳转面板 (引用)")]
    public EfficiencyDetailPanelManager detailPanelManager;

    [Header("模式切换按钮(可选)")]
    public UnityEngine.UI.Button btnDangri;
    public UnityEngine.UI.Button btnLishi;

    [Header("数据策略")]
    public bool useMockDataWhenApiUnavailable = true;

    [Header("自适应列宽")]
    public int tableColumnCount = 6;
    public float tableHorizontalPadding = 24f;
    public float minAutoColumnWidth = 52f;

    private readonly List<Frame74Data> currentDangriRows = new List<Frame74Data>();
    private readonly List<Frame74Data> currentLishiRows = new List<Frame74Data>();
    private readonly List<SummaryRowData> currentDangriSummaryRows = new List<SummaryRowData>();
    private readonly List<SummaryRowData> currentLishiSummaryRows = new List<SummaryRowData>();

    private class SummaryRowData
    {
        public string label;
        public int occurCount;
        public float duration;
        public float peak;
        public float avg;
        public float power;
    }

    private void Start()
    {
        AutoBindMissingReferences();
        // 绑定点击事件
        BindTableClickEvents();
        BindModeButtons();
        ConfigureTableLayouts();

        // 默认获取当日数据
        FetchData("dangri");
    }

    private void AutoBindMissingReferences()
    {
        if (detailPanelManager == null)
            detailPanelManager = FindObjectOfType<EfficiencyDetailPanelManager>(true);

        if (dangri_UITable_Efficient == null)
            dangri_UITable_Efficient = TryFindTableByKeywords("dangri", "efficient", "effect");
        if (dangri_UITable_Summary == null)
            dangri_UITable_Summary = TryFindTableByKeywords("dangri", "summary");
        if (lishi_UITable_Efficient == null)
            lishi_UITable_Efficient = TryFindTableByKeywords("lishi", "efficient", "effect");
        if (lishi_UITable_Summary == null)
            lishi_UITable_Summary = TryFindTableByKeywords("lishi", "summary");

        if (btnDangri == null)
        {
            var go = GameObject.Find("btnDangri") ?? GameObject.Find("BtnDangri");
            if (go != null) btnDangri = go.GetComponent<UnityEngine.UI.Button>();
        }

        if (btnLishi == null)
        {
            var go = GameObject.Find("btnLishi") ?? GameObject.Find("BtnLishi");
            if (go != null) btnLishi = go.GetComponent<UnityEngine.UI.Button>();
        }
    }

    private UITable TryFindTableByKeywords(params string[] keywords)
    {
        var allTables = GetComponentsInChildren<UITable>(true);
        for (int i = 0; i < allTables.Length; i++)
        {
            var n = allTables[i].name.ToLowerInvariant();
            bool allHit = true;
            for (int k = 0; k < keywords.Length; k++)
            {
                if (!n.Contains(keywords[k]))
                {
                    allHit = false;
                    break;
                }
            }

            if (allHit) return allTables[i];
        }

        return null;
    }

    private void BindModeButtons()
    {
        if (btnDangri != null)
        {
            btnDangri.onClick.RemoveListener(OnDangriButtonClicked);
            btnDangri.onClick.AddListener(OnDangriButtonClicked);
        }

        if (btnLishi != null)
        {
            btnLishi.onClick.RemoveListener(OnLishiButtonClicked);
            btnLishi.onClick.AddListener(OnLishiButtonClicked);
        }
    }

    private void OnDangriButtonClicked()
    {
        FetchData("dangri");
    }

    private void OnLishiButtonClicked()
    {
        FetchData("lishi");
    }

    private void BindTableClickEvents()
    {
        if (dangri_UITable_Efficient != null)
            dangri_UITable_Efficient.onTableClick = (row, col, cell) => OnUITableClicked(row, col, cell, "dangri_effect");
        
        if (dangri_UITable_Summary != null)
            dangri_UITable_Summary.onTableClick = (row, col, cell) => OnUITableClicked(row, col, cell, "dangri_summary");

        if (lishi_UITable_Efficient != null)
            lishi_UITable_Efficient.onTableClick = (row, col, cell) => OnUITableClicked(row, col, cell, "lishi_effect");

        if (lishi_UITable_Summary != null)
            lishi_UITable_Summary.onTableClick = (row, col, cell) => OnUITableClicked(row, col, cell, "lishi_summary");
    }

    private void OnUITableClicked(int row, int column, TableCell cell, string tableType)
    {
        // row为-1表示点击了表头，忽略
        if (row < 0 || detailPanelManager == null) return;

        UITable targetTable = null;
        bool isSummary = false;

        switch (tableType)
        {
            case "dangri_effect": targetTable = dangri_UITable_Efficient; break;
            case "lishi_effect": targetTable = lishi_UITable_Efficient; break;
            case "dangri_summary": targetTable = dangri_UITable_Summary; isSummary = true; break;
            case "lishi_summary": targetTable = lishi_UITable_Summary; isSummary = true; break;
        }

        if (targetTable == null || row >= targetTable.data.Count) return;

        // XCharts 中取出对应行的数据
        List<TableCell> rowData = targetTable.data[row].data;
        
        // 解析基础数据，防止越界或转换异常
        try
        {
            if (isSummary)
            {
                string areaName = rowData.Count > 0 ? rowData[0].stringValue : "";
                string dateStr = DateTime.Now.ToString("yyyy.MM.dd");
                string mode = tableType.StartsWith("lishi") ? "lishi" : "dangri";
                var summaryCache = mode == "dangri" ? currentDangriSummaryRows : currentLishiSummaryRows;
                int occurCount = 0;
                float duration = 0f, peak = 0f, avg = 0f, power = 0f;
                if (row >= 0 && row < summaryCache.Count)
                {
                    occurCount = summaryCache[row].occurCount;
                    duration = summaryCache[row].duration;
                    peak = summaryCache[row].peak;
                    avg = summaryCache[row].avg;
                    power = summaryCache[row].power;
                    if (string.IsNullOrEmpty(areaName)) areaName = summaryCache[row].label;
                }
                detailPanelManager.ShowWorkAreaDetail(areaName, dateStr, occurCount, duration, peak, avg, power, mode);
            }
            else
            {
                string areaName = "青铜峡135KA"; // 从大下拉框获取，这里预留
                string slotNo = rowData.Count > 0 ? rowData[0].stringValue : "";
                string dateStr = DateTime.Now.ToString("yyyy.MM.dd");
                string mode = tableType.StartsWith("lishi") ? "lishi" : "dangri";
                var cache = mode == "dangri" ? currentDangriRows : currentLishiRows;
                int occurCount = 0;
                float duration = 0f, peak = 0f, avg = 0f, power = 0f;
                if (row >= 0 && row < cache.Count)
                {
                    occurCount = cache[row].occurrenceCount;
                    duration = cache[row].effectDuration;
                    peak = cache[row].peakVoltage;
                    avg = cache[row].averageVoltage;
                    power = cache[row].powerConsumption;
                }
                detailPanelManager.ShowSlotDetail(areaName, slotNo, dateStr, occurCount, duration, peak, avg, power, mode);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析表格行数据失败: {e.Message}");
        }
    }

    public void FetchData(string mode)
    {
        // 先清理旧数据，提供一个视觉反馈表示正在加载
        if (mode == "dangri")
        {
            if (dangri_UITable_Efficient != null) dangri_UITable_Efficient.ClearData();
            if (dangri_UITable_Summary != null) dangri_UITable_Summary.ClearData();
        }
        else
        {
            if (lishi_UITable_Efficient != null) lishi_UITable_Efficient.ClearData();
            if (lishi_UITable_Summary != null) lishi_UITable_Summary.ClearData();
        }

        StartCoroutine(RoutineFetchEffectData(mode));
    }

    /// <summary>
    /// UI面板上如果选择了“日期”可以调用此事件
    /// </summary>
    public void SetDateAndFetch(string dateStr)
    {
        // 将类似 2024.07.01 或者 2024-07-01 转换成 API 需要的 20240701
        currentDate = dateStr.Replace(".", "").Replace("-", "").Replace("/", "");
        
        // 自动拉取更新（如果需要区分当日或历史，可以传入带判断的mode，通常日期变更是在历史报表）
        FetchData("lishi");
    }

    /// <summary>
    /// UI面板如果切换了“工区”或“车间”的下拉框，可以调用此方法
    /// </summary>
    public void SetAreaAndFetch(string areaNameOrId)
    {
        // 此处的 areaNameOrId 这里先简单存储，你可以根据实际需要映射到具体的id
        currentSlotOrArea = areaNameOrId;
        FetchData("lishi");
    }

    private IEnumerator RoutineFetchEffectData(string mode)
    {
        // 按照旧系统的接口和参数格式（1033-74-槽号-日期）请求
        string requestDate = string.IsNullOrEmpty(currentDate) ? DateTime.Now.ToString("yyyyMMdd") : currentDate;
        string requestParam = $"{enterpriseId}-74-{currentSlotOrArea}-{requestDate}-{requestDate}";
        string jsonRequest = $"\"{requestParam}\"";
        Debug.Log($"[EfficiencyReport] 开始请求 API: {apiUrl} | mode={mode} | param={requestParam}");
        
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[{mode}] Fetch 成功 (Frame 74): " + (responseText.Length > 100 ? responseText.Substring(0,100)+"..." : responseText));
                
                // 优先用真实数据解析，解析失败时按配置回退演示数据。
                PopulateTables(mode, responseText);
            }
            else
            {
                // 改成 LogWarning 而不是 LogError，这样控制台就不会报红报错了，只会提示黄字
                Debug.LogWarning($"[请求失败或无数据] API: {apiUrl} | 参数: {requestParam} | 抛出错误: {request.error}");
                
                if (useMockDataWhenApiUnavailable)
                {
                    PopulateTables(mode, "");
                }
            }
        }
    }

    private void PopulateTables(string mode, string jsonResponse)
    {
        UITable targetEfficientTable = mode == "dangri" ? dangri_UITable_Efficient : lishi_UITable_Efficient;
        UITable targetSummaryTable = mode == "dangri" ? dangri_UITable_Summary : lishi_UITable_Summary;
        List<Frame74Data> effectiveRows = null;
        var cacheRows = mode == "dangri" ? currentDangriRows : currentLishiRows;
        var cacheSummary = mode == "dangri" ? currentDangriSummaryRows : currentLishiSummaryRows;
        cacheRows.Clear();
        cacheSummary.Clear();

        if (targetEfficientTable != null)
        {
            // 清理旧数据
            targetEfficientTable.ClearData();
            targetEfficientTable.SetColumns(new List<string> { "槽号", "发生次数", "持续时间", "峰压", "均压", "功耗" });

            List<Frame74Data> parsedRows = ParseFrame74RowsWithFallback(jsonResponse);
            if (parsedRows != null && parsedRows.Count > 0)
            {
                effectiveRows = parsedRows;
                foreach (var row in parsedRows)
                {
                    cacheRows.Add(row);
                    targetEfficientTable.AddRow(new List<string>
                    {
                        string.IsNullOrEmpty(row.slotId) ? "-" : row.slotId,
                        row.occurrenceCount.ToString(),
                        row.effectDuration.ToString("F2"),
                        row.peakVoltage.ToString("F3"),
                        row.averageVoltage.ToString("F3"),
                        row.powerConsumption.ToString("F3")
                    });
                }
                Debug.Log($"[EfficiencyReport] 已填充真实数据 {parsedRows.Count} 行。");
            }
            else if (useMockDataWhenApiUnavailable)
            {
                // 回退演示数据（固定值，不是随机数）
                effectiveRows = new List<Frame74Data>();
                for (int i = 0; i < 5; i++)
                {
                    string slotNo = "203" + i;
                    string occurTime = "01:42:00";
                    float duration = 3.97f;
                    float peak = 3.984f;
                    float avg = 3.98f;
                    float power = 3.98f;

                    targetEfficientTable.AddRow(new List<string> { slotNo, occurTime, duration.ToString("F2"), peak.ToString("F3"), avg.ToString("F3"), power.ToString("F3") });
                    effectiveRows.Add(new Frame74Data
                    {
                        slotId = slotNo,
                        occurrenceTime = occurTime,
                        effectDuration = duration,
                        peakVoltage = peak,
                        averageVoltage = avg,
                        powerConsumption = power
                    });
                    cacheRows.Add(effectiveRows[effectiveRows.Count - 1]);
                }
                Debug.LogWarning("[EfficiencyReport] 使用演示数据回退（API无可解析数据）。");
            }
        }

        if (targetSummaryTable != null)
        {
            targetSummaryTable.ClearData();
            targetSummaryTable.SetColumns(new List<string> { "槽号", "发生次数", "持续时间", "峰压", "均压", "功耗" });

            if (effectiveRows != null && effectiveRows.Count > 0)
            {
                var summaryRows = BuildSummaryRows(effectiveRows);
                for (int i = 0; i < summaryRows.Count; i++)
                {
                    var s = summaryRows[i];
                    cacheSummary.Add(s);
                    targetSummaryTable.AddRow(new List<string>
                    {
                        s.label,
                        s.occurCount.ToString(),
                        s.duration.ToString("F2"),
                        s.peak.ToString("F3"),
                        s.avg.ToString("F3"),
                        s.power.ToString("F3")
                    });
                }
            }
        }

        ForceRefreshTable(targetEfficientTable);
        ForceRefreshTable(targetSummaryTable);
    }

            private List<SummaryRowData> BuildSummaryRows(List<Frame74Data> rows)
    {
        var result = new List<SummaryRowData>();
        if (rows == null || rows.Count == 0) return result;

        // 获取第一行的数据作为目标车间和工区
        string targetWorkshop = rows[0].workshopName;
        string targetArea = rows[0].areaName;
        
        // 1. 目标工区平均
        float areaDur = 0, areaPeak = 0, areaAvg = 0, areaPower = 0;
        int areaCount = 0, areaOccur = 0;
        
        // 2. 目标车间平均
        float workDur = 0, workPeak = 0, workAvg = 0, workPower = 0;
        int workCount = 0, workOccur = 0;
        
        // 3. 系列(全部)平均
        float allDur = 0, allPeak = 0, allAvg = 0, allPower = 0;
        int allCount = 0, allOccur = 0;

        foreach (var r in rows)
        {
            allDur += r.effectDuration;
            allPeak += r.peakVoltage;
            allAvg += r.averageVoltage;
            allPower += r.powerConsumption;
            allOccur += r.occurrenceCount;
            allCount++;

            if (r.workshopName == targetWorkshop)
            {
                workDur += r.effectDuration;
                workPeak += r.peakVoltage;
                workAvg += r.averageVoltage;
                workPower += r.powerConsumption;
                workOccur += r.occurrenceCount;
                workCount++;
                
                if (r.areaName == targetArea)
                {
                    areaDur += r.effectDuration;
                    areaPeak += r.peakVoltage;
                    areaAvg += r.averageVoltage;
                    areaPower += r.powerConsumption;
                    areaOccur += r.occurrenceCount;
                    areaCount++;
                }
            }
        }

        // Add Target Area row
        result.Add(new SummaryRowData
        {
            label = string.IsNullOrEmpty(targetArea) ? "当前工区" : targetArea,
            occurCount = areaOccur,
            duration = areaCount > 0 ? areaDur / areaCount : 0,
            peak = areaCount > 0 ? areaPeak / areaCount : 0,
            avg = areaCount > 0 ? areaAvg / areaCount : 0,
            power = areaCount > 0 ? areaPower / areaCount : 0
        });

        // Add Target Workshop row
        result.Add(new SummaryRowData
        {
            label = string.IsNullOrEmpty(targetWorkshop) ? "当前车间" : targetWorkshop,
            occurCount = workOccur,
            duration = workCount > 0 ? workDur / workCount : 0,
            peak = workCount > 0 ? workPeak / workCount : 0,
            avg = workCount > 0 ? workAvg / workCount : 0,
            power = workCount > 0 ? workPower / workCount : 0
        });

        // Add Series row
        result.Add(new SummaryRowData
        {
            label = "全系列",
            occurCount = allOccur,
            duration = allCount > 0 ? allDur / allCount : 0,
            peak = allCount > 0 ? allPeak / allCount : 0,
            avg = allCount > 0 ? allAvg / allCount : 0,
            power = allCount > 0 ? allPower / allCount : 0
        });

        return result;
    }

    private SummaryRowData AggregateRows(string label, List<Frame74Data> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return new SummaryRowData { label = label, occurCount = 0, duration = 0, peak = 0, avg = 0, power = 0 };
        }

        float count = rows.Count;
        float sumDuration = 0f, sumPeak = 0f, sumAvg = 0f, sumPower = 0f;
        int sumOccur = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            sumOccur += rows[i].occurrenceCount;
            sumDuration += rows[i].effectDuration;
            sumPeak += rows[i].peakVoltage;
            sumAvg += rows[i].averageVoltage;
            sumPower += rows[i].powerConsumption;
        }
        return new SummaryRowData
        {
            label = label,
            occurCount = Mathf.RoundToInt(sumOccur / count),
            duration = sumDuration / count,
            peak = sumPeak / count,
            avg = sumAvg / count,
            power = sumPower / count
        };
    }

    private void TryGetSelectedWorkshopAndArea(ref int workshop, ref int area, List<Frame74Data> rows)
    {
        var m = Regex.Match(currentSlotOrArea ?? "", @"(\d+)\s*车间.*?(\d+)\s*工区");
        if (m.Success)
        {
            int.TryParse(m.Groups[1].Value, out workshop);
            int.TryParse(m.Groups[2].Value, out area);
            return;
        }

        if (rows != null && rows.Count > 0 && ushort.TryParse(rows[0].slotId, out ushort slot))
        {
            var cfg = GlobalElectrolyticCellDataManager.Instance?.GetConfigBySlotNumber(slot);
            if (cfg != null)
            {
                workshop = cfg.workshopNumber;
                area = cfg.workAreaNumber;
            }
        }
    }

    private List<Frame74Data> FilterRowsByWorkshop(List<Frame74Data> rows, int workshop)
    {
        var list = new List<Frame74Data>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (!ushort.TryParse(rows[i].slotId, out ushort slot)) continue;
            var cfg = GlobalElectrolyticCellDataManager.Instance.GetConfigBySlotNumber(slot);
            if (cfg != null && cfg.workshopNumber == workshop) list.Add(rows[i]);
        }
        return list.Count > 0 ? list : rows;
    }

    private List<Frame74Data> FilterRowsByWorkshopAndArea(List<Frame74Data> rows, int workshop, int area)
    {
        var list = new List<Frame74Data>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (!ushort.TryParse(rows[i].slotId, out ushort slot)) continue;
            var cfg = GlobalElectrolyticCellDataManager.Instance.GetConfigBySlotNumber(slot);
            if (cfg != null && cfg.workshopNumber == workshop && cfg.workAreaNumber == area) list.Add(rows[i]);
        }
        return list.Count > 0 ? list : rows;
    }

    private void ConfigureTableLayouts()
    {
        ConfigureSingleTable(dangri_UITable_Efficient, tableColumnCount, tableHorizontalPadding);
        ConfigureSingleTable(dangri_UITable_Summary, tableColumnCount, tableHorizontalPadding);
        ConfigureSingleTable(lishi_UITable_Efficient, tableColumnCount, tableHorizontalPadding);
        ConfigureSingleTable(lishi_UITable_Summary, tableColumnCount, tableHorizontalPadding);
    }

    private void ConfigureSingleTable(UITable table, int columns, float horizontalPadding)
    {
        if (table == null) return;

        float columnWidth = CalculateAutoColumnWidth(table, columns, horizontalPadding);
        float minColumnWidth = Mathf.Min(columnWidth, Mathf.Max(36f, minAutoColumnWidth));

        table.supportScroll = true;
        table.scrollSensitivity = 10f;
        if (table.scrollbarStyle != null)
        {
            table.scrollbarStyle.show = true;
            table.scrollbarStyle.horizontal = false;
            table.scrollbarStyle.vertical = true;
            table.scrollbarStyle.width = 10f;
        }

        if (table.columnStyle != null)
        {
            table.columnStyle.columnWidth = columnWidth;
            table.columnStyle.minColumnWidth = minColumnWidth;
        }

        table.SetAllDirty();
        table.RefreshGraph();
        table.RefreshTable();
    }

    private float CalculateAutoColumnWidth(UITable table, int columns, float horizontalPadding)
    {
        int validColumns = Mathf.Max(1, columns);
        float widthBase = Screen.width;
        if (table != null)
        {
            var rt = table.GetComponent<RectTransform>();
            if (rt != null)
            {
                var rectWidth = rt.rect.width;
                if (rectWidth > 10f) widthBase = rectWidth;
            }
        }

        float usableWidth = Mathf.Max(160f, widthBase - horizontalPadding * 2f);
        float width = usableWidth / validColumns;
        return Mathf.Max(36f, width);
    }

    private void ForceRefreshTable(UITable table)
    {
        if (table == null) return;
        table.SetAllDirty();
        table.RefreshGraph();
        table.RefreshTable();
    }

    private List<Frame74Data> ParseFrame74RowsWithFallback(string response)
    {
        List<Frame74Data> rows = TryParseFrame74Rows(response);
        if (rows != null && rows.Count > 0) return rows;

        rows = TryParseFrame74BinaryRows(response);
        if (rows != null && rows.Count > 0)
        {
            Debug.Log($"[EfficiencyReport] Base64帧解析成功，得到 {rows.Count} 条记录。");
            return rows;
        }

        return null;
    }

    private byte[] ParseBase64Response(string response)
    {
        if (string.IsNullOrEmpty(response)) return null;
        try
        {
            string clean = response.Trim('"').Trim();
            int padding = clean.Length % 4;
            if (padding > 0) clean += new string('=', 4 - padding);
            return Convert.FromBase64String(clean);
        }
        catch
        {
            return null;
        }
    }

        private List<Frame74Data> TryParseFrame74BinaryRows(string response)
    {
        byte[] fullData = ParseBase64Response(response);
        if (fullData == null || fullData.Length < 14) return null;

        byte[] validData = fullData;
        int startIndex = 0;
        
        // 解析包头，如果是170, 85开头，则是带有包头格式
        if (fullData[0] == 170 && fullData[1] == 85)
        {
            int offset = 8;
            uint dataLength = ReadUInt32BE(fullData, offset);
            offset += 4;
            if (offset + dataLength <= fullData.Length)
            {
                validData = new byte[dataLength];
                Array.Copy(fullData, offset, validData, 0, (int)dataLength);
            }
        }

        int recordSize = 58; // 根据新文档，有效数据长度为58的整数倍
        if (validData.Length % recordSize != 0) 
        {
            Debug.LogWarning("[EfficiencyReport] invalid binary length: " + validData.Length);
            // 尝试忽略末尾多余字节
        }

        int count = validData.Length / recordSize;
        if (count <= 0) return null;

        var rows = new List<Frame74Data>(count);
        for (int i = 0; i < count; i++)
        {
            int recOffset = i * recordSize;
            if (recOffset + recordSize > validData.Length) break;

            ushort recordId = ReadUInt16BE(validData, recOffset);
            ushort slot = ReadUInt16BE(validData, recOffset + 2);
            uint date = ReadUInt32BE(validData, recOffset + 4);
            uint timeRaw = ReadUInt32BE(validData, recOffset + 8);
            byte workshopNo = validData[recOffset + 12];
            byte areaNo = validData[recOffset + 13];
            uint duration = ReadUInt32BE(validData, recOffset + 14);
            uint peakRaw = ReadUInt32BE(validData, recOffset + 18);
            uint avgRaw = ReadUInt32BE(validData, recOffset + 22);
            uint powerRaw = ReadUInt32BE(validData, recOffset + 26);

            if (slot == 0) continue;

            float peak = peakRaw / 1000f;        
            float avg = avgRaw / 1000f;
            float power = powerRaw / 1000f;

            rows.Add(new Frame74Data
            {
                slotId = slot.ToString(),
                occurrenceTime = timeRaw > 0 ? timeRaw.ToString() : "-",
                occurrenceCount = 1, // 每次记录视为发生1次
                effectDuration = duration,
                peakVoltage = peak,
                averageVoltage = avg,
                powerConsumption = power,
                workshopName = workshopNo + "车间",
                areaName = workshopNo + "车间" + areaNo + "工区"
            });
        }

        return rows.Count > 0 ? rows : null;
    }

    private static ushort ReadUInt16BE(byte[] data, int index)
    {
        if (data == null || index < 0 || index + 1 >= data.Length) return 0;
        return (ushort)((data[index] << 8) | data[index + 1]);
    }

    private static uint ReadUInt32BE(byte[] data, int index)
    {
        if (data == null || index < 0 || index + 3 >= data.Length) return 0;
        return (uint)((data[index] << 24) | (data[index + 1] << 16) | (data[index + 2] << 8) | data[index + 3]);
    }

    private float CalculateAutoColumnWidth()
    {
        int columns = Mathf.Max(1, tableColumnCount);
        float screenWidth = Screen.width;

        float usableWidth = Mathf.Max(200f, screenWidth - tableHorizontalPadding * 2f);
        float width = usableWidth / columns;
        return Mathf.Max(36f, width);
    }

    private List<Frame74Data> TryParseFrame74Rows(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse)) return null;

        try
        {
            var wrapped = JsonConvert.DeserializeObject<AlwitResponse_Frame74>(jsonResponse);
            if (wrapped != null && wrapped.data != null && wrapped.data.Count > 0)
            {
                return wrapped.data;
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            var list = JsonConvert.DeserializeObject<List<Frame74Data>>(jsonResponse);
            if (list != null && list.Count > 0)
            {
                return list;
            }
        }
        catch
        {
            // ignored
        }

        Debug.LogWarning("[EfficiencyReport] API返回未匹配到Frame74 JSON结构，未使用该返回。");
        return null;
    }

    // 在 UITable_Effect 点击行的时候触发
    public void OnSlotRowClicked(string areaName, string slotNo, string dateStr, string occurTime, float duration, float peak, float avg, float power)
    {
        if (detailPanelManager != null)
        {
            detailPanelManager.ShowSlotDetail(areaName, slotNo, dateStr, occurTime, duration, peak, avg, power);
        }
    }

    // 在 UITable_Summary 点击行的时候触发
    public void OnWorkAreaRowClicked(string areaName, string dateStr, int occurCount, float duration, float peak, float avg, float power)
    {
        if (detailPanelManager != null)
        {
            detailPanelManager.ShowWorkAreaDetail(areaName, dateStr, occurCount, duration, peak, avg, power);
        }
    }
}




