using System;

public enum HWJ_EnemyAIActionType
{
    None,
    Idle,
    Detect,
    Approach,
    AttackPrepare,
    Attack,
    Recovery,
    Repath,
    HitStun,
    Dead,
    SkillNavigationBlock
}

public enum HWJ_EnemyAIActionFailureCode
{
    None,
    BehaviorDisabled,
    MissingEnemyData,
    MissingTarget,
    TargetStateBlocked,
    TargetOutOfTrackingRange,
    TargetOutOfAttackRange,
    StateTimerActive,
    CannotMove,
    MovementBlocked,
    MissingAttackSystem,
    AttackRequestRejected,
    InvalidState
}

[Serializable]
// Captures why the current monster AI action did or did not advance.
public readonly struct HWJ_EnemyAIActionResult
{
    public readonly bool Succeeded;
    public readonly HWJ_EnemyAIActionType ActionType;
    public readonly HWJ_EnemyAIActionFailureCode FailureCode;
    public readonly HWJ_MonsterAIState State;
    public readonly HWJ_MonsterAIState NextState;
    public readonly float TargetDistance;
    public readonly string Message;

    public HWJ_EnemyAIActionResult(
        bool succeeded,
        HWJ_EnemyAIActionType actionType,
        HWJ_EnemyAIActionFailureCode failureCode,
        HWJ_MonsterAIState state,
        HWJ_MonsterAIState nextState,
        float targetDistance,
        string message)
    {
        Succeeded = succeeded;
        ActionType = actionType;
        FailureCode = failureCode;
        State = state;
        NextState = nextState;
        TargetDistance = targetDistance;
        Message = message;
    }

    public static HWJ_EnemyAIActionResult Success(
        HWJ_EnemyAIActionType actionType,
        HWJ_MonsterAIState state,
        HWJ_MonsterAIState nextState,
        float targetDistance,
        string message)
    {
        return new HWJ_EnemyAIActionResult(
            true,
            actionType,
            HWJ_EnemyAIActionFailureCode.None,
            state,
            nextState,
            targetDistance,
            message);
    }

    public static HWJ_EnemyAIActionResult Fail(
        HWJ_EnemyAIActionType actionType,
        HWJ_EnemyAIActionFailureCode failureCode,
        HWJ_MonsterAIState state,
        HWJ_MonsterAIState nextState,
        float targetDistance,
        string message)
    {
        return new HWJ_EnemyAIActionResult(
            false,
            actionType,
            failureCode,
            state,
            nextState,
            targetDistance,
            message);
    }
}
