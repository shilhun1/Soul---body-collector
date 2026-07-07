/// <summary>
/// RootObjectData가 선택할 수 있는 최상위 오브젝트 유형입니다.
/// Resolver와 TypeData에서 Player, Enemy, NPC, Boss를 구분할 때 사용합니다.
/// </summary>
public enum HWJ_ObjectType
{
    Player,
    Enemy,
    NPC,
    Boss
}

/// <summary>
/// 공통 데미지 데이터에서 공격 속성을 구분할 때 사용합니다.
/// 전투 계산식이 확장될 때 물리/마법/고정 피해 분기에 연결합니다.
/// </summary>
public enum HWJ_DamageType
{
    Physical,
    Magical,
    TrueDamage
}

/// <summary>
/// 오브젝트의 소속을 나타냅니다.
/// 추후 피아식별, 타겟팅, 상호작용 필터링에서 사용합니다.
/// </summary>
public enum HWJ_Faction
{
    Neutral,
    Player,
    Empire,
    Monster,
    NPC
}

/// <summary>
/// 육신 또는 적 유형이 사용하는 무기 종류입니다.
/// 스킬 조건, 빙의 후 능력 교체, 적 역할 구분에 사용합니다.
/// </summary>
public enum HWJ_WeaponType
{
    None,
    GreatSword,
    DualSword,
    Sword,
    Lance,
    Bow,
    Axe,
    Shield,
    Staff,
    Mace,
    Claw
}

/// <summary>
/// 보스 유형을 중간 보스와 최종 보스로 나눌 때 사용합니다.
/// BossTypeDataSO의 패턴, 보상, 페이즈 규칙 분기에 연결합니다.
/// </summary>
public enum HWJ_BossRank
{
    MidBoss,
    FinalBoss
}

/// <summary>
/// 스폰 위치가 어떤 용도인지 구분합니다.
/// 플레이어 시작 지점, 적 스폰, NPC 배치, 보스 스폰을 같은 스폰 시스템에서 다룰 때 사용합니다.
/// </summary>
public enum HWJ_SpawnPointType
{
    PlayerStart,
    Enemy,
    NPC,
    Boss,
    StatOrb
}

/// <summary>
/// 런타임 상태를 공통으로 표현할 때 사용합니다.
/// 플레이어 상태, 적 상태, UI 표시 상태를 같은 단어로 맞추기 위한 enum입니다.
/// </summary>
public enum HWJ_RuntimeState
{
    None,
    Idle,
    Move,
    Attack,
    Hit,
    Possessed,
    Soul,
    Dead
}

/// <summary>
/// 능력치 구슬이 어떤 능력치를 올리는지 구분합니다.
/// StatOrbDataSO와 StatOrbSystem이 이 값을 기준으로 Status에 보너스를 적용합니다.
/// </summary>
public enum HWJ_StatOrbType
{
    MaxHp,
    MoveSpeed,
    AttackPower,
    Defense,
    AttackSpeed
}

/// <summary>
/// 스킬이 실행될 때 사용할 기본 행동 타입입니다.
/// SkillActionSystem이 발사체, 근접 공격, 버프, 대쉬 같은 행동 분기로 사용합니다.
/// </summary>
public enum HWJ_SkillActionType
{
    None,
    Melee,
    Projectile,
    Buff,
    Dash,
    Area
}

/// <summary>
/// 보스 패턴이 실행될 조건입니다.
/// BossPatternSystem이 HP 비율, 시간, 쿨타임 기준으로 패턴을 고를 때 사용합니다.
/// </summary>
public enum HWJ_BossPatternTrigger
{
    Always,
    HpBelowRatio,
    Timed,
    PhaseChanged
}

public enum HWJ_BossFSMState
{
    Inactive,
    Idle,
    Chase,
    Attack,
    PhaseTransition,
    Groggy,
    Dead
}

public enum HWJ_MonsterAIState
{
    Idle,
    Detect,
    Approach,
    AttackPrepare,
    Attack,
    Recovery,
    Repath,
    HitStun,
    Dead
}
