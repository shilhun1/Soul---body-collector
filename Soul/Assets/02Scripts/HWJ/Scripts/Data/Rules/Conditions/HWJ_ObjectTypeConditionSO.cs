using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ObjectTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Object Type")]
public class HWJ_ObjectTypeConditionSO : HWJ_GameplayConditionSO
{
    [Header("오브젝트 유형 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 오브젝트 유형을 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [Tooltip("필요한 오브젝트 유형입니다.")]
    [InspectorName("필요 오브젝트 유형")]
    [SerializeField] private HWJ_ObjectType requiredObjectType = HWJ_ObjectType.Enemy;
    [Tooltip("켜면 보스를 적 유형으로도 인정합니다.")]
    [InspectorName("보스를 적으로 취급")]
    [SerializeField] private bool treatBossAsEnemy;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver resolver = context.GetResolver(actor);

        if (resolver == null)
        {
            return false;
        }

        if (resolver.ObjectType == requiredObjectType)
        {
            return true;
        }

        return treatBossAsEnemy
            && requiredObjectType == HWJ_ObjectType.Enemy
            && resolver.ObjectType == HWJ_ObjectType.Boss;
    }
}
