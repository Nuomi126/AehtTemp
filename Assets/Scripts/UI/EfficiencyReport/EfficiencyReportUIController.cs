using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using XCharts.Runtime.UI;

public class EfficiencyReportUIController : MonoBehaviour
{
    [Header("Top Level Screens")]
    public GameObject dangribaobiaoPanel;
    public GameObject lishitongjiPanel;
    public GameObject detailPanel;

    [Header("Tabs/Buttons")]
    public Button btnDangri;
    public Button btnLishi;

    [Header("Data Manager (可选)")]
    public EfficiencyReportManager dataManager;

    private void Start()
    {
        AutoBindReferencesIfMissing();
        ResolvePanelsIfMisconfigured();
        NormalizeTabButtonLayout();
        SyncDetailPanelListReferences();
        BindTabButtons();
        
        // Default
        ShowDangriScreen();
    }

    private void AutoBindReferencesIfMissing()
    {
        if (dataManager == null)
        {
            dataManager = FindObjectOfType<EfficiencyReportManager>(true);
        }

        if (btnDangri == null)
        {
            var go = GameObject.Find("btnDangri");
            if (go == null) go = GameObject.Find("BtnDangri");
            if (go == null) go = GameObject.Find("Button_Dangri");
            if (go != null) btnDangri = go.GetComponent<Button>();
        }

        if (btnLishi == null)
        {
            var go = GameObject.Find("btnLishi");
            if (go == null) go = GameObject.Find("BtnLishi");
            if (go == null) go = GameObject.Find("Button_Lishi");
            if (go != null) btnLishi = go.GetComponent<Button>();
        }
    }

    private void BindTabButtons()
    {
        if (btnDangri != null)
        {
            btnDangri.onClick.RemoveListener(ShowDangriScreen);
            btnDangri.onClick.AddListener(ShowDangriScreen);
        }
        else
        {
            Debug.LogWarning("[EfficiencyReportUIController] 当日报表按钮未绑定。");
        }

        if (btnLishi != null)
        {
            btnLishi.onClick.RemoveListener(ShowLishiScreen);
            btnLishi.onClick.AddListener(ShowLishiScreen);
        }
        else
        {
            Debug.LogWarning("[EfficiencyReportUIController] 历史报表按钮未绑定。");
        }
    }

    private void ResolvePanelsIfMisconfigured()
    {
        bool dangriInvalid = dangribaobiaoPanel == null || (btnDangri != null && dangribaobiaoPanel == btnDangri.gameObject);
        bool lishiInvalid = lishitongjiPanel == null || (btnLishi != null && lishitongjiPanel == btnLishi.gameObject);

        if (dataManager != null)
        {
            if (dangriInvalid)
            {
                dangribaobiaoPanel = ResolvePanelFromTable(dataManager.dangri_UITable_Efficient);
            }

            if (lishiInvalid)
            {
                lishitongjiPanel = ResolvePanelFromTable(dataManager.lishi_UITable_Efficient);
            }
        }
    }

    private static GameObject ResolvePanelFromTable(UITable table)
    {
        if (table == null) return null;

        // 通常 UITable 的父节点就是报表内容容器。
        Transform t = table.transform.parent;
        if (t != null) return t.gameObject;

        return table.gameObject;
    }

    private void NormalizeTabButtonLayout()
    {
        if (btnDangri == null || btnLishi == null) return;

        RectTransform rtDangri = btnDangri.GetComponent<RectTransform>();
        RectTransform rtLishi = btnLishi.GetComponent<RectTransform>();
        if (rtDangri == null || rtLishi == null) return;
        if (rtDangri.parent != rtLishi.parent) return;

        RectTransform parent = rtDangri.parent as RectTransform;
        if (parent == null) return;

        float parentWidth = parent.rect.width;
        if (parentWidth <= 1f) return;

        // 仅当按钮明显跑偏（超出容器）时自动回正，避免影响已有正确布局。
        bool lishiOutOfBounds = Mathf.Abs(rtLishi.anchoredPosition.x) > parentWidth * 0.45f;
        if (!lishiOutOfBounds) return;

        float half = parentWidth * 0.25f;
        float btnWidth = Mathf.Max(120f, parentWidth * 0.30f);

        rtDangri.anchorMin = new Vector2(0.5f, rtDangri.anchorMin.y);
        rtDangri.anchorMax = new Vector2(0.5f, rtDangri.anchorMax.y);
        rtDangri.pivot = new Vector2(0.5f, rtDangri.pivot.y);
        rtDangri.anchoredPosition = new Vector2(-half, rtDangri.anchoredPosition.y);
        rtDangri.sizeDelta = new Vector2(btnWidth, rtDangri.sizeDelta.y);

        rtLishi.anchorMin = new Vector2(0.5f, rtLishi.anchorMin.y);
        rtLishi.anchorMax = new Vector2(0.5f, rtLishi.anchorMax.y);
        rtLishi.pivot = new Vector2(0.5f, rtLishi.pivot.y);
        rtLishi.anchoredPosition = new Vector2(half, rtLishi.anchoredPosition.y);
        rtLishi.sizeDelta = new Vector2(btnWidth, rtLishi.sizeDelta.y);
    }

    private void SyncDetailPanelListReferences()
    {
        if (dataManager == null || dataManager.detailPanelManager == null) return;
        dataManager.detailPanelManager.dangriListPanel = dangribaobiaoPanel;
        dataManager.detailPanelManager.lishiListPanel = lishitongjiPanel;
    }

    public void ShowDangriScreen()
    {
        if (dangribaobiaoPanel) dangribaobiaoPanel.SetActive(true);
        if (lishitongjiPanel) lishitongjiPanel.SetActive(false);
        if (detailPanel) detailPanel.SetActive(false);

        if (dataManager != null) dataManager.FetchData("dangri");
    }

    public void ShowLishiScreen()
    {
        if (dangribaobiaoPanel) dangribaobiaoPanel.SetActive(false);
        if (lishitongjiPanel) lishitongjiPanel.SetActive(true);
        if (detailPanel) detailPanel.SetActive(false);

        if (dataManager != null) dataManager.FetchData("lishi");
    }
    
    public void ShowDetailPanel()
    {
        if (detailPanel) detailPanel.SetActive(true);
    }
}
