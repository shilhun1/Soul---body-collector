using UnityEngine;

/// <summary>
/// 현재 빙의한 몬스터의 무기와 스킬 데이터를 제공합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessedSkillProvider : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_SkillUnlockSystem skillUnlockSystem;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public bool TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet)
    {
        ResolveReferences();
        skillSet = null;

        if (possessionSystem == null
            || !possessionSystem.HasActivePossessedBody
            || possessionSystem.PossessedBodyResolver == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver bodyResolver = possessionSystem.PossessedBodyResolver;

        if (bodyResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            if (HasAnySkillEntry(enemyData.PlayerPossessionSkillSet))
            {
                skillSet = enemyData.PlayerPossessionSkillSet;
                return true;
            }

            skillSet = enemyData.SkillCycle;
            return HasAnySkillEntry(skillSet);
        }

        if (bodyResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            skillSet = bossData.SkillCycle;
            return skillSet != null;
        }

        return false;
    }

    public bool TryGetPrimaryPossessedSkillId(out string skillId)
    {
        return TryGetPossessedSkillIdAt(0, out skillId);
    }

    public bool TryGetPossessedSkillIdAt(int slotIndex, out string skillId)
    {
        skillId = null;

        if (slotIndex < 0
            || !TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet)
            || skillSet.skills == null)
        {
            return false;
        }

        HWJ_WeaponType weaponType = possessionSystem.CurrentWeaponType;
        int matchedSlotIndex = 0;

        for (int i = 0; i < skillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData skill = skillSet.skills[i];

            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                continue;
            }

            bool weaponMatched = skill.requiredWeaponType == HWJ_WeaponType.None
                || skill.requiredWeaponType == weaponType;

            if (!weaponMatched || !IsPossessedSkillEntryUnlocked(skill))
            {
                continue;
            }

            if (matchedSlotIndex == slotIndex)
            {
                skillId = skill.skillId;
                return true;
            }

            matchedSlotIndex++;
        }

        return false;
    }

    public HWJ_SkillNodeDataSO[] GetCurrentPossessedSkillNodes()
    {
        ResolveReferences();

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            return new HWJ_SkillNodeDataSO[0];
        }

        return skillUnlockSystem != null
            ? skillUnlockSystem.GetSkillNodesForWeapon(possessionSystem.CurrentWeaponType)
            : new HWJ_SkillNodeDataSO[0];
    }

    public bool TryGetCurrentPossessedSkillNodeAt(
        int slotIndex,
        out HWJ_SkillNodeDataSO skillNode)
    {
        skillNode = null;

        if (slotIndex < 0)
        {
            return false;
        }

        HWJ_SkillNodeDataSO[] skillNodes = GetCurrentPossessedSkillNodes();

        if (slotIndex >= skillNodes.Length)
        {
            return false;
        }

        skillNode = skillNodes[slotIndex];
        return skillNode != null;
    }

    private bool IsPossessedSkillEntryUnlocked(HWJ_SkillEntryData skill)
    {
        ResolveReferences();

        if (skill == null || string.IsNullOrEmpty(skill.skillId))
        {
            return false;
        }

        return skillUnlockSystem != null
            ? skillUnlockSystem.IsSkillEntryUnlocked(skill)
            : skill.startsUnlocked;
    }

    private static bool HasAnySkillEntry(HWJ_SkillSetData skillSet)
    {
        return skillSet != null
            && skillSet.skills != null
            && skillSet.skills.Length > 0;
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem = GetComponent<HWJ_SkillUnlockSystem>();
        }
    }
}
