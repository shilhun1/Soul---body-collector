using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_AICondition", menuName = "HWJ/Data/Rules/Conditions/AI")]
public class HWJ_AIConditionSO : HWJ_GameplayConditionSO
{
    [Header("AI 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 AI를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("AI에서 검사할 조건 종류입니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_AIRequirement requirement = HWJ_AIRequirement.HasTarget;
    [Tooltip("StateMatches 조건에서 비교할 AI 상태입니다.")]
    [InspectorName("필요 AI 상태")]
    [SerializeField] private HWJ_MonsterAIState requiredState = HWJ_MonsterAIState.Idle;
    [Tooltip("거리 비교 조건에서 사용할 비교 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
    [Tooltip("거리 비교 조건에서 사용할 기준 거리입니다.")]
    [InspectorName("기준 거리")]
    [SerializeField] private float distance = 1f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_MonsterAISystem monsterAI = context.GetMonsterAI(actor);

        if (monsterAI == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_AIRequirement.HasTarget:
                return monsterAI.Target != null;
            case HWJ_AIRequirement.StateMatches:
                return monsterAI.CurrentState == requiredState;
            case HWJ_AIRequirement.TargetInTrackingRange:
                return monsterAI.TargetInTrackingRange;
            case HWJ_AIRequirement.TargetInAttackRange:
                return monsterAI.TargetInAttackRange;
            case HWJ_AIRequirement.TargetDistanceCompare:
                return HWJ_ConditionUtility.Compare(monsterAI.CurrentTargetDistance, compareMode, distance);
            case HWJ_AIRequirement.TargetBodyState:
                return monsterAI.IsTargetBodyStateForRule();
            default:
                return false;
        }
    }
}
