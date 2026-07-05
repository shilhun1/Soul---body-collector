using UnityEngine;

/// <summary>
/// 공통 전투 계산을 담당하는 컴포넌트입니다.
/// 공격하거나 피해를 받을 수 있는 플레이어, 적, 보스 오브젝트에 붙여 RootObjectData의 Status/Damage/ReceivedDamage를 사용합니다.
/// </summary>
public class HWJ_CombatSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

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

    /// <summary>
    /// 외부에서 들어온 피해량에 방어력, 무적, 피해 배율을 적용한 최종 피해량을 반환합니다.
    /// 실제 HP 또는 부패 수치 차감은 이 값을 호출한 시스템에서 처리합니다.
    /// </summary>
    public float GetReceivedDamage(float incomingDamage)
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

        HWJ_StatusData status = GetStatusData();
        float defense = status != null ? status.defense : 0f;
        float reducedDamage = Mathf.Max(0f, incomingDamage - defense);
        return reducedDamage * receivedDamage.damageMultiplier;
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
