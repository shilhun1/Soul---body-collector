using UnityEngine;

public abstract class HWJ_CharacterBase : MonoBehaviour
{
    public int currentHp;
    public HWJ_StatData currentStats;

    public virtual void Init(HWJ_StatData stats)
    {
        currentStats = stats != null ? stats.Clone() : new HWJ_StatData();
        currentHp = Mathf.Max(1, currentStats.maxHp);
    }

    public virtual void TakeDamage(HWJ_DamageInfo damageInfo)
    {
        if (damageInfo == null)
        {
            Debug.LogError(name + " received null HWJ_DamageInfo.");
            return;
        }

        int finalDamage = CalculateDamage(damageInfo);
        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    protected virtual int CalculateDamage(HWJ_DamageInfo damageInfo)
    {
        if (damageInfo.damageType == HWJ_LegacyDamageType.True)
        {
            return Mathf.Max(1, damageInfo.amount);
        }

        int defense = currentStats != null ? currentStats.defense : 0;
        return Mathf.Max(1, damageInfo.amount - defense);
    }

    public abstract void Die();
}
