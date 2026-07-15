using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_BodyDecayCondition", menuName = "HWJ/Data/Rules/Conditions/Body Decay")]
public class HWJ_BodyDecayConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [SerializeField] private HWJ_BodyDecayRequirement requirement = HWJ_BodyDecayRequirement.HasDecayRemaining;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.Greater;
    [SerializeField] private float value;
    [SerializeField] [Range(0f, 1f)] private float ratio = 0.5f;
    [SerializeField] private HWJ_DecayDangerLevel dangerLevel = HWJ_DecayDangerLevel.Warning;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_BodyDecaySystem bodyDecay = context.GetBodyDecay(actor);

        if (bodyDecay == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_BodyDecayRequirement.IsDecaying:
                return bodyDecay.IsDecaying;
            case HWJ_BodyDecayRequirement.IsNotDecaying:
                return !bodyDecay.IsDecaying;
            case HWJ_BodyDecayRequirement.HasDecayRemaining:
                return bodyDecay.HasDecayRemaining;
            case HWJ_BodyDecayRequirement.DecayValueCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.CurrentDecayValue, compareMode, value);
            case HWJ_BodyDecayRequirement.DecayRatioCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.CurrentDecayRatio, compareMode, ratio);
            case HWJ_BodyDecayRequirement.RemainingDecayValueCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.RemainingDecayValue, compareMode, value);
            case HWJ_BodyDecayRequirement.RemainingDecayRatioCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.RemainingDecayRatio, compareMode, ratio);
            case HWJ_BodyDecayRequirement.DangerLevelAtLeast:
                return bodyDecay.CurrentDangerLevel >= dangerLevel;
            case HWJ_BodyDecayRequirement.DangerLevelEquals:
                return bodyDecay.CurrentDangerLevel == dangerLevel;
            default:
                return false;
        }
    }
}
