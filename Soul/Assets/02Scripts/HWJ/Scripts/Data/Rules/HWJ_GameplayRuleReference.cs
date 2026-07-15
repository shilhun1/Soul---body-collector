using System;
using UnityEngine;

[Serializable]
public class HWJ_GameplayRuleReference
{
    [Header("규칙 참조")]
    [Tooltip("직접 연결할 GameplayRule SO입니다.")]
    [InspectorName("규칙 SO")]
    [SerializeField] private HWJ_GameplayRuleSO rule;
    [Tooltip("직접 참조가 비어 있을 때 데이터베이스에서 찾을 규칙 ID입니다.")]
    [InspectorName("규칙 ID")]
    [SerializeField] private string ruleId;
    [Tooltip("규칙을 찾지 못했을 때 통과로 처리할지 정합니다.")]
    [InspectorName("규칙 없음 시 통과")]
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
