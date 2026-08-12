using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the seven ordered mid-boss pattern routines.
/// This partial belongs to the single HWJ_MidBossPatternSystem component.
/// </summary>
public partial class HWJ_MidBossPatternSystem
{
    private IEnumerator Pattern1SummonRoutine(Transform target)
    {
        float totalSeconds = Mathf.Max(0f, dashSeconds)
            + Mathf.Max(0f, pattern1WhistleSeconds)
            + Mathf.Max(0, pattern1SummonCount) * Mathf.Max(0f, pattern1SummonIntervalSeconds)
            + Mathf.Max(0f, recoverySeconds);
        skillActionSystem?.BlockNavigationForSkill(totalSeconds, true);

        Vector3 edgePosition = ResolveFarEdgePosition(target, pattern1EdgePadding);
        yield return DashToPosition(edgePosition, dashSeconds);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern1WhistleSeconds));

        int spawnCount = Mathf.Max(0, pattern1SummonCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPossessableMonster(i);

            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, pattern1SummonIntervalSeconds));
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

    private IEnumerator Pattern2DashDoubleSlashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds + slashWarningSeconds * 2f + 0.5f + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.9f);
        float horizontalSlashDirection = GetDirectionToTarget(target);
        ShowForwardSlashWarning(horizontalSlashDirection, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f, horizontalSlashDirection);

        yield return new WaitForSeconds(0.5f);
        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

    private IEnumerator Pattern3SlashAndFullWaveRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds * 2f + slashWarningSeconds + pattern3ChargeSeconds + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.75f);
        float openingSlashDirection = GetDirectionToTarget(target);
        ShowForwardSlashWarning(openingSlashDirection, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f, openingSlashDirection);

        yield return DashToPosition(ResolveFarEdgePosition(target, pattern1EdgePadding), dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, pattern3ChargeSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern3ChargeSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, pattern3DamageMultiplier);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

    private IEnumerator Pattern4FixedDamageDashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(pattern4AimSeconds + dashSeconds + pattern4GroggySeconds, true);

        float direction = GetDirectionToTarget(target);
        Vector3 lockedTargetPosition = target != null ? target.position : transform.position;
        Vector3 dashDestination = lockedTargetPosition - Vector3.right * direction * 0.35f;
        dashDestination.y = transform.position.y;
        dashDestination.z = transform.position.z;
        float dashDistance = Mathf.Max(1f, Mathf.Abs(dashDestination.x - transform.position.x));

        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            direction,
            dashDistance,
            0.8f,
            pattern4AimSeconds,
            warningColor,
            warningLineWidth);

        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4AimSeconds));
        yield return DashToPosition(dashDestination, dashSeconds);
        DamageTargetByFixedMaxHpRatioIfInsideForwardRange(
            target,
            direction,
            horizontalSlashRange * 1.5f,
            pattern4FixedMaxHpDamageRatio);
        FinishPatternRoutine();
        bossBrain?.ForceGroggy(pattern4GroggySeconds);
    }

    private IEnumerator Pattern5RedSwordComboRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern5BuffHoldSeconds
            + slashWarningSeconds * 4f
            + pattern5ComboIntervalSeconds * 2f
            + recoverySeconds,
            true);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern5BuffHoldSeconds));

        float comboSlashDirection = GetDirectionToTarget(target);
        ShowForwardSlashWarning(comboSlashDirection, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f, comboSlashDirection);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, pattern7ImpactRadius, 1f);

        float comboShockwaveDirection = GetDirectionToTarget(target);
        ShowHalfRoomShockwaveWarning(
            pattern5ShockwaveWidthRatio,
            shockwaveHeight,
            slashWarningSeconds,
            comboShockwaveDirection);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(
            target,
            pattern5ShockwaveWidthRatio,
            shockwaveHeight,
            1.15f,
            comboShockwaveDirection);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

    private IEnumerator Pattern6VanishBackstabRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern6VanishDelaySeconds
            + dashSeconds
            + slashWarningSeconds * 2f
            + pattern6ReturnDelaySeconds
            + recoverySeconds,
            true);

        Vector3 returnPosition = transform.position;
        yield return new WaitForSeconds(Mathf.Max(0f, pattern6VanishDelaySeconds));
        SetSpriteVisible(false);
        SetPosition(ResolveBehindTargetPosition(target));
        SetSpriteVisible(true);

        float backstabDirection = GetDirectionToTarget(target);
        ShowForwardSlashWarning(backstabDirection, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f, backstabDirection);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern6ReturnDelaySeconds));
        yield return DashToPosition(returnPosition, dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

    private IEnumerator Pattern7JumpSlamRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern7AirHoldSeconds + slashWarningSeconds * 2f + recoverySeconds,
            true);

        Vector3 startPosition = transform.position;
        Vector3 airPosition = startPosition + Vector3.up * Mathf.Max(0f, pattern7JumpHeight);
        SetPosition(airPosition);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern7AirHoldSeconds));

        Vector3 landingPosition = target != null
            ? new Vector3(target.position.x, startPosition.y, startPosition.z)
            : startPosition;
        ShowCircleWarning(landingPosition, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        float landingShockwaveDirection = GetDirectionToTarget(target);
        SetPosition(landingPosition);
        DamageTargetIfInsideCircle(target, landingPosition, pattern7ImpactRadius, 1.2f);

        ShowHalfRoomShockwaveWarning(
            pattern7ShockwaveWidthRatio,
            shockwaveHeight,
            slashWarningSeconds,
            landingShockwaveDirection);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(
            target,
            pattern7ShockwaveWidthRatio,
            shockwaveHeight,
            1.1f,
            landingShockwaveDirection);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        FinishPatternRoutine();
    }

}
