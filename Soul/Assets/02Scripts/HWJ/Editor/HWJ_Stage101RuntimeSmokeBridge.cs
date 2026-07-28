using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage1-1 씬이 Play Mode에서도 실제 플레이어, 몬스터, 기믹 그래픽을 표시하는지 확인하는 발표용 검증 도구입니다.
/// 씬과 런타임 시스템을 수정하지 않고, 검증 결과만 C:\Docs\Generated 아래에 기록합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_Stage101RuntimeSmokeBridge
{
    private const string ScenePath = "Assets/01Scenes/HWJ_Stage1_01_RuinedVillage.unity";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunStage101RuntimeSmoke.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_Stage1_01_RuntimeSmokeReport.md";
    private const string RunningKey = "HWJ.Stage101RuntimeSmoke.Running";
    private const string SourceKey = "HWJ.Stage101RuntimeSmoke.Source";
    private const string StartedAtKey = "HWJ.Stage101RuntimeSmoke.StartedAt";
    private const string ValidateAtKey = "HWJ.Stage101RuntimeSmoke.ValidateAt";
    private const string EnteredPlayModeKey = "HWJ.Stage101RuntimeSmoke.EnteredPlayMode";
    private const float ValidateDelaySeconds = 1.75f;
    private const float TimeoutSeconds = 35f;

    private static double nextFlagPollTime;

    static HWJ_Stage101RuntimeSmokeBridge()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Stage1/Run Stage1-1 Runtime Smoke")]
    public static void RunFromMenu()
    {
        StartSmoke("Unity Editor menu");
    }

    [MenuItem("Tools/Stage1/Create Stage1-1 Runtime Smoke Flag")]
    public static void CreateRuntimeSmokeFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
        Debug.Log($"[Stage1-1 Runtime Smoke] Flag created: {FlagFilePath}");
    }

    private static void Update()
    {
        PollFlag();
        ContinueSmokeIfNeeded();
    }

    private static void PollFlag()
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
        StartSmoke("flag file");
    }

    private static void StartSmoke(string source)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WriteExceptionReport(source, "Unity가 이미 Play Mode로 전환 중이거나 실행 중이라 Stage1-1 런타임 검증을 시작하지 못했습니다.");
            return;
        }

        if (!EnsureStage101SceneReady(source))
        {
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(EnteredPlayModeKey, false);
        SessionState.SetString(SourceKey, source);
        SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
        SessionState.SetFloat(ValidateAtKey, 0f);

        WriteStartedReport(source);
        EditorApplication.isPlaying = true;
    }

    private static bool EnsureStage101SceneReady(string source)
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
                $"현재 열려 있는 씬이 저장되지 않은 상태입니다. Active='{activeScene.path}'. 저장 후 다시 실행하세요.");
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
            SessionState.SetBool(EnteredPlayModeKey, true);
            SessionState.SetFloat(ValidateAtKey, (float)(EditorApplication.timeSinceStartup + ValidateDelaySeconds));
        }
    }

    private static void ContinueSmokeIfNeeded()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        float startedAt = SessionState.GetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);

        if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
        {
            FinishWithException("Stage1-1 런타임 검증 시간이 초과되었습니다.");
            return;
        }

        if (!EditorApplication.isPlaying || !SessionState.GetBool(EnteredPlayModeKey, false))
        {
            return;
        }

        float validateAt = SessionState.GetFloat(ValidateAtKey, 0f);

        if (validateAt <= 0f || EditorApplication.timeSinceStartup < validateAt)
        {
            return;
        }

        HWJ_Stage101RuntimeSmokeReport report = ValidatePlayModeScene();
        WriteReport(SessionState.GetString(SourceKey, "unknown"), report);
        ClearSession();
        EditorApplication.isPlaying = false;
    }

    private static HWJ_Stage101RuntimeSmokeReport ValidatePlayModeScene()
    {
        List<HWJ_Stage101RuntimeIssue> issues = new List<HWJ_Stage101RuntimeIssue>();
        Scene activeScene = SceneManager.GetActiveScene();
        HWJ_RootObjectDataResolver[] resolvers = UnityEngine.Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        GameObject playerObject = ResolvePlayerObject(resolvers);
        int enemyCount = CountResolversByType(resolvers, HWJ_ObjectType.Enemy);
        int bossCount = CountResolversByType(resolvers, HWJ_ObjectType.Boss);
        int possessableCount = CountPossessableResolvers(resolvers);
        int visibleSpriteRendererCount = CountVisibleSpriteRenderers();
        int animatorWithControllerCount = CountAnimatorsWithController();

        ValidatePlayer(playerObject, issues);
        ValidateEnemies(resolvers, issues);
        ValidateSceneSystems(issues);
        ValidateMainCamera(playerObject, issues);
        ValidateBackground(issues);
        ValidateVisibleGimmick("PF_Checkpoint_Stage1_01_Entrance", true, issues);
        ValidateVisibleGimmick("PF_Checkpoint_Stage1_01_BeforeGimmicks", true, issues);
        ValidateVisibleGimmick("PF_SpikeTrap_Stage1_01_DashGap", true, issues);
        ValidateVisibleGimmick("PF_SpikeTrap_Stage1_01_FinalPit", true, issues);
        ValidateVisibleGimmick("PF_SandstormLane_Stage1_01_SoulPreview", true, issues);
        ValidateVisibleGimmick("PF_BowSwitch_Stage1_01_LongShot", true, issues);
        ValidateVisibleGimmick("PF_GimmickDoor_Stage1_01_BowSwitchDoor", true, issues);
        ValidateVisibleGimmick("PF_SoulPassWall_Stage1_01_ScoutGate", true, issues);
        ValidateVisibleGimmick("HWJ_Portal_Stage1_01_ToOutpost", true, issues);

        if (enemyCount < 8)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(
                "RUNTIME_ENEMY_COUNT_LOW",
                $"Stage1-1에 플레이 가능한 적이 부족합니다. 현재 감지된 Enemy Resolver 수: {enemyCount}, 요구: 8 이상"));
        }

        if (visibleSpriteRendererCount < 60)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(
                "RUNTIME_VISIBLE_SPRITE_TOO_FEW",
                $"Game View에 표시될 SpriteRenderer가 부족합니다. 현재: {visibleSpriteRendererCount}"));
        }

        return new HWJ_Stage101RuntimeSmokeReport(
            activeScene.name,
            activeScene.path,
            playerObject != null ? GetScenePath(playerObject) : "Missing",
            enemyCount,
            bossCount,
            possessableCount,
            visibleSpriteRendererCount,
            animatorWithControllerCount,
            issues);
    }

    private static GameObject ResolvePlayerObject(HWJ_RootObjectDataResolver[] resolvers)
    {
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].gameObject;
            }
        }

        return GameObject.Find("HWJ_Player");
    }

    private static int CountResolversByType(HWJ_RootObjectDataResolver[] resolvers, HWJ_ObjectType objectType)
    {
        int count = 0;

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == objectType)
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
            if (resolvers[i] == null || resolvers[i].RootObjectData == null)
            {
                continue;
            }

            if (TryResolveCanBePossessed(resolvers[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryResolveCanBePossessed(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return enemyData.PossessionBody != null && enemyData.PossessionBody.canBePossessed;
        }

        if (resolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            return bossData.PossessionBody != null && bossData.PossessionBody.canBePossessed;
        }

        return false;
    }

    private static int CountVisibleSpriteRenderers()
    {
        SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled && renderers[i].sprite != null)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountAnimatorsWithController()
    {
        Animator[] animators = UnityEngine.Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].runtimeAnimatorController != null)
            {
                count++;
            }
        }

        return count;
    }

    private static void ValidatePlayer(GameObject playerObject, List<HWJ_Stage101RuntimeIssue> issues)
    {
        if (playerObject == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_PLAYER_MISSING", "Play Mode에서 HWJ_Player를 찾지 못했습니다."));
            return;
        }

        ValidateVisibleActor(playerObject, "RUNTIME_PLAYER_VISUAL", "플레이어", issues);
        RequireComponent<HWJ_RootObjectDataResolver>(playerObject, "RUNTIME_PLAYER_RESOLVER", issues);
        RequireComponent<HWJ_RuntimeStatusSystem>(playerObject, "RUNTIME_PLAYER_STATUS", issues);
        RequireComponent<HWJ_SoulSystem>(playerObject, "RUNTIME_PLAYER_SOUL", issues);
        RequireComponent<HWJ_PossessionSystem>(playerObject, "RUNTIME_PLAYER_POSSESSION", issues);
        RequireComponent<HWJ_BodyDecaySystem>(playerObject, "RUNTIME_PLAYER_MENTAL", issues);
        RequireComponent<HWJ_PlayerMovementSystem>(playerObject, "RUNTIME_PLAYER_MOVEMENT", issues);
        RequireComponent<Rigidbody2D>(playerObject, "RUNTIME_PLAYER_RIGIDBODY", issues);

        if (playerObject.GetComponent<Collider2D>() == null && playerObject.GetComponentInChildren<Collider2D>() == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_PLAYER_COLLIDER", "플레이어에 Collider2D가 없습니다."));
        }

        if (playerObject.transform.localScale.x < 0.5f || playerObject.transform.localScale.y < 0.5f)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(
                "RUNTIME_PLAYER_SCALE_TOO_SMALL",
                $"플레이어 Scale이 지나치게 작습니다. scale={playerObject.transform.localScale}"));
        }
    }

    private static void ValidateEnemies(HWJ_RootObjectDataResolver[] resolvers, List<HWJ_Stage101RuntimeIssue> issues)
    {
        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Enemy)
            {
                continue;
            }

            ValidateVisibleActor(resolver.gameObject, "RUNTIME_ENEMY_VISUAL", resolver.name, issues);
            RequireComponent<HWJ_RuntimeStatusSystem>(resolver.gameObject, "RUNTIME_ENEMY_STATUS", issues);

            if (resolver.GetComponent<Rigidbody2D>() == null)
            {
                issues.Add(HWJ_Stage101RuntimeIssue.Warning(
                    "RUNTIME_ENEMY_RIGIDBODY",
                    $"{resolver.name}에 Rigidbody2D가 없습니다. 정적 장애물형 적이라면 무시할 수 있습니다."));
            }
        }
    }

    private static void ValidateVisibleActor(GameObject actorObject, string code, string displayName, List<HWJ_Stage101RuntimeIssue> issues)
    {
        SpriteRenderer[] renderers = actorObject.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasSprite = false;
        bool hasVisibleRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].sprite == null)
            {
                continue;
            }

            hasSprite = true;

            if (renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
            {
                hasVisibleRenderer = true;
            }
        }

        if (!hasSprite)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(code + "_MISSING_SPRITE", $"{displayName}에 null이 아닌 SpriteRenderer.sprite가 없습니다."));
        }

        if (!hasVisibleRenderer)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(code + "_NOT_VISIBLE", $"{displayName}의 SpriteRenderer가 Play Mode에서 표시되지 않습니다."));
        }

        Animator[] animators = actorObject.GetComponentsInChildren<Animator>(true);

        if (animators.Length == 0)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning(code + "_ANIMATOR_MISSING", $"{displayName}에 Animator가 없습니다."));
            return;
        }

        bool hasController = false;

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].runtimeAnimatorController != null)
            {
                hasController = true;
                break;
            }
        }

        if (!hasController)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning(code + "_ANIMATOR_CONTROLLER_MISSING", $"{displayName}에 Animator Controller가 연결되어 있지 않습니다."));
        }
    }

    private static void ValidateSceneSystems(List<HWJ_Stage101RuntimeIssue> issues)
    {
        RequireSingleRuntimeObject<HWJ_GameManager>("RUNTIME_GAME_MANAGER", issues);
        RequireSingleRuntimeObject<HWJ_StageEnemyCountSystem>("RUNTIME_ENEMY_COUNT_SYSTEM", issues);
        RequireSingleRuntimeObject<HWJ_StageProgressionSystem>("RUNTIME_STAGE_PROGRESSION", issues);
        RequireSingleRuntimeObject<HWJ_ScenePortalSystem>("RUNTIME_SCENE_PORTAL", issues);
        RequireSingleRuntimeObject<HWJ_DemoHudSystem>("RUNTIME_DEMO_HUD", issues);
        RequireRuntimePresentationSupport("HWJ_PresentationAudioSystem", issues);
        RequireRuntimePresentationSupport("HWJ_PresentationDebugSystem", issues);
        RequireRuntimePresentationSupport("HWJ_PresentationClearOverlaySystem", issues);
    }

    private static void RequireRuntimePresentationSupport(string typeName, List<HWJ_Stage101RuntimeIssue> issues)
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
            {
                return;
            }
        }

        issues.Add(HWJ_Stage101RuntimeIssue.Warning(
            "RUNTIME_PRESENTATION_SUPPORT_MISSING",
            $"{typeName} 컴포넌트가 없습니다. Unity 스크립트 Refresh 후 Stage1-1을 다시 복구하면 자동으로 붙습니다."));
    }

    private static void ValidateMainCamera(GameObject playerObject, List<HWJ_Stage101RuntimeIssue> issues)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_CAMERA_MISSING", "MainCamera 태그가 붙은 카메라를 찾지 못했습니다."));
            return;
        }

        if (!mainCamera.orthographic)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_CAMERA_ORTHOGRAPHIC", "Stage1-1 카메라가 Orthographic이 아닙니다."));
        }

        if (mainCamera.orthographicSize < 4f || mainCamera.orthographicSize > 9f)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning(
                "RUNTIME_CAMERA_SIZE",
                $"카메라 Orthographic Size가 발표용 권장 범위를 벗어났습니다. size={mainCamera.orthographicSize}"));
        }

        if (playerObject == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(mainCamera.transform.position, playerObject.transform.position);

        if (distanceToPlayer > 30f)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning(
                "RUNTIME_CAMERA_FAR_FROM_PLAYER",
                $"카메라가 플레이어에서 멀리 떨어져 있습니다. distance={distanceToPlayer:0.00}"));
        }
    }

    private static void ValidateBackground(List<HWJ_Stage101RuntimeIssue> issues)
    {
        GameObject backgroundRoot = GameObject.Find("BackgroundRoot");

        if (backgroundRoot == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_BACKGROUND_ROOT", "BackgroundRoot를 찾지 못했습니다."));
            return;
        }

        if (backgroundRoot.GetComponentInChildren<Collider2D>(true) != null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_BACKGROUND_COLLIDER", "배경 아래에 Collider2D가 있습니다. 배경은 충돌 지형과 분리되어야 합니다."));
        }

        if (backgroundRoot.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_BACKGROUND_SPRITE", "BackgroundRoot 아래에 SpriteRenderer가 없습니다."));
        }
    }

    private static void ValidateVisibleGimmick(string objectName, bool requireCollider, List<HWJ_Stage101RuntimeIssue> issues)
    {
        GameObject target = GameObject.Find(objectName);

        if (target == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_GIMMICK_MISSING", $"{objectName} 오브젝트를 찾지 못했습니다."));
            return;
        }

        bool hasVisibleSprite = false;
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null
                && renderers[i].sprite != null
                && renderers[i].enabled
                && renderers[i].gameObject.activeInHierarchy)
            {
                hasVisibleSprite = true;
                break;
            }
        }

        if (!hasVisibleSprite)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error("RUNTIME_GIMMICK_VISUAL", $"{objectName}에 Game View에서 보이는 SpriteRenderer가 없습니다."));
        }

        if (requireCollider && target.GetComponentInChildren<Collider2D>(true) == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning("RUNTIME_GIMMICK_COLLIDER", $"{objectName}에 Collider2D가 없습니다."));
        }
    }

    private static void RequireComponent<T>(GameObject owner, string code, List<HWJ_Stage101RuntimeIssue> issues)
        where T : Component
    {
        if (owner.GetComponent<T>() == null)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(code, $"{owner.name}에 {typeof(T).Name} 컴포넌트가 없습니다."));
        }
    }

    private static void RequireSingleRuntimeObject<T>(string code, List<HWJ_Stage101RuntimeIssue> issues)
        where T : UnityEngine.Object
    {
        T[] objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (objects.Length == 0)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Error(code + "_MISSING", $"{typeof(T).Name} 컴포넌트를 찾지 못했습니다."));
        }
        else if (objects.Length > 1)
        {
            issues.Add(HWJ_Stage101RuntimeIssue.Warning(code + "_DUPLICATE", $"{typeof(T).Name} 컴포넌트가 {objects.Length}개 있습니다."));
        }
    }

    private static void WriteStartedReport(string source)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            "# Stage1-1 Play Mode 런타임 검증\n\n"
            + $"- 시작 시간: {DateTime.Now:O}\n"
            + $"- 실행 방식: {source}\n"
            + "- 상태: Play Mode 진입 대기 중\n",
            Encoding.UTF8);
    }

    private static void WriteReport(string source, HWJ_Stage101RuntimeSmokeReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder(2048);
        int errorCount = 0;
        int warningCount = 0;

        for (int i = 0; i < report.Issues.Count; i++)
        {
            if (report.Issues[i].Severity == "Error")
            {
                errorCount++;
            }
            else if (report.Issues[i].Severity == "Warning")
            {
                warningCount++;
            }
        }

        builder.AppendLine("# Stage1-1 Play Mode 런타임 검증");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 씬: {report.SceneName} / {report.ScenePath}");
        builder.AppendLine($"- 플레이어: {report.PlayerPath}");
        builder.AppendLine($"- Enemy Resolver 수: {report.EnemyCount}");
        builder.AppendLine($"- Boss Resolver 수: {report.BossCount}");
        builder.AppendLine($"- 빙의 가능 Resolver 수: {report.PossessableCount}");
        builder.AppendLine($"- 활성 SpriteRenderer 수: {report.VisibleSpriteRendererCount}");
        builder.AppendLine($"- Animator Controller 연결 수: {report.AnimatorWithControllerCount}");
        builder.AppendLine();
        builder.AppendLine("|심각도|검증 코드|문제|");
        builder.AppendLine("|---|---|---|");

        for (int i = 0; i < report.Issues.Count; i++)
        {
            HWJ_Stage101RuntimeIssue issue = report.Issues[i];
            builder.AppendLine($"|{issue.Severity}|{issue.Code}|{issue.Message}|");
        }

        builder.AppendLine();
        builder.AppendLine($"- Error: {errorCount}");
        builder.AppendLine($"- Warning: {warningCount}");
        builder.AppendLine(errorCount == 0 ? "- 결과: 통과" : "- 결과: 실패");
        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);

        if (errorCount > 0)
        {
            Debug.LogError($"[Stage1-1 Runtime Smoke] Failed. errors={errorCount}, warnings={warningCount}, report={ReportFilePath}");
        }
        else
        {
            Debug.Log($"[Stage1-1 Runtime Smoke] Passed. warnings={warningCount}, report={ReportFilePath}");
        }
    }

    private static void WriteExceptionReport(string source, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            "# Stage1-1 Play Mode 런타임 검증\n\n"
            + $"- 실행 시간: {DateTime.Now:O}\n"
            + $"- 실행 방식: {source}\n"
            + "- 결과: 실패\n"
            + $"- Error: {message}\n",
            Encoding.UTF8);
        Debug.LogError($"[Stage1-1 Runtime Smoke] {message}");
    }

    private static void FinishWithException(string message)
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
        SessionState.EraseBool(EnteredPlayModeKey);
        SessionState.EraseString(SourceKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseFloat(ValidateAtKey);
    }

    private static string GetScenePath(GameObject target)
    {
        if (target == null)
        {
            return "Missing";
        }

        Stack<string> path = new Stack<string>();
        Transform current = target.transform;

        while (current != null)
        {
            path.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", path.ToArray());
    }

    private sealed class HWJ_Stage101RuntimeSmokeReport
    {
        public HWJ_Stage101RuntimeSmokeReport(
            string sceneName,
            string scenePath,
            string playerPath,
            int enemyCount,
            int bossCount,
            int possessableCount,
            int visibleSpriteRendererCount,
            int animatorWithControllerCount,
            List<HWJ_Stage101RuntimeIssue> issues)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            PlayerPath = playerPath;
            EnemyCount = enemyCount;
            BossCount = bossCount;
            PossessableCount = possessableCount;
            VisibleSpriteRendererCount = visibleSpriteRendererCount;
            AnimatorWithControllerCount = animatorWithControllerCount;
            Issues = issues;
        }

        public string SceneName { get; }
        public string ScenePath { get; }
        public string PlayerPath { get; }
        public int EnemyCount { get; }
        public int BossCount { get; }
        public int PossessableCount { get; }
        public int VisibleSpriteRendererCount { get; }
        public int AnimatorWithControllerCount { get; }
        public List<HWJ_Stage101RuntimeIssue> Issues { get; }
    }

    private readonly struct HWJ_Stage101RuntimeIssue
    {
        private HWJ_Stage101RuntimeIssue(string severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public static HWJ_Stage101RuntimeIssue Error(string code, string message)
        {
            return new HWJ_Stage101RuntimeIssue("Error", code, message);
        }

        public static HWJ_Stage101RuntimeIssue Warning(string code, string message)
        {
            return new HWJ_Stage101RuntimeIssue("Warning", code, message);
        }
    }
}
