using UnityEngine;

/// <summary>
/// Definition data for one stage and its progression links. Runtime state is stored separately in save DTOs.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_StageDefinitionData", menuName = "HWJ/Data/System/Stage Definition")]
public class HWJ_StageDefinitionDataSO : ScriptableObject
{
    [Header("스테이지 기본 정보")]
    [Tooltip("저장과 진행도에서 사용할 고정 스테이지 ID입니다.")]
    [InspectorName("스테이지 ID")]
    [SerializeField] private string stageId;
    [Tooltip("사람이 읽는 스테이지 이름입니다.")]
    [InspectorName("표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("이 스테이지가 속한 지역 ID입니다.")]
    [InspectorName("지역 ID")]
    [SerializeField] private string regionId;
    [Tooltip("클리어 후 이어질 다음 스테이지 ID입니다.")]
    [InspectorName("다음 스테이지 ID")]
    [SerializeField] private string nextStageId;
    [Header("보스와 해금")]
    [Tooltip("켜면 이 스테이지에는 보스전이 있습니다.")]
    [InspectorName("보스 있음")]
    [SerializeField] private bool hasBoss;
    [Tooltip("이 스테이지에서 사용할 보스 ID입니다.")]
    [InspectorName("보스 ID")]
    [SerializeField] private string bossId;
    [Tooltip("켜면 스테이지 클리어 시 지역을 해금합니다.")]
    [InspectorName("클리어 시 지역 해금")]
    [SerializeField] private bool unlocksRegionOnClear;
    [Tooltip("해금할 다음 지역 ID입니다.")]
    [InspectorName("해금 지역 ID")]
    [SerializeField] private string unlockRegionId;
    [Header("입장 조건")]
    [Tooltip("이 스테이지에 들어가기 전에 클리어되어 있어야 하는 스테이지 ID 목록입니다.")]
    [InspectorName("필요 클리어 스테이지 ID")]
    [SerializeField] private string[] requiredClearedStageIds;
    [Tooltip("이 스테이지에 들어가기 전에 해금되어 있어야 하는 지역 ID 목록입니다.")]
    [InspectorName("필요 해금 지역 ID")]
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
