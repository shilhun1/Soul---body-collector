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
    [SerializeField] private bool useGameplayTargetRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO targetExecutionCore;
    [SerializeField] private string targetExecutionCoreId = "monster_target_execution";
    [SerializeField] private HWJ_GameplayRuleSO targetDetectRule;
    [SerializeField] private string targetDetectRuleId = "monster_detect_player_body";
    [SerializeField] private string lastTargetRuleResult;
    [SerializeField] private bool raiseActionResultEvents = true;
    [SerializeField] private bool raiseRepeatedActionResultEvents;
    [SerializeField] private HWJ_MonsterAIState currentState = HWJ_MonsterAIState.Idle;
    [SerializeField] private HWJ_EnemyAITransitionFailureCode lastTransitionFailureCode;
    [SerializeField] private string lastTransitionMessage;
    [SerializeField] private HWJ_EnemyAIActionFailureCode lastActionFailureCode;
    [SerializeField] private string lastActionMessage;

    public HWJ_EnemyTypeDataSO EnemyData { get; private set; }
    public bool DrivesBehavior => driveBehavior;
    public HWJ_MonsterAIState CurrentState => currentState;
    public Transform Target => target;
    public float CurrentTargetDistance => GetTargetDistance();
    public bool TargetInTrackingRange => EnemyData != null && target != null && IsTargetInTrackingRange();
    public bool TargetInAttackRange => EnemyData != null && target != null && IsTargetInAttackRange();
    public string LastTargetRuleResult => lastTargetRuleResult;
    public bool RaiseActionResultEvents => raiseActionResultEvents;
    public bool RaiseRepeatedActionResultEvents => raiseRepeatedActionResultEvents;
    public HWJ_EnemyAITransitionResult LastTransitionResult { get; private set; }
    public HWJ_EnemyAITransitionFailureCode LastTransitionFailureCode => lastTransitionFailureCode;
    public string LastTransitionMessage => lastTransitionMessage;
    public HWJ_EnemyAIActionResult LastActionResult { get; private set; }
    public HWJ_EnemyAIActionFailureCode LastActionFailureCode => lastActionFailureCode;
    public string LastActionMessage => lastActionMessage;

    private float stateEndTime;
    private float nextTargetSearchTime;
    private float nextDecisionTime;
    private bool hasStoredActionResult;

    private void Awake()
    {
        CacheReferences();
        RefreshData();
    }

    private void Update()
    {
        if (!driveBehavior)
        {
            StoreActionResult(CreateActionFailure(
                HWJ_EnemyAIActionType.Idle,
                HWJ_EnemyAIActionFailureCode.BehaviorDisabled,
                currentState,
                "Monster AI action skipped: behavior driving is disabled."));
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
            StoreActionResult(CreateActionSuccess(
                HWJ_EnemyAIActionType.Dead,
                HWJ_MonsterAIState.Dead,
                "Monster AI action resolved: monster is dead."));
            return;
        }

        if (runtimeStatus != null && runtimeStatus.IsHitStunned)
        {
            SetAIState(HWJ_MonsterAIState.HitStun);
            StopHorizontalMovementIfNotKnockedBack();
            StoreActionResult(CreateActionSuccess(
                HWJ_EnemyAIActionType.HitStun,
                HWJ_MonsterAIState.HitStun,
                "Monster AI action resolved: monster is hit stunned."));
            return;
        }

        if (HandleSkillNavigationBlock())
        {
            return;
        }

        if (EnemyData == null)
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            StopHorizontalMovement();
            StoreActionResult(CreateActionFailure(
                HWJ_EnemyAIActionType.Idle,
                HWJ_EnemyAIActionFailureCode.MissingEnemyData,
                HWJ_MonsterAIState.Idle,
                "Monster AI action failed: enemy type data is missing."));
            return;
        }

        if (target == null)
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            StopHorizontalMovement();
            StoreActionResult(CreateActionFailure(
                HWJ_EnemyAIActionType.Idle,
                HWJ_EnemyAIActionFailureCode.MissingTarget,
                HWJ_MonsterAIState.Idle,
                "Monster AI action failed: target is missing."));
            return;
        }

        if (!CanUseTargetState())
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            StopHorizontalMovement();
            StoreActionResult(CreateActionFailure(
                HWJ_EnemyAIActionType.Idle,
                HWJ_EnemyAIActionFailureCode.TargetStateBlocked,
                HWJ_MonsterAIState.Idle,
                "Monster AI action failed: target state is blocked by AI target rules."));
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

    public bool IsTargetBodyStateForRule()
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

    private void RunState()
    {
        HWJ_EnemyAIActionResult actionResult;

        switch (currentState)
        {
            case HWJ_MonsterAIState.Idle:
                actionResult = RunIdle();
                break;
            case HWJ_MonsterAIState.Detect:
                actionResult = RunDetect();
                break;
            case HWJ_MonsterAIState.Approach:
                actionResult = RunApproach();
                break;
            case HWJ_MonsterAIState.AttackPrepare:
                actionResult = RunAttackPrepare();
                break;
            case HWJ_MonsterAIState.Attack:
                actionResult = RunAttack();
                break;
            case HWJ_MonsterAIState.Recovery:
                actionResult = RunRecovery();
                break;
            case HWJ_MonsterAIState.Repath:
                actionResult = RunRepath();
                break;
            case HWJ_MonsterAIState.HitStun:
                SetAIState(HWJ_MonsterAIState.Approach);
                actionResult = CreateActionSuccess(
                    HWJ_EnemyAIActionType.HitStun,
                    HWJ_MonsterAIState.Approach,
                    "Monster AI action resolved: hit stun ended and AI returned to approach.");
                break;
            case HWJ_MonsterAIState.Dead:
                StopHorizontalMovement();
                actionResult = CreateActionSuccess(
                    HWJ_EnemyAIActionType.Dead,
                    HWJ_MonsterAIState.Dead,
                    "Monster AI action resolved: dead state keeps movement stopped.");
                break;
            default:
                actionResult = CreateActionFailure(
                    HWJ_EnemyAIActionType.None,
                    HWJ_EnemyAIActionFailureCode.InvalidState,
                    currentState,
                    "Monster AI action failed: current state is invalid.");
                break;
        }

        StoreActionResult(actionResult);
    }

    private HWJ_EnemyAIActionResult RunIdle()
    {
        StopHorizontalMovement();
        runtimeStatus?.SetState(HWJ_RuntimeState.Idle);

        if (IsTargetInTrackingRange())
        {
            SetAIState(HWJ_MonsterAIState.Detect, EnemyData.AI.detectSeconds);
            return CreateActionSuccess(
                HWJ_EnemyAIActionType.Idle,
                HWJ_MonsterAIState.Detect,
                "Monster AI action succeeded: target entered tracking range.");
        }

        return CreateActionFailure(
            HWJ_EnemyAIActionType.Idle,
            HWJ_EnemyAIActionFailureCode.TargetOutOfTrackingRange,
            HWJ_MonsterAIState.Idle,
            "Monster AI action waiting: target is outside tracking range.");
    }

    private HWJ_EnemyAIActionResult RunDetect()
    {
        StopHorizontalMovement();
        FaceTarget();

        if (!IsStateTimeDone())
        {
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Detect,
                HWJ_EnemyAIActionFailureCode.StateTimerActive,
                HWJ_MonsterAIState.Detect,
                "Monster AI action waiting: detect timer is still active.");
        }

        HWJ_MonsterAIState nextState = IsTargetInAttackRange()
            ? HWJ_MonsterAIState.AttackPrepare
            : HWJ_MonsterAIState.Approach;
        SetAIState(nextState);
        return CreateActionSuccess(
            HWJ_EnemyAIActionType.Detect,
            nextState,
            "Monster AI action succeeded: detection finished.");
    }

    private HWJ_EnemyAIActionResult RunApproach()
    {
        if (!IsTargetInTrackingRange())
        {
            SetAIState(HWJ_MonsterAIState.Idle);
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Approach,
                HWJ_EnemyAIActionFailureCode.TargetOutOfTrackingRange,
                HWJ_MonsterAIState.Idle,
                "Monster AI action failed: target left tracking range.");
        }

        FaceTarget();

        if (IsTargetInAttackRange())
        {
            SetAIState(HWJ_MonsterAIState.AttackPrepare, EnemyData.AI.attackPrepareSeconds);
            return CreateActionSuccess(
                HWJ_EnemyAIActionType.Approach,
                HWJ_MonsterAIState.AttackPrepare,
                "Monster AI action succeeded: target reached attack range.");
        }

        return MoveTowardTarget();
    }

    private HWJ_EnemyAIActionResult RunAttackPrepare()
    {
        StopHorizontalMovement();
        FaceTarget();
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);

        if (!IsTargetInAttackRange())
        {
            SetAIState(HWJ_MonsterAIState.Approach);
            return CreateActionFailure(
                HWJ_EnemyAIActionType.AttackPrepare,
                HWJ_EnemyAIActionFailureCode.TargetOutOfAttackRange,
                HWJ_MonsterAIState.Approach,
                "Monster AI action failed: target left attack range during prepare.");
        }

        if (IsStateTimeDone())
        {
            SetAIState(HWJ_MonsterAIState.Attack);
            return CreateActionSuccess(
                HWJ_EnemyAIActionType.AttackPrepare,
                HWJ_MonsterAIState.Attack,
                "Monster AI action succeeded: attack prepare timer finished.");
        }

        return CreateActionFailure(
            HWJ_EnemyAIActionType.AttackPrepare,
            HWJ_EnemyAIActionFailureCode.StateTimerActive,
            HWJ_MonsterAIState.AttackPrepare,
            "Monster AI action waiting: attack prepare timer is still active.");
    }

    private HWJ_EnemyAIActionResult RunAttack()
    {
        StopHorizontalMovement();
        FaceTarget();
        enemyAttackSystem?.SetTarget(target);
        bool attackSucceeded = enemyAttackSystem != null && enemyAttackSystem.TryAutoAttack();
        SetAIState(HWJ_MonsterAIState.Recovery, EnemyData.AI.attackRecoverySeconds);

        if (enemyAttackSystem == null)
        {
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Attack,
                HWJ_EnemyAIActionFailureCode.MissingAttackSystem,
                HWJ_MonsterAIState.Recovery,
                "Monster AI action failed: enemy attack system is missing.");
        }

        if (!attackSucceeded)
        {
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Attack,
                HWJ_EnemyAIActionFailureCode.AttackRequestRejected,
                HWJ_MonsterAIState.Recovery,
                enemyAttackSystem.LastAttackResult);
        }

        return CreateActionSuccess(
            HWJ_EnemyAIActionType.Attack,
            HWJ_MonsterAIState.Recovery,
            enemyAttackSystem.LastAttackResult);
    }

    private HWJ_EnemyAIActionResult RunRecovery()
    {
        StopHorizontalMovement();

        if (IsStateTimeDone())
        {
            SetAIState(HWJ_MonsterAIState.Repath, EnemyData.AI.repathSeconds);
            return CreateActionSuccess(
                HWJ_EnemyAIActionType.Recovery,
                HWJ_MonsterAIState.Repath,
                "Monster AI action succeeded: recovery timer finished.");
        }

        return CreateActionFailure(
            HWJ_EnemyAIActionType.Recovery,
            HWJ_EnemyAIActionFailureCode.StateTimerActive,
            HWJ_MonsterAIState.Recovery,
            "Monster AI action waiting: recovery timer is still active.");
    }

    private HWJ_EnemyAIActionResult RunRepath()
    {
        StopHorizontalMovement();

        if (!IsStateTimeDone())
        {
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Repath,
                HWJ_EnemyAIActionFailureCode.StateTimerActive,
                HWJ_MonsterAIState.Repath,
                "Monster AI action waiting: repath timer is still active.");
        }

        HWJ_MonsterAIState nextState = IsTargetInAttackRange()
            ? HWJ_MonsterAIState.AttackPrepare
            : HWJ_MonsterAIState.Approach;
        SetAIState(nextState);
        return CreateActionSuccess(
            HWJ_EnemyAIActionType.Repath,
            nextState,
            "Monster AI action succeeded: repath timer finished.");
    }

    public HWJ_EnemyAITransitionResult TrySetAIState(HWJ_MonsterAIState nextState)
    {
        return TrySetAIState(nextState, GetDefaultStateDuration(nextState));
    }

    // External systems should use this instead of changing currentState directly.
    public HWJ_EnemyAITransitionResult TrySetAIState(
        HWJ_MonsterAIState nextState,
        float durationSeconds)
    {
        return SetAIState(nextState, durationSeconds);
    }

    private HWJ_EnemyAITransitionResult SetAIState(HWJ_MonsterAIState nextState)
    {
        return SetAIState(nextState, GetDefaultStateDuration(nextState));
    }

    private HWJ_EnemyAITransitionResult SetAIState(HWJ_MonsterAIState nextState, float durationSeconds)
    {
        // Invalid enum values can happen when data or tools pass a stale serialized value.
        if (!System.Enum.IsDefined(typeof(HWJ_MonsterAIState), nextState))
        {
            return StoreTransitionResult(
                HWJ_EnemyAITransitionResult.Fail(
                    HWJ_EnemyAITransitionFailureCode.InvalidState,
                    currentState,
                    nextState,
                    stateEndTime,
                    $"Monster AI transition failed: {nextState} is not a valid state."),
                false);
        }

        if (currentState == nextState && Time.time < stateEndTime)
        {
            return StoreTransitionResult(
                HWJ_EnemyAITransitionResult.Fail(
                    HWJ_EnemyAITransitionFailureCode.SameStateTimerActive,
                    currentState,
                    nextState,
                    stateEndTime,
                    $"Monster AI transition skipped: {nextState} timer is still active."),
                false);
        }

        HWJ_MonsterAIState previousState = currentState;
        currentState = nextState;
        float safeDuration = Mathf.Max(0f, durationSeconds);
        stateEndTime = Time.time + safeDuration;
        runtimeStatus?.SetState(HWJ_FSMStateUtility.ToRuntimeState(currentState));

        bool stateChanged = previousState != currentState;
        return StoreTransitionResult(
            HWJ_EnemyAITransitionResult.Success(
                previousState,
                currentState,
                safeDuration,
                stateEndTime,
                stateChanged
                    ? $"Monster AI transitioned from {previousState} to {currentState}."
                    : $"Monster AI refreshed {currentState}."),
            stateChanged);
    }

    private float GetDefaultStateDuration(HWJ_MonsterAIState state)
    {
        return HWJ_FSMStateUtility.GetDefaultMonsterStateDuration(state, EnemyData);
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
        float attackRange = GetAttackStartRange();
        return GetTargetDistance() <= attackRange;
    }

    // 스킬 사거리는 실제 스킬 실행 가능 여부에만 쓰고, AI 접근을 멈추는 기준은 몬스터 포지셔닝 데이터로 제한합니다.
    private float GetAttackStartRange()
    {
        if (EnemyData.State.attackRange > 0f)
        {
            return EnemyData.State.attackRange;
        }

        if (EnemyData.Navigation.stoppingDistance > 0f)
        {
            return EnemyData.Navigation.stoppingDistance;
        }

        return fallbackAttackRange;
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

    private HWJ_EnemyAIActionResult MoveTowardTarget()
    {
        if (target == null)
        {
            StopHorizontalMovement();
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Approach,
                HWJ_EnemyAIActionFailureCode.MissingTarget,
                HWJ_MonsterAIState.Approach,
                "Monster AI action failed: target disappeared while approaching.");
        }

        if (runtimeStatus != null && !runtimeStatus.CanMove)
        {
            StopHorizontalMovement();
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Approach,
                HWJ_EnemyAIActionFailureCode.CannotMove,
                HWJ_MonsterAIState.Approach,
                "Monster AI action failed: runtime status cannot move.");
        }

        float directionX = Mathf.Sign(target.position.x - transform.position.x);

        if (!CanMoveForward(directionX))
        {
            SetAIState(HWJ_MonsterAIState.Repath, EnemyData.AI.repathSeconds);
            StopHorizontalMovement();
            return CreateActionFailure(
                HWJ_EnemyAIActionType.Approach,
                HWJ_EnemyAIActionFailureCode.MovementBlocked,
                HWJ_MonsterAIState.Repath,
                "Monster AI action failed: forward movement is blocked.");
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return CreateActionSuccess(
                HWJ_EnemyAIActionType.Approach,
                HWJ_MonsterAIState.Approach,
                "Monster AI action succeeded: moving toward target.");
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
        return CreateActionSuccess(
            HWJ_EnemyAIActionType.Approach,
            HWJ_MonsterAIState.Approach,
            "Monster AI action succeeded: moving transform toward target.");
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

        StoreActionResult(CreateActionSuccess(
            HWJ_EnemyAIActionType.SkillNavigationBlock,
            currentState,
            "Monster AI action held: skill action is controlling navigation."));
        return true;
    }

    private bool CanUseTargetState()
    {
        if (!targetOnlyBodyState || target == null)
        {
            return true;
        }

        if (useGameplayTargetRule)
        {
            HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

            if (targetResolver == null)
            {
                targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
            }

            HWJ_GameplayContext context = HWJ_GameplayContext
                .Create(dataResolver, targetResolver)
                .WithSource(this);

            if (targetResolver != null)
            {
                context.WithTarget(targetResolver);
            }

            if (ResolveTargetExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
            {
                bool corePassed = executionCore.TryExecute(context, out HWJ_RuleExecutionResult executionResult);
                lastTargetRuleResult = executionResult.Message;
                return corePassed;
            }

            if (IsTargetRuleAvailable(out HWJ_GameplayRuleSO rule))
            {
                bool passed = rule.TryEvaluate(context, out HWJ_RuleEvaluationResult result);
                lastTargetRuleResult = result.Message;
                return passed;
            }
        }

        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul == null || targetSoul.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private bool ResolveTargetExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (targetExecutionCore != null)
        {
            executionCore = targetExecutionCore;
            return true;
        }

        if (string.IsNullOrEmpty(targetExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(targetExecutionCoreId, out executionCore);
    }

    private bool IsTargetRuleAvailable(out HWJ_GameplayRuleSO rule)
    {
        rule = targetDetectRule;

        if (rule != null)
        {
            return true;
        }

        return !string.IsNullOrEmpty(targetDetectRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(targetDetectRuleId, out rule);
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

    private HWJ_EnemyAITransitionResult StoreTransitionResult(
        HWJ_EnemyAITransitionResult result,
        bool raiseEvent)
    {
        LastTransitionResult = result;
        lastTransitionFailureCode = result.FailureCode;
        lastTransitionMessage = result.Message;

        if (raiseEvent)
        {
            HWJ_GameplayEvents.RaiseEnemyAIStateTransitioned(
                new HWJ_EnemyAITransitionEvent(this, result));
        }

        return result;
    }

    private HWJ_EnemyAIActionResult StoreActionResult(HWJ_EnemyAIActionResult result)
    {
        bool shouldRaiseEvent = ShouldRaiseActionResultEvent(result);
        LastActionResult = result;
        lastActionFailureCode = result.FailureCode;
        lastActionMessage = result.Message;
        hasStoredActionResult = true;

        if (shouldRaiseEvent)
        {
            HWJ_GameplayEvents.RaiseEnemyAIActionResolved(
                new HWJ_EnemyAIActionEvent(this, result));
        }

        return result;
    }

    private bool ShouldRaiseActionResultEvent(HWJ_EnemyAIActionResult result)
    {
        if (!raiseActionResultEvents)
        {
            return false;
        }

        if (raiseRepeatedActionResultEvents || !hasStoredActionResult)
        {
            return true;
        }

        return LastActionResult.Succeeded != result.Succeeded
            || LastActionResult.ActionType != result.ActionType
            || LastActionResult.FailureCode != result.FailureCode
            || LastActionResult.State != result.State
            || LastActionResult.NextState != result.NextState;
    }

    private HWJ_EnemyAIActionResult CreateActionSuccess(
        HWJ_EnemyAIActionType actionType,
        HWJ_MonsterAIState nextState,
        string message)
    {
        return HWJ_EnemyAIActionResult.Success(
            actionType,
            currentState,
            nextState,
            GetTargetDistance(),
            message);
    }

    private HWJ_EnemyAIActionResult CreateActionFailure(
        HWJ_EnemyAIActionType actionType,
        HWJ_EnemyAIActionFailureCode failureCode,
        HWJ_MonsterAIState nextState,
        string message)
    {
        return HWJ_EnemyAIActionResult.Fail(
            actionType,
            failureCode,
            currentState,
            nextState,
            GetTargetDistance(),
            message);
    }
}
