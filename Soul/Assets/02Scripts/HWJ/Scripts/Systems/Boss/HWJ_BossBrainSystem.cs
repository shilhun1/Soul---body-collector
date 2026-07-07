using UnityEngine;

public class HWJ_BossBrainSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BossPatternSystem patternSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private HWJ_BossFSMState currentState = HWJ_BossFSMState.Inactive;
    [SerializeField] private bool encounterStarted;
    [SerializeField] private int currentPhaseIndex = -1;
    [SerializeField] private string currentPhaseId;
    [SerializeField] private float phaseTransitionTimer;
    [SerializeField] private float groggyTimer;
    [SerializeField] private int groggyHitCount;
    [SerializeField] private float groggyHitWindowTimer;

    private int pendingPhaseIndex = -1;

    public HWJ_BossFSMState CurrentState => currentState;
    public bool EncounterStarted => encounterStarted;
    public bool IsGroggy => currentState == HWJ_BossFSMState.Groggy;
    public bool HasSuperArmor => IsSuperArmorActive();
    public float ReceivedDamageMultiplier => IsGroggy && TryGetBossData(out HWJ_BossTypeDataSO bossData)
        ? Mathf.Max(0f, bossData.FSM.groggyDamageMultiplier)
        : 1f;

    private void Awake()
    {
        CacheReferences();
        currentPhaseIndex = ResolvePhaseIndex();
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

        if (!encounterStarted)
        {
            SetBossState(HWJ_BossFSMState.Inactive);

            if (ShouldStartEncounter(bossData))
            {
                StartBossEncounter();
            }

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
    }

    public void StopBossEncounter()
    {
        encounterStarted = false;
        SetBossState(HWJ_BossFSMState.Inactive);
        StopHorizontalMovement();
    }

    public void NotifyDamageTaken(float damage)
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

        if (distanceX <= attackRange && patternSystem != null && patternSystem.TryUseAvailablePattern(target))
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

    private bool TryEnterPhaseTransition(HWJ_BossTypeDataSO bossData)
    {
        int nextPhaseIndex = ResolvePhaseIndex();

        if (nextPhaseIndex < 0 || nextPhaseIndex == currentPhaseIndex)
        {
            return false;
        }

        currentPhaseIndex = nextPhaseIndex;
        currentPhaseId = GetPhaseId(currentPhaseIndex);
        pendingPhaseIndex = nextPhaseIndex;
        phaseTransitionTimer = Mathf.Max(0f, bossData.FSM.phaseTransitionSeconds);
        SetBossState(HWJ_BossFSMState.PhaseTransition);
        StopHorizontalMovement();
        patternSystem?.TryUsePhaseChangedPattern(target);
        return phaseTransitionTimer > 0f;
    }

    private bool UpdatePhaseTransition()
    {
        if (currentState != HWJ_BossFSMState.PhaseTransition)
        {
            return false;
        }

        StopHorizontalMovement();
        phaseTransitionTimer -= Time.deltaTime;

        if (phaseTransitionTimer <= 0f)
        {
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
        SetBossState(HWJ_BossFSMState.Groggy);
        StopHorizontalMovement();
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

        switch (currentState)
        {
            case HWJ_BossFSMState.Chase:
                runtimeStatus.SetState(HWJ_RuntimeState.Move);
                break;
            case HWJ_BossFSMState.Attack:
                runtimeStatus.SetState(HWJ_RuntimeState.Attack);
                break;
            case HWJ_BossFSMState.PhaseTransition:
            case HWJ_BossFSMState.Groggy:
                runtimeStatus.SetState(HWJ_RuntimeState.Hit);
                break;
            case HWJ_BossFSMState.Dead:
                runtimeStatus.SetState(HWJ_RuntimeState.Dead);
                break;
            default:
                runtimeStatus.SetState(HWJ_RuntimeState.Idle);
                break;
        }
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

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
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
