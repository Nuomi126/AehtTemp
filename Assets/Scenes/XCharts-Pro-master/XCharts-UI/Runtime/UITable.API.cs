using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XCharts.Runtime.UI
{
    /// <summary>
    /// 表格UI组件。
    /// 用于展示行列数据。
    /// </summary>
    public partial class UITable
    {
        /// <summary>
        /// the count of row.
        /// ||行数。
        /// </summary>
        public int rowCount { get { return m_Data.Count; } }
        /// <summary>
        /// the count of column.
        /// ||列数。
        /// </summary>
        public int columnCount { get { return m_Columns.Count > 0 ? m_Columns.Count : (m_Data.Count > 0 ? m_Data[0].data.Count : 0); } }

        /// <summary>
        /// the callback function when table click. return the row and column of clicked cell. -1 means clicked header, in this case, the cell is null.
        /// ||点击表格时的回调函数。返回点击的行row和列column。为-1时表示点击的是表头或列头，此时的cell为null。
        /// </summary>
        public Action<int, int, TableCell> onTableClick;

        /// <summary>
        /// set the table columns and data.
        /// ||设置Columns和Data数据。
        /// </summary>
        /// <param name="columns">column data</param>
        /// <param name="rows"></param>rows data<param>
        public void SetColumnsAndData(List<string> columns, List<List<string>> rows)
        {
            SetColumns(columns);
            SetData(rows);
        }

        /// <summary>
        /// set the table columns.
        /// ||设置列表头数据。
        /// </summary>
        /// <param name="columns">column data</param>
        public void SetColumns(List<string> columns)
        {
            ClearColumns();
            foreach (var column in columns)
            {
                m_Columns.Add(new TableColumn() { content = column });
            }
        }

        /// <summary>
        /// clear the table columns.
        /// ||清除Columns数据。
        /// </summary>
        public void ClearColumns()
        {
            m_Columns.Clear();
            SetDataDirty();
        }

        /// <summary>
        /// clear the table data.
        /// ||清除Data数据。
        /// </summary>
        public void ClearData()
        {
            m_Data.Clear();
            SetDataDirty();
        }

        /// <summary>
        /// get the table cell data by row and column.
        /// ||获取指定行列的单元格数据。
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public TableCell GetData(int row, int column)
        {
            if (row >= 0 && row < m_Data.Count)
            {
                var rowData = m_Data[row];
                if (column >= 0 && column < rowData.data.Count)
                {
                    return rowData.data[column];
                }
            }
            return null;
        }

        /// <summary>
        /// get the table cell content by row and column.
        /// ||获取指定行列的单元格内容。
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public string GetContent(int row, int column)
        {
            var cell = GetData(row, column);
            if (cell != null)
            {
                return cell.GetContent();
            }
            return null;
        }

        /// <summary>
        /// set the table data.
        /// ||设置Data数据。
        /// </summary>
        /// <param name="rows">row data</param>
        public void SetData(List<List<string>> rows)
        {
            ClearData();
            foreach (var row in rows)
            {
                AddRow(row);
            }
        }

        /// <summary>
        /// add a row data.
        /// ||添加一行数据。
        /// </summary>
        /// <param name="row">row data</param>
        public void AddRow(List<string> row)
        {
            var data = new List<TableCell>();
            foreach (var cell in row)
            {
                data.Add(new TableCell() { stringValue = cell });
            }
            m_Data.Add(new TableRow() { data = data });
            // SetDataDirty();
        }

        /// <summary>
        /// add a row data.
        /// ||添加一行数据。
        /// </summary>
        /// <param name="row">row data</param>
        public void AddRow(params string[] row)
        {
            var data = new List<TableCell>();
            foreach (var cell in row)
            {
                data.Add(new TableCell() { stringValue = cell });
            }
            m_Data.Add(new TableRow() { data = data });
            SetDataDirty();
        }

        /// <summary>
        /// add a column data.
        /// ||添加一列string类型数据。
        /// </summary>
        /// <param name="title">列表头</param>
        /// <param name="data">列数据</param>
        public void AddColumn(string title, params string[] data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { stringValue = cell } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { stringValue = data[i] });
                    }
                }
            }
            SetDataDirty();
        }

        /// <summary>
        /// add a column data.
        /// ||添加一列string类型数据。
        /// </summary>
        /// <param name="title">列表头</param>
        /// <param name="data">列数据</param>
        public void AddColumn(string title, List<string> data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { stringValue = cell } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { stringValue = data[i] });
                    }
                }
            }
            SetDataDirty();
        }


        /// <summary>
        /// add a column data.
        /// ||添加一列double类型数据。 
        /// </summary>
        /// <param name="title">列表头</param>
        /// <param name="data">列数据</param>
        public void AddColumn(string title, params double[] data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { stringValue = cell.ToString(), type = DataType.Double } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { stringValue = data[i].ToString(), type = DataType.Double });
                    }
                }
            }
            SetDataDirty();
        }

        /// <summary>
        /// add a column data.
        /// ||添加一列double类型数据。 
        /// </summary>
        /// <param name="title">列表头</param>
        /// <param name="data">列数据</param>
        public void AddColumn(string title, List<double> data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { stringValue = cell.ToString(), type = DataType.Double } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { stringValue = data[i].ToString(), type = DataType.Double });
                    }
                }
            }
            SetDataDirty();
        }

        /// <summary>
        /// add a column data.
        /// ||添加一列Sprite类型数据。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="data"></param>
        public void AddColumn(string title, params Sprite[] data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { spriteValue = cell, type = DataType.Sprite } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { spriteValue = data[i], type = DataType.Sprite });
                    }
                }
            }
            SetDataDirty();
        }

        /// <summary>
        /// add a column data.
        /// ||添加一列Sprite类型数据。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="data"></param>
        public void AddColumn(string title, List<Sprite> data)
        {
            m_Columns.Add(new TableColumn() { content = title });
            if (m_Data.Count == 0)
            {
                foreach (var cell in data)
                {
                    m_Data.Add(new TableRow() { data = new List<TableCell>() { new TableCell() { spriteValue = cell, type = DataType.Sprite } } });
                }
            }
            else
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (i < m_Data.Count)
                    {
                        m_Data[i].data.Add(new TableCell() { spriteValue = data[i], type = DataType.Sprite });
                    }
                }
            }
            SetDataDirty();
        }

        /// <summary>
        /// upate a row data.
        /// ||更新一行数据。 
        /// </summary>
        /// <param name="rowIndex">row index</param>
        /// <param name="row">row data</param>
        public void UpdateRow(int rowIndex, List<string> row)
        {
            if (rowIndex >= 0 && rowIndex < m_Data.Count)
            {
                var tableRow = m_Data[rowIndex];
                for (int i = 0; i < tableRow.data.Count; i++)
                {
                    if (i < row.Count)
                    {
                        tableRow.data[i].stringValue = row[i];
                        tableRow.data[i].type = DataType.String;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a row data.
        /// ||更新一行数据。
        /// </summary>
        /// <param name="rowIndex">row index</param>
        /// <param name="row">row data</param>
        public void UpdateRow(int rowIndex, params string[] row)
        {
            if (rowIndex >= 0 && rowIndex < m_Data.Count)
            {
                var tableRow = m_Data[rowIndex];
                for (int i = 0; i < tableRow.data.Count; i++)
                {
                    if (i < row.Length)
                    {
                        tableRow.data[i].stringValue = row[i];
                        tableRow.data[i].type = DataType.String;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param> 
        public void UpdateColumn(int columnIndex, List<string> column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Count; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.stringValue = column[i];
                        cell.type = DataType.String;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param>
        public void UpdateColumn(int columnIndex, params string[] column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Length; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.stringValue = column[i];
                        cell.type = DataType.String;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param> 
        public void UpdateColumn(int columnIndex, List<double> column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Count; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.doubleValue = column[i];
                        cell.type = DataType.Double;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param>
        public void UpdateColumn(int columnIndex, params double[] column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Length; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.doubleValue = column[i];
                        cell.type = DataType.Double;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param>
        public void UpdateColumn(int columnIndex, List<Sprite> column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Count; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.spriteValue = column[i];
                        cell.type = DataType.Sprite;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a column data.
        /// ||更新指定索引的列数据。 
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="column"></param>
        public void UpdateColumn(int columnIndex, params Sprite[] column)
        {
            if (columnIndex >= 0)
            {
                for (int i = 0; i < column.Length; i++)
                {
                    if (i < m_Data.Count && columnIndex < m_Data[i].data.Count)
                    {
                        var cell = m_Data[i].data[columnIndex];
                        cell.spriteValue = column[i];
                        cell.type = DataType.Sprite;
                    }
                }
                SetDataDirty();
            }
        }

        /// <summary>
        /// update a cell data.
        /// ||更新一个单元格数据。 
        /// </summary>
        /// <param name="row">row index</param>
        /// <param name="column">column index</param>
        /// <param name="value">string value</param>
        public void UpdateCell(int row, int column, string value)
        {
            if (row >= 0 && row < m_Data.Count)
            {
                var rowData = m_Data[row];
                if (column >= 0 && column < rowData.data.Count)
                {
                    rowData.data[column].stringValue = value;
                    rowData.data[column].type = DataType.String;
                    SetDataDirty();
                }
            }
        }

        /// <summary>
        /// update a cell data.
        /// ||更新一个单元格数据。
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        public void UpdateCell(int row, int column, double value)
        {
            if (row >= 0 && row < m_Data.Count)
            {
                var rowData = m_Data[row];
                if (column >= 0 && column < rowData.data.Count)
                {
                    rowData.data[column].doubleValue = value;
                    rowData.data[column].type = DataType.Double;
                    SetDataDirty();
                }
            }
        }

        /// <summary>
        /// update a cell data.
        /// ||更新一个单元格数据。 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        public void UpdateCell(int row, int column, Sprite value)
        {
            if (row >= 0 && row < m_Data.Count)
            {
                var rowData = m_Data[row];
                if (column >= 0 && column < rowData.data.Count)
                {
                    rowData.data[column].spriteValue = value;
                    rowData.data[column].type = DataType.Sprite;
                    SetDataDirty();
                }
            }
        }



        /// <summary>
        /// remove a row data.
        /// ||删除一行数据。
        /// </summary>
        /// <param name="rowIndex">row index</param>
        public void RemoveRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < m_Data.Count)
            {
                m_Data.RemoveAt(rowIndex);
                SetDataDirty();
            }
        }

        /// <summary>
        /// refresh the table.
        /// ||刷新表格。
        /// </summary>
        public void RefreshTable()
        {
            RefreshGraph();
        }
    }
}