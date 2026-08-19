using UnityEngine;

/// <summary>
/// 프로젝트별 레이어 구성 차이로 지면 판정 마스크가 비는 문제를 방지합니다.
/// Ground 레이어가 없으면 현재 맵 지형이 사용하는 Default 레이어를 사용합니다.
/// </summary>
public static class HWJ_PhysicsLayerUtility
{
    public static int ResolveGroundMask(LayerMask configuredMask)
    {
        if (configuredMask.value != 0)
        {
            return configuredMask.value;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");

        if (groundLayer >= 0)
        {
            return 1 << groundLayer;
        }

        int defaultLayer = LayerMask.NameToLayer("Default");
        return defaultLayer >= 0 ? 1 << defaultLayer : Physics2D.DefaultRaycastLayers;
    }

    public static LayerMask CreateGroundMaskForCurrentProject()
    {
        return ResolveGroundMask(default);
    }

    /// <summary>
    /// 두 캐릭터의 Transform 피벗이 아니라 실제 몸 콜라이더 표면 사이의 거리를 반환합니다.
    /// 캐릭터마다 스프라이트 높이와 피벗이 달라도 근접 공격 사거리가 일관되게 동작하도록 사용합니다.
    /// </summary>
    public static float GetColliderSurfaceDistance(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            return float.MaxValue;
        }

        Collider2D sourceCollider = FindEnabledCollider(source);
        Collider2D targetCollider = FindEnabledCollider(target);

        if (sourceCollider != null && targetCollider != null)
        {
            ColliderDistance2D colliderDistance = sourceCollider.Distance(targetCollider);

            if (colliderDistance.isValid)
            {
                return Mathf.Max(0f, colliderDistance.distance);
            }
        }

        return Vector2.Distance(source.position, target.position);
    }

    private static Collider2D FindEnabledCollider(Transform owner)
    {
        Collider2D collider = owner.GetComponent<Collider2D>();

        if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
        {
            return collider;
        }

        Collider2D[] childColliders = owner.GetComponentsInChildren<Collider2D>(false);

        for (int i = 0; i < childColliders.Length; i++)
        {
            Collider2D childCollider = childColliders[i];

            if (childCollider != null
                && childCollider.enabled
                && !childCollider.isTrigger
                && childCollider.gameObject.activeInHierarchy)
            {
                return childCollider;
            }
        }

        return null;
    }
}
