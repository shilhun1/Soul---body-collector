using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene-load retries, player start placement, and Cinemachine target wiring.
/// This is part of HWJ_GameManager; the scene still needs only the original component.
/// </summary>
public partial class HWJ_GameManager
{
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueSceneBootstrap($"scene loaded: {scene.name}");
    }

    private void QueueSceneBootstrap(string source)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (sceneBootstrapRoutine != null)
        {
            StopCoroutine(sceneBootstrapRoutine);
        }

        sceneBootstrapRoutine = StartCoroutine(SceneBootstrapRoutine(source));
    }

    private IEnumerator SceneBootstrapRoutine(string source)
    {
        bool placedPlayer = !autoPlacePlayerAtSceneStart;
        bool boundCameras = !autoBindSceneCamerasToPlayer;
        float endTime = Time.realtimeSinceStartup + Mathf.Max(0.05f, sceneBootstrapRetrySeconds);

        do
        {
            ResolveSceneReferences();

            if (!placedPlayer && TryPlacePlayerAtSceneStart())
            {
                placedPlayer = true;
            }

            if (!boundCameras && TryBindSceneCamerasToPlayer())
            {
                boundCameras = true;
            }

            if (placedPlayer && boundCameras)
            {
                break;
            }

            yield return null;
        }
        while (Time.realtimeSinceStartup < endTime);

        lastSceneBootstrapResult = $"{source} | playerStart={placedPlayer} | cameras={boundCameras}";
        sceneBootstrapRoutine = null;
    }

    private bool TryPlacePlayerAtSceneStart()
    {
        HWJ_PlayerStartSystem[] startSystems = FindObjectsByType<HWJ_PlayerStartSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < startSystems.Length; i++)
        {
            if (startSystems[i] != null && startSystems[i].TryPlacePlayerAtStart())
            {
                playerResolver = PlayerResolver != null ? PlayerResolver : FindPlayerResolverInScene();

                if (playerResolver != null)
                {
                    HWJ_PlayerStartSystem.NormalizePlayerVisualOffset(playerResolver.transform);
                }

                return true;
            }
        }

        if (playerResolver == null)
        {
            playerResolver = FindPlayerResolverInScene();
        }

        if (playerResolver == null)
        {
            return false;
        }

        Transform startTransform = FindBestScenePlayerStartTransform();

        if (startTransform == null)
        {
            return false;
        }

        Transform playerTransform = playerResolver.transform;
        playerTransform.SetPositionAndRotation(startTransform.position, startTransform.rotation);

        if (playerTransform.TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        HWJ_PlayerStartSystem.NormalizePlayerVisualOffset(playerTransform);

        RegisterPlayer(playerResolver);
        return true;
    }

    private bool TryBindSceneCamerasToPlayer()
    {
        if (playerResolver == null)
        {
            playerResolver = FindPlayerResolverInScene();
        }

        if (playerResolver == null)
        {
            return false;
        }

        int boundCount = 0;
        Transform playerTransform = playerResolver.transform;

        HWJ_PlayerCameraFollowSystem[] followSystems = FindObjectsByType<HWJ_PlayerCameraFollowSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < followSystems.Length; i++)
        {
            if (followSystems[i] == null)
            {
                continue;
            }

            followSystems[i].SetTarget(playerResolver);
            boundCount++;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || !IsCinemachineCameraComponent(behaviour))
            {
                continue;
            }

            if (TryAssignCinemachineTrackingTarget(behaviour, playerTransform))
            {
                boundCount++;
            }
        }

        return boundCount > 0;
    }

    private static Transform FindBestScenePlayerStartTransform()
    {
        string requestedPointId = null;

        if (HWJ_SceneTransitionTransfer.TryPeekTargetSpawnPointId(out string transitionSpawnPointId))
        {
            requestedPointId = transitionSpawnPointId;
        }

        HWJ_SpawnPoint[] points = FindObjectsByType<HWJ_SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HWJ_SpawnPoint typedFallbackPoint = null;
        HWJ_SpawnPoint namedFallbackPoint = null;

        for (int i = 0; i < points.Length; i++)
        {
            HWJ_SpawnPoint point = points[i];

            if (point == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(requestedPointId)
                && string.Equals(point.PointId, requestedPointId, StringComparison.OrdinalIgnoreCase))
            {
                return point.transform;
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

        if (typedFallbackPoint != null)
        {
            return typedFallbackPoint.transform;
        }

        if (namedFallbackPoint != null)
        {
            return namedFallbackPoint.transform;
        }

        return FindNamedPlayerStartTransform(requestedPointId);
    }

    private static bool IsLikelyPlayerStartMarker(HWJ_SpawnPoint point)
    {
        return point != null
            && (IsLikelyPlayerStartName(point.PointId) || IsLikelyPlayerStartName(point.gameObject.name));
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

    private static bool IsCinemachineCameraComponent(MonoBehaviour behaviour)
    {
        Type type = behaviour.GetType();
        string fullName = type.FullName ?? string.Empty;
        return fullName == "Unity.Cinemachine.CinemachineCamera"
            || fullName == "Cinemachine.CinemachineVirtualCamera"
            || type.Name == "CinemachineCamera"
            || type.Name == "CinemachineVirtualCamera";
    }

    private static bool TryAssignCinemachineTrackingTarget(MonoBehaviour cameraComponent, Transform target)
    {
        if (cameraComponent == null || target == null)
        {
            return false;
        }

        Type type = cameraComponent.GetType();
        bool assigned = TrySetTransformProperty(type, cameraComponent, "Follow", target);

        assigned |= TrySetCinemachineTargetProperty(type, cameraComponent, target);
        assigned |= TrySetCinemachineTargetField(type, cameraComponent, target);
        return assigned;
    }

    private static bool TrySetTransformProperty(Type type, object owner, string propertyName, Transform target)
    {
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        if (property == null || !property.CanWrite || !typeof(Transform).IsAssignableFrom(property.PropertyType))
        {
            return false;
        }

        property.SetValue(owner, target);
        return true;
    }

    private static bool TrySetCinemachineTargetProperty(Type type, object owner, Transform target)
    {
        PropertyInfo targetProperty = type.GetProperty("Target", BindingFlags.Instance | BindingFlags.Public);

        if (targetProperty == null || !targetProperty.CanRead || !targetProperty.CanWrite)
        {
            return false;
        }

        object targetValue = targetProperty.GetValue(owner);

        if (!TrySetTrackingTargetOnValue(ref targetValue, target))
        {
            return false;
        }

        targetProperty.SetValue(owner, targetValue);
        return true;
    }

    private static bool TrySetCinemachineTargetField(Type type, object owner, Transform target)
    {
        FieldInfo targetField = type.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (targetField == null)
        {
            return false;
        }

        object targetValue = targetField.GetValue(owner);

        if (!TrySetTrackingTargetOnValue(ref targetValue, target))
        {
            return false;
        }

        targetField.SetValue(owner, targetValue);
        return true;
    }

    private static bool TrySetTrackingTargetOnValue(ref object targetValue, Transform target)
    {
        if (targetValue == null)
        {
            return false;
        }

        Type targetType = targetValue.GetType();
        FieldInfo trackingField = targetType.GetField("TrackingTarget", BindingFlags.Instance | BindingFlags.Public);

        if (trackingField != null && typeof(Transform).IsAssignableFrom(trackingField.FieldType))
        {
            trackingField.SetValue(targetValue, target);
            return true;
        }

        PropertyInfo trackingProperty = targetType.GetProperty("TrackingTarget", BindingFlags.Instance | BindingFlags.Public);

        if (trackingProperty != null
            && trackingProperty.CanWrite
            && typeof(Transform).IsAssignableFrom(trackingProperty.PropertyType))
        {
            trackingProperty.SetValue(targetValue, target);
            return true;
        }

        return false;
    }

}
