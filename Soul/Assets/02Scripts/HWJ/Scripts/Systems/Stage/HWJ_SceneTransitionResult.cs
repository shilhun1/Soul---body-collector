using UnityEngine.SceneManagement;

public enum HWJ_SceneTransitionFailureCode
{
    None,
    MissingTargetSceneName,
    TargetSceneNotInBuildSettings,
    TransitionAlreadyInProgress,
    MissingPlayer,
    SceneLoadFailed,
    TargetSpawnPointNotFound
}

public readonly struct HWJ_SceneTransitionResult
{
    public readonly bool Succeeded;
    public readonly HWJ_SceneTransitionFailureCode FailureCode;
    public readonly string SourceSceneName;
    public readonly string TargetSceneName;
    public readonly string TargetSpawnPointId;
    public readonly string Message;

    public HWJ_SceneTransitionResult(
        bool succeeded,
        HWJ_SceneTransitionFailureCode failureCode,
        string sourceSceneName,
        string targetSceneName,
        string targetSpawnPointId,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        SourceSceneName = sourceSceneName;
        TargetSceneName = targetSceneName;
        TargetSpawnPointId = targetSpawnPointId;
        Message = message;
    }

    public static HWJ_SceneTransitionResult Success(
        string targetSceneName,
        string targetSpawnPointId,
        string message)
    {
        return new HWJ_SceneTransitionResult(
            true,
            HWJ_SceneTransitionFailureCode.None,
            SceneManager.GetActiveScene().name,
            targetSceneName,
            targetSpawnPointId,
            message);
    }

    public static HWJ_SceneTransitionResult Fail(
        HWJ_SceneTransitionFailureCode failureCode,
        string targetSceneName,
        string targetSpawnPointId,
        string message)
    {
        return new HWJ_SceneTransitionResult(
            false,
            failureCode,
            SceneManager.GetActiveScene().name,
            targetSceneName,
            targetSpawnPointId,
            message);
    }
}
