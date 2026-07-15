using System;

public enum HWJ_CoreLoopOperationType
{
    None,
    RefreshPossessionTarget,
    PossessDiscoveredBody,
    PossessTarget,
    CollapseBody,
    EnterExploring,
    EnterCombat,
    CompleteObjective,
    UnlockBoss,
    StartBossBattle,
    MarkBossDefeated,
    ClearStage,
    UnlockRegion,
    SetSceneTransition
}

public enum HWJ_CoreLoopFailureCode
{
    None,
    InvalidRequest,
    MissingBodyDiscoverySystem,
    MissingPossessionSystem,
    MissingCollapseSystem,
    MissingStageProgressionSystem,
    MissingBossFlowSystem,
    TransitionInProgress,
    NoPossessionTarget,
    PossessionFailed,
    CollapseFailed,
    StageTransitionFailed,
    BossFlowFailed
}

[Serializable]
public readonly struct HWJ_CoreLoopOperationResult
{
    public readonly bool Succeeded;
    public readonly HWJ_CoreLoopOperationType OperationType;
    public readonly HWJ_CoreLoopFailureCode FailureCode;
    public readonly HWJ_PossessionFailureCode PossessionFailureCode;
    public readonly HWJ_BodyCollapseFailureCode CollapseFailureCode;
    public readonly HWJ_StageFlowTransitionFailureCode StageFailureCode;
    public readonly HWJ_BossFlowFailureCode BossFlowFailureCode;
    public readonly HWJ_RootObjectDataResolver TargetResolver;
    public readonly HWJ_PossessionResult PossessionResult;
    public readonly HWJ_BodyCollapseResult CollapseResult;
    public readonly HWJ_StageFlowTransitionResult StageTransitionResult;
    public readonly HWJ_BossFlowResult BossFlowResult;
    public readonly string Message;

    public HWJ_CoreLoopOperationResult(
        bool succeeded,
        HWJ_CoreLoopOperationType operationType,
        HWJ_CoreLoopFailureCode failureCode,
        HWJ_PossessionFailureCode possessionFailureCode,
        HWJ_BodyCollapseFailureCode collapseFailureCode,
        HWJ_StageFlowTransitionFailureCode stageFailureCode,
        HWJ_BossFlowFailureCode bossFlowFailureCode,
        HWJ_RootObjectDataResolver targetResolver,
        HWJ_PossessionResult possessionResult,
        HWJ_BodyCollapseResult collapseResult,
        HWJ_StageFlowTransitionResult stageTransitionResult,
        HWJ_BossFlowResult bossFlowResult,
        string message)
    {
        Succeeded = succeeded;
        OperationType = operationType;
        FailureCode = failureCode;
        PossessionFailureCode = possessionFailureCode;
        CollapseFailureCode = collapseFailureCode;
        StageFailureCode = stageFailureCode;
        BossFlowFailureCode = bossFlowFailureCode;
        TargetResolver = targetResolver;
        PossessionResult = possessionResult;
        CollapseResult = collapseResult;
        StageTransitionResult = stageTransitionResult;
        BossFlowResult = bossFlowResult;
        Message = message;
    }

    public static HWJ_CoreLoopOperationResult Success(
        HWJ_CoreLoopOperationType operationType,
        string message,
        HWJ_RootObjectDataResolver targetResolver = null)
    {
        return new HWJ_CoreLoopOperationResult(
            true,
            operationType,
            HWJ_CoreLoopFailureCode.None,
            HWJ_PossessionFailureCode.None,
            HWJ_BodyCollapseFailureCode.None,
            HWJ_StageFlowTransitionFailureCode.None,
            HWJ_BossFlowFailureCode.None,
            targetResolver,
            default(HWJ_PossessionResult),
            default(HWJ_BodyCollapseResult),
            default(HWJ_StageFlowTransitionResult),
            default(HWJ_BossFlowResult),
            message);
    }

    public static HWJ_CoreLoopOperationResult Fail(
        HWJ_CoreLoopOperationType operationType,
        HWJ_CoreLoopFailureCode failureCode,
        string message,
        HWJ_RootObjectDataResolver targetResolver = null)
    {
        return new HWJ_CoreLoopOperationResult(
            false,
            operationType,
            failureCode,
            HWJ_PossessionFailureCode.None,
            HWJ_BodyCollapseFailureCode.None,
            HWJ_StageFlowTransitionFailureCode.None,
            HWJ_BossFlowFailureCode.None,
            targetResolver,
            default(HWJ_PossessionResult),
            default(HWJ_BodyCollapseResult),
            default(HWJ_StageFlowTransitionResult),
            default(HWJ_BossFlowResult),
            message);
    }

    public static HWJ_CoreLoopOperationResult FromPossession(
        HWJ_CoreLoopOperationType operationType,
        HWJ_PossessionResult possessionResult)
    {
        return new HWJ_CoreLoopOperationResult(
            possessionResult.Succeeded,
            operationType,
            possessionResult.Succeeded ? HWJ_CoreLoopFailureCode.None : HWJ_CoreLoopFailureCode.PossessionFailed,
            possessionResult.FailureCode,
            HWJ_BodyCollapseFailureCode.None,
            HWJ_StageFlowTransitionFailureCode.None,
            HWJ_BossFlowFailureCode.None,
            possessionResult.TargetResolver,
            possessionResult,
            default(HWJ_BodyCollapseResult),
            default(HWJ_StageFlowTransitionResult),
            default(HWJ_BossFlowResult),
            possessionResult.Message);
    }

    public static HWJ_CoreLoopOperationResult FromCollapse(
        HWJ_CoreLoopOperationType operationType,
        HWJ_BodyCollapseResult collapseResult)
    {
        return new HWJ_CoreLoopOperationResult(
            collapseResult.Succeeded,
            operationType,
            collapseResult.Succeeded ? HWJ_CoreLoopFailureCode.None : HWJ_CoreLoopFailureCode.CollapseFailed,
            HWJ_PossessionFailureCode.None,
            collapseResult.FailureCode,
            HWJ_StageFlowTransitionFailureCode.None,
            HWJ_BossFlowFailureCode.None,
            null,
            default(HWJ_PossessionResult),
            collapseResult,
            default(HWJ_StageFlowTransitionResult),
            default(HWJ_BossFlowResult),
            collapseResult.Message);
    }

    public static HWJ_CoreLoopOperationResult FromStageTransition(
        HWJ_CoreLoopOperationType operationType,
        HWJ_StageFlowTransitionResult stageResult)
    {
        return new HWJ_CoreLoopOperationResult(
            stageResult.Succeeded,
            operationType,
            stageResult.Succeeded ? HWJ_CoreLoopFailureCode.None : HWJ_CoreLoopFailureCode.StageTransitionFailed,
            HWJ_PossessionFailureCode.None,
            HWJ_BodyCollapseFailureCode.None,
            stageResult.FailureCode,
            HWJ_BossFlowFailureCode.None,
            null,
            default(HWJ_PossessionResult),
            default(HWJ_BodyCollapseResult),
            stageResult,
            default(HWJ_BossFlowResult),
            stageResult.Message);
    }

    public static HWJ_CoreLoopOperationResult FromBossFlow(
        HWJ_CoreLoopOperationType operationType,
        HWJ_BossFlowResult bossFlowResult)
    {
        return new HWJ_CoreLoopOperationResult(
            bossFlowResult.Succeeded,
            operationType,
            bossFlowResult.Succeeded ? HWJ_CoreLoopFailureCode.None : HWJ_CoreLoopFailureCode.BossFlowFailed,
            HWJ_PossessionFailureCode.None,
            HWJ_BodyCollapseFailureCode.None,
            bossFlowResult.StageTransitionResult.FailureCode,
            bossFlowResult.FailureCode,
            null,
            default(HWJ_PossessionResult),
            default(HWJ_BodyCollapseResult),
            bossFlowResult.StageTransitionResult,
            bossFlowResult,
            bossFlowResult.Message);
    }
}
