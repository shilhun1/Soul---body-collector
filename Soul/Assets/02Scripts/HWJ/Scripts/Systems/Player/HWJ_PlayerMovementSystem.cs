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
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private bool isGrounded;

    private int usedJumpCount;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 moveInput;

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

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (isGrounded)
        {
            usedJumpCount = 0;
        }

        if (Input.GetButtonDown("Jump"))
        {
            TryJump();
        }

        if (IsDashInputPressed())
        {
            TryDash();
        }
    }

    private void FixedUpdate()
    {
        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        if (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            MoveSoul(playerData);
            return;
        }

        MoveBody(playerData);
    }

    /// <summary>
    /// 외부 지면 판정 스크립트가 현재 접지 상태를 알려줄 때 사용합니다.
    /// 지면 판정 구현이 바뀌어도 이동 데이터 구조는 유지됩니다.
    /// </summary>
    public void SetGrounded(bool grounded)
    {
        isGrounded = grounded;
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

        if (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            return;
        }

        if (usedJumpCount >= playerData.Control.maxJumpCount)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.y = playerData.Control.jumpPower;
        body.linearVelocity = velocity;
        usedJumpCount++;
    }

    private void TryDash()
    {
        if (body == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
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

    private bool IsDashInputPressed()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Control != null)
        {
            return Input.GetKeyDown(playerData.Control.dashKey);
        }

        return Input.GetKeyDown(KeyCode.LeftShift);
    }
}
