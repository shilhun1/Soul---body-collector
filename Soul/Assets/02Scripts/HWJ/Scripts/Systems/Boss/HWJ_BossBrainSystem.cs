using UnityEngine;

public class HWJ_BossBrainSystem : MonoBehaviour, HWJ_IHealthDepletionHandler
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BossPatternSystem patternSystem;
    [SerializeField] private HWJ_Stage1BossPatternSystem stageOnePatternSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_BossCameraFocusSystem cameraFocusSystem;
    [SerializeField] private HWJ_BossDialogueBubbleSystem dialogueBubbleSystem;
    [SerializeField] private HWJ_FighterBossComboSystem fighterComboSystem;
    [SerializeField] private HWJ_FighterBossChargeSystem fighterChargeSystem;
    [SerializeField] private HWJ_FighterBossUppercutSystem fighterUppercutSystem;
    [SerializeField] private HWJ_FighterBossGroundSlamSystem fighterGroundSlamSystem;
    [SerializeField] private HWJ_FighterBossPhaseTwoPatternSystem fighterPhaseTwoPatternSystem;
    [SerializeField] private HWJ_FighterBossDeathSystem fighterDeathSystem;
    [SerializeField] private HWJ_FighterBossAnimatorSystem fighterAnimatorSystem;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool aiEnabled = true;
    [SerializeField] private HWJ_BossFSMState currentState = HWJ_BossFSMState.Inactive;
    [SerializeField] private bool encounterStarted;
    [SerializeField] private int currentPhaseIndex = -1;
    [SerializeField] private int currentPhaseNumber = 1;
    [SerializeField] private string currentPhaseId;
    [SerializeField] private float phaseTransitionTimer;
    [SerializeField] private float groggyTimer;
    [SerializeField] private int groggyHitCount;
    [SerializeField] private float groggyHitWindowTimer;
    [Header("Fighter Boss Two-Bar Phase")]
    [Tooltip("Phase1 HP 0을 사망 대신 전환으로 처리하고, 전환 종료 시 HP를 완전히 회복합니다.")]
    [SerializeField] private bool useTwoBarPhaseHealth;
    [Tooltip("격투가 보스의 현재 두 줄 체력 페이즈입니다.")]
    [SerializeField] private HWJ_FighterBossPhase fighterPhase = HWJ_FighterBossPhase.Phase1;

    private int pendingPhaseIndex = -1;
    private Vector3 roomAnchorPosition;
    private bool phaseTwoTriggered;
    private bool wasTargetSoulState;
    private float phaseTransitionElapsed;
    private int transitionAnimationStep = -1;
    private int completedTwoBarTransitionCount;
    private bool finalDeathCleanupCompleted;

    public HWJ_BossFSMState CurrentState => currentState;
    public bool EncounterStarted => encounterStarted;
    public int CurrentPhaseNumber => currentPhaseNumber;
    public Transform Target => target;
    public Vector2 BossRoomCenter => GetBossRoomCenter();
    public Vector2 BossRoomSize => GetBossRoomSize();
    public bool IsGroggy => currentState == HWJ_BossFSMState.Groggy;
    public bool HasSuperArmor => IsSuperArmorActive();
    public bool UsesTwoBarPhaseHealth => useTwoBarPhaseHealth;
    public HWJ_FighterBossPhase FighterPhase => fighterPhase;
    public HWJ_FighterBossState FighterState => ResolveFighterBossState();
    public int TransitionAnimationStep => transitionAnimationStep;
    public int CompletedTwoBarTransitionCount => completedTwoBarTransitionCount;
    public bool AIEnabled => aiEnabled;
    public bool CanUsePhaseOneCombo => !useTwoBarPhaseHealth
        || fighterPhase == HWJ_FighterBossPhase.Phase1;
    public bool CanUsePhaseTwoPatterns => !useTwoBarPhaseHealth
        || fighterPhase == HWJ_FighterBossPhase.Phase2;
    public bool IsHealthDepletionHandled => useTwoBarPhaseHealth
        && fighterPhase == HWJ_FighterBossPhase.Transition;
    public float ReceivedDamageMultiplier => IsGroggy && TryGetBossData(out HWJ_BossTypeDataSO bossData)
        ? Mathf.Max(0f, bossData.FSM.groggyDamageMultiplier)
        : 1f;

    public void SetAIEnabled(bool enabled)
    {
        aiEnabled = enabled;

        if (!aiEnabled)
        {
            CancelCurrentBossActions();
            StopHorizontalMovement();
            SetBossState(HWJ_BossFSMState.Idle);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public bool ForcePhaseTransition()
    {
        CacheReferences();

        if (!useTwoBarPhaseHealth || fighterPhase != HWJ_FighterBossPhase.Phase1)
        {
            return false;
        }

        StartTwoBarPhaseTransition();
        return fighterPhase == HWJ_FighterBossPhase.Transition;
    }

    public void ForcePhaseTwo()
    {
        CacheReferences();
        CancelCurrentBossActions();
        useTwoBarPhaseHealth = true;
        fighterPhase = HWJ_FighterBossPhase.Phase2;
        phaseTwoTriggered = true;
        encounterStarted = true;
        currentPhaseIndex = 1;
        pendingPhaseIndex = -1;
        currentPhaseId = "phase_2";
        phaseTransitionTimer = 0f;
        transitionAnimationStep = 4;
        fighterDeathSystem?.ResetDeathState();
        runtimeStatus?.ResetForEncounter(true);
        SetCurrentPhaseNumber(2);
        SetBossState(HWJ_BossFSMState.Idle);
        fighterPhaseTwoPatternSystem?.MarkPhaseTwoStarted();
        fighterAnimatorSystem?.SetPhase(2);
        fighterAnimatorSystem?.PlayState("P1_Idle", 0f);
    }

    public void ForcePhaseOne()
    {
        CacheReferences();
        CancelCurrentBossActions();
        useTwoBarPhaseHealth = true;
        fighterPhase = HWJ_FighterBossPhase.Phase1;
        phaseTwoTriggered = false;
        finalDeathCleanupCompleted = false;
        encounterStarted = true;
        currentPhaseIndex = 0;
        pendingPhaseIndex = -1;
        currentPhaseId = "phase_1";
        phaseTransitionTimer = 0f;
        transitionAnimationStep = -1;
        fighterDeathSystem?.ResetDeathState();
        runtimeStatus?.ResetForEncounter(true);
        SetCurrentPhaseNumber(1);
        SetBossState(HWJ_BossFSMState.Idle);
        patternSystem?.ResetPatternHistory();
        fighterAnimatorSystem?.ResetAnimatorState();
    }

    public void ForceFinalDeath()
    {
        CacheReferences();
        fighterPhase = HWJ_FighterBossPhase.Dead;
        EnterFinalDeathState();
    }

    private void Awake()
    {
        CacheReferences();
        roomAnchorPosition = transform.position;
        currentPhaseIndex = ResolvePhaseIndex();
        currentPhaseNumber = Mathf.Max(1, currentPhaseIndex + 1);
        currentPhaseId = GetPhaseId(currentPhaseIndex);

        if (useTwoBarPhaseHealth && fighterPhase != HWJ_FighterBossPhase.Dead)
        {
            fighterPhase = currentPhaseNumber >= 2
                ? HWJ_FighterBossPhase.Phase2
                : HWJ_FighterBossPhase.Phase1;
        }
    }

    private void OnDisable()
    {
        CancelCurrentBossActions();
        StopHorizontalMovement();
    }

    private void Update()
    {
        CacheReferences();

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            EnterFinalDeathState();
            return;
        }

        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            SetBossState(HWJ_BossFSMState.Inactive);
            StopHorizontalMovement();
            return;
        }

        if (target == null && autoFindPlayerTarget)
        {
            target = FindPlayerTarget();
        }

        if (UpdatePhaseTransition())
        {
            return;
        }

        if (encounterStarted && IsTargetDead())
        {
            StopBossEncounter();
            return;
        }

        if (!encounterStarted)
        {
            SetBossState(HWJ_BossFSMState.Inactive);

            if (ShouldStartEncounter(bossData))
            {
                StartBossEncounter();
            }

            return;
        }

        // 시작 대사가 끝나기 전에는 보스가 이동하거나 패턴을 시작하지 않습니다.
        if (dialogueBubbleSystem != null && dialogueBubbleSystem.BlocksBossActions)
        {
            SetBossState(HWJ_BossFSMState.Idle);
            StopHorizontalMovement();
            return;
        }

        if (HandleSoulTargetState(bossData))
        {
            return;
        }

        if (TryEnterPhaseTransition(bossData))
        {
            return;
        }

        if (UpdateGroggy())
        {
            return;
        }

        if ((patternSystem != null && patternSystem.IsSpecialPatternRunning)
            || (stageOnePatternSystem != null && stageOnePatternSystem.IsPatternRunning))
        {
            SetBossState(IsFighterPatternRecovering()
                ? HWJ_BossFSMState.Recover
                : HWJ_BossFSMState.Attack);
            FaceTarget();
            StopHorizontalMovement();
            return;
        }

        if (skillActionSystem != null && skillActionSystem.IsNavigationBlocked)
        {
            SetBossState(HWJ_BossFSMState.Attack);
            FaceTarget();

            if (skillActionSystem.ShouldStopNavigationMovement)
            {
                StopHorizontalMovement();
            }

            return;
        }

        if (!aiEnabled)
        {
            SetBossState(HWJ_BossFSMState.Idle);
            StopHorizontalMovement();
            return;
        }

        RunCombatLoop(bossData);
    }

    public void StartBossEncounter()
    {
        encounterStarted = true;
        SetBossState(HWJ_BossFSMState.Idle);
        dialogueBubbleSystem?.ShowIntroDialogue();
        FocusCameraForDialogue(HWJ_BossDialogueSequenceType.Intro, GetCameraFocusSeconds());
    }

    public void StopBossEncounter()
    {
        encounterStarted = false;
        CancelCurrentBossActions();
        dialogueBubbleSystem?.CancelDialogueSequence(true);
        cameraFocusSystem?.StopFocus();
        SetBossState(HWJ_BossFSMState.Inactive);
        StopHorizontalMovement();
    }

    /// <summary>
    /// RuntimeStatusSystem이 HP 0을 감지했을 때 호출합니다.
    /// 1페이즈에서는 사망을 보류하고 전환을 시작하며, 2페이즈에서는 기존 최종 사망 처리를 계속합니다.
    /// </summary>
    public bool TryHandleHealthDepleted(HWJ_RuntimeStatusSystem status)
    {
        if (!useTwoBarPhaseHealth || status == null || status != runtimeStatus)
        {
            return false;
        }

        if (fighterPhase == HWJ_FighterBossPhase.Phase1)
        {
            StartTwoBarPhaseTransition();
            return true;
        }

        if (fighterPhase == HWJ_FighterBossPhase.Transition)
        {
            return true;
        }

        if (fighterPhase == HWJ_FighterBossPhase.Phase2)
        {
            fighterPhase = HWJ_FighterBossPhase.Dead;
            EnterFinalDeathState();
        }

        return false;
    }

    public void NotifyComboAttackStarted()
    {
        if (CanUsePhaseOneCombo && fighterPhase != HWJ_FighterBossPhase.Dead)
        {
            SetBossState(HWJ_BossFSMState.Attack);
            StopHorizontalMovement();
        }
    }

    public void NotifyComboRecoveryStarted()
    {
        if (CanUsePhaseOneCombo && fighterPhase != HWJ_FighterBossPhase.Dead)
        {
            SetBossState(HWJ_BossFSMState.Recover);
            StopHorizontalMovement();
        }
    }

    public void NotifyComboAttackEnded()
    {
        if (fighterPhase == HWJ_FighterBossPhase.Phase1)
        {
            SetBossState(HWJ_BossFSMState.Idle);
        }
    }

    /// <summary>
    /// Phase-two custom executors use these notifications to share the boss FSM contract.
    /// </summary>
    public void NotifyFighterPatternAttackStarted()
    {
        if (fighterPhase == HWJ_FighterBossPhase.Transition
            || fighterPhase == HWJ_FighterBossPhase.Dead)
        {
            return;
        }

        SetBossState(HWJ_BossFSMState.Attack);
        StopHorizontalMovement();
    }

    public void NotifyFighterPatternRecoveryStarted()
    {
        if (fighterPhase == HWJ_FighterBossPhase.Transition
            || fighterPhase == HWJ_FighterBossPhase.Dead)
        {
            return;
        }

        SetBossState(HWJ_BossFSMState.Recover);
        StopHorizontalMovement();
    }

    public void NotifyFighterPatternAttackEnded()
    {
        if (fighterPhase == HWJ_FighterBossPhase.Phase1
            || fighterPhase == HWJ_FighterBossPhase.Phase2)
        {
            SetBossState(HWJ_BossFSMState.Idle);
        }
    }

    public void ForceGroggy(float durationSeconds)
    {
        groggyHitCount = 0;
        groggyHitWindowTimer = 0f;
        groggyTimer = Mathf.Max(0.01f, durationSeconds);
        ApplyGroggyEntryEffects(TryGetBossData(out HWJ_BossTypeDataSO bossData) ? bossData.FSM : null);
        SetBossState(HWJ_BossFSMState.Groggy);
        StopHorizontalMovement();
    }

    public void NotifyDamageTaken(float damage)
    {
        NotifyDamageTaken(damage, false, false);
    }

    public void NotifyDamageTaken(
        float damage,
        bool hitReactionBlockedBySuperArmor,
        bool hitReactionBlockedByLimit)
    {
        if (damage <= 0f || !TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return;
        }

        HWJ_BossFSMData fsm = bossData.FSM;

        if (currentState == HWJ_BossFSMState.Groggy || currentState == HWJ_BossFSMState.PhaseTransition)
        {
            return;
        }

        if (hitReactionBlockedBySuperArmor && !fsm.countGroggyHitsDuringSuperArmor)
        {
            return;
        }

        if (hitReactionBlockedByLimit && !fsm.countGroggyHitsWhileHitReactionLimited)
        {
            return;
        }

        if (fsm.groggyHitCountThreshold <= 0)
        {
            return;
        }

        if (groggyHitWindowTimer <= 0f)
        {
            groggyHitCount = 0;
        }

        groggyHitCount++;
        groggyHitWindowTimer = Mathf.Max(0.01f, fsm.groggyHitWindowSeconds);

        if (groggyHitCount >= fsm.groggyHitCountThreshold)
        {
            EnterGroggy(fsm);
        }
    }

    private void RunCombatLoop(HWJ_BossTypeDataSO bossData)
    {
        // 이번 1차 구현은 P1 Combo만 활성화합니다. Phase2 공격은 다음 검증 단계 전까지 시작하지 않습니다.
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

    private void EnterGroggy(HWJ_BossFSMData fsm)
    {
        groggyHitCount = 0;
        groggyHitWindowTimer = 0f;
        groggyTimer = Mathf.Max(0.01f, fsm.groggyDurationSeconds);
        ApplyGroggyEntryEffects(fsm);
        SetBossState(HWJ_BossFSMState.Groggy);
        StopHorizontalMovement();
    }

    private void ApplyGroggyEntryEffects(HWJ_BossFSMData fsm)
    {
        if (fsm == null || fsm.cancelActionsOnGroggy)
        {
            CancelCurrentBossActions();
        }

        if (fsm == null || fsm.clearHitReactionLimitOnGroggy)
        {
            runtimeStatus?.ClearHitReactionLimit();
        }
    }

    private bool UpdateGroggy()
    {
        if (groggyHitWindowTimer > 0f)
        {
            groggyHitWindowTimer = Mathf.Max(0f, groggyHitWindowTimer - Time.deltaTime);

            if (groggyHitWindowTimer <= 0f)
            {
                groggyHitCount = 0;
            }
        }

        if (currentState != HWJ_BossFSMState.Groggy)
        {
            return false;
        }

        StopHorizontalMovement();
        groggyTimer -= Time.deltaTime;

        if (groggyTimer <= 0f)
        {
            if (TryGetBossData(out HWJ_BossTypeDataSO bossData) && bossData.FSM.knockbackOnGroggyEnd)
            {
                KnockbackTarget(bossData.FSM.groggyEndKnockbackPower);
            }

            SetBossState(HWJ_BossFSMState.Idle);
        }

        return true;
    }

    private int ResolvePhaseIndex()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData)
            || bossData.Phases == null
            || bossData.Phases.Length == 0
            || runtimeStatus == null
            || runtimeStatus.MaxHp <= 0f)
        {
            return -1;
        }

        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;
        int bestIndex = -1;
        float bestThreshold = float.MaxValue;

        for (int i = 0; i < bossData.Phases.Length; i++)
        {
            HWJ_BossPhaseData phase = bossData.Phases[i];

            if (phase == null || hpRatio > phase.startHpRatio)
            {
                continue;
            }

            if (phase.startHpRatio < bestThreshold)
            {
                bestThreshold = phase.startHpRatio;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private string GetPhaseId(int phaseIndex)
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData)
            || bossData.Phases == null
            || phaseIndex < 0
            || phaseIndex >= bossData.Phases.Length
            || bossData.Phases[phaseIndex] == null)
        {
            return null;
        }

        return bossData.Phases[phaseIndex].phaseId;
    }

    private bool IsSuperArmorActive()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return false;
        }

        return (currentState == HWJ_BossFSMState.Attack && bossData.FSM.superArmorDuringAttack)
            || (currentState == HWJ_BossFSMState.PhaseTransition && bossData.FSM.superArmorDuringPhaseTransition);
    }

    private float GetAttackRange(HWJ_BossTypeDataSO bossData)
    {
        if (bossData.FSM.attackStartRange > 0f)
        {
            return bossData.FSM.attackStartRange;
        }

        return Mathf.Max(1f, bossData.Navigation.stoppingDistance);
    }

    private float GetOptimalDistance(HWJ_BossTypeDataSO bossData)
    {
        if (bossData.FSM.optimalAttackDistance > 0f)
        {
            return bossData.FSM.optimalAttackDistance;
        }

        return GetAttackRange(bossData);
    }

    private void MoveTowardTarget(HWJ_BossTypeDataSO bossData)
    {
        if (target == null)
        {
            StopHorizontalMovement();
            return;
        }

        float directionX = Mathf.Sign(target.position.x - transform.position.x);
        float moveSpeed = runtimeStatus != null
            ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        moveSpeed *= Mathf.Max(0f, bossData.FSM.chaseMoveSpeedMultiplier);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return;
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
    }

    private void MoveTowardPosition(Vector2 destination, HWJ_BossTypeDataSO bossData, float stoppingDistance)
    {
        float distanceX = Mathf.Abs(destination.x - transform.position.x);

        if (distanceX <= Mathf.Max(0.01f, stoppingDistance))
        {
            StopHorizontalMovement();
            return;
        }

        float directionX = Mathf.Sign(destination.x - transform.position.x);
        float moveSpeed = runtimeStatus != null
            ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        moveSpeed *= Mathf.Max(0f, bossData.FSM.chaseMoveSpeedMultiplier);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return;
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
    }

    private void TeleportToRoomCenter()
    {
        Vector2 center = GetBossRoomCenter();
        Vector3 targetPosition = new Vector3(center.x, center.y, transform.position.z);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = (Vector2)targetPosition;
        }

        transform.position = targetPosition;
    }

    private Vector2 GetBossRoomCenter()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return roomAnchorPosition;
        }

        return (Vector2)roomAnchorPosition + bossData.FSM.bossRoomOffset;
    }

    private Vector2 GetBossRoomSize()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return new Vector2(28f, 14f);
        }

        return bossData.FSM.bossRoomSize;
    }

    private void FaceTarget()
    {
        if (target == null || motionSystem == null)
        {
            return;
        }

        motionSystem.FaceDirection(target.position.x - transform.position.x);
    }

    private void StopHorizontalMovement()
    {
        if (body == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = 0f;
        body.linearVelocity = velocity;
    }

    private void KnockbackTarget(float power)
    {
        if (target == null || power <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = target.GetComponent<HWJ_KnockbackSystem>();

        if (knockbackSystem == null)
        {
            knockbackSystem = target.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = target.position - transform.position;
        direction.y = 0f;
        knockbackSystem.PlayKnockback(direction, power, 0.25f);
    }

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

            if (fighterDeathSystem == null || !fighterDeathSystem.BeginFinalDeath())
            {
                PlayAnimatorState("P2_Death");
            }
        }

        SetBossState(HWJ_BossFSMState.Dead);
        StopHorizontalMovement();
    }

    private void PlayAnimatorState(string stateName)
    {
        if (fighterAnimatorSystem != null
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

    private bool TryGetBossData(out HWJ_BossTypeDataSO bossData)
    {
        bossData = null;
        return dataResolver != null && dataResolver.TryGetTypeData(out bossData);
    }

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (patternSystem == null)
        {
            patternSystem = GetComponent<HWJ_BossPatternSystem>();
        }

        if (stageOnePatternSystem == null)
        {
            stageOnePatternSystem = GetComponent<HWJ_Stage1BossPatternSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (cameraFocusSystem == null)
        {
            cameraFocusSystem = GetComponent<HWJ_BossCameraFocusSystem>();
        }

        if (dialogueBubbleSystem == null)
        {
            dialogueBubbleSystem = GetComponent<HWJ_BossDialogueBubbleSystem>();
        }

        if (fighterComboSystem == null)
        {
            fighterComboSystem = GetComponent<HWJ_FighterBossComboSystem>();
        }

        if (fighterChargeSystem == null)
        {
            fighterChargeSystem = GetComponent<HWJ_FighterBossChargeSystem>();
        }

        if (fighterUppercutSystem == null)
        {
            fighterUppercutSystem = GetComponent<HWJ_FighterBossUppercutSystem>();
        }

        if (fighterGroundSlamSystem == null)
        {
            fighterGroundSlamSystem = GetComponent<HWJ_FighterBossGroundSlamSystem>();
        }

        if (fighterPhaseTwoPatternSystem == null)
        {
            fighterPhaseTwoPatternSystem = GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        }

        if (fighterDeathSystem == null)
        {
            fighterDeathSystem = GetComponent<HWJ_FighterBossDeathSystem>();
        }

        if (fighterAnimatorSystem == null)
        {
            fighterAnimatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        CacheReferences();

        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData) || !bossData.FSM.useBossRoomBounds)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Vector3 center = transform.position + (Vector3)bossData.FSM.bossRoomOffset;
        Gizmos.DrawWireCube(center, bossData.FSM.bossRoomSize);
    }
}
