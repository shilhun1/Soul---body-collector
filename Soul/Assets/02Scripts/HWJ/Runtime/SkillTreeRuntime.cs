using System.Collections.Generic; // HashSet을 사용하기 위해 불러옵니다.
using UnityEngine; // MonoBehaviour를 사용하기 위해 불러옵니다.

public class SkillTreeRuntime : MonoBehaviour // 레벨업 스킬 포인트로 스킬 트리를 해금합니다.
{
    [SerializeField] private PlayerLevelRuntime levelRuntime; // 플레이어 레벨과 스킬 포인트를 관리하는 컴포넌트입니다.
    [SerializeField] private PlayerStatController statController; // 스킬 트리 능력치 보너스를 적용할 컴포넌트입니다.
    [SerializeField] private SkillController skillController; // 해금 스킬을 추가할 컴포넌트입니다.

    private readonly HashSet<string> unlockedNodeIds = new HashSet<string>(); // 해금된 노드 ID 목록입니다.
    private readonly StatData unlockedStatBonus = new StatData(); // 해금된 노드들의 누적 능력치 보너스입니다.

    private void Awake() // 오브젝트 생성 시 호출됩니다.
    { 
        if (levelRuntime == null) levelRuntime = GetComponent<PlayerLevelRuntime>(); // 레벨 런타임을 찾습니다.
        if (statController == null) statController = GetComponent<PlayerStatController>(); // 능력치 컨트롤러를 찾습니다.
        if (skillController == null) skillController = GetComponent<SkillController>(); // 스킬 컨트롤러를 찾습니다.
    }

    public bool CanUnlock(string nodeId) // 특정 노드를 해금할 수 있는지 확인합니다.
    { 
        SkillTreeNodeJsonData node = GameDataManager.Instance.GetSkillTreeNode(nodeId); // 노드 데이터를 가져옵니다.

        if (node == null) return false; // 노드 데이터가 없으면 해금할 수 없습니다.
        if (levelRuntime == null) return false; // 레벨 런타임이 없으면 해금할 수 없습니다.
        if (unlockedNodeIds.Contains(nodeId)) return false; // 이미 해금된 노드는 다시 해금할 수 없습니다.
        if (levelRuntime.SkillPoint <= 0) return false; // 스킬 포인트가 없으면 해금할 수 없습니다.
        if (levelRuntime.CurrentLevel < node.requiredLevel) return false; // 요구 레벨보다 낮으면 해금할 수 없습니다.

        foreach (string requiredId in node.requiredNodeIds) // 선행 노드들을 확인합니다.
        {
            if (!unlockedNodeIds.Contains(requiredId)) // 선행 노드가 해금되지 않았는지 확인합니다.
            {
                return false; // 선행 조건을 만족하지 못하면 해금할 수 없습니다.
            }
        }

        return true; // 모든 조건을 만족하면 해금할 수 있습니다.
    }

    public bool Unlock(string nodeId) // 스킬 포인트를 사용해서 노드를 해금합니다.
    {
        if (!CanUnlock(nodeId)) return false; // 해금 조건을 만족하지 못하면 실패합니다.

        SkillTreeNodeJsonData node = GameDataManager.Instance.GetSkillTreeNode(nodeId); // 노드 데이터를 가져옵니다.

        if (!levelRuntime.TryUseSkillPoint()) return false; // 스킬 포인트 1개를 사용합니다.

        unlockedNodeIds.Add(nodeId); // 해금 목록에 노드를 추가합니다.
        unlockedStatBonus.Add(node.statBouns); // 노드 능력치 보너스를 누적합니다.

        if (statController != null) // 능력치 컨트롤러가 있는지 확인합니다.
        {
            statController.SetSkillTreeBonus(unlockedStatBonus); // 누적 스킬 트리 보너스를 적용합니다.
        } 

        if (skillController != null && !string.IsNullOrEmpty(node.unlockSkillId)) // 해금할 스킬이 있는지 확인합니다.
        {
            skillController.AddSkill(node.unlockSkillId); // 스킬을 추가합니다.
        } 

        return true;
    }
} 