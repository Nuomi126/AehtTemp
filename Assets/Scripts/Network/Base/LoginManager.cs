using System;
using System.Collections;
using System.Collections.Generic; // 添加这一行以使用 Dictionary
using System.Data;
using System.Data.SqlClient;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // 添加 TextMeshPro 命名空间

public class LoginManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TMP_InputField usernameInput; // 修改为 TMP_InputField
    [SerializeField] private TMP_InputField passwordInput; // 修改为 TMP_InputField
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI errorText; // 修改为 TextMeshProUGUI
    
    [Header("数据库设置")]
    [SerializeField] private string serverName = "localhost";
    [SerializeField] private string databaseName = "WebSysConfig";
    [SerializeField] private string userId = "sa";
    [SerializeField] private string password = "password";
    
    [Header("场景设置")]
    [SerializeField] private string nextSceneName = "MainScene";
    
    private string connectionString;
    
    private void Start()
    {
        // 初始化连接字符串
        connectionString = $"Server={serverName};Database={databaseName};User Id={userId};Password={password};";
        
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
        
        StartCoroutine(ValidateUser(username, userPassword));
    }
    
    private IEnumerator ValidateUser(string username, string userPassword)
    {
        // 显示加载状态
        loginButton.interactable = false;
        if (errorText != null)
            errorText.text = "正在验证...";
        
        yield return new WaitForSeconds(0.1f); // 给UI更新的时间
        
        bool loginSuccess = false;
        int userId = 0;
        
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // 验证用户名和密码
                string query = "SELECT userid FROM zzh.useraccount WHERE username = @Username AND password = @Password";
                
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", userPassword);
                    
                    object result = command.ExecuteScalar();
                    
                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                        // 在这里加载用户数据，但不使用 yield return
                        LoadUserData(userId, connection);
                        loginSuccess = true;
                    }
                    else
                    {
                        ShowError("用户名或密码错误");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"数据库连接错误: {ex.Message}");
            ShowError("连接数据库失败，请稍后再试");
        }
        
        // 恢复UI状态
        loginButton.interactable = true;
        
        // 在 try-catch 块外部处理登录成功的情况
        if (loginSuccess)
        {
            // 登录成功，加载下一个场景
            SceneManager.LoadScene(nextSceneName);
        }
    }
    
    // 修改为非协程方法
    private void LoadUserData(int userId, SqlConnection connection)
    {
        try
        {
            // 首先查询表的结构，获取实际的列名
            string schemaQuery = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'zzh' AND TABLE_NAME = 'userglobalproperty'";
            using (SqlCommand schemaCommand = new SqlCommand(schemaQuery, connection))
            {
                using (SqlDataReader schemaReader = schemaCommand.ExecuteReader())
                {
                    Debug.Log("表 zzh.userglobalproperty 的列名:");
                    while (schemaReader.Read())
                    {
                        Debug.Log(schemaReader["COLUMN_NAME"].ToString());
                    }
                }
            }

            // 使用 SELECT * 查询所有列，并打印所有列名和值
            string query = "SELECT * FROM zzh.userglobalproperty WHERE userid = @UserId";
            
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // 存储用户数据到全局变量
                        GlobalUserData.UserId = userId;
                        
                        // 打印所有列名和值，帮助调试
                        Debug.Log("查询结果中的所有列和值:");
                        
                        // 创建一个字典来存储所有列名和值
                        Dictionary<string, string> columnValues = new Dictionary<string, string>();
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            string value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                            Debug.Log($"列名: {columnName}, 值: {value}");
                            
                            // 存储到字典中，转为小写以便不区分大小写比较
                            columnValues[columnName.ToLower()] = value;
                        }
                        
                        // 添加调试信息，检查字典中的所有键
                        Debug.Log("字典中的所有键:");
                        foreach (var key in columnValues.Keys)
                        {
                            Debug.Log($"键: {key}");
                        }
                        
                        // 尝试不同的列名格式来获取 NetCommIndex
                        string[] possibleNetCommIndexColumns = new string[] { 
                            "netcommindex", "net_comm_index", "netcomm_index", "net_commindex"
                        };
                        
                        // 尝试所有可能的 NetCommIndex 列名
                        foreach (string colName in possibleNetCommIndexColumns)
                        {
                            if (columnValues.ContainsKey(colName))
                            {
                                string value = columnValues[colName];
                                if (int.TryParse(value, out int netCommIndex))
                                {
                                    GlobalUserData.NetCommIndex = netCommIndex;
                                    Debug.Log($"从列 '{colName}' 设置 NetCommIndex = {GlobalUserData.NetCommIndex}");
                                }
                                else
                                {
                                    GlobalUserData.NetCommIndex = value == "NULL" ? 0 : 0;
                                    Debug.Log($"从列 '{colName}' 设置 NetCommIndex = {GlobalUserData.NetCommIndex}（无法解析为整数）");
                                }
                                break;
                            }
                        }
                        
                        // 尝试不同的列名格式来获取 LanServerIp
                        string[] possibleLanServerIpColumns = new string[] { 
                            "lanserverip", "lan_server_ip", "lanserver_ip", "lan_serverip", 
                            "serverip", "server_ip", "ipaddress", "ip_address", "ip"
                        };
                        
                        // 尝试所有可能的 LanServerIp 列名
                        foreach (string colName in possibleLanServerIpColumns)
                        {
                            if (columnValues.ContainsKey(colName))
                            {
                                string value = columnValues[colName];
                                GlobalUserData.LanServerIp = value == "NULL" ? "" : value;
                                Debug.Log($"从列 '{colName}' 设置 LanServerIp = {GlobalUserData.LanServerIp}");
                                break;
                            }
                        }
                        
                        // 如果上面的方法都失败，尝试模糊匹配 LanServerIp
                        if (string.IsNullOrEmpty(GlobalUserData.LanServerIp))
                        {
                            Debug.Log("尝试使用模糊匹配查找 LanServerIp 列");
                            foreach (var pair in columnValues)
                            {
                                if ((pair.Key.Contains("lan") && pair.Key.Contains("server") && pair.Key.Contains("ip")) ||
                                    (pair.Key.Contains("server") && pair.Key.Contains("ip")) ||
                                    (pair.Key.Contains("ip") && pair.Key.Contains("address")))
                                {
                                    GlobalUserData.LanServerIp = pair.Value == "NULL" ? "" : pair.Value;
                                    Debug.Log($"通过模糊匹配从列 '{pair.Key}' 设置 LanServerIp = {GlobalUserData.LanServerIp}");
                                    break;
                                }
                            }
                        }
                        
                        // 如果上面的方法都失败，尝试模糊匹配 NetCommIndex
                        if (GlobalUserData.NetCommIndex == 0)
                        {
                            Debug.Log("尝试使用模糊匹配查找 NetCommIndex 列");
                            foreach (var pair in columnValues)
                            {
                                if (pair.Key.Contains("net") && pair.Key.Contains("comm") && pair.Key.Contains("index"))
                                {
                                    if (int.TryParse(pair.Value, out int netCommIndex))
                                    {
                                        GlobalUserData.NetCommIndex = netCommIndex;
                                        Debug.Log($"通过模糊匹配从列 '{pair.Key}' 设置 NetCommIndex = {GlobalUserData.NetCommIndex}");
                                    }
                                    else
                                    {
                                        GlobalUserData.NetCommIndex = pair.Value == "NULL" ? 0 : 0;
                                        Debug.Log($"通过模糊匹配从列 '{pair.Key}' 设置 NetCommIndex = {GlobalUserData.NetCommIndex}（无法解析为整数）");
                                    }
                                    break;
                                }
                            }
                        }
                        
                        // 尝试不同的列名格式
                        string[] possibleAlSerialPantIdColumns = new string[] { 
                            "alserialpantid", "al_serial_pant_id", "alserialplantid", "al_serial_plant_id", 
                            "serialpantid", "serial_pant_id", "serialplantid", "serial_plant_id" 
                        };
                        
                        string[] possibleAlSerialPantNameColumns = new string[] { 
                            "alserialpantname", "al_serial_pant_name", "alserialplantname", "al_serial_plant_name", 
                            "serialpantname", "serial_pant_name", "serialplantname", "serial_plant_name" 
                        };
                        
                        // 尝试所有可能的列名
                        foreach (string colName in possibleAlSerialPantIdColumns)
                        {
                            if (columnValues.ContainsKey(colName))
                            {
                                GlobalUserData.AlSerialPantId = columnValues[colName];
                                Debug.Log($"从列 '{colName}' 设置 AlSerialPantId = {GlobalUserData.AlSerialPantId}");
                                break;
                            }
                        }
                        
                        foreach (string colName in possibleAlSerialPantNameColumns)
                        {
                            if (columnValues.ContainsKey(colName))
                            {
                                GlobalUserData.AlSerialPantIdName = columnValues[colName];
                                Debug.Log($"从列 '{colName}' 设置 AlSerialPantIdName = {GlobalUserData.AlSerialPantIdName}");
                                break;
                            }
                        }
                        
                        // 如果上面的方法都失败，尝试模糊匹配
                        if (string.IsNullOrEmpty(GlobalUserData.AlSerialPantId))
                        {
                            Debug.Log("尝试使用模糊匹配查找 AlSerialPantId 列");
                            foreach (var pair in columnValues)
                            {
                                // 检查键是否包含相关关键词
                                if ((pair.Key.Contains("serial") && pair.Key.Contains("id") && !pair.Key.Contains("name")) ||
                                    (pair.Key.Contains("plant") && pair.Key.Contains("id") && !pair.Key.Contains("name")) ||
                                    (pair.Key.Contains("pant") && pair.Key.Contains("id") && !pair.Key.Contains("name")))
                                {
                                    GlobalUserData.AlSerialPantId = pair.Value;
                                    Debug.Log($"通过模糊匹配从列 '{pair.Key}' 设置 AlSerialPantId = {pair.Value}");
                                    break;
                                }
                            }
                        }
                        
                        if (string.IsNullOrEmpty(GlobalUserData.AlSerialPantIdName))
                        {
                            Debug.Log("尝试使用模糊匹配查找 AlSerialPantIdName 列");
                            foreach (var pair in columnValues)
                            {
                                // 检查键是否包含相关关键词
                                if ((pair.Key.Contains("serial") && pair.Key.Contains("name")) ||
                                    (pair.Key.Contains("plant") && pair.Key.Contains("name")) ||
                                    (pair.Key.Contains("pant") && pair.Key.Contains("name")))
                                {
                                    GlobalUserData.AlSerialPantIdName = pair.Value;
                                    Debug.Log($"通过模糊匹配从列 '{pair.Key}' 设置 AlSerialPantIdName = {pair.Value}");
                                    break;
                                }
                            }
                        }
                        
                        Debug.Log("用户数据加载成功");
                        
                        // 打印最终的全局用户数据
                        Debug.Log("最终的全局用户数据:");
                        Debug.Log($"UserId: {GlobalUserData.UserId}");
                        Debug.Log($"AlSerialPantId: {GlobalUserData.AlSerialPantId}");
                        Debug.Log($"AlSerialPantIdName: {GlobalUserData.AlSerialPantIdName}");
                        Debug.Log($"NetCommIndex: {GlobalUserData.NetCommIndex}");
                        Debug.Log($"LanServerIp: {GlobalUserData.LanServerIp}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到用户ID {userId} 的全局属性数据");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载用户数据错误: {ex.Message}");
            Debug.LogError($"异常堆栈: {ex.StackTrace}");
        }
    }
    
    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
        
        Debug.LogWarning(message);
    }
}