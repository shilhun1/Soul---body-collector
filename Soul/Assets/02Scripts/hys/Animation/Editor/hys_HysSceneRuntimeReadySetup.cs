#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// hys 테스트 씬을 RuntimeReady 유령 시작 및 시체 빙의 구조로 자동 구성합니다.
public static class hys_HysSceneRuntimeReadySetup
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";
    private const string PlayerPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/HWJ_Runtime_Player_Soul.prefab";
    private const string CorpsePrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Corpses/HWJ_Runtime_Corpse_Shield.prefab";
    private const string EnemyPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_Possessable_Sword.prefab";

    private static readonly string[] RemovedCharacterNames =
    {
        "hys_Shield_Player",
        "hys_ShieldMonster_Test",
        "HWJ_Runtime_Player_Soul",
        "HWJ_Runtime_Corpse_Shield",
        "HWJ_Runtime_Enemy_Possessable_Sword"
    };

    [MenuItem("hys/RuntimeReady/유령 시작 빙의 테스트 씬 배치")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    // Unity 배치 모드에서도 동일한 씬 구성을 실행할 수 있습니다.
    public static void ApplyFromCommandLine()
    {
        Apply();
    }

    private static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Vector3 playerPosition = new Vector3(-26.41f, 3.48f, 0f);
        Vector3 enemyPosition = new Vector3(-10f, 4.43f, 0f);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "hys_Shield_Player" || root.name == "HWJ_Runtime_Player_Soul")
            {
                playerPosition = root.transform.position;
            }

            if (root.name == "hys_ShieldMonster_Test"
                || root.name == "HWJ_Runtime_Enemy_Possessable_Sword")
            {
                enemyPosition = root.transform.position;
            }
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(RemovedCharacterNames, root.name) >= 0)
            {
                Object.DestroyImmediate(root);
            }
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject corpsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorpsePrefabPath);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);

        if (playerPrefab == null || corpsePrefab == null || enemyPrefab == null)
        {
            throw new System.InvalidOperationException(
                "RuntimeReady 플레이어, Shield 시체 또는 Sword 몬스터 프리팹을 찾지 못했습니다.");
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        player.name = "HWJ_Runtime_Player_Soul";
        player.transform.SetPositionAndRotation(playerPosition, Quaternion.identity);

        GameObject corpse = (GameObject)PrefabUtility.InstantiatePrefab(corpsePrefab, scene);
        corpse.name = "HWJ_Runtime_Corpse_Shield";
        corpse.transform.SetPositionAndRotation(playerPosition + new Vector3(3f, 0f, 0f), Quaternion.identity);

        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
        enemy.name = "HWJ_Runtime_Enemy_Possessable_Sword";
        enemy.transform.SetPositionAndRotation(enemyPosition, Quaternion.identity);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[hys RuntimeReady 씬] 유령 플레이어, Shield 시체와 Sword 전투 몬스터를 배치했습니다. " +
            $"Player={player.transform.position}, Corpse={corpse.transform.position}, Enemy={enemy.transform.position}");
    }
}
#endif
