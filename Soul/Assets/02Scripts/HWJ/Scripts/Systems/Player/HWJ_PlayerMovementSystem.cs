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
    [SerializeField] private Rigidbody2D body;

    [Header("Ground Check")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.12f);

    [Header("Soul Collision")]
    [SerializeField] private bool phaseThroughCollidersInSoul = true;
    [SerializeField] private bool isSoulCollisionMode;

    private int usedDoubleJumpCount;
    private float dashEndTime;
    private float nextDashTime;
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
        bool canUseBodyActions = CanUseBodyActions();

        if (canUseBodyActions)
        {
            UpdateGrounded();
        }

        if (canUseBodyActions && isGrounded)
        {
            usedDoubleJumpCount = 0;
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
                SetGravitySuppressed(false);
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

            if (hit == null || IsOwnCollider(hit))
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

        if (!isGrounded && !playerData.Control.canAirDash)
        {
            return;
        }

        float direction = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) : Mathf.Sign(transform.localScale.x);
        body.linearVelocity = new Vector2(direction * playerData.Control.dashSpeed, 0f);
        float dashDuration = playerData.Control.dashDuration > 0f
            ? playerData.Control.dashDuration
            : playerData.Control.dashDistance / Mathf.Max(0.01f, playerData.Control.dashSpeed);

        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + playerData.Control.dashCooldown;
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
