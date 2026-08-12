using UnityEngine;

/// <summary>
/// Cancels attacks, handles final death, and synchronizes animator/FSM state.
/// This partial belongs to the single HWJ_BossBrainSystem component.
/// </summary>
public partial class HWJ_BossBrainSystem
{
    private void CancelCurrentBossActions()
    {
        skillActionSystem?.CancelCurrentAction();
        patternSystem?.CancelActiveSpecialPatterns();
        stageOnePatternSystem?.CancelActivePattern();
        fighterComboSystem?.CancelActivePattern();
        fighterChargeSystem?.CancelActivePattern();
        fighterUppercutSystem?.CancelActivePattern();
        fighterGroundSlamSystem?.CancelActivePattern();
        fighterPhaseTwoPatternSystem?.CancelActivePattern();

        HWJ_FighterBossHitboxSystem[] hitboxes =
            GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i]?.Disarm();
        }
    }

    private bool IsFighterPatternRecovering()
    {
        return fighterComboSystem != null && fighterComboSystem.IsRecovering
            || fighterChargeSystem != null && fighterChargeSystem.IsRecovering
            || fighterUppercutSystem != null && fighterUppercutSystem.IsRecovering
            || fighterGroundSlamSystem != null && fighterGroundSlamSystem.IsRecovering
            || fighterPhaseTwoPatternSystem != null && fighterPhaseTwoPatternSystem.IsRecovering;
    }

    private void EnterFinalDeathState()
    {
        fighterPhase = useTwoBarPhaseHealth
            ? HWJ_FighterBossPhase.Dead
            : fighterPhase;

        if (!finalDeathCleanupCompleted)
        {
            finalDeathCleanupCompleted = true;
            CancelCurrentBossActions();
            dialogueBubbleSystem?.ShowDeathDialogue();
            FocusCameraForDialogue(HWJ_BossDialogueSequenceType.Death, GetCameraFocusSeconds());

            if (!useTwoBarPhaseHealth
                || fighterDeathSystem == null
                || !fighterDeathSystem.BeginFinalDeath())
            {
                PlayAnimatorState("P2_Death");
            }
        }

        SetBossState(HWJ_BossFSMState.Dead);
        StopHorizontalMovement();
    }

    private void PlayAnimatorState(string stateName)
    {
        if (useTwoBarPhaseHealth
            && fighterAnimatorSystem != null
            && fighterAnimatorSystem.PlayState(stateName, 0f))
        {
            return;
        }

        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash($"Base Layer.{stateName}");

        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }
    }

    public void HandleAnimationPhaseStep(int step)
    {
        if (fighterPhase != HWJ_FighterBossPhase.Transition)
        {
            return;
        }

        transitionAnimationStep = Mathf.Clamp(step, 0, 4);

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

    private HWJ_FighterBossState ResolveFighterBossState()
    {
        switch (currentState)
        {
            case HWJ_BossFSMState.Chase:
                return HWJ_FighterBossState.Chase;
            case HWJ_BossFSMState.Attack:
                return HWJ_FighterBossState.Attack;
            case HWJ_BossFSMState.Recover:
                return HWJ_FighterBossState.Recover;
            case HWJ_BossFSMState.Groggy:
            case HWJ_BossFSMState.Stagger:
                return HWJ_FighterBossState.Stagger;
            case HWJ_BossFSMState.PhaseTransition:
                return HWJ_FighterBossState.PhaseTransition;
            case HWJ_BossFSMState.Dead:
                return HWJ_FighterBossState.Death;
            case HWJ_BossFSMState.Inactive:
                return encounterStarted ? HWJ_FighterBossState.Idle : HWJ_FighterBossState.Intro;
            default:
                return HWJ_FighterBossState.Idle;
        }
    }

    private bool IsTargetSoulState()
    {
        if (target == null)
        {
            return false;
        }

        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul != null
            && (targetSoul.CurrentState == HWJ_SoulRuntimeState.Soul
                || targetSoul.CurrentState == HWJ_SoulRuntimeState.BodyToSoul);
    }

    private bool IsTargetDead()
    {
        if (target == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();

        if (targetStatus == null)
        {
            targetStatus = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        return targetStatus != null && targetStatus.IsDead;
    }

    private float GetCameraFocusSeconds()
    {
        return TryGetBossData(out HWJ_BossTypeDataSO bossData)
            ? Mathf.Max(0f, bossData.FSM.cameraFocusSeconds)
            : 0f;
    }

    private void FocusCameraForDialogue(
        HWJ_BossDialogueSequenceType sequenceType,
        float minimumDurationSeconds)
    {
        float dialogueDuration = dialogueBubbleSystem != null
            ? dialogueBubbleSystem.GetSequenceDuration(sequenceType)
            : 0f;
        cameraFocusSystem?.FocusOnBoss(
            transform,
            target,
            Mathf.Max(minimumDurationSeconds, dialogueDuration));
    }

    private void SetBossState(HWJ_BossFSMState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;

        if (runtimeStatus == null)
        {
            return;
        }

        runtimeStatus.SetState(HWJ_FSMStateUtility.ToRuntimeState(currentState));
    }

    private void SetCurrentPhaseNumber(int nextPhaseNumber)
    {
        nextPhaseNumber = Mathf.Max(1, nextPhaseNumber);

        if (currentPhaseNumber == nextPhaseNumber)
        {
            return;
        }

        int previousPhaseNumber = currentPhaseNumber;
        currentPhaseNumber = nextPhaseNumber;
        HWJ_GameplayEvents.RaiseBossPhaseChanged(
            new HWJ_BossPhaseChangedEvent(this, previousPhaseNumber, currentPhaseNumber));
    }

}
