using System;

public enum HWJ_RuleValidationSeverity
{
    Info,
    Warning,
    Error
}

[Serializable]
public struct HWJ_RuleValidationEntry
{
    public HWJ_RuleValidationSeverity Severity;
    public string Id;
    public string Message;

    public HWJ_RuleValidationEntry(
        HWJ_RuleValidationSeverity severity,
        string id,
        string message)
    {
        Severity = severity;
        Id = id;
        Message = message;
    }
}

[Serializable]
public class HWJ_RuleValidationResult
{
    public int errorCount;
    public int warningCount;
    public int infoCount;
    public HWJ_RuleValidationEntry[] entries;

    public bool IsValid => errorCount == 0;
}
