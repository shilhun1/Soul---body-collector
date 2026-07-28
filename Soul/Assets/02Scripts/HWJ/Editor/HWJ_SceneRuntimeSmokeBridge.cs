using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Starts the HWJ scene in Play Mode, validates minimum runtime wiring, then exits Play Mode.
/// This is a bridge between pure data validation and manual hands-on play testing.
/// </summary>
[InitializeOnLoad]
public static class HWJ_SceneRuntimeSmokeBridge
{
    private const string ScenePath = "Assets/01Scenes/HWJ.unity";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunSceneRuntimeSmoke.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_SceneRuntimeSmokeReport.md";
    private const string RunningKey = "HWJ.SceneRuntimeSmoke.Running";
    private const string SourceKey = "HWJ.SceneRuntimeSmoke.Source";
    private const string StartedAtKey = "HWJ.SceneRuntimeSmoke.StartedAt";
    private const string ValidateAtKey = "HWJ.SceneRuntimeSmoke.ValidateAt";
    private const string HasEnteredPlayModeKey = "HWJ.SceneRuntimeSmoke.HasEnteredPlayMode";
    private const float ValidateDelaySeconds = 1.5f;
    private const float TimeoutSeconds = 30f;

    private static double nextFlagCheckTime;

    static HWJ_SceneRuntimeSmokeBridge()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/HWJ/Scene/Run HWJ Scene Runtime Smoke")]
    public static void RunFromMenu()
    {
        StartRuntimeSmoke("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Scene/Create HWJ Scene Runtime Smoke Flag")]
    public static void CreateRuntimeSmokeFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
        Debug.Log($"HWJ scene runtime smoke flag created: {FlagFilePath}");
    }

    private static void Update()
    {
        PollFlagFile();
        ContinueRuntimeSmokeIfNeeded();
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

        File.Delete(FlagFilePath);
        StartRuntimeSmoke("flag file");
    }

    private static void StartRuntimeSmoke(string source)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WriteExceptionReport(source, "Cannot start runtime smoke while Unity is already entering or running Play Mode.");
            return;
        }

        if (!EnsureHwjSceneReady(source))
        {
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(HasEnteredPlayModeKey, false);
        SessionState.SetString(SourceKey, source);
        SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
        SessionState.SetFloat(ValidateAtKey, 0f);

        WriteStartedReport(source);
        EditorApplication.isPlaying = true;
    }

    private static bool EnsureHwjSceneReady(string source)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid() && activeScene.path == ScenePath)
        {
            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            return true;
        }

        if (activeScene.IsValid() && activeScene.isDirty)
        {
            WriteExceptionReport(
                source,
                $"Active scene is dirty and is not the HWJ scene. Active='{activeScene.path}'. Save or close it before runtime smoke.");
            return false;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetBool(HasEnteredPlayModeKey, true);
            SessionState.SetFloat(
                ValidateAtKey,
                (float)(EditorApplication.timeSinceStartup + ValidateDelaySeconds));
        }
    }

    private static void ContinueRuntimeSmokeIfNeeded()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        float startedAt = SessionState.GetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);

        if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
        {
            FinishRuntimeSmokeWithException("Runtime smoke timed out before validation completed.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        if (!SessionState.GetBool(HasEnteredPlayModeKey, false))
        {
            return;
        }

        float validateAt = SessionState.GetFloat(ValidateAtKey, 0f);

        if (validateAt <= 0f || EditorApplication.timeSinceStartup < validateAt)
        {
            return;
        }

        HWJ_RuntimeSmokeReport report = ValidateRuntimeScene();
        WriteReport(SessionState.GetString(SourceKey, "unknown"), report);
        ClearSession();
        EditorApplication.isPlaying = false;
    }

    private static HWJ_RuntimeSmokeReport ValidateRuntimeScene()
    {
        List<HWJ_RuntimeSmokeIssue> issues = new List<HWJ_RuntimeSmokeIssue>();
        Scene activeScene = SceneManager.GetActiveScene();
        HWJ_GameManager[] managers = UnityEngine.Object.FindObjectsByType<HWJ_GameManager>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        HWJ_RootObjectDataResolver[] resolvers = UnityEngine.Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        HWJ_SpawnerSystem[] spawners = UnityEngine.Object.FindObjectsByType<HWJ_SpawnerSystem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        HWJ_RootObjectDataResolver player = FindResolverByType(resolvers, HWJ_ObjectType.Player);
        int enemyCount = CountResolversByType(resolvers, HWJ_ObjectType.Enemy);
        int bossCount = CountResolversByType(resolvers, HWJ_ObjectType.Boss);
        int possessableCount = CountPossessableResolvers(resolvers);

        if (managers.Length == 0)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Error("RUNTIME_GAMEMANAGER_MISSING", "No active HWJ_GameManager exists in Play Mode."));
        }
        else if (managers.Length > 1)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Warning("RUNTIME_GAMEMANAGER_DUPLICATE", $"There are {managers.Length} active HWJ_GameManager components."));
        }

        if (player == null)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Error("RUNTIME_PLAYER_MISSING", "No active player resolver exists in Play Mode."));
        }
        else
        {
            ValidateRuntimePlayer(player, managers.Length > 0 ? managers[0] : null, issues);
        }

        for (int i = 0; i < spawners.Length; i++)
        {
            string lastSpawnResult = spawners[i].LastSpawnResult;

            if (!string.IsNullOrEmpty(lastSpawnResult)
                && lastSpawnResult.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                issues.Add(HWJ_RuntimeSmokeIssue.Warning("RUNTIME_SPAWN_FAILED", lastSpawnResult));
            }
        }

        return new HWJ_RuntimeSmokeReport(
            activeScene.name,
            activeScene.path,
            managers.Length,
            player != null ? GetSceneObjectPath(player.gameObject) : "Missing Player",
            enemyCount,
            bossCount,
            spawners.Length,
            possessableCount,
            issues);
    }

    private static void ValidateRuntimePlayer(
        HWJ_RootObjectDataResolver player,
        HWJ_GameManager manager,
        List<HWJ_RuntimeSmokeIssue> issues)
    {
        RequireRuntimeComponent<HWJ_RuntimeStatusSystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_SoulSystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_PossessedBodySystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_PossessionSystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_BodyDecaySystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_CollapseSystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_BodyDiscoverySystem>(player.gameObject, issues);
        RequireRuntimeComponent<HWJ_PlayerMovementSystem>(player.gameObject, issues);

        if (manager == null)
        {
            return;
        }

        if (manager.PlayerResolver != player)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Error(
                "RUNTIME_MANAGER_PLAYER_RESOLVER_MISMATCH",
                "HWJ_GameManager.PlayerResolver does not point to the active player resolver."));
        }

        if (manager.PlayerPossession == null)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Error(
                "RUNTIME_MANAGER_POSSESSION_MISSING",
                "HWJ_GameManager.PlayerPossession is missing in Play Mode."));
        }

        if (manager.PlayerBodyDecay == null)
        {
            issues.Add(HWJ_RuntimeSmokeIssue.Error(
                "RUNTIME_MANAGER_DECAY_MISSING",
                "HWJ_GameManager.PlayerBodyDecay is missing in Play Mode."));
        }
    }

    private static void RequireRuntimeComponent<T>(
        GameObject owner,
        List<HWJ_RuntimeSmokeIssue> issues) where T : Component
    {
        if (owner.GetComponent<T>() != null)
        {
            return;
        }

        issues.Add(HWJ_RuntimeSmokeIssue.Error(
            "RUNTIME_COMPONENT_MISSING",
            $"{typeof(T).Name} is missing from {GetSceneObjectPath(owner)} in Play Mode."));
    }

    private static HWJ_RootObjectDataResolver FindResolverByType(
        HWJ_RootObjectDataResolver[] resolvers,
        HWJ_ObjectType objectType)
    {
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null
                && resolvers[i].RootObjectData != null
                && resolvers[i].ObjectType == objectType)
            {
                return resolvers[i];
            }
        }

        return null;
    }

    private static int CountResolversByType(
        HWJ_RootObjectDataResolver[] resolvers,
        HWJ_ObjectType objectType)
    {
        int count = 0;

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null
                && resolvers[i].RootObjectData != null
                && resolvers[i].ObjectType == objectType)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountPossessableResolvers(HWJ_RootObjectDataResolver[] resolvers)
    {
        int count = 0;

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null)
            {
                continue;
            }

            if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
                && enemyData.PossessionBody != null
                && enemyData.PossessionBody.canBePossessed)
            {
                count++;
                continue;
            }

        }

        return count;
    }

    private static void FinishRuntimeSmokeWithException(string message)
    {
        WriteExceptionReport(SessionState.GetString(SourceKey, "unknown"), message);
        ClearSession();

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void ClearSession()
    {
        SessionState.EraseBool(RunningKey);
        SessionState.EraseBool(HasEnteredPlayModeKey);
        SessionState.EraseString(SourceKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseFloat(ValidateAtKey);
    }

    private static string GetSceneObjectPath(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return "Missing Scene Object";
        }

        Stack<string> pathSegments = new Stack<string>();
        Transform current = targetObject.transform;

        while (current != null)
        {
            pathSegments.Push(current.name);
            current = current.parent;
        }

        string sceneName = string.IsNullOrEmpty(targetObject.scene.name)
            ? "Unsaved Scene"
            : targetObject.scene.name;
        return sceneName + "/" + string.Join("/", pathSegments.ToArray());
    }

    private static void WriteStartedReport(string source)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 씬 런타임 스모크 검증\n\n- 상태: Started\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n",
            Encoding.UTF8);
    }

    private static void WriteReport(string source, HWJ_RuntimeSmokeReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 씬 런타임 스모크 검증");
        builder.AppendLine();
        builder.AppendLine("- 상태: Finished");
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 씬 이름: {report.SceneName}");
        builder.AppendLine($"- 씬 경로: {report.ScenePath}");
        builder.AppendLine($"- 결과: {(report.IsValid ? "통과" : "실패")}");
        builder.AppendLine($"- Error: {report.ErrorCount}");
        builder.AppendLine($"- Warning: {report.WarningCount}");
        builder.AppendLine();
        builder.AppendLine("## 런타임 구성 요약");
        builder.AppendLine();
        builder.AppendLine("| 항목 | 값 |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| HWJ_GameManager | {report.GameManagerCount} |");
        builder.AppendLine($"| Player | {report.PlayerPath} |");
        builder.AppendLine($"| Enemy Resolver | {report.EnemyCount} |");
        builder.AppendLine($"| Boss Resolver | {report.BossCount} |");
        builder.AppendLine($"| HWJ_SpawnerSystem | {report.SpawnerCount} |");
        builder.AppendLine($"| Active Possessable Resolver | {report.PossessableResolverCount} |");
        builder.AppendLine();

        if (report.Issues.Count == 0)
        {
            builder.AppendLine("검증 문제가 없습니다.");
        }
        else
        {
            builder.AppendLine("## 검증 문제");
            builder.AppendLine();
            builder.AppendLine("| 심각도 | 코드 | 문제 |");
            builder.AppendLine("|---|---|---|");

            for (int i = 0; i < report.Issues.Count; i++)
            {
                HWJ_RuntimeSmokeIssue issue = report.Issues[i];
                builder.AppendLine($"| {issue.Severity} | {issue.Code} | {SanitizeCell(issue.Message)} |");
            }
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"HWJ scene runtime smoke report written: {ReportFilePath}");
    }

    private static void WriteExceptionReport(string source, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 씬 런타임 스모크 검증\n\n- 상태: Failed\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n- 문제: {message}\n",
            Encoding.UTF8);
    }

    private static string SanitizeCell(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private readonly struct HWJ_RuntimeSmokeIssue
    {
        private HWJ_RuntimeSmokeIssue(string severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public bool IsError => Severity == "Error";

        public static HWJ_RuntimeSmokeIssue Error(string code, string message)
        {
            return new HWJ_RuntimeSmokeIssue("Error", code, message);
        }

        public static HWJ_RuntimeSmokeIssue Warning(string code, string message)
        {
            return new HWJ_RuntimeSmokeIssue("Warning", code, message);
        }
    }

    private sealed class HWJ_RuntimeSmokeReport
    {
        public readonly string SceneName;
        public readonly string ScenePath;
        public readonly int GameManagerCount;
        public readonly string PlayerPath;
        public readonly int EnemyCount;
        public readonly int BossCount;
        public readonly int SpawnerCount;
        public readonly int PossessableResolverCount;
        public readonly List<HWJ_RuntimeSmokeIssue> Issues;
        public readonly int ErrorCount;
        public readonly int WarningCount;
        public bool IsValid => ErrorCount == 0;

        public HWJ_RuntimeSmokeReport(
            string sceneName,
            string scenePath,
            int gameManagerCount,
            string playerPath,
            int enemyCount,
            int bossCount,
            int spawnerCount,
            int possessableResolverCount,
            List<HWJ_RuntimeSmokeIssue> issues)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            GameManagerCount = gameManagerCount;
            PlayerPath = playerPath;
            EnemyCount = enemyCount;
            BossCount = bossCount;
            SpawnerCount = spawnerCount;
            PossessableResolverCount = possessableResolverCount;
            Issues = issues ?? new List<HWJ_RuntimeSmokeIssue>();

            for (int i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].IsError)
                {
                    ErrorCount++;
                }
                else
                {
                    WarningCount++;
                }
            }
        }
    }
}
