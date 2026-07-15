using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(hys_Player_State))]
public class hys_Player_Movement : MonoBehaviour
{
    [Header("Move")]
    // 좌우 이동 속도입니다.
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    // 점프 시작 속도와 점프 감각 조절 값들입니다.
    [SerializeField] private float jumpPower = 12f;
    [SerializeField] private float jumpStartBoost = 1.12f;

    // 상승/최상단/낙하 구간별 중력 배율입니다.
    [SerializeField] private float risingGravityMultiplier = 1.35f;
    [SerializeField] private float apexVelocityThreshold = 4f;
    [SerializeField] private float apexGravityMultiplier = 4.5f;

    // 최상단에서 오래 멈춰 보이지 않도록 아래 방향 속도를 살짝 넣는 값입니다.
    [SerializeField] private float apexSnapVelocityThreshold = 0.8f;
    [SerializeField] private float apexSnapFallSpeed = 4f;

    // 점프 입력 허용 횟수와 입력 보정 시간입니다.
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 3.2f;
    [SerializeField] private float maxFallSpeed = 30f;

    [Header("Dash")]
    // 대시 속도, 지속 시간, 쿨타임, 입력 버퍼 값입니다.
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.12f;
    // 대쉬 종료 직전 몇 프레임을 피격 가능 구간으로 둘지 정합니다.
    [SerializeField, Range(2, 3)] private int dashInvincibilityEndLeadFrames = 3;
    [SerializeField, Min(1f)] private float dashAnimationFrameRate = 60f;
    [SerializeField] private float dashEndSpeedMultiplier = 0.05f;
    [SerializeField] private float dashEndSmoothTime = 0.02f;
    [SerializeField] private float dashCooldown = 0.16f;
    [SerializeField] private float dashInputBufferTime = 0.25f;
    [SerializeField] private int maxDashCount = 2;
    [SerializeField] private bool canAirDash = true;

    [Header("Dash Recovery")]
    // 기본값은 후딜레이가 끝날 때까지 모든 행동을 막습니다.
    [SerializeField] private bool allowMoveDuringDashRecovery;
    [SerializeField] private bool allowJumpDuringDashRecovery;
    [SerializeField] private bool allowAttackDuringDashRecovery;
    [SerializeField] private bool allowConsecutiveDashDuringRecovery;

    [Header("Landing")]
    // 착지 중 물리는 그대로 두고 좌우 입력 적용만 잠깐 제한합니다.
    [SerializeField, Min(0f)] private float landingInputLockDuration = 0.05f;
    [SerializeField] private bool allowMoveDuringLanding;

    [Header("Physics")]
    // Rigidbody 보간을 켜서 이동이 덜 끊겨 보이게 합니다.
    [SerializeField] private bool useRigidbodyInterpolation = true;

    [Header("Soul State")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private bool disableBodyMovementInSoulState = true;

    [Header("Pass Platform")]
    // 아래+점프로 통과 가능한 발판 처리 값입니다.
    [SerializeField] private string passPlatformTag = "Pass";
    [SerializeField] private float passCheckRadius = 0.15f;
    [SerializeField] private float passDownVelocity = -6f;
    [SerializeField] private float passThroughMaxTime = 1.5f;

    private Rigidbody2D rb;
    private hys_Player_State playerState;
    private Collider2D[] playerColliders;
    private float moveInput;
    private float lastMoveDirection = 1f;
    private float jumpBufferCounter;
    private float coyoteCounter;
    private float dashBufferCounter;
    private float defaultGravityScale;
    private int jumpCount;
    private int dashCount;
    private int jumpVersion;
    private int downJumpVersion;
    private int dashVersion;
    private int landingVersion;
    private Coroutine dashRoutine;
    private Coroutine dashCooldownRoutine;
    private bool isDashCoolingDown;
    private bool isPassingThrough;
    private bool isDownJumping;
    private bool wasGrounded;
    private float landingEndTime;
    private float dashDirection = 1f;
    private readonly HashSet<Collider2D> ignoredPlatforms = new HashSet<Collider2D>();

    // 다른 스크립트가 대시/무적 여부를 확인할 수 있게 공개합니다.
    public bool Is_Dashing { get; private set; }
    public bool Is_DashEnding { get; private set; }
    public bool Is_Invincible { get; private set; }
    public bool Is_Landing { get; private set; }

    // 하단 발판을 완전히 빠져나올 때까지 Fall 애니메이션을 유지할 때 사용합니다.
    public bool Is_DownJumping => isDownJumping;

    // 공격 방향 계산에 사용하는 마지막 좌우 방향입니다.
    public float LastMoveDirection => lastMoveDirection;

    // 대쉬 시작 순간 저장한 방향으로, 대쉬 중 반대 입력이 들어와도 바뀌지 않습니다.
    public float DashDirection => dashDirection;

    // 달리기 애니메이션 속도를 실제 최고 이동 속도와 맞출 때 사용합니다.
    public float MoveSpeed => moveSpeed;

    // 애니메이션 전환에서 사용하는 현재 접지 여부입니다.
    public bool Is_Grounded => IsGrounded();

    // 같은 Jump 상태 안에서 2단 점프가 성공해도 애니메이션 트리거를 다시 보낼 수 있게 합니다.
    public int JumpVersion => jumpVersion;

    // 하단 점프가 성공한 순간 낙하 애니메이션으로 바로 전환하기 위한 버전 값입니다.
    public int DownJumpVersion => downJumpVersion;

    // 대쉬와 착지 시작 순간을 Animator Trigger로 한 번만 전달하기 위한 값입니다.
    public int DashVersion => dashVersion;
    public int LandingVersion => landingVersion;

    // 공격 스크립트가 대쉬 후딜레이 취소 허용 여부를 확인할 때 사용합니다.
    public bool CanAttackDuringDashRecovery =>
        Is_Dashing && Is_DashEnding && allowAttackDuringDashRecovery;

    private void Awake()
    {
        // 필요한 컴포넌트와 기본 중력 값을 캐싱합니다.
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<hys_Player_State>();
        soulSystem = soulSystem != null ? soulSystem : GetComponent<HWJ_SoulSystem>();
        playerColliders = GetComponents<Collider2D>();
        defaultGravityScale = rb.gravityScale;
        wasGrounded = IsGrounded();

        if (useRigidbodyInterpolation)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Update()
    {
        if (ShouldSkipBodyMovementForSoulState())
        {
            CancelBodyMovementStateForSoulState();
            ApplySoulGravityOverride();
            return;
        }

        TickLandingState();

        // 입력은 매 프레임 읽고, 실제 물리 이동은 FixedUpdate에서 처리합니다.
        ReadMoveInput();
        UpdateGroundState();
        HandleJumpInput();
        HandleDashInput();
        HandleDownPlatformInput();
    }

    private void FixedUpdate()
    {
        if (ShouldSkipBodyMovementForSoulState())
        {
            CancelBodyMovementStateForSoulState();
            ApplySoulGravityOverride();
            return;
        }

        // 점프/낙하 감각을 먼저 적용한 뒤 이동을 처리합니다.
        ApplyBetterFallGravity();
        TryPassUpThroughPlatform(FindPassPlatformAbove());

        if (Is_Dashing)
        {
            // 후딜레이 이동 허용 옵션을 켠 경우에만 좌우 입력을 적용합니다.
            if (Is_DashEnding && allowMoveDuringDashRecovery)
            {
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            }

            return;
        }

        if ((Is_Landing && !allowMoveDuringLanding) ||
            (playerState != null && !playerState.CanMove))
        {
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        UpdateMoveState();
    }

    private void ReadMoveInput()
    {
        // 현재는 좌우 방향키로만 이동합니다.
        moveInput = 0f;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput -= 1f;
            lastMoveDirection = -1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput += 1f;
            lastMoveDirection = 1f;
        }
    }

    private bool ShouldSkipBodyMovementForSoulState()
    {
        return disableBodyMovementInSoulState &&
            soulSystem != null &&
            (soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul ||
             soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul);
    }

    private void ApplySoulGravityOverride()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    private void ClearBodyMovementInput()
    {
        moveInput = 0f;
        jumpBufferCounter = 0f;
        dashBufferCounter = 0f;
    }

    private void CancelBodyMovementStateForSoulState()
    {
        ClearBodyMovementInput();

        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        if (dashCooldownRoutine != null)
        {
            StopCoroutine(dashCooldownRoutine);
            dashCooldownRoutine = null;
        }

        if (Is_Dashing)
        {
            Is_Dashing = false;
            Is_DashEnding = false;
            DisableDashInvincibility();
        }

        Is_Landing = false;
        isDashCoolingDown = false;
        rb.gravityScale = defaultGravityScale;
    }

    private void UpdateGroundState()
    {
        // 접지 중이면 점프/대시 횟수를 회복하고, 공중이면 코요테 타임을 줄입니다.
        bool isGrounded = IsGrounded() && !isDownJumping;
        bool landedThisFrame = !wasGrounded && isGrounded && rb.linearVelocity.y <= 0f;

        if (landedThisFrame && !Is_Dashing)
        {
            StartLandingDelay();
        }

        wasGrounded = isGrounded;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;

            if (!isDashCoolingDown)
            {
                dashCount = 0;
            }

            if (rb.linearVelocity.y <= 0f)
            {
                jumpCount = 0;
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void StartLandingDelay()
    {
        // 착지 애니메이션 시작 신호와 입력 잠금 시간만 기록하고 Rigidbody는 건드리지 않습니다.
        Is_Landing = true;
        landingEndTime = Time.time + landingInputLockDuration;
        landingVersion++;
        jumpBufferCounter = 0f;
    }

    private void TickLandingState()
    {
        if (Is_Landing && (Time.time >= landingEndTime || !IsGrounded()))
        {
            Is_Landing = false;
        }
    }

    private void HandleJumpInput()
    {
        // 점프 입력을 짧게 먼저 눌러도 착지 직후 점프되도록 버퍼를 둡니다.
        if (Keyboard.current == null)
        {
            jumpBufferCounter -= Time.deltaTime;
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !TryStartDownJump())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && TryJump())
        {
            jumpBufferCounter = 0f;
        }
    }

    private bool TryJump()
    {
        // 착지 딜레이와 기본 대쉬 구간에는 점프가 끼어들지 못합니다.
        if (Is_Landing)
        {
            return false;
        }

        if (Is_Dashing)
        {
            if (!Is_DashEnding || !allowJumpDuringDashRecovery)
            {
                return false;
            }

            InterruptDashRecovery(true);
        }

        if (playerState != null && !playerState.CanMove)
        {
            return false;
        }

        // 코요테 타임 덕분에 발판에서 살짝 벗어나도 첫 점프는 허용됩니다.
        bool canUseCoyoteJump = jumpCount == 0 && coyoteCounter > 0f;
        if (jumpCount >= maxJumpCount && !canUseCoyoteJump)
        {
            return false;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower * jumpStartBoost);
        coyoteCounter = 0f;
        jumpCount++;
        jumpVersion++;
        SetPlayerState(hys_PlayerState.Jump);
        return true;
    }

    private void HandleDownPlatformInput()
    {
        // 아래 방향키와 점프를 함께 누르면 Pass 태그 발판을 아래로 통과합니다.
        if (Keyboard.current == null || isPassingThrough)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryStartDownJump();
        }
    }

    private bool TryStartDownJump()
    {
        // 실제로 아래에 통과 가능한 발판이 있을 때만 충돌 무시 루틴을 시작합니다.
        if (Keyboard.current == null ||
            !Keyboard.current.downArrowKey.isPressed ||
            isPassingThrough ||
            Is_Dashing ||
            (playerState != null && !playerState.CanMove))
        {
            return false;
        }

        Collider2D platform = FindPassPlatformBelow();
        if (platform == null)
        {
            return false;
        }

        // 일반 점프의 상승 모션을 거치지 않고 Fall 상태로 보내기 위한 신호입니다.
        Is_Landing = false;
        downJumpVersion++;
        StartCoroutine(PassThroughPlatform(platform, true));
        return true;
    }

    private void HandleDashInput()
    {
        // D 키 대시 입력을 버퍼에 저장해서 살짝 일찍 눌러도 대시가 나가게 합니다.
        if (Keyboard.current == null)
        {
            dashBufferCounter -= Time.deltaTime;
            return;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            dashBufferCounter = dashInputBufferTime;
        }
        else
        {
            TickDashBuffer();
        }

        TryConsumeDashBuffer();
    }

    private void TickDashBuffer()
    {
        // 대시 입력 버퍼 시간을 줄입니다.
        if (dashBufferCounter > 0f)
        {
            dashBufferCounter -= Time.deltaTime;
        }
    }

    private void TryConsumeDashBuffer()
    {
        // 버퍼에 저장된 대시 입력이 있으면 실제 대시를 시도합니다.
        if (dashBufferCounter <= 0f)
        {
            return;
        }

        if (TryDash())
        {
            dashBufferCounter = 0f;
        }
    }

    private bool TryDash()
    {
        // 대시 중복, 쿨타임, 공중 대시 허용 여부를 검사합니다.
        bool isGrounded = IsGrounded();

        // 공격/피격/사망처럼 이동이 잠긴 상태에서는 대시가 끼어들지 못하게 합니다.
        if (playerState != null && !playerState.CanMove)
        {
            return false;
        }

        if (Is_Dashing)
        {
            if (!Is_DashEnding || !allowConsecutiveDashDuringRecovery)
            {
                return false;
            }

            InterruptDashRecovery(false);
        }

        if (isDashCoolingDown || dashCount >= maxDashCount || (!isGrounded && !canAirDash))
        {
            return false;
        }

        Is_Landing = false;
        dashRoutine = StartCoroutine(DashRoutine());
        return true;
    }

    private IEnumerator DashRoutine()
    {
        // 대쉬 시작 방향을 저장하고 고정 시간 동안 입력과 무관하게 같은 방향으로 이동합니다.
        dashCount++;
        dashVersion++;
        dashDirection = Mathf.Approximately(lastMoveDirection, 0f) ? 1f : Mathf.Sign(lastMoveDirection);
        Is_Dashing = true;
        Is_DashEnding = false;
        EnableDashInvincibility();
        SetPlayerState(hys_PlayerState.Dash);

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        float dashEndTime = Time.time + dashDuration;
        float invincibilityLeadTime = dashInvincibilityEndLeadFrames /
            Mathf.Max(1f, dashAnimationFrameRate);
        float invincibleEndTime = dashEndTime - invincibilityLeadTime;

        // 대쉬 포즈와 속도는 유지하고 종료 직전 2~3프레임에는 무적만 먼저 해제합니다.
        while (Time.time < dashEndTime)
        {
            if (Time.time >= invincibleEndTime)
            {
                DisableDashInvincibility();
            }

            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            yield return null;
        }

        DisableDashInvincibility();
        Is_DashEnding = true;
        rb.gravityScale = defaultGravityScale;

        // DashEnd 후딜레이 중 기본값은 감속만 허용하며, 옵션으로 이동을 열 수 있습니다.
        float endTime = Time.time + dashEndSmoothTime;
        float startSpeed = dashDirection * dashSpeed;
        float endSpeed = dashDirection * dashSpeed * dashEndSpeedMultiplier;

        while (Time.time < endTime)
        {
            if (!allowMoveDuringDashRecovery)
            {
                float t = 1f - ((endTime - Time.time) / Mathf.Max(0.01f, dashEndSmoothTime));
                float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, t);
                rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
            }

            yield return null;
        }

        if (!allowMoveDuringDashRecovery)
        {
            rb.linearVelocity = new Vector2(endSpeed, rb.linearVelocity.y);
        }

        Is_Dashing = false;
        Is_DashEnding = false;
        DisableDashInvincibility();
        dashRoutine = null;
        UpdateMoveState();
        BeginDashCooldown();
    }

    // Animation Event를 나중에 연결해도 같은 무적 값을 사용하도록 공개 메서드로 둡니다.
    public void EnableDashInvincibility()
    {
        Is_Invincible = true;
    }

    public void DisableDashInvincibility()
    {
        Is_Invincible = false;
    }

    // 공격 스크립트가 후딜 공격 허용 옵션을 사용할 때 대쉬 상태를 안전하게 끝냅니다.
    public bool TryConsumeDashRecoveryForAttack()
    {
        if (!CanAttackDuringDashRecovery)
        {
            return false;
        }

        InterruptDashRecovery(true);
        return true;
    }

    private void InterruptDashRecovery(bool startCooldown)
    {
        // 점프/공격/연속 대쉬 옵션이 후딜을 취소해도 중력과 무적을 반드시 복구합니다.
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        if (dashCooldownRoutine != null)
        {
            StopCoroutine(dashCooldownRoutine);
            dashCooldownRoutine = null;
        }

        rb.gravityScale = defaultGravityScale;
        Is_Dashing = false;
        Is_DashEnding = false;
        DisableDashInvincibility();

        if (startCooldown)
        {
            BeginDashCooldown();
        }
    }

    private void BeginDashCooldown()
    {
        if (dashCooldownRoutine == null)
        {
            dashCooldownRoutine = StartCoroutine(DashCooldownRoutine());
        }
    }

    private IEnumerator DashCooldownRoutine()
    {
        isDashCoolingDown = true;
        yield return new WaitForSeconds(dashCooldown);
        isDashCoolingDown = false;
        dashCooldownRoutine = null;
        TryConsumeDashBuffer();
    }

    private void ApplyBetterFallGravity()
    {
        // 상승, 최상단, 낙하 구간의 중력을 다르게 적용해서 점프 감각을 만듭니다.
        if (Is_Dashing)
        {
            return;
        }

        if (Mathf.Abs(rb.linearVelocity.y) < apexVelocityThreshold)
        {
            rb.gravityScale = defaultGravityScale * apexGravityMultiplier;

            if (Mathf.Abs(rb.linearVelocity.y) < apexSnapVelocityThreshold)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -apexSnapFallSpeed);
            }
        }
        else if (rb.linearVelocity.y > 0f)
        {
            rb.gravityScale = defaultGravityScale * risingGravityMultiplier;
        }
        else if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;

            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    private bool IsGrounded()
    {
        // 플레이어 발밑에 자기 자신이 아닌 콜라이더가 있으면 접지로 봅니다.
        Vector2 checkPosition = GetGroundCheckPosition();
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, 0.15f);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && !IsPlayerCollider(hit))
            {
                return true;
            }
        }

        return false;
    }

    private Collider2D FindPassPlatformBelow()
    {
        // 아래 방향 통과에 사용할 Pass 발판을 찾습니다.
        Vector2 checkPosition = GetGroundCheckPosition();
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, passCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (IsPassPlatform(hit))
            {
                return hit;
            }
        }

        RaycastHit2D[] rayHits = Physics2D.RaycastAll(checkPosition, Vector2.down, passCheckRadius * 2f);
        foreach (RaycastHit2D hit in rayHits)
        {
            if (IsPassPlatform(hit.collider))
            {
                return hit.collider;
            }
        }

        return null;
    }

    private Collider2D FindPassPlatformAbove()
    {
        // 위로 점프할 때 Pass 발판에 머리가 걸리지 않도록 위쪽 발판을 찾습니다.
        if (isPassingThrough || rb.linearVelocity.y <= 0f)
        {
            return null;
        }

        Bounds playerBounds = GetPlayerBounds();
        Vector2 checkCenter = new Vector2(playerBounds.center.x, playerBounds.max.y + passCheckRadius);
        Vector2 checkSize = new Vector2(playerBounds.size.x * 0.8f, passCheckRadius * 2f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);

        foreach (Collider2D hit in hits)
        {
            if (IsPassPlatform(hit))
            {
                return hit;
            }
        }

        return null;
    }

    private void TryPassUpThroughPlatform(Collider2D platform)
    {
        // 위로 지나가는 중인 Pass 발판 충돌을 잠깐 무시합니다.
        if (platform == null)
        {
            return;
        }

        StartCoroutine(PassThroughPlatform(platform, false));
    }

    private IEnumerator PassThroughPlatform(Collider2D platform, bool pushDown)
    {
        // 플레이어 콜라이더와 발판 충돌을 무시했다가 완전히 빠져나오면 복구합니다.
        isPassingThrough = true;
        SetIgnoreCollision(platform, true);

        if (pushDown)
        {
            isDownJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, passDownVelocity);
        }

        float endTime = Time.time + passThroughMaxTime;
        while (platform != null && Time.time < endTime && ShouldKeepPassingThrough(platform, pushDown))
        {
            yield return null;
        }

        if (platform != null)
        {
            SetIgnoreCollision(platform, false);
        }

        if (pushDown)
        {
            isDownJumping = false;
        }

        isPassingThrough = false;
    }

    private bool ShouldKeepPassingThrough(Collider2D platform, bool pushDown)
    {
        // 아직 발판 안쪽에 걸쳐 있으면 충돌 무시를 유지합니다.
        Bounds playerBounds = GetPlayerBounds();
        Bounds platformBounds = platform.bounds;

        if (pushDown)
        {
            return playerBounds.max.y > platformBounds.min.y - 0.05f;
        }

        return playerBounds.min.y < platformBounds.max.y + 0.05f;
    }

    private void SetIgnoreCollision(Collider2D platform, bool ignore)
    {
        // 플레이어가 여러 콜라이더를 가질 수 있어서 전부 처리합니다.
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null && platform != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platform, ignore);
            }
        }

        if (platform != null)
        {
            if (ignore)
            {
                ignoredPlatforms.Add(platform);
            }
            else
            {
                ignoredPlatforms.Remove(platform);
            }
        }
    }

    private bool IsPassPlatform(Collider2D target)
    {
        // Pass 태그 또는 PlatformEffector2D를 가진 One Way Platform을 통과 대상으로 봅니다.
        return target != null &&
            !IsPlayerCollider(target) &&
            (target.CompareTag(passPlatformTag) || target.GetComponent<PlatformEffector2D>() != null);
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 중에도 무적·중력·발판 충돌 무시가 남지 않게 모두 복구합니다.
        StopAllCoroutines();

        Collider2D[] platformsToRestore = new Collider2D[ignoredPlatforms.Count];
        ignoredPlatforms.CopyTo(platformsToRestore);
        foreach (Collider2D platform in platformsToRestore)
        {
            if (platform != null)
            {
                SetIgnoreCollision(platform, false);
            }
        }

        ignoredPlatforms.Clear();
        dashRoutine = null;
        dashCooldownRoutine = null;
        isDashCoolingDown = false;
        isPassingThrough = false;
        isDownJumping = false;
        Is_Dashing = false;
        Is_DashEnding = false;
        Is_Landing = false;
        DisableDashInvincibility();

        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    private bool IsPlayerCollider(Collider2D target)
    {
        // 충돌 검사 결과에서 플레이어 자신의 콜라이더를 제외하기 위한 체크입니다.
        EnsurePlayerColliders();

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (target == playerCollider)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateMoveState()
    {
        // 현재 속도와 접지 상태에 맞춰 플레이어 상태를 갱신합니다.
        if (playerState == null || !playerState.CanControl)
        {
            return;
        }

        if (!IsGrounded())
        {
            SetPlayerState(rb.linearVelocity.y > 0f ? hys_PlayerState.Jump : hys_PlayerState.Fall);
            return;
        }

        SetPlayerState(Mathf.Abs(moveInput) > 0.01f ? hys_PlayerState.Move : hys_PlayerState.Idle);
    }

    private void SetPlayerState(hys_PlayerState nextState)
    {
        // 상태 스크립트가 있을 때만 상태를 전달합니다.
        if (playerState != null)
        {
            playerState.SetState(nextState);
        }
    }

    private Bounds GetPlayerBounds()
    {
        // 여러 콜라이더를 하나의 플레이어 영역으로 합칩니다.
        EnsurePlayerColliders();

        Collider2D firstCollider = playerColliders.Length > 0 ? playerColliders[0] : null;
        if (firstCollider == null)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        Bounds bounds = firstCollider.bounds;
        for (int i = 1; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
            {
                bounds.Encapsulate(playerColliders[i].bounds);
            }
        }

        return bounds;
    }

    private Vector2 GetGroundCheckPosition()
    {
        // 발밑 접지 검사를 할 기준 위치입니다.
        EnsurePlayerColliders();

        Collider2D firstCollider = playerColliders.Length > 0 ? playerColliders[0] : null;
        if (firstCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = firstCollider.bounds;
        return new Vector2(bounds.center.x, bounds.min.y);
    }

    private void EnsurePlayerColliders()
    {
        // 다른 스크립트가 Awake 순서상 먼저 접지 여부를 물어도 안전하게 콜라이더를 준비합니다.
        if (playerColliders == null || playerColliders.Length == 0)
        {
            playerColliders = GetComponents<Collider2D>();
        }
    }
}
