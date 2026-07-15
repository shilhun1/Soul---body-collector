using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_GameplayRule", menuName = "HWJ/Data/Rules/Gameplay Rule")]
public class HWJ_GameplayRuleSO : ScriptableObject
{
    [SerializeField] private string ruleId;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private bool passWhenNoCondition;
    [SerializeField] private HWJ_GameplayConditionSO rootCondition;

    public string RuleId => ruleId;
    public string Description => description;
    public HWJ_GameplayConditionSO RootCondition => rootCondition;

    public bool IsSatisfied(HWJ_GameplayContext context)
    {
        return rootCondition != null ? rootCondition.IsMet(context) : passWhenNoCondition;
    }

    public bool TryEvaluate(HWJ_GameplayContext context, out HWJ_RuleEvaluationResult result)
    {
        if (rootCondition == null)
        {
            result = passWhenNoCondition
                ? HWJ_RuleEvaluationResult.Pass(ruleId, null, "Rule passed because no root condition is set.")
                : HWJ_RuleEvaluationResult.Fail(ruleId, null, "Rule failed because no root condition is set.");
            return passWhenNoCondition;
        }

        bool passed = rootCondition.TryEvaluate(context, out result);
        result.RuleId = ruleId;
        return passed;
    }

    public bool CanExecute(HWJ_GameplayContext context)
    {
        return IsSatisfied(context);
    }
}
