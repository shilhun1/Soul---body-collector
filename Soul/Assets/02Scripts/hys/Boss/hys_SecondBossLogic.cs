using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-70)]
public class hys_SecondBossLogic : MonoBehaviour
{
    // 두 번째 보스의 추적, 패턴 선택, 공통 대기시간과 방향 고정을 관리합니다.
    private enum hys_SecondBossState { Inactive, Idle, Chase, Attack, Wait, Groggy, Dead }

    private struct hys_IgnoredColliderPair
    {
        public Collider2D BossCollider;
        public Collider2D PlayerCollider;

        public hys_IgnoredColliderPair(Collider2D bossCollider, Collider2D playerCollider)
        {
            BossCollider = bossCollider;
            PlayerCollider = playerCollider;
        }
    }

    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private hys_SecondBossPattern patternSystem;
    [SerializeField] private Transform target;

    [Header("플레이어 충돌")]
    // 보스와 플레이어의 몸 Collider만 서로 통과시키며 바닥과 공격 판정은 유지합니다.
    [SerializeField] private bool ignorePlayerBodyCollision = true;

    [Header("전투 시작")]
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool autoStartEncounter = true;
    // 첫 전투 진입에서는 기획 순서대로 패턴 1, 2, 3을 한 번씩 실행합니다.
    // 테스트 씬에서는 영혼 상태도 추적하며, 필요하면 Inspector에서 육체 상태만 대상으로 제한합니다.
    [SerializeField, Min(0.1f)] private float encounterRange = 30f;
    [SerializeField, Min(0.1f)] private float patternStartRange = 14f;

    [Header("전투 위치 조정")]
    // 패턴 대기 중에도 적정 거리를 잡아 보스가 제자리에만 서 있지 않게 합니다.
    [SerializeField, Min(0f)] private float preferredCombatMinDistance = 3.5f;
    [SerializeField, Min(0f)] private float preferredCombatMaxDistance = 6f;
    [SerializeField, Range(0.1f, 1f)] private float repositionSpeedMultiplier = 0.35f;

    [Header("공통 패턴 규칙")]
    // 패턴이 완전히 종료된 시점부터 다음 패턴 선택까지 1초를 기다립니다.
    [SerializeField, Min(0f)] private float postPatternWaitSeconds = 1f;
    [SerializeField] private bool flipSpriteWhenFacingLeft = true;

    [Header("디버그")]
    [SerializeField] private hys_SecondBossState currentState = hys_SecondBossState.Inactive;
    [SerializeField] private hys_SecondBossPatternId lastPattern = hys_SecondBossPatternId.None;
    [SerializeField] private float nextPatternSelectTime;
    [SerializeField] private float lockedAttackDirection = 1f;
    [SerializeField] private string lastAction;

    private readonly List<hys_IgnoredColliderPair> ignoredColliderPairs = new List<hys_IgnoredColliderPair>();
    private float nextTargetSearchTime;
    private bool encounterStarted;
    private Transform collisionIgnoredTarget;

    public bool EncounterStarted => encounterStarted;
    public bool IsDefeated => runtimeStatus != null && runtimeStatus.IsDead;
    // Animator 브리지가 현재 이동 및 그로기 상태를 안전하게 읽도록 공개합니다.
    public bool IsMoving => currentState == hys_SecondBossState.Chase;
    public bool IsGroggy => currentState == hys_SecondBossState.Groggy;
    public hys_SecondBossPatternId LastPattern => lastPattern;
    public string LastAction => lastAction;

    private void Awake() { CacheReferences(); }

    private void Update()
    {
        CacheReferences();
        UpdateTarget();
        RefreshPlayerBodyCollisionIgnore();

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            SetState(hys_SecondBossState.Dead);
            StopHorizontalMovement();
            return;
        }

        // 플레이어가 영혼이거나 전환 중이면 즉시 패턴을 끊고 공격 대상으로 보지 않습니다.
        if (!IsTargetCombatBody())
        {
            if (patternSystem != null && patternSystem.IsPatternRunning)
            {
                patternSystem.CancelActivePattern();
            }

            nextPatternSelectTime = Time.time + Mathf.Max(0f, postPatternWaitSeconds);
            SetState(encounterStarted ? hys_SecondBossState.Idle : hys_SecondBossState.Inactive);
            StopHorizontalMovement();
            lastAction = "플레이어 영혼 상태 - 인식 및 공격 중지";
            return;
        }

        if (patternSystem != null && patternSystem.IsPatternRunning)
        {
            SetState(patternSystem.IsGroggy ? hys_SecondBossState.Groggy : hys_SecondBossState.Attack);
            return;
        }

        if (!encounterStarted)
        {
            SetState(hys_SecondBossState.Inactive);
            StopHorizontalMovement();
            if (ShouldStartEncounter()) StartEncounter();
            return;
        }

        if (Time.time < nextPatternSelectTime)
        {
            SetState(TryAdjustCombatPosition()
                ? hys_SecondBossState.Chase
                : hys_SecondBossState.Wait);
            return;
        }

        if (GetHorizontalDistanceToTarget() > patternStartRange)
        {
            SetState(hys_SecondBossState.Chase);
            MoveTowardTarget();
            return;
        }

        SetState(hys_SecondBossState.Idle);
        StopHorizontalMovement();
        if (!TryStartNextPattern())
        {
            SetState(TryAdjustCombatPosition()
                ? hys_SecondBossState.Chase
                : hys_SecondBossState.Idle);
        }
    }

    public void StartEncounter()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead) return;
        encounterStarted = true;
        patternSystem?.ResetPatternSelection();
        nextPatternSelectTime = Time.time;
        lastAction = "두 번째 보스 전투 시작";
        SetState(hys_SecondBossState.Idle);
    }

    public void StopEncounter()
    {
        encounterStarted = false;
        patternSystem?.CancelActivePattern();
        patternSystem?.ResetPatternSelection();
        StopHorizontalMovement();
        SetState(hys_SecondBossState.Inactive);
        lastAction = "두 번째 보스 전투 중지";
    }

    private bool ShouldStartEncounter()
    {
        return autoStartEncounter && target != null
            && IsTargetCombatBody()
            && GetHorizontalDistanceToTarget() <= encounterRange;
    }

    private bool TryStartNextPattern()
    {
        if (patternSystem == null || target == null) return false;

        lockedAttackDirection = ResolveDirectionToTarget();
        FaceDirection(lockedAttackDirection);

        // 패턴 순서, HWJ 데이터 조건, 쿨타임과 가중치 선택은 패턴 시스템에서 한 번에 처리합니다.
        if (!patternSystem.TryStartNextPattern(
                target,
                lockedAttackDirection,
                HandlePatternFinished,
                out hys_SecondBossPatternId selected))
        {
            lastAction = "HWJ 데이터 조건에 맞는 패턴 대기";
            return false;
        }

        lastPattern = selected;
        lastAction = $"{selected} 시작";
        SetState(hys_SecondBossState.Attack);
        StopHorizontalMovement();
        return true;
    }

    private void HandlePatternFinished(hys_SecondBossPatternId completedPattern)
    {
        nextPatternSelectTime = Time.time + Mathf.Max(0f, postPatternWaitSeconds);
        lastAction = $"{completedPattern} 종료, {postPatternWaitSeconds:0.##}초 대기";
        SetState(hys_SecondBossState.Wait);
        StopHorizontalMovement();
    }

    private bool TryAdjustCombatPosition()
    {
        if (target == null || body == null)
        {
            StopHorizontalMovement();
            return false;
        }

        float distance = GetHorizontalDistanceToTarget();
        float towardTarget = ResolveDirectionToTarget();
        float moveDirection;

        if (distance > Mathf.Max(preferredCombatMinDistance, preferredCombatMaxDistance))
        {
            moveDirection = towardTarget;
        }
        else if (distance < Mathf.Max(0f, preferredCombatMinDistance))
        {
            moveDirection = -towardTarget;
        }
        else
        {
            FaceDirection(towardTarget);
            StopHorizontalMovement();
            return false;
        }

        float baseMoveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        float repositionSpeed = Mathf.Max(0f, baseMoveSpeed) * Mathf.Clamp01(repositionSpeedMultiplier);
        FaceDirection(towardTarget);
        body.linearVelocity = new Vector2(moveDirection * repositionSpeed, body.linearVelocity.y);
        return repositionSpeed > 0f;
    }

    private void MoveTowardTarget()
    {
        if (target == null || body == null) { StopHorizontalMovement(); return; }
        float direction = ResolveDirectionToTarget();
        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        FaceDirection(direction);
        body.linearVelocity = new Vector2(direction * Mathf.Max(0f, moveSpeed), body.linearVelocity.y);
    }

    private float ResolveDirectionToTarget()
    {
        if (target == null) return lockedAttackDirection == 0f ? 1f : Mathf.Sign(lockedAttackDirection);
        float deltaX = target.position.x - transform.position.x;
        return Mathf.Abs(deltaX) <= 0.01f
            ? (lockedAttackDirection == 0f ? 1f : Mathf.Sign(lockedAttackDirection)) : Mathf.Sign(deltaX);
    }

    private void FaceDirection(float direction)
    {
        if (spriteRenderer != null && direction != 0f)
            spriteRenderer.flipX = flipSpriteWhenFacingLeft && direction < 0f;
    }

    private void StopHorizontalMovement()
    {
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void SetState(hys_SecondBossState state)
    {
        currentState = state;
        if (runtimeStatus == null || runtimeStatus.IsDead) return;
        switch (state)
        {
            case hys_SecondBossState.Chase: runtimeStatus.SetState(HWJ_RuntimeState.Move); break;
            case hys_SecondBossState.Attack: runtimeStatus.SetState(HWJ_RuntimeState.Attack); break;
            case hys_SecondBossState.Groggy: runtimeStatus.SetState(HWJ_RuntimeState.Hit); break;
            case hys_SecondBossState.Dead: runtimeStatus.SetState(HWJ_RuntimeState.Dead); break;
            default: runtimeStatus.SetState(HWJ_RuntimeState.Idle); break;
        }
    }

    private void UpdateTarget()
    {
        if (target != null || !autoFindPlayerTarget || Time.time < nextTargetSearchTime) return;
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
            { target = resolvers[i].transform; return; }
        }
    }

    private bool IsTargetCombatBody()
    {
        if (target == null) return false;
        HWJ_SoulSystem soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();
        return soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private float GetHorizontalDistanceToTarget()
    {
        return target != null ? Mathf.Abs(target.position.x - transform.position.x) : float.PositiveInfinity;
    }

    private void CacheReferences()
    {
        if (dataResolver == null) dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (patternSystem == null) patternSystem = GetComponent<hys_SecondBossPattern>();
    }

    private void RefreshPlayerBodyCollisionIgnore()
    {
        if (!ignorePlayerBodyCollision || target == null)
        {
            RestorePlayerBodyCollision();
            return;
        }

        if (collisionIgnoredTarget == target && ignoredColliderPairs.Count > 0)
        {
            // 영혼 전환 중 Collider가 꺼졌다 켜져도 통과 설정이 유지되도록 다시 확인합니다.
            for (int i = 0; i < ignoredColliderPairs.Count; i++)
            {
                hys_IgnoredColliderPair pair = ignoredColliderPairs[i];
                if (pair.BossCollider != null && pair.PlayerCollider != null
                    && !Physics2D.GetIgnoreCollision(pair.BossCollider, pair.PlayerCollider))
                {
                    Physics2D.IgnoreCollision(pair.BossCollider, pair.PlayerCollider, true);
                }
            }

            return;
        }

        RestorePlayerBodyCollision();

        Collider2D[] bossColliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] playerColliders = target.GetComponentsInChildren<Collider2D>(true);

        for (int bossIndex = 0; bossIndex < bossColliders.Length; bossIndex++)
        {
            Collider2D bossCollider = bossColliders[bossIndex];
            if (bossCollider == null || bossCollider.isTrigger)
            {
                continue;
            }

            for (int playerIndex = 0; playerIndex < playerColliders.Length; playerIndex++)
            {
                Collider2D playerCollider = playerColliders[playerIndex];
                if (playerCollider == null || playerCollider.isTrigger || playerCollider == bossCollider)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(bossCollider, playerCollider, true);
                ignoredColliderPairs.Add(new hys_IgnoredColliderPair(bossCollider, playerCollider));
            }
        }

        collisionIgnoredTarget = target;
    }

    private void RestorePlayerBodyCollision()
    {
        for (int i = 0; i < ignoredColliderPairs.Count; i++)
        {
            hys_IgnoredColliderPair pair = ignoredColliderPairs[i];
            if (pair.BossCollider != null && pair.PlayerCollider != null)
            {
                Physics2D.IgnoreCollision(pair.BossCollider, pair.PlayerCollider, false);
            }
        }

        ignoredColliderPairs.Clear();
        collisionIgnoredTarget = null;
    }

    private void OnDisable()
    {
        patternSystem?.CancelActivePattern();
        StopHorizontalMovement();
        RestorePlayerBodyCollision();
    }
}
