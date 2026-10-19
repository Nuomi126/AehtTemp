using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ApiService<T> where T : class
{
    // API端点
    protected abstract string Endpoint { get; }
    
    // 完整URL
    protected virtual string Url => Endpoint;
    
    // 数据更新事件
    public event Action<T> OnDataReceived;
    
    // 错误事件
    public event Action<string> OnError;
    
    // 获取数据
    public virtual void FetchData(Dictionary<string, string> parameters = null)
    {
        string jsonData = parameters != null ? JsonUtility.ToJson(parameters) : "{}";
        
        HttpManager.Instance.Post(Url, jsonData, OnRequestComplete);
    }
    
    // 请求完成回调
    protected virtual void OnRequestComplete(string response, bool success)
    {
        if (success)
        {
            T data = HttpManager.Instance.ParseJson<T>(response);
            if (data != null)
            {
                OnDataReceived?.Invoke(data);
            }
            else
            {
                OnError?.Invoke("数据解析失败");
            }
        }
        else
        {
            OnError?.Invoke(response);
        }
    }
}
