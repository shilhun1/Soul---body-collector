using UnityEngine;

/// <summary>
/// Completes patterns, restores state, and resolves movement, walls, ground, and arena bounds.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private bool IsTargetDead()
    {
        if (currentTarget == null)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            currentTarget.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private void CompletePattern()
    {
        if (!isPatternRunning)
        {
            ForceCleanup(false);
            return;
        }

        string completedPatternId = activeProfile != null
            ? NormalizePatternId(activeProfile.PatternId)
            : string.Empty;
        Record(completedCounts, completedPatternId);
        isPatternRunning = false;
        activeRoutine = null;
        StopWatchdog();
        ForceCleanup(true);
    }

    private void ForceCleanup(bool returnToIdle)
    {
        if (activeProfile != null)
        {
            activeProfile.Hitbox?.Disarm();
            SetTelegraphVisible(activeProfile, false);

            if (hasActiveHitboxOriginalPosition && activeProfile.HitboxRoot != null)
            {
                activeProfile.HitboxRoot.localPosition = activeHitboxOriginalLocalPosition;
            }
        }

        StopHorizontalMovement();
        StopAndReleaseRuntimeObjects();
        RestoreTeleportPresentation();
        ReleaseSoulBind();
        isCasting = false;
        isRecovering = false;
        currentTarget = null;
        hasActiveHitboxOriginalPosition = false;
        activeProfile = null;

        if (returnToIdle)
        {
            bossBrain?.NotifyFighterPatternAttackEnded();
            animatorSystem?.EndAttack();
        }
        else
        {
            animatorSystem?.EndAttack(false);
        }
    }

    private void StopWatchdog()
    {
        if (watchdogRoutine == null)
        {
            return;
        }

        StopCoroutine(watchdogRoutine);
        watchdogRoutine = null;
    }

    private void ArmProfileHitbox(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        int strike,
        float damageMultiplier,
        float knockback)
    {
        ApplyFacingAndHitboxPosition(profile);
        profile.Hitbox?.ArmStrike(strike, damageMultiplier, knockback);
    }

    private void ApplyFacingAndHitboxPosition(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        if (facingRenderer != null)
        {
            facingRenderer.flipX = movementDirection < 0f;
        }

        if (profile?.HitboxRoot != null)
        {
            profile.HitboxRoot.localPosition = new Vector3(
                Mathf.Abs(profile.HitboxLocalX) * movementDirection,
                profile.HitboxLocalY,
                profile.HitboxRoot.localPosition.z);
        }
    }

    private void ConfigureRing(LineRenderer line, Vector3 worldPosition, float radius)
    {
        if (line == null)
        {
            return;
        }

        const int segments = 32;
        line.transform.position = worldPosition;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            line.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 0.28f,
                    0f));
        }
    }

    private void SetTelegraphVisible(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        bool visible)
    {
        if (profile?.TelegraphRenderer != null)
        {
            // LineRenderer 참조는 기존 프리팹 호환을 위해 남기되 현재 연출에서는 사용하지 않습니다.
            profile.TelegraphRenderer.enabled = false;
        }
    }

    private float ResolveDirectionToTarget()
    {
        float direction = currentTarget != null
            ? Mathf.Sign(currentTarget.position.x - transform.position.x)
            : 0f;

        if (Mathf.Approximately(direction, 0f))
        {
            direction = facingRenderer != null && facingRenderer.flipX ? -1f : 1f;
        }

        return direction;
    }

    private float ResolveMaximumTravel(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        float roomWidth = bossBrain != null ? bossBrain.BossRoomSize.x : 20f;
        return Mathf.Max(4f, roomWidth * Mathf.Clamp(profile.ArenaWidthTravelRatio, 0.2f, 0.6f));
    }

    private void MoveBodyHorizontal(float distance)
    {
        Vector2 delta = Vector2.right * movementDirection * Mathf.Max(0f, distance);

        if (body != null)
        {
            body.position += delta;
        }
        else
        {
            transform.position += (Vector3)delta;
        }

        Physics2D.SyncTransforms();
    }

    private void StopHorizontalMovement()
    {
        if (body == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = 0f;
        body.linearVelocity = velocity;
    }

    private bool TryGetWallDistance(float castDistance, out float wallDistance)
    {
        wallDistance = 0f;

        if (bodyCollider == null)
        {
            return false;
        }

        Physics2D.SyncTransforms();
        Bounds bounds = bodyCollider.bounds;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bounds.center,
            new Vector2(
                Mathf.Max(0.1f, bounds.size.x * 0.82f),
                Mathf.Max(0.1f, bounds.size.y * 0.88f)),
            0f,
            Vector2.right * movementDirection,
            castDistance + 0.05f);
        float nearest = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;

            if (ShouldIgnoreMovementHit(hit) || Mathf.Abs(hits[i].normal.x) < 0.45f)
            {
                continue;
            }

            nearest = Mathf.Min(nearest, hits[i].distance);
        }

        if (nearest == float.MaxValue)
        {
            return false;
        }

        wallDistance = nearest;
        return true;
    }

    private bool RuntimeObjectHitsWall(
        RuntimeAttackObject runtimeObject,
        float direction,
        float castDistance)
    {
        Bounds bounds = runtimeObject.Collider.bounds;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bounds.center,
            bounds.size,
            0f,
            Vector2.right * direction,
            castDistance + 0.02f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;

            if (hit == null
                || hit.isTrigger
                || hit == runtimeObject.Collider
                || hit.transform.IsChildOf(transform)
                || transform.IsChildOf(hit.transform))
            {
                continue;
            }

            HWJ_RootObjectDataResolver resolver =
                hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (resolver != null && resolver.ObjectType == HWJ_ObjectType.Player)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldIgnoreMovementHit(Collider2D hit)
    {
        if (hit == null
            || hit.isTrigger
            || hit.transform.IsChildOf(transform)
            || transform.IsChildOf(hit.transform))
        {
            return true;
        }

        HWJ_RootObjectDataResolver resolver =
            hit.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return resolver != null && resolver.ObjectType == HWJ_ObjectType.Player;
    }

    private bool IsGroundedAfterAscent(float startY)
    {
        if (body == null)
        {
            return true;
        }

        if (body.linearVelocity.y > 0.01f)
        {
            return false;
        }

        if (bodyCollider == null)
        {
            return body.position.y <= startY + 0.08f;
        }

        Bounds bounds = bodyCollider.bounds;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bounds.center,
            new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.8f), 0.08f),
            0f,
            Vector2.down,
            bounds.extents.y + 0.12f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!ShouldIgnoreMovementHit(hits[i].collider) && hits[i].normal.y > 0.45f)
            {
                return true;
            }
        }

        return body.position.y <= startY + 0.08f;
    }

    private Vector3 GetBehindTargetPosition(float offset)
    {
        if (currentTarget == null)
        {
            return transform.position;
        }

        float targetFacing = 1f;
        HWJ_CharacterMotionSystem targetMotion =
            currentTarget.GetComponentInParent<HWJ_CharacterMotionSystem>();

        if (targetMotion != null)
        {
            targetFacing = targetMotion.CurrentFacingDirection;
        }

        return currentTarget.position - Vector3.right * targetFacing * offset;
    }

    private Vector3 FindSafeTeleportPosition(Vector3 requested)
    {
        Vector3 clamped = ClampToArena(requested);
        Vector2[] offsets =
        {
            Vector2.zero,
            Vector2.left * 0.75f,
            Vector2.right * 0.75f,
            Vector2.up * 0.75f,
            Vector2.down * 0.5f
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 candidate = ClampToArena(clamped + (Vector3)offsets[i]);

            if (!IsTeleportPositionBlocked(candidate))
            {
                return candidate;
            }
        }

        return bossBrain != null ? (Vector3)bossBrain.BossRoomCenter : transform.position;
    }

    private bool IsTeleportPositionBlocked(Vector3 position)
    {
        Vector2 size = bodyCollider != null
            ? bodyCollider.bounds.size * 0.82f
            : new Vector2(1.1f, 1.7f);
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(position, size, 0f);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];

            if (overlap == null
                || overlap.isTrigger
                || overlap.transform.IsChildOf(transform)
                || transform.IsChildOf(overlap.transform)
                || currentTarget != null
                    && (overlap.transform.IsChildOf(currentTarget)
                        || currentTarget.IsChildOf(overlap.transform)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private Vector3 ClampToArena(Vector3 position)
    {
        if (bossBrain == null)
        {
            return position;
        }

        Vector2 center = bossBrain.BossRoomCenter;
        Vector2 half = bossBrain.BossRoomSize * 0.5f;
        const float padding = 0.9f;
        position.x = Mathf.Clamp(position.x, center.x - half.x + padding, center.x + half.x - padding);
        position.y = Mathf.Clamp(position.y, center.y - half.y + padding, center.y + half.y - padding);
        return position;
    }

}
