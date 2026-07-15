using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_AttackHitCondition", menuName = "HWJ/Data/Rules/Conditions/Attack Hit")]
public class HWJ_AttackHitConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_AttackHitRequirement requirement = HWJ_AttackHitRequirement.HasHitConfirmed;
    [SerializeField] private string requiredHitboxId;
    [SerializeField] private HWJ_DamageType requiredDamageType = HWJ_DamageType.Physical;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        switch (requirement)
        {
            case HWJ_AttackHitRequirement.HasHitConfirmed:
                return context.HasHitConfirmed;
            case HWJ_AttackHitRequirement.HasHitboxId:
                return !string.IsNullOrEmpty(context.HitboxId);
            case HWJ_AttackHitRequirement.HitboxIdMatches:
                return string.Equals(context.HitboxId, requiredHitboxId, StringComparison.Ordinal);
            case HWJ_AttackHitRequirement.HasHitCollider:
                return context.HitCollider != null;
            case HWJ_AttackHitRequirement.DamageTypeMatches:
                return context.DamageData != null && context.DamageData.damageType == requiredDamageType;
            default:
                return false;
        }
    }
}
