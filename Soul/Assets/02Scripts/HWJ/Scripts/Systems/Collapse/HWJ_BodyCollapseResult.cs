public enum HWJ_BodyCollapseReason
{
    Unknown,
    DecayMaxed,
    HpDepleted,
    Forced
}

public enum HWJ_BodyCollapseFailureCode
{
    None,
    MissingSoulSystem,
    NotPossessedState,
    MissingPossessionSystem,
    MissingPossessedBody,
    AlreadyCollapsing,
    AlreadyCollapsed
}

public readonly struct HWJ_BodyCollapseResult
{
    public readonly bool Succeeded;
    public readonly HWJ_BodyCollapseFailureCode FailureCode;
    public readonly HWJ_BodyCollapseReason Reason;
    public readonly string Message;

    public HWJ_BodyCollapseResult(
        bool succeeded,
        HWJ_BodyCollapseFailureCode failureCode,
        HWJ_BodyCollapseReason reason,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        Reason = reason;
        Message = message;
    }

    public static HWJ_BodyCollapseResult Success(HWJ_BodyCollapseReason reason, string message)
    {
        return new HWJ_BodyCollapseResult(true, HWJ_BodyCollapseFailureCode.None, reason, message);
    }

    public static HWJ_BodyCollapseResult Fail(
        HWJ_BodyCollapseFailureCode failureCode,
        HWJ_BodyCollapseReason reason,
        string message)
    {
        return new HWJ_BodyCollapseResult(false, failureCode, reason, message);
    }
}
