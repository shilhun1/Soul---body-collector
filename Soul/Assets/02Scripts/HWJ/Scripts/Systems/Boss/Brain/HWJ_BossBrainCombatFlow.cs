using UnityEngine;

/// <summary>
/// Runs encounter activation, soul-target behavior, and phase transition flow.
/// This partial belongs to the single HWJ_BossBrainSystem component.
/// </summary>
public partial class HWJ_BossBrainSystem
{
    private void RunCombatLoop(HWJ_BossTypeDataSO bossData)
    {
        // 두 줄 체력형 보스는 전환/사망 상태에서 공통 패턴 루프를 돌리지 않습니다.
        if (useTwoBarPhaseHealth
            && fighterPhase != HWJ_FighterBossPhase.Phase1
            && fighterPhase != HWJ_FighterBossPhase.Phase2)
        {
            SetBossState(HWJ_BossFSMState.Idle);
            StopHorizontalMovement();
            return;
        }

        if (target == null)
        {
            SetBossState(HWJ_BossFSMState.Idle);
            StopHorizontalMovement();
            return;
        }

        FaceTarget();

        float distanceX = Mathf.Abs(target.position.x - transform.position.x);
        float attackRange = GetAttackRange(bossData);
        bool isCloseRange = distanceX <= Mathf.Max(0.1f, bossData.FSM.closeSkillRange);

        if (distanceX <= attackRange
            && patternSystem != null
            && patternSystem.TryUseAvailablePattern(target, currentPhaseNumber, isCloseRange))
        {
            SetBossState(HWJ_BossFSMState.Attack);
            StopHorizontalMovement();
            return;
        }

        float optimalDistance = GetOptimalDistance(bossData);

        if (distanceX > optimalDistance)
        {
            SetBossState(HWJ_BossFSMState.Chase);
            MoveTowardTarget(bossData);
            return;
        }

        SetBossState(HWJ_BossFSMState.Idle);
        StopHorizontalMovement();
    }

    private bool ShouldStartEncounter(HWJ_BossTypeDataSO bossData)
    {
        if (target == null)
        {
            return false;
        }

        if (IsTargetSoulState())
        {
            return false;
        }

        if (!bossData.FSM.autoStartWhenPlayerEntersRoom)
        {
            return false;
        }

        if (!bossData.FSM.useBossRoomBounds)
        {
            return true;
        }

        Vector2 center = (Vector2)transform.position + bossData.FSM.bossRoomOffset;
        Vector2 halfSize = bossData.FSM.bossRoomSize * 0.5f;
        Vector2 targetPosition = target.position;

        return Mathf.Abs(targetPosition.x - center.x) <= halfSize.x
            && Mathf.Abs(targetPosition.y - center.y) <= halfSize.y;
    }

    private bool HandleSoulTargetState(HWJ_BossTypeDataSO bossData)
    {
        if (!IsTargetSoulState())
        {
            wasTargetSoulState = false;
            return false;
        }

        if (!wasTargetSoulState)
        {
            CancelCurrentBossActions();
            dialogueBubbleSystem?.ShowSoulLostDialogue();
            cameraFocusSystem?.FocusOnBoss(
                transform,
                target,
                dialogueBubbleSystem != null
                    ? dialogueBubbleSystem.GetSoulLostDialogueDuration()
                    : GetCameraFocusSeconds());
        }

        wasTargetSoulState = true;
        SetBossState(HWJ_BossFSMState.Idle);
        MoveTowardPosition(GetBossRoomCenter(), bossData, bossData.FSM.soulReturnCenterStoppingDistance);
        return true;
    }

    private bool TryEnterPhaseTransition(HWJ_BossTypeDataSO bossData)
    {
        if (useTwoBarPhaseHealth)
        {
            return false;
        }

        if (!phaseTwoTriggered
            && runtimeStatus != null
            && runtimeStatus.MaxHp > 0f
            && runtimeStatus.CurrentHp / runtimeStatus.MaxHp <= Mathf.Clamp01(bossData.FSM.phaseTwoHpRatio))
        {
            phaseTwoTriggered = true;
            SetCurrentPhaseNumber(2);
            currentPhaseId = "phase_2";
            pendingPhaseIndex = 1;
            StartPhaseTransition(bossData);
            return true;
        }

        int nextPhaseIndex = ResolvePhaseIndex();

        if (nextPhaseIndex < 0 || nextPhaseIndex == currentPhaseIndex)
        {
            return false;
        }

        currentPhaseIndex = nextPhaseIndex;
        SetCurrentPhaseNumber(Mathf.Max(1, nextPhaseIndex + 1));
        currentPhaseId = GetPhaseId(currentPhaseIndex);
        pendingPhaseIndex = nextPhaseIndex;
        StartPhaseTransition(bossData);
        return true;
    }

    private void StartPhaseTransition(HWJ_BossTypeDataSO bossData)
    {
        CancelCurrentBossActions();
        TeleportToRoomCenter();
        SetBossState(HWJ_BossFSMState.PhaseTransition);
        StopHorizontalMovement();
        dialogueBubbleSystem?.ShowPhaseTwoDialogue();

        float transitionDuration = Mathf.Max(0f, bossData.FSM.phaseTransitionSeconds);
        float transitionLaserSeconds = 0f;
        bool startedTransitionLaser = currentPhaseNumber == 2
            && stageOnePatternSystem != null
            && stageOnePatternSystem.TryExecutePhaseTwoTransitionLaser(target, out transitionLaserSeconds);

        if (startedTransitionLaser)
        {
            transitionDuration = Mathf.Max(transitionDuration, transitionLaserSeconds);
        }
        else
        {
            patternSystem?.TryUsePhaseChangedPattern(target);
        }

        phaseTransitionTimer = transitionDuration;
        runtimeStatus?.GrantInvincibility(phaseTransitionTimer + 0.1f);
        FocusCameraForDialogue(
            HWJ_BossDialogueSequenceType.PhaseTransition,
            Mathf.Max(GetCameraFocusSeconds(), phaseTransitionTimer));
    }

    private void StartTwoBarPhaseTransition()
    {
        if (fighterPhase != HWJ_FighterBossPhase.Phase1
            || !TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return;
        }

        phaseTwoTriggered = true;
        fighterPhase = HWJ_FighterBossPhase.Transition;
        encounterStarted = true;
        pendingPhaseIndex = 1;
        currentPhaseId = "transition";
        phaseTransitionElapsed = 0f;
        transitionAnimationStep = -1;

        CancelCurrentBossActions();
        TeleportToRoomCenter();
        SetBossState(HWJ_BossFSMState.PhaseTransition);
        StopHorizontalMovement();
        fighterAnimatorSystem?.BeginPhaseTransition();
        dialogueBubbleSystem?.ShowPhaseTwoDialogue();

        phaseTransitionTimer = Mathf.Max(0.1f, bossData.FSM.phaseTransitionSeconds);
        runtimeStatus?.GrantInvincibility(phaseTransitionTimer + 0.05f);
        FocusCameraForDialogue(
            HWJ_BossDialogueSequenceType.PhaseTransition,
            Mathf.Max(GetCameraFocusSeconds(), phaseTransitionTimer));
        UpdateTwoBarTransitionAnimation(0f);
    }

    private bool UpdatePhaseTransition()
    {
        if (currentState != HWJ_BossFSMState.PhaseTransition)
        {
            return false;
        }

        StopHorizontalMovement();
        phaseTransitionTimer -= Time.deltaTime;
        phaseTransitionElapsed += Time.deltaTime;

        if (useTwoBarPhaseHealth && fighterPhase == HWJ_FighterBossPhase.Transition)
        {
            UpdateTwoBarTransitionAnimation(phaseTransitionElapsed);
        }

        bool isTransitionPatternRunning =
            !useTwoBarPhaseHealth
            && ((patternSystem != null && patternSystem.IsSpecialPatternRunning)
                || (stageOnePatternSystem != null && stageOnePatternSystem.IsPatternRunning));

        bool isPhaseDialogueRunning = useTwoBarPhaseHealth
            && dialogueBubbleSystem != null
            && dialogueBubbleSystem.IsPhaseTransitionSequencePlaying;

        if (phaseTransitionTimer <= 0f
            && !isTransitionPatternRunning
            && !isPhaseDialogueRunning)
        {
            if (useTwoBarPhaseHealth && fighterPhase == HWJ_FighterBossPhase.Transition)
            {
                CompleteTwoBarPhaseTransition();
                return true;
            }

            if (pendingPhaseIndex >= 0)
            {
                currentPhaseIndex = pendingPhaseIndex;
            }

            pendingPhaseIndex = -1;
            SetBossState(HWJ_BossFSMState.Idle);
        }

        return true;
    }

    private void CompleteTwoBarPhaseTransition()
    {
        runtimeStatus?.RefreshCurrentHpFromData(true);
        completedTwoBarTransitionCount++;
        fighterPhase = HWJ_FighterBossPhase.Phase2;
        currentPhaseIndex = pendingPhaseIndex >= 0 ? pendingPhaseIndex : 1;
        pendingPhaseIndex = -1;
        currentPhaseId = "phase_2";
        SetCurrentPhaseNumber(2);
        SetBossState(HWJ_BossFSMState.Idle);
        fighterPhaseTwoPatternSystem?.MarkPhaseTwoStarted();
        fighterAnimatorSystem?.CompletePhaseTransition();
        PlayAnimatorState("P1_Idle");
    }

    private void UpdateTwoBarTransitionAnimation(float elapsedSeconds)
    {
        float duration = Mathf.Max(0.1f, elapsedSeconds + Mathf.Max(0f, phaseTransitionTimer));
        float ratio = Mathf.Clamp01(elapsedSeconds / duration);
        int nextStep = ratio < 0.2f ? 0
            : ratio < 0.42f ? 1
            : ratio < 0.62f ? 2
            : ratio < 0.82f ? 3
            : 4;

        if (transitionAnimationStep == nextStep)
        {
            return;
        }

        transitionAnimationStep = nextStep;

        switch (transitionAnimationStep)
        {
            case 0:
                PlayAnimatorState("PhaseBreak_Down");
                break;
            case 1:
                PlayAnimatorState("PhaseBreak_Prayer");
                break;
            case 2:
                PlayAnimatorState("PhaseBreak_LightningHit");
                break;
            case 3:
                PlayAnimatorState("PhaseBreak_Transform");
                break;
            default:
                PlayAnimatorState("Phase2_Start");
                break;
        }
    }

}
