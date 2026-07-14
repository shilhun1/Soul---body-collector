using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_BossCondition", menuName = "HWJ/Data/Rules/Conditions/Boss")]
public class HWJ_BossConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_BossRequirement requirement = HWJ_BossRequirement.EncounterStarted;
    [SerializeField] private HWJ_BossFSMState requiredState = HWJ_BossFSMState.Idle;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.GreaterOrEqual;
    [SerializeField] private int phaseNumber = 1;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_BossBrainSystem bossBrain = context.GetBossBrain(actor);

        if (bossBrain == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_BossRequirement.EncounterStarted:
                return bossBrain.EncounterStarted;
            case HWJ_BossRequirement.EncounterNotStarted:
                return !bossBrain.EncounterStarted;
            case HWJ_BossRequirement.StateMatches:
                return bossBrain.CurrentState == requiredState;
            case HWJ_BossRequirement.PhaseNumberCompare:
                return HWJ_ConditionUtility.Compare(bossBrain.CurrentPhaseNumber, compareMode, phaseNumber);
            case HWJ_BossRequirement.IsGroggy:
                return bossBrain.IsGroggy;
            case HWJ_BossRequirement.NotGroggy:
                return !bossBrain.IsGroggy;
            case HWJ_BossRequirement.TargetInsideBossRoom:
                return IsTargetInsideBossRoom(context, bossBrain);
            default:
                return false;
        }
    }

    private bool IsTargetInsideBossRoom(HWJ_GameplayContext context, HWJ_BossBrainSystem bossBrain)
    {
        Transform targetTransform = bossBrain.Target;

        if (targetTransform == null)
        {
            HWJ_GameplayActorSlot otherActor = actor == HWJ_GameplayActorSlot.Source
                ? HWJ_GameplayActorSlot.Target
                : HWJ_GameplayActorSlot.Source;
            targetTransform = context.GetTransform(otherActor);
        }

        if (targetTransform == null)
        {
            return false;
        }

        Vector2 center = bossBrain.BossRoomCenter;
        Vector2 halfSize = bossBrain.BossRoomSize * 0.5f;
        Vector2 targetPosition = targetTransform.position;

        return Mathf.Abs(targetPosition.x - center.x) <= halfSize.x
            && Mathf.Abs(targetPosition.y - center.y) <= halfSize.y;
    }
}
