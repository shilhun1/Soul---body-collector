using System.Collections;
using UnityEngine;

/// <summary>
/// Runs dash movement and displays attack warning shapes.
/// This partial belongs to the single HWJ_MidBossPatternSystem component.
/// </summary>
public partial class HWJ_MidBossPatternSystem
{
    private IEnumerator DashToTargetSide(Transform target, float stopOffset)
    {
        if (target == null)
        {
            yield break;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 destination = target.position - Vector3.right * direction * Mathf.Max(0f, stopOffset);
        destination.y = transform.position.y;
        destination.z = transform.position.z;
        yield return DashToPosition(destination, dashSeconds);
    }

    private IEnumerator DashToPosition(Vector3 destination, float seconds)
    {
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.01f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetPosition(Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetPosition(destination);
        StopMovement();
    }

    private Vector3 ResolveFarEdgePosition(Transform target, float padding)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float leftX = roomCenter.x - roomSize.x * 0.5f + Mathf.Max(0f, padding);
        float rightX = roomCenter.x + roomSize.x * 0.5f - Mathf.Max(0f, padding);
        float targetX = target != null ? target.position.x : roomCenter.x;
        float destinationX = Mathf.Abs(targetX - leftX) > Mathf.Abs(targetX - rightX) ? leftX : rightX;
        return new Vector3(destinationX, transform.position.y, transform.position.z);
    }

    private Vector3 ResolveBehindTargetPosition(Transform target)
    {
        if (target == null)
        {
            return transform.position;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 position = target.position + Vector3.right * direction * Mathf.Max(0f, pattern6BehindOffset);
        position.y = transform.position.y;
        position.z = transform.position.z;
        return position;
    }

    private void ShowForwardSlashWarning(Transform target, float range, float seconds)
    {
        ShowForwardSlashWarning(GetDirectionToTarget(target), range, seconds);
    }

    private void ShowForwardSlashWarning(float lockedDirection, float range, float seconds)
    {
        float direction = Mathf.Sign(Mathf.Approximately(lockedDirection, 0f) ? 1f : lockedDirection);
        HWJ_SkillWarningIndicator.ShowForwardArc(
            transform.position,
            direction,
            Mathf.Max(0.1f, range),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowCircleWarning(Vector3 center, float radius, float seconds)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            center,
            Mathf.Max(0.1f, radius),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowRoomWideHorizontalWarning(float height, float seconds)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        HWJ_SkillWarningIndicator.ShowRectangle(
            new Vector3(roomCenter.x, transform.position.y, transform.position.z),
            new Vector2(Mathf.Max(1f, roomSize.x), Mathf.Max(0.1f, height)),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowHalfRoomShockwaveWarning(float widthRatio, float height, float seconds)
    {
        ShowHalfRoomShockwaveWarning(
            widthRatio,
            height,
            seconds,
            GetDirectionToTarget(bossBrain != null ? bossBrain.Target : null));
    }

    private void ShowHalfRoomShockwaveWarning(float widthRatio, float height, float seconds, float lockedDirection)
    {
        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height, lockedDirection);
        HWJ_SkillWarningIndicator.ShowRectangle(
            rect.center,
            rect.size,
            seconds,
            warningColor,
            warningLineWidth);
    }

}
