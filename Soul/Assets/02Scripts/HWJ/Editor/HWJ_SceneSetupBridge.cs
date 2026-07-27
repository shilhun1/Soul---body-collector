using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HWJ 테스트 씬에 필요한 런타임 컴포넌트와 GameManager 참조를 실제 씬 파일에 반영합니다.
/// 이 도구는 HWJ 씬만 대상으로 하며 다른 씬이나 ProjectSettings는 수정하지 않습니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_SceneSetupBridge
{
    private const string ScenePath = "Assets/01Scenes/HWJ.unity";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunSceneSetup.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_SceneSetupReport.md";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_SceneSetupBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Scene/Apply HWJ Scene Test Setup")]
    public static void ApplySceneSetupFromMenu()
    {
        ApplySceneSetup("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Scene/Create HWJ Scene Setup Flag")]
    public static void CreateSceneSetupFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
        Debug.Log($"HWJ scene setup flag created: {FlagFilePath}");
    }

    private static void PollFlagFile()
    {
        if (EditorApplication.timeSinceStartup < nextFlagCheckTime)
        {
            return;
        }

        nextFlagCheckTime = EditorApplication.timeSinceStartup + 2d;

        if (!File.Exists(FlagFilePath))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        File.Delete(FlagFilePath);
        ApplySceneSetup("flag file");
    }

    private static void ApplySceneSetup(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ scene setup is already running.");
            return;
        }

        isRunning = true;

        try
        {
            HWJ_SceneSetupReport report = ApplySetupToScene();
            WriteReport(source, report);
            Debug.Log($"HWJ scene setup finished. Changed={report.Changed} Report={ReportFilePath}");
        }
        catch (Exception exception)
        {
            WriteExceptionReport(source, exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static HWJ_SceneSetupReport ApplySetupToScene()
    {
        List<string> changes = new List<string>();
        Scene activeScene = EnsureHwjSceneOpen();

        HWJ_RootObjectDataResolver playerResolver = FindPlayerResolver();

        if (playerResolver == null)
        {
            throw new InvalidOperationException("HWJ scene setup failed: Player resolver was not found in the HWJ scene.");
        }

        EnsurePlayerComponent<HWJ_RuntimeStatusSystem>(playerResolver.gameObject, changes);
        EnsurePlayerComponent<HWJ_SoulSystem>(playerResolver.gameObject, changes);
        EnsurePlayerComponent<HWJ_PossessedBodySystem>(playerResolver.gameObject, changes);
        EnsurePlayerComponent<HWJ_PossessionSystem>(playerResolver.gameObject, changes);
        EnsurePlayerPossessionMentalComponent(playerResolver.gameObject, changes);
        EnsurePlayerComponent<HWJ_CollapseSystem>(playerResolver.gameObject, changes);
        EnsurePlayerComponent<HWJ_BodyDiscoverySystem>(playerResolver.gameObject, changes);

        WireGameManagerPlayerReferences(playerResolver, changes);

        if (changes.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
        }

        return new HWJ_SceneSetupReport(
            activeScene.name,
            activeScene.path,
            GetSceneObjectPath(playerResolver.gameObject),
            changes);
    }

    private static Scene EnsureHwjSceneOpen()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid() && activeScene.path == ScenePath)
        {
            return activeScene;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        throw new InvalidOperationException("HWJ scene setup cancelled because modified scenes were not saved.");
    }

    private static void EnsurePlayerComponent<T>(GameObject playerObject, List<string> changes)
        where T : Component
    {
        if (playerObject.GetComponent<T>() != null)
        {
            return;
        }

        Undo.AddComponent<T>(playerObject);
        changes.Add($"Added `{typeof(T).Name}` to `{GetSceneObjectPath(playerObject)}`.");
    }

    private static void EnsurePlayerPossessionMentalComponent(GameObject playerObject, List<string> changes)
    {
        if (playerObject.GetComponent<HWJ_BodyDecaySystem>() != null)
        {
            return;
        }

        Undo.AddComponent<HWJ_PossessionMentalSystem>(playerObject);
        changes.Add($"Added `HWJ_PossessionMentalSystem` to `{GetSceneObjectPath(playerObject)}`.");
    }

    private static void WireGameManagerPlayerReferences(
        HWJ_RootObjectDataResolver playerResolver,
        List<string> changes)
    {
        HWJ_GameManager manager = UnityEngine.Object.FindFirstObjectByType<HWJ_GameManager>();

        if (manager == null)
        {
            changes.Add("Skipped GameManager reference wiring because `HWJ_GameManager` was not found.");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        bool changed = false;

        changed |= SetObjectReference(serializedManager, "playerResolver", playerResolver);
        changed |= SetObjectReference(serializedManager, "playerInput", playerResolver.GetComponent<HWJ_PlayerInputSystem>());
        changed |= SetObjectReference(serializedManager, "playerStatus", playerResolver.GetComponent<HWJ_RuntimeStatusSystem>());
        changed |= SetObjectReference(serializedManager, "playerLevel", playerResolver.GetComponent<HWJ_LevelUpSystem>());
        changed |= SetObjectReference(serializedManager, "playerSoul", playerResolver.GetComponent<HWJ_SoulSystem>());
        changed |= SetObjectReference(serializedManager, "playerBodyDecay", playerResolver.GetComponent<HWJ_BodyDecaySystem>());
        changed |= SetObjectReference(serializedManager, "playerPossession", playerResolver.GetComponent<HWJ_PossessionSystem>());

        if (changed)
        {
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            changes.Add($"Wired `HWJ_GameManager` player references to `{GetSceneObjectPath(playerResolver.gameObject)}`.");
        }
    }

    private static bool SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }

    private static HWJ_RootObjectDataResolver FindPlayerResolver()
    {
        HWJ_RootObjectDataResolver[] resolvers = UnityEngine.Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver != null
                && resolver.RootObjectData != null
                && resolver.ObjectType == HWJ_ObjectType.Player)
            {
                return resolver;
            }
        }

        return null;
    }

    private static string GetSceneObjectPath(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return "Missing Scene Object";
        }

        Stack<string> pathSegments = new Stack<string>();
        Transform current = targetObject.transform;

        while (current != null)
        {
            pathSegments.Push(current.name);
            current = current.parent;
        }

        string sceneName = string.IsNullOrEmpty(targetObject.scene.name)
            ? "Unsaved Scene"
            : targetObject.scene.name;
        return sceneName + "/" + string.Join("/", pathSegments.ToArray());
    }

    private static void WriteReport(string source, HWJ_SceneSetupReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 씬 세팅 적용 보고서");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 씬 이름: {report.SceneName}");
        builder.AppendLine($"- 씬 경로: {report.ScenePath}");
        builder.AppendLine($"- 플레이어: {report.PlayerPath}");
        builder.AppendLine($"- 변경 여부: {(report.Changed ? "변경됨" : "변경 없음")}");
        builder.AppendLine();

        if (report.Changes.Count == 0)
        {
            builder.AppendLine("이미 필요한 씬 세팅이 적용되어 있습니다.");
        }
        else
        {
            builder.AppendLine("## 변경 내용");
            builder.AppendLine();

            for (int i = 0; i < report.Changes.Count; i++)
            {
                builder.AppendLine($"- {report.Changes[i]}");
            }
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteExceptionReport(string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 씬 세팅 적용 예외\n\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n\n```text\n{exception}\n```\n",
            Encoding.UTF8);
    }

    private sealed class HWJ_SceneSetupReport
    {
        public readonly string SceneName;
        public readonly string ScenePath;
        public readonly string PlayerPath;
        public readonly List<string> Changes;
        public bool Changed => Changes.Count > 0;

        public HWJ_SceneSetupReport(
            string sceneName,
            string scenePath,
            string playerPath,
            List<string> changes)
        {
            SceneName = sceneName;
            ScenePath = scenePath;
            PlayerPath = playerPath;
            Changes = changes ?? new List<string>();
        }
    }
}
