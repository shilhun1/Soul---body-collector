using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs HWJ editor validators from an already-open Unity Editor and writes a document-friendly report.
/// </summary>
[InitializeOnLoad]
public static class HWJ_ValidationRunnerBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunValidation.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_ValidationReport.md";
    private const string ModeAll = "all";
    private const string ModeSceneSaveIdentities = "scene-save-identities";
    private const string ModeSceneBossSetups = "scene-boss-setups";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_ValidationRunnerBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Validation/Run All And Write Report")]
    public static void RunAllFromMenu()
    {
        RunValidation(ModeAll, "Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Validation/Run Scene Save Identity Report")]
    public static void RunSceneSaveIdentitiesFromMenu()
    {
        RunValidation(ModeSceneSaveIdentities, "Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Validation/Run Scene Boss Setup Report")]
    public static void RunSceneBossSetupsFromMenu()
    {
        RunValidation(ModeSceneBossSetups, "Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Validation/Create All Validation Flag")]
    public static void CreateAllValidationFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, ModeAll, Encoding.UTF8);
        Debug.Log($"HWJ validation flag created: {FlagFilePath}");
    }

    private static void PollFlagFile()
    {
        if (EditorApplication.timeSinceStartup < nextFlagCheckTime)
        {
            return;
        }

        nextFlagCheckTime = EditorApplication.timeSinceStartup + 2d;

        if (!File.Exists(FlagFilePath))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        string mode = File.ReadAllText(FlagFilePath).Trim();
        File.Delete(FlagFilePath);
        RunValidation(string.IsNullOrWhiteSpace(mode) ? ModeAll : mode, "flag file");
    }

    private static void RunValidation(string mode, string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ validation is already running.");
            return;
        }

        isRunning = true;

        try
        {
            HWJ_GameDataValidator.HWJ_EditorValidationReport report;
            string title;

            switch (mode)
            {
                case ModeSceneSaveIdentities:
                    report = HWJ_GameDataValidator.ValidateOpenSceneSaveIdentities();
                    title = "열린 씬 저장 ID 검증";
                    break;
                case ModeSceneBossSetups:
                    report = HWJ_GameDataValidator.ValidateOpenSceneBossSetups();
                    title = "열린 씬 보스 세팅 검증";
                    break;
                case ModeAll:
                    report = HWJ_GameDataValidator.ValidateAllGameData();
                    title = "전체 게임 데이터 검증";
                    break;
                default:
                    WriteUnknownModeReport(mode, source);
                    Debug.LogError($"HWJ validation mode is unknown. Mode={mode}");
                    return;
            }

            WriteReport(title, mode, source, report);
            LogReport(report);
        }
        catch (Exception exception)
        {
            WriteExceptionReport(mode, source, exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static void WriteReport(
        string title,
        string mode,
        string source,
        HWJ_GameDataValidator.HWJ_EditorValidationReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"# {title}");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 실행 모드: {mode}");
        builder.AppendLine($"- 결과: {(report.IsValid ? "통과" : "실패")}");
        builder.AppendLine($"- Error: {report.ErrorCount}");
        builder.AppendLine($"- Warning: {report.WarningCount}");
        builder.AppendLine($"- Info: {report.InfoCount}");
        builder.AppendLine();

        if (report.Issues.Count == 0)
        {
            builder.AppendLine("검증 문제가 없습니다.");
        }
        else
        {
            builder.AppendLine("| 심각도 | 코드 | 요구사항 | 에셋/오브젝트 | 필드 | 문제 | 수정 방법 |");
            builder.AppendLine("|---|---|---|---|---|---|---|");

            for (int i = 0; i < report.Issues.Count; i++)
            {
                HWJ_GameDataValidator.HWJ_EditorValidationIssue issue = report.Issues[i];
                builder.Append("| ");
                builder.Append(SanitizeCell(issue.Severity.ToString()));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.ValidationCode));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.RequirementId));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.AssetPath));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.FieldName));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.Problem));
                builder.Append(" | ");
                builder.Append(SanitizeCell(issue.Fix));
                builder.AppendLine(" |");
            }
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"HWJ validation report written: {ReportFilePath}");
    }

    private static void WriteUnknownModeReport(string mode, string source)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 검증 실행 실패\n\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n- 알 수 없는 모드: {mode}\n",
            Encoding.UTF8);
    }

    private static void WriteExceptionReport(string mode, string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 검증 실행 예외\n\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n- 실행 모드: {mode}\n\n```text\n{exception}\n```\n",
            Encoding.UTF8);
    }

    private static void LogReport(HWJ_GameDataValidator.HWJ_EditorValidationReport report)
    {
        for (int i = 0; i < report.Issues.Count; i++)
        {
            HWJ_GameDataValidator.HWJ_EditorValidationIssue issue = report.Issues[i];
            string line = issue.ToLogLine();

            if (issue.Severity == HWJ_GameDataValidationSeverity.Error)
            {
                Debug.LogError(line);
            }
            else if (issue.Severity == HWJ_GameDataValidationSeverity.Warning)
            {
                Debug.LogWarning(line);
            }
            else
            {
                Debug.Log(line);
            }
        }

        Debug.Log($"[HWJ Validation Bridge] {report.ToSummaryText()} Report:{ReportFilePath}");
    }

    private static string SanitizeCell(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return value
            .Replace("|", "\\|")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}
