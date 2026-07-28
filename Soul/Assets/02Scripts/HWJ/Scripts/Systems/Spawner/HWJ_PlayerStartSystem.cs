using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HWJ_PlayerStartSystem : MonoBehaviour
{
    public const string DefaultPlayerVisualObjectName = "HWJ_PlayerVisual";

    [Header("Player Reference")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_SpawnPoint playerStartPoint;

    [Space(8f)]
    [Header("Start Position")]
    [SerializeField] private string playerStartPointId;
    [SerializeField] private bool useSceneTransitionTargetSpawnPoint = true;

    [Space(8f)]
    [Header("Auto Run")]
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private bool registerToGameManager = true;
    [SerializeField] private bool preferPersistentPlayerDuringSceneTransition = true;
    [SerializeField] private bool destroySceneFallbackPlayersDuringSceneTransition = true;

    [Space(8f)]
    [Header("Visual Correction")]
    [SerializeField] private bool normalizePlayerVisualOffsetOnPlacement = true;
    [SerializeField] private string playerVisualObjectName = DefaultPlayerVisualObjectName;

    [Space(8f)]
    [Header("Debug Result")]
    [SerializeField] private string lastPlacementResult;

    private Transform resolvedFallbackStartTransform;

    public string PlayerStartPointId => playerStartPointId;
    public string LastPlacementResult => lastPlacementResult;

    private void Start()
    {
        if (placeOnStart)
        {
            TryPlacePlayerAtStart();
        }
    }

    public void PlacePlayerAtStart()
    {
        TryPlacePlayerAtStart();
    }

    /// <summary>
    /// Moves the active player to the selected start marker.
    /// The fallback accepts clearly named PlayerStart markers even if their SpawnPointType was left as Enemy in the scene.
    /// </summary>
    public bool TryPlacePlayerAtStart()
    {
        ResolvePlayer();
        ResolveStartPoint();

        Transform startTransform = ResolveStartTransform();

        if (playerResolver == null || startTransform == null)
        {
            lastPlacementResult = "Player start placement failed: missing player or start marker.";
            return false;
        }

        Transform playerTransform = playerResolver.transform;
        playerTransform.SetPositionAndRotation(startTransform.position, startTransform.rotation);

        if (playerTransform.TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (normalizePlayerVisualOffsetOnPlacement)
        {
            NormalizePlayerVisualOffset(playerTransform, playerVisualObjectName);
        }

        if (registerToGameManager && HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.RegisterPlayer(playerResolver);
        }

        string startLabel = playerStartPoint != null && !string.IsNullOrWhiteSpace(playerStartPoint.PointId)
            ? playerStartPoint.PointId
            : startTransform.name;
        lastPlacementResult = $"Player start placement complete: {startLabel}";

        if (useSceneTransitionTargetSpawnPoint && HWJ_SceneTransitionTransfer.HasPendingArrival)
        {
            HWJ_SceneTransitionTransfer.ClearPendingArrival();
        }

        return true;
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
            resolvedFallbackStartTransform = playerStartPoint.transform;
            return;
        }

        resolvedFallbackStartTransform = null;
        HWJ_SpawnPoint[] points = FindObjectsByType<HWJ_SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HWJ_SpawnPoint typedFallbackPoint = null;
        HWJ_SpawnPoint namedFallbackPoint = null;
        string requestedPointId = ResolveRequestedStartPointId();

        for (int i = 0; i < points.Length; i++)
        {
            HWJ_SpawnPoint point = points[i];

            if (point == null)
            {
                continue;
            }

            if (IsRequestedPoint(point, requestedPointId))
            {
                playerStartPoint = point;
                resolvedFallbackStartTransform = point.transform;
                return;
            }

            if (point.SpawnPointType == HWJ_SpawnPointType.PlayerStart)
            {
                typedFallbackPoint ??= point;
                continue;
            }

            if (namedFallbackPoint == null && IsLikelyPlayerStartMarker(point))
            {
                namedFallbackPoint = point;
            }
        }

        playerStartPoint = typedFallbackPoint != null ? typedFallbackPoint : namedFallbackPoint;
        resolvedFallbackStartTransform = playerStartPoint != null
            ? playerStartPoint.transform
            : FindNamedPlayerStartTransform(requestedPointId);
    }

    private Transform ResolveStartTransform()
    {
        return playerStartPoint != null ? playerStartPoint.transform : resolvedFallbackStartTransform;
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

    private static bool IsRequestedPoint(HWJ_SpawnPoint point, string requestedPointId)
    {
        return point != null
            && !string.IsNullOrWhiteSpace(requestedPointId)
            && string.Equals(point.PointId, requestedPointId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyPlayerStartMarker(HWJ_SpawnPoint point)
    {
        if (point == null)
        {
            return false;
        }

        return IsLikelyPlayerStartName(point.PointId)
            || IsLikelyPlayerStartName(point.gameObject.name);
    }

    private static Transform FindNamedPlayerStartTransform(string requestedPointId)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Transform namedFallback = null;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(requestedPointId)
                && string.Equals(candidate.name, requestedPointId, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            if (namedFallback == null && IsLikelyPlayerStartName(candidate.name))
            {
                namedFallback = candidate;
            }
        }

        return namedFallback;
    }

    /// <summary>
    /// Keeps the visible player sprite aligned to the player root after spawn or scene transition placement.
    /// Scene objects can accidentally keep an old world-position offset on the visual child, so only the visual child is reset.
    /// </summary>
    public static bool NormalizePlayerVisualOffset(
        Transform playerTransform,
        string visualObjectName = DefaultPlayerVisualObjectName)
    {
        Transform visualTransform = FindPlayerVisualTransform(playerTransform, visualObjectName);

        if (visualTransform == null)
        {
            return false;
        }

        Vector3 localPosition = visualTransform.localPosition;
        visualTransform.localPosition = new Vector3(0f, 0f, localPosition.z);
        visualTransform.localRotation = Quaternion.identity;
        return true;
    }

    private static Transform FindPlayerVisualTransform(Transform playerTransform, string visualObjectName)
    {
        if (playerTransform == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(visualObjectName))
        {
            Transform namedVisual = playerTransform.Find(visualObjectName);

            if (namedVisual != null && namedVisual.GetComponentInChildren<SpriteRenderer>(true) != null)
            {
                return namedVisual;
            }
        }

        SpriteRenderer[] renderers = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null || renderer.transform == playerTransform)
            {
                continue;
            }

            if (IsLikelyPlayerVisualName(renderer.gameObject.name))
            {
                return renderer.transform;
            }
        }

        return null;
    }

    private static bool IsLikelyPlayerVisualName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .ToLowerInvariant();

        return normalized.Contains("visual", StringComparison.Ordinal)
            || normalized.Contains("sprite", StringComparison.Ordinal)
            || normalized.Contains("ghost", StringComparison.Ordinal);
    }

    private static bool IsLikelyPlayerStartName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .ToLowerInvariant();

        return normalized.Contains("playerstart", StringComparison.Ordinal)
            && !normalized.Contains("system", StringComparison.Ordinal)
            && !normalized.Contains("visual", StringComparison.Ordinal);
    }
}
