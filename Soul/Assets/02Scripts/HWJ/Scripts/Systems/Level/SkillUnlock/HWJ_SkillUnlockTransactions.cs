using UnityEngine;

/// <summary>
/// Validates and performs skill or node unlock transactions and skill-point spending.
/// This partial belongs to the single HWJ_SkillUnlockSystem component.
/// </summary>
public partial class HWJ_SkillUnlockSystem
{
    public HWJ_SkillUnlockResult TryUnlockSkill(string skillId)
    {
        return TryUnlockSkill(skillId, true);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNode(string nodeId)
    {
        return TryUnlockSkillNode(nodeId, true);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNode(string nodeId, bool spendSkillPoint)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(nodeId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillNodeId,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill node unlock failed: missing node id."));
        }

        if (!ResolveGameplayDatabase())
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingGameplayDatabase,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill node unlock failed: missing gameplay database."));
        }

        if (!gameplayDatabase.TryGetSkillNode(nodeId, out HWJ_SkillNodeDataSO skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNodeNotFound,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is not registered.",
                null));
        }

        if (IsSkillNodeUnlocked(skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is already unlocked.",
                skillNode));
        }

        if (GetCurrentLevel() < skillNode.RequiredLevel)
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.LevelTooLow,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} requires level {skillNode.RequiredLevel}.",
                skillNode));
        }

        if (!IsPreviousSkillNodeRequirementMet(skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.PreviousSkillNodeLocked,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} requires previous node {skillNode.PrerequisiteNodeId}.",
                skillNode));
        }

        int skillPointCost = skillNode.SkillPointCost;

        if (spendSkillPoint && skillPointCost > 0)
        {
            if (playerLevelProgress == null)
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.MissingLevelSystem,
                    nodeId,
                    null,
                    skillPointCost,
                    0,
                    $"Skill node unlock failed: {nodeId} requires a level system.",
                    skillNode));
            }

            if (!playerLevelProgress.TrySpendSkillPoint(skillPointCost))
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.NotEnoughSkillPoint,
                    nodeId,
                    null,
                    skillPointCost,
                    playerLevelProgress.SkillPoint,
                    $"Skill node unlock failed: {nodeId} requires {skillPointCost} skill points.",
                    skillNode));
            }
        }

        if (!AddUnlockedSkillNodeId(skillNode.NodeId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                nodeId,
                null,
                skillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is already unlocked.",
                skillNode));
        }

        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();
        lastSkillNodeMessage = $"Skill node unlocked: {nodeId}. Common step {cachedCommonUnlockedSkillStep}.";

        return RaiseUnlocked(
            skillNode.NodeId,
            null,
            skillPointCost,
            skillPointCost > 0 ? "Skill node unlocked by spending skill points." : "Skill node unlocked.",
            skillNode);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNodeForSkillAction(string skillActionId, bool spendSkillPoint = true)
    {
        if (!TryGetSkillNodeForSkillAction(skillActionId, out HWJ_SkillNodeDataSO skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNodeNotFound,
                skillActionId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: no node is linked to {skillActionId}."));
        }

        return TryUnlockSkillNode(skillNode.NodeId, spendSkillPoint);
    }

    public HWJ_SkillUnlockResult TryUnlockSkill(string skillId, bool spendSkillPoint)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill unlock failed: missing skill id."));
        }

        if (!TryFindSkillEntry(skillId, out HWJ_SkillEntryData definitionEntry))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNotFound,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} is not in player skill set."));
        }

        if (IsSkillUnlocked(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} is already unlocked."));
        }

        if (!IsLevelRequirementMet(definitionEntry))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.LevelTooLow,
                skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} requires level {definitionEntry.unlockLevel}."));
        }

        int skillPointCost = Mathf.Max(0, definitionEntry.requiredSkillPoint);

        if (spendSkillPoint && skillPointCost > 0)
        {
            if (playerLevelProgress == null)
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.MissingLevelSystem,
                    skillId,
                    definitionEntry,
                    skillPointCost,
                    0,
                    $"Skill unlock failed: {skillId} requires a level system."));
            }

            if (!playerLevelProgress.TrySpendSkillPoint(skillPointCost))
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.NotEnoughSkillPoint,
                    skillId,
                    definitionEntry,
                    skillPointCost,
                    playerLevelProgress.SkillPoint,
                    $"Skill unlock failed: {skillId} requires {skillPointCost} skill points."));
            }
        }

        return UnlockWithoutCost(
            definitionEntry,
            skillPointCost > 0 ? "Skill unlocked by spending skill points." : "Skill unlocked.");
    }

    /// <summary>
    /// 보상, 디버그, 이벤트 연출처럼 조건을 우회해야 하는 경우에만 사용합니다.
    /// </summary>
    public HWJ_SkillUnlockResult ForceUnlockSkill(string skillId, string reason = null)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Force unlock failed: missing skill id."));
        }

        if (IsSkillUnlocked(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Force unlock failed: {skillId} is already unlocked."));
        }

        AddUnlockedSkillId(skillId);
        return RaiseUnlocked(skillId, null, 0, reason ?? "Skill force unlocked.");
    }

    private HWJ_SkillUnlockResult UnlockWithoutCost(HWJ_SkillEntryData definitionEntry, string message)
    {
        if (definitionEntry == null || string.IsNullOrEmpty(definitionEntry.skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                null,
                definitionEntry,
                0,
                GetRemainingSkillPoint(),
                "Skill unlock failed: missing skill entry."));
        }

        if (!AddUnlockedSkillId(definitionEntry.skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                definitionEntry.skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {definitionEntry.skillId} is already unlocked."));
        }

        return RaiseUnlocked(
            definitionEntry.skillId,
            definitionEntry,
            Mathf.Max(0, definitionEntry.requiredSkillPoint),
            message);
    }

    private HWJ_SkillUnlockResult RaiseUnlocked(
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        string message,
        HWJ_SkillNodeDataSO skillNode = null)
    {
        HWJ_SkillUnlockResult unlockResult = HWJ_SkillUnlockResult.Success(
            skillId,
            definitionEntry,
            skillPointCost,
            GetRemainingSkillPoint(),
            message,
            skillNode);
        SetLastResult(unlockResult);

        HWJ_GameplayEvents.RaiseSkillUnlocked(
            new HWJ_SkillUnlockedEvent(
                this,
                skillId,
                GetCurrentLevel(),
                unlockResult.RemainingSkillPoint,
                skillPointCost > 0,
                message));

        return unlockResult;
    }

}
