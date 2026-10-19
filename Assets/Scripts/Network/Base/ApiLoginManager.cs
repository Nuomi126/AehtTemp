using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class ApiLoginManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    
    [Header("场景设置")]
    [SerializeField] private string nextSceneName = "MainScene";
    
    // 企业信息数据结构
    [System.Serializable]
    public class CompanyInfo
    {
        public string companyId;
        public int featureFlag;
        
        public CompanyInfo(string id, int flag)
        {
            companyId = id;
            featureFlag = flag;
        }
    }
    
    private void Start()
    {
        // 添加登录按钮事件
        loginButton.onClick.AddListener(OnLoginButtonClick);
        
        // 清空错误信息
        if (errorText != null)
            errorText.text = "";
	}
    
    private void OnLoginButtonClick()
    {
        string username = usernameInput.text;
        string userPassword = passwordInput.text;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userPassword))
        {
            ShowError("用户名和密码不能为空");
            return;
        }
        
        StartCoroutine(LoginWithApi(username, userPassword));
    }
    
    private IEnumerator LoginWithApi(string username, string password)
    {
        // 显示加载状态
        loginButton.interactable = false;
        if (errorText != null)
            errorText.text = "正在验证...";
        
        // 构建登录参数："8888-" + 用户名 + "-" + 密码
        string loginParam = $"8888-{username}-{password}";
        
        // 按照GetTemperature的方式创建JSON数据 - 直接用双引号包装字符串
        string jsonData = $"\"{loginParam}\"";
        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        // 创建UnityWebRequest - 使用与GetTemperature相同的方式
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        // 恢复UI状态
        loginButton.interactable = true;
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"API响应: {responseText}");
            
            // 处理响应 - 可能需要去除首尾的双引号
            string cleanResponse = responseText;
            if (responseText.StartsWith("\"") && responseText.EndsWith("\""))
            {
                cleanResponse = responseText.Substring(1, responseText.Length - 2);
            }
            
            // 解析响应数据
            if (ParseLoginResponse(cleanResponse, username))
            {
                // 等待企业名称获取完成后再跳转场景
                yield return StartCoroutine(WaitForCompanyNameAndLoadScene());
            }
            else
            {
                ShowError("登录失败，请检查用户名和密码");
            }
        }
        else
        {
            Debug.LogError($"API请求失败: {webRequest.error}");
            ShowError($"网络请求失败: {webRequest.error}");
        }
    }
    private IEnumerator WaitForCompanyNameAndLoadScene()
{
    // 等待企业名称获取完成
    float timeout = 10f; // 10秒超时
    float elapsed = 0f;
    
    while (string.IsNullOrEmpty(GlobalUserData.AlSerialPantIdName) && elapsed < timeout)
    {
        yield return new WaitForSeconds(0.1f);
        elapsed += 0.1f;
    }
    
    // 无论是否获取到企业名称，都根据保存的首选配置跳转场景
    string targetSceneName = PlayerPrefs.GetString("DefaultScene", nextSceneName);
    Debug.Log($"[ApiLoginManager] 准备跳转至保存的默认场景: {targetSceneName}");
    SceneManager.LoadScene(targetSceneName);
}
    private bool ParseLoginResponse(string response, string username)
    {
        try
        {
            // 检查响应是否为空或错误
            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("API响应为空");
                return false;
            }
            
            // 解析企业信息：格式如 "1032/0-1033/1-1031/0"
            // 用"-"分割不同企业，用"/"分割企业标识和特征标识
            string[] companies = response.Split('-');
            List<CompanyInfo> companyList = new List<CompanyInfo>();
            
            foreach (string company in companies)
            {
                if (string.IsNullOrEmpty(company.Trim()))
                    continue;
                    
                string[] parts = company.Split('/');
                if (parts.Length == 2)
                {
                    string companyId = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int featureFlag))
                    {
                        companyList.Add(new CompanyInfo(companyId, featureFlag));
                        Debug.Log($"解析企业信息: ID={companyId}, 特征标识={featureFlag}");
                    }
                }
            }
            
            // 检查是否有有效的企业信息
            if (companyList.Count == 0)
            {
                Debug.LogError("未找到有效的企业信息");
                return false;
            }
            
            // 存储用户数据到GlobalUserData
            StoreUserData(username, companyList);
            
            // 移除这里的场景切换，改为在企业名称获取完成后切换
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析登录响应时出错: {ex.Message}");
            return false;
        }
    }
    
    private void StoreUserData(string username, List<CompanyInfo> companies)
    {
        // 清除之前的用户数据
        GlobalUserData.ClearUserData();
        
        // 设置基本用户信息
        // 这里使用用户名的哈希码作为UserId，您可以根据实际需求修改
        GlobalUserData.UserId = username.GetHashCode();
        
        // 如果是超级用户（alwit），可能返回多个企业标识
        // 这里我们使用第一个企业的信息
        if (companies.Count > 0)
        {
            CompanyInfo primaryCompany = companies[0];
            GlobalUserData.AlSerialPantId = primaryCompany.companyId;
            
            // 根据特征标识设置相关信息
            // 特征标识：1代表没有槽控信息，只有温度信息；0代表有完整信息
            GlobalUserData.NetCommIndex = primaryCompany.featureFlag;
            
            // 设置服务器IP（这里使用API的IP，您可以根据需要修改）
            GlobalUserData.LanServerIp = "183.214.222.76";
            
            
            Debug.Log($"准备启动信息获取流程，企业ID: {primaryCompany.companyId}");
            StartCoroutine(GetAllInfoSequentially(primaryCompany.companyId));
            // // 异步获取企业名称
            // Debug.Log($"准备启动GetCompanyNameAsync协程，企业ID: {primaryCompany.companyId}");
            // StartCoroutine(GetCompanyNameAsync(primaryCompany.companyId));
            // Debug.Log("GetCompanyNameAsync协程已启动");

            // // 异步获取车间信息
            // Debug.Log($"准备启动GetWorkshopInfoAsync协程，企业ID: {primaryCompany.companyId}");
            // StartCoroutine(GetWorkshopInfoAsync(primaryCompany.companyId));
            // Debug.Log("GetWorkshopInfoAsync协程已启动");

            // // 异步获取工区信息
            // Debug.Log($"准备启动GetWorkAreaInfoAsync协程，企业ID: {primaryCompany.companyId}");
            // StartCoroutine(GetWorkAreaInfoAsync(primaryCompany.companyId));
            // Debug.Log("GetWorkAreaInfoAsync协程已启动");
            
        }
        

        
        // 如果有多个企业，打印所有企业信息
        if (companies.Count > 1)
        {
            Debug.Log("=== 所有企业信息 ===");
            for (int i = 0; i < companies.Count; i++)
            {
                Debug.Log($"企业{i + 1}: ID={companies[i].companyId}, 特征={companies[i].featureFlag}");
            }
        }
    }

        // 新增：顺序获取所有信息的方法
        private IEnumerator GetAllInfoSequentially(string companyId)
        {
            Debug.Log("=== 开始顺序获取企业信息 ===");
            
            // 1. 首先获取企业名称
            Debug.Log("步骤1: 获取企业名称");
            yield return StartCoroutine(GetCompanyNameAsync(companyId));
            
            // 2. 然后获取车间信息
            Debug.Log("步骤2: 获取车间信息");
            yield return StartCoroutine(GetWorkshopInfoAsync(companyId));
            
            // 3. 最后获取工区信息
            Debug.Log("步骤3: 获取工区信息");
            yield return StartCoroutine(GetWorkAreaInfoAsync(companyId));
            
            Debug.Log("=== 所有信息获取完成 ===");
            
            // 获取完成后，打印最终结果进行验证
            PrintFinalResults();
        }

        // 新增：打印最终结果的方法
        private void PrintFinalResults()
        {
            Debug.Log("=== 最终结果验证 ===");
            Debug.Log($"企业名称: {GlobalUserData.AlSerialPantIdName}");
            Debug.Log($"车间信息字符串: {GlobalUserData.WorkshopInfo}");
            Debug.Log($"车间列表数量: {GlobalUserData.WorkshopList?.Count ?? 0}");
            
            if (GlobalUserData.WorkshopList != null && GlobalUserData.WorkshopList.Count > 0)
            {
                for (int i = 0; i < GlobalUserData.WorkshopList.Count; i++)
                {
                    var workshop = GlobalUserData.WorkshopList[i];
                    Debug.Log($"车间{i + 1}: 号码={workshop.WorkshopNumber}, 名称={workshop.WorkshopName}");
                }
            }
            else
            {
                Debug.LogWarning("车间信息列表仍然为空！");
            }
            
            Debug.Log($"工区列表数量: {GlobalUserData.WorkAreaList?.Count ?? 0}");
        }
// 新增：异步获取工区信息的方法
    private IEnumerator GetWorkAreaInfoAsync(string companyId)
    {
        Debug.Log($"=== 开始获取工区信息 ===");
        Debug.Log($"输入参数 - 企业ID: {companyId}");
        Debug.Log($"API URL: {apiUrl}");
        
        // 构建获取工区信息的参数：企业ID + "-64"
        string workAreaParam = $"{companyId}-64";
        Debug.Log($"构建的参数: {workAreaParam}");
        
        // 按照GetTemperature的方式创建JSON数据
        string jsonData = $"\"{workAreaParam}\"";
        Debug.Log($"JSON数据: {jsonData}");
        
        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);
        Debug.Log($"字节数组长度: {byteArray.Length}");
        
        // 创建UnityWebRequest
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        Debug.Log($"请求创建完成，准备发送...");
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        Debug.Log($"请求完成，状态: {webRequest.result}");
        Debug.Log($"HTTP响应码: {webRequest.responseCode}");
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"原始响应内容: '{responseText}'");
            Debug.Log($"响应长度: {responseText.Length}");
            Debug.Log($"响应是否为空: {string.IsNullOrEmpty(responseText)}");
            
            // 处理响应 - 去除首尾的双引号
            string workAreaInfo = responseText;
            bool hasQuotes = responseText.StartsWith("\"") && responseText.EndsWith("\"");
            Debug.Log($"响应是否包含双引号: {hasQuotes}");
            
            if (hasQuotes)
            {
                workAreaInfo = responseText.Substring(1, responseText.Length - 2);
                Debug.Log($"去除双引号后的工区信息: '{workAreaInfo}'");
            }
            else
            {
                Debug.Log($"响应不包含双引号，直接使用: '{workAreaInfo}'");
            }
            
            // 检查工区信息是否有效
            if (string.IsNullOrEmpty(workAreaInfo) || string.IsNullOrWhiteSpace(workAreaInfo))
            {
                Debug.LogWarning("获取到的工区信息为空或只包含空白字符");
                workAreaInfo = "默认工区";
                Debug.Log($"使用默认工区信息: {workAreaInfo}");
            }
            
            // 解析工区信息
            ParseWorkAreaInfo(workAreaInfo);
            
            Debug.Log($"=== 工区信息获取成功 ===");
        }
        else
        {
            Debug.LogError($"=== 工区信息获取失败 ===");
            Debug.LogError($"错误类型: {webRequest.result}");
            Debug.LogError($"错误信息: {webRequest.error}");
            Debug.LogError($"HTTP响应码: {webRequest.responseCode}");
            
            // 尝试获取错误响应内容
            if (webRequest.downloadHandler != null)
            {
                string errorResponse = webRequest.downloadHandler.text;
                Debug.LogError($"错误响应内容: '{errorResponse}'");
            }
            
            // 如果获取失败，使用默认信息
            Debug.Log("使用默认工区信息");
            // 这里可以设置默认的工区信息到GlobalUserData或其他地方
        }
        
        // 清理资源
        webRequest.Dispose();
        Debug.Log($"=== GetWorkAreaInfoAsync 方法执行完成 ===");
    }

    // 解析工区信息的辅助方法
    private void ParseWorkAreaInfo(string workAreaInfo)
    {
        Debug.Log($"=== 开始解析工区信息 ===");
        Debug.Log($"原始工区信息: '{workAreaInfo}'");
        
        try
        {
            // 清空之前的工区信息
            GlobalUserData.WorkAreaList.Clear();
            
            // 工区信息格式："1/1/1车间1工区-1/2/1车间2工区-1/3/1车间3工区-2/4/2车间1工区-2/5/2车间2工区-2/6/2车间3工区"
            // 用"-"分割不同工区
            string[] workAreas = workAreaInfo.Split('-');
            Debug.Log($"分割后的工区数量: {workAreas.Length}");
            
            List<WorkAreaInfo> workAreaList = new List<WorkAreaInfo>();
            
            foreach (string workArea in workAreas)
            {
                if (string.IsNullOrEmpty(workArea.Trim()))
                    continue;
                    
                Debug.Log($"处理工区信息: '{workArea}'");
                
                // 用"/"分割车间号、工区号和工区名称
                string[] parts = workArea.Split('/');
                if (parts.Length == 3)
                {
                    string workshopNumber = parts[0].Trim();
                    string workAreaNumber = parts[1].Trim();
                    string workAreaName = parts[2].Trim();
                    
                    // 添加到本地列表（用于兼容性）
                    workAreaList.Add(new WorkAreaInfo(workshopNumber, workAreaNumber, workAreaName));
                    
                    // 添加到全局数组
                    GlobalUserData.WorkAreaList.Add(new WorkAreaData(workshopNumber, workAreaNumber, workAreaName));
                    
                    Debug.Log($"解析工区信息: 车间号={workshopNumber}, 工区号={workAreaNumber}, 工区名称={workAreaName}");
                }
                else
                {
                    Debug.LogWarning($"工区信息格式不正确: '{workArea}'");
                }
            }
            
            // 存储工区信息到GlobalUserData（保持原有字符串格式的兼容性）
            if (workAreaList.Count > 0)
            {
                // 将工区信息存储为字符串格式（保持兼容性）
                GlobalUserData.WorkAreaInfo = workAreaInfo; // 存储原始工区信息
                
                Debug.Log($"设置 GlobalUserData.WorkAreaInfo: '{GlobalUserData.WorkAreaInfo}'");
                Debug.Log($"设置 GlobalUserData.WorkAreaList: 共{GlobalUserData.WorkAreaList.Count}个工区");
            }
            
            Debug.Log($"=== 工区信息解析完成，共解析到 {workAreaList.Count} 个工区 ===");
            
            // 打印所有工区信息
            for (int i = 0; i < GlobalUserData.WorkAreaList.Count; i++)
            {
                Debug.Log($"工区{i + 1}: 车间号={GlobalUserData.WorkAreaList[i].WorkshopNumber}, 工区号={GlobalUserData.WorkAreaList[i].WorkAreaNumber}, 工区名称={GlobalUserData.WorkAreaList[i].WorkAreaName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析工区信息时出错: {ex.Message}");
        }
    }

    // 新增：异步获取车间信息的方法
    private IEnumerator GetWorkshopInfoAsync(string companyId)
    {
        Debug.Log($"=== 开始获取车间信息 ===");
        Debug.Log($"输入参数 - 企业ID: {companyId}");
        Debug.Log($"API URL: {apiUrl}");
        
        // 构建获取车间信息的参数：企业ID + "-63"
        string workshopParam = $"{companyId}-63";
        Debug.Log($"构建的参数: {workshopParam}");
        
        // 按照GetTemperature的方式创建JSON数据
        string jsonData = $"\"{workshopParam}\"";
        Debug.Log($"JSON数据: {jsonData}");
        
        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);
        Debug.Log($"字节数组长度: {byteArray.Length}");
        
        // 创建UnityWebRequest
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        Debug.Log($"请求创建完成，准备发送...");
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        Debug.Log($"请求完成，状态: {webRequest.result}");
        Debug.Log($"HTTP响应码: {webRequest.responseCode}");
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"原始响应内容: '{responseText}'");
            Debug.Log($"响应长度: {responseText.Length}");
            Debug.Log($"响应是否为空: {string.IsNullOrEmpty(responseText)}");
            
            // 处理响应 - 去除首尾的双引号
            string workshopInfo = responseText;
            bool hasQuotes = responseText.StartsWith("\"") && responseText.EndsWith("\"");
            Debug.Log($"响应是否包含双引号: {hasQuotes}");
            
            if (hasQuotes)
            {
                workshopInfo = responseText.Substring(1, responseText.Length - 2);
                Debug.Log($"去除双引号后的车间信息: '{workshopInfo}'");
            }
            else
            {
                Debug.Log($"响应不包含双引号，直接使用: '{workshopInfo}'");
            }
            
            // 检查车间信息是否有效
            if (string.IsNullOrEmpty(workshopInfo) || string.IsNullOrWhiteSpace(workshopInfo))
            {
                Debug.LogWarning("获取到的车间信息为空或只包含空白字符");
                workshopInfo = "默认车间";
                Debug.Log($"使用默认车间信息: {workshopInfo}");
            }
            
            // 解析车间信息
            ParseWorkshopInfo(workshopInfo);
            
            Debug.Log($"=== 车间信息获取成功 ===");
        }
        else
        {
            Debug.LogError($"=== 车间信息获取失败 ===");
            Debug.LogError($"错误类型: {webRequest.result}");
            Debug.LogError($"错误信息: {webRequest.error}");
            Debug.LogError($"HTTP响应码: {webRequest.responseCode}");
            
            // 尝试获取错误响应内容
            if (webRequest.downloadHandler != null)
            {
                string errorResponse = webRequest.downloadHandler.text;
                Debug.LogError($"错误响应内容: '{errorResponse}'");
            }
            
            // 如果获取失败，使用默认信息
            Debug.Log("使用默认车间信息");
            // 这里可以设置默认的车间信息到GlobalUserData或其他地方
        }
        
        // 清理资源
        webRequest.Dispose();
        Debug.Log($"=== GetWorkshopInfoAsync 方法执行完成 ===");
    }
    
    // 解析车间信息的辅助方法
    private void ParseWorkshopInfo(string workshopInfo)
    {
        Debug.Log($"=== 开始解析车间信息 ===");
        Debug.Log($"原始车间信息: '{workshopInfo}'");
        
        try
        {
            // 清空之前的车间信息
            GlobalUserData.WorkshopList.Clear();
            
            // 车间信息格式："1/1车间-2/2车间"
            // 用"-"分割不同车间
            string[] workshops = workshopInfo.Split('-');
            Debug.Log($"分割后的车间数量: {workshops.Length}");
            
            List<WorkshopData> workshopList = new List<WorkshopData>();
            
            foreach (string workshop in workshops)
            {
                if (string.IsNullOrEmpty(workshop.Trim()))
                    continue;
                    
                Debug.Log($"处理车间信息: '{workshop}'");
                
                // 用"/"分割车间号和车间名称
                string[] parts = workshop.Split('/');
                if (parts.Length == 2)
                {
                    string workshopNumber = parts[0].Trim();
                    string workshopName = parts[1].Trim();
                    
                    // 添加到本地列表（用于兼容性）
                    workshopList.Add(new WorkshopData(workshopNumber, workshopName));
                    
                    // 添加到全局数组
                    GlobalUserData.WorkshopList.Add(new WorkshopData(workshopNumber, workshopName));
                    
                    Debug.Log($"解析车间信息: 车间号={workshopNumber}, 车间名称={workshopName}");
                }
                else
                {
                    Debug.LogWarning($"车间信息格式不正确: '{workshop}'");
                }
            }
            
            // 存储车间信息到GlobalUserData（保持原有字符串格式的兼容性）
            if (workshopList.Count > 0)
            {
                // 将车间信息存储为字符串格式（保持兼容性）
                GlobalUserData.WorkshopInfo = workshopInfo; // 存储原始车间信息
                
                // 构建工作区域信息（可以根据需要调整格式）
                List<string> workAreaList = new List<string>();
                foreach (var ws in workshopList)
                {
                    workAreaList.Add($"{ws.WorkshopNumber}/{ws.WorkshopName}");
                }
                
                Debug.Log($"设置 GlobalUserData.WorkshopInfo: '{GlobalUserData.WorkshopInfo}'");
                Debug.Log($"设置 GlobalUserData.WorkshopList: 共{GlobalUserData.WorkshopList.Count}个车间");
            }
            
            Debug.Log($"=== 车间信息解析完成，共解析到 {workshopList.Count} 个车间 ===");
            
            // 打印所有车间信息
            for (int i = 0; i < GlobalUserData.WorkshopList.Count; i++)
            {
                Debug.Log($"车间{i + 1}: 号码={GlobalUserData.WorkshopList[i].WorkshopNumber}, 名称={GlobalUserData.WorkshopList[i].WorkshopName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析车间信息时出错: {ex.Message}");
        }
    }
    
    // 车间信息数据结构
    [System.Serializable]
    public class WorkshopInfo
    {
        public string WorkshopNumber;
        public string WorkshopName;
        
        public WorkshopInfo(string number, string name)
        {
            WorkshopNumber = number;
            WorkshopName = name;
        }
    }
    // 新增：异步获取企业名称的方法
    private IEnumerator GetCompanyNameAsync(string companyId)
    {
        Debug.Log($"=== 开始获取企业名称 ===");
        Debug.Log($"输入参数 - 企业ID: {companyId}");
        Debug.Log($"API URL: {apiUrl}");
        
        // 构建获取企业名称的参数：企业ID + "-62"
        string companyNameParam = $"{companyId}-62";
        Debug.Log($"构建的参数: {companyNameParam}");
        
        // 按照GetTemperature的方式创建JSON数据
        string jsonData = $"\"{companyNameParam}\"";
        Debug.Log($"JSON数据: {jsonData}");
        
        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonData);
        Debug.Log($"字节数组长度: {byteArray.Length}");
        
        // 创建UnityWebRequest
        UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        Debug.Log($"请求创建完成，准备发送...");
        
        // 发送请求
        yield return webRequest.SendWebRequest();
        
        Debug.Log($"请求完成，状态: {webRequest.result}");
        Debug.Log($"HTTP响应码: {webRequest.responseCode}");
        
        // 处理响应
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"原始响应内容: '{responseText}'");
            Debug.Log($"响应长度: {responseText.Length}");
            Debug.Log($"响应是否为空: {string.IsNullOrEmpty(responseText)}");
            
            // 处理响应 - 去除首尾的双引号
            string companyName = responseText;
            bool hasQuotes = responseText.StartsWith("\"") && responseText.EndsWith("\"");
            Debug.Log($"响应是否包含双引号: {hasQuotes}");
            
            if (hasQuotes)
            {
                companyName = responseText.Substring(1, responseText.Length - 2);
                Debug.Log($"去除双引号后的企业名称: '{companyName}'");
            }
            else
            {
                Debug.Log($"响应不包含双引号，直接使用: '{companyName}'");
            }
            
            // 检查企业名称是否有效
            if (string.IsNullOrEmpty(companyName) || string.IsNullOrWhiteSpace(companyName))
            {
                Debug.LogWarning("获取到的企业名称为空或只包含空白字符");
                companyName = $"企业{companyId}";
                Debug.Log($"使用默认企业名称: {companyName}");
            }
            
            // 设置企业名称到GlobalUserData
            Debug.Log($"设置前 GlobalUserData.AlSerialPantIdName: '{GlobalUserData.AlSerialPantIdName}'");
            GlobalUserData.AlSerialPantIdName = companyName;
            Debug.Log($"设置后 GlobalUserData.AlSerialPantIdName: '{GlobalUserData.AlSerialPantIdName}'");
            
            Debug.Log($"=== 企业名称获取成功 ===");
        }
        else
        {
            Debug.LogError($"=== 企业名称获取失败 ===");
            Debug.LogError($"错误类型: {webRequest.result}");
            Debug.LogError($"错误信息: {webRequest.error}");
            Debug.LogError($"HTTP响应码: {webRequest.responseCode}");
            
            // 尝试获取错误响应内容
            if (webRequest.downloadHandler != null)
            {
                string errorResponse = webRequest.downloadHandler.text;
                Debug.LogError($"错误响应内容: '{errorResponse}'");
            }
            
            
        }
        
        // 清理资源
        webRequest.Dispose();
        Debug.Log($"=== GetCompanyNameAsync 方法执行完成 ===");
    }
    
    // 删除原来的GetCompanyName方法，因为现在通过API获取
    // private string GetCompanyName(string companyId) { ... } // 删除这个方法
    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
        
        Debug.LogWarning(message);
    }
    
    // 用于测试的方法
    [ContextMenu("测试登录解析")]
    private void TestLoginParsing()
    {
        string testResponse = "1032/0-1033/1-1031/0";
        bool result = ParseLoginResponse(testResponse, "testuser");
        Debug.Log($"测试解析结果: {result}");
    }
}
