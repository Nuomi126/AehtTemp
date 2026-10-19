using UnityEngine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Collections;
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
    public class Line_LocalDB : MonoBehaviour
    {
        [Header("数据库设置")]
        [SerializeField] private string serverName = "localhost";
        [SerializeField] private string databaseName = "master";
        [SerializeField] private string userId = "sa";
        [SerializeField] private string password = "337333";
        [SerializeField] private int connectionTimeout = 30;
        
        // 移除原来的 connectionString 公共字段
        private string connectionString;
        
        [Header("筛选设置")]
        public TextMeshProUGUI potNameText; // 电解槽文本
        public TextMeshProUGUI dateText;    // 日期文本
        public Button applyFilterButton;
        
        [Header("查询按钮")]
        public Button queryButton1; // 查询按钮1
        public Button queryButton2; // 查询按钮2
        public Button queryButton3; // 查询按钮3
        
        // 查询类型枚举
        private enum QueryType
        {
            Type1 = 1, // posindex = 4 AND directiontype = 1
            Type2 = 2, // posindex = 1 AND directiontype = 1
            Type3 = 3  // posindex = 3 AND directiontype = 1
        }
        
        private QueryType currentQueryType = QueryType.Type1; // 默认查询类型
        
        [Header("图表设置")]
        //public string chartTitle = "温度数据图表";
        public Color lineColor = Color.blue;
        [SerializeField] private int targetPositionIndex = -1; // 添加目标位置索引
        
        // 筛选条件
        private string selectedPotName = "1001";
        private string selectedDate = "20240421";
        
        // 数据缓存
        private List<TempData> cachedData = new List<TempData>();
        private List<int> sampleIndexes = new List<int>();
        private string[] lagendname;

        // 数据结构
        [System.Serializable]
        public class TempData
        {
            public string potName;
            public string dateData;
            public int positionIndex;
            public float tempValue;
            public short recordIndex; // 添加记录索引字段
            
            public TempData(string pot, string date, int position, float temp)
            {
                potName = pot;
                dateData = date;
                positionIndex = position;
                tempValue = temp;
                recordIndex = 0; // 默认值
            }
        }
        
        void Awake()
        {
            // 初始化连接字符串
            connectionString = $"Server={serverName};Database={databaseName};User ID={userId};Password={password};Connection Timeout={connectionTimeout};";
            
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
            
            // 初始化查询按钮
            InitQueryButtons();
        }
        
        // 初始化查询按钮
        private void InitQueryButtons()
        {
            // 查询按钮1
            if (queryButton1 != null)
            {
                queryButton1.onClick.RemoveAllListeners();
                queryButton1.onClick.AddListener(() => {
                    Debug.Log("查询按钮1被点击");
                    currentQueryType = QueryType.Type1;
                    // 更新按钮视觉状态
                    UpdateButtonVisualState();
                    // 立即执行查询
                    LoadDataFromDatabase();
                });
            }
            
            // 查询按钮2
            if (queryButton2 != null)
            {
                queryButton2.onClick.RemoveAllListeners();
                queryButton2.onClick.AddListener(() => {
                    Debug.Log("查询按钮2被点击");
                    currentQueryType = QueryType.Type2;
                    // 更新按钮视觉状态
                    UpdateButtonVisualState();
                    // 立即执行查询
                    LoadDataFromDatabase();
                });
            }
            
            // 查询按钮3
            if (queryButton3 != null)
            {
                queryButton3.onClick.RemoveAllListeners();
                queryButton3.onClick.AddListener(() => {
                    Debug.Log("查询按钮3被点击");
                    currentQueryType = QueryType.Type3;
                    // 更新按钮视觉状态
                    UpdateButtonVisualState();
                    // 立即执行查询
                    LoadDataFromDatabase();
                });
            }
            
            // 初始化时更新按钮状态
            UpdateButtonVisualState();
        }
        
        // 更新按钮视觉状态
        private void UpdateButtonVisualState()
        {
            // 定义选中和未选中的颜色
            Color selectedColor = new Color(0.2f, 0.6f, 1f); // 蓝色
            Color normalColor = Color.white;
            
            // 更新按钮颜色
            if (queryButton1 != null)
            {
                queryButton1.GetComponent<Image>().color = (currentQueryType == QueryType.Type1) ? selectedColor : normalColor;
            }
            
            if (queryButton2 != null)
            {
                queryButton2.GetComponent<Image>().color = (currentQueryType == QueryType.Type2) ? selectedColor : normalColor;
            }
            
            if (queryButton3 != null)
            {
                queryButton3.GetComponent<Image>().color = (currentQueryType == QueryType.Type3) ? selectedColor : normalColor;
            }
            
            // 可以在这里添加更多视觉反馈，如文本颜色变化等
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
            LoadDataFromDatabase();
        }
        
        // 从数据库加载数据
        private void LoadDataFromDatabase()
        {
            // 检查游戏对象是否激活
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("游戏对象未激活，无法启动协程。请确保对象已激活。");
                return;
            }
            
            // 清除缓存数据
            cachedData.Clear();
            
            // 启动协程获取sampleIndex
            StartCoroutine(FetchSampleIndexes());
        }

        // 从数据库获取sampleIndex的协程
        private IEnumerator FetchSampleIndexes()
        {
            sampleIndexes.Clear();
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Debug.LogError("数据库连接字符串未设置");
                yield break;
            }
            
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //Debug.Log("数据库连接成功，开始获取sampleIndex");
                    
                    // 根据当前查询类型构建查询条件
                    string whereClause = "";
                    switch (currentQueryType)
                    {
                        case QueryType.Type1:
                            whereClause = "WHERE posindex = 4 AND directiontype = 1";
                            break;
                        case QueryType.Type2:
                            whereClause = "WHERE posindex = 1 AND directiontype = 1";
                            break;
                        case QueryType.Type3:
                            whereClause = "WHERE posindex = 3 AND directiontype = 1";
                            break;
                        default:
                            whereClause = "WHERE posindex = 4 AND directiontype = 1";
                            break;
                    }
                    
                    // 查询sampleIndex
                    string query = $@"
                        SELECT sampleindex 
                        FROM mobile260101.cxt.pottempalarmconfig 
                        {whereClause}";
                    
                    //Debug.Log($"执行查询: {query}");
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // 修改这里：使用GetInt16读取smallint类型
                                short smallIntValue = reader.GetInt16(0);
                                int sampleIndex = Convert.ToInt32(smallIntValue);
                                sampleIndexes.Add(sampleIndex);
                                //Debug.Log($"读取到sampleIndex: {sampleIndex}");
                            }
                        }
                    }
                    
                    //Debug.Log($"获取到 {sampleIndexes.Count} 个sampleIndex值");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取sampleIndex错误: {ex.Message}");
                
                // 使用默认sampleIndex值 - 这里可能是限制了只有5个位置的地方
                
                sampleIndexes.Clear();
                for (int i = 1; i <= 56; i++) // 假设我们需要56个索引
                {
                    sampleIndexes.Add(i);
                }
                Debug.Log($"使用默认sampleIndex值，共 {sampleIndexes.Count} 个");
            }
            
            // 获取到sampleIndex后，继续获取温度数据
            StartCoroutine(FetchTempData());
            
            yield return null;
        }
        
        // 从数据库获取温度数据的协程
        private IEnumerator FetchTempData()
        {
            cachedData.Clear();
            
            if (sampleIndexes.Count == 0)
            {
                Debug.LogError("没有有效的sampleIndex");
                yield break;
            }
            
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Debug.Log("数据库连接成功，开始获取温度数据");
                    
                    // 构建动态查询
                    string columns = "";
                    foreach (int index in sampleIndexes)
                    {
                        if (!string.IsNullOrEmpty(columns))
                        {
                            columns += ", ";
                        }
                        // 确保列名格式正确
                        columns += $"Position{index+1}";
                    }
                    
                    // 打印查询的列名，用于调试
                    Debug.Log($"查询的列: recordIndex, {columns}");
                    
                    // 修改查询，添加recordIndex
                    string query = $@"
                        SELECT recordIndex, {columns}
                        FROM mobiletempdata260101.zzh.mobiletempdata1
                        WHERE potName = @PotName AND dateData = @DateData
                        ORDER BY recordIndex ASC";
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // potName可能是smallint类型，尝试转换
                        try {
                            command.Parameters.Add("@PotName", System.Data.SqlDbType.SmallInt).Value = Convert.ToInt16(selectedPotName);
                        } catch {
                            command.Parameters.AddWithValue("@PotName", selectedPotName);
                        }
                        command.Parameters.AddWithValue("@DateData", selectedDate);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // 获取列信息，用于调试
                            var schemaTable = reader.GetSchemaTable();
                            if (schemaTable != null)
                            {
                                Debug.Log("数据库返回的列信息:");
                                foreach (System.Data.DataRow row in schemaTable.Rows)
                                {
                                    //Debug.Log($"列名: {row["ColumnName"]}, 类型: {row["DataType"]}");
                                }
                            }
                            
                            int recordCount = 0;
                            
                            while (reader.Read())
                            {
                                // 读取recordIndex (smallint类型)
                                short recordIndex = reader.GetInt16(0);
                                
                                for (int i = 0; i < sampleIndexes.Count; i++)
                                {
                                    float tempValue = 0;
                                    
                                    // 列索引需要+1，因为第一列是recordIndex
                                    if (!reader.IsDBNull(i + 1))
                                    {
                                        try {
                                            // 尝试直接获取值并转换为float
                                            object rawValue = reader.GetValue(i + 1);
                                           // Debug.Log($"位置{sampleIndexes[i]}的原始值: {rawValue}, 类型: {rawValue.GetType().Name}");
                                            
                                            // 根据类型进行转换
                                            if (rawValue is float)
                                                tempValue = (float)rawValue;
                                            else if (rawValue is double)
                                                tempValue = (float)(double)rawValue;
                                            else if (rawValue is decimal)
                                                tempValue = (float)(decimal)rawValue;
                                            else if (rawValue is int)
                                                tempValue = (int)rawValue;
                                            else if (rawValue is short)
                                                tempValue = (short)rawValue;
                                            else
                                                tempValue = Convert.ToSingle(rawValue);
                                                
                                            //Debug.Log($"转换后的温度值: {tempValue}");
                                        } catch (Exception ex) {
                                            //Debug.LogWarning($"转换温度值失败: {ex.Message}，使用默认值0");
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log($"位置{sampleIndexes[i]}的值为NULL");
                                    }
                                    
                                    TempData data = new TempData(
                                        selectedPotName,
                                        selectedDate,
                                        sampleIndexes[i],
                                        tempValue
                                    );
                                    
                                    // 设置recordIndex
                                    data.recordIndex = recordIndex;
                                    
                                    cachedData.Add(data);
                                }
                                
                                recordCount++;
                            }
                            
                            Debug.Log($"从数据库读取了 {recordCount} 条记录，共 {cachedData.Count} 个温度数据点");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取温度数据错误: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                
                
            }
            
            // 更新图表
            UpdateChart();
            
            yield return null;
        }
        
        // 生成测试数据（当数据库连接失败时使用）
        
        
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
                
                // 确保有正确的系列
                if (chart.series.Count == 0)
                {
                    var initialSerie = chart.AddSerie<Line>("温度数据");
                    initialSerie.lineStyle.color = lineColor;
                }
                
                // 获取唯一的记录索引
                HashSet<short> uniqueRecords = new HashSet<short>();
                
                foreach (var data in cachedData)
                {
                    uniqueRecords.Add(data.recordIndex);
                }
                
                // 将记录索引转换为时间标签
                List<short> sortedRecords = new List<short>(uniqueRecords);
                sortedRecords.Sort();
                
                // 添加X轴数据（时间标签）
                
                // 配置X轴显示
                var xAxis = chart.EnsureChartComponent<XAxis>();
                
                
                // 固定写死X轴标签
                string[] timeLabels = new string[721]; // 0-720的数组
                for (int i = 0; i < timeLabels.Length; i++)
                {
                    timeLabels[i] = ""; // 默认所有标签为空
                }
                
                // 设置特定位置的标签
                timeLabels[0] = "00:00";
                timeLabels[120] = "04:00";
                timeLabels[240] = "08:00";
                timeLabels[360] = "12:00";
                timeLabels[480] = "16:00";
                timeLabels[600] = "20:00";
                timeLabels[720] = "24:00";
                
                // 添加所有标签到X轴
                foreach (string label in timeLabels)
                {
                    chart.AddXAxisData(label);
                    //Debug.Log($"添加X轴标签: {label}");
                    xAxis.SetAllDirty();
                    chart.RefreshChart();
                }
                xAxis.type = Axis.AxisType.Category;  // 设置为类别轴
                xAxis.splitNumber = 6; // 7个标签点：00:00, 04:00, 08:00, 12:00, 16:00, 20:00, 24:00
                xAxis.axisLabel.interval = 0; // 显示所有非空标签
                xAxis.axisLabel.textStyle.fontSize = 14; // 增大字体大小以便更清晰
                xAxis.axisLabel.textStyle.color = Color.red; // 设置X轴数字为红色
                xAxis.axisLine.show = true; // 显示坐标轴线
                xAxis.axisTick.show = true; // 显示刻度
                xAxis.boundaryGap = true; // 设置边界间隙
                xAxis.SetAllDirty();
                chart.RefreshChart();
                
                // 配置Y轴
                var yAxis = chart.EnsureChartComponent<YAxis>();
                yAxis.type = Axis.AxisType.Value;
                yAxis.min = 260; // 设置最小值
                yAxis.max = 320; // 设置最大值
                yAxis.position = Axis.AxisPosition.Left;
                
                // 刷新图表，应用第一个Y轴设置
                chart.RefreshChart();
                
                // 添加第二个Y轴组件
                chart.AddChartComponent<YAxis>(); // 添加新的Y轴组件
                // 获取新添加的Y轴组件
                var yAxis1 = chart.GetChartComponent<YAxis>(1);

                yAxis1.type = Axis.AxisType.Value;
                yAxis1.minMaxType = Axis.AxisMinMaxType.Custom;
                yAxis1.min = 260; // 设置最小值
                yAxis1.max = 320; // 设置最大值
                yAxis1.position = Axis.AxisPosition.Right;
                yAxis1.SetAllDirty();
                
                // 再次刷新图表，应用所有设置
                chart.RefreshChart();
                
                // 确定要显示的位置点
                int targetPosition;
                
                // 如果设置了目标位置索引且该位置存在于采样位置列表中
                if (targetPositionIndex >= 0)
                {
                    targetPosition = targetPositionIndex;
                    Debug.Log($"使用指定的位置索引: {targetPosition}");
                }
                else if (sampleIndexes.Count > 0)
                {
                    // 使用第一个采样位置
                    targetPosition = sampleIndexes[0];
                    Debug.Log($"使用默认的第一个位置索引: {targetPosition}");
                }
                else
                {
                    Debug.LogWarning("没有可用的位置数据");
                    return;
                }
                
                // 更新图表标题
                //chart.EnsureChartComponent<Title>().text = $"{selectedPotName}电解槽 {selectedDate}日温度数据";
                
                // 添加图例组件
                var legend = chart.EnsureChartComponent<Legend>();
                legend.show = true;
                legend.itemWidth = 25;
                legend.itemHeight = 12;
                legend.location.align = Location.Align.TopLeft;
                legend.location.top = 200;  // 调整图例位置，避免与标题重叠
                legend.location.left = 180;
                
                
                lagendname = new string[] { "A面阴极A1-1", "A面阴极A1-2", "B面阴极A1-1", "B面阴极A1-2" };
                foreach (string label in lagendname)
                {
                    legend.AddData(label);
                    legend.SetAllDirty();
                }
                
                // 清除现有系列
                chart.RemoveAllSerie();
                
                // 定义四个不同的颜色
                Color[] serieColors = new Color[] {
                    new Color(0.2f, 0.6f, 1f),    // 蓝色
                    new Color(1f, 0.4f, 0.4f),    // 红色
                    new Color(0.4f, 0.8f, 0.4f),  // 绿色
                    new Color(0.8f, 0.8f, 0.2f)   // 黄色
                };

                // //将legend颜色设置为serieColors中的颜色
                // legend.colors = new List<Color> { Color.white, Color.white, Color.white, Color.white };
                
                // for (int i = 0; i < 4; i++)
                // {
                    
                //     legend.colors[i] = serieColors[i];
                //     legend.SetAllDirty();
                // }

                // 选择四个位置索引来显示
                List<int> displayPositions = new List<int>();
                
                // 如果指定了目标位置，将其作为第一个位置
                if (targetPositionIndex >= 0)
                {
                    displayPositions.Add(targetPositionIndex);
                }
                
                // 从采样索引中选择剩余的位置（最多4个）
                foreach (int pos in sampleIndexes)
                {
                    if (!displayPositions.Contains(pos))
                    {
                        displayPositions.Add(pos);
                        if (displayPositions.Count >= 4)
                            break;
                    }
                }
                
                // 如果位置不足4个，使用默认位置填充
                if (displayPositions.Count < 4 && sampleIndexes.Count > 0)
                {
                    for (int i = 0; i < sampleIndexes.Count && displayPositions.Count < 4; i++)
                    {
                        if (!displayPositions.Contains(sampleIndexes[i]))
                        {
                            displayPositions.Add(sampleIndexes[i]);
                        }
                    }
                }
                
                Debug.Log($"将显示 {displayPositions.Count} 个位置的温度曲线");
                
                // 为每个位置创建一个系列并添加数据
                for (int i = 0; i < displayPositions.Count; i++)
                {
                    int position = displayPositions[i];
                    string serieName;
                    //position等于1，2，3，4时，serieName分别为A面阴极A1-1，A面阴极A1-2，B面阴极A1-1，B面阴极A1-2
                    if (position == 1)
                        serieName = lagendname[0];
                    else if (position == 2)
                        serieName = lagendname[1];
                    else if (position == 4)
                        serieName = lagendname[2];
                    else if (position == 5)
                        serieName = lagendname[3];
                    else
                        serieName = $"位置{position}"; // 添加默认值，处理其他位置情况
                
                // 添加系列（统一蓝色折线 + 蓝色渐变填充）
                var serie = chart.AddSerie<Line>(serieName);
                Color blue = new Color(0f, 0.47f, 1f);
                serie.lineStyle.color = blue;
                serie.itemStyle.color = blue; // 设置数据点颜色
                serie.symbol.show = false;  // 不显示数据点标记，使曲线更清晰
                serie.lineStyle.width = 2;  // 设置线宽
                serie.large = false;

                    // 添加蓝色渐变填充区域
                    var area = serie.EnsureComponent<AreaStyle>();
                    area.show = true;
                    area.color = new Color(0f, 0.47f, 1f, 0.25f);
                    area.toColor = new Color(0f, 0.47f, 1f, 0f);
                
                    // 获取该位置的所有温度数据
                    var positionData = cachedData.FindAll(d => d.positionIndex == position);
                    positionData.Sort((a, b) => a.recordIndex.CompareTo(b.recordIndex));
                    
                    Debug.Log($"位置{position}的数据点数量: {positionData.Count}");
                    
                    // 添加数据点
                    foreach (var data in positionData)
                    {
                        var serieData = chart.AddData(i, data.tempValue);
                        // 保持为蓝色（符号颜色使用Symbol配置）
                        serie.itemStyle.color = blue;
                        // 只在特定记录索引处添加标签和符号
                        if (data.recordIndex == 0 || data.recordIndex == 120 || data.recordIndex == 240 || 
                            data.recordIndex == 360 || data.recordIndex == 480 || 
                            data.recordIndex == 600 || data.recordIndex == 720)
                        {
                            // 为数据点添加标签组件
                            var labelStyle = serieData.EnsureComponent<LabelStyle>();
                            labelStyle.show = true;
                            labelStyle.position = LabelStyle.Position.Top;
                            labelStyle.offset = new Vector3(0, 20, 0);
                            labelStyle.formatter = "{c}"; // 显示数值
                            labelStyle.numericFormatter = "F1"; // 保留一位小数
                            //设置颜色为红色
                            labelStyle.textStyle.color = Color.red;
                            //设置字体加粗
                            labelStyle.textStyle.fontStyle = FontStyle.Bold;
                            
                            
                            // 为数据点添加符号组件 - 使用SerieSymbol而不是SymbolStyle
                            var symbol = serieData.EnsureComponent<SerieSymbol>();
                            symbol.show = true;
                            symbol.type = SymbolType.Diamond; // 可以设置为其他形状
                            symbol.size = 5; // 设置符号大小
                            //symbol.color = Color.red; // 设置符号颜色;
                            
                        }
                    }
                }
                
                // 刷新图表
                chart.RefreshChart();
            }
            catch (Exception e)
            {
                Debug.LogError($"更新图表时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 设置目标位置索引的公共方法
        public void SetTargetPosition(int positionIndex)
        {
            targetPositionIndex = positionIndex;
            //Debug.Log($"设置图表 {gameObject.name} 的目标位置为: {positionIndex}");
            
            // 如果已有数据，立即更新图表
            if (cachedData.Count > 0)
            {
                UpdateChart();
            }
        }
        
        // 手动加载数据的方法（可通过按钮调用）
        public void LoadData()
        {
            LoadDataFromDatabase();
        }
        
        // 获取采样位置索引列表
        public List<int> GetSampleIndexes()
        {
            // 确保返回的列表不为空
            if (sampleIndexes.Count == 0)
            {
                Debug.LogWarning("sampleIndexes 为空");
                //return new List<int> { 1, 2, 3, 4, 5 };
            }
            
            Debug.Log($"返回 {sampleIndexes.Count} 个采样位置索引");
            return new List<int>(sampleIndexes);
        }
        
        // 设置查询类型的公共方法（供外部调用）
        public void SetQueryType(int queryTypeValue)
        {
            if (queryTypeValue >= 1 && queryTypeValue <= 3)
            {
                currentQueryType = (QueryType)queryTypeValue;
                Debug.Log($"设置查询类型为: {currentQueryType}");
                
                // 更新按钮视觉状态
                UpdateButtonVisualState();
            }
            else
            {
                Debug.LogError($"无效的查询类型值: {queryTypeValue}");
            }
        }
    }
}