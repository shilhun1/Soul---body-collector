using UnityEngine;

/// <summary>
/// 일반 적 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 적 역할, 추적, AI, 네비게이션, 스킬, 빙의 가능 몸 데이터를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_EnemyTypeData", menuName = "HWJ/Data/Type Data/Enemy")]
public class HWJ_EnemyTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("몬스터 역할")]
    [Tooltip("무기, 엘리트 여부, 시체/빙의/스탯 구슬 관련 설정입니다.")]
    [InspectorName("역할 데이터")]
    [SerializeField] private HWJ_EnemyRoleData role = new HWJ_EnemyRoleData();

    [Header("AI와 이동")]
    [Tooltip("플레이어를 인식하고 추적하는 거리 설정입니다.")]
    [InspectorName("추적 데이터")]
    [SerializeField] private HWJ_TrackingData tracking = new HWJ_TrackingData();
    [Tooltip("대기, 인식, 공격 준비, 후딜, 스킬 기본 쿨타임 같은 AI 시간 설정입니다.")]
    [InspectorName("AI 시간 설정")]
    [SerializeField] private HWJ_AIData ai = new HWJ_AIData();
    [Tooltip("멈춤 거리, 경로 갱신, 낙하 방지 같은 이동 설정입니다.")]
    [InspectorName("이동/네비게이션")]
    [SerializeField] private HWJ_NavigationData navigation = new HWJ_NavigationData();
    [Tooltip("시작 상태, 공격 시작 거리, 피격 반응 같은 상태 설정입니다.")]
    [InspectorName("상태 규칙")]
    [SerializeField] private HWJ_EnemyStateData state = new HWJ_EnemyStateData();

    [Header("전투와 빙의")]
    [Tooltip("몬스터가 살아있을 때 AI로 사용하는 스킬 목록입니다.")]
    [InspectorName("몬스터 스킬 사이클")]
    [SerializeField] private HWJ_SkillSetData skillCycle = new HWJ_SkillSetData();
    [Tooltip("플레이어가 이 몸에 빙의했을 때 사용할 수 있는 스킬 목록입니다.")]
    [InspectorName("빙의 후 플레이어 스킬")]
    [SerializeField] private HWJ_SkillSetData playerPossessionSkillSet = new HWJ_SkillSetData();
    [Tooltip("이 몬스터가 빙의 가능한 몸인지, 처치 상태가 필요한지 설정합니다.")]
    [InspectorName("빙의 대상 설정")]
    [SerializeField] private HWJ_PossessionData possessionBody = new HWJ_PossessionData();

    public HWJ_EnemyRoleData Role => role;
    public HWJ_TrackingData Tracking => tracking;
    public HWJ_AIData AI => ai;
    public HWJ_NavigationData Navigation => navigation;
    public HWJ_EnemyStateData State => state;
    public HWJ_SkillSetData SkillCycle => skillCycle;
    public HWJ_SkillSetData PlayerPossessionSkillSet => playerPossessionSkillSet;
    public HWJ_PossessionData PossessionBody => possessionBody;
}
