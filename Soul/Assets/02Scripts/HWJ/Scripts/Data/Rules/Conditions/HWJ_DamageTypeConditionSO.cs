using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DamageTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Damage Type")]
public class HWJ_DamageTypeConditionSO : HWJ_GameplayConditionSO
{
    [Header("데미지 타입 조건")]
    [Tooltip("필요한 데미지 타입입니다.")]
    [InspectorName("필요 데미지 타입")]
    [SerializeField] private HWJ_DamageType requiredDamageType = HWJ_DamageType.Physical;
    [Tooltip("컨텍스트에 데미지 데이터가 없으면 Source의 기본 데미지 데이터를 사용할지 정합니다.")]
    [InspectorName("Source 데미지로 대체")]
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
