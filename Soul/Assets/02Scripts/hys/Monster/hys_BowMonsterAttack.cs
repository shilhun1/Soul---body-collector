using System.Collections;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class hys_BowMonsterAttack : MonoBehaviour
{
    // 활 몬스터 전용 공격 스크립트.
    // HWJ 몬스터 AI의 추적/공격 준비 상태는 그대로 쓰고, 실제 공격 패턴만 활 전용으로 바꾼다.

    [Header("References")]
    // 이미 몬스터 오브젝트에 붙어있는 HWJ 시스템들을 연결해서 데이터, 상태, 데미지 처리를 같이 사용한다.
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_MonsterAISystem monsterAI;
    [SerializeField] private HWJ_EnemyAttackSystem defaultEnemyAttack;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform target;

    [Header("AI Link")]
    // target을 비워두면 GameManager 또는 씬 안의 Player Resolver를 찾아 자동으로 공격 대상을 잡는다.
    [SerializeField] private bool autoFindPlayerTarget = true;

    // true면 HWJ_MonsterAISystem이 공격 준비/공격 상태일 때만 활 패턴을 시작한다.
    // false로 두면 사거리 안에 들어오기만 해도 이 스크립트가 독립적으로 공격한다.
    [SerializeField] private bool attackOnlyWhenMonsterAIAttacks = true;
    [SerializeField] private bool useBowRangeAsAttackTrigger = true;

    // true면 기존 HWJ_EnemyAttackSystem의 기본 공격이 같이 나가지 않도록 막는다.
    [SerializeField] private bool takeOverDefaultEnemyAttack = true;

    [Header("Bow Range")]
    // 기획서 표 기준 Bow 사거리 10m를 기본값으로 둔다.
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.4f;

    // 횡스크롤 전투 기준이라 기본은 x축 거리만 보고 사거리 판정한다.
    [SerializeField] private bool horizontalDistanceOnly = true;
    [SerializeField] private bool returnToPatrolWhenTargetLostAfterAttack = true;
    [SerializeField] private float fallbackTrackingRange = 12f;
    [SerializeField] private float fallbackLoseTargetRange = 15.71f;
    [SerializeField] private bool applyBowRangeToRuntimeEnemyData = true;

    [Header("Pattern 1")]
    // 패턴 1: 선딜 후 점프하고, 플레이어 주변에 아래로 떨어지는 화살 3발을 생성한다.
    [SerializeField] private float pattern1StartDelay = 0.3f;
    [SerializeField] private float pattern1JumpPower = 5f;
    [SerializeField] private int pattern1ArrowCount = 3;
    [SerializeField] private float pattern1ArrowSpacing = 1f;

    [Header("Pattern 2")]
    // 패턴 2: 패턴 1 이후, 플레이어 위치를 따라잡는 낙하 화살 2발을 연속으로 쏜다.
    [SerializeField] private float pattern2StartDelay = 0.5f;
    [SerializeField] private int pattern2ArrowCount = 2;
    [SerializeField] private float pattern2ArrowInterval = 0.2f;

    [Header("Pattern 3")]
    // 패턴 3: 1초간 준비 자세 후, 기존 화살보다 두꺼운 직선 화살을 발사한다.
    [SerializeField] private float pattern3ReadySeconds = 1f;
    [SerializeField] private float pattern3ArrowWidthMultiplier = 2f;

    [Header("Arrow Hit")]
    // 현재는 실제 화살 프리팹 대신, 예고 표시 후 범위 안에 있으면 데미지를 주는 방식이다.
    [SerializeField] private float arrowWarningSeconds = 0.35f;
    [SerializeField] private float arrowHitRadius = 0.55f;
    [SerializeField] private float arrowDamageMultiplier = 1f;
    [SerializeField] private float strongArrowDamageMultiplier = 1.6f;

    // 공격 성공 시 플레이어를 살짝 밀어내는 활 몬스터 특수 AI 값.
    [SerializeField] private float extraPushPower = 2.5f;

    [Header("Timing")]
    // 하나의 패턴이 끝난 뒤 다음 패턴을 시작하기 전까지 기다리는 시간.
    [SerializeField] private float attackCooldownSeconds = 1.2f;

    [Header("Debug")]
    [SerializeField] private string lastBowAttackResult;

    private float nextAttackTime;
    private float nextTargetSearchTime;
    private int patternIndex;
    private bool isAttacking;

    public string LastBowAttackResult => lastBowAttackResult;

    private void Awake()
    {
        CacheReferences();

        // 활 몬스터는 기본 근접 공격 대신 이 스크립트의 패턴 공격을 사용한다.
        if (takeOverDefaultEnemyAttack)
        {
            StopDefaultEnemyAttackFromMonsterAI();
        }
    }

    private void Update()
    {
        CacheReferences();

        // HWJ 몬스터 AI가 매 프레임 기본 공격을 다시 잡을 수 있어서 먼저 끊어준다.
        if (takeOverDefaultEnemyAttack)
        {
            StopDefaultEnemyAttackFromMonsterAI();
        }

        UpdateTarget();
        HoldPositionInBowRange();

        if (!CanStartAttack())
        {
            return;
        }

        StartCoroutine(BowAttackRoutine());
    }

    private void LateUpdate()
    {
        HoldPositionInBowRange();
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

        ApplyBowRangeToRuntimeEnemyData();
    }

    private void ApplyBowRangeToRuntimeEnemyData()
    {
        // Bow 데이터가 비어 있거나 짧으면 런타임에서 활 몬스터용 인지/공격 거리로 보정한다.
        if (!applyBowRangeToRuntimeEnemyData || monsterAI == null)
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

        float trackingRange = Mathf.Max(attackRange, fallbackTrackingRange);
        float loseRange = Mathf.Max(trackingRange, fallbackLoseTargetRange);

        if (monsterAI.EnemyData.State.attackRange < attackRange)
        {
            monsterAI.EnemyData.State.attackRange = attackRange;
        }

        if (monsterAI.EnemyData.Tracking.trackingRange < trackingRange)
        {
            monsterAI.EnemyData.Tracking.trackingRange = trackingRange;
        }

        if (monsterAI.EnemyData.Tracking.loseTargetRange < loseRange)
        {
            monsterAI.EnemyData.Tracking.loseTargetRange = loseRange;
        }
    }

    private void UpdateTarget()
    {
        // 이미 target이 있으면 계속 같은 플레이어를 바라본다.
        // target이 사라지는 상황까지 필요해지면 여기서 null 재탐색 조건을 늘리면 된다.
        if (target != null || !autoFindPlayerTarget || Time.time < nextTargetSearchTime)
        {
            return;
        }

        // GameManager의 플레이어 참조를 우선 사용하고, 없으면 씬에서 직접 찾는다.
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
        // 공격 중이거나 쿨타임이면 새 패턴을 시작하지 않는다.
        if (isAttacking || Time.time < nextAttackTime)
        {
            return false;
        }

        if (runtimeStatus != null && (runtimeStatus.IsDead || !runtimeStatus.CanAttack))
        {
            return false;
        }

        // 영혼 상태 플레이어는 몬스터가 무시해야 하므로 Body 상태일 때만 공격한다.
        if (target == null || !IsTargetBodyState() || !IsTargetInRange())
        {
            return false;
        }

        if (!attackOnlyWhenMonsterAIAttacks || monsterAI == null)
        {
            return true;
        }

        // HWJ AI의 공격 준비/공격 타이밍에 맞춰 활 패턴을 시작한다.
        if (useBowRangeAsAttackTrigger)
        {
            return IsMonsterAwareOfTarget();
        }

        return monsterAI.CurrentState == HWJ_MonsterAIState.AttackPrepare
            || monsterAI.CurrentState == HWJ_MonsterAIState.Attack;
    }

    private IEnumerator BowAttackRoutine()
    {
        // 기획서 순서대로 1, 2, 3번 패턴을 반복한다.
        isAttacking = true;
        nextAttackTime = Time.time + Mathf.Max(0f, attackCooldownSeconds);
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        FaceTarget();

        switch (patternIndex)
        {
            case 0:
                yield return Pattern1Routine();
                break;
            case 1:
                yield return Pattern2Routine();
                break;
            default:
                yield return Pattern3Routine();
                break;
        }

        patternIndex = (patternIndex + 1) % 3;
        isAttacking = false;

        if (returnToPatrolWhenTargetLostAfterAttack && IsTargetLostAfterAttack())
        {
            ReturnToPatrolState();
        }
    }

    private IEnumerator Pattern1Routine()
    {
        lastBowAttackResult = "Pattern 1: jump and falling arrows.";

        // 0.3초 선딜 후 위로 뛰고, 플레이어 주변에 낙하 화살 3발을 뿌린다.
        yield return new WaitForSeconds(Mathf.Max(0f, pattern1StartDelay));

        // Rigidbody가 있으면 점프 연출만 주고, 실제 화살 판정은 아래 원형 판정으로 처리한다.
        if (rb != null)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.y = Mathf.Max(0f, pattern1JumpPower);
            rb.linearVelocity = velocity;
        }

        Vector3 center = target != null ? target.position : transform.position;
        int count = Mathf.Max(1, pattern1ArrowCount);

        // 여러 발을 한 점에 겹치지 않게 플레이어 중심 기준 좌우로 벌려서 배치한다.
        float startOffset = -(count - 1) * Mathf.Max(0f, pattern1ArrowSpacing) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 strikePosition = center + Vector3.right * (startOffset + pattern1ArrowSpacing * i);
            StartCoroutine(FallingArrowRoutine(strikePosition, arrowHitRadius, arrowDamageMultiplier));
        }

        yield return new WaitForSeconds(arrowWarningSeconds + 0.05f);
    }

    private IEnumerator Pattern2Routine()
    {
        lastBowAttackResult = "Pattern 2: tracking falling arrows.";

        // 플레이어 현재 위치를 다시 잡아가며 화살을 연속으로 떨어뜨린다.
        yield return new WaitForSeconds(Mathf.Max(0f, pattern2StartDelay));

        int count = Mathf.Max(1, pattern2ArrowCount);
        for (int i = 0; i < count; i++)
        {
            // 매 발마다 target.position을 다시 읽어서 플레이어 이동을 따라가게 한다.
            Vector3 strikePosition = target != null ? target.position : transform.position;
            yield return FallingArrowRoutine(strikePosition, arrowHitRadius, arrowDamageMultiplier);

            if (i < count - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, pattern2ArrowInterval));
            }
        }
    }

    private IEnumerator Pattern3Routine()
    {
        lastBowAttackResult = "Pattern 3: strong straight arrow.";
        FaceTarget();

        // 긴 준비 자세 후 기존 화살보다 두꺼운 직선 공격을 사용한다.
        ShowStraightArrowWarning(pattern3ReadySeconds, pattern3ArrowWidthMultiplier);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern3ReadySeconds));

        TryDamageTargetByStraightArrow(
            arrowHitRadius * Mathf.Max(1f, pattern3ArrowWidthMultiplier),
            strongArrowDamageMultiplier);
    }

    private IEnumerator FallingArrowRoutine(Vector3 strikePosition, float radius, float damageMultiplier)
    {
        // 실제 피격 전에 범위 예고를 먼저 보여준다.
        HWJ_SkillWarningIndicator.ShowCircle(
            strikePosition,
            radius,
            arrowWarningSeconds,
            new Color(0.15f, 0.75f, 1f, 0.9f),
            0.06f);

        yield return new WaitForSeconds(Mathf.Max(0.01f, arrowWarningSeconds));
        TryDamageTargetInCircle(strikePosition, radius, damageMultiplier);
    }

    private void TryDamageTargetInCircle(Vector3 center, float radius, float damageMultiplier)
    {
        // 원형 판정 안에 플레이어가 있을 때만 데미지를 적용한다.
        HWJ_RootObjectDataResolver targetResolver = FindTargetResolver();
        if (targetResolver == null || combatSystem == null)
        {
            return;
        }

        if (Vector2.Distance(targetResolver.transform.position, center) > Mathf.Max(0.1f, radius))
        {
            return;
        }

        if (combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _))
        {
            PushTarget(targetResolver);
        }
    }

    private void TryDamageTargetByStraightArrow(float halfHeight, float damageMultiplier)
    {
        // 보스 방향 앞쪽, 사거리 안, 화살 높이 안에 들어오면 맞은 것으로 본다.
        HWJ_RootObjectDataResolver targetResolver = FindTargetResolver();
        if (targetResolver == null || combatSystem == null)
        {
            return;
        }

        float direction = GetTargetDirection();
        Vector2 toTarget = targetResolver.transform.position - transform.position;

        // 직선 화살은 몬스터가 바라보는 방향 앞쪽에 있는 대상만 맞는다.
        bool isFront = Mathf.Sign(toTarget.x) == Mathf.Sign(direction);
        bool isInRange = Mathf.Abs(toTarget.x) <= Mathf.Max(0.1f, attackRange);
        bool isInHeight = Mathf.Abs(toTarget.y) <= Mathf.Max(0.1f, halfHeight);

        if (!isFront || !isInRange || !isInHeight)
        {
            return;
        }

        if (combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _))
        {
            PushTarget(targetResolver);
        }
    }

    private void PushTarget(HWJ_RootObjectDataResolver targetResolver)
    {
        // 활 공격 성공 시 플레이어를 살짝 밀쳐내는 특수 AI 처리.
        if (targetResolver == null || extraPushPower <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = targetResolver.GetComponent<HWJ_KnockbackSystem>();
        if (knockbackSystem == null)
        {
            knockbackSystem = targetResolver.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = targetResolver.transform.position - transform.position;
        direction.y = 0f;
        knockbackSystem.PlayKnockback(direction, extraPushPower, 0.18f);
    }

    private void HoldPositionInBowRange()
    {
        // 활 몬스터는 사거리 안에 들어오면 더 붙지 않고 제자리에서 공격한다.
        if (!isAttacking && (!IsTargetInRange() || !IsMonsterAwareOfTarget()))
        {
            return;
        }

        StopHorizontalMovement();
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

    private void ShowStraightArrowWarning(float warningSeconds, float widthMultiplier)
    {
        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            GetTargetDirection(),
            Mathf.Max(0.1f, attackRange),
            arrowHitRadius * Mathf.Max(1f, widthMultiplier) * 2f,
            Mathf.Max(0.01f, warningSeconds),
            new Color(0.15f, 0.75f, 1f, 0.9f),
            0.06f);
    }

    private bool IsTargetInRange()
    {
        // 활 몬스터는 높이 차이가 조금 있어도 가로 사거리에 들어오면 공격하도록 기본 설정했다.
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

    private bool IsTargetLostAfterAttack()
    {
        // 공격은 중간에 끊지 않고, 끝난 뒤에만 플레이어가 인지 범위를 벗어났는지 확인한다.
        if (target == null || !IsTargetBodyState())
        {
            return true;
        }

        return GetTargetDistance() > GetLoseTargetRange();
    }

    private float GetTargetDistance()
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        if (horizontalDistanceOnly)
        {
            return Mathf.Abs(target.position.x - transform.position.x);
        }

        return Vector2.Distance(transform.position, target.position);
    }

    private float GetLoseTargetRange()
    {
        if (monsterAI != null && monsterAI.EnemyData != null)
        {
            if (monsterAI.EnemyData.Tracking.loseTargetRange > 0f)
            {
                return monsterAI.EnemyData.Tracking.loseTargetRange;
            }

            if (monsterAI.EnemyData.Tracking.trackingRange > 0f)
            {
                return monsterAI.EnemyData.Tracking.trackingRange;
            }
        }

        return Mathf.Max(attackRange, fallbackLoseTargetRange);
    }

    private bool IsMonsterAwareOfTarget()
    {
        // Idle은 아직 못 본 상태로 보고, Detect/Approach 이후부터 활 사거리 공격을 허용한다.
        if (monsterAI == null)
        {
            return true;
        }

        return monsterAI.CurrentState == HWJ_MonsterAIState.Detect
            || monsterAI.CurrentState == HWJ_MonsterAIState.Approach
            || monsterAI.CurrentState == HWJ_MonsterAIState.AttackPrepare
            || monsterAI.CurrentState == HWJ_MonsterAIState.Attack
            || monsterAI.CurrentState == HWJ_MonsterAIState.Recovery
            || monsterAI.CurrentState == HWJ_MonsterAIState.Repath;
    }

    private bool IsTargetBodyState()
    {
        // 플레이어가 영혼 상태면 몬스터 공격 대상에서 제외한다.
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
        // 데미지 시스템은 Transform이 아니라 RootObjectDataResolver 기준으로 대상을 받는다.
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

    private void ReturnToPatrolState()
    {
        // 플레이어가 인지 범위 밖으로 나갔으면 타겟을 비워서 순찰 스크립트가 다시 동작하게 한다.
        target = null;
        monsterAI?.SetTarget(null);
        StopHorizontalMovement();

        if (runtimeStatus != null && !runtimeStatus.IsDead)
        {
            runtimeStatus.SetState(HWJ_RuntimeState.Idle);
        }

        if (monsterAI == null)
        {
            return;
        }

        FieldInfo targetField = typeof(HWJ_MonsterAISystem).GetField(
            "target",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo stateField = typeof(HWJ_MonsterAISystem).GetField(
            "currentState",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo stateEndTimeField = typeof(HWJ_MonsterAISystem).GetField(
            "stateEndTime",
            BindingFlags.Instance | BindingFlags.NonPublic);

        targetField?.SetValue(monsterAI, null);
        stateField?.SetValue(monsterAI, HWJ_MonsterAIState.Idle);
        stateEndTimeField?.SetValue(monsterAI, Time.time);
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

    private void StopDefaultEnemyAttackFromMonsterAI()
    {
        // 기존 HWJ_EnemyAttackSystem이 같이 공격하지 않도록 런타임에서 차단한다.
        if (defaultEnemyAttack != null)
        {
            defaultEnemyAttack.enabled = false;

            // MonsterAI가 TryAutoAttack을 직접 호출해도 기본 공격 쿨타임에 걸리게 만들어 한 번 더 막는다.
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

        // MonsterAI 내부의 enemyAttackSystem 참조를 비워서 RunAttack에서 기본 공격 호출을 막는다.
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
