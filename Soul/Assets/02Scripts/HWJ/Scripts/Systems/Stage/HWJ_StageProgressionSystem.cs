using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_StageProgressionSystem : MonoBehaviour
{
    [SerializeField] private string stageId;
    [SerializeField] private string nextRegionId;
    [SerializeField] private HWJ_StageFlowState initialState = HWJ_StageFlowState.Entering;
    [SerializeField] private HWJ_StageFlowState currentState = HWJ_StageFlowState.None;
    [SerializeField] private bool objectiveComplete;
    [SerializeField] private bool bossUnlocked;
    [SerializeField] private bool bossBattleStarted;
    [SerializeField] private bool bossDefeated;
    [SerializeField] private bool regionUnlocked;
    [SerializeField] private bool transitionLocked;
    [SerializeField] private bool requireObjectiveCompleteForBossBattle = true;
    [SerializeField] private bool requireBossReadyForBossBattle = true;
    [SerializeField] private bool requirePossessedBodyForBossBattle;
    [SerializeField] private HWJ_SoulSystem playerSoulSystem;
    [SerializeField] private HWJ_PossessionSystem playerPossessionSystem;
    [SerializeField] private HWJ_CollapseSystem playerCollapseSystem;
    [SerializeField] private HWJ_StageFlowTransitionFailureCode lastFailureCode;
    [SerializeField] private string lastTransitionMessage;

    public string StageId => stageId;
    public string NextRegionId => nextRegionId;
    public HWJ_StageFlowState CurrentState => currentState;
    public bool ObjectiveComplete => objectiveComplete;
    public bool BossUnlocked => bossUnlocked;
    public bool BossBattleStarted => bossBattleStarted;
    public bool BossDefeated => bossDefeated;
    public bool RegionUnlocked => regionUnlocked;
    public bool TransitionLocked => transitionLocked;
    public HWJ_StageFlowTransitionFailureCode LastFailureCode => lastFailureCode;
    public string LastTransitionMessage => lastTransitionMessage;

    private void Awake()
    {
        ResolveReferences();

        if (currentState == HWJ_StageFlowState.None && initialState != HWJ_StageFlowState.None)
        {
            ForceSetState(initialState, "Initial stage flow state.");
        }
    }

    private void Reset()
    {
        ResolveReferences();
        currentState = initialState;
    }

    public void SetStageIds(string newStageId, string newNextRegionId)
    {
        stageId = newStageId;
        nextRegionId = newNextRegionId;
    }

    public void SetTransitionLocked(bool locked)
    {
        transitionLocked = locked;
    }

    public HWJ_RuntimeStageFlowSnapshot CreateSnapshot()
    {
        return HWJ_RuntimeStageFlowSnapshot.FromStage(this);
    }

    /// <summary>
    /// 저장 데이터에서 스테이지 진행 상태를 복원합니다.
    /// 로드 시점에는 이전 상태 전환 순서를 재생하지 않고 저장된 플래그를 그대로 맞춥니다.
    /// </summary>
    public void RestoreStageFlowSnapshot(HWJ_RuntimeStageFlowSnapshot snapshot, bool raiseStateEvent = true)
    {
        HWJ_StageFlowState previousState = currentState;
        stageId = snapshot.stageId;
        nextRegionId = snapshot.nextRegionId;
        currentState = snapshot.currentState != HWJ_StageFlowState.None
            ? snapshot.currentState
            : initialState;
        objectiveComplete = snapshot.objectiveComplete;
        bossUnlocked = snapshot.bossUnlocked;
        bossBattleStarted = snapshot.bossBattleStarted;
        bossDefeated = snapshot.bossDefeated;
        regionUnlocked = snapshot.regionUnlocked;
        transitionLocked = snapshot.transitionLocked;

        StoreResult(HWJ_StageFlowTransitionResult.Success(
            previousState,
            currentState,
            "Stage flow restored from save data."));

        if (raiseStateEvent && previousState != currentState)
        {
            RaiseStageFlowStateChanged(previousState, currentState, "Stage flow restored from save data.");
        }
    }

    public HWJ_StageFlowTransitionResult TryEnterExploring(string reason = null)
    {
        return TrySetState(HWJ_StageFlowState.Exploring, reason ?? "Stage exploration started.");
    }

    public HWJ_StageFlowTransitionResult TryEnterCombat(string reason = null)
    {
        return TrySetState(HWJ_StageFlowState.Combat, reason ?? "Stage combat started.");
    }

    public HWJ_StageFlowTransitionResult TryMarkObjectiveComplete(string reason = null)
    {
        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.ObjectiveComplete,
            out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        objectiveComplete = true;
        HWJ_StageFlowTransitionResult result = ForceSetState(
            HWJ_StageFlowState.ObjectiveComplete,
            reason ?? "Stage objective completed.");

        if (result.Succeeded)
        {
            RaiseStageObjectiveChanged(reason ?? "Stage objective completed.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryUnlockBoss(string reason = null)
    {
        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.BossReady,
            out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        if (!objectiveComplete && requireObjectiveCompleteForBossBattle)
        {
            return StoreResult(HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.ObjectiveNotComplete,
                currentState,
                HWJ_StageFlowState.BossReady,
                "Boss unlock failed: stage objective is not complete."));
        }

        if (bossDefeated)
        {
            return StoreResult(HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.BossAlreadyDefeated,
                currentState,
                HWJ_StageFlowState.BossReady,
                "Boss unlock failed: boss is already defeated."));
        }

        bossUnlocked = true;
        HWJ_StageFlowTransitionResult result = ForceSetState(HWJ_StageFlowState.BossReady, reason ?? "Boss is ready.");

        if (result.Succeeded)
        {
            RaiseBossUnlocked(reason ?? "Boss unlocked.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryStartBossBattle(string reason = null)
    {
        HWJ_StageFlowTransitionResult validationResult = ValidateBossBattleStart();

        if (!validationResult.Succeeded)
        {
            return StoreResult(validationResult);
        }

        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.BossBattle,
            out HWJ_StageFlowTransitionResult transitionValidationResult))
        {
            return StoreResult(transitionValidationResult);
        }

        bossBattleStarted = true;
        HWJ_StageFlowTransitionResult result = ForceSetState(HWJ_StageFlowState.BossBattle, reason ?? "Boss battle started.");

        if (result.Succeeded)
        {
            RaiseBossBattleStarted(reason ?? "Boss battle started.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryMarkBossDefeated(string reason = null)
    {
        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.StageClear,
            out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        bossDefeated = true;
        HWJ_StageFlowTransitionResult result = ForceSetState(
            HWJ_StageFlowState.StageClear,
            reason ?? "Boss defeated. Stage clear.");

        if (result.Succeeded)
        {
            RaiseBossDefeated(reason ?? "Boss defeated.");
            RaiseStageCleared(reason ?? "Stage cleared.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryClearStage(string reason = null)
    {
        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.StageClear,
            out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        HWJ_StageFlowTransitionResult result = ForceSetState(HWJ_StageFlowState.StageClear, reason ?? "Stage cleared.");

        if (result.Succeeded)
        {
            RaiseStageCleared(reason ?? "Stage cleared.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryUnlockRegion(string regionId, string reason = null)
    {
        if (!TryValidateTransitionRequest(
            HWJ_StageFlowState.RegionTransition,
            out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        if (!string.IsNullOrEmpty(regionId))
        {
            nextRegionId = regionId;
        }

        regionUnlocked = true;
        HWJ_StageFlowTransitionResult result = ForceSetState(HWJ_StageFlowState.RegionTransition, reason ?? "Region transition started.");

        if (result.Succeeded)
        {
            RaiseRegionUnlocked(reason ?? "Region unlocked.");
        }

        return result;
    }

    public HWJ_StageFlowTransitionResult TryFailStage(string reason = null)
    {
        return TrySetState(HWJ_StageFlowState.Failed, reason ?? "Stage failed.");
    }

    public HWJ_StageFlowTransitionResult TrySetState(HWJ_StageFlowState nextState, string reason = null)
    {
        if (!TryValidateTransitionRequest(nextState, out HWJ_StageFlowTransitionResult validationResult))
        {
            return StoreResult(validationResult);
        }

        return ForceSetState(nextState, reason ?? $"Stage state changed to {nextState}.");
    }

    public HWJ_StageFlowTransitionResult ResetStageFlow(
        HWJ_StageFlowState resetState = HWJ_StageFlowState.Entering,
        bool clearProgressFlags = true)
    {
        if (resetState == HWJ_StageFlowState.None)
        {
            resetState = HWJ_StageFlowState.Entering;
        }

        if (clearProgressFlags)
        {
            objectiveComplete = false;
            bossUnlocked = false;
            bossBattleStarted = false;
            bossDefeated = false;
            regionUnlocked = false;
        }

        transitionLocked = false;
        return ForceSetState(resetState, "Stage flow reset.");
    }

    private HWJ_StageFlowTransitionResult ForceSetState(HWJ_StageFlowState nextState, string reason)
    {
        HWJ_StageFlowState previousState = currentState;
        currentState = nextState;
        HWJ_StageFlowTransitionResult result = HWJ_StageFlowTransitionResult.Success(
            previousState,
            currentState,
            reason);
        StoreResult(result);
        RaiseStageFlowStateChanged(previousState, currentState, reason);
        return result;
    }

    private HWJ_StageFlowTransitionResult ValidateBossBattleStart()
    {
        ResolveReferences();

        if (requireObjectiveCompleteForBossBattle && !objectiveComplete)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.ObjectiveNotComplete,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: stage objective is not complete.");
        }

        if (requireBossReadyForBossBattle && !bossUnlocked)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.BossNotReady,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: boss is not unlocked.");
        }

        if (bossDefeated)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.BossAlreadyDefeated,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: boss is already defeated.");
        }

        if (!requirePossessedBodyForBossBattle)
        {
            return HWJ_StageFlowTransitionResult.Success(
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start request is valid.");
        }

        if (playerSoulSystem == null)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.MissingPlayerSoul,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: missing player soul system.");
        }

        if (playerSoulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Possessed
            || playerPossessionSystem == null
            || !playerPossessionSystem.HasActivePossessedBody)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.PlayerNotPossessed,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: player is not possessing a valid body.");
        }

        if (playerCollapseSystem != null && playerCollapseSystem.IsCollapsing)
        {
            return HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.PlayerCollapsing,
                currentState,
                HWJ_StageFlowState.BossBattle,
                "Boss battle start failed: possessed body is collapsing.");
        }

        return HWJ_StageFlowTransitionResult.Success(
            currentState,
            HWJ_StageFlowState.BossBattle,
            "Boss battle start request is valid.");
    }

    private bool TryValidateTransitionRequest(
        HWJ_StageFlowState nextState,
        out HWJ_StageFlowTransitionResult result)
    {
        if (transitionLocked)
        {
            result = HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.TransitionLocked,
                currentState,
                nextState,
                "Stage transition failed: transition is locked.");
            return false;
        }

        if (nextState == HWJ_StageFlowState.None)
        {
            result = HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.InvalidState,
                currentState,
                nextState,
                "Stage transition failed: None is not a runtime stage flow state.");
            return false;
        }

        if (currentState == nextState)
        {
            result = HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.SameState,
                currentState,
                nextState,
                "Stage transition skipped: requested state is already active.");
            return false;
        }

        if (!CanTransition(currentState, nextState))
        {
            result = HWJ_StageFlowTransitionResult.Fail(
                HWJ_StageFlowTransitionFailureCode.InvalidTransition,
                currentState,
                nextState,
                $"Stage transition failed: {currentState} cannot transition to {nextState}.");
            return false;
        }

        result = HWJ_StageFlowTransitionResult.Success(
            currentState,
            nextState,
            "Stage transition request is valid.");
        return true;
    }

    private static bool CanTransition(HWJ_StageFlowState from, HWJ_StageFlowState to)
    {
        if (from == HWJ_StageFlowState.None)
        {
            return to == HWJ_StageFlowState.Entering
                || to == HWJ_StageFlowState.Exploring
                || to == HWJ_StageFlowState.Failed;
        }

        if (to == HWJ_StageFlowState.Failed)
        {
            return true;
        }

        switch (from)
        {
            case HWJ_StageFlowState.Entering:
                return to == HWJ_StageFlowState.Exploring
                    || to == HWJ_StageFlowState.Combat;
            case HWJ_StageFlowState.Exploring:
                return to == HWJ_StageFlowState.Combat
                    || to == HWJ_StageFlowState.ObjectiveComplete
                    || to == HWJ_StageFlowState.BossReady
                    || to == HWJ_StageFlowState.StageClear;
            case HWJ_StageFlowState.Combat:
                return to == HWJ_StageFlowState.Exploring
                    || to == HWJ_StageFlowState.ObjectiveComplete
                    || to == HWJ_StageFlowState.BossReady;
            case HWJ_StageFlowState.ObjectiveComplete:
                return to == HWJ_StageFlowState.Exploring
                    || to == HWJ_StageFlowState.BossReady
                    || to == HWJ_StageFlowState.StageClear;
            case HWJ_StageFlowState.BossReady:
                return to == HWJ_StageFlowState.BossBattle
                    || to == HWJ_StageFlowState.Exploring;
            case HWJ_StageFlowState.BossBattle:
                return to == HWJ_StageFlowState.StageClear;
            case HWJ_StageFlowState.StageClear:
                return to == HWJ_StageFlowState.RegionTransition;
            case HWJ_StageFlowState.RegionTransition:
                return to == HWJ_StageFlowState.Entering;
            case HWJ_StageFlowState.Failed:
                return to == HWJ_StageFlowState.Entering;
            default:
                return false;
        }
    }

    private HWJ_StageFlowTransitionResult StoreResult(HWJ_StageFlowTransitionResult result)
    {
        lastFailureCode = result.FailureCode;
        lastTransitionMessage = result.Message;
        return result;
    }

    private void ResolveReferences()
    {
        if (playerSoulSystem == null && HWJ_GameAccess.HasManager)
        {
            playerSoulSystem = HWJ_GameAccess.Manager.PlayerSoul;
        }

        if (playerPossessionSystem == null && HWJ_GameAccess.HasManager)
        {
            playerPossessionSystem = HWJ_GameAccess.Manager.PlayerPossession;
        }

        if (playerCollapseSystem == null
            && playerSoulSystem != null)
        {
            playerCollapseSystem = playerSoulSystem.GetComponent<HWJ_CollapseSystem>();
        }
    }

    private void RaiseStageFlowStateChanged(
        HWJ_StageFlowState previousState,
        HWJ_StageFlowState nextState,
        string reason)
    {
        HWJ_GameplayEvents.RaiseStageFlowStateChanged(
            new HWJ_StageFlowStateChangedEvent(this, stageId, previousState, nextState, reason));
    }

    private void RaiseStageObjectiveChanged(string message)
    {
        HWJ_GameplayEvents.RaiseStageObjectiveChanged(CreateProgressionEvent(message));
    }

    private void RaiseBossUnlocked(string message)
    {
        HWJ_GameplayEvents.RaiseBossUnlocked(CreateProgressionEvent(message));
    }

    private void RaiseBossBattleStarted(string message)
    {
        HWJ_GameplayEvents.RaiseBossBattleStarted(CreateProgressionEvent(message));
    }

    private void RaiseBossDefeated(string message)
    {
        HWJ_GameplayEvents.RaiseBossDefeated(CreateProgressionEvent(message));
    }

    private void RaiseStageCleared(string message)
    {
        HWJ_GameplayEvents.RaiseStageCleared(CreateProgressionEvent(message));
    }

    private void RaiseRegionUnlocked(string message)
    {
        HWJ_GameplayEvents.RaiseRegionUnlocked(CreateProgressionEvent(message));
    }

    private HWJ_StageProgressionEvent CreateProgressionEvent(string message)
    {
        return new HWJ_StageProgressionEvent(
            this,
            stageId,
            nextRegionId,
            currentState,
            objectiveComplete,
            bossUnlocked,
            bossBattleStarted,
            bossDefeated,
            regionUnlocked,
            message);
    }
}
