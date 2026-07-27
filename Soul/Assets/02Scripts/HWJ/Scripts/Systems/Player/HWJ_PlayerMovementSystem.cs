using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerTypeDataSO.Control과 RootObjectDataSO.Status를 사용해 플레이어 이동을 처리하는 기본 시스템입니다.
/// 실제 입력 시스템을 바꿔도 이 컴포넌트는 데이터만 읽도록 유지하고, 입력 값만 외부에서 주입하는 방식으로 확장할 수 있습니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HWJ_RuntimeStatusSystem))]
public class HWJ_PlayerMovementSystem : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_PlayerAttackSystem playerAttackSystem;
    [SerializeField] private Rigidbody2D body;

    [Space(8f)]
    [Header("Ground Check")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.12f);

    [Space(8f)]
    [Header("Soul Collision")]
    [SerializeField] private bool phaseThroughCollidersInSoul = true;
    [SerializeField] private bool swapLayerInSoulState = true;
    [SerializeField] private string soulLayerName = "Soul";
    [SerializeField] private string bodyLayerName = "Player";
    [SerializeField] private bool isSoulCollisionMode;

    [Space(8f)]
    [Header("Enemy Collision")]
    [SerializeField] private bool ignoreEnemyBodyCollision = true;
    [SerializeField] private float enemyCollisionRefreshSeconds = 0.25f;

    private int usedDoubleJumpCount;
    private int usedDashCount;
    private float dashEndTime;
    private float nextDashTime;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private float nextEnemyCollisionRefreshTime;
    private Vector2 moveInput;
    private float originalGravityScale;
    private bool hasOriginalGravityScale;
    private bool isGravitySuppressed;
    private bool jumpCutApplied;
    private Collider2D[] ownedColliders;
    private bool[] originalColliderTriggerStates;
    private int originalRootLayer;
    private bool hasOriginalRootLayer;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private int groundHitCount;
    private readonly List<HWJ_TemporaryIgnoredCollider> ignoredPlatformColliders =
        new List<HWJ_TemporaryIgnoredCollider>();

    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        AutoWireReferences();
        CacheOriginalGravityScale();
        CacheOwnedColliders();
        CacheOriginalRootLayer();
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;
    }

    private void Reset()
    {
        AutoWireReferences();
    }

    private void OnValidate()
    {
        AutoWireReferences();
    }

    private void AutoWireReferences()
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
            ResolvePlayerInput();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (playerAttackSystem == null)
        {
            playerAttackSystem = GetComponent<HWJ_PlayerAttackSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        ResolvePlayerInput();
        moveInput = playerInput != null ? playerInput.MoveInput : Vector2.zero;
        UpdateSoulCollisionMode();
        UpdateEnemyCollisionIgnores();
        UpdateTemporaryPlatformIgnores();
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
            if (IsDropInputHeld())
            {
                lastJumpPressedTime = -999f;
                TryDropThroughPlatform();
                return;
            }

            lastJumpPressedTime = Time.time;
        }

        if (canUseBodyActions)
        {
            TryBufferedJump();
            ApplyVariableJumpCut();
        }

        if (canUseBodyActions && playerInput != null && playerInput.DashPressedThisFrame)
        {
            TryDash();
        }
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput = GetComponent<HWJ_PlayerInputSystem>();

        if (playerInput == null)
        {
            playerInput = HWJ_GameAccess.PlayerInput;
        }
    }

    private void FixedUpdate()
    {
        UpdateSoulCollisionMode();
        UpdateEnemyCollisionIgnores();
        UpdateTemporaryPlatformIgnores();

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
                ApplyBodyGravityTuning(playerData);
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

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
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

        groundHitCount = Physics2D.OverlapBox(checkCenter, groundCheckSize, 0f, filter, groundHits);

        for (int i = 0; i < groundHitCount; i++)
        {
            Collider2D hit = groundHits[i];

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

        if (runtimeStatus != null && !runtimeStatus.CanMove)
        {
            StopHorizontalMovement();
            return;
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;
        Vector2 velocity = body.linearVelocity;
        float targetSpeed = moveInput.x * moveSpeed;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f
            ? isGrounded ? playerData.Control.acceleration : playerData.Control.airAcceleration
            : isGrounded ? playerData.Control.deceleration : playerData.Control.airDeceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, Mathf.Max(0f, acceleration) * Time.fixedDeltaTime);

        if (IsWallAhead(Mathf.Sign(velocity.x), playerData.Control))
        {
            velocity.x = 0f;
        }

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

    private void TryBufferedJump()
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        float bufferTime = playerData.Control.normalJump != null
            ? Mathf.Max(0f, playerData.Control.normalJump.jumpBufferTime)
            : 0f;

        if (Time.time > lastJumpPressedTime + bufferTime)
        {
            return;
        }

        if (TryJump())
        {
            lastJumpPressedTime = -999f;
        }
    }

    private bool TryJump()
    {
        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        if (!CanUseBodyActions() || runtimeStatus != null && !runtimeStatus.CanMove)
        {
            return false;
        }

        if (isGrounded || Time.time <= lastGroundedTime + Mathf.Max(0f, playerData.Control.coyoteTimeSeconds))
        {
            ExecuteNormalJump(playerData.Control.normalJump);
            return true;
        }

        if (!CanDoubleJump(playerData.Control.doubleJump))
        {
            return false;
        }

        ExecuteDoubleJump(playerData.Control.doubleJump);
        return true;
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
        jumpCutApplied = false;
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
        jumpCutApplied = false;
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

        if (runtimeStatus != null && !runtimeStatus.CanDash)
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
        skillActionSystem?.CancelCurrentAction();
        playerAttackSystem?.CancelCurrentAttack();
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
        runtimeStatus?.LockAttack(dashDuration + Mathf.Max(0f, playerData.Control.dashRecoverySeconds));
        runtimeStatus?.GrantInvincibility(playerData.Control.dashInvincibleSeconds);
    }

    private void ResetDashCountWhenReady()
    {
        if (Time.time < dashEndTime || Time.time < nextDashTime)
        {
            return;
        }

        usedDashCount = 0;
    }

    private void ApplyVariableJumpCut()
    {
        if (body == null || playerInput == null || playerInput.JumpHeld || jumpCutApplied)
        {
            return;
        }

        if (body.linearVelocity.y <= 0.01f
            || dataResolver == null
            || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        float cutMultiplier = playerData.Control.jumpCutMultiplier > 0f
            ? Mathf.Clamp(playerData.Control.jumpCutMultiplier, 0.55f, 1f)
            : 0.65f;
        velocity.y *= cutMultiplier;
        body.linearVelocity = velocity;
        jumpCutApplied = true;
    }

    private void ApplyBodyGravityTuning(HWJ_PlayerTypeDataSO playerData)
    {
        if (body == null || playerData == null || Time.time < dashEndTime || isGravitySuppressed)
        {
            return;
        }

        CacheOriginalGravityScale();

        if (body.linearVelocity.y < -0.01f)
        {
            body.gravityScale = originalGravityScale * Mathf.Max(1f, playerData.Control.fallGravityMultiplier);
        }
        else
        {
            body.gravityScale = originalGravityScale;
        }

        if (playerData.Control.maxFallSpeed > 0f && body.linearVelocity.y < -playerData.Control.maxFallSpeed)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = -playerData.Control.maxFallSpeed;
            body.linearVelocity = velocity;
        }
    }

    private bool TryDropThroughPlatform()
    {
        if (!isGrounded || !IsDropInputHeld())
        {
            return false;
        }

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        bool dropped = false;
        float ignoreSeconds = Mathf.Max(0.05f, playerData.Control.dropThroughSeconds);

        for (int i = 0; i < groundHitCount; i++)
        {
            Collider2D platformCollider = groundHits[i];

            if (platformCollider == null || !IsOneWayPlatform(platformCollider))
            {
                continue;
            }

            IgnorePlatformTemporarily(platformCollider, ignoreSeconds);
            dropped = true;
        }

        if (!dropped)
        {
            dropped = TryDropThroughPlatformBelow(ignoreSeconds);
        }

        if (!dropped)
        {
            return false;
        }

        isGrounded = false;
        lastGroundedTime = -999f;

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Min(velocity.y, -2f);
            body.linearVelocity = velocity;
        }

        motionSystem?.PlayDropJump();
        return true;
    }

    private bool TryDropThroughPlatformBelow(float ignoreSeconds)
    {
        Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = groundLayer.value != 0,
            layerMask = groundLayer,
            useTriggers = false
        };

        Collider2D[] hits = new Collider2D[8];
        int hitCount = Physics2D.OverlapBox(checkCenter, groundCheckSize * 1.6f, 0f, filter, hits);
        bool dropped = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D platformCollider = hits[i];

            if (platformCollider == null || !IsOneWayPlatform(platformCollider))
            {
                continue;
            }

            IgnorePlatformTemporarily(platformCollider, ignoreSeconds);
            dropped = true;
        }

        return dropped;
    }

    private bool IsDropInputHeld()
    {
        return moveInput.y < -0.5f || playerInput != null && playerInput.DownHeld;
    }

    private void IgnorePlatformTemporarily(Collider2D platformCollider, float seconds)
    {
        if (platformCollider == null)
        {
            return;
        }

        if (ownedColliders == null || ownedColliders.Length == 0)
        {
            CacheOwnedColliders();
        }

        for (int i = 0; i < ownedColliders.Length; i++)
        {
            Collider2D ownedCollider = ownedColliders[i];

            if (ownedCollider == null || ownedCollider.isTrigger)
            {
                continue;
            }

            Physics2D.IgnoreCollision(ownedCollider, platformCollider, true);
            ignoredPlatformColliders.Add(new HWJ_TemporaryIgnoredCollider(
                ownedCollider,
                platformCollider,
                Time.time + seconds));
        }
    }

    private void UpdateTemporaryPlatformIgnores()
    {
        for (int i = ignoredPlatformColliders.Count - 1; i >= 0; i--)
        {
            HWJ_TemporaryIgnoredCollider ignored = ignoredPlatformColliders[i];

            if (Time.time < ignored.endTime)
            {
                continue;
            }

            if (ignored.ownerCollider != null && ignored.platformCollider != null)
            {
                Physics2D.IgnoreCollision(ignored.ownerCollider, ignored.platformCollider, false);
            }

            ignoredPlatformColliders.RemoveAt(i);
        }
    }

    private bool IsOneWayPlatform(Collider2D platformCollider)
    {
        return platformCollider != null
            && (platformCollider.GetComponentInParent<HWJ_OneWayPlatformSystem>() != null
                || platformCollider.GetComponent<PlatformEffector2D>() != null
                || platformCollider.usedByEffector);
    }

    private bool IsWallAhead(float directionX, HWJ_ControlData controlData)
    {
        if (body == null
            || Mathf.Abs(directionX) <= 0.01f
            || controlData == null
            || controlData.wallCheckDistance <= 0f
            || groundLayer.value == 0)
        {
            return false;
        }

        int layerMask = groundLayer.value;
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.right * Mathf.Sign(directionX);
        float distance = controlData.wallCheckDistance;

        return Physics2D.Raycast(origin + Vector2.up * 0.35f, direction, distance, layerMask)
            || Physics2D.Raycast(origin, direction, distance, layerMask)
            || Physics2D.Raycast(origin + Vector2.down * 0.35f, direction, distance, layerMask);
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

                if (targetCollider == null || targetCollider.isTrigger || targetCollider == ownedCollider)
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

        ApplySoulLayer(enabled);
        isSoulCollisionMode = enabled;
    }

    private void CacheOriginalRootLayer()
    {
        if (hasOriginalRootLayer)
        {
            return;
        }

        originalRootLayer = gameObject.layer;
        hasOriginalRootLayer = true;
    }

    private void ApplySoulLayer(bool enabled)
    {
        if (!swapLayerInSoulState)
        {
            return;
        }

        CacheOriginalRootLayer();
        int targetLayer = enabled ? LayerMask.NameToLayer(soulLayerName) : LayerMask.NameToLayer(bodyLayerName);

        if (targetLayer < 0)
        {
            targetLayer = enabled ? originalRootLayer : originalRootLayer;
        }

        SetLayerRecursively(transform, targetLayer);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkCenter = transform.position + (Vector3)groundCheckOffset;
        Gizmos.DrawWireCube(checkCenter, groundCheckSize);
    }

    private readonly struct HWJ_TemporaryIgnoredCollider
    {
        public readonly Collider2D ownerCollider;
        public readonly Collider2D platformCollider;
        public readonly float endTime;

        public HWJ_TemporaryIgnoredCollider(Collider2D ownerCollider, Collider2D platformCollider, float endTime)
        {
            this.ownerCollider = ownerCollider;
            this.platformCollider = platformCollider;
            this.endTime = endTime;
        }
    }
}
