using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;

namespace XCharts.Example
{
    /// <summary>
    /// 传感曲线专用管理器。
    /// 
    /// 设计目标：
    /// - 不影响原来的历史曲线 MultiSensorTemperatureChartManager。
    /// - 专门用于 chuanganquxian 场景中 frameType = 8 的传感曲线。
    /// - 每张图包含 4 根曲线。
    /// - 如果接口返回多条记录，则按记录数量生成多张图。
    /// 
    /// 协议约定（按当前已知规则实现）：
    /// - Base64 响应
    /// - 帧头 AA 55
    /// - 2 字节企业标识
    /// - 2 字节帧类型
    /// - 2 字节槽号
    /// - 4 字节数据长度
    /// - 数据体
    /// - 帧尾 A5 5A
    /// - 每条记录 480 字节
    /// - 240 个 ushort 值
    /// - 每 4 个点拆成一张图中的 4 根曲线，每根曲线 60 个点
    /// </summary>
    [DisallowMultipleComponent]
    public class ChuanganSensorCurveChartManager : MonoBehaviour
    {
        [Header("API设置")]
        [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        [SerializeField] private string companyId = "1033";
        [SerializeField] private int frameType = 8;
        [SerializeField] private int connectionTimeout = 30;

        [Header("查询条件")]
        public TextMeshProUGUI potNameText;
        public TextMeshProUGUI dateText;
        public Button applyFilterButton;

        [Header("分类按钮")]
        public Button categoryButton阴极A;
        public Button categoryButton阴极B;
        public Button categoryButton钢窗A;
        public Button categoryButton钢窗B;
        public Button categoryButton端部;
        public Button categoryButton底部;

        [Header("图表布局")]
        public GameObject chartPrefab;
        public Transform chartsContainer;
        public ScrollRect scrollRect;
        public Vector2 chartSize = new Vector2(520, 260);
        public Vector2 spacing = new Vector2(18, 18);

        [Header("UI提示")]
        public TextMeshProUGUI loadingText;

        private string selectedPotName;
        private string selectedDate;
        private string selectedCategory = "阴极A";

        private readonly List<LineChart> chartInstances = new List<LineChart>();
        private readonly List<ChartBlockData> parsedCharts = new List<ChartBlockData>();

        private const int RecordByteSize = 480;
        private const int ValuesPerRecord = RecordByteSize / 2;
        private const int SeriesPerChart = 4;
        private const int ValuesPerSeries = ValuesPerRecord / SeriesPerChart;

        [Serializable]
        private class ChartBlockData
        {
            public string chartTitle;
            public List<SeriesBlock> seriesList = new List<SeriesBlock>(SeriesPerChart);
        }

        [Serializable]
        private class SeriesBlock
        {
            public string seriesName;
            public List<int> values = new List<int>(ValuesPerSeries);
        }

        [Serializable]
        private class RecordData
        {
            public int recordIndex;
            public string chartTitle;
            public List<SeriesBlock> seriesList = new List<SeriesBlock>(SeriesPerChart);
        }

        private void Awake()
        {
            AutoBindSceneReferences();
            InitUI();
            InitCategoryButtons();
        }

        private void Start()
        {
            if (applyFilterButton != null)
            {
                applyFilterButton.onClick.RemoveAllListeners();
                applyFilterButton.onClick.AddListener(OnApplyFilter);
            }

            RefreshData();
        }

        public void OnApplyFilter() => RefreshData();

        public void RefreshData()
        {
            selectedPotName = ResolveSelectedSlotNumber();
            selectedDate = GetValidatedDate();
            StartCoroutine(FetchAndRender());
        }

        private void AutoBindSceneReferences()
        {
            if (chartPrefab == null)
            {
                var chart = FindObjectOfType<LineChart>(true);
                if (chart != null) chartPrefab = chart.gameObject;
            }

            if (chartsContainer == null)
            {
                var scrollViews = FindObjectsOfType<ScrollRect>(true);
                foreach (var scroll in scrollViews)
                {
                    if (scroll != null && scroll.content != null)
                    {
                        chartsContainer = scroll.content;
                        scrollRect = scroll;
                        break;
                    }
                }
            }

            if (scrollRect == null && chartsContainer != null)
            {
                scrollRect = chartsContainer.GetComponentInParent<ScrollRect>(true);
            }

            if (potNameText == null) potNameText = FindTextByName("槽号", "slot", "pot", "potname", "槽号选择");
            if (dateText == null) dateText = FindTextByName("日期", "date", "riqi", "dateText");
            if (loadingText == null) loadingText = FindTextByName("loading", "loadingText", "正在读取", "加载");
            if (applyFilterButton == null) applyFilterButton = FindButtonByName("筛选", "应用", "查询", "apply", "filter");
            if (categoryButton阴极A == null) categoryButton阴极A = FindButtonByName("阴极A", "A面阴极");
            if (categoryButton阴极B == null) categoryButton阴极B = FindButtonByName("阴极B", "B面阴极");
            if (categoryButton钢窗A == null) categoryButton钢窗A = FindButtonByName("钢窗A", "A面钢窗");
            if (categoryButton钢窗B == null) categoryButton钢窗B = FindButtonByName("钢窗B", "B面钢窗");
            if (categoryButton端部 == null) categoryButton端部 = FindButtonByName("端部");
            if (categoryButton底部 == null) categoryButton底部 = FindButtonByName("底部");
        }

        private TextMeshProUGUI FindTextByName(params string[] keywords)
        {
            var texts = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text == null) continue;
                var name = text.name.ToLowerInvariant();
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && name.Contains(keyword.ToLowerInvariant())) return text;
                }
            }
            return null;
        }

        private Button FindButtonByName(params string[] keywords)
        {
            var buttons = FindObjectsOfType<Button>(true);
            foreach (var button in buttons)
            {
                if (button == null) continue;
                var name = button.name.ToLowerInvariant();
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && name.Contains(keyword.ToLowerInvariant())) return button;
                }
            }
            return null;
        }

        private void InitUI()
        {
            selectedPotName = ResolveSelectedSlotNumber();
            selectedDate = GetValidatedDate();

            if (potNameText != null && string.IsNullOrWhiteSpace(potNameText.text)) potNameText.text = selectedPotName;
            if (dateText != null && string.IsNullOrWhiteSpace(dateText.text)) dateText.text = selectedDate;
        }

        private void InitCategoryButtons()
        {
            HookCategoryButton(categoryButton阴极A, "阴极A");
            HookCategoryButton(categoryButton阴极B, "阴极B");
            HookCategoryButton(categoryButton钢窗A, "钢窗A");
            HookCategoryButton(categoryButton钢窗B, "钢窗B");
            HookCategoryButton(categoryButton端部, "端部");
            HookCategoryButton(categoryButton底部, "底部");
        }

        private void HookCategoryButton(Button btn, string category)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                selectedCategory = category;
                RefreshData();
            });
        }

        private IEnumerator FetchAndRender()
        {
            ShowLoading(true, "正在读取传感曲线...");
            ClearCharts();
            parsedCharts.Clear();

            string requestParam = $"{companyId}-{frameType}-{selectedPotName}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes($"\"{requestParam}\"");

            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = connectionTimeout;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"传感曲线请求失败: {request.error}");
                    ShowLoading(false);
                    yield break;
                }

                if (!TryParseResponse(request.downloadHandler.text, out var records))
                {
                    Debug.LogError("传感曲线响应解析失败");
                    ShowLoading(false);
                    yield break;
                }

                BuildChartsFromRecords(records);
                yield return StartCoroutine(CreateChartsInBatches());
            }
        }

        private bool TryParseResponse(string responseText, out List<RecordData> records)
        {
            records = new List<RecordData>();
            if (string.IsNullOrWhiteSpace(responseText)) return false;

            string base64Data = responseText.Trim();
            if (base64Data.StartsWith("\"") && base64Data.EndsWith("\""))
            {
                base64Data = base64Data.Substring(1, base64Data.Length - 2);
            }

            while (base64Data.Length % 4 != 0) base64Data += "=";

            byte[] frameData;
            try
            {
                frameData = Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Base64 解码失败: {ex.Message}");
                return false;
            }

            if (frameData.Length < 14) return false;

            int offset = 0;
            if (frameData[offset] != 170 || frameData[offset + 1] != 85) return false;
            offset += 2;

            int enterpriseId = ReadUInt16(frameData, offset); offset += 2;
            int parsedFrameType = ReadUInt16(frameData, offset); offset += 2;
            int slot = ReadUInt16(frameData, offset); offset += 2;
            int dataLength = ReadInt32(frameData, offset); offset += 4;

            if (parsedFrameType != frameType)
            {
                Debug.LogWarning($"帧类型不一致，返回={parsedFrameType}，请求={frameType}");
            }

            if (offset + dataLength + 2 > frameData.Length) return false;
            if (frameData[offset + dataLength] != 165 || frameData[offset + dataLength + 1] != 90) return false;

            int recordCount = dataLength / RecordByteSize;
            if (recordCount <= 0) return false;

            for (int i = 0; i < recordCount; i++)
            {
                var record = ParseRecordPayload(frameData, offset + i * RecordByteSize, i);
                if (record != null) records.Add(record);
            }

            Debug.Log($"帧解析成功: 企业={enterpriseId}, 槽号={slot}, 记录数={records.Count}");
            return records.Count > 0;
        }

        private RecordData ParseRecordPayload(byte[] data, int startOffset, int recordIndex)
        {
            int endOffset = startOffset + RecordByteSize;
            if (endOffset > data.Length) return null;

            var values = new List<int>(ValuesPerRecord);
            for (int i = 0; i < ValuesPerRecord; i++)
            {
                values.Add(ReadUInt16(data, startOffset + i * 2));
            }

            var record = new RecordData
            {
                recordIndex = recordIndex,
                chartTitle = $"{selectedCategory}-记录{recordIndex + 1}"
            };

            for (int s = 0; s < SeriesPerChart; s++)
            {
                var series = new SeriesBlock { seriesName = GetSeriesName(s) };
                for (int i = 0; i < ValuesPerSeries; i++)
                {
                    series.values.Add(values[s * ValuesPerSeries + i]);
                }
                record.seriesList.Add(series);
            }

            return record;
        }

        private void BuildChartsFromRecords(List<RecordData> records)
        {
            foreach (var record in records)
            {
                var data = new ChartBlockData { chartTitle = record.chartTitle };
                data.seriesList.AddRange(record.seriesList);
                parsedCharts.Add(data);
            }
        }

        private IEnumerator CreateChartsInBatches()
        {
            if (chartPrefab == null || chartsContainer == null)
            {
                ShowLoading(false);
                yield break;
            }

            if (chartsContainer is RectTransform rt)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(0, parsedCharts.Count * (chartSize.y + spacing.y)));
            }

            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }

            for (int i = 0; i < parsedCharts.Count; i++)
            {
                CreateSingleChart(parsedCharts[i], i);
                if (i % 3 == 0) yield return null;
            }

            ShowLoading(false);
        }

        private void CreateSingleChart(ChartBlockData data, int index)
        {
            GameObject chartObj = Instantiate(chartPrefab, chartsContainer);
            chartObj.name = data.chartTitle;

            if (chartObj.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = new Vector2(0, -index * (chartSize.y + spacing.y));
                rect.sizeDelta = chartSize;
            }

            LineChart chart = chartObj.GetComponent<LineChart>();
            if (chart == null) return;

            ConfigureChart(chart, data);
            chartInstances.Add(chart);
        }

        private void ConfigureChart(LineChart chart, ChartBlockData data)
        {
            chart.ClearData();
            chart.RemoveAllSerie();

            chart.theme.enableCustomTheme = true;
            chart.theme.transparentBackground = false;
            chart.theme.customBackgroundColor = new Color32(55, 59, 71, 255);

            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.backgroundColor = new Color32(40, 47, 57, 255);
            grid.showBorder = true;
            grid.borderWidth = 2f;
            grid.borderColor = Color.white;

            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            title.text = data.chartTitle;
            title.labelStyle.textStyle.color = Color.white;
            title.location.align = Location.Align.TopLeft;
            title.location.left = 0.03f;
            title.location.top = 0.03f;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Category;
            xAxis.axisLabel.textStyle.color = Color.white;
            xAxis.axisLine.show = true;
            xAxis.axisTick.show = true;
            xAxis.boundaryGap = true;
            xAxis.splitNumber = 5;

            for (int i = 0; i < ValuesPerSeries; i++)
            {
                chart.AddXAxisData(i.ToString());
            }

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.position = Axis.AxisPosition.Left;
            yAxis.axisLabel.textStyle.color = Color.white;
            yAxis.splitLine.show = false;

            var allValues = data.seriesList.SelectMany(s => s.values).ToList();
            float min = Mathf.Max(0f, (float)allValues.Min());
            float max = (float)allValues.Max();
            float padding = Mathf.Max(2f, (max - min) * 0.1f);
            yAxis.min = Mathf.Max(0f, min - padding);
            float axisMin = (float)yAxis.min;
            yAxis.max = Mathf.Max(axisMin + 10f, max + padding);

            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.type = Tooltip.Type.Line;

            Color[] colors =
            {
                new Color32(160, 96, 255, 255),
                new Color32(84, 183, 98, 255),
                new Color32(240, 198, 84, 255),
                new Color32(233, 76, 126, 255)
            };

            for (int s = 0; s < data.seriesList.Count; s++)
            {
                var series = data.seriesList[s];
                var serie = chart.AddSerie<Line>(series.seriesName);
                serie.lineStyle.color = colors[s % colors.Length];
                serie.lineStyle.width = 2f;
                serie.symbol.show = true;
                serie.symbol.size = 4;
                serie.large = false;

                for (int i = 0; i < series.values.Count; i++)
                {
                    var point = chart.AddData(s, series.values[i]);
                    if (i == 0 || i == series.values.Count - 1)
                    {
                        var label = point.EnsureComponent<LabelStyle>();
                        label.show = true;
                        label.position = LabelStyle.Position.Top;
                        label.formatter = "{c}";
                        label.textStyle.color = Color.white;
                        label.textStyle.autoColor = false;
                    }
                }
            }

            chart.RefreshChart();
        }

        private void ClearCharts()
        {
            foreach (var chart in chartInstances)
            {
                if (chart != null) Destroy(chart.gameObject);
            }
            chartInstances.Clear();

            if (chartsContainer == null) return;
            for (int i = chartsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(chartsContainer.GetChild(i).gameObject);
            }
        }

        private string ResolveSelectedSlotNumber()
        {
            if (potNameText != null)
            {
                var txt = (potNameText.text ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(txt) && Regex.IsMatch(txt, @"^\d+$")) return txt;
            }
            return "1001";
        }

        private string GetValidatedDate()
        {
            var txt = dateText != null ? (dateText.text ?? string.Empty).Trim() : string.Empty;
            if (!string.IsNullOrEmpty(txt) && Regex.IsMatch(txt, @"^\d{8}$")) return txt;
            return DateTime.Now.ToString("yyyyMMdd");
        }

        private string GetSeriesName(int seriesIndex)
        {
            switch (selectedCategory)
            {
                case "阴极A": return $"A曲线{seriesIndex + 1}";
                case "阴极B": return $"B曲线{seriesIndex + 1}";
                case "钢窗A": return $"钢窗A曲线{seriesIndex + 1}";
                case "钢窗B": return $"钢窗B曲线{seriesIndex + 1}";
                case "端部": return $"端部曲线{seriesIndex + 1}";
                case "底部": return $"底部曲线{seriesIndex + 1}";
                default: return $"曲线{seriesIndex + 1}";
            }
        }

        private void ShowLoading(bool show, string text = null)
        {
            if (loadingText == null) return;
            loadingText.gameObject.SetActive(show);
            if (show && !string.IsNullOrWhiteSpace(text)) loadingText.text = text;
        }

        private int ReadUInt16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];
        private int ReadInt32(byte[] data, int offset) => (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }
}
