using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HWJ_RuleExecutionEntry
{
    [Header("규칙 실행 항목")]
    [Tooltip("이 실행 항목을 구분하는 ID입니다.")]
    [InspectorName("항목 ID")]
    [SerializeField] private string entryId;
    [Tooltip("끄면 이 항목은 실행에서 제외됩니다.")]
    [InspectorName("활성화")]
    [SerializeField] private bool enabled = true;
    [Tooltip("실행할 GameplayRule입니다. 직접 참조하거나 ID로 찾을 수 있습니다.")]
    [InspectorName("규칙 참조")]
    [SerializeField] private HWJ_GameplayRuleReference rule = new HWJ_GameplayRuleReference();

    public string EntryId => entryId;
    public bool Enabled => enabled;
    public HWJ_GameplayRuleReference Rule => rule;

    public bool TryEvaluate(HWJ_GameplayContext context, out HWJ_RuleEvaluationResult result)
    {
        if (!enabled)
        {
            result = HWJ_RuleEvaluationResult.Pass(null, null, $"{GetLabel()} skipped because it is disabled.");
            return true;
        }

        if (rule == null)
        {
            result = HWJ_RuleEvaluationResult.Fail(null, null, $"{GetLabel()} failed because the rule reference is missing.");
            return false;
        }

        return rule.TryEvaluate(context, out result);
    }

    private string GetLabel()
    {
        return !string.IsNullOrEmpty(entryId) ? entryId : "Rule execution entry";
    }
}

[CreateAssetMenu(fileName = "HWJ_RuleExecutionCore", menuName = "HWJ/Data/Rules/Rule Execution Core")]
public class HWJ_RuleExecutionCoreSO : ScriptableObject
{
    [Header("규칙 실행 코어")]
    [Tooltip("이 실행 코어를 구분하는 고정 ID입니다.")]
    [InspectorName("실행 코어 ID")]
    [SerializeField] private string executionCoreId;
    [Tooltip("이 실행 코어가 어떤 시스템에서 어떤 조건을 검사하는지 적습니다.")]
    [InspectorName("설명")]
    [SerializeField] [TextArea] private string description;
    [Tooltip("등록된 규칙들을 어떤 정책으로 평가할지 정합니다.")]
    [InspectorName("실행 정책")]
    [SerializeField] private HWJ_RuleExecutionPolicy executionPolicy = HWJ_RuleExecutionPolicy.AllMustPass;
    [Tooltip("규칙이 하나도 없을 때 통과로 처리할지 정합니다.")]
    [InspectorName("규칙 없음 시 통과")]
    [SerializeField] private bool passWhenNoRules;
    [Tooltip("비활성화된 규칙 항목을 실패가 아니라 건너뛰기로 처리합니다.")]
    [InspectorName("비활성 규칙 무시")]
    [SerializeField] private bool ignoreDisabledRules = true;
    [Tooltip("실행할 규칙 항목 목록입니다.")]
    [InspectorName("규칙 항목 목록")]
    [SerializeField] private HWJ_RuleExecutionEntry[] rules;

    public string ExecutionCoreId => executionCoreId;
    public string Description => description;
    public HWJ_RuleExecutionPolicy ExecutionPolicy => executionPolicy;
    public bool PassWhenNoRules => passWhenNoRules;
    public HWJ_RuleExecutionEntry[] Rules => rules;

    public bool CanExecute(HWJ_GameplayContext context)
    {
        return TryExecute(context, out _);
    }

    public bool TryExecute(HWJ_GameplayContext context, out HWJ_RuleExecutionResult result)
    {
        List<HWJ_RuleEvaluationResult> evaluations = new List<HWJ_RuleEvaluationResult>();
        int evaluatedCount = 0;
        int passedCount = 0;
        int failedCount = 0;
        string matchedRuleId = null;
        string failedRuleId = null;

        if (rules == null || rules.Length == 0)
        {
            result = BuildResult(
                passWhenNoRules,
                matchedRuleId,
                failedRuleId,
                passWhenNoRules
                    ? $"{GetLabel()} passed because no rules are configured."
                    : $"{GetLabel()} failed because no rules are configured.",
                evaluatedCount,
                passedCount,
                failedCount,
                evaluations);
            return result.Passed;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            HWJ_RuleExecutionEntry entry = rules[i];

            if (entry == null)
            {
                HWJ_RuleEvaluationResult missingEntryResult = HWJ_RuleEvaluationResult.Fail(
                    null,
                    null,
                    $"{GetLabel()} failed because rule entry {i} is missing.");
                evaluations.Add(missingEntryResult);
                failedCount++;

                if (executionPolicy == HWJ_RuleExecutionPolicy.AllMustPass
                    || executionPolicy == HWJ_RuleExecutionPolicy.FirstFailingRule)
                {
                    result = BuildResult(false, matchedRuleId, failedRuleId, missingEntryResult.Message, evaluatedCount, passedCount, failedCount, evaluations);
                    return false;
                }

                continue;
            }

            if (ignoreDisabledRules && !entry.Enabled)
            {
                continue;
            }

            bool passed = entry.TryEvaluate(context, out HWJ_RuleEvaluationResult ruleResult);
            evaluations.Add(ruleResult);
            evaluatedCount++;

            if (passed)
            {
                passedCount++;
                matchedRuleId = !string.IsNullOrEmpty(ruleResult.RuleId)
                    ? ruleResult.RuleId
                    : entry.EntryId;

                if (executionPolicy == HWJ_RuleExecutionPolicy.AnyCanPass
                    || executionPolicy == HWJ_RuleExecutionPolicy.FirstPassingRule)
                {
                    result = BuildResult(true, matchedRuleId, failedRuleId, $"{GetLabel()} passed by {matchedRuleId}.", evaluatedCount, passedCount, failedCount, evaluations);
                    return true;
                }

                continue;
            }

            failedCount++;
            failedRuleId = !string.IsNullOrEmpty(ruleResult.RuleId)
                ? ruleResult.RuleId
                : entry.EntryId;

            if (executionPolicy == HWJ_RuleExecutionPolicy.AllMustPass
                || executionPolicy == HWJ_RuleExecutionPolicy.FirstFailingRule)
            {
                result = BuildResult(false, matchedRuleId, failedRuleId, ruleResult.Message, evaluatedCount, passedCount, failedCount, evaluations);
                return false;
            }
        }

        bool finalPassed = GetFinalPassed(evaluatedCount, passedCount, failedCount);
        string finalMessage = finalPassed
            ? $"{GetLabel()} passed. {passedCount}/{evaluatedCount} rules passed."
            : $"{GetLabel()} failed. {passedCount}/{evaluatedCount} rules passed.";
        result = BuildResult(finalPassed, matchedRuleId, failedRuleId, finalMessage, evaluatedCount, passedCount, failedCount, evaluations);
        return finalPassed;
    }

    private bool GetFinalPassed(int evaluatedCount, int passedCount, int failedCount)
    {
        if (evaluatedCount == 0)
        {
            return passWhenNoRules;
        }

        switch (executionPolicy)
        {
            case HWJ_RuleExecutionPolicy.AllMustPass:
                return failedCount == 0;
            case HWJ_RuleExecutionPolicy.AnyCanPass:
            case HWJ_RuleExecutionPolicy.FirstPassingRule:
                return passedCount > 0;
            case HWJ_RuleExecutionPolicy.FirstFailingRule:
                return failedCount == 0;
            default:
                return false;
        }
    }

    private HWJ_RuleExecutionResult BuildResult(
        bool passed,
        string matchedRuleId,
        string failedRuleId,
        string message,
        int evaluatedCount,
        int passedCount,
        int failedCount,
        List<HWJ_RuleEvaluationResult> evaluations)
    {
        return HWJ_RuleExecutionResult.Create(
            passed,
            executionCoreId,
            matchedRuleId,
            failedRuleId,
            message,
            evaluatedCount,
            passedCount,
            failedCount,
            evaluations.ToArray());
    }

    private string GetLabel()
    {
        return !string.IsNullOrEmpty(executionCoreId) ? executionCoreId : name;
    }
}
