using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using XCharts.Runtime.UI;
using UnityEngine.TextCore.LowLevel; // GlyphRenderMode

/// <summary>
/// 单槽详情面板管理器
/// 使用 UITable 显示 Frame 72 数据
/// </summary>
public class SlotDetailPanelManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject listPanel;      // lishitongji（列表面板）
    public GameObject detailPanel;    // panelslotdetail（详情面板）

    [Header("返回按钮")]
    public Button btnBack;            // back_b

    [Header("顶部信息文本")]
    public TMP_Text textWorkArea;     // Text (TMP) - 显示工区
    public TMP_Text textSlotNumber;   // Text (TMP) (1) - 显示槽号
    public TMP_Text textDateRange;    // Text (TMP) (2) - 统计周期

    [Header("数据表格")]
    public UITable detailTable;       // UITable_SlotDetail

    [Header("字体设置")]
    public TMP_FontAsset chineseFont; // 支持中文的字体（如思源黑体、方正书宋）

    [Header("API设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    [SerializeField] private string companyIndex = "1033";

    // 缓存当前查询的槽号和日期
    private int currentSlotNumber;
    private string currentDateRange;

    private void Start()
    {
        // 初始化：隐藏详情面板，显示列表
        SetPanelVisibility(detailPanel, false);
        SetPanelVisibility(listPanel, true);

        // 绑定返回按钮点击事件
        if (btnBack != null)
        {
            btnBack.onClick.AddListener(OnBackButtonClicked);
            Debug.Log("[SlotDetailPanel] ✓ 返回按钮已绑定");
        }
        else
        {
            Debug.LogError("[SlotDetailPanel] ✗ 返回按钮未配置！");
        }

        // 初始化表格
        InitDetailTable();
    }

    /// <summary>
    /// 初始化详情表格（设置4列，配置字体样式）
    /// </summary>
    private void InitDetailTable()
    {
        if (detailTable == null)
        {
            Debug.LogError("[SlotDetailPanel] ✗ UITable 未配置！");
            return;
        }

        // 字体回退策略：强制使用动态字体（与 StatisticsReportManager 一致）
        if (chineseFont == null || chineseFont.name.Contains("Alibaba") || chineseFont.name.Contains("Liberation"))
        {
            Debug.Log("[SlotDetailPanel] 检测到静态/缺失字体，创建动态字体...");
            chineseFont = CreateDynamicFont();
            
            // 如果动态字体创建失败，尝试从 StatisticsReportManager 获取
            if (chineseFont == null)
            {
                // 这里不能用 StatisticsReportManager.Instance：
                // Instance 在找不到现存对象时会动态创建一个 StatisticsReportManager，
                // 导致在“槽况分类”等非统计报表场景也出现 tongjibaobiao 的按钮/日志。
                // 只做一次查找，找不到就算了。
                var srm = FindObjectOfType<StatisticsReportManager>();
                if (srm != null && srm.tableFont != null)
                {
                    chineseFont = srm.tableFont;
                    Debug.Log($"[SlotDetailPanel] 从 StatisticsReportManager 获取字体: {chineseFont.name}");
                }
            }
        }

        detailTable.ClearData();

        // 隐藏表头和行号
        if (detailTable.columnHeaderStyle != null)
            detailTable.columnHeaderStyle.show = false;
        
        if (detailTable.rowHeaderStyle != null)
            detailTable.rowHeaderStyle.show = false;

        // 设置4列
        detailTable.SetColumns(new List<string> { "", "", "", "" });

        // 🔥 关键：设置表格默认字体样式（这样新生成的单元格会自动使用该字体）
        if (chineseFont != null)
        {
            // 设置列样式的默认字体
            if (detailTable.columnStyle != null && detailTable.columnStyle.defaultLabelStyle != null)
            {
#if dUI_TextMeshPro
                detailTable.columnStyle.defaultLabelStyle.textStyle.tmpFont = chineseFont;
                detailTable.columnStyle.defaultLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                detailTable.columnStyle.defaultLabelStyle.textStyle.font = chineseFont;
#endif
                detailTable.columnStyle.defaultLabelStyle.textStyle.fontSize = 28;
                Debug.Log($"[SlotDetailPanel] ✓ 已设置列样式字体: {chineseFont.name}");
            }

            // 为每一列单独设置字体
            for (int i = 0; i < detailTable.columns.Count && i < 4; i++)
            {
                var column = detailTable.columns[i];
                if (column != null && column.dataLabelStyle != null)
                {
                    column.dataLabelStyle.show = true;
#if dUI_TextMeshPro
                    column.dataLabelStyle.textStyle.tmpFont = chineseFont;
                    column.dataLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                    column.dataLabelStyle.textStyle.font = chineseFont;
#endif
                    column.dataLabelStyle.textStyle.fontSize = 28;
                    column.dataLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        Debug.Log("[SlotDetailPanel] ✓ 表格初始化完成（4列，无表头，无行号，已设置字体）");
    }

    /// <summary>
    /// 更新表格字体为中文字体（填充数据后调用）
    /// </summary>
    private void UpdateTableFont()
    {
        if (detailTable == null || chineseFont == null) return;

        // 更新所有现有的文本组件
        var texts = detailTable.GetComponentsInChildren<TextMeshProUGUI>(true);
        int updated = 0;
        foreach (var t in texts)
        {
            if (t.font != chineseFont)
            {
                t.font = chineseFont;
                updated++;
            }
        }

        // 强制刷新表格
        detailTable.SetAllDirty();
        detailTable.RefreshGraph();

        Debug.Log($"[SlotDetailPanel] ✓ 已更新 {updated}/{texts.Length} 个文本组件的字体");
    }

    /// <summary>
    /// 显示槽详情（由表格点击调用）
    /// </summary>
    public void ShowSlotDetail(StatisticsReportManager.ReportData slotData, string workAreaName, string dateRange)
    {
        Debug.Log($"[SlotDetailPanel] 显示槽 {slotData.caoh} 的详情");

        currentSlotNumber = slotData.caoh;
        currentDateRange = dateRange;

        // ===== 填充顶部信息 =====
        if (textWorkArea != null)
            textWorkArea.text = "工区：" + workAreaName;

        if (textSlotNumber != null)
            textSlotNumber.text = "槽号：" + slotData.caoh.ToString();

        if (textDateRange != null)
            textDateRange.text = "统计周期：" + dateRange;

        // ===== 切换面板 =====
        SetPanelVisibility(listPanel, false);
        SetPanelVisibility(detailPanel, true);

        // ===== 调用API获取Frame 72数据 =====
        string startDate = dateRange.Replace(".", "");
        string endDate = startDate;

        // 如果dateRange包含"-"，解析起止日期
        if (dateRange.Contains("-"))
        {
            string[] parts = dateRange.Split('-');
            if (parts.Length == 2)
            {
                startDate = parts[0].Replace(".", "").Trim();
                endDate = parts[1].Replace(".", "").Trim();
            }
        }

        string requestData = $"{companyIndex}-72-{slotData.caoh}-{startDate}-{endDate}";
        StartCoroutine(FetchFrame72Data(slotData.caoh, requestData));
    }

    /// <summary>
    /// 调用API获取Frame 72数据
    /// </summary>
    private IEnumerator FetchFrame72Data(int slotNumber, string requestData)
    {
        string jsonData = $"\"{requestData}\"";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        Debug.Log($"[SlotDetailPanel] 请求Frame 72: {requestData}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                byte[] decodedData = DecodeBase64Payload(responseText);

                if (decodedData != null)
                {
                    Debug.Log($"[SlotDetailPanel] Base64解码成功，真实字节数: {decodedData.Length}");
                    
                    // 尝试在数据中查找Frame72数据开头
                    int offset = 0;
                    Frame72SlotData frame72 = null;
                    
                    while (offset + 14 <= decodedData.Length)
                    {
                        if (decodedData[offset] != 170 || decodedData[offset + 1] != 85)
                        {
                            offset++;
                            continue;
                        }
                        
                        int frameStart = offset;
                        offset += 2; // 跳过 AA 55
                        
                        ushort companyId = ReadUInt16BE(decodedData, ref offset);
                        ushort frameType = ReadUInt16BE(decodedData, ref offset);
                        ushort slot = ReadUInt16BE(decodedData, ref offset);
                        uint dataLength = ReadUInt32BE(decodedData, ref offset);
                        
                        if (offset + dataLength + 2 > decodedData.Length) break;
                        
                        if (frameType == 72)
                        {
                            // 实际数据长度至少有134字节
                            if (dataLength >= 134)
                            {
                                int recordOffset = offset;
                                frame72 = ParseFrame72Bytes(decodedData, recordOffset);
                                break;
                            }
                        }
                        
                        offset = frameStart + 12 + (int)dataLength + 2; // 跳过整帧
                    }

                    if (frame72 != null)
                    {
                        FillTableWithFrame72Data(frame72);
                    }
                    else
                    {
                        Debug.LogWarning("[SlotDetailPanel] 未能从数据中解析出有效的 Frame 72 记录，填充默认值");
                        FillTableWithDefaultData();
                    }
                }
                else
                {
                    Debug.LogWarning("[SlotDetailPanel] Base64解码失败，填充默认值");
                    FillTableWithDefaultData();
                }
            }
            else
            {
                Debug.LogError($"[SlotDetailPanel] API请求失败: {request.error}");
                FillTableWithDefaultData();
            }
        }
    }

    /// <summary>
    /// 用 Frame72 数据填充 UITable（15行 x 4列）
    /// </summary>
    private void FillTableWithFrame72Data(Frame72SlotData data)
    {
        if (detailTable == null)
        {
            Debug.LogError("[SlotDetailPanel] ✗ UITable 未配置！");
            return;
        }

        // 清空并重新填充
        detailTable.ClearData();

        // 添加15行数据（每行4列：左项目名 | 左数值 | 右项目名 | 右数值）
        detailTable.AddRow("设定电压", FormatVoltage(data.设定电压), "平均电压", FormatVoltage(data.平均电压));
        detailTable.AddRow("工作电压", FormatVoltage(data.工作电压), "系列电流", (data.系列电流 / 1000f).ToString("F0") + "KA");
        detailTable.AddRow("标准电压", FormatVoltage(data.标准电压), "针振", data.针振.ToString());
        detailTable.AddRow("摆动", data.摆动.ToString(), "总功耗", data.总功耗.ToString() + "KW");
        detailTable.AddRow("下料次数", data.下料次数.ToString(), "下料量", data.下料量.ToString() + "Kg");
        detailTable.AddRow("出铝时间", data.出铝时间.ToString() + "h", "换极时间", data.换极时间.ToString() + "h");
        detailTable.AddRow("辅料时间", data.辅料时间.ToString() + "h", "效应距今", data.效应距今.ToString() + "h");
        detailTable.AddRow("换极距今", data.换极距今.ToString() + "h", "出铝距今", data.出铝距今.ToString() + "h");
        detailTable.AddRow("辅料距今", data.辅料距今.ToString() + "h", "氟盐下料次数", data.氟盐下料次数.ToString());
        detailTable.AddRow("电阻率", data.电阻率.ToString() + "mΩ", "基准下料间隔", data.基准下料间隔.ToString() + "min");
        detailTable.AddRow("阳极下降量", data.阳极下降量.ToString() + "mm", "设定效应间隔", data.设定效应间隔.ToString() + "h");
        detailTable.AddRow("电压上限", FormatVoltage(data.电压上限), "电压下限", FormatVoltage(data.电压下限));
        detailTable.AddRow("系列电压", data.系列电压.ToString() + "V", "效应等待持续", data.效应等待持续.ToString() + "s");
        detailTable.AddRow("电压异常持续", data.电压异常持续.ToString() + "s", "摆动持续", data.摆动持续.ToString() + "s");
        detailTable.AddRow("效应次数", data.效应次数.ToString(), "效应持续时间", data.效应持续时间.ToString() + "s");

        // 更新字体
        UpdateTableFont();

        Debug.Log("[SlotDetailPanel] ✓ 表格数据填充完成");
    }

    /// <summary>
    /// 填充默认数据（API失败时使用）
    /// </summary>
    private void FillTableWithDefaultData()
    {
        if (detailTable == null) return;

        detailTable.ClearData();

        detailTable.AddRow("设定电压", "0V", "平均电压", "0V");
        detailTable.AddRow("工作电压", "0V", "系列电流", "0KA");
        detailTable.AddRow("标准电压", "0V", "针振", "0");
        detailTable.AddRow("摆动", "0", "总功耗", "0KW");
        detailTable.AddRow("下料次数", "0", "下料量", "0Kg");
        detailTable.AddRow("出铝时间", "0h", "换极时间", "0h");
        detailTable.AddRow("辅料时间", "0h", "效应距今", "0h");
        detailTable.AddRow("换极距今", "0h", "出铝距今", "0h");
        detailTable.AddRow("辅料距今", "0h", "氟盐下料次数", "0");
        detailTable.AddRow("电阻率", "0mΩ", "基准下料间隔", "0min");
        detailTable.AddRow("阳极下降量", "0mm", "设定效应间隔", "0h");
        detailTable.AddRow("电压上限", "0V", "电压下限", "0V");
        detailTable.AddRow("系列电压", "0V", "效应等待持续", "0s");
        detailTable.AddRow("电压异常持续", "0s", "摆动持续", "0s");
        detailTable.AddRow("效应次数", "0", "效应持续时间", "0s");

        // 更新字体
        UpdateTableFont();

        Debug.Log("[SlotDetailPanel] 已填充默认数据");
    }

    /// <summary>
    /// 解析Frame 72二进制数据（134字节）
    /// </summary>
    private Frame72SlotData ParseFrame72Bytes(byte[] data, int offset)
    {
        Frame72SlotData result = new Frame72SlotData();
        int pos = offset;

        result.记录号 = ReadUInt16(data, ref pos);
        result.槽号 = ReadUInt16(data, ref pos);
        result.日期 = ReadUInt32(data, ref pos);
        result.时间 = ReadUInt32(data, ref pos);
        result.车间号 = ReadByte(data, ref pos);
        result.工区号 = ReadByte(data, ref pos);
        result.设定电压 = ReadUInt16(data, ref pos);
        result.平均电压 = ReadUInt16(data, ref pos);
        result.工作电压 = ReadUInt16(data, ref pos);
        result.系列电流 = ReadUInt16(data, ref pos);
        result.标准电压 = ReadUInt16(data, ref pos);
        result.针振 = ReadUInt16(data, ref pos);
        result.摆动 = ReadUInt16(data, ref pos);
        result.总功耗 = ReadUInt32(data, ref pos);
        result.下料次数 = ReadUInt16(data, ref pos);
        result.下料量 = ReadUInt16(data, ref pos);
        result.出铝时间 = ReadUInt24(data, ref pos);
        result.换极时间 = ReadUInt24(data, ref pos);
        result.辅料时间 = ReadUInt24(data, ref pos);
        result.效应距今 = ReadUInt24(data, ref pos);
        result.换极距今 = ReadUInt24(data, ref pos);
        result.出铝距今 = ReadUInt24(data, ref pos);
        result.辅料距今 = ReadUInt24(data, ref pos);
        result.备注 = ReadUInt16(data, ref pos);
        result.氟盐下料次数 = ReadUInt16(data, ref pos);
        result.电阻率 = ReadUInt16(data, ref pos);
        result.基准下料间隔 = ReadUInt16(data, ref pos);
        result.阳极下降量 = ReadUInt16(data, ref pos);
        result.设定效应间隔 = ReadUInt32(data, ref pos);
        result.电压上限 = ReadUInt16(data, ref pos);
        result.电压下限 = ReadUInt16(data, ref pos);
        result.系列电压 = ReadUInt32(data, ref pos);
        result.效应等待持续 = ReadUInt32(data, ref pos);
        result.电压异常持续 = ReadUInt16(data, ref pos);
        result.摆动持续 = ReadUInt16(data, ref pos);
        result.手动持续 = ReadUInt16(data, ref pos);
        result.通信断持续 = ReadUInt16(data, ref pos);
        result.欠量下料次数 = ReadUInt16(data, ref pos);
        result.过量下料次数 = ReadUInt16(data, ref pos);
        result.闪烁效应基准 = ReadUInt16(data, ref pos);
        result.抬母线时间 = ReadUInt24(data, ref pos);
        result.功能键次数 = ReadUInt16(data, ref pos);
        result.功能键时间 = ReadUInt16(data, ref pos);
        result.阳极行程 = ReadUInt16(data, ref pos);
        result.效应次数 = ReadUInt16(data, ref pos);
        result.效应持续时间 = ReadUInt32(data, ref pos);
        result.效应峰压 = ReadUInt32(data, ref pos);
        result.效应均压 = ReadUInt32(data, ref pos);
        result.效应功耗 = ReadUInt32(data, ref pos);
        result.效应设定间隔 = ReadUInt32(data, ref pos);
        result.效应设定等待时间 = ReadUInt16(data, ref pos);
        result.实际效应等待时间 = ReadUInt32(data, ref pos);

        return result;
    }

    private ushort ReadUInt16BE(byte[] data, ref int pos)
    {
        ushort val = (ushort)((data[pos] << 8) | data[pos + 1]);
        pos += 2;
        return val;
    }

    private uint ReadUInt32BE(byte[] data, ref int pos)
    {
        uint val = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
        pos += 4;
        return val;
    }

    private byte ReadByte(byte[] data, ref int pos) => data[pos++];

    private ushort ReadUInt16(byte[] data, ref int pos)
    {
        ushort val = (ushort)((data[pos] << 8) | data[pos + 1]);
        pos += 2;
        return val;
    }

    private uint ReadUInt24(byte[] data, ref int pos)
    {
        uint val = (uint)((data[pos] << 16) | (data[pos + 1] << 8) | data[pos + 2]);
        pos += 3;
        return val;
    }

    private uint ReadUInt32(byte[] data, ref int pos)
    {
        uint val = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
        pos += 4;
        return val;
    }

    private static byte[] DecodeBase64Payload(string responseText)
    {
        if (string.IsNullOrEmpty(responseText)) return null;
        try
        {
            string clean = responseText.Trim().Trim('"');
            int mod = clean.Length % 4;
            if (mod > 0) clean += new string('=', 4 - mod);
            return System.Convert.FromBase64String(clean);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    private string FormatVoltage(int millivolts)
    {
        return (millivolts / 1000f).ToString("F3") + "V";
    }

    /// <summary>
    /// 返回按钮点击 - 返回列表页
    /// </summary>
    private void OnBackButtonClicked()
    {
        Debug.Log("[SlotDetailPanel] 返回列表页");
        SetPanelVisibility(detailPanel, false);
        SetPanelVisibility(listPanel, true);
    }

    /// <summary>
    /// 设置面板可见性（使用CanvasGroup）
    /// </summary>
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel == null)
        {
            Debug.LogWarning("[SlotDetailPanel] Panel is null!");
            return;
        }

        if (isVisible && !panel.activeSelf)
        {
            panel.SetActive(true);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }

        cg.alpha = isVisible ? 1f : 0f;
        cg.interactable = isVisible;
        cg.blocksRaycasts = isVisible;

        Debug.Log($"[SlotDetailPanel] {panel.name} 可见性: {isVisible}");
    }

    /// <summary>
    /// 创建动态TMP字体（自动补字）
    /// </summary>
    private TMP_FontAsset CreateDynamicFont()
    {
        // 加载 TTF 源字体
        Font ttf = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular");
        if (ttf == null)
        {
            ttf = Resources.Load<Font>("Alibaba-PuHuiTi-Regular");
        }

        if (ttf == null)
        {
            Debug.LogError("[SlotDetailPanel] 找不到 TTF 字体文件，无法创建动态字体");
            return null;
        }

        Debug.Log($"[SlotDetailPanel] 找到 TTF 字体: {ttf.name}，创建动态 TMP 字体...");

        // 创建动态字体
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            ttf,
            90,
            10,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic);

        if (fontAsset != null)
        {
            fontAsset.isMultiAtlasTexturesEnabled = true;
            Debug.Log("[SlotDetailPanel] ✓ 动态字体创建成功，支持运行时补字");
        }
        else
        {
            Debug.LogError("[SlotDetailPanel] 动态字体创建失败");
        }

        return fontAsset;
    }
}

/// <summary>
/// Frame 72 单槽跨度日报数据结构（134字节）
/// </summary>
public class Frame72SlotData
{
    public ushort 记录号, 槽号;
    public uint 日期, 时间;
    public byte 车间号, 工区号;
    public ushort 设定电压, 平均电压, 工作电压, 系列电流, 标准电压, 针振, 摆动;
    public uint 总功耗;
    public ushort 下料次数, 下料量;
    public uint 出铝时间, 换极时间, 辅料时间, 效应距今, 换极距今, 出铝距今, 辅料距今;
    public ushort 备注, 氟盐下料次数, 电阻率, 基准下料间隔, 阳极下降量;
    public uint 设定效应间隔;
    public ushort 电压上限, 电压下限;
    public uint 系列电压, 效应等待持续;
    public ushort 电压异常持续, 摆动持续, 手动持续, 通信断持续, 欠量下料次数, 过量下料次数, 闪烁效应基准;
    public uint 抬母线时间;
    public ushort 功能键次数, 功能键时间, 阳极行程, 效应次数;
    public uint 效应持续时间, 效应峰压, 效应均压, 效应功耗, 效应设定间隔;
    public ushort 效应设定等待时间;
    public uint 实际效应等待时间;
}
