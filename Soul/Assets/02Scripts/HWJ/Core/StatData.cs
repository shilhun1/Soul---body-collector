using UnityEngine;
using System;

[Serializable]
public class StatData
{
    public int maxHp; // 체력
    public int physicalAttack; // 물리 공격력
    public int defense; //  방어력
    public float moveSpeed; // 이동 속도
    public float attackSpeed; // 공격속도

    public void Add(StatData other) // 다른 능력치를 현재 능력치에 더함
    {
        if (other == null) // 더할 데이터가 없는지 확인
        {
            return; // 데이터 없으면 함수 종료
        }
        maxHp += other.maxHp; // 최대 체력 보너스
        physicalAttack += other.physicalAttack; // 물리 공격력 보너스
        defense += other.defense; // 방어력 보너스
        moveSpeed += other.moveSpeed; // 이동 속도 보너스
        attackSpeed += other.attackSpeed; // 공격 속도 보너스
    }

    public StatData Clone() // 원본 능력치를 복사
    {
        return new StatData // 새 능력치 객체를 만들어 반환
        {
            maxHp = maxHp, //최대체력
            physicalAttack = physicalAttack, //물리 공격력
            defense = defense, // 방어력
            moveSpeed = moveSpeed, // 이동 속도 복사
            attackSpeed = attackSpeed // 공격 속도 복사
        };
    }
}