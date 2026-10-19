// APIManager.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIManager : MonoBehaviour
{
    // 单例模式确保全局唯一实例
    private static APIManager _instance;
    public static APIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("APIManager");
                _instance = obj.AddComponent<APIManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    [Header("API Configuration")]
    [SerializeField] private string baseUrl = "https://111.230.87.254:8080/system/dayreport/list"; // 包含API版本

    /// <summary>
    /// 发送POST请求的核心方法
    /// </summary>
    public void PostDailyReport(DailyReportRequest requestData,
                              System.Action<DailyReportResponse> onSuccess,
                              System.Action<string> onError)
    {
        StartCoroutine(PostRequestCoroutine("/daily-reports", requestData, onSuccess, onError));
    }

    private IEnumerator PostRequestCoroutine(string endpoint,
                                           DailyReportRequest requestData,
                                           System.Action<DailyReportResponse> onSuccess,
                                           System.Action<string> onError)
    {
        // 1. 构建完整URL
        string url = baseUrl + endpoint;
        Debug.Log($"POST请求URL: {url}");

        // 2. 序列化请求体为JSON
        string jsonBody = JsonUtility.ToJson(requestData);
        Debug.Log($"请求体JSON: {jsonBody}");

        // 3. 创建UnityWebRequest (POST方法)
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // 配置请求头和请求体
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. 可选：添加认证Token
            // string token = PlayerPrefs.GetString("auth_token");
            // if (!string.IsNullOrEmpty(token)) {
            //     request.SetRequestHeader("Authorization", $"Bearer {token}");
            // }

            // 5. 发送请求
            yield return request.SendWebRequest();

            // 6. 处理响应
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"原始响应: {request.downloadHandler.text}");
                try
                {
                    DailyReportResponse response = JsonUtility.FromJson<DailyReportResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (System.Exception e)
                {
                    onError?.Invoke($"JSON解析失败: {e.Message}");
                }
            }
            else
            {
                string errorMsg = $"HTTP {request.responseCode}";
                switch (request.responseCode)
                {
                    case 404:
                        errorMsg += ": 接口不存在，请检查URL和请求方法";
                        break;
                    case 400:
                        errorMsg += ": 请求参数错误";
                        break;
                    case 401:
                        errorMsg += ": 未授权，需要登录";
                        break;
                }
                Debug.LogError($"{errorMsg}\nURL: {url}\nError: {request.error}");
                onError?.Invoke(errorMsg);
            }
        }
    }
}