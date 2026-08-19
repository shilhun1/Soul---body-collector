using System;
using UnityEngine;

/// <summary>
/// 빙의 대상의 종류, 상태, 사용 여부, 게임플레이 규칙을 검사합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionTargetValidator : MonoBehaviour
{
    private static readonly string[] DefaultDeathAnimationClipKeywords =
    {
        "death",
        "die",
        "dead"
    };

    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_LivePossessionSystem livePossessionSystem;

    [Space(8f)]
    [Header("빙의 규칙")]
    [SerializeField] private bool useGameplayPossessionRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO possessionExecutionCore;
    [SerializeField] private string possessionExecutionCoreId = "possession_execution";
    [SerializeField] private HWJ_GameplayRuleSO possessionRule;
    [SerializeField] private string possessionRuleId = "possession_can_start";

    [Space(8f)]
    [Header("Corpse Readiness")]
    [Tooltip("If enabled, corpse possession waits until the target death/falling animation is finished.")]
    [SerializeField] private bool blockCorpsePossessionDuringDeathAnimation = true;
    [Tooltip("Animation clip name keywords that count as a death/falling corpse animation.")]
    [SerializeField] private string[] deathAnimationClipKeywords = { "death", "die", "dead" };

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

        if (!isLiveTarget
            && IsCorpseReadinessBlocked(targetDataResolver, out string corpseReadinessMessage))
        {
            return HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                corpseReadinessMessage,
                targetDataResolver);
        }

        // 생체 대상은 수동 해제 후 재빙의할 수 있으므로 Consumed 플래그만으로 차단하지 않습니다.
        if (IsConsumedPossessionBody(targetDataResolver)
            && !isLiveTarget)
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

        if (!IsGameplayRuleSatisfied(targetDataResolver, out string ruleMessage))
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

        if (enemyData.Role == null || !enemyData.Role.isPossessableBody)
        {
            return false;
        }

        possessionBody = enemyData.PossessionBody;

        if (possessionBody == null)
        {
            return false;
        }

        // 생체 빙의 가능 여부와 사망 후 시체 유지 여부는 서로 다른 규칙입니다.
        // HP 0으로 제거되는 일반 몬스터는 살아 있을 때만 빙의할 수 있습니다.
        return !IsDefeatedTarget(targetDataResolver)
            || enemyData.Role.leavesCorpseOnDeath;
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

    private bool IsCorpseReadinessBlocked(
        HWJ_RootObjectDataResolver targetDataResolver,
        out string message)
    {
        message = null;

        if (targetDataResolver == null)
        {
            return false;
        }

        if (targetDataResolver.TryGetComponent(out HWJ_EnemyDeathLifecycleSystem deathLifecycle)
            && !deathLifecycle.IsCorpsePossessionReady)
        {
            message = "Possession failed: target corpse is still playing its death animation.";
            return true;
        }

        if (IsDeathAnimationStillPlaying(targetDataResolver))
        {
            message = "Possession failed: target corpse death animation is not finished.";
            return true;
        }

        return false;
    }

    private bool IsDeathAnimationStillPlaying(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!blockCorpsePossessionDuringDeathAnimation || targetDataResolver == null)
        {
            return false;
        }

        Animator animator = targetDataResolver.GetComponentInChildren<Animator>(true);

        if (animator == null
            || !animator.isActiveAndEnabled
            || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        int layerCount = animator.layerCount;

        if (layerCount <= 0)
        {
            return false;
        }

        for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
        {
            if (IsLayerPlayingBlockingDeathClip(animator, layerIndex, false))
            {
                return true;
            }

            if (animator.IsInTransition(layerIndex)
                && IsLayerPlayingBlockingDeathClip(animator, layerIndex, true))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLayerPlayingBlockingDeathClip(
        Animator animator,
        int layerIndex,
        bool useNextClip)
    {
        AnimatorClipInfo[] clips = useNextClip
            ? animator.GetNextAnimatorClipInfo(layerIndex)
            : animator.GetCurrentAnimatorClipInfo(layerIndex);

        if (clips == null || clips.Length == 0)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = useNextClip
            ? animator.GetNextAnimatorStateInfo(layerIndex)
            : animator.GetCurrentAnimatorStateInfo(layerIndex);

        if (!useNextClip && stateInfo.normalizedTime >= 1f)
        {
            return false;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i].clip;

            if (clip != null && IsBlockingDeathClipName(clip.name))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlockingDeathClipName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            return false;
        }

        string[] keywords = deathAnimationClipKeywords != null && deathAnimationClipKeywords.Length > 0
            ? deathAnimationClipKeywords
            : DefaultDeathAnimationClipKeywords;

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (!string.IsNullOrEmpty(keyword)
                && clipName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
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
