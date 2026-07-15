using UnityEngine;

public class HWJ_BossBrainSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BossPatternSystem patternSystem;
    [SerializeField] private HWJ_Stage1BossPatternSystem stageOnePatternSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_BossCameraFocusSystem cameraFocusSystem;
    [SerializeField] private HWJ_BossDialogueBubbleSystem dialogueBubbleSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private HWJ_BossFSMState currentState = HWJ_BossFSMState.Inactive;
    [SerializeField] private bool encounterStarted;
    [SerializeField] private int currentPhaseIndex = -1;
    [SerializeField] private int currentPhaseNumber = 1;
    [SerializeField] private string currentPhaseId;
    [SerializeField] private float phaseTransitionTimer;
    [SerializeField] private float groggyTimer;
    [SerializeField] private int groggyHitCount;
    [SerializeField] private float groggyHitWindowTimer;

    private int pendingPhaseIndex = -1;
    private Vector3 roomAnchorPosition;
    private bool phaseTwoTriggered;
    private bool wasTargetSoulState;

    public HWJ_BossFSMState CurrentState => currentState;
    public bool EncounterStarted => encounterStarted;
    public int CurrentPhaseNumber => currentPhaseNumber;
    public Transform Target => target;
    public Vector2 BossRoomCenter => GetBossRoomCenter();
    public Vector2 BossRoomSize => GetBossRoomSize();
    public bool IsGroggy => currentState == HWJ_BossFSMState.Groggy;
    public bool HasSuperArmor => IsSuperArmorActive();
    public float ReceivedDamageMultiplier => IsGroggy && TryGetBossData(out HWJ_BossTypeDataSO bossData)
        ? Mathf.Max(0f, bossData.FSM.groggyDamageMultiplier)
        : 1f;

    private void Awake()
    {
        CacheReferences();
        roomAnchorPosition = transform.position;
        currentPhaseIndex = ResolvePhaseIndex();
        currentPhaseNumber = Mathf.Max(1, currentPhaseIndex + 1);
        currentPhaseId = GetPhaseId(currentPhaseIndex);
    }

    private void Update()
    {
        CacheReferences();

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            SetBossState(HWJ_BossFSMState.Dead);
            StopHorizontalMovement();
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

        if (HandleSoulTargetState(bossData))
        {
            return;
        }

        if (TryEnterPhaseTransition(bossData))
        {
            return;
        }

        if (UpdatePhaseTransition())
        {
            return;
        }

        if (UpdateGroggy())
        {
            return;
        }

        if (stageOnePatternSystem != null && stageOnePatternSystem.IsPatternRunning)
        {
            SetBossState(HWJ_BossFSMState.Attack);
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

        RunCombatLoop(bossData);
    }

    public void StartBossEncounter()
    {
        encounterStarted = true;
        SetBossState(HWJ_BossFSMState.Idle);
        cameraFocusSystem?.FocusOnBoss(transform, target, GetCameraFocusSeconds());
        dialogueBubbleSystem?.ShowIntroDialogue();
    }

    public void StopBossEncounter()
    {
        encounterStarted = false;
        CancelCurrentBossActions();
        SetBossState(HWJ_BossFSMState.Inactive);
        StopHorizontalMovement();
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
        cameraFocusSystem?.FocusOnBoss(transform, target, Mathf.Max(GetCameraFocusSeconds(), phaseTransitionTimer));
    }

    private bool UpdatePhaseTransition()
    {
        if (currentState != HWJ_BossFSMState.PhaseTransition)
        {
            return false;
        }

        StopHorizontalMovement();
        phaseTransitionTimer -= Time.deltaTime;

        bool isTransitionPatternRunning = stageOnePatternSystem != null && stageOnePatternSystem.IsPatternRunning;

        if (phaseTransitionTimer <= 0f && !isTransitionPatternRunning)
        {
            if (pendingPhaseIndex >= 0)
            {
                currentPhaseIndex = pendingPhaseIndex;
            }

            pendingPhaseIndex = -1;
            SetBossState(HWJ_BossFSMState.Idle);
        }

        return true;
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
        stageOnePatternSystem?.CancelActivePattern();
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
