using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_WeaponTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Weapon Type")]
public class HWJ_WeaponTypeConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver resolver = context.GetResolver(actor);
        return resolver != null && resolver.WeaponType == requiredWeaponType;
    }
}
