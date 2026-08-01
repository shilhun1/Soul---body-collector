using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HWJ_FighterBossPhaseTwoPatternKind
{
    EnhancedCombo,
    DoubleCharge,
    ThunderUppercut,
    DarkGroundSlam,
    ShadowCombo,
    LightningCast,
    DarkWave,
    SoulBind,
    Ultimate
}

[Serializable]
public sealed class HWJ_FighterBossPhaseTwoPatternProfile
{
    [SerializeField] private string patternId;
    [SerializeField] private string animatorState;
    [SerializeField] private HWJ_FighterBossPhaseTwoPatternKind patternKind;
    [SerializeField] private HWJ_FighterBossHitboxSystem hitbox;
    [SerializeField] private Transform hitboxRoot;
    [SerializeField] private LineRenderer telegraphRenderer;
    [SerializeField] private float hitboxLocalX = 1f;
    [SerializeField] private float hitboxLocalY = 1f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float extraKnockbackPower = 10f;
    [SerializeField, Min(0f)] private float castWaitSeconds = 0.7f;
    [SerializeField] private float watchdogSeconds = 4f;
    [SerializeField] private float movementSpeed = 30f;
    [SerializeField] private float movementDuration = 0.45f;
    [SerializeField] private float arenaWidthTravelRatio = 0.5f;
    [SerializeField] private float telegraphRange = 5f;

    public string PatternId => patternId;
    public string AnimatorState => animatorState;
    public HWJ_FighterBossPhaseTwoPatternKind PatternKind => patternKind;
    public HWJ_FighterBossHitboxSystem Hitbox => hitbox;
    public Transform HitboxRoot => hitboxRoot;
    public LineRenderer TelegraphRenderer => telegraphRenderer;
    public float HitboxLocalX => hitboxLocalX;
    public float HitboxLocalY => hitboxLocalY;
    public float DamageMultiplier => damageMultiplier;
    public float ExtraKnockbackPower => extraKnockbackPower;
    public float CastWaitSeconds => castWaitSeconds;
    public float WatchdogSeconds => watchdogSeconds;
    public float MovementSpeed => movementSpeed;
    public float MovementDuration => movementDuration;
    public float ArenaWidthTravelRatio => arenaWidthTravelRatio;
    public float TelegraphRange => telegraphRange;
}

/// <summary>
/// Executes all nine phase-two fighter patterns.
/// Animation clips provide presentation, while this component owns movement, hitboxes,
/// pooled projectiles, lingering hazards, SoulBind, cancellation, and watchdog cleanup.
/// </summary>
[DisallowMultipleComponent]
public sealed class HWJ_FighterBossPhaseTwoPatternSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor,
    HWJ_IFighterBossAnimationEventReceiver
{
    private const string PhaseTwoExecutorKey = "fighter_boss_phase_two";
    private const float DefaultUltimateCooldownSeconds = 18f;

    private sealed class RuntimeAttackObject
    {
        public GameObject Root;
        public BoxCollider2D Collider;
        public HWJ_FighterBossHitboxSystem Hitbox;
        public LineRenderer Line;
        public bool IsHazard;
        public bool InUse;
    }

    [Header("Core References")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer facingRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private HWJ_FighterBossPhaseTwoPatternProfile[] profiles;

    [Header("Shared Runtime Tuning")]
    [SerializeField] private float teleportWarningSeconds = 0.3f;
    [SerializeField] private float lightningIntervalSeconds = 0.45f;
    [SerializeField] private float lingeringHazardSeconds = 1.5f;
    [SerializeField] private float soulBindMaximumDistance = 9f;
    [SerializeField] private float soulBindPullSpeed = 11f;
    [SerializeField] private float ultimateMinimumPhaseSeconds = 10f;
    [SerializeField] private float ultimateMaximumHpRatio = 0.35f;
    [SerializeField] private float ultimateGroggySeconds = 1.35f;

    private readonly Dictionary<string, int> completedCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> projectileCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> hazardCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> teleportOutCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> teleportInCounts = new Dictionary<string, int>();
    private readonly List<RuntimeAttackObject> runtimeObjectPool = new List<RuntimeAttackObject>();
    private readonly List<Coroutine> runtimeEffectRoutines = new List<Coroutine>();

    private Coroutine activeRoutine;
    private Coroutine movementRoutine;
    private Coroutine watchdogRoutine;
    private HWJ_FighterBossPhaseTwoPatternProfile activeProfile;
    private Transform currentTarget;
    private bool isPatternRunning;
    private bool isCasting;
    private bool isRecovering;
    private bool lastPatternUsedAnimator;
    private bool lastMovementStoppedByWall;
    private bool soulBindActive;
    private bool teleportPresentationHidden;
    private bool bodyColliderEnabledBeforeTeleport;
    private float movementDirection = 1f;
    private float lastMovementDistance;
    private float phaseTwoStartedAt = -1f;
    private float nextUltimateUseTime;
    private Vector3 pendingTeleportPosition;
    private Vector3 activeHitboxOriginalLocalPosition;
    private bool hasActiveHitboxOriginalPosition;
    private int cameraShakeHookCount;
    private int sfxHookCount;

    public string ExecutorKey => PhaseTwoExecutorKey;
    public bool IsPatternRunning => isPatternRunning;
    public bool IsCasting => isCasting;
    public bool IsRecovering => isRecovering;
    public bool LastPatternUsedAnimator => lastPatternUsedAnimator;
    public bool LastMovementStoppedByWall => lastMovementStoppedByWall;
    public float LastMovementDistance => lastMovementDistance;
    public Vector3 LastTeleportLandingPosition { get; private set; }
    public string ActivePatternId => activeProfile != null ? activeProfile.PatternId : string.Empty;
    public int CameraShakeHookCount => cameraShakeHookCount;
    public int SfxHookCount => sfxHookCount;
    public bool IsSoulBindActive => soulBindActive;
    public int ActiveProjectileCount => CountRuntimeObjects(false);
    public int ActiveLingeringHazardCount => CountRuntimeObjects(true);
    public float PhaseTwoElapsedSeconds => phaseTwoStartedAt < 0f ? 0f : Time.time - phaseTwoStartedAt;

    private void Awake()
    {
        CacheReferences();
        ForceCleanup(false);
    }

    private void Update()
    {
        CacheReferences();

        if (bossBrain != null
            && bossBrain.FighterPhase == HWJ_FighterBossPhase.Phase2
            && phaseTwoStartedAt < 0f)
        {
            phaseTwoStartedAt = Time.time;
        }
    }

    private void OnDisable()
    {
        CancelActivePattern();
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (pattern == null
            || target == null
            || isPatternRunning
            || runtimeStatus != null && runtimeStatus.IsDead
            || bossBrain != null && !bossBrain.CanUsePhaseTwoPatterns
            || !string.Equals(
                pattern.CustomPatternExecutorKey,
                PhaseTwoExecutorKey,
                StringComparison.Ordinal))
        {
            return false;
        }

        HWJ_FighterBossPhaseTwoPatternProfile profile = FindProfile(pattern.PatternId);

        if (profile == null)
        {
            return false;
        }

        float hpRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? runtimeStatus.CurrentHp / runtimeStatus.MaxHp
            : 1f;

        switch (profile.PatternKind)
        {
            case HWJ_FighterBossPhaseTwoPatternKind.ShadowCombo:
            case HWJ_FighterBossPhaseTwoPatternKind.LightningCast:
            case HWJ_FighterBossPhaseTwoPatternKind.SoulBind:
                if (hpRatio > 0.7f)
                {
                    return false;
                }
                break;
            case HWJ_FighterBossPhaseTwoPatternKind.Ultimate:
                return hpRatio <= ultimateMaximumHpRatio
                    && PhaseTwoElapsedSeconds >= ultimateMinimumPhaseSeconds
                    && Time.time >= nextUltimateUseTime;
        }

        return profile.PatternKind != HWJ_FighterBossPhaseTwoPatternKind.LightningCast
            || ActiveLingeringHazardCount < 2;
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        return CanUsePattern(pattern, target) && TryStartPattern(pattern.PatternId, target);
    }

    /// <summary>
    /// Force-test entry point. AI conditions are intentionally evaluated by CanUsePattern,
    /// while this method only requires a valid phase-two profile and a target.
    /// </summary>
    public bool TryStartPattern(string patternId, Transform target)
    {
        CacheReferences();
        HWJ_FighterBossPhaseTwoPatternProfile profile = FindProfile(patternId);

        if (profile == null
            || target == null
            || isPatternRunning
            || runtimeStatus != null && runtimeStatus.IsDead
            || bossBrain != null && !bossBrain.CanUsePhaseTwoPatterns)
        {
            return false;
        }

        activeProfile = profile;
        currentTarget = target;
        isPatternRunning = true;
        isCasting = true;
        isRecovering = false;
        lastMovementStoppedByWall = false;
        lastMovementDistance = 0f;
        movementDirection = ResolveDirectionToTarget();
        activeHitboxOriginalLocalPosition = profile.HitboxRoot != null
            ? profile.HitboxRoot.localPosition
            : Vector3.zero;
        hasActiveHitboxOriginalPosition = profile.HitboxRoot != null;
        ApplyFacingAndHitboxPosition(profile);
        profile.Hitbox?.Disarm();
        SetTelegraphVisible(profile, false);
        bossBrain?.NotifyFighterPatternAttackStarted();

        int attackId = GetAttackId(profile.PatternKind);
        string castStateName = GetCastStateName(profile.PatternKind);
        lastPatternUsedAnimator = animatorSystem != null
            ? animatorSystem.BeginCast(attackId, castStateName)
            : PlayLegacyAnimatorState(castStateName);
        activeRoutine = StartCoroutine(CastThenRunPatternSequence(profile));
        watchdogRoutine = StartCoroutine(PatternWatchdogRoutine(profile));

        if (activeRoutine == null)
        {
            isPatternRunning = false;
            ForceCleanup(false);
            return false;
        }

        return true;
    }

    public void CancelActivePattern()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        StopWatchdog();

        isPatternRunning = false;
        ForceCleanup(false);
    }

    public void ForceCleanupAttackObjects()
    {
        CancelActivePattern();
    }

    public void MarkPhaseTwoStarted()
    {
        phaseTwoStartedAt = Time.time;
    }

    public int GetCompletedCount(string patternId) =>
        GetCount(completedCounts, NormalizePatternId(patternId));

    public int GetProjectileCount(string patternId) =>
        GetCount(projectileCounts, NormalizePatternId(patternId));

    public int GetGroundHazardCount(string patternId) =>
        GetCount(hazardCounts, NormalizePatternId(patternId));

    public int GetTeleportOutCount(string patternId) =>
        GetCount(teleportOutCounts, NormalizePatternId(patternId));

    public int GetTeleportInCount(string patternId) =>
        GetCount(teleportInCounts, NormalizePatternId(patternId));

    public HWJ_FighterBossHitboxSystem GetHitbox(string patternId)
    {
        return FindProfile(patternId)?.Hitbox;
    }

    public bool IsTelegraphVisible(string patternId)
    {
        LineRenderer telegraph = FindProfile(patternId)?.TelegraphRenderer;
        return telegraph != null && telegraph.enabled;
    }

    // P2 mechanics are sequence-driven. Events remain valid hooks and cannot finish a pattern early.
    public void OnAnimationAttackStart()
    {
        bossBrain?.NotifyFighterPatternAttackStarted();
    }

    public void OnAnimationTelegraphStart()
    {
        // 이전 Animation Clip 이벤트와 호환은 유지하되 공격 예고선은 표시하지 않습니다.
        SetTelegraphVisible(activeProfile, false);
    }

    public void OnAnimationEnableHitbox(int strikeNumber)
    {
    }

    public void OnAnimationDisableHitbox()
    {
    }

    public void OnAnimationApplyMovement()
    {
    }

    public void OnAnimationStopMovement()
    {
        StopHorizontalMovement();
    }

    public void OnAnimationSpawnProjectile()
    {
    }

    public void OnAnimationSpawnGroundHazard()
    {
    }

    public void OnAnimationTeleportOut()
    {
    }

    public void OnAnimationTeleportIn()
    {
    }

    public void OnAnimationCameraShakeHook()
    {
        cameraShakeHookCount++;
    }

    public void OnAnimationSfxHook()
    {
        sfxHookCount++;
    }

    public void OnAnimationRecoveryStart()
    {
    }

    public void OnAnimationAttackEnd()
    {
    }

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

    private void SpawnHorizontalProjectile(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        float direction,
        float speed,
        float maximumDistance)
    {
        Vector3 start = transform.position + new Vector3(direction * 1.1f, 0.9f, 0f);
        RuntimeAttackObject projectile = AcquireRuntimeObject(
            false,
            start,
            new Vector2(1.1f, 0.35f),
            new Color(0.25f, 0.55f, 1f, 0.95f));
        projectile.Hitbox.ArmStrike(
            1,
            activeProfile != null ? activeProfile.DamageMultiplier : 1f,
            activeProfile != null ? activeProfile.ExtraKnockbackPower : 0f);
        Record(projectileCounts, profile.PatternId);
        Coroutine routine = StartCoroutine(ProjectileRoutine(
            projectile,
            Mathf.Sign(direction),
            Mathf.Max(1f, speed),
            Mathf.Max(1f, maximumDistance)));
        runtimeEffectRoutines.Add(routine);
    }

    private IEnumerator ProjectileRoutine(
        RuntimeAttackObject projectile,
        float direction,
        float speed,
        float maximumDistance)
    {
        float distance = 0f;

        while (projectile.InUse && distance < maximumDistance)
        {
            float step = speed * Time.fixedDeltaTime;

            if (RuntimeObjectHitsWall(projectile, direction, step))
            {
                break;
            }

            projectile.Root.transform.position += Vector3.right * direction * step;
            distance += step;
            yield return new WaitForFixedUpdate();
        }

        ReleaseRuntimeObject(projectile);
    }

    private void SpawnLingeringHazard(
        HWJ_FighterBossPhaseTwoPatternProfile profile,
        Vector3 position,
        Vector2 size,
        float duration)
    {
        while (ActiveLingeringHazardCount >= 2)
        {
            RuntimeAttackObject oldest = FindFirstActiveHazard();

            if (oldest == null)
            {
                break;
            }

            ReleaseRuntimeObject(oldest);
        }

        RuntimeAttackObject hazard = AcquireRuntimeObject(
            true,
            ClampToArena(position),
            size,
            new Color(0.45f, 0.3f, 1f, 0.9f));
        hazard.Hitbox.ArmStrike(
            1,
            profile.DamageMultiplier * 0.8f,
            profile.ExtraKnockbackPower * 0.35f);
        Record(hazardCounts, profile.PatternId);
        Coroutine routine = StartCoroutine(HazardRoutine(hazard, duration));
        runtimeEffectRoutines.Add(routine);
    }

    private IEnumerator HazardRoutine(RuntimeAttackObject hazard, float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        ReleaseRuntimeObject(hazard);
    }

    private RuntimeAttackObject AcquireRuntimeObject(
        bool isHazard,
        Vector3 position,
        Vector2 size,
        Color color)
    {
        RuntimeAttackObject runtimeObject = null;

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (!runtimeObjectPool[i].InUse)
            {
                runtimeObject = runtimeObjectPool[i];
                break;
            }
        }

        if (runtimeObject == null)
        {
            GameObject root = new GameObject("HWJ_FighterBoss_RuntimeAttack");
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.enabled = false;
            HWJ_FighterBossHitboxSystem hitbox = root.AddComponent<HWJ_FighterBossHitboxSystem>();
            hitbox.Configure(combatSystem, collider);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = 2;
            line.widthMultiplier = 0.1f;
            line.numCapVertices = 2;
            line.sortingOrder = facingRenderer != null ? facingRenderer.sortingOrder + 1 : 11;

            Material material = ResolveTelegraphMaterial();

            if (material != null)
            {
                line.sharedMaterial = material;
            }

            runtimeObject = new RuntimeAttackObject
            {
                Root = root,
                Collider = collider,
                Hitbox = hitbox,
                Line = line
            };
            runtimeObjectPool.Add(runtimeObject);
        }

        runtimeObject.IsHazard = isHazard;
        runtimeObject.InUse = true;
        runtimeObject.Root.name = isHazard
            ? "TEMP_HAZARD_FighterBoss"
            : "TEMP_PROJECTILE_FighterBoss";
        runtimeObject.Root.layer = gameObject.layer;
        runtimeObject.Root.transform.position = position;
        runtimeObject.Root.transform.rotation = Quaternion.identity;
        runtimeObject.Root.transform.localScale = Vector3.one;
        runtimeObject.Root.SetActive(true);
        runtimeObject.Collider.size = size;
        runtimeObject.Collider.offset = Vector2.zero;
        runtimeObject.Hitbox.Configure(combatSystem, runtimeObject.Collider);
        ConfigureRuntimeLine(runtimeObject, size, color);
        return runtimeObject;
    }

    private void ConfigureRuntimeLine(RuntimeAttackObject runtimeObject, Vector2 size, Color color)
    {
        if (runtimeObject.Line == null)
        {
            return;
        }

        runtimeObject.Line.startColor = color;
        runtimeObject.Line.endColor = color;
        runtimeObject.Line.widthMultiplier = Mathf.Max(0.08f, size.y * 0.3f);

        if (runtimeObject.IsHazard)
        {
            const int segments = 24;
            runtimeObject.Line.loop = true;
            runtimeObject.Line.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                runtimeObject.Line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * size.x * 0.5f,
                        Mathf.Sin(angle) * size.y * 0.5f,
                        0f));
            }
        }
        else
        {
            runtimeObject.Line.loop = false;
            runtimeObject.Line.positionCount = 2;
            runtimeObject.Line.SetPosition(0, new Vector3(-size.x * 0.5f, 0f, 0f));
            runtimeObject.Line.SetPosition(1, new Vector3(size.x * 0.5f, 0f, 0f));
        }

        runtimeObject.Line.enabled = true;
    }

    private void ReleaseRuntimeObject(RuntimeAttackObject runtimeObject)
    {
        if (runtimeObject == null || !runtimeObject.InUse)
        {
            return;
        }

        runtimeObject.Hitbox?.Disarm();

        if (runtimeObject.Line != null)
        {
            runtimeObject.Line.enabled = false;
        }

        runtimeObject.InUse = false;
        runtimeObject.Root.SetActive(false);
    }

    private void StopAndReleaseRuntimeObjects()
    {
        for (int i = 0; i < runtimeEffectRoutines.Count; i++)
        {
            if (runtimeEffectRoutines[i] != null)
            {
                StopCoroutine(runtimeEffectRoutines[i]);
            }
        }

        runtimeEffectRoutines.Clear();

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            ReleaseRuntimeObject(runtimeObjectPool[i]);
        }
    }

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

    private HWJ_FighterBossPhaseTwoPatternProfile FindProfile(string patternId)
    {
        string normalized = NormalizePatternId(patternId);

        if (profiles == null)
        {
            return null;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            HWJ_FighterBossPhaseTwoPatternProfile profile = profiles[i];

            if (profile != null
                && string.Equals(
                    NormalizePatternId(profile.PatternId),
                    normalized,
                    StringComparison.Ordinal))
            {
                return profile;
            }
        }

        return null;
    }

    private bool PlayLegacyAnimatorState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        string[] candidates =
        {
            $"Base Layer.Phase2Attacks.{stateName}",
            $"Base Layer.{stateName}"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            int hash = Animator.StringToHash(candidates[i]);

            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                return true;
            }
        }

        return false;
    }

    private Material ResolveTelegraphMaterial()
    {
        if (profiles == null)
        {
            return null;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i]?.TelegraphRenderer != null
                && profiles[i].TelegraphRenderer.sharedMaterial != null)
            {
                return profiles[i].TelegraphRenderer.sharedMaterial;
            }
        }

        return null;
    }

    private int CountRuntimeObjects(bool hazards)
    {
        int count = 0;

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (runtimeObjectPool[i].InUse && runtimeObjectPool[i].IsHazard == hazards)
            {
                count++;
            }
        }

        return count;
    }

    private RuntimeAttackObject FindFirstActiveHazard()
    {
        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (runtimeObjectPool[i].InUse && runtimeObjectPool[i].IsHazard)
            {
                return runtimeObjectPool[i];
            }
        }

        return null;
    }

    private static int GetAttackId(HWJ_FighterBossPhaseTwoPatternKind kind)
    {
        return 11 + (int)kind;
    }

    private static string GetCastStateName(HWJ_FighterBossPhaseTwoPatternKind kind)
    {
        return kind switch
        {
            HWJ_FighterBossPhaseTwoPatternKind.EnhancedCombo => "P2_Cast_EnhancedCombo",
            HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge => "P2_Cast_DoubleCharge",
            HWJ_FighterBossPhaseTwoPatternKind.ThunderUppercut => "P2_Cast_ThunderUppercut",
            HWJ_FighterBossPhaseTwoPatternKind.DarkGroundSlam => "P2_Cast_DarkGroundSlam",
            HWJ_FighterBossPhaseTwoPatternKind.ShadowCombo => "P2_Cast_ShadowCombo",
            HWJ_FighterBossPhaseTwoPatternKind.LightningCast => "P2_Cast_LightningCast",
            HWJ_FighterBossPhaseTwoPatternKind.DarkWave => "P2_Cast_DarkWave",
            HWJ_FighterBossPhaseTwoPatternKind.SoulBind => "P2_Cast_SoulBind",
            HWJ_FighterBossPhaseTwoPatternKind.Ultimate => "P2_Cast_Ultimate",
            _ => string.Empty
        };
    }

    private static int GetCount(Dictionary<string, int> source, string key)
    {
        return !string.IsNullOrEmpty(key) && source.TryGetValue(key, out int count)
            ? count
            : 0;
    }

    private static void Record(Dictionary<string, int> destination, string patternId)
    {
        string key = NormalizePatternId(patternId);

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        destination[key] = GetCount(destination, key) + 1;
    }

    private static string NormalizePatternId(string patternId)
    {
        switch (patternId)
        {
            case "P2_Enhanced_Combo":
                return "P2_EnhancedCombo";
            case "P2_Enhanced_Charge":
                return "P2_DoubleCharge";
            case "P2_Enhanced_Uppercut":
                return "P2_ThunderUppercut";
            case "P2_Enhanced_GroundSlam":
                return "P2_DarkGroundSlam";
            case "P2_Attack_Shockwave":
                return "P2_ShadowCombo";
            case "P2_Attack_AerialDive":
                return "P2_LightningCast";
            case "P2_Attack_CrossSlash":
                return "P2_DarkWave";
            case "P2_Attack_PhantomRush":
                return "P2_SoulBind";
            case "P2_Attack_Execution":
                return "P2_Ultimate";
            default:
                return patternId;
        }
    }

    private void CacheReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (animatorSystem == null)
        {
            animatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (facingRenderer == null)
        {
            facingRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }
    }
}
