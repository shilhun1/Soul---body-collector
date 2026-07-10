using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_SoulStateCondition", menuName = "HWJ/Data/Rules/Conditions/Soul State")]
public class HWJ_SoulStateConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_SoulRuntimeState requiredState = HWJ_SoulRuntimeState.Soul;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_SoulSystem soulSystem = context.GetSoul(actor);
        return soulSystem != null && soulSystem.CurrentState == requiredState;
    }
}
