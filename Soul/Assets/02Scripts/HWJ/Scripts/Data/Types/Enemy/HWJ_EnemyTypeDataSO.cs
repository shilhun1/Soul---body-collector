using UnityEngine;

/// <summary>
/// 일반 적 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 적 역할, 추적, AI, 네비게이션, 스킬, 빙의 가능 몸 데이터를 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_EnemyTypeData", menuName = "HWJ/Data/Type Data/Enemy")]
public class HWJ_EnemyTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("Role")]
    [SerializeField] private HWJ_EnemyRoleData role = new HWJ_EnemyRoleData();

    [Header("AI")]
    [SerializeField] private HWJ_TrackingData tracking = new HWJ_TrackingData();
    [SerializeField] private HWJ_AIData ai = new HWJ_AIData();
    [SerializeField] private HWJ_NavigationData navigation = new HWJ_NavigationData();
    [SerializeField] private HWJ_EnemyStateData state = new HWJ_EnemyStateData();

    [Header("Combat And Possession")]
    [SerializeField] private HWJ_SkillSetData skillCycle = new HWJ_SkillSetData();
    [SerializeField] private HWJ_SkillSetData playerPossessionSkillSet = new HWJ_SkillSetData();
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
