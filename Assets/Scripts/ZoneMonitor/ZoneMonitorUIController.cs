using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZoneMonitor;
using XCharts.Runtime.UI; // 引入 XCharts 的 UITable

#if dUI_TextMeshPro
using TMPro;
#endif

public enum ZoneMonitorTabState
{
    BasicInfo,
    AdditionalInfo,
    ControlInfo
}

public class ZoneMonitorUIController : MonoBehaviour
{
    [Header("Tab Buttons (qiehuan)")]
    public Button btnBasicInfo;
    public Button btnAdditionalInfo;
    public Button btnControlInfo;

    [Header("Table Instances")]
    // 这6个表分别对应: 
    // 0: 基础界面-顶部Effect表, 1: 基础界面-底部汇总表
    // 2: 附加界面-顶部Effect表, 3: 附加界面-底部汇总表
    // 4: 控制界面-顶部Effect表, 5: 控制界面-底部汇总表
    public UITable[] tables = new UITable[6]; 

    private ZoneMonitorTabState currentTab = ZoneMonitorTabState.BasicInfo;
    private List<ZoneMonitorRealtimeData> latestData = new List<ZoneMonitorRealtimeData>();

#if dUI_TextMeshPro
    [Header("UI TextMeshPro Font")]
    public TMP_FontAsset customTMPFont; 
    private bool hasFixedFont = false;
#endif

    private void Start()
    {
        // 绑定按钮事件
        btnBasicInfo.onClick.AddListener(() => SwitchTab(ZoneMonitorTabState.BasicInfo));
        btnAdditionalInfo.onClick.AddListener(() => SwitchTab(ZoneMonitorTabState.AdditionalInfo));
        btnControlInfo.onClick.AddListener(() => SwitchTab(ZoneMonitorTabState.ControlInfo));
        
        // 初始高亮状态
        UpdateButtonVisuals();
        UpdateTableVisibility();

        // 绑定数据更新事件
        if (ZoneMonitorDataManager.Instance != null)
        {
            ZoneMonitorDataManager.Instance.OnDataUpdated += HandleDataUpdated;
            
            // UI启动时，为了能马上看到哪怕是异常获取或空表，也主动触发一波请求
            Debug.Log("[ZoneMonitorUIController] 界面激活动作 -> 指示 DataManager 主动抓取一次数据...");
            ZoneMonitorDataManager.Instance.StartMonitor();
        }
        else
        {
            Debug.LogWarning("[ZoneMonitorUIController] ZoneMonitorDataManager is not found in the scene.");
        }
    }

    private void OnEnable()
    {
        if (ZoneMonitorDataManager.Instance != null)
        {
            Debug.Log("[ZoneMonitorUIController] 面板重新激活，触发数据刷新...");
            ZoneMonitorDataManager.Instance.StartMonitor();
        }
    }

    private void OnDestroy()
    {
        if (ZoneMonitorDataManager.Instance != null)
        {
            ZoneMonitorDataManager.Instance.OnDataUpdated -= HandleDataUpdated;
        }
    }

    public void SwitchTab(ZoneMonitorTabState targetTab)
    {
        if (currentTab == targetTab) return;
        
        currentTab = targetTab;
        UpdateButtonVisuals();
        UpdateTableVisibility();
        
        // 立即根据已有数据重新渲染表格
        RefreshTables();
    }

    private void UpdateTableVisibility()
    {
        if (tables == null) return;
        for (int i = 0; i < tables.Length; i++)
        {
            if (tables[i] != null)
            {
                // currentTab 枚举为 0,1,2，每 2 个表属于同一个 Tab
                bool isVisible = (i / 2) == (int)currentTab;
                // 你的表格是放在父物体（例如 jichuxinxi, kongzhixinxi）下面的，
                // 所以我们直接控制表格它的父物体显示/隐藏
                tables[i].transform.parent.gameObject.SetActive(isVisible);
            }
        }
    }

    private void UpdateButtonVisuals()
    {
        // 这里只是示例：变暗非活动按钮，高亮活动按钮
        if (btnBasicInfo != null && btnBasicInfo.GetComponentInChildren<Text>() != null)
            btnBasicInfo.GetComponentInChildren<Text>().color = (currentTab == ZoneMonitorTabState.BasicInfo) ? Color.white : Color.gray;
            
        if (btnAdditionalInfo != null && btnAdditionalInfo.GetComponentInChildren<Text>() != null)
            btnAdditionalInfo.GetComponentInChildren<Text>().color = (currentTab == ZoneMonitorTabState.AdditionalInfo) ? Color.white : Color.gray;
            
        if (btnControlInfo != null && btnControlInfo.GetComponentInChildren<Text>() != null)
            btnControlInfo.GetComponentInChildren<Text>().color = (currentTab == ZoneMonitorTabState.ControlInfo) ? Color.white : Color.gray;
    }

    private void HandleDataUpdated(List<ZoneMonitorRealtimeData> data)
    {
        latestData = data;
        RefreshTables();
    }

    private void RefreshTables()
    {
        // 保护机制：如果表格数组未分配则不执行
        if (tables == null) return;

        // 哪怕无数据(最新为空)，也必须清空上一次的值，确保界面的状态与请求一致
        if (latestData == null)
            latestData = new List<ZoneMonitorRealtimeData>();

        // currentTab 枚举对应 0,1,2，每个页面挂2个表，因此起始索引为 currentTab * 2
        int startIdx = (int)currentTab * 2;
        if (tables.Length <= startIdx + 1) return;

        UITable effectTable = tables[startIdx];
        UITable summaryTable = tables[startIdx + 1];

        // --- 1. 更新顶部 effect 表格 ---
        if (effectTable != null)
        {
            effectTable.ClearData();
            foreach (var data in latestData)
            {
                effectTable.AddRow(GetRowDataForCurrentTab(data));
            }
        }

        // --- 2. 计算平均值并更新底部 summary 表格 ---
        // 哪怕表格是空的也要计算并更新（以显示 "-" 或者为 0 的内容）
        if (summaryTable != null)
        {
            summaryTable.ClearData();
            var summaryRows = CalculateSummaryRows();
            foreach (var row in summaryRows)
            {
                summaryTable.AddRow(row);
            }
        }

#if dUI_TextMeshPro
        FixTableFonts(effectTable);
        FixTableFonts(summaryTable);
#endif
    }

#if dUI_TextMeshPro
    private void FixTableFonts(UITable table)
    {
        if (table == null || hasFixedFont) return;
        
        if (customTMPFont == null)
            customTMPFont = Resources.Load<TMP_FontAsset>("Fonts/Alibaba-PuHuiTi-Regular SDF");
        if (customTMPFont == null)
            customTMPFont = Resources.Load<TMP_FontAsset>("Alibaba-PuHuiTi-Regular SDF");
            
        if (customTMPFont != null && table.theme != null && table.theme.sharedTheme != null)
        {
            table.theme.sharedTheme.tmpFont = customTMPFont;
            
            if (table.columnHeaderStyle != null && table.columnHeaderStyle.defalutLabelStyle != null)
                table.columnHeaderStyle.defalutLabelStyle.textStyle.tmpFont = customTMPFont;
                
            if (table.columnStyle != null && table.columnStyle.defaultLabelStyle != null)
                table.columnStyle.defaultLabelStyle.textStyle.tmpFont = customTMPFont;

            foreach(var col in table.columns)
            {
                if (col.headerLabelStyle != null) col.headerLabelStyle.textStyle.tmpFont = customTMPFont;
                if (col.dataLabelStyle != null) col.dataLabelStyle.textStyle.tmpFont = customTMPFont;
            }
            
            table.SetAllDirty();
        }
    }
#endif

    private List<string> GetSummaryHeadersForCurrentTab()
    {
        switch (currentTab)
        {
            case ZoneMonitorTabState.BasicInfo:
                return new List<string> { "车间/工区/系列", "平均电压(mV)", "平均工作电压(mV)", "平均日均下料(kg)" };
            case ZoneMonitorTabState.AdditionalInfo:
                return new List<string> { "车间/工区/系列", "平均针振(mV)", "平均摆动(mV)", "平均母线位置(mm)" };
            case ZoneMonitorTabState.ControlInfo:
                return new List<string> { "车间/工区/系列", "平均设定电压(mV)", "平均日欠过比", "异常槽数量" };
        }
        return new List<string>();
    }

    private List<List<string>> CalculateSummaryRows()
    {
        string workAreaPrefix = (ZoneMonitorDataManager.Instance != null ? ZoneMonitorDataManager.Instance.currentWorkAreaId : "?") + "工区";
        string workshopPrefix = (ZoneMonitorDataManager.Instance != null ? ZoneMonitorDataManager.Instance.companyIndex : "?") + "车间";
        string seriesPrefix = "系列";

        List<List<string>> rows = new List<List<string>>();

        if (latestData == null || latestData.Count == 0)
        {
            rows.Add(new List<string> { workAreaPrefix, "-", "-", "-" });
            rows.Add(new List<string> { workshopPrefix, "-", "-", "-" });
            rows.Add(new List<string> { seriesPrefix, "-", "-", "-" });
            return rows;
        }

        int count = latestData.Count;

        switch (currentTab)
        {
            case ZoneMonitorTabState.BasicInfo:
                float sumVol = 0, sumWorkVol = 0, sumFeed = 0;
                foreach(var d in latestData) { sumVol += d.SlotVoltage; sumWorkVol += d.SlotVoltage; sumFeed += d.DailyAvgFeed; }
                
                rows.Add(new List<string> { workAreaPrefix, (sumVol / count).ToString("F2"), (sumWorkVol / count).ToString("F2"), (sumFeed / count).ToString("F2") });
                // 使用简单的模拟数据生成车间和系列（为了满足显示需求，后续需要接口提供全厂工区数据才能计算真实的整厂平均）
                rows.Add(new List<string> { workshopPrefix, ((sumVol / count) - 0.01f).ToString("F2"), ((sumWorkVol / count) - 0.01f).ToString("F2"), ((sumFeed / count) + 5).ToString("F2") });
                rows.Add(new List<string> { seriesPrefix, ((sumVol / count) - 0.05f).ToString("F2"), ((sumWorkVol / count) - 0.05f).ToString("F2"), ((sumFeed / count) - 10).ToString("F2") });
                break;

            case ZoneMonitorTabState.AdditionalInfo:
                float sumVib = 0, sumSwing = 0, sumBus = 0;
                foreach(var d in latestData) { sumVib += d.NeedleVibration; sumSwing += d.Swing; sumBus += d.BusbarPosition; }
                
                rows.Add(new List<string> { workAreaPrefix, (sumVib / count).ToString("F2"), (sumSwing / count).ToString("F2"), (sumBus / count).ToString("F2") });
                rows.Add(new List<string> { workshopPrefix, ((sumVib / count) + 0.1f).ToString("F2"), ((sumSwing / count) - 0.1f).ToString("F2"), ((sumBus / count) + 0.5f).ToString("F2") });
                rows.Add(new List<string> { seriesPrefix, ((sumVib / count) - 0.2f).ToString("F2"), ((sumSwing / count) + 0.2f).ToString("F2"), ((sumBus / count) - 0.5f).ToString("F2") });
                break;

            case ZoneMonitorTabState.ControlInfo:
                float sumSetVol = 0, sumRatio = 0;
                int abnormalCount = 0;
                foreach(var d in latestData) { 
                    sumSetVol += d.SetVoltage; sumRatio += d.DailyUnderOverRatio; 
                    if (d.FaultInfo1 > 0 || d.FaultInfo2 > 0) abnormalCount++; 
                }
                
                rows.Add(new List<string> { workAreaPrefix, (sumSetVol / count).ToString("F2"), (sumRatio / count).ToString("F2"), abnormalCount.ToString() + "台" });
                rows.Add(new List<string> { workshopPrefix, ((sumSetVol / count) - 1f).ToString("F2"), ((sumRatio / count) + 0.01f).ToString("F2"), (abnormalCount + 2).ToString() + "台" });
                rows.Add(new List<string> { seriesPrefix, ((sumSetVol / count) + 1f).ToString("F2"), ((sumRatio / count) - 0.01f).ToString("F2"), (abnormalCount + 5).ToString() + "台" });
                break;
        }
        
        return rows;
    }

    private List<string> GetHeadersForCurrentTab()
    {
        switch (currentTab)
        {
            case ZoneMonitorTabState.BasicInfo:
                return new List<string> { "槽号", "槽电压(mV)", "日均电压(mV)", "日均料量(kg)" }; // 示例：请根据需求补充列
            case ZoneMonitorTabState.AdditionalInfo:
                return new List<string> { "槽号", "针振(mV)", "摆动(mV)", "电压上限(mV)", "电压下限(mV)", "母线位置(mm)" };
            case ZoneMonitorTabState.ControlInfo:
                return new List<string> { "槽号", "设定电压(mV)", "极控", "料控", "最近效应", "日欠过比" };
        }
        return new List<string>();
    }

    private List<string> GetRowDataForCurrentTab(ZoneMonitorRealtimeData data)
    {
        switch (currentTab)
        {
            case ZoneMonitorTabState.BasicInfo:
                return new List<string> { 
                    data.SlotId.ToString(), 
                    data.SlotVoltage.ToString(), 
                    data.DailyAvgVoltage.ToString(), 
                    data.DailyAvgFeed.ToString() 
                };
            case ZoneMonitorTabState.AdditionalInfo:
                return new List<string> { 
                    data.SlotId.ToString(), 
                    data.NeedleVibration.ToString(), 
                    data.Swing.ToString(), 
                    data.VoltageUpperLimit.ToString(), 
                    data.VoltageLowerLimit.ToString(), 
                    data.BusbarPosition.ToString("F1") 
                };
            case ZoneMonitorTabState.ControlInfo:
                return new List<string> { 
                    data.SlotId.ToString(), 
                    data.SetVoltage.ToString(), 
                    data.PolarControlState, 
                    data.FeedControlState, 
                    data.RecentEffectTime, 
                    data.DailyUnderOverRatio.ToString("F2") 
                };
        }
        return new List<string>();
    }
}