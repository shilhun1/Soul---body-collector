using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ProgressionCondition", menuName = "HWJ/Data/Rules/Conditions/Progression")]
public class HWJ_ProgressionConditionSO : HWJ_GameplayConditionSO
{
    [Header("성장/진행 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 성장 상태를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("레벨, 경험치, 스킬 포인트 중 무엇을 검사할지 정합니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_ProgressionRequirement requirement = HWJ_ProgressionRequirement.LevelCompare;
    [Tooltip("현재 값과 기준값을 비교하는 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.GreaterOrEqual;
    [Tooltip("비교에 사용할 기준값입니다.")]
    [InspectorName("기준값")]
    [SerializeField] private int value = 1;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_LevelUpSystem level = context.GetLevel(actor);

        if (level == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_ProgressionRequirement.LevelCompare:
                return HWJ_ConditionUtility.Compare(level.CurrentLevel, compareMode, value);
            case HWJ_ProgressionRequirement.ExperienceCompare:
                return HWJ_ConditionUtility.Compare(level.CurrentExperience, compareMode, value);
            case HWJ_ProgressionRequirement.SkillPointCompare:
                return HWJ_ConditionUtility.Compare(level.SkillPoint, compareMode, value);
            case HWJ_ProgressionRequirement.HasSkillPoint:
                return level.SkillPoint >= Mathf.Max(1, value);
            default:
                return false;
        }
    }
}
