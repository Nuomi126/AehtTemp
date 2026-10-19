using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Graphs;

namespace XCharts.Example.Graphs
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_Graph : MonoBehaviour
    {
        private GraphChart chart;
        private float updateTime;
        public int dayCount = 10;
        public int taskCount = 5;

        void Awake()
        {
            chart = gameObject.GetComponent<GraphChart>();
            AddData();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                UpdateData();
            }
        }

        private int totalIndex = 8;
        void UpdateData()
        {
            var dataId = "数据总额" + totalIndex;
            chart.AddData(0, Random.Range(10000, 20000), dataId, dataId);
            chart.AddLink(0, "数据占比", dataId, 5);
            totalIndex++;
        }

        void AddData()
        {
            chart.ClearData();

            chart.AddData(0, 10, "数据占比", "数据占比");
            chart.AddData(0, 20000, "数据总额", "数据总额1");
            chart.AddData(0, 20000, "数据总额", "数据总额2");
            chart.AddData(0, 599, "数据总额", "数据总额3");
            chart.AddData(0, 50, "数据总额", "数据总额4");
            chart.AddData(0, 95650, "数据总额", "数据总额5");
            chart.AddData(0, 99999, "数据总额", "数据总额6");
            chart.AddData(0, 15599, "数据总额", "数据总额7");

            chart.AddData(0, 65, "65%", "数据总额1-1");
            chart.AddData(0, 25, "25%", "数据总额1-2");

            chart.AddData(0, 40, "40", "数据总额1-1-1");
            chart.AddData(0, 125, "125", "数据总额1-1-2");
            chart.AddData(0, 125, "125", "数据总额1-1-3");

            chart.AddData(0, 125, "125", "数据总额1-2-1");
            chart.AddData(0, 125, "125", "数据总额1-2-2");

            chart.AddData(0, 99, "99%", "数据总额2-1");
            chart.AddData(0, 125, "125", "数据总额2-1-1");
            chart.AddData(0, 95, "95", "数据总额2-1-2");
            chart.AddData(0, 125, "125", "数据总额2-1-3");

            chart.AddData(0, 95, "95", "数据总额4-1");
            chart.AddData(0, 125, "125", "数据总额4-2");
            chart.AddData(0, 95, "95", "数据总额4-3");
            chart.AddData(0, 95, "95", "数据总额4-4");


            chart.AddData(0, 69, "69%", "数据总额5-1");
            chart.AddData(0, 559, "559", "数据总额5-1-1");
            chart.AddData(0, 50, "50", "数据总额5-1-2");
            chart.AddData(0, 515, "515", "数据总额5-1-3");
            chart.AddData(0, 256, "256", "数据总额5-1-4");

            chart.AddData(0, 85, "85%", "数据总额7-1");
            chart.AddData(0, 65, "65%", "数据总额7-2");
            chart.AddData(0, 559, "559", "数据总额7-1-1");
            chart.AddData(0, 50, "50", "数据总额7-1-2");
            chart.AddData(0, 515, "515", "数据总额7-1-3");
            chart.AddData(0, 256, "256", "数据总额7-1-4");

            chart.AddData(0, 256, "256", "数据总额7-2-1");
            chart.AddData(0, 772, "772", "数据总额7-2-2");
            chart.AddData(0, 40, "40", "数据总额7-2-3");
            chart.AddData(0, 125, "125", "数据总额7-2-4");


            chart.AddLink(0, "数据占比", "数据总额1", 5);
            chart.AddLink(0, "数据占比", "数据总额2", 5);
            chart.AddLink(0, "数据占比", "数据总额3", 5);
            chart.AddLink(0, "数据占比", "数据总额4", 5);
            chart.AddLink(0, "数据占比", "数据总额5", 5);
            chart.AddLink(0, "数据占比", "数据总额6", 5);
            chart.AddLink(0, "数据占比", "数据总额7", 5);


            chart.AddLink(0, "数据总额1", "数据总额1-1", 5);
            chart.AddLink(0, "数据总额1", "数据总额1-2", 5);

            chart.AddLink(0, "数据总额1-1", "数据总额1-1-1", 5);
            chart.AddLink(0, "数据总额1-1", "数据总额1-1-2", 5);
            chart.AddLink(0, "数据总额1-1", "数据总额1-1-3", 5);

            chart.AddLink(0, "数据总额1-2", "数据总额1-2-1", 5);
            chart.AddLink(0, "数据总额1-2", "数据总额1-2-2", 5);

            chart.AddLink(0, "数据总额2", "数据总额2-1", 5);
            chart.AddLink(0, "数据总额2-1", "数据总额2-1-1", 5);
            chart.AddLink(0, "数据总额2-1", "数据总额2-1-2", 5);
            chart.AddLink(0, "数据总额2-1", "数据总额2-1-3", 5);

            chart.AddLink(0, "数据总额4", "数据总额4-1", 5);
            chart.AddLink(0, "数据总额4", "数据总额4-2", 5);
            chart.AddLink(0, "数据总额4", "数据总额4-3", 5);
            chart.AddLink(0, "数据总额4", "数据总额4-4", 5);

            chart.AddLink(0, "数据总额5", "数据总额5-1", 5);
            chart.AddLink(0, "数据总额5-1", "数据总额5-1-1", 5);
            chart.AddLink(0, "数据总额5-1", "数据总额5-1-2", 5);
            chart.AddLink(0, "数据总额5-1", "数据总额5-1-3", 5);
            chart.AddLink(0, "数据总额5-1", "数据总额5-1-4", 5);

            chart.AddLink(0, "数据总额7", "数据总额7-1", 5);
            chart.AddLink(0, "数据总额7", "数据总额7-2", 5);
            chart.AddLink(0, "数据总额7-1", "数据总额7-1-1", 5);
            chart.AddLink(0, "数据总额7-1", "数据总额7-1-2", 5);
            chart.AddLink(0, "数据总额7-1", "数据总额7-1-3", 5);
            chart.AddLink(0, "数据总额7-1", "数据总额7-1-4", 5);

            chart.AddLink(0, "数据总额7-2", "数据总额7-2-1", 5);
            chart.AddLink(0, "数据总额7-2", "数据总额7-2-2", 5);
            chart.AddLink(0, "数据总额7-2", "数据总额7-2-3", 5);
            chart.AddLink(0, "数据总额7-2", "数据总额7-2-4", 5);


        }
    }
}