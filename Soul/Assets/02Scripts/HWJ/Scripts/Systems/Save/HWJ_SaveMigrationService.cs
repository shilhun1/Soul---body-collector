/// <summary>
/// 저장 DTO를 현재 런타임 스키마로 올리는 진입점입니다.
/// 실제 변환 규칙은 버전별 요구사항이 확정될 때 이 서비스 안에 명시적으로 추가합니다.
/// </summary>
public static class HWJ_SaveMigrationService
{
    public static HWJ_SaveMigrationResult MigrateToCurrent(HWJ_GameSaveData saveData)
    {
        if (saveData == null)
        {
            return HWJ_SaveMigrationResult.Fail(
                HWJ_SaveMigrationFailureCode.MissingSaveData,
                0,
                HWJ_SaveSchema.CurrentVersion,
                null,
                "Save migration failed: save data is null.");
        }

        saveData.EnsureDefaults();

        int sourceVersion = saveData.schemaVersion;
        int targetVersion = HWJ_SaveSchema.CurrentVersion;

        if (sourceVersion == targetVersion)
        {
            return HWJ_SaveMigrationResult.NotRequired(saveData, targetVersion);
        }

        if (sourceVersion > targetVersion)
        {
            return HWJ_SaveMigrationResult.Fail(
                HWJ_SaveMigrationFailureCode.UnsupportedFutureVersion,
                sourceVersion,
                targetVersion,
                saveData,
                $"Save migration failed: schema {sourceVersion} is newer than runtime schema {targetVersion}.");
        }

        return HWJ_SaveMigrationResult.Fail(
            HWJ_SaveMigrationFailureCode.UnsupportedLegacyVersion,
            sourceVersion,
            targetVersion,
            saveData,
            $"Save migration failed: no migration path from schema {sourceVersion} to {targetVersion} is registered.");
    }
}
