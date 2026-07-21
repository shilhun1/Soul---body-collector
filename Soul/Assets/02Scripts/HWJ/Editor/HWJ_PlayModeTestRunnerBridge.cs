using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Runs the HWJ PlayMode test assembly from an already-open Unity Editor.
/// This avoids the project lock problem that blocks a separate batchmode Test Runner process.
/// </summary>
[InitializeOnLoad]
public static class HWJ_PlayModeTestRunnerBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunPlayModeTests.flag";
    private const string ResultXmlPath = @"C:\Docs\Generated\HWJ_PlayModeTestResults.xml";
    private const string ResultSummaryPath = @"C:\Docs\Generated\HWJ_PlayModeTestSummary.md";
    private const string TestAssemblyName = "HWJ.PlayModeTests";
    private const double RunTimeoutSeconds = 180d;

    private static readonly HWJ_PlayModeTestCallbacks TestCallbacks = new HWJ_PlayModeTestCallbacks();
    private static TestRunnerApi activeTestRunnerApi;
    private static bool isRunning;
    private static double nextFlagCheckTime;
    private static double runStartedEditorTime;

    static HWJ_PlayModeTestRunnerBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Tests/Run HWJ PlayMode Tests")]
    public static void RunFromMenu()
    {
        RunPlayModeTests("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Tests/Create HWJ PlayMode Test Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"));
        Debug.Log($"HWJ PlayMode test flag created: {FlagFilePath}");
    }

    private static void PollFlagFile()
    {
        TryCompletePendingRunFromLocalResult();
        CheckRunTimeout();

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

        string flagText = File.ReadAllText(FlagFilePath, Encoding.UTF8).Trim();

        if (!string.Equals(flagText, "refreshed", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(FlagFilePath, "refreshed", Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }

        File.Delete(FlagFilePath);
        RunPlayModeTests("flag file after AssetDatabase refresh");
    }

    private static void RunPlayModeTests(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ PlayMode tests are already running.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ResultSummaryPath));
        isRunning = true;
        runStartedEditorTime = EditorApplication.timeSinceStartup;
        TestCallbacks.Reset(source);
        TestRunnerApi.RegisterTestCallback(TestCallbacks);

        Filter filter = new Filter
        {
            testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
            assemblyNames = new[] { TestAssemblyName }
        };

        ExecutionSettings settings = new ExecutionSettings(filter);
        activeTestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        string runId = activeTestRunnerApi.Execute(settings);

        File.WriteAllText(
            ResultSummaryPath,
            $"# HWJ PlayMode Test Run\n\n- Status: Started\n- Source: {source}\n- Run Id: {runId}\n- Started: {DateTime.Now:O}\n");
        Debug.Log($"HWJ PlayMode tests started by {source}. RunId={runId}");
    }

    private static void TryCompletePendingRunFromLocalResult()
    {
        if (!File.Exists(ResultSummaryPath))
        {
            return;
        }

        string summaryText = File.ReadAllText(ResultSummaryPath, Encoding.UTF8);

        if (!summaryText.Contains("- Status: Started"))
        {
            return;
        }

        string localResultPath = Path.Combine(Application.persistentDataPath, "TestResults.xml");

        if (!File.Exists(localResultPath))
        {
            return;
        }

        DateTime summaryWriteTime = File.GetLastWriteTimeUtc(ResultSummaryPath);
        DateTime localResultWriteTime = File.GetLastWriteTimeUtc(localResultPath);

        if (localResultWriteTime <= summaryWriteTime)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ResultXmlPath));
        File.Copy(localResultPath, ResultXmlPath, true);
        WriteSummaryFromResultXml(localResultPath, "LocalLow TestResults fallback");
        TestRunnerApi.UnregisterTestCallback(TestCallbacks);
        DisposeActiveApi();
        isRunning = false;
    }

    private static void CheckRunTimeout()
    {
        if (!isRunning || EditorApplication.timeSinceStartup - runStartedEditorTime < RunTimeoutSeconds)
        {
            return;
        }

        try
        {
            File.WriteAllText(
                ResultSummaryPath,
                $"# HWJ PlayMode Test Run\n\n- Status: Timed out\n- Timeout Seconds: {RunTimeoutSeconds:0}\n- Finished: {DateTime.Now:O}\n",
                Encoding.UTF8);
            Debug.LogError($"HWJ PlayMode test run timed out. Summary={ResultSummaryPath}");
        }
        finally
        {
            TestRunnerApi.UnregisterTestCallback(TestCallbacks);
            DisposeActiveApi();
            isRunning = false;
        }
    }

    private static void DisposeActiveApi()
    {
        if (activeTestRunnerApi == null)
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(activeTestRunnerApi);
        activeTestRunnerApi = null;
    }

    private static void WriteSummaryFromResultXml(string resultXmlPath, string source)
    {
        XmlDocument document = new XmlDocument();
        document.Load(resultXmlPath);
        XmlElement testRun = document["test-run"];

        string result = testRun != null ? testRun.GetAttribute("result") : "Unknown";
        string total = testRun != null ? testRun.GetAttribute("total") : "0";
        string passed = testRun != null ? testRun.GetAttribute("passed") : "0";
        string failed = testRun != null ? testRun.GetAttribute("failed") : "0";
        string skipped = testRun != null ? testRun.GetAttribute("skipped") : "0";
        string inconclusive = testRun != null ? testRun.GetAttribute("inconclusive") : "0";
        string duration = testRun != null ? testRun.GetAttribute("duration") : "0";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ PlayMode Test Run");
        builder.AppendLine();
        builder.AppendLine($"- Status: {result}");
        builder.AppendLine($"- Source: {source}");
        builder.AppendLine($"- Finished: {DateTime.Now:O}");
        builder.AppendLine($"- Total: {total}");
        builder.AppendLine($"- Passed: {passed}");
        builder.AppendLine($"- Failed: {failed}");
        builder.AppendLine($"- Skipped: {skipped}");
        builder.AppendLine($"- Inconclusive: {inconclusive}");
        builder.AppendLine($"- Duration: {duration}s");
        builder.AppendLine($"- Result XML: {ResultXmlPath}");

        XmlNodeList failedTests = document.SelectNodes("//test-case[@result='Failed']");

        if (failedTests != null && failedTests.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Failed Tests");

            for (int i = 0; i < failedTests.Count; i++)
            {
                XmlNode failedTest = failedTests[i];
                builder.AppendLine($"- {failedTest.Attributes?["fullname"]?.Value}: {failedTest.Attributes?["label"]?.Value}");
            }
        }

        File.WriteAllText(ResultSummaryPath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"HWJ PlayMode test fallback summary written. Summary={ResultSummaryPath}");
    }

    private sealed class HWJ_PlayModeTestCallbacks : ICallbacks
    {
        private readonly List<string> failedTests = new List<string>();
        private string runSource;
        private DateTime startedAt;

        public void Reset(string source)
        {
            runSource = source;
            startedAt = DateTime.Now;
            failedTests.Clear();
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"HWJ PlayMode test run started. Tests={testsToRun?.TestCaseCount ?? 0}");
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result == null
                || result.HasChildren
                || result.TestStatus != UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
            {
                return;
            }

            failedTests.Add($"{result.FullName}: {result.Message}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            try
            {
                if (result != null)
                {
                    TestRunnerApi.SaveResultToFile(result, ResultXmlPath);
                }

                WriteSummary(result);
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    ResultSummaryPath,
                    $"# HWJ PlayMode Test Run\n\n- Status: Result write failed\n- Error: {exception}\n");
                Debug.LogException(exception);
            }
            finally
            {
                TestRunnerApi.UnregisterTestCallback(this);
                DisposeActiveApi();
                isRunning = false;
            }
        }

        private void WriteSummary(ITestResultAdaptor result)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# HWJ PlayMode Test Run");
            builder.AppendLine();
            builder.AppendLine($"- Status: {(result != null && result.FailCount == 0 ? "Passed" : "Failed")}");
            builder.AppendLine($"- Source: {runSource}");
            builder.AppendLine($"- Started: {startedAt:O}");
            builder.AppendLine($"- Finished: {DateTime.Now:O}");

            if (result != null)
            {
                builder.AppendLine($"- Passed: {result.PassCount}");
                builder.AppendLine($"- Failed: {result.FailCount}");
                builder.AppendLine($"- Skipped: {result.SkipCount}");
                builder.AppendLine($"- Inconclusive: {result.InconclusiveCount}");
                builder.AppendLine($"- Duration: {result.Duration:0.###}s");
                builder.AppendLine($"- Result XML: {ResultXmlPath}");
            }
            else
            {
                builder.AppendLine("- Result: missing result adaptor");
            }

            if (failedTests.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Failed Tests");

                for (int i = 0; i < failedTests.Count; i++)
                {
                    builder.AppendLine($"- {failedTests[i]}");
                }
            }

            File.WriteAllText(ResultSummaryPath, builder.ToString());
            Debug.Log($"HWJ PlayMode test run finished. Summary={ResultSummaryPath}");
        }
    }
}
