using System.Collections;
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
    [SerializeField] private float dashEndSpeedMultiplier = 0.05f;
    [SerializeField] private float dashEndSmoothTime = 0.02f;
    [SerializeField] private float dashCooldown = 0.16f;
    [SerializeField] private float dashInputBufferTime = 0.25f;
    [SerializeField] private int maxDashCount = 2;
    [SerializeField] private bool canAirDash = true;

    [Header("Physics")]
    // Rigidbody 보간을 켜서 이동이 덜 끊겨 보이게 합니다.
    [SerializeField] private bool useRigidbodyInterpolation = true;

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
    private bool isDashCoolingDown;
    private bool isPassingThrough;

    // 다른 스크립트가 대시/무적 여부를 확인할 수 있게 공개합니다.
    public bool Is_Dashing { get; private set; }
    public bool Is_Invincible { get; private set; }

    // 공격 방향 계산에 사용하는 마지막 좌우 방향입니다.
    public float LastMoveDirection => lastMoveDirection;

    private void Awake()
    {
        // 필요한 컴포넌트와 기본 중력 값을 캐싱합니다.
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<hys_Player_State>();
        playerColliders = GetComponents<Collider2D>();
        defaultGravityScale = rb.gravityScale;

        if (useRigidbodyInterpolation)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Update()
    {
        // 입력은 매 프레임 읽고, 실제 물리 이동은 FixedUpdate에서 처리합니다.
        ReadMoveInput();
        UpdateGroundState();
        HandleJumpInput();
        HandleDashInput();
        HandleDownPlatformInput();
    }

    private void FixedUpdate()
    {
        // 점프/낙하 감각을 먼저 적용한 뒤 이동을 처리합니다.
        ApplyBetterFallGravity();
        TryPassUpThroughPlatform(FindPassPlatformAbove());

        if (Is_Dashing || (playerState != null && !playerState.CanMove))
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

    private void UpdateGroundState()
    {
        // 접지 중이면 점프/대시 횟수를 회복하고, 공중이면 코요테 타임을 줄입니다.
        if (IsGrounded())
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
        // 코요테 타임 덕분에 발판에서 살짝 벗어나도 첫 점프는 허용됩니다.
        bool canUseCoyoteJump = jumpCount == 0 && coyoteCounter > 0f;
        if (jumpCount >= maxJumpCount && !canUseCoyoteJump)
        {
            return false;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower * jumpStartBoost);
        coyoteCounter = 0f;
        jumpCount++;
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
        if (Keyboard.current == null || !Keyboard.current.downArrowKey.isPressed || isPassingThrough)
        {
            return false;
        }

        Collider2D platform = FindPassPlatformBelow();
        if (platform == null)
        {
            return false;
        }

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

        if (Is_Dashing)
        {
            return false;
        }

        if (isDashCoolingDown || dashCount >= maxDashCount || (!isGrounded && !canAirDash))
        {
            return false;
        }

        StartCoroutine(DashRoutine());
        return true;
    }

    private IEnumerator DashRoutine()
    {
        // 대시 중에는 중력을 끄고, 짧은 시간 동안 가로 속도를 강하게 줍니다.
        dashCount++;
        Is_Dashing = true;
        Is_Invincible = true;
        SetPlayerState(hys_PlayerState.Dash);

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(lastMoveDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        float endTime = Time.time + dashEndSmoothTime;
        float startSpeed = lastMoveDirection * dashSpeed;
        float endSpeed = lastMoveDirection * dashSpeed * dashEndSpeedMultiplier;

        while (Time.time < endTime)
        {
            float t = 1f - ((endTime - Time.time) / Mathf.Max(0.01f, dashEndSmoothTime));
            float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, t);
            rb.linearVelocity = new Vector2(currentSpeed, 0f);
            yield return null;
        }

        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(endSpeed, rb.linearVelocity.y);
        Is_Dashing = false;
        Is_Invincible = false;
        UpdateMoveState();

        if (dashBufferCounter > 0f && dashCount < maxDashCount)
        {
            dashBufferCounter = 0f;
            StartCoroutine(DashRoutine());
            yield break;
        }

        isDashCoolingDown = true;
        yield return new WaitForSeconds(dashCooldown);
        isDashCoolingDown = false;
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
    }

    private bool IsPassPlatform(Collider2D target)
    {
        // 자기 자신이 아니고 지정 태그를 가진 발판만 통과 발판으로 봅니다.
        return target != null &&
            !IsPlayerCollider(target) &&
            target.CompareTag(passPlatformTag);
    }

    private bool IsPlayerCollider(Collider2D target)
    {
        // 충돌 검사 결과에서 플레이어 자신의 콜라이더를 제외하기 위한 체크입니다.
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
        Collider2D firstCollider = playerColliders.Length > 0 ? playerColliders[0] : null;
        if (firstCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = firstCollider.bounds;
        return new Vector2(bounds.center.x, bounds.min.y);
    }
}
