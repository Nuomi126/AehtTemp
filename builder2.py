import sys

path = r'd:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs'

with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('private void FillTable(UITable table, List<CanshuXiuGaiDataManager.ParameterRecordRow> dataRows)', 'private void FillTable(UITable table, List<CanshuXiuGaiDataManager.ParameterModifyRecord> dataRows)')
text = text.replace('r.modifeidValue', 'r.modifiedValue')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

