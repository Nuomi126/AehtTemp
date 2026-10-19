using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;
using XCharts.Runtime.UI;

public class BanbaoReportController : MonoBehaviour
{
    [Serializable]
    public class OptionConfirmedEvent : UnityEvent<string> { }

    [Serializable]
    public class OptionSelector
    {
        public Button openButton;
        public GameObject panel;
        public Button[] optionButtons;
        public string[] optionValues;
        public TextMeshProUGUI displayText;
        public string defaultValue;
        public OptionConfirmedEvent onConfirmed = new OptionConfirmedEvent();

        private string selectedValue;

        public string Value
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(selectedValue))
                    return selectedValue;
                if (!string.IsNullOrWhiteSpace(defaultValue))
                    return defaultValue;
                return optionValues != null && optionValues.Length > 0 ? optionValues[0] : string.Empty;
            }
        }

        public void Bind(MonoBehaviour owner)
        {
            if (panel != null)
                panel.SetActive(false);

            if (openButton != null)
                openButton.onClick.AddListener(TogglePanel);

            if (optionButtons == null)
                return;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button optionButton = optionButtons[i];
                if (optionButton == null)
                    continue;

                string optionValue = ResolveOptionValue(i, optionButton);
                optionButton.onClick.AddListener(() => Confirm(optionValue));
            }

            if (string.IsNullOrWhiteSpace(selectedValue))
                selectedValue = Value;

            UpdateDisplay();
        }

        public void Confirm(string value)
        {
            selectedValue = value;
            UpdateDisplay();

            if (panel != null)
                panel.SetActive(false);

            onConfirmed?.Invoke(selectedValue);
        }

        private void TogglePanel()
        {
            if (panel == null)
                return;

            bool nextActive = !panel.activeSelf;
            SelectorManager.CloseActiveSelector();
            panel.SetActive(nextActive);
            if (nextActive)
                SelectorManager.SetActiveSelector(panel);
        }

        private string ResolveOptionValue(int index, Button optionButton)
        {
            if (optionValues != null && index >= 0 && index < optionValues.Length && !string.IsNullOrWhiteSpace(optionValues[index]))
                return optionValues[index];

            TextMeshProUGUI tmp = optionButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                return tmp.text.Trim();

            Text text = optionButton.GetComponentInChildren<Text>(true);
            if (text != null && !string.IsNullOrWhiteSpace(text.text))
                return text.text.Trim();

            return string.Empty;
        }

        private void UpdateDisplay()
        {
            if (displayText != null)
                displayText.text = Value;
        }
    }

    [Serializable]
    public class ReportTables
    {
        public UITable voltageTable;
        public UITable effectTable;
        public UITable feedingTable;
        public UITable needleTable;
        public UITable manualTable;
        public UITable specialTable;

        public IEnumerable<UITable> AllTables()
        {
            yield return voltageTable;
            yield return effectTable;
            yield return feedingTable;
            yield return needleTable;
            yield return manualTable;
            yield return specialTable;
        }

        public UITable GetTableByIndex(int index)
        {
            switch (index)
            {
                case 0: return voltageTable;
                case 1: return effectTable;
                case 2: return feedingTable;
                case 3: return needleTable;
                case 4: return manualTable;
                case 5: return specialTable;
                default: return voltageTable;
            }
        }
    }

    [Serializable]
    public class SegmentButtons
    {
        public Button voltageButton;
        public Button effectButton;
        public Button feedingButton;
        public Button needleButton;
        public Button manualButton;
        public Button specialButton;
        public Color normalColor = Color.white;
        public Color selectedColor = new Color(0.23f, 0.58f, 0.36f, 1f);

        public Button GetButtonByIndex(int index)
        {
            switch (index)
            {
                case 0: return voltageButton;
                case 1: return effectButton;
                case 2: return feedingButton;
                case 3: return needleButton;
                case 4: return manualButton;
                case 5: return specialButton;
                default: return voltageButton;
            }
        }

        public IEnumerable<Button> AllButtons()
        {
            yield return voltageButton;
            yield return effectButton;
            yield return feedingButton;
            yield return needleButton;
            yield return manualButton;
            yield return specialButton;
        }
    }

    [Header("Tab")]
    [SerializeField] private Button todayReportButton;
    [SerializeField] private Button historyReportButton;
    [SerializeField] private GameObject todayReportPanel;
    [SerializeField] private GameObject historyReportPanel;

    [Header("Today Query")]
    [SerializeField] private OptionSelector todayShiftSelector;
    [SerializeField] private WorkAreaSelector todayWorkAreaSelector;
    [SerializeField] private Button todayWorkAreaButton;
    [SerializeField] private SegmentButtons todaySegmentButtons;
    [SerializeField] private ReportTables todayTables;

    [Header("History Query")]
    [SerializeField] private OptionSelector historyShiftSelector;
    [SerializeField] private WorkAreaSelector historyWorkAreaSelector;
    [SerializeField] private Button historyWorkAreaButton;
    [SerializeField] private riqSelector historyDateSelector;
    [SerializeField] private Button historyDateButton;
    [SerializeField] private SegmentButtons historySegmentButtons;
    [SerializeField] private ReportTables historyTables;

    [Header("API Placeholder")]
    [SerializeField] private string reportApiUrl = "https://kefu.csyytx.cn:20001/system/dayreport/list";
    [SerializeField] private bool useMockData = true;
    [SerializeField] private string defaultTodayWorkArea = "1工区";
    [SerializeField] private string defaultHistoryWorkArea = "1工区";

    private string todayShift = "早班";
    private string todayWorkArea;
    private string historyShift = "早班";
    private string historyWorkArea;
    private string historyDate;
    private int todayTableIndex;
    private int historyTableIndex;
    private Coroutine currentRequest;

    private void Awake()
    {
        todayWorkArea = defaultTodayWorkArea;
        historyWorkArea = defaultHistoryWorkArea;
        historyDate = DateTime.Now.ToString("yyyyMMdd");
    }

    private void Start()
    {
        if (todayReportButton != null)
            todayReportButton.onClick.AddListener(ShowTodayReport);

        if (historyReportButton != null)
            historyReportButton.onClick.AddListener(ShowHistoryReport);

        BindTodaySelectors();
        BindHistorySelectors();
        BindSegmentButtons(todaySegmentButtons, todayTables, isToday: true);
        BindSegmentButtons(historySegmentButtons, historyTables, isToday: false);
        ShowTodayReport();
        TryFetchTodayReport();
    }

    public void ShowTodayReport()
    {
        if (todayReportPanel != null)
            todayReportPanel.SetActive(true);
        if (historyReportPanel != null)
            historyReportPanel.SetActive(false);

        SwitchTable(todayTables, todaySegmentButtons, todayTableIndex);
    }

    public void ShowHistoryReport()
    {
        if (todayReportPanel != null)
            todayReportPanel.SetActive(false);
        if (historyReportPanel != null)
            historyReportPanel.SetActive(true);

        SwitchTable(historyTables, historySegmentButtons, historyTableIndex);
    }

    public void TryFetchTodayReport()
    {
        RequestAndFill(todayTables, todayWorkArea, todayShift, DateTime.Now.ToString("yyyyMMdd"), false);
    }

    public void TryFetchHistoryReport()
    {
        if (string.IsNullOrWhiteSpace(historyDate))
            return;

        RequestAndFill(historyTables, historyWorkArea, historyShift, historyDate, true);
    }

    private void BindTodaySelectors()
    {
        if (todayShiftSelector != null)
        {
            todayShiftSelector.Bind(this);
            todayShift = todayShiftSelector.Value;
            todayShiftSelector.onConfirmed.AddListener(value =>
            {
                todayShift = value;
                TryFetchTodayReport();
            });
        }

        if (todayWorkAreaSelector != null)
        {
            todayWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(value =>
            {
                todayWorkArea = value;
                TryFetchTodayReport();
            });
        }

        if (todayWorkAreaButton != null && todayWorkAreaSelector != null)
            todayWorkAreaButton.onClick.AddListener(todayWorkAreaSelector.OpenSelectorPanel);
    }

    private void BindHistorySelectors()
    {
        if (historyShiftSelector != null)
        {
            historyShiftSelector.Bind(this);
            historyShift = historyShiftSelector.Value;
            historyShiftSelector.onConfirmed.AddListener(value =>
            {
                historyShift = value;
                TryFetchHistoryReport();
            });
        }

        if (historyWorkAreaSelector != null)
        {
            historyWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(value =>
            {
                historyWorkArea = value;
                TryFetchHistoryReport();
            });
        }

        if (historyWorkAreaButton != null && historyWorkAreaSelector != null)
            historyWorkAreaButton.onClick.AddListener(historyWorkAreaSelector.OpenSelectorPanel);

        if (historyDateSelector != null)
        {
            historyDateSelector.OnDateConfirmed.AddListener(value =>
            {
                historyDate = value;
                TryFetchHistoryReport();
            });
        }

        if (historyDateButton != null && historyDateSelector != null)
        {
            TextMeshProUGUI dateText = historyDateButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (dateText != null)
                historyDateSelector.selectedDisplayText = dateText;

            historyDateButton.onClick.AddListener(historyDateSelector.OpenSelectorPanel);
        }
    }

    private void BindSegmentButtons(SegmentButtons segmentButtons, ReportTables tables, bool isToday)
    {
        if (segmentButtons == null)
            return;

        for (int i = 0; i < 6; i++)
        {
            int tableIndex = i;
            Button button = segmentButtons.GetButtonByIndex(tableIndex);
            if (button == null)
                continue;

            button.onClick.AddListener(() =>
            {
                if (isToday)
                    todayTableIndex = tableIndex;
                else
                    historyTableIndex = tableIndex;

                SwitchTable(tables, segmentButtons, tableIndex);
            });
        }

        SwitchTable(tables, segmentButtons, 0);
    }

    private void SwitchTable(ReportTables tables, SegmentButtons segmentButtons, int activeIndex)
    {
        if (tables != null)
        {
            for (int i = 0; i < 6; i++)
            {
                UITable table = tables.GetTableByIndex(i);
                if (table != null)
                    table.gameObject.SetActive(i == activeIndex);
            }
        }

        UpdateSegmentButtonColors(segmentButtons, activeIndex);
    }

    private void UpdateSegmentButtonColors(SegmentButtons segmentButtons, int activeIndex)
    {
        if (segmentButtons == null)
            return;

        for (int i = 0; i < 6; i++)
        {
            Button button = segmentButtons.GetButtonByIndex(i);
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = i == activeIndex ? segmentButtons.selectedColor : segmentButtons.normalColor;
        }
    }

    private void RequestAndFill(ReportTables targetTables, string workArea, string shift, string date, bool isHistory)
    {
        if (currentRequest != null)
            StopCoroutine(currentRequest);

        currentRequest = StartCoroutine(RequestReportData(targetTables, workArea, shift, date, isHistory));
    }

    private IEnumerator RequestReportData(ReportTables targetTables, string workArea, string shift, string date, bool isHistory)
    {
        if (targetTables == null)
            yield break;

        ClearTables(targetTables);

        if (useMockData)
        {
            yield return new WaitForSeconds(0.2f);
            FillTables(targetTables, BuildMockRows(workArea, shift, date));
            yield break;
        }

        string jsonBody = JsonUtility.ToJson(new ReportRequestBody
        {
            parameters = new ReportRequestParameters
            {
                gongqu = Normalize(workArea),
                banci = Normalize(shift),
                date = Normalize(date),
                reportType = isHistory ? "history" : "today"
            }
        });

        using (UnityWebRequest request = new UnityWebRequest(reportApiUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[BanbaoReport] API request failed: {request.error}");
                yield break;
            }

            DailyReportResponse response = JsonUtility.FromJson<DailyReportResponse>(request.downloadHandler.text);
            if (response == null || response.rows == null)
            {
                Debug.LogWarning("[BanbaoReport] API returned empty data.");
                yield break;
            }

            FillTables(targetTables, response.rows);
        }
    }

    private void FillTables(ReportTables targetTables, ReportData[] rows)
    {
        if (rows == null)
            return;

        FillTable(targetTables.voltageTable, rows, VoltageColumns);
        FillTable(targetTables.effectTable, rows, EffectColumns);
        FillTable(targetTables.feedingTable, rows, FeedingColumns);
        FillTable(targetTables.needleTable, rows, NeedleColumns);
        FillTable(targetTables.manualTable, rows, ManualColumns);
        FillTable(targetTables.specialTable, rows, SpecialColumns);
    }

    private void FillTable(UITable table, ReportData[] rows, Func<ReportData, List<string>> mapRow)
    {
        if (table == null)
            return;

        table.ClearData();

        foreach (ReportData row in rows)
        {
            List<string> cells = mapRow(row);
            int columnCount = table.columns != null && table.columns.Count > 0 ? table.columns.Count : cells.Count;
            while (cells.Count < columnCount)
                cells.Add(string.Empty);

            if (cells.Count > columnCount)
                cells.RemoveRange(columnCount, cells.Count - columnCount);

            table.AddRow(cells);
        }

        table.RefreshGraph();
        table.RefreshTable();
    }

    private static List<string> VoltageColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            StatusText(row.caokCaozht),
            FormatVoltage(row.sheddy),
            FormatVoltage(row.pingjdy),
            FormatVoltage(row.gongzdy)
        };
    }

    private static List<string> EffectColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            row.xiaoyXiaoycsh.ToString(),
            row.xiaoyShanshxycsh.ToString(),
            FormatSeconds(row.xiaoyChixshj),
            FormatVoltage(row.xiaoyPingjdy),
            row.xiaoyGongh.ToString()
        };
    }

    private static List<string> FeedingColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            row.xialXialjg.ToString(),
            row.xialZongcsh.ToString(),
            row.xialQianlcsh.ToString(),
            SafeObject(row.xialGuolcsh),
            SafeObject(row.xialXialzl)
        };
    }

    private static List<string> NeedleColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            GetOptionalValue(row, "zhenzhen", "0"),
            GetOptionalValue(row, "baidong", "0"),
            GetOptionalValue(row, "zonggonghao", "0"),
            GetOptionalValue(row, "baidongchixu", "0"),
            GetOptionalValue(row, "dianyayichangchixu", "0")
        };
    }

    private static List<string> ManualColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            GetOptionalValue(row, "shoudongcishu", "0"),
            GetOptionalValue(row, "shoudongshijian", ""),
            GetOptionalValue(row, "shoudongliang", "0"),
            GetOptionalValue(row, "remark", "")
        };
    }

    private static List<string> SpecialColumns(ReportData row)
    {
        return new List<string>
        {
            row.caoh.ToString(),
            GetOptionalValue(row, "teshuleixing", ""),
            GetOptionalValue(row, "teshucishu", "0"),
            GetOptionalValue(row, "teshuchixu", "0"),
            GetOptionalValue(row, "remark", "")
        };
    }

    private void ClearTables(ReportTables targetTables)
    {
        foreach (UITable table in targetTables.AllTables())
        {
            if (table == null)
                continue;

            table.ClearData();
            table.RefreshGraph();
            table.RefreshTable();
        }
    }

    private static ReportData[] BuildMockRows(string workArea, string shift, string date)
    {
        ReportData[] rows = new ReportData[12];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new ReportData
            {
                dayId = i + 1,
                caoh = 2031 + i,
                caokCaozht = i % 4,
                sheddy = 3970 + i,
                pingjdy = 3984 + i,
                gongzdy = 3981 + i,
                xiaoyXiaoycsh = i % 3,
                xiaoyShanshxycsh = i % 2,
                xiaoyChixshj = 120 + i * 10,
                xiaoyPingjdy = 4050 + i,
                xiaoyGongh = 30 + i,
                xialXialjg = 90,
                xialZongcsh = 40 + i,
                xialQianlcsh = i % 5,
                xialGuolcsh = i % 4,
                xialXialzl = 120 + i
            };
        }

        Debug.Log($"[BanbaoReport] Mock data: gongqu={workArea}, banci={shift}, date={date}, rows={rows.Length}");
        return rows;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string SafeObject(object value)
    {
        return value == null ? string.Empty : value.ToString();
    }

    private static string FormatVoltage(int value)
    {
        return (value / 1000f).ToString("F3");
    }

    private static string FormatSeconds(int seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
        return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
    }

    private static string StatusText(int status)
    {
        switch (status)
        {
            case 0: return "联机";
            case 1: return "停槽";
            case 2: return "降流";
            case 3: return "启动";
            default: return "未知";
        }
    }

    private static string GetOptionalValue(ReportData row, string fieldName, string fallback)
    {
        if (row == null)
            return fallback;

        System.Reflection.FieldInfo field = typeof(ReportData).GetField(fieldName);
        if (field == null)
            return fallback;

        object value = field.GetValue(row);
        return value == null ? fallback : value.ToString();
    }

    [Serializable]
    private class ReportRequestBody
    {
        public ReportRequestParameters parameters;
    }

    [Serializable]
    private class ReportRequestParameters
    {
        public string gongqu;
        public string banci;
        public string date;
        public string reportType;
    }
}
