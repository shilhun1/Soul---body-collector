using UnityEngine;

/// <summary>
/// 보스 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 보스 등급, AI, 스킬 사이클, 페이즈, 변신, 빙의 가능 몸 데이터를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_BossTypeData", menuName = "HWJ/Data/Type Data/Boss")]
public class HWJ_BossTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("Boss")]
    [SerializeField] private HWJ_BossRank bossRank;

    [Header("Common FSM")]
    [SerializeField] private HWJ_BossFSMData fsm = new HWJ_BossFSMData();

    [Header("AI")]
    [SerializeField] private HWJ_AIData ai = new HWJ_AIData();
    [SerializeField] private HWJ_NavigationData navigation = new HWJ_NavigationData();

    [Header("Combat")]
    [SerializeField] private HWJ_SkillSetData skillCycle = new HWJ_SkillSetData();

    [Header("Phase")]
    [SerializeField] private HWJ_BossPhaseData[] phases;
    [SerializeField] private HWJ_BossTransformData phaseTransform = new HWJ_BossTransformData();

    [Header("Possession")]
    [SerializeField] private HWJ_PossessionData possessionBody = new HWJ_PossessionData();

    public HWJ_BossRank BossRank => bossRank;
    public HWJ_BossFSMData FSM => fsm;
    public HWJ_AIData AI => ai;
    public HWJ_NavigationData Navigation => navigation;
    public HWJ_SkillSetData SkillCycle => skillCycle;
    public HWJ_BossPhaseData[] Phases => phases;
    public HWJ_BossTransformData PhaseTransform => phaseTransform;
    public HWJ_PossessionData PossessionBody => possessionBody;
}
