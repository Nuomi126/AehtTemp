import sys

path = r'd:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs'

content = '''using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime.UI;

public class CanshuXiuGaiController : MonoBehaviour
{
    [Header("Top Tabs")]
    [SerializeField] private Button dangrixiugaiTabButton;
    [SerializeField] private Button lishijiluTabButton;
    [SerializeField] private Button xinzengTabButton;
    [SerializeField] private GameObject dangrixiugaiPanel;
    [SerializeField] private GameObject lishijiluPanel;
    [SerializeField] private GameObject xinzengPanel;

    [Header("Selectors")]
    [SerializeField] private WorkAreaSelector dangrixiugaiWorkAreaSelector;
    [SerializeField] private WorkAreaSelector lishijiluWorkAreaSelector;
    [SerializeField] private WorkAreaSelector xinzengWorkAreaSelector;
    [SerializeField] private riqSelector lishijiluDateSelector;
    [SerializeField] private Button lishijiluDateButton;
    [SerializeField] private Button xinzengSlotButton;
    [SerializeField] private SlotNumberSelector xinzengSlotSelector;
    [SerializeField] private TextMeshProUGUI xinzengSlotText;

    [Header("Tables")]
    [SerializeField] private UITable dangrixiugaiTable;
    [SerializeField] private UITable lishijiluTable;

    [Header("Xinzeng Sub-Tabs (Buttons)")]
    [SerializeField] private Button xinzengTabJichuButton;
    [SerializeField] private Button xinzengTabDingrongqiButton;
    [SerializeField] private Button xinzengTabTeshuButton;
    [SerializeField] private Button xinzengTabQitaButton;

    [Header("Xinzeng Sub-Tables")]
    [SerializeField] private UITable xinzengTableJichu;
    [SerializeField] private UITable xinzengTableDingrongqi;
    [SerializeField] private UITable xinzengTableTeshu;
    [SerializeField] private UITable xinzengTable;

    [Header("Editable Row Bindings")]
    [SerializeField] private TMP_InputField xinzengPopupInput;
    private int editingRowIndex = -1;

    [Header("Data Manager")]
    [SerializeField] private CanshuXiuGaiDataManager dataManager;

    [Header("Defaults")]
    [SerializeField] private string defaultWorkArea = "1工区";
    [SerializeField] private string defaultSlot = "2001";

    private string currentDangriWorkArea;
    private string currentLishiWorkArea;
    private string currentLishiDate;
    private string currentXinzengWorkArea;
    private string currentXinzengSlot;
    
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRows = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsJichu = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsDingrongqi = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsTeshu = new List<CanshuXiuGaiDataManager.EditableParameterRow>();

    private enum PageType { Dangri, Lishi, Xinzeng }
    private PageType currentPage;

    private enum XinzengCategoryType { Jichu, Dingrongqi, Teshu, Qita }
    private XinzengCategoryType currentXinzengCategory = XinzengCategoryType.Jichu;

    private void Awake()
    {
        currentDangriWorkArea = defaultWorkArea;
        currentLishiWorkArea = defaultWorkArea;
        currentLishiDate = System.DateTime.Now.ToString("yyyyMMdd");
        currentXinzengWorkArea = defaultWorkArea;
        currentXinzengSlot = defaultSlot;
    }

    private void Start()
    {
        EnsureTableFonts(dangrixiugaiTable);
        EnsureTableFonts(lishijiluTable);
        EnsureTableFonts(xinzengTable);
        EnsureTableFonts(xinzengTableJichu);
        EnsureTableFonts(xinzengTableDingrongqi);
        EnsureTableFonts(xinzengTableTeshu);

        BindTabs();
        BindSelectors();
        BindInputFields();
        RefreshAll();
        ShowPage(PageType.Dangri);
    }

    private void EnsureTableFonts(UITable table)
    {
        if (table == null) return;
        TMP_FontAsset fallbackFont = TMP_Settings.defaultFontAsset;
        if (fallbackFont == null)
        {
            var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.font != null) { fallbackFont = t.font; break; }
            }
        }
        if (fallbackFont != null)
        {
            if (table.theme != null && table.theme.sharedTheme != null)
            {
                if (table.theme.sharedTheme.common != null && table.theme.sharedTheme.common.tmpFont == null) table.theme.sharedTheme.common.tmpFont = fallbackFont;
                if (table.theme.sharedTheme.title != null && table.theme.sharedTheme.title.tmpFont == null) table.theme.sharedTheme.title.tmpFont = fallbackFont;
            }
            if (table.rowHeaderStyle != null && table.rowHeaderStyle.labelStyle != null && table.rowHeaderStyle.labelStyle.textStyle != null && table.rowHeaderStyle.labelStyle.textStyle.tmpFont == null) table.rowHeaderStyle.labelStyle.textStyle.tmpFont = fallbackFont;
            if (table.columnHeaderStyle != null && table.columnHeaderStyle.defalutLabelStyle != null && table.columnHeaderStyle.defalutLabelStyle.textStyle != null && table.columnHeaderStyle.defalutLabelStyle.textStyle.tmpFont == null) table.columnHeaderStyle.defalutLabelStyle.textStyle.tmpFont = fallbackFont;
            if (table.columns != null)
            {
                foreach (var col in table.columns)
                {
                    if (col != null && col.dataLabelStyle != null && col.dataLabelStyle.textStyle != null && col.dataLabelStyle.textStyle.tmpFont == null) col.dataLabelStyle.textStyle.tmpFont = fallbackFont;
                    if (col != null && col.headerLabelStyle != null && col.headerLabelStyle.textStyle != null && col.headerLabelStyle.textStyle.tmpFont == null) col.headerLabelStyle.textStyle.tmpFont = fallbackFont;
                }
            }
        }
    }

    private List<CanshuXiuGaiDataManager.EditableParameterRow> GetCurrentXinzengRows()
    {
        switch (currentXinzengCategory)
        {
            case XinzengCategoryType.Jichu: return xinzengRowsJichu;
            case XinzengCategoryType.Dingrongqi: return xinzengRowsDingrongqi;
            case XinzengCategoryType.Teshu: return xinzengRowsTeshu;
            case XinzengCategoryType.Qita:
            default: return xinzengRows;
        }
    }

    private UITable GetCurrentXinzengTable()
    {
        switch (currentXinzengCategory)
        {
            case XinzengCategoryType.Jichu: return xinzengTableJichu;
            case XinzengCategoryType.Dingrongqi: return xinzengTableDingrongqi;
            case XinzengCategoryType.Teshu: return xinzengTableTeshu;
            case XinzengCategoryType.Qita:
            default: return xinzengTable;
        }
    }

    private void SetupTableClick(UITable table)
    {
        if (table != null)
        {
            table.supportSelect = true;
            table.onTableClick = (row, col, cell) =>
            {
                if (currentPage == PageType.Xinzeng && row >= 0 && col == 3)
                {
                    editingRowIndex = row;
                    var rows = GetCurrentXinzengRows();
                    if (xinzengPopupInput != null && rows != null && row < rows.Count)
                    {
                        xinzengPopupInput.gameObject.SetActive(true);
                        xinzengPopupInput.transform.SetAsLastSibling();
                        xinzengPopupInput.text = rows[row].modifiedValue ?? "";
                        xinzengPopupInput.transform.position = UnityEngine.Input.mousePosition;
                        xinzengPopupInput.Select();
                        xinzengPopupInput.ActivateInputField();
                    }
                }
            };
        }
    }

    private void BindInputFields()
    {
        SetupTableClick(xinzengTableJichu);
        SetupTableClick(xinzengTableDingrongqi);
        SetupTableClick(xinzengTableTeshu);
        SetupTableClick(xinzengTable);

        if (xinzengPopupInput != null)
        {
            xinzengPopupInput.gameObject.SetActive(false);
            xinzengPopupInput.onEndEdit.RemoveAllListeners();
            xinzengPopupInput.onEndEdit.AddListener(val => 
            {
                if (editingRowIndex >= 0)
                {
                    var rows = GetCurrentXinzengRows();
                    var table = GetCurrentXinzengTable();
                    if (rows != null && editingRowIndex < rows.Count)
                    {
                        rows[editingRowIndex].modifiedValue = val;
                        // 记录临时修改并同步
                        if (currentXinzengCategory == XinzengCategoryType.Qita && dataManager != null)
                        {
                            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, editingRowIndex, val);
                        }

                        if (table != null)
                        {
                            table.UpdateCell(editingRowIndex, 3, val);
                            table.RefreshGraph();
                            table.RefreshTable();
                        }
                    }
                    RefreshDangri();
                    RefreshLishi();
                }
                xinzengPopupInput.text = "";
                xinzengPopupInput.gameObject.SetActive(false);
                editingRowIndex = -1;
            });
        }
    }

    private void ShowXinzengCategory(XinzengCategoryType category)
    {
        currentXinzengCategory = category;
        if (xinzengTableJichu != null) xinzengTableJichu.gameObject.SetActive(category == XinzengCategoryType.Jichu);
        if (xinzengTableDingrongqi != null) xinzengTableDingrongqi.gameObject.SetActive(category == XinzengCategoryType.Dingrongqi);
        if (xinzengTableTeshu != null) xinzengTableTeshu.gameObject.SetActive(category == XinzengCategoryType.Teshu);
        if (xinzengTable != null) xinzengTable.gameObject.SetActive(category == XinzengCategoryType.Qita);

        RefreshXinzeng();
    }

    private void BindTabs()
    {
        if (dangrixiugaiTabButton != null)
            dangrixiugaiTabButton.onClick.AddListener(() => ShowPage(PageType.Dangri));
        if (lishijiluTabButton != null)
            lishijiluTabButton.onClick.AddListener(() => ShowPage(PageType.Lishi));
        if (xinzengTabButton != null)
            xinzengTabButton.onClick.AddListener(() => ShowPage(PageType.Xinzeng));

        if (xinzengTabJichuButton != null) xinzengTabJichuButton.onClick.AddListener(() => ShowXinzengCategory(XinzengCategoryType.Jichu));
        if (xinzengTabDingrongqiButton != null) xinzengTabDingrongqiButton.onClick.AddListener(() => ShowXinzengCategory(XinzengCategoryType.Dingrongqi));
        if (xinzengTabTeshuButton != null) xinzengTabTeshuButton.onClick.AddListener(() => ShowXinzengCategory(XinzengCategoryType.Teshu));
        if (xinzengTabQitaButton != null) xinzengTabQitaButton.onClick.AddListener(() => ShowXinzengCategory(XinzengCategoryType.Qita));
    }

    private void BindSelectors()
    {
        if (dangrixiugaiWorkAreaSelector != null)
            dangrixiugaiWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(v => { currentDangriWorkArea = v; RefreshDangri(); });

        if (lishijiluWorkAreaSelector != null)
            lishijiluWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(v => { currentLishiWorkArea = v; RefreshLishi(); });

        if (xinzengWorkAreaSelector != null)
        {
            xinzengWorkAreaSelector.OnWorkAreaConfirmedEvent.AddListener(v =>
            {
                currentXinzengWorkArea = v;
                RefreshXinzeng();
            });
        }

        if (lishijiluDateSelector != null)
            lishijiluDateSelector.OnDateConfirmed.AddListener(v => { currentLishiDate = v; RefreshLishi(); });

        if (lishijiluDateButton != null && lishijiluDateSelector != null)
        {
            var text = lishijiluDateButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) lishijiluDateSelector.selectedDisplayText = text;
            lishijiluDateButton.onClick.AddListener(lishijiluDateSelector.OpenSelectorPanel);
        }

        if (xinzengSlotButton != null)
        {
            var slotText = xinzengSlotButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (slotText != null)
                xinzengSlotText = slotText;

            if (xinzengSlotSelector != null)
            {
                xinzengSlotButton.onClick.AddListener(() => {
                    xinzengSlotSelector.OpenSelectorPanel();
                });
                xinzengSlotSelector.OnSlotConfirmed.AddListener(v => 
                {
                    currentXinzengSlot = v;
                    UpdateSlotText();
                    if (currentPage == PageType.Xinzeng)
                        RefreshXinzeng();
                });
            }
            UpdateSlotText();
        }
    }

    public void SubmitXinzeng()
    {
        if (dataManager == null)
            return;

        if (currentXinzengCategory == XinzengCategoryType.Qita)
        {
            dataManager.SubmitEditableRows(currentXinzengWorkArea, currentXinzengSlot, xinzengRows);
        }
        
        RefreshDangri();
        RefreshLishi();
        RefreshXinzeng();
    }

    private void ShowPage(PageType page)
    {
        currentPage = page;
        if (dangrixiugaiPanel != null) dangrixiugaiPanel.SetActive(page == PageType.Dangri);
        if (lishijiluPanel != null) lishijiluPanel.SetActive(page == PageType.Lishi);
        if (xinzengPanel != null) xinzengPanel.SetActive(page == PageType.Xinzeng);
        RefreshCurrentPage();
    }

    private void RefreshCurrentPage()
    {
        if (currentPage == PageType.Dangri) RefreshDangri();
        else if (currentPage == PageType.Lishi) RefreshLishi();
        else RefreshXinzeng();
    }

    private void RefreshAll()
    {
        RefreshDangri();
        RefreshLishi();
        RefreshXinzeng();
    }

    private void RefreshDangri()
    {
        if (dataManager == null || dangrixiugaiTable == null) return;
        var rows = dataManager.GetDangriRows(currentDangriWorkArea, null);
        FillTable(dangrixiugaiTable, rows);
    }

    private void RefreshLishi()
    {
        if (dataManager == null || lishijiluTable == null) return;
        var rows = dataManager.GetLishiRows(currentLishiWorkArea, currentLishiDate);
        FillTable(lishijiluTable, rows);
    }

    private void InitStaticXinzengRows(List<CanshuXiuGaiDataManager.EditableParameterRow> rows, string[] paramNames)
    {
        if (rows.Count == 0 || rows.Count != paramNames.Length)
        {
            rows.Clear();
            foreach (var p in paramNames)
            {
                rows.Add(new CanshuXiuGaiDataManager.EditableParameterRow
                {
                    parameterName = p,
                    currentValue = "0",
                    modifiedValue = ""
                });
            }
        }
        foreach (var r in rows)
        {
            r.workArea = currentXinzengWorkArea;
            r.slotNumber = currentXinzengSlot;
        }
    }

    private void RefreshXinzeng()
    {
        if (dataManager != null)
            xinzengRows = dataManager.GetEditableRows(currentXinzengWorkArea, currentXinzengSlot, 20);

        InitStaticXinzengRows(xinzengRowsJichu, new[] { "设定电压", "基准间隔", "氟盐量", "母线基准" });
        InitStaticXinzengRows(xinzengRowsDingrongqi, new[] { "一次氧化铝下料量", "一次氟盐下料量" });
        InitStaticXinzengRows(xinzengRowsTeshu, new[] { "自动下料", "定时下料", "停控", "停料", "平控", "自控" });

        switch (currentXinzengCategory)
        {
            case XinzengCategoryType.Jichu:
                if (xinzengTableJichu != null) FillEditableTable(xinzengTableJichu, xinzengRowsJichu);
                break;
            case XinzengCategoryType.Dingrongqi:
                if (xinzengTableDingrongqi != null) FillEditableTable(xinzengTableDingrongqi, xinzengRowsDingrongqi);
                break;
            case XinzengCategoryType.Teshu:
                if (xinzengTableTeshu != null) FillEditableTable(xinzengTableTeshu, xinzengRowsTeshu);
                break;
            case XinzengCategoryType.Qita:
            default:
                if (xinzengTable != null) FillEditableTable(xinzengTable, xinzengRows);
                break;
        }

        UpdateSlotText();
    }

    public void ApplyXinzengModifiedValue(int rowIndex, string value)
    {
        if (xinzengRows == null || rowIndex < 0 || rowIndex >= xinzengRows.Count)
            return;

        xinzengRows[rowIndex].modifiedValue = value;

        if (dataManager != null)
            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, rowIndex, value);
    }

    private void UpdateSlotText()
    {
        if (xinzengSlotText != null)
        {
            xinzengSlotText.text = currentXinzengSlot;
        }
    }

    private void FillTable(UITable table, List<CanshuXiuGaiDataManager.ParameterRecordRow> dataRows)
    {
        if (table == null) return;
        table.ClearData();
        for (int i = 0; i < dataRows.Count; i++)
        {
            var r = dataRows[i];
            table.AddRow();
            table.UpdateCell(i, 0, (i + 1).ToString());
            table.UpdateCell(i, 1, r.workArea);
            table.UpdateCell(i, 2, r.slotNumber);
            table.UpdateCell(i, 3, r.parameterName);
            table.UpdateCell(i, 4, r.modifeidValue);
            table.UpdateCell(i, 5, r.timestamp);
        }
        table.RefreshGraph();
        table.RefreshTable();
    }

    private void FillEditableTable(UITable table, List<CanshuXiuGaiDataManager.EditableParameterRow> dataRows)
    {
        if (table == null) return;
        table.ClearData();
        for (int i = 0; i < dataRows.Count; i++)
        {
            var r = dataRows[i];
            table.AddRow();
            table.UpdateCell(i, 0, (i + 1).ToString());
            table.UpdateCell(i, 1, r.parameterName);
            table.UpdateCell(i, 2, r.currentValue);
            table.UpdateCell(i, 3, r.modifiedValue);
        }
        table.RefreshGraph();
        table.RefreshTable();
    }
}
'''

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
