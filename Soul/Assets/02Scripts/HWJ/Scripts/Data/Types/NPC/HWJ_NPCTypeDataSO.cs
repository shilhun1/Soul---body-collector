using UnityEngine;

/// <summary>
/// NPC 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 대화, 행동 ID, 상호작용/버프 정보를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_NPCTypeData", menuName = "HWJ/Data/Type Data/NPC")]
public class HWJ_NPCTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("Interaction")]
    [SerializeField] private HWJ_NPCInteractionData interaction = new HWJ_NPCInteractionData();

    [Header("Content")]
    [SerializeField] private string dialogueId;
    [SerializeField] private string behaviorId;

    public HWJ_NPCInteractionData Interaction => interaction;
    public string DialogueId => dialogueId;
    public string BehaviorId => behaviorId;
}
