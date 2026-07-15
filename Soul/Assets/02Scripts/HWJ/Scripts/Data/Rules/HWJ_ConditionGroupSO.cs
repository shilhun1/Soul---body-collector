using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_ConditionGroup", menuName = "HWJ/Data/Rules/Condition Group")]
public class HWJ_ConditionGroupSO : HWJ_GameplayConditionSO
{
    [Header("조건 그룹")]
    [Tooltip("모든 조건을 만족해야 하는지, 하나만 만족해도 되는지 정합니다.")]
    [InspectorName("조건 묶음 방식")]
    [SerializeField] private HWJ_ConditionGroupMode groupMode = HWJ_ConditionGroupMode.All;
    [Tooltip("조건 배열이 비어 있을 때 통과로 처리할지 정합니다.")]
    [InspectorName("비어 있으면 통과")]
    [SerializeField] private bool passWhenEmpty;
    [Tooltip("이 그룹에서 검사할 조건 목록입니다.")]
    [InspectorName("조건 목록")]
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
