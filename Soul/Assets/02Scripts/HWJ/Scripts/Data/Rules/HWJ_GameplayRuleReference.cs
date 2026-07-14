using System;
using UnityEngine;

[Serializable]
public class HWJ_GameplayRuleReference
{
    [SerializeField] private HWJ_GameplayRuleSO rule;
    [SerializeField] private string ruleId;
    [SerializeField] private bool passWhenRuleMissing;

    public HWJ_GameplayRuleSO Rule => rule;
    public string RuleId => ruleId;

    public bool TryGetRule(out HWJ_GameplayRuleSO resolvedRule)
    {
        resolvedRule = rule;

        if (resolvedRule != null)
        {
            return true;
        }

        return !string.IsNullOrEmpty(ruleId)
            && HWJ_GameAccess.TryGetGameplayRule(ruleId, out resolvedRule);
    }

    public bool IsSatisfied(HWJ_GameplayContext context)
    {
        return TryGetRule(out HWJ_GameplayRuleSO resolvedRule)
            ? resolvedRule.IsSatisfied(context)
            : passWhenRuleMissing;
    }

    public bool TryEvaluate(HWJ_GameplayContext context, out HWJ_RuleEvaluationResult result)
    {
        if (TryGetRule(out HWJ_GameplayRuleSO resolvedRule))
        {
            return resolvedRule.TryEvaluate(context, out result);
        }

        result = passWhenRuleMissing
            ? HWJ_RuleEvaluationResult.Pass(ruleId, null, "Rule reference passed because the rule is missing.")
            : HWJ_RuleEvaluationResult.Fail(ruleId, null, "Rule reference failed because the rule is missing.");
        return passWhenRuleMissing;
    }
}
