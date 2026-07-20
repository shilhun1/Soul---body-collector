using System;
using UnityEngine;

/// <summary>
/// 적의 전투 역할과 빙의 가능 여부를 관리합니다.
/// 적 생성, 빙의 판정, 스탯 구슬 보상 판단에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_EnemyRoleData
{
    [Header("역할")]
    [InspectorName("무기 타입")]
    [Tooltip("몬스터가 사용하는 무기입니다. 빙의 후 플레이어 스킬 선택에도 영향을 줍니다.")]
    public HWJ_WeaponType weaponType;

    [InspectorName("엘리트 몬스터")]
    [Tooltip("엘리트 보정이나 단계 보상 판정에 사용할 수 있는 플래그입니다.")]
    public bool isElite;

    [InspectorName("사망 후 시체 남김")]
    [Tooltip("켜면 사망 후 시체가 남아 빙의 대상이 될 수 있습니다.")]
    public bool leavesCorpseOnDeath = true;

    [InspectorName("빙의 가능한 몸")]
    [Tooltip("켜면 처치 후 플레이어가 이 몸에 빙의할 수 있습니다.")]
    public bool isPossessableBody = true;

    [InspectorName("스탯 구슬 확정")]
    [Tooltip("켜면 처치 후 스탯 구슬 보상을 확정으로 지급하는 판정에 사용할 수 있습니다.")]
    public bool guaranteesStatOrb;
}

/// <summary>
/// 적이 플레이어를 감지하고 추적하는 거리 조건입니다.
/// MonsterAISystem 또는 이후 AI 실행 스크립트가 대상 추적 판단에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_TrackingData
{
    [Header("추적 거리")]
    [InspectorName("인식 거리")]
    [Tooltip("플레이어를 발견하고 추적을 시작하는 거리입니다.")]
    public float trackingRange;

    [InspectorName("어그로 해제 거리")]
    [Tooltip("플레이어가 이 거리보다 멀어지면 추적을 해제할 수 있습니다. 0이면 별도 해제 거리로 사용하지 않습니다.")]
    public float loseTargetRange;
}

/// <summary>
/// 몬스터 AI 판단 주기와 공격 흐름 시간을 관리합니다.
/// 스킬 순환, 기본 공격 간격, 상태 전환 시간의 기준 데이터입니다.
/// </summary>
[Serializable]
public class HWJ_AIData
{
    [Header("AI 프로필")]
    [InspectorName("AI 프로필 ID")]
    [Tooltip("행동 프로필을 구분하기 위한 ID입니다.")]
    public string aiProfileId;

    [InspectorName("판단 주기")]
    [Tooltip("AI가 다음 행동을 다시 판단하는 주기입니다. 낮을수록 반응이 빨라집니다.")]
    public float decisionIntervalSeconds = 0.1f;

    [InspectorName("행동 트리 ID")]
    [Tooltip("추후 행동 트리나 AI 프로필을 연결할 때 사용할 ID입니다.")]
    public string behaviorTreeId;

    [Header("상태별 시간")]
    [InspectorName("대기 시간")]
    [Tooltip("Idle 상태에 머무르는 기본 시간입니다.")]
    public float idleSeconds = 0.2f;

    [InspectorName("인식 시간")]
    [Tooltip("플레이어를 발견한 뒤 접근 상태로 넘어가기 전까지의 시간입니다.")]
    public float detectSeconds = 0.15f;

    [InspectorName("공격 준비 시간")]
    [Tooltip("공격 전 준비 상태에 머무르는 시간입니다.")]
    public float attackPrepareSeconds = 0.25f;

    [InspectorName("공격 후딜 시간")]
    [Tooltip("공격 후 다시 추적하거나 판단하기 전까지 기다리는 시간입니다.")]
    public float attackRecoverySeconds = 0.35f;

    [InspectorName("경로 갱신 주기")]
    [Tooltip("추적 중 목표 위치를 다시 계산하는 주기입니다.")]
    public float repathSeconds = 0.15f;

    [Header("공격 규칙")]
    [InspectorName("기본 스킬 쿨타임")]
    [Tooltip("몬스터가 1, 2, 3번 스킬을 순서대로 사용할 때 각 스킬 사이에 기다리는 시간입니다.")]
    public float defaultSkillCooldownSeconds = 3f;

    [InspectorName("기본 공격 간격")]
    [Tooltip("몬스터가 스킬을 진행하지 않을 때 사용하는 기본 공격 간격입니다.")]
    public float basicAttackIntervalSeconds = 1.5f;

    [InspectorName("스킬 사이클 개수")]
    [Tooltip("몬스터가 순서대로 사용할 스킬 슬롯 개수입니다. 기본은 1, 2, 3번 스킬입니다.")]
    public int skillCycleCount = 3;

    [InspectorName("스킬 사이클 초기화 딜레이")]
    [Tooltip("1, 2, 3번 스킬을 모두 사용한 뒤 다시 1번 스킬을 쓰기 전까지 기다리는 시간입니다.")]
    public float skillCycleResetDelaySeconds = 5f;
}

/// <summary>
/// 몬스터의 이동 경로 갱신과 정지 거리 조건입니다.
/// 네비게이션 또는 플랫폼 이동 AI에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_NavigationData
{
    [Header("이동")]
    [InspectorName("정지 거리")]
    [Tooltip("대상에게 이 거리만큼 가까워지면 이동을 멈춥니다.")]
    public float stoppingDistance;

    [InspectorName("경로 갱신 시간")]
    [Tooltip("이동 경로를 다시 계산하는 간격입니다.")]
    public float pathRefreshSeconds;

    [InspectorName("단방향 발판 내려가기 사용")]
    [Tooltip("켜면 AI가 단방향 발판에서 내려가는 행동을 사용할 수 있습니다.")]
    public bool canUsePlatformDrop;

    [Header("끼임/낙하 방지")]
    [InspectorName("낭떠러지 회피")]
    [Tooltip("켜면 몬스터가 발판 끝에서 떨어지지 않도록 이동을 제한합니다.")]
    public bool avoidLedges = true;

    [InspectorName("전방 낭떠러지 확인 거리")]
    [Tooltip("앞쪽에 바닥이 있는지 검사하는 거리입니다.")]
    public float ledgeCheckForwardDistance = 0.45f;

    [InspectorName("아래 바닥 확인 거리")]
    [Tooltip("아래쪽 바닥을 검사하는 거리입니다.")]
    public float ledgeCheckDownDistance = 1.2f;

    [InspectorName("벽 확인 거리")]
    [Tooltip("벽이나 모서리에 끼이지 않도록 앞쪽 벽을 검사하는 거리입니다.")]
    public float wallCheckDistance = 0.15f;
}

/// <summary>
/// 적의 상태 전환 규칙을 관리합니다.
/// EnemyNavigationSystem과 MonsterAISystem이 대기, 추적, 공격, 사망 상태 판단에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_EnemyStateData
{
    [Header("상태")]
    [InspectorName("시작 상태")]
    [Tooltip("스폰 직후 시작할 런타임 상태입니다.")]
    public HWJ_RuntimeState startState = HWJ_RuntimeState.Idle;

    [InspectorName("공격 시작 거리")]
    [Tooltip("AI가 접근을 멈추고 공격 준비로 들어가는 거리입니다. 스킬 사거리와는 별도입니다.")]
    public float attackRange;

    [InspectorName("대기 복귀 시간")]
    [Tooltip("목표를 잃었거나 행동 후 Idle로 돌아가기 전 기다리는 시간입니다.")]
    public float returnToIdleDelaySeconds = 1f;

    [InspectorName("피격 시 정지")]
    [Tooltip("켜면 피격 상태에서 이동과 공격을 멈춥니다.")]
    public bool stopWhenHit = true;

    [InspectorName("슈퍼아머")]
    [Tooltip("켜면 피격 경직과 넉백을 무시하는 판정에 사용합니다.")]
    public bool hasSuperArmor;

    [InspectorName("피격 경직 면역")]
    [Tooltip("켜면 피격 시 경직되지 않습니다.")]
    public bool immuneToHitStun;
}
