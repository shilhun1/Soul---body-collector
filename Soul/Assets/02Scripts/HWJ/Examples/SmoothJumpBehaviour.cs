using UnityEngine; // Vector2, Mathf를 사용하기 위해 필요합니다.

public class SmoothJumpBehaviour : JumpBehaviourBase // 스컬 같은 액션 게임 느낌을 목표로 한 부드러운 점프 행동입니다.
{
    private bool hasCutJump; // 점프 버튼을 떼서 상승 속도를 이미 줄였는지 저장합니다.

    public override void SetGrounded(bool grounded) // 바닥 접촉 상태를 전달받습니다.
    {
        base.SetGrounded(grounded); // 부모 클래스의 바닥 처리와 점프 횟수 회복을 먼저 실행합니다.

        if (grounded && rb != null && rb.linearVelocity.y <= 0.01f) // 안정적으로 바닥에 닿아 있을 때만 실행합니다.
        {
            hasCutJump = false; // 다음 점프에서 다시 짧은 점프 처리가 가능하게 초기화합니다.
        }
    }

    protected override void ExecuteJump() // 실제 점프를 실행합니다.
    {
        if (rb == null || jumpData == null) // Rigidbody2D나 점프 데이터가 없으면 실행할 수 없습니다.
        {
            return; // 안전하게 종료합니다.
        }

        rb.gravityScale = baseGravityScale; // 점프 시작 순간에는 중력을 기본값으로 되돌립니다.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpData.jumpPower); // Unity 6 기준 linearVelocity로 위쪽 속도를 직접 설정합니다.

        remainJumpCount--; // 점프를 한 번 사용했으므로 남은 횟수를 줄입니다.
        lastJumpPressedTime = -999f; // 점프 버퍼가 같은 입력을 반복 사용하지 못하게 비웁니다.
        isGrounded = false; // 점프 직후에는 공중 상태로 처리합니다.
        hasCutJump = false; // 새 점프가 시작되었으므로 점프 컷 상태를 초기화합니다.
    }

    protected override void ApplyFallGravity() // 상승, 짧은 점프, 정점, 낙하 상태에 따라 중력을 다르게 적용합니다.
    {
        if (rb == null || jumpData == null) // Rigidbody2D나 점프 데이터가 없으면 실행하지 않습니다.
        {
            return; // 안전하게 종료합니다.
        }

        Vector2 velocity = rb.linearVelocity; // 현재 속도를 한 번 가져와서 계산에 사용합니다.
        float jumpCutMultiplier = GetPositiveOrDefault(jumpData.jumpCutMultiplier, 0.55f); // 버튼을 뗐을 때 상승 속도를 줄일 비율입니다.
        float lowJumpMultiplier = GetPositiveOrDefault(jumpData.lowJumpGravityMultiplier, 2.8f); // 짧은 점프용 중력 배율입니다.
        float fallMultiplier = GetPositiveOrDefault(jumpData.fallGravityMultiplier, 2.3f); // 낙하 중력 배율입니다.
        float apexMultiplier = GetPositiveOrDefault(jumpData.apexGravityMultiplier, 0.8f); // 점프 정점 근처 중력 배율입니다.
        float apexThreshold = GetPositiveOrDefault(jumpData.apexVelocityThreshold, 1.0f); // 정점 근처라고 볼 세로 속도 범위입니다.
        float maxFallSpeed = GetPositiveOrDefault(jumpData.maxFallSpeed, 18f); // 최대 낙하 속도입니다.

        if (!isJumpHeld && !hasCutJump && velocity.y > 0f) // 상승 중 점프 버튼을 떼면 짧은 점프를 만듭니다.
        {
            velocity.y *= jumpCutMultiplier; // 현재 상승 속도를 줄여 낮게 뛰게 합니다.
            rb.linearVelocity = velocity; // 줄어든 속도를 Rigidbody2D에 다시 적용합니다.
            hasCutJump = true; // 같은 점프에서 여러 번 컷되지 않도록 표시합니다.
        }

        if (rb.linearVelocity.y < -maxFallSpeed) // 낙하 속도가 너무 빠른지 확인합니다.
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed); // 최대 낙하 속도로 제한합니다.
        }

        float gravityMultiplier = 1f; // 기본 중력 배율은 1입니다.

        if (Mathf.Abs(rb.linearVelocity.y) <= apexThreshold) // 세로 속도가 거의 0이면 점프 정점 근처입니다.
        {
            gravityMultiplier = apexMultiplier; // 정점 근처에서는 중력을 살짝 낮춰 부드럽게 만듭니다.
        }
        else if (rb.linearVelocity.y < 0f) // 아래로 떨어지는 중인지 확인합니다.
        {
            gravityMultiplier = fallMultiplier; // 떨어질 때는 중력을 강하게 줘서 조작감을 선명하게 만듭니다.
        }
        else if (!isJumpHeld) // 상승 중이지만 버튼을 유지하지 않는지 확인합니다.
        {
            gravityMultiplier = lowJumpMultiplier; // 버튼을 짧게 누르면 더 빨리 상승을 줄입니다.
        }

        rb.gravityScale = baseGravityScale * gravityMultiplier; // 계산된 중력 배율을 Rigidbody2D에 적용합니다.
    }
}
