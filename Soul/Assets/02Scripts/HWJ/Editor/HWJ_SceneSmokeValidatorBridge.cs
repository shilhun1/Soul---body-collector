using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 열린 씬이 HWJ 코어 루프를 수동 테스트할 수 있는 최소 배치를 갖췄는지 확인합니다.
/// 이 검증기는 씬을 수정하지 않고 읽기 전용 리포트만 생성합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_SceneSmokeValidatorBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunSceneSmoke.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_SceneSmokeReport.md";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_SceneSmokeValidatorBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Validation/Run Open Scene Smoke Report")]
    public static void RunOpenSceneSmokeReportFromMenu()
    {
        RunSceneSmokeReport("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Validation/Create Open Scene Smoke Flag")]
    public static void CreateOpenSceneSmokeFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
        Debug.Log($"HWJ scene smoke flag created: {FlagFilePath}");
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
        RunSceneSmokeReport("flag file");
    }

    private static void RunSceneSmokeReport(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ scene smoke validation is already running.");
            return;
        }

        isRunning = true;

        try
        {
            HWJ_OpenSceneSmokeReport report = ValidateOpenScene();
            WriteReport(source, report);
            LogReport(report);
        }
        catch (Exception exception)
        {
            WriteExceptionReport(source, exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    public static HWJ_OpenSceneSmokeReport ValidateOpenScene()
    {
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues =
            new List<HWJ_GameDataValidator.HWJ_EditorValidationIssue>();
        Scene activeScene = SceneManager.GetActiveScene();
        HWJ_RootObjectDataResolver[] resolvers = FindSceneComponents<HWJ_RootObjectDataResolver>();
        HWJ_GameManager[] managers = FindSceneComponents<HWJ_GameManager>();
        HWJ_SpawnerSystem[] spawners = FindSceneComponents<HWJ_SpawnerSystem>();
        HWJ_SpawnPoint[] spawnPoints = FindSceneComponents<HWJ_SpawnPoint>();
        HWJ_BossFlowSystem[] bossFlows = FindSceneComponents<HWJ_BossFlowSystem>();
        HWJ_BossBrainSystem[] bossBrains = FindSceneComponents<HWJ_BossBrainSystem>();

        List<HWJ_RootObjectDataResolver> players = CollectResolversByType(resolvers, HWJ_ObjectType.Player);
        List<HWJ_RootObjectDataResolver> enemies = CollectResolversByType(resolvers, HWJ_ObjectType.Enemy);
        List<HWJ_RootObjectDataResolver> bosses = CollectResolversByType(resolvers, HWJ_ObjectType.Boss);
        int possessableBodyCount = CountPossessableBodies(enemies)
            + CountPossessableSpawnEntries(spawners);

        ValidateGameManagerSetup(issues, managers);
        ValidatePlayerSetup(issues, players, managers);
        ValidateSpawnerSetup(issues, spawners, spawnPoints);
        ValidatePossessionTargets(issues, enemies, bosses, possessableBodyCount);
        ValidateBossSetup(issues, bosses, bossFlows, bossBrains);

        return new HWJ_OpenSceneSmokeReport(
            activeScene.name,
            activeScene.path,
            issues,
            managers.Length,
            players.Count,
            enemies.Count,
            bosses.Count,
            spawners.Length,
            spawnPoints.Length,
            possessableBodyCount);
    }

    private static void ValidateGameManagerSetup(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        HWJ_GameManager[] managers)
    {
        if (managers.Length == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_GAMEMANAGER_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_GameManager",
                "The open scene has no HWJ_GameManager. Some tests can still run if all references are local, but scene-level input/database access may be missing.",
                "Place one HWJ_GameManager in the scene or keep it in a persistent bootstrap scene.");
            return;
        }

        if (managers.Length > 1)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_GAMEMANAGER_DUPLICATE",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_GameManager",
                $"The open scene has {managers.Length} HWJ_GameManager components.",
                "Keep one active HWJ_GameManager unless this scene is intentionally testing duplicate manager cleanup.");
        }
    }

    private static void ValidatePlayerSetup(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        List<HWJ_RootObjectDataResolver> players,
        HWJ_GameManager[] managers)
    {
        if (players.Count == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_PLAYER_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "Player Resolver",
                "No active HWJ_RootObjectDataResolver with ObjectType Player was found.",
                "Place a player object or configure a PlayerStart spawn entry that spawns a player before manual testing.");
            return;
        }

        if (players.Count > 1)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_PLAYER_MULTIPLE",
                "SCENE-TEST-001",
                "Open Scene",
                "Player Resolver",
                $"The open scene has {players.Count} active player resolvers.",
                "Keep one controllable player for the HWJ smoke test scene unless this is a multiplayer/cutscene test.");
        }

        for (int i = 0; i < players.Count; i++)
        {
            HWJ_RootObjectDataResolver player = players[i];
            string objectPath = GetSceneObjectPath(player.gameObject);
            bool canRuntimeBootstrap = CanRuntimeBootstrapPlayer(managers);

            ValidateResolverData(issues, player, "SCENE_PLAYER_DATA_INVALID", "Player RootObjectData");
            RequirePlayerCoreComponent<HWJ_RuntimeStatusSystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_STATUS_MISSING", "HWJ_RuntimeStatusSystem", canRuntimeBootstrap);
            RequirePlayerCoreComponent<HWJ_SoulSystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_SOUL_MISSING", "HWJ_SoulSystem", canRuntimeBootstrap);
            RequirePlayerCoreComponent<HWJ_PossessionSystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_POSSESSION_MISSING", "HWJ_PossessionSystem", canRuntimeBootstrap);
            RequirePlayerCoreComponent<HWJ_BodyDiscoverySystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_DISCOVERY_MISSING", "HWJ_BodyDiscoverySystem", canRuntimeBootstrap);
            RequirePlayerCoreComponent<HWJ_BodyDecaySystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_DECAY_MISSING", "HWJ_BodyDecaySystem", canRuntimeBootstrap);
            RequirePlayerCoreComponent<HWJ_CollapseSystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_COLLAPSE_MISSING", "HWJ_CollapseSystem", canRuntimeBootstrap);
            RequireComponent<HWJ_PlayerMovementSystem>(issues, player.gameObject, objectPath, "SCENE_PLAYER_MOVEMENT_MISSING", "HWJ_PlayerMovementSystem");

            if (!HasInputForPlayer(player.gameObject, managers))
            {
                AddIssue(
                    issues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_PLAYER_INPUT_MISSING",
                    "SCENE-TEST-001",
                    objectPath,
                    "HWJ_PlayerInputSystem",
                    "No HWJ_PlayerInputSystem was found on the player or under an active HWJ_GameManager.",
                    "Add HWJ_PlayerInputSystem to the player or to the GameManager hierarchy.");
            }
        }
    }

    private static void ValidateSpawnerSetup(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        HWJ_SpawnerSystem[] spawners,
        HWJ_SpawnPoint[] spawnPoints)
    {
        if (spawners.Length == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_SPAWNER_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_SpawnerSystem",
                "No HWJ_SpawnerSystem was found. Manual scene objects can still be tested, but spawn-table flow cannot be smoke-tested.",
                "Add HWJ_SpawnerSystem when testing player starts, monsters, boss adds, or stat orb spawning.");
        }

        for (int i = 0; i < spawners.Length; i++)
        {
            HWJ_SpawnerSystem spawner = spawners[i];
            string objectPath = GetSceneObjectPath(spawner.gameObject);
            HWJ_SpawnTableDataSO spawnTable = ReadObjectReference<HWJ_SpawnTableDataSO>(spawner, "spawnTable");

            if (spawnTable == null)
            {
                AddIssue(
                    issues,
                    HWJ_GameDataValidationSeverity.Warning,
                    "SCENE_SPAWNER_TABLE_MISSING",
                    "SCENE-TEST-001",
                    objectPath,
                    "spawnTable",
                    "Spawner has no SpawnTableDataSO assigned.",
                    "Assign a HWJ_SpawnTableDataSO or use direct manual objects for this scene test.");
            }
            else if (spawnTable.Entries == null || spawnTable.Entries.Length == 0)
            {
                AddIssue(
                    issues,
                    HWJ_GameDataValidationSeverity.Warning,
                    "SCENE_SPAWNER_TABLE_EMPTY",
                    "SCENE-TEST-001",
                    objectPath,
                    "spawnTable.entries",
                    "Spawner table has no entries.",
                    "Add player, enemy, boss, or stat orb spawn entries to the table.");
            }
        }

        if (spawnPoints.Length == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_SPAWNPOINT_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_SpawnPoint",
                "No spawn points were found.",
                "Add at least a PlayerStart and the enemy/boss points required by the current test scene.");
            return;
        }

        if (!HasSpawnPointType(spawnPoints, HWJ_SpawnPointType.PlayerStart))
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_PLAYER_START_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_SpawnPoint.PlayerStart",
                "No PlayerStart spawn point was found.",
                "Add a HWJ_SpawnPoint with SpawnPointType PlayerStart.");
        }
    }

    private static void ValidatePossessionTargets(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        List<HWJ_RootObjectDataResolver> enemies,
        List<HWJ_RootObjectDataResolver> bosses,
        int possessableBodyCount)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            ValidateEnemyOrBossRuntime(issues, enemies[i], "Enemy");
        }

        for (int i = 0; i < bosses.Count; i++)
        {
            ValidateEnemyOrBossRuntime(issues, bosses[i], "Boss");
        }

        if (possessableBodyCount == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_POSSESSABLE_BODY_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "Possession Target",
                "No possessable enemy or boss body data was found in active scene resolvers.",
                "Place a possessable corpse test object or configure a spawner entry that creates one.");
        }
    }

    private static void ValidateBossSetup(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        List<HWJ_RootObjectDataResolver> bosses,
        HWJ_BossFlowSystem[] bossFlows,
        HWJ_BossBrainSystem[] bossBrains)
    {
        if (bosses.Count == 0 && bossFlows.Length == 0 && bossBrains.Length == 0)
        {
            return;
        }

        if (bossFlows.Length == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_BOSS_FLOW_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_BossFlowSystem",
                "Boss-related objects exist, but no HWJ_BossFlowSystem was found.",
                "Add HWJ_BossFlowSystem when testing boss entry, defeat, reward, or region unlock flow.");
        }

        if (bossBrains.Length == 0)
        {
            AddIssue(
                issues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_BOSS_BRAIN_MISSING",
                "SCENE-TEST-001",
                "Open Scene",
                "HWJ_BossBrainSystem",
                "Boss-related objects exist, but no HWJ_BossBrainSystem was found.",
                "Add HWJ_BossBrainSystem to the boss object when testing boss FSM and patterns.");
        }
    }

    private static void ValidateEnemyOrBossRuntime(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        HWJ_RootObjectDataResolver resolver,
        string label)
    {
        string objectPath = GetSceneObjectPath(resolver.gameObject);
        ValidateResolverData(issues, resolver, $"SCENE_{label.ToUpperInvariant()}_DATA_INVALID", $"{label} RootObjectData");
        RequireComponent<HWJ_RuntimeStatusSystem>(issues, resolver.gameObject, objectPath, $"SCENE_{label.ToUpperInvariant()}_STATUS_MISSING", "HWJ_RuntimeStatusSystem");
        RequireComponent<HWJ_CombatSystem>(issues, resolver.gameObject, objectPath, $"SCENE_{label.ToUpperInvariant()}_COMBAT_MISSING", "HWJ_CombatSystem");

        if (resolver.ObjectType == HWJ_ObjectType.Enemy)
        {
            RequireComponent<HWJ_MonsterAISystem>(issues, resolver.gameObject, objectPath, "SCENE_ENEMY_AI_MISSING", "HWJ_MonsterAISystem");
        }
    }

    private static void ValidateResolverData(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        HWJ_RootObjectDataResolver resolver,
        string validationCode,
        string fieldName)
    {
        if (resolver == null)
        {
            return;
        }

        if (resolver.RootObjectData != null)
        {
            return;
        }

        AddIssue(
            issues,
            HWJ_GameDataValidationSeverity.Error,
            validationCode,
            "SCENE-TEST-001",
            GetSceneObjectPath(resolver.gameObject),
            fieldName,
            "Resolver has no RootObjectData assigned.",
            "Assign a RootObjectData asset or spawn this object through HWJ_SpawnerSystem.");
    }

    private static void RequireComponent<T>(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        GameObject owner,
        string objectPath,
        string validationCode,
        string componentName) where T : Component
    {
        if (owner.GetComponent<T>() != null)
        {
            return;
        }

        AddIssue(
            issues,
            HWJ_GameDataValidationSeverity.Error,
            validationCode,
            "SCENE-TEST-001",
            objectPath,
            componentName,
            $"{componentName} is missing.",
            $"Add {componentName} to this object or use a prefab that includes it.");
    }

    private static void RequirePlayerCoreComponent<T>(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        GameObject owner,
        string objectPath,
        string validationCode,
        string componentName,
        bool canRuntimeBootstrap) where T : Component
    {
        if (owner.GetComponent<T>() != null)
        {
            return;
        }

        HWJ_GameDataValidationSeverity severity = canRuntimeBootstrap
            ? HWJ_GameDataValidationSeverity.Warning
            : HWJ_GameDataValidationSeverity.Error;
        string problem = canRuntimeBootstrap
            ? $"{componentName} is missing in the edit-time scene, but HWJ_GameManager can add it at runtime."
            : $"{componentName} is missing.";
        string fix = canRuntimeBootstrap
            ? $"Runtime can continue, but the final player prefab should include {componentName} explicitly."
            : $"Add {componentName} to this object or use a prefab that includes it.";

        AddIssue(
            issues,
            severity,
            validationCode,
            "SCENE-TEST-001",
            objectPath,
            componentName,
            problem,
            fix);
    }

    private static bool HasInputForPlayer(GameObject playerObject, HWJ_GameManager[] managers)
    {
        if (playerObject.GetComponent<HWJ_PlayerInputSystem>() != null)
        {
            return true;
        }

        for (int i = 0; i < managers.Length; i++)
        {
            HWJ_GameManager manager = managers[i];

            if (manager == null)
            {
                continue;
            }

            if (manager.PlayerInput != null || manager.GetComponentInChildren<HWJ_PlayerInputSystem>(true) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountPossessableBodies(List<HWJ_RootObjectDataResolver> resolvers)
    {
        int count = 0;

        for (int i = 0; i < resolvers.Count; i++)
        {
            if (IsPossessableBody(resolvers[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountPossessableSpawnEntries(HWJ_SpawnerSystem[] spawners)
    {
        int count = 0;

        for (int i = 0; i < spawners.Length; i++)
        {
            HWJ_SpawnTableDataSO spawnTable = ReadObjectReference<HWJ_SpawnTableDataSO>(spawners[i], "spawnTable");

            if (spawnTable == null || spawnTable.Entries == null)
            {
                continue;
            }

            for (int entryIndex = 0; entryIndex < spawnTable.Entries.Length; entryIndex++)
            {
                HWJ_SpawnEntryData entry = spawnTable.Entries[entryIndex];

                if (entry != null && IsPossessableRootObject(entry.rootObjectData))
                {
                    count += Mathf.Max(1, entry.spawnCount);
                }
            }
        }

        return count;
    }

    private static bool IsPossessableBody(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.PossessionBody != null
            && enemyData.PossessionBody.canBePossessed)
        {
            return true;
        }

        return false;
    }

    private static bool IsPossessableRootObject(HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null)
        {
            return false;
        }

        if (rootObjectData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.PossessionBody != null
            && enemyData.PossessionBody.canBePossessed)
        {
            return true;
        }

        return false;
    }

    private static bool CanRuntimeBootstrapPlayer(HWJ_GameManager[] managers)
    {
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(managers[i]);
            SerializedProperty property = serializedObject.FindProperty("autoAddMissingPlayerCoreSystems");

            if (property == null || property.boolValue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSpawnPointType(HWJ_SpawnPoint[] spawnPoints, HWJ_SpawnPointType spawnPointType)
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && spawnPoints[i].SpawnPointType == spawnPointType)
            {
                return true;
            }
        }

        return false;
    }

    private static List<HWJ_RootObjectDataResolver> CollectResolversByType(
        HWJ_RootObjectDataResolver[] resolvers,
        HWJ_ObjectType objectType)
    {
        List<HWJ_RootObjectDataResolver> filtered = new List<HWJ_RootObjectDataResolver>();

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver != null && resolver.RootObjectData != null && resolver.ObjectType == objectType)
            {
                filtered.Add(resolver);
            }
        }

        return filtered;
    }

    private static T ReadObjectReference<T>(UnityEngine.Object owner, string serializedFieldName)
        where T : UnityEngine.Object
    {
        SerializedObject serializedObject = new SerializedObject(owner);
        SerializedProperty property = serializedObject.FindProperty(serializedFieldName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static T[] FindSceneComponents<T>() where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

    private static void AddIssue(
        List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
        HWJ_GameDataValidationSeverity severity,
        string validationCode,
        string requirementId,
        string assetPath,
        string fieldName,
        string problem,
        string fix)
    {
        issues.Add(new HWJ_GameDataValidator.HWJ_EditorValidationIssue(
            severity,
            validationCode,
            requirementId,
            assetPath,
            fieldName,
            problem,
            fix));
    }

    private static void WriteReport(string source, HWJ_OpenSceneSmokeReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 열린 씬 스모크 검증");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 씬 이름: {report.SceneName}");
        builder.AppendLine($"- 씬 경로: {report.ScenePath}");
        builder.AppendLine($"- 결과: {(report.IsValid ? "통과" : "실패")}");
        builder.AppendLine($"- Error: {report.ErrorCount}");
        builder.AppendLine($"- Warning: {report.WarningCount}");
        builder.AppendLine($"- Info: {report.InfoCount}");
        builder.AppendLine();
        builder.AppendLine("## 씬 구성 요약");
        builder.AppendLine();
        builder.AppendLine("| 항목 | 수량 |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| HWJ_GameManager | {report.GameManagerCount} |");
        builder.AppendLine($"| Player Resolver | {report.PlayerCount} |");
        builder.AppendLine($"| Enemy Resolver | {report.EnemyCount} |");
        builder.AppendLine($"| Boss Resolver | {report.BossCount} |");
        builder.AppendLine($"| HWJ_SpawnerSystem | {report.SpawnerCount} |");
        builder.AppendLine($"| HWJ_SpawnPoint | {report.SpawnPointCount} |");
        builder.AppendLine($"| Possessable Body Data | {report.PossessableBodyCount} |");
        builder.AppendLine();

        if (report.Issues.Count == 0)
        {
            builder.AppendLine("검증 문제가 없습니다.");
        }
        else
        {
            builder.AppendLine("## 검증 문제");
            builder.AppendLine();
            builder.AppendLine("| 심각도 | 코드 | 요구사항 | 씬 오브젝트 | 필드 | 문제 | 수정 방법 |");
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
        Debug.Log($"HWJ scene smoke report written: {ReportFilePath}");
    }

    private static void WriteExceptionReport(string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 열린 씬 스모크 검증 예외\n\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n\n```text\n{exception}\n```\n",
            Encoding.UTF8);
    }

    private static void LogReport(HWJ_OpenSceneSmokeReport report)
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

        string summary = $"[HWJ Scene Smoke] Errors:{report.ErrorCount}, Warnings:{report.WarningCount}, Info:{report.InfoCount}. Report:{ReportFilePath}";

        if (report.ErrorCount > 0)
        {
            Debug.LogError(summary);
        }
        else
        {
            Debug.Log(summary);
        }
    }

    private static string SanitizeCell(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    public sealed class HWJ_OpenSceneSmokeReport
    {
        public readonly string SceneName;
        public readonly string ScenePath;
        public readonly List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> Issues;
        public readonly int ErrorCount;
        public readonly int WarningCount;
        public readonly int InfoCount;
        public readonly int GameManagerCount;
        public readonly int PlayerCount;
        public readonly int EnemyCount;
        public readonly int BossCount;
        public readonly int SpawnerCount;
        public readonly int SpawnPointCount;
        public readonly int PossessableBodyCount;
        public bool IsValid => ErrorCount == 0;

        public HWJ_OpenSceneSmokeReport(
            string sceneName,
            string scenePath,
            List<HWJ_GameDataValidator.HWJ_EditorValidationIssue> issues,
            int gameManagerCount,
            int playerCount,
            int enemyCount,
            int bossCount,
            int spawnerCount,
            int spawnPointCount,
            int possessableBodyCount)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            Issues = issues ?? new List<HWJ_GameDataValidator.HWJ_EditorValidationIssue>();
            GameManagerCount = gameManagerCount;
            PlayerCount = playerCount;
            EnemyCount = enemyCount;
            BossCount = bossCount;
            SpawnerCount = spawnerCount;
            SpawnPointCount = spawnPointCount;
            PossessableBodyCount = possessableBodyCount;

            for (int i = 0; i < Issues.Count; i++)
            {
                switch (Issues[i].Severity)
                {
                    case HWJ_GameDataValidationSeverity.Error:
                        ErrorCount++;
                        break;
                    case HWJ_GameDataValidationSeverity.Warning:
                        WarningCount++;
                        break;
                    default:
                        InfoCount++;
                        break;
                }
            }
        }
    }
}
