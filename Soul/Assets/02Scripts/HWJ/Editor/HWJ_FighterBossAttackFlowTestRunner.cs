using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Runs only the fighter-boss attack-flow PlayMode tests from the already-open Editor.
/// A flag file avoids launching a second Unity process against the locked project.
/// </summary>
[InitializeOnLoad]
public static class HWJ_FighterBossAttackFlowTestRunner
{
    private const string FlagPath = @"C:\Docs\Generated\HWJ_RunFighterBossAttackFlow.flag";
    private const string ResultXmlPath = @"C:\Docs\Generated\HWJ_FighterBossAttackFlow.xml";
    private const string AssemblyName = "HWJ.FighterBossPlayModeTests";
    private const string TestClassName = "HWJ_FighterBossAttackFlowPlayModeTests";
    private const double TimeoutSeconds = 300d;

    private static readonly TestCallbacks Callbacks = new TestCallbacks();
    private static TestRunnerApi activeApi;
    private static bool isRunning;
    private static double nextFlagPoll;
    private static double runStartedAt;

    private static string SummaryPath => Path.Combine(
        Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
        "Assets",
        "02Scripts",
        "HWJ",
        "Art",
        "Boss",
        "_Generated",
        "Reports",
        "FighterBossAttackFlowPlayMode.md");

    static HWJ_FighterBossAttackFlowTestRunner()
    {
        EditorApplication.update -= PollFlag;
        EditorApplication.update += PollFlag;
    }

    [MenuItem("Tools/HWJ/Boss/Run Fighter Boss Attack Flow Tests")]
    public static void RunFromMenu()
    {
        StartRun("Unity Editor menu", false);
    }

    [MenuItem("Tools/HWJ/Boss/Run All Fighter Boss PlayMode Tests")]
    public static void RunAllFromMenu()
    {
        StartRun("Unity Editor full-suite menu", true);
    }

    private static void PollFlag()
    {
        TryRecoverCompletedRun();

        if (isRunning && EditorApplication.timeSinceStartup - runStartedAt > TimeoutSeconds)
        {
            FinishWithoutResult("Timed out");
            return;
        }

        if (EditorApplication.timeSinceStartup < nextFlagPoll)
        {
            return;
        }

        nextFlagPoll = EditorApplication.timeSinceStartup + 1d;

        if (!File.Exists(FlagPath)
            || isRunning
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        string request = File.ReadAllText(FlagPath, Encoding.UTF8);
        File.Delete(FlagPath);
        StartRun(
            request.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0
                ? "external full-suite validation flag"
                : "external validation flag",
            request.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    /// <summary>
    /// PlayMode entry reloads the Editor domain, so recover the official Test Runner XML
    /// when the in-memory callback instance does not survive that reload.
    /// </summary>
    private static void TryRecoverCompletedRun()
    {
        if (!File.Exists(SummaryPath))
        {
            return;
        }

        string summary = File.ReadAllText(SummaryPath, Encoding.UTF8);

        if (!summary.Contains("- Status: Started"))
        {
            return;
        }

        string localResultPath = Path.Combine(Application.persistentDataPath, "TestResults.xml");

        if (!File.Exists(localResultPath)
            || File.GetLastWriteTimeUtc(localResultPath) <= File.GetLastWriteTimeUtc(SummaryPath))
        {
            return;
        }

        XmlDocument document = new XmlDocument();
        document.Load(localResultPath);

        if (document.SelectSingleNode($"//test-suite[@fullname='{TestClassName}']") == null)
        {
            return;
        }

        XmlElement testRun = document["test-run"];
        Directory.CreateDirectory(Path.GetDirectoryName(ResultXmlPath));
        File.Copy(localResultPath, ResultXmlPath, true);
        StringBuilder report = new StringBuilder();
        report.AppendLine("# Fighter Boss Attack Flow PlayMode Test");
        report.AppendLine();
        report.AppendLine($"- Status: {testRun?.GetAttribute("result") ?? "Unknown"}");
        report.AppendLine("- Source: Unity Test Runner XML recovery");
        report.AppendLine($"- Finished: {DateTime.Now:O}");
        report.AppendLine($"- Passed: {testRun?.GetAttribute("passed") ?? "0"}");
        report.AppendLine($"- Failed: {testRun?.GetAttribute("failed") ?? "0"}");
        report.AppendLine($"- Duration: {testRun?.GetAttribute("duration") ?? "0"}s");
        report.AppendLine($"- Result XML: {ResultXmlPath}");
        File.WriteAllText(SummaryPath, report.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(
            "Assets/02Scripts/HWJ/Art/Boss/_Generated/Reports/FighterBossAttackFlowPlayMode.md",
            ImportAssetOptions.ForceUpdate);
        isRunning = false;
    }

    private static void StartRun(string source, bool runFullAssembly)
    {
        if (isRunning)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ResultXmlPath));
        Directory.CreateDirectory(Path.GetDirectoryName(SummaryPath));
        isRunning = true;
        runStartedAt = EditorApplication.timeSinceStartup;
        Callbacks.Reset(source);
        TestRunnerApi.RegisterTestCallback(Callbacks);

        Filter filter = new Filter
        {
            testMode = TestMode.PlayMode,
            assemblyNames = new[] { AssemblyName }
        };

        if (!runFullAssembly)
        {
            filter.groupNames = new[] { TestClassName };
        }

        activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        string runId = activeApi.Execute(new ExecutionSettings(filter));
        File.WriteAllText(
            SummaryPath,
            $"# Fighter Boss Attack Flow PlayMode Test\n\n- Status: Started\n- Run Id: {runId}\n- Source: {source}\n",
            Encoding.UTF8);
        Debug.Log($"[HWJ] Fighter boss attack-flow PlayMode tests started. RunId={runId}");
    }

    private static void FinishWithoutResult(string status)
    {
        File.WriteAllText(
            SummaryPath,
            $"# Fighter Boss Attack Flow PlayMode Test\n\n- Status: {status}\n- Finished: {DateTime.Now:O}\n",
            Encoding.UTF8);
        CleanupRunner();
    }

    private static void CleanupRunner()
    {
        TestRunnerApi.UnregisterTestCallback(Callbacks);

        if (activeApi != null)
        {
            UnityEngine.Object.DestroyImmediate(activeApi);
            activeApi = null;
        }

        isRunning = false;
    }

    private sealed class TestCallbacks : ICallbacks
    {
        private readonly List<string> failures = new List<string>();
        private string source;
        private DateTime startedAt;

        public void Reset(string runSource)
        {
            source = runSource;
            startedAt = DateTime.Now;
            failures.Clear();
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result != null && !result.HasChildren && result.TestStatus == TestStatus.Failed)
            {
                failures.Add($"{result.FullName}: {result.Message}");
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            try
            {
                if (result != null)
                {
                    TestRunnerApi.SaveResultToFile(result, ResultXmlPath);
                }

                StringBuilder report = new StringBuilder();
                report.AppendLine("# Fighter Boss Attack Flow PlayMode Test");
                report.AppendLine();
                report.AppendLine($"- Status: {(result != null && result.FailCount == 0 ? "Passed" : "Failed")}");
                report.AppendLine($"- Source: {source}");
                report.AppendLine($"- Started: {startedAt:O}");
                report.AppendLine($"- Finished: {DateTime.Now:O}");
                report.AppendLine($"- Passed: {result?.PassCount ?? 0}");
                report.AppendLine($"- Failed: {result?.FailCount ?? 0}");
                report.AppendLine($"- Duration: {result?.Duration ?? 0d:0.###}s");
                report.AppendLine($"- Result XML: {ResultXmlPath}");

                for (int i = 0; i < failures.Count; i++)
                {
                    report.AppendLine($"- Failure: {failures[i]}");
                }

                File.WriteAllText(SummaryPath, report.ToString(), Encoding.UTF8);
                AssetDatabase.ImportAsset(
                    "Assets/02Scripts/HWJ/Art/Boss/_Generated/Reports/FighterBossAttackFlowPlayMode.md",
                    ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                CleanupRunner();
            }
        }
    }
}
