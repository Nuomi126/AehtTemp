using System.Collections.Generic;
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
    [SerializeField] private Transform xinzengInputRoot;
    [SerializeField] private GameObject xinzengRowTemplate;
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
        BindTabs();
        BindSelectors();
        BindInputFields();
        RefreshAll();
        ShowPage(PageType.Dangri);
        ShowXinzengCategory(currentXinzengCategory);
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
                    var currentRows = GetCurrentXinzengRows();
                    if (currentRows != null && editingRowIndex < currentRows.Count)
                    {
                        var row = currentRows[editingRowIndex];
                        row.modifiedValue = val;

                        if (dataManager != null)
                            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, row.parameterName, row.currentValue, val);

                        RefreshXinzeng();
                        RefreshDangri();
                        RefreshLishi();
                    }
                }
                xinzengPopupInput.text = "";
                xinzengPopupInput.gameObject.SetActive(false);
                editingRowIndex = -1;
            });
        }
    }

    private List<CanshuXiuGaiDataManager.EditableParameterRow> GetCurrentXinzengRows()
    {
        switch (currentXinzengCategory)
        {
            case XinzengCategoryType.Jichu:
                return xinzengRowsJichu;
            case XinzengCategoryType.Dingrongqi:
                return xinzengRowsDingrongqi;
            case XinzengCategoryType.Teshu:
                return xinzengRowsTeshu;
            case XinzengCategoryType.Qita:
            default:
                return xinzengRows;
        }
    }

    private void SetupTableClick(UITable table)
    {
        if (table == null)
            return;

        table.supportSelect = true;
        table.onTableClick = (row, col, cell) =>
        {
            if (currentPage != PageType.Xinzeng || col != 3 || row < 0)
                return;

            var rows = GetCurrentXinzengRows();
            if (rows == null || row >= rows.Count)
                return;

            editingRowIndex = row;
            if (xinzengPopupInput != null)
            {
                xinzengPopupInput.gameObject.SetActive(true);
                xinzengPopupInput.transform.SetAsLastSibling();
                xinzengPopupInput.text = rows[row].modifiedValue ?? string.Empty;
                xinzengPopupInput.transform.position = Input.mousePosition;
                xinzengPopupInput.Select();
                xinzengPopupInput.ActivateInputField();
            }
        };
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
        var rows = dataManager.GetDailyRecords(currentDangriWorkArea, null);
        FillTable(dangrixiugaiTable, rows);
    }

    private void RefreshLishi()
    {
        if (dataManager == null || lishijiluTable == null) return;
        var rows = dataManager.GetHistoryRecords(currentLishiWorkArea, currentLishiDate);
        FillTable(lishijiluTable, rows);
    }

    private void InitStaticXinzengRows(List<CanshuXiuGaiDataManager.EditableParameterRow> rows, string[] paramNames)
    {
        if (rows == null)
            return;

        if (rows.Count == 0 || rows.Count != paramNames.Length)
        {
            rows.Clear();
            foreach (var p in paramNames)
            {
                rows.Add(new CanshuXiuGaiDataManager.EditableParameterRow
                {
                    parameterName = p,
                    currentValue = "0",
                    modifiedValue = string.Empty,
                    modifier = dataManager != null ? dataManager.CurrentUserName : "管理员",
                    modifyTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    workArea = currentXinzengWorkArea,
                    slotNumber = currentXinzengSlot
                });
            }
        }
        foreach (var r in rows)
        {
            r.workArea = currentXinzengWorkArea;
            r.slotNumber = currentXinzengSlot;
            if (string.IsNullOrWhiteSpace(r.currentValue)) r.currentValue = "0";
            if (string.IsNullOrWhiteSpace(r.modifier)) r.modifier = dataManager != null ? dataManager.CurrentUserName : "管理员";
            if (string.IsNullOrWhiteSpace(r.modifyTime)) r.modifyTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    private void RefreshXinzeng()
    {
        if (dataManager == null)
            return;

        InitStaticXinzengRows(xinzengRowsJichu, new[] { "设定电压", "基准间隔", "氟盐量", "母线基准" });
        InitStaticXinzengRows(xinzengRowsDingrongqi, new[] { "一次氧化铝下料量", "一次氟盐下料量" });
        InitStaticXinzengRows(xinzengRowsTeshu, new[] { "自动下料", "定时下料", "停控", "停料", "平控", "自控" });
        InitStaticXinzengRows(xinzengRows, new[]
        {
            "参数1", "参数2", "参数3", "参数4", "参数5", "参数6", "参数7", "参数8", "参数9", "参数10",
            "参数11", "参数12", "参数13", "参数14", "参数15", "参数16", "参数17", "参数18", "参数19", "参数20"
        });

        var previousValues = new Dictionary<string, string>();
        if (xinzengRows != null)
        {
            foreach (var row in xinzengRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.parameterName))
                    continue;
                previousValues[row.parameterName] = row.modifiedValue;
            }
        }

        xinzengRows = dataManager.GetEditableRows(currentXinzengWorkArea, currentXinzengSlot, 20);
        for (int i = 0; i < xinzengRows.Count && i < 20; i++)
        {
            xinzengRows[i].currentValue = xinzengRows[i].currentValue ?? "0";
            if (previousValues.TryGetValue(xinzengRows[i].parameterName, out string oldValue))
                xinzengRows[i].modifiedValue = oldValue;
            else
                xinzengRows[i].modifiedValue = xinzengRows[i].modifiedValue ?? string.Empty;
        }

        if (currentXinzengCategory == XinzengCategoryType.Jichu)
            FillXinzengTable(xinzengTableJichu, xinzengRowsJichu);
        else if (currentXinzengCategory == XinzengCategoryType.Dingrongqi)
            FillXinzengTable(xinzengTableDingrongqi, xinzengRowsDingrongqi);
        else if (currentXinzengCategory == XinzengCategoryType.Teshu)
            FillXinzengTable(xinzengTableTeshu, xinzengRowsTeshu);
        else
            FillXinzengTable(xinzengTable, xinzengRows);

        UpdateSlotText();
    }

    public void ApplyXinzengModifiedValue(int rowIndex, string value)
    {
        var rows = GetCurrentXinzengRows();
        if (rows == null || rowIndex < 0 || rowIndex >= rows.Count)
            return;

        rows[rowIndex].modifiedValue = value;

        if (dataManager != null)
        {
            var row = rows[rowIndex];
            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, row.parameterName, row.currentValue, value);
        }
    }

    private void UpdateSlotText()
    {
        if (xinzengSlotText != null)
        {
            xinzengSlotText.text = currentXinzengSlot;
        }
    }

    private void BuildInputRows(List<CanshuXiuGaiDataManager.EditableParameterRow> rows, Transform root, GameObject template)
    {
        if (root == null || template == null || rows == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            GameObject go = Instantiate(template, root);
            go.SetActive(true);

            var index = go.transform.Find("Index")?.GetComponent<TMP_Text>();
            var workArea = go.transform.Find("WorkArea")?.GetComponent<TMP_Text>();
            var slot = go.transform.Find("Slot")?.GetComponent<TMP_Text>();
            var parameter = go.transform.Find("Parameter")?.GetComponent<TMP_Text>();
            var current = go.transform.Find("CurrentValue")?.GetComponent<TMP_Text>();
            var input = go.transform.Find("ModifiedValue")?.GetComponent<TMP_InputField>();
            var modifier = go.transform.Find("Modifier")?.GetComponent<TMP_Text>();
            var time = go.transform.Find("ModifyTime")?.GetComponent<TMP_Text>();

            if (index != null) index.text = (i + 1).ToString();
            if (workArea != null) workArea.text = row.workArea;
            if (slot != null) slot.text = row.slotNumber;
            if (parameter != null) parameter.text = row.parameterName;
            if (current != null) current.text = row.currentValue;
            if (modifier != null) modifier.text = row.modifier;
            if (time != null) time.text = row.modifyTime;

            if (input != null)
            {
                input.SetTextWithoutNotify(row.modifiedValue ?? string.Empty);
                int idx = i;
                input.onValueChanged.RemoveAllListeners();
                input.onValueChanged.AddListener(v =>
                {
                    if (idx >= 0 && idx < rows.Count)
                    {
                        rows[idx].modifiedValue = v;
                        if (dataManager != null)
                            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, row.parameterName, row.currentValue, v);
                    }
                });
            }
        }
    }

    private void FillXinzengTable(UITable table, List<CanshuXiuGaiDataManager.EditableParameterRow> rows)
    {
        if (table == null) return;
        table.ClearData();
        if (rows == null) return;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            table.AddRow(new List<string>
            {
                (i + 1).ToString(),
                r.parameterName,
                r.currentValue,
                r.modifiedValue
            });
        }
    }

    private void FillTable(UITable table, List<CanshuXiuGaiDataManager.ParameterModifyRecord> dataRows)
    {
        if (table == null) return;
        table.ClearData();
        if (dataRows == null) return;
        for (int i = 0; i < dataRows.Count; i++)
        {
            var r = dataRows[i];
            table.AddRow(new List<string>
            {
                r.slotNumber,
                (i + 1).ToString(),
                r.parameterName,
                r.currentValue,
                r.modifiedValue,
                r.modifier,
                r.modifyTime
            });
        }
    }

    
}
