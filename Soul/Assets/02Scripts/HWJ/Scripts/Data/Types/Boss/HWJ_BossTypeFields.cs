using System;
using UnityEngine;

[Serializable]
public class HWJ_BossFSMData
{
    [Header("보스 이동 정책")]
    [InspectorName("평상시 이동 방식")]
    [Tooltip("추적형 보스는 플레이어에게 접근하고, 고정형 시전자 보스는 제자리에서 방 전체를 대상으로 패턴을 사용합니다.")]
    public HWJ_BossLocomotionMode locomotionMode = HWJ_BossLocomotionMode.Chase;

    [Header("보스전 시작")]
    [InspectorName("플레이어 입장 시 자동 시작")]
    [Tooltip("켜면 플레이어가 보스방에 들어왔을 때 보스전을 시작합니다.")]
    public bool autoStartWhenPlayerEntersRoom = true;
    [InspectorName("보스방 범위 사용")]
    [Tooltip("켜면 지정한 보스방 범위 안에서 플레이어를 인식합니다.")]
    public bool useBossRoomBounds = true;
    [InspectorName("보스방 중심 오프셋")]
    [Tooltip("보스 위치 기준 보스방 중심 보정값입니다.")]
    public Vector2 bossRoomOffset;
    [InspectorName("보스방 크기")]
    [Tooltip("보스가 플레이어를 계속 인식하는 방 크기입니다.")]
    public Vector2 bossRoomSize = new Vector2(28f, 14f);
    [Header("거리와 추적")]
    [InspectorName("공격 시작 거리")]
    [Tooltip("이 거리 안에 들어오면 보스가 공격 준비를 할 수 있습니다.")]
    public float attackStartRange = 5f;
    [InspectorName("최적 공격 거리")]
    [Tooltip("보스가 근거리/원거리 패턴을 고르기 위한 기준 거리입니다.")]
    public float optimalAttackDistance = 3.5f;
    [InspectorName("근거리 스킬 거리")]
    [Tooltip("이 거리 이하면 근거리 패턴으로 판단합니다.")]
    public float closeSkillRange = 4f;
    [InspectorName("추적 이동 속도 배율")]
    [Tooltip("보스가 추적할 때 기본 이동 속도에 곱할 값입니다.")]
    public float chaseMoveSpeedMultiplier = 1f;
    [Header("페이즈 전환")]
    [InspectorName("2페이즈 HP 비율")]
    [Tooltip("보스 HP 비율이 이 값 이하가 되면 2페이즈 전환 조건을 만족합니다.")]
    public float phaseTwoHpRatio = 0.5f;
    [InspectorName("페이즈 전환 시간")]
    [Tooltip("대사, 무적, 연출을 포함한 페이즈 전환 시간입니다.")]
    public float phaseTransitionSeconds = 1.5f;
    [InspectorName("카메라 집중 시간")]
    [Tooltip("페이즈 전환이나 등장 시 카메라가 보스를 잡는 시간입니다.")]
    public float cameraFocusSeconds = 2f;
    [InspectorName("영혼 상태 복귀 중심 정지 거리")]
    [Tooltip("플레이어가 영혼 상태일 때 보스가 중앙으로 돌아가며 멈출 거리입니다.")]
    public float soulReturnCenterStoppingDistance = 0.2f;
    [Header("슈퍼아머")]
    [InspectorName("공격 중 슈퍼아머")]
    [Tooltip("켜면 보스가 공격 중 피격 경직을 받지 않습니다.")]
    public bool superArmorDuringAttack = true;
    [InspectorName("페이즈 전환 중 슈퍼아머")]
    [Tooltip("켜면 페이즈 전환 연출 중 피격 경직을 받지 않습니다.")]
    public bool superArmorDuringPhaseTransition = true;
    [Header("그로기")]
    [InspectorName("슈퍼아머 중 그로기 누적")]
    [Tooltip("켜면 슈퍼아머 중에도 그로기 누적 피격 수를 계산합니다.")]
    public bool countGroggyHitsDuringSuperArmor = true;
    [InspectorName("피격 제한 중 그로기 누적")]
    [Tooltip("켜면 연속 피격 제한 중에도 그로기 누적 피격 수를 계산합니다.")]
    public bool countGroggyHitsWhileHitReactionLimited = true;
    [InspectorName("그로기 시 피격 제한 해제")]
    [Tooltip("켜면 그로기에 들어갈 때 연속 피격 제한 상태를 해제합니다.")]
    public bool clearHitReactionLimitOnGroggy = true;
    [InspectorName("그로기 시 행동 취소")]
    [Tooltip("켜면 그로기에 들어갈 때 진행 중인 패턴을 취소합니다.")]
    public bool cancelActionsOnGroggy = true;
    [InspectorName("그로기 필요 피격 수")]
    [Tooltip("이 횟수만큼 연속 피격되면 그로기에 들어갑니다.")]
    public int groggyHitCountThreshold = 5;
    [InspectorName("그로기 피격 집계 시간")]
    [Tooltip("연속 피격 수를 집계하는 시간입니다.")]
    public float groggyHitWindowSeconds = 3f;
    [InspectorName("그로기 지속 시간")]
    [Tooltip("보스가 행동 불능이 되는 시간입니다.")]
    public float groggyDurationSeconds = 2.5f;
    [InspectorName("그로기 중 받는 피해 배율")]
    [Tooltip("그로기 상태에서 보스가 받는 피해 배율입니다.")]
    public float groggyDamageMultiplier = 1.5f;
    [InspectorName("그로기 종료 넉백")]
    [Tooltip("켜면 그로기가 끝날 때 주변 넉백을 발생시킵니다.")]
    public bool knockbackOnGroggyEnd = true;
    [InspectorName("그로기 종료 넉백 힘")]
    [Tooltip("그로기 종료 넉백의 힘입니다.")]
    public float groggyEndKnockbackPower = 8f;
}

/// <summary>
/// 보스의 HP 비율별 페이즈 데이터를 관리합니다.
/// 보스 전투 시스템이 현재 HP 비율을 기준으로 스킬 세트나 스테이지 이벤트를 바꿀 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_BossPhaseData
{
    [Header("페이즈")]
    [InspectorName("페이즈 ID")]
    [Tooltip("이 페이즈를 구분하는 고정 ID입니다.")]
    public string phaseId;
    [InspectorName("시작 HP 비율")]
    [Tooltip("보스 HP 비율이 이 값 이하일 때 이 페이즈를 사용할 수 있습니다.")]
    public float startHpRatio = 1f;
    [InspectorName("페이즈 스킬 목록")]
    [Tooltip("이 페이즈에서 사용할 스킬 목록입니다.")]
    public HWJ_SkillSetData skillSet = new HWJ_SkillSetData();
    [InspectorName("스테이지 이벤트 ID")]
    [Tooltip("페이즈 진입 시 실행할 스테이지 이벤트 ID입니다.")]
    public string stageEventId;
    [InspectorName("스테이지 변화 있음")]
    [Tooltip("켜면 페이즈 전환 시 맵/기믹 변화가 있다고 표시합니다.")]
    public bool changesStage;
}

/// <summary>
/// 보스가 특정 HP 비율에서 변신하는 규칙을 관리합니다.
/// 최종 보스 2페이즈처럼 모델, 애니메이션, 패턴 전환이 필요할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_BossTransformData
{
    [Header("페이즈 변신")]
    [InspectorName("변신 가능")]
    [Tooltip("켜면 특정 HP 이하에서 모델/애니메이션 전환을 허용합니다.")]
    public bool canTransform;
    [InspectorName("변신 HP 비율")]
    [Tooltip("이 HP 비율 이하가 되면 변신 조건을 만족합니다.")]
    public float transformHpRatio = 0.5f;
    [InspectorName("변신 후 모델 ID")]
    [Tooltip("변신 후 사용할 모델 또는 표현 데이터 ID입니다.")]
    public string transformedModelId;
    [InspectorName("변신 애니메이션 ID")]
    [Tooltip("변신 연출에 사용할 애니메이션 ID입니다.")]
    public string transformAnimationId;
}
