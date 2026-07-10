using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_RuntimeStateCondition", menuName = "HWJ/Data/Rules/Conditions/Runtime State")]
public class HWJ_RuntimeStateConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [SerializeField] private HWJ_RuntimeState requiredState = HWJ_RuntimeState.Idle;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);
        return status != null && status.CurrentState == requiredState;
    }
}
