using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ConditionGroup", menuName = "HWJ/Data/Rules/Condition Group")]
public class HWJ_ConditionGroupSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_ConditionGroupMode groupMode = HWJ_ConditionGroupMode.All;
    [SerializeField] private bool passWhenEmpty;
    [SerializeField] private HWJ_GameplayConditionSO[] conditions;

    public HWJ_ConditionGroupMode GroupMode => groupMode;
    public HWJ_GameplayConditionSO[] Conditions => conditions;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return passWhenEmpty;
        }

        bool hasValidCondition = false;

        if (groupMode == HWJ_ConditionGroupMode.All)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] == null)
                {
                    continue;
                }

                hasValidCondition = true;

                if (!conditions[i].IsMet(context))
                {
                    return false;
                }
            }

            return hasValidCondition || passWhenEmpty;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i] == null)
            {
                continue;
            }

            hasValidCondition = true;

            if (conditions[i].IsMet(context))
            {
                return true;
            }
        }

        return !hasValidCondition && passWhenEmpty;
    }
}
