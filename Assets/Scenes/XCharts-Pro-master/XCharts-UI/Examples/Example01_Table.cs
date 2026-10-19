using UnityEngine;
#if INPUT_SYSTEM_ENABLED
using Input = XCharts.Runtime.InputHelper;
#endif
using System.Collections.Generic;
using XCharts.Runtime;
using XCharts.Runtime.UI;


namespace XCharts.Example.UI
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [RequireComponent(typeof(UITable))]
    public class Example01_Table : MonoBehaviour
    {
        UITable table;

        void Awake()
        {
            table = gameObject.GetComponent<UITable>();
            table.onTableClick = OnTableClick;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //AddData();
                AddColumn();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                //UpdateData();
                //AddColumn2();
                UpdateColumn();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                table.ClearData();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                table.RemoveRow(0);
            }
        }

        void OnTableClick(int row, int column, TableCell cellData)
        {
            Debug.Log("OnTableClick row:" + row + " column:" + column);
        }

        void AddData()
        {
            table.AddRow("test1", "test2", "test3", "test4", "t est5");
        }

        void AddColumn()
        {
            table.AddColumn("Column1", "data1", "data2", "data3", "data4", "data5");
            table.AddColumn("Column1", new string[] { "data1", "data2", "data3", "data4", "data5" });
        }

        void AddColumn2()
        {
            table.AddColumn(null, 1, 2, 3, 4, 5);
        }

        void UpdateRow()
        {
            var rowIndex = Random.Range(0, table.rowCount);
            var columnCount = table.columnCount;
            var list = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                list.Add("test" + Random.Range(0, 100));
            }
            table.UpdateRow(rowIndex, list);
        }

        void UpdateColumn()
        {
            table.UpdateColumn(0, new List<string>() { "new1", "new2", "new3" });
        }
    }
}