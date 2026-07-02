using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_PlayerTypeData", menuName = "HWJ/Data/Type Data/Player")]
public class HWJ_PlayerTypeDataSO : HWJ_ObjectTypeDataSO
{
    [SerializeField] private HWJ_ControlData control;
    [SerializeField] private HWJ_SkillSetData skillSet;
    [SerializeField] private HWJ_SoulStateData soulState;
    [SerializeField] private HWJ_PossessionData possession;
    [SerializeField] private HWJ_BodyDecayData bodyDecay;

    public HWJ_ControlData Control => control;
    public HWJ_SkillSetData SkillSet => skillSet;
    public HWJ_SoulStateData SoulState => soulState;
    public HWJ_PossessionData Possession => possession;
    public HWJ_BodyDecayData BodyDecay => bodyDecay;
}
