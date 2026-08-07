using UnityEngine;

/// <summary>
/// 빙의 대상의 종류, 상태, 사용 여부, 게임플레이 규칙을 검사합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionTargetValidator : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_LivePossessionSystem livePossessionSystem;

    [Space(8f)]
    [Header("빙의 규칙")]
    [SerializeField] private bool useGameplayPossessionRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO possessionExecutionCore;
    [SerializeField] private string possessionExecutionCoreId = "possession_execution";
    [SerializeField] private HWJ_GameplayRuleSO possessionRule;
    [SerializeField] private string possessionRuleId = "possession_can_start";

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public HWJ_PossessionResult Evaluate(HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (targetDataResolver == null)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.InvalidTarget,
                "Possession failed: missing target.");
        }

        if (possessionSystem == null)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Possession failed: missing HWJ_PossessionSystem.",
                targetDataResolver);
        }

        if (targetDataResolver.ObjectType == HWJ_ObjectType.Boss)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.BossPossessionBlocked,
                "Possession failed: bosses cannot be possessed.",
                targetDataResolver);
        }

        HWJ_SoulSystem soulSystem = possessionSystem.SoulSystem;

        if (soulSystem != null && soulSystem.IsTransitioningExistence)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TransitionInProgress,
                "Possession failed: player existence state is transitioning.",
                targetDataResolver);
        }

        if (soulSystem != null
            && soulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Spirit)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.NotInSpiritState,
                "Possession failed: player is not in Spirit existence state.",
                targetDataResolver);
        }

        if (possessionSystem.HasPossessedBodyReference
            || possessionSystem.HasActivePossessedBody)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.AlreadyPossessingBody,
                "Possession failed: player already has an occupied body.",
                targetDataResolver);
        }

        if (!targetDataResolver.gameObject.activeInHierarchy)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target body is inactive.",
                targetDataResolver);
        }

        HWJ_RootObjectDataResolver ownerDataResolver = possessionSystem.OwnerDataResolver;

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null
            && !playerData.Possession.canPossess)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Possession failed: player possession is disabled.",
                targetDataResolver);
        }

        if (!TryGetPossessionBodyData(targetDataResolver, out HWJ_PossessionData possessionBody))
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.InvalidTarget,
                "Possession failed: target has no possessable body data.",
                targetDataResolver);
        }

        if (!possessionBody.canBePossessed)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target cannot be possessed.",
                targetDataResolver);
        }

        if (!IsDefeatedIfRequired(targetDataResolver, possessionBody))
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target is not defeated.",
                targetDataResolver);
        }

        bool isLiveTarget = IsLiveTarget(targetDataResolver, possessionBody);
        bool canUseConsumedCorpse = CanUseConsumedCorpse(targetDataResolver, isLiveTarget);

        // 생체 대상은 수동 해제 후 재빙의할 수 있으므로 Consumed 플래그만으로 차단하지 않습니다.
        if (IsConsumedPossessionBody(targetDataResolver)
            && !isLiveTarget
            && !canUseConsumedCorpse)
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetAlreadyPossessed,
                "Possession failed: target body was already consumed.",
                targetDataResolver);
        }

        if (isLiveTarget)
        {
            if (livePossessionSystem == null)
            {
                return HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.PossessionBlocked,
                    "Possession failed: missing HWJ_LivePossessionSystem.",
                    targetDataResolver);
            }

            if (!livePossessionSystem.CanAttemptTarget(targetDataResolver, out string liveMessage))
            {
                return HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetUnavailable,
                    liveMessage,
                    targetDataResolver);
            }
        }

        // 생체 빙의 중 사망해 시체가 된 대상은 기존 corpse_available 규칙이
        // Consumed 플래그로 막을 수 있으므로 이 경우에는 직접 검증 결과를 사용합니다.
        if (!canUseConsumedCorpse
            && !IsGameplayRuleSatisfied(targetDataResolver, out string ruleMessage))
        {
            return HWJ_PossessionResult.Fail(
                ResolveRuleFailureCode(ruleMessage),
                ruleMessage,
                targetDataResolver);
        }

        return HWJ_PossessionResult.Success(
            targetDataResolver,
            isLiveTarget
                ? "Live possession target is valid."
                : "Corpse possession target is valid.");
    }

    public bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver targetDataResolver,
        out HWJ_PossessionData possessionBody)
    {
        possessionBody = null;

        if (targetDataResolver == null)
        {
            return false;
        }

        if (!targetDataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return false;
        }

        if (enemyData.Role != null
            && (!enemyData.Role.leavesCorpseOnDeath
                || !enemyData.Role.isPossessableBody))
        {
            return false;
        }

        possessionBody = enemyData.PossessionBody;
        return possessionBody != null;
    }

    public bool IsLiveTarget(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        return targetDataResolver != null
            && possessionBody != null
            && !possessionBody.requiresDefeatedState
            && IsEnemyTarget(targetDataResolver)
            && !IsDefeatedTarget(targetDataResolver);
    }

    public bool IsEnemyTarget(HWJ_RootObjectDataResolver targetDataResolver)
    {
        return targetDataResolver != null
            && targetDataResolver.ObjectType == HWJ_ObjectType.Enemy;
    }

    public bool IsDefeatedTarget(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        return targetStatus != null
            && (targetStatus.CurrentState == HWJ_RuntimeState.Dead
                || (targetStatus.UsesHp && targetStatus.CurrentHp <= 0f));
    }

    public bool IsConsumedPossessionBody(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            targetDataResolver.GetComponent<HWJ_PossessionBodyState>();

        return bodyState != null && bodyState.IsConsumed;
    }

    private bool IsDefeatedIfRequired(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (possessionBody == null || !possessionBody.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        return targetStatus != null && targetStatus.IsDead;
    }

    private static bool CanUseConsumedCorpse(
        HWJ_RootObjectDataResolver targetDataResolver,
        bool isLiveTarget)
    {
        if (isLiveTarget || targetDataResolver == null)
        {
            return false;
        }

        return targetDataResolver.TryGetComponent(out HWJ_LivePossessionMentalState mentalState)
            && mentalState.CanPossessAsCorpse();
    }

    private bool IsGameplayRuleSatisfied(
        HWJ_RootObjectDataResolver targetDataResolver,
        out string message)
    {
        message = null;

        if (!useGameplayPossessionRule)
        {
            return true;
        }

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(possessionSystem.OwnerDataResolver, targetDataResolver)
            .WithSource(possessionSystem)
            .WithTarget(targetDataResolver);

        if (ResolveExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool passed = executionCore.TryExecute(
                context,
                out HWJ_RuleExecutionResult executionResult);

            message = executionResult.Message;
            return passed;
        }

        HWJ_GameplayRuleSO rule = possessionRule;

        if (rule == null
            && !string.IsNullOrEmpty(possessionRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(
                possessionRuleId,
                out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool rulePassed = rule.TryEvaluate(
            context,
            out HWJ_RuleEvaluationResult result);

        message = result.Message;
        return rulePassed;
    }

    private bool ResolveExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = possessionExecutionCore;

        if (executionCore != null)
        {
            return true;
        }

        if (string.IsNullOrEmpty(possessionExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(
            possessionExecutionCoreId,
            out executionCore);
    }

    private static HWJ_PossessionFailureCode ResolveRuleFailureCode(string ruleMessage)
    {
        if (string.IsNullOrEmpty(ruleMessage))
        {
            return HWJ_PossessionFailureCode.PossessionBlocked;
        }

        if (ruleMessage.Contains("within_possession_range"))
        {
            return HWJ_PossessionFailureCode.TargetOutOfRange;
        }

        if (ruleMessage.Contains("target_corpse_available"))
        {
            return HWJ_PossessionFailureCode.TargetAlreadyPossessed;
        }

        if (ruleMessage.Contains("source_soul_state"))
        {
            return HWJ_PossessionFailureCode.NotInSpiritState;
        }

        if (ruleMessage.Contains("source_can_possess"))
        {
            return HWJ_PossessionFailureCode.PossessionBlocked;
        }

        if (ruleMessage.Contains("source_can_pay_possession_spirit_mental_cost"))
        {
            return HWJ_PossessionFailureCode.InsufficientSpiritMental;
        }

        if (ruleMessage.Contains("target_can_be_possessed")
            || ruleMessage.Contains("target_defeated")
            || ruleMessage.Contains("target_object"))
        {
            return HWJ_PossessionFailureCode.TargetUnavailable;
        }

        return HWJ_PossessionFailureCode.PossessionBlocked;
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (livePossessionSystem == null)
        {
            livePossessionSystem = GetComponent<HWJ_LivePossessionSystem>();
        }
    }
}
