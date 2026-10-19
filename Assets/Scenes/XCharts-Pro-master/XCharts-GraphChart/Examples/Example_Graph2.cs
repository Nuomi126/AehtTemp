using UnityEngine;
using XCharts.Runtime;
using XCharts.Runtime.Graphs;

namespace XCharts.Example.Graphs
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Example_Graph2 : MonoBehaviour
    {
        private GraphChart chart;
        private float updateTime;
        public int dayCount = 10;
        public int taskCount = 5;

        void Awake()
        {
            chart = gameObject.GetComponent<GraphChart>();
            StartCoroutine(JsonUtil.GetWebJson("https://echarts.apache.org/examples/data/asset/data/webkit-dep.json", (WebkitInfo info) =>
            {
                Debug.Log("json: " + info.type);
                chart.ClearData();
                for(int i=0;i<info.nodes.Length;i++)
                {
                    var node = info.nodes[i];
                    var id = i.ToString();
                    chart.AddData(0, node.value, node.name, id);
                }
                foreach(var link in info.links)
                {
                    chart.AddLink(0, link.source.ToString(), link.target.ToString());
                }
            }));

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {

            }
        }

        private void AddData()
        {

        }

#pragma warning disable 0649
        [System.Serializable]
        class WebkitInfo
        {
            public string type;
            public WebkitInfoCategory[] categories;
            public WebkitInfoNode[] nodes;
            public WebkitInfoLink[] links;
        }

        [System.Serializable]
        class WebkitInfoCategory
        {
            public string name;
            public string keyword;
            //public string base;
        }

        [System.Serializable]
        class WebkitInfoNode
        {
            public string name;
            public string category;
            public int value;
        }

        [System.Serializable]
        class WebkitInfoLink
        {
            public int source;
            public int target;
        }
#pragma warning restore 0649
    }
}