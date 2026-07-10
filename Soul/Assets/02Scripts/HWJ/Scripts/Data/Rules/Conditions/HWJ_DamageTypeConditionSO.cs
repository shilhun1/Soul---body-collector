using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DamageTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Damage Type")]
public class HWJ_DamageTypeConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_DamageType requiredDamageType = HWJ_DamageType.Physical;
    [SerializeField] private bool fallbackToSourceDamage = true;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_DamageData damageData = context.DamageData;

        if (damageData == null && fallbackToSourceDamage)
        {
            HWJ_RootObjectDataResolver source = context.GetResolver(HWJ_GameplayActorSlot.Source);
            damageData = source != null ? source.Damage : null;
        }

        return damageData != null && damageData.damageType == requiredDamageType;
    }
}
