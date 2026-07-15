using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_PossessionStateCondition", menuName = "HWJ/Data/Rules/Conditions/Possession State")]
public class HWJ_PossessionStateConditionSO : HWJ_GameplayConditionSO
{
    [Header("빙의 상태 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 빙의 상태를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("빙의 상태에서 검사할 조건 종류입니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_PossessionStateRequirement requirement = HWJ_PossessionStateRequirement.HasActivePossessedBody;
    [Tooltip("빙의한 몸의 무기 타입이 이 값과 일치하는지 검사할 때 사용합니다.")]
    [InspectorName("필요 무기")]
    [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;
    [Tooltip("빙의한 몸의 오브젝트 유형이 이 값과 일치하는지 검사할 때 사용합니다.")]
    [InspectorName("필요 오브젝트 유형")]
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
