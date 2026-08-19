using UnityEngine;

/// <summary>
/// 최종보스의 전투 진입, 거리 조절, 패턴 선택과 공통 상태를 관리합니다.
/// </summary>
public class hys_FinalBossLogic : MonoBehaviour
{
    private enum hys_FinalBossState { Inactive, Idle, Chase, Attack, PhaseTransition, Wait, Groggy, Dead }

    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private hys_FinalBossPattern patternSystem;
    [SerializeField] private Transform target;

    [Header("전투 진입과 위치")]
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool autoStartEncounter = true;
    // 현재 보스방(-22~22)을 전부 포함하도록 넉넉한 인식 폭을 사용합니다.
    [SerializeField, Min(0.1f)] private float encounterRange = 50f;
    [SerializeField, Min(0.1f)] private float patternStartRange = 20f;
    [SerializeField, Min(0f)] private float preferredMinDistance = 7f;
    [SerializeField, Min(0f)] private float preferredMaxDistance = 11f;
    [SerializeField, Range(0.1f, 1f)] private float repositionSpeedMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float postPatternWaitSeconds = 1f;

    [Header("디버그")]
    [SerializeField] private hys_FinalBossState currentState = hys_FinalBossState.Inactive;
    [SerializeField] private hys_FinalBossPatternId lastPattern;
    [SerializeField] private string lastAction;

    private bool encounterStarted;
    private bool wasPatternRunning;
    private float nextPatternTime;
    private float nextTargetSearchTime;

    public bool EncounterStarted => encounterStarted;
    public bool IsDefeated => runtimeStatus != null && runtimeStatus.IsDead;
    // Animator 브리지가 현재 이동 및 그로기 상태를 안전하게 읽도록 공개합니다.
    public bool IsMoving => currentState == hys_FinalBossState.Chase;
    public bool IsGroggy => currentState == hys_FinalBossState.Groggy;
    public string LastAction => lastAction;

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
            SetState(hys_FinalBossState.Dead);
            StopHorizontalMovement();
            return;
        }

        if (!IsTargetCombatBody())
        {
            patternSystem?.CancelActivePattern();
            SetState(encounterStarted ? hys_FinalBossState.Idle : hys_FinalBossState.Inactive);
            StopHorizontalMovement();
            return;
        }

        if (patternSystem != null && patternSystem.IsPatternRunning)
        {
            wasPatternRunning = true;
            SetState(patternSystem.IsPhaseTransitioning
                ? hys_FinalBossState.PhaseTransition
                : patternSystem.IsGroggy ? hys_FinalBossState.Groggy : hys_FinalBossState.Attack);
            StopHorizontalMovement();
            return;
        }

        if (wasPatternRunning)
        {
            // 일반 패턴, 페이즈 전환, 공통 그로기 종료 뒤 모두 1초 호흡을 보장합니다.
            wasPatternRunning = false;
            nextPatternTime = Mathf.Max(nextPatternTime, Time.time + postPatternWaitSeconds);
        }

        if (!encounterStarted)
        {
            SetState(hys_FinalBossState.Inactive);
            StopHorizontalMovement();
            if (autoStartEncounter && GetHorizontalDistance() <= encounterRange) StartEncounter();
            return;
        }

        if (Time.time < nextPatternTime)
        {
            SetState(TryReposition() ? hys_FinalBossState.Chase : hys_FinalBossState.Wait);
            return;
        }

        if (GetHorizontalDistance() > patternStartRange)
        {
            SetState(hys_FinalBossState.Chase);
            MoveTowardTarget(1f);
            return;
        }

        // 공격 가능 거리 안에서도 최적 사거리까지 접근해 제자리 스킬 난사를 방지합니다.
        if (GetHorizontalDistance() > preferredMaxDistance)
        {
            SetState(hys_FinalBossState.Chase);
            MoveTowardTarget(repositionSpeedMultiplier);
            return;
        }

        FaceTarget();
        StopHorizontalMovement();
        SetState(hys_FinalBossState.Idle);
        if (patternSystem != null && patternSystem.TryStartNextPattern(target, HandlePatternFinished, out hys_FinalBossPatternId selected))
        {
            lastPattern = selected;
            lastAction = $"{selected} 시작";
            SetState(hys_FinalBossState.Attack);
        }
        else
        {
            nextPatternTime = Time.time + 0.2f;
        }
    }

    public void StartEncounter()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead) return;
        encounterStarted = true;
        wasPatternRunning = false;
        nextPatternTime = Time.time;
        patternSystem?.ResetPatternSelection();
        SetState(hys_FinalBossState.Idle);
        lastAction = "최종보스 1페이즈 시작";
    }

    public void StopEncounter()
    {
        encounterStarted = false;
        wasPatternRunning = false;
        patternSystem?.CancelActivePattern();
        StopHorizontalMovement();
        SetState(hys_FinalBossState.Inactive);
    }

    private void HandlePatternFinished(hys_FinalBossPatternId completed)
    {
        nextPatternTime = Time.time + Mathf.Max(0f, postPatternWaitSeconds);
        lastAction = $"{completed} 종료";
        SetState(hys_FinalBossState.Wait);
    }

    private bool TryReposition()
    {
        float distance = GetHorizontalDistance();
        if (distance > preferredMaxDistance)
        {
            MoveTowardTarget(repositionSpeedMultiplier);
            return true;
        }

        if (distance < preferredMinDistance)
        {
            MoveTowardTarget(-repositionSpeedMultiplier);
            return true;
        }

        FaceTarget();
        StopHorizontalMovement();
        return false;
    }

    private void MoveTowardTarget(float speedMultiplier)
    {
        if (target == null || body == null) return;
        float direction = Mathf.Sign(target.position.x - transform.position.x) * Mathf.Sign(speedMultiplier);
        float speed = runtimeStatus != null ? runtimeStatus.MoveSpeed : 0f;
        body.linearVelocity = new Vector2(direction * speed * Mathf.Abs(speedMultiplier), body.linearVelocity.y);
        FaceTarget();
    }

    private void FaceTarget()
    {
        if (target != null && spriteRenderer != null)
            spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    private float GetHorizontalDistance()
    {
        return target != null ? Mathf.Abs(target.position.x - transform.position.x) : float.PositiveInfinity;
    }

    private bool IsTargetCombatBody()
    {
        if (target == null) return false;
        HWJ_SoulSystem soul = target.GetComponentInParent<HWJ_SoulSystem>();
        if (soul != null && soul.CurrentState != HWJ_SoulRuntimeState.Body) return false;
        HWJ_RuntimeStatusSystem targetStatus = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        return targetStatus == null || !targetStatus.IsDead;
    }

    private void UpdateTarget()
    {
        if (target != null || !autoFindPlayerTarget || Time.time < nextTargetSearchTime) return;
        nextTargetSearchTime = Time.time + 0.4f;
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

    private void SetState(hys_FinalBossState state)
    {
        currentState = state;
        if (runtimeStatus == null || runtimeStatus.IsDead) return;
        switch (state)
        {
            case hys_FinalBossState.Chase: runtimeStatus.SetState(HWJ_RuntimeState.Move); break;
            case hys_FinalBossState.Attack: runtimeStatus.SetState(HWJ_RuntimeState.Attack); break;
            case hys_FinalBossState.PhaseTransition: runtimeStatus.SetState(HWJ_RuntimeState.Attack); break;
            case hys_FinalBossState.Groggy: runtimeStatus.SetState(HWJ_RuntimeState.Hit); break;
            case hys_FinalBossState.Dead: runtimeStatus.SetState(HWJ_RuntimeState.Dead); break;
            default: runtimeStatus.SetState(HWJ_RuntimeState.Idle); break;
        }
    }

    private void StopHorizontalMovement()
    {
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void CacheReferences()
    {
        if (dataResolver == null) dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (patternSystem == null) patternSystem = GetComponent<hys_FinalBossPattern>();
    }

    private void OnDisable()
    {
        patternSystem?.CancelActivePattern();
        StopHorizontalMovement();
    }
}
