using System;
using UnityEngine;

/// <summary>
/// 모든 오브젝트가 공통으로 가지는 식별 정보입니다.
/// RootObjectDataSO에서 오브젝트 ID, 표시 이름, 타입, 진영, 기본 무기를 관리할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_IdentityData
{
    public string objectId;
    public string displayName;
    public HWJ_ObjectType objectType;
    public HWJ_Faction faction;
    public HWJ_WeaponType weaponType;
}

/// <summary>
/// 플레이어, 적, NPC, 보스가 공통으로 참조할 수 있는 기본 능력치입니다.
/// 이동, 전투, 성장 시스템이 Resolver를 통해 이 값을 읽습니다.
/// </summary>
[Serializable]
public class HWJ_StatusData
{
    public int level = 1;
    public float maxHp;
    public float moveSpeed;
    public float attackPower;
    public float defense;
    public float attackSpeed = 1f;
    public float bodyWeight = 1f;
}

/// <summary>
/// 오브젝트의 모델, 애니메이터, 이펙트 부착 위치를 관리합니다.
/// 프리팹 생성, 빙의 가능 심장 이펙트, 피격 이펙트 연결에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ModelData
{
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;
    public string heartEffectSocketName;
    public string hitEffectSocketName;
    public string possessedEyeSocketName;
}

/// <summary>
/// 오브젝트가 가하는 데미지 정보를 관리합니다.
/// CombatSystem이 공격력과 기본 데미지를 합산하고, 추후 치명타/넉백/경직 계산에 사용합니다.
/// </summary>
[Serializable]
public class HWJ_DamageData
{
    public HWJ_DamageType damageType;
    public float baseDamage;
    public float criticalChance;
    public float criticalMultiplier = 1.5f;
    public float knockbackPower;
    public float hitStunSeconds;
    public float hitStopSeconds;
    public float sameTargetHitCooldownSeconds = 0.08f;
}

/// <summary>
/// 오브젝트가 피해를 받을 때 적용되는 방어/무적 정보를 관리합니다.
/// CombatSystem과 함정/적 공격 판정에서 피격 가능 여부를 확인할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_ReceivedDamageData
{
    public float damageMultiplier = 1f;
    public float invincibleSecondsAfterHit;
    public bool isInvincible;
    public bool ignoreTrapDamage;
    public bool ignoreEnemyDamage;
    public float hitStunSeconds = 0.18f;
    public float hitReactionImmuneSeconds;
    public int maxHitReactionsPerWindow;
    public float hitReactionWindowSeconds = 1f;
    public float hitReactionLimitImmuneSeconds;
    public float knockbackWeightMultiplier = 1f;
    public bool hasSuperArmor;
    public bool ignoreHitStun;
    public bool ignoreKnockback;
}

/// <summary>
/// 오브젝트가 상호작용 가능한 대상인지 관리합니다.
/// NPC 대화, 빙의 대상 감지, 조사 오브젝트 같은 상호작용 시스템에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_InteractionData
{
    public bool canInteract;
    public bool canBeTargeted = true;
    public float interactionRange;
}

/// <summary>
/// 적이나 보스를 처치했을 때 지급할 보상 정보를 관리합니다.
/// 경험치, 스킬 포인트, 능력치 구슬 드롭 시스템에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_RewardData
{
    public int experienceReward;
    public int skillPointReward;
    public bool dropsStatOrb;
    public string statOrbId;
}
