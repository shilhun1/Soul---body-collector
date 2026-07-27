using UnityEngine;
using UnityEngine.SceneManagement;

public class HWJ_PlayerStartSystem : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_SpawnPoint playerStartPoint;

    [Space(8f)]
    [Header("시작 위치")]
    [SerializeField] private string playerStartPointId;
    [SerializeField] private bool useSceneTransitionTargetSpawnPoint = true;

    [Space(8f)]
    [Header("자동 실행")]
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private bool registerToGameManager = true;
    [SerializeField] private bool preferPersistentPlayerDuringSceneTransition = true;
    [SerializeField] private bool destroySceneFallbackPlayersDuringSceneTransition = true;

    [Space(8f)]
    [Header("런타임 결과")]
    [SerializeField] private string lastPlacementResult;

    public string PlayerStartPointId => playerStartPointId;
    public string LastPlacementResult => lastPlacementResult;

    private void Start()
    {
        if (placeOnStart)
        {
            PlacePlayerAtStart();
        }
    }

    public void PlacePlayerAtStart()
    {
        ResolvePlayer();
        ResolveStartPoint();

        if (playerResolver == null || playerStartPoint == null)
        {
            lastPlacementResult = "플레이어 시작 위치 적용 실패: 플레이어 또는 시작 스폰 포인트가 없습니다.";
            return;
        }

        Transform playerTransform = playerResolver.transform;
        playerTransform.SetPositionAndRotation(playerStartPoint.Position, playerStartPoint.Rotation);

        if (playerTransform.TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (registerToGameManager && HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.RegisterPlayer(playerResolver);
        }

        lastPlacementResult = $"플레이어 시작 위치 적용 완료: {playerStartPoint.PointId}";

        if (useSceneTransitionTargetSpawnPoint && HWJ_SceneTransitionTransfer.HasPendingArrival)
        {
            HWJ_SceneTransitionTransfer.ClearPendingArrival();
        }
    }

    private void ResolvePlayer()
    {
        if (playerResolver != null)
        {
            return;
        }

        if (preferPersistentPlayerDuringSceneTransition
            && HWJ_SceneTransitionTransfer.HasPendingArrival
            && TryResolvePersistentTransitionPlayer(out HWJ_RootObjectDataResolver persistentPlayerResolver))
        {
            playerResolver = persistentPlayerResolver;
            DestroySceneFallbackPlayers(playerResolver);
            return;
        }

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            playerResolver = HWJ_GameAccess.Manager.PlayerResolver;
            return;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                playerResolver = resolvers[i];
                return;
            }
        }
    }

    private static bool TryResolvePersistentTransitionPlayer(out HWJ_RootObjectDataResolver persistentPlayerResolver)
    {
        persistentPlayerResolver = null;
        Scene activeScene = SceneManager.GetActiveScene();
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver.ObjectType != HWJ_ObjectType.Player
                || resolver.gameObject.scene == activeScene)
            {
                continue;
            }

            persistentPlayerResolver = resolver;
            return true;
        }

        return false;
    }

    private void DestroySceneFallbackPlayers(HWJ_RootObjectDataResolver keepResolver)
    {
        if (!destroySceneFallbackPlayersDuringSceneTransition)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver == keepResolver
                || resolver.ObjectType != HWJ_ObjectType.Player
                || resolver.gameObject.scene != activeScene)
            {
                continue;
            }

            Destroy(resolver.gameObject);
        }
    }

    private void ResolveStartPoint()
    {
        if (playerStartPoint != null)
        {
            return;
        }

        HWJ_SpawnPoint[] points = FindObjectsByType<HWJ_SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HWJ_SpawnPoint fallbackPoint = null;
        string requestedPointId = ResolveRequestedStartPointId();

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null || points[i].SpawnPointType != HWJ_SpawnPointType.PlayerStart)
            {
                continue;
            }

            if (fallbackPoint == null)
            {
                fallbackPoint = points[i];
            }

            if (!string.IsNullOrWhiteSpace(requestedPointId) && points[i].PointId == requestedPointId)
            {
                playerStartPoint = points[i];
                return;
            }
        }

        playerStartPoint = fallbackPoint;
    }

    private string ResolveRequestedStartPointId()
    {
        if (useSceneTransitionTargetSpawnPoint
            && HWJ_SceneTransitionTransfer.TryPeekTargetSpawnPointId(out string transitionSpawnPointId))
        {
            return transitionSpawnPointId;
        }

        return playerStartPointId;
    }
}
