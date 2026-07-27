using System;
using UnityEngine;

/// <summary>
/// 모든 오브젝트가 공통으로 가지는 식별 정보입니다.
/// RootObjectDataSO에서 오브젝트 ID, 표시 이름, 타입, 진영, 기본 무기를 관리할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_IdentityData
{
    [Header("ID")]
    [InspectorName("오브젝트 ID")]
    [Tooltip("저장, 보상 중복 방지, 데이터 조회에 쓰는 고정 ID입니다.")]
    public string objectId;
    [InspectorName("표시 이름")]
    [Tooltip("인스펙터와 UI에서 사람이 읽는 이름입니다.")]
    public string displayName;
    [InspectorName("오브젝트 유형")]
    [Tooltip("플레이어, 적, NPC, 보스 중 어떤 유형인지 정합니다.")]
    public HWJ_ObjectType objectType;
    [InspectorName("진영")]
    [Tooltip("전투에서 아군/적군 판정을 할 때 사용합니다.")]
    public HWJ_Faction faction;
    [InspectorName("기본 무기")]
    [Tooltip("이 오브젝트가 기본으로 사용하는 무기입니다.")]
    public HWJ_WeaponType weaponType;
    [InspectorName("Ability Tags")]
    [Tooltip("Map gimmicks this possessed body can use. Stage systems read this instead of hardcoding every body by weapon.")]
    public HWJ_AbilityTag[] abilityTags;
}

/// <summary>
/// 플레이어, 적, NPC, 보스가 공통으로 참조할 수 있는 기본 능력치입니다.
/// 이동, 전투, 성장 시스템이 Resolver를 통해 이 값을 읽습니다.
/// </summary>
[Serializable]
public class HWJ_StatusData
{
    [Header("기본 능력치")]
    [InspectorName("레벨")]
    [Tooltip("오브젝트의 기준 레벨입니다.")]
    public int level = 1;
    [InspectorName("최대 HP")]
    [Tooltip("전투에서 사용할 최대 체력입니다.")]
    public float maxHp;
    [InspectorName("이동 속도")]
    [Tooltip("기본 이동 속도입니다.")]
    public float moveSpeed;
    [InspectorName("공격력")]
    [Tooltip("기본 공격력입니다. 스킬 데미지 계산의 기준값으로 사용됩니다.")]
    public float attackPower;
    [InspectorName("방어력")]
    [Tooltip("받는 데미지를 줄이는 방어력입니다.")]
    public float defense;
    [InspectorName("공격 속도")]
    [Tooltip("기본 공격 간격 계산에 사용합니다. 1이면 기본 속도입니다.")]
    public float attackSpeed = 1f;
    [InspectorName("무게")]
    [Tooltip("넉백을 얼마나 덜 받는지 계산할 때 사용합니다. 높을수록 덜 밀립니다.")]
    public float bodyWeight = 1f;
}

/// <summary>
/// 오브젝트의 모델, 애니메이터, 이펙트 부착 위치를 관리합니다.
/// 프리팹 생성, 빙의 가능 심장 이펙트, 피격 이펙트 연결에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ModelData
{
    [Header("모델과 이펙트 위치")]
    [InspectorName("모델 프리팹")]
    [Tooltip("이 오브젝트를 표현할 프리팹입니다.")]
    public GameObject modelPrefab;
    [InspectorName("애니메이터 컨트롤러")]
    [Tooltip("애니메이션 상태를 재생할 Animator Controller입니다.")]
    public RuntimeAnimatorController animatorController;
    [InspectorName("심장 이펙트 위치 이름")]
    [Tooltip("빙의 가능 표시 이펙트를 붙일 소켓 이름입니다.")]
    public string heartEffectSocketName;
    [InspectorName("피격 이펙트 위치 이름")]
    [Tooltip("피격 이펙트를 붙일 소켓 이름입니다.")]
    public string hitEffectSocketName;
    [InspectorName("빙의 눈 이펙트 위치 이름")]
    [Tooltip("빙의 상태 표현 이펙트를 붙일 소켓 이름입니다.")]
    public string possessedEyeSocketName;
}

/// <summary>
/// 오브젝트가 가하는 데미지 정보를 관리합니다.
/// CombatSystem이 공격력과 기본 데미지를 합산하고, 추후 치명타/넉백/경직 계산에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_DamageData
{
    [Header("공격 데미지")]
    [InspectorName("데미지 타입")]
    [Tooltip("물리, 마법 등 데미지 성격입니다.")]
    public HWJ_DamageType damageType;
    [InspectorName("기본 데미지")]
    [Tooltip("공격의 기본 데미지입니다.")]
    public float baseDamage;
    [InspectorName("치명타 확률")]
    [Tooltip("0~1 사이 값으로 사용합니다. 0.2는 20%입니다.")]
    public float criticalChance;
    [InspectorName("치명타 배율")]
    [Tooltip("치명타가 발생했을 때 곱해지는 배율입니다.")]
    public float criticalMultiplier = 1.5f;
    [InspectorName("넉백 힘")]
    [Tooltip("피격 대상을 밀어내는 힘입니다.")]
    public float knockbackPower;
    [InspectorName("히트스턴 시간")]
    [Tooltip("피격 대상의 조작을 멈추는 시간입니다.")]
    public float hitStunSeconds;
    [InspectorName("히트스톱 시간")]
    [Tooltip("타격감을 위해 순간적으로 멈추는 시간입니다.")]
    public float hitStopSeconds;
    [InspectorName("동일 대상 연속 타격 제한")]
    [Tooltip("같은 대상에게 너무 자주 데미지가 들어가지 않게 막는 시간입니다.")]
    public float sameTargetHitCooldownSeconds = 0.08f;
}

/// <summary>
/// 오브젝트가 피해를 받을 때 적용되는 방어/무적 정보를 관리합니다.
/// CombatSystem과 함정/적 공격 판정에서 피격 가능 여부를 확인할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ReceivedDamageData
{
    [Header("피해 받기")]
    [InspectorName("받는 데미지 배율")]
    [Tooltip("1은 그대로, 0.5는 절반, 2는 두 배 피해입니다.")]
    public float damageMultiplier = 1f;
    [InspectorName("피격 후 무적 시간")]
    [Tooltip("피격 직후 추가 피해를 무시하는 시간입니다.")]
    public float invincibleSecondsAfterHit;
    [InspectorName("항상 무적")]
    [Tooltip("켜면 데미지를 받지 않습니다.")]
    public bool isInvincible;
    [InspectorName("함정 데미지 무시")]
    [Tooltip("켜면 함정 데미지를 받지 않습니다.")]
    public bool ignoreTrapDamage;
    [InspectorName("적 데미지 무시")]
    [Tooltip("켜면 적 공격 데미지를 받지 않습니다.")]
    public bool ignoreEnemyDamage;
    [InspectorName("피격 경직 시간")]
    [Tooltip("피격 시 행동이 막히는 시간입니다.")]
    public float hitStunSeconds = 0.18f;
    [InspectorName("피격 반응 면역 시간")]
    [Tooltip("피격 반응을 잠시 무시하는 시간입니다.")]
    public float hitReactionImmuneSeconds;
    [InspectorName("연속 피격 반응 최대 횟수")]
    [Tooltip("정해진 시간 안에 피격 반응을 몇 번까지 허용할지 정합니다.")]
    public int maxHitReactionsPerWindow;
    [InspectorName("연속 피격 판정 시간")]
    [Tooltip("연속 피격 반응 횟수를 계산하는 시간 창입니다.")]
    public float hitReactionWindowSeconds = 1f;
    [InspectorName("연속 피격 제한 후 면역 시간")]
    [Tooltip("연속 피격 제한에 도달한 뒤 피격 반응을 막는 시간입니다.")]
    public float hitReactionLimitImmuneSeconds;
    [InspectorName("넉백 무게 배율")]
    [Tooltip("높을수록 넉백을 덜 받습니다.")]
    public float knockbackWeightMultiplier = 1f;
    [InspectorName("슈퍼아머")]
    [Tooltip("켜면 피격 경직과 넉백을 대부분 무시합니다.")]
    public bool hasSuperArmor;
    [InspectorName("피격 경직 무시")]
    [Tooltip("켜면 데미지를 받아도 경직되지 않습니다.")]
    public bool ignoreHitStun;
    [InspectorName("넉백 무시")]
    [Tooltip("켜면 데미지를 받아도 밀리지 않습니다.")]
    public bool ignoreKnockback;
}

/// <summary>
/// 오브젝트가 상호작용 가능한 대상인지 관리합니다.
/// NPC 대화, 빙의 대상 감지, 조사 오브젝트 같은 상호작용 시스템에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_InteractionData
{
    [Header("상호작용")]
    [InspectorName("상호작용 가능")]
    [Tooltip("켜면 상호작용 대상이 됩니다.")]
    public bool canInteract;
    [InspectorName("타겟 지정 가능")]
    [Tooltip("켜면 공격, 탐색, 판정의 대상이 될 수 있습니다.")]
    public bool canBeTargeted = true;
    [InspectorName("상호작용 거리")]
    [Tooltip("상호작용 가능한 거리입니다.")]
    public float interactionRange;
}

[Serializable]
public class HWJ_StatOrbRewardEntry
{
    [InspectorName("스탯 구슬 ID")]
    [Tooltip("랜덤 후보로 사용할 StatOrbDataSO의 고정 ID입니다.")]
    public string statOrbId;

    [InspectorName("가중치")]
    [Tooltip("랜덤 선택 가중치입니다. 0 이하면 후보에서 제외됩니다.")]
    public int weight = 1;
}

/// <summary>
/// 적이나 보스를 처치했을 때 지급할 보상 정보를 관리합니다.
/// 경험치, 스킬 포인트, 능력치 구슬 드롭 시스템에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_RewardData
{
    [Header("보상")]
    [InspectorName("경험치 보상")]
    [Tooltip("처치 또는 완료 시 지급할 경험치입니다.")]
    public int experienceReward;
    [InspectorName("스킬 포인트 보상")]
    [Tooltip("처치 또는 완료 시 지급할 스킬 포인트입니다.")]
    public int skillPointReward;
    [InspectorName("경험치 구슬로 지급")]
    [Tooltip("켜면 경험치를 즉시 지급하지 않고 경험치 구슬 프리팹으로 생성합니다. 프리팹이 없으면 즉시 지급으로 처리합니다.")]
    public bool dropsExperienceOrb;
    [InspectorName("경험치 구슬 프리팹")]
    [Tooltip("경험치를 담아 생성할 구슬 프리팹입니다. HWJ_ExperienceOrbPickupSystem이 없으면 생성 시 자동으로 붙입니다.")]
    public GameObject experienceOrbPrefab;
    [InspectorName("경험치 구슬 생성 반경")]
    [Tooltip("처치 위치에서 경험치 구슬이 살짝 흩어져 생성되는 반경입니다.")]
    public float experienceOrbSpawnRadius;
    [InspectorName("스탯 구슬 드롭")]
    [Tooltip("켜면 스탯 구슬 보상을 드롭합니다.")]
    public bool dropsStatOrb;
    [InspectorName("스탯 구슬 드롭 확률")]
    [Tooltip("스탯 구슬이 드롭될 확률입니다. 파워 몬스터 확정 플래그가 켜진 적은 이 값을 무시하고 100%로 처리합니다.")]
    [Range(0f, 1f)]
    public float statOrbDropChance = 1f;
    [InspectorName("스탯 구슬 생성 반경")]
    [Tooltip("처치 위치에서 스탯 구슬이 살짝 흩어져 생성되는 반경입니다.")]
    public float statOrbSpawnRadius = 0.75f;
    [InspectorName("스탯 구슬 ID")]
    [Tooltip("고정으로 드롭할 StatOrbDataSO의 ID입니다. 랜덤 후보가 있으면 랜덤 후보를 우선 사용합니다.")]
    public string statOrbId;
    [InspectorName("랜덤 스탯 구슬 후보")]
    [Tooltip("여러 구슬 중 하나를 랜덤으로 드롭할 때 사용합니다.")]
    public HWJ_StatOrbRewardEntry[] statOrbCandidates;
    [InspectorName("후보 없을 때 전체 구슬 사용")]
    [Tooltip("켜면 고정 ID와 랜덤 후보가 비어 있을 때 GameplayDatabase에 등록된 모든 StatOrbDataSO 중 하나를 고릅니다.")]
    public bool useAllRegisteredStatOrbsWhenEmpty;
}
