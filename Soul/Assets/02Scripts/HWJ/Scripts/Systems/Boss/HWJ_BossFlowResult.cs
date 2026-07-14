using System;

public enum HWJ_BossFlowOperationType
{
    None,
    EvaluateBossBattleStart,
    StartBossBattle,
    MarkBossDefeated,
    UnlockRegionAfterBoss
}

public enum HWJ_BossFlowFailureCode
{
    None,
    MissingStageProgressionSystem,
    MissingBossBrainSystem,
    MissingBossData,
    MissingPlayerSoulSystem,
    MissingPlayerPossessionSystem,
    MissingPlayerStatusSystem,
    MissingBodyDecaySystem,
    MissingSkillUnlockSystem,
    ObjectiveNotComplete,
    BossNotReady,
    BossAlreadyDefeated,
    BossBattleAlreadyStarted,
    BossEntryTriggerInactive,
    PlayerNotPossessed,
    PlayerBodyCollapsing,
    PossessedBodyHpTooLow,
    PossessedBodyDecayTooHigh,
    RequiredWeaponMismatch,
    RequiredBodyTypeMismatch,
    RequiredSkillLocked,
    BodyIdNotAllowed,
    BodyIdBlocked,
    AdditionalRuleFailed,
    StageTransitionFailed
}

[Serializable]
// Result object used by UI, trigger scripts, and tests instead of hiding boss flow failures behind false.
public readonly struct HWJ_BossFlowResult
{
    public readonly bool Succeeded;
    public readonly HWJ_BossFlowOperationType OperationType;
    public readonly HWJ_BossFlowFailureCode FailureCode;
    public readonly HWJ_StageFlowTransitionResult StageTransitionResult;
    public readonly HWJ_RuleExecutionResult RuleExecutionResult;
    public readonly string Message;

    public HWJ_BossFlowResult(
        bool succeeded,
        HWJ_BossFlowOperationType operationType,
        HWJ_BossFlowFailureCode failureCode,
        HWJ_StageFlowTransitionResult stageTransitionResult,
        HWJ_RuleExecutionResult ruleExecutionResult,
        string message)
    {
        Succeeded = succeeded;
        OperationType = operationType;
        FailureCode = failureCode;
        StageTransitionResult = stageTransitionResult;
        RuleExecutionResult = ruleExecutionResult;
        Message = message;
    }

    public static HWJ_BossFlowResult Success(
        HWJ_BossFlowOperationType operationType,
        string message,
        HWJ_StageFlowTransitionResult stageTransitionResult = default(HWJ_StageFlowTransitionResult),
        HWJ_RuleExecutionResult ruleExecutionResult = default(HWJ_RuleExecutionResult))
    {
        return new HWJ_BossFlowResult(
            true,
            operationType,
            HWJ_BossFlowFailureCode.None,
            stageTransitionResult,
            ruleExecutionResult,
            message);
    }

    public static HWJ_BossFlowResult Fail(
        HWJ_BossFlowOperationType operationType,
        HWJ_BossFlowFailureCode failureCode,
        string message,
        HWJ_StageFlowTransitionResult stageTransitionResult = default(HWJ_StageFlowTransitionResult),
        HWJ_RuleExecutionResult ruleExecutionResult = default(HWJ_RuleExecutionResult))
    {
        return new HWJ_BossFlowResult(
            false,
            operationType,
            failureCode,
            stageTransitionResult,
            ruleExecutionResult,
            message);
    }
}
