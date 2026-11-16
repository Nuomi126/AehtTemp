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
        private const int SectionMonitorFrameType = 2;
        private const int SectionMonitorRecordSize = 41;

        public static ZoneMonitorDataManager Instance;

        public string companyIndex = "1"; // 当前车间号，仅用于界面显示
        public string currentWorkAreaId = "1"; // 接口使用的全局工区ID

        [SerializeField] private string defaultEnterpriseId = "1033";
        [SerializeField] private bool useMockDataWhenUnavailable = true;

        private string currentWorkAreaDisplayName = "1工区";
        private int currentLocalWorkAreaNumber = 1;
        
        private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit"; 
        
        public float pollInterval = 2.0f; // 2秒轮询
        private bool isPollingActive = false; // 是否开始轮询

        public List<ZoneMonitorRealtimeData> currentDataList = new List<ZoneMonitorRealtimeData>();

        // 数据更新事件
        public delegate void OnDataUpdatedHandler(List<ZoneMonitorRealtimeData> data);
        public event OnDataUpdatedHandler OnDataUpdated;

        private Coroutine pollingCoroutine;
        private int requestGeneration;

        public string CurrentWorkAreaDisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(currentWorkAreaDisplayName)
                    ? currentWorkAreaId + "工区"
                    : currentWorkAreaDisplayName;
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Debug.LogWarning(
                $"[ZoneMonitorDataManager] Start 初始化：企业:{ResolveEnterpriseId()} 车间:{companyIndex} 工区ID:{currentWorkAreaId}");
            StartMonitor();
        }

        // WorkAreaSelector 传入格式类似 "1车间1工区"。
        public void OnConditionSelected(string formattedAreaString)
        {
            Debug.LogWarning($"[ZoneMonitorDataManager] 接收到工区切换事件: {formattedAreaString}");

            if (string.IsNullOrWhiteSpace(formattedAreaString))
            {
                Debug.LogWarning("[ZoneMonitorDataManager] 收到的工区字符串为空，跳过处理");
                return;
            }

            string selectedName = formattedAreaString.Trim();
            WorkAreaData matchedArea = FindWorkArea(selectedName);
            if (matchedArea != null)
            {
                companyIndex = matchedArea.WorkshopNumber;
                currentWorkAreaId = matchedArea.WorkAreaNumber;
                currentLocalWorkAreaNumber = ExtractLocalWorkAreaNumber(matchedArea.WorkAreaName, currentWorkAreaId);
                currentWorkAreaDisplayName = currentLocalWorkAreaNumber + "工区";
            }
            else
            {
                // 登录工区配置暂未加载时保留兼容路径，但只作为兜底。
                var match = System.Text.RegularExpressions.Regex.Match(selectedName, @"(\d+)\D+(\d+)\s*工区");
                if (!match.Success)
                {
                    Debug.LogError($"[ZoneMonitorDataManager] 无法识别工区 '{selectedName}'，本次不发起请求。");
                    return;
                }

                companyIndex = match.Groups[1].Value;
                currentWorkAreaId = match.Groups[2].Value;
                currentLocalWorkAreaNumber = int.Parse(match.Groups[2].Value);
                currentWorkAreaDisplayName = currentLocalWorkAreaNumber + "工区";
                Debug.LogWarning(
                    $"[ZoneMonitorDataManager] 未找到登录工区映射，临时使用显示编号 {currentWorkAreaId}。" +
                    "二车间等场景可能需要等待工区配置加载完成。");
            }

            Debug.LogWarning(
                $"[ZoneMonitorDataManager] 工区映射完成：企业:{ResolveEnterpriseId()} 车间:{companyIndex} 工区ID:{currentWorkAreaId}");
            StartMonitor();
        }

        public void StartMonitor()
        {
            isPollingActive = true;
            requestGeneration++;

            if (pollingCoroutine != null)
                StopCoroutine(pollingCoroutine);

            pollingCoroutine = StartCoroutine(PollDataRoutine(requestGeneration));
        }

        public void StopPolling()
        {
            isPollingActive = false;
            requestGeneration++;

            if (pollingCoroutine != null)
            {
                StopCoroutine(pollingCoroutine);
                pollingCoroutine = null;
            }
        }

        private void FetchDataImmediately()
        {
            if (!isPollingActive) return;
            StartCoroutine(RequestData(currentWorkAreaId, requestGeneration));
        }

        private IEnumerator PollDataRoutine(int generation)
        {
            while (isPollingActive && generation == requestGeneration)
            {
                yield return StartCoroutine(RequestData(currentWorkAreaId, generation));
                yield return new WaitForSeconds(pollInterval);
            }
        }

        private IEnumerator RequestData(string areaId, int generation)
        {
            string enterpriseId = ResolveEnterpriseId();
            string testParam = enterpriseId + "-" + SectionMonitorFrameType + "-" + areaId;
            string jsonData = "\"" + testParam + "\"";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);

            Debug.LogWarning($"[ZoneMonitorDataManager] 开始请求 API，参数: {testParam}, URL: {apiUrl}");
            
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(byteArray);
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 15;

                yield return request.SendWebRequest();

                // 切换工区后，旧请求即使晚返回也不能覆盖新工区的数据。
                if (generation != requestGeneration || areaId != currentWorkAreaId)
                {
                    Debug.Log($"[ZoneMonitorDataManager] 忽略过期响应: {testParam}");
                    yield break;
                }

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
                    
                    byte[] decodedBytes;
                    if (!TryDecodeResponse(responseText, out decodedBytes))
                    {
                        HandleUnavailableData($"接口未返回可解析的 Frame 2 数据，参数: {testParam}");
                        yield break;
                    }

                    Debug.LogWarning($"[ZoneMonitorDataManager] Base64解码成功，解码后字节数: {decodedBytes.Length}");
                    List<ZoneMonitorRealtimeData> parsedRows;
                    if (!TryParseSectionMonitorData(decodedBytes, out parsedRows))
                    {
                        HandleUnavailableData($"Frame 2 数据格式无效，参数: {testParam}");
                        yield break;
                    }

                    PublishRows(parsedRows);
                }
                else
                {
                    Debug.LogError($"[ZoneMonitorDataManager] API Request Error - HTTP状态码: {request.responseCode}");
                    Debug.LogError($"[ZoneMonitorDataManager] 错误信息: {request.error}");
                    HandleUnavailableData($"接口请求失败: {request.error}");
                }
            }
        }

        private string ResolveEnterpriseId()
        {
            return !string.IsNullOrWhiteSpace(GlobalUserData.AlSerialPantId)
                ? GlobalUserData.AlSerialPantId.Trim()
                : defaultEnterpriseId;
        }

        private static WorkAreaData FindWorkArea(string selectedName)
        {
            if (GlobalUserData.WorkAreaList == null)
                return null;

            foreach (WorkAreaData area in GlobalUserData.WorkAreaList)
            {
                if (area != null && string.Equals(area.WorkAreaName, selectedName, StringComparison.OrdinalIgnoreCase))
                    return area;
            }

            return null;
        }

        private static int ExtractLocalWorkAreaNumber(string workAreaName, string fallbackId)
        {
            if (!string.IsNullOrWhiteSpace(workAreaName))
            {
                var match = System.Text.RegularExpressions.Regex.Match(workAreaName, @"(\d+)\s*工区");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int localAreaNumber))
                    return localAreaNumber;
            }

            return int.TryParse(fallbackId, out int fallbackAreaNumber) ? fallbackAreaNumber : 1;
        }

        private static bool TryDecodeResponse(string responseText, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrWhiteSpace(responseText))
                return false;

            string base64Text = responseText.Trim();
            if (base64Text == "\"\"")
                return false;

            if (base64Text.StartsWith("\"") && base64Text.EndsWith("\"") && base64Text.Length >= 2)
                base64Text = base64Text.Substring(1, base64Text.Length - 2);

            if (string.IsNullOrWhiteSpace(base64Text))
                return false;

            int remainder = base64Text.Length % 4;
            if (remainder != 0)
                base64Text += new string('=', 4 - remainder);

            try
            {
                bytes = Convert.FromBase64String(base64Text);
                return bytes.Length > 0;
            }
            catch (FormatException ex)
            {
                Debug.LogError($"[ZoneMonitorDataManager] Base64解析失败: {ex.Message}");
                return false;
            }
        }

        private static bool TryParseSectionMonitorData(byte[] bytes, out List<ZoneMonitorRealtimeData> rows)
        {
            rows = new List<ZoneMonitorRealtimeData>();
            if (bytes == null || bytes.Length == 0)
                return false;

            // Alwit 正常响应包含 AA55 帧头、12字节头部和 A55A 帧尾。
            if (bytes.Length >= 14 && bytes[0] == 0xAA && bytes[1] == 0x55)
                return TryParseFramedData(bytes, rows);

            // 兼容旧服务可能直接返回纯数据区的情况。
            ParseRecordRange(bytes, 0, bytes.Length, rows);
            return rows.Count > 0;
        }

        private static bool TryParseFramedData(byte[] bytes, List<ZoneMonitorRealtimeData> rows)
        {
            int offset = 0;
            bool foundSectionFrame = false;

            while (offset + 14 <= bytes.Length)
            {
                if (bytes[offset] != 0xAA || bytes[offset + 1] != 0x55)
                {
                    offset++;
                    continue;
                }

                int frameStart = offset;
                int enterpriseId = ReadUInt16BigEndian(bytes, offset + 2);
                int frameType = ReadUInt16BigEndian(bytes, offset + 4);
                int areaOrSlotId = ReadUInt16BigEndian(bytes, offset + 6);
                int dataLength = ReadInt32BigEndian(bytes, offset + 8);
                int dataStart = offset + 12;
                int dataEnd = dataStart + dataLength;

                if (dataLength < 0 || dataEnd + 2 > bytes.Length)
                {
                    Debug.LogError(
                        $"[ZoneMonitorDataManager] Frame 2长度无效: enterprise={enterpriseId}, " +
                        $"area={areaOrSlotId}, dataLength={dataLength}, total={bytes.Length}");
                    return false;
                }

                bool tailValid = bytes[dataEnd] == 0xA5 && bytes[dataEnd + 1] == 0x5A;
                if (!tailValid)
                {
                    Debug.LogError($"[ZoneMonitorDataManager] 帧尾无效，frameStart={frameStart}");
                    return false;
                }

                if (frameType == SectionMonitorFrameType)
                {
                    foundSectionFrame = true;
                    ParseRecordRange(bytes, dataStart, dataLength, rows);
                }

                offset = dataEnd + 2;
            }

            return foundSectionFrame && rows.Count > 0;
        }

        private static void ParseRecordRange(
            byte[] bytes,
            int startIndex,
            int length,
            List<ZoneMonitorRealtimeData> rows)
        {
            int recordCount = length / SectionMonitorRecordSize;
            int remainder = length % SectionMonitorRecordSize;
            if (remainder != 0)
            {
                Debug.LogWarning(
                    $"[ZoneMonitorDataManager] Frame 2数据区长度 {length} 不是 {SectionMonitorRecordSize} 的整数倍，" +
                    $"末尾 {remainder} 字节将忽略。");
            }

            for (int i = 0; i < recordCount; i++)
            {
                ZoneMonitorRealtimeData row = ZoneMonitorRealtimeData.Parse(
                    bytes,
                    startIndex + i * SectionMonitorRecordSize);
                if (row != null)
                    rows.Add(row);
            }
        }

        private static int ReadUInt16BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 8) | bytes[offset + 1];
        }

        private static int ReadInt32BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) |
                   (bytes[offset + 1] << 16) |
                   (bytes[offset + 2] << 8) |
                   bytes[offset + 3];
        }

        private void PublishRows(List<ZoneMonitorRealtimeData> rows)
        {
            currentDataList.Clear();
            if (rows != null)
                currentDataList.AddRange(rows);

            Debug.Log($"[ZoneMonitorDataManager] 分区监控数据更新: {currentDataList.Count} 条");
            OnDataUpdated?.Invoke(currentDataList);
        }

        private void HandleUnavailableData(string reason)
        {
            Debug.LogWarning($"[ZoneMonitorDataManager] {reason}");
            if (useMockDataWhenUnavailable)
            {
                InjectFakeData();
                return;
            }

            currentDataList.Clear();
            OnDataUpdated?.Invoke(currentDataList);
        }

        // ---------- 用来塞入假数据让页面能显示东西的函数 ----------
        private void InjectFakeData()
        {
            currentDataList.Clear();
            List<ushort> mockSlots = ResolveMockSlotNumbers();

            for (int i = 0; i < mockSlots.Count; i++)
            {
                ZoneMonitorRealtimeData fakeItem = new ZoneMonitorRealtimeData();
                fakeItem.SlotId = mockSlots[i];
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

        private List<ushort> ResolveMockSlotNumbers()
        {
            List<ushort> slots = new List<ushort>();
            if (byte.TryParse(companyIndex, out byte workshopNumber) &&
                currentLocalWorkAreaNumber > 0 && currentLocalWorkAreaNumber <= byte.MaxValue &&
                GlobalElectrolyticCellDataManager.Instance != null &&
                GlobalElectrolyticCellDataManager.Instance.isDataLoaded)
            {
                List<ushort> configuredSlots =
                    GlobalElectrolyticCellDataManager.Instance.GetSlotNumbersByWorkshopAndArea(
                        workshopNumber,
                        (byte)currentLocalWorkAreaNumber);

                int configuredCount = Mathf.Min(10, configuredSlots.Count);
                for (int i = 0; i < configuredCount; i++)
                    slots.Add(configuredSlots[i]);
            }

            if (slots.Count > 0)
                return slots;

            int workshop = int.TryParse(companyIndex, out int parsedWorkshop) ? parsedWorkshop : 1;
            int area = Mathf.Max(1, currentLocalWorkAreaNumber);
            int baseSlotId = workshop * 1000 + 1 + (area - 1) * 55;
            for (int i = 0; i < 10; i++)
                slots.Add((ushort)(baseSlotId + i));

            return slots;
        }

        private void OnDestroy()
        {
            StopPolling();
            if (Instance == this)
                Instance = null;
        }
    }
}
