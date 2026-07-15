using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_FactionCondition", menuName = "HWJ/Data/Rules/Conditions/Faction")]
public class HWJ_FactionConditionSO : HWJ_GameplayConditionSO
{
    [Header("진영 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 진영을 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("필요한 진영입니다.")]
    [InspectorName("필요 진영")]
    [SerializeField] private HWJ_Faction requiredFaction = HWJ_Faction.Player;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver resolver = context.GetResolver(actor);
        return resolver != null
            && resolver.Identity != null
            && resolver.Identity.faction == requiredFaction;
    }
}
