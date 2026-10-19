using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 一键导出 APK。
/// 使用：菜单栏 -> 工具 -> 导出APK -> 历史曲线测试版
/// 关键：
///  1) IPostGenerateGradleAndroidProject：Unity 生成完 Gradle 工程后、调用 Gradle 前自动注入
///     国内镜像仓库（阿里云），解决 dl.google.com 访问超时；
///  2) 自动修复 buildToolsVersion，使用 SDK 中实际已安装的版本。
///  3) 同时准备了 GRADLE_USER_HOME/init.d/mirror.gradle（全局 init 脚本）作为双保险。
/// </summary>
public static class BuildApk
{
    private const string TestScene = "Assets/Scenes/lishiquxiantu.scene";
    private const string OutputDir = "Build/Android";

    [MenuItem("工具/导出APK/历史曲线测试版(lishiquxiantu)")]
    public static void BuildTestApk()
    {
        BuildInternal(new[] { TestScene }, "lishiquxiantu_test.apk");
    }

    [MenuItem("工具/导出APK/正式版(全部场景)")]
    public static void BuildReleaseApk()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            scenes[i] = EditorBuildSettings.scenes[i].path;
        BuildInternal(scenes, "AehtTemp_release.apk");
    }

    private static void BuildInternal(string[] scenes, string apkName)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[BuildApk] 切换到 Android 平台失败！请确认已在团结引擎 Hub 安装 Android Build Support 模块。");
                return;
            }
        }

        PlayerSettings.Android.useCustomKeystore = false;

        if (scenes.Length == 0 || !File.Exists(scenes[0]))
        {
            Debug.LogError("[BuildApk] 没有可用场景，请检查 Build Settings。");
            return;
        }

        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);
        string outPath = Path.Combine(OutputDir, apkName);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(outPath);
            Debug.Log($"[BuildApk] 构建成功！APK 位置: {Path.GetFullPath(outPath)}");
        }
        else
        {
            Debug.LogError($"[BuildApk] 构建失败: {report.summary.result}");
        }
    }

    /// <summary>
    /// 供菜单手动调用：立即修复当前已生成的 Gradle 工程（调试用）。
    /// </summary>
    [MenuItem("工具/修复 Gradle 工程 (一键)")]
    public static void PatchGradleFiles()
    {
        // 定位已生成的工程目录
        string gradleRoot = "Library/Bee/Android/Prj/IL2CPP/Gradle";
        PatchGeneratedGradleProject(gradleRoot);
    }

    /// <summary>
    /// 修复 Unity 自动生成的 Gradle 工程：
    /// 1) settings.gradle 的 pluginManagement 与 dependencyResolutionManagement 注入阿里云镜像
    /// 2) root build.gradle 注入 allprojects 仓库
    /// 3) launcher / tuanjieLibrary 的 buildToolsVersion 修正为 SDK 实际安装的版本
    /// </summary>
    public static void PatchGeneratedGradleProject(string gradleRoot)
    {
        // Unity 回调传入的 path 可能是 tuanjieLibrary 子目录，向上查找真正的工程根目录（含 settings.gradle）
        string dir = gradleRoot;
        for (int i = 0; i < 3 && !string.IsNullOrEmpty(dir); i++)
        {
            if (File.Exists(Path.Combine(dir, "settings.gradle")))
                break;
            var parent = Directory.GetParent(dir);
            dir = parent != null ? parent.FullName : null;
        }
        if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "settings.gradle")))
            gradleRoot = dir;
        else
        {
            Debug.LogWarning($"[BuildApk] 未找到含 settings.gradle 的工程根目录（入参: {gradleRoot}），尝试标准路径。");
            string std = "Library/Bee/Android/Prj/IL2CPP/Gradle";
            if (File.Exists(Path.Combine(std, "settings.gradle")))
                gradleRoot = std;
        }

        // 0) 强制 local.properties 的 sdk.dir 指向 D 盘 SDK
        //    （Unity 默认生成的是它自带的 SDK，缺 build-tools 34.0.0，会导致 Gradle 联网下载被墙）
        string sdkRoot = ConfigureAndroidSdks.SdkPath;
        string localProps = Path.Combine(gradleRoot, "local.properties");
        if (File.Exists(localProps))
        {
            string sdkDirEscaped = sdkRoot.Replace("\\", "\\\\").Replace(":", "\\:");
            File.WriteAllText(localProps, "sdk.dir=" + sdkDirEscaped + "\n");
            Debug.Log($"[BuildApk] 已改写 local.properties -> sdk.dir={sdkRoot}");
        }

        // 1) SDK build-tools 实际版本
        string btDir = Path.Combine(sdkRoot, "build-tools");
        string actualBt = null;
        if (Directory.Exists(btDir))
        {
            foreach (var preferred in new[] { "34.0.0", "33.0.1", "35.0.0", "30.0.3" })
            {
                if (Directory.Exists(Path.Combine(btDir, preferred))) { actualBt = preferred; break; }
            }
            if (actualBt == null)
            {
                var dirs = Directory.GetDirectories(btDir);
                if (dirs.Length > 0) actualBt = new DirectoryInfo(dirs[0]).Name;
            }
        }
        if (string.IsNullOrEmpty(actualBt))
        {
            Debug.LogError("[BuildApk] SDK build-tools 目录为空，请确认 SDK 安装。");
            return;
        }
        Debug.Log($"[BuildApk] 将使用 build-tools 版本: {actualBt}");

        // 2) settings.gradle —— 注入镜像（pluginManagement + dependencyResolutionManagement）
        string settingsPath = Path.Combine(gradleRoot, "settings.gradle");
        if (File.Exists(settingsPath))
        {
            string s = File.ReadAllText(settingsPath);
            string patched = s;

            const string aliyunGoogle = "maven { url 'https://maven.aliyun.com/repository/google' }";
            const string aliyunPublic = "maven { url 'https://maven.aliyun.com/repository/public' }";
            const string aliyunPlugin = "maven { url 'https://maven.aliyun.com/repository/gradle-plugin' }";
            string[] mirrorLines = { aliyunGoogle, aliyunPublic, aliyunPlugin };

            // 2a) pluginManagement 块：定位到它的 repositories { 后插入镜像（若块内无镜像）
            int pmIdx = patched.IndexOf("pluginManagement", StringComparison.Ordinal);
            if (pmIdx >= 0)
            {
                int pmRepos = patched.IndexOf("repositories {", pmIdx, StringComparison.Ordinal);
                int pmEnd = patched.IndexOf("}", pmRepos + "repositories {".Length, StringComparison.Ordinal);
                if (pmRepos >= 0 && pmEnd > pmRepos)
                {
                    string block = patched.Substring(pmRepos, pmEnd - pmRepos);
                    if (!block.Contains("maven.aliyun.com"))
                    {
                        string insert = "repositories {\n" + string.Join("\n", mirrorLines) + "\n        ";
                        patched = patched.Substring(0, pmRepos) + insert +
                                  patched.Substring(pmRepos + "repositories {".Length);
                    }
                }
            }

            // 2b) dependencyResolutionManagement 块：定位它的 repositories { 后插入镜像（若块内无镜像）
            //     这个块决定依赖从哪里下载（PREFER_SETTINGS 模式下只有这里生效），是关键！
            //     用行级处理，避免 ${project(...)} 字符串里的花括号干扰深度匹配。
            {
                string[] lines = patched.Split('\n');
                bool inDrm = false;
                int insertLine = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    string t = lines[i].Trim();
                    if (t.StartsWith("dependencyResolutionManagement")) { inDrm = true; continue; }
                    if (inDrm && t.StartsWith("repositories {")) { insertLine = i; break; }
                    if (inDrm && (t.StartsWith("include ") || t.StartsWith("}") || t.Length == 0))
                    {
                        // dependencyResolutionManagement 结束后（兜底：不继续找错块）
                        inDrm = false;
                    }
                }
                if (insertLine >= 0)
                {
                    string blockAfter = string.Join("\n", lines, insertLine + 1, lines.Length - insertLine - 1);
                    if (!blockAfter.Contains("maven.aliyun.com"))
                    {
                        var newLines = new System.Collections.Generic.List<string>();
                        for (int i = 0; i <= insertLine; i++) newLines.Add(lines[i]);
                        foreach (var m in mirrorLines) newLines.Add("        " + m);
                        for (int i = insertLine + 1; i < lines.Length; i++) newLines.Add(lines[i]);
                        patched = string.Join("\n", newLines);
                    }
                }
            }

            if (patched != s)
            {
                File.WriteAllText(settingsPath, patched);
                Debug.Log($"[BuildApk] 已注入镜像: {settingsPath}");
            }
            else
            {
                Debug.Log($"[BuildApk] settings.gradle 已是镜像版（或无法定位块）: {settingsPath}");
            }
        }

        // 3) root build.gradle —— 注入 allprojects 仓库（双保险）
        string rootGradlePath = Path.Combine(gradleRoot, "build.gradle");
        if (File.Exists(rootGradlePath))
        {
            string g = File.ReadAllText(rootGradlePath);
            if (!g.Contains("maven.aliyun.com"))
            {
                string appended = "\n\n// [BuildApk 自动注入] 国内镜像\nallprojects {\n    repositories {\n        maven { url 'https://maven.aliyun.com/repository/google' }\n        maven { url 'https://maven.aliyun.com/repository/public' }\n        maven { url 'https://maven.aliyun.com/repository/gradle-plugin' }\n    }\n}\n";
                File.AppendAllText(rootGradlePath, appended);
                Debug.Log($"[BuildApk] 已注入 allprojects 镜像: {rootGradlePath}");
            }
        }

        // 4) launcher / tuanjieLibrary build.gradle —— 修 buildToolsVersion
        foreach (var p in new[]
        {
            Path.Combine(gradleRoot, "launcher", "build.gradle"),
            Path.Combine(gradleRoot, "tuanjieLibrary", "build.gradle"),
        })
        {
            if (!File.Exists(p)) continue;
            string g = File.ReadAllText(p);
            string g2 = Regex.Replace(g, @"buildToolsVersion\s+['""][0-9]+\.[0-9]+\.[0-9]+['""]", $"buildToolsVersion '{actualBt}'");
            if (g2 != g)
            {
                File.WriteAllText(p, g2);
                Debug.Log($"[BuildApk] 已修复 buildToolsVersion: {p}");
            }
        }

        Debug.Log("[BuildApk] Gradle 工程修复完毕。");
    }
}

/// <summary>
/// Unity 生成 Gradle 工程后、调用 Gradle 之前自动执行：
/// 注入国内镜像 + 修复 buildToolsVersion。这是构建流程的标准扩展点，
/// 比"构建前手动 patch"更可靠（Unity 每次都会重新生成工程文件）。
/// </summary>
public class GradleMirrorInjector : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => -100; // 越早执行越好

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log($"[GradleMirrorInjector] 生成完成，开始注入镜像: {path}");
        BuildApk.PatchGeneratedGradleProject(path);
    }
}
