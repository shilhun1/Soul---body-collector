using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_RuntimeStatCondition", menuName = "HWJ/Data/Rules/Conditions/Runtime Stat")]
public class HWJ_RuntimeStatConditionSO : HWJ_GameplayConditionSO
{
    [Header("런타임 능력치 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 능력치를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [Tooltip("검사할 런타임 능력치입니다.")]
    [InspectorName("능력치 종류")]
    [SerializeField] private HWJ_RuntimeStatField statField = HWJ_RuntimeStatField.Defense;
    [Tooltip("현재 능력치와 기준값을 비교하는 방식입니다.")]
    [InspectorName("비교 방식")]
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.GreaterOrEqual;
    [Tooltip("비교에 사용할 기준값입니다.")]
    [InspectorName("기준값")]
    [SerializeField] private float value;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        if (!TryGetValue(context, out float currentValue))
        {
            return false;
        }

        return HWJ_ConditionUtility.Compare(currentValue, compareMode, value);
    }

    private bool TryGetValue(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);

        switch (statField)
        {
            case HWJ_RuntimeStatField.CurrentHp:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.CurrentHp;
                return true;
            case HWJ_RuntimeStatField.MaxHp:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.MaxHp;
                return true;
            case HWJ_RuntimeStatField.MoveSpeed:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.MoveSpeed;
                return true;
            case HWJ_RuntimeStatField.AttackPower:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.AttackPower;
                return true;
            case HWJ_RuntimeStatField.Defense:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.Defense;
                return true;
            case HWJ_RuntimeStatField.AttackSpeed:
                if (status == null)
                {
                    return false;
                }

                currentValue = status.AttackSpeed;
                return true;
            case HWJ_RuntimeStatField.BodyDecayValue:
                return TryGetPossessionMentalConsumedValue(context, out currentValue);
            case HWJ_RuntimeStatField.BodyDecayRatio:
                return TryGetPossessionMentalConsumedRatio(context, out currentValue);
            case HWJ_RuntimeStatField.PossessionMentalConsumedValue:
                return TryGetPossessionMentalConsumedValue(context, out currentValue);
            case HWJ_RuntimeStatField.PossessionMentalConsumedRatio:
                return TryGetPossessionMentalConsumedRatio(context, out currentValue);
            case HWJ_RuntimeStatField.PossessionMentalRemainingValue:
                return TryGetPossessionMentalRemainingValue(context, out currentValue);
            case HWJ_RuntimeStatField.PossessionMentalRemainingRatio:
                return TryGetPossessionMentalRemainingRatio(context, out currentValue);
            default:
                return false;
        }
    }

    private bool TryGetPossessionMentalConsumedValue(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem possessionMental = context.GetPossessionMental(actor);

        if (possessionMental == null)
        {
            return false;
        }

        currentValue = possessionMental.ConsumedPossessionMentalValue;
        return true;
    }

    private bool TryGetPossessionMentalConsumedRatio(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem possessionMental = context.GetPossessionMental(actor);

        if (possessionMental == null)
        {
            return false;
        }

        currentValue = possessionMental.ConsumedPossessionMentalRatio;
        return true;
    }

    private bool TryGetPossessionMentalRemainingValue(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem possessionMental = context.GetPossessionMental(actor);

        if (possessionMental == null)
        {
            return false;
        }

        currentValue = possessionMental.RemainingPossessionMentalValue;
        return true;
    }

    private bool TryGetPossessionMentalRemainingRatio(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem possessionMental = context.GetPossessionMental(actor);

        if (possessionMental == null)
        {
            return false;
        }

        currentValue = possessionMental.RemainingPossessionMentalRatio;
        return true;
    }
}
