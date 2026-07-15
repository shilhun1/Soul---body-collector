using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_WeaponTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Weapon Type")]
public class HWJ_WeaponTypeConditionSO : HWJ_GameplayConditionSO
{
    [Header("무기 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 무기를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("필요한 무기 타입입니다.")]
    [InspectorName("필요 무기")]
    [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver resolver = context.GetResolver(actor);
        return resolver != null && resolver.WeaponType == requiredWeaponType;
    }
}
