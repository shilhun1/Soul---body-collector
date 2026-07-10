using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ObjectTypeCondition", menuName = "HWJ/Data/Rules/Conditions/Object Type")]
public class HWJ_ObjectTypeConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [SerializeField] private HWJ_ObjectType requiredObjectType = HWJ_ObjectType.Enemy;
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
