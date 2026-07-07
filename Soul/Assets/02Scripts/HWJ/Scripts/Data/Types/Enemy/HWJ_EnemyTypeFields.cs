using System;

/// <summary>
/// 적의 전투 역할과 육신 사용 가능 여부를 관리합니다.
/// 적 생성, 빙의 판정, 능력치 구슬 드롭 판단에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_EnemyRoleData
{
    public HWJ_WeaponType weaponType;
    public bool isElite;
    public bool leavesCorpseOnDeath = true;
    public bool isPossessableBody = true;
    public bool guaranteesStatOrb;
}

/// <summary>
/// 적이 플레이어를 감지하고 추적할 거리 조건입니다.
/// MonsterAISystem 또는 추후 AI 실행 스크립트가 타겟 추적 판단에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_TrackingData
{
    public float trackingRange;
    public float loseTargetRange;
}

/// <summary>
/// 적/보스 AI의 의사결정 프로필과 갱신 주기를 관리합니다.
/// AI 시스템이 behaviorTreeId나 aiProfileId를 읽어 행동 패턴을 선택합니다.
/// </summary>
[Serializable]
public class HWJ_AIData
{
    public string aiProfileId;
    public float decisionIntervalSeconds = 0.1f;
    public string behaviorTreeId;
    public float idleSeconds = 0.2f;
    public float detectSeconds = 0.15f;
    public float attackPrepareSeconds = 0.25f;
    public float attackRecoverySeconds = 0.35f;
    public float repathSeconds = 0.15f;
}

/// <summary>
/// 적/보스의 이동 경로 갱신과 정지 거리 조건입니다.
/// 네비게이션 또는 플랫폼 이동 AI에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_NavigationData
{
    public float stoppingDistance;
    public float pathRefreshSeconds;
    public bool canUsePlatformDrop;
    public bool avoidLedges = true;
    public float ledgeCheckForwardDistance = 0.45f;
    public float ledgeCheckDownDistance = 1.2f;
    public float wallCheckDistance = 0.15f;
}

/// <summary>
/// 적의 상태 전환 규칙을 관리합니다.
/// EnemyNavigationSystem과 MonsterAISystem이 대기, 추적, 공격, 사망 상태를 판단할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_EnemyStateData
{
    public HWJ_RuntimeState startState = HWJ_RuntimeState.Idle;
    public float attackRange;
    public float returnToIdleDelaySeconds = 1f;
    public bool stopWhenHit = true;
    public bool hasSuperArmor;
    public bool immuneToHitStun;
}
