using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_PossessionStateCondition", menuName = "HWJ/Data/Rules/Conditions/Possession State")]
public class HWJ_PossessionStateConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_PossessionStateRequirement requirement = HWJ_PossessionStateRequirement.HasActivePossessedBody;
    [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;
    [SerializeField] private HWJ_ObjectType requiredObjectType = HWJ_ObjectType.Enemy;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_PossessionSystem possessionSystem = context.GetPossessionSystem(actor);

        if (possessionSystem == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_PossessionStateRequirement.HasActivePossessedBody:
                return possessionSystem.HasActivePossessedBody;
            case HWJ_PossessionStateRequirement.DoesNotHaveActivePossessedBody:
                return !possessionSystem.HasActivePossessedBody;
            case HWJ_PossessionStateRequirement.PossessedWeaponMatches:
                return possessionSystem.HasActivePossessedBody
                    && possessionSystem.CurrentWeaponType == requiredWeaponType;
            case HWJ_PossessionStateRequirement.PossessedObjectTypeMatches:
                return possessionSystem.HasActivePossessedBody
                    && possessionSystem.PossessedBodyResolver != null
                    && possessionSystem.PossessedBodyResolver.ObjectType == requiredObjectType;
            case HWJ_PossessionStateRequirement.CanLoadBodyStats:
                return possessionSystem.CanLoadPossessedBodyStats;
            default:
                return false;
        }
    }
}
