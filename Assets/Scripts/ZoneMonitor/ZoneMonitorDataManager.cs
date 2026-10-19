using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ZoneMonitor
{
    [System.Serializable]
    public class ZoneMonitorRealtimeData
    {
        public int SlotId;              // 槽号
        public float SlotVoltage;       // 槽电压 (mV)
        public float NeedleVibration;   // 针振 (mV)
        public float Swing;             // 摆动 (mV)
        public float VoltageUpperLimit; // 电压上限 (mV)
        public float VoltageLowerLimit; // 电压下限 (mV)
        public float BaseFeedInterval;  // 基准下料间隔 (0.1秒)
        public float ActualFeedInterval;// 实际下料间隔 (0.1秒)
        public float SetVoltage;        // 设定电压 (mV)
        public float BusbarPosition;    // 母线位置 (0.1mm)
        
        // 状态信息原始值
        public byte StateInfo1;
        public byte StateInfo2;
        public byte StateInfo3;
        
        // 解析出的文本状态 (极控 ＆ 料控)
        public string PolarControlState; // 极控
        public string FeedControlState;  // 料控
        
        public byte FaultInfo1;
        public byte FaultInfo2;
        public byte AbnormalFeed1;
        public byte AbnormalFeed2;
        public byte AbnormalFeed3;
        public byte AbnormalFeed4;
        
        // 效应信息
        public string RecentEffectTime;  // 最近效应发生时间 (月-日 时:分)
        public int RecentEffectDuration;// 最近效应持续时间 (min)
        
        public float DailyUnderOverRatio; // 日欠过比 (0.01)
        public float DailyAvgVoltage;     // 日均电压 (mV)
        public float DailyAvgFeed;        // 日均料量 (kg)

        public static ZoneMonitorRealtimeData Parse(byte[] data, int startIndex)
        {
            if (startIndex + 41 > data.Length) return null;

            ZoneMonitorRealtimeData item = new ZoneMonitorRealtimeData();

            // 解析2字节数据 (H*256+L)
            item.SlotId = (data[startIndex + 0] << 8) | data[startIndex + 1];
            item.SlotVoltage = (data[startIndex + 2] << 8) | data[startIndex + 3];
            item.NeedleVibration = (data[startIndex + 4] << 8) | data[startIndex + 5];
            item.Swing = (data[startIndex + 6] << 8) | data[startIndex + 7];
            item.VoltageUpperLimit = (data[startIndex + 8] << 8) | data[startIndex + 9];
            item.VoltageLowerLimit = (data[startIndex + 10] << 8) | data[startIndex + 11];
            item.BaseFeedInterval = ((data[startIndex + 12] << 8) | data[startIndex + 13]) * 0.1f;
            item.ActualFeedInterval = ((data[startIndex + 14] << 8) | data[startIndex + 15]) * 0.1f;
            item.SetVoltage = (data[startIndex + 16] << 8) | data[startIndex + 17];
            item.BusbarPosition = ((data[startIndex + 18] << 8) | data[startIndex + 19]) * 0.1f;

            // 1字节状态
            item.StateInfo1 = data[startIndex + 20];
            item.StateInfo2 = data[startIndex + 21];
            item.StateInfo3 = data[startIndex + 22];
            item.FaultInfo1 = data[startIndex + 23];
            item.FaultInfo2 = data[startIndex + 24];
            item.AbnormalFeed1 = data[startIndex + 25];
            item.AbnormalFeed2 = data[startIndex + 26];
            item.AbnormalFeed3 = data[startIndex + 27];
            item.AbnormalFeed4 = data[startIndex + 28];

            int month = data[startIndex + 29];
            int day = data[startIndex + 30];
            int hour = data[startIndex + 31];
            int min = data[startIndex + 32];
            item.RecentEffectTime = $"{month:D2}-{day:D2} {hour:D2}:{min:D2}";
            
            item.RecentEffectDuration = (data[startIndex + 33] << 8) | data[startIndex + 34];
            item.DailyUnderOverRatio = ((data[startIndex + 35] << 8) | data[startIndex + 36]) * 0.01f;
            item.DailyAvgVoltage = (data[startIndex + 37] << 8) | data[startIndex + 38];
            item.DailyAvgFeed = (data[startIndex + 39] << 8) | data[startIndex + 40];

            // 派生状态解析
            // 极控判断 (状态信息3 Bit2)
            bool isStopControl = (item.StateInfo3 & (1 << 2)) != 0;
            item.PolarControlState = isStopControl ? "停控" : "自动";

            // 料控判断 (状态信息3 Bit1, Bit0)
            bool isTiming = (item.StateInfo3 & (1 << 1)) != 0;
            bool isStopFeed = (item.StateInfo3 & (1 << 0)) != 0;
            if (isTiming) item.FeedControlState = "定时";
            else if (isStopFeed) item.FeedControlState = "停料";
            else item.FeedControlState = "自动";

            return item;
        }
    }

    public class ZoneMonitorDataManager : MonoBehaviour
    {
        public static ZoneMonitorDataManager Instance;

        public string companyIndex = "1"; // 一厂/二厂等
        public string currentWorkAreaId = "1"; // 当前选中的工区
        
        private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit"; 
        
        public float pollInterval = 2.0f; // 2秒轮询
        private bool isPollingActive = false; // 是否开始轮询

        public List<ZoneMonitorRealtimeData> currentDataList = new List<ZoneMonitorRealtimeData>();

        // 数据更新事件
        public delegate void OnDataUpdatedHandler(List<ZoneMonitorRealtimeData> data);
        public event OnDataUpdatedHandler OnDataUpdated;

        private Coroutine pollingCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Debug.LogWarning($"[ZoneMonitorDataManager] Start 初始化，立刻使用参数发起请求：车间:{companyIndex} 工区:{currentWorkAreaId}");
            StartMonitor();
        }

    // --- 给你的工区选择按钮/下拉框/WorkAreaSelector调用的方法 ---
    // 参数传入格式类似 "1车间1工区" 
    public void OnConditionSelected(string formattedAreaString)
    {
        Debug.LogWarning($"[ZoneMonitorDataManager] ---> 接收到工区切换事件: {formattedAreaString} <---");
        
        if (string.IsNullOrEmpty(formattedAreaString)) 
        {
            Debug.LogWarning("[ZoneMonitorDataManager] 收到的工区字符串为空，跳过处理");
            return;
        }

        // 尝试解析 "X车间Y工区" 格式
        var match = System.Text.RegularExpressions.Regex.Match(formattedAreaString, @"(\d+).*?(\d+)");
        if (match.Success)
        {
            companyIndex = match.Groups[1].Value;
            currentWorkAreaId = match.Groups[2].Value;
            Debug.LogWarning($"[ZoneMonitorDataManager] 正则解析工区字符成功，车间:{companyIndex}, 工区:{currentWorkAreaId}");
        }
        else
        {
            // 如果传进来的只认出一个数字或者其它情况：
            var singleMatch = System.Text.RegularExpressions.Regex.Match(formattedAreaString, @"\d+");
            if (singleMatch.Success)
            {
                currentWorkAreaId = singleMatch.Value;
                Debug.LogWarning($"[ZoneMonitorDataManager] 只提取到部分数字，将尝试设为工区:{currentWorkAreaId} (车间保留:{companyIndex})");
            }
            else
            {
                Debug.LogWarning($"[ZoneMonitorDataManager] 完全无法解析 {formattedAreaString} 里的数字！强制使用原始字符串。");
                currentWorkAreaId = formattedAreaString;
            }
        }

        Debug.LogWarning($"[ZoneMonitorDataManager] 准备启动监控轮询 -> 最终参数将会是: {companyIndex}-2-{currentWorkAreaId}");
        StartMonitor();
    }

    public void StartMonitor()
        {
            isPollingActive = true;
            if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
            pollingCoroutine = StartCoroutine(PollDataRoutine());
        }

        public void StopPolling()
        {
            isPollingActive = false;
            if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
        }

        private void FetchDataImmediately()
        {
            if (!isPollingActive) return;
            StartCoroutine(RequestData(currentWorkAreaId));
        }

        private IEnumerator PollDataRoutine()
        {
            while (true)
            {
                yield return StartCoroutine(RequestData(currentWorkAreaId));
                yield return new WaitForSeconds(pollInterval);
            }
        }

        private string lastResponseText = null;
        private string lastTestParam = null;

        private IEnumerator RequestData(string areaId)
        {
            // 构造类似 1-2-1 的请求体 (其中 2 表示 "分区监控实时信息")
            string testParam = companyIndex + "-2-" + areaId;
            string jsonData = "\"" + testParam + "\"";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 无论响应是否相同，每次发起请求前都强制打印一遍日志
            Debug.LogWarning($"[ZoneMonitorDataManager] 开始请求 API，参数: {testParam}, URL: {apiUrl}");
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(byteArray);
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    
                    Debug.LogWarning($"[ZoneMonitorDataManager] API 请求完成 - HTTP状态码: {request.responseCode}");
                    Debug.LogWarning($"[ZoneMonitorDataManager] API 响应长度: {responseText.Length} 字符");
                    
                    if (responseText.Length > 0 && responseText.Length <= 100)
                    {
                        Debug.LogWarning($"[ZoneMonitorDataManager] 返回内容预览: {responseText}");
                    }
                    else if (responseText.Length > 100)
                    {
                        Debug.LogWarning($"[ZoneMonitorDataManager] 返回内容预览: {responseText.Substring(0, 100)}...");
                    }
                    else
                    {
                        Debug.LogWarning("[ZoneMonitorDataManager] 返回内容为空！");
                    }
                    
                    // 清理上一次的数据，准备解析新数据（即使是空也先清空）
                    currentDataList.Clear();

                    if (!string.IsNullOrEmpty(responseText) && responseText.Length > 2 && responseText.StartsWith("\"") && responseText != "\"\"")
                    {
                        // 去掉前后的引号
                        string base64Str = responseText.Substring(1, responseText.Length - 2);
                        // 处理可能的填充
                        int paddingCount = base64Str.Length % 4;
                        if (paddingCount > 0)
                        {
                            base64Str += new string('=', 4 - paddingCount);
                        }

                        if (base64Str.Length > 0)
                        {
                            try
                            {
                                byte[] bytes = System.Convert.FromBase64String(base64Str);
                                Debug.LogWarning($"[ZoneMonitorDataManager] Base64解码成功，解码后字节数: {bytes.Length}");
                                ParseRawData(bytes);
                                // 解析出真实数据时，正常回调填充表格
                                OnDataUpdated?.Invoke(currentDataList);
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[ZoneMonitorDataManager] Base64 解析失败: {ex.Message}");
                                InjectFakeData(); // 解析失败使用模拟数据填充
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[ZoneMonitorDataManager] Base64字符串为空，未能提取出有效数据。");
                            InjectFakeData();  // 解析为空使用模拟数据填充
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ZoneMonitorDataManager] 接口返回为空或者不是正确格式。开始填充随机数据...");
                        InjectFakeData(); // 如果返回空（如 "" ），使用随机数据填充表格
                    }
                }
                else
                {
                    Debug.LogError($"[ZoneMonitorDataManager] API Request Error - HTTP状态码: {request.responseCode}");
                    Debug.LogError($"[ZoneMonitorDataManager] 错误信息: {request.error}");
                    InjectFakeData(); // 网络错误使用随机数据填充表格
                }
            }
        }

        // ---------- 用来塞入假数据让页面能显示东西的函数 ----------
        private void InjectFakeData()
        {
            currentDataList.Clear();
            int baseSlotId = 2001; // 假装从 2001 槽开始

            for (int i = 0; i < 10; i++) // 生成 10 条伪造数据
            {
                ZoneMonitorRealtimeData fakeItem = new ZoneMonitorRealtimeData();
                fakeItem.SlotId = baseSlotId + i;
                fakeItem.SlotVoltage = 3981 + UnityEngine.Random.Range(-5, 5); // 毫伏级微小波动
                fakeItem.NeedleVibration = UnityEngine.Random.Range(1, 16); 
                fakeItem.Swing = UnityEngine.Random.Range(1, 3);
                fakeItem.VoltageUpperLimit = 3981;
                fakeItem.VoltageLowerLimit = 3820;
                fakeItem.BaseFeedInterval = 96;
                fakeItem.ActualFeedInterval = UnityEngine.Random.Range(60, 120);
                fakeItem.SetVoltage = 3960;
                fakeItem.BusbarPosition = 165f + UnityEngine.Random.Range(0, 50) * 0.1f;
                // 这几个需要计算显示成字符
                fakeItem.PolarControlState = "自动";
                int feedRandom = UnityEngine.Random.Range(0, 3);
                fakeItem.FeedControlState = feedRandom == 0 ? "自动" : (feedRandom == 1 ? "自动下料" : "单点下料");
                fakeItem.RecentEffectTime = $"11-12/07:45"; 
                fakeItem.RecentEffectDuration = UnityEngine.Random.Range(0, 35);
                fakeItem.DailyUnderOverRatio = 0.5f + UnityEngine.Random.Range(0, 40) * 0.01f;
                fakeItem.DailyAvgVoltage = 3900 + UnityEngine.Random.Range(0, 80);
                fakeItem.DailyAvgFeed = 3300 + UnityEngine.Random.Range(0, 400);

                currentDataList.Add(fakeItem);
            }

            Debug.LogWarning($"[ZoneMonitorDataManager] 成功伪造 {currentDataList.Count} 条数据，开始通知UI更新！");
            OnDataUpdated?.Invoke(currentDataList);
        }

        private void ParseRawData(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;

            currentDataList.Clear();
            int totalRecords = bytes.Length / 41;
            
            if (totalRecords > 0)
            {
                Debug.LogWarning($"[ZoneMonitorDataManager] 开始按照41字节切分数据。总字节数: {bytes.Length}, 将解析为 {totalRecords} 条槽记录。");
            }

            for (int i = 0; i < totalRecords; i++)
            {
                var data = ZoneMonitorRealtimeData.Parse(bytes, i * 41);
                if (data != null)
                {
                    currentDataList.Add(data);
                }
            }

            Debug.Log($"[ZoneMonitorDataManager] 数据解析完成。一共存入了 {currentDataList.Count} 条记录，即将下发给 UI 更新。");

            // 触发UI更新通知
            OnDataUpdated?.Invoke(currentDataList);
        }
    }
}