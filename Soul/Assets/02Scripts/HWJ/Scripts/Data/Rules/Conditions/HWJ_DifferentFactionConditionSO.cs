using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_DifferentFactionCondition", menuName = "HWJ/Data/Rules/Conditions/Different Faction")]
public class HWJ_DifferentFactionConditionSO : HWJ_GameplayConditionSO
{
    [Header("다른 진영 조건")]
    [Tooltip("켜면 Neutral 진영도 서로 다른 진영 판정에 포함합니다.")]
    [InspectorName("Neutral 허용")]
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
