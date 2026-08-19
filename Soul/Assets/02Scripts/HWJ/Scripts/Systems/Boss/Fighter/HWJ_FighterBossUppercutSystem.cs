using System.Collections;
using UnityEngine;

/// <summary>
/// Executes the phase-one uppercut pattern.
/// The Animator owns timing, while this component owns telegraph, hitbox, facing, and cleanup.
/// </summary>
public class HWJ_FighterBossUppercutSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor,
    HWJ_IFighterBossAnimationEventReceiver
{
    private const string PatternId = "P1_Attack_Uppercut";
    private const string CastAnimatorState = "P1_Cast_Uppercut";
    private const string AnimatorState = "P1_Attack_Uppercut";

    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_FighterBossHitboxSystem uppercutHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private SpriteRenderer facingRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform uppercutRoot;
    [SerializeField] private LineRenderer telegraphRenderer;
    [SerializeField] private string executorKey = "fighter_boss_uppercut";
    [SerializeField, Min(0f)] private float castWaitSeconds = 0.65f;
    [SerializeField] private float watchdogSeconds = 1.85f;
    [SerializeField] private float hitboxLocalX = 0.78f;
    [SerializeField] private float hitboxLocalY = 1.55f;
    [SerializeField] private float damageMultiplier = 1.65f;
    [SerializeField] private float extraKnockbackPower = 14f;
    [SerializeField] private float riseVelocity = 11f;
    [SerializeField] private float maximumRiseSeconds = 0.4f;
    [SerializeField] private float maximumAirSeconds = 1.5f;

    private Coroutine activeRoutine;
    private Coroutine movementRoutine;
    private Transform currentTarget;
    private bool isPatternRunning;
    private bool isCasting;
    private bool isRecovering;
    private bool lastUppercutUsedAnimator;
    private bool animationEnded;
    private bool lastUppercutLanded;
    private float lastUppercutRiseHeight;
    private float uppercutStartY;
    private float attackDirection = 1f;
    private int completedUppercutCount;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => isPatternRunning;
    public bool IsCasting => isCasting;
    public float CastWaitSeconds => castWaitSeconds;
    public bool IsRecovering => isRecovering;
    public bool LastUppercutUsedAnimator => lastUppercutUsedAnimator;
    public int CompletedUppercutCount => completedUppercutCount;
    public bool LastUppercutLanded => lastUppercutLanded;
    public float LastUppercutRiseHeight => lastUppercutRiseHeight;
    public HWJ_FighterBossHitboxSystem UppercutHitbox => uppercutHitbox;
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
        return CanUsePattern(pattern, target) && TryStartUppercut(target);
    }

    /// <summary>
    /// Entry point shared by boss AI and forced Play Mode verification.
    /// </summary>
    public bool TryStartUppercut(Transform target)
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
        animationEnded = false;
        lastUppercutLanded = false;
        lastUppercutRiseHeight = 0f;
        uppercutStartY = transform.position.y;
        attackDirection = Mathf.Sign(target.position.x - transform.position.x);

        if (Mathf.Approximately(attackDirection, 0f))
        {
            attackDirection = facingRenderer != null && facingRenderer.flipX ? -1f : 1f;
        }

        ApplyFacing();
        uppercutHitbox?.Disarm();
        SetTelegraphVisible(false);
        bossBrain?.NotifyComboAttackStarted();
        lastUppercutUsedAnimator = animatorSystem != null
            && animatorSystem.BeginCast(3, CastAnimatorState);
        activeRoutine = StartCoroutine(lastUppercutUsedAnimator
            ? CastThenAnimatorWatchdogRoutine()
            : FallbackUppercutRoutine());

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

        StopUppercutMovement();
        isPatternRunning = false;
        ForceCleanup(false);
    }

    public void OnAnimationAttackStart()
    {
        bossBrain?.NotifyComboAttackStarted();
    }

    public void OnAnimationTelegraphStart()
    {
        ApplyFacing();
        // 보스의 준비 자세만 보여주고 기존 궤적 예고선은 사용하지 않습니다.
        SetTelegraphVisible(false);
    }

    public void OnAnimationEnableHitbox(int strikeNumber)
    {
        ApplyFacing();
        SetTelegraphVisible(false);
        uppercutHitbox?.ArmStrike(1, damageMultiplier, extraKnockbackPower);
    }

    public void OnAnimationDisableHitbox()
    {
        uppercutHitbox?.Disarm();
    }

    public void OnAnimationApplyMovement()
    {
        StopUppercutMovement();
        movementRoutine = StartCoroutine(UppercutMovementRoutine());
    }

    public void OnAnimationStopMovement()
    {
        uppercutHitbox?.Disarm();
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
        uppercutHitbox?.Disarm();
        SetTelegraphVisible(false);
        isRecovering = true;
        bossBrain?.NotifyComboRecoveryStarted();
    }

    public void OnAnimationAttackEnd()
    {
        animationEnded = true;

        if (movementRoutine == null || lastUppercutLanded)
        {
            CompleteUppercut();
        }
    }

    private IEnumerator CastThenAnimatorWatchdogRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        if (animatorSystem == null || !animatorSystem.CommitAttack(AnimatorState))
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

    private IEnumerator FallbackUppercutRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        OnAnimationAttackStart();
        yield return new WaitForSeconds(0.4f);
        OnAnimationEnableHitbox(1);
        yield return new WaitForSeconds(0.06f);
        OnAnimationApplyMovement();
        yield return new WaitForSeconds(0.28f);
        OnAnimationDisableHitbox();
        OnAnimationCameraShakeHook();
        OnAnimationSfxHook();
        OnAnimationRecoveryStart();
        float waitStart = Time.time;

        while (!lastUppercutLanded && Time.time - waitStart < maximumAirSeconds)
        {
            yield return null;
        }

        CompleteUppercut();
    }

    private IEnumerator UppercutMovementRoutine()
    {
        if (body == null)
        {
            lastUppercutLanded = true;
            movementRoutine = null;
            yield break;
        }

        uppercutStartY = body.position.y;
        Vector2 velocity = body.linearVelocity;
        velocity.y = Mathf.Max(0.1f, riseVelocity);
        body.linearVelocity = velocity;
        float elapsed = 0f;

        // The Rigidbody performs the ascent; a ceiling collision naturally stops it.
        while (isPatternRunning
            && elapsed < Mathf.Max(0.05f, maximumRiseSeconds)
            && body.linearVelocity.y > 0.01f)
        {
            lastUppercutRiseHeight = Mathf.Max(
                lastUppercutRiseHeight,
                body.position.y - uppercutStartY);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        uppercutHitbox?.Disarm();
        elapsed = 0f;

        while (isPatternRunning && elapsed < Mathf.Max(0.1f, maximumAirSeconds))
        {
            lastUppercutRiseHeight = Mathf.Max(
                lastUppercutRiseHeight,
                body.position.y - uppercutStartY);

            if (body.linearVelocity.y <= 0.01f && IsTouchingGround())
            {
                lastUppercutLanded = true;
                break;
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        lastUppercutLanded = lastUppercutLanded || IsTouchingGround();
        movementRoutine = null;

        if (animationEnded && isPatternRunning)
        {
            CompleteUppercut();
        }
    }

    private bool IsTouchingGround()
    {
        if (bodyCollider == null)
        {
            return body == null || body.position.y <= uppercutStartY + 0.08f;
        }

        Physics2D.SyncTransforms();
        Bounds bounds = bodyCollider.bounds;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bounds.center,
            new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.8f), 0.08f),
            0f,
            Vector2.down,
            Mathf.Max(0.08f, bounds.extents.y + 0.1f));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;

            if (hit != null
                && !hit.isTrigger
                && !hit.transform.IsChildOf(transform)
                && !transform.IsChildOf(hit.transform)
                && hits[i].normal.y > 0.45f)
            {
                return true;
            }
        }

        return body.position.y <= uppercutStartY + 0.08f && body.linearVelocity.y <= 0f;
    }

    private void CompleteUppercut()
    {
        if (!isPatternRunning)
        {
            ForceCleanup(false);
            return;
        }

        completedUppercutCount++;
        ForceCleanup(true);
        isPatternRunning = false;
        activeRoutine = null;
    }

    private void ForceCleanup(bool returnToIdle)
    {
        StopUppercutMovement();
        uppercutHitbox?.Disarm();
        SetTelegraphVisible(false);
        isCasting = false;
        isRecovering = false;
        currentTarget = null;

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

    private void StopUppercutMovement()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        if (body != null && body.linearVelocity.y > 0f)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;
        }
    }

    private void ApplyFacing()
    {
        if (facingRenderer != null)
        {
            facingRenderer.flipX = attackDirection < 0f;
        }

        if (uppercutRoot != null)
        {
            uppercutRoot.localPosition = new Vector3(
                Mathf.Abs(hitboxLocalX) * attackDirection,
                hitboxLocalY,
                uppercutRoot.localPosition.z);
        }

        if (telegraphRenderer != null)
        {
            telegraphRenderer.useWorldSpace = false;
            telegraphRenderer.positionCount = 3;
            telegraphRenderer.SetPosition(0, new Vector3(0.2f * attackDirection, 0.15f, 0f));
            telegraphRenderer.SetPosition(1, new Vector3(0.85f * attackDirection, 1.25f, 0f));
            telegraphRenderer.SetPosition(2, new Vector3(0.55f * attackDirection, 2.75f, 0f));
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

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (facingRenderer == null)
        {
            facingRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (uppercutHitbox == null)
        {
            HWJ_FighterBossHitboxSystem[] hitboxes =
                GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null && hitboxes[i].name.Contains("Uppercut"))
                {
                    uppercutHitbox = hitboxes[i];
                    break;
                }
            }
        }

        if (uppercutRoot == null && uppercutHitbox != null)
        {
            uppercutRoot = uppercutHitbox.transform;
        }
    }
}
