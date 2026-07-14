using System;
using UnityEngine;

[Serializable]
// Stored on BossTypeDataSO so each boss can define entry gates without mutating runtime state.
public class HWJ_BossEntryRequirementData
{
    [Header("Stage")]
    public bool requireObjectiveComplete = true;
    public bool requireBossUnlocked = true;
    public bool requireBossNotDefeated = true;
    public bool requireEntryTriggerActive = true;

    [Header("Possessed Body")]
    public bool requirePossessedBody = true;
    public bool requireBodyNotCollapsing = true;
    public bool requireBodyObjectType;
    public HWJ_ObjectType requiredBodyObjectType = HWJ_ObjectType.Enemy;
    public HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;
    public float minimumCurrentHp = 1f;
    [Range(0f, 1f)] public float maximumCurrentDecayRatio = 1f;

    [Header("Skill")]
    public string[] requiredUnlockedSkillIds;

    [Header("Body Id Filter")]
    public string[] allowedBodyObjectIds;
    public string[] blockedBodyObjectIds;

    [Header("Additional Rule")]
    public HWJ_RuleExecutionCoreSO additionalRuleExecutionCore;
    public string additionalRuleExecutionCoreId = "boss_entry_execution";
}
