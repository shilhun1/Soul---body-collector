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
}
