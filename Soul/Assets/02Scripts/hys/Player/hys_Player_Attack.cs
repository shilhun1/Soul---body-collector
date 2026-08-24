using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(hys_Player_State))]
[DefaultExecutionOrder(50)]
public class hys_Player_Attack : MonoBehaviour, IPlayerAttackHandler
{
    [Header("Attack")]
    // 공격이 맞았을 때 상대에게 전달할 데미지입니다.
    [SerializeField] private float attackDamage = 1f;

    // 공격을 다시 사용할 수 있을 때까지 기다리는 시간입니다.
    [SerializeField] private float attackCooldown = 0.04f;

    // 플레이어 상태를 Attack으로 유지하는 전체 시간입니다.
    [SerializeField] private float attackStateTime = 0.16f;

    // 공격 입력 후 실제 히트박스가 켜지는 타이밍입니다.
    [SerializeField] private float hitDelay = 0.06f;

    // 공격 판정이 감지할 레이어입니다.
    [SerializeField] private LayerMask targetLayers = ~0;

    // 검 끝이나 손 위치를 따로 기준점으로 쓰고 싶을 때 넣습니다.
    [SerializeField] private Transform attackPoint;

    [Header("Hit Box")]
    // 플레이어 기준 공격 박스 중심 위치입니다. x는 바라보는 방향에 따라 좌우 반전됩니다.
    [SerializeField] private Vector2 hitBoxOffset = new Vector2(0.95f, 0f);

    // 실제 데미지가 들어가는 가로 막대 판정 크기입니다.
    [SerializeField] private Vector2 hitBoxSize = new Vector2(1.6f, 0.45f);

    [Header("Debug")]
    // 공격 성공/타격 수를 Console에 출력할지 정합니다.
    [SerializeField] private bool useDebugLog = true;

    // Scene 뷰에서 공격 판정을 표시할지 정합니다.
    [SerializeField] private bool drawAttackRange = true;
    [SerializeField] private Color attackRangeColor = Color.red;

    // 공격이 실제로 발생한 순간 잠깐 채워지는 색입니다.
    [SerializeField] private Color attackRangeFillColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private float attackFillVisibleTime = 0.08f;

    // 연타 입력이 버퍼처럼 밀리지 않고 바로 다음 공격으로 이어지게 하는 최소 간격입니다.
    [SerializeField] private float continuousAttackInterval = 0.03f;

    [Header("Axe Dive Attack")]
    // Axe는 공중 공격 대신 빠르게 내려찍고 착지 순간 아래쪽을 공격합니다.
    [SerializeField] private bool enableAxeDiveAttack = true;
    [SerializeField, Min(0f)] private float axeDiveStartDuration = 0.2f;
    [SerializeField, Min(1f)] private float axeDiveFallSpeed = 20f;
    [SerializeField, Range(0f, 1f)] private float axeDiveHorizontalSpeedMultiplier = 0.2f;
    [SerializeField, Min(0f)] private float axeDiveLandingRecovery = 0.4f;
    [SerializeField, Min(0f)] private float axeDiveLandingCheckDelay = 0.05f;
    [SerializeField] private Vector2 axeDiveHitBoxOffset = new Vector2(0f, -0.8f);
    [SerializeField] private Vector2 axeDiveHitBoxSize = new Vector2(2.2f, 1.2f);

    // 같은 대상이 박스 안에 여러 콜라이더를 가져도 한 번만 맞게 하기 위한 임시 목록입니다.
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    // HWJ 보스가 여러 콜라이더를 사용해도 공격 한 번에 체력이 한 번만 감소하도록 기록합니다.
    private readonly HashSet<HWJ_RootObjectDataResolver> hitHwjTargets = new HashSet<HWJ_RootObjectDataResolver>();
    private readonly ContactPoint2D[] axeDiveGroundContacts = new ContactPoint2D[16];

    private hys_Player_State playerState;
    private hys_Player_Movement playerMovement;
    private HWJ_CombatSystem combatSystem;
    private HWJ_PossessionSystem possessionSystem;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D[] bodyColliders;

    // 공격 도중 플레이어가 움직여도 판정 위치가 흔들리지 않게 공격 시작 순간을 저장합니다.
    private Vector2 attackOriginSnapshot;
    private Vector2 attackDirectionSnapshot;
    private float nextAttackTime;
    private float nextContinuousAttackTime;
    private float attackFillEndTime;
    private int attackSequence;
    private Coroutine attackRoutine;
    private bool isAxeDiveStarting;
    private bool isAxeDiveFalling;
    private bool isAxeDiveRecovering;
    private bool useAxeDiveHitBox;
    private float axeDiveStartedTime;
    private float axeDiveRecoveryEndTime;

    // 애니메이터가 준비, 낙하, 착지 회복 모션을 각각 한 번만 시작할 때 사용합니다.
    public bool IsAxeDiveAttacking => isAxeDiveStarting || isAxeDiveFalling || isAxeDiveRecovering;
    public bool IsAxeDiveStarting => isAxeDiveStarting;
    public bool IsAxeDiveFalling => isAxeDiveFalling;
    public bool IsAxeDiveRecovering => isAxeDiveRecovering;
    // 실제 지면 접촉 뒤에만 Animator의 Plunge Fall -> Land 조건을 켭니다.
    public bool IsAxeDiveGrounded => isAxeDiveRecovering;

    // 활과 방패는 지상 공격 중 자리를 잡고 수평 이동을 멈춥니다.
    public bool ShouldLockGroundMovementForStationaryAttack =>
        playerState != null &&
        playerState.CurrentState == hys_PlayerState.Attack &&
        possessionSystem != null &&
        (possessionSystem.CurrentWeaponType == HWJ_WeaponType.Bow ||
         possessionSystem.CurrentWeaponType == HWJ_WeaponType.Shield) &&
        playerMovement != null &&
        playerMovement.Is_Grounded;

    private void Awake()
    {
        playerState = GetComponent<hys_Player_State>();
        playerMovement = GetComponent<hys_Player_Movement>();
        combatSystem = GetComponent<HWJ_CombatSystem>();
        possessionSystem = GetComponent<HWJ_PossessionSystem>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bodyColliders = GetComponents<Collider2D>();
    }

    private void Update()
    {
        if (hys_PlayerCinematicControlLock.IsLockedFor(transform))
        {
            CancelForCinematic();
            return;
        }

        TickAxeDiveAttack();
    }

    private void FixedUpdate()
    {
        if (hys_PlayerCinematicControlLock.IsLockedFor(transform))
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (rb == null)
        {
            return;
        }

        if (isAxeDiveStarting)
        {
            // 준비 모션 동안 잠깐 체공해 도끼를 앞으로 들어 올리는 자세를 보여줍니다.
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * axeDiveHorizontalSpeedMultiplier,
                0f);
            return;
        }

        if (!isAxeDiveFalling)
        {
            return;
        }

        // 이동 스크립트가 적용된 뒤 수직 속도를 고정해 확실한 내려찍기 궤적을 만듭니다.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x * axeDiveHorizontalSpeedMultiplier,
            -GetAxeDiveFallSpeed());
    }

    private void OnDisable()
    {
        CancelAxeDiveAttack(false);
    }

    private void CancelForCinematic()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            attackSequence++;
        }

        CancelAxeDiveAttack(false);
        hitTargets.Clear();
        hitHwjTargets.Clear();
        useAxeDiveHitBox = false;
        attackFillEndTime = 0f;

        if (playerState != null && playerState.CurrentState == hys_PlayerState.Attack)
            playerState.SetState(hys_PlayerState.Idle);
    }

    // 공격 가능 여부를 확인하고 공격 루틴을 시작합니다.
    public bool TryAttack()
    {
        if (hys_PlayerCinematicControlLock.IsLockedFor(transform)) return false;

        if (playerState == null)
        {
            return false;
        }

        bool isContinuingAttack = playerState.CurrentState == hys_PlayerState.Attack;
        if (isContinuingAttack)
        {
            if (Time.time < nextContinuousAttackTime)
            {
                return false;
            }
        }
        else if (Time.time < nextAttackTime)
        {
            return false;
        }

        // 기본 상태에서 공격할 수 없더라도 대쉬 후딜 공격 허용 옵션은 별도로 확인합니다.
        if (!playerState.CanAttack &&
            (playerMovement == null || !playerMovement.TryConsumeDashRecoveryForAttack()))
        {
            return false;
        }

        if (ShouldStartAxeDiveAttack())
        {
            StartAxeDiveAttack();
            return true;
        }

        StartFirstAttack();
        return true;
    }

    private bool ShouldStartAxeDiveAttack()
    {
        // 전용 Animator 상태가 준비된 도끼만 낙하 공격 흐름으로 진입합니다.
        return enableAxeDiveAttack &&
            ((possessionSystem != null && IsPlungeAttackWeapon(possessionSystem.CurrentWeaponType)) ||
             IsDirectAxeTestController()) &&
            playerMovement != null &&
            !playerMovement.Is_Grounded;
    }

    // 공중 공격을 일반 공격 대신 낙공으로 처리할 무기 종류입니다.
    private bool IsPlungeAttackWeapon(HWJ_WeaponType weaponType)
    {
        // 도끼와 방패는 공중 공격 입력을 각 무기의 전용 내려찍기로 처리합니다.
        return weaponType == HWJ_WeaponType.Axe ||
            weaponType == HWJ_WeaponType.Shield;
    }

    private bool IsDirectAxeTestController()
    {
        // 빙의 없이 Axe/Shield 컨트롤러로 시작하는 hys 씬에서도 낙하 공격 테스트를 허용합니다.
        return animator != null &&
            animator.runtimeAnimatorController != null &&
            (animator.runtimeAnimatorController.name == "hys_Player_Axe" ||
             animator.runtimeAnimatorController.name == "hys_Player_Shield");
    }

    private void StartAxeDiveAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        attackSequence++;
        playerState.SetState(hys_PlayerState.Attack);
        nextAttackTime = Time.time + attackCooldown;
        isAxeDiveStarting = true;
        isAxeDiveFalling = false;
        isAxeDiveRecovering = false;
        axeDiveStartedTime = Time.time;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * axeDiveHorizontalSpeedMultiplier,
                0f);
        }
    }

    private void BeginAxeDiveFall()
    {
        isAxeDiveStarting = false;
        isAxeDiveFalling = true;
        axeDiveStartedTime = Time.time;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * axeDiveHorizontalSpeedMultiplier,
                -GetAxeDiveFallSpeed());
        }
    }

    private float GetAxeDiveFallSpeed()
    {
        // 이전 씬에 빠른 값이 저장돼 있어도 새 낙공 속도 상한을 적용합니다.
        return Mathf.Min(axeDiveFallSpeed, 20f);
    }

    private void TickAxeDiveAttack()
    {
        if (!IsAxeDiveAttacking)
        {
            return;
        }

        bool lostPlungeBody = !IsDirectAxeTestController() &&
            (possessionSystem == null || !IsPlungeAttackWeapon(possessionSystem.CurrentWeaponType));
        bool interrupted = playerState == null ||
            playerState.CurrentState == hys_PlayerState.Dead ||
            playerState.CurrentState == hys_PlayerState.Hit;

        if (lostPlungeBody || interrupted)
        {
            CancelAxeDiveAttack(false);
            return;
        }

        if (isAxeDiveStarting && Time.time >= axeDiveStartedTime + axeDiveStartDuration)
        {
            // 준비 클립이 끝난 뒤에만 실제 낙하 속도를 적용합니다.
            BeginAxeDiveFall();
            return;
        }

        if (isAxeDiveFalling &&
            Time.time >= axeDiveStartedTime + axeDiveLandingCheckDelay &&
            HasAxeDiveGroundContact())
        {
            CompleteAxeDiveLanding();
            return;
        }

        if (isAxeDiveRecovering && Time.time >= axeDiveRecoveryEndTime)
        {
            FinishAxeDiveAttack();
        }
    }

    private bool HasAxeDiveGroundContact()
    {
        if (bodyColliders == null || bodyColliders.Length == 0)
        {
            bodyColliders = GetComponents<Collider2D>();
        }

        for (int colliderIndex = 0; colliderIndex < bodyColliders.Length; colliderIndex++)
        {
            Collider2D bodyCollider = bodyColliders[colliderIndex];
            if (bodyCollider == null || !bodyCollider.enabled || bodyCollider.isTrigger)
            {
                continue;
            }

            int contactCount = bodyCollider.GetContacts(axeDiveGroundContacts);
            for (int contactIndex = 0; contactIndex < contactCount; contactIndex++)
            {
                // 발밑 원형 범위가 아니라 위쪽을 향한 실제 물리 접촉면에서만 착지로 전환합니다.
                ContactPoint2D contact = axeDiveGroundContacts[contactIndex];
                if (contact.otherCollider != null && contact.normal.y >= 0.5f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CompleteAxeDiveLanding()
    {
        isAxeDiveStarting = false;
        isAxeDiveFalling = false;
        isAxeDiveRecovering = true;
        // 충돌, 도끼 회수, 기상 3프레임이 모두 보이도록 최소 0.5초를 보장합니다.
        axeDiveRecoveryEndTime = Time.time + Mathf.Max(axeDiveLandingRecovery, 0.5f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        attackOriginSnapshot = GetCurrentAttackOrigin();
        attackDirectionSnapshot = GetCurrentAttackDirection();
        useAxeDiveHitBox = true;
        attackFillEndTime = Time.time + attackFillVisibleTime;
        HitTargets();
        useAxeDiveHitBox = false;

        if (axeDiveLandingRecovery <= 0f)
        {
            FinishAxeDiveAttack();
        }
    }

    private void FinishAxeDiveAttack()
    {
        isAxeDiveStarting = false;
        isAxeDiveFalling = false;
        isAxeDiveRecovering = false;
        useAxeDiveHitBox = false;

        if (playerState != null && playerState.CurrentState == hys_PlayerState.Attack)
        {
            playerState.SetState(GetReturnState());
        }
    }

    private void CancelAxeDiveAttack(bool restorePlayerState)
    {
        bool wasActive = IsAxeDiveAttacking;
        isAxeDiveStarting = false;
        isAxeDiveFalling = false;
        isAxeDiveRecovering = false;
        useAxeDiveHitBox = false;

        if (restorePlayerState && wasActive && playerState != null &&
            playerState.CurrentState == hys_PlayerState.Attack)
        {
            playerState.SetState(GetReturnState());
        }
    }

    private void StartFirstAttack()
    {
        // 공격 입력이 들어온 프레임에 바로 Attack 상태로 바꿔서 점프 전환에 씹히지 않게 합니다.
        playerState.SetState(hys_PlayerState.Attack);

        if (ShouldLockGroundMovementForStationaryAttack && rb != null)
        {
            // 달리다가 활이나 방패로 공격해도 미끄러지지 않도록 수평 속도를 제거합니다.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        attackOriginSnapshot = GetCurrentAttackOrigin();
        attackDirectionSnapshot = GetCurrentAttackDirection();
        nextAttackTime = Time.time + attackCooldown;
        nextContinuousAttackTime = Time.time + continuousAttackInterval;
        int currentAttackSequence = ++attackSequence;

        attackRoutine = StartCoroutine(AttackRoutine(currentAttackSequence));
    }

    // 공격 상태 진입, 선딜레이, 판정 발생, 상태 복구를 순서대로 처리합니다.
    private IEnumerator AttackRoutine(int currentAttackSequence)
    {
        yield return new WaitForSeconds(hitDelay);

        if (playerState.CurrentState != hys_PlayerState.Attack)
        {
            ClearAttackRoutineIfLatest(currentAttackSequence);
            yield break;
        }

        attackFillEndTime = Time.time + attackFillVisibleTime;
        HitTargets();

        float remainStateTime = Mathf.Max(0f, attackStateTime - hitDelay);
        yield return new WaitForSeconds(remainStateTime);

        if (currentAttackSequence == attackSequence && playerState.CurrentState == hys_PlayerState.Attack)
        {
            playerState.SetState(GetReturnState());
        }

        ClearAttackRoutineIfLatest(currentAttackSequence);
    }

    private void ClearAttackRoutineIfLatest(int currentAttackSequence)
    {
        // 연타 중 오래된 공격 루틴이 최신 공격 상태를 끊지 않게 마지막 루틴만 정리합니다.
        if (currentAttackSequence == attackSequence)
        {
            attackRoutine = null;
        }
    }

    // 저장된 공격 위치 기준으로 박스 판정을 검사하고 TakeDamage 메시지를 보냅니다.
    private void HitTargets()
    {
        hitTargets.Clear();
        hitHwjTargets.Clear();
        Collider2D[] hits = Physics2D.OverlapBoxAll(GetHitBoxCenter(), GetActiveHitBoxSize(), 0f, targetLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                hitTargets.Add(hit);
            }
        }

        foreach (Collider2D hit in hitTargets)
        {
            HWJ_RootObjectDataResolver targetResolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (targetResolver != null)
            {
                if (hitHwjTargets.Add(targetResolver))
                {
                    DamageHwjTarget(targetResolver);
                }

                continue;
            }

            // HWJ 전투 구조를 사용하지 않는 기존 적은 이전 방식으로 피해를 전달합니다.
            hit.SendMessageUpwards("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (useDebugLog)
        {
            Debug.Log($"[Player Attack] X Attack / hits: {hitTargets.Count}", this);
        }
    }

    private void DamageHwjTarget(HWJ_RootObjectDataResolver targetResolver)
    {
        if (targetResolver == null)
        {
            return;
        }

        // 플레이어에게 HWJ 전투 시스템이 있으면 공격력, 방어력, 무적 판정을 포함한 공통 계산을 사용합니다.
        if (combatSystem != null)
        {
            combatSystem.TryDealDamageTo(targetResolver, out _);
            return;
        }

        // 순수 hys 플레이어도 HWJ 보스와 바로 싸울 수 있도록 기본 공격 데미지를 보스 체력에 전달합니다.
        HWJ_RuntimeStatusSystem targetStatus = targetResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        if (targetStatus != null)
        {
            targetStatus.ApplyDamage(attackDamage, this, null);
        }
    }

    // 공격이 끝났을 때 돌아갈 상태를 정합니다.
    private hys_PlayerState GetReturnState()
    {
        if (playerMovement != null && playerMovement.Is_Dashing)
        {
            return hys_PlayerState.Dash;
        }

        if (playerMovement != null)
        {
            // 공중 공격이 끝나면 Idle로 끊지 않고 현재 점프 흐름으로 되돌립니다.
            if (!playerMovement.Is_Grounded)
            {
                return playerMovement.VerticalVelocity > 0f ? hys_PlayerState.Jump : hys_PlayerState.Fall;
            }

            return playerMovement.HasMoveInput ? hys_PlayerState.Move : hys_PlayerState.Idle;
        }

        return hys_PlayerState.Idle;
    }

    // attackPoint가 있으면 그 위치를, 없으면 플레이어 위치를 공격 기준점으로 사용합니다.
    private Vector2 GetCurrentAttackOrigin()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        return transform.position;
    }

    // 마지막 이동 방향 또는 오브젝트 스케일을 기준으로 공격 방향을 정합니다.
    private Vector2 GetCurrentAttackDirection()
    {
        float direction = playerMovement != null ? playerMovement.LastMoveDirection : Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(direction, 0f))
        {
            direction = 1f;
        }

        return Vector2.right * direction;
    }

    // 실제 공격이 발생한 순간의 박스 중심입니다.
    private Vector2 GetHitBoxCenter()
    {
        float directionSign = Mathf.Sign(attackDirectionSnapshot.x);
        Vector2 activeOffset = useAxeDiveHitBox ? axeDiveHitBoxOffset : hitBoxOffset;
        return attackOriginSnapshot + new Vector2(activeOffset.x * directionSign, activeOffset.y);
    }

    private Vector2 GetActiveHitBoxSize()
    {
        return useAxeDiveHitBox ? axeDiveHitBoxSize : hitBoxSize;
    }

    // Scene 뷰에서 미리 보여줄 현재 기준 박스 중심입니다.
    private Vector2 GetPreviewHitBoxCenter()
    {
        Vector2 direction = GetCurrentAttackDirection();
        float directionSign = Mathf.Sign(direction.x);
        return GetCurrentAttackOrigin() + new Vector2(hitBoxOffset.x * directionSign, hitBoxOffset.y);
    }

    // 선택된 상태에서 공격 판정을 시각적으로 확인하기 위한 기즈모입니다.
    private void OnDrawGizmosSelected()
    {
        if (!drawAttackRange)
        {
            return;
        }

        if (Application.isPlaying && Time.time < attackFillEndTime)
        {
            Gizmos.color = attackRangeFillColor;
            Gizmos.DrawCube(GetHitBoxCenter(), GetActiveHitBoxSize());
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireCube(GetHitBoxCenter(), GetActiveHitBoxSize());
        }
    }

    public bool OnAttackPressed()
    {
        if(IsAxeDiveAttacking)
        {
            return false;
        }

        return TryAttack();
    }

    public void OnAttackReleased()
    {
        //
    }

    public void CancelAttack()
    {
        attackSequence++;

        if(attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        CancelAxeDiveAttack(true);

        if(playerState != null && playerState.CurrentState == hys_PlayerState.Attack)
        {
            playerState.SetState(GetReturnState());
        }
    }
}
