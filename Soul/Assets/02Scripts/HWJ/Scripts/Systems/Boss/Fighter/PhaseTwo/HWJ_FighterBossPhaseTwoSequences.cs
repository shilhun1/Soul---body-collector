using System.Collections;
using UnityEngine;

/// <summary>
/// Runs cast waits and the ordered steps for every phase-two pattern.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private IEnumerator CastThenRunPatternSequence(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        SetTelegraphVisible(profile, false);
        yield return new WaitForSeconds(Mathf.Max(0f, profile.CastWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        bool attackMotionStarted = animatorSystem != null
            ? animatorSystem.CommitAttack(profile.AnimatorState)
            : PlayLegacyAnimatorState(profile.AnimatorState);

        if (!attackMotionStarted)
        {
            // Animator가 없어도 기믹 검증은 가능하도록 실제 패턴 코루틴은 계속 실행합니다.
            lastPatternUsedAnimator = false;
        }

        yield return RunPatternSequence(profile);
    }

    private IEnumerator RunPatternSequence(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        switch (profile.PatternKind)
        {
            case HWJ_FighterBossPhaseTwoPatternKind.EnhancedCombo:
                yield return EnhancedComboRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge:
                yield return DoubleChargeRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.ThunderUppercut:
                yield return ThunderUppercutRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.DarkGroundSlam:
                yield return DarkGroundSlamRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.ShadowCombo:
                yield return ShadowComboRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.LightningCast:
                yield return LightningCastRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.DarkWave:
                yield return DarkWaveRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.SoulBind:
                yield return SoulBindRoutine(profile);
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.Ultimate:
                yield return UltimateRoutine(profile);
                break;
        }

        if (isPatternRunning)
        {
            CompletePattern();
        }
    }

    private IEnumerator PatternWatchdogRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        yield return new WaitForSeconds(Mathf.Max(0.5f, profile.WatchdogSeconds));
        watchdogRoutine = null;

        if (!isPatternRunning)
        {
            yield break;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isPatternRunning = false;
        Debug.LogWarning($"[HWJ] Fighter boss pattern watchdog cleaned up {profile.PatternId}.", this);
        ForceCleanup(true);
    }

    private IEnumerator EnhancedComboRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        for (int strike = 1; strike <= 3 && isPatternRunning; strike++)
        {
            float multiplier = profile.DamageMultiplier * (strike == 3 ? 1.2f : 0.75f + strike * 0.08f);
            float knockback = strike == 3 ? profile.ExtraKnockbackPower + 8f : 0f;
            ArmProfileHitbox(profile, strike, multiplier, knockback);
            yield return new WaitForSeconds(strike == 3 ? 0.14f : 0.1f);
            profile.Hitbox?.Disarm();

            if (strike < 3)
            {
                yield return new WaitForSeconds(0.13f);
            }
        }

        yield return Recovery(profile, 0.65f);
    }

    private IEnumerator DoubleChargeRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        for (int charge = 0; charge < 2 && isPatternRunning; charge++)
        {
            movementDirection = ResolveDirectionToTarget();
            ApplyFacingAndHitboxPosition(profile);

            if (charge > 0)
            {
                yield return new WaitForSeconds(0.2f);
            }

            ArmProfileHitbox(
                profile,
                charge + 1,
                profile.DamageMultiplier,
                profile.ExtraKnockbackPower);
            yield return MoveHorizontalRoutine(
                profile,
                Mathf.Max(24f, profile.MovementSpeed),
                Mathf.Max(0.25f, profile.MovementDuration));
            profile.Hitbox?.Disarm();
            yield return new WaitForSeconds(0.18f);
        }

        yield return Recovery(profile, 0.8f);
    }

    private IEnumerator ThunderUppercutRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        float startY = body != null ? body.position.y : transform.position.y;

        ArmProfileHitbox(profile, 1, profile.DamageMultiplier, profile.ExtraKnockbackPower);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = 13f;
            body.linearVelocity = velocity;
        }

        yield return new WaitForSeconds(0.28f);
        profile.Hitbox?.Disarm();
        float elapsed = 0f;

        while (isPatternRunning && elapsed < 1.35f && !IsGroundedAfterAscent(startY))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        SpawnLingeringHazard(
            profile,
            transform.position + Vector3.down * 0.6f,
            new Vector2(2.4f, 0.45f),
            0.6f);
        yield return new WaitForSeconds(0.62f);
        yield return Recovery(profile, 0.65f);
    }

    private IEnumerator DarkGroundSlamRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        ArmProfileHitbox(profile, 1, profile.DamageMultiplier, profile.ExtraKnockbackPower);
        SpawnHorizontalProjectile(profile, -1f, 15f, 7f);
        SpawnHorizontalProjectile(profile, 1f, 15f, 7f);
        Record(hazardCounts, profile.PatternId);
        yield return new WaitForSeconds(0.18f);
        profile.Hitbox?.Disarm();
        cameraShakeHookCount++;
        yield return new WaitForSeconds(0.75f);
        yield return Recovery(profile, 0.9f);
    }

    private IEnumerator ShadowComboRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        Vector3 behind = FindSafeTeleportPosition(GetBehindTargetPosition(1.5f));
        TeleportOut(profile);
        yield return null;
        TeleportIn(profile, behind);
        ArmProfileHitbox(profile, 1, profile.DamageMultiplier, profile.ExtraKnockbackPower);
        yield return new WaitForSeconds(0.12f);
        profile.Hitbox?.Disarm();

        Vector3 opposite = FindSafeTeleportPosition(GetBehindTargetPosition(-1.5f));
        yield return new WaitForSeconds(Mathf.Min(0.15f, teleportWarningSeconds));
        TeleportOut(profile);
        yield return null;
        TeleportIn(profile, opposite);
        ArmProfileHitbox(profile, 2, profile.DamageMultiplier, profile.ExtraKnockbackPower);
        yield return new WaitForSeconds(0.12f);
        profile.Hitbox?.Disarm();

        Vector3 above = FindSafeTeleportPosition(
            currentTarget != null ? currentTarget.position + Vector3.up * 3.5f : transform.position);
        yield return new WaitForSeconds(Mathf.Min(0.15f, teleportWarningSeconds));
        TeleportOut(profile);
        yield return null;
        TeleportIn(profile, above);

        if (body != null)
        {
            body.linearVelocity = new Vector2(0f, -16f);
        }

        yield return new WaitForSeconds(0.18f);
        ArmProfileHitbox(
            profile,
            3,
            profile.DamageMultiplier * 1.35f,
            profile.ExtraKnockbackPower + 8f);
        yield return new WaitForSeconds(0.18f);
        profile.Hitbox?.Disarm();
        yield return Recovery(profile, 0.8f);
    }

    private IEnumerator LightningCastRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        for (int strike = 0; strike < 3 && isPatternRunning; strike++)
        {
            Vector3 strikePosition = currentTarget != null
                ? currentTarget.position
                : transform.position;
            strikePosition = ClampToArena(strikePosition);
            SpawnLingeringHazard(profile, strikePosition, new Vector2(1.65f, 1.9f), lingeringHazardSeconds);

            if (strike < 2)
            {
                yield return new WaitForSeconds(lightningIntervalSeconds);
            }
        }

        float waitStart = Time.time;

        while (isPatternRunning
            && ActiveLingeringHazardCount > 0
            && Time.time - waitStart < lingeringHazardSeconds + 0.2f)
        {
            yield return null;
        }

        yield return Recovery(profile, 0.55f);
    }

    private IEnumerator DarkWaveRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        movementDirection = ResolveDirectionToTarget();
        ApplyFacingAndHitboxPosition(profile);
        SpawnHorizontalProjectile(profile, movementDirection, 18f, Mathf.Max(9f, profile.TelegraphRange));
        yield return new WaitForSeconds(0.75f);
        yield return Recovery(profile, 0.7f);
    }

    private IEnumerator SoulBindRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        if (!CanMaintainSoulBind())
        {
            yield return Recovery(profile, 0.35f);
            yield break;
        }

        soulBindActive = true;
        float elapsed = 0f;

        while (isPatternRunning && soulBindActive && elapsed < 0.7f)
        {
            if (!CanMaintainSoulBind() || IsTargetDashing())
            {
                break;
            }

            PullBoundTarget();
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        ReleaseSoulBind();
        yield return new WaitForSeconds(0.2f);
        ArmProfileHitbox(
            profile,
            1,
            profile.DamageMultiplier * 1.25f,
            profile.ExtraKnockbackPower + 10f);
        yield return new WaitForSeconds(0.14f);
        profile.Hitbox?.Disarm();
        yield return Recovery(profile, 0.65f);
    }

    private IEnumerator UltimateRoutine(HWJ_FighterBossPhaseTwoPatternProfile profile)
    {
        yield return MoveToPositionRoutine(bossBrain != null
            ? bossBrain.BossRoomCenter
            : (Vector2)transform.position, 0.35f);

        Vector3 targetPosition = currentTarget != null ? currentTarget.position : transform.position;
        SpawnLingeringHazard(
            profile,
            ClampToArena(targetPosition + Vector3.left * 1.4f),
            new Vector2(1.5f, 1.9f),
            0.8f);
        SpawnLingeringHazard(
            profile,
            ClampToArena(targetPosition + Vector3.right * 1.4f),
            new Vector2(1.5f, 1.9f),
            0.8f);
        yield return new WaitForSeconds(0.7f);

        movementDirection = ResolveDirectionToTarget();
        ApplyFacingAndHitboxPosition(profile);
        ArmProfileHitbox(profile, 1, profile.DamageMultiplier, profile.ExtraKnockbackPower);
        yield return MoveHorizontalRoutine(profile, 31f, 0.32f);
        profile.Hitbox?.Disarm();

        if (body != null)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, 12f);
        }

        ArmProfileHitbox(profile, 2, profile.DamageMultiplier, profile.ExtraKnockbackPower);
        yield return new WaitForSeconds(0.28f);
        profile.Hitbox?.Disarm();

        if (currentTarget != null)
        {
            Vector3 chasePosition = FindSafeTeleportPosition(currentTarget.position + Vector3.up * 2.7f);
            yield return MoveToPositionRoutine(chasePosition, 0.25f);
        }

        if (body != null)
        {
            body.linearVelocity = new Vector2(0f, -18f);
        }

        yield return new WaitForSeconds(0.25f);
        ArmProfileHitbox(
            profile,
            3,
            profile.DamageMultiplier * 1.5f,
            profile.ExtraKnockbackPower + 12f);
        SpawnHorizontalProjectile(profile, -1f, 18f, ResolveMaximumTravel(profile));
        SpawnHorizontalProjectile(profile, 1f, 18f, ResolveMaximumTravel(profile));
        yield return new WaitForSeconds(0.2f);
        profile.Hitbox?.Disarm();
        cameraShakeHookCount++;
        yield return new WaitForSeconds(Mathf.Clamp(ultimateGroggySeconds, 1.2f, 1.5f));
        nextUltimateUseTime = Time.time + DefaultUltimateCooldownSeconds;
        yield return Recovery(profile, 0.1f);
    }

}
