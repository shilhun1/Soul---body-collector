using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_RuntimeStatCondition", menuName = "HWJ/Data/Rules/Conditions/Runtime Stat")]
public class HWJ_RuntimeStatConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [SerializeField] private HWJ_RuntimeStatField statField = HWJ_RuntimeStatField.Defense;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.GreaterOrEqual;
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
                return TryGetBodyDecayValue(context, out currentValue);
            case HWJ_RuntimeStatField.BodyDecayRatio:
                return TryGetBodyDecayRatio(context, out currentValue);
            default:
                return false;
        }
    }

    private bool TryGetBodyDecayValue(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem bodyDecay = context.GetBodyDecay(actor);

        if (bodyDecay == null)
        {
            return false;
        }

        currentValue = bodyDecay.CurrentDecayValue;
        return true;
    }

    private bool TryGetBodyDecayRatio(HWJ_GameplayContext context, out float currentValue)
    {
        currentValue = 0f;
        HWJ_BodyDecaySystem bodyDecay = context.GetBodyDecay(actor);

        if (bodyDecay == null)
        {
            return false;
        }

        currentValue = bodyDecay.CurrentDecayRatio;
        return true;
    }
}
