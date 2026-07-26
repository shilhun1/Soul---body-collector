using UnityEngine;

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
