using System;
using System.Collections;
using System.Text;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class HttpManager : MonoBehaviour
{
    // 单例模式实现
    private static HttpManager _instance;
    public static HttpManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("HttpManager");
                _instance = go.AddComponent<HttpManager>();
                _instance._ignoreCertificateValidation = true;
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 控制是否忽略证书验证
    [SerializeField] private bool _ignoreCertificateValidation = true;
    
    // 基础URL，可以在Inspector中设置
    [SerializeField] private string _baseUrl = "https://api.example.com";
    
    // 通用请求头
    private Dictionary<string, string> _defaultHeaders;
    
    private void Awake()
    {
        // 确保忽略证书验证
        _ignoreCertificateValidation = true;
        
        // 全局设置忽略SSL证书错误
        ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        
        // 初始化默认请求头
        _defaultHeaders = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };
        
        Debug.Log("✓ HttpManager 初始化，证书验证已忽略");
    }
    
    /// <summary>
    /// 设置通用请求头
    /// </summary>
    public void SetDefaultHeader(string key, string value)
    {
        if (_defaultHeaders.ContainsKey(key))
            _defaultHeaders[key] = value;
        else
            _defaultHeaders.Add(key, value);
    }
    
    /// <summary>
    /// 设置认证令牌
    /// </summary>
    public void SetAuthToken(string token)
    {
        SetDefaultHeader("Authorization", $"Bearer {token}");
    }
    
    /// <summary>
    /// 发送GET请求
    /// </summary>
    public void Get(string endpoint, Action<string, bool> callback, 
                   Dictionary<string, string> headers = null)
    {
        string url = _baseUrl + endpoint;
        StartCoroutine(SendRequest(url, "GET", null, callback, headers));
    }
    
    /// <summary>
    /// 发送POST请求
    /// </summary>
    public void Post(string url, string jsonData, Action<string, bool> callback, 
                    Dictionary<string, string> headers = null)
    {
        StartCoroutine(SendRequest(url, "POST", jsonData, callback, headers));
    }
    
    /// <summary>
    /// 发送PUT请求
    /// </summary>
    public void Put(string endpoint, string jsonData, Action<string, bool> callback, 
                   Dictionary<string, string> headers = null)
    {
        string url = _baseUrl + endpoint;
        StartCoroutine(SendRequest(url, "PUT", jsonData, callback, headers));
    }
    
    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    public void Delete(string endpoint, Action<string, bool> callback, 
                      Dictionary<string, string> headers = null)
    {
        string url = _baseUrl + endpoint;
        StartCoroutine(SendRequest(url, "DELETE", null, callback, headers));
    }
    
    /// <summary>
    /// 通用请求发送方法
    /// </summary>
    private IEnumerator SendRequest(string url, string method, string jsonData, 
                                  Action<string, bool> callback, 
                                  Dictionary<string, string> headers = null)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            // 设置请求体
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // 添加默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            // 添加自定义请求头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
            
            // 忽略证书验证
            if (_ignoreCertificateValidation)
            {
                request.certificateHandler = new BypassCertificate();
            }
            
            // 发送请求
            yield return request.SendWebRequest();
            
            // 处理响应
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                callback?.Invoke(responseText, true);
                Debug.Log($"{method}请求成功: {url}");
            }
            else
            {
                string errorMessage = request.error;
                callback?.Invoke(errorMessage, false);
                Debug.LogError($"{method}请求失败: {url}, 错误: {errorMessage}");
            }
        }
    }
    
    /// <summary>
    /// 解析JSON数据为指定类型的对象
    /// </summary>
    public T ParseJson<T>(string jsonData) where T : class
    {
        try
        {
            return JsonUtility.FromJson<T>(jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON解析失败: {e.Message}");
            return null;
        }
    }
}

// 用于绕过证书验证的类
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}