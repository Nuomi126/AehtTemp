using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.UI;

namespace XCharts.UI.Demo
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class SingleSlotInfoDailyTable : MonoBehaviour
    {
        [Header("组件")]
        [SerializeField] private UITable m_Table;

        [Header("字体（可选）")]
        [SerializeField] private TMP_FontAsset customTMPFont;

        [Header("样式")]
        [SerializeField] private Color32 headerBgColor = new Color32(237, 248, 243, 255);
        [SerializeField] private Color32 headerTextColor = new Color32(51, 51, 51, 255);
        [SerializeField] private Color32 dataTextColor = new Color32(51, 51, 51, 255);
        [SerializeField] private Color32 oddRowColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color32 evenRowColor = new Color32(237, 248, 243, 255);

        public Action<int, DailySlotInfoRow> onCellClicked;
        private List<DailySlotInfoRow> _currentRows = new List<DailySlotInfoRow>();

        [Serializable]
        public class DailySlotInfoRow
        {
            public int SlotNumber;
            public string FullDate;
            public float SetVoltage;
            public float AvgVoltage;
            public float WorkVoltage;
            public float Temperature;
            public float MoleRatio;
            public float AluminumLevel;
            public float ElectrolyteLevel;
            public float FeedingAmount;
            public float AluminumOutput;
            public float TotalPower;
        }

        private void Awake()
        {
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
            }

            if (m_Table != null)
            {
                m_Table.onTableClick = (row, col, cell) =>
                {
                    if (_currentRows != null && row >= 0 && row < _currentRows.Count)
                    {
                        onCellClicked?.Invoke(col, _currentRows[row]);
                    }
                };
            }

            EnsureTMPFont();

            InitializeTable();
        }

        private void EnsureTMPFont()
        {
            if (customTMPFont == null)
            {
                customTMPFont = Resources.Load<TMP_FontAsset>("Fonts/Alibaba-PuHuiTi-Regular SDF");
            }

            if (customTMPFont == null)
            {
                customTMPFont = TMP_Settings.defaultFontAsset;
            }
        }

        public void SetRows(List<DailySlotInfoRow> rows)
        {
            if (m_Table == null)
            {
                return;
            }

            _currentRows.Clear();
            EnsureColumns();
            if (m_Table.context != null)
            {
                m_Table.context.selectRow = -1;
                m_Table.context.selectColumn = -1;
            }
            m_Table.ClearData();

            if (rows == null || rows.Count == 0)
            {
                AddMessageRow("暂无数据");
                Refresh();
                return;
            }

            _currentRows.AddRange(rows);

            for (int i = 0; i < rows.Count; i++)
            {
                DailySlotInfoRow rowData = rows[i];
                m_Table.AddRow(new string[9]);

                var row = m_Table.data[m_Table.data.Count - 1];
                while (row.data.Count < 9)
                {
                    row.data.Add(new TableCell());
                }

                row.data[0].stringValue = FormatDateMMDD(rowData.FullDate);
                row.data[1].stringValue = rowData.WorkVoltage.ToString("F3", CultureInfo.InvariantCulture);
                row.data[2].stringValue = FormatMaybe(rowData.Temperature, "F0");
                row.data[3].stringValue = FormatMaybe(rowData.MoleRatio, "F2");
                row.data[4].stringValue = FormatMaybe(rowData.AluminumLevel, "F0");
                row.data[5].stringValue = FormatMaybe(rowData.ElectrolyteLevel, "F0");
                row.data[6].stringValue = rowData.FeedingAmount.ToString("F0", CultureInfo.InvariantCulture);
                row.data[7].stringValue = FormatMaybe(rowData.AluminumOutput, "F0");
                row.data[8].stringValue = rowData.TotalPower.ToString("F0", CultureInfo.InvariantCulture);
            }

            Refresh();
        }

        public UITable GetTable()
        {
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
            }
            return m_Table;
        }

        public void ClearAndShowMessage(string message)
        {
            if (m_Table == null)
            {
                return;
            }

            EnsureColumns();
            if (m_Table.context != null)
            {
                m_Table.context.selectRow = -1;
                m_Table.context.selectColumn = -1;
            }
            m_Table.ClearData();
            AddMessageRow(string.IsNullOrEmpty(message) ? "暂无数据" : message);
            Refresh();
        }

        private void InitializeTable()
        {
            if (m_Table == null)
            {
                return;
            }

            EnsureColumns();

            if (m_Table.theme != null && m_Table.theme.sharedTheme != null && customTMPFont != null)
            {
                m_Table.theme.sharedTheme.tmpFont = customTMPFont;
                m_Table.theme.sharedTheme.SyncTMPFontToSubComponent();
                m_Table.SetAllDirty();
            }

            if (m_Table.rowHeaderStyle != null)
            {
                // 避免在缺字库/空字库情况下创建行头文本触发TMP空引用
                m_Table.rowHeaderStyle.show = false;
            }

            if (m_Table.columnHeaderStyle != null)
            {
                m_Table.columnHeaderStyle.show = true;
                m_Table.columnHeaderStyle.height = 70;
                m_Table.columnHeaderStyle.backgroundColor = headerBgColor;
                m_Table.columnHeaderStyle.defalutLabelStyle.show = true;
                m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.color = headerTextColor;
                m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.fontSize = 28;
                m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_Table.columnStyle != null)
            {
                m_Table.columnStyle.defaultLabelStyle.show = true;
                m_Table.columnStyle.defaultLabelStyle.textStyle.color = dataTextColor;
                m_Table.columnStyle.defaultLabelStyle.textStyle.fontSize = 26;
                m_Table.columnStyle.defaultLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_Table.rowStyle != null)
            {
                m_Table.rowStyle.rowHeight = 72;
                m_Table.rowStyle.minRowHeight = 72;
                m_Table.rowStyle.rowBackgroundColors.Clear();
                m_Table.rowStyle.rowBackgroundColors.Add(oddRowColor);
                m_Table.rowStyle.rowBackgroundColors.Add(evenRowColor);
            }

            if (m_Table.separatorStyle != null)
            {
                m_Table.separatorStyle.show = true;
                m_Table.separatorStyle.horizontal = true;
                m_Table.separatorStyle.vertical = true;
                m_Table.separatorStyle.horizontalWidth = 1;
                m_Table.separatorStyle.verticalWidth = 1;
                m_Table.separatorStyle.horizontalColor = new Color32(224, 224, 224, 255);
                m_Table.separatorStyle.verticalColor = new Color32(224, 224, 224, 255);
            }

            if (m_Table.borderStyle != null)
            {
                m_Table.borderStyle.show = true;
                m_Table.borderStyle.roundedCorner = true;
                m_Table.borderStyle.cornerRadius = new[] { 18f, 18f, 18f, 18f };
                m_Table.borderStyle.borderColor = new Color32(220, 220, 220, 255);
                m_Table.borderStyle.borderWidth = 1;
            }

            m_Table.ClearData();
            Refresh();
        }

        private void EnsureColumns()
        {
            if (m_Table == null)
            {
                return;
            }

            List<string> headers = new List<string>
            {
                "日期",
                "工作电压",
                "温度",
                "分子比",
                "铝水平",
                "电解质水平",
                "下料量",
                "出铝量",
                "总功耗"
            };

            m_Table.SetColumns(headers);

            float[] widths = { 120f, 120f, 90f, 90f, 100f, 120f, 110f, 110f, 110f };
            if (m_Table.columns != null)
            {
                for (int i = 0; i < m_Table.columns.Count && i < widths.Length; i++)
                {
                    m_Table.columns[i].width = widths[i];
                }
            }
        }

        private void AddMessageRow(string message)
        {
            m_Table.AddRow(new string[9]);
            if (m_Table.data.Count == 0)
            {
                return;
            }

            var row = m_Table.data[0];
            while (row.data.Count < 9)
            {
                row.data.Add(new TableCell());
            }

            row.data[0].stringValue = "--";
            row.data[1].stringValue = "--";
            row.data[2].stringValue = message;
            row.data[3].stringValue = "--";
            row.data[4].stringValue = "--";
            row.data[5].stringValue = "--";
            row.data[6].stringValue = "--";
            row.data[7].stringValue = "--";
            row.data[8].stringValue = "--";
        }

        private void Refresh()
        {
            m_Table.RefreshGraph();
            m_Table.RefreshTable();
        }

        private static string FormatDateMMDD(string yyyymmdd)
        {
            if (!string.IsNullOrEmpty(yyyymmdd) && yyyymmdd.Length == 8)
            {
                return yyyymmdd.Substring(4, 2) + "-" + yyyymmdd.Substring(6, 2);
            }

            return yyyymmdd;
        }

        private static string FormatMaybe(float v, string format)
        {
            if (float.IsNaN(v) || float.IsInfinity(v))
            {
                return "--";
            }

            return v.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
