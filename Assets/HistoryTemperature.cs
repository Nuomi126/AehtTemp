using System;
using System.Collections;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Text;
//using System.Threading.Tasks;
using TMPro;
//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
//using UnityEngine.UI;
using static HistoryTemperature;


public class HisTemDataDB
{
	public string CompanyIndex; //企业号
	public string slotNumber; // 槽号
	public string Date;  //历史数据日期
    public List<DateTime> DateList; //历史数据采集时间列表
	public List<List<int>> HistoryTemperatureValues; // 存储历史温度数据列表
    public HisTemDataDB()
    {
		CompanyIndex = "1033";
        slotNumber = "1001";
		DateTime currentDate = DateTime.Today;
		Date = currentDate.ToString("yyyyMMdd");
		DateList = new List<DateTime>();
		HistoryTemperatureValues = new List<List<int>>();
	}
}
public class HistoryTemperature : MonoBehaviour
{
    [Header("默认参数值")]
    public string defaultCompanyIndex = "1033";
    public int defaultSlotNumber = 1001;
    public string defaultDate = "20250708";

    [Header("API设置")]
    public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";

    //public List<List<int>> HistoryTemperatureValues; // 存储历史温度数据列表
	private Coroutine currentRequest;
	
	[System.Serializable]
    public class HistoryTemperatureData
    {
        public DateTime date;
        public int recordNumber;
        public List<int> temperatureValues; // 253个采样点的温度数据

        public HistoryTemperatureData()
        {
            temperatureValues = new List<int>();
        }
    }

    void Start()
    {        
		// 获取输入参数		
		string slotNumberStr = defaultSlotNumber.ToString();
		if (!int.TryParse(slotNumberStr, out int slotNumber))
		{
			Debug.LogError("错误：槽号必须是数字！");
			return;
		}       
	}

    public void GetHisTempData(string CompanyIndex, string slotNumber, string Date,System.Action<HisTemDataDB> onSuccessHisTmpData)
    {

		if (currentRequest != null)
		{
			StopCoroutine(currentRequest);
		}        
		currentRequest = StartCoroutine(GetHistoryTemperatureData(CompanyIndex, slotNumber, Date,onSuccessHisTmpData));
	}

	private IEnumerator GetHistoryTemperatureData(string companyIndex,string slotNumber,string date,System.Action<HisTemDataDB> onSuccessHisTmpData)
    {
		//定义历史温度数据对象
		HisTemDataDB HistoryTemperatureDatDB = new();

		// 构建请求参数：电解企业标识 + "-67-" + 槽号 + "-" + 日期
		string requestData = $"{companyIndex}-67-{slotNumber}-{date}";
        string jsonData = $"\"{requestData}\"";
     
        //Debug.Log($"请求参数: {requestData}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                //Debug.Log($"API响应: {responseText}");               
                try
                {
					HistoryTemperatureDatDB = ProcessHistoryTemperatureResponse(responseText, companyIndex, slotNumber, date);
                    if (HistoryTemperatureDatDB != null && HistoryTemperatureDatDB.DateList.Count > 0)
                    {
                        HistoryTemperatureDatDB.CompanyIndex = companyIndex;
                        HistoryTemperatureDatDB.slotNumber = slotNumber;
                        HistoryTemperatureDatDB.Date = date;
                        // 触发成功回调
                        onSuccessHisTmpData?.Invoke(HistoryTemperatureDatDB);
                    }
                    else
                    {
                        string errorMsg = "温度数据解析失败";
                        Debug.LogError(errorMsg);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"数据处理异常: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"API请求失败: {request.error}");
            }
        }		

	}

    private HisTemDataDB ProcessHistoryTemperatureResponse(string responseText, string companyIndex, string slotNumber, string date)
    {
		//定义历史温度数据对象
		HisTemDataDB HistoryTemperatureDatDB = new();

		// 去掉响应字符串的引号
		string base64Data = responseText.Trim();
        
        if (base64Data.StartsWith("\"") && base64Data.EndsWith("\""))
        {
            base64Data = base64Data.Substring(1, base64Data.Length - 2);
        }

        // Base64填充处理
        int paddingCount = base64Data.Length % 4;
        if (paddingCount > 0)
        {
            base64Data += new string('=', 4 - paddingCount);
        }

        // Base64解码
        byte[] frameData;
        try
        {
            frameData = Convert.FromBase64String(base64Data);
        }
        catch (FormatException ex)
        {
            Debug.LogError($"Base64解码失败！\n错误信息: {ex.Message}");
            return null;
        }

		//Debug.Log($"解码后数据长度: {frameData.Length} 字节");

		// 解析帧格式
		HistoryTemperatureDatDB = ParseFrameData(frameData, out List<HistoryTemperatureData> historyData);
		if (HistoryTemperatureDatDB == null )
        {
            Debug.LogError("帧数据解析失败！");
            return null;
        }

        return HistoryTemperatureDatDB;

	}

    private HisTemDataDB ParseFrameData(byte[] frameData, out List<HistoryTemperatureData> historyData)
    {
        historyData = new List<HistoryTemperatureData>();
        //定义历史温度数据对象
		HisTemDataDB HistoryTemperatureDatDB = new();
		// 初始化历史温度数据列表

		if (frameData.Length < 14) // 最小帧长度：帧头(2) + 企业标识(2) + 帧类型(2) + 槽号(2) + 数据长度(4) + 帧尾(2)
        {
            Debug.LogError("帧数据长度不足");
            return null;
        }

        int offset = 0;

        // 验证帧头：170, 85
        if (frameData[offset] != 170 || frameData[offset + 1] != 85)
        {
            Debug.LogError($"帧头验证失败: {frameData[offset]}, {frameData[offset + 1]} (期望: 170, 85)");
            return null;
        }
        offset += 2;

        // 读取服务电解企业标识 (2字节)
        int enterpriseId = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;

        // 读取帧类型 (2字节)
        int frameType = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;

        // 读取槽号 (2字节)
        int slotNumber = (frameData[offset] << 8) | frameData[offset + 1];
        offset += 2;

        // 读取有效数据长度 (4字节)
        int dataLength = (frameData[offset] << 24) | (frameData[offset + 1] << 16) |
                        (frameData[offset + 2] << 8) | frameData[offset + 3];
        offset += 4;

        Debug.Log($"企业标识: {enterpriseId}, 帧类型: {frameType}, 槽号: {slotNumber}, 数据长度: {dataLength}");

        // 验证数据长度
        if (offset + dataLength + 2 > frameData.Length)
        {
            Debug.LogError($"数据长度验证失败: 期望 {offset + dataLength + 2}, 实际 {frameData.Length}");
            return null;
        }

        // 验证帧尾
        int tailOffset = offset + dataLength;
        if (frameData[tailOffset] != 165 || frameData[tailOffset + 1] != 90)
        {
            Debug.LogError($"帧尾验证失败: {frameData[tailOffset]}, {frameData[tailOffset + 1]} (期望: 165, 90)");
            return null;
        }

        // 解析历史温度数据
        // 数据长度为512的整数倍，每512字节包含：日期(4字节) + 记录号(2字节) + 253个采样点数据(每个2字节)
        int recordSize = 512;
        int recordCount = dataLength / recordSize;

        //Debug.Log($"历史记录数量: {recordCount}");

        for (int i = 0; i < recordCount; i++)
        {
            int recordOffset = offset + i * recordSize;

            var historyRecord = new HistoryTemperatureData();

            // 读取日期 (4字节) - 假设为Unix时间戳
            int dateValue = (frameData[recordOffset] << 24) | (frameData[recordOffset + 1] << 16) |
                           (frameData[recordOffset + 2] << 8) | frameData[recordOffset + 3];

            // 将日期值转换为DateTime（这里需要根据实际格式调整）
            try
            {
                historyRecord.date = DateTimeOffset.FromUnixTimeSeconds(dateValue).DateTime;
            }
            catch
            {
                // 如果不是Unix时间戳，使用其他方式解析
                historyRecord.date = DateTime.Now;
            }

            // 读取记录号 (2字节)
            historyRecord.recordNumber = (frameData[recordOffset + 4] << 8) | frameData[recordOffset + 5];

            // 读取253个采样点数据 (每个2字节)
            for (int j = 0; j < 253; j++)
            {
                int sampleOffset = recordOffset + 6 + j * 2;
                int temperatureValue = (frameData[sampleOffset] << 8) | frameData[sampleOffset + 1];
                historyRecord.temperatureValues.Add(temperatureValue);
            }

            historyData.Add(historyRecord);

            // 将采样点温度数据保存到历史记录对象
            if (historyRecord.temperatureValues[0] != 0)
            {
				HistoryTemperatureDatDB.DateList.Add(historyRecord.date);
				HistoryTemperatureDatDB.HistoryTemperatureValues.Add(historyRecord.temperatureValues);
			}
		}            
        Debug.Log("HistoryTemperatureDatDB.DateList.Count = " + HistoryTemperatureDatDB.DateList.Count);
		return HistoryTemperatureDatDB;
    }

 //   public Task<List<List<int>>> HisTmpSync()
 //   {
	//	// 获取输入参数
	//	string companyIndex = defaultCompanyIndex;
	//	string slotNumberStr = defaultslotNumber.ToString();
	//	string date = defaultDate;
       
	//	var HisTmpValues = new TaskCompletionSource<List<List<int>>>();

	//	if (!int.TryParse(slotNumberStr, out int slotNumber))
	//	{
	//		Debug.LogError("错误：槽号必须是数字！");
	//		return null;
	//	}

	//	//Debug.Log("正在获取单槽温度历史数据，请稍候...");        
	//	StartCoroutine(GetHistoryTemperatureData(companyIndex, slotNumber, date, HisTmpValues));

	//	return HisTmpValues.Task;
 //   }

	//public async void GetHisTmpValues(List<List<int>> HisTmpValues)
	//{
	//	HisTmpValues = await HisTmpSync();
		
	//}
}