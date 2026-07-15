using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_HitConfirmedCondition", menuName = "HWJ/Data/Rules/Conditions/Hit Confirmed")]
public class HWJ_HitConfirmedConditionSO : HWJ_GameplayConditionSO
{
    [Header("적중 확인 조건")]
    [Tooltip("공격이 실제로 적중했는지 기대하는 값입니다.")]
    [InspectorName("기대 적중 여부")]
    [SerializeField] private bool expectedValue = true;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        return context.HasHitConfirmed == expectedValue;
    }
}
