using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 在现有的GetTemperature类中添加新的方法
public class GetTemperature : MonoBehaviour
{
    // ... 现有代码 ...
    
    // 新增：返回二维数据结构的方法
    public IEnumerator WebAPIPostHelperAsync(string companyindex, string caohao, Action<List<List<int>>> callback)
    {
        Debug.Log("[GetTemperature] 开始异步获取温度矩阵数据 - 公司索引: " + companyindex + ", 槽号: " + caohao);
        
        List<List<int>> temperatureMatrix = new List<List<int>>();
        string url = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
        string test = companyindex + "-8-" + caohao;
        
        string jsonData = "\"" + test + "\"";
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
        
        UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string readerContentFromServer = webRequest.downloadHandler.text;
            int pos = readerContentFromServer.IndexOf('"');
            
            if (pos == 0)
            {
                string tmpText = readerContentFromServer.Substring(1, readerContentFromServer.Length - 2);
                
                // 处理Base64填充
                int paddingCount = tmpText.Length % 4;
                if (paddingCount > 0)
                {
                    for (int i = 0; i < 4 - paddingCount; i++)
                    {
                        tmpText += "=";
                    }
                }
                
                try
                {
                    byte[] tmpBuffer = Convert.FromBase64String(tmpText);
                    Debug.Log("[GetTemperature] Base64解码成功，字节数: " + tmpBuffer.Length);
                    
                    if (tmpBuffer.Length > 0)
                    {
                        List<int> list = new List<int>();
                        List<int> result1 = new List<int>();
                        
                        // 按照Java代码的逻辑处理数据
                        int jk = tmpBuffer.Length / 32;
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
                        jk = tmpBuffer.Length % 32;
                        if (jk != 0)
                        {
                            for (int j = 0; j < jk; j++)
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
                        
                        // 按照Java代码逻辑处理240个测温点
                        int k = 12;
                        for (int i = 0; i < 240; i++)
                        {
                            List<int> sensorData = new List<int>();
                            temperatureMatrix.Add(sensorData);
                            
                            // 每个测温点从k开始，每隔480个字节取一个数据点
                            for (int j = k; j < 144012 && j + 1 < result1.Count; j += 480)
                            {
                                int data = (result1[j]) * 256 + result1[j + 1];
                                temperatureMatrix[i].Add(data);
                            }
                            k += 2;
                        }
                        
                        Debug.Log($"[GetTemperature] 成功处理240个测温点，每个点有{(temperatureMatrix.Count > 0 ? temperatureMatrix[0].Count : 0)}个数据");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[GetTemperature] Base64解码错误: " + e.Message);
                }
            }
        }
        else
        {
            Debug.LogError("[GetTemperature] 请求错误: " + webRequest.error);
        }
        
        callback(temperatureMatrix);
    }
}