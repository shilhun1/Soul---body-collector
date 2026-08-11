using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Restores, clears, and records unlocked skill and node identifiers.
/// This partial belongs to the single HWJ_SkillUnlockSystem component.
/// </summary>
public partial class HWJ_SkillUnlockSystem
{
    public void RestoreUnlockedSkills(IEnumerable<string> skillIds)
    {
        RestoreUnlockedSkills(skillIds, null);
    }

    public void RestoreUnlockedSkills(IEnumerable<string> skillIds, IEnumerable<string> skillNodeIds)
    {
        unlockedSkillIds.Clear();
        unlockedSkillNodeIds.Clear();

        RestoreUnlockedSkillIds(skillIds, true);
        RestoreUnlockedSkillNodeIds(skillNodeIds);
        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();

        int restoredSkillCount = unlockedSkillIds.Count;
        int restoredSkillNodeCount = unlockedSkillNodeIds.Count;

        if (restoredSkillCount <= 0 && restoredSkillNodeCount <= 0)
        {
            lastUnlockMessage = "Restored empty unlocked skill list.";
            return;
        }

        lastUnlockMessage = $"Restored {restoredSkillCount} skills and {restoredSkillNodeCount} skill nodes.";
    }

    /// <summary>
    /// 플레이어의 모든 런타임 해금 상태(스킬 노드 및 스킬)를 초기화하고,
    /// 옵션에 따라 수동 해금에 소비했던 포인트를 환급합니다.
    /// </summary>
    public int ClearAllUnlocks(bool refundSkillPoints = true)
    {
        ResolveReferences();

        int refundedPoints = 0;

        if (refundSkillPoints)
        {
            // 1. 해금 노드 포인트 환급 계산
            if (ResolveGameplayDatabase())
            {
                foreach (string nodeId in unlockedSkillNodeIds)
                {
                    if (gameplayDatabase.TryGetSkillNode(nodeId, out HWJ_SkillNodeDataSO skillNode))
                    {
                        if (!IsAutoUnlockedStartingSkillNode(skillNode))
                        {
                            refundedPoints += skillNode.SkillPointCost;
                        }
                    }
                }
            }

            // 2. 일반 스킬 포인트 환급 계산
            if (TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
                && playerTypeData.SkillSet != null
                && playerTypeData.SkillSet.skills != null)
            {
                foreach (string skillId in unlockedSkillIds)
                {
                    if (TryFindSkillEntry(skillId, out HWJ_SkillEntryData definitionEntry))
                    {
                        if (!definitionEntry.startsUnlocked)
                        {
                            refundedPoints += Mathf.Max(0, definitionEntry.requiredSkillPoint);
                        }
                    }
                }
            }

            if (refundedPoints > 0 && playerLevelProgress != null)
            {
                playerLevelProgress.AddSkillPoint(refundedPoints);
            }
        }

        unlockedSkillIds.Clear();
        unlockedSkillNodeIds.Clear();

        cachedCommonUnlockedSkillStep = 0;
        RefreshStartingAndLevelUnlockedSkills();

        lastUnlockMessage = $"Cleared all skill unlocks. Refunded {refundedPoints} skill points.";
        return refundedPoints;
    }

    /// <summary>
    /// 시작 해금 스킬과 현재 레벨 조건을 만족하는 무료 스킬을 런타임 해금 목록에 반영합니다.
    /// </summary>
    public int RefreshStartingAndLevelUnlockedSkills()
    {
        ResolveReferences();

        int unlockedCount = 0;

        if (TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
            && playerTypeData.SkillSet != null
            && playerTypeData.SkillSet.skills != null)
        {
            for (int i = 0; i < playerTypeData.SkillSet.skills.Length; i++)
            {
                HWJ_SkillEntryData definitionEntry = playerTypeData.SkillSet.skills[i];

                if (!IsAutoUnlockEligible(definitionEntry))
                {
                    continue;
                }

                if (UnlockWithoutCost(definitionEntry, "Skill unlocked by starting data or level.").Succeeded)
                {
                    unlockedCount++;
                }
            }
        }
        else
        {
            lastUnlockMessage = "Skill refresh skipped: missing player skill set.";
        }

        unlockedCount += RefreshStartingAndLevelUnlockedSkillNodes();
        return unlockedCount;
    }

    public int RefreshStartingAndLevelUnlockedSkillNodes()
    {
        ResolveReferences();

        if (!useSkillNodeProgress || !autoUnlockFreeStartingSkillNodes)
        {
            return 0;
        }

        if (!TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            lastSkillNodeMessage = "Skill node refresh skipped: missing gameplay database or skill nodes.";
            return 0;
        }

        int unlockedCount = 0;

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (!IsAutoUnlockEligible(skillNode))
            {
                continue;
            }

            if (AddUnlockedSkillNodeId(skillNode.NodeId))
            {
                unlockedCount++;
                RaiseUnlocked(
                    skillNode.NodeId,
                    null,
                    0,
                    "Skill node unlocked by starting data.",
                    skillNode);
            }
        }

        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();
        lastSkillNodeMessage = $"Refreshed starting skill nodes. Added {unlockedCount}.";
        return unlockedCount;
    }

    /// <summary>
    /// 스킬 포인트 소비를 포함해 플레이어 스킬을 해금합니다.
    /// 실패해도 내부 상태를 바꾸지 않고 실패 코드를 반환합니다.
    /// </summary>
    private HWJ_SkillUnlockResult SetLastResult(HWJ_SkillUnlockResult result)
    {
        lastUnlockMessage = result.Message;
        return result;
    }

    private bool AddUnlockedSkillId(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || unlockedSkillIds.Contains(skillId))
        {
            return false;
        }

        unlockedSkillIds.Add(skillId);
        return true;
    }

    private bool AddUnlockedSkillNodeId(string skillNodeId)
    {
        if (string.IsNullOrEmpty(skillNodeId) || unlockedSkillNodeIds.Contains(skillNodeId))
        {
            return false;
        }

        unlockedSkillNodeIds.Add(skillNodeId);
        return true;
    }

    private int RestoreUnlockedSkillIds(IEnumerable<string> skillIds, bool classifyKnownSkillNodes)
    {
        if (skillIds == null)
        {
            return 0;
        }

        int restoredCount = 0;

        foreach (string skillId in skillIds)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                continue;
            }

            // Older saves stored skill node IDs in the legacy skill list. Runtime separates them here.
            if (classifyKnownSkillNodes && IsKnownSkillNodeId(skillId))
            {
                if (AddUnlockedSkillNodeId(skillId))
                {
                    restoredCount++;
                }

                continue;
            }

            if (AddUnlockedSkillId(skillId))
            {
                restoredCount++;
            }
        }

        return restoredCount;
    }

    private int RestoreUnlockedSkillNodeIds(IEnumerable<string> skillNodeIds)
    {
        if (skillNodeIds == null)
        {
            return 0;
        }

        int restoredCount = 0;

        foreach (string skillNodeId in skillNodeIds)
        {
            if (AddUnlockedSkillNodeId(skillNodeId))
            {
                restoredCount++;
            }
        }

        return restoredCount;
    }

    private bool IsKnownSkillNodeId(string skillNodeId)
    {
        return !string.IsNullOrEmpty(skillNodeId)
            && ResolveGameplayDatabase()
            && gameplayDatabase.TryGetSkillNode(skillNodeId, out _);
    }

}
