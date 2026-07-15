using UnityEngine;

/// <summary>
/// Definition data for one region, its stage list, and next-region progression links.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_RegionDefinitionData", menuName = "HWJ/Data/System/Region Definition")]
public class HWJ_RegionDefinitionDataSO : ScriptableObject
{
    [Header("지역 기본 정보")]
    [Tooltip("저장과 진행도에서 사용할 고정 지역 ID입니다.")]
    [InspectorName("지역 ID")]
    [SerializeField] private string regionId;
    [Tooltip("사람이 읽는 지역 이름입니다.")]
    [InspectorName("표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("이 지역에서 처음 시작할 스테이지 ID입니다.")]
    [InspectorName("첫 스테이지 ID")]
    [SerializeField] private string firstStageId;
    [Tooltip("이 지역 클리어 후 이어질 다음 지역 ID입니다.")]
    [InspectorName("다음 지역 ID")]
    [SerializeField] private string nextRegionId;
    [Tooltip("이 지역에 포함된 스테이지 ID 목록입니다.")]
    [InspectorName("스테이지 ID 목록")]
    [SerializeField] private string[] stageIds;
    [Header("입장 조건")]
    [Tooltip("이 지역에 들어가기 전에 클리어되어 있어야 하는 스테이지 ID 목록입니다.")]
    [InspectorName("필요 클리어 스테이지 ID")]
    [SerializeField] private string[] requiredClearedStageIds;
    [Tooltip("이 지역에 들어가기 전에 해금되어 있어야 하는 지역 ID 목록입니다.")]
    [InspectorName("필요 해금 지역 ID")]
    [SerializeField] private string[] requiredUnlockedRegionIds;

    public string RegionId => regionId;
    public string DisplayName => displayName;
    public string FirstStageId => firstStageId;
    public string NextRegionId => nextRegionId;
    public string[] StageIds => stageIds;
    public string[] RequiredClearedStageIds => requiredClearedStageIds;
    public string[] RequiredUnlockedRegionIds => requiredUnlockedRegionIds;
}
