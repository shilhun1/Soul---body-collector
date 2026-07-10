using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_HitConfirmedCondition", menuName = "HWJ/Data/Rules/Conditions/Hit Confirmed")]
public class HWJ_HitConfirmedConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private bool expectedValue = true;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        return context.HasHitConfirmed == expectedValue;
    }
}
