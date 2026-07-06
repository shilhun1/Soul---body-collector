using UnityEngine;

/// <summary>
/// NPC 오브젝트에 붙는 기본 상호작용 컴포넌트입니다.
/// NPCTypeDataSO의 dialogueId, behaviorId, 버프 정보를 읽어 대화/버프 시스템으로 넘기는 진입점 역할을 합니다.
/// </summary>
public class HWJ_NPCInteractionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;

    public string DialogueId { get; private set; }
    public string BehaviorId { get; private set; }

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }
    }

    /// <summary>
    /// NPC와 상호작용을 시도합니다.
    /// 현재는 ID를 읽어두는 기본 처리만 하며, 실제 대화 UI는 DialogueId를 받아 연결하면 됩니다.
    /// </summary>
    public bool TryInteract(GameObject interactor)
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_NPCTypeDataSO npcData))
        {
            return false;
        }

        DialogueId = npcData.DialogueId;
        BehaviorId = npcData.BehaviorId;
        return true;
    }
}
