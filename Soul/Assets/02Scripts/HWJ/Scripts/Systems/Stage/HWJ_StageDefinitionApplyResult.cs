public enum HWJ_StageDefinitionApplyFailureCode
{
    None,
    MissingStageDefinition,
    MissingStageId,
    MissingRegionId,
    RegionDefinitionMismatch,
    MissingBossId,
    MissingUnlockRegionId,
    MissingProgressionData,
    MissingClearedStageRequirement,
    MissingUnlockedRegionRequirement
}

public readonly struct HWJ_StageDefinitionApplyResult
{
    public readonly bool Succeeded;
    public readonly HWJ_StageDefinitionApplyFailureCode FailureCode;
    public readonly string StageId;
    public readonly string RegionId;
    public readonly string MissingRequirementId;
    public readonly string Message;

    public HWJ_StageDefinitionApplyResult(
        bool succeeded,
        HWJ_StageDefinitionApplyFailureCode failureCode,
        string stageId,
        string regionId,
        string missingRequirementId,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        StageId = stageId;
        RegionId = regionId;
        MissingRequirementId = missingRequirementId;
        Message = message;
    }

    public static HWJ_StageDefinitionApplyResult Success(
        string stageId,
        string regionId,
        string message)
    {
        return new HWJ_StageDefinitionApplyResult(
            true,
            HWJ_StageDefinitionApplyFailureCode.None,
            stageId,
            regionId,
            null,
            message);
    }

    public static HWJ_StageDefinitionApplyResult Fail(
        HWJ_StageDefinitionApplyFailureCode failureCode,
        string stageId,
        string regionId,
        string missingRequirementId,
        string message)
    {
        return new HWJ_StageDefinitionApplyResult(
            false,
            failureCode,
            stageId,
            regionId,
            missingRequirementId,
            message);
    }
}
