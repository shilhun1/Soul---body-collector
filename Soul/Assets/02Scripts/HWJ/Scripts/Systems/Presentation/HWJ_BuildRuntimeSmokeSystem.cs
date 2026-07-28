using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Windows 발표 빌드가 실제 Stage1-1 구성 요소를 로드했는지 파일로 남기는 런타임 검증 시스템입니다.
/// 게임 진행에는 개입하지 않고, 개발 빌드와 에디터에서만 한 번 실행됩니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_BuildRuntimeSmokeSystem : MonoBehaviour
{
    private const string PrimaryReportPath = @"C:\Docs\Generated\HWJ_BuildRuntimeSmokeReport.md";

    [Header("빌드 런타임 검증")]
    [Tooltip("켜져 있으면 개발 빌드에서 Stage1-1 런타임 구성 검증 리포트를 생성합니다.")]
    [SerializeField] private bool runSmokeCheck = true;
    [Tooltip("개발 빌드가 아닌 일반 빌드에서도 검증 리포트를 만들지 결정합니다.")]
    [SerializeField] private bool allowInReleaseBuild;
    [Tooltip("씬 로드 직후 초기화 시간을 기다린 뒤 검증하는 지연 시간입니다.")]
    [SerializeField] private float validationDelaySeconds = 2.5f;
    [Tooltip("검증이 끝난 뒤 게임을 자동 종료할지 결정합니다. 발표 빌드에서는 꺼둡니다.")]
    [SerializeField] private bool quitAfterReport;

    [Header("필수 오브젝트 이름")]
    [SerializeField] private string playerObjectName = "HWJ_Player";
    [SerializeField] private string portalObjectName = "HWJ_Portal_Stage1_01_ToOutpost";
    [SerializeField] private string bowSwitchObjectName = "PF_BowSwitch_Stage1_01_LongShot";
    [SerializeField] private string bowDoorObjectName = "PF_GimmickDoor_Stage1_01_BowSwitchDoor";
    [SerializeField] private string soulWallObjectName = "PF_SoulPassWall_Stage1_01_ScoutGate";
    [SerializeField] private string sandstormObjectName = "PF_SandstormLane_Stage1_01_SoulPreview";
    [SerializeField] private string spikeTrapObjectName = "PF_SpikeTrap_Stage1_01_DashGap";
    [SerializeField] private string checkpointObjectName = "PF_Checkpoint_Stage1_01_Entrance";

    [Header("검증 기준")]
    [SerializeField] private int minimumEnemyResolverCount = 8;
    [SerializeField] private int minimumVisibleSpriteRendererCount = 60;
    [SerializeField] private int minimumAnimatorControllerCount = 10;

    [Header("확인용 상태")]
    [SerializeField] private bool hasRun;
    [SerializeField] private string lastReportPath;
    [SerializeField] private string lastResult;

    private bool IsAllowed => runSmokeCheck && (Application.isEditor || Debug.isDebugBuild || allowInReleaseBuild);

    private void Start()
    {
        if (!IsAllowed || hasRun)
        {
            Debug.Log($"[HWJ Build Runtime Smoke] Skipped. allowed={IsAllowed}, hasRun={hasRun}, timeScale={Time.timeScale:0.###}");
            return;
        }

        Debug.Log($"[HWJ Build Runtime Smoke] Started. delay={validationDelaySeconds:0.###}, timeScale={Time.timeScale:0.###}");
        StartCoroutine(RunSmokeAfterDelay());
    }

    private IEnumerator RunSmokeAfterDelay()
    {
        hasRun = true;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, validationDelaySeconds));

        HWJ_BuildRuntimeSmokeReport report = ValidateScene();
        WriteReport(report);

        if (quitAfterReport)
        {
            Application.Quit();
        }
    }

    private HWJ_BuildRuntimeSmokeReport ValidateScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        List<HWJ_BuildRuntimeSmokeIssue> issues = new List<HWJ_BuildRuntimeSmokeIssue>();
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        GameObject playerObject = GameObject.Find(playerObjectName);
        int enemyResolverCount = CountResolversByType(resolvers, HWJ_ObjectType.Enemy);
        int visibleSpriteRendererCount = CountVisibleSpriteRenderers();
        int animatorControllerCount = CountAnimatorControllers();

        ValidatePlayer(playerObject, issues);
        ValidateCount("BUILD_ENEMY_COUNT", enemyResolverCount, minimumEnemyResolverCount, "Enemy Resolver", issues);
        ValidateCount("BUILD_VISIBLE_SPRITE_COUNT", visibleSpriteRendererCount, minimumVisibleSpriteRendererCount, "활성 SpriteRenderer", issues);
        ValidateCount("BUILD_ANIMATOR_COUNT", animatorControllerCount, minimumAnimatorControllerCount, "Animator Controller", issues);
        ValidateVisibleObject(portalObjectName, issues);
        ValidateVisibleObject(bowSwitchObjectName, issues);
        ValidateVisibleObject(bowDoorObjectName, issues);
        ValidateVisibleObject(soulWallObjectName, issues);
        ValidateVisibleObject(sandstormObjectName, issues);
        ValidateVisibleObject(spikeTrapObjectName, issues);
        ValidateVisibleObject(checkpointObjectName, issues);
        ValidateSceneSystem("HWJ_DemoHudSystem", true, issues);
        ValidateSceneSystem("HWJ_PresentationAudioSystem", false, issues);
        ValidateSceneSystem("HWJ_PresentationDebugSystem", false, issues);
        ValidateSceneSystem("HWJ_PresentationClearOverlaySystem", false, issues);
        ValidateSceneSystem("HWJ_ManualDemoRunRecorderSystem", true, issues);
        ValidateSceneSystem("HWJ_StageEnemyCountSystem", true, issues);
        ValidateSceneSystem("HWJ_StageProgressionSystem", true, issues);

        return new HWJ_BuildRuntimeSmokeReport(
            DateTime.Now,
            scene.name,
            scene.path,
            playerObject != null ? GetScenePath(playerObject) : "Missing",
            enemyResolverCount,
            visibleSpriteRendererCount,
            animatorControllerCount,
            issues);
    }

    private void ValidatePlayer(GameObject playerObject, List<HWJ_BuildRuntimeSmokeIssue> issues)
    {
        if (playerObject == null)
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error("BUILD_PLAYER_MISSING", "HWJ_Player를 찾지 못했습니다."));
            return;
        }

        if (!HasVisibleSpriteRenderer(playerObject))
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error("BUILD_PLAYER_SPRITE", "플레이어에 보이는 SpriteRenderer가 없습니다."));
        }

        RequireComponent<HWJ_RootObjectDataResolver>(playerObject, "BUILD_PLAYER_RESOLVER", issues);
        RequireComponent<HWJ_RuntimeStatusSystem>(playerObject, "BUILD_PLAYER_STATUS", issues);
        RequireComponent<HWJ_PlayerMovementSystem>(playerObject, "BUILD_PLAYER_MOVEMENT", issues);
        RequireComponent<HWJ_PossessionSystem>(playerObject, "BUILD_PLAYER_POSSESSION", issues);
        RequireComponent<HWJ_BodyDecaySystem>(playerObject, "BUILD_PLAYER_MENTAL", issues);
        RequireComponent<Rigidbody2D>(playerObject, "BUILD_PLAYER_RIGIDBODY", issues);
    }

    private void ValidateVisibleObject(string objectName, List<HWJ_BuildRuntimeSmokeIssue> issues)
    {
        GameObject targetObject = GameObject.Find(objectName);

        if (targetObject == null)
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error("BUILD_OBJECT_MISSING", $"{objectName} 오브젝트를 찾지 못했습니다."));
            return;
        }

        if (!HasVisibleSpriteRenderer(targetObject))
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error("BUILD_OBJECT_SPRITE", $"{objectName}에 보이는 SpriteRenderer가 없습니다."));
        }
    }

    private void ValidateSceneSystem(string typeName, bool errorIfMissing, List<HWJ_BuildRuntimeSmokeIssue> issues)
    {
        if (SceneContainsMonoBehaviourType(typeName))
        {
            return;
        }

        if (errorIfMissing)
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error("BUILD_SYSTEM_MISSING", $"{typeName} 시스템을 찾지 못했습니다."));
        }
        else
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Warning("BUILD_PRESENTATION_SYSTEM_MISSING", $"{typeName} 발표 보조 시스템을 찾지 못했습니다."));
        }
    }

    private void ValidateCount(
        string code,
        int currentCount,
        int minimumCount,
        string displayName,
        List<HWJ_BuildRuntimeSmokeIssue> issues)
    {
        if (currentCount >= minimumCount)
        {
            return;
        }

        issues.Add(HWJ_BuildRuntimeSmokeIssue.Error(
            code,
            $"{displayName} 수가 부족합니다. 현재 {currentCount}, 요구 {minimumCount} 이상"));
    }

    private void RequireComponent<T>(
        GameObject targetObject,
        string code,
        List<HWJ_BuildRuntimeSmokeIssue> issues)
        where T : Component
    {
        if (targetObject.GetComponent<T>() == null)
        {
            issues.Add(HWJ_BuildRuntimeSmokeIssue.Error(code, $"{targetObject.name}에 {typeof(T).Name} 컴포넌트가 없습니다."));
        }
    }

    private int CountResolversByType(HWJ_RootObjectDataResolver[] resolvers, HWJ_ObjectType objectType)
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

    private int CountVisibleSpriteRenderers()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
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

    private int CountAnimatorControllers()
    {
        Animator[] animators = FindObjectsByType<Animator>(
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

    private bool HasVisibleSpriteRenderer(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        SpriteRenderer[] renderers = targetObject.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null
                && renderers[i].enabled
                && renderers[i].gameObject.activeInHierarchy
                && renderers[i].sprite != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool SceneContainsMonoBehaviourType(string typeName)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
            {
                return true;
            }
        }

        return false;
    }

    private string GetScenePath(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return "Missing";
        }

        Stack<string> names = new Stack<string>();
        Transform current = targetObject.transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private void WriteReport(HWJ_BuildRuntimeSmokeReport report)
    {
        string reportPath = PrimaryReportPath;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, BuildReportText(report), Encoding.UTF8);
        }
        catch (Exception)
        {
            reportPath = Path.Combine(Application.persistentDataPath, "HWJ_BuildRuntimeSmokeReport.md");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, BuildReportText(report), Encoding.UTF8);
        }

        lastReportPath = reportPath;
        lastResult = report.ErrorCount > 0 ? "실패" : "통과";

        if (report.ErrorCount > 0)
        {
            Debug.LogError($"[HWJ Build Runtime Smoke] Failed. errors={report.ErrorCount}, warnings={report.WarningCount}, report={reportPath}");
        }
        else
        {
            Debug.Log($"[HWJ Build Runtime Smoke] Passed. warnings={report.WarningCount}, report={reportPath}");
        }
    }

    private string BuildReportText(HWJ_BuildRuntimeSmokeReport report)
    {
        StringBuilder builder = new StringBuilder(4096);
        builder.AppendLine("# HWJ 빌드 런타임 스모크 검증");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {report.CreatedAt:O}");
        builder.AppendLine($"- Application.version: {Application.version}");
        builder.AppendLine($"- Development Build: {Debug.isDebugBuild}");
        builder.AppendLine($"- 씬: {report.SceneName} / {report.ScenePath}");
        builder.AppendLine($"- 플레이어: {report.PlayerPath}");
        builder.AppendLine($"- Enemy Resolver 수: {report.EnemyResolverCount}");
        builder.AppendLine($"- 활성 SpriteRenderer 수: {report.VisibleSpriteRendererCount}");
        builder.AppendLine($"- Animator Controller 수: {report.AnimatorControllerCount}");
        builder.AppendLine();
        builder.AppendLine("|심각도|코드|내용|");
        builder.AppendLine("|---|---|---|");

        for (int i = 0; i < report.Issues.Count; i++)
        {
            HWJ_BuildRuntimeSmokeIssue issue = report.Issues[i];
            builder.AppendLine($"|{issue.Severity}|{issue.Code}|{issue.Message}|");
        }

        builder.AppendLine();
        builder.AppendLine($"- Error: {report.ErrorCount}");
        builder.AppendLine($"- Warning: {report.WarningCount}");
        builder.AppendLine(report.ErrorCount > 0 ? "- 결과: 실패" : "- 결과: 통과");
        return builder.ToString();
    }

    private sealed class HWJ_BuildRuntimeSmokeReport
    {
        public HWJ_BuildRuntimeSmokeReport(
            DateTime createdAt,
            string sceneName,
            string scenePath,
            string playerPath,
            int enemyResolverCount,
            int visibleSpriteRendererCount,
            int animatorControllerCount,
            List<HWJ_BuildRuntimeSmokeIssue> issues)
        {
            CreatedAt = createdAt;
            SceneName = sceneName;
            ScenePath = scenePath;
            PlayerPath = playerPath;
            EnemyResolverCount = enemyResolverCount;
            VisibleSpriteRendererCount = visibleSpriteRendererCount;
            AnimatorControllerCount = animatorControllerCount;
            Issues = issues ?? new List<HWJ_BuildRuntimeSmokeIssue>();
        }

        public DateTime CreatedAt { get; }
        public string SceneName { get; }
        public string ScenePath { get; }
        public string PlayerPath { get; }
        public int EnemyResolverCount { get; }
        public int VisibleSpriteRendererCount { get; }
        public int AnimatorControllerCount { get; }
        public List<HWJ_BuildRuntimeSmokeIssue> Issues { get; }

        public int ErrorCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == "Error")
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == "Warning")
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }

    private sealed class HWJ_BuildRuntimeSmokeIssue
    {
        private HWJ_BuildRuntimeSmokeIssue(string severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public string Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public static HWJ_BuildRuntimeSmokeIssue Error(string code, string message)
        {
            return new HWJ_BuildRuntimeSmokeIssue("Error", code, message);
        }

        public static HWJ_BuildRuntimeSmokeIssue Warning(string code, string message)
        {
            return new HWJ_BuildRuntimeSmokeIssue("Warning", code, message);
        }
    }
}
