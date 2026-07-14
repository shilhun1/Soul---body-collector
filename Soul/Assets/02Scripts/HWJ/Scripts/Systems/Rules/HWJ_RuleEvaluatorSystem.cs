using UnityEngine;

public class HWJ_RuleEvaluatorSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver sourceResolver;
    [SerializeField] private HWJ_RootObjectDataResolver targetResolver;
    [SerializeField] private HWJ_RuleExecutionCoreSO executionCore;
    [SerializeField] private string executionCoreId;
    [SerializeField] private HWJ_GameplayRuleSO rule;
    [SerializeField] private string ruleId;
    [SerializeField] private bool preferExecutionCore = true;
    [SerializeField] private bool passWhenExecutionCoreMissing;
    [SerializeField] private bool passWhenRuleMissing;
    [SerializeField] private bool lastRulePassed;
    [SerializeField] private string lastExecutionCoreId;
    [SerializeField] private string lastRuleId;
    [SerializeField] private string lastConditionId;
    [SerializeField] private string lastRuleMessage;

    public bool LastRulePassed => lastRulePassed;
    public string LastExecutionCoreId => lastExecutionCoreId;
    public string LastRuleId => lastRuleId;
    public string LastConditionId => lastConditionId;
    public string LastRuleMessage => lastRuleMessage;

    private void Awake()
    {
        CacheSourceResolver();
    }

    public bool EvaluateConfigured()
    {
        return EvaluateConfigured(CreateDefaultContext());
    }

    public bool EvaluateConfigured(HWJ_GameplayContext context)
    {
        if (preferExecutionCore && executionCore != null)
        {
            return Execute(executionCore, context);
        }

        if (preferExecutionCore && !string.IsNullOrEmpty(executionCoreId))
        {
            return ExecuteById(executionCoreId, context);
        }

        if (rule != null)
        {
            return Evaluate(rule, context);
        }

        return EvaluateById(ruleId, context);
    }

    public bool Execute(HWJ_RuleExecutionCoreSO ruleExecutionCore, HWJ_GameplayContext context)
    {
        if (ruleExecutionCore == null)
        {
            HWJ_RuleExecutionResult missingResult = HWJ_RuleExecutionResult.Create(
                passWhenExecutionCoreMissing,
                null,
                null,
                null,
                passWhenExecutionCoreMissing
                    ? "Rule execution core passed because the core is missing."
                    : "Rule execution core failed because the core is missing.",
                0,
                0,
                0,
                null);
            StoreExecutionResult(missingResult);
            return passWhenExecutionCoreMissing;
        }

        bool passed = ruleExecutionCore.TryExecute(context ?? CreateDefaultContext(), out HWJ_RuleExecutionResult result);
        StoreExecutionResult(result);
        return passed;
    }

    public bool ExecuteById(string ruleExecutionCoreId, HWJ_GameplayContext context)
    {
        if (string.IsNullOrEmpty(ruleExecutionCoreId)
            || !HWJ_GameAccess.TryGetRuleExecutionCore(ruleExecutionCoreId, out HWJ_RuleExecutionCoreSO resolvedExecutionCore))
        {
            HWJ_RuleExecutionResult missingResult = HWJ_RuleExecutionResult.Create(
                passWhenExecutionCoreMissing,
                ruleExecutionCoreId,
                null,
                null,
                passWhenExecutionCoreMissing
                    ? "Rule execution core passed because the core id is missing."
                    : "Rule execution core failed because the core id is missing.",
                0,
                0,
                0,
                null);
            StoreExecutionResult(missingResult);
            return passWhenExecutionCoreMissing;
        }

        return Execute(resolvedExecutionCore, context);
    }

    public bool Evaluate(HWJ_GameplayRuleSO gameplayRule, HWJ_GameplayContext context)
    {
        if (gameplayRule == null)
        {
            HWJ_RuleEvaluationResult missingResult = passWhenRuleMissing
                ? HWJ_RuleEvaluationResult.Pass(null, null, "Rule evaluator passed because the rule is missing.")
                : HWJ_RuleEvaluationResult.Fail(null, null, "Rule evaluator failed because the rule is missing.");
            StoreResult(missingResult);
            return passWhenRuleMissing;
        }

        bool passed = gameplayRule.TryEvaluate(context ?? CreateDefaultContext(), out HWJ_RuleEvaluationResult result);
        StoreResult(result);
        return passed;
    }

    public bool EvaluateById(string gameplayRuleId, HWJ_GameplayContext context)
    {
        if (string.IsNullOrEmpty(gameplayRuleId)
            || !HWJ_GameAccess.TryGetGameplayRule(gameplayRuleId, out HWJ_GameplayRuleSO gameplayRule))
        {
            HWJ_RuleEvaluationResult missingResult = passWhenRuleMissing
                ? HWJ_RuleEvaluationResult.Pass(gameplayRuleId, null, "Rule evaluator passed because the rule id is missing.")
                : HWJ_RuleEvaluationResult.Fail(gameplayRuleId, null, "Rule evaluator failed because the rule id is missing.");
            StoreResult(missingResult);
            return passWhenRuleMissing;
        }

        return Evaluate(gameplayRule, context);
    }

    public HWJ_GameplayContext CreateDefaultContext()
    {
        CacheSourceResolver();

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(sourceResolver, targetResolver)
            .WithSource(this);

        if (targetResolver != null)
        {
            context.WithTarget(targetResolver);
        }

        return context;
    }

    public void SetTarget(HWJ_RootObjectDataResolver target)
    {
        targetResolver = target;
    }

    private void StoreResult(HWJ_RuleEvaluationResult result)
    {
        lastRulePassed = result.Passed;
        lastExecutionCoreId = null;
        lastRuleId = result.RuleId;
        lastConditionId = result.ConditionId;
        lastRuleMessage = result.Message;
    }

    private void StoreExecutionResult(HWJ_RuleExecutionResult result)
    {
        lastRulePassed = result.Passed;
        lastExecutionCoreId = result.ExecutionCoreId;
        lastRuleId = !string.IsNullOrEmpty(result.FailedRuleId)
            ? result.FailedRuleId
            : result.MatchedRuleId;
        lastConditionId = null;
        lastRuleMessage = result.Message;
    }

    private void CacheSourceResolver()
    {
        if (sourceResolver == null)
        {
            sourceResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }
    }
}
