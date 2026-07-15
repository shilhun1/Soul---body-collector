using System;

public enum HWJ_EnemyAITransitionFailureCode
{
    None,
    InvalidState,
    SameStateTimerActive
}

[Serializable]
// Carries an explicit success or failure reason for a monster AI state request.
public readonly struct HWJ_EnemyAITransitionResult
{
    public readonly bool Succeeded;
    public readonly HWJ_EnemyAITransitionFailureCode FailureCode;
    public readonly HWJ_MonsterAIState PreviousState;
    public readonly HWJ_MonsterAIState CurrentState;
    public readonly HWJ_MonsterAIState RequestedState;
    public readonly float DurationSeconds;
    public readonly float StateEndTime;
    public readonly string Message;

    public HWJ_EnemyAITransitionResult(
        bool succeeded,
        HWJ_EnemyAITransitionFailureCode failureCode,
        HWJ_MonsterAIState previousState,
        HWJ_MonsterAIState currentState,
        HWJ_MonsterAIState requestedState,
        float durationSeconds,
        float stateEndTime,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        PreviousState = previousState;
        CurrentState = currentState;
        RequestedState = requestedState;
        DurationSeconds = durationSeconds;
        StateEndTime = stateEndTime;
        Message = message;
    }

    // Use this factory only after the monster AI state has actually accepted the request.
    public static HWJ_EnemyAITransitionResult Success(
        HWJ_MonsterAIState previousState,
        HWJ_MonsterAIState currentState,
        float durationSeconds,
        float stateEndTime,
        string message)
    {
        return new HWJ_EnemyAITransitionResult(
            true,
            HWJ_EnemyAITransitionFailureCode.None,
            previousState,
            currentState,
            currentState,
            durationSeconds,
            stateEndTime,
            message);
    }

    // Use this factory when another system needs to know why an AI transition was rejected.
    public static HWJ_EnemyAITransitionResult Fail(
        HWJ_EnemyAITransitionFailureCode failureCode,
        HWJ_MonsterAIState currentState,
        HWJ_MonsterAIState requestedState,
        float stateEndTime,
        string message)
    {
        return new HWJ_EnemyAITransitionResult(
            false,
            failureCode,
            currentState,
            currentState,
            requestedState,
            0f,
            stateEndTime,
            message);
    }
}
