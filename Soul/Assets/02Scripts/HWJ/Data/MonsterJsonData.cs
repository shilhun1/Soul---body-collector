using System; // Serializable을 사용하기 위해 필요합니다.

[Serializable] // JSON에서 몬스터 데이터를 읽을 수 있게 합니다.
public class MonsterJsonData // 몬스터 기본 수치와 드롭 정보를 담습니다.
{
    public string id; // 몬스터 고유 ID입니다.
    public string name; // 몬스터 이름입니다. 이름을 쓰지 않으면 빈 문자열로 둡니다.

    public int maxHp; // 최대 체력입니다.
    public int physicalAttack; // 물리 공격력입니다.
    public int magicAttack; // 마법 공격력입니다.
    public int defense; // 방어력입니다.
    public float moveSpeed; // 이동 속도입니다.
    public float attackSpeed; // 공격 속도입니다.

    public int soulReward; // 처치 시 영혼 보상입니다. 현재 구조에서는 0으로 두어도 됩니다.
    public int expReward; // 처치 시 경험치 보상입니다.

    public bool canSpawnPossessionBody; // 죽었을 때 빙의체를 생성할 수 있는지 여부입니다.
    public string possessionBodyId; // 생성할 빙의체 ID입니다.

    public bool isEnhanced; // 강화체 몬스터인지 여부입니다.
    public bool canDropStatOrb; // 죽었을 때 능력치 구슬을 드롭할 수 있는지 여부입니다.
    public string statOrbId; // 드롭할 능력치 구슬 ID입니다.
}
