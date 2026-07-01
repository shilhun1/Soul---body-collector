using System; // Serializable을 사용하기 위해 필요합니다.
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

[Serializable] // JSON에서 빙의체 데이터를 읽을 수 있게 합니다.
public class PossessionBodyJsonData // 플레이어가 빙의했을 때 적용되는 데이터입니다.
{
    public string id; // 빙의체 고유 ID입니다.
    public string name; // 빙의체 이름입니다.

    public int maxHp; // 빙의 중 적용할 최대 체력 보너스입니다.
    public int physicalAttack; // 빙의 중 적용할 물리 공격력 보너스입니다.
    public int magicAttack; // 빙의 중 적용할 마법 공격력 보너스입니다.
    public int defense; // 빙의 중 적용할 방어력 보너스입니다.
    public float moveSpeed; // 빙의 중 적용할 이동 속도 보너스입니다.
    public float attackSpeed; // 빙의 중 적용할 공격 속도 보너스입니다.

    public float duration; // 빙의 지속 시간입니다.
    public string possessionBehaviourId; // 실제 빙의 행동 클래스와 연결할 ID입니다.
    public string jumpDataId; // 빙의 중 사용할 점프 데이터 ID입니다.
    public List<string> skillIds = new List<string>(); // 빙의 중 사용할 스킬 ID 목록입니다.
}
