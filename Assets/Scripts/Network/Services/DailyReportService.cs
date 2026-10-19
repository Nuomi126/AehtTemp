using System.Collections.Generic;
using UnityEngine; // 添加 UnityEngine 命名空间

public class DailyReportService : ApiService<DailyReportResponse>
{
    protected override string Endpoint => "https://kefu.csyytx.cn:20001/system/dayreport/list";
    
    // 可以添加特定于日报API的方法
    public void FetchReportByDate(string date)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "date", date }
        };
        
        // 创建一个可序列化的类来包装日期参数
        DateParameter dateParam = new DateParameter { date = date };
        string jsonData = JsonUtility.ToJson(dateParam);
        
        HttpManager.Instance.Post(Url, jsonData, OnRequestComplete);
    }
}

// 用于序列化日期参数的辅助类
[System.Serializable]
public class DateParameter
{
    public string date;
}