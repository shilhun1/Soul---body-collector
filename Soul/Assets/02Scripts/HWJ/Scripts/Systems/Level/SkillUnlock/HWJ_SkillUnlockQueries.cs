using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides read-only skill, node, and equipped-entry unlock queries.
/// This partial belongs to the single HWJ_SkillUnlockSystem component.
/// </summary>
public partial class HWJ_SkillUnlockSystem
{
    public bool IsSkillUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        if (unlockedSkillIds.Contains(skillId))
        {
            return true;
        }

        return IsSkillNodeUnlocked(skillId) || IsSkillActionUnlockedBySkillNodeProgress(skillId);
    }

    public bool HasSkillDefinition(string skillId)
    {
        return TryFindSkillEntry(skillId, out _);
    }

    public bool HasSkillNodeDefinitionForSkillAction(string skillActionId)
    {
        return TryGetSkillNodeForSkillAction(skillActionId, out _);
    }

    public bool TryGetSkillNodeForSkillAction(string skillActionId, out HWJ_SkillNodeDataSO skillNode)
    {
        skillNode = null;

        if (string.IsNullOrEmpty(skillActionId) || !ResolveGameplayDatabase())
        {
            return false;
        }

        return gameplayDatabase.TryGetSkillNodeBySkillAction(skillActionId, out skillNode);
    }

    public bool IsSkillActionUnlockedBySkillNodeProgress(string skillActionId)
    {
        if (!useSkillNodeProgress || !TryGetSkillNodeForSkillAction(skillActionId, out HWJ_SkillNodeDataSO skillNode))
        {
            return false;
        }

        return IsSkillNodeUnlocked(skillNode);
    }

    public bool IsSkillEntryUnlocked(HWJ_SkillEntryData skillEntry)
    {
        if (skillEntry == null || string.IsNullOrEmpty(skillEntry.skillId))
        {
            return false;
        }

        if (HasSkillNodeDefinitionForSkillAction(skillEntry.skillId))
        {
            return IsSkillActionUnlockedBySkillNodeProgress(skillEntry.skillId);
        }

        if (HasSkillDefinition(skillEntry.skillId))
        {
            return IsSkillUnlocked(skillEntry.skillId);
        }

        return skillEntry.startsUnlocked;
    }

    public HWJ_SkillNodeDataSO[] GetSkillNodesForWeapon(HWJ_WeaponType weaponType)
    {
        if (!TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            return new HWJ_SkillNodeDataSO[0];
        }

        List<HWJ_SkillNodeDataSO> matchedNodes = new List<HWJ_SkillNodeDataSO>();

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (skillNode != null && skillNode.MatchesWeapon(weaponType))
            {
                matchedNodes.Add(skillNode);
            }
        }

        return matchedNodes.ToArray();
    }

    public bool IsSkillNodeUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(nodeId))
        {
            return true;
        }

        if (!useSkillNodeProgress || !ResolveGameplayDatabase())
        {
            return false;
        }

        return gameplayDatabase.TryGetSkillNode(nodeId, out HWJ_SkillNodeDataSO skillNode)
            && IsSkillNodeUnlocked(skillNode);
    }

    public bool IsSkillNodeUnlocked(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(skillNode.NodeId))
        {
            return true;
        }

        if (!useSkillNodeProgress)
        {
            return false;
        }

        int skillStep = skillNode.SkillStep;
        return skillStep > 0 && skillStep <= GetUnlockedCommonSkillNodeStep();
    }

    /// <summary>
    /// 저장 시스템이 Unity Object 참조 없이 문자열 ID 목록만 가져갈 때 사용합니다.
    /// </summary>
    public string[] GetUnlockedSkillIds()
    {
        return unlockedSkillIds.ToArray();
    }

    public string[] GetUnlockedSkillNodeIds()
    {
        return unlockedSkillNodeIds.ToArray();
    }

    /// <summary>
    /// 저장 데이터에서 복원한 스킬 ID 목록을 런타임 상태에 다시 넣습니다.
    /// </summary>
}
