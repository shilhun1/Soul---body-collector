using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_EnemyTypeData", menuName = "HWJ/Data/Type Data/Enemy")]
public class HWJ_EnemyTypeDataSO : HWJ_ObjectTypeDataSO
{
    [SerializeField] private HWJ_TrackingData tracking;
    [SerializeField] private HWJ_AIData ai;
    [SerializeField] private HWJ_NavigationData navigation;
    [SerializeField] private HWJ_SkillSetData skillCycle;
    [SerializeField] private HWJ_PossessionData possessionBody;

    public HWJ_TrackingData Tracking => tracking;
    public HWJ_AIData AI => ai;
    public HWJ_NavigationData Navigation => navigation;
    public HWJ_SkillSetData SkillCycle => skillCycle;
    public HWJ_PossessionData PossessionBody => possessionBody;
}
