using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_AttackHitCondition", menuName = "HWJ/Data/Rules/Conditions/Attack Hit")]
public class HWJ_AttackHitConditionSO : HWJ_GameplayConditionSO
{
    [Header("공격 적중 조건")]
    [Tooltip("공격 적중에서 검사할 조건 종류입니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_AttackHitRequirement requirement = HWJ_AttackHitRequirement.HasHitConfirmed;
    [Tooltip("특정 히트박스 ID와 일치하는지 검사할 때 사용합니다.")]
    [InspectorName("필요 히트박스 ID")]
    [SerializeField] private string requiredHitboxId;
    [Tooltip("특정 데미지 타입과 일치하는지 검사할 때 사용합니다.")]
    [InspectorName("필요 데미지 타입")]
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
