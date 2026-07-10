using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DifferentFactionCondition", menuName = "HWJ/Data/Rules/Conditions/Different Faction")]
public class HWJ_DifferentFactionConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private bool allowNeutralFaction;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver source = context.GetResolver(HWJ_GameplayActorSlot.Source);
        HWJ_RootObjectDataResolver target = context.GetResolver(HWJ_GameplayActorSlot.Target);

        if (source == null || target == null || source.Identity == null || target.Identity == null)
        {
            return false;
        }

        if (!allowNeutralFaction
            && (source.Identity.faction == HWJ_Faction.Neutral || target.Identity.faction == HWJ_Faction.Neutral))
        {
            return false;
        }

        return source.Identity.faction != target.Identity.faction;
    }
}
