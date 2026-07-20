using System;
using UnityEngine;

/// <summary>
/// 빙의 육신별 스킬 해금 노드 정의 데이터입니다.
/// 실제 스킬 실행은 SkillActionDataSO가 담당하고, 이 SO는 해금 조건과 선행 노드만 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_SkillNodeData", menuName = "HWJ/Data/System/Skill Node")]
public class HWJ_SkillNodeDataSO : ScriptableObject
{
    [Header("노드 기본 정보")]
    [Tooltip("저장 데이터와 해금 상태에서 사용하는 고정 노드 ID입니다.")]
    [InspectorName("노드 ID")]
    [SerializeField] private string nodeId;
    [Tooltip("기획표에 적힌 육신 ID입니다. 예: 1101 검, 1201 창.")]
    [InspectorName("육신 ID")]
    [SerializeField] private string possessedBodyId;
    [Tooltip("이 노드가 속한 무기 유형입니다.")]
    [InspectorName("무기 유형")]
    [SerializeField] private HWJ_WeaponType weaponType;
    [Tooltip("공통 해금 단계입니다. 1단계를 해금하면 모든 육신의 1단계 스킬이 사용 가능합니다.")]
    [InspectorName("공통 해금 단계")]
    [SerializeField] private int skillStep = 1;
    [Tooltip("기획자와 UI가 보여줄 스킬 이름입니다.")]
    [InspectorName("스킬 이름")]
    [SerializeField] private string skillDisplayName;
    [Tooltip("먼저 해금되어야 하는 노드 ID입니다. 없으면 비워둡니다.")]
    [InspectorName("선행 노드 ID")]
    [SerializeField] private string prerequisiteNodeId;

    [Header("해금 조건")]
    [Tooltip("이 노드를 해금하기 위해 필요한 플레이어 레벨입니다.")]
    [InspectorName("요구 레벨")]
    [SerializeField] private int requiredLevel = 1;
    [Tooltip("이 노드를 해금할 때 소비하는 스킬 포인트입니다.")]
    [InspectorName("소모 스킬 포인트")]
    [SerializeField] private int skillPointCost;

    [Header("실행 스킬 연결")]
    [Tooltip("실제로 실행할 SkillActionDataSO의 ID입니다.")]
    [InspectorName("스킬 실행 ID")]
    [SerializeField] private string skillActionId;
    [Tooltip("실제로 실행할 SkillActionDataSO 에셋입니다. 비워도 Skill Action ID로 찾을 수 있습니다.")]
    [InspectorName("스킬 실행 에셋")]
    [SerializeField] private HWJ_SkillActionDataSO skillAction;

    [Header("설명")]
    [TextArea]
    [Tooltip("기획자, UI, 문서에서 참고할 설명입니다.")]
    [InspectorName("노드 설명")]
    [SerializeField] private string description;

    public string NodeId => nodeId;
    public string PossessedBodyId => possessedBodyId;
    public HWJ_WeaponType WeaponType => weaponType;
    public int SkillStep => Mathf.Max(0, skillStep);
    public int AuthoredSkillStep => skillStep;
    public string SkillDisplayName => skillDisplayName;
    public string AuthoredPrerequisiteNodeId => prerequisiteNodeId;
    public string PrerequisiteNodeId => IsEmptyPrerequisite(prerequisiteNodeId) ? null : prerequisiteNodeId;
    public int RequiredLevel => Mathf.Max(1, requiredLevel);
    public int AuthoredRequiredLevel => requiredLevel;
    public int SkillPointCost => Mathf.Max(0, skillPointCost);
    public int AuthoredSkillPointCost => skillPointCost;
    public string AuthoredSkillActionId => skillActionId;
    public string SkillActionId => skillAction != null && !string.IsNullOrWhiteSpace(skillAction.SkillActionId)
        ? skillAction.SkillActionId
        : skillActionId;
    public HWJ_SkillActionDataSO SkillAction => skillAction;
    public string Description => description;
    public bool HasPrerequisite => !string.IsNullOrEmpty(PrerequisiteNodeId);
    public bool IsFreeStartingNode => !HasPrerequisite && RequiredLevel <= 1 && SkillPointCost <= 0;

    public bool MatchesSkillAction(string checkedSkillActionId)
    {
        return !string.IsNullOrEmpty(checkedSkillActionId)
            && string.Equals(SkillActionId, checkedSkillActionId, StringComparison.Ordinal);
    }

    public bool MatchesWeapon(HWJ_WeaponType checkedWeaponType)
    {
        return weaponType == HWJ_WeaponType.None || weaponType == checkedWeaponType;
    }

    private static bool IsEmptyPrerequisite(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase);
    }
}
