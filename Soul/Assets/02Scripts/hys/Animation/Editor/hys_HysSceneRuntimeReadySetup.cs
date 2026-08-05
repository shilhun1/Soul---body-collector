#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// hys 씬에 유령 플레이어와 무기별 빙의 가능 몬스터 5종을 배치합니다.
[InitializeOnLoad]
public static class hys_HysSceneRuntimeReadySetup
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";
    private const string PlayerName = "HWJ_Runtime_Player_Soul";
    private const string PlayerPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/HWJ_Runtime_Player_Soul.prefab";
    private const string AutomaticSessionKey = "hys.HysSceneRuntimeReadySetup.SixCharacters.v2";

    private sealed class MonsterSpec
    {
        public readonly string Name;
        public readonly string PrefabPath;
        public readonly Vector3 Position;

        public MonsterSpec(string weaponName, Vector3 position)
        {
            Name = $"HWJ_Runtime_Enemy_Possessable_{weaponName}";
            PrefabPath =
                $"Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/{Name}.prefab";
            Position = position;
        }
    }

    // 기존 Sword의 좌우 빈 공간을 사용해 다섯 종류가 겹치지 않도록 배치합니다.
    private static readonly MonsterSpec[] MonsterSpecs =
    {
        new MonsterSpec("Sword", new Vector3(-10f, 4.43f, 0f)),
        new MonsterSpec("Axe", new Vector3(-22f, 4.43f, 0f)),
        new MonsterSpec("Bow", new Vector3(-16f, 4.43f, 0f)),
        new MonsterSpec("Lance", new Vector3(-4f, 4.43f, 0f)),
        new MonsterSpec("Shield", new Vector3(2f, 4.43f, 0f))
    };

    static hys_HysSceneRuntimeReadySetup()
    {
        EditorApplication.delayCall += ApplyAutomaticallyToOpenHysScene;
    }

    [MenuItem("hys/RuntimeReady/유령 플레이어와 몬스터 5종 배치")]
    public static void ApplyFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Apply(scene);
    }

    // Unity 배치 모드에서도 동일한 구성을 만들 수 있습니다.
    public static void ApplyFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Apply(scene);
    }

    private static void ApplyAutomaticallyToOpenHysScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || SessionState.GetBool(AutomaticSessionKey, false))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        // Unity가 미저장 씬을 Temp 백업 경로에서 복원해도 hys 씬 이름으로 식별합니다.
        if (!scene.IsValid()
            || (scene.path != ScenePath && !string.Equals(scene.name, "hys", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SessionState.SetBool(AutomaticSessionKey, true);
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        Dictionary<string, GameObject> roots = GetRootsByName(scene);
        RemoveCorpseTestObjects(roots);

        GameObject player = EnsurePrefabInstance(
            scene,
            roots,
            PlayerName,
            PlayerPrefabPath,
            new Vector3(-26.41f, 3.48f, 0f),
            preserveExistingPosition: true);

        List<string> placedMonsters = new List<string>();
        foreach (MonsterSpec spec in MonsterSpecs)
        {
            GameObject monster = EnsurePrefabInstance(
                scene,
                roots,
                spec.Name,
                spec.PrefabPath,
                spec.Position,
                preserveExistingPosition: spec.Name.EndsWith("Sword", StringComparison.Ordinal));

            placedMonsters.Add(monster.name);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[hys RuntimeReady] 유령 플레이어와 몬스터 5종 배치를 완료했습니다. " +
            $"Player={player.name}, Monsters={string.Join(", ", placedMonsters)}");
    }

    private static Dictionary<string, GameObject> GetRootsByName(Scene scene)
    {
        Dictionary<string, GameObject> roots = new Dictionary<string, GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            roots[root.name] = root;
        }

        return roots;
    }

    // 이전 테스트에서 사용한 시체 프리팹은 실제 빙의 가능 몬스터와 중복되므로 제거합니다.
    private static void RemoveCorpseTestObjects(Dictionary<string, GameObject> roots)
    {
        List<string> namesToRemove = new List<string>();
        foreach (KeyValuePair<string, GameObject> pair in roots)
        {
            if (!pair.Key.StartsWith("HWJ_Runtime_Corpse_", StringComparison.Ordinal))
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(pair.Value);
            namesToRemove.Add(pair.Key);
        }

        foreach (string name in namesToRemove)
        {
            roots.Remove(name);
        }
    }

    private static GameObject EnsurePrefabInstance(
        Scene scene,
        Dictionary<string, GameObject> roots,
        string objectName,
        string prefabPath,
        Vector3 defaultPosition,
        bool preserveExistingPosition)
    {
        if (roots.TryGetValue(objectName, out GameObject existing))
        {
            if (!preserveExistingPosition)
            {
                existing.transform.SetPositionAndRotation(defaultPosition, Quaternion.identity);
            }

            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"RuntimeReady 프리팹을 찾지 못했습니다: {prefabPath}");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = objectName;
        instance.transform.SetPositionAndRotation(defaultPosition, Quaternion.identity);
        roots[objectName] = instance;
        return instance;
    }
}
#endif
