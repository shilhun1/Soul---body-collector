using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// hys 씬에서 기존 Sword Player의 기능을 새 Sword 오브젝트로 안전하게 이전합니다.
public static class hys_SwordSceneTransferTool
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";

    [MenuItem("Tools/HYS/Animation/Sword Player를 Sword로 이전")]
    public static void Transfer()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException("hys 씬을 연 상태에서 실행해야 합니다: " + ScenePath);
        }

        GameObject source = FindGameObject(scene, "Sword Player");
        GameObject target = FindGameObject(scene, "Sword");
        if (source == null || target == null)
        {
            throw new InvalidOperationException("Sword Player와 Sword 오브젝트를 모두 찾을 수 있어야 합니다.");
        }

        if (source == target)
        {
            throw new InvalidOperationException("Sword Player와 Sword가 같은 오브젝트입니다.");
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Sword Player를 Sword로 이전");

        Dictionary<UnityEngine.Object, UnityEngine.Object> referenceMap =
            new Dictionary<UnityEngine.Object, UnityEngine.Object>();
        referenceMap[source] = target;
        referenceMap[source.transform] = target.transform;

        SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            targetRenderer = Undo.AddComponent<SpriteRenderer>(target);
        }

        if (sourceRenderer != null)
        {
            referenceMap[sourceRenderer] = targetRenderer;
        }

        // 새 Sword의 Red Knight Sprite는 보존하고 플레이어 기능 컴포넌트만 복사합니다.
        foreach (Component sourceComponent in source.GetComponents<Component>())
        {
            if (sourceComponent == null
                || sourceComponent is Transform
                || sourceComponent is SpriteRenderer)
            {
                continue;
            }

            Component targetComponent = CopyComponentToTarget(sourceComponent, target);
            referenceMap[sourceComponent] = targetComponent;
        }

        // 자식 오브젝트가 있다면 새 Sword 아래로 옮겨 기존 구성도 유지합니다.
        while (source.transform.childCount > 0)
        {
            Transform child = source.transform.GetChild(0);
            Undo.SetTransformParent(child, target.transform, "Sword 자식 오브젝트 이전");
        }

        Undo.RecordObject(target, "Sword 기본 설정 이전");
        target.layer = source.layer;
        target.tag = source.tag;
        target.SetActive(source.activeSelf);

        // 복사된 컴포넌트뿐 아니라 카메라 등 씬의 외부 참조도 새 Sword를 바라보게 바꿉니다.
        foreach (Component component in FindAllComponents(scene))
        {
            RemapReferences(component, referenceMap);
        }

        Undo.DestroyObjectImmediate(source);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[hys Sword] Sword Player 기능을 Sword로 이전하고 씬 참조를 다시 연결했습니다.");
    }

    // 배치 모드에서 실제 hys 씬을 열어 Sword 이전 결과와 Animator 연결을 검증합니다.
    public static void ValidateFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject target = FindGameObject(scene, "Sword");
        GameObject oldSource = FindGameObject(scene, "Sword Player");
        if (target == null || oldSource != null)
        {
            throw new InvalidOperationException("Sword 이전 결과가 올바르지 않습니다.");
        }

        Animator animator = target.GetComponent<Animator>();
        if (animator == null
            || animator.runtimeAnimatorController == null
            || animator.runtimeAnimatorController.name != "hys_Player_Sword")
        {
            throw new InvalidOperationException("Sword 전용 Animator Controller가 연결되지 않았습니다.");
        }

        int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
        if (missingScriptCount > 0)
        {
            throw new InvalidOperationException("Sword에 Missing Script가 있습니다: " + missingScriptCount);
        }

        Debug.Log("[hys Sword] 씬 검증 완료: Sword 구성 요소 "
            + target.GetComponents<Component>().Length
            + "개, Controller="
            + animator.runtimeAnimatorController.name);
    }

    private static Component CopyComponentToTarget(Component sourceComponent, GameObject target)
    {
        Type type = sourceComponent.GetType();
        Component targetComponent = target.GetComponent(type);
        ComponentUtility.CopyComponent(sourceComponent);

        if (targetComponent != null)
        {
            if (!ComponentUtility.PasteComponentValues(targetComponent))
            {
                throw new InvalidOperationException(type.Name + " 값을 Sword에 복사하지 못했습니다.");
            }

            return targetComponent;
        }

        if (!ComponentUtility.PasteComponentAsNew(target))
        {
            throw new InvalidOperationException(type.Name + " 컴포넌트를 Sword에 추가하지 못했습니다.");
        }

        Component[] candidates = target.GetComponents(type);
        return candidates[candidates.Length - 1];
    }

    private static void RemapReferences(
        Component component,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> referenceMap)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.GetIterator();
        bool changed = false;
        if (property.Next(true))
        {
            do
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                UnityEngine.Object current = property.objectReferenceValue;
                if (current != null && referenceMap.TryGetValue(current, out UnityEngine.Object replacement))
                {
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
            }
            while (property.Next(true));
        }

        if (changed)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }
    }

    private static GameObject FindGameObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                // Hierarchy에서 sword/Sword처럼 대소문자가 달라도 같은 대상으로 인식합니다.
                if (string.Equals(transform.name, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static IEnumerable<Component> FindAllComponents(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                yield return component;
            }
        }
    }
}
