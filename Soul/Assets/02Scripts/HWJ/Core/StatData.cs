using UnityEngine;
using System;

[Serializable]
public class StatData
{
    public int maxHp; // 체력
    public int physicalAttack; // 물리 공격력
    public int magicAttack; // 마법 공격력
    public int defense; //  방어력
    public float moveSpeed; // 이동 속도
    public float attackSpeed; // 공격속도

    public void Add(StatData other)
    {
        maxHp += other.maxHp; // 최대 체력 보너스
        physicalAttack += other.physicalAttack; // 물리 공격력 보너스
        defense += other.defense; // 방어력 보너스
        moveSpeed += other.moveSpeed; // 이동 속도 보너스
        attackSpeed += other.attackSpeed; // 공격 속도 보너스
    }

    public StatData Clone()
    {
        return new StatData
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