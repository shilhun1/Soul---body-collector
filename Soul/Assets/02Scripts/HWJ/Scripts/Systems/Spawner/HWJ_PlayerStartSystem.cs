using UnityEngine;

public class HWJ_PlayerStartSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_SpawnPoint playerStartPoint;
    [SerializeField] private string playerStartPointId;
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private bool registerToGameManager = true;

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

            if (!string.IsNullOrWhiteSpace(playerStartPointId) && points[i].PointId == playerStartPointId)
            {
                playerStartPoint = points[i];
                return;
            }
        }

        playerStartPoint = fallbackPoint;
    }
}
