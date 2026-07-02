using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_NPCTypeData", menuName = "HWJ/Data/Type Data/NPC")]
public class HWJ_NPCTypeDataSO : HWJ_ObjectTypeDataSO
{
    [SerializeField] private HWJ_NPCInteractionData interaction;
    [SerializeField] private string dialogueId;
    [SerializeField] private string behaviorId;

    public HWJ_NPCInteractionData Interaction => interaction;
    public string DialogueId => dialogueId;
    public string BehaviorId => behaviorId;
}
