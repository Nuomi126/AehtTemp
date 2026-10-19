using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using XCharts.Runtime;
using XCharts.Runtime.UI;
using TMPro;

using UnityEngine.UI;

public class CaokuangTableManager : MonoBehaviour
{
    [Header("当日报表设置")]
    public UITable dangriTable;
    public WorkAreaSelector dangriWorkAreaSelector;

    [Header("历史趋势设置")]
    public UITable lishiTable;
    public WorkAreaSelector lishiWorkAreaSelector;
    [Header(">> 日期选择器组件 (挂载了riqSelector脚本的对象) <<")]
    public riqSelector lishiStartDateSelector; // 开始日期选择器
    public riqSelector lishiEndDateSelector;   // 结束日期选择器
    
    [Header(">> 打开日期选择面板的按钮 <<")]
    public Button lishiStartDateBtn;           // 点击用来弹出“开始日期”面板的按钮
    public Button lishiEndDateBtn;             // 点击用来弹出“结束日期”面板的按钮

    [Header(">> 打开工区选择面板的按钮 <<")]
    public Button dangriWorkAreaBtn;           // 当日报表：点击弹出工区选择面板的按钮
    public Button lishiWorkAreaBtn;            // 历史趋势：点击弹出工区选择面板的按钮

    [Header("详情弹窗 (panelslotdetail)")]
    public GameObject panelSlotDetail;

    private string currentLishiWorkArea = "";
    private string currentLishiStartDate = "";
    private string currentLishiEndDate = "";

    void Start()
    {
        // 1. 绑定当日报表的工区选择事件
        if (dangriWorkAreaSelector != null)
            dangriWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnDangriWorkAreaChanged);

        if (dangriWorkAreaBtn != null && dangriWorkAreaSelector != null)
            dangriWorkAreaBtn.onClick.AddListener(() => dangriWorkAreaSelector.OpenSelectorPanel());

        // 2. 绑定历史趋势的工区和日期选择事件
        if (lishiWorkAreaSelector != null)
            lishiWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnLishiWorkAreaChanged);

        if (lishiWorkAreaBtn != null && lishiWorkAreaSelector != null)
            lishiWorkAreaBtn.onClick.AddListener(() => lishiWorkAreaSelector.OpenSelectorPanel());
            
        if (lishiStartDateSelector != null)
            lishiStartDateSelector.OnDateConfirmed.AddListener(OnLishiStartDateChanged);

        if (lishiEndDateSelector != null)
            lishiEndDateSelector.OnDateConfirmed.AddListener(OnLishiEndDateChanged);

        // 3. 绑定点击按钮弹出对应日期面板的事件，并动态修复展示文字绑定
        if (lishiStartDateBtn != null && lishiStartDateSelector != null)
        {
            // 自动为选择器绑定按钮上的文本显示
            var textComp = lishiStartDateBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textComp != null) lishiStartDateSelector.selectedDisplayText = textComp;
            
            lishiStartDateBtn.onClick.AddListener(() => lishiStartDateSelector.OpenSelectorPanel());
        }

        if (lishiEndDateBtn != null && lishiEndDateSelector != null)
        {
            var textComp = lishiEndDateBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textComp != null) lishiEndDateSelector.selectedDisplayText = textComp;
            
            lishiEndDateBtn.onClick.AddListener(() => lishiEndDateSelector.OpenSelectorPanel());
        }

        // 4. 绑定两个表格的行点击事件，防止XCharts越界异常
        if (dangriTable != null)
        {
            dangriTable.onTableClick = (int row, int col, TableCell cell) => OnDangriTableClick(row);
        }
            
        if (lishiTable != null)
        {
            lishiTable.onTableClick = (int row, int col, TableCell cell) => OnLishiTableClick(row);
        }

        // 初始确保详情页是关闭的
        if (panelSlotDetail != null)
            panelSlotDetail.SetActive(false);
    }

    private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    private string enterpriseId = "1033"; // 电解企业标识
    private string frameType = "72";      // Frame 72：槽控跨日报表（当日报表/历史趋势通用，字段最全）

    // ================= 当日报表回调 =================

    private void OnDangriWorkAreaChanged(string workArea)
    {
        // 当日报表，日期直接取今天
        string today = DateTime.Now.ToString("yyyyMMdd");
        Debug.Log($"[当日报表] 准备请求API: 工区={workArea}, 日期={today}");
        
        StartCoroutine(FetchRemoteData(dangriTable, workArea, today, true));
    }

    private void OnDangriTableClick(int rowIndex)
    {
        if (dangriTable == null || rowIndex < 0 || rowIndex >= dangriTable.data.Count) return;
        // 获取该行第0列（即槽号）
        string slotNumber = dangriTable.data[rowIndex].data[0].ToString();
        OpenDetailPanel(slotNumber);
    }

    // ================= 历史趋势回调 =================

    private void OnLishiWorkAreaChanged(string workArea)
    {
        currentLishiWorkArea = workArea;
        TryFetchLishiData();
    }

    private void OnLishiStartDateChanged(string dateStr)
    {
        currentLishiStartDate = dateStr;
        TryFetchLishiData();
    }

    private void OnLishiEndDateChanged(string dateStr)
    {
        currentLishiEndDate = dateStr;
        TryFetchLishiData();
    }

    private void TryFetchLishiData()
    {
        // 只有当工区、开始日期和结束日期都选择了才发起请求
        if (!string.IsNullOrEmpty(currentLishiWorkArea) && 
            !string.IsNullOrEmpty(currentLishiStartDate) && 
            !string.IsNullOrEmpty(currentLishiEndDate))
        {
            Debug.Log($"[历史趋势] 准备请求API: 工区={currentLishiWorkArea}, 开始时间={currentLishiStartDate}, 结束时间={currentLishiEndDate}");
            // 你可以将两个日期拼接来发送请求，如： "20240101_20240131"
            string dateRange = $"{currentLishiStartDate}_{currentLishiEndDate}";
            StartCoroutine(FetchRemoteData(lishiTable, currentLishiWorkArea, dateRange, false));
        }
    }

    private void OnLishiTableClick(int rowIndex)
    {
        if (lishiTable == null || rowIndex < 0 || rowIndex >= lishiTable.data.Count) return;
        // 获取该行第0列（即槽号）
        string slotNumber = lishiTable.data[rowIndex].data[0].ToString();
        OpenDetailPanel(slotNumber);
    }

    // ================= API 请求与数据填充 =================

    private IEnumerator FetchRemoteData(UITable table, string workArea, string date, bool isDangri)
    {
        if (table == null) yield break;

        // 日期：当日报表传单日 yyyyMMdd；历史趋势传 "start_end"
        string reqDate = !string.IsNullOrEmpty(date) ? date : DateTime.Now.ToString("yyyyMMdd");
        string startDate = reqDate, endDate = reqDate;
        if (!isDangri && reqDate.Contains("_"))
        {
            string[] parts = reqDate.Split('_');
            startDate = parts[0];
            endDate = parts.Length > 1 ? parts[1] : parts[0];
        }

        // 从工区字符串（如 "1车间1工区"）解析车间号、工区号，再取出该工区下所有槽号
        ParseWorkArea(workArea, out byte workshop, out byte workAreaNum);
        List<ushort> slotNumbers = GetSlotNumbers(workshop, workAreaNum);
        if (slotNumbers.Count == 0)
        {
            Debug.LogWarning($"[CaokuangTable] 工区「{workArea}」下没有获取到槽号，请检查槽配置数据");
            table.ClearData();
            yield break;
        }

        Debug.Log($"[CaokuangTable] 开始请求{(isDangri ? "当日报表" : "历史趋势")}数据：{workArea} 共 {slotNumbers.Count} 个槽，日期 {startDate}~{endDate}");
        table.ClearData();

        int successCount = 0;
        for (int i = 0; i < slotNumbers.Count; i++)
        {
            ushort slot = slotNumbers[i];

            // Frame 72（槽控跨日报表）：企业ID-72-槽号-起始日期-终止日期
            // 当日报表起始=终止=当天；历史趋势为所选日期范围，每天一条记录
            string postDataString = $"\"{enterpriseId}-{frameType}-{slot}-{startDate}-{endDate}\"";

            using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postDataString);
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[CaokuangTable] 槽号 {slot} 请求失败：{request.error}");
                    continue;
                }

                List<CaokuangRecord> records = ParseFrame72Data(request.downloadHandler.text);
                if (records == null || records.Count == 0) continue;

                if (isDangri)
                    FillDangriRow(slot, records, reqDate);
                else
                    FillLishiRow(slot, records);

                successCount++;
            }

            // 请求间隔，避免瞬时并发过多导致服务端限流
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log($"[CaokuangTable] {(isDangri ? "当日报表" : "历史趋势")}填充完成：成功 {successCount}/{slotNumbers.Count} 个槽，表格共 {table.data.Count} 行");
    }

    // ================= 点击工具与帮助方法 =================

    private void OpenDetailPanel(string slotNumber)
    {
        Debug.Log($"用户点击了表格，打开槽号 {slotNumber} 的详情");
        if (panelSlotDetail != null)
        {
            panelSlotDetail.SetActive(true);
        }
    }

    // 封装的通用行点击绑定方法，避免 Index Out Of Range 报错，准确寻找点击的是哪一行
    private void HandleTableClick(UITable table, PointerEventData eventData, Action<int> onRowClickedAction)
    {
        if (table == null) return;

        Vector2 localPos;
        if (!table.ScreenPointToChartPoint(eventData.position, out localPos)) return;

        int row = -1;
        int colCount = table.columns != null ? table.columns.Count : 0;
        if (colCount <= 0 || table.context == null || table.context.dataLabels == null) return;

        // 根据点击的本地坐标换算点中了哪一行
        for (int i = 0; i < table.data.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < colCount; j++)
            {
                int idx = i * colCount + j;
                if (idx >= 0 && idx < table.context.dataLabels.Count) // 防止越界
                {
                    var label = table.context.dataLabels[idx];
                    if (label != null && label.rect.Contains(localPos))
                    {
                        row = i;
                        found = true;
                        break;
                    }
                }
            }
            if (found) break;
        }

        if (row >= 0)
        {
            onRowClickedAction?.Invoke(row);
        }
    }

    // ================= 接口数据解析与填充 =================

    /// <summary>
    /// 解析 Frame 72 数据（槽控跨日报表，134 字节/条，大端序）。
    /// 协议格式：企业ID-72-槽号-起始日期-终止日期，返回每天一条记录。
    /// </summary>
    private List<CaokuangRecord> ParseFrame72Data(string responseText)
    {
        var result = new List<CaokuangRecord>();

        byte[] fullData = ParseBase64Response(responseText);
        if (fullData == null || fullData.Length < 14)
        {
            Debug.LogError($"[CaokuangTable] 响应数据无效或长度不足：{(fullData == null ? "null" : fullData.Length.ToString())}");
            return result;
        }

        // 验证帧头 AA 55 与帧尾 A5 5A
        if (fullData[0] != 170 || fullData[1] != 85 ||
            fullData[fullData.Length - 2] != 165 || fullData[fullData.Length - 1] != 90)
        {
            Debug.LogError($"[CaokuangTable] 帧头帧尾校验失败：[{fullData[0]},{fullData[1]}]...[{fullData[fullData.Length - 2]},{fullData[fullData.Length - 1]}]");
            return result;
        }

        // 帧头：企业ID(2) + 帧类型(2) + 槽号(2) + 有效数据长度(4)
        int offset = 2;
        offset += 2; // 企业ID
        offset += 2; // 帧类型
        offset += 2; // 槽号
        uint dataLength = (uint)((fullData[offset] << 24) | (fullData[offset + 1] << 16) |
                                 (fullData[offset + 2] << 8) | fullData[offset + 3]);
        offset += 4;

        byte[] validData = new byte[dataLength];
        Array.Copy(fullData, offset, validData, 0, (int)dataLength);

        // Frame 72 记录 134 字节，字段偏移如下（大端序）：
        //  2-3 槽号 | 4-7 日期(yyyyMMdd) | 12 车间号 | 13 工区号
        //  14-15 设定电压 | 16-17 平均电压 | 18-19 工作电压 | 20-21 系列电流 | 22-23 标准电压
        //  24-25 针振 | 26-27 摆动 | 28-31 总功耗 | 32-33 下料次数 | 34-35 下料量
        //  59-60 氟盐下料次数 | 63-64 基准下料间隔 | 83-84 电压异常持续 | 85-86 摆动持续
        //  91-92 欠量下料次数 | 93-94 过量下料次数 | 95-96 闪烁效应基准
        //  106-107 效应次数 | 108-111 效应持续时间 | 116-119 效应均压 | 120-123 效应功耗
        const int recordSize = 134;
        int recordCount = validData.Length / recordSize;

        for (int i = 0; i < recordCount; i++)
        {
            int rec = i * recordSize;
            try
            {
                var r = new CaokuangRecord();
                r.caoh = (ushort)((validData[rec + 2] << 8) | validData[rec + 3]);
                int dateValue = (validData[rec + 4] << 24) | (validData[rec + 5] << 16) |
                                (validData[rec + 6] << 8) | validData[rec + 7];
                r.recordTime = dateValue.ToString();
                r.sheddy = (validData[rec + 14] << 8) | validData[rec + 15];
                r.pingjdy = (validData[rec + 16] << 8) | validData[rec + 17];
                r.gongzdy = (validData[rec + 18] << 8) | validData[rec + 19];
                r.xiedl = (validData[rec + 20] << 8) | validData[rec + 21];
                r.zhenzhen = (validData[rec + 24] << 8) | validData[rec + 25];
                r.baidong = (validData[rec + 26] << 8) | validData[rec + 27];
                r.xialiaocishu = (validData[rec + 32] << 8) | validData[rec + 33];
                r.qianliang = (validData[rec + 91] << 8) | validData[rec + 92];
                r.guoliang = (validData[rec + 93] << 8) | validData[rec + 94];
                r.xiaoyingcishu = (validData[rec + 106] << 8) | validData[rec + 107];
                result.Add(r);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CaokuangTable] 解析第 {i + 1} 条 Frame72 记录失败：{e.Message}");
            }
        }

        if (result.Count > 0)
            Debug.Log($"[CaokuangTable] 槽号 {result[0].caoh} 解析到 {result.Count} 条记录（最新日期 {result[result.Count - 1].recordTime}）");
        return result;
    }

    /// <summary>
    /// 将接口返回的 Base64 字符串还原为字节数组（自动去除引号与补齐 padding）
    /// </summary>
    private byte[] ParseBase64Response(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogError("[CaokuangTable] 响应内容为空");
            return null;
        }
        try
        {
            string clean = response.Trim('"');
            int paddingCount = clean.Length % 4;
            if (paddingCount > 0)
                clean += new string('=', 4 - paddingCount);
            return System.Convert.FromBase64String(clean);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CaokuangTable] Base64 转换失败：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析工区字符串（如 "1车间1工区"）→ 车间号、工区号
    /// </summary>
    private void ParseWorkArea(string workArea, out byte workshop, out byte area)
    {
        workshop = 1;
        area = 1;
        if (string.IsNullOrEmpty(workArea)) return;

        var matches = System.Text.RegularExpressions.Regex.Matches(workArea, @"\d+");
        if (matches.Count >= 1) byte.TryParse(matches[0].Value, out workshop);
        if (matches.Count >= 2) byte.TryParse(matches[1].Value, out area);
    }

    /// <summary>
    /// 获取某车间工区下所有槽号（优先全局槽配置，未就绪时回退槽号选择器）
    /// </summary>
    private List<ushort> GetSlotNumbers(byte workshop, byte area)
    {
        var list = new List<ushort>();
        if (GlobalElectrolyticCellDataManager.Instance != null)
        {
            var slots = GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea(workshop, area);
            if (slots != null) list.AddRange(slots);
        }

        if (list.Count == 0)
        {
            var selector = FindObjectOfType<SlotNumberSelector>();
            if (selector != null)
            {
                foreach (var s in selector.GetSlotNumberItems())
                {
                    if (ushort.TryParse(s, out ushort sn) && !list.Contains(sn))
                        list.Add(sn);
                }
                if (list.Count > 0)
                    Debug.LogWarning($"[CaokuangTable] 全局槽配置未就绪，使用槽号选择器回退，共 {list.Count} 个槽");
            }
        }
        return list;
    }

    /// <summary>
    /// 当日报表：每个槽一行，取当天最新一条记录。
    /// 列：槽号 / 日期 / 设定电压 / 平均电压 / 工作电压 / 评分值 / 类别
    /// </summary>
    private void FillDangriRow(ushort slot, List<CaokuangRecord> records, string date)
    {
        CaokuangRecord last = records[records.Count - 1]; // 最新一条
        int score = ComputeScore(last);
        dangriTable.AddRow(
            slot.ToString(),
            FormatDate(date),
            FormatVoltage(last.sheddy),
            FormatVoltage(last.pingjdy),
            FormatVoltage(last.gongzdy),
            score.ToString(),
            ScoreToKuangQing(score));
    }

    /// <summary>
    /// 历史趋势：每个槽一行，展示最近 3 天的评分。
    /// 列：槽号 / 评分值1 / 评分值2 / 评分值3 / 平均分 / 趋势 / 整体槽况
    /// </summary>
    private void FillLishiRow(ushort slot, List<CaokuangRecord> records)
    {
        int count = records.Count;
        int n = Mathf.Min(3, count); // 最多取最近 3 天（记录按日期升序返回）

        int[] scores = new int[n];
        for (int i = 0; i < n; i++)
            scores[i] = ComputeScore(records[count - n + i]);

        int s1 = scores[0];                    // 最早一天
        int s3 = scores[n - 1];                // 最近一天
        int s2 = n >= 2 ? scores[n - 2] : s1;  // 中间一天

        double avg = (s1 + s2 + s3) / (double)n;

        string trend;
        if (s3 > s1) trend = "上升 ↑";
        else if (s3 < s1) trend = "下降 ↓";
        else trend = "平稳 →";

        lishiTable.AddRow(
            slot.ToString(),
            s1.ToString(),
            s2.ToString(),
            s3.ToString(),
            avg.ToString("F1"),
            trend,
            ScoreToKuangQing((int)Math.Round(avg)));
    }

    /// <summary>
    /// 评分计算（初步设计，请按实际业务规则调整）。
    /// 基于 Frame72 字段：电压偏差、针振、摆动、效应次数、欠量/过量下料。
    /// </summary>
    private int ComputeScore(CaokuangRecord r)
    {
        int score = 100;

        // 平均电压与设定电压的偏差（单位 mV），每 20mV 扣 1 分
        int dv = Mathf.Abs(r.pingjdy - r.sheddy);
        score -= dv / 20;

        // 针振、摆动（量纲 0.1），每 50 扣 1 分
        score -= r.zhenzhen / 50;
        score -= r.baidong / 50;

        // 每次效应扣 3 分
        score -= r.xiaoyingcishu * 3;

        // 欠量/过量下料每 2 次扣 1 分
        score -= (r.qianliang + r.guoliang) / 2;

        return Mathf.Clamp(score, 0, 100);
    }

    /// <summary>
    /// 评分 → 槽况类别（与槽况分类规则一致）
    /// </summary>
    private string ScoreToKuangQing(int score)
    {
        if (score >= 90) return "A类槽";
        if (score >= 85) return "B类槽";
        if (score >= 75) return "C类槽";
        return "D类槽";
    }

    /// <summary>
    /// "20260826" → "2026-08-26"
    /// </summary>
    private string FormatDate(string yyyyMMdd)
    {
        if (string.IsNullOrEmpty(yyyyMMdd) || yyyyMMdd.Length != 8) return yyyyMMdd;
        return $"{yyyyMMdd.Substring(0, 4)}-{yyyyMMdd.Substring(4, 2)}-{yyyyMMdd.Substring(6, 2)}";
    }

    /// <summary>
    /// 电压原始值（mV）→ V 字符串
    /// </summary>
    private string FormatVoltage(int mV) => (mV / 1000.0).ToString("F3");

    /// <summary>
    /// Frame72 解析用临时数据结构
    /// </summary>
    private class CaokuangRecord
    {
        public ushort caoh;       // 槽号
        public string recordTime; // 日期 yyyyMMdd
        public int sheddy;        // 设定电压 (mV)
        public int pingjdy;       // 平均电压 (mV)
        public int gongzdy;       // 工作电压 (mV)
        public int xiedl;         // 系列电流
        public int zhenzhen;      // 针振
        public int baidong;       // 摆动
        public int xialiaocishu;  // 下料次数
        public int qianliang;     // 欠量下料次数
        public int guoliang;      // 过量下料次数
        public int xiaoyingcishu; // 效应次数
    }
}