#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 배치 검증에서 HYS 중간보스2 씬을 열고 실제 Play Mode를 시작합니다.
public static class hys_MidBoss2PlayModeLauncher
{
    private const string ScenePath = "Assets/01Scenes/Soul_Test/hys middle boss2.unity";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[hys MidBoss2 PlayMode] 검증용 Play Mode를 시작합니다.");
        EditorApplication.isPlaying = true;
    }
}
#endif
