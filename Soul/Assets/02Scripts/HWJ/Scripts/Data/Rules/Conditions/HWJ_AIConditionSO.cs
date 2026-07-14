using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_AICondition", menuName = "HWJ/Data/Rules/Conditions/AI")]
public class HWJ_AIConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_AIRequirement requirement = HWJ_AIRequirement.HasTarget;
    [SerializeField] private HWJ_MonsterAIState requiredState = HWJ_MonsterAIState.Idle;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
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
