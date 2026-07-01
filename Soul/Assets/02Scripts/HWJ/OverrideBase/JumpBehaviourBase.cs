using UnityEngine; // MonoBehaviour, Rigidbody2D, Time, Mathf, Vector2를 사용하기 위해 필요합니다.

public abstract class JumpBehaviourBase : MonoBehaviour // 모든 점프 방식이 상속받는 부모 클래스입니다.
{
    protected Rigidbody2D rb; // 점프 힘을 적용할 Rigidbody2D입니다.
    protected JumpJsonData jumpData; // 현재 사용하는 점프 JSON 데이터입니다.
    protected int remainJumpCount; // 남은 점프 횟수입니다.
    protected float lastGroundedTime = -999f; // 마지막으로 바닥에 닿았던 시간입니다.
    protected float lastJumpPressedTime = -999f; // 마지막으로 점프 버튼을 누른 시간입니다.
    protected float baseGravityScale = 1f; // 원래 Rigidbody2D 중력 배율입니다.
    protected bool isGrounded; // 현재 바닥에 닿아 있는지 저장합니다.
    protected bool isJumpHeld; // 현재 점프 버튼을 누르고 있는지 저장합니다.

    public virtual void Init(Rigidbody2D rigidbody, JumpJsonData data) // 점프 행동을 초기화합니다.
    {
        rb = rigidbody; // 전달받은 Rigidbody2D를 저장합니다.
        jumpData = data; // 전달받은 점프 데이터를 저장합니다.
        remainJumpCount = data != null ? Mathf.Max(1, data.maxJumpCount) : 1; // 최소 1회 이상 점프할 수 있도록 점프 횟수를 설정합니다.
        baseGravityScale = rb != null ? rb.gravityScale : 1f; // Rigidbody2D의 원래 중력 배율을 저장합니다.
        lastGroundedTime = -999f; // 바닥 접촉 시간을 먼 과거로 초기화합니다.
        lastJumpPressedTime = -999f; // 점프 입력 시간을 먼 과거로 초기화합니다.
        isJumpHeld = false; // 점프 버튼 유지 상태를 초기화합니다.
    }

    public virtual void SetGrounded(bool grounded) // 바닥 접촉 상태를 전달받습니다.
    {
        isGrounded = grounded; // 현재 바닥 상태를 저장합니다.

        bool canRefreshJump = grounded && (rb == null || rb.linearVelocity.y <= 0.01f); // 위로 솟는 중에는 점프 횟수를 바로 회복하지 않도록 검사합니다.

        if (canRefreshJump) // 실제로 바닥에 안정적으로 닿아 있을 때만 실행합니다.
        {
            lastGroundedTime = Time.time; // 코요테 타임 기준 시간을 갱신합니다.
            remainJumpCount = jumpData != null ? Mathf.Max(1, jumpData.maxJumpCount) : 1; // 바닥에 닿으면 점프 횟수를 회복합니다.
        }
    }

    public virtual void SetJumpHeld(bool held) // 점프 버튼을 누르고 있는지 전달받습니다.
    {
        isJumpHeld = held; // 버튼 유지 상태를 저장합니다.
    }

    public virtual void PressJump() // 점프 버튼을 누르는 순간 호출합니다.
    {
        lastJumpPressedTime = Time.time; // 점프 버퍼가 사용할 입력 시간을 저장합니다.
    }

    public virtual void Tick() // 물리 프레임마다 점프 가능 여부와 중력을 처리합니다.
    {
        if (CanJumpByBuffer()) // 버퍼, 코요테 타임, 점프 횟수 조건을 모두 확인합니다.
        {
            ExecuteJump(); // 조건이 맞으면 실제 점프를 실행합니다.
        }

        ApplyFallGravity(); // 현재 세로 속도에 맞게 중력을 적용합니다.
    }

    protected virtual bool CanJumpByBuffer() // 현재 점프할 수 있는 상태인지 확인합니다.
    {
        if (jumpData == null) // 점프 데이터가 없으면 실행할 수 없습니다.
        {
            return false; // 점프 불가능을 반환합니다.
        }

        int maxJumpCount = Mathf.Max(1, jumpData.maxJumpCount); // JSON 값이 0이어도 최소 1회 점프로 보정합니다.
        bool buffered = Time.time - lastJumpPressedTime <= jumpData.jumpBufferTime; // 입력이 버퍼 시간 안에 들어왔는지 확인합니다.
        bool coyote = Time.time - lastGroundedTime <= jumpData.coyoteTime; // 바닥에서 떨어진 직후 허용 시간 안인지 확인합니다.
        bool hasJumpCount = remainJumpCount > 0; // 아직 사용할 점프 횟수가 남았는지 확인합니다.
        bool isAirJump = remainJumpCount < maxJumpCount; // 첫 점프 이후의 공중 점프인지 확인합니다.

        return buffered && hasJumpCount && (isGrounded || coyote || isAirJump); // 모든 조건을 조합해 점프 가능 여부를 반환합니다.
    }

    protected virtual void ExecuteJump() // 실제 점프 힘을 적용합니다.
    {
        if (rb == null || jumpData == null) // Rigidbody2D나 점프 데이터가 없으면 실행하지 않습니다.
        {
            return; // 안전하게 종료합니다.
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Unity 6 기준으로 세로 속도를 먼저 초기화합니다.
        rb.AddForce(Vector2.up * jumpData.jumpPower, ForceMode2D.Impulse); // 기존 방식처럼 순간 힘으로 점프합니다.

        remainJumpCount--; // 사용한 점프 횟수만큼 차감합니다.
        lastJumpPressedTime = -999f; // 같은 입력이 여러 번 소비되지 않도록 버퍼를 비웁니다.
        isGrounded = false; // 점프 직후에는 공중 상태로 처리합니다.
    }

    protected virtual void ApplyFallGravity() // 떨어질 때 중력을 더 강하게 적용합니다.
    {
        if (rb == null || jumpData == null) // Rigidbody2D나 점프 데이터가 없으면 실행하지 않습니다.
        {
            return; // 안전하게 종료합니다.
        }

        float fallMultiplier = GetPositiveOrDefault(jumpData.fallGravityMultiplier, 1.8f); // 낙하 중력 배율을 가져오고 없으면 기본값을 사용합니다.
        rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravityScale * fallMultiplier : baseGravityScale; // 내려갈 때만 중력을 강하게 적용합니다.
    }

    protected float GetPositiveOrDefault(float value, float fallback) // 0보다 큰 값만 사용하고 아니면 기본값을 반환합니다.
    {
        return value > 0f ? value : fallback; // JSON에 값이 없을 때도 안정적으로 작동하게 합니다.
    }
}
