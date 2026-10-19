using UnityEngine;
using System.Collections.Generic;

public static class GlobalUserData
{
    // 用户基本信息
    public static int UserId { get; set; }
    
    // 用户全局属性
    public static string AlSerialPantId { get; set; }
    public static string AlSerialPantIdName { get; set; }
    public static int NetCommIndex { get; set; }
    public static string LanServerIp { get; set; }
    
    // 车间和工区信息
    public static string WorkshopInfo { get; set; }
    public static string WorkAreaInfo { get; set; }
    
    // 新增：车间信息数组
    public static List<WorkshopData> WorkshopList { get; set; } = new List<WorkshopData>();

        // 新增：工区信息列表
    public static List<WorkAreaData> WorkAreaList { get; set; } = new List<WorkAreaData>();
    
    
    // 清除所有用户数据（登出时使用）
    public static void ClearUserData()
    {
        UserId = 0;
        AlSerialPantId = string.Empty;
        AlSerialPantIdName = string.Empty;
        NetCommIndex = 0;
        LanServerIp = string.Empty;
        WorkshopInfo = string.Empty;
        WorkAreaInfo = string.Empty;
        WorkshopList.Clear();
        WorkAreaList.Clear(); // 添加这一行
    }
}

// 车间数据结构
[System.Serializable]
public class WorkshopData
{
    public string WorkshopNumber { get; set; }
    public string WorkshopName { get; set; }
    
    public WorkshopData(string number, string name)
    {
        WorkshopNumber = number;
        WorkshopName = name;
    }
    
    public override string ToString()
    {
        return $"{WorkshopNumber}/{WorkshopName}";
    }
}

// 工区信息数据结构
[System.Serializable]
public class WorkAreaData
{
    public string WorkshopNumber { get; set; }
    public string WorkAreaNumber { get; set; }
    public string WorkAreaName { get; set; }
    
    public WorkAreaData(string workshopNumber, string workAreaNumber, string workAreaName)
    {
        WorkshopNumber = workshopNumber;
        WorkAreaNumber = workAreaNumber;
        WorkAreaName = workAreaName;
    }
}

// 工区信息类（用于兼容性）
public class WorkAreaInfo
{
    public string WorkshopNumber { get; set; }
    public string WorkAreaNumber { get; set; }
    public string WorkAreaName { get; set; }
    
    public WorkAreaInfo(string workshopNumber, string workAreaNumber, string workAreaName)
    {
        WorkshopNumber = workshopNumber;
        WorkAreaNumber = workAreaNumber;
        WorkAreaName = workAreaName;
    }
}
