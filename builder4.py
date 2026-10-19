import sys

path = r'd:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs'

with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('GetDangriRows', 'GetDailyRecords')
text = text.replace('GetLishiRows', 'GetHistoryRecords')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

