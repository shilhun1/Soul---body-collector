using UnityEngine;

public class HWJ_DamageInfo
{
    public int amount;
    public HWJ_LegacyDamageType damageType;
    public GameObject attacker;
    public Vector2 attackDirection;

    public HWJ_DamageInfo(
        int amount,
        HWJ_LegacyDamageType damageType,
        GameObject attacker,
        Vector2 attackDirection)
    {
        this.amount = amount;
        this.damageType = damageType;
        this.attacker = attacker;
        this.attackDirection = attackDirection;
    }
}
