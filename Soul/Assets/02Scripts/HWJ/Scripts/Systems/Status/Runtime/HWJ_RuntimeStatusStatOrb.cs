using UnityEngine;

/// <summary>
/// Applies persistent stat-orb bonuses to HWJ_RuntimeStatusSystem.
/// The original RuntimeStatus component remains the only component added to objects.
/// </summary>
public partial class HWJ_RuntimeStatusSystem
{
    public bool CanApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return false;
        }

        HWJ_StatOrbProgressSystem statOrbProgressSystem = GetComponent<HWJ_StatOrbProgressSystem>();
        return statOrbProgressSystem == null || statOrbProgressSystem.CanApplyStatOrb(statOrbData);
    }

    public bool TryApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return false;
        }

        HWJ_StatOrbProgressSystem statOrbProgressSystem = GetComponent<HWJ_StatOrbProgressSystem>();

        if (statOrbProgressSystem != null)
        {
            return statOrbProgressSystem.TryApplyStatOrb(statOrbData, this, out _);
        }

        ApplyStatOrbBonus(statOrbData, false);
        return true;
    }

    public void ApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        TryApplyStatOrb(statOrbData);
    }

    public void ClearStatOrbBonuses()
    {
        ResolveReferences();

        maxHpBonus = 0f;
        moveSpeedBonus = 0f;
        attackPowerBonus = 0f;
        defenseBonus = 0f;
        attackSpeedBonus = 0f;
        RefreshCurrentHpFromData(false);
    }

    public void ApplyStatOrbBonus(HWJ_StatOrbDataSO statOrbData, bool preserveCurrentHp)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return;
        }

        switch (statOrbData.OrbType)
        {
            case HWJ_StatOrbType.MaxHp:
                maxHpBonus += statOrbData.Amount;
                if (preserveCurrentHp)
                {
                    RefreshCurrentHpFromData(false);
                }
                else
                {
                    currentHp = Mathf.Min(MaxHp, currentHp + statOrbData.Amount);
                    CacheCurrentHpForActiveState();
                }
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

}
