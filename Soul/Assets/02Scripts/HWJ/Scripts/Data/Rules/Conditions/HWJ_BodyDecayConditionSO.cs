using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_BodyDecayCondition", menuName = "HWJ/Data/Rules/Conditions/Possession Mental")]
public class HWJ_BodyDecayConditionSO : HWJ_GameplayConditionSO
{
    [Header("빙의체 정신력 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 빙의체 정신력 상태를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("빙의체 정신력에서 검사할 조건 종류입니다. 기존 enum 이름의 Decay는 소모된 정신력으로 해석합니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_BodyDecayRequirement requirement = HWJ_BodyDecayRequirement.HasDecayRemaining;
    [Tooltip("숫자 비교 조건에서 사용할 비교 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.Greater;
    [Tooltip("정신력 수치 비교에 사용할 기준값입니다.")]
    [InspectorName("기준 수치")]
    [SerializeField] private float value;
    [Tooltip("정신력 비율 비교에 사용할 기준값입니다. 0.5는 50%입니다.")]
    [InspectorName("기준 비율")]
    [SerializeField] [Range(0f, 1f)] private float ratio = 0.5f;
    [Tooltip("비교할 정신력 위험 단계입니다.")]
    [InspectorName("위험 단계")]
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
            case HWJ_BodyDecayRequirement.PossessionMentalDraining:
                return bodyDecay.IsDecaying;
            case HWJ_BodyDecayRequirement.IsNotDecaying:
            case HWJ_BodyDecayRequirement.PossessionMentalNotDraining:
                return !bodyDecay.IsDecaying;
            case HWJ_BodyDecayRequirement.HasDecayRemaining:
            case HWJ_BodyDecayRequirement.HasPossessionMentalRemaining:
                return bodyDecay.HasDecayRemaining;
            case HWJ_BodyDecayRequirement.DecayValueCompare:
            case HWJ_BodyDecayRequirement.ConsumedPossessionMentalValueCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.CurrentDecayValue, compareMode, value);
            case HWJ_BodyDecayRequirement.DecayRatioCompare:
            case HWJ_BodyDecayRequirement.ConsumedPossessionMentalRatioCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.CurrentDecayRatio, compareMode, ratio);
            case HWJ_BodyDecayRequirement.RemainingDecayValueCompare:
            case HWJ_BodyDecayRequirement.RemainingPossessionMentalValueCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.RemainingDecayValue, compareMode, value);
            case HWJ_BodyDecayRequirement.RemainingDecayRatioCompare:
            case HWJ_BodyDecayRequirement.RemainingPossessionMentalRatioCompare:
                return HWJ_ConditionUtility.Compare(bodyDecay.RemainingDecayRatio, compareMode, ratio);
            case HWJ_BodyDecayRequirement.DangerLevelAtLeast:
            case HWJ_BodyDecayRequirement.PossessionMentalDangerLevelAtLeast:
                return bodyDecay.CurrentDangerLevel >= dangerLevel;
            case HWJ_BodyDecayRequirement.DangerLevelEquals:
            case HWJ_BodyDecayRequirement.PossessionMentalDangerLevelEquals:
                return bodyDecay.CurrentDangerLevel == dangerLevel;
            default:
                return false;
        }
    }
}
