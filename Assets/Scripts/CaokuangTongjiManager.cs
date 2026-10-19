using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;
using XCharts.Runtime.UI;
using System.Collections.Generic;
using System;

public class CaokuangTongjiManager : MonoBehaviour
{
    [Header("筛选条件显示(拖入带文本的物体)")]
    public GameObject dateDisplayText; // 绑 日期显示 的那个物体
    public GameObject workAreaDisplayText; // 绑 车间显示 的那个物体

    [Header("ABCD类槽切换(拖入对应Button)")]
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;

    [Header("四个类别的UITable")]
    public UITable tableA;
    public UITable tableB;
    public UITable tableC;
    public UITable tableD;

    void Start()
    {
        // 0. 初始默认赋予 UI 文字（防止读到Unity里默认的"工区选择"这种提示文字）
        InitDefaultFilters();

        // 绑定 ABCD 切换事件：点击谁，就让谁对应的表显示
        if (buttonA != null) buttonA.onClick.AddListener(() => SwitchToTable("A"));
        if (buttonB != null) buttonB.onClick.AddListener(() => SwitchToTable("B"));
        if (buttonC != null) buttonC.onClick.AddListener(() => SwitchToTable("C"));
        if (buttonD != null) buttonD.onClick.AddListener(() => SwitchToTable("D"));

        // 初始化：默认显示 A 类槽
        SwitchToTable("A");

        // 初始时或者想要默认填充一次数据可以调用这里
        FetchDataFromAPI();
    }

    /// <summary>
    /// 控制ABCD四个表格面板的交替显示与隐藏
    /// </summary>
    private void SwitchToTable(string classType)
    {
        if (tableA != null) tableA.gameObject.SetActive(classType == "A");
        if (tableB != null) tableB.gameObject.SetActive(classType == "B");
        if (tableC != null) tableC.gameObject.SetActive(classType == "C");
        if (tableD != null) tableD.gameObject.SetActive(classType == "D");
    }

    // ==========================================
    // API 数据获取接口
    // ==========================================

    /// <summary>
    /// 初始化UI默认值，解决“工区选择”未及时刷新变成“1车间1工区”的问题
    /// </summary>
    private void InitDefaultFilters()
    {
        SetTextContent(dateDisplayText, DateTime.Today.ToString("yyyyMMdd"));
        SetTextContent(workAreaDisplayText, "1车间1工区");
    }

    /// <summary>
    /// 当你在"日期"和"车间"选择后，触发此方法统一请求数据并分配
    /// </summary>
    public void FetchDataFromAPI()
    {
        // 自动读取你在界面上最新选中的时间和车间文本
        string selectedDate = GetTextContent(dateDisplayText);
        string selectedWorkArea = GetTextContent(workAreaDisplayText);

        // 如果用户还没选或没取到，或者取到的是预设的提示文字，强制给定默认容错值
        if (string.IsNullOrEmpty(selectedDate) || selectedDate.Contains("选择")) selectedDate = DateTime.Today.ToString("yyyyMMdd");
        if (string.IsNullOrEmpty(selectedWorkArea) || selectedWorkArea.Contains("选择")) selectedWorkArea = "1车间1工区";

        Debug.Log($"[槽况统计拉取API] 请求参数 -> 日期: {selectedDate}, 车间: {selectedWorkArea}");

        // TODO: 将这里替换成真实的 HTTP WebRequest 请求后端
        // StartCoroutine(SendRequestToServer(selectedDate, selectedWorkArea));

        // 暂时使用模拟数据进行表格的六列填充预览。当你 API 接通后调用真实数据填充即可。
        FillMockDataForTables(selectedDate, selectedWorkArea);
    }

    /// <summary>
    /// 将获取到的数据分组填充至 A、B、C、D 四个分别的表格中 (每表6列)
    /// </summary>
    private void FillMockDataForTables(string date, string workArea)
    {
        // 截取“工区”部分，去掉“车间”及之前的内容（例如从 "1车间1工区" 提取出 "1工区"）
        string displayWorkArea = workArea;
        int chejianIndex = displayWorkArea.IndexOf("车间");
        if (chejianIndex >= 0)
        {
            displayWorkArea = displayWorkArea.Substring(chejianIndex + 2); // 越过"车间"两个字
        }

        // ------------------ 填充 A类槽 数据 ------------------
        if (tableA != null)
        {
            tableA.ClearData();
            // 参数数量需要对应你配置的 6 列 (Column0 到 Column5)
            // 假设你的列是："工区", "槽号", "评分", "槽况", "平均分", "趋势"
            tableA.AddRow(displayWorkArea, "1001", "90", "A类槽", "95", "上升 ↑");
            tableA.AddRow(displayWorkArea, "1002", "92", "A类槽", "95", "平稳 -");
            tableA.AddRow(displayWorkArea, "1003", "89", "A类槽", "95", "上升 ↑");
        }

        // ------------------ 填充 B类槽 数据 ------------------
        if (tableB != null)
        {
            tableB.ClearData();
            tableB.AddRow(displayWorkArea, "1015", "85", "B类槽", "84", "下降 ↓");
            tableB.AddRow(displayWorkArea, "1016", "82", "B类槽", "84", "平稳 -");
        }

        // ------------------ 填充 C类槽 数据 ------------------
        if (tableC != null)
        {
            tableC.ClearData();
            tableC.AddRow(displayWorkArea, "1030", "75", "C类槽", "76", "下降 ↓");
        }

        // ------------------ 填充 D类槽 数据 ------------------
        if (tableD != null)
        {
            tableD.ClearData();
            tableD.AddRow(displayWorkArea, "1044", "55", "D类槽", "60", "严重!!!");
        }
    }

    // ==========================================
    // UI 文字获取与设置工具（兼容普通Text或者TMP_Text）
    // ==========================================
    private void SetTextContent(GameObject textObj, string content)
    {
        if (textObj == null) return;

        var normalText = textObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (normalText != null)
        {
            normalText.text = content;
            return;
        }

        var tmpText = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = content;
        }
    }

    private string GetTextContent(GameObject textObj)
    {
        if (textObj == null) return "";

        var normalText = textObj.GetComponentInChildren<UnityEngine.UI.Text>();
        if (normalText != null) return normalText.text;

        var tmpText = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null) return tmpText.text;

        return "";
    }
}