public enum HWJ_SaveMigrationFailureCode
{
    None,
    MissingSaveData,
    UnsupportedLegacyVersion,
    UnsupportedFutureVersion
}

public readonly struct HWJ_SaveMigrationResult
{
    public readonly bool Succeeded;
    public readonly bool MigrationRequired;
    public readonly bool Migrated;
    public readonly HWJ_SaveMigrationFailureCode FailureCode;
    public readonly int SourceVersion;
    public readonly int TargetVersion;
    public readonly HWJ_GameSaveData SaveData;
    public readonly string Message;

    private HWJ_SaveMigrationResult(
        bool succeeded,
        bool migrationRequired,
        bool migrated,
        HWJ_SaveMigrationFailureCode failureCode,
        int sourceVersion,
        int targetVersion,
        HWJ_GameSaveData saveData,
        string message)
    {
        Succeeded = succeeded;
        MigrationRequired = migrationRequired;
        Migrated = migrated;
        FailureCode = failureCode;
        SourceVersion = sourceVersion;
        TargetVersion = targetVersion;
        SaveData = saveData;
        Message = message;
    }

    public static HWJ_SaveMigrationResult NotRequired(HWJ_GameSaveData saveData, int currentVersion)
    {
        return new HWJ_SaveMigrationResult(
            true,
            false,
            false,
            HWJ_SaveMigrationFailureCode.None,
            currentVersion,
            currentVersion,
            saveData,
            "Save migration skipped: schema is current.");
    }

    public static HWJ_SaveMigrationResult MigratedSave(
        HWJ_GameSaveData saveData,
        int sourceVersion,
        int targetVersion,
        string message)
    {
        return new HWJ_SaveMigrationResult(
            true,
            true,
            true,
            HWJ_SaveMigrationFailureCode.None,
            sourceVersion,
            targetVersion,
            saveData,
            message);
    }

    public static HWJ_SaveMigrationResult Fail(
        HWJ_SaveMigrationFailureCode failureCode,
        int sourceVersion,
        int targetVersion,
        HWJ_GameSaveData saveData,
        string message)
    {
        return new HWJ_SaveMigrationResult(
            false,
            sourceVersion < targetVersion,
            false,
            failureCode,
            sourceVersion,
            targetVersion,
            saveData,
            message);
    }
}
