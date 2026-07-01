using System; // Serializable을 사용하기 위해 필요합니다.

[Serializable] // Unity JsonUtility가 읽을 수 있는 데이터로 만듭니다.
public class StatData // 체력, 공격력, 방어력, 속도 같은 능력치 묶음입니다.
{
    public int maxHp; // 최대 체력입니다.
    public int physicalAttack; // 물리 공격력입니다.
    public int magicAttack; // 마법 공격력입니다.
    public int defense; // 방어력입니다.
    public float moveSpeed; // 이동 속도입니다.
    public float attackSpeed; // 공격 속도입니다.

    public void Add(StatData other) // 다른 능력치를 현재 능력치에 더합니다.
    {
        if (other == null) // 더할 데이터가 없으면
        {
            return; // 아무 처리도 하지 않습니다.
        }

        maxHp += other.maxHp; // 최대 체력을 더합니다.
        physicalAttack += other.physicalAttack; // 물리 공격력을 더합니다.
        magicAttack += other.magicAttack; // 마법 공격력을 더합니다.
        defense += other.defense; // 방어력을 더합니다.
        moveSpeed += other.moveSpeed; // 이동 속도를 더합니다.
        attackSpeed += other.attackSpeed; // 공격 속도를 더합니다.
    }

    public StatData Clone() // 원본 능력치를 직접 수정하지 않도록 복사본을 만듭니다.
    {
        return new StatData // 현재 값과 같은 새 StatData를 반환합니다.
        {
            maxHp = maxHp, // 최대 체력을 복사합니다.
            physicalAttack = physicalAttack, // 물리 공격력을 복사합니다.
            magicAttack = magicAttack, // 마법 공격력을 복사합니다.
            defense = defense, // 방어력을 복사합니다.
            moveSpeed = moveSpeed, // 이동 속도를 복사합니다.
            attackSpeed = attackSpeed // 공격 속도를 복사합니다.
        };
    }
}
