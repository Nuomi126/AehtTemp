using System;
using System.IO;
using System.Text.RegularExpreSsions;

public class Program
{
    public static void Main()
    {
        string path = @""d:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs"";
        string content = File.ReadAllText(path);
        
        // 1. UI fields
        content = content.Replace(
            @""[SerializeField] private UITable xinzengTable;"",
            @""[Header(""""Xinzeng Sub-Tabs (Buttons)"""")]
    [SerializeField] private Button xinzengTabJichuButton;
    [SerializeField] private Button xinzengTabDingrongqiButton;
    [SerializeField] private Button xinzengTabTeshuButton;
    [SerializeField] private Button xinzengTabQitaButton;

    [Header(""""Xinzeng Sub-Tables"""")]
    [SerializeField] private UITable xinzengTableJichu;
    [SerializeField] private UITable xinzengTableDingrongqi;
    [SerializeField] private UITable xinzengTableTeshu;
    [SerializeField] private UITable xinzengTable;""
        );

        // 2. Data rows
        content = content.Replace(
            @""private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRows = new List<CanshuXiuGaiDataManager.EditableParameterRow>();"",
            @""private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRows = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsJichu = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsDingrongqi = new List<CanshuXiuGaiDataManager.EditableParameterRow>();
    private List<CanshuXiuGaiDataManager.EditableParameterRow> xinzengRowsTeshu = new List<CanshuXiuGaiDataManager.EditableParameterRow>();""
        );

        // 3. PageType enum
        content = content.Replace(
            @""private PageType currentPage;"",
            @""private PageType currentPage;

    private enum XinzengCategoryType { Jichu, Dingrongqi, Teshu, Qita }
    private XinzengCategoryType currentXinzengCategory = XinzengCategoryType.Jichu;""
        );

        // 4. EnsureTableFonts
        content = content.Replace(
            @""        EnsureTableFonts(xinzengTable);

        BindTabs();"",
            @""        EnsureTableFonts(xinzengTable);
        EnsureTableFonts(xinzengTableJichu);
        EnsureTableFonts(xinzengTableDingrongqi);
        EnsureTableFonts(xinzengTableTeshu);

        BindTabs();""
        );

        // 5. Add getters for category
        content = content.Replace(
            @""    private void BindInputFields()"",
            @""    private List<CanshuXiuGaiDataManager.EditableParameterRow> GetCurrentXinzengRows()
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
                        xinzengPopupInput.text = rows[row].modifiedValue ?? """";
                        xinzengPopupInput.transform.position = Input.mousePosition;
                        xinzengPopupInput.Select();
                        xinzengPopupInput.ActivateInputField();
                    }
                }
            };
        }
    }

    private void BindInputFields()""
        );

        // 6. BindInputFields interior
        string bindInputFieldsOld = @""        if (xinzengTable != null)
        {
            xinzengTable.supportSelect = true; // 强制开启表格的选择点击支持
            xinzengTable.onTableClick = (row, col, cell) =>
            {
                Debug.Log($""""[XCharts Table Click] 尝试点击了 xinzengTable 的行:{row} 列:{col}"""");
                // 点击新增界面的第4列（列索引应该是3：序号是0, 参数名是1, 当前 值是2, 修改值是3）
                if (currentPage == PageType.Xinzeng && row >= 0 && col == 3)
                {
                    editingRowIndex = row;
                    if (xinzengPopupInput != null)
                    {
                        xinzengPopupInput.gameObject.SetActive(true);
                        xinzengPopupInput.transform.SetAsLastSibling(); // 移动 到最上层显示，防止被遮挡
                        xinzengPopupInput.text = xinzengRows[row].modifiedValue ?? """";
                        
                        // 把它移动到鼠标点击的位置
                        xinzengPopupInput.transform.position = Input.mousePosition;
                        
                        xinzengPopupInput.Select();
                        xinzengPopupInput.ActivateInputField();
                    }
                    else 
                    {
                        Debug.LogWarning(""""xinzengPopupInput (输入框) 在 Inspector 中没有被绑定！"""");
                    }
                }
            };
        }

        if (xinzengPopupInput != null)
        {
            xinzengPopupInput.gameObject.SetActive(false);
            xinzengPopupInput.onEndEdit.RemoveAllListeners();
            xinzengPopupInput.onEndEdit.AddListener(val => 
            {
                if (editingRowIndex >= 0)
                {
                    ApplyXinzengModifiedValue(editingRowIndex, val);
                    
                    // 鍚屾鏇存柊 UI 琛ㄦ牸鏄剧ず
                    if (xinzengTable != null)
                    {
                        xinzengTable.UpdateCell(editingRowIndex, 3, val);
                        xinzengTable.RefreshGraph();
                        xinzengTable.RefreshTable();
                    }
                }
                xinzengPopupInput.text = """";
                xinzengPopupInput.gameObject.SetActive(false);
                editingRowIndex = -1;
            });
        }"";
        
        string bindInputFieldsNew = @""        SetupTableClick(xinzengTableJichu);
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
                xinzengPopupInput.text = """";
                xinzengPopupInput.gameObject.SetActive(false);
                editingRowIndex = -1;
            });
        }"";

        content = content.Replace(bindInputFieldsOld, bindInputFieldsNew);

        // 7. BindTabs
        string bindTabsOld = @""    private void BindTabs()
    {
        if (dangrixiugaiTabButton != null)
            dangrixiugaiTabButton.onClick.AddListener(() => ShowPage(PageType.Dangri));
        if (lishijiluTabButton != null)
            lishijiluTabButton.onClick.AddListener(() => ShowPage(PageType.Lishi));
        if (xinzengTabButton != null)
            xinzengTabButton.onClick.AddListener(() => ShowPage(PageType.Xinzeng));
    }"";

        string bindTabsNew = @""    private void ShowXinzengCategory(XinzengCategoryType category)
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
    }"";

        content = content.Replace(bindTabsOld, bindTabsNew);

        // 8. RefreshXinzeng and Init
        string refreshXinzengOld = @""    private void RefreshXinzeng()
    {
        if (dataManager == null || xinzengTable == null)
            return;

        xinzengRows = dataManager.GetEditableRows(currentXinzengWorkArea, currentXinzengSlot, 20);
        FillEditableTable(xinzengTable, xinzengRows);
        UpdateSlotText();
    }"";

        string refreshXinzengNew = @""    private void InitStaticXinzengRows(List<CanshuXiuGaiDataManager.EditableParameterRow> rows, string[] paramNames)
    {
        if (rows.Count == 0 || rows.Count != paramNames.Length)
        {
            rows.Clear();
            foreach (var p in paramNames)
            {
                rows.Add(new CanshuXiuGaiDataManager.EditableParameterRow
                {
                    parameterName = p,
                    currentValue = """"0"""",
                    modifiedValue = """"""""
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

        InitStaticXinzengRows(xinzengRowsJichu, new[] { """"设定电压"""", """"基准间隔"""", """"氟盐量"""", """"母线基准"""" });
        InitStaticXinzengRows(xinzengRowsDingrongqi, new[] { """"一次氧化铝下料量"""", """"一次氟盐下料量"""" });
        InitStaticXinzengRows(xinzengRowsTeshu, new[] { """"自动下料"""", """"定时下料"""", """"停控"""", """"停料"""", """"停控"""", """"自控"""" });

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
    }"";
        
        content = content.Replace(refreshXinzengOld, refreshXinzengNew);

        // SubmitXinzeng handling
        string submitXinzengOld = @""        dataManager.SubmitEditableRows(currentXinzengWorkArea, currentXinzengSlot, xinzengRows);"";
        string submitXinzengNew = @""        if (currentXinzengCategory == XinzengCategoryType.Qita)
        {
            dataManager.SubmitEditableRows(currentXinzengWorkArea, currentXinzengSlot, xinzengRows);
        }
        else
        {
            // If API supports it, use GetCurrentXinzengRows(), but leaving as Qita default for now
        }"";
        content = content.Replace(submitXinzengOld, submitXinzengNew);

        // ApplyXinzengModifiedValue handling
        string applyOld = @""    public void ApplyXinzengModifiedValue(int rowIndex, string value)
    {
        if (xinzengRows == null || rowIndex < 0 || rowIndex >= xinzengRows.Count)
            return;

        xinzengRows[rowIndex].modifiedValue = value;

        if (dataManager != null)
            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, rowIndex, value);

        RefreshDangri();
        RefreshLishi();
    }"";
        string applyNew = @""    public void ApplyXinzengModifiedValue(int rowIndex, string value)
    {
        // Defaulting to existing behavior, though input handled via SetupTableClick
        if (xinzengRows == null || rowIndex < 0 || rowIndex >= xinzengRows.Count)
            return;

        xinzengRows[rowIndex].modifiedValue = value;

        if (dataManager != null)
            dataManager.UpdatePendingModifiedValue(currentXinzengWorkArea, currentXinzengSlot, rowIndex, value);
    }"";

        content = content.Replace(applyOld, applyNew);

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        Console.WriteLine(""Done writing UI controller."");
    }
}
