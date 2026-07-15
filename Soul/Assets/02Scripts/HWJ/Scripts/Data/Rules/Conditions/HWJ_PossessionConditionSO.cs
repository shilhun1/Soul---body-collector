using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_PossessionCondition", menuName = "HWJ/Data/Rules/Conditions/Possession")]
public class HWJ_PossessionConditionSO : HWJ_GameplayConditionSO
{
    [Header("빙의 조건")]
    [Tooltip("빙의에서 검사할 조건 종류입니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_PossessionRequirement requirement = HWJ_PossessionRequirement.TargetCanBePossessed;
    [Tooltip("대상 빙의 거리 데이터가 없을 때 사용할 기본 거리입니다.")]
    [InspectorName("기본 빙의 거리")]
    [SerializeField] private float fallbackRange = 1.5f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        switch (requirement)
        {
            case HWJ_PossessionRequirement.SourceCanPossess:
                return context.TryGetPossessionData(HWJ_GameplayActorSlot.Source, out HWJ_PossessionData sourcePossession)
                    && sourcePossession.canPossess;
            case HWJ_PossessionRequirement.SourceIsSoulState:
                HWJ_SoulSystem sourceSoul = context.GetSoul(HWJ_GameplayActorSlot.Source);
                return sourceSoul != null
                    && sourceSoul.CurrentExistenceState == HWJ_PlayerExistenceState.Spirit;
            case HWJ_PossessionRequirement.TargetIsEnemyOrBoss:
                HWJ_RootObjectDataResolver target = context.GetResolver(HWJ_GameplayActorSlot.Target);
                return target != null
                    && (target.ObjectType == HWJ_ObjectType.Enemy || target.ObjectType == HWJ_ObjectType.Boss);
            case HWJ_PossessionRequirement.TargetCanBePossessed:
                return context.TryGetPossessionData(HWJ_GameplayActorSlot.Target, out HWJ_PossessionData targetPossession)
                    && targetPossession.canBePossessed;
            case HWJ_PossessionRequirement.TargetDefeatedIfRequired:
                return IsTargetDefeatedIfRequired(context);
            case HWJ_PossessionRequirement.WithinPossessionRange:
                return IsWithinPossessionRange(context);
            case HWJ_PossessionRequirement.TargetCorpseAvailable:
                return IsTargetCorpseAvailable(context);
            default:
                return false;
        }
    }

    private bool IsTargetDefeatedIfRequired(HWJ_GameplayContext context)
    {
        if (!context.TryGetPossessionData(HWJ_GameplayActorSlot.Target, out HWJ_PossessionData targetPossession))
        {
            return false;
        }

        if (!targetPossession.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = context.GetStatus(HWJ_GameplayActorSlot.Target);
        return targetStatus != null && targetStatus.IsDead;
    }

    private bool IsWithinPossessionRange(HWJ_GameplayContext context)
    {
        float range = fallbackRange;

        if (context.TryGetPossessionData(HWJ_GameplayActorSlot.Source, out HWJ_PossessionData sourcePossession)
            && sourcePossession.possessionRange > 0f)
        {
            range = sourcePossession.possessionRange;
        }
        else if (context.TryGetPossessionData(HWJ_GameplayActorSlot.Target, out HWJ_PossessionData targetPossession)
            && targetPossession.possessionRange > 0f)
        {
            range = targetPossession.possessionRange;
        }

        return context.GetDistance() <= Mathf.Max(0f, range);
    }

    private bool IsTargetCorpseAvailable(HWJ_GameplayContext context)
    {
        HWJ_RootObjectDataResolver target = context.GetResolver(HWJ_GameplayActorSlot.Target);

        if (target == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState = target.GetComponent<HWJ_PossessionBodyState>();
        return bodyState == null || !bodyState.IsConsumed;
    }
}
