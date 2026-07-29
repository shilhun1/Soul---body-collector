using System.Collections;
using UnityEngine;

/// <summary>
/// Executes the phase-one ground slam pattern.
/// A temporary ring previews the impact area before one wide ground hitbox is armed.
/// </summary>
public class HWJ_FighterBossGroundSlamSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor,
    HWJ_IFighterBossAnimationEventReceiver
{
    private const string PatternId = "P1_Attack_GroundSlam";
    private const string AnimatorState = "P1_Attack_GroundSlam";

    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_FighterBossHitboxSystem slamHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private LineRenderer telegraphRenderer;
    [SerializeField] private string executorKey = "fighter_boss_ground_slam";
    [SerializeField] private float watchdogSeconds = 2.1f;
    [SerializeField] private float telegraphRadius = 2.75f;
    [SerializeField] private float damageMultiplier = 1.8f;
    [SerializeField] private float extraKnockbackPower = 18f;

    private Coroutine activeRoutine;
    private bool isPatternRunning;
    private bool isRecovering;
    private bool lastGroundSlamUsedAnimator;
    private int completedGroundSlamCount;
    private int spawnedGroundHazardCount;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => isPatternRunning;
    public bool IsRecovering => isRecovering;
    public bool LastGroundSlamUsedAnimator => lastGroundSlamUsedAnimator;
    public int CompletedGroundSlamCount => completedGroundSlamCount;
    public int SpawnedGroundHazardCount => spawnedGroundHazardCount;
    public HWJ_FighterBossHitboxSystem SlamHitbox => slamHitbox;
    public bool IsTelegraphVisible => telegraphRenderer != null && telegraphRenderer.enabled;

    private void Awake()
    {
        CacheReferences();
        ConfigureTelegraph();
        ForceCleanup(false);
    }

    private void OnDisable()
    {
        CancelActivePattern();
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        return MatchesPattern(pattern)
            && target != null
            && !isPatternRunning
            && (runtimeStatus == null || !runtimeStatus.IsDead)
            && (bossBrain == null || bossBrain.CanUsePhaseOneCombo);
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        return CanUsePattern(pattern, target) && TryStartGroundSlam(target);
    }

    /// <summary>
    /// Entry point shared by boss AI and forced Play Mode verification.
    /// </summary>
    public bool TryStartGroundSlam(Transform target)
    {
        CacheReferences();

        if (target == null
            || isPatternRunning
            || runtimeStatus != null && runtimeStatus.IsDead
            || bossBrain != null && !bossBrain.CanUsePhaseOneCombo)
        {
            return false;
        }

        isPatternRunning = true;
        isRecovering = false;
        slamHitbox?.Disarm();
        ConfigureTelegraph();
        SetTelegraphVisible(false);
        bossBrain?.NotifyComboAttackStarted();
        lastGroundSlamUsedAnimator = animatorSystem != null
            ? animatorSystem.BeginAttack(4, AnimatorState)
            : HasAnimatorState(AnimatorState);
        activeRoutine = StartCoroutine(lastGroundSlamUsedAnimator
            ? AnimatorWatchdogRoutine()
            : FallbackGroundSlamRoutine());

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

        isPatternRunning = false;
        ForceCleanup(false);
    }

    public void OnAnimationAttackStart()
    {
        bossBrain?.NotifyComboAttackStarted();
    }

    public void OnAnimationTelegraphStart()
    {
        ConfigureTelegraph();
        SetTelegraphVisible(true);
    }

    public void OnAnimationEnableHitbox(int strikeNumber)
    {
        SetTelegraphVisible(false);
        slamHitbox?.ArmStrike(1, damageMultiplier, extraKnockbackPower);
    }

    public void OnAnimationDisableHitbox()
    {
        slamHitbox?.Disarm();
    }

    public void OnAnimationApplyMovement()
    {
    }

    public void OnAnimationStopMovement()
    {
    }

    public void OnAnimationSpawnProjectile()
    {
    }

    public void OnAnimationSpawnGroundHazard()
    {
        // Final ground VFX can subscribe at this hook without changing damage timing.
        spawnedGroundHazardCount++;
    }

    public void OnAnimationTeleportOut()
    {
    }

    public void OnAnimationTeleportIn()
    {
    }

    public void OnAnimationCameraShakeHook()
    {
    }

    public void OnAnimationSfxHook()
    {
    }

    public void OnAnimationRecoveryStart()
    {
        slamHitbox?.Disarm();
        SetTelegraphVisible(false);
        isRecovering = true;
        bossBrain?.NotifyComboRecoveryStarted();
    }

    public void OnAnimationAttackEnd()
    {
        CompleteGroundSlam();
    }

    private IEnumerator AnimatorWatchdogRoutine()
    {
        if (animatorSystem == null)
        {
            animator.Play(GetFullPathHash(AnimatorState), 0, 0f);
        }

        float elapsed = 0f;

        while (isPatternRunning && elapsed < Mathf.Max(0.1f, watchdogSeconds))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (isPatternRunning)
        {
            ForceCleanup(true);
            isPatternRunning = false;
            activeRoutine = null;
        }
    }

    private IEnumerator FallbackGroundSlamRoutine()
    {
        OnAnimationAttackStart();
        OnAnimationTelegraphStart();
        yield return new WaitForSeconds(0.6f);
        OnAnimationSpawnGroundHazard();
        OnAnimationEnableHitbox(1);
        yield return new WaitForSeconds(0.18f);
        OnAnimationDisableHitbox();
        OnAnimationCameraShakeHook();
        OnAnimationSfxHook();
        OnAnimationRecoveryStart();
        yield return new WaitForSeconds(0.82f);
        CompleteGroundSlam();
    }

    private void CompleteGroundSlam()
    {
        if (!isPatternRunning)
        {
            ForceCleanup(false);
            return;
        }

        completedGroundSlamCount++;
        ForceCleanup(true);
        isPatternRunning = false;
        activeRoutine = null;
    }

    private void ForceCleanup(bool returnToIdle)
    {
        slamHitbox?.Disarm();
        SetTelegraphVisible(false);
        isRecovering = false;

        if (!returnToIdle)
        {
            animatorSystem?.EndAttack(false);
            return;
        }

        bossBrain?.NotifyComboAttackEnded();
        animatorSystem?.EndAttack();

        if (animatorSystem == null && HasAnimatorState("Idle"))
        {
            animator.Play(GetFullPathHash("Idle"), 0, 0f);
        }
    }

    private void ConfigureTelegraph()
    {
        if (telegraphRenderer == null)
        {
            return;
        }

        const int segmentCount = 32;
        telegraphRenderer.useWorldSpace = false;
        telegraphRenderer.loop = true;
        telegraphRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            telegraphRenderer.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * telegraphRadius,
                    0.15f + Mathf.Sin(angle) * telegraphRadius * 0.22f,
                    0f));
        }
    }

    private void SetTelegraphVisible(bool visible)
    {
        if (telegraphRenderer != null)
        {
            telegraphRenderer.enabled = visible;
        }
    }

    private bool MatchesPattern(HWJ_BossPatternDataSO pattern)
    {
        return pattern != null
            && string.Equals(pattern.PatternId, PatternId, System.StringComparison.Ordinal)
            && string.Equals(pattern.CustomPatternExecutorKey, executorKey, System.StringComparison.Ordinal);
    }

    private bool HasAnimatorState(string stateName)
    {
        return animator != null
            && animator.runtimeAnimatorController != null
            && animator.HasState(0, GetFullPathHash(stateName));
    }

    private static int GetFullPathHash(string stateName)
    {
        return Animator.StringToHash($"Base Layer.{stateName}");
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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animatorSystem == null)
        {
            animatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (slamHitbox == null)
        {
            HWJ_FighterBossHitboxSystem[] hitboxes =
                GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null && hitboxes[i].name.Contains("GroundSlam"))
                {
                    slamHitbox = hitboxes[i];
                    break;
                }
            }
        }
    }
}
