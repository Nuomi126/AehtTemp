using UnityEngine;
using TMPro;

public class UserDataDisplay : MonoBehaviour
{
    [Header("用户信息显示组件 - 可选分配")]
    [SerializeField] private TextMeshProUGUI userAccountText;  // 改名为更清楚的账号显示
    [SerializeField] private TextMeshProUGUI serialPantIdText;
    [SerializeField] private TextMeshProUGUI serialPantNameText;
    [SerializeField] private TextMeshProUGUI netCommIndexText;
    [SerializeField] private TextMeshProUGUI lanServerIpText;
    [SerializeField] private TextMeshProUGUI workshopInfoText;
    [SerializeField] private TextMeshProUGUI workAreaInfoText;
    
    [Header("通用显示组件 - 兼容旧版本")]
    [SerializeField] private TextMeshProUGUI displayText;
    
    private void Start()
    {
        // 打印全局用户数据以进行调试
        PrintGlobalUserData();
        
        // 显示用户数据
        DisplayUserData();
    }
    
    // 添加调试方法，打印所有全局用户数据
    private void PrintGlobalUserData()
    {
        Debug.Log("========== 全局用户数据调试信息 ==========");
        Debug.Log($"登录账号: {GlobalUserData.UserId}");  // 更改调试信息描述
        Debug.Log($"AlSerialPantId: '{GlobalUserData.AlSerialPantId}'");
        Debug.Log($"AlSerialPantIdName: '{GlobalUserData.AlSerialPantIdName}'");
        Debug.Log($"NetCommIndex: {GlobalUserData.NetCommIndex}");
        Debug.Log($"LanServerIp: '{GlobalUserData.LanServerIp}'");
        
        // 新增：打印车间信息调试
        Debug.Log("---------- 车间信息调试 ----------");
        Debug.Log($"WorkshopInfo (字符串): '{GlobalUserData.WorkshopInfo}'");
        Debug.Log($"WorkshopList 数量: {GlobalUserData.WorkshopList?.Count ?? 0}");
        
        if (GlobalUserData.WorkshopList != null && GlobalUserData.WorkshopList.Count > 0)
        {
            for (int i = 0; i < GlobalUserData.WorkshopList.Count; i++)
            {
                var workshop = GlobalUserData.WorkshopList[i];
                Debug.Log($"车间{i + 1}: 号码='{workshop.WorkshopNumber}', 名称='{workshop.WorkshopName}'");
            }
        }
        else
        {
            Debug.LogWarning("车间信息列表为空或未初始化");
        }
        
        // 新增：打印工区信息调试
        Debug.Log("---------- 工区信息调试 ----------");
        Debug.Log($"WorkAreaInfo (字符串): '{GlobalUserData.WorkAreaInfo}'");
        Debug.Log($"WorkAreaList 数量: {GlobalUserData.WorkAreaList?.Count ?? 0}");
        
        if (GlobalUserData.WorkAreaList != null && GlobalUserData.WorkAreaList.Count > 0)
        {
            for (int i = 0; i < GlobalUserData.WorkAreaList.Count; i++)
            {
                var workArea = GlobalUserData.WorkAreaList[i];
                Debug.Log($"工区{i + 1}: 车间号='{workArea.WorkshopNumber}', 工区号='{workArea.WorkAreaNumber}', 工区名称='{workArea.WorkAreaName}'");
            }
        }
        else
        {
            Debug.LogWarning("工区信息列表为空或未初始化");
        }
        
        Debug.Log("========================================");
    }
    
    public void DisplayUserData()
    {
        // 检查用户是否已登录
        if (GlobalUserData.UserId <= 0)
        {
            SetTextSafely(displayText, "未登录");
            SetTextSafely(userAccountText, "未登录");
            Debug.LogWarning("用户尚未登录，无法显示用户数据");
            return;
        }
        
        // 分别设置各个Text组件的内容（如果已分配）
        SetTextSafely(userAccountText, $"{GlobalUserData.UserId}");  // 显示为"账号"
        SetTextSafely(serialPantIdText, $"序列号: {GlobalUserData.AlSerialPantId}");
        SetTextSafely(serialPantNameText, GlobalUserData.AlSerialPantIdName);
        SetTextSafely(netCommIndexText, $"网络索引: {GlobalUserData.NetCommIndex}");
        SetTextSafely(lanServerIpText, $"服务器IP: {GlobalUserData.LanServerIp}");
        
        // 车间信息显示
        if (GlobalUserData.WorkshopList != null && GlobalUserData.WorkshopList.Count > 0)
        {
            string workshopDisplay = "车间: ";
            for (int i = 0; i < GlobalUserData.WorkshopList.Count; i++)
            {
                var workshop = GlobalUserData.WorkshopList[i];
                workshopDisplay += $"{workshop.WorkshopName}";
                if (i < GlobalUserData.WorkshopList.Count - 1)
                    workshopDisplay += ", ";
            }
            SetTextSafely(workshopInfoText, workshopDisplay);
        }
        else
        {
            SetTextSafely(workshopInfoText, "车间: 未设置");
        }
        
        // 工区信息显示
        if (GlobalUserData.WorkAreaList != null && GlobalUserData.WorkAreaList.Count > 0)
        {
            string workAreaDisplay = "工区: ";
            for (int i = 0; i < GlobalUserData.WorkAreaList.Count; i++)
            {
                var workArea = GlobalUserData.WorkAreaList[i];
                workAreaDisplay += $"{workArea.WorkAreaName}";
                if (i < GlobalUserData.WorkAreaList.Count - 1)
                    workAreaDisplay += ", ";
            }
            SetTextSafely(workAreaInfoText, workAreaDisplay);
        }
        else
        {
            SetTextSafely(workAreaInfoText, "工区: 未设置");
        }
        
        // 兼容旧版本：如果设置了displayText，显示主要信息
        if (displayText != null)
        {
            displayText.text = GlobalUserData.AlSerialPantIdName;
        }
        
        Debug.Log($"已显示用户数据: {GlobalUserData.AlSerialPantIdName}");
    }
    
    // 安全设置Text内容的辅助方法
    private void SetTextSafely(TextMeshProUGUI textComponent, string content)
    {
        if (textComponent != null)
        {
            textComponent.text = content;
        }
    }
    
    // 可以添加一个公共方法用于刷新显示
    public void RefreshDisplay()
    {
        PrintGlobalUserData(); // 刷新时也打印调试信息
        DisplayUserData();
    }
    
    // 新增：单独更新账号显示的方法
    public void UpdateUserAccountDisplay()
    {
        SetTextSafely(userAccountText, $"账号: {GlobalUserData.UserId}");
    }
    
    public void UpdateWorkshopDisplay()
    {
        if (GlobalUserData.WorkshopList != null && GlobalUserData.WorkshopList.Count > 0)
        {
            string workshopDisplay = "车间: ";
            for (int i = 0; i < GlobalUserData.WorkshopList.Count; i++)
            {
                var workshop = GlobalUserData.WorkshopList[i];
                workshopDisplay += $"{workshop.WorkshopName}";
                if (i < GlobalUserData.WorkshopList.Count - 1)
                    workshopDisplay += ", ";
            }
            SetTextSafely(workshopInfoText, workshopDisplay);
        }
        else
        {
            SetTextSafely(workshopInfoText, "车间: 未设置");
        }
    }
    
    public void UpdateWorkAreaDisplay()
    {
        if (GlobalUserData.WorkAreaList != null && GlobalUserData.WorkAreaList.Count > 0)
        {
            string workAreaDisplay = "工区: ";
            for (int i = 0; i < GlobalUserData.WorkAreaList.Count; i++)
            {
                var workArea = GlobalUserData.WorkAreaList[i];
                workAreaDisplay += $"{workArea.WorkAreaName}";
                if (i < GlobalUserData.WorkAreaList.Count - 1)
                    workAreaDisplay += ", ";
            }
            SetTextSafely(workAreaInfoText, workAreaDisplay);
        }
        else
        {
            SetTextSafely(workAreaInfoText, "工区: 未设置");
        }
    }
}