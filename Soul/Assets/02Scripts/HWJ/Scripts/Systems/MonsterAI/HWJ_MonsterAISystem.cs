using UnityEngine;

/// <summary>
/// 적 오브젝트에서 EnemyTypeDataSO를 가져오는 AI 연결 컴포넌트입니다.
/// 현재는 데이터 참조 단계이며, 실제 추적/공격 행동은 EnemyData의 Tracking, AI, Navigation 값을 읽는 후속 AI 로직에서 구현합니다.
/// </summary>
public class HWJ_MonsterAISystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_EnemyAttackSystem enemyAttackSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_KnockbackSystem knockbackSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform target;
    [SerializeField] private bool driveBehavior = true;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool targetOnlyBodyState = true;
    [SerializeField] private bool horizontalMoveOnly = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float fallbackTrackingRange = 8f;
    [SerializeField] private float fallbackAttackRange = 1.2f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.5f;
    [SerializeField] private HWJ_MonsterAIState currentState = HWJ_MonsterAIState.Idle;

    public HWJ_EnemyTypeDataSO EnemyData { get; private set; }
    public bool DrivesBehavior => driveBehavior;
    public HWJ_MonsterAIState CurrentState => currentState;

    private float stateEndTime;
    private float nextTargetSearchTime;
    private float nextDecisionTime;

    private void Awake()
    {
        CacheReferences();
        RefreshData();
    }

    private void Update()
    {
        if (!driveBehavior)
        {
            return;
        }

        CacheReferences();
        RefreshData();

        if (target == null && autoFindPlayerTarget && Time.time >= nextTargetSearchTime)
        {
            target = FindPlayerTarget();
            nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchIntervalSeconds);
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            SetAIState(HWJ_MonsterAIState.Dead);
            StopHorizontalMovement();
            return;
        }

        if (runtimeStatus != null && runtimeStatus.IsHitStunned)
        {
            SetAIState(HWJ_MonsterAIState.HitStun);
            StopHorizontalMovementIfNotKnockedBack();
            return;
        }

        if (HandleSkillNavigationBlock())
        {
            return;
        }

        if (EnemyData == null || target == null || !CanUseTargetState())
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            StopHorizontalMovement();
            return;
        }

        if (Time.time < nextDecisionTime)
        {
            return;
        }

        nextDecisionTime = Time.time + Mathf.Max(0.02f, EnemyData.AI.decisionIntervalSeconds);
        RunState();
    }

    public void RefreshData()
    {
        if (dataResolver != null)
        {
            dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData);
            EnemyData = enemyData;
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        enemyAttackSystem?.SetTarget(target);
    }

    private void RunState()
    {
        switch (currentState)
        {
            case HWJ_MonsterAIState.Idle:
                RunIdle();
                break;
            case HWJ_MonsterAIState.Detect:
                RunDetect();
                break;
            case HWJ_MonsterAIState.Approach:
                RunApproach();
                break;
            case HWJ_MonsterAIState.AttackPrepare:
                RunAttackPrepare();
                break;
            case HWJ_MonsterAIState.Attack:
                RunAttack();
                break;
            case HWJ_MonsterAIState.Recovery:
                RunRecovery();
                break;
            case HWJ_MonsterAIState.Repath:
                RunRepath();
                break;
            case HWJ_MonsterAIState.HitStun:
                SetAIState(HWJ_MonsterAIState.Approach);
                break;
        }
    }

    private void RunIdle()
    {
        StopHorizontalMovement();
        runtimeStatus?.SetState(HWJ_RuntimeState.Idle);

        if (IsTargetInTrackingRange())
        {
            SetAIState(HWJ_MonsterAIState.Detect, EnemyData.AI.detectSeconds);
        }
    }

    private void RunDetect()
    {
        StopHorizontalMovement();
        FaceTarget();

        if (!IsStateTimeDone())
        {
            return;
        }

        SetAIState(IsTargetInAttackRange() ? HWJ_MonsterAIState.AttackPrepare : HWJ_MonsterAIState.Approach);
    }

    private void RunApproach()
    {
        if (!IsTargetInTrackingRange())
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            return;
        }

        FaceTarget();

        if (IsTargetInAttackRange())
        {
            SetAIState(HWJ_MonsterAIState.AttackPrepare, EnemyData.AI.attackPrepareSeconds);
            return;
        }

        MoveTowardTarget();
    }

    private void RunAttackPrepare()
    {
        StopHorizontalMovement();
        FaceTarget();
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);

        if (!IsTargetInAttackRange())
        {
            SetAIState(HWJ_MonsterAIState.Approach);
            return;
        }

        if (IsStateTimeDone())
        {
            SetAIState(HWJ_MonsterAIState.Attack);
        }
    }

    private void RunAttack()
    {
        StopHorizontalMovement();
        FaceTarget();
        enemyAttackSystem?.SetTarget(target);
        enemyAttackSystem?.TryAutoAttack();
        SetAIState(HWJ_MonsterAIState.Recovery, EnemyData.AI.attackRecoverySeconds);
    }

    private void RunRecovery()
    {
        StopHorizontalMovement();

        if (IsStateTimeDone())
        {
            SetAIState(HWJ_MonsterAIState.Repath, EnemyData.AI.repathSeconds);
        }
    }

    private void RunRepath()
    {
        StopHorizontalMovement();

        if (!IsStateTimeDone())
        {
            return;
        }

        SetAIState(IsTargetInAttackRange() ? HWJ_MonsterAIState.AttackPrepare : HWJ_MonsterAIState.Approach);
    }

    private void SetAIState(HWJ_MonsterAIState nextState)
    {
        SetAIState(nextState, GetDefaultStateDuration(nextState));
    }

    private void SetAIState(HWJ_MonsterAIState nextState, float durationSeconds)
    {
        if (currentState == nextState && Time.time < stateEndTime)
        {
            return;
        }

        currentState = nextState;
        stateEndTime = Time.time + Mathf.Max(0f, durationSeconds);

        switch (currentState)
        {
            case HWJ_MonsterAIState.Approach:
                runtimeStatus?.SetState(HWJ_RuntimeState.Move);
                break;
            case HWJ_MonsterAIState.AttackPrepare:
            case HWJ_MonsterAIState.Attack:
                runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
                break;
            case HWJ_MonsterAIState.HitStun:
                runtimeStatus?.SetState(HWJ_RuntimeState.Hit);
                break;
            case HWJ_MonsterAIState.Dead:
                runtimeStatus?.SetState(HWJ_RuntimeState.Dead);
                break;
            default:
                runtimeStatus?.SetState(HWJ_RuntimeState.Idle);
                break;
        }
    }

    private float GetDefaultStateDuration(HWJ_MonsterAIState state)
    {
        if (EnemyData == null)
        {
            return 0f;
        }

        switch (state)
        {
            case HWJ_MonsterAIState.Idle:
                return EnemyData.AI.idleSeconds;
            case HWJ_MonsterAIState.Detect:
                return EnemyData.AI.detectSeconds;
            case HWJ_MonsterAIState.AttackPrepare:
                return EnemyData.AI.attackPrepareSeconds;
            case HWJ_MonsterAIState.Recovery:
                return EnemyData.AI.attackRecoverySeconds;
            case HWJ_MonsterAIState.Repath:
                return EnemyData.AI.repathSeconds;
            default:
                return 0f;
        }
    }

    private bool IsStateTimeDone()
    {
        return Time.time >= stateEndTime;
    }

    private bool IsTargetInTrackingRange()
    {
        float trackingRange = EnemyData.Tracking.trackingRange > 0f
            ? EnemyData.Tracking.trackingRange
            : fallbackTrackingRange;
        return GetTargetDistance() <= trackingRange;
    }

    private bool IsTargetInAttackRange()
    {
        float attackRange = GetSkillAwareAttackRange();
        return GetTargetDistance() <= attackRange;
    }

    private float GetSkillAwareAttackRange()
    {
        float attackRange = EnemyData.State.attackRange > 0f
            ? EnemyData.State.attackRange
            : fallbackAttackRange;

        if (skillActionSystem == null || EnemyData.SkillCycle == null || EnemyData.SkillCycle.skills == null)
        {
            return attackRange;
        }

        for (int i = 0; i < EnemyData.SkillCycle.skills.Length; i++)
        {
            HWJ_SkillEntryData skillEntry = EnemyData.SkillCycle.skills[i];

            if (!CanUseSkillEntryForRange(skillEntry)
                || !skillActionSystem.TryGetSkillAction(skillEntry.skillId, out HWJ_SkillActionDataSO skillAction))
            {
                continue;
            }

            float skillRange = skillAction.Range > 0f ? skillAction.Range : skillAction.HitRange;
            attackRange = Mathf.Max(attackRange, skillRange);
        }

        return attackRange;
    }

    private bool CanUseSkillEntryForRange(HWJ_SkillEntryData skillEntry)
    {
        if (skillEntry == null || string.IsNullOrEmpty(skillEntry.skillId) || !skillEntry.startsUnlocked)
        {
            return false;
        }

        HWJ_WeaponType weaponType = dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
        return skillEntry.requiredWeaponType == HWJ_WeaponType.None
            || skillEntry.requiredWeaponType == weaponType;
    }

    private float GetTargetDistance()
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        return horizontalMoveOnly
            ? Mathf.Abs(target.position.x - transform.position.x)
            : Vector2.Distance(transform.position, target.position);
    }

    private void MoveTowardTarget()
    {
        if (target == null || runtimeStatus != null && !runtimeStatus.CanMove)
        {
            StopHorizontalMovement();
            return;
        }

        float directionX = Mathf.Sign(target.position.x - transform.position.x);

        if (!CanMoveForward(directionX))
        {
            SetAIState(HWJ_MonsterAIState.Repath, EnemyData.AI.repathSeconds);
            StopHorizontalMovement();
            return;
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return;
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
    }

    private bool CanMoveForward(float directionX)
    {
        if (EnemyData == null || Mathf.Abs(directionX) <= 0.01f)
        {
            return false;
        }

        int layerMask = groundLayer.value != 0 ? groundLayer.value : Physics2D.AllLayers;
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.right * Mathf.Sign(directionX);

        if (EnemyData.Navigation.wallCheckDistance > 0f
            && Physics2D.Raycast(origin, direction, EnemyData.Navigation.wallCheckDistance, layerMask))
        {
            return false;
        }

        if (!EnemyData.Navigation.avoidLedges)
        {
            return true;
        }

        Vector2 ledgeOrigin = origin
            + direction * Mathf.Max(0.05f, EnemyData.Navigation.ledgeCheckForwardDistance)
            + Vector2.down * 0.05f;
        float ledgeDistance = Mathf.Max(0.1f, EnemyData.Navigation.ledgeCheckDownDistance);
        return Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeDistance, layerMask);
    }

    private void FaceTarget()
    {
        if (target == null || motionSystem == null)
        {
            return;
        }

        motionSystem.FaceDirection(target.position.x - transform.position.x);
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

    private void StopHorizontalMovementIfNotKnockedBack()
    {
        if (knockbackSystem != null && knockbackSystem.IsActive)
        {
            return;
        }

        StopHorizontalMovement();
    }

    private bool HandleSkillNavigationBlock()
    {
        if (skillActionSystem == null || !skillActionSystem.IsNavigationBlocked)
        {
            return false;
        }

        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);

        if (skillActionSystem.ShouldStopNavigationMovement)
        {
            StopHorizontalMovementIfNotKnockedBack();
        }

        return true;
    }

    private bool CanUseTargetState()
    {
        if (!targetOnlyBodyState || target == null)
        {
            return true;
        }

        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul == null || targetSoul.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (enemyAttackSystem == null)
        {
            enemyAttackSystem = GetComponent<HWJ_EnemyAttackSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (knockbackSystem == null)
        {
            knockbackSystem = GetComponent<HWJ_KnockbackSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }
}
