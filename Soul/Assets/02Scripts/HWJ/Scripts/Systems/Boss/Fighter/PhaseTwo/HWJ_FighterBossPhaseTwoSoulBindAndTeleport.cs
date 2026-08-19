using UnityEngine;

/// <summary>
/// Handles teleport presentation, soul binding, line-of-sight, and target pulling.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private void TeleportOut(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        profile.Hitbox?.Disarm();
        teleportPresentationHidden = true;
        Record(teleportOutCounts, profile.PatternId);

        if (facingRenderer != null)
        {
            facingRenderer.enabled = false;
        }

        if (bodyCollider != null)
        {
            bodyColliderEnabledBeforeTeleport = bodyCollider.enabled;
            bodyCollider.enabled = false;
        }
    }

    private void TeleportIn(HWJ_FighterBossPhaseTwoPatternProfile profile, Vector3 position)
    {
        pendingTeleportPosition = FindSafeTeleportPosition(position);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = pendingTeleportPosition;
        }

        transform.position = pendingTeleportPosition;
        Physics2D.SyncTransforms();
        RestoreTeleportPresentation();
        LastTeleportLandingPosition = pendingTeleportPosition;
        Record(teleportInCounts, profile.PatternId);
        movementDirection = ResolveDirectionToTarget();
        ApplyFacingAndHitboxPosition(profile);
    }

    private void RestoreTeleportPresentation()
    {
        if (!teleportPresentationHidden)
        {
            return;
        }

        if (facingRenderer != null)
        {
            facingRenderer.enabled = true;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = bodyColliderEnabledBeforeTeleport;
        }

        teleportPresentationHidden = false;
    }

    private void PullBoundTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Rigidbody2D targetBody = currentTarget.GetComponent<Rigidbody2D>();

        if (targetBody == null)
        {
            targetBody = currentTarget.GetComponentInParent<Rigidbody2D>();
        }

        Vector2 direction = ((Vector2)transform.position - (Vector2)currentTarget.position).normalized;

        if (targetBody != null)
        {
            targetBody.linearVelocity = direction * soulBindPullSpeed;
        }
        else
        {
            currentTarget.position += (Vector3)(direction * soulBindPullSpeed * Time.fixedDeltaTime);
        }
    }

    private bool CanMaintainSoulBind()
    {
        if (currentTarget == null || IsTargetDead())
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);
        return distance <= soulBindMaximumDistance && HasClearLineToTarget();
    }

    private bool IsTargetDashing()
    {
        if (currentTarget == null)
        {
            return false;
        }

        HWJ_PlayerMovementSystem movement = currentTarget.GetComponent<HWJ_PlayerMovementSystem>();

        if (movement == null)
        {
            movement = currentTarget.GetComponentInParent<HWJ_PlayerMovementSystem>();
        }

        return movement != null && movement.IsDashing;
    }

    private bool HasClearLineToTarget()
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(transform.position, currentTarget.position);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;

            if (hit == null
                || hit.isTrigger
                || hit.transform.IsChildOf(transform)
                || transform.IsChildOf(hit.transform)
                || hit.transform.IsChildOf(currentTarget)
                || currentTarget.IsChildOf(hit.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void ReleaseSoulBind()
    {
        soulBindActive = false;
    }

}
