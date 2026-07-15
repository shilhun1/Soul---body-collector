using UnityEngine;

/// <summary>
/// 보스 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 보스 등급, AI, 스킬 사이클, 페이즈, 변신, 빙의 가능 몸 데이터를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_BossTypeData", menuName = "HWJ/Data/Type Data/Boss")]
public class HWJ_BossTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("보스 기본")]
    [Tooltip("보스의 등급 또는 분류입니다.")]
    [InspectorName("보스 등급")]
    [SerializeField] private HWJ_BossRank bossRank;

    [Header("공통 FSM")]
    [Tooltip("보스방 인식, 추적 거리, 페이즈 전환, 그로기 같은 공통 보스 행동 설정입니다.")]
    [InspectorName("FSM 데이터")]
    [SerializeField] private HWJ_BossFSMData fsm = new HWJ_BossFSMData();

    [Header("AI와 이동")]
    [Tooltip("보스 AI의 판단 주기와 상태별 시간을 설정합니다.")]
    [InspectorName("AI 시간 설정")]
    [SerializeField] private HWJ_AIData ai = new HWJ_AIData();
    [Tooltip("보스 이동과 위치 보정에 사용하는 데이터입니다.")]
    [InspectorName("이동/네비게이션")]
    [SerializeField] private HWJ_NavigationData navigation = new HWJ_NavigationData();

    [Header("전투")]
    [Tooltip("보스가 기본적으로 사용할 수 있는 스킬 목록입니다.")]
    [InspectorName("보스 스킬 사이클")]
    [SerializeField] private HWJ_SkillSetData skillCycle = new HWJ_SkillSetData();

    [Header("페이즈")]
    [Tooltip("HP 비율별 페이즈와 페이즈별 스킬 목록입니다.")]
    [InspectorName("페이즈 목록")]
    [SerializeField] private HWJ_BossPhaseData[] phases;
    [Tooltip("페이즈 전환 시 모델이나 애니메이션을 바꾸는 설정입니다.")]
    [InspectorName("페이즈 변신 데이터")]
    [SerializeField] private HWJ_BossTransformData phaseTransform = new HWJ_BossTransformData();

    [Header("보스전 입장")]
    [Tooltip("보스전 시작 전에 검사해야 하는 조건입니다.")]
    [InspectorName("입장 조건")]
    [SerializeField] private HWJ_BossEntryRequirementData entryRequirements = new HWJ_BossEntryRequirementData();

    [Header("빙의")]
    [Tooltip("보스 몸이 빙의 가능한 대상인지 설정합니다.")]
    [InspectorName("빙의 대상 설정")]
    [SerializeField] private HWJ_PossessionData possessionBody = new HWJ_PossessionData();

    public HWJ_BossRank BossRank => bossRank;
    public HWJ_BossFSMData FSM => fsm;
    public HWJ_AIData AI => ai;
    public HWJ_NavigationData Navigation => navigation;
    public HWJ_SkillSetData SkillCycle => skillCycle;
    public HWJ_BossPhaseData[] Phases => phases;
    public HWJ_BossTransformData PhaseTransform => phaseTransform;
    public HWJ_BossEntryRequirementData EntryRequirements => entryRequirements;
    public HWJ_PossessionData PossessionBody => possessionBody;
}
