using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime.Treemaps
{
    [System.Serializable]
    [SerieHandler(typeof(TreemapHandler), true)]
    [SerieComponent(typeof(LabelStyle))]
    [SerieDataComponent(typeof(ItemStyle), typeof(LabelStyle), typeof(EmphasisStyle), typeof(BlurStyle), typeof(SelectStyle))]
    [SerieDataExtraField("m_Id", "m_ParentId")]
    public class Treemap : Serie
    {
        [SerializeField] private int m_LeafDepth = 0;
        [SerializeField] private int m_VisualDimension = 0;
        [SerializeField] private string m_DrillDownIcon = "▶";
        [SerializeField] private GameObject m_CustomGoUpLevelButton;
        [SerializeField] private LabelStyle m_UpperLabel = new LabelStyle();
        [SerializeField] private LevelStyle m_LevelStyle = new LevelStyle();

        /// <summary>
        /// 自定义返回上一层按钮，自定义的按钮需要自己处理点击事件，调用GoUpALevel()接口。
        /// </summary>
        public GameObject customGoUpLevelButton { get { return m_CustomGoUpLevelButton; } set { m_CustomGoUpLevelButton = value; } }
        /// <summary>
        /// 用于计算占比的数据维数。
        /// </summary>
        public int visualDimension { get { return m_VisualDimension; } set { m_VisualDimension = value; SetVerticesDirty(); } }
        /// <summary>
        /// 默认显示的几层节点。默认为0时表示尽可能展示多层节点，设置大于0时，下钻功能开启，需要点击后才展示子节点。
        /// </summary>
        public int leafDepth { get { return m_LeafDepth < 1 ? int.MaxValue : m_LeafDepth; } set { m_LeafDepth = value; SetVerticesDirty(); } }
        /// <summary>
        /// 当节点可以下钻时的提示符，只能是字符。默认为▶
        /// </summary>
        public string drillDownIcon { get { return m_DrillDownIcon; } set { m_DrillDownIcon = value; SetVerticesDirty(); } }
        /// <summary>
        /// 父节点标签。show为true时表示显示父节点标签，margin参数设置子节点和父节点的间距。
        /// </summary>
        public LabelStyle upperLabel { get { return m_UpperLabel; } }
        public LevelStyle levelStyle { get { return m_LevelStyle; } }

        public ItemStyle GetLevelItemStyle(int level)
        {
            if (m_LevelStyle.show && level < m_LevelStyle.levels.Count) return m_LevelStyle.levels[level].itemStyle;
            else return itemStyle;
        }

        public LabelStyle GetLevelLabel(int level)
        {
            if (m_LevelStyle.show && level < m_LevelStyle.levels.Count) return m_LevelStyle.levels[level].label;
            else return label;
        }

        public LabelStyle GetLevelUpperLabel(int level)
        {
            if (m_LevelStyle.show && level < m_LevelStyle.levels.Count) return m_LevelStyle.levels[level].upperLabel;
            else return m_UpperLabel;
        }

        public void SetSelectedSerieData(SerieData serieData)
        {
            (handler as TreemapHandler).SetSelectedSerieData(serieData);
        }

        /// <summary>
        /// 设置选中（要展示的）的数据名
        /// </summary>
        /// <param name="serieDataName"></param>
        /// <returns></returns>
        public bool SetSelectedSerieData(string serieDataName)
        {
            foreach (var serieData in data)
            {
                if (serieDataName.Equals(serieData.name))
                {
                    (handler as TreemapHandler).SetSelectedSerieData(serieData);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 返回上一层
        /// </summary>
        public void GoUpALevel()
        {
            (handler as TreemapHandler).GoUpALevel();
        }

        /// <summary>
        /// 是否可以返回上一层
        /// </summary>
        /// <returns></returns>
        public bool CanGoUpALevel()
        {
            return (handler as TreemapHandler).CanGoUpALevel();
        }

        public override void OnDataUpdate()
        {
            context.rootData.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                var serieData = data[i];
                serieData.index = i;
                serieData.context.children.Clear();
            }
            foreach (var serieData in data)
            {
                if (string.IsNullOrEmpty(serieData.parentId))
                    context.rootData.Add(serieData);
                else
                {
                    var parent = GetSerieData(serieData.parentId);
                    if (parent != null)
                    {
                        serieData.context.parent = parent;
                        if (!parent.context.children.Contains(serieData))
                        {
                            parent.context.children.Add(serieData);
                        }
                    }
                }
            }
        }

        public static void AddDefaultSerie(BaseChart chart, string serieName)
        {
            var serie = chart.AddSerie<Treemap>(serieName);

            serie.leafDepth = 2;
            serie.upperLabel.show = true;
            serie.upperLabel.distance = 30;

            serie.levelStyle.levels.Clear();
            // var level = new TreemapLevel();
            // level.itemStyle.borderWidth = 4;
            // level.upperLabel.show = true;
            // level.upperLabel.margin = 30;
            // level.label.show = true;
            // m_Levels.Add(level);

            serie.left = 50f;
            serie.right = 50f;
            serie.top = 50f;
            serie.bottom = 50f;

            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.textStyle.color = Color.white;

            serie.itemStyle.borderWidth = 4;

            var serieData1 = chart.AddData(serie.index, new List<double>() { 6, 4, 6, 3, 2 }, "NodeA", "NodeA");
            AddChildData(serie, AddChildData(serie, AddChildData(serie, serieData1)));

            AddChildData(serie, AddChildData(serie, AddChildData(serie, chart.AddData(serie.index, new List<double>() { 6, 2, 3, 5, 6 }, "NodeB", "NodeB"))));
            chart.AddData(serie.index, new List<double>() { 4, 1, 3, 2, 5 }, "NodeC", "NodeC");
            var serieData4 = chart.AddData(serie.index, new List<double>() { 3, 1, 2, 2, 5 }, "NodeD", "NodeD");
            AddChildData(serie, serieData4);
            AddChildData(serie, chart.AddData(serie.index, new List<double>() { 3, 4, 1, 1, 4 }, "NodeE", "NodeE"));
            chart.AddData(serie.index, new List<double>() { 2, 6, 3, 3, 1 }, "NodeF", "NodeF");
            AddChildData(serie, chart.AddData(serie.index, new List<double>() { 1, 5, 3, 3, 1 }, "NodeG", "NodeG"));
        }

        private static SerieData AddChildData(Serie serie, SerieData serieData)
        {
            var parentName = serieData.name;
            var head = serie.AddChildData(serieData, new List<double>() { 1, 5, 1, 5, 3 }, parentName + "-1", parentName + "-1");
            serie.AddChildData(serieData, new List<double>() { 2, 4, 5, 1, 2 }, parentName + "-2", parentName + "-2");
            serie.AddChildData(serieData, new List<double>() { 3, 3, 2, 4, 4 }, parentName + "-3", parentName + "-3");
            serie.AddChildData(serieData, new List<double>() { 4, 2, 4, 2, 1 }, parentName + "-4", parentName + "-4");
            serie.AddChildData(serieData, new List<double>() { 5, 1, 3, 3, 25 }, parentName + "-5", parentName + "-5");
            return head;
        }
    }
}