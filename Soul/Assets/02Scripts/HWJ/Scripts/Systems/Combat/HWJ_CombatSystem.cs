using UnityEngine;

/// <summary>
/// 공통 전투 계산을 담당하는 컴포넌트입니다.
/// 공격하거나 피해를 받을 수 있는 플레이어, 적, 보스 오브젝트에 붙여 RootObjectData의 Status/Damage/ReceivedDamage를 사용합니다.
/// </summary>
public class HWJ_CombatSystem : MonoBehaviour
{
    private const float DefaultKnockbackDuration = 0.25f;

    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }

    /// <summary>
    /// 현재 오브젝트가 공격할 때 사용할 기본 피해량을 반환합니다.
    /// Status.attackPower와 Damage.baseDamage를 합산하므로 무기/스킬 시스템의 기본값으로 사용할 수 있습니다.
    /// </summary>
    public float GetOutgoingDamage()
    {
        HWJ_StatusData status = GetStatusData();
        HWJ_DamageData damage = GetDamageData();

        if (status == null || damage == null)
        {
            return 0f;
        }

        return status.attackPower + damage.baseDamage;
    }

    public float GetOutgoingDamage(float damageMultiplier)
    {
        return GetOutgoingDamage() * Mathf.Max(0f, damageMultiplier);
    }

    /// <summary>
    /// 외부에서 들어온 피해량에 방어력, 무적, 피해 배율을 적용한 최종 피해량을 반환합니다.
    /// 실제 HP 또는 부패 수치 차감은 이 값을 호출한 시스템에서 처리합니다.
    /// </summary>
    public float GetReceivedDamage(float incomingDamage)
    {
        return GetReceivedDamage(incomingDamage, HWJ_ObjectType.Player, false);
    }

    public float GetReceivedDamage(float incomingDamage, HWJ_ObjectType attackerType)
    {
        return GetReceivedDamage(incomingDamage, attackerType, true);
    }

    private float GetReceivedDamage(float incomingDamage, HWJ_ObjectType attackerType, bool hasAttackerType)
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage == null)
        {
            return incomingDamage;
        }

        if (receivedDamage.isInvincible)
        {
            return 0f;
        }

        if (hasAttackerType
            && receivedDamage.ignoreEnemyDamage
            && (attackerType == HWJ_ObjectType.Enemy || attackerType == HWJ_ObjectType.Boss))
        {
            return 0f;
        }

        HWJ_StatusData status = GetStatusData();
        float defense = status != null ? status.defense : 0f;
        float reducedDamage = Mathf.Max(0f, incomingDamage - defense);
        return reducedDamage * receivedDamage.damageMultiplier;
    }

    public bool TryDealDamageTo(HWJ_RootObjectDataResolver targetResolver, out float finalDamage)
    {
        return TryDealDamageTo(targetResolver, 1f, out finalDamage);
    }

    public bool TryDealDamageTo(
        HWJ_RootObjectDataResolver targetResolver,
        float damageMultiplier,
        out float finalDamage)
    {
        finalDamage = 0f;

        if (!CanDealDamageTo(targetResolver))
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_CombatSystem targetCombat = targetResolver.GetComponent<HWJ_CombatSystem>();
        HWJ_DamageData damageData = GetDamageData();
        float outgoingDamage = Mathf.Max(0f, GetOutgoingDamage(damageMultiplier));

        finalDamage = targetCombat != null
            ? targetCombat.GetReceivedDamage(outgoingDamage, dataResolver != null ? dataResolver.ObjectType : HWJ_ObjectType.Player)
            : outgoingDamage;

        if (finalDamage <= 0f)
        {
            return false;
        }

        targetStatus.ApplyDamage(finalDamage);
        ApplyKnockback(targetResolver, damageData);
        return true;
    }

    public bool CanDealDamageTo(HWJ_RootObjectDataResolver targetResolver)
    {
        if (targetResolver == null || targetResolver == dataResolver)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        if (targetStatus == null || targetStatus.IsDead)
        {
            return false;
        }

        return runtimeStatus == null || !runtimeStatus.IsDead;
    }

    private void ApplyKnockback(HWJ_RootObjectDataResolver targetResolver, HWJ_DamageData damageData)
    {
        if (targetResolver == null || damageData == null || damageData.knockbackPower <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = targetResolver.GetComponent<HWJ_KnockbackSystem>();

        if (knockbackSystem == null)
        {
            knockbackSystem = targetResolver.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = targetResolver.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            float fallbackX = Mathf.Sign(targetResolver.transform.position.x - transform.position.x);
            direction = new Vector2(fallbackX == 0f ? 1f : fallbackX, 0f);
        }

        float duration = damageData.hitStunSeconds > 0f
            ? damageData.hitStunSeconds
            : DefaultKnockbackDuration;
        knockbackSystem.PlayKnockback(direction, damageData.knockbackPower, duration);
    }

    private HWJ_StatusData GetStatusData()
    {
        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return possessedStatus;
        }

        return dataResolver != null ? dataResolver.Status : null;
    }

    private HWJ_DamageData GetDamageData()
    {
        if (possessionSystem != null && possessionSystem.TryGetPossessedDamage(out HWJ_DamageData possessedDamage))
        {
            return possessedDamage;
        }

        return dataResolver != null ? dataResolver.Damage : null;
    }

    private HWJ_ReceivedDamageData GetReceivedDamageData()
    {
        if (possessionSystem != null
            && possessionSystem.TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData possessedReceivedDamage))
        {
            return possessedReceivedDamage;
        }

        return dataResolver != null ? dataResolver.ReceivedDamage : null;
    }
}
