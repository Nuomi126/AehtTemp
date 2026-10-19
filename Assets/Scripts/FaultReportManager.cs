using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using XCharts.Runtime.UI;

// === 数据结构定义 ===
[Serializable]
public class FaultData
{
    public string slotNumber;       // 槽号
    public string occurTime;        // 发生时间
    public string faultType;        // 故障类型
}

[Serializable]
public class OperationData
{
    public string slotNumber;       // 槽号
    public string operationTime;    // 操作时间
    public string operationType;    // 操作类型
}

public class FaultReportManager : MonoBehaviour
{
    [Header("=== 顶部 Tabs (页面切换) ===")]
    [Tooltip("切换到 当日报表 场景的按钮")]
    public Button dangribaobiaoTabBtn;
    [Tooltip("切换到 历史报表 场景的按钮")]
    public Button lishibaobiaoTabBtn;
    [Tooltip("当日报表的根节点 (Panel)")]
    public GameObject dangribaobiaoPanel;
    [Tooltip("历史报表的根节点 (Panel)")]
    public GameObject lishibaobiaoPanel;

    [Header("=== 当日报表 (dangribaobiao) ===")]
    [Tooltip("工区选择组件")]
    public WorkAreaSelector todayWorkAreaSelector;
    [Tooltip("工区选择打开按钮")]
    public Button todayWorkAreaBtn;
    [Tooltip("故障信息按钮")]
    public Button todayFaultInfoBtn;
    [Tooltip("操作信息按钮")]
    public Button todayOperationInfoBtn;
    [Tooltip("对应 UITable_guzhang 的父物体")]
    public GameObject todayFaultTableObj;
    [Tooltip("对应 UITable_caozuo 的父物体")]
    public GameObject todayOperationTableObj;

    [Header("=== 历史报表 (lishibaobiao) ===")]
    [Tooltip("工区选择组件")]
    public WorkAreaSelector historyWorkAreaSelector;
    [Tooltip("工区选择打开按钮")]
    public Button historyWorkAreaBtn;
    [Tooltip("日期选择组件")]
    public riqSelector historyDateSelector;
    [Tooltip("日期选择打开按钮")]
    public Button historyDateBtn;
    [Tooltip("故障信息按钮")]
    public Button historyFaultInfoBtn;
    [Tooltip("操作信息按钮")]
    public Button historyOperationInfoBtn;
    [Tooltip("对应 UITable_guzhang 的父物体")]
    public GameObject historyFaultTableObj;
    [Tooltip("对应 UITable_caozuo 的父物体")]
    public GameObject historyOperationTableObj;

    // --- 数据缓存 ---
    private string currentTodayWorkArea;
    private string currentHistoryWorkArea;
    private string currentHistoryDate;

    private List<FaultData> todayFaultList = new List<FaultData>();
    private List<OperationData> todayOperationList = new List<OperationData>();
    private List<FaultData> historyFaultList = new List<FaultData>();
    private List<OperationData> historyOperationList = new List<OperationData>();

    private enum PageType { Dangri, Lishi }
    private PageType currentPage;

    private void Start()
    {
        // 0. 绑定顶部 Tabs 切换
        BindTabs();

        // 1. 绑定并初始化工区和日期的选择器面板逻辑
        BindSelectors();

        // 2. 绑定当日报表表格切换事件
        if (todayFaultInfoBtn != null)
            todayFaultInfoBtn.onClick.AddListener(ShowTodayFaultTable);
        
        if (todayOperationInfoBtn != null)
            todayOperationInfoBtn.onClick.AddListener(ShowTodayOperationTable);

        // 3. 绑定历史报表表格切换事件
        if (historyFaultInfoBtn != null)
            historyFaultInfoBtn.onClick.AddListener(ShowHistoryFaultTable);
            
        if (historyOperationInfoBtn != null)
            historyOperationInfoBtn.onClick.AddListener(ShowHistoryOperationTable);

        // 4. 初始显示状态
        ShowPage(PageType.Dangri);
        ShowTodayFaultTable();
        ShowHistoryFaultTable();

        // 5. 初始化当前选中的默认数据 (赋予默认工区名字，防止为空时拦截请求)
        currentTodayWorkArea = "默认工区";
        currentHistoryWorkArea = "默认工区";
        currentHistoryDate = System.DateTime.Now.ToString("yyyyMMdd");
        
        FetchTodayData();
        FetchHistoryData();
    }

    private void BindTabs()
    {
        if (dangribaobiaoTabBtn != null)
            dangribaobiaoTabBtn.onClick.AddListener(() => ShowPage(PageType.Dangri));
            
        if (lishibaobiaoTabBtn != null)
            lishibaobiaoTabBtn.onClick.AddListener(() => ShowPage(PageType.Lishi));
    }

    private void BindSelectors()
    {
        // === 当日报表: 工区选择 ===
        if (todayWorkAreaBtn != null && todayWorkAreaSelector != null)
        {
            var text = todayWorkAreaBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) todayWorkAreaSelector.selectedDisplayText = text;
            todayWorkAreaBtn.onClick.AddListener(todayWorkAreaSelector.OpenSelectorPanel);
        }

        if (todayWorkAreaSelector != null)
        {
            todayWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(v => 
            {
                currentTodayWorkArea = v;
                if (currentPage == PageType.Dangri) FetchTodayData();
            });
        }

        // === 历史报表: 工区选择 ===
        if (historyWorkAreaBtn != null && historyWorkAreaSelector != null)
        {
            var text = historyWorkAreaBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) historyWorkAreaSelector.selectedDisplayText = text;
            historyWorkAreaBtn.onClick.AddListener(historyWorkAreaSelector.OpenSelectorPanel);
        }

        if (historyWorkAreaSelector != null)
        {
            historyWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(v => 
            {
                currentHistoryWorkArea = v;
                if (currentPage == PageType.Lishi) FetchHistoryData();
            });
        }

        // === 历史报表: 日期选择 ===
        if (historyDateBtn != null && historyDateSelector != null)
        {
            var text = historyDateBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) historyDateSelector.selectedDisplayText = text;
            historyDateBtn.onClick.AddListener(historyDateSelector.OpenSelectorPanel);
        }

        if (historyDateSelector != null)
        {
            historyDateSelector.OnDateConfirmed.AddListener(v =>
            {
                currentHistoryDate = v;
                if (currentPage == PageType.Lishi) FetchHistoryData();
            });
        }
    }

    #region -- 界面切换逻辑 --

    private void ShowPage(PageType page)
    {
        currentPage = page;
        if (dangribaobiaoPanel != null) dangribaobiaoPanel.SetActive(page == PageType.Dangri);
        if (lishibaobiaoPanel != null) lishibaobiaoPanel.SetActive(page == PageType.Lishi);
    }

    private void ShowTodayFaultTable()
    {
        if (todayFaultTableObj) todayFaultTableObj.SetActive(true);
        if (todayOperationTableObj) todayOperationTableObj.SetActive(false);
    }

    private void ShowTodayOperationTable()
    {
        if (todayFaultTableObj) todayFaultTableObj.SetActive(false);
        if (todayOperationTableObj) todayOperationTableObj.SetActive(true);
    }

    private void ShowHistoryFaultTable()
    {
        if (historyFaultTableObj) historyFaultTableObj.SetActive(true);
        if (historyOperationTableObj) historyOperationTableObj.SetActive(false);
    }

    private void ShowHistoryOperationTable()
    {
        if (historyFaultTableObj) historyFaultTableObj.SetActive(false);
        if (historyOperationTableObj) historyOperationTableObj.SetActive(true);
    }

    #endregion

    #region -- 数据获取与刷新逻辑 --

    private void FetchTodayData()
    {
        if (string.IsNullOrEmpty(currentTodayWorkArea)) return;
        
        // 调取假接口获取故障和操作数据，这里将 currentTodayWorkArea 传入
        StartCoroutine(MockApiGetTodayFaultData(currentTodayWorkArea));
        StartCoroutine(MockApiGetTodayOperationData(currentTodayWorkArea));
    }

    private void FetchHistoryData()
    {
        if (string.IsNullOrEmpty(currentHistoryWorkArea) || string.IsNullOrEmpty(currentHistoryDate)) return;

        // 调取假接口获取历史报表信息 
        StartCoroutine(MockApiGetHistoryFaultData(currentHistoryWorkArea, currentHistoryDate));
        StartCoroutine(MockApiGetHistoryOperationData(currentHistoryWorkArea, currentHistoryDate));
    }

    private void UpdateTodayFaultUI()
    {
        FillFaultTable(todayFaultTableObj, todayFaultList);
        Debug.Log($"[当日] 故障数据更新: {todayFaultList.Count} 条");
    }

    private void UpdateTodayOperationUI()
    {
        FillOperationTable(todayOperationTableObj, todayOperationList);
        Debug.Log($"[当日] 操作数据更新: {todayOperationList.Count} 条");
    }

    private void UpdateHistoryFaultUI()
    {
        FillFaultTable(historyFaultTableObj, historyFaultList);
        Debug.Log($"[历史] 故障数据更新: {historyFaultList.Count} 条");
    }

    private void UpdateHistoryOperationUI()
    {
        FillOperationTable(historyOperationTableObj, historyOperationList);
        Debug.Log($"[历史] 操作数据更新: {historyOperationList.Count} 条");
    }

    private void FillFaultTable(GameObject tableObj, List<FaultData> list)
    {
        if (tableObj == null) return;
        var table = tableObj.GetComponent<UITable>();
        if (table == null) return;

        table.ClearData();
        foreach (var data in list)
        {
            table.AddRow(data.slotNumber, data.occurTime, data.faultType);
        }
    }

    private void FillOperationTable(GameObject tableObj, List<OperationData> list)
    {
        if (tableObj == null) return;
        var table = tableObj.GetComponent<UITable>();
        if (table == null) return;

        table.ClearData();
        foreach (var data in list)
        {
            table.AddRow(data.slotNumber, data.operationTime, data.operationType);
        }
    }

    #endregion

    #region -- 假接口 (Mock API) 等待真实 API 接入 --

    private IEnumerator MockApiGetTodayFaultData(string workArea)
    {
        yield return new WaitForSeconds(1.0f); // 模拟网络耗时，增加等待时间确保UI初始化
        todayFaultList = new List<FaultData> 
        { 
            new FaultData { slotNumber = "2031", occurTime = "00:05:20", faultType = "电压异常" },
            new FaultData { slotNumber = "2031", occurTime = "01:03:29", faultType = "电压异常" },
            new FaultData { slotNumber = "2031", occurTime = "11:12:23", faultType = "下料故障" }
        };
        UpdateTodayFaultUI();
    }

    private IEnumerator MockApiGetTodayOperationData(string workArea)
    {
        yield return new WaitForSeconds(1.0f);
        todayOperationList = new List<OperationData> 
        { 
            new OperationData { slotNumber = "2031", operationTime = "02:49:12", operationType = "手动阳升" },
            new OperationData { slotNumber = "2031", operationTime = "09:11:23", operationType = "手动阳升" },
            new OperationData { slotNumber = "2031", operationTime = "10:12:12", operationType = "手动小下料" }
        };
        UpdateTodayOperationUI();
    }

    private IEnumerator MockApiGetHistoryFaultData(string workArea, string date)
    {
        yield return new WaitForSeconds(1.0f);
        historyFaultList = new List<FaultData> 
        { 
            new FaultData { slotNumber = "2031", occurTime = "17:11:09", faultType = "电压异常" },
            new FaultData { slotNumber = "2031", occurTime = "00:05:20", faultType = "电压异常" },
            new FaultData { slotNumber = "2031", occurTime = "01:03:29", faultType = "下料故障" }
        };
        UpdateHistoryFaultUI();
    }

    private IEnumerator MockApiGetHistoryOperationData(string workArea, string date)
    {
        yield return new WaitForSeconds(1.0f);
        historyOperationList = new List<OperationData> 
        { 
            new OperationData { slotNumber = "2031", operationTime = "14:25:34", operationType = "手动阳降" },
            new OperationData { slotNumber = "2031", operationTime = "19:38:09", operationType = "手动阳升" },
            new OperationData { slotNumber = "2031", operationTime = "20:22:48", operationType = "手动阳升" }
        };
        UpdateHistoryOperationUI();
    }

    #endregion
}
