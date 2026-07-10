using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DistanceCondition", menuName = "HWJ/Data/Rules/Conditions/Distance")]
public class HWJ_DistanceConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
    [SerializeField] private float distance = 1.5f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        return HWJ_ConditionUtility.Compare(context.GetDistance(), compareMode, distance);
    }
}
