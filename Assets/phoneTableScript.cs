using JetBrains.Annotations;
using System;
//using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
//using System.IO;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Threading;
//using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using XCharts.Runtime;
using XCharts.Runtime.UI;
using static HistoryTemperature;
using static System.Net.Mime.MediaTypeNames;
public class phoneTableScript : MonoBehaviour
{
	// 表格的名称
	[Header("UI组件")]
	public UITable zhongJianTable;
	public UITable shangTable;
	public UITable xiaTable;
	public UITable zuoYiTable;
	public UITable zuoErTable;
	public UITable youYiTable;
	public UITable youErTable;
	public UITable zhongJianTable_pos;
	public UITable shangTable_pos;
	public UITable xiaTable_pos;
	public UITable zuoYiTable_pos;
	public UITable zuoErTable_pos;
	public UITable youYiTable_pos;
	public UITable youErTable_pos;
	public GameObject lineChart;
	public Canvas canver;
	private UITable pointedTable;
	public RawImage tableBackImage;
	public UnityEngine.UI.Toggle posTog; //位置信息开关
	//public TMP_InputField slotInputField; //槽号
	[SerializeField]private TextMeshProUGUI slotInputText; //槽号
	[SerializeField] private TextMeshProUGUI getHisTmpNoticeText;
	[Header("API管理器")]
	public TemperatureAPIManager temperatureAPI; 
	public HistoryTemperature historyTemperatureAPI;	
	public UnityEngine.UI.Button slotButton;

	private bool useMouseRightDrag = true;
	private RectTransform contentRect;
	private Vector2 lastPosition;
	private List<float> temperatureData;
	private List<List<int>> djcConfig; // 存储配置信息
	private HisTemDataDB HistoryTemperatureDatDB = new();
	// 配置文件路径
	private string companyIndex = "1033";  // 企业标识
	private string slotNumber ="";  //槽号	
	private string defaultSlotNumber = "1001";  //槽号	
	private string Date ="";  //日期
	float nextExecutionTime = 0; // 下次执行的时间点
	float nextGetHisTmpTime = 0; // 下次执行的时间点
	TableCell selectCell;

	// Start is called before the first frame update
	void Start()
    {
		lineChart.SetActive(false);
		zhongJianTable.onTableClick = OnTableCellClicked;
		shangTable.onTableClick = OnTableCellClicked;
		xiaTable.onTableClick = OnTableCellClicked;
		zuoYiTable.onTableClick = OnTableCellClicked;
		zuoErTable.onTableClick = OnTableCellClicked;
		youYiTable.onTableClick = OnTableCellClicked;
		youErTable.onTableClick = OnTableCellClicked;
		LineChart smootLineChart = lineChart.ConvertTo<LineChart>();
				
		// 将图表全部数据清零
		ClearTableData();

		// 设置热力图位置信息
		UpdatePosInfo(false);

		posTog.onValueChanged.AddListener(UpdatePosInfo);
		// 隐藏获取历史温度数据提示
		getHisTmpNoticeText.text = "";

		// 设置默认日期为当天日期
		DateTime currentDate = DateTime.Today;
		Date = currentDate.ToString("yyyyMMdd");

		if (canver == null)
		{
			Debug.LogError("Canvas Content未设置！");
			return;
		}

		//// 设置默认选中表格
		pointedTable = zhongJianTable;
		contentRect = canver.GetComponent<RectTransform>();

		// 根据表格设置的默认槽号更新表格温度信息 20250804
		slotInputText.text = defaultSlotNumber;
		OnUpdataButtonClick();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		//Debug.Log("OnBeginDrag");
		lastPosition = eventData.position;
	}

	public void OnDrag(PointerEventData eventData)
	{
		//Debug.Log("OnDrag");
		if (useMouseRightDrag && Input.GetMouseButton(1))
		{
			Vector2 delta = eventData.position - lastPosition;
			contentRect.anchoredPosition += delta;
			lastPosition = eventData.position;
		}
	}

	// Update is called once per frame
	void Update()
    {
		if (slotNumber != "" && Time.time >= nextExecutionTime) // 检查是否到了执行时间点
		{			
			// 获取电解槽配置信息
			temperatureAPI.GetDjcConfig(
							companyIndex,
							slotNumber,
							onConfigSuccess: OnSuccessGetConfig,
							onConfigError: OnGetConfigError
							);

			nextExecutionTime = Time.time + 5; // 更新下次执行的时间点为当前时间加5秒
		}
		if (slotNumber != "" && Time.time >= nextGetHisTmpTime) // 检查是否到了执行时间点
		{
			getHisTmpNoticeText.text = "正在获取历史温度数据……";
			// 调用API获取历史温度数据
			historyTemperatureAPI.GetHisTempData(companyIndex, slotNumber, Date, onSuccessHisTmpData: onSuccessGetHisTmpData);
			nextGetHisTmpTime = Time.time + 600; // 更新下次执行的时间点为当前时间加600秒，10分钟
		}
	}
	
	//表格被点击时显示折线图表
	public void OnTableCellClicked(int row, int column, TableCell cell)
	{
		Rect windowRect;
		string tmpconfig;
		string temPosconfig;
		int sampleindex = 0;  //采样编号
		int posindex = 0;    // 位置编号
		int irectiontype = 0;   //方向类型
		int directionindex = 0;//方向编号
		int nodeindex = 0;   //节点号

		float x = 0, y = 0;		
		// 获取LineChart 锚点组件
		RectTransform RectLineChar = lineChart.GetComponent<RectTransform>();
		
		
		// 设置点击表格颜色，取消选中表格颜色		
		pointedTable.selectColor = new Color(1f, 1f, 1f, 0f);

		// 图表被点击的时候，如果已经绘制了折线图，则清除折线图并返回
		if (lineChart.activeSelf && selectCell != null && selectCell == cell)
		{ 
			lineChart.SetActive(false);			
			return;
		}			
		// 如果历史温度数据记录数为0，则退出，不显示折线图
		if (HistoryTemperatureDatDB == null || HistoryTemperatureDatDB.DateList.Count == 0)
		{
			lineChart.SetActive(false);
			Debug.Log("历史温度数据为空！");
			return;
		}

		// 确定被点击的表格
		if (zhongJianTable.isPointerClick)
		{
			pointedTable = zhongJianTable;
			posindex = 5;
			if (row == 0) irectiontype = 2;
			else irectiontype = 1;
			directionindex = 0;
			if (row == 0) nodeindex = (14 - column)*2;
			if (row == 1) nodeindex = (14 - column) * 2 - 1;
		}
		else if (shangTable.isPointerClick)
		{
			pointedTable = shangTable;
			if (row == 0) posindex = 1;
			else posindex = 4;
			irectiontype = 2;
			if (row == 1) directionindex = 2;
			if (row == 2) directionindex = 1;
			nodeindex = 28 - column;
		}
		else if (xiaTable.isPointerClick)
		{
			pointedTable = xiaTable;
			if (row == 2) posindex = 1;
			else posindex = 4;
			irectiontype = 1;
			if (row == 0) directionindex = 1;
			if (row == 1) directionindex = 2;
			nodeindex = 28 - column;
		}
		else if (zuoYiTable.isPointerClick)
		{
			pointedTable = zuoYiTable;
			posindex = 3;
			irectiontype = 0;
			directionindex = 0;
			nodeindex = row + 1;
		}
		else if (zuoErTable.isPointerClick)
		{
			pointedTable = zuoErTable;
			posindex = 3;
			irectiontype = 0;
			directionindex = 0;
			nodeindex = row + 5;
		}
		else if (youYiTable.isPointerClick)
		{
			pointedTable = youYiTable;
			posindex = 2;
			irectiontype = 0;
			directionindex = 0;
			nodeindex = row + 1;
		}
		else if (youErTable.isPointerClick)
		{
			pointedTable = youErTable;
			posindex = 2;
			irectiontype = 0;
			directionindex = 0;
			nodeindex = row + 5;
		}
		
		// 根据配置信息，确定温度数据的位置序号
		temPosconfig = posindex.ToString() + "-" + irectiontype.ToString() + "-" + directionindex.ToString() + "-" + nodeindex.ToString();
		Debug.Log($"温度数据的位置序号 temPosconfig = {temPosconfig}");
		sampleindex = -1;
		for (int i = 0; i < djcConfig.Count; i++)
		{
			tmpconfig = string.Join("-", djcConfig[i]);
			if (tmpconfig.Contains(temPosconfig))
			{
				sampleindex = djcConfig[i][0];
				Debug.Log($"配置的索引 sampleindex:{sampleindex}");
			}
		}	

		// 获取点击位置
		Vector2 mouseClickPos = pointedTable.clickPos;
		// 获取点击位置的坐标		

		// 设置选中单元格颜色
		pointedTable.selectColor = new Color(0f, 1f, 0f, 1f);
		
		// 折线图显示
		lineChart.SetActive(true);
		selectCell = cell;
		// 坐标转换为世界坐标
		mouseClickPos = pointedTable.LocalPointToWorldPoint(mouseClickPos);
		//Debug.Log($"mouseClickPos = {mouseClickPos}");
		// 将坐标转换为本地坐标
		Vector3 localPos = canver.transform.InverseTransformPoint(mouseClickPos);
		// 将Z 轴设置为0
		localPos.z = 0;
		
		
		// 根据鼠标点击的位置设置图表的锚点位置，下面的数字是屏幕分辨率
		if (mouseClickPos.x > 2392* canver.transform.localScale.x / 2)
		{
			x = 1;
		}
		else
		{
			x = 0;
		}
		if (mouseClickPos.y > 1080* canver.transform.localScale.y / 2)
		{
			y = 1;
		}
		else
		{
			y = 0;
		}
		RectLineChar.anchorMin = new Vector2(x, y);
		RectLineChar.anchorMax = new Vector2(x, y);
		RectLineChar.pivot = new Vector2(x, y);
		// 设置图表的位置
		lineChart.transform.localPosition = localPos;
		UpdateDataLineCharts(HistoryTemperatureDatDB, sampleindex);		
	}

	/// <summary>
	/// 第一次获取API数据 ，API成功回调 - 将数据显示在UI 表格上
	/// </summary>
	private void OnFistAPISuccess(List<float> tempData)
	{
		UpdataTableData(tempData);
		getHisTmpNoticeText.text = "正在获取历史温度数据……";

		// 调用API获取历史温度数据
		historyTemperatureAPI.GetHisTempData(companyIndex, slotNumber, Date, onSuccessHisTmpData: onSuccessGetHisTmpData);
	}

	/// <summary>
	/// 定时获取API数据 ，API成功回调 - 将数据显示在UI 表格上
	/// </summary>
	private void OnAPISuccess(List<float> tempData)
	{
		UpdataTableData(tempData);		
	}

	//更新表格数据
	private void UpdataTableData(List<float> tempData)
	{ 
		// 采样编号，位置编号，方向类型，方向编号,节点号,
	/** 位置编号
		1 => "钢窗",
		2 => "出铝端",
		3 => "烟道端",
		4 => "阴极钢棒",
		5 => "炉底",
		_ => "未知位置"

	** 方向类型
		0 => "无A/B面分别位置",
		1 => "A面",
		2 => "B面",
		_ => "未知方向类型"

	** 方向编号
		0 => "只有一个测量点",
		1 => "阴极钢棒-1位置点",
		2 => "阴极钢棒-2位置点",
		_ => "未知方向编号"
	**/

		int sampleindex; // 采样编号
		int posindex; // 位置编号
		int directiontype; // 方向类型
		int directionindex; // 方向编号
		int nodeindex; // 节点号
	
		if (tempData == null || tempData.Count == 0)
		{
			Debug.Log("API返回成功，但数据为空！");
			return;
		}
		
		temperatureData = tempData;

		foreach (var config in djcConfig)
		{
			sampleindex = config[0];
			posindex = config[1];
			directiontype = config[2];
			directionindex = config[3];
			nodeindex = config[4];

			// A面阴极 A: directiontype=1 AND posindex=4
			if (directiontype == 1 && posindex == 4)
			{
				if (directionindex == 1)
				{
					xiaTable.UpdateCell(0, 28 - nodeindex, tempData[sampleindex].ToString());
					// 设置单元格颜色
					
				}
				else if (directionindex == 2)
				{
					xiaTable.UpdateCell(1, 28 - nodeindex, tempData[sampleindex].ToString());
				}
			}
			// B面阴极 B: directiontype=2 AND posindex=4
			else if (directiontype == 2 && posindex == 4)
			{
				if (directionindex == 1)
				{
					shangTable.UpdateCell(2, 28 - nodeindex, tempData[sampleindex].ToString());
				}
				else if (directionindex == 2)
				{
					shangTable.UpdateCell(1, 28 - nodeindex, tempData[sampleindex].ToString());
				}

			}
			// A面钢窗WA: directiontype=1 AND posindex=1
			else if (directiontype == 1 && posindex == 1)
			{
				xiaTable.UpdateCell(2, 28 - nodeindex, tempData[sampleindex].ToString());
			}
			// B面钢窗WB: directiontype=2 AND posindex=1
			else if (directiontype == 2 && posindex == 1)
			{
				shangTable.UpdateCell(0, 28 - nodeindex, tempData[sampleindex].ToString());
			}
			// 出铝端端口: directiontype=0 AND posindex=2
			else if (directiontype == 0 && posindex == 2)
			{
				if (nodeindex <= 4)
				{
					youYiTable.UpdateCell(nodeindex - 1, 0, tempData[sampleindex].ToString());
				}
				if (nodeindex >= 5 && nodeindex <= 9)
				{
					youErTable.UpdateCell(nodeindex - 5, 0, tempData[sampleindex].ToString());
				}
			}
			// 烟道端端口: directiontype=0 AND posindex=3
			else if (directiontype == 0 && posindex == 3)
			{
				if (nodeindex <= 4)
				{
					zuoYiTable.UpdateCell(nodeindex - 1, 0, tempData[sampleindex].ToString());
				}
				if (nodeindex >= 5 && nodeindex <= 9)
				{
					zuoErTable.UpdateCell(nodeindex - 5, 0, tempData[sampleindex].ToString());
				}
			}
			// 底部: directionindex=0 AND posindex=5
			else if (directionindex == 0 && posindex == 5)
			{
				if (directiontype == 1)
				{
					zhongJianTable.UpdateCell(1, 14 - (int)((nodeindex + 1) / 2), tempData[sampleindex].ToString());
				}
				else if (directiontype == 2)
				{
					zhongJianTable.UpdateCell(0, 14 - (int)(nodeindex / 2), tempData[sampleindex].ToString());
				}

			}
		}	

	}

	/// <summary>
	/// API错误回调
	/// </summary>
	private void OnAPIError(string errorMessage)
	{
		Debug.LogError($"API调用失败：{errorMessage}");
	}


	/// <summary>
	/// 第一次成功获取配置API回调
	/// </summary>
	private void OnFistSuccessGetConfig(List<List<int>> ConfigData)
	{
		djcConfig = ConfigData;

		// 调用API获取热力图温度数据
		temperatureAPI.GetTemperatureData(
			companyIndex,
			slotNumber,
			onSuccess: OnFistAPISuccess,
			onError: OnAPIError
		);
	}

	/// <summary>
	/// 定期获取配置信息，API获取配置API回调
	/// </summary>
	private void OnSuccessGetConfig(List<List<int>> ConfigData)
	{
		djcConfig = ConfigData;
		//Debug.Log("phoneTableScript OnSuccessGetConfig() 获取配置成功！");
		//string strConfig = "";
		//foreach (var djc in djcConfig)
		//{
		//	strConfig = strConfig + string.Join(",", djc) + "\n";
		//}
		//Debug.Log(strConfig);

		// 调用API获取热力图温度数据
		temperatureAPI.GetTemperatureData(
			companyIndex,
			slotNumber,
			onSuccess: OnAPISuccess,
			onError: OnAPIError
		);
	}

	/// <summary>
	/// 获取配置错误API错误回调
	/// </summary>
	private void OnGetConfigError(string errorMessage)
	{
		Debug.LogError($"调用API获取配置失败{errorMessage}");
	}

	/// <summary>
	/// API成功回调 -获取历史温度数据
	/// </summary>
	private void onSuccessGetHisTmpData(HisTemDataDB hisTempData)
	{
		HistoryTemperatureDatDB = hisTempData;
		// 调试输出历史数据
		Debug.Log("phoneTableScript onSuccessGetHisTmpData() 获取历史数据成功！");
		for (int i = 0; i < HistoryTemperatureDatDB.HistoryTemperatureValues.Count; i++)
		{
			Debug.Log($"index{i} :"+string.Join(",", HistoryTemperatureDatDB.HistoryTemperatureValues[i]));
		}

		// 隐藏获取历史温度数据提示
		getHisTmpNoticeText.text = "";
	}

	/// <summary>
	/// 获取历史温度数据
	/// </summary>
	public void OnUpdataButtonClick()
	{
		lineChart.SetActive(false);
		// 设置点击表格颜色，取消选中表格颜色		
		pointedTable.selectColor = new Color(1f, 1f, 1f, 0f);
		//Debug.Log($"更新按钮被点击！槽号 = {slotInputText.text}");
		//if (slotNumber != slotInputField.text)
		if (slotNumber == "" || slotNumber != slotInputText.text)
		{
			//slotNumber = slotInputField.text;
			slotNumber = slotInputText.text;
			// 将图表全部数据清零
			ClearTableData();

			//获取配置、实时温度数据更新表格
			temperatureAPI.GetDjcConfig(
						companyIndex,
						slotNumber,
						onConfigSuccess: OnFistSuccessGetConfig,
						onConfigError: OnGetConfigError
						);
		}
		else { 
			Debug.Log("参数信息没变化，请勿重复点击！");
		}
		nextExecutionTime = Time.time + 5;  //5秒
		nextGetHisTmpTime = Time.time + 600; //10分钟 600秒
	}

	// 热力图清零
	public void ClearTableData()
	{

		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 28; j++)
			{
				shangTable.UpdateCell(i, j, 0);
				xiaTable.UpdateCell(i, j, 0);
			}
		}

		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 14; j++)
			{
				zhongJianTable.UpdateCell(i, j, 0);
			}
		}

		for (int i = 0; i < 4; i++)
		{
			zuoYiTable.UpdateCell(i, 0, 0);
			youYiTable.UpdateCell(i, 0, 0);			
		}

		for (int i = 0; i < 5; i++)
		{
			zuoErTable.UpdateCell(i, 0, 0);
			youErTable.UpdateCell(i, 0, 0);
		}
	}

	//关闭折线图
	public void closeLinechart()
	{
		lineChart.SetActive(false);
		// 设置点击表格颜色，取消选中表格颜色		
		pointedTable.selectColor = new Color(1f, 1f, 1f, 0f);
	}

	//更新折线图数据
	void UpdateDataLineCharts(HisTemDataDB HistoryTemperatureDatDB, int sampleindex)
	{
		//1.修改主组件上的参数
		LineChart smootLineChart = lineChart.ConvertTo<LineChart>();
		var title = smootLineChart.EnsureChartComponent<Title>();
		var xAxis = smootLineChart.EnsureChartComponent<XAxis>();
		xAxis.splitNumber = 10;
		xAxis.boundaryGap = true;
		xAxis.type = Axis.AxisType.Category;
		var dataZoom = smootLineChart.EnsureChartComponent<DataZoom>();
		dataZoom.enable = true;
		dataZoom.supportInside = true;
		dataZoom.supportSlider = true;

		//2.给主组件添加额外组件
		var legend = smootLineChart.EnsureChartComponent<Legend>();

		//3.修改serie的参数
		var serie = smootLineChart.GetSerie<Line>();//通过类型获取serie
		serie.serieName = "";
		serie.symbol.show = false;
		serie.state = SerieState.Normal;
		serie.animation.enable = true;
		serie.large = true;

		//5.修改数据项SerieData上的参数
		smootLineChart.ClearData();
		for (int i = 0; i < 180; i++)
		{
			smootLineChart.AddData(0, HistoryTemperatureDatDB.HistoryTemperatureValues[i][sampleindex]);
		}
		for (int i = 0; i < 180; i++)
		{
			xAxis.AddData("");
			if (i % 20 == 0)
			{
				var serieData = serie.GetSerieData(i); //从已有数据中获取
				var itemStyle = serieData.EnsureComponent<LabelStyle>(); //启用SerieData的Component
				itemStyle.offset = new Vector3(0, 20, 0);
			}

		}
		smootLineChart.RefreshChart();//刷新图表绘制
	}

	// 热力图位置信息更新
	public void UpdatePosInfo(bool flag)
	{		
		
		string[,] shangTableInfo = new string[,]
		{
			{"WB28", "WB27", "WB26", "WB25", "WB24", "WB23", "WB22", "WB21", "WB20", "WB19", "WB18", "WB17", "WB16", "WB15", "WB14", "WB13", "WB12", "WB11", "WB10", "WB9", "WB8", "WB7", "WB6", "WB5", "WB4", "WB3", "WB2", "WB1"},
			{"B28-2","B27-2","B26-2","B25-2","B24-2","B23-2","B22-2","B21-2","B20-2","B19-2","B18-2","B17-2","B16-2","B15-2","B14-2","B13-2","B12-2","B11-2","B10-2","B9-2","B8-2","B7-2","B6-2","B5-2","B4-2","B3-2","B2-2","B1-2"},
			{"B28-1","B27-1","B26-1","B25-1","B24-1","B23-1","B22-1","B21-1","B20-1","B19-1","B18-1","B17-1","B16-1","B15-1","B14-1","B13-1","B12-1","B11-1","B10-1","B9-1","B8-1","B7-1","B6-1","B5-1","B4-1","B3-1","B2-1","B1-1"}
		};
		string[,] shangTablePos = new string[,]
		{
			{"165","162","159","156","153","150","147","144","141","138","135","132","129","126","123","120","117","114","111","108","105","102","99","96","93","90","87","84"},
			{"167","164","161","158","155","152","149","146","143","140","137","134","131","128","125","122","119","116","113","110","107","104","101","98","95","92","89","86"},
			{"166","163","160","157","154","151","148","145","142","139","136","133","130","127","124","121","118","115","112","109","106","103","100","97","94","91","88","85"}
		};
		string[,] zhongTbaleInfo = new string[,]
		{
			{"BB28","BB26","BB24","BB22","BB20","BB18","BB16","BB14","BB12","BB10","BB8","BB6","BB4","BB2"},
			{"BB27","BB25","BB23","BB21","BB19","BB17","BB15","BB13","BB11","BB9","BB7","BB5","BB3","BB1"}
		};
		string[,] zhongTbalePos = new string[,]
		{
			{"213","212","211","210","209","208","207","206","205","204","203","202","201","200"},
			{"199","198","197","196","195","194","193","192","191","190","189","188","187","186"}
		};
		string[,] xiaTableInfo = new string[,]
		{
			{"A28-1","A27-1","A26-1","A25-1","A24-1","A23-1","A22-1","A21-1","A20-1","A19-1","A18-1","A17-1","A16-1","A15-1","A14-1","A13-1","A12-1","A11-1","A10-1","A9-1","A8-1","A7-1","A6-1","A5-1","A4-1","A3-1","A2-1","A1-1"},
			{"A28-2","A27-2","A26-2","A25-2","A24-2","A23-2","A22-2","A21-2","A20-2","A19-2","A18-2","A17-2","A16-2","A15-2","A14-2","A13-2","A12-2","A11-2","A10-2","A9-2","A8-2","A7-2","A6-2","A5-2","A4-2","A3-2","A2-2","A1-2"},
			{"WA28", "WA27", "WA26", "WA25", "WA24", "WA23", "WA22", "WA21", "WA20", "WA19", "WA18", "WA17", "WA16", "WA15", "WA14", "WA13", "WA12", "WA11", "WA10", "WA9", "WA8", "WA7", "WA6", "WA5", "WA4", "WA3", "WA2", "WA1"}
		};
		string[,] xiaTablePos = new string[,]
		{
			{"82","79","76","73","70","67","64","61","58","55","52","49","46","43","40","37","34","31","28","25","22","19","16","13","10","7","4","1"},
			{"83","80","77","74","71","68","65","62","59","56","53","50","47","44","41","38","35","32","29","26","23","20","17","14","11","8","5","2"},
			{"81","78","75","72","69","66","63","60","57","54","51","48","45","42","39","36","33","30","27","24","21","18","15","12","9","6","3","0"}
		};
		string[] zuoTbaleInfo = new string[]{ "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9" };
		string[] youTbaleInfo = new string[]{ "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9" };
		string[] zuoTbalePos = new string[] { "177", "178", "179", "180", "181", "182", "183", "184", "185" };
		string[] youTbalePos = new string[] { "168", "169", "170", "171", "172", "173", "174", "175", "176" };

		if (flag) //如果位置信息复选框被选中，显示采集点位置信息
		{
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 28; j++)
				{
					shangTable_pos.UpdateCell(i, j, shangTablePos[i, j]);
					xiaTable_pos.UpdateCell(i, j, xiaTablePos[i, j]);
				}
			}

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 14; j++)
				{
					zhongJianTable_pos.UpdateCell(i, j, zhongTbalePos[i, j]);
				}
			}

			for (int i = 0; i < 4; i++)
			{
				zuoYiTable_pos.UpdateCell(i, 0, zuoTbalePos[i]);
				youYiTable_pos.UpdateCell(i, 0, youTbalePos[i]);
			}

			for (int i = 0; i < 5; i++)
			{
				zuoErTable_pos.UpdateCell(i, 0, zuoTbalePos[i + 4]);
				youErTable_pos.UpdateCell(i, 0, youTbalePos[i + 4]);
			}
		}
		else //如果位置信息复选框未选中，显示默认位置信息
		{
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 28; j++)
				{
					shangTable_pos.UpdateCell(i, j, shangTableInfo[i, j]);
					xiaTable_pos.UpdateCell(i, j, xiaTableInfo[i, j]);
				}
			}

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 14; j++)
				{
					zhongJianTable_pos.UpdateCell(i, j, zhongTbaleInfo[i, j]);
				}
			}

			for (int i = 0; i < 4; i++)
			{
				zuoYiTable_pos.UpdateCell(i, 0, zuoTbaleInfo[i]);
				youYiTable_pos.UpdateCell(i, 0, youTbaleInfo[i]);
			}

			for (int i = 0; i < 5; i++)
			{
				zuoErTable_pos.UpdateCell(i, 0, zuoTbaleInfo[i + 4]);
				youErTable_pos.UpdateCell(i, 0, youTbaleInfo[i + 4]);
			}
		}
	}

	// 热力图槽号更新，获取槽号信息
	public void onSlotChanged()
	{ 
		//Debug.Log($"槽号信息变化，槽号是：onSlotChanged={slotInputText.text}");
		// 当前槽号发生改变，更新热力图信息
		OnUpdataButtonClick();
	}
}
