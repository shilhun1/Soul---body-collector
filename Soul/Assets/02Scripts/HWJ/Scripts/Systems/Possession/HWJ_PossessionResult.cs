using System;

public enum HWJ_PossessionFailureCode
{
    None,
    NotInSpiritState,
    AlreadyPossessingBody,
    InvalidTarget,
    TargetOutOfRange,
    TargetUnavailable,
    TargetAlreadyPossessed,
    TargetDestroyed,
    PossessionBlocked,
    InsufficientSpiritMental,
    PossessionResisted,
    TransitionInProgress
}

[Serializable]
public readonly struct HWJ_PossessionResult
{
    public readonly bool Succeeded;
    public readonly HWJ_PossessionFailureCode FailureCode;
    public readonly string Message;
    public readonly HWJ_RootObjectDataResolver TargetResolver;

    public HWJ_PossessionResult(
        bool succeeded,
        HWJ_PossessionFailureCode failureCode,
        string message,
        HWJ_RootObjectDataResolver targetResolver)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        Message = message;
        TargetResolver = targetResolver;
    }

    public static HWJ_PossessionResult Success(
        HWJ_RootObjectDataResolver targetResolver,
        string message)
    {
        return new HWJ_PossessionResult(
            true,
            HWJ_PossessionFailureCode.None,
            message,
            targetResolver);
    }

    public static HWJ_PossessionResult Fail(
        HWJ_PossessionFailureCode failureCode,
        string message,
        HWJ_RootObjectDataResolver targetResolver = null)
    {
        return new HWJ_PossessionResult(
            false,
            failureCode,
            message,
            targetResolver);
    }
}
