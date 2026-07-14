using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ProgressionCondition", menuName = "HWJ/Data/Rules/Conditions/Progression")]
public class HWJ_ProgressionConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_ProgressionRequirement requirement = HWJ_ProgressionRequirement.LevelCompare;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.GreaterOrEqual;
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
