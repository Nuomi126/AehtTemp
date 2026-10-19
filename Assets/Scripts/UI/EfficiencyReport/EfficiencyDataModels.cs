using System;
using System.Collections.Generic;

[Serializable]
public class AlwitRequestConfig_Frame74
{
    public string IP;
    public int Port;
    public string Method;
    public int FrameType; // 74 for Effect Report
    public int SubFrameType;
}

[Serializable]
public class AlwitResponse_Frame74
{
    public List<Frame74Data> data;
}

[Serializable]
public class Frame74Data
{
    public string slotId; // 槽号
    public string occurrenceTime; // 发生时刻 (单槽特有)
    public int occurrenceCount; // 发生次数
    public float effectDuration; // 效应持续时间
    public float peakVoltage; // 效应峰压
    public float averageVoltage; // 效应均压
    public float powerConsumption;
    public string workshopName;
    public string areaName; // 效应功耗
}

