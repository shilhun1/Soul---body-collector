using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class hys_Player_Movement : MonoBehaviour
{
    // 데이터 담당자와 맞춘 플레이어 조작 변수입니다.
    [SerializeField] private float Move_Speed = 6f;
    [SerializeField] private float Jump_Force = 12f;
    [SerializeField] private float Jump_Buffer_Time = 0.15f;
    [SerializeField] private float Dash_Speed = 15f;
    [SerializeField] private float Dash_Duration = 0.2f;
    [SerializeField] private float Dash_Cooldown = 0.2f;
    [SerializeField] private bool Can_Air_Dash = true;

    private Rigidbody2D rb;
    private Collider2D[] playerColliders;
    private float moveInput;
    private float lastMoveDirection = 1f;
    private float jumpBufferCounter;
    private int jumpCount;
    private int dashCount;
    private bool isDashCoolingDown;
    private bool queuedDash;
    private bool isPassingThrough;

    public bool Is_Dashing { get; private set; }
    public bool Is_Invincible { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerColliders = GetComponents<Collider2D>();
    }

    void Update()
    {
        ReadMoveInput();
        UpdateJumpState();
        HandleJumpInput();
        HandleDashInput();
        HandleDownPlatformInput();
    }

    void FixedUpdate()
    {
        TryPassUpThroughPlatform(FindPassPlatformAbove());

        if (Is_Dashing)
        {
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * Move_Speed, rb.linearVelocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryPassUpThroughPlatform(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryPassUpThroughPlatform(collision.collider);
    }

    private void ReadMoveInput()
    {
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

    private void UpdateJumpState()
    {
        if (!Is_Dashing && IsGrounded() && rb.linearVelocity.y <= 0f)
        {
            jumpCount = 0;

            if (!isDashCoolingDown)
            {
                dashCount = 0;
            }
        }
    }

    private void HandleJumpInput()
    {
        if (Keyboard.current == null)
        {
            jumpBufferCounter -= Time.deltaTime;
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !IsDownInput())
        {
            jumpBufferCounter = Jump_Buffer_Time;
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
        if (jumpCount >= 2)
        {
            return false;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Jump_Force);
        jumpCount++;
        return true;
    }

    private void HandleDashInput()
    {
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        {
            TryDash();
        }
    }

    private void TryDash()
    {
        bool isGrounded = IsGrounded();

        if (Is_Dashing)
        {
            if (dashCount < 2)
            {
                queuedDash = true;
            }

            return;
        }

        if (isDashCoolingDown || dashCount >= 2 || (!isGrounded && !Can_Air_Dash))
        {
            return;
        }

        StartCoroutine(DashRoutine(isGrounded, true));
    }

    private IEnumerator DashRoutine(bool startedOnGround, bool countDash)
    {
        if (countDash)
        {
            dashCount++;
        }

        Is_Dashing = true;
        Is_Invincible = true;
        queuedDash = false;

        float originalGravity = rb.gravityScale;
        float dashYVelocity = startedOnGround ? rb.linearVelocity.y : 0f;

        if (!startedOnGround)
        {
            rb.gravityScale = 0f;
        }

        rb.linearVelocity = new Vector2(lastMoveDirection * Dash_Speed, dashYVelocity);

        yield return new WaitForSeconds(Dash_Duration);

        rb.gravityScale = originalGravity;
        Is_Dashing = false;
        Is_Invincible = false;

        if (IsGrounded() && rb.linearVelocity.y <= 0f)
        {
            jumpCount = 0;
        }

        if (queuedDash && dashCount < 2)
        {
            StartCoroutine(DashRoutine(IsGrounded(), true));
            yield break;
        }

        isDashCoolingDown = true;
        yield return new WaitForSeconds(Dash_Cooldown);
        isDashCoolingDown = false;
    }

    private void HandleDownPlatformInput()
    {
        if (Keyboard.current == null || isPassingThrough)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsDownInput())
        {
            TryDownJump();
        }
    }

    private void TryDownJump()
    {
        Collider2D passPlatform = FindPassPlatformBelow();
        if (passPlatform == null)
        {
            return;
        }

        StartCoroutine(PassThroughPlatform(passPlatform, true));
    }

    private void TryPassUpThroughPlatform(Collider2D platform)
    {
        if (isPassingThrough || rb.linearVelocity.y <= 0f || !IsPassPlatform(platform))
        {
            return;
        }

        StartCoroutine(PassThroughPlatform(platform, false));
    }

    private Collider2D FindPassPlatformBelow()
    {
        Vector2 origin = GetDefaultGroundCheckPosition();
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(origin, 0.15f);

        foreach (Collider2D overlap in overlaps)
        {
            if (IsPassPlatform(overlap))
            {
                return overlap;
            }
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.3f);
        foreach (RaycastHit2D hit in hits)
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
        if (isPassingThrough || rb.linearVelocity.y <= 0f)
        {
            return null;
        }

        Bounds playerBounds = GetPlayerBounds();
        Vector2 origin = new Vector2(playerBounds.center.x, playerBounds.max.y);
        Vector2 size = new Vector2(playerBounds.size.x * 0.8f, 0.4f);
        Vector2 center = origin + Vector2.up * 0.2f;

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D overlap in overlaps)
        {
            if (IsPassPlatform(overlap))
            {
                return overlap;
            }
        }

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, Vector2.up, 0.4f);
        foreach (RaycastHit2D hit in hits)
        {
            if (IsPassPlatform(hit.collider))
            {
                return hit.collider;
            }
        }

        return null;
    }

    private IEnumerator PassThroughPlatform(Collider2D platform, bool pushDown)
    {
        isPassingThrough = true;
        SetIgnoreCollision(platform, true);

        if (pushDown)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -6f);
        }

        float endTime = Time.time + 1.5f;
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
        Bounds playerBounds = GetPlayerBounds();
        Bounds platformBounds = platform.bounds;

        if (pushDown)
        {
            return playerBounds.max.y > platformBounds.min.y - 0.05f;
        }

        if (playerBounds.min.y >= platformBounds.max.y + 0.05f)
        {
            return false;
        }

        return rb.linearVelocity.y > 0f;
    }

    private void SetIgnoreCollision(Collider2D platform, bool ignore)
    {
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null && platform != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platform, ignore);
            }
        }
    }

    private bool IsGrounded()
    {
        Vector2 checkPosition = GetDefaultGroundCheckPosition();
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

    private bool IsPassPlatform(Collider2D target)
    {
        return target != null && !IsPlayerCollider(target) && target.CompareTag("Pass");
    }

    private bool IsPlayerCollider(Collider2D target)
    {
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (target == playerCollider)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDownInput()
    {
        return Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
    }

    private Vector2 GetDefaultGroundCheckPosition()
    {
        Collider2D firstCollider = playerColliders.Length > 0 ? playerColliders[0] : null;
        if (firstCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = firstCollider.bounds;
        return new Vector2(bounds.center.x, bounds.min.y);
    }

    private Bounds GetPlayerBounds()
    {
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
}
