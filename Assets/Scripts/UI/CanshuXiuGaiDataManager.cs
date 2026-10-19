using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CanshuXiuGaiDataManager : MonoBehaviour
{
    [Serializable]
    public class ParameterModifyRecord
    {
        public string workArea;
        public string slotNumber;
        public string parameterName;
        public string currentValue;
        public string modifiedValue;
        public string modifier;
        public string modifyTime;
        public string modifyDate;
        public string sourceType;
    }

    [Serializable]
    public class EditableParameterRow
    {
        public string workArea;
        public string slotNumber;
        public string parameterName;
        public string currentValue;
        public string modifiedValue;
        public string modifier;
        public string modifyTime;
    }

    [Header("Mock Settings")]
    [SerializeField] private bool useMockData = true;
    [SerializeField] private string currentUserName = "管理员";

    private readonly List<ParameterModifyRecord> dailyRecords = new List<ParameterModifyRecord>();
    private readonly List<ParameterModifyRecord> historyRecords = new List<ParameterModifyRecord>();
    private readonly Dictionary<string, ParameterModifyRecord> pendingModifiedRecords = new Dictionary<string, ParameterModifyRecord>();

    public string CurrentUserName => currentUserName;

    private void Awake()
    {
        if (useMockData)
            BuildMockData();
    }

    public void BuildMockData()
    {
        dailyRecords.Clear();
        historyRecords.Clear();

        string[] workAreas = { "1工区", "2工区", "3工区" };
        string[] slots = { "2001", "2002", "2003", "2004" };
        string[] parameters = { "电压", "电流", "温度", "流量" };

        for (int i = 0; i < 12; i++)
        {
            string workArea = workAreas[i % workAreas.Length];
            string slot = slots[i % slots.Length];
            string parameter = parameters[i % parameters.Length];
            string date = DateTime.Now.AddDays(-(i % 5)).ToString("yyyyMMdd");

            var record = new ParameterModifyRecord
            {
                workArea = workArea,
                slotNumber = slot,
                parameterName = parameter,
                currentValue = (100 + i).ToString(),
                modifiedValue = (110 + i).ToString(),
                modifier = i % 2 == 0 ? "张三" : "李四",
                modifyTime = DateTime.Now.AddMinutes(-i * 7).ToString("yyyy-MM-dd HH:mm:ss"),
                modifyDate = date,
                sourceType = "当日"
            };

            dailyRecords.Add(record);
            historyRecords.Add(record);
        }
    }

    public List<ParameterModifyRecord> GetDailyRecords(string workArea, string slotNumber = null)
    {
        return FilterRecords(dailyRecords, workArea, null, slotNumber);
    }

    public List<ParameterModifyRecord> GetHistoryRecords(string workArea, string date, string slotNumber = null)
    {
        return FilterRecords(historyRecords, workArea, date, slotNumber);
    }

    public List<EditableParameterRow> GetEditableRows(string workArea, string slotNumber, int count = 20)
    {
        var rows = new List<EditableParameterRow>();
        string[][] parameterGroups =
        {
            new[] { "第1点加料百分比", "9" },
            new[] { "第2点加料百分比", "5" },
            new[] { "第3点加料百分比", "0" },
            new[] { "第4点加料百分比", "0" },
            new[] { "第5点加料百分比", "5" },
            new[] { "第6点加料百分比", "5" },
            new[] { "第7点加料百分比", "0" },
            new[] { "第8点加料百分比", "0" },
            new[] { "第1点打壳百分比", "24" },
            new[] { "第2点打壳百分比", "24" },
            new[] { "第3点打壳百分比", "24" },
            new[] { "第4点打壳百分比", "24" },
            new[] { "第5点打壳百分比", "24" },
            new[] { "第6点打壳百分比", "24" },
            new[] { "第7点打壳百分比", "0" },
            new[] { "第8点打壳百分比", "0" },
            new[] { "标准系列电流,单位：0.1KA", "4422" },
            new[] { "线条过料率条件与", "2006" },
            new[] { "线条过料率条件与", "2007" },
            new[] { "线条过料率条件与", "2008" },
            new[] { "线条过料率条件与", "2009" }
        };

        int rowCount = Mathf.Min(count, parameterGroups.Length);
        for (int i = 0; i < rowCount; i++)
        {
            rows.Add(new EditableParameterRow
            {
                workArea = workArea,
                slotNumber = slotNumber,
                parameterName = parameterGroups[i][0],
                currentValue = parameterGroups[i][1],
                modifiedValue = string.Empty,
                modifier = currentUserName,
                modifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        return rows;
    }

    public void SubmitEditableRows(string workArea, string slotNumber, IList<EditableParameterRow> rows)
    {
        if (rows == null)
            return;

        string nowText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string today = DateTime.Now.ToString("yyyyMMdd");

        foreach (var row in rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.modifiedValue))
                continue;

            var record = new ParameterModifyRecord
            {
                workArea = workArea,
                slotNumber = slotNumber,
                parameterName = row.parameterName,
                currentValue = row.currentValue,
                modifiedValue = row.modifiedValue,
                modifier = currentUserName,
                modifyTime = nowText,
                modifyDate = today,
                sourceType = "新增"
            };

            string key = BuildRecordKey(workArea, slotNumber, row.parameterName);
            pendingModifiedRecords[key] = record;
            dailyRecords.Add(record);
            historyRecords.Add(record);
        }
    }

    public void UpdatePendingModifiedValue(string workArea, string slotNumber, string parameterName, string currentValue, string modifiedValue)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return;

        string key = BuildRecordKey(workArea, slotNumber, parameterName);
        ParameterModifyRecord record;
        if (!pendingModifiedRecords.TryGetValue(key, out record))
        {
            record = new ParameterModifyRecord
            {
                workArea = workArea,
                slotNumber = slotNumber,
                parameterName = parameterName,
                currentValue = currentValue,
                modifyDate = DateTime.Now.ToString("yyyyMMdd")
            };
            pendingModifiedRecords[key] = record;
            dailyRecords.Add(record);
            historyRecords.Add(record);
        }

        record.currentValue = currentValue;
        record.modifiedValue = modifiedValue;
        record.modifier = currentUserName;
        record.modifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        record.modifyDate = DateTime.Now.ToString("yyyyMMdd");
        record.sourceType = "新增";
    }

    private string GetParameterNameByIndex(int rowIndex)
    {
        string[] names =
        {
            "第1点加料百分比", "第2点加料百分比", "第3点加料百分比", "第4点加料百分比", "第5点加料百分比",
            "第6点加料百分比", "第7点加料百分比", "第8点加料百分比", "第1点打壳百分比", "第2点打壳百分比",
            "第3点打壳百分比", "第4点打壳百分比", "第5点打壳百分比", "第6点打壳百分比", "第7点打壳百分比",
            "第8点打壳百分比", "标准系列电流,单位：0.1KA", "线条过料率条件与", "线条过料率条件与", "线条过料率条件与"
        };

        if (rowIndex >= 0 && rowIndex < names.Length)
            return names[rowIndex];
        return null;
    }

    private string GetCurrentValueByIndex(int rowIndex)
    {
        string[] values =
        {
            "9", "5", "0", "0", "5", "5", "0", "0", "24", "24",
            "24", "24", "24", "24", "0", "0", "4422", "2006", "2007", "2008"
        };

        if (rowIndex >= 0 && rowIndex < values.Length)
            return values[rowIndex];
        return string.Empty;
    }

    private string BuildRecordKey(string workArea, string slotNumber, string parameterName)
    {
        return $"{workArea}|{slotNumber}|{parameterName}";
    }

    private List<ParameterModifyRecord> FilterRecords(List<ParameterModifyRecord> source, string workArea, string date, string slot)
    {
        var result = new List<ParameterModifyRecord>();
        foreach (var record in source)
        {
            if (!string.IsNullOrWhiteSpace(workArea) && !string.Equals(record.workArea, workArea, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(date) && !string.Equals(record.modifyDate, date, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(slot) && !string.Equals(record.slotNumber, slot, StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(record);
        }

        return result;
    }
}
