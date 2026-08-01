using System.Collections;
using UnityEngine;

/// <summary>
/// 1페이즈 철권 연타(P1_Attack_Combo) 한 패턴만 실행합니다.
/// 실제 타격 타이밍은 Animation Event가 전달하며, 이벤트가 누락되어도 watchdog이 Hitbox를 정리합니다.
/// </summary>
public class HWJ_FighterBossComboSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor,
    HWJ_IFighterBossAnimationEventReceiver
{
    private const string ComboPatternId = "P1_Attack_Combo";
    private const string ComboCastAnimatorState = "P1_Cast_Combo";
    private const string ComboAnimatorState = "P1_Attack_Combo";

    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_FighterBossHitboxSystem comboHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private SpriteRenderer facingRenderer;
    [SerializeField] private LineRenderer telegraphRenderer;
    [SerializeField] private Transform attackRoot;
    [SerializeField] private string executorKey = "fighter_boss_combo";
    [SerializeField, Min(0f)] private float castWaitSeconds = 0.65f;
    [SerializeField] private float watchdogSeconds = 2.25f;
    [SerializeField] private float rightHandLocalX = 1.05f;
    [SerializeField] private float rightHandLocalY = 0.95f;
    [SerializeField] private float firstDamageMultiplier = 0.75f;
    [SerializeField] private float secondDamageMultiplier = 0.85f;
    [SerializeField] private float finisherDamageMultiplier = 1.25f;
    [SerializeField] private float finisherExtraKnockbackPower = 12f;

    private Coroutine activeRoutine;
    private Transform currentTarget;
    private bool isPatternRunning;
    private bool isCasting;
    private bool isRecovering;
    private bool lastComboUsedAnimator;
    private int completedComboCount;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => isPatternRunning;
    public bool IsCasting => isCasting;
    public float CastWaitSeconds => castWaitSeconds;
    public bool IsRecovering => isRecovering;
    public bool LastComboUsedAnimator => lastComboUsedAnimator;
    public int CompletedComboCount => completedComboCount;
    public HWJ_FighterBossHitboxSystem ComboHitbox => comboHitbox;
    public bool IsTelegraphVisible => telegraphRenderer != null && telegraphRenderer.enabled;

    private void Awake()
    {
        CacheReferences();
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
        return CanUsePattern(pattern, target) && TryStartCombo(target);
    }

    /// <summary>
    /// AI, 디버그 도구, Play Mode 검증에서 같은 실행 경로를 사용하기 위한 진입점입니다.
    /// </summary>
    public bool TryStartCombo(Transform target)
    {
        CacheReferences();

        if (target == null
            || isPatternRunning
            || runtimeStatus != null && runtimeStatus.IsDead
            || bossBrain != null && !bossBrain.CanUsePhaseOneCombo)
        {
            return false;
        }

        currentTarget = target;
        isPatternRunning = true;
        isCasting = true;
        isRecovering = false;
        lastComboUsedAnimator = animatorSystem != null
            && animatorSystem.BeginCast(1, ComboCastAnimatorState);
        ApplyFacingToAttackRoot();
        comboHitbox?.Disarm();
        SetTelegraphVisible(false);
        bossBrain?.NotifyComboAttackStarted();

        activeRoutine = StartCoroutine(lastComboUsedAnimator
            ? CastThenAnimatorWatchdogRoutine()
            : FallbackComboRoutine());

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
        ApplyFacingToAttackRoot();
        // 이전 버전 Clip의 이벤트가 남아 있어도 선형 예고 표시는 다시 켜지지 않습니다.
        SetTelegraphVisible(false);
    }

    public void OnAnimationEnableHitbox(int strikeNumber)
    {
        SetTelegraphVisible(false);
        ApplyFacingToAttackRoot();

        switch (strikeNumber)
        {
            case 1:
                comboHitbox?.ArmStrike(1, firstDamageMultiplier, 0f);
                break;
            case 2:
                comboHitbox?.ArmStrike(2, secondDamageMultiplier, 0f);
                break;
            default:
                comboHitbox?.ArmStrike(3, finisherDamageMultiplier, finisherExtraKnockbackPower);
                break;
        }
    }

    public void OnAnimationDisableHitbox()
    {
        comboHitbox?.Disarm();
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
        comboHitbox?.Disarm();
        SetTelegraphVisible(false);
        isRecovering = true;
        bossBrain?.NotifyComboRecoveryStarted();
    }

    public void OnAnimationAttackEnd()
    {
        CompleteCombo();
    }

    private IEnumerator CastThenAnimatorWatchdogRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        if (animatorSystem == null || !animatorSystem.CommitAttack(ComboAnimatorState))
        {
            isPatternRunning = false;
            ForceCleanup(true);
            activeRoutine = null;
            yield break;
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

    private IEnumerator FallbackComboRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        OnAnimationAttackStart();
        yield return new WaitForSeconds(0.28f);
        OnAnimationEnableHitbox(1);
        yield return new WaitForSeconds(0.1f);
        OnAnimationDisableHitbox();
        yield return new WaitForSeconds(0.17f);
        OnAnimationEnableHitbox(2);
        yield return new WaitForSeconds(0.11f);
        OnAnimationDisableHitbox();
        yield return new WaitForSeconds(0.2f);
        OnAnimationEnableHitbox(3);
        yield return new WaitForSeconds(0.14f);
        OnAnimationDisableHitbox();
        OnAnimationRecoveryStart();
        yield return new WaitForSeconds(0.7f);
        CompleteCombo();
    }

    private void CompleteCombo()
    {
        if (!isPatternRunning)
        {
            ForceCleanup(false);
            return;
        }

        completedComboCount++;
        ForceCleanup(true);
        isPatternRunning = false;
        activeRoutine = null;
    }

    private void ForceCleanup(bool returnToIdle)
    {
        comboHitbox?.Disarm();
        SetTelegraphVisible(false);
        isCasting = false;
        isRecovering = false;
        currentTarget = null;

        if (returnToIdle)
        {
            bossBrain?.NotifyComboAttackEnded();
            animatorSystem?.EndAttack();

            if (animatorSystem == null && HasAnimatorState("Idle"))
            {
                animator.Play(GetFullPathHash("Idle"), 0, 0f);
            }
        }
        else
        {
            animatorSystem?.EndAttack(false);
        }
    }

    private void ApplyFacingToAttackRoot()
    {
        if (attackRoot == null)
        {
            return;
        }

        float facingSign = facingRenderer != null && facingRenderer.flipX ? -1f : 1f;
        attackRoot.localPosition = new Vector3(
            Mathf.Abs(rightHandLocalX) * facingSign,
            rightHandLocalY,
            attackRoot.localPosition.z);

        if (telegraphRenderer != null)
        {
            telegraphRenderer.useWorldSpace = false;
            telegraphRenderer.positionCount = 2;
            telegraphRenderer.SetPosition(0, new Vector3(0.15f * facingSign, rightHandLocalY, 0f));
            telegraphRenderer.SetPosition(1, new Vector3(2.1f * facingSign, rightHandLocalY, 0f));
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
        if (pattern == null)
        {
            return false;
        }

        return string.Equals(pattern.PatternId, ComboPatternId, System.StringComparison.Ordinal)
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

        if (facingRenderer == null)
        {
            facingRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (comboHitbox == null)
        {
            comboHitbox = GetComponentInChildren<HWJ_FighterBossHitboxSystem>(true);
        }

        if (attackRoot == null && comboHitbox != null)
        {
            attackRoot = comboHitbox.transform;
        }
    }
}
