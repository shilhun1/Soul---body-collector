using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_SoulStateCondition", menuName = "HWJ/Data/Rules/Conditions/Soul State")]
public class HWJ_SoulStateConditionSO : HWJ_GameplayConditionSO
{
    [Header("영혼 상태 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 영혼 상태를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("필요한 영혼 런타임 상태입니다.")]
    [InspectorName("필요 영혼 상태")]
    [SerializeField] private HWJ_SoulRuntimeState requiredState = HWJ_SoulRuntimeState.Soul;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_SoulSystem soulSystem = context.GetSoul(actor);
        return soulSystem != null && soulSystem.CurrentState == requiredState;
    }
}
