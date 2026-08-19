using System;
using UnityEngine;

/// <summary>
/// 플레이어 육신 상태의 이동/점프/대쉬 조작 값을 관리합니다.
/// 이동 담당 스크립트가 PlayerTypeDataSO.Control을 통해 읽어 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ControlData
{
    [Header("이동감")]
    [InspectorName("지상 가속")]
    [Tooltip("땅에서 목표 속도까지 빨라지는 정도입니다.")]
    public float acceleration = 70f;
    [InspectorName("지상 감속")]
    [Tooltip("땅에서 입력을 멈췄을 때 느려지는 정도입니다.")]
    public float deceleration = 80f;
    [InspectorName("공중 가속")]
    [Tooltip("공중에서 목표 속도까지 빨라지는 정도입니다.")]
    public float airAcceleration = 45f;
    [InspectorName("공중 감속")]
    [Tooltip("공중에서 입력을 멈췄을 때 느려지는 정도입니다.")]
    public float airDeceleration = 35f;
    [InspectorName("코요테 타임")]
    [Tooltip("발판을 벗어난 직후에도 점프를 허용하는 시간입니다.")]
    public float coyoteTimeSeconds = 0.1f;
    [InspectorName("최대 낙하 속도")]
    [Tooltip("아래로 떨어질 때 제한할 최대 속도입니다.")]
    public float maxFallSpeed = 24f;
    [InspectorName("점프 컷 배율")]
    [Tooltip("점프 키를 빨리 뗐을 때 점프 높이를 줄이는 배율입니다.")]
    public float jumpCutMultiplier = 0.65f;
    [Header("점프")]
    [InspectorName("일반 점프")]
    [Tooltip("첫 번째 점프 설정입니다.")]
    public HWJ_NormalJumpData normalJump = new HWJ_NormalJumpData();
    [InspectorName("더블 점프")]
    [Tooltip("공중 추가 점프 설정입니다.")]
    public HWJ_DoubleJumpData doubleJump = new HWJ_DoubleJumpData();
    [Header("대쉬")]
    [InspectorName("대쉬 거리")]
    [Tooltip("대쉬로 이동할 기준 거리입니다.")]
    public float dashDistance = 9f;
    [InspectorName("대쉬 속도")]
    [Tooltip("대쉬 중 이동 속도입니다.")]
    public float dashSpeed = 15f;
    [InspectorName("대쉬 지속 시간")]
    [Tooltip("대쉬가 유지되는 시간입니다.")]
    public float dashDuration = 0.2f;
    [InspectorName("대쉬 쿨타임")]
    [Tooltip("대쉬 횟수가 회복되기까지 기다리는 시간입니다.")]
    public float dashCooldown = 1f;
    [InspectorName("최대 대쉬 횟수")]
    [Tooltip("한 번에 보유할 수 있는 대쉬 횟수입니다.")]
    public int maxDashCount = 2;
    [InspectorName("연속 대쉬 간격")]
    [Tooltip("대쉬를 연속으로 사용할 때 최소 간격입니다.")]
    public float dashStepCooldown = 0.15f;
    [InspectorName("공중 대쉬 가능")]
    [Tooltip("켜면 공중에서도 대쉬할 수 있습니다.")]
    public bool canAirDash = true;
    [Header("낙하/충돌 보정")]
    [InspectorName("낙하 중력 배율")]
    [Tooltip("떨어질 때 적용할 중력 배율입니다.")]
    public float fallGravityMultiplier = 1.5f;
    [InspectorName("대쉬 무적 시간")]
    [Tooltip("대쉬 중 무적을 부여할 시간입니다.")]
    public float dashInvincibleSeconds = 0.16f;
    [InspectorName("대쉬 후딜 시간")]
    [Tooltip("대쉬 후 다시 조작 가능한 상태까지 기다리는 시간입니다.")]
    public float dashRecoverySeconds;
    [InspectorName("단방향 발판 내려가기 시간")]
    [Tooltip("아래 방향과 점프 입력으로 발판을 내려갈 때 충돌을 무시하는 시간입니다.")]
    public float dropThroughSeconds = 0.25f;
    [InspectorName("벽 확인 거리")]
    [Tooltip("벽 끼임 방지를 위해 확인할 거리입니다.")]
    public float wallCheckDistance = 0.08f;
    [InspectorName("모서리 밀어내기 거리")]
    [Tooltip("모서리에 끼였을 때 살짝 밀어내는 거리입니다.")]
    public float cornerNudgeDistance = 0.08f;
    [InspectorName("자유 비행 가능")]
    [Tooltip("켜면 영혼처럼 자유 비행 이동을 사용할 수 있습니다.")]
    public bool canFreeFly;
}

[Serializable]
public class HWJ_NormalJumpData
{
    [InspectorName("점프 힘")]
    [Tooltip("일반 점프의 위쪽 힘입니다.")]
    public float jumpPower = 12f;
    [InspectorName("점프 입력 버퍼")]
    [Tooltip("착지 직전 입력한 점프를 잠시 기억하는 시간입니다.")]
    public float jumpBufferTime = 0.15f;
}

[Serializable]
public class HWJ_DoubleJumpData
{
    [InspectorName("더블 점프 가능")]
    [Tooltip("켜면 공중에서 추가 점프를 사용할 수 있습니다.")]
    public bool canDoubleJump = true;
    [InspectorName("더블 점프 횟수")]
    [Tooltip("공중에서 사용할 수 있는 추가 점프 횟수입니다.")]
    public int maxDoubleJumpCount = 1;
    [InspectorName("더블 점프 힘")]
    [Tooltip("더블 점프의 위쪽 힘입니다.")]
    public float jumpPower = 11f;
}

/// <summary>
/// 플레이어 기본 공격과 공격 입력 규칙을 관리합니다.
/// PlayerAttackSystem이 사거리, 공격 간격, 입력 ID, 스킬 연결 ID를 읽어 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PlayerAttackData
{
    [Header("기본 공격")]
    [InspectorName("공격 사거리")]
    [Tooltip("기본 공격이 닿는 거리입니다.")]
    public float attackRange;
    [InspectorName("공격 간격")]
    [Tooltip("기본 공격을 다시 사용할 수 있는 시간입니다.")]
    public float attackIntervalSeconds = 0.3f;
    [InspectorName("콤보 초기화 시간")]
    [Tooltip("이 시간 안에 다음 공격을 하지 않으면 콤보가 초기화됩니다.")]
    public float comboResetSeconds = 0.8f;
    [InspectorName("기본 공격 스킬 ID")]
    [Tooltip("기본 공격 입력 시 실행할 SkillActionDataSO ID입니다.")]
    public string basicAttackSkillActionId;
    [Header("공격 타이밍")]
    [InspectorName("판정 시작 시간")]
    [Tooltip("공격 시작 후 데미지 판정이 켜지는 시간입니다.")]
    public float hitStartSeconds = 0.08f;
    [InspectorName("판정 지속 시간")]
    [Tooltip("데미지 판정이 유지되는 시간입니다.")]
    public float hitActiveSeconds = 0.08f;
    [InspectorName("공격 후딜")]
    [Tooltip("공격 후 행동이 회복되기까지 걸리는 시간입니다.")]
    public float recoverySeconds = 0.18f;
    [InspectorName("이동 잠금 시간")]
    [Tooltip("공격 중 이동을 막는 시간입니다.")]
    public float movementLockSeconds = 0.18f;
    [InspectorName("대쉬 캔슬 가능 시점")]
    [Tooltip("공격 시작 후 이 시간이 지나면 대쉬 캔슬을 허용합니다.")]
    public float dashCancelStartSeconds = 0.12f;
    [InspectorName("대쉬 캔슬 가능")]
    [Tooltip("켜면 공격 중 일정 시점 이후 대쉬로 캔슬할 수 있습니다.")]
    public bool canDashCancel = true;
}

/// <summary>
/// 플레이어 상태 전환에 필요한 공통 규칙을 관리합니다.
/// PlayerStatusSystem, 이동, 공격, 피격 시스템이 조작 가능 여부와 상태 우선순위를 맞출 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PlayerStateData
{
    [Header("상태 허용")]
    [InspectorName("시작 상태")]
    [Tooltip("게임 시작 시 플레이어의 런타임 상태입니다.")]
    public HWJ_RuntimeState startState = HWJ_RuntimeState.Idle;
    [InspectorName("육신 상태 이동 가능")]
    [Tooltip("켜면 빙의한 몸 상태에서 이동할 수 있습니다.")]
    public bool canMoveOnBodyState = true;
    [InspectorName("육신 상태 공격 가능")]
    [Tooltip("켜면 빙의한 몸 상태에서 공격할 수 있습니다.")]
    public bool canAttackOnBodyState = true;
    [InspectorName("영혼 상태 이동 가능")]
    [Tooltip("켜면 영혼 상태에서 이동할 수 있습니다.")]
    public bool canMoveOnSoulState = true;
    [InspectorName("영혼 상태 공격 가능")]
    [Tooltip("켜면 영혼 상태에서 공격할 수 있습니다. 현재 기획은 보통 꺼둡니다.")]
    public bool canAttackOnSoulState;
}

/// <summary>
/// 플레이어 카메라 추적 값을 관리합니다.
/// PlayerCameraFollowSystem이 따라갈 속도, 오프셋, 카메라 제한 사용 여부를 읽습니다.
/// </summary>
[Serializable]
public class HWJ_PlayerCameraData
{
    [Header("카메라")]
    [InspectorName("추적 속도")]
    [Tooltip("카메라가 플레이어를 따라가는 속도입니다.")]
    public float followSpeed = 8f;
    [InspectorName("진행 방향 앞보기 거리")]
    [Tooltip("플레이어가 보는 방향으로 카메라를 얼마나 앞서 둘지 정합니다.")]
    public float lookAheadDistance = 1.5f;
    [InspectorName("세로 오프셋")]
    [Tooltip("플레이어 기준 카메라의 세로 위치 보정입니다.")]
    public float verticalOffset = 1f;
    [InspectorName("카메라 범위 제한")]
    [Tooltip("켜면 min/max 범위 밖으로 카메라가 나가지 않습니다.")]
    public bool clampToBounds;
    [InspectorName("최소 범위")]
    [Tooltip("카메라가 이동할 수 있는 최소 좌표입니다.")]
    public UnityEngine.Vector2 minBounds;
    [InspectorName("최대 범위")]
    [Tooltip("카메라가 이동할 수 있는 최대 좌표입니다.")]
    public UnityEngine.Vector2 maxBounds;
}

/// <summary>
/// 플레이어가 영혼 상태일 때의 규칙을 관리합니다.
/// SoulSystem과 영혼 이동 스크립트가 무적, 벽 통과, 자유 비행, 10초 데드라인에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SoulStateData
{
    [Header("영혼 상태")]
    [InspectorName("영혼으로 시작")]
    [Tooltip("켜면 게임 시작 시 몸이 아닌 영혼 상태로 시작합니다.")]
    public bool startAsSoul = true;
    [InspectorName("시작 즉시 데드라인 작동")]
    [Tooltip("켜면 영혼 상태 시작과 동시에 제한 시간이 흐릅니다.")]
    public bool startSoulDeadlineImmediately = true;
    [InspectorName("빙의 제한 시간")]
    [Tooltip("영혼 상태에서 새 몸에 빙의해야 하는 제한 시간입니다.")]
    public float possessionDeadlineSeconds = 10f;
    [InspectorName("영혼 이동 속도")]
    [Tooltip("영혼 상태에서의 이동 속도입니다.")]
    public float soulMoveSpeed = 6f;
    [InspectorName("무적")]
    [Tooltip("켜면 영혼 상태에서 피해를 받지 않습니다.")]
    public bool isInvincible;
    [InspectorName("벽 통과 가능")]
    [Tooltip("켜면 영혼 상태에서 벽 충돌을 무시할 수 있습니다.")]
    public bool canPassThroughWalls = true;
    [InspectorName("자유 비행 가능")]
    [Tooltip("켜면 영혼 상태에서 상하좌우 자유 이동을 사용합니다.")]
    public bool canFreeFly = true;
    [InspectorName("HP 사용")]
    [Tooltip("켜면 영혼 상태도 HP를 가질 수 있습니다.")]
    public bool hasHp = true;
    [InspectorName("제한 시간 종료 시 조작 차단")]
    [Tooltip("켜면 영혼 제한 시간이 끝났을 때 조작을 막습니다.")]
    public bool blocksControlOnDeadline = true;
}

/// <summary>
/// 플레이어가 육신에 들어가 있을 때의 빙의체 정신력 소모 단계를 관리합니다.
/// 기존 저장/직렬화 이름은 BodyDecay를 유지하지만, 실제 기획 의미는 PossessionMental입니다.
/// </summary>
public enum HWJ_DecayDangerLevel
{
    Stable,
    Warning,
    Dangerous,
    Critical,
    Collapsed
}

[Serializable]
public class HWJ_DecayDangerThresholdData
{
    [InspectorName("위험 단계")]
    [Tooltip("이 비율에 도달했을 때 표시할 시체 부패 위험 단계입니다.")]
    public HWJ_DecayDangerLevel dangerLevel = HWJ_DecayDangerLevel.Warning;
    [InspectorName("부패 진행 비율")]
    [Tooltip("0~1 사이 값입니다. 0.5는 시체 부패가 절반 진행된 상태를 의미합니다.")]
    public float ratio = 0.5f;
}

[Serializable]
public class HWJ_BodyDecayData
{
    [Header("시체 부패 표시")]
    [InspectorName("표시 이름")]
    [Tooltip("시체에 빙의했을 때 UI나 디버그에 보여줄 부패 자원 이름입니다.")]
    public string resourceDisplayName = "시체 부패";

    [Header("시체 부패 기본값")]
    [InspectorName("초기 부패도")]
    [Tooltip("시체에 들어갔을 때 시작할 부패도입니다. 보통 0으로 둡니다.")]
    public float initialDecayValue;
    [InspectorName("최대 부패도")]
    [Tooltip("이 값까지 부패하면 시체가 붕괴하고 빙의가 해제됩니다.")]
    public float maxDecayValue = 120f;
    [InspectorName("부패 틱 시간")]
    [Tooltip("시간 기반 부패가 적용되는 간격입니다.")]
    public float decayTickSeconds = 0.5f;
    [InspectorName("틱당 부패 증가량")]
    [Tooltip("부패 틱마다 증가하는 기본 부패도입니다.")]
    public float decayAmountPerTick = 1f;
    [Header("행동별 부패 증가")]
    [InspectorName("이동 초당 부패 증가량")]
    [Tooltip("이동 중 매초 추가되는 부패도입니다.")]
    public float moveDecayPerSecond;
    [InspectorName("기본 공격 부패 증가량")]
    [Tooltip("기본 공격을 사용할 때 추가되는 부패도입니다.")]
    public float basicAttackDecayAmount;
    [InspectorName("스킬 사용 부패 증가량")]
    [Tooltip("스킬을 사용할 때 추가되는 부패도입니다.")]
    public float skillDecayAmount;
    [InspectorName("특정 행동 부패 증가량")]
    [Tooltip("기타 행동에 공통으로 추가할 부패도입니다.")]
    public float actionDecayAmount;
    [InspectorName("미사용 - 피격 부패 증가량")]
    [Tooltip("현재 기획에서는 피격 시 HP만 감소합니다. 기존 데이터 호환용으로 남아 있으며 실제 피격 처리에서는 사용하지 않습니다.")]
    public float hitDecayPenalty;
    [InspectorName("부패 저항력")]
    [Tooltip("적용되는 부패 증가량을 줄이는 0~1 값입니다.")]
    public float decayResistance;

    [Header("시체 HP 감소")]
    [InspectorName("부패량만큼 HP 감소")]
    [Tooltip("켜면 최종 부패 증가량과 같은 수치만큼 현재 빙의체 HP도 감소합니다. 생체 빙의에는 적용되지 않습니다.")]
    public bool reduceBodyHpWithDecay;

    [Header("부패 상태 전환")]
    [InspectorName("시체 진입 시 부패 시작")]
    [Tooltip("켜면 시체에 빙의한 순간부터 부패가 진행됩니다.")]
    public bool startDecayOnEnterBody = true;
    [InspectorName("최대 부패 시 영혼 전환")]
    [Tooltip("켜면 부패도가 최대치에 도달했을 때 시체가 붕괴하고 영혼 상태로 돌아갑니다.")]
    public bool enterSoulStateWhenMaxed = true;
    [InspectorName("구형 0 이하 전환 호환")]
    [Tooltip("구형 감소식과 호환하기 위한 옵션입니다. 현재는 최대 소모 방식을 우선 사용합니다.")]
    public bool enterSoulStateWhenEmpty = true;

    [InspectorName("시체 붕괴 메시지")]
    [Tooltip("부패 또는 HP 소진으로 시체가 붕괴했을 때 로그/대사/UI에 사용할 문장입니다.")]
    public string depletedMessage = "이 몸은 더 이상 못 쓰겠다.";
    [InspectorName("부패 위험 단계")]
    [Tooltip("부패 진행 비율별 Warning/Dangerous/Critical 단계를 설정합니다.")]
    public HWJ_DecayDangerThresholdData[] dangerThresholds =
    {
        new HWJ_DecayDangerThresholdData
        {
            dangerLevel = HWJ_DecayDangerLevel.Warning,
            ratio = 0.5f
        },
        new HWJ_DecayDangerThresholdData
        {
            dangerLevel = HWJ_DecayDangerLevel.Dangerous,
            ratio = 0.75f
        },
        new HWJ_DecayDangerThresholdData
        {
            dangerLevel = HWJ_DecayDangerLevel.Critical,
            ratio = 0.9f
        }
    };
}
