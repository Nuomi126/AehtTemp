using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
#if INPUT_SYSTEM_ENABLED
using Input = XCharts.Runtime.InputHelper;
#endif
using XCharts.Runtime;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace XCharts.Example
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Line_API : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033"; // 电解企业标识
        [SerializeField] private int connectionTimeout = 30;
        
        [Header("筛选设置")]
        public TextMeshProUGUI potNameText; // 电解槽文本
        public TextMeshProUGUI dateText;    // 日期文本
        public Button applyFilterButton;
        
        [Header("图表设置")]
        public Color lineColor = Color.blue;
        
        // 筛选条件
        private string selectedPotName = "1001";
        private string selectedDate = "20250622";
        
        // 数据缓存
        private List<TempData> cachedData = new List<TempData>();
        
        // 数据结构
        [System.Serializable]
        public class TempData
        {
            public string potName;
            public string dateData;
            public int sampleIndex; // 采样编号 (0-252)
            public float tempValue;
            public short recordIndex; // 记录号
            
            public TempData(string pot, string date, int sample, float temp, short record)
            {
                potName = pot;
                dateData = date;
                sampleIndex = sample;
                tempValue = temp;
                recordIndex = record;
            }
        }
        
        // 帧数据结构
        [System.Serializable]
        public class FrameData
        {
            public byte[] frameHeader = new byte[2]; // 帧头 170, 85
            public ushort companyId;                 // 服务电解企业标识
            public ushort frameType;                 // 帧类型
            public ushort slotNumber;                // 槽号
            public uint dataLength;                  // 有效数据长度
            public byte[] data;                      // 数据具体内容
            public byte[] frameFooter = new byte[2]; // 帧尾 165, 90
            
            public bool IsValid()
            {
                return frameHeader[0] == 170 && frameHeader[1] == 85 &&
                       frameFooter[0] == 165 && frameFooter[1] == 90;
            }
        }
        
        void Awake()
        {
            // 初始化UI
            InitUI();
        }
        
        // 初始化UI
        void InitUI()
        {
            // 初始化输入框
            if (potNameText != null)
            {
                potNameText.text = selectedPotName;
            }
            
            if (dateText != null)
            {
                dateText.text = selectedDate;
            }
            
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveAllListeners();
                applyFilterButton.onClick.AddListener(OnApplyFilter);
            }
        }
        
        // 应用筛选按钮点击事件
        private void OnApplyFilter()
        {
            // 获取文本的值
            if (potNameText != null)
            {
                selectedPotName = potNameText.text;
            }
            
            if (dateText != null)
            {
                selectedDate = dateText.text;
            }
            
            Debug.Log($"应用筛选: 电解槽={selectedPotName}, 日期={selectedDate}");
            
            // 加载数据
            LoadDataFromAPI();
        }
        
        // 从API加载数据
        private void LoadDataFromAPI()
        {
            // 检查游戏对象是否激活
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("游戏对象未激活，无法启动协程。请确保对象已激活。");
                return;
            }
            
            // 清除缓存数据
            cachedData.Clear();
            
            // 启动协程获取温度数据
            StartCoroutine(FetchTempDataFromAPI());
        }
        
        // 从API获取温度数据的协程
        private IEnumerator FetchTempDataFromAPI()
        {
            // 构建请求参数：电解企业标识 + "-67-" + 槽号 + "-" + 日期
            string requestParam = $"{companyId}-67-{selectedPotName}-{selectedDate}";
            
            Debug.Log($"开始获取温度历史数据，参数: {requestParam}");
            
            // 按照现有API模式创建请求
            string jsonData = $"\"{requestParam}\"";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
            
            // 创建UnityWebRequest
            UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = connectionTimeout;
            
            Debug.Log($"发送请求到: {apiUrl}");
            Debug.Log($"请求参数: {jsonData}");
            
            // 记录请求开始时间
            DateTime requestStartTime = DateTime.Now;
            
            // 发送请求
            yield return webRequest.SendWebRequest();
            
            // 记录请求结束时间
            DateTime requestEndTime = DateTime.Now;
            TimeSpan requestDuration = requestEndTime - requestStartTime;
            
            Debug.Log($"请求完成，耗时: {requestDuration.TotalMilliseconds:F2} 毫秒");
            Debug.Log($"请求状态: {webRequest.result}");
            
            // 处理响应
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler.text;
                
                Debug.Log($"API响应: {responseText}");
                
                // 解析响应数据
                ParseTempDataResponse(responseText);
            }
            else
            {
                string errorMsg = $"请求失败: {webRequest.error}";
                Debug.LogError(errorMsg);
                
                // 生成测试数据用于调试
                GenerateTestData();
            }
            
            // 清理资源
            webRequest.Dispose();
        }
        
        // 解析温度数据响应
        private void ParseTempDataResponse(string responseText)
        {
            try
            {
                cachedData.Clear();
                
                // 去除可能的引号
                string cleanResponse = responseText;
                if (responseText.StartsWith("\"") && responseText.EndsWith("\""))
                {
                    cleanResponse = responseText.Substring(1, responseText.Length - 2);
                }
                
                Debug.Log($"清理后的响应: {cleanResponse}");
                
                // 处理Base64填充
                string tmpText = cleanResponse;
                int paddingCount = tmpText.Length % 4;
                if (paddingCount > 0)
                {
                    for (int i = 0; i < 4 - paddingCount; i++)
                    {
                        tmpText += "=";
                    }
                }
                
                // 将Base64字符串转换为字节数组
                byte[] responseData = Convert.FromBase64String(tmpText);
                
                Debug.Log($"解码后数据长度: {responseData.Length} 字节");
                Debug.Log($"原始字节数据(十六进制): {BitConverter.ToString(responseData)}");
                
                // 解析帧数据
                ParseFrameData(responseData);
            }
            catch (Exception ex)
            {
                string errorMsg = $"解析温度数据时出错: {ex.Message}";
                Debug.LogError(errorMsg);
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                
                // 生成测试数据
                GenerateTestData();
            }
        }
        
        // 解析帧数据
        private void ParseFrameData(byte[] data)
        {
            if (data.Length < 14) // 最小帧长度：帧头(2) + 标识(2) + 帧类型(2) + 槽号(2) + 长度(4) + 帧尾(2)
            {
                Debug.LogError($"数据长度不足，无法解析帧数据。实际长度: {data.Length}");
                return;
            }
            
            int offset = 0;
            FrameData frame = new FrameData();
            
            // 解析帧头 (2字节)
            frame.frameHeader[0] = data[offset];
            frame.frameHeader[1] = data[offset + 1];
            offset += 2;
            
            // 解析服务电解企业标识 (2字节，大端序)
            frame.companyId = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            
            // 解析帧类型 (2字节，大端序)
            frame.frameType = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            
            // 解析槽号 (2字节，大端序)
            frame.slotNumber = (ushort)((data[offset] << 8) | data[offset + 1]);
            offset += 2;
            
            // 解析有效数据长度 (4字节，大端序)
            frame.dataLength = (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
            offset += 4;
            
            // 解析帧尾 (最后2字节)
            if (data.Length >= 2)
            {
                frame.frameFooter[0] = data[data.Length - 2];
                frame.frameFooter[1] = data[data.Length - 1];
            }
            
            // 验证帧格式
            if (!frame.IsValid())
            {
                Debug.LogWarning($"帧格式验证失败。帧头: {frame.frameHeader[0]},{frame.frameHeader[1]}, 帧尾: {frame.frameFooter[0]},{frame.frameFooter[1]}");
            }
            
            Debug.Log($"帧解析结果 - 企业标识: {frame.companyId}, 帧类型: {frame.frameType}, 槽号: {frame.slotNumber}, 数据长度: {frame.dataLength}");
            
            // 提取有效数据
            int dataStartIndex = offset;
            int dataEndIndex = data.Length - 2; // 排除帧尾
            int actualDataLength = dataEndIndex - dataStartIndex;
            
            if (actualDataLength != frame.dataLength)
            {
                Debug.LogWarning($"实际数据长度({actualDataLength})与声明长度({frame.dataLength})不匹配");
            }
            
            if (actualDataLength > 0)
            {
                frame.data = new byte[actualDataLength];
                Array.Copy(data, dataStartIndex, frame.data, 0, actualDataLength);
                
                // 解析温度历史数据
                ParseTemperatureHistoryData(frame.data);
            }
            else
            {
                Debug.LogError("没有有效的温度数据");
            }
        }
        
        // 解析温度历史数据
        private void ParseTemperatureHistoryData(byte[] data)
        {
            // 数据长度应该是512的整数倍
            if (data.Length % 512 != 0)
            {
                Debug.LogWarning($"数据长度({data.Length})不是512的整数倍");
            }
            
            int recordCount = data.Length / 512;
            Debug.Log($"解析 {recordCount} 条温度记录");
            
            for (int recordIndex = 0; recordIndex < recordCount; recordIndex++)
            {
                int recordOffset = recordIndex * 512;
                
                // 第1-4字节为日期
                uint dateValue = (uint)((data[recordOffset] << 24) | (data[recordOffset + 1] << 16) | 
                                       (data[recordOffset + 2] << 8) | data[recordOffset + 3]);
                
                // 第5-6字节为记录号
                short recordNumber = (short)((data[recordOffset + 4] << 8) | data[recordOffset + 5]);
                
                Debug.Log($"记录 {recordIndex + 1}: 日期={dateValue}, 记录号={recordNumber}");
                
                // 从第7个字节开始，每2字节代表一个采样编号的数据，共253个采样编号
                for (int sampleIndex = 0; sampleIndex < 253; sampleIndex++)
                {
                    int sampleOffset = recordOffset + 6 + (sampleIndex * 2);
                    
                    if (sampleOffset + 1 < data.Length)
                    {
                        // 读取2字节温度数据（大端序）
                        short tempRaw = (short)((data[sampleOffset] << 8) | data[sampleOffset + 1]);
                        
                        // 转换为实际温度值（可能需要根据实际情况调整转换公式）
                        float tempValue = tempRaw / 10.0f; // 假设温度值需要除以10
                        
                        // 创建温度数据对象
                        TempData tempData = new TempData(
                            selectedPotName,
                            selectedDate,
                            sampleIndex,
                            tempValue,
                            recordNumber
                        );
                        
                        cachedData.Add(tempData);
                    }
                }
            }
            
            Debug.Log($"解析完成，共获得 {cachedData.Count} 个温度数据点");
            
            // 更新图表
            UpdateChart();
        }
        
        // 生成测试数据（当API连接失败时使用）
        private void GenerateTestData()
        {
            Debug.Log("生成测试数据用于调试");
            
            cachedData.Clear();
            
            // 生成24小时的测试数据（每小时30个数据点，共720个点）
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 2) // 每2分钟一个数据点
                {
                    for (int sampleIndex = 0; sampleIndex < 5; sampleIndex++) // 只生成前5个采样点的数据
                    {
                        // 生成模拟温度数据（基础温度 + 随机波动）
                        float baseTemp = 950.0f + sampleIndex * 10.0f; // 不同采样点有不同的基础温度
                        float timeVariation = Mathf.Sin((hour * 60 + minute) * Mathf.PI / 720) * 20.0f; // 时间相关的变化
                        float randomNoise = UnityEngine.Random.Range(-5.0f, 5.0f); // 随机噪声
                        float tempValue = baseTemp + timeVariation + randomNoise;
                        
                        TempData data = new TempData(
                            selectedPotName,
                            selectedDate,
                            sampleIndex,
                            tempValue,
                            (short)(hour * 30 + minute / 2) // 记录号
                        );
                        
                        cachedData.Add(data);
                    }
                }
            }
            
            Debug.Log($"生成了 {cachedData.Count} 个测试温度数据点");
            
            // 更新图表
            UpdateChart();
        }
        
        // 更新图表
        private void UpdateChart()
        {
            var chart = gameObject.GetComponent<LineChart>();
            if (chart == null)
            {
                Debug.LogError("找不到LineChart组件");
                return;
            }
            
            try
            {
                // 清除现有数据
                chart.ClearData();
                
                // 获取唯一的采样编号
                HashSet<int> uniqueSamples = new HashSet<int>();
                foreach (var data in cachedData)
                {
                    uniqueSamples.Add(data.sampleIndex);
                }
                
                List<int> sortedSamples = new List<int>(uniqueSamples);
                sortedSamples.Sort();
                
                Debug.Log($"图表将显示 {sortedSamples.Count} 个采样点的数据");
                
                // 为每个采样点创建一个系列
                foreach (int sampleIndex in sortedSamples)
                {
                    var serie = chart.AddSerie<Line>($"采样点{sampleIndex + 1}");
                    serie.lineStyle.color = GetColorForSample(sampleIndex);
                    serie.lineStyle.width = 2;
                }
                
                // 获取唯一的记录号并排序（用作X轴）
                HashSet<short> uniqueRecords = new HashSet<short>();
                foreach (var data in cachedData)
                {
                    uniqueRecords.Add(data.recordIndex);
                }
                
                List<short> sortedRecords = new List<short>(uniqueRecords);
                sortedRecords.Sort();
                
                // 添加X轴数据（时间标签）
                foreach (short record in sortedRecords)
                {
                    // 将记录号转换为时间标签
                    int totalMinutes = record * 2; // 假设每个记录代表2分钟
                    int hours = totalMinutes / 60;
                    int minutes = totalMinutes % 60;
                    string timeLabel = (hours < 24) ? $"{hours:D2}:{minutes:D2}" : "";
                    
                    chart.AddXAxisData(timeLabel);
                }
                
                // 添加Y轴数据
                for (int serieIndex = 0; serieIndex < sortedSamples.Count; serieIndex++)
                {
                    int currentSample = sortedSamples[serieIndex];
                    
                    foreach (short record in sortedRecords)
                    {
                        // 查找对应的温度数据
                        var tempData = cachedData.FirstOrDefault(d => 
                            d.sampleIndex == currentSample && d.recordIndex == record);
                        
                        float tempValue = tempData?.tempValue ?? 0f;
                        chart.AddData(serieIndex, tempValue);
                    }
                }
                
                // 配置图表
                ConfigureChart(chart);
                
                Debug.Log($"图表更新完成，显示 {sortedSamples.Count} 个系列，{sortedRecords.Count} 个数据点");
            }
            catch (Exception ex)
            {
                Debug.LogError($"更新图表时出错: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        // 为不同采样点获取不同颜色
        private Color GetColorForSample(int sampleIndex)
        {
            Color[] colors = {
                Color.red,
                Color.blue,
                Color.green,
                Color.yellow,
                Color.magenta,
                Color.cyan,
                new Color(1f, 0.5f, 0f), // 橙色
                new Color(0.5f, 0f, 1f), // 紫色
                new Color(0f, 0.5f, 0.5f), // 青绿色
                new Color(0.5f, 0.5f, 0f)  // 橄榄色
            };
            
            return colors[sampleIndex % colors.Length];
        }
        
        // 配置图表
        private void ConfigureChart(LineChart chart)
        {
            // 配置标题
            var title = chart.EnsureChartComponent<Title>();
            title.text = $"电解槽{selectedPotName}温度历史数据 ({selectedDate})";
            title.show = true;
            
                        // 配置X轴
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.show = true;
            xAxis.type = Axis.AxisType.Category;
            xAxis.axisName.name = "时间";
            xAxis.axisName.labelStyle.position = LabelStyle.Position.End;
            
            // 配置Y轴
            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.show = true;
            yAxis.type = Axis.AxisType.Value;
            yAxis.axisName.name = "温度(°C)";
            yAxis.axisName.labelStyle.position = LabelStyle.Position.End;
            
            // 配置图例
            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;
            legend.location.align = Location.Align.TopRight;
            
            // 配置工具提示
            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;
            
            // 配置数据缩放
            var dataZoom = chart.EnsureChartComponent<DataZoom>();
            dataZoom.enable = true;
            dataZoom.supportInside = true;
            dataZoom.supportSlider = true;
            dataZoom.start = 0;
            dataZoom.end = 100;
        }
        
        // 公共方法：手动触发数据加载
        public void LoadData()
        {
            LoadDataFromAPI();
        }
        
        // 公共方法：设置筛选条件
        public void SetFilter(string potName, string date)
        {
            selectedPotName = potName;
            selectedDate = date;
            
            if (potNameText != null)
                potNameText.text = selectedPotName;
            
            if (dateText != null)
                dateText.text = selectedDate;
        }
    }
}