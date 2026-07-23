using System.Collections;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class hys_ShieldMonsterAttack : MonoBehaviour
{
    // 방패 몬스터 전용 공격 스크립트.
    // HWJ 몬스터 AI의 인식/추격 흐름은 사용하고, 실제 공격 패턴만 방패 전용으로 처리한다.

    [Header("References")]
    // 같은 오브젝트에 붙은 HWJ 시스템을 찾아서 상태, 방향, 데미지 처리를 같이 사용한다.
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_MonsterAISystem monsterAI;
    [SerializeField] private HWJ_EnemyAttackSystem defaultEnemyAttack;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform target;

    [Header("AI Link")]
    // target이 비어 있으면 GameManager 또는 씬 안의 Player Resolver를 찾아 공격 대상을 잡는다.
    [SerializeField] private bool autoFindPlayerTarget = true;

    // true면 HWJ AI가 공격 준비/공격 상태일 때만 방패 패턴을 시작한다.
    [SerializeField] private bool attackOnlyWhenMonsterAIAttacks = true;

    // true면 기존 HWJ_EnemyAttackSystem의 기본 공격이 같이 나가지 않게 막는다.
    [SerializeField] private bool takeOverDefaultEnemyAttack = true;

    [Header("Shield Range")]
    // 기획서 표 기준 Shield 사거리 1.2m를 기본값으로 둔다.
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackAngle = 90f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.4f;
    [SerializeField] private bool horizontalDistanceOnly = true;
    [SerializeField] private bool applyShieldRangeToRuntimeEnemyData = true;

    [Header("Pattern 1")]
    // 패턴 1: 방패를 드는 선동작 후 전방으로 3m 돌진한다.
    [SerializeField] private float shieldRaiseSeconds = 1.25f;
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashSeconds = 0.22f;
    [SerializeField] private float dashHitRadius = 0.75f;
    [SerializeField] private float dashDamageMultiplier = 1f;

    [Header("Pattern 2")]
    // 패턴 2: 0.5초 선딜 후 방패 내려찍기 공격을 사용한다.
    [SerializeField] private float slamStartDelay = 0.5f;
    [SerializeField] private float slamHitRadius = 1.1f;
    [SerializeField] private float slamDamageMultiplier = 1.2f;

    [Header("Pattern 3")]
    // 패턴 3: 1초 준비 후 사선 밀치기 공격을 사용한다.
    [SerializeField] private float diagonalPushReadySeconds = 1f;
    [SerializeField] private float diagonalPushRange = 1.4f;
    [SerializeField] private float diagonalPushHeight = 1.2f;
    [SerializeField] private float diagonalPushDamageMultiplier = 1.1f;
    [SerializeField] private float diagonalPushPower = 4f;

    [Header("Special AI")]
    // 방패 몬스터는 공격 성공 시 플레이어 조작을 잠깐 막아 스턴처럼 보이게 한다.
    [SerializeField] private float stunSeconds = 0.6f;

    [Header("Timing")]
    [SerializeField] private float attackCooldownSeconds = 1f;

    [Header("Debug")]
    [SerializeField] private string lastShieldAttackResult;

    private float nextAttackTime;
    private float nextTargetSearchTime;
    private int patternIndex;
    private bool isAttacking;

    public string LastShieldAttackResult => lastShieldAttackResult;

    private void Awake()
    {
        CacheReferences();

        // 방패 몬스터는 기본 공격 대신 이 스크립트의 패턴 공격을 사용한다.
        if (takeOverDefaultEnemyAttack)
        {
            StopDefaultEnemyAttackFromMonsterAI();
        }
    }

    private void Update()
    {
        CacheReferences();

        // MonsterAI가 기본 공격 참조를 다시 잡을 수 있어서 매 프레임 먼저 끊어준다.
        if (takeOverDefaultEnemyAttack)
        {
            StopDefaultEnemyAttackFromMonsterAI();
        }

        UpdateTarget();

        if (!CanStartAttack())
        {
            return;
        }

        StartCoroutine(ShieldAttackRoutine());
    }

    private void CacheReferences()
    {
        // 프리팹에서 직접 연결하지 않아도 같은 오브젝트에 있으면 자동으로 찾아온다.
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (monsterAI == null)
        {
            monsterAI = GetComponent<HWJ_MonsterAISystem>();
        }

        if (defaultEnemyAttack == null)
        {
            defaultEnemyAttack = GetComponent<HWJ_EnemyAttackSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        ApplyShieldRangeToRuntimeEnemyData();
    }

    private void ApplyShieldRangeToRuntimeEnemyData()
    {
        // Shield 데이터의 공격 사거리가 비어 있거나 짧으면 기획값 1.2m까지 런타임에서 보정한다.
        if (!applyShieldRangeToRuntimeEnemyData || monsterAI == null)
        {
            return;
        }

        if (monsterAI.EnemyData == null)
        {
            monsterAI.RefreshData();
        }

        if (monsterAI.EnemyData == null)
        {
            return;
        }

        if (monsterAI.EnemyData.State.attackRange < attackRange)
        {
            monsterAI.EnemyData.State.attackRange = attackRange;
        }
    }

    private void UpdateTarget()
    {
        if (target != null || !autoFindPlayerTarget || Time.time < nextTargetSearchTime)
        {
            return;
        }

        nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchIntervalSeconds);

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            target = HWJ_GameAccess.Manager.PlayerResolver.transform;
            return;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                target = resolvers[i].transform;
                return;
            }
        }
    }

    private bool CanStartAttack()
    {
        if (isAttacking || Time.time < nextAttackTime)
        {
            return false;
        }

        if (runtimeStatus != null && (runtimeStatus.IsDead || !runtimeStatus.CanAttack))
        {
            return false;
        }

        if (target == null || !IsTargetBodyState() || !IsTargetInRange())
        {
            return false;
        }

        if (!attackOnlyWhenMonsterAIAttacks || monsterAI == null)
        {
            return true;
        }

        return monsterAI.CurrentState == HWJ_MonsterAIState.AttackPrepare
            || monsterAI.CurrentState == HWJ_MonsterAIState.Attack;
    }

    private IEnumerator ShieldAttackRoutine()
    {
        // 기획서 순서대로 1, 2, 3번 패턴을 반복한다.
        isAttacking = true;
        nextAttackTime = Time.time + Mathf.Max(0f, attackCooldownSeconds);
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        StopHorizontalMovement();
        FaceTarget();

        switch (patternIndex)
        {
            case 0:
                yield return ShieldDashRoutine();
                break;
            case 1:
                yield return ShieldSlamRoutine();
                break;
            default:
                yield return DiagonalPushRoutine();
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
        isAttacking = false;
    }

    private IEnumerator ShieldDashRoutine()
    {
        lastShieldAttackResult = "Pattern 1: shield raise and dash.";

        // 방패를 드는 선동작 동안 이동을 멈추고 공격 준비 상태를 유지한다.
        StopHorizontalMovement();
        yield return new WaitForSeconds(Mathf.Max(0f, shieldRaiseSeconds));

        FaceTarget();
        float direction = GetTargetDirection();
        float elapsed = 0f;
        float dashSpeed = Mathf.Max(0f, dashDistance) / Mathf.Max(0.01f, dashSeconds);
        bool hasHit = false;

        while (elapsed < dashSeconds)
        {
            elapsed += Time.deltaTime;

            if (rb != null)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.x = direction * dashSpeed;
                rb.linearVelocity = velocity;
            }
            else
            {
                transform.position += Vector3.right * direction * dashSpeed * Time.deltaTime;
            }

            if (!hasHit && TryDamageTargetInFront(dashHitRadius, dashHitRadius, dashDamageMultiplier, true))
            {
                hasHit = true;
            }

            yield return null;
        }

        StopHorizontalMovement();
    }

    private IEnumerator ShieldSlamRoutine()
    {
        lastShieldAttackResult = "Pattern 2: shield slam.";
        StopHorizontalMovement();
        FaceTarget();

        yield return new WaitForSeconds(Mathf.Max(0f, slamStartDelay));

        ShowCircleWarning(transform.position + Vector3.right * GetTargetDirection() * attackRange, slamHitRadius, 0.12f);
        TryDamageTargetInFront(attackRange, slamHitRadius, slamDamageMultiplier, true);
    }

    private IEnumerator DiagonalPushRoutine()
    {
        lastShieldAttackResult = "Pattern 3: diagonal shield push.";
        StopHorizontalMovement();
        FaceTarget();

        yield return new WaitForSeconds(Mathf.Max(0f, diagonalPushReadySeconds));

        TryDamageTargetInFront(diagonalPushRange, diagonalPushHeight, diagonalPushDamageMultiplier, true);
    }

    private bool TryDamageTargetInFront(float range, float halfHeight, float damageMultiplier, bool applyPush)
    {
        HWJ_RootObjectDataResolver targetResolver = FindTargetResolver();
        if (targetResolver == null || combatSystem == null)
        {
            return false;
        }

        Vector2 toTarget = targetResolver.transform.position - transform.position;
        float direction = GetTargetDirection();
        bool isFront = Mathf.Sign(toTarget.x) == Mathf.Sign(direction);
        bool isInRange = Mathf.Abs(toTarget.x) <= Mathf.Max(0.1f, range);
        bool isInHeight = Mathf.Abs(toTarget.y) <= Mathf.Max(0.1f, halfHeight);
        bool isInAngle = IsTargetInFrontAngle(toTarget, direction);

        if (!isFront || !isInRange || !isInHeight || !isInAngle)
        {
            return false;
        }

        if (!combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _))
        {
            return false;
        }

        ApplyTargetStun(targetResolver);

        if (applyPush)
        {
            PushTarget(targetResolver);
        }

        return true;
    }

    private bool IsTargetInFrontAngle(Vector2 toTarget, float direction)
    {
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector2 forward = direction >= 0f ? Vector2.right : Vector2.left;
        float angle = Vector2.Angle(forward, toTarget.normalized);
        return angle <= Mathf.Max(1f, attackAngle) * 0.5f;
    }

    private void ApplyTargetStun(HWJ_RootObjectDataResolver targetResolver)
    {
        // HWJ에는 별도 Stun 상태가 없어서 조작 잠금과 Hit 상태로 스턴 느낌을 만든다.
        if (targetResolver == null || stunSeconds <= 0f)
        {
            return;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        if (targetStatus == null)
        {
            targetStatus = targetResolver.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        if (targetStatus == null || targetStatus.IsDead)
        {
            return;
        }

        targetStatus.LockControl(stunSeconds);
        targetStatus.SetState(HWJ_RuntimeState.Hit);
    }

    private void PushTarget(HWJ_RootObjectDataResolver targetResolver)
    {
        if (targetResolver == null || diagonalPushPower <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = targetResolver.GetComponent<HWJ_KnockbackSystem>();
        if (knockbackSystem == null)
        {
            knockbackSystem = targetResolver.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = targetResolver.transform.position - transform.position;
        direction.y = Mathf.Abs(direction.y) + 0.35f;
        knockbackSystem.PlayKnockback(direction, diagonalPushPower, 0.2f);
    }

    private void ShowCircleWarning(Vector3 position, float radius, float warningSeconds)
    {
        hys_SkillWarningIndicator.ShowCircle(
            position,
            radius,
            Mathf.Max(0.01f, warningSeconds),
            new Color(0.2f, 0.8f, 1f, 0.85f),
            0.06f);
    }

    private bool IsTargetInRange()
    {
        if (target == null)
        {
            return false;
        }

        if (horizontalDistanceOnly)
        {
            return Mathf.Abs(target.position.x - transform.position.x) <= Mathf.Max(0.1f, attackRange);
        }

        return Vector2.Distance(transform.position, target.position) <= Mathf.Max(0.1f, attackRange);
    }

    private bool IsTargetBodyState()
    {
        if (target == null)
        {
            return false;
        }

        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();
        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul == null || targetSoul.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private HWJ_RootObjectDataResolver FindTargetResolver()
    {
        if (target == null)
        {
            return null;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();
        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        return targetResolver;
    }

    private void FaceTarget()
    {
        motionSystem?.FaceDirection(GetTargetDirection());
    }

    private float GetTargetDirection()
    {
        if (target == null)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            return facing == 0f ? 1f : facing;
        }

        float direction = Mathf.Sign(target.position.x - transform.position.x);
        return direction == 0f ? 1f : direction;
    }

    private void StopHorizontalMovement()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private void StopDefaultEnemyAttackFromMonsterAI()
    {
        // 기존 HWJ_EnemyAttackSystem이 같이 공격하지 않도록 런타임에서 차단한다.
        if (defaultEnemyAttack != null)
        {
            defaultEnemyAttack.enabled = false;

            FieldInfo nextAttackTimeField = typeof(HWJ_EnemyAttackSystem).GetField(
                "nextAttackTime",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (nextAttackTimeField != null)
            {
                nextAttackTimeField.SetValue(defaultEnemyAttack, float.PositiveInfinity);
            }
        }

        if (monsterAI == null)
        {
            return;
        }

        FieldInfo field = typeof(HWJ_MonsterAISystem).GetField(
            "enemyAttackSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field != null)
        {
            field.SetValue(monsterAI, null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, attackRange));
    }
}
