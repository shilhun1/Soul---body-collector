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
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem :
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

}
