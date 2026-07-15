using System;

public enum HWJ_RuleExecutionPolicy
{
    AllMustPass,
    AnyCanPass,
    FirstPassingRule,
    FirstFailingRule
}

[Serializable]
public struct HWJ_RuleExecutionResult
{
    public bool Passed;
    public string ExecutionCoreId;
    public string MatchedRuleId;
    public string FailedRuleId;
    public string Message;
    public int evaluatedCount;
    public int passedCount;
    public int failedCount;
    public HWJ_RuleEvaluationResult[] ruleResults;

    public static HWJ_RuleExecutionResult Create(
        bool passed,
        string executionCoreId,
        string matchedRuleId,
        string failedRuleId,
        string message,
        int evaluatedCount,
        int passedCount,
        int failedCount,
        HWJ_RuleEvaluationResult[] ruleResults)
    {
        return new HWJ_RuleExecutionResult
        {
            Passed = passed,
            ExecutionCoreId = executionCoreId,
            MatchedRuleId = matchedRuleId,
            FailedRuleId = failedRuleId,
            Message = message,
            evaluatedCount = evaluatedCount,
            passedCount = passedCount,
            failedCount = failedCount,
            ruleResults = ruleResults
        };
    }
}
