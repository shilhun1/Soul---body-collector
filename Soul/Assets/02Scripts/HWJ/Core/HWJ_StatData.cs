using System;

[Serializable]
public class HWJ_StatData
{
    public int maxHp;
    public int physicalAttack;
    public int magicAttack;
    public int defense;
    public float moveSpeed;
    public float attackSpeed;

    public void Add(HWJ_StatData other)
    {
        if (other == null)
        {
            return;
        }

        maxHp += other.maxHp;
        physicalAttack += other.physicalAttack;
        magicAttack += other.magicAttack;
        defense += other.defense;
        moveSpeed += other.moveSpeed;
        attackSpeed += other.attackSpeed;
    }

    public HWJ_StatData Clone()
    {
        return new HWJ_StatData
        {
            maxHp = maxHp,
            physicalAttack = physicalAttack,
            magicAttack = magicAttack,
            defense = defense,
            moveSpeed = moveSpeed,
            attackSpeed = attackSpeed
        };
    }
}
