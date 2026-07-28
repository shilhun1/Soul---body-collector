using UnityEngine.SceneManagement;

public static class HWJ_SceneTransitionTransfer
{
    private static bool hasPendingArrival;
    private static bool transitionInProgress;
    private static string sourceSceneName;
    private static string targetSceneName;
    private static string targetSpawnPointId;

    public static bool HasPendingArrival => hasPendingArrival;
    public static bool TransitionInProgress => transitionInProgress;
    public static string SourceSceneName => sourceSceneName;
    public static string TargetSceneName => targetSceneName;
    public static string TargetSpawnPointId => targetSpawnPointId;

    public static void BeginTransition(string nextSceneName, string nextSpawnPointId)
    {
        sourceSceneName = SceneManager.GetActiveScene().name;
        targetSceneName = nextSceneName;
        targetSpawnPointId = nextSpawnPointId;
        hasPendingArrival = true;
        transitionInProgress = true;
    }

    public static bool TryPeekTargetSpawnPointId(out string spawnPointId)
    {
        spawnPointId = targetSpawnPointId;
        return hasPendingArrival && !string.IsNullOrWhiteSpace(spawnPointId);
    }

    public static void MarkSceneLoaded()
    {
        transitionInProgress = false;
    }

    public static void ClearPendingArrival()
    {
        hasPendingArrival = false;
        transitionInProgress = false;
        sourceSceneName = null;
        targetSceneName = null;
        targetSpawnPointId = null;
    }
}
