using UnityEngine;

/// <summary>
/// 플레이어 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 조작, 스킬, 성장, 소울, 빙의, 부패 데이터가 함께 적용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_PlayerTypeData", menuName = "HWJ/Data/Type Data/Player")]
public class HWJ_PlayerTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("Control")]
    [SerializeField] private HWJ_ControlData control = new HWJ_ControlData();

    [Header("Attack And State")]
    [SerializeField] private HWJ_PlayerAttackData attack = new HWJ_PlayerAttackData();
    [SerializeField] private HWJ_PlayerStateData state = new HWJ_PlayerStateData();

    [Header("Camera")]
    [SerializeField] private HWJ_PlayerCameraData camera = new HWJ_PlayerCameraData();

    [Header("Skill And Growth")]
    [SerializeField] private HWJ_SkillSetData skillSet = new HWJ_SkillSetData();
    [SerializeField] private HWJ_GrowthData growth = new HWJ_GrowthData();

    [Header("Soul And Body")]
    [SerializeField] private HWJ_SoulStateData soulState = new HWJ_SoulStateData();
    [SerializeField] private HWJ_PossessionData possession = new HWJ_PossessionData();
    [SerializeField] private HWJ_BodyDecayData bodyDecay = new HWJ_BodyDecayData();

    public HWJ_ControlData Control => control;
    public HWJ_PlayerAttackData Attack => attack;
    public HWJ_PlayerStateData State => state;
    public HWJ_PlayerCameraData Camera => camera;
    public HWJ_SkillSetData SkillSet => skillSet;
    public HWJ_GrowthData Growth => growth;
    public HWJ_SoulStateData SoulState => soulState;
    public HWJ_PossessionData Possession => possession;
    public HWJ_BodyDecayData BodyDecay => bodyDecay;
}
