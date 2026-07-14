using UnityEngine;

public abstract class HWJ_GameplayConditionSO : ScriptableObject
{
    [SerializeField] private string conditionId;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private bool invertResult;
    [SerializeField] private bool passWhenContextMissing;

    public string ConditionId => conditionId;
    public string Description => description;
    public bool InvertResult => invertResult;

    public bool IsMet(HWJ_GameplayContext context)
    {
        bool result = context != null ? Evaluate(context) : passWhenContextMissing;
        return invertResult ? !result : result;
    }

    public virtual bool TryEvaluate(
        HWJ_GameplayContext context,
        out HWJ_RuleEvaluationResult result)
    {
        bool rawResult = context != null ? Evaluate(context) : passWhenContextMissing;
        bool passed = invertResult ? !rawResult : rawResult;
        string message = passed
            ? $"{GetConditionLabel()} passed."
            : $"{GetConditionLabel()} failed.";

        result = passed
            ? HWJ_RuleEvaluationResult.Pass(null, conditionId, message)
            : HWJ_RuleEvaluationResult.Fail(null, conditionId, message);
        return passed;
    }

    protected string GetConditionLabel()
    {
        if (!string.IsNullOrEmpty(conditionId))
        {
            return conditionId;
        }

        return name;
    }

    protected abstract bool Evaluate(HWJ_GameplayContext context);
}
