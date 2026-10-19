using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XCharts.Runtime.UI;

public class CanshuUIController : MonoBehaviour
{
    [Header("界面面板 (Panels)")]
    public GameObject panelMoren;         // 默认场景选择界面
    public GameObject panelJizhun;        // 基准值设置界面
    public GameObject panelJishu;         // 技术条件设置界面
    public GameObject panelYichang;       // 异常槽况设置界面

    [Header("切换按钮 (Tab Buttons)")]
    public Button btnMoren;
    public Button btnJizhun;
    public Button btnJishu;
    public Button btnYichang;

    [Header("界面1：默认场景选择")]
    public Toggle toggleShengchan;        // 生产现场
    public Toggle toggleYuancheng;        // 远程办公
    public Toggle toggleYichangInfo;      // 异常信息
    public Toggle toggleQuanchangjing;    // 全场景
    public Button btnSaveScene;           // 保存按钮

    [Header("表格引用 (UITables)")]
    public UITable tableJizhun;           // 基准值表格 (图2)
    public UITable tableJishu;            // 技术条件表格 (图3)
    public UITable tableYichang;          // 异常槽况表格 (图4)

    // 在这里暂存要发往数据库的修改数据
    private Dictionary<string, string> pendingModifications = new Dictionary<string, string>();

    void Start()
    {
        // 1. 绑定顶部四个界面的切换事件
        btnMoren.onClick.AddListener(() => SwitchTab(0));
        btnJizhun.onClick.AddListener(() => SwitchTab(1));
        btnJishu.onClick.AddListener(() => SwitchTab(2));
        btnYichang.onClick.AddListener(() => SwitchTab(3));

        // 2. 界面1：初始化单选框状态和保存按钮
        InitSceneSelection();
        btnSaveScene.onClick.AddListener(SaveDefaultScene);

        // 3. 初始化并填充三张表格的数据
        InitJizhunTable();
        InitJishuTable();
        InitYichangTable();

        // 4. 监听表格的点击事件，判断是否点击了"修改"
        if (tableJizhun != null) tableJizhun.onTableClick += OnJizhunTableClicked;
        if (tableJishu != null) tableJishu.onTableClick += OnJishuTableClicked;
        if (tableYichang != null) tableYichang.onTableClick += OnYichangTableClicked;

        // 默认显示第一个界面
        SwitchTab(0);
    }

    private void SwitchTab(int tabIndex)
    {
        panelMoren.SetActive(tabIndex == 0);
        panelJizhun.SetActive(tabIndex == 1);
        panelJishu.SetActive(tabIndex == 2);
        panelYichang.SetActive(tabIndex == 3);
    }

    #region 第1个界面：默认场景选择
    private void InitSceneSelection()
    {
        // 读取本地保存的默认场景，没有则默认是 MainScene
        string defaultScene = PlayerPrefs.GetString("DefaultScene", "MainScene");
        if (defaultScene == "shengchanxianchang") toggleShengchan.isOn = true;
        else if (defaultScene == "yuanchengbangong") toggleYuancheng.isOn = true;
        else if (defaultScene == "yichangxinxi") toggleYichangInfo.isOn = true;
        else toggleQuanchangjing.isOn = true; // MainScene
    }

    private void SaveDefaultScene()
    {
        string targetScene = "MainScene"; // 默认全场景

        if (toggleShengchan.isOn) targetScene = "shengchanxianchang";
        else if (toggleYuancheng.isOn) targetScene = "yuanchengbangong";
        else if (toggleYichangInfo.isOn) targetScene = "yichangxinxi";
        
        PlayerPrefs.SetString("DefaultScene", targetScene);
        PlayerPrefs.Save();
        Debug.Log("已保存默认场景偏好为: " + targetScene);
    }
    #endregion

    #region 第2个界面：基准值设置 (图2)
    private void InitJizhunTable()
    {
        if (tableJizhun == null) return;
        tableJizhun.ClearData();

        // 每一行的数据定义 [名称, 默认基准值, 注释]
        string[,] jizhunData = {
            { "槽温", "970", "基准值可调" },
            { "铝水平", "20", "基准值可调" },
            { "电解质水平", "18", "基准值可调" },
            { "分子比", "2.2", "基准值可调" }
        };

        for (int i = 0; i < jizhunData.GetLength(0); i++)
        {
            string name = jizhunData[i, 0];
            string defaultVal = jizhunData[i, 1];
            string note = jizhunData[i, 2];

            // 尝试读取用户是否曾经修改过此基准值
            string savedVal = PlayerPrefs.GetString("Jizhun_" + name, defaultVal);
            
            // 将修改按钮改为蓝色可点击的文本
            string actionText = "<color=#0078FF>修改</color>";
            
            tableJizhun.AddRow(name, savedVal, note, actionText);
        }
    }

    private void OnJizhunTableClicked(int row, int col, TableCell cell)
    {
        // 只有点击了第4列（索引为3的"操作"列），并且不是点击表头(row >= 0)
        if (row >= 0 && col == 3)
        {
            string name = tableJizhun.GetContent(row, 0); // 获取第一列的名称
            string currentVal = tableJizhun.GetContent(row, 1); // 获取当前基准值
            Debug.Log($"点击了基准值表的修改，行：{row}，名称：{name}，当前值：{currentVal}");
            
            // TODO: 这里可以弹出一个让用户输入新值的输入框
            // 为了演示局部变量保存，我这里暂时用一个写死的假数据测试
            // string newValue = "999"; 
            // SaveNewValue("Jizhun", name, newValue);
        }
    }
    #endregion

    #region 第3个界面：技术条件设置 (图3)
    private void InitJishuTable()
    {
        if (tableJishu == null) return;
        tableJishu.ClearData();

        // [名称, 运算符, 默认基准值, 默认异常标准, 注释]
        string[,] jishuData = {
            { "压差", ">", "", "50", "压差=平均电压-设定电压，标准值可调" },
            { "针振", ">", "", "20", "标准值可调" },
            { "摆动", ">", "", "10", "标准值可调" },
            { "槽温高值", ">", "970", "+5", "基准值、标准值可调" },
            // ... 按照图3继续往这里添加剩下的17行即可
            { "槽温低值", "<", "970", "-5", "基准值、标准值可调" }
        };

        for (int i = 0; i < jishuData.GetLength(0); i++)
        {
            string name = jishuData[i, 0];
            string op = jishuData[i, 1];
            
            // 获取可能存在本地存储的基准值和异常标准
            string savedJizhun = PlayerPrefs.GetString("JishuJizhun_" + name, jishuData[i, 2]);
            string savedYichang = PlayerPrefs.GetString("JishuYichang_" + name, jishuData[i, 3]);
            
            string note = jishuData[i, 4];
            string actionText = "<color=#0078FF>修改</color>";
            
            tableJishu.AddRow(name, op, savedJizhun, savedYichang, note, actionText);
        }
    }

    private void OnJishuTableClicked(int row, int col, TableCell cell)
    {
        // 操作列是第6列（索引5）
        if (row >= 0 && col == 5)
        {
            string name = tableJishu.GetContent(row, 0);
            Debug.Log($"点击了技术条件表的修改，名称：{name}");
        }
    }
    #endregion

    #region 第4个界面：异常槽况设置 (图4)
    private void InitYichangTable()
    {
        if (tableYichang == null) return;
        tableYichang.ClearData();

        // 异常槽况中含有多行文本，我们在字符串里以 \n 换行
        string[,] yichangData = {
            { "1 针振摆动超标", "压差>50\nor   针振>20\nor   摆动>10" },
            { "2 电压过高/低", "炉底压降>400\nor   槽温高值>970+5\nor   槽温低值<970-5" },
            { "3 下料异常", "过欠比高值>0.6\nor   过欠比低值<0.4\nor   下料量高值>Q1\nor   下料量低值<Q2" },
            { "4 条件持续偏离", "铝水平高值>18+2\nor   铝水平低值<18-2\nand 电解质水平高值>20+2\nor   电解质水平低值<20-2" }
        };

        for (int i = 0; i < yichangData.GetLength(0); i++)
        {
            string typeName = yichangData[i, 0];
            string savedCondition = PlayerPrefs.GetString("YichangCond_" + typeName, yichangData[i, 1]);
            string actionText = "<color=#0078FF>修改</color>";
            
            tableYichang.AddRow(typeName, savedCondition, actionText);
        }
    }

    private void OnYichangTableClicked(int row, int col, TableCell cell)
    {
        // 操作列是第3列（索引2）
        if (row >= 0 && col == 2)
        {
            string typeName = tableYichang.GetContent(row, 0);
            Debug.Log($"点击了异常槽况表的修改，类型：{typeName}");
        }
    }
    #endregion

    #region 修改数据和持久化保存
    // 用于未来对接数据库的统一保存接口
    private void SaveNewValue(string tablePrefix, string rowName, string newValue, string subField = "")
    {
        string key = $"{tablePrefix}_{subField}{rowName}";
        
        // 1. 保存到本地 PlayerPrefs 用于单机记忆
        PlayerPrefs.SetString(key, newValue);
        PlayerPrefs.Save();
        
        // 2. 存入内存字典待上传
        if (pendingModifications.ContainsKey(key))
            pendingModifications[key] = newValue;
        else
            pendingModifications.Add(key, newValue);

        // 3. 刷新对应的表格以显示最新数据
        if (tablePrefix == "Jizhun") InitJizhunTable();
        else if (tablePrefix == "Jishu") InitJishuTable();
        else if (tablePrefix == "Yichang") InitYichangTable();

        Debug.Log($"[准备发送至数据库] Key: {key}, 新值: {newValue}");
    }
    #endregion
}