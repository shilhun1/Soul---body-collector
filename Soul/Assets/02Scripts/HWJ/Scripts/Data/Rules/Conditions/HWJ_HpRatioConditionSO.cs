using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_HpRatioCondition", menuName = "HWJ/Data/Rules/Conditions/HP Ratio")]
public class HWJ_HpRatioConditionSO : HWJ_GameplayConditionSO
{
    [Header("HP 비율 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 HP 비율을 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [Tooltip("현재 HP 비율과 기준 비율을 비교하는 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
    [Tooltip("비교할 기준 HP 비율입니다. 0.5는 50%입니다.")]
    [InspectorName("HP 비율")]
    [SerializeField] [Range(0f, 1f)] private float hpRatio = 0.5f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);

        if (status == null || status.MaxHp <= 0f)
        {
            return false;
        }

        float currentRatio = Mathf.Clamp01(status.CurrentHp / status.MaxHp);
        return HWJ_ConditionUtility.Compare(currentRatio, compareMode, hpRatio);
    }
}
