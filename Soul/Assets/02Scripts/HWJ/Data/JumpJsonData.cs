using System; // Serializable 속성을 사용하기 위해 필요합니다.

[Serializable] // 이 클래스가 JSON 데이터로 변환될 수 있게 표시합니다.
public class JumpJsonData // 점프에 필요한 수치를 담는 데이터 클래스입니다.
{
    public string id; // 점프 데이터의 고유 ID입니다.
    public float jumpPower; // 위로 튀어 오르는 점프 속도입니다.
    public int maxJumpCount; // 공중 점프까지 포함한 최대 점프 횟수입니다.
    public float coyoteTime; // 발판에서 떨어진 직후에도 점프를 허용하는 시간입니다.
    public float jumpBufferTime; // 점프 버튼을 조금 일찍 눌러도 입력을 기억하는 시간입니다.
    public float fallGravityMultiplier; // 떨어질 때 적용할 중력 배율입니다.
    public float lowJumpGravityMultiplier; // 버튼을 짧게 눌렀을 때 낮은 점프를 만들 중력 배율입니다.
    public float jumpCutMultiplier; // 점프 버튼을 떼는 순간 상승 속도를 줄이는 비율입니다.
    public float apexGravityMultiplier; // 점프 정점 근처에서 살짝 부드럽게 만드는 중력 배율입니다.
    public float apexVelocityThreshold; // 정점 근처라고 판단할 세로 속도 범위입니다.
    public float maxFallSpeed; // 너무 빠르게 떨어지지 않도록 제한하는 최대 낙하 속도입니다.
    public string behaviourId; // JumpBehaviourFactory가 어떤 점프 스크립트를 붙일지 판단하는 ID입니다.
}
