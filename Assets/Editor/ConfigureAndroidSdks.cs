using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android SDK / NDK / JDK 路径配置工具。
/// 说明：
///  - 静态类仅提供配置逻辑 + 菜单入口（MenuItem 必须是静态方法）
///  - 构建前自动执行由独立的非静态类 AndroidBuildPreprocessor 实现
///    （静态类不能实现接口，IPreprocessBuildWithReport 必须由实例类实现）
/// </summary>
public static class ConfigureAndroidSdks
{
    public const string SdkPath = "D:/development/Android/sdk";
    public const string NdkPath = "D:/development/Android/sdk/ndk/27.0.12077973";
    public const string JdkPath = "D:/Java/java-17";

    // 日志文件位置（用户可以从文件管理器打开看，绕过 Unity Console 看不见的问题）
    public static readonly string LogFile = Path.Combine(
        Path.GetDirectoryName(Application.dataPath) ?? "", "Logs", "ConfigureAndroidSdks.log");

    /// <summary>
    /// 强制写入所有配置（EditorPrefs + 环境变量 + 构建系统设置）。
    /// 菜单和构建前回调都调用这里。
    /// </summary>
    public static void ForceApply()
    {
        try
        {
            var logDir = Path.GetDirectoryName(LogFile);
            if (!string.IsNullOrEmpty(logDir)) Directory.CreateDirectory(logDir);

            // 1) 校验路径存在
            bool ok = true;
            if (!Directory.Exists(SdkPath)) { Log("ERR SDK 不存在: " + SdkPath); ok = false; }
            if (!Directory.Exists(NdkPath)) { Log("ERR NDK 不存在: " + NdkPath); ok = false; }
            if (!Directory.Exists(JdkPath)) { Log("ERR JDK 不存在: " + JdkPath); ok = false; }
            if (!ok) return;

            // 2) 写入 EditorPrefs - 同时写多个 Unity/团结引擎可能使用的键名
            string[] sdkKeys = { "AndroidSdkRoot", "AndroidSdkRootPath", "AndroidSdkRootFolder" };
            string[] ndkKeys = { "AndroidNdkRoot", "AndroidNdkRootPath", "AndroidNdkRootFolder" };
            string[] jdkKeys = { "JdkPath", "JavaRootPath", "JdkPathFolder" };
            foreach (var k in sdkKeys) EditorPrefs.SetString(k, SdkPath);
            foreach (var k in ndkKeys) EditorPrefs.SetString(k, NdkPath);
            foreach (var k in jdkKeys) EditorPrefs.SetString(k, JdkPath);

            // 3) 持久化系统环境变量（用户级）
            SetUserEnv("ANDROID_HOME", SdkPath);
            SetUserEnv("ANDROID_SDK_ROOT", SdkPath);
            SetUserEnv("ANDROID_NDK_HOME", NdkPath);
            SetUserEnv("ANDROID_NDK_ROOT", NdkPath);
            SetUserEnv("JAVA_HOME", JdkPath);

            // 4) 当前进程内也设一份
            Environment.SetEnvironmentVariable("ANDROID_HOME", SdkPath);
            Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", SdkPath);
            Environment.SetEnvironmentVariable("ANDROID_NDK_HOME", NdkPath);
            Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", NdkPath);
            Environment.SetEnvironmentVariable("JAVA_HOME", JdkPath);

            // 5) 强制使用 Gradle 构建系统
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // 6) 反馈当前状态（同时输出到 Unity Console + 日志文件）
            string sdkNow = EditorPrefs.GetString("AndroidSdkRoot", "(empty)");
            string ndkNow = EditorPrefs.GetString("AndroidNdkRoot", "(empty)");
            string jdkNow = EditorPrefs.GetString("JdkPath", "(empty)");
            Debug.Log("[ConfigureAndroidSdks] 已强制写入 (SDK/NDK/JDK):\n" +
                      $"  SDK={sdkNow}\n  NDK={ndkNow}\n  JDK={jdkNow}");
            Log($"OK {DateTime.Now:HH:mm:ss} SDK={sdkNow} NDK={ndkNow} JDK={jdkNow}");
        }
        catch (Exception e)
        {
            Log("EX " + e);
        }
    }

    public static void Log(string s)
    {
        try { File.AppendAllText(LogFile, s + Environment.NewLine); } catch { }
    }

    private static void SetUserEnv(string name, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }
        catch (Exception e)
        {
            Log("EnvErr " + name + " " + e.Message);
        }
    }

    [MenuItem("工具/配置 Android 路径 (一键)")]
    public static void Configure()
    {
        ForceApply();
    }

    [MenuItem("工具/配置 Android 路径 (一键)/查看当前配置")]
    public static void ShowCurrent()
    {
        string sdk = EditorPrefs.GetString("AndroidSdkRoot", "(未设置)");
        string ndk = EditorPrefs.GetString("AndroidNdkRoot", "(未设置)");
        string jdk = EditorPrefs.GetString("JdkPath", "(未设置)");
        Debug.Log($"[ConfigureAndroidSdks] 当前:\n  SDK={sdk}\n  NDK={ndk}\n  JDK={jdk}");
        Log($"Show SDK={sdk} NDK={ndk} JDK={jdk}");
    }

    [MenuItem("工具/配置 Android 路径 (一键)/打开日志文件")]
    public static void OpenLog()
    {
        if (File.Exists(LogFile)) EditorUtility.RevealInFinder(LogFile);
        else Debug.LogWarning("日志文件还没生成: " + LogFile);
    }
}

/// <summary>
/// 构建前自动执行配置（非静态类才能实现 IPreprocessBuildWithReport）。
/// 同时通过 InitializeOnLoad 在编辑器启动时执行一次。
/// </summary>
[InitializeOnLoad]
public class AndroidBuildPreprocessor : IPreprocessBuildWithReport
{
    static AndroidBuildPreprocessor()
    {
        EditorApplication.delayCall += ConfigureAndroidSdks.ForceApply;
    }

    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        ConfigureAndroidSdks.ForceApply();
    }
}
