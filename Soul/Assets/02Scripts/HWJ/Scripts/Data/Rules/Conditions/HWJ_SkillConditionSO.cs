using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_SkillCondition", menuName = "HWJ/Data/Rules/Conditions/Skill")]
public class HWJ_SkillConditionSO : HWJ_GameplayConditionSO
{
    [Header("스킬 조건")]
    [Tooltip("스킬 사용 가능 여부에서 검사할 조건 종류입니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_SkillRequirement requirement = HWJ_SkillRequirement.HasSkillAction;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        switch (requirement)
        {
            case HWJ_SkillRequirement.HasSkillAction:
                return context.SkillAction != null || !string.IsNullOrEmpty(context.ActionId);
            case HWJ_SkillRequirement.RequiredWeaponMatchesSource:
                return IsWeaponMatched(context);
            case HWJ_SkillRequirement.SourceCanAttack:
                HWJ_RuntimeStatusSystem attackStatus = context.GetStatus(HWJ_GameplayActorSlot.Source);
                return attackStatus != null && attackStatus.CanAttack;
            case HWJ_SkillRequirement.SourceCanDash:
                HWJ_RuntimeStatusSystem dashStatus = context.GetStatus(HWJ_GameplayActorSlot.Source);
                return dashStatus != null && dashStatus.CanDash;
            case HWJ_SkillRequirement.SkillCooldownReady:
                return IsSkillCooldownReady(context);
            case HWJ_SkillRequirement.SkillUnlockedBySource:
                return IsSkillUnlockedBySource(context);
            default:
                return false;
        }
    }

    private bool IsWeaponMatched(HWJ_GameplayContext context)
    {
        if (context.SkillAction == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver source = context.GetResolver(HWJ_GameplayActorSlot.Source);
        HWJ_WeaponType weaponType = GetEffectiveWeaponType(source);
        return context.SkillAction.CanUseWithWeapon(weaponType);
    }

    private HWJ_WeaponType GetEffectiveWeaponType(HWJ_RootObjectDataResolver source)
    {
        if (source == null)
        {
            return HWJ_WeaponType.None;
        }

        HWJ_PossessionSystem possessionSystem = source.GetComponent<HWJ_PossessionSystem>();

        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        return source.WeaponType;
    }

    private bool IsSkillCooldownReady(HWJ_GameplayContext context)
    {
        string skillId = context.SkillAction != null ? context.SkillAction.SkillActionId : context.ActionId;

        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        HWJ_SkillActionSystem skillActionSystem = context.GetSkillActionSystem(HWJ_GameplayActorSlot.Source);
        return skillActionSystem == null || skillActionSystem.IsSkillReady(skillId);
    }

    private bool IsSkillUnlockedBySource(HWJ_GameplayContext context)
    {
        string skillId = context.SkillAction != null ? context.SkillAction.SkillActionId : context.ActionId;

        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        HWJ_SkillUnlockSystem sourceUnlockState = context.GetSkillUnlockSystem(HWJ_GameplayActorSlot.Source);
        return sourceUnlockState != null && sourceUnlockState.IsSkillUnlocked(skillId);
    }
}
