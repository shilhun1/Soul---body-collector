using System.Collections.Generic;
using UnityEngine;

public class HWJ_RuleValidationSystem : MonoBehaviour
{
    [SerializeField] private HWJ_GameplayDatabaseSO database;
    [SerializeField] private bool logInfoEntries;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool logErrors = true;
    [SerializeField] private HWJ_RuleValidationResult lastResult;

    public HWJ_RuleValidationResult LastResult => lastResult;

    private void Awake()
    {
        if (database == null)
        {
            database = HWJ_GameAccess.Database;
        }
    }

    [ContextMenu("HWJ/Validate Rule Database")]
    public void ValidateConfiguredDatabase()
    {
        lastResult = Validate(database);
        LogResult(lastResult);
    }

    public HWJ_RuleValidationResult Validate(HWJ_GameplayDatabaseSO targetDatabase)
    {
        List<HWJ_RuleValidationEntry> entries = new List<HWJ_RuleValidationEntry>();

        if (targetDatabase == null)
        {
            Add(entries, HWJ_RuleValidationSeverity.Error, "database", "Gameplay database is missing.");
            return BuildResult(entries);
        }

        ValidateConditions(targetDatabase.Conditions, entries);
        ValidateRules(targetDatabase.GameplayRules, entries);
        ValidateRuleExecutionCores(targetDatabase.RuleExecutionCores, entries);
        return BuildResult(entries);
    }

    private static void ValidateConditions(
        HWJ_GameplayConditionSO[] conditions,
        List<HWJ_RuleValidationEntry> entries)
    {
        HashSet<string> ids = new HashSet<string>();

        if (conditions == null || conditions.Length == 0)
        {
            Add(entries, HWJ_RuleValidationSeverity.Warning, "conditions", "No gameplay conditions are registered.");
            return;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            HWJ_GameplayConditionSO condition = conditions[i];

            if (condition == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, $"conditions[{i}]", "Condition reference is null.");
                continue;
            }

            string id = condition.ConditionId;

            if (string.IsNullOrEmpty(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Warning, condition.name, "Condition id is empty.");
            }
            else if (!ids.Add(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, id, "Condition id is duplicated.");
            }

            if (condition is HWJ_ConditionGroupSO group)
            {
                ValidateConditionGroup(group, entries);
            }
        }
    }

    private static void ValidateConditionGroup(
        HWJ_ConditionGroupSO group,
        List<HWJ_RuleValidationEntry> entries)
    {
        HWJ_GameplayConditionSO[] children = group.Conditions;

        if (children == null || children.Length == 0)
        {
            Add(entries, HWJ_RuleValidationSeverity.Warning, group.ConditionId, "Condition group has no children.");
            return;
        }

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, group.ConditionId, $"Condition group child {i} is null.");
            }
        }
    }

    private static void ValidateRules(
        HWJ_GameplayRuleSO[] rules,
        List<HWJ_RuleValidationEntry> entries)
    {
        HashSet<string> ids = new HashSet<string>();

        if (rules == null || rules.Length == 0)
        {
            Add(entries, HWJ_RuleValidationSeverity.Warning, "rules", "No gameplay rules are registered.");
            return;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            HWJ_GameplayRuleSO rule = rules[i];

            if (rule == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, $"rules[{i}]", "Rule reference is null.");
                continue;
            }

            string id = rule.RuleId;

            if (string.IsNullOrEmpty(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Warning, rule.name, "Rule id is empty.");
            }
            else if (!ids.Add(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, id, "Rule id is duplicated.");
            }

            if (rule.RootCondition == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, id, "Rule root condition is missing.");
            }
        }
    }

    private static void ValidateRuleExecutionCores(
        HWJ_RuleExecutionCoreSO[] executionCores,
        List<HWJ_RuleValidationEntry> entries)
    {
        HashSet<string> ids = new HashSet<string>();

        if (executionCores == null || executionCores.Length == 0)
        {
            Add(entries, HWJ_RuleValidationSeverity.Warning, "rule_execution_cores", "No rule execution cores are registered.");
            return;
        }

        for (int i = 0; i < executionCores.Length; i++)
        {
            HWJ_RuleExecutionCoreSO executionCore = executionCores[i];

            if (executionCore == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, $"ruleExecutionCores[{i}]", "Rule execution core reference is null.");
                continue;
            }

            string id = executionCore.ExecutionCoreId;

            if (string.IsNullOrEmpty(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Warning, executionCore.name, "Rule execution core id is empty.");
            }
            else if (!ids.Add(id))
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, id, "Rule execution core id is duplicated.");
            }

            ValidateRuleExecutionEntries(executionCore, entries);
        }
    }

    private static void ValidateRuleExecutionEntries(
        HWJ_RuleExecutionCoreSO executionCore,
        List<HWJ_RuleValidationEntry> entries)
    {
        HWJ_RuleExecutionEntry[] ruleEntries = executionCore.Rules;

        if (ruleEntries == null || ruleEntries.Length == 0)
        {
            Add(entries, HWJ_RuleValidationSeverity.Warning, executionCore.ExecutionCoreId, "Rule execution core has no rule entries.");
            return;
        }

        for (int i = 0; i < ruleEntries.Length; i++)
        {
            HWJ_RuleExecutionEntry entry = ruleEntries[i];

            if (entry == null)
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, executionCore.ExecutionCoreId, $"Rule execution entry {i} is null.");
                continue;
            }

            HWJ_GameplayRuleReference ruleReference = entry.Rule;

            if (ruleReference == null
                || (ruleReference.Rule == null && string.IsNullOrEmpty(ruleReference.RuleId)))
            {
                Add(entries, HWJ_RuleValidationSeverity.Error, executionCore.ExecutionCoreId, $"Rule execution entry {i} has no rule reference.");
            }
        }
    }

    private static void Add(
        List<HWJ_RuleValidationEntry> entries,
        HWJ_RuleValidationSeverity severity,
        string id,
        string message)
    {
        entries.Add(new HWJ_RuleValidationEntry(severity, id, message));
    }

    private static HWJ_RuleValidationResult BuildResult(List<HWJ_RuleValidationEntry> entries)
    {
        HWJ_RuleValidationResult result = new HWJ_RuleValidationResult
        {
            entries = entries.ToArray()
        };

        for (int i = 0; i < result.entries.Length; i++)
        {
            switch (result.entries[i].Severity)
            {
                case HWJ_RuleValidationSeverity.Error:
                    result.errorCount++;
                    break;
                case HWJ_RuleValidationSeverity.Warning:
                    result.warningCount++;
                    break;
                default:
                    result.infoCount++;
                    break;
            }
        }

        return result;
    }

    private void LogResult(HWJ_RuleValidationResult result)
    {
        if (result == null || result.entries == null)
        {
            return;
        }

        for (int i = 0; i < result.entries.Length; i++)
        {
            HWJ_RuleValidationEntry entry = result.entries[i];
            string message = $"[HWJ Rule Validation] {entry.Id}: {entry.Message}";

            if (entry.Severity == HWJ_RuleValidationSeverity.Error && logErrors)
            {
                Debug.LogError(message, this);
            }
            else if (entry.Severity == HWJ_RuleValidationSeverity.Warning && logWarnings)
            {
                Debug.LogWarning(message, this);
            }
            else if (entry.Severity == HWJ_RuleValidationSeverity.Info && logInfoEntries)
            {
                Debug.Log(message, this);
            }
        }
    }
}
