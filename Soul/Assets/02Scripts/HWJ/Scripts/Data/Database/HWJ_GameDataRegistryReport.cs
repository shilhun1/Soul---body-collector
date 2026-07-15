using System;
using System.Collections.Generic;

public enum HWJ_GameDataValidationSeverity
{
    Info,
    Warning,
    Error
}

[Serializable]
public struct HWJ_GameDataValidationEntry
{
    public HWJ_GameDataValidationSeverity Severity;
    public string ValidationCode;
    public string RequirementId;
    public string DataCategory;
    public string AssetName;
    public string FieldName;
    public string IdValue;
    public string Problem;
    public string Fix;

    public HWJ_GameDataValidationEntry(
        HWJ_GameDataValidationSeverity severity,
        string validationCode,
        string requirementId,
        string dataCategory,
        string assetName,
        string fieldName,
        string idValue,
        string problem,
        string fix)
    {
        Severity = severity;
        ValidationCode = validationCode;
        RequirementId = requirementId;
        DataCategory = dataCategory;
        AssetName = assetName;
        FieldName = fieldName;
        IdValue = idValue;
        Problem = problem;
        Fix = fix;
    }
}

[Serializable]
public class HWJ_GameDataRegistryReport
{
    public int errorCount;
    public int warningCount;
    public int infoCount;
    public HWJ_GameDataValidationEntry[] entries;

    public bool IsValid => errorCount == 0;

    public static HWJ_GameDataRegistryReport FromEntries(List<HWJ_GameDataValidationEntry> sourceEntries)
    {
        HWJ_GameDataRegistryReport report = new HWJ_GameDataRegistryReport();

        if (sourceEntries == null || sourceEntries.Count == 0)
        {
            report.entries = Array.Empty<HWJ_GameDataValidationEntry>();
            return report;
        }

        report.entries = sourceEntries.ToArray();

        for (int i = 0; i < report.entries.Length; i++)
        {
            switch (report.entries[i].Severity)
            {
                case HWJ_GameDataValidationSeverity.Error:
                    report.errorCount++;
                    break;
                case HWJ_GameDataValidationSeverity.Warning:
                    report.warningCount++;
                    break;
                default:
                    report.infoCount++;
                    break;
            }
        }

        return report;
    }

    public bool HasEntry(string validationCode)
    {
        if (entries == null || string.IsNullOrEmpty(validationCode))
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (string.Equals(entries[i].ValidationCode, validationCode, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
