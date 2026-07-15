using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_CoreLoopCoordinator : MonoBehaviour
{
    [Header("Player Flow")]
    [SerializeField] private HWJ_SoulSystem playerSoulSystem;
    [SerializeField] private HWJ_BodyDiscoverySystem bodyDiscoverySystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_CollapseSystem collapseSystem;

    [Header("Stage Flow")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;

    [Header("Boss Flow")]
    [SerializeField] private HWJ_BossFlowSystem bossFlowSystem;

    [Header("Reference Resolution")]
    [SerializeField] private bool useGameManagerPlayerFallback = true;

    [Header("Runtime Guard")]
    [SerializeField] private bool sceneTransitionInProgress;
    [SerializeField] private HWJ_CoreLoopOperationType lastOperationType;
    [SerializeField] private HWJ_CoreLoopFailureCode lastFailureCode;
    [SerializeField] private string lastOperationMessage;

    private HWJ_CoreLoopOperationResult lastOperationResult;

    public bool SceneTransitionInProgress => sceneTransitionInProgress;
    public HWJ_CoreLoopOperationResult LastOperationResult => lastOperationResult;
    public HWJ_CoreLoopOperationType LastOperationType => lastOperationType;
    public HWJ_CoreLoopFailureCode LastFailureCode => lastFailureCode;
    public string LastOperationMessage => lastOperationMessage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public HWJ_CoreLoopOperationResult RefreshPossessionTarget()
    {
        ResolveReferences();
        return StoreAndRaise(BuildPossessionTargetRefreshResult(
            HWJ_CoreLoopOperationType.RefreshPossessionTarget));
    }

    public HWJ_CoreLoopOperationResult TryPossessBestDiscoveredBody()
    {
        ResolveReferences();

        if (IsTransitionBlocked())
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                HWJ_CoreLoopOperationType.PossessDiscoveredBody,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress."));
        }

        HWJ_CoreLoopOperationResult discoveryResult = BuildPossessionTargetRefreshResult(
            HWJ_CoreLoopOperationType.PossessDiscoveredBody);

        if (!discoveryResult.Succeeded)
        {
            return StoreAndRaise(discoveryResult);
        }

        return TryPossessTargetInternal(
            discoveryResult.TargetResolver,
            HWJ_CoreLoopOperationType.PossessDiscoveredBody);
    }

    public HWJ_CoreLoopOperationResult TryPossessTarget(HWJ_RootObjectDataResolver targetResolver)
    {
        ResolveReferences();
        return TryPossessTargetInternal(targetResolver, HWJ_CoreLoopOperationType.PossessTarget);
    }

    public HWJ_CoreLoopOperationResult RequestBodyCollapse(
        HWJ_BodyCollapseReason reason,
        bool refillSoulHp = false)
    {
        ResolveReferences();

        if (IsTransitionBlocked())
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                HWJ_CoreLoopOperationType.CollapseBody,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress."));
        }

        if (collapseSystem == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                HWJ_CoreLoopOperationType.CollapseBody,
                HWJ_CoreLoopFailureCode.MissingCollapseSystem,
                "Core loop failed: missing HWJ_CollapseSystem."));
        }

        return StoreAndRaise(HWJ_CoreLoopOperationResult.FromCollapse(
            HWJ_CoreLoopOperationType.CollapseBody,
            collapseSystem.TryCollapseCurrentBody(reason, refillSoulHp)));
    }

    public HWJ_CoreLoopOperationResult TryEnterExploring(string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.EnterExploring,
            stage => stage.TryEnterExploring(reason));
    }

    public HWJ_CoreLoopOperationResult TryEnterCombat(string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.EnterCombat,
            stage => stage.TryEnterCombat(reason));
    }

    public HWJ_CoreLoopOperationResult TryCompleteObjective(string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.CompleteObjective,
            stage => stage.TryMarkObjectiveComplete(reason));
    }

    public HWJ_CoreLoopOperationResult TryUnlockBoss(string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.UnlockBoss,
            stage => stage.TryUnlockBoss(reason));
    }

    public HWJ_CoreLoopOperationResult TryStartBossBattle(string reason = null)
    {
        // Boss battle entry has body and rule gates, so it must go through BossFlowSystem first.
        return TryRunBossFlow(
            HWJ_CoreLoopOperationType.StartBossBattle,
            bossFlow => bossFlow.TryStartBossBattle(reason));
    }

    public HWJ_CoreLoopOperationResult TryMarkBossDefeated(
        string reason = null,
        bool unlockNextRegion = false,
        string nextRegionId = null)
    {
        return TryRunBossFlow(
            HWJ_CoreLoopOperationType.MarkBossDefeated,
            bossFlow => bossFlow.TryMarkBossDefeated(reason, unlockNextRegion, nextRegionId));
    }

    public HWJ_CoreLoopOperationResult TryClearStage(string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.ClearStage,
            stage => stage.TryClearStage(reason));
    }

    public HWJ_CoreLoopOperationResult TryUnlockRegion(string regionId, string reason = null)
    {
        return TryRunStageTransition(
            HWJ_CoreLoopOperationType.UnlockRegion,
            stage => stage.TryUnlockRegion(regionId, reason));
    }

    public HWJ_CoreLoopOperationResult SetSceneTransitionInProgress(
        bool inProgress,
        string reason = null)
    {
        ResolveReferences();
        sceneTransitionInProgress = inProgress;
        stageProgressionSystem?.SetTransitionLocked(inProgress);

        return StoreAndRaise(HWJ_CoreLoopOperationResult.Success(
            HWJ_CoreLoopOperationType.SetSceneTransition,
            reason ?? (inProgress
                ? "Core loop scene transition guard enabled."
                : "Core loop scene transition guard disabled.")));
    }

    private HWJ_CoreLoopOperationResult TryPossessTargetInternal(
        HWJ_RootObjectDataResolver targetResolver,
        HWJ_CoreLoopOperationType operationType)
    {
        if (IsTransitionBlocked())
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress.",
                targetResolver));
        }

        if (possessionSystem == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.MissingPossessionSystem,
                "Core loop failed: missing HWJ_PossessionSystem.",
                targetResolver));
        }

        if (targetResolver == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.FromPossession(
                operationType,
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.InvalidTarget,
                    "Core loop possession failed: missing target.")));
        }

        bool possessed = possessionSystem.TryPossess(targetResolver);
        HWJ_PossessionResult possessionResult = possessed
            ? HWJ_PossessionResult.Success(targetResolver, possessionSystem.LastPossessionResult)
            : HWJ_PossessionResult.Fail(
                possessionSystem.LastPossessionFailureCode,
                possessionSystem.LastPossessionResult,
                targetResolver);

        return StoreAndRaise(HWJ_CoreLoopOperationResult.FromPossession(
            operationType,
            possessionResult));
    }

    private HWJ_CoreLoopOperationResult BuildPossessionTargetRefreshResult(
        HWJ_CoreLoopOperationType operationType)
    {
        if (IsTransitionBlocked())
        {
            return HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress.");
        }

        if (bodyDiscoverySystem == null)
        {
            return HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.MissingBodyDiscoverySystem,
                "Core loop failed: missing HWJ_BodyDiscoverySystem.");
        }

        bool hasTarget = bodyDiscoverySystem.TryGetBestTarget(
            out HWJ_RootObjectDataResolver target,
            out HWJ_BodyDiscoveryResult discoveryResult);

        if (!hasTarget)
        {
            return HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.NoPossessionTarget,
                discoveryResult.Message);
        }

        return HWJ_CoreLoopOperationResult.Success(
            operationType,
            discoveryResult.Message,
            target);
    }

    private HWJ_CoreLoopOperationResult TryRunStageTransition(
        HWJ_CoreLoopOperationType operationType,
        System.Func<HWJ_StageProgressionSystem, HWJ_StageFlowTransitionResult> transitionRequest)
    {
        ResolveReferences();

        if (IsTransitionBlocked())
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress."));
        }

        if (stageProgressionSystem == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.MissingStageProgressionSystem,
                "Core loop failed: missing HWJ_StageProgressionSystem."));
        }

        if (transitionRequest == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.InvalidRequest,
                "Core loop failed: missing stage transition request."));
        }

        return StoreAndRaise(HWJ_CoreLoopOperationResult.FromStageTransition(
            operationType,
            transitionRequest(stageProgressionSystem)));
    }

    private HWJ_CoreLoopOperationResult TryRunBossFlow(
        HWJ_CoreLoopOperationType operationType,
        System.Func<HWJ_BossFlowSystem, HWJ_BossFlowResult> bossFlowRequest)
    {
        ResolveReferences();

        if (IsTransitionBlocked())
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.TransitionInProgress,
                "Core loop failed: scene transition is in progress."));
        }

        if (bossFlowSystem == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.MissingBossFlowSystem,
                "Core loop failed: missing HWJ_BossFlowSystem."));
        }

        if (bossFlowRequest == null)
        {
            return StoreAndRaise(HWJ_CoreLoopOperationResult.Fail(
                operationType,
                HWJ_CoreLoopFailureCode.InvalidRequest,
                "Core loop failed: missing boss flow request."));
        }

        return StoreAndRaise(HWJ_CoreLoopOperationResult.FromBossFlow(
            operationType,
            bossFlowRequest(bossFlowSystem)));
    }

    private bool IsTransitionBlocked()
    {
        return sceneTransitionInProgress
            || (playerSoulSystem != null
                && playerSoulSystem.CurrentExistenceState == HWJ_PlayerExistenceState.Transitioning);
    }

    private HWJ_CoreLoopOperationResult StoreAndRaise(HWJ_CoreLoopOperationResult result)
    {
        lastOperationResult = result;
        lastOperationType = result.OperationType;
        lastFailureCode = result.FailureCode;
        lastOperationMessage = result.Message;
        HWJ_GameplayEvents.RaiseCoreLoopOperationCompleted(
            new HWJ_CoreLoopOperationEvent(this, result));
        return result;
    }

    private void ResolveReferences()
    {
        if (playerSoulSystem == null)
        {
            playerSoulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (bodyDiscoverySystem == null)
        {
            bodyDiscoverySystem = GetComponent<HWJ_BodyDiscoverySystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (collapseSystem == null)
        {
            collapseSystem = GetComponent<HWJ_CollapseSystem>();
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponent<HWJ_StageProgressionSystem>();
        }

        if (bossFlowSystem == null)
        {
            bossFlowSystem = GetComponent<HWJ_BossFlowSystem>();
        }

        // Scene assignment is preferred; auto search is only a fallback for simple one-boss scenes and tests.
        if (bossFlowSystem == null)
        {
            HWJ_BossFlowSystem[] bossFlows = FindObjectsByType<HWJ_BossFlowSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (bossFlows.Length == 1)
            {
                bossFlowSystem = bossFlows[0];
            }
        }

        if (!useGameManagerPlayerFallback || !HWJ_GameAccess.HasManager)
        {
            return;
        }

        if (playerSoulSystem == null)
        {
            playerSoulSystem = HWJ_GameAccess.Manager.PlayerSoul;
        }

        if (bodyDiscoverySystem == null && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            bodyDiscoverySystem = HWJ_GameAccess.Manager.PlayerResolver.GetComponent<HWJ_BodyDiscoverySystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = HWJ_GameAccess.Manager.PlayerPossession;
        }

        if (collapseSystem == null && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            collapseSystem = HWJ_GameAccess.Manager.PlayerResolver.GetComponent<HWJ_CollapseSystem>();
        }
    }
}
