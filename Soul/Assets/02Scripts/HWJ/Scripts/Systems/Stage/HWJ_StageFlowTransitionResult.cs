public enum HWJ_StageFlowTransitionFailureCode
{
    None,
    SameState,
    InvalidState,
    InvalidTransition,
    TransitionLocked,
    ObjectiveNotComplete,
    BossNotReady,
    BossAlreadyDefeated,
    MissingPlayerSoul,
    PlayerNotPossessed,
    PlayerCollapsing
}

public readonly struct HWJ_StageFlowTransitionResult
{
    public readonly bool Succeeded;
    public readonly HWJ_StageFlowTransitionFailureCode FailureCode;
    public readonly HWJ_StageFlowState PreviousState;
    public readonly HWJ_StageFlowState CurrentState;
    public readonly HWJ_StageFlowState RequestedState;
    public readonly string Message;

    public HWJ_StageFlowTransitionResult(
        bool succeeded,
        HWJ_StageFlowTransitionFailureCode failureCode,
        HWJ_StageFlowState previousState,
        HWJ_StageFlowState currentState,
        HWJ_StageFlowState requestedState,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        PreviousState = previousState;
        CurrentState = currentState;
        RequestedState = requestedState;
        Message = message;
    }

    public static HWJ_StageFlowTransitionResult Success(
        HWJ_StageFlowState previousState,
        HWJ_StageFlowState currentState,
        string message)
    {
        return new HWJ_StageFlowTransitionResult(
            true,
            HWJ_StageFlowTransitionFailureCode.None,
            previousState,
            currentState,
            currentState,
            message);
    }

    public static HWJ_StageFlowTransitionResult Fail(
        HWJ_StageFlowTransitionFailureCode failureCode,
        HWJ_StageFlowState currentState,
        HWJ_StageFlowState requestedState,
        string message)
    {
        return new HWJ_StageFlowTransitionResult(
            false,
            failureCode,
            currentState,
            currentState,
            requestedState,
            message);
    }
}
