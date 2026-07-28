using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 중간발표 중 소프트락이나 실수로 시연이 막혔을 때 사용하는 숨겨진 복구키 시스템입니다.
/// 기본 화면에는 보이지 않고 F1을 눌렀을 때만 도움말을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PresentationDebugSystem : MonoBehaviour
{
    [Header("발표 디버그 사용")]
    [Tooltip("켜져 있으면 F1/F5~F9 발표 복구키를 사용할 수 있습니다.")]
    [SerializeField] private bool enablePresentationDebug = true;
    [Tooltip("에디터 또는 개발 빌드가 아니어도 복구키를 허용할지 결정합니다.")]
    [SerializeField] private bool allowInReleaseBuild;

    [Header("복구키")]
    [SerializeField] private KeyCode toggleHelpKey = KeyCode.F1;
    [SerializeField] private KeyCode restartAtCheckpointKey = KeyCode.F5;
    [SerializeField] private KeyCode restoreResourcesKey = KeyCode.F6;
    [SerializeField] private KeyCode defeatEnemiesKey = KeyCode.F7;
    [SerializeField] private KeyCode moveToNextCheckpointKey = KeyCode.F8;
    [SerializeField] private KeyCode forcePortalKey = KeyCode.F9;

    [Header("복구 옵션")]
    [Tooltip("F7을 누르면 현재 씬의 활성 적을 모두 처치합니다. 발표용 강제 진행 기능입니다.")]
    [SerializeField] private bool defeatAllActiveEnemies = true;
    [Tooltip("F5/F8 이동 후 체력과 정신력을 함께 회복합니다.")]
    [SerializeField] private bool restoreResourcesAfterTeleport = true;
    [Tooltip("F9를 누르면 남은 적 수와 관계없이 포탈 조건을 완료 처리합니다.")]
    [SerializeField] private bool allowForcePortalObjective = true;

    [Header("확인용 상태")]
    [SerializeField] private bool showHelp;
    [SerializeField] private string lastDebugResult;

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;

    private bool IsAllowed => enablePresentationDebug
        && (Application.isEditor || Debug.isDebugBuild || allowInReleaseBuild);

    private void Update()
    {
        if (!IsAllowed)
        {
            return;
        }

        if (Input.GetKeyDown(toggleHelpKey))
        {
            showHelp = !showHelp;
        }

        if (Input.GetKeyDown(restartAtCheckpointKey))
        {
            TeleportToCheckpoint(false);
        }

        if (Input.GetKeyDown(restoreResourcesKey))
        {
            RestorePlayerResources("F6 체력/정신력 회복");
        }

        if (Input.GetKeyDown(defeatEnemiesKey))
        {
            DefeatStageEnemies();
        }

        if (Input.GetKeyDown(moveToNextCheckpointKey))
        {
            TeleportToCheckpoint(true);
        }

        if (Input.GetKeyDown(forcePortalKey))
        {
            ForcePortalObjective();
        }
    }

    private void OnGUI()
    {
        if (!IsAllowed || !showHelp)
        {
            return;
        }

        EnsureGuiStyles();
        GUILayout.BeginArea(new Rect(18f, 78f, 420f, 220f), panelStyle);
        GUILayout.Label("HWJ 발표 복구키", labelStyle);
        GUILayout.Label("F1 도움말 표시/숨김", labelStyle);
        GUILayout.Label("F5 현재 체크포인트로 이동", labelStyle);
        GUILayout.Label("F6 체력/영혼 정신력/빙의 정신력 회복", labelStyle);
        GUILayout.Label("F7 활성 적 강제 처치", labelStyle);
        GUILayout.Label("F8 다음 체크포인트로 이동", labelStyle);
        GUILayout.Label("F9 포탈 조건 강제 완료", labelStyle);
        GUILayout.Space(8f);
        GUILayout.Label(string.IsNullOrWhiteSpace(lastDebugResult) ? "최근 실행 없음" : lastDebugResult, labelStyle);
        GUILayout.EndArea();
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = Texture2D.grayTexture;
            panelStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 15;
            labelStyle.normal.textColor = Color.white;
            labelStyle.wordWrap = true;
        }
    }

    private void TeleportToCheckpoint(bool nextCheckpoint)
    {
        HWJ_RootObjectDataResolver player = ResolvePlayer();

        if (player == null)
        {
            lastDebugResult = "복구 실패: 플레이어를 찾지 못했습니다.";
            return;
        }

        Transform checkpoint = nextCheckpoint
            ? FindNextCheckpoint(player.transform.position.x)
            : FindCurrentCheckpoint(player.transform.position.x);

        if (checkpoint == null)
        {
            checkpoint = FindPlayerStart();
        }

        if (checkpoint == null)
        {
            lastDebugResult = "복구 실패: 체크포인트/시작 지점을 찾지 못했습니다.";
            return;
        }

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        player.transform.position = checkpoint.position + Vector3.up * 0.15f;

        if (restoreResourcesAfterTeleport)
        {
            RestorePlayerResources("체크포인트 이동 후 회복");
        }

        lastDebugResult = nextCheckpoint
            ? $"다음 체크포인트 이동: {checkpoint.name}"
            : $"현재 체크포인트 이동: {checkpoint.name}";
    }

    private void RestorePlayerResources(string reason)
    {
        HWJ_RootObjectDataResolver player = ResolvePlayer();

        if (player == null)
        {
            lastDebugResult = "회복 실패: 플레이어를 찾지 못했습니다.";
            return;
        }

        HWJ_RuntimeStatusSystem status = player.GetComponent<HWJ_RuntimeStatusSystem>();

        if (status != null)
        {
            status.RestoreHpSnapshot(status.MaxHp, status.SoulMaxHp, status.MaxHp);
            status.SetState(HWJ_RuntimeState.Idle);
        }

        HWJ_BodyDecaySystem mentalSystem = player.GetComponent<HWJ_BodyDecaySystem>();

        if (mentalSystem != null)
        {
            mentalSystem.ResetDecay();
        }

        lastDebugResult = reason;
    }

    private void DefeatStageEnemies()
    {
        if (!defeatAllActiveEnemies)
        {
            lastDebugResult = "적 처치 스킵: defeatAllActiveEnemies가 꺼져 있습니다.";
            return;
        }

        int defeatedCount = 0;
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Enemy)
            {
                continue;
            }

            HWJ_RuntimeStatusSystem status = resolver.GetComponent<HWJ_RuntimeStatusSystem>();

            if (status == null || status.IsDead)
            {
                continue;
            }

            status.ApplyDamage(Mathf.Max(99999f, status.MaxHp + 9999f), this, null);
            defeatedCount++;
        }

        HWJ_StageEnemyCountSystem enemyCountSystem = FindFirstObjectByType<HWJ_StageEnemyCountSystem>(FindObjectsInactive.Include);
        enemyCountSystem?.ForceScan("presentation_debug_defeat_enemies");
        lastDebugResult = $"활성 적 강제 처치: {defeatedCount}마리";
    }

    private void ForcePortalObjective()
    {
        if (!allowForcePortalObjective)
        {
            lastDebugResult = "포탈 강제 활성화 실패: 옵션이 꺼져 있습니다.";
            return;
        }

        HWJ_StageEnemyCountSystem enemyCountSystem = FindFirstObjectByType<HWJ_StageEnemyCountSystem>(FindObjectsInactive.Include);
        enemyCountSystem?.ForceScan("presentation_debug_force_portal_before");

        HWJ_StageProgressionSystem progression = FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Include);

        if (progression == null)
        {
            lastDebugResult = "포탈 강제 활성화 실패: StageProgressionSystem 없음";
            return;
        }

        if (progression.CurrentState == HWJ_StageFlowState.Entering)
        {
            progression.TryEnterExploring("Presentation debug force portal.");
        }

        HWJ_StageFlowTransitionResult result = progression.ObjectiveComplete
            ? progression.TryClearStage("Presentation debug clear stage.")
            : progression.TryMarkObjectiveComplete("Presentation debug force portal objective.");

        lastDebugResult = result.Succeeded
            ? "포탈 조건 강제 완료"
            : $"포탈 조건 강제 실패: {result.Message}";
    }

    private static HWJ_RootObjectDataResolver ResolvePlayer()
    {
        if (HWJ_GameAccess.PlayerResolver != null)
        {
            return HWJ_GameAccess.PlayerResolver;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i];
            }
        }

        return null;
    }

    private static Transform FindCurrentCheckpoint(float playerX)
    {
        List<Transform> checkpoints = FindCheckpointTransforms();
        Transform best = null;
        float bestX = float.NegativeInfinity;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            Transform checkpoint = checkpoints[i];

            if (checkpoint.position.x <= playerX + 0.5f && checkpoint.position.x > bestX)
            {
                best = checkpoint;
                bestX = checkpoint.position.x;
            }
        }

        return best;
    }

    private static Transform FindNextCheckpoint(float playerX)
    {
        List<Transform> checkpoints = FindCheckpointTransforms();
        Transform best = null;
        float bestX = float.PositiveInfinity;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            Transform checkpoint = checkpoints[i];

            if (checkpoint.position.x > playerX + 1f && checkpoint.position.x < bestX)
            {
                best = checkpoint;
                bestX = checkpoint.position.x;
            }
        }

        return best;
    }

    private static List<Transform> FindCheckpointTransforms()
    {
        List<Transform> checkpoints = new List<Transform>();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            CollectNamedTransforms(roots[i].transform, "PF_Checkpoint", checkpoints);
        }

        checkpoints.Sort((left, right) => left.position.x.CompareTo(right.position.x));
        return checkpoints;
    }

    private static Transform FindPlayerStart()
    {
        HWJ_SpawnPoint[] spawnPoints = FindObjectsByType<HWJ_SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && spawnPoints[i].SpawnPointType == HWJ_SpawnPointType.PlayerStart)
            {
                return spawnPoints[i].transform;
            }
        }

        GameObject namedStart = GameObject.Find("HWJ_PlayerStart_Stage1_01");
        return namedStart != null ? namedStart.transform : null;
    }

    private static void CollectNamedTransforms(Transform root, string nameContains, List<Transform> results)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.Contains(nameContains) && root.GetComponentInChildren<SpriteRenderer>(true) != null)
        {
            results.Add(root);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectNamedTransforms(root.GetChild(i), nameContains, results);
        }
    }
}
