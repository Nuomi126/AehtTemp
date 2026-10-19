import sys

path = r'd:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

start_idx = content.find('    private void BindInputFields()')
end_idx = content.find('    private void ShowXinzengCategory', start_idx)

bind_input_fields_new = '''    private List<CanshuXiuGaiDataManager.EditableParameterRow> GetCurrentXinzengRows()
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

'''

content = content[:start_idx] + bind_input_fields_new + content[end_idx:]

# Next, let's also update RefreshXinzeng
start_idx_refresh = content.find('    private void RefreshXinzeng()')
end_idx_refresh_end = content.find('    public void ApplyXinzengModifiedValue', start_idx_refresh)

refresh_xinzeng_new = '''    private void InitStaticXinzengRows(List<CanshuXiuGaiDataManager.EditableParameterRow> rows, string[] paramNames)
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
        InitStaticXinzengRows(xinzengRowsTeshu, new[] { "自动下料", "定时下料", "停控", "停料", "停控", "自控" });

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

'''
content = content[:start_idx_refresh] + refresh_xinzeng_new + content[end_idx_refresh_end:]


with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done")
