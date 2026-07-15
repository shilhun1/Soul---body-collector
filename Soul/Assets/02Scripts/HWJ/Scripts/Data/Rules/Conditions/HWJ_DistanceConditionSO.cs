using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DistanceCondition", menuName = "HWJ/Data/Rules/Conditions/Distance")]
public class HWJ_DistanceConditionSO : HWJ_GameplayConditionSO
{
    [Header("거리 조건")]
    [Tooltip("현재 거리와 기준 거리를 비교하는 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
    [Tooltip("비교할 기준 거리입니다.")]
    [InspectorName("기준 거리")]
    [SerializeField] private float distance = 1.5f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        return HWJ_ConditionUtility.Compare(context.GetDistance(), compareMode, distance);
    }
}
