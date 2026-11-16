using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using XCharts.Runtime;

/// <summary>
/// Keeps the existing slot-control chart and refreshes its current series when
/// the date or work-area selector is confirmed.
/// </summary>
public sealed class SlotControlCurveFilterController : MonoBehaviour
{
    [Header("Selectors")]
    [SerializeField] private riqSelector dateSelector;
    [SerializeField] private WorkAreaSelector workAreaSelector;

    [Header("Existing chart")]
    [SerializeField] private LineChart targetChart;

    [Header("API")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string companyId = "1033";
    [SerializeField] private int connectionTimeout = 30;
    [SerializeField, Range(0, 19)] private int valueIndex = 0;
    [SerializeField] private bool refreshOnStart = true;

    private const int FrameType = 4;
    private const int FrameHeaderSize = 12;
    private const int FrameTailSize = 2;
    private const int RecordSize = 49;

    private Coroutine refreshRoutine;
    private bool listenersBound;
    private bool waitingForSlotConfig;
    private GlobalElectrolyticCellDataManager slotConfigManager;

    private sealed class Frame4Result
    {
        public readonly List<int> values = new List<int>();
    }

    private void Start()
    {
        BindListeners();

        slotConfigManager = GlobalElectrolyticCellDataManager.Instance;
        slotConfigManager.OnDataUpdated -= HandleSlotConfigUpdated;
        slotConfigManager.OnDataUpdated += HandleSlotConfigUpdated;

        if (refreshOnStart)
        {
            StartCoroutine(RefreshOnNextFrame());
        }
    }

    private void OnDestroy()
    {
        UnbindListeners();

        if (slotConfigManager != null)
        {
            slotConfigManager.OnDataUpdated -= HandleSlotConfigUpdated;
        }
    }

    private IEnumerator RefreshOnNextFrame()
    {
        yield return null;
        RefreshForCurrentFilters();
    }

    private void BindListeners()
    {
        if (listenersBound) return;

        if (dateSelector != null && dateSelector.OnDateConfirmed != null)
        {
            dateSelector.OnDateConfirmed.AddListener(HandleDateConfirmed);
        }

        if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null)
        {
            workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(HandleWorkAreaConfirmed);
        }

        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!listenersBound) return;

        if (dateSelector != null && dateSelector.OnDateConfirmed != null)
        {
            dateSelector.OnDateConfirmed.RemoveListener(HandleDateConfirmed);
        }

        if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null)
        {
            workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(HandleWorkAreaConfirmed);
        }

        listenersBound = false;
    }

    private void HandleDateConfirmed(string _)
    {
        RefreshForCurrentFilters();
    }

    private void HandleWorkAreaConfirmed(string _)
    {
        RefreshForCurrentFilters();
    }

    private void HandleSlotConfigUpdated()
    {
        if (!waitingForSlotConfig) return;
        waitingForSlotConfig = false;
        RefreshForCurrentFilters();
    }

    public void RefreshForCurrentFilters()
    {
        if (!isActiveAndEnabled || targetChart == null) return;

        if (!TryResolveFirstSlot(out ushort slotNumber))
        {
            waitingForSlotConfig = true;
            Debug.LogWarning("[SlotControlCurve] No slot is available for the selected work area. The existing chart is kept.");
            return;
        }

        waitingForSlotConfig = false;
        string selectedDate = ResolveSelectedDate();

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        refreshRoutine = StartCoroutine(FetchAndRefresh(slotNumber, selectedDate));
    }

    private bool TryResolveFirstSlot(out ushort slotNumber)
    {
        slotNumber = 0;
        if (workAreaSelector == null) return false;

        int workshop = workAreaSelector.GetSelectedWorkshopNumber();
        int workArea = workAreaSelector.GetSelectedWorkAreaNumber();
        if (workshop < byte.MinValue || workshop > byte.MaxValue ||
            workArea < byte.MinValue || workArea > byte.MaxValue)
        {
            return false;
        }

        var manager = slotConfigManager != null
            ? slotConfigManager
            : GlobalElectrolyticCellDataManager.Instance;
        if (!manager.isDataLoaded) return false;

        List<ushort> slots = manager.GetSlotNumbersByWorkshopAndArea((byte)workshop, (byte)workArea);
        if (slots == null || slots.Count == 0) return false;

        slotNumber = slots[0];
        return true;
    }

    private string ResolveSelectedDate()
    {
        string value = dateSelector != null && dateSelector.selectedDisplayText != null
            ? dateSelector.selectedDisplayText.text
            : string.Empty;

        value = (value ?? string.Empty).Trim().Replace("-", string.Empty).Replace("/", string.Empty);
        if (value.Length == 8) return value;
        return DateTime.Today.ToString("yyyyMMdd");
    }

    private IEnumerator FetchAndRefresh(ushort slotNumber, string selectedDate)
    {
        string requestParameter = $"{companyId}-{FrameType}-{slotNumber}-{selectedDate}";
        byte[] requestBody = Encoding.UTF8.GetBytes($"\"{requestParameter}\"");

        using (var request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(requestBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = connectionTimeout;

            Debug.Log($"[SlotControlCurve] Refresh date={selectedDate}, workArea={workAreaSelector.GetSelectedWorkArea()}, slot={slotNumber}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SlotControlCurve] Request failed: {request.error}. The existing chart is kept.");
                refreshRoutine = null;
                yield break;
            }

            if (!TryParseFrame4(request.downloadHandler.text, out Frame4Result result))
            {
                Debug.LogWarning("[SlotControlCurve] Frame 4 response is empty or invalid. The existing chart is kept.");
                refreshRoutine = null;
                yield break;
            }

            ApplyValuesToExistingChart(result.values);
        }

        refreshRoutine = null;
    }

    private bool TryParseFrame4(string responseText, out Frame4Result result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(responseText)) return false;

        string base64 = responseText.Trim();
        if (base64.Length >= 2 && base64[0] == '"' && base64[base64.Length - 1] == '"')
        {
            base64 = base64.Substring(1, base64.Length - 2);
        }

        byte[] frame;
        try
        {
            frame = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (frame.Length < FrameHeaderSize + FrameTailSize) return false;
        if (frame[0] != 0xAA || frame[1] != 0x55) return false;
        if (frame[frame.Length - 2] != 0xA5 || frame[frame.Length - 1] != 0x5A) return false;

        int parsedFrameType = ReadUInt16(frame, 4);
        if (parsedFrameType != FrameType) return false;

        uint declaredLength = ReadUInt32(frame, 8);
        if (declaredLength > int.MaxValue) return false;

        int dataLength = (int)declaredLength;
        if (FrameHeaderSize + dataLength + FrameTailSize > frame.Length) return false;

        int recordCount = dataLength / RecordSize;
        if (recordCount <= 0) return false;

        var parsed = new Frame4Result();
        int valueByteOffset = valueIndex * 2;
        for (int record = 0; record < recordCount; record++)
        {
            int recordOffset = FrameHeaderSize + record * RecordSize;
            int valueOffset = recordOffset + valueByteOffset;
            if (valueOffset + 1 >= FrameHeaderSize + dataLength) break;
            parsed.values.Add(ReadUInt16(frame, valueOffset));
        }

        if (parsed.values.Count == 0) return false;
        result = parsed;
        return true;
    }

    private void ApplyValuesToExistingChart(List<int> values)
    {
        if (targetChart == null || values == null || values.Count == 0) return;

        // ClearData keeps the existing series and its visual configuration.
        targetChart.ClearData();
        for (int i = 0; i < values.Count; i++)
        {
            targetChart.AddXAxisData((i * 2).ToString());
            targetChart.AddData(0, values[i]);
        }

        targetChart.RefreshChart();
        Debug.Log($"[SlotControlCurve] Existing chart refreshed with {values.Count} points.");
    }

    private static int ReadUInt16(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) |
               ((uint)data[offset + 1] << 16) |
               ((uint)data[offset + 2] << 8) |
               data[offset + 3];
    }
}
