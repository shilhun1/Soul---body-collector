using UnityEngine;

public class JumpBehaviourBase : MonoBehaviour // 점프 행동 클래스의 부모
{
    protected Rigidbody2D rb; // 플레이어 rigidbody 2d임
    protected JumpJsonData jumpData; // 현재 점프 데이터
    protected int remainJumpCount; // 남은 점프 횟수
    protected float lastGroundedTime; // 마지막으로 땅에 닿은 시간
    protected float lastJumpPressedTime; // 마지막으로 점프 버튼을 누른 시간
    protected float baseGravityScale; // 원래 중력 배율

    public virtual void Init(Rigidbody2D rigidbody, JumpJsonData data) // 점프 행동 초기화
    {
        rb = rigidbody; // rigidbody2d 저장
        jumpData = data; // 점프 데이터 저장
        remainJumpCount = data != null ? data.maxJumpCount : 1; // 남은 점프 횟수 설정
        baseGravityScale = rb != null ? rb.gravityScale : 1f; // 기본 중력 배율 저장
        lastGroundedTime = -999f; // 초기 땅 접촉 시간을 먼 과거로 설정
        lastJumpPressedTime = -999f; // 초기 점프 입력 시간을 먼 과거로 설정
    }

    public virtual void OnGrounded() // 땅에 닿아 있을 때 호출
    {
        lastGroundedTime = Time.time; // 현재 시간을 땅 접촉 시간으로 저장
        remainJumpCount = jumpData != null ? jumpData.maxJumpCount : 1; // 점프 횟수를 회복
    }

    public virtual void PressJump() // 점프 입력이 들어왔을 때 호출
    {
        lastJumpPressedTime = Time.time; // 현재 시간을 점프 입력 시간으로 저장
    }

    public virtual void Tick() // 매 프레임점프 로직 처리
    {
        // 점프 버퍼는 “조금 일찍 누른 점프 입력을 잠깐 기억해두는 것”// 코요테 타임은 “발판에서 살짝 떨어진 직후에도 점프를 허용하는 것”
        if (CanJumpByBuffer()) // 버퍼와 코요테 타임 조건으로 점프 가능한지 확인
        {
            ExcuteJump(); // 실제 점프 실행
        }

        ApplyFallGravity(); // 낙하 중력 배율 적용
    }

    protected virtual bool CanJumpByBuffer() // 점프 가능한지 판단
    {
        if (jumpData == null) // 점프 데이터가 없는지 확인
        {
            return false; // 데이터가 없으면 점프 불가
        }

        bool buffered = Time.time - lastJumpPressedTime <= jumpData.jumpBufferTime; // 점프 입력 버퍼가 유효한지 확인
        bool coyote = Time.time - lastGroundedTime <= jumpData.coyoteTime; // 코요테 타임이 유효한지 확인
        bool hasJumpCount = remainJumpCount > 0; // 남은 점프 횟수가 있는지 확인
        bool isAirJump = remainJumpCount < jumpData.maxJumpCount; // 공중 점프 상태인지 확인

        return buffered && hasJumpCount && (coyote || isAirJump); // 모든 조건을 종합해서 반환
    }

    protected virtual void ExcuteJump() // 실제 점프 힘을 적용
    {
        if (rb == null || jumpData == null) // rigidbody나 점프 데이터가 없는지 확인
        {
            return; // 실행하지 않고 종료
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // 기존 수직 속도를 초기화
        rb.AddForce(Vector2.up * jumpData.jumpPower, ForceMode2D.Impulse);  // 위쪽으로 순간 힘을 줌
        remainJumpCount--; // 남은 점프 횟수 줄임
        lastJumpPressedTime = -999f; // 점프 입력 버퍼를 소비
    }

    protected virtual void ApplyFallGravity() // 낙하 중 중력 배율을 적용
    {
        if (rb == null || jumpData == null) // Rigidbody나 점프 데이터가 없는지 확인
        {
            return; // 실행하지 않고 종료
        }

        if (rb.linearVelocity.y < 0f) // 아래로 떨어지고 있는지 확인
        {
            rb.gravityScale = baseGravityScale * jumpData.fallGravityMultiplier; // 낙하 중력 배율을 적용.
        }
        else // 상승 중이거나 멈춰 있을 때 실행
        {
            rb.gravityScale = baseGravityScale; // 기본 중력으로 복구
        }
    }
}