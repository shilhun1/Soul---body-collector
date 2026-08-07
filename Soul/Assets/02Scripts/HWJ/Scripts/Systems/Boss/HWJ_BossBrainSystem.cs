using UnityEngine;

public partial class HWJ_BossBrainSystem : MonoBehaviour, HWJ_IHealthDepletionHandler
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

}
