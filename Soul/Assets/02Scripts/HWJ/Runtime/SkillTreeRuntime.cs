using System.Collections.Generic; // HashSet을 사용하기 위해 필요합니다.
using UnityEngine; // MonoBehaviour를 사용하기 위해 필요합니다.

public class SkillTreeRuntime : MonoBehaviour // 레벨업으로 얻은 스킬 포인트를 사용해 스킬 트리를 해금합니다.
{
    [SerializeField] private PlayerLevelRuntime levelRuntime; // 레벨과 스킬 포인트를 확인할 컴포넌트입니다.
    [SerializeField] private PlayerStatController statController; // 능력치 보너스를 적용할 컴포넌트입니다.
    [SerializeField] private SkillController skillController; // 해금 스킬을 추가할 컴포넌트입니다.

    private readonly HashSet<string> unlockedNodeIds = new HashSet<string>(); // 이미 해금된 노드 ID 목록입니다.
    private readonly StatData totalSkillTreeBonus = new StatData(); // 스킬 트리에서 얻은 누적 능력치 보너스입니다.

    private void Awake() // 오브젝트가 생성될 때 실행됩니다.
    {
        if (levelRuntime == null) levelRuntime = GetComponent<PlayerLevelRuntime>(); // 같은 오브젝트에서 레벨 런타임을 찾습니다.
        if (statController == null) statController = GetComponent<PlayerStatController>(); // 같은 오브젝트에서 능력치 컨트롤러를 찾습니다.
        if (skillController == null) skillController = GetComponent<SkillController>(); // 같은 오브젝트에서 스킬 컨트롤러를 찾습니다.
    }

    public bool CanUnlock(string nodeId) // 특정 노드를 해금할 수 있는지 확인합니다.
    {
        SkillTreeNodeJsonData node = GameDataManager.Instance.GetSkillTreeNode(nodeId); // 노드 데이터를 가져옵니다.

        if (node == null) return false; // 노드 데이터가 없으면 실패입니다.
        if (levelRuntime == null) return false; // 레벨 런타임이 없으면 실패입니다.
        if (unlockedNodeIds.Contains(nodeId)) return false; // 이미 해금된 노드는 실패입니다.
        if (levelRuntime.SkillPoint <= 0) return false; // 스킬 포인트가 없으면 실패입니다.
        if (levelRuntime.CurrentLevel < node.requiredLevel) return false; // 요구 레벨보다 낮으면 실패입니다.

        foreach (string requiredNodeId in node.requiredNodeIds) // 선행 노드를 검사합니다.
        {
            if (!unlockedNodeIds.Contains(requiredNodeId)) // 선행 노드가 해금되지 않았다면
            {
                return false; // 실패입니다.
            }
        }

        return true; // 모든 조건을 만족하면 해금 가능합니다.
    }

    public bool Unlock(string nodeId) // 스킬 포인트를 사용해 노드를 해금합니다.
    {
        if (!CanUnlock(nodeId)) return false; // 조건을 만족하지 못하면 실패입니다.

        SkillTreeNodeJsonData node = GameDataManager.Instance.GetSkillTreeNode(nodeId); // 해금할 노드 데이터를 가져옵니다.

        if (!levelRuntime.TryUseSkillPoint()) return false; // 스킬 포인트 1개 사용을 시도합니다.

        unlockedNodeIds.Add(nodeId); // 해금 목록에 추가합니다.
        totalSkillTreeBonus.Add(node.statBonus); // 노드 능력치 보너스를 누적합니다.

        if (statController != null) statController.SetSkillTreeBonus(totalSkillTreeBonus.Clone()); // 누적 능력치 보너스를 적용합니다.
        if (skillController != null && !string.IsNullOrEmpty(node.unlockSkillId)) skillController.AddSkill(node.unlockSkillId); // 해금 스킬이 있으면 추가합니다.

        return true; // 해금 성공입니다.
    }

    public bool IsUnlocked(string nodeId) // 노드가 이미 해금되었는지 확인합니다.
    {
        return unlockedNodeIds.Contains(nodeId); // 해금 목록에 있으면 true입니다.
    }
}
