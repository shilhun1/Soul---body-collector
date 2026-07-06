using UnityEngine;

/// <summary>
/// 플레이어, 적, 보스가 공통으로 사용할 런타임 HP와 상태를 관리합니다.
/// 원본 능력치는 RootObjectDataSO에서 읽고, 구슬/버프 같은 임시 변화는 이 컴포넌트의 보너스로만 관리합니다.
/// </summary>
public class HWJ_RuntimeStatusSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeState currentState = HWJ_RuntimeState.Idle;
    [SerializeField] private float currentHp;
    [SerializeField] private float soulHp;
    [SerializeField] private float possessedBodyHp;

    private float maxHpBonus;
    private float moveSpeedBonus;
    private float attackPowerBonus;
    private float defenseBonus;
    private float attackSpeedBonus;

    public HWJ_RuntimeState CurrentState => currentState;
    public float CurrentHp => currentHp;
    public float SoulHp => soulHp;
    public float PossessedBodyHp => possessedBodyHp;
    public float SoulMaxHp => GetOwnerBaseStatusValue(status => status.maxHp) + maxHpBonus;
    public float MaxHp => GetBaseStatusValue(status => status.maxHp) + maxHpBonus;
    public float MoveSpeed => GetBaseStatusValue(status => status.moveSpeed) + moveSpeedBonus;
    public float AttackPower => GetBaseStatusValue(status => status.attackPower) + attackPowerBonus;
    public float Defense => GetBaseStatusValue(status => status.defense) + defenseBonus;
    public float AttackSpeed => GetBaseStatusValue(status => status.attackSpeed) + attackSpeedBonus;
    public bool UsesHp => MaxHp > 0f;
    public bool IsDead => currentState == HWJ_RuntimeState.Dead
        || (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        || (soulSystem == null && UsesHp && currentHp <= 0f);

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

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        soulHp = SoulMaxHp;
        possessedBodyHp = MaxHp;
        currentHp = GetStoredHpForActiveState();
    }

    /// <summary>
    /// 현재 상태를 변경합니다.
    /// 이동, 공격, AI, UI가 같은 상태 값을 보도록 한 곳에서 관리합니다.
    /// </summary>
    public void SetState(HWJ_RuntimeState state)
    {
        currentState = state;
    }

    /// <summary>
    /// 피해를 적용합니다.
    /// 플레이어는 빙의 상태 HP가 0이면 영혼 상태가 되고, 영혼 상태 HP가 0이면 게임오버 상태가 됩니다.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (IsDead)
        {
            return;
        }

        currentHp = Mathf.Max(0f, currentHp - damage);
        CacheCurrentHpForActiveState();

        if (currentHp <= 0f)
        {
            HandleEmptyHp();
        }
        else
        {
            SetState(HWJ_RuntimeState.Hit);
        }
    }

    /// <summary>
    /// HP를 회복합니다.
    /// 최대 HP를 넘지 않도록 RuntimeStatusSystem에서 제한합니다.
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHp = Mathf.Min(MaxHp, currentHp + amount);
        CacheCurrentHpForActiveState();
    }

    /// <summary>
    /// 현재 데이터 기준으로 HP를 다시 맞춥니다.
    /// 빙의 성공으로 육신 스탯을 읽기 시작할 때 최대 HP를 새 몸 기준으로 갱신합니다.
    /// </summary>
    public void RefreshCurrentHpFromData(bool refillToMax)
    {
        if (soulSystem == null)
        {
            currentHp = refillToMax ? MaxHp : Mathf.Min(currentHp, MaxHp);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body && possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            float bodyMaxHp = MaxHp;
            possessedBodyHp = refillToMax ? bodyMaxHp : Mathf.Min(possessedBodyHp, bodyMaxHp);
            currentHp = possessedBodyHp;
            return;
        }

        float soulMaxHp = SoulMaxHp;
        soulHp = refillToMax ? soulMaxHp : Mathf.Min(soulHp, soulMaxHp);
        currentHp = soulHp;
    }

    /// <summary>
    /// 현재 표시 중인 HP를 영혼 HP 또는 빙의 육신 HP 저장값에 반영합니다.
    /// 상태 전환 직전에 호출해서 두 체력 풀을 서로 덮어쓰지 않게 합니다.
    /// </summary>
    public void CacheCurrentHpForActiveState()
    {
        if (soulSystem == null)
        {
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            possessedBodyHp = currentHp;
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            soulHp = currentHp;
        }
    }

    /// <summary>
    /// 능력치 구슬 데이터를 런타임 보너스로 적용합니다.
    /// ScriptableObject 원본 스탯은 수정하지 않기 때문에 데이터 오염을 막을 수 있습니다.
    /// </summary>
    public void ApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        if (statOrbData == null)
        {
            return;
        }

        switch (statOrbData.OrbType)
        {
            case HWJ_StatOrbType.MaxHp:
                maxHpBonus += statOrbData.Amount;
                currentHp = Mathf.Min(MaxHp, currentHp + statOrbData.Amount);
                CacheCurrentHpForActiveState();
                break;
            case HWJ_StatOrbType.MoveSpeed:
                moveSpeedBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.AttackPower:
                attackPowerBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.Defense:
                defenseBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.AttackSpeed:
                attackSpeedBonus += statOrbData.Amount;
                break;
        }
    }

    private float GetBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return selector(possessedStatus);
        }

        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetOwnerBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetStoredHpForActiveState()
    {
        if (soulSystem == null)
        {
            return MaxHp;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            return possessedBodyHp;
        }

        return soulHp;
    }

    private void HandleEmptyHp()
    {
        if (soulSystem == null)
        {
            SetState(HWJ_RuntimeState.Dead);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            soulSystem.EnterSoulState(false);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            soulSystem.EnterDeadState();
            return;
        }

        SetState(HWJ_RuntimeState.Dead);
    }
}
