using UnityEngine;

/// <summary>
/// Evaluates attack areas, applies damage, and resolves boss-room positions.
/// This partial belongs to the single HWJ_MidBossPatternSystem component.
/// </summary>
public partial class HWJ_MidBossPatternSystem
{
    private void DamageTargetIfInsideForwardRange(Transform target, float range, float damageMultiplier)
    {
        DamageTargetIfInsideForwardRange(target, range, damageMultiplier, GetDirectionToTarget(target));
    }

    private void DamageTargetIfInsideForwardRange(
        Transform target,
        float range,
        float damageMultiplier,
        float lockedDirection)
    {
        if (target == null)
        {
            return;
        }

        float direction = Mathf.Sign(Mathf.Approximately(lockedDirection, 0f) ? 1f : lockedDirection);
        Vector3 delta = target.position - transform.position;

        if (Mathf.Sign(delta.x) != Mathf.Sign(direction)
            || Mathf.Abs(delta.x) > Mathf.Max(0.1f, range)
            || Mathf.Abs(delta.y) > Mathf.Max(0.1f, verticalSlashRadius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetByFixedMaxHpRatioIfInsideForwardRange(
        Transform target,
        float lockedDirection,
        float range,
        float damageRatio)
    {
        if (target == null)
        {
            return;
        }

        float direction = Mathf.Sign(Mathf.Approximately(lockedDirection, 0f) ? 1f : lockedDirection);
        Vector3 delta = target.position - transform.position;

        if (Mathf.Sign(delta.x) != Mathf.Sign(direction)
            || Mathf.Abs(delta.x) > Mathf.Max(0.1f, range)
            || Mathf.Abs(delta.y) > Mathf.Max(0.1f, verticalSlashRadius))
        {
            return;
        }

        DamageTargetByFixedMaxHpRatio(target, damageRatio);
    }

    private void DamageTargetIfInsideCircle(Transform target, Vector3 center, float radius, float damageMultiplier)
    {
        if (target == null || Vector2.Distance(target.position, center) > Mathf.Max(0.1f, radius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideRoomHorizontalBand(Transform target, float height, float damageMultiplier)
    {
        if (target == null || Mathf.Abs(target.position.y - transform.position.y) > Mathf.Max(0.1f, height) * 0.5f)
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideHalfRoomShockwave(Transform target, float widthRatio, float height, float damageMultiplier)
    {
        DamageTargetIfInsideHalfRoomShockwave(
            target,
            widthRatio,
            height,
            damageMultiplier,
            GetDirectionToTarget(bossBrain != null ? bossBrain.Target : null));
    }

    private void DamageTargetIfInsideHalfRoomShockwave(
        Transform target,
        float widthRatio,
        float height,
        float damageMultiplier,
        float lockedDirection)
    {
        if (target == null)
        {
            return;
        }

        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height, lockedDirection);

        if (!rect.Contains((Vector2)target.position))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetByFixedMaxHpRatio(Transform target, float damageRatio)
    {
        HWJ_RuntimeStatusSystem targetStatus = target != null
            ? target.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;

        if (targetStatus == null || targetStatus.IsDead || targetStatus.MaxHp <= 0f)
        {
            return;
        }

        targetStatus.ApplyDamage(targetStatus.MaxHp * Mathf.Clamp01(damageRatio), combatSystem, null);
    }

    private void DamageTarget(Transform target, float damageMultiplier)
    {
        HWJ_RootObjectDataResolver targetResolver = target != null
            ? target.GetComponentInParent<HWJ_RootObjectDataResolver>()
            : null;

        combatSystem?.TryDealDamageTo(targetResolver, damageMultiplier, out _);
    }

    private Rect ResolveHalfRoomShockwaveRect(float widthRatio, float height)
    {
        return ResolveHalfRoomShockwaveRect(
            widthRatio,
            height,
            GetDirectionToTarget(bossBrain != null ? bossBrain.Target : null));
    }

    private Rect ResolveHalfRoomShockwaveRect(float widthRatio, float height, float lockedDirection)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float direction = Mathf.Sign(Mathf.Approximately(lockedDirection, 0f) ? 1f : lockedDirection);
        float width = Mathf.Max(1f, roomSize.x * Mathf.Clamp01(widthRatio));
        float centerX = transform.position.x + direction * width * 0.5f;
        return new Rect(
            centerX - width * 0.5f,
            transform.position.y - Mathf.Max(0.1f, height) * 0.5f,
            width,
            Mathf.Max(0.1f, height));
    }

    private bool IsInsideBossRoom(Vector3 position)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        Vector2 halfSize = roomSize * 0.5f;
        return Mathf.Abs(position.x - roomCenter.x) <= halfSize.x
            && Mathf.Abs(position.y - roomCenter.y) <= halfSize.y;
    }

    private Vector2 GetRoomCenter()
    {
        return bossBrain != null ? bossBrain.BossRoomCenter : (Vector2)transform.position;
    }

    private Vector2 GetRoomSize()
    {
        return bossBrain != null ? bossBrain.BossRoomSize : new Vector2(28f, 14f);
    }

    private float GetDirectionToTarget(Transform target)
    {
        if (target == null)
        {
            return transform.localScale.x < 0f ? -1f : 1f;
        }

        float deltaX = target.position.x - transform.position.x;
        return Mathf.Abs(deltaX) <= 0.001f ? 1f : Mathf.Sign(deltaX);
    }

    private Transform FindPlayerTarget()
    {
        if (bossBrain != null && bossBrain.Target != null)
        {
            return bossBrain.Target;
        }

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void SetPosition(Vector3 position)
    {
        if (body != null)
        {
            body.position = (Vector2)position;
            return;
        }

        transform.position = position;
    }

    private void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void BeginPatternPhysicsOverride()
    {
        if (body == null || isPatternPhysicsOverridden)
        {
            return;
        }

        cachedPatternGravityScale = body.gravityScale;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        isPatternPhysicsOverridden = true;
    }

    private void EndPatternPhysicsOverride()
    {
        if (body == null || !isPatternPhysicsOverridden)
        {
            return;
        }

        body.gravityScale = cachedPatternGravityScale;
        body.linearVelocity = Vector2.zero;
        isPatternPhysicsOverridden = false;
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = visible;
            }
        }
    }

}
