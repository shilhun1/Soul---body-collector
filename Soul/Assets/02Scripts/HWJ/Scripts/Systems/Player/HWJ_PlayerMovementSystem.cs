using UnityEngine;

/// <summary>
/// PlayerTypeDataSO.Control과 RootObjectDataSO.Status를 사용해 플레이어 이동을 처리하는 기본 시스템입니다.
/// 실제 입력 시스템을 바꿔도 이 컴포넌트는 데이터만 읽도록 유지하고, 입력 값만 외부에서 주입하는 방식으로 확장할 수 있습니다.
/// </summary>
public class HWJ_PlayerMovementSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Rigidbody2D body;

    [Header("Ground Check")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.12f);

    [Header("Soul Collision")]
    [SerializeField] private bool phaseThroughCollidersInSoul = true;
    [SerializeField] private bool isSoulCollisionMode;

    [Header("Enemy Collision")]
    [SerializeField] private bool ignoreEnemyBodyCollision = true;
    [SerializeField] private float enemyCollisionRefreshSeconds = 0.25f;

    private int usedDoubleJumpCount;
    private int usedDashCount;
    private float dashEndTime;
    private float nextDashTime;
    private float nextEnemyCollisionRefreshTime;
    private Vector2 moveInput;
    private float originalGravityScale;
    private bool hasOriginalGravityScale;
    private bool isGravitySuppressed;
    private Collider2D[] ownedColliders;
    private bool[] originalColliderTriggerStates;

    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<HWJ_PlayerInputSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        CacheOriginalGravityScale();
        CacheOwnedColliders();
    }

    private void Update()
    {
        moveInput = playerInput != null ? playerInput.MoveInput : Vector2.zero;
        UpdateSoulCollisionMode();
        UpdateEnemyCollisionIgnores();
        bool canUseBodyActions = CanUseBodyActions();

        if (canUseBodyActions)
        {
            UpdateGrounded();
        }

        if (canUseBodyActions && isGrounded)
        {
            usedDoubleJumpCount = 0;
            ResetDashCountWhenReady();
        }

        if (canUseBodyActions && playerInput != null && playerInput.JumpPressedThisFrame)
        {
            TryJump();
        }

        if (canUseBodyActions && playerInput != null && playerInput.DashPressedThisFrame)
        {
            TryDash();
        }
    }

    private void FixedUpdate()
    {
        UpdateSoulCollisionMode();
        UpdateEnemyCollisionIgnores();

        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        UpdateGrounded();

        if (soulSystem == null)
        {
            SetGravitySuppressed(false);
            MoveBody(playerData);
            return;
        }

        switch (soulSystem.CurrentState)
        {
            case HWJ_SoulRuntimeState.Body:
                SetGravitySuppressed(Time.time < dashEndTime);
                MoveBody(playerData);
                break;
            case HWJ_SoulRuntimeState.BodyToSoul:
                StopMovement(true);
                break;
            case HWJ_SoulRuntimeState.Soul:
                SetGravitySuppressed(true);
                MoveSoul(playerData);
                break;
            case HWJ_SoulRuntimeState.Dead:
                StopMovement(true);
                break;
        }
    }

    /// <summary>
    /// 외부 지면 판정 스크립트가 현재 접지 상태를 알려줄 때 사용합니다.
    /// 지면 판정 구현이 바뀌어도 이동 데이터 구조는 유지됩니다.
    /// </summary>
    public void SetGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    private void UpdateGrounded()
    {
        if (!CanUseBodyActions())
        {
            isGrounded = false;
            return;
        }

        if (body != null && body.linearVelocity.y > 0.01f)
        {
            isGrounded = false;
            return;
        }

        bool wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        if (isGrounded && !wasGrounded)
        {
            usedDoubleJumpCount = 0;
            usedDashCount = 0;
        }
    }

    private void OnDisable()
    {
        SetSoulCollisionMode(false);
    }

    private bool CheckGrounded()
    {
        Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = groundLayer.value != 0,
            layerMask = groundLayer,
            useTriggers = false
        };

        Collider2D[] hits = new Collider2D[8];
        int hitCount = Physics2D.OverlapBox(checkCenter, groundCheckSize, 0f, filter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || IsOwnCollider(hit) || IsEnemyOrBossCollider(hit))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider2D hit)
    {
        return hit.attachedRigidbody == body || hit.transform.IsChildOf(transform);
    }

    private bool IsEnemyOrBossCollider(Collider2D hit)
    {
        if (hit == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver resolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return resolver != null
            && (resolver.ObjectType == HWJ_ObjectType.Enemy || resolver.ObjectType == HWJ_ObjectType.Boss);
    }

    private void MoveBody(HWJ_PlayerTypeDataSO playerData)
    {
        if (Time.time < dashEndTime)
        {
            return;
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput.x * moveSpeed;
        body.linearVelocity = velocity;

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(Mathf.Abs(moveInput.x) > 0.01f ? HWJ_RuntimeState.Move : HWJ_RuntimeState.Idle);
        }
    }

    private void MoveSoul(HWJ_PlayerTypeDataSO playerData)
    {
        if (!playerData.SoulState.canFreeFly)
        {
            StopMovement(true);
            return;
        }

        body.linearVelocity = moveInput.normalized * playerData.SoulState.soulMoveSpeed;

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(HWJ_RuntimeState.Soul);
        }
    }

    private void TryJump()
    {
        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        if (!CanUseBodyActions())
        {
            return;
        }

        if (isGrounded)
        {
            ExecuteNormalJump(playerData.Control.normalJump);
            return;
        }

        if (!CanDoubleJump(playerData.Control.doubleJump))
        {
            return;
        }

        ExecuteDoubleJump(playerData.Control.doubleJump);
    }

    private void ExecuteNormalJump(HWJ_NormalJumpData jumpData)
    {
        if (jumpData == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.y = jumpData.jumpPower;
        body.linearVelocity = velocity;
        isGrounded = false;
        motionSystem?.PlayJump();
    }

    private void ExecuteDoubleJump(HWJ_DoubleJumpData jumpData)
    {
        if (jumpData == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.y = jumpData.jumpPower;
        body.linearVelocity = velocity;
        usedDoubleJumpCount++;
        motionSystem?.PlayDoubleJump();
    }

    private bool CanDoubleJump(HWJ_DoubleJumpData jumpData)
    {
        return jumpData != null
            && jumpData.canDoubleJump
            && usedDoubleJumpCount < jumpData.maxDoubleJumpCount;
    }

    private void TryDash()
    {
        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        if (!CanUseBodyActions())
        {
            return;
        }

        if (Time.time < nextDashTime)
        {
            return;
        }

        int maxDashCount = Mathf.Max(1, playerData.Control.maxDashCount);

        if (usedDashCount >= maxDashCount)
        {
            return;
        }

        if (!isGrounded && !playerData.Control.canAirDash)
        {
            return;
        }

        float direction = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) : Mathf.Sign(transform.localScale.x);
        body.linearVelocity = new Vector2(direction * playerData.Control.dashSpeed, 0f);
        SetGravitySuppressed(true);
        float dashDuration = playerData.Control.dashDuration > 0f
            ? playerData.Control.dashDuration
            : playerData.Control.dashDistance / Mathf.Max(0.01f, playerData.Control.dashSpeed);

        dashEndTime = Time.time + dashDuration;
        usedDashCount++;
        float nextDashDelay = usedDashCount >= maxDashCount
            ? playerData.Control.dashCooldown
            : playerData.Control.dashStepCooldown;
        nextDashTime = Time.time + Mathf.Max(0f, nextDashDelay);
        motionSystem?.PlayDash();
    }

    private void ResetDashCountWhenReady()
    {
        if (Time.time < dashEndTime || Time.time < nextDashTime)
        {
            return;
        }

        usedDashCount = 0;
    }

    private void StopMovement(bool suppressGravity = false)
    {
        if (body != null)
        {
            SetGravitySuppressed(suppressGravity);
            body.linearVelocity = Vector2.zero;
        }
    }

    private void CacheOriginalGravityScale()
    {
        if (body == null || hasOriginalGravityScale)
        {
            return;
        }

        originalGravityScale = body.gravityScale;
        hasOriginalGravityScale = true;
    }

    private void SetGravitySuppressed(bool suppress)
    {
        if (body == null)
        {
            return;
        }

        CacheOriginalGravityScale();

        if (suppress)
        {
            if (!isGravitySuppressed)
            {
                body.gravityScale = 0f;
                isGravitySuppressed = true;
            }

            return;
        }

        if (isGravitySuppressed)
        {
            body.gravityScale = originalGravityScale;
            isGravitySuppressed = false;
        }
    }

    private bool CanUseBodyActions()
    {
        return soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private void CacheOwnedColliders()
    {
        ownedColliders = GetComponentsInChildren<Collider2D>();
        originalColliderTriggerStates = new bool[ownedColliders.Length];

        for (int i = 0; i < ownedColliders.Length; i++)
        {
            originalColliderTriggerStates[i] = ownedColliders[i] != null && ownedColliders[i].isTrigger;
        }
    }

    private void UpdateEnemyCollisionIgnores()
    {
        if (!ignoreEnemyBodyCollision || Time.time < nextEnemyCollisionRefreshTime)
        {
            return;
        }

        nextEnemyCollisionRefreshTime = Time.time + Mathf.Max(0.02f, enemyCollisionRefreshSeconds);

        if (ownedColliders == null || ownedColliders.Length == 0)
        {
            CacheOwnedColliders();
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver == dataResolver
                || (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss))
            {
                continue;
            }

            IgnoreCollisionWithResolver(resolver);
        }
    }

    private void IgnoreCollisionWithResolver(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || ownedColliders == null)
        {
            return;
        }

        Collider2D[] targetColliders = resolver.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < ownedColliders.Length; i++)
        {
            Collider2D ownedCollider = ownedColliders[i];

            if (ownedCollider == null)
            {
                continue;
            }

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider2D targetCollider = targetColliders[j];

                if (targetCollider == null || targetCollider == ownedCollider)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(ownedCollider, targetCollider, true);
            }
        }
    }

    private void UpdateSoulCollisionMode()
    {
        if (!phaseThroughCollidersInSoul)
        {
            SetSoulCollisionMode(false);
            return;
        }

        bool shouldPhase = soulSystem != null
            && (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
                || soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul);

        SetSoulCollisionMode(shouldPhase);
    }

    private void SetSoulCollisionMode(bool enabled)
    {
        if (isSoulCollisionMode == enabled)
        {
            return;
        }

        if (ownedColliders == null || originalColliderTriggerStates == null)
        {
            CacheOwnedColliders();
        }

        for (int i = 0; i < ownedColliders.Length; i++)
        {
            if (ownedColliders[i] == null)
            {
                continue;
            }

            ownedColliders[i].isTrigger = enabled || originalColliderTriggerStates[i];
        }

        isSoulCollisionMode = enabled;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkCenter = transform.position + (Vector3)groundCheckOffset;
        Gizmos.DrawWireCube(checkCenter, groundCheckSize);
    }
}
