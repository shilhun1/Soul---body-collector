using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_RuntimeStateCondition", menuName = "HWJ/Data/Rules/Conditions/Runtime State")]
public class HWJ_RuntimeStateConditionSO : HWJ_GameplayConditionSO
{
    [Header("런타임 상태 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 상태를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [Tooltip("필요한 런타임 상태입니다.")]
    [InspectorName("필요 상태")]
    [SerializeField] private HWJ_RuntimeState requiredState = HWJ_RuntimeState.Idle;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);
        return status != null && status.CurrentState == requiredState;
    }
}
