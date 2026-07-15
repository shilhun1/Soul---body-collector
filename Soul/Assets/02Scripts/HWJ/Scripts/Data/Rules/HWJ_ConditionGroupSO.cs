using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ConditionGroup", menuName = "HWJ/Data/Rules/Condition Group")]
public class HWJ_ConditionGroupSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_ConditionGroupMode groupMode = HWJ_ConditionGroupMode.All;
    [SerializeField] private bool passWhenEmpty;
    [SerializeField] private HWJ_GameplayConditionSO[] conditions;

    public HWJ_ConditionGroupMode GroupMode => groupMode;
    public HWJ_GameplayConditionSO[] Conditions => conditions;

    public override bool TryEvaluate(
        HWJ_GameplayContext context,
        out HWJ_RuleEvaluationResult result)
    {
        if (conditions == null || conditions.Length == 0)
        {
            result = passWhenEmpty
                ? HWJ_RuleEvaluationResult.Pass(null, ConditionId, $"{GetConditionLabel()} passed because it is empty.")
                : HWJ_RuleEvaluationResult.Fail(null, ConditionId, $"{GetConditionLabel()} failed because it is empty.");
            return passWhenEmpty;
        }

        bool hasValidCondition = false;
        HWJ_RuleEvaluationResult lastFailure = default;

        if (groupMode == HWJ_ConditionGroupMode.All)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                HWJ_GameplayConditionSO condition = conditions[i];

                if (condition == null)
                {
                    continue;
                }

                hasValidCondition = true;

                if (!condition.TryEvaluate(context, out result))
                {
                    result.ConditionId = string.IsNullOrEmpty(result.ConditionId)
                        ? ConditionId
                        : result.ConditionId;
                    return false;
                }
            }

            bool passed = hasValidCondition || passWhenEmpty;
            result = passed
                ? HWJ_RuleEvaluationResult.Pass(null, ConditionId, $"{GetConditionLabel()} passed.")
                : HWJ_RuleEvaluationResult.Fail(null, ConditionId, $"{GetConditionLabel()} failed because it has no valid conditions.");
            return passed;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            HWJ_GameplayConditionSO condition = conditions[i];

            if (condition == null)
            {
                continue;
            }

            hasValidCondition = true;

            if (condition.TryEvaluate(context, out result))
            {
                return true;
            }

            lastFailure = result;
        }

        if (!hasValidCondition && passWhenEmpty)
        {
            result = HWJ_RuleEvaluationResult.Pass(null, ConditionId, $"{GetConditionLabel()} passed because it is empty.");
            return true;
        }

        result = hasValidCondition
            ? lastFailure
            : HWJ_RuleEvaluationResult.Fail(null, ConditionId, $"{GetConditionLabel()} failed because it has no valid conditions.");
        return false;
    }

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
