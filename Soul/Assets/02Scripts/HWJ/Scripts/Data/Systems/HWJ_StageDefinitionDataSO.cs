using UnityEngine;

/// <summary>
/// Definition data for one stage and its progression links. Runtime state is stored separately in save DTOs.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_StageDefinitionData", menuName = "HWJ/Data/System/Stage Definition")]
public class HWJ_StageDefinitionDataSO : ScriptableObject
{
    [SerializeField] private string stageId;
    [SerializeField] private string displayName;
    [SerializeField] private string regionId;
    [SerializeField] private string nextStageId;
    [SerializeField] private bool hasBoss;
    [SerializeField] private string bossId;
    [SerializeField] private bool unlocksRegionOnClear;
    [SerializeField] private string unlockRegionId;
    [SerializeField] private string[] requiredClearedStageIds;
    [SerializeField] private string[] requiredUnlockedRegionIds;

    public string StageId => stageId;
    public string DisplayName => displayName;
    public string RegionId => regionId;
    public string NextStageId => nextStageId;
    public bool HasBoss => hasBoss;
    public string BossId => bossId;
    public bool UnlocksRegionOnClear => unlocksRegionOnClear;
    public string UnlockRegionId => unlockRegionId;
    public string[] RequiredClearedStageIds => requiredClearedStageIds;
    public string[] RequiredUnlockedRegionIds => requiredUnlockedRegionIds;
}
