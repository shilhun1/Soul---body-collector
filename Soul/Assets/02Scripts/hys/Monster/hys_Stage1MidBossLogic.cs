using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class hys_Stage1MidBossLogic : MonoBehaviour
{
    // 중간보스의 기본 AI 흐름만 관리합니다.
    // 실제 공격 패턴은 hys_Stage1MidBossPattern 스크립트에서 따로 실행합니다.

    private enum hys_MidBossState
    {
        Inactive,
        Idle,
        Chase,
        Attack,
        PhaseTransition,
        Groggy,
        Dead
    }

    [Header("References")]
    // HWJ 공통 시스템과 패턴 스크립트를 연결해서 상태, 체력, 전투 계산을 같이 사용합니다.
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private hys_Stage1MidBossPattern patternSystem;
    [SerializeField] private Transform target;

    [Header("Encounter")]
    // 플레이어가 보스룸 안으로 들어오면 보스전을 자동으로 시작합니다.
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool autoStartWhenPlayerEntersRoom = true;
    [SerializeField] private Vector2 bossRoomOffset = Vector2.zero;
    [SerializeField] private Vector2 bossRoomSize = new Vector2(24f, 10f);

    [Header("Movement")]
    // 패턴을 쓰지 않는 동안 플레이어와의 거리에 따라 추격하거나 대기합니다.
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 9f;
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Phase")]
    // 페이즈 전환과 강제 패턴 조건은 기본 로직에서 판단하고, 실행은 패턴 스크립트에 맡깁니다.
    [SerializeField] private float phaseTwoHpRatio = 0.5f;
    [SerializeField] private float pattern5HpRatio = 0.3f;

    [Header("Debug")]
    [SerializeField] private hys_MidBossState currentState = hys_MidBossState.Inactive;
    [SerializeField] private int nextPatternNumber = 1;
    [SerializeField] private string lastBossAction;

    private Coroutine groggyRoutine;
    private float nextPatternTime;
    private float nextTargetSearchTime;
    private bool encounterStarted;
    private bool deathHandled;
    private bool pattern5Used;
    private bool phaseTwoStarted;

    public bool EncounterStarted => encounterStarted;
    public bool IsDefeated => runtimeStatus != null && runtimeStatus.IsDead;
    public string LastBossAction => lastBossAction;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        CacheReferences();
        UpdateTarget();

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            HandleBossDeath();
            SetState(hys_MidBossState.Dead);
            StopHorizontalMovement();
            return;
        }

        if (!encounterStarted)
        {
            SetState(hys_MidBossState.Inactive);

            if (ShouldStartEncounter())
            {
                StartEncounter();
            }

            return;
        }

        if (!IsTargetCombatBody())
        {
            // 플레이어가 영혼 상태이면 공격을 멈추고 육체 상태로 돌아올 때까지 대기합니다.
            SetState(hys_MidBossState.Idle);
            StopHorizontalMovement();
            return;
        }

        if (patternSystem != null && patternSystem.IsPatternRunning)
        {
            StopHorizontalMovement();
            return;
        }

        if (TryStartPhaseTwoLaser())
        {
            return;
        }

        if (TryStartPattern5())
        {
            return;
        }

        RunCombatLoop();
    }

    public void StartEncounter()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return;
        }

        ResetEncounterState();
        encounterStarted = true;
        nextPatternTime = Time.time + 0.5f;
        SetState(hys_MidBossState.Idle);
        lastBossAction = "보스전 시작";
    }

    public void StopEncounter()
    {
        encounterStarted = false;
        patternSystem?.CancelCurrentPattern();
        StopGroggyRoutine();
        SetState(hys_MidBossState.Inactive);
        StopHorizontalMovement();
        lastBossAction = "보스전 중지";
    }

    public void ResetEncounterState()
    {
        // 재도전 시 이전 전투의 페이즈와 예약 공격이 남지 않도록 모두 초기화합니다.
        patternSystem?.ResetForEncounter();
        StopGroggyRoutine();
        deathHandled = false;
        pattern5Used = false;
        phaseTwoStarted = false;
        nextPatternNumber = 1;
        nextPatternTime = 0f;
    }

    public void SetPlayerBodyRemainingSeconds(float seconds)
    {
        // 외부 육신 부패 UI나 시스템에서 남은 시간을 넘길 때 패턴 스크립트로 전달합니다.
        patternSystem?.SetPlayerBodyRemainingSeconds(seconds);
    }

    private void RunCombatLoop()
    {
        if (target == null)
        {
            SetState(hys_MidBossState.Idle);
            StopHorizontalMovement();
            return;
        }

        FaceTarget();

        float distance = GetHorizontalDistanceToTarget();

        if (distance <= attackRange && Time.time >= nextPatternTime)
        {
            StartNextNormalPattern();
            return;
        }

        if (distance <= chaseRange && distance > attackRange)
        {
            SetState(hys_MidBossState.Chase);
            MoveTowardTarget();
            return;
        }

        SetState(hys_MidBossState.Idle);
        StopHorizontalMovement();
    }

    private void StartNextNormalPattern()
    {
        if (patternSystem == null)
        {
            return;
        }

        int patternNumber = ResolveNextPatternNumber();
        nextPatternNumber = patternNumber + 1;

        if (nextPatternNumber > 4)
        {
            nextPatternNumber = 1;
        }

        SetState(hys_MidBossState.Attack);
        StopHorizontalMovement();
        lastBossAction = $"패턴 {patternNumber} 시작";

        patternSystem.TryStartPattern(
            patternNumber,
            target,
            GetRoomCenter(),
            GetRoomSize(),
            OnPatternFinished);
    }

    private int ResolveNextPatternNumber()
    {
        if (patternSystem != null && patternSystem.CanUsePattern4(target, GetHpRatio()))
        {
            return 4;
        }

        return Mathf.Clamp(nextPatternNumber, 1, 3);
    }

    private bool TryStartPattern5()
    {
        if (pattern5Used || patternSystem == null || GetHpRatio() > pattern5HpRatio)
        {
            return false;
        }

        pattern5Used = true;
        SetState(hys_MidBossState.Attack);
        StopHorizontalMovement();
        lastBossAction = "패턴 5 시작";

        return patternSystem.TryStartPattern5(
            target,
            GetRoomCenter(),
            GetRoomSize(),
            OnGroggyStarted,
            OnPatternFinished);
    }

    private bool TryStartPhaseTwoLaser()
    {
        if (phaseTwoStarted || patternSystem == null || GetHpRatio() > phaseTwoHpRatio)
        {
            return false;
        }

        phaseTwoStarted = true;
        SetState(hys_MidBossState.PhaseTransition);
        StopHorizontalMovement();
        lastBossAction = "페이즈 2 전환 레이저";

        return patternSystem.TryStartPhaseTwoLaser(
            target,
            GetRoomCenter(),
            GetRoomSize(),
            OnPatternFinished);
    }

    private void OnPatternFinished()
    {
        nextPatternTime = Time.time + (patternSystem != null ? patternSystem.PatternCooldownSeconds : 1f);
        SetState(hys_MidBossState.Idle);
    }

    private void OnGroggyStarted(float seconds)
    {
        StopGroggyRoutine();
        groggyRoutine = StartCoroutine(GroggyRoutine(seconds));
    }

    private IEnumerator GroggyRoutine(float seconds)
    {
        SetState(hys_MidBossState.Groggy);
        StopHorizontalMovement();
        yield return new WaitForSeconds(Mathf.Max(0.01f, seconds));
        groggyRoutine = null;
    }

    private void StopGroggyRoutine()
    {
        if (groggyRoutine != null)
        {
            StopCoroutine(groggyRoutine);
            groggyRoutine = null;
        }
    }

    private bool ShouldStartEncounter()
    {
        if (!autoStartWhenPlayerEntersRoom || target == null)
        {
            return false;
        }

        Vector2 center = GetRoomCenter();
        Vector2 halfSize = GetRoomSize() * 0.5f;
        Vector2 targetPosition = target.position;

        return Mathf.Abs(targetPosition.x - center.x) <= halfSize.x
            && Mathf.Abs(targetPosition.y - center.y) <= halfSize.y;
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

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (patternSystem == null)
        {
            patternSystem = GetComponent<hys_Stage1MidBossPattern>();
        }
    }

    private void UpdateTarget()
    {
        if (target != null || !autoFindPlayerTarget || Time.time < nextTargetSearchTime)
        {
            return;
        }

        nextTargetSearchTime = Time.time + 0.4f;

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            target = HWJ_GameAccess.Manager.PlayerResolver.transform;
            return;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                target = resolvers[i].transform;
                return;
            }
        }
    }

    private bool IsTargetCombatBody()
    {
        if (target == null)
        {
            return false;
        }

        HWJ_SoulSystem soulSystem = target.GetComponent<HWJ_SoulSystem>();

        if (soulSystem == null)
        {
            soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private void HandleBossDeath()
    {
        if (deathHandled)
        {
            return;
        }

        // 사망 이후 남은 시간차 공격과 분신, 소환물을 즉시 정리합니다.
        deathHandled = true;
        encounterStarted = false;
        patternSystem?.CancelCurrentPattern();
        StopGroggyRoutine();
        lastBossAction = "보스 처치";
    }

    private void OnDisable()
    {
        patternSystem?.CancelCurrentPattern();
        StopGroggyRoutine();
        StopHorizontalMovement();
    }

    private void MoveTowardTarget()
    {
        if (target == null)
        {
            StopHorizontalMovement();
            return;
        }

        float direction = Mathf.Sign(target.position.x - transform.position.x);
        SetHorizontalVelocity(direction * Mathf.Max(0f, moveSpeed));
        SetRuntimeState(HWJ_RuntimeState.Move);
    }

    private void StopHorizontalMovement()
    {
        SetHorizontalVelocity(0f);
    }

    private void SetHorizontalVelocity(float velocityX)
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }

    private void FaceTarget()
    {
        if (target == null)
        {
            return;
        }

        float direction = Mathf.Sign(target.position.x - transform.position.x);

        if (direction == 0f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void SetState(hys_MidBossState state)
    {
        currentState = state;

        switch (state)
        {
            case hys_MidBossState.Chase:
                SetRuntimeState(HWJ_RuntimeState.Move);
                break;
            case hys_MidBossState.Attack:
            case hys_MidBossState.PhaseTransition:
                SetRuntimeState(HWJ_RuntimeState.Attack);
                break;
            case hys_MidBossState.Groggy:
                SetRuntimeState(HWJ_RuntimeState.Hit);
                break;
            case hys_MidBossState.Dead:
                SetRuntimeState(HWJ_RuntimeState.Dead);
                break;
            default:
                SetRuntimeState(HWJ_RuntimeState.Idle);
                break;
        }
    }

    private void SetRuntimeState(HWJ_RuntimeState state)
    {
        if (runtimeStatus != null && !runtimeStatus.IsDead)
        {
            runtimeStatus.SetState(state);
        }
    }

    private float GetHorizontalDistanceToTarget()
    {
        return target != null ? Mathf.Abs(target.position.x - transform.position.x) : float.PositiveInfinity;
    }

    private float GetHpRatio()
    {
        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp);
    }

    private Vector2 GetRoomCenter()
    {
        return (Vector2)transform.position + bossRoomOffset;
    }

    private Vector2 GetRoomSize()
    {
        return new Vector2(Mathf.Max(1f, bossRoomSize.x), Mathf.Max(1f, bossRoomSize.y));
    }
}
