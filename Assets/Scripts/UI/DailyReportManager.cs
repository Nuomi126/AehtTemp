using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DailyReportManager : MonoBehaviour
{
    private static DailyReportManager _instance;
    public static DailyReportManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = GameObject.Find("DailyReportManager");
                if (go == null)
                {
                    go = new GameObject("DailyReportManager");
                }

                _instance = go.GetComponent<DailyReportManager>() ?? go.AddComponent<DailyReportManager>();
            }

            return _instance;
        }
    }

    private const string DailyReportUrl = "https://kefu.csyytx.cn:20001/system/dayreport/list";

    private DailyReportResponse currentReportData;

    public UnityEvent<DailyReportResponse> OnDataUpdated = new UnityEvent<DailyReportResponse>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        DestroyInstance();
    }

    public void FetchDailyReports(string gongchang = null, string gongqu = null, string banci = null)
    {
        DailyReportQueryParameters parameters = new DailyReportQueryParameters
        {
            gongchang = NormalizeParameter(gongchang),
            gongqu = NormalizeParameter(gongqu),
            banci = NormalizeParameter(banci)
        };

        string jsonData = JsonUtility.ToJson(new DailyReportRequestBody { parameters = parameters });
        Debug.Log($"[DailyReport] Request: {jsonData}");

        HttpManager.Instance.Post(
            DailyReportUrl,
            jsonData,
            OnRequestComplete
        );
    }

    public void FetchDailyReportsWithTMPParams(
        TMPro.TextMeshProUGUI gongchangTMP,
        TMPro.TextMeshProUGUI gongquTMP,
        TMPro.TextMeshProUGUI banciTMP = null)
    {
        string gongchang = gongchangTMP != null ? gongchangTMP.text : null;
        string gongqu = gongquTMP != null ? gongquTMP.text : null;
        string banci = banciTMP != null ? banciTMP.text : null;

        FetchDailyReports(gongchang, gongqu, banci);
    }

    private static string NormalizeParameter(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [System.Serializable]
    private class DailyReportRequestBody
    {
        public DailyReportQueryParameters parameters;
    }

    [System.Serializable]
    private class DailyReportQueryParameters
    {
        public string gongchang;
        public string gongqu;
        public string banci;
    }

    private void OnRequestComplete(string response, bool success)
    {
        if (!success)
        {
            Debug.LogError("Failed to fetch daily report data: " + response);
            return;
        }

        DailyReportResponse reportResponse = HttpManager.Instance.ParseJson<DailyReportResponse>(response);
        if (reportResponse == null)
        {
            Debug.LogError("Failed to parse daily report data");
            return;
        }

        currentReportData = reportResponse;
        Debug.Log($"Daily report loaded: {reportResponse.total} records");
        OnDataUpdated.Invoke(currentReportData);
    }

    public DailyReportResponse GetCurrentReportData()
    {
        return currentReportData;
    }

    public ReportData GetReportById(int id)
    {
        if (currentReportData == null || currentReportData.rows == null)
        {
            return null;
        }

        foreach (var report in currentReportData.rows)
        {
            if (report.dayId == id)
            {
                return report;
            }
        }

        return null;
    }

    public static void DestroyInstance()
    {
        if (_instance == null)
        {
            return;
        }

        SceneManager.sceneUnloaded -= _instance.OnSceneUnloaded;
        Destroy(_instance.gameObject);
        _instance = null;
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        _instance = null;
    }

    private void OnApplicationQuit()
    {
        DestroyInstance();
    }
}
