using System; // Serializable을 사용하기 위해 필요합니다.
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

[Serializable] // JSON에서 스킬 트리 노드 데이터를 읽을 수 있게 합니다.
public class SkillTreeNodeJsonData // 스킬 트리 해금 조건과 보상을 담습니다.
{
    public string id; // 스킬 트리 노드 고유 ID입니다.
    public string name; // 노드 이름입니다.
    public int requiredLevel; // 해금에 필요한 최소 플레이어 레벨입니다.
    public List<string> requiredNodeIds = new List<string>(); // 먼저 해금해야 하는 노드 ID 목록입니다.
    public StatData statBonus; // 해금 시 적용되는 능력치 보너스입니다.
    public string unlockSkillId; // 해금 시 추가되는 스킬 ID입니다.
}
