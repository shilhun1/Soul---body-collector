using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class HWJ_SceneTransitionSystem : MonoBehaviour
{
    [Header("핵심 참조")]
    [Tooltip("비워두면 현재 씬의 HWJ_GameManager를 자동으로 찾습니다.")]
    [SerializeField] private HWJ_GameManager gameManager;
    [Tooltip("전환 중 빙의, 공격, 붕괴 같은 코어 루프 요청을 막기 위한 시스템입니다.")]
    [SerializeField] private HWJ_CoreLoopCoordinator coreLoopCoordinator;

    [Space(8f)]
    [Header("전환 설정")]
    [Tooltip("씬을 넘어갈 때 현재 플레이어 오브젝트를 파괴하지 않고 다음 씬까지 유지합니다.")]
    [SerializeField] private bool preservePlayerObjectAcrossScenes = true;
    [Tooltip("씬 로드 전에 잠깐 기다릴 시간입니다. 페이드 연출이 들어오면 이 값을 사용할 수 있습니다.")]
    [SerializeField] private float preLoadDelaySeconds;
    [Tooltip("씬 전환 시작 순간 플레이어 이동/공격/대쉬를 막는 시간입니다.")]
    [SerializeField] private float controlLockSeconds = 2f;
    [Tooltip("목표 스폰 ID가 없거나 찾지 못했을 때 첫 번째 PlayerStart 스폰 포인트를 사용합니다.")]
    [SerializeField] private bool useFallbackPlayerStart = true;

    [Space(8f)]
    [Header("런타임 결과")]
    [SerializeField] private bool isTransitioning;
    [SerializeField] private HWJ_SceneTransitionFailureCode lastFailureCode;
    [SerializeField] private string lastTransitionMessage;
    [SerializeField] private string lastTargetSceneName;
    [SerializeField] private string lastTargetSpawnPointId;

    private Coroutine activeTransitionRoutine;
    private HWJ_RootObjectDataResolver persistentPlayerResolver;

    public bool IsTransitioning => isTransitioning || HWJ_SceneTransitionTransfer.TransitionInProgress;
    public HWJ_SceneTransitionFailureCode LastFailureCode => lastFailureCode;
    public string LastTransitionMessage => lastTransitionMessage;

    private void Awake()
    {
        ResolveReferences();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public static HWJ_SceneTransitionSystem GetOrCreate()
    {
        HWJ_SceneTransitionSystem existing = FindFirstObjectByType<HWJ_SceneTransitionSystem>(
            FindObjectsInactive.Include);

        if (existing != null)
        {
            return existing;
        }

        GameObject owner = HWJ_GameAccess.HasManager
            ? HWJ_GameAccess.Manager.gameObject
            : new GameObject("HWJ_SceneTransitionSystem");

        HWJ_SceneTransitionSystem transitionSystem = owner.GetComponent<HWJ_SceneTransitionSystem>();

        if (transitionSystem == null)
        {
            transitionSystem = owner.AddComponent<HWJ_SceneTransitionSystem>();
        }

        if (!HWJ_GameAccess.HasManager)
        {
            DontDestroyOnLoad(owner);
        }

        return transitionSystem;
    }

    public bool TryStartTransition(
        string targetSceneName,
        string targetSpawnPointId,
        GameObject playerObject,
        Object requestSource = null)
    {
        return TryStartTransitionResult(targetSceneName, targetSpawnPointId, playerObject, requestSource).Succeeded;
    }

    public HWJ_SceneTransitionResult TryStartTransitionResult(
        string targetSceneName,
        string targetSpawnPointId,
        GameObject playerObject,
        Object requestSource = null)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            return StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.MissingTargetSceneName,
                targetSceneName,
                targetSpawnPointId,
                "씬 전환 실패: 이동할 씬 이름이 비어 있습니다."));
        }

        if (IsTransitioning)
        {
            return StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.TransitionAlreadyInProgress,
                targetSceneName,
                targetSpawnPointId,
                "씬 전환 실패: 이미 다른 씬 전환이 진행 중입니다."));
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            return StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.TargetSceneNotInBuildSettings,
                targetSceneName,
                targetSpawnPointId,
                $"씬 전환 실패: `{targetSceneName}` 씬이 Build Settings에 등록되어 있지 않습니다."));
        }

        HWJ_RootObjectDataResolver playerResolver = ResolvePlayerResolver(playerObject);

        if (playerResolver == null)
        {
            return StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.MissingPlayer,
                targetSceneName,
                targetSpawnPointId,
                "씬 전환 실패: 이동시킬 플레이어를 찾지 못했습니다."));
        }

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
        }

        activeTransitionRoutine = StartCoroutine(TransitionRoutine(
            targetSceneName,
            targetSpawnPointId,
            playerResolver,
            requestSource));

        return StoreResult(HWJ_SceneTransitionResult.Success(
            targetSceneName,
            targetSpawnPointId,
            $"씬 전환 시작: {targetSceneName} / SpawnId={targetSpawnPointId}"));
    }

    private IEnumerator TransitionRoutine(
        string targetSceneName,
        string targetSpawnPointId,
        HWJ_RootObjectDataResolver playerResolver,
        Object requestSource)
    {
        isTransitioning = true;
        persistentPlayerResolver = playerResolver;
        lastTargetSceneName = targetSceneName;
        lastTargetSpawnPointId = targetSpawnPointId;

        PreparePlayerForTransition(playerResolver);
        SetCoreLoopTransitionLock(true);
        HWJ_SceneTransitionTransfer.BeginTransition(targetSceneName, targetSpawnPointId);

        if (preLoadDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(preLoadDelaySeconds);
        }

        AsyncOperation loadOperation = null;

        try
        {
            loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            isTransitioning = false;
            SetCoreLoopTransitionLock(false);
            HWJ_SceneTransitionTransfer.ClearPendingArrival();
            StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.SceneLoadFailed,
                targetSceneName,
                targetSpawnPointId,
                $"씬 전환 실패: {exception.Message}"));
            yield break;
        }

        if (loadOperation == null)
        {
            isTransitioning = false;
            SetCoreLoopTransitionLock(false);
            HWJ_SceneTransitionTransfer.ClearPendingArrival();
            StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.SceneLoadFailed,
                targetSceneName,
                targetSpawnPointId,
                "씬 전환 실패: SceneManager.LoadSceneAsync가 null을 반환했습니다."));
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!HWJ_SceneTransitionTransfer.HasPendingArrival)
        {
            return;
        }

        ResolveReferences();
        HWJ_SceneTransitionTransfer.MarkSceneLoaded();

        HWJ_RootObjectDataResolver playerResolver = ResolvePlayerResolver(
            persistentPlayerResolver != null ? persistentPlayerResolver.gameObject : null);

        if (playerResolver == null)
        {
            isTransitioning = false;
            SetCoreLoopTransitionLock(false);
            StoreResult(HWJ_SceneTransitionResult.Fail(
                HWJ_SceneTransitionFailureCode.MissingPlayer,
                scene.name,
                HWJ_SceneTransitionTransfer.TargetSpawnPointId,
                "씬 도착 실패: 플레이어를 찾지 못했습니다."));
            return;
        }

        bool placed = TryPlacePlayerAtArrivalSpawn(playerResolver, HWJ_SceneTransitionTransfer.TargetSpawnPointId);

        if (gameManager != null)
        {
            gameManager.RegisterPlayer(playerResolver);
            gameManager.ResolveSceneReferences();
        }

        isTransitioning = false;
        SetCoreLoopTransitionLock(false);

        if (placed)
        {
            StoreResult(HWJ_SceneTransitionResult.Success(
                scene.name,
                HWJ_SceneTransitionTransfer.TargetSpawnPointId,
                $"씬 도착 완료: {scene.name} / SpawnId={HWJ_SceneTransitionTransfer.TargetSpawnPointId}"));
            HWJ_SceneTransitionTransfer.ClearPendingArrival();
            return;
        }

        StoreResult(HWJ_SceneTransitionResult.Fail(
            HWJ_SceneTransitionFailureCode.TargetSpawnPointNotFound,
            scene.name,
            HWJ_SceneTransitionTransfer.TargetSpawnPointId,
            $"씬 도착 경고: `{HWJ_SceneTransitionTransfer.TargetSpawnPointId}` 스폰 포인트를 찾지 못했습니다."));
        HWJ_SceneTransitionTransfer.ClearPendingArrival();
    }

    private void PreparePlayerForTransition(HWJ_RootObjectDataResolver playerResolver)
    {
        if (gameManager != null)
        {
            gameManager.RegisterPlayer(playerResolver);
            gameManager.SavePlayerRuntimeSnapshot();
        }

        if (playerResolver.TryGetComponent(out HWJ_RuntimeStatusSystem runtimeStatus))
        {
            runtimeStatus.LockControl(controlLockSeconds);
        }

        if (preservePlayerObjectAcrossScenes)
        {
            DontDestroyOnLoad(playerResolver.gameObject);
        }
    }

    private bool TryPlacePlayerAtArrivalSpawn(
        HWJ_RootObjectDataResolver playerResolver,
        string targetSpawnPointId)
    {
        HWJ_SpawnPoint spawnPoint = FindArrivalSpawnPoint(targetSpawnPointId);

        if (spawnPoint == null)
        {
            return false;
        }

        Transform playerTransform = playerResolver.transform;
        playerTransform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);

        if (playerTransform.TryGetComponent(out Rigidbody2D rigidbody))
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
        }

        return true;
    }

    private HWJ_SpawnPoint FindArrivalSpawnPoint(string targetSpawnPointId)
    {
        HWJ_SpawnPoint[] spawnPoints = FindObjectsByType<HWJ_SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HWJ_SpawnPoint fallbackPoint = null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            HWJ_SpawnPoint point = spawnPoints[i];

            if (point == null || point.SpawnPointType != HWJ_SpawnPointType.PlayerStart)
            {
                continue;
            }

            if (fallbackPoint == null)
            {
                fallbackPoint = point;
            }

            if (!string.IsNullOrWhiteSpace(targetSpawnPointId) && point.PointId == targetSpawnPointId)
            {
                return point;
            }
        }

        return useFallbackPlayerStart ? fallbackPoint : null;
    }

    private HWJ_RootObjectDataResolver ResolvePlayerResolver(GameObject playerObject)
    {
        if (persistentPlayerResolver != null)
        {
            return persistentPlayerResolver;
        }

        if (playerObject != null)
        {
            HWJ_RootObjectDataResolver resolver = playerObject.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (resolver == null)
            {
                resolver = playerObject.GetComponentInChildren<HWJ_RootObjectDataResolver>();
            }

            if (resolver != null && resolver.ObjectType == HWJ_ObjectType.Player)
            {
                return resolver;
            }
        }

        if (gameManager != null && gameManager.PlayerResolver != null)
        {
            return gameManager.PlayerResolver;
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

    private void SetCoreLoopTransitionLock(bool locked)
    {
        ResolveReferences();
        coreLoopCoordinator?.SetSceneTransitionInProgress(
            locked,
            locked ? "씬 전환 시작" : "씬 전환 종료");
    }

    private void ResolveReferences()
    {
        if (gameManager == null && HWJ_GameAccess.HasManager)
        {
            gameManager = HWJ_GameAccess.Manager;
        }

        if (coreLoopCoordinator == null && gameManager != null && gameManager.PlayerResolver != null)
        {
            coreLoopCoordinator = gameManager.PlayerResolver.GetComponent<HWJ_CoreLoopCoordinator>();
        }

        if (coreLoopCoordinator == null)
        {
            coreLoopCoordinator = FindFirstObjectByType<HWJ_CoreLoopCoordinator>(
                FindObjectsInactive.Include);
        }
    }

    private HWJ_SceneTransitionResult StoreResult(HWJ_SceneTransitionResult result)
    {
        lastFailureCode = result.FailureCode;
        lastTransitionMessage = result.Message;
        lastTargetSceneName = result.TargetSceneName;
        lastTargetSpawnPointId = result.TargetSpawnPointId;
        return result;
    }
}
