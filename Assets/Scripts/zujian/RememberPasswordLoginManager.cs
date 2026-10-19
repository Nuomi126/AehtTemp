using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class RememberPasswordLoginManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [Header("记住密码功能")]
    [SerializeField] private Toggle rememberPasswordToggle; // 记住密码复选框
    [SerializeField] private Button clearSavedDataButton; // 清除保存数据按钮（可选）
    
    [Header("API 设置")]
    [SerializeField] private string apiUrl = "http://183.214.222.76:9000/api/Home/GetRemoteDataFromAlwit";
    
    [Header("场景设置")]
    [SerializeField] private string nextSceneName = "MainScene";
    
    // PlayerPrefs 键名常量
    private const string SAVED_USERNAME_KEY = "SavedUsername";
    private const string SAVED_PASSWORD_KEY = "SavedPassword";
    private const string REMEMBER_PASSWORD_KEY = "RememberPassword";
    
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
        
        // 添加记住密码切换事件
        if (rememberPasswordToggle != null)
        {
            rememberPasswordToggle.onValueChanged.AddListener(OnRememberPasswordToggleChanged);
        }
        
        // 添加清除保存数据按钮事件（如果存在）
        if (clearSavedDataButton != null)
        {
            clearSavedDataButton.onClick.AddListener(ClearSavedLoginData);
        }
        
        // 清空错误信息
        if (errorText != null)
            errorText.text = "";
            
        // 加载保存的登录信息
        LoadSavedLoginData();
    }
    
    /// <summary>
    /// 加载保存的登录信息
    /// </summary>
    private void LoadSavedLoginData()
    {
        try
        {
            // 检查是否启用了记住密码功能
            bool rememberPassword = PlayerPrefs.GetInt(REMEMBER_PASSWORD_KEY, 0) == 1;
            
            if (rememberPasswordToggle != null)
            {
                rememberPasswordToggle.isOn = rememberPassword;
            }
            
            if (rememberPassword)
            {
                // 加载保存的用户名和密码
                string savedUsername = PlayerPrefs.GetString(SAVED_USERNAME_KEY, "");
                string savedPassword = PlayerPrefs.GetString(SAVED_PASSWORD_KEY, "");
                
                if (!string.IsNullOrEmpty(savedUsername))
                {
                    usernameInput.text = savedUsername;
                    Debug.Log("已加载保存的用户名");
                }
                
                if (!string.IsNullOrEmpty(savedPassword))
                {
                    passwordInput.text = savedPassword;
                    Debug.Log("已加载保存的密码");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载保存的登录信息时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 保存登录信息
    /// </summary>
    private void SaveLoginData(string username, string password)
    {
        try
        {
            if (rememberPasswordToggle != null && rememberPasswordToggle.isOn)
            {
                // 保存用户名和密码
                PlayerPrefs.SetString(SAVED_USERNAME_KEY, username);
                PlayerPrefs.SetString(SAVED_PASSWORD_KEY, password);
                PlayerPrefs.SetInt(REMEMBER_PASSWORD_KEY, 1);
                PlayerPrefs.Save();
                
                Debug.Log("登录信息已保存");
            }
            else
            {
                // 如果未勾选记住密码，清除保存的密码（但保留用户名）
                PlayerPrefs.SetString(SAVED_USERNAME_KEY, username);
                PlayerPrefs.DeleteKey(SAVED_PASSWORD_KEY);
                PlayerPrefs.SetInt(REMEMBER_PASSWORD_KEY, 0);
                PlayerPrefs.Save();
                
                Debug.Log("仅保存用户名，密码已清除");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存登录信息时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 清除所有保存的登录数据
    /// </summary>
    public void ClearSavedLoginData()
    {
        try
        {
            PlayerPrefs.DeleteKey(SAVED_USERNAME_KEY);
            PlayerPrefs.DeleteKey(SAVED_PASSWORD_KEY);
            PlayerPrefs.DeleteKey(REMEMBER_PASSWORD_KEY);
            PlayerPrefs.Save();
            
            // 清空输入框
            usernameInput.text = "";
            passwordInput.text = "";
            
            // 取消勾选记住密码
            if (rememberPasswordToggle != null)
            {
                rememberPasswordToggle.isOn = false;
            }
            
            Debug.Log("已清除所有保存的登录数据");
            
            // 可选：显示提示信息
            if (errorText != null)
            {
                errorText.text = "已清除保存的登录信息";
                StartCoroutine(ClearMessageAfterDelay(2f));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"清除保存的登录数据时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 记住密码切换事件
    /// </summary>
    private void OnRememberPasswordToggleChanged(bool isOn)
    {
        if (!isOn)
        {
            // 如果取消勾选，询问是否清除已保存的密码
            Debug.Log("记住密码功能已关闭");
        }
        else
        {
            Debug.Log("记住密码功能已开启");
        }
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
                // 登录成功，保存登录信息
                SaveLoginData(username, password);
                
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
    Debug.Log($"[RememberPasswordLoginManager] 准备跳转至保存的默认场景: {targetSceneName}");
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
            
            // 只验证登录成功，不修改全局用户数据
            // 让其他系统（如原始的ApiLoginManager）来处理用户数据的设置
            Debug.Log("登录验证成功，企业信息解析完成");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析登录响应时出错: {ex.Message}");
            return false;
        }
    }
    
    // 完全移除StoreUserData方法，或者让它只处理登录状态
    private void StoreUserData(string username, List<CompanyInfo> companies)
    {
        // 记住密码功能不应该修改全局用户数据
        // 只记录登录成功的状态即可
        Debug.Log($"用户 {username} 登录成功，企业数量: {companies.Count}");
    }
    
    private IEnumerator GetCompanyName(string companyId)
    {
        // 这里应该实现获取企业名称的逻辑
        // 暂时使用默认名称
        yield return new WaitForSeconds(0.1f);
        GlobalUserData.AlSerialPantIdName = $"企业_{companyId}";
        Debug.Log($"企业名称已设置: {GlobalUserData.AlSerialPantIdName}");
    }
    
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            Debug.LogWarning($"登录错误: {message}");
        }
    }
    
    /// <summary>
    /// 延迟清除消息
    /// </summary>
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.text = "";
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        if (loginButton != null)
            loginButton.onClick.RemoveListener(OnLoginButtonClick);
            
        if (rememberPasswordToggle != null)
            rememberPasswordToggle.onValueChanged.RemoveListener(OnRememberPasswordToggleChanged);
            
        if (clearSavedDataButton != null)
            clearSavedDataButton.onClick.RemoveListener(ClearSavedLoginData);
    }
}