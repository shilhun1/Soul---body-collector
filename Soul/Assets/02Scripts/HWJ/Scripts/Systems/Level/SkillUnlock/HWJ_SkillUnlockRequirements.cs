using UnityEngine;

/// <summary>
/// Checks node order, level requirements, database definitions, and runtime references.
/// This partial belongs to the single HWJ_SkillUnlockSystem component.
/// </summary>
public partial class HWJ_SkillUnlockSystem
{
    public int GetUnlockedCommonSkillNodeStep()
    {
        if (!useSkillNodeProgress || !TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            cachedCommonUnlockedSkillStep = 0;
            return cachedCommonUnlockedSkillStep;
        }

        int highestUnlockedStep = 0;

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
            {
                continue;
            }

            bool nodeDirectlyUnlocked = unlockedSkillNodeIds.Contains(skillNode.NodeId);
            bool nodeStartsUnlocked = IsAutoUnlockedStartingSkillNode(skillNode);

            if (!nodeDirectlyUnlocked && !nodeStartsUnlocked)
            {
                continue;
            }

            highestUnlockedStep = Mathf.Max(highestUnlockedStep, skillNode.SkillStep);
        }

        cachedCommonUnlockedSkillStep = highestUnlockedStep;
        return cachedCommonUnlockedSkillStep;
    }

    private bool IsAutoUnlockedStartingSkillNode(HWJ_SkillNodeDataSO skillNode)
    {
        return useSkillNodeProgress
            && autoUnlockFreeStartingSkillNodes
            && skillNode != null
            && skillNode.IsFreeStartingNode
            && GetCurrentLevel() >= skillNode.RequiredLevel;
    }

    private bool IsPreviousSkillNodeRequirementMet(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null)
        {
            return false;
        }

        if (!skillNode.HasPrerequisite)
        {
            return true;
        }

        return IsSkillNodeUnlocked(skillNode.PrerequisiteNodeId);
    }

    private bool TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList)
    {
        skillNodeList = null;

        if (!ResolveGameplayDatabase() || gameplayDatabase.SkillNodes == null)
        {
            return false;
        }

        skillNodeList = gameplayDatabase.SkillNodes;
        return skillNodeList.Length > 0;
    }

    private bool ResolveGameplayDatabase()
    {
        if (gameplayDatabase != null)
        {
            return true;
        }

        gameplayDatabase = HWJ_GameAccess.Database;
        return gameplayDatabase != null;
    }

    private bool IsAutoUnlockEligible(HWJ_SkillEntryData definitionEntry)
    {
        if (definitionEntry == null || string.IsNullOrEmpty(definitionEntry.skillId))
        {
            return false;
        }

        if (IsSkillUnlocked(definitionEntry.skillId))
        {
            return false;
        }

        if (definitionEntry.startsUnlocked)
        {
            return true;
        }

        if (!autoUnlockLevelSkills)
        {
            return false;
        }

        if (autoUnlockOnlyFreeSkills && definitionEntry.requiredSkillPoint > 0)
        {
            return false;
        }

        return definitionEntry.unlockLevel > 0 && IsLevelRequirementMet(definitionEntry);
    }

    private bool IsAutoUnlockEligible(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(skillNode.NodeId))
        {
            return false;
        }

        return IsAutoUnlockedStartingSkillNode(skillNode);
    }

    private bool IsLevelRequirementMet(HWJ_SkillEntryData definitionEntry)
    {
        return definitionEntry == null
            || definitionEntry.unlockLevel <= 0
            || GetCurrentLevel() >= definitionEntry.unlockLevel;
    }

    private bool TryFindSkillEntry(string skillId, out HWJ_SkillEntryData definitionEntry)
    {
        definitionEntry = null;

        if (string.IsNullOrEmpty(skillId) || !TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData))
        {
            return false;
        }

        if (playerTypeData.SkillSet == null || playerTypeData.SkillSet.skills == null)
        {
            return false;
        }

        for (int i = 0; i < playerTypeData.SkillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData definedSkill = playerTypeData.SkillSet.skills[i];

            if (definedSkill != null && definedSkill.skillId == skillId)
            {
                definitionEntry = definedSkill;
                return true;
            }
        }

        return false;
    }

    private bool TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
    {
        ResolveReferences();

        if (ownerDataResolver != null && ownerDataResolver.TryGetTypeData(out playerTypeData))
        {
            return playerTypeData != null;
        }

        playerTypeData = null;
        return false;
    }

    private int GetCurrentLevel()
    {
        return playerLevelProgress != null ? playerLevelProgress.CurrentLevel : 1;
    }

    private int GetRemainingSkillPoint()
    {
        return playerLevelProgress != null ? playerLevelProgress.SkillPoint : 0;
    }

    private void OnPlayerLevelChanged(HWJ_PlayerLevelChangedEvent levelChangedEvent)
    {
        ResolveReferences();

        if (playerLevelProgress == null || levelChangedEvent.LevelSystem != playerLevelProgress)
        {
            return;
        }

        RefreshStartingAndLevelUnlockedSkills();
    }

    private void ResolveReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (playerLevelProgress == null)
        {
            playerLevelProgress = GetComponent<HWJ_LevelUpSystem>();
        }

        ResolveGameplayDatabase();
    }
}
