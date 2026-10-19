using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using XCharts.Runtime;
using XCharts.Runtime.UI;

/// <summary>
/// 采况统计 - 详情面板管理。
/// 挂在常驻激活的 tongjiManager 物体上（不要挂在 xiangqing 上，否则 xiangqing 初始隐藏时
/// Awake/Start 不会执行，表格点击绑定永远注册不上，形成死锁）：
///   1) 启动时隐藏详情面板 detailPanel（= xiangqing）
///   2) 监听 UITable_A/B/C/D 的行点击，取该行的工区、槽号
///   3) 显示详情面板，写入头部（工区/槽号/统计周期）
///   4) 返回按钮点击 → 隐藏面板
///   5) xuanze 标签栏四个按钮：配置了场景名的跳转对应场景，未配置的在详情内切换面板
///   6) 运行时构建 30 行参数表（label/value × 2 列），后续 API 用 SetParamValue 填值
/// </summary>
public class CaokuangDetailManager : MonoBehaviour
{
    [Header("详情面板（拖入 xiangqing 物体）")]
    public GameObject detailPanel;

    [Header("四个 ABCD 类别的 UITable")]
    public UITable tableA;
    public UITable tableB;
    public UITable tableC;
    public UITable tableD;

    [Header("详情页头部文本（拖入对应 Text/TMP 物体）")]
    public GameObject workAreaText;    // 工区显示
    public GameObject slotNoText;      // 槽号显示
    public GameObject scoreText;       // 统计周期显示

    [Header("返回按钮（点击关闭详情页）")]
    public Button backButton;

    [Header("四个标签按钮跳转的场景名（留空=在详情内切换面板）")]
    [Tooltip("顺序：dangrishuju(当日数据)、lishiquxian(历史曲线)、gongyiquxian(工艺曲线)、chuanganquxian(传感器曲线)")]
    public string[] tabSceneNames = new string[]
    {
        "",              // dangrishuju 当日数据
        "lishiquxiantu", // lishiquxian 历史曲线
        "gongyiquxian",  // gongyiquxian 工艺曲线
        "chuanganquxian" // chuanganquxian 传感器曲线
    };

    [Header("参数表容器（留空=自动找 detailPanel/dangrishuju）")]
    public GameObject paramGridContainer;

    [Header("参数表样式")]
    public int paramTableFontSize = 26;
    public float paramRowHeight = 56f;
    public Color paramLabelColor = new Color(0.18f, 0.18f, 0.18f);
    public Color paramValueColor = new Color(0.18f, 0.18f, 0.18f);
    public Color paramRowBgA = Color.white;
    public Color paramRowBgB = new Color(0.93f, 0.93f, 0.93f);

    /// <summary>
    /// 单个参数定义（标签 + 默认占位值）。后续 API 可按索引或 label 填值。
    /// </summary>
    public class DetailParamDef
    {
        public string label;
        public string defaultValue;
    }

    /// <summary>
    /// 30 个参数（label + 默认占位值），与截图"统计报表"布局一致。
    /// 顺序：每 2 个 (label, value) 占一行内 4 列中的 2 列，30 个参数共 15 行。
    /// 索引 = ParamDefs 数组下标 (0-29)。
    /// </summary>
    public static readonly DetailParamDef[] ParamDefs = new DetailParamDef[]
    {
        new DetailParamDef { label = "设定电压",     defaultValue = "0V"   }, // 0
        new DetailParamDef { label = "平均电压",     defaultValue = "0V"   }, // 1
        new DetailParamDef { label = "工作电压",     defaultValue = "0V"   }, // 2
        new DetailParamDef { label = "系列电流",     defaultValue = "0KA"  }, // 3
        new DetailParamDef { label = "标准电压",     defaultValue = "0V"   }, // 4
        new DetailParamDef { label = "针振",         defaultValue = "0"    }, // 5
        new DetailParamDef { label = "摆动",         defaultValue = "0"    }, // 6
        new DetailParamDef { label = "总功耗",       defaultValue = "0KW"  }, // 7
        new DetailParamDef { label = "下料次数",     defaultValue = "0"    }, // 8
        new DetailParamDef { label = "下料量",       defaultValue = "0Kg"  }, // 9
        new DetailParamDef { label = "出铝时间",     defaultValue = "0h"   }, // 10
        new DetailParamDef { label = "换极时间",     defaultValue = "0h"   }, // 11
        new DetailParamDef { label = "铺料时间",     defaultValue = "0h"   }, // 12
        new DetailParamDef { label = "效应距今",     defaultValue = "0h"   }, // 13
        new DetailParamDef { label = "换极距今",     defaultValue = "0h"   }, // 14
        new DetailParamDef { label = "出铝距今",     defaultValue = "0h"   }, // 15
        new DetailParamDef { label = "铺料距今",     defaultValue = "0h"   }, // 16
        new DetailParamDef { label = "氟盐下料次数", defaultValue = "0"    }, // 17
        new DetailParamDef { label = "电阻率",       defaultValue = "0mΩ"  }, // 18
        new DetailParamDef { label = "基准下料间隔", defaultValue = "0min" }, // 19
        new DetailParamDef { label = "阳极下降量",   defaultValue = "0mm"  }, // 20
        new DetailParamDef { label = "设定效应间隔", defaultValue = "0h"   }, // 21
        new DetailParamDef { label = "电压上限",     defaultValue = "0V"   }, // 22
        new DetailParamDef { label = "电压下限",     defaultValue = "0V"   }, // 23
        new DetailParamDef { label = "系列电压",     defaultValue = "0V"   }, // 24
        new DetailParamDef { label = "效应等待持续", defaultValue = "0s"   }, // 25
        new DetailParamDef { label = "电压异常持续", defaultValue = "0s"   }, // 26
        new DetailParamDef { label = "摆动持续",     defaultValue = "0s"   }, // 27
        new DetailParamDef { label = "效应次数",     defaultValue = "0"    }, // 28
        new DetailParamDef { label = "效应持续时间", defaultValue = "0s"   }  // 29
    };

    private static readonly string[] DetailPanelNames = { "dangrishuju", "lishiquxian", "gongyiquxian", "chuanganquxian" };

    // runtime
    private List<TextMeshProUGUI> paramValueTexts;
    private GameObject paramGridRoot;

    void Start()
    {
        // 1) 初始不展示详情面板
        if (detailPanel == null)
        {
            Debug.LogWarning("[CaokuangDetailManager] detailPanel 未赋值", this);
        }
        else
        {
            detailPanel.SetActive(false);
        }

        // 2) 绑定四个表格的行点击回调
        if (tableA != null) tableA.onTableClick = (int row, int col, TableCell cell) => OnTableRowClicked(tableA, row);
        if (tableB != null) tableB.onTableClick = (int row, int col, TableCell cell) => OnTableRowClicked(tableB, row);
        if (tableC != null) tableC.onTableClick = (int row, int col, TableCell cell) => OnTableRowClicked(tableC, row);
        if (tableD != null) tableD.onTableClick = (int row, int col, TableCell cell) => OnTableRowClicked(tableD, row);

        // 3) 返回按钮
        if (backButton != null) backButton.onClick.AddListener(Hide);

        // 4) 标签按钮
        BindTabButtons();

        // 5) 构建参数表（必须在 Start 末尾做，因为 dangrishuju 此时层级已稳定）
        BuildParamGrid();
    }

    /// <summary>
    /// 在 detailPanel 的 xuanze 标签栏中，按名称找到四个按钮并绑定点击。
    /// </summary>
    private void BindTabButtons()
    {
        if (detailPanel == null) return;

        Transform xuanze = detailPanel.transform.Find("xuanze");
        if (xuanze == null)
        {
            Debug.LogWarning("[CaokuangDetailManager] 找不到 xuanze 标签栏，四个切换按钮未绑定！", detailPanel);
            return;
        }

        for (int i = 0; i < DetailPanelNames.Length; i++)
        {
            string panelName = DetailPanelNames[i];
            Transform btnT = xuanze.Find(panelName);
            if (btnT == null) continue;
            Button btn = btnT.GetComponent<Button>();
            if (btn == null) continue;

            string sceneName = (tabSceneNames != null && i < tabSceneNames.Length) ? tabSceneNames[i] : "";
            string capturePanel = panelName;
            btn.onClick.AddListener(() => OnTabClicked(capturePanel, sceneName));
        }
    }

    private void OnTabClicked(string panelName, string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"[CaokuangDetailManager] 按钮 {panelName} 跳转场景: {sceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return;
        }
        SwitchPanel(panelName);
    }

    public void SwitchPanel(string panelName)
    {
        if (detailPanel == null) return;
        foreach (string name in DetailPanelNames)
        {
            Transform t = detailPanel.transform.Find(name);
            if (t != null) t.gameObject.SetActive(name == panelName);
        }
    }

    /// <summary>
    /// 表格某一行被点击：取工区、槽号写入详情头部，并显示面板。
    /// 参数表的 30 个值先重置为默认值，等待 API 后续 SetParamValue 覆盖。
    /// </summary>
    private void OnTableRowClicked(UITable table, int rowIndex)
    {
        if (table == null || table.data == null || table.data.Count == 0) return;
        if (rowIndex < 0 || rowIndex >= table.data.Count) return;
        if (detailPanel == null) return;

        var rowData = table.data[rowIndex].data;
        string workArea = GetCol(rowData, 0);
        string slotNo   = GetCol(rowData, 1);
        string period   = System.DateTime.Today.ToString("yyyy.MM.dd");

        // 头部三个文本
        SetHeader(workArea, slotNo, period);
        // 参数表重置为默认值
        ResetAllParamValuesToDefault();

        // 显示详情面板并切到"当日数据"子页
        detailPanel.SetActive(true);
        SwitchPanel("dangrishuju");
    }

    private string GetCol(System.Collections.Generic.List<TableCell> rowData, int colIndex)
    {
        if (rowData == null || colIndex < 0 || colIndex >= rowData.Count) return "";
        var cell = rowData[colIndex];
        return cell == null ? "" : cell.GetContent();
    }

    private void SetText(GameObject textObj, string content)
    {
        if (textObj == null) return;
        var t = textObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (t != null) { t.text = content; return; }
        var tmp = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null) { tmp.text = content; return; }
    }

    public void Hide()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    public void Show()
    {
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    // ===========================================================
    // 参数表（运行时构建）：4 列 label/value/label/value，15 行
    // ===========================================================

    private GameObject ResolveParamGridContainer()
    {
        if (paramGridContainer != null) return paramGridContainer;
        if (detailPanel == null) return null;
        Transform t = detailPanel.transform.Find("dangrishuju");
        return t != null ? t.gameObject : detailPanel;
    }

    private void BuildParamGrid()
    {
        var container = ResolveParamGridContainer();
        if (container == null)
        {
            Debug.LogWarning("[CaokuangDetailManager] 找不到参数表容器，跳过表格构建", this);
            return;
        }

        // 1) 隐藏 dangrishuju 里的空 UITable（XCharts），它一直显示 cell_X_Y 占位格
        var oldUItables = container.GetComponentsInChildren<UITable>(true);
        foreach (var t in oldUItables)
        {
            Debug.Log($"[CaokuangDetailManager] 隐藏旧 UITable: {t.name}", t);
            t.gameObject.SetActive(false);
        }

        // 2) 清理我自己的旧构建
        Transform existing = container.transform.Find("__RuntimeParamGrid");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        // 3) 创建参数表根节点（占底部 70% 区域，顶部 30% 留给 3 个 header 文本）
        paramGridRoot = new GameObject("__RuntimeParamGrid", typeof(RectTransform));
        var gridRT = paramGridRoot.GetComponent<RectTransform>();
        gridRT.SetParent(container.transform, false);
        gridRT.anchorMin = new Vector2(0f, 0f);
        gridRT.anchorMax = new Vector2(1f, 0.7f);
        gridRT.offsetMin = new Vector2(20f, 20f);
        gridRT.offsetMax = new Vector2(-20f, 0f);
        gridRT.SetAsLastSibling();

        // 用 VerticalLayoutGroup 装行（行内再用 HorizontalLayoutGroup 装 4 个 cell）
        // 这种嵌套结构可以让 cell 自动按父容器宽度等分，无需手动算 cellSize.x
        var vlg = paramGridRoot.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        paramValueTexts = new List<TextMeshProUGUI>(ParamDefs.Length);

        int rowCount = (ParamDefs.Length + 1) / 2; // 15
        for (int visualRow = 0; visualRow < rowCount; visualRow++)
        {
            int idxLeft = visualRow * 2;
            int idxRight = visualRow * 2 + 1;
            bool hasRight = idxRight < ParamDefs.Length;

            var rowGO = CreateParamRow();
            rowGO.transform.SetParent(paramGridRoot.transform, false);

            // 左 label + 左 value
            AddParamCell(rowGO, visualRow, true,  ParamDefs[idxLeft].label);
            var leftValTMP = AddParamCell(rowGO, visualRow, false, ParamDefs[idxLeft].defaultValue);
            if (leftValTMP != null) paramValueTexts.Add(leftValTMP);

            // 右 label + 右 value
            string rightLabel = hasRight ? ParamDefs[idxRight].label : "";
            string rightVal   = hasRight ? ParamDefs[idxRight].defaultValue : "";
            AddParamCell(rowGO, visualRow, true, rightLabel);
            var rightValTMP = AddParamCell(rowGO, visualRow, false, rightVal);
            if (hasRight && rightValTMP != null) paramValueTexts.Add(rightValTMP);
        }
    }

    private GameObject CreateParamRow()
    {
        var row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
        var le = row.GetComponent<LayoutElement>();
        le.minHeight = paramRowHeight;
        le.preferredHeight = paramRowHeight;
        le.flexibleHeight = 0f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = 2f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        return row;
    }

    /// <summary>
    /// 在一行里添加一个 cell（label 或 value）。label cell 返回 null；value cell 返回其 TMP 引用。
    /// </summary>
    private TextMeshProUGUI AddParamCell(GameObject row, int visualRow, bool isLabel, string text)
    {
        var cell = new GameObject(isLabel ? "Label" : "Value", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        cell.transform.SetParent(row.transform, false);

        var img = cell.GetComponent<Image>();
        img.color = (visualRow % 2 == 0) ? paramRowBgA : paramRowBgB;
        img.raycastTarget = false;

        var le = cell.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = paramRowHeight;
        le.preferredHeight = paramRowHeight;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(cell.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 4f);
        textRT.offsetMax = new Vector2(-8f, -4f);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = paramTableFontSize;
        tmp.color = isLabel ? paramLabelColor : paramValueColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = isLabel ? FontStyles.Bold : FontStyles.Normal;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.raycastTarget = false;

        return isLabel ? null : tmp;
    }

    /// <summary>
    /// 设置详情面板头部的三个文本（工区/槽号/统计周期）。
    /// </summary>
    public void SetHeader(string workArea, string slotNo, string period)
    {
        if (workAreaText != null) SetText(workAreaText, "工区：" + workArea);
        if (slotNoText != null)   SetText(slotNoText,   "槽号：" + slotNo);
        if (scoreText != null)    SetText(scoreText,    "统计周期：" + period);
    }

    /// <summary>
    /// 按索引 (0-29) 设置参数值。后续 API 接入示例：
    ///   grid.SetParamValue(0, "4.2V");   // 设定电压
    /// </summary>
    public void SetParamValue(int paramIndex, string value)
    {
        if (paramValueTexts == null || paramIndex < 0 || paramIndex >= paramValueTexts.Count) return;
        paramValueTexts[paramIndex].text = value;
    }

    /// <summary>
    /// 按参数名设置参数值。示例：
    ///   grid.SetParamValue("设定电压", "4.2V");
    ///   grid.SetParamValue("平均电压", "4.1V");
    /// </summary>
    public void SetParamValue(string label, string value)
    {
        for (int i = 0; i < ParamDefs.Length; i++)
        {
            if (ParamDefs[i].label == label)
            {
                SetParamValue(i, value);
                return;
            }
        }
        Debug.LogWarning($"[CaokuangDetailManager] 找不到参数: {label}");
    }

    /// <summary>
    /// 把所有 30 个参数值重置为 ParamDefs 中的默认值。
    /// </summary>
    public void ResetAllParamValuesToDefault()
    {
        if (paramValueTexts == null) return;
        for (int i = 0; i < ParamDefs.Length && i < paramValueTexts.Count; i++)
        {
            paramValueTexts[i].text = ParamDefs[i].defaultValue;
        }
    }
}
