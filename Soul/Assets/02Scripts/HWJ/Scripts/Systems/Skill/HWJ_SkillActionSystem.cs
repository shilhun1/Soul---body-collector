using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SkillActionDataSO를 실행하는 공통 스킬 행동 시스템입니다.
/// 플레이어 공격, 적 스킬 사이클, 보스 패턴 시스템이 같은 방식으로 스킬 데이터를 사용할 수 있게 합니다.
/// </summary>
public class HWJ_SkillActionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private HWJ_GameplayDatabaseSO database;
    [SerializeField] private HWJ_SkillActionDataSO[] localSkillActions;

    private readonly Dictionary<string, float> nextUseTimes = new Dictionary<string, float>();

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    /// <summary>
    /// 스킬 ID로 스킬 데이터를 찾아 실행합니다.
    /// 먼저 로컬 목록을 보고, 없으면 GameplayDatabase에서 찾습니다.
    /// </summary>
    public bool TryUseSkill(string skillActionId)
    {
        if (!TryGetSkillAction(skillActionId, out HWJ_SkillActionDataSO skillAction))
        {
            return false;
        }

        return TryUseSkill(skillAction);
    }

    /// <summary>
    /// 이미 참조된 SkillActionDataSO를 직접 실행합니다.
    /// 보스 패턴처럼 데이터가 배열로 연결된 경우 이 경로를 사용합니다.
    /// </summary>
    public bool TryUseSkill(HWJ_SkillActionDataSO skillAction)
    {
        if (skillAction == null || dataResolver == null)
        {
            return false;
        }

        if (!skillAction.CanUseWithWeapon(GetCurrentWeaponType()))
        {
            return false;
        }

        if (nextUseTimes.TryGetValue(skillAction.SkillActionId, out float nextUseTime) && Time.time < nextUseTime)
        {
            return false;
        }

        nextUseTimes[skillAction.SkillActionId] = Time.time + skillAction.CooldownSeconds;
        ExecuteSkill(skillAction);
        return true;
    }

    private void ExecuteSkill(HWJ_SkillActionDataSO skillAction)
    {
        if (skillAction.ActionEffectPrefab != null)
        {
            SpawnPooled(skillAction.ActionEffectPrefab);
        }

        if (skillAction.ProjectilePrefab != null)
        {
            SpawnPooled(skillAction.ProjectilePrefab);
        }

        float baseDamage = combatSystem != null ? combatSystem.GetOutgoingDamage() : 0f;
        float finalDamage = baseDamage * skillAction.DamageMultiplier;

        // 실제 히트박스나 투사체 시스템은 finalDamage를 받아 타겟에게 전달하도록 확장합니다.
        _ = finalDamage;
    }

    private GameObject SpawnPooled(GameObject prefab)
    {
        if (objectPool != null)
        {
            return objectPool.Spawn(prefab, transform.position, transform.rotation);
        }

        return HWJ_GameAccess.Spawn(prefab, transform.position, transform.rotation);
    }

    private bool TryGetSkillAction(string skillActionId, out HWJ_SkillActionDataSO skillAction)
    {
        skillAction = null;

        if (localSkillActions != null)
        {
            for (int i = 0; i < localSkillActions.Length; i++)
            {
                if (localSkillActions[i] != null && localSkillActions[i].SkillActionId == skillActionId)
                {
                    skillAction = localSkillActions[i];
                    return true;
                }
            }
        }

        if (database != null && database.TryGetSkillAction(skillActionId, out skillAction))
        {
            return true;
        }

        return HWJ_GameAccess.TryGetSkillAction(skillActionId, out skillAction);
    }

    private HWJ_WeaponType GetCurrentWeaponType()
    {
        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        return dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
    }
}
