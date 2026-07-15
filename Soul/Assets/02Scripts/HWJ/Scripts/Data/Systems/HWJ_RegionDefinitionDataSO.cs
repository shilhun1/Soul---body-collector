using UnityEngine;

/// <summary>
/// Definition data for one region, its stage list, and next-region progression links.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_RegionDefinitionData", menuName = "HWJ/Data/System/Region Definition")]
public class HWJ_RegionDefinitionDataSO : ScriptableObject
{
    [SerializeField] private string regionId;
    [SerializeField] private string displayName;
    [SerializeField] private string firstStageId;
    [SerializeField] private string nextRegionId;
    [SerializeField] private string[] stageIds;
    [SerializeField] private string[] requiredClearedStageIds;
    [SerializeField] private string[] requiredUnlockedRegionIds;

    public string RegionId => regionId;
    public string DisplayName => displayName;
    public string FirstStageId => firstStageId;
    public string NextRegionId => nextRegionId;
    public string[] StageIds => stageIds;
    public string[] RequiredClearedStageIds => requiredClearedStageIds;
    public string[] RequiredUnlockedRegionIds => requiredUnlockedRegionIds;
}
