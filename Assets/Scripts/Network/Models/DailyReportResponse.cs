[System.Serializable]
public class DailyReportResponse
{
    public int total;      // 总记录数
    public ReportData[] rows; // 数据列表，注意这里是rows而不是data
    public int code;       // 状态码
    public string msg;     // 消息，注意这里是msg而不是message
    public int type;       // 类型
}

[System.Serializable]
public class ReportData
{
    public string createBy;
    public string createTime;
    public string updateBy;
    public string updateTime;
    public string remark;
    public int dayId;         // 日报ID，注意这里是int类型
    public int caoh;      // 炒货
    public int sheddy;       // 注意这里是int类型
    public int gongzdy;      // 工作时间，注意这里是int类型
    public int caokCaozht;   // 注意这里是int类型
    public int xiaoyXiaoycsh; // 注意这里是int类型
    public int xiaoyShanshxycsh; // 注意这里是int类型
    public int xiaoyChixshj;  // 注意这里是int类型
    public int xiaoyPingjdy;  // 注意这里是int类型
    public int xiaoyGongh;    // 新增字段
    public int xialXialjg;    // 新增字段
    public int xialZongcsh;   // 新增字段
    public int xialQianlcsh;  // 新增字段
    public object xialGuolcsh; // 新增字段，可能为null
    public object xialXialzl;  // 新增字段，可能为null
    public int pingjdy;       // 新增字段
}
