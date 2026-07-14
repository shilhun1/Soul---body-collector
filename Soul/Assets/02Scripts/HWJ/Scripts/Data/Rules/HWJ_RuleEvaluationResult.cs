using System;

[Serializable]
public struct HWJ_RuleEvaluationResult
{
    public bool Passed;
    public string RuleId;
    public string ConditionId;
    public string Message;

    public HWJ_RuleEvaluationResult(
        bool passed,
        string ruleId,
        string conditionId,
        string message)
    {
        Passed = passed;
        RuleId = ruleId;
        ConditionId = conditionId;
        Message = message;
    }

    public static HWJ_RuleEvaluationResult Pass(
        string ruleId,
        string conditionId,
        string message)
    {
        return new HWJ_RuleEvaluationResult(true, ruleId, conditionId, message);
    }

    public static HWJ_RuleEvaluationResult Fail(
        string ruleId,
        string conditionId,
        string message)
    {
        return new HWJ_RuleEvaluationResult(false, ruleId, conditionId, message);
    }
}
