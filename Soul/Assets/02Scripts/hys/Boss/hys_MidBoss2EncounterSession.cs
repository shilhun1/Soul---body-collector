using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 같은 실행 중 보스방 재입장 시 인트로 재생과 처치 후 재소환 여부를 기억합니다.
public static class hys_MidBoss2EncounterSession
{
    private static readonly HashSet<string> IntroSeenScenes = new HashSet<string>();
    private static readonly HashSet<string> DefeatedScenes = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        IntroSeenScenes.Clear();
        DefeatedScenes.Clear();
    }

    public static string GetSceneKey(Component context)
    {
        Scene scene = context != null ? context.gameObject.scene : SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(scene.path)) return scene.path;
        if (!string.IsNullOrWhiteSpace(scene.name)) return scene.name;
        return "hys_MidBoss2_UnknownScene";
    }

    public static bool HasSeenIntro(Component context)
    {
        return IntroSeenScenes.Contains(GetSceneKey(context));
    }

    public static void MarkIntroSeen(Component context)
    {
        IntroSeenScenes.Add(GetSceneKey(context));
    }

    public static bool IsDefeated(Component context)
    {
        return DefeatedScenes.Contains(GetSceneKey(context));
    }

    public static void MarkDefeated(Component context)
    {
        string key = GetSceneKey(context);
        IntroSeenScenes.Add(key);
        DefeatedScenes.Add(key);
    }
}
