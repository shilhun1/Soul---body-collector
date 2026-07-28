using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 중간발표용 Windows 빌드를 생성하는 HWJ 전용 Editor 도구입니다.
/// ProjectSettings의 Build Settings를 직접 바꾸지 않고, 빌드할 HWJ 씬 목록을 BuildPlayerOptions에 명시합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_MidtermPresentationBuildBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunMidtermPresentationBuild.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_MidtermPresentationBuildReport.md";
    private const string BuildDirectory = "Builds/HWJ_MidtermDemo";
    private const string BuildExecutableName = "SoulBodyCollector_HWJ_MidtermDemo.exe";

    private static readonly string[] CandidateScenePaths =
    {
        "Assets/01Scenes/HWJ_Stage1_01_RuinedVillage.unity",
        "Assets/01Scenes/HWJ_Stage1_02_RuinedOutpost.unity",
        "Assets/01Scenes/HWJ_Stage1_03_BackRoad.unity",
        "Assets/01Scenes/HWJ_Stage1_04_MidBossBarracks.unity"
    };

    private static double nextFlagPollTime;

    static HWJ_MidtermPresentationBuildBridge()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    [MenuItem("Tools/Stage1/Build Midterm Windows Demo")]
    public static void BuildFromMenu()
    {
        BuildMidtermDemo("Unity Editor menu");
    }

    [MenuItem("Tools/Stage1/Create Midterm Windows Build Flag")]
    public static void CreateBuildFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"[HWJ Midterm Build] Flag created: {FlagFilePath}");
    }

    private static void Update()
    {
        if (EditorApplication.timeSinceStartup < nextFlagPollTime)
        {
            return;
        }

        nextFlagPollTime = EditorApplication.timeSinceStartup + 2d;

        if (!File.Exists(FlagFilePath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        File.Delete(FlagFilePath);
        BuildMidtermDemo("flag file");
    }

    private static void BuildMidtermDemo(string source)
    {
        List<string> scenes = CollectExistingScenes();
        string fullBuildDirectory = Path.GetFullPath(BuildDirectory);
        string executablePath = Path.Combine(fullBuildDirectory, BuildExecutableName);
        HWJ_MidtermBuildReport report = new HWJ_MidtermBuildReport(source, executablePath);

        if (scenes.Count == 0)
        {
            report.AddError("BUILD_SCENE_MISSING", "빌드할 HWJ Stage1 씬을 찾지 못했습니다.");
            WriteReport(report);
            Debug.LogError($"[HWJ Midterm Build] Failed: no scenes. Report={ReportFilePath}");
            return;
        }

        try
        {
            Directory.CreateDirectory(fullBuildDirectory);
            report.ScenePaths.AddRange(scenes);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            report.BuildStartedAt = DateTime.Now;
            UnityEditor.Build.Reporting.BuildReport unityReport = BuildPipeline.BuildPlayer(options);
            report.BuildFinishedAt = DateTime.Now;
            report.UnityResult = unityReport.summary.result.ToString();
            report.TotalSizeBytes = unityReport.summary.totalSize;
            report.TotalTime = unityReport.summary.totalTime;

            if (unityReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                report.AddError("BUILD_FAILED", $"Unity BuildPipeline result: {unityReport.summary.result}");
            }
            else if (!File.Exists(executablePath))
            {
                report.AddError("BUILD_EXE_MISSING", $"빌드 결과 exe를 찾지 못했습니다: {executablePath}");
            }
        }
        catch (Exception exception)
        {
            report.BuildFinishedAt = DateTime.Now;
            report.AddError("BUILD_EXCEPTION", exception.Message);
            Debug.LogException(exception);
        }

        WriteReport(report);

        if (report.ErrorCount > 0)
        {
            Debug.LogError($"[HWJ Midterm Build] Failed. errors={report.ErrorCount}, report={ReportFilePath}");
        }
        else
        {
            Debug.Log($"[HWJ Midterm Build] Succeeded. exe={executablePath}, report={ReportFilePath}");
        }
    }

    private static List<string> CollectExistingScenes()
    {
        List<string> scenes = new List<string>();

        for (int i = 0; i < CandidateScenePaths.Length; i++)
        {
            if (File.Exists(CandidateScenePaths[i]))
            {
                scenes.Add(CandidateScenePaths[i]);
            }
        }

        return scenes;
    }

    private static void WriteReport(HWJ_MidtermBuildReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder(2048);
        builder.AppendLine("# HWJ 중간발표 Windows 빌드 리포트");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {report.Source}");
        builder.AppendLine($"- 빌드 파일: {report.ExecutablePath}");
        builder.AppendLine($"- Unity 결과: {report.UnityResult}");
        builder.AppendLine($"- 빌드 시작: {report.BuildStartedAt:O}");
        builder.AppendLine($"- 빌드 종료: {report.BuildFinishedAt:O}");
        builder.AppendLine($"- 빌드 시간: {report.TotalTime}");
        builder.AppendLine($"- 빌드 크기: {report.TotalSizeBytes} bytes");
        builder.AppendLine();
        builder.AppendLine("## 포함 씬");

        for (int i = 0; i < report.ScenePaths.Count; i++)
        {
            builder.AppendLine($"- {report.ScenePaths[i]}");
        }

        builder.AppendLine();
        builder.AppendLine("|심각도|코드|내용|");
        builder.AppendLine("|---|---|---|");

        for (int i = 0; i < report.Issues.Count; i++)
        {
            HWJ_MidtermBuildIssue issue = report.Issues[i];
            builder.AppendLine($"|{issue.Severity}|{issue.Code}|{issue.Message}|");
        }

        builder.AppendLine();
        builder.AppendLine($"- Error: {report.ErrorCount}");
        builder.AppendLine($"- Warning: {report.WarningCount}");
        builder.AppendLine(report.ErrorCount == 0 ? "- 결과: 통과" : "- 결과: 실패");
        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private sealed class HWJ_MidtermBuildReport
    {
        public HWJ_MidtermBuildReport(string source, string executablePath)
        {
            Source = source;
            ExecutablePath = executablePath;
        }

        public string Source { get; }
        public string ExecutablePath { get; }
        public string UnityResult { get; set; } = "NotStarted";
        public DateTime BuildStartedAt { get; set; }
        public DateTime BuildFinishedAt { get; set; }
        public TimeSpan TotalTime { get; set; }
        public ulong TotalSizeBytes { get; set; }
        public List<string> ScenePaths { get; } = new List<string>();
        public List<HWJ_MidtermBuildIssue> Issues { get; } = new List<HWJ_MidtermBuildIssue>();
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        public void AddError(string code, string message)
        {
            ErrorCount++;
            Issues.Add(new HWJ_MidtermBuildIssue("Error", code, message));
        }

        public void AddWarning(string code, string message)
        {
            WarningCount++;
            Issues.Add(new HWJ_MidtermBuildIssue("Warning", code, message));
        }
    }

    private readonly struct HWJ_MidtermBuildIssue
    {
        public HWJ_MidtermBuildIssue(string severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }
    }
}
