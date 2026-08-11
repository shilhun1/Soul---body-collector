using System.Collections;
using UnityEngine;

/// <summary>
/// Runs recovery movement and line, arc, or ring warning visuals.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private IEnumerator Recovery(HWJ_FighterBossPhaseTwoPatternProfile profile, float seconds)
    {
        profile.Hitbox?.Disarm();
        SetTelegraphVisible(profile, false);
        StopHorizontalMovement();
        isRecovering = true;
        bossBrain?.NotifyFighterPatternRecoveryStarted();
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

    private IEnumerator MoveHorizontalRoutine(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        float speed,
        float duration)
    {
        float elapsed = 0f;
        float maximumDistance = ResolveMaximumTravel(profile);

        while (isPatternRunning
            && elapsed < duration
            && lastMovementDistance < maximumDistance)
        {
            float step = Mathf.Min(
                Mathf.Max(0f, speed) * Time.fixedDeltaTime,
                maximumDistance - lastMovementDistance);

            if (step <= 0f)
            {
                break;
            }

            if (TryGetWallDistance(step, out float wallDistance))
            {
                float safeDistance = Mathf.Max(0f, wallDistance - 0.03f);
                MoveBodyHorizontal(safeDistance);
                lastMovementDistance += safeDistance;
                lastMovementStoppedByWall = true;
                break;
            }

            MoveBodyHorizontal(step);
            lastMovementDistance += step;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopHorizontalMovement();
    }

    private IEnumerator MoveToPositionRoutine(Vector2 destination, float duration)
    {
        Vector2 start = body != null ? body.position : (Vector2)transform.position;
        Vector2 clampedDestination = ClampToArena(destination);
        float elapsed = 0f;

        while (isPatternRunning && elapsed < Mathf.Max(0.05f, duration))
        {
            elapsed += Time.fixedDeltaTime;
            float ratio = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
            Vector2 next = Vector2.Lerp(start, clampedDestination, ratio);

            if (body != null)
            {
                body.MovePosition(next);
            }
            else
            {
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            yield return new WaitForFixedUpdate();
        }

        StopHorizontalMovement();
    }

    private IEnumerator ShowLineWarning(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        float range,
        float seconds)
    {
        SetTelegraphVisible(profile, false);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

    private IEnumerator ShowArcWarning(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        float seconds)
    {
        SetTelegraphVisible(profile, false);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

    private IEnumerator ShowRingWarning(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        Vector3 worldPosition,
        float radius,
        float seconds)
    {
        SetTelegraphVisible(profile, false);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

}
