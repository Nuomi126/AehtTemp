using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class TemperatureRequest
{
	public string companyIndex;
	public string caoHao;
	public string timestamp;
}

[System.Serializable]
public class TemperatureResponse
{
	public bool success;
	public string message;
	public string data; // Base64编码的温度数据
	public int dataLength;
}

public class TemperatureAPIManager : MonoBehaviour
{
	[Header("API配置")]
	public string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
	string companyIndex;	// 企业标识
	string slotNumber;   // 电解槽号
	public string defaultCompanyId = "1033"; // 默认企业标识

	[Header("数据配置")]
	public int expectedDataLength = 480; // 480字节
	public int temperaturePointCount = 240; // 240个测温点

	[Header("调试选项")]
	public bool enableDebugLog = true;

	// 事件回调
	public System.Action<List<float>> OnTemperatureDataReceived;
	public System.Action<string> OnAPIError;

	public System.Action<List<List<int>>> OnConfigDataReceived;

	// 定义电解槽配置列表
	List<List<int>> djcConfig = new List<List<int>>();

	private Coroutine currentRequest;

	// 新增：存储最新的温度数据，供其他脚本调用
	[Header("温度数据存储")]
	[SerializeField] private List<float> latestTemperatureData = new List<float>();

	/// <summary>
	/// 获取最新的温度数据（只读）
	/// </summary>
	public List<float> LatestTemperatureData
	{
		get { return new List<float>(latestTemperatureData); } // 返回副本，防止外部修改
	}


	// 温度传感器配置数据结构（每12字节一组）
	[System.Serializable]
	public class TemperatureSensorConfig
	{
		public byte[] rawData; // 原始12字节数据
		public ushort sn; // 序号
		public ushort sampleindex; // 采样编号
		public ushort posindex; // 位置编号
		public ushort directiontype; // 方向类型
		public ushort directionindex; // 方向编号
		public ushort nodeindex; // 节点号

		public TemperatureSensorConfig(byte[] data)
		{
			if (data.Length != 12)
			{
				throw new ArgumentException("温度传感器配置数据必须为12字节");
			}

			rawData = new byte[12];
			Array.Copy(data, rawData, 12);

			ParseFields();
		}

		private void ParseFields()
		{
			// 解析各个字段（大端序）
			sn = (ushort)((rawData[0] << 8) | rawData[1]);
			sampleindex = (ushort)((rawData[2] << 8) | rawData[3]);
			posindex = (ushort)((rawData[4] << 8) | rawData[5]);
			directiontype = (ushort)((rawData[6] << 8) | rawData[7]);
			directionindex = (ushort)((rawData[8] << 8) | rawData[9]);
			nodeindex = (ushort)((rawData[10] << 8) | rawData[11]);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"=== 温度传感器配置 {sn} ===");
			sb.AppendLine($"序号: {sn}");
			sb.AppendLine($"采样编号: {sampleindex}");
			sb.AppendLine($"位置编号: {posindex} ({GetPositionText(posindex)})");
			sb.AppendLine($"方向类型: {directiontype} ({GetdirectiontypeText(directiontype)})");
			sb.AppendLine($"方向编号: {directionindex} ({GetdirectionindexText(directionindex)})");
			sb.AppendLine($"节点号: {nodeindex}");
			sb.AppendLine($"原始数据: {BitConverter.ToString(rawData)}");
			sb.AppendLine();

			return sb.ToString();
		}

		// 将配置信息转换为数组
		public List<int> ToArray()
		{		
			List<int> array = new List<int>();
			array.Add(sampleindex);  //采样编号
			array.Add(posindex);    // 位置编号
			array.Add(directiontype);   //方向类型
			array.Add(directionindex);//方向编号
			array.Add(nodeindex);   //节点号			
			return array;
		}

		private string GetPositionText(ushort position)
		{
			return position switch
			{
				1 => "钢窗",
				2 => "出铝端",
				3 => "烟道端",
				4 => "阴极钢棒",
				5 => "炉底",
				_ => "未知位置"
			};
		}

		private string GetdirectiontypeText(ushort directiontype)
		{
			return directiontype switch
			{
				0 => "无A/B面分别位置",
				1 => "A面",
				2 => "B面",
				_ => "未知方向类型"
			};
		}

		private string GetdirectionindexText(ushort directionindex)
		{
			return directionindex switch
			{
				0 => "只有一个测量点",
				1 => "阴极钢棒-1位置点",
				2 => "阴极钢棒-2位置点",
				_ => "未知方向编号"
			};
		}
	}

	private List<TemperatureSensorConfig> temperatureSensorConfigs = new List<TemperatureSensorConfig>();

	/// <summary>
	/// 获取指定索引的温度值
	/// </summary>
	/// <param name="index">温度点索引（0-239）</param>
	/// <returns>温度值，如果索引无效返回0</returns>
	public float GetTemperatureAtIndex(int index)
	{
		if (index >= 0 && index < latestTemperatureData.Count)
		{
			return latestTemperatureData[index];
		}
		Debug.LogWarning($"[TemperatureAPI] 温度数据索引{index}超出范围，当前数据长度：{latestTemperatureData.Count}");
		return 0f;
	}

	/// <summary>
	/// 获取温度数据的数量
	/// </summary>
	public int GetTemperatureDataCount()
	{
		return latestTemperatureData.Count;
	}

	/// <summary>
	/// 检查是否有有效的温度数据
	/// </summary>
	public bool HasValidTemperatureData()
	{
		return latestTemperatureData.Count > 0;
	}



	/// <summary>
	/// 获取温度数据（带回调）
	/// </summary>
	public void GetTemperatureData(string companyIndex, string slotNumber, System.Action<List<float>> onSuccess, System.Action<string> onError = null)
	{
		if (currentRequest != null)
		{
			StopCoroutine(currentRequest);
		}

		currentRequest = StartCoroutine(GetTemperatureDataCoroutine(companyIndex, slotNumber, onSuccess, onError));
	}

	/// <summary>
	/// 核心协程方法 - 修改为匹配Java版本的请求格式
	/// </summary>
	private IEnumerator GetTemperatureDataCoroutine(string companyIndex, string slotNumber, System.Action<List<float>> onSuccess, System.Action<string> onError)
	{
		// 构建请求数据（与GetTemperature.cs保持一致）
		string test = companyIndex + "-7-" + slotNumber;  // 注意这里是-8-而不是-7-
		string jsonData = "\"" + test + "\"";
		byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

		if (enableDebugLog)
		{
			Debug.Log($"[TemperatureAPI] 发送请求到: {apiUrl}");
			Debug.Log($"[TemperatureAPI] 请求数据: {jsonData}");
		}

		// 创建请求
		UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
		request.uploadHandler = new UploadHandlerRaw(byteArray);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		//request.timeout = requestTimeout;

		// 发送请求
		yield return request.SendWebRequest();

		// 处理响应
		if (request.result == UnityWebRequest.Result.Success)
		{
			try
			{
				string responseText = request.downloadHandler.text;

				if (enableDebugLog)
				{
					Debug.Log($"[TemperatureAPI] 收到响应: {responseText}");
				}

				// 按照GetTemperature.cs的逻辑处理响应
				List<float> temperatureData = ParseTemperatureDataLikeGetTemperature(responseText);

				if (temperatureData != null && temperatureData.Count > 0)
				{
					// 触发成功回调
					OnTemperatureDataReceived?.Invoke(temperatureData);
					onSuccess?.Invoke(temperatureData);

					if (enableDebugLog)
					{
						Debug.Log($"[TemperatureAPI] 成功获取{temperatureData.Count}个温度数据点");
					}
				}
				else
				{
					string errorMsg = "温度数据解析失败";
					HandleError(errorMsg, onError);
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"响应解析异常: {ex.Message}";
				HandleError(errorMsg, onError);
			}
		}
		else
		{
			string errorMsg = $"网络请求失败: {request.error}";
			HandleError(errorMsg, onError);
		}
	}

	/// <summary>
	/// 按照GetTemperature.cs的逻辑解析温度数据
	/// </summary>
	private List<float> ParseTemperatureDataLikeGetTemperature(string responseText)
	{
		try
		{
			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] 原始响应数据: {responseText}");
				Debug.Log($"[TemperatureAPI] 响应数据长度: {responseText.Length}");
			}

			// 检查响应格式
			int pos = responseText.IndexOf('"');
			if (pos != 0)
			{
				Debug.LogError($"[TemperatureAPI] 响应格式不正确，第一个引号位置: {pos}");
				return null;
			}

			// 提取Base64字符串
			string tmpText = responseText.Substring(1, responseText.Length - 2);

			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] 提取的Base64字符串: {tmpText}");
				Debug.Log($"[TemperatureAPI] Base64字符串长度: {tmpText.Length}");
			}

			// 处理Base64填充（关键修复）
			int paddingCount = tmpText.Length % 4;
			if (paddingCount > 0)
			{
				for (int i = 0; i < 4 - paddingCount; i++)
				{
					tmpText += "=";
				}
				if (enableDebugLog)
				{
					Debug.Log($"[TemperatureAPI] 添加了{4 - paddingCount}个填充字符");
				}
			}

			// Base64解码
			byte[] tmpBuffer = Convert.FromBase64String(tmpText);

			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] Base64解码成功，字节数: {tmpBuffer.Length}");
				if (tmpBuffer.Length > 0)
				{
					string hexString = "";
					int maxBytes = Math.Min(20, tmpBuffer.Length);
					for (int i = 0; i < maxBytes; i++)
					{
						hexString += tmpBuffer[i].ToString("X2") + " ";
					}
					Debug.Log($"[TemperatureAPI] 前{maxBytes}字节数据(十六进制): {hexString}");
				}
			}

			if (tmpBuffer.Length == 0)
			{
				Debug.LogError("[TemperatureAPI] 解码后的数据为空");
				return null;
			}

			// 按照GetTemperature.cs的逻辑处理数据
			List<int> list = new List<int>();
			List<int> result1 = new List<int>();

			// 处理32字节块
			int jk = tmpBuffer.Length / 32;
			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] 32字节块数量: {jk}");
			}

			for (int j = 0; j < jk; j++)
			{
				for (int jt = 0; jt < 32; jt++)
				{
					int offset = j * 32 + jt;
					int unsignedByte = tmpBuffer[offset] & 0xFF;
					list.Add(unsignedByte);
				}
				list.Add(-1); // 分隔符
			}

			// 处理剩余字节
			int offsetRemain = jk * 32;
			int remainingBytes = tmpBuffer.Length % 32;
			if (remainingBytes != 0)
			{
				if (enableDebugLog)
				{
					Debug.Log($"[TemperatureAPI] 剩余字节数: {remainingBytes}");
				}
				for (int j = 0; j < remainingBytes; j++)
				{
					int unsignedByte = tmpBuffer[offsetRemain + j] & 0xFF;
					list.Add(unsignedByte);
				}
			}

			// 移除分隔符
			foreach (int value in list)
			{
				if (value != -1)
				{
					result1.Add(value);
				}
			}

			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] 过滤后的数据长度: {result1.Count}");
			}

			// 如果只需要240个温度点的简单数据，可以使用这种方式
			List<float> temperatureData = new List<float>();

			// 从第12字节开始，每2字节组合成一个温度值
			for (int i = 12; i < result1.Count - 3 && temperatureData.Count < 240; i += 2)
			{
				if (i + 1 < result1.Count)
				{
					int tmp = result1[i] * 256 + result1[i + 1];
					float temperature = tmp; // 转换为实际温度值
					temperatureData.Add(tmp);

					// 输出前几个温度值的详细计算过程
					if (enableDebugLog && temperatureData.Count <= 5)
					{
						Debug.Log($"[TemperatureAPI] 温度值{temperatureData.Count}: {result1[i]} * 256 + {result1[i + 1]} = {tmp} -> {temperature}°C");
					}
				}
			}

			if (enableDebugLog)
			{
				Debug.Log($"[TemperatureAPI] 解析出{temperatureData.Count}个温度值");
				if (temperatureData.Count > 0)
				{
					Debug.Log($"[TemperatureAPI] 第一个温度值: {temperatureData[0]}°C, 最后一个温度值: {temperatureData[temperatureData.Count - 1]}°C");
				}
			}

			return temperatureData;


		}
		catch (Exception ex)
		{
			Debug.LogError($"[TemperatureAPI] 数据解析异常: {ex.Message}");
			Debug.LogError($"[TemperatureAPI] 异常堆栈: {ex.StackTrace}");
			return null;
		}
	}

	/// <summary>
	/// 错误处理
	/// </summary>
	private void HandleError(string errorMessage, System.Action<string> onError)
	{
		Debug.LogError($"[TemperatureAPI] {errorMessage}");
		OnAPIError?.Invoke(errorMessage);
		onError?.Invoke(errorMessage);
	}

	/// <summary>
	/// 停止当前请求
	/// </summary>
	public void StopCurrentRequest()
	{
		if (currentRequest != null)
		{
			StopCoroutine(currentRequest);
			currentRequest = null;
		}
	}

	/// <summary>
	/// 设置API配置
	/// </summary>
	public void SetAPIConfig(string url, string company, string cao)
	{
		apiUrl = url;
		companyIndex = company;
		slotNumber = cao;
	}

	void OnDestroy()
	{
		StopCurrentRequest();
	}

	/// <summary>
	/// 获取电解槽配置列表（带回调）
	/// </summary>
	public void GetDjcConfig(string CompanyIndex, string SlotNumber, System.Action<List<List<int>>> onConfigSuccess, System.Action<string> onConfigError = null)
	{
		if (currentRequest != null)
		{
			StopCoroutine(currentRequest);
		}

		currentRequest = StartCoroutine(GetTemperatureSensorConfig(CompanyIndex, slotNumber, onConfigSuccess, onConfigError));
	}

	private IEnumerator GetTemperatureSensorConfig(string companyIndex, string slotNumber, System.Action<List<List<int>>> onConfigSuccess, System.Action<string> onConfigError = null)
	{
		List<List<int>> djcSensorConfigData = new List<List<int>>();
		// 构建请求参数：企业标识 + "-66"
		string requestParam = $"{companyIndex}-66";

		if (enableDebugLog)
		{
			Debug.Log($"开始获取温度传感器配置信息，参数: {requestParam}");
		}

		// 按照现有API模式创建请求 - 直接用双引号包装字符串
		string jsonData = $"\"{requestParam}\"";
		byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

		// 创建UnityWebRequest
		UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

		request.uploadHandler = new UploadHandlerRaw(byteArray);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");

		if (enableDebugLog)
		{
			Debug.Log($"发送请求到: {apiUrl}");
			Debug.Log($"请求参数: {jsonData}");
		}

		// 发送请求
		yield return request.SendWebRequest();

		if (request.result == UnityWebRequest.Result.Success)
		{
			try
			{
				string responseText = request.downloadHandler.text;
				if (enableDebugLog)
				{
					Debug.Log($"响应内容: {responseText}");
				}				
				// 解析响应
				djcSensorConfigData = ParseTemperatureSensorConfigResponse(responseText);				
				if (djcSensorConfigData != null && djcSensorConfigData.Count > 0)
				{
					// 触发成功回调
					OnConfigDataReceived?.Invoke(djcSensorConfigData);
					onConfigSuccess?.Invoke(djcSensorConfigData);				
					if (enableDebugLog)
					{
						Debug.Log($"[TemperatureAPI] 成功获取{djcSensorConfigData.Count}个配置点");
					}
				}
				else
				{
					string errorMsg = "电解槽配置解析失败";
					HandleError(errorMsg, onConfigError);
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"响应解析异常: {ex.Message}";
				HandleError(errorMsg, onConfigError);
			}
		}
		else
		{
			string errorMsg = $"请求失败: {request.error}";
			Debug.Log(errorMsg);
			Debug.LogError(errorMsg);
			yield break;
		}

	}

	private List<List<int>> ParseTemperatureSensorConfigResponse(string responseText)
	{
		List<List<int>> djcResponsConfigData = new List<List<int>>();
		try
		{
			// 去除可能的引号
			string tmpText = responseText.Trim('\"');

			// 处理转义字符
			if (tmpText.Contains("\\\""))
			{
				tmpText = tmpText.Replace("\\\"", "\"");
			}

			// 将Base64字符串转换为字节数组
			byte[] responseData = Convert.FromBase64String(tmpText);

			if (enableDebugLog)
			{
				Debug.Log($"解码后数据长度: {responseData.Length} 字节");
				Debug.Log($"原始字节数据: {BitConverter.ToString(responseData)}");

				// 按16字节一行显示，便于查看
				for (int i = 0; i < responseData.Length; i += 16)
				{
					int length = Math.Min(16, responseData.Length - i);
					byte[] chunk = new byte[length];
					Array.Copy(responseData, i, chunk, 0, length);
					Debug.Log($"偏移 {i:D4}: {BitConverter.ToString(chunk)}");
				}
			}

			// 解析完整数据帧
			djcResponsConfigData = ParseDataFrame(responseData);
		}
		catch (Exception ex)
		{
			string errorMsg = $"解析温度传感器配置数据时出错: {ex.Message}";
			Debug.LogError(errorMsg);

		}
		return djcResponsConfigData;
	}

	private List<List<int>> ParseDataFrame(byte[] frameData)
	{
		List<List<int>> djcConfigFrameData = new List<List<int>>();

		if (frameData.Length < 14)
		{
			string errorMsg = $"数据帧长度不足：{frameData.Length} 字节，最少需要14字节";
			Debug.LogError(errorMsg);
			return null;
		}

		// 验证帧头：170, 85
		if (frameData[0] != 170 || frameData[1] != 85)
		{
			string errorMsg = $"帧头错误：期望[170,85]，实际[{frameData[0]},{frameData[1]}]";
			Debug.LogError(errorMsg);
			return null;
		}

		// 验证帧尾：165, 90
		if (frameData[frameData.Length - 2] != 165 || frameData[frameData.Length - 1] != 90)
		{
			string errorMsg = $"帧尾错误：期望[165,90]，实际[{frameData[frameData.Length - 2]},{frameData[frameData.Length - 1]}]";
			Debug.LogError(errorMsg);
			return null;
		}

		// 解析帧信息（大端序）
		ushort companyId = (ushort)((frameData[2] << 8) | frameData[3]);
		ushort frameType = (ushort)((frameData[4] << 8) | frameData[5]);
		ushort cellNumber = (ushort)((frameData[6] << 8) | frameData[7]);
		uint dataLength = (uint)((frameData[8] << 24) | (frameData[9] << 16) | (frameData[10] << 8) | frameData[11]);

		if (enableDebugLog)
		{
			Debug.Log($"帧头验证通过: [170, 85]");
			Debug.Log($"企业标识: {companyId}");
			Debug.Log($"帧类型: {frameType}");
			Debug.Log($"槽号: {cellNumber}");
			Debug.Log($"有效数据长度: {dataLength} 字节");
			Debug.Log($"帧尾验证通过: [165, 90]");
		}

		// 验证数据长度
		uint expectedDataLength = (uint)(frameData.Length - 14); // 总长度 - 帧头帧尾和标识信息
		if (dataLength != expectedDataLength)
		{
			string errorMsg = $"数据长度不匹配：声明{dataLength}字节，实际{expectedDataLength}字节";
			Debug.LogError(errorMsg);
			return null;
		}

		// 提取有效数据内容（跳过前12字节的帧头和标识信息，去掉最后2字节的帧尾）
		byte[] validData = new byte[dataLength];
		Array.Copy(frameData, 12, validData, 0, (int)dataLength);

		if (enableDebugLog)
		{
			Debug.Log($"提取有效数据: {validData.Length} 字节");
			Debug.Log($"有效数据内容: {BitConverter.ToString(validData)}");
		}

		// 解析温度传感器配置数据
		djcConfigFrameData = ParseTemperatureSensorData(validData);
		return djcConfigFrameData;
	}


	private List<List<int>> ParseTemperatureSensorData(byte[] data)
	{
		List<List<int>> djcConfigData = new List<List<int>>();
		temperatureSensorConfigs.Clear();

		if (data.Length % 12 != 0)
		{
			string errorMsg = $"有效数据长度错误：{data.Length} 字节，应为12的整数倍";
			Debug.LogError(errorMsg);
			return null;
		}

		int configCount = data.Length / 12;

		if (enableDebugLog)
		{
			Debug.Log($"解析到 {configCount} 个温度传感器配置");
		}

		// 按12字节一组解析配置数据
		for (int i = 0; i < configCount; i++)
		{
			byte[] configData = new byte[12];
			Array.Copy(data, i * 12, configData, 0, 12);

			try
			{
				TemperatureSensorConfig config = new TemperatureSensorConfig(configData);
				temperatureSensorConfigs.Add(config);

				if (enableDebugLog)
				{
					Debug.Log($"配置 {i + 1}: 序号={config.sn}, 采样编号={config.sampleindex}, 位置={config.posindex}, 方向类型={config.directiontype}, 方向编号={config.directionindex}, 节点号={config.nodeindex}");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"解析第 {i + 1} 个配置时出错: {ex.Message}");
			}
		}

		// 显示详细配置信息
		foreach (var tempConfig in temperatureSensorConfigs)
		{
			// 添加到配置列表
			djcConfigData.Add(tempConfig.ToArray());
		}

		// 将配置列表进行排序
		djcConfigData = djcConfigData.OrderBy(x => x[0]).ToList();

		return djcConfigData;
		//// 更新显示
		//UpdateDisplayWithConfigs();
	}

	private void UpdateDisplayWithConfigs()
	{
		// 执行分类
		//ClassifyTemperatureSensorConfigs();

		// 存储到全局管理器
		//StoreToGlobalManager();

		// 显示详细配置信息
		foreach (var config in temperatureSensorConfigs)
		{
			// 添加到配置列表
			djcConfig.Add(config.ToArray());
		}

		// 将配置列表进行排序
		djcConfig = djcConfig.OrderBy(x => x[0]).ToList();
		string strConfig = "";
		foreach (var djc in djcConfig)
		{
			strConfig = strConfig + string.Join(",", djc) + "\n";
		}
		Debug.Log(strConfig);
	}

	public List<List<int>> ApiGetDjcConfig()
	{
		// 获取电解槽配置列表
		string companyId = defaultCompanyId;
		StartCoroutine( GetTemperatureSensorConfig(companyIndex, slotNumber, OnSuccessGetConfig));
		//string strConfig = "";
		//foreach (var djc in djcConfig)
		//{
		//    strConfig = strConfig + string.Join(",", djc) + "\n";
		//}
		//Debug.Log(strConfig);
		return djcConfig;
	}

	private void OnSuccessGetConfig(List<List<int>> ConfigData)
	{
		djcConfig = ConfigData;
		//Debug.Log("OnSuccessGetConfig 获取配置成功！");
		//string strConfig = "";
		//foreach (var djc in djcConfig)
		//{
		//	strConfig = strConfig + string.Join(",", djc) + "\n";
		//}
		//Debug.Log(strConfig);
	}
}