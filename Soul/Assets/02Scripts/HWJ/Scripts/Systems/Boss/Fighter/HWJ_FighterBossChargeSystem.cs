using System.Collections;
using UnityEngine;

/// <summary>
/// 1페이즈 파쇄 돌진(P1_Attack_Charge)을 실행합니다.
/// 0.55초 텔레그래프 뒤 오른쪽 어깨 Hitbox를 켜고, 보스방 폭 기준 직선 돌진 후 0.8초 회복합니다.
/// </summary>
public class HWJ_FighterBossChargeSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor,
    HWJ_IFighterBossAnimationEventReceiver
{
    private const string ChargePatternId = "P1_Attack_Charge";
    private const string ChargeCastAnimatorState = "P1_Cast_Charge";
    private const string ChargeAnimatorState = "P1_Attack_Charge";

    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_FighterBossHitboxSystem chargeHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private SpriteRenderer facingRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Transform shoulderRoot;
    [SerializeField] private LineRenderer telegraphRenderer;
    [SerializeField] private string executorKey = "fighter_boss_charge";
    [SerializeField, Min(0f)] private float castWaitSeconds = 0.75f;
    [SerializeField] private float watchdogSeconds = 2.45f;
    [SerializeField] private float chargeDurationSeconds = 0.5f;
    [SerializeField] private float chargeSpeed = 34f;
    [SerializeField] private float arenaWidthTravelRatio = 0.48f;
    [SerializeField] private float shoulderLocalX = 0.92f;
    [SerializeField] private float shoulderLocalY = 1.02f;
    [SerializeField] private float damageMultiplier = 1.4f;
    [SerializeField] private float extraKnockbackPower = 16f;

    private Coroutine activeRoutine;
    private Coroutine movementRoutine;
    private Transform currentTarget;
    private bool isPatternRunning;
    private bool isCasting;
    private bool isRecovering;
    private bool lastChargeUsedAnimator;
    private bool lastChargeStoppedByWall;
    private float lastChargeDistance;
    private float chargeDirection = 1f;
    private int completedChargeCount;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => isPatternRunning;
    public bool IsCasting => isCasting;
    public float CastWaitSeconds => castWaitSeconds;
    public bool IsRecovering => isRecovering;
    public bool LastChargeUsedAnimator => lastChargeUsedAnimator;
    public bool LastChargeStoppedByWall => lastChargeStoppedByWall;
    public float LastChargeDistance => lastChargeDistance;
    public int CompletedChargeCount => completedChargeCount;
    public HWJ_FighterBossHitboxSystem ChargeHitbox => chargeHitbox;
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
        return CanUsePattern(pattern, target) && TryStartCharge(target);
    }

    /// <summary>
    /// AI와 Play Mode 검증이 공통으로 사용하는 Charge 진입점입니다.
    /// </summary>
    public bool TryStartCharge(Transform target)
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
        lastChargeDistance = 0f;
        lastChargeStoppedByWall = false;
        chargeDirection = Mathf.Sign(target.position.x - transform.position.x);

        if (Mathf.Approximately(chargeDirection, 0f))
        {
            chargeDirection = facingRenderer != null && facingRenderer.flipX ? -1f : 1f;
        }

        ApplyFacing();
        chargeHitbox?.Disarm();
        SetTelegraphVisible(false);
        bossBrain?.NotifyComboAttackStarted();
        lastChargeUsedAnimator = animatorSystem != null
            && animatorSystem.BeginCast(2, ChargeCastAnimatorState);
        activeRoutine = StartCoroutine(lastChargeUsedAnimator
            ? CastThenAnimatorWatchdogRoutine()
            : FallbackChargeRoutine());

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

        StopChargeMovement();
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
        // 시전 대기 모션이 공격 방향을 전달하므로 이전 선형 표시는 항상 숨깁니다.
        SetTelegraphVisible(false);
    }

    public void OnAnimationEnableHitbox(int strikeNumber)
    {
        SetTelegraphVisible(false);
        ApplyFacing();
        chargeHitbox?.ArmStrike(1, damageMultiplier, extraKnockbackPower);
    }

    public void OnAnimationDisableHitbox()
    {
        chargeHitbox?.Disarm();
    }

    public void OnAnimationApplyMovement()
    {
        StopChargeMovement();
        movementRoutine = StartCoroutine(ChargeMovementRoutine());
    }

    public void OnAnimationStopMovement()
    {
        StopChargeMovement();
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
        StopChargeMovement();
        chargeHitbox?.Disarm();
        SetTelegraphVisible(false);
        isRecovering = true;
        bossBrain?.NotifyComboRecoveryStarted();
    }

    public void OnAnimationAttackEnd()
    {
        CompleteCharge();
    }

    private IEnumerator CastThenAnimatorWatchdogRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        if (animatorSystem == null || !animatorSystem.CommitAttack(ChargeAnimatorState))
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

    private IEnumerator FallbackChargeRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, castWaitSeconds));
        isCasting = false;

        if (!isPatternRunning)
        {
            yield break;
        }

        OnAnimationAttackStart();
        yield return new WaitForSeconds(0.5f);
        OnAnimationEnableHitbox(1);
        yield return new WaitForSeconds(0.08f);
        OnAnimationApplyMovement();
        yield return new WaitForSeconds(0.52f);
        OnAnimationStopMovement();
        OnAnimationDisableHitbox();
        OnAnimationRecoveryStart();
        yield return new WaitForSeconds(0.8f);
        CompleteCharge();
    }

    private IEnumerator ChargeMovementRoutine()
    {
        float elapsed = 0f;
        float maxDistance = ResolveMaximumChargeDistance();

        while (isPatternRunning
            && elapsed < Mathf.Max(0.05f, chargeDurationSeconds)
            && lastChargeDistance < maxDistance)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            float step = Mathf.Min(
                Mathf.Max(0f, chargeSpeed) * Time.fixedDeltaTime,
                maxDistance - lastChargeDistance);

            if (step <= 0f)
            {
                break;
            }

            if (TryGetWallDistance(step, out float wallDistance))
            {
                float safeDistance = Mathf.Max(0f, wallDistance - 0.03f);
                MoveChargeBody(safeDistance);
                lastChargeDistance += safeDistance;
                lastChargeStoppedByWall = true;
                break;
            }

            MoveChargeBody(step);
            lastChargeDistance += step;
        }

        movementRoutine = null;
    }

    private bool TryGetWallDistance(float castDistance, out float wallDistance)
    {
        wallDistance = 0f;

        if (bodyCollider == null)
        {
            return false;
        }

        // Rigidbody2D.position is changed during the same FixedUpdate loop.
        // Sync before the cast so collider bounds use the latest charge position.
        Physics2D.SyncTransforms();
        Bounds bounds = bodyCollider.bounds;
        Vector2 castSize = new Vector2(
            Mathf.Max(0.1f, bounds.size.x * 0.82f),
            Mathf.Max(0.1f, bounds.size.y * 0.88f));
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            bounds.center,
            castSize,
            0f,
            Vector2.right * chargeDirection,
            castDistance + 0.05f);
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;

            if (hitCollider == null
                || hitCollider.isTrigger
                || hitCollider.transform.IsChildOf(transform)
                || transform.IsChildOf(hitCollider.transform)
                || Mathf.Abs(hits[i].normal.x) < 0.5f)
            {
                continue;
            }

            HWJ_RootObjectDataResolver hitResolver =
                hitCollider.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (hitResolver != null && hitResolver.ObjectType == HWJ_ObjectType.Player)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hits[i].distance);
        }

        if (nearestDistance == float.MaxValue)
        {
            return false;
        }

        wallDistance = nearestDistance;
        return true;
    }

    private void MoveChargeBody(float distance)
    {
        Vector2 delta = Vector2.right * chargeDirection * Mathf.Max(0f, distance);

        if (body != null)
        {
            body.position += delta;
            Physics2D.SyncTransforms();
            return;
        }

        transform.position += (Vector3)delta;
        Physics2D.SyncTransforms();
    }

    private float ResolveMaximumChargeDistance()
    {
        float roomWidth = bossBrain != null ? bossBrain.BossRoomSize.x : 20f;
        return Mathf.Max(4f, roomWidth * Mathf.Clamp(arenaWidthTravelRatio, 0.4f, 0.55f));
    }

    private void CompleteCharge()
    {
        if (!isPatternRunning)
        {
            ForceCleanup(false);
            return;
        }

        completedChargeCount++;
        ForceCleanup(true);
        isPatternRunning = false;
        activeRoutine = null;
    }

    private void ForceCleanup(bool returnToIdle)
    {
        StopChargeMovement();
        chargeHitbox?.Disarm();
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

    private void StopChargeMovement()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
        }
    }

    private void ApplyFacing()
    {
        if (facingRenderer != null)
        {
            facingRenderer.flipX = chargeDirection < 0f;
        }

        if (shoulderRoot != null)
        {
            shoulderRoot.localPosition = new Vector3(
                Mathf.Abs(shoulderLocalX) * chargeDirection,
                shoulderLocalY,
                shoulderRoot.localPosition.z);
        }

        if (telegraphRenderer != null)
        {
            float warningDistance = ResolveMaximumChargeDistance();
            telegraphRenderer.useWorldSpace = false;
            telegraphRenderer.positionCount = 2;
            telegraphRenderer.SetPosition(0, new Vector3(0.2f * chargeDirection, 0.25f, 0f));
            telegraphRenderer.SetPosition(
                1,
                new Vector3(warningDistance * chargeDirection, 0.25f, 0f));
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
            && string.Equals(pattern.PatternId, ChargePatternId, System.StringComparison.Ordinal)
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

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (chargeHitbox == null)
        {
            HWJ_FighterBossHitboxSystem[] hitboxes =
                GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null && hitboxes[i].name.Contains("Charge"))
                {
                    chargeHitbox = hitboxes[i];
                    break;
                }
            }
        }

        if (shoulderRoot == null && chargeHitbox != null)
        {
            shoulderRoot = chargeHitbox.transform;
        }
    }
}
