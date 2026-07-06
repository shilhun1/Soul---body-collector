using System;

/// <summary>
/// 플레이어 육신 상태의 이동/점프/대쉬 조작 값을 관리합니다.
/// 이동 담당 스크립트가 PlayerTypeDataSO.Control을 통해 읽어 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ControlData
{
    public float acceleration;
    public float deceleration;
    public HWJ_NormalJumpData normalJump = new HWJ_NormalJumpData();
    public HWJ_DoubleJumpData doubleJump = new HWJ_DoubleJumpData();
    public float dashDistance = 9f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public int maxDashCount = 2;
    public float dashStepCooldown = 0.15f;
    public bool canAirDash = true;
    public float fallGravityMultiplier = 1.5f;
    public float dashInvincibleSeconds = 0.16f;
    public float dashRecoverySeconds;
    public bool canFreeFly;
}

[Serializable]
public class HWJ_NormalJumpData
{
    public float jumpPower = 12f;
    public float jumpBufferTime = 0.15f;
}

[Serializable]
public class HWJ_DoubleJumpData
{
    public bool canDoubleJump = true;
    public int maxDoubleJumpCount = 1;
    public float jumpPower = 11f;
}

/// <summary>
/// 플레이어 기본 공격과 공격 입력 규칙을 관리합니다.
/// PlayerAttackSystem이 사거리, 공격 간격, 입력 ID, 스킬 연결 ID를 읽어 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PlayerAttackData
{
    public float attackRange;
    public float attackIntervalSeconds = 0.3f;
    public float comboResetSeconds = 0.8f;
    public string basicAttackSkillActionId;
}

/// <summary>
/// 플레이어 상태 전환에 필요한 공통 규칙을 관리합니다.
/// PlayerStatusSystem, 이동, 공격, 피격 시스템이 조작 가능 여부와 상태 우선순위를 맞출 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PlayerStateData
{
    public HWJ_RuntimeState startState = HWJ_RuntimeState.Idle;
    public bool canMoveOnBodyState = true;
    public bool canAttackOnBodyState = true;
    public bool canMoveOnSoulState = true;
    public bool canAttackOnSoulState;
}

/// <summary>
/// 플레이어 카메라 추적 값을 관리합니다.
/// PlayerCameraFollowSystem이 따라갈 속도, 오프셋, 카메라 제한 사용 여부를 읽습니다.
/// </summary>
[Serializable]
public class HWJ_PlayerCameraData
{
    public float followSpeed = 8f;
    public float lookAheadDistance = 1.5f;
    public float verticalOffset = 1f;
    public bool clampToBounds;
    public UnityEngine.Vector2 minBounds;
    public UnityEngine.Vector2 maxBounds;
}

/// <summary>
/// 플레이어가 영혼 상태일 때의 규칙을 관리합니다.
/// SoulSystem과 영혼 이동 스크립트가 무적, 벽 통과, 자유 비행, 10초 데드라인에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SoulStateData
{
    public float possessionDeadlineSeconds = 10f;
    public float soulMoveSpeed = 6f;
    public bool isInvincible;
    public bool canPassThroughWalls = true;
    public bool canFreeFly = true;
    public bool hasHp = true;
    public bool blocksControlOnDeadline = true;
}

/// <summary>
/// 플레이어가 육신에 들어가 있을 때의 부패 수치를 관리합니다.
/// BodyDecaySystem이 틱 감소, 피격 패널티, 영혼 상태 전환 조건으로 사용합니다.
/// </summary>
[Serializable]
public class HWJ_BodyDecayData
{
    public float maxDecayValue = 120f;
    public float decayTickSeconds = 0.5f;
    public float decayAmountPerTick = 1f;
    public float hitDecayPenalty;
    public bool startDecayOnEnterBody = true;
    public bool enterSoulStateWhenEmpty = true;
}
