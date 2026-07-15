/// <summary>
/// 저장/불러오기 실패 원인을 호출자가 분기할 수 있게 하는 코드입니다.
/// 단순 bool 실패로 숨기지 않기 위해 SaveService의 모든 공개 작업 결과에 포함합니다.
/// </summary>
public enum HWJ_SaveOperationFailureCode
{
    None,
    EmptySlotId,
    MissingRuntimeContext,
    InvalidSaveData,
    FileNotFound,
    EmptyJson,
    JsonParseFailed,
    MigrationRequired,
    UnsupportedFutureVersion,
    MissingDatabase,
    MissingDatabaseEntry,
    IoException
}

public readonly struct HWJ_SaveOperationResult
{
    public readonly bool Succeeded;
    public readonly HWJ_SaveOperationFailureCode FailureCode;
    public readonly string SlotId;
    public readonly string FilePath;
    public readonly HWJ_GameSaveData SaveData;
    public readonly string Message;

    private HWJ_SaveOperationResult(
        bool succeeded,
        HWJ_SaveOperationFailureCode failureCode,
        string slotId,
        string filePath,
        HWJ_GameSaveData saveData,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        SlotId = slotId;
        FilePath = filePath;
        SaveData = saveData;
        Message = message;
    }

    public static HWJ_SaveOperationResult Success(
        string slotId,
        string filePath,
        HWJ_GameSaveData saveData,
        string message)
    {
        return new HWJ_SaveOperationResult(
            true,
            HWJ_SaveOperationFailureCode.None,
            slotId,
            filePath,
            saveData,
            message);
    }

    public static HWJ_SaveOperationResult Fail(
        HWJ_SaveOperationFailureCode failureCode,
        string slotId,
        string filePath,
        HWJ_GameSaveData saveData,
        string message)
    {
        return new HWJ_SaveOperationResult(
            false,
            failureCode,
            slotId,
            filePath,
            saveData,
            message);
    }
}
