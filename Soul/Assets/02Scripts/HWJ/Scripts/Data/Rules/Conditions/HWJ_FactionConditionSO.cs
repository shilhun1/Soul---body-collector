using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_FactionCondition", menuName = "HWJ/Data/Rules/Conditions/Faction")]
public class HWJ_FactionConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_Faction requiredFaction = HWJ_Faction.Player;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver resolver = context.GetResolver(actor);
        return resolver != null
            && resolver.Identity != null
            && resolver.Identity.faction == requiredFaction;
    }
}
