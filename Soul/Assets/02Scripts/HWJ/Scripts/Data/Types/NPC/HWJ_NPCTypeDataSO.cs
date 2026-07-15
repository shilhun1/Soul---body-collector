using UnityEngine;

/// <summary>
/// NPC 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 대화, 행동 ID, 상호작용/버프 정보를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_NPCTypeData", menuName = "HWJ/Data/Type Data/NPC")]
public class HWJ_NPCTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("상호작용")]
    [Tooltip("NPC와 상호작용할 거리, 버프 보상 등을 설정합니다.")]
    [InspectorName("상호작용 데이터")]
    [SerializeField] private HWJ_NPCInteractionData interaction = new HWJ_NPCInteractionData();

    [Header("콘텐츠")]
    [Tooltip("대화 시스템에서 찾을 대화 ID입니다.")]
    [InspectorName("대화 ID")]
    [SerializeField] private string dialogueId;
    [Tooltip("NPC 행동 프로필 또는 스크립트에서 사용할 행동 ID입니다.")]
    [InspectorName("행동 ID")]
    [SerializeField] private string behaviorId;

    public HWJ_NPCInteractionData Interaction => interaction;
    public string DialogueId => dialogueId;
    public string BehaviorId => behaviorId;
}
