using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 외부 플래그를 감지해 일반 Play 모드에서 빙의 검증 러너를 시작합니다.
/// Test Runner처럼 작업 씬을 닫지 않으므로 저장하지 않은 씬 변경도 그대로 보존됩니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_PossessionDirectPlayModeBridge
{
    private const string RequestFlagPath =
        @"C:\Docs\Generated\HWJ_RunPossessionDirectPlayMode.flag";
    private const string RuntimeFlagPath =
        @"C:\Docs\Generated\HWJ_PossessionDirectPlayModeRuntime.flag";
    private const string SummaryPath =
        @"C:\Docs\Generated\HWJ_PossessionDirectPlayModeSummary.md";

    private static double nextPollTime;

    static HWJ_PossessionDirectPlayModeBridge()
    {
        EditorApplication.update -= PollRequest;
        EditorApplication.update += PollRequest;
    }

    [MenuItem("Tools/HWJ/Tests/Run Possession Validation Without Saving Scene")]
    public static void CreateRequest()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RequestFlagPath));
        File.WriteAllText(RequestFlagPath, DateTime.Now.ToString("O"), Encoding.UTF8);
    }

    private static void PollRequest()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
        {
            return;
        }

        nextPollTime = EditorApplication.timeSinceStartup + 1d;

        if (!File.Exists(RequestFlagPath)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(RuntimeFlagPath));
        File.Delete(RequestFlagPath);
        File.WriteAllText(RuntimeFlagPath, DateTime.Now.ToString("O"), Encoding.UTF8);
        File.WriteAllText(
            SummaryPath,
            $"# HWJ Possession Direct PlayMode Validation\n\n- Status: Started\n- Started: {DateTime.Now:O}\n",
            Encoding.UTF8);

        Debug.Log("[HWJ] Starting possession validation in regular Play mode without saving the scene.");
        EditorApplication.EnterPlaymode();
    }
}
