import sys

path = r'd:\AehtTemp1103\AehtTemp\Assets\Scripts\UI\CanshuXiuGaiController.cs'

with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('r.timestamp', 'r.modifyTime')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

