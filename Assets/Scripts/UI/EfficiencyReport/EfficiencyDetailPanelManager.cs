using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XCharts.Runtime.UI;
using System.Collections.Generic;

public class EfficiencyDetailPanelManager : MonoBehaviour
{
    [Header("列表面板（返回时使用）")]
    public GameObject dangriListPanel;
    public GameObject lishiListPanel;

    [Header("单槽详情面板 (基于 UITable_Effect 点击)")]
    public GameObject panelSlotDetail;
    public TextMeshProUGUI txtSlotTitle;   // e.g. "1车间2工区"
    public TextMeshProUGUI txtSlotNumber;  // 槽号: "2031"
    public TextMeshProUGUI txtSlotDate;    // 日期: "2024.07.01"
    public UITable slotDetailTable;        // 显示单槽详细数据的表格
    public Button btnBackSlot;             // 返回按钮

    [Header("工区详情面板 (基于 UITable_Summary 点击)")]
    public GameObject panelWorkAreaDetail;
    public TextMeshProUGUI txtWorkAreaTitle; // e.g. "1车间2工区"
    public TextMeshProUGUI txtWorkAreaDate;  // 日期: "2024.07.02"
    public UITable workAreaDetailTable;      // 显示工区详细数据的表格
    public Button btnBackWorkArea;           // 返回按钮

    private string lastMode = "dangri";

    private void Start()
    {
        AutoBindIfMissing();

        // 初始关闭所有详情面板
        if (panelSlotDetail != null) panelSlotDetail.SetActive(false);
        if (panelWorkAreaDetail != null) panelWorkAreaDetail.SetActive(false);

        // 绑定返回按钮
        if (btnBackSlot != null)
        {
            btnBackSlot.onClick.RemoveListener(BackToPreviousList);
            btnBackSlot.onClick.AddListener(BackToPreviousList);
        }
        if (btnBackWorkArea != null)
        {
            btnBackWorkArea.onClick.RemoveListener(BackToPreviousList);
            btnBackWorkArea.onClick.AddListener(BackToPreviousList);
        }
    }

    private void OnDestroy()
    {
        if (btnBackSlot != null) btnBackSlot.onClick.RemoveListener(BackToPreviousList);
        if (btnBackWorkArea != null) btnBackWorkArea.onClick.RemoveListener(BackToPreviousList);
    }

        private void AutoBindIfMissing()
    {
        if (panelSlotDetail == null)
        {
            var go = GameObject.Find("panelSlotDetail") ?? GameObject.Find("SlotDetailPanel") ?? GameObject.Find("slotDetailPanel");
            if (go != null) panelSlotDetail = go;
        }

        if (panelWorkAreaDetail == null)
        {
            var go = GameObject.Find("panelWorkAreaDetail") ?? GameObject.Find("WorkAreaDetailPanel") ?? GameObject.Find("workAreaDetailPanel");
            if (go != null) panelWorkAreaDetail = go;
        }

        if (slotDetailTable == null && panelSlotDetail != null) {
            slotDetailTable = panelSlotDetail.GetComponentInChildren<UITable>(true);
        }
        if (workAreaDetailTable == null && panelWorkAreaDetail != null) {
            workAreaDetailTable = panelWorkAreaDetail.GetComponentInChildren<UITable>(true);
        }

        if (dangriListPanel == null)
        {
            var go = GameObject.Find("dangribaobiaoPanel") ?? GameObject.Find("DangriPanel");
            if (go != null) dangriListPanel = go;
        }

        if (lishiListPanel == null)
        {
            var go = GameObject.Find("lishitongjiPanel") ?? GameObject.Find("LishiPanel");
            if (go != null) lishiListPanel = go;
        }

        if (btnBackSlot == null)
        {
            btnBackSlot = FindBackButton(panelSlotDetail);
            if (btnBackSlot == null) btnBackSlot = FindAnyBackButton();
        }

        if (btnBackWorkArea == null)
        {
            btnBackWorkArea = FindBackButton(panelWorkAreaDetail);
            if (btnBackWorkArea == null) btnBackWorkArea = FindAnyBackButton();
        }
    }

    private Button FindBackButton(GameObject root)
    {
        if (root == null) return null;
        var btns = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < btns.Length; i++)
        {
            string n = btns[i].name.ToLowerInvariant();
            if (n == "back_b" || n.Contains("back") || n.Contains("返回"))
                return btns[i];
        }
        return null;
    }

    private Button FindAnyBackButton()
    {
        var all = FindObjectsOfType<Button>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name.ToLowerInvariant();
            if (n == "back_b" || n.Contains("back") || n.Contains("返回"))
                return all[i];
        }
        return null;
    }

    /// <summary>
    /// 打开单槽详情（当点击 UITable_Effect 里的行时调用）
    /// </summary>
    public void ShowSlotDetail(string areaName, string slotNo, string dateStr, string occurTime, float duration, float peak, float avg, float power)
    {
        ShowSlotDetail(areaName, slotNo, dateStr, 0, duration, peak, avg, power, "dangri");
    }

    public void ShowSlotDetail(string areaName, string slotNo, string dateStr, int occurCount, float duration, float peak, float avg, float power, string mode)
    {
        lastMode = string.IsNullOrEmpty(mode) ? "dangri" : mode;
        SetListPanelVisible(lastMode, false);
        CloseAll();
        if (panelSlotDetail != null) panelSlotDetail.SetActive(true);

        if (txtSlotTitle != null) txtSlotTitle.text = areaName;
        if (txtSlotNumber != null) txtSlotNumber.text = slotNo;
        if (txtSlotDate != null) txtSlotDate.text = dateStr;

        if (slotDetailTable != null)
        {
            slotDetailTable.ClearData();
            // 第一行: 发生次数 | 效应持续时间
            slotDetailTable.AddRow(new List<string> { "发生次数", occurCount.ToString(), "效应持续时间", duration.ToString("F2") });
            // 第二行: 效应峰压 | 效应均压
            slotDetailTable.AddRow(new List<string> { "效应峰压", peak.ToString("F2"), "效应均压", avg.ToString("F2") });
            // 第三行: 效应功耗 | 空 (为了和上面对齐结构，补全4列)
            slotDetailTable.AddRow(new List<string> { "效应功耗", power.ToString("F0"), "", "" });
            slotDetailTable.RefreshTable();
        }
    }

    /// <summary>
    /// 打开工区详情（当点击 UITable_Summary 里的行时调用）
    /// </summary>
    public void ShowWorkAreaDetail(string areaName, string dateStr, int occurCount, float duration, float peak, float avg, float power)
    {
        ShowWorkAreaDetail(areaName, dateStr, occurCount, duration, peak, avg, power, "dangri");
    }

    public void ShowWorkAreaDetail(string areaName, string dateStr, int occurCount, float duration, float peak, float avg, float power, string mode)
    {
        lastMode = string.IsNullOrEmpty(mode) ? "dangri" : mode;
        SetListPanelVisible(lastMode, false);
        CloseAll();
        if (panelWorkAreaDetail != null) panelWorkAreaDetail.SetActive(true);

        if (txtWorkAreaTitle != null) txtWorkAreaTitle.text = areaName;
        if (txtWorkAreaDate != null) txtWorkAreaDate.text = dateStr;

        if (workAreaDetailTable != null)
        {
            workAreaDetailTable.ClearData();
            // 第一行: 发生次数 | 效应持续时间
            workAreaDetailTable.AddRow(new List<string> { "发生次数", occurCount.ToString(), "效应持续时间", duration.ToString("F2") });
            // 第二行: 效应峰压 | 效应均压
            workAreaDetailTable.AddRow(new List<string> { "效应峰压", peak.ToString("F2"), "效应均压", avg.ToString("F2") });
            // 第三行: 效应功耗 | 空 (为了和上面对齐结构，补全4列)
            workAreaDetailTable.AddRow(new List<string> { "效应功耗", power.ToString("F0"), "", "" });
            workAreaDetailTable.RefreshTable();
        }
    }

    public void CloseAll()
    {
        if (panelSlotDetail != null) panelSlotDetail.SetActive(false);
        if (panelWorkAreaDetail != null) panelWorkAreaDetail.SetActive(false);
    }

    public void BackToPreviousList()
    {
        CloseAll();
        SetListPanelVisible(lastMode, true);
    }

    private void SetListPanelVisible(string mode, bool visible)
    {
        if (mode == "lishi")
        {
            if (lishiListPanel != null) lishiListPanel.SetActive(visible);
            if (dangriListPanel != null && visible) dangriListPanel.SetActive(false);
        }
        else
        {
            if (dangriListPanel != null) dangriListPanel.SetActive(visible);
            if (lishiListPanel != null && visible) lishiListPanel.SetActive(false);
        }
    }
}


