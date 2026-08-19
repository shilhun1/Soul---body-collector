#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// hys 씬의 플레이어를 HWJ 시스템 하나로 구성하고 무기별 애니메이션 프로필을 연결합니다.
[InitializeOnLoad]
public static class hys_HysSceneRuntimeReadySetup
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";
    private const string PlayerName = "HWJ_Runtime_Player_Soul";
    private const string LegacyPlayerName = "HWJ_Player";
    private const string PlayerPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/HWJ_Runtime_Player_Soul.prefab";
    private const string SoulDataPath =
        "Assets/02Scripts/hys/hys_Data/hys_Player_Soul_RootObjectData.asset";
    private const string ProfileRoot =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/hys_Animation_RuntimeReady/Profiles/";
    private const string AutomaticSessionKey = "hys.HysSceneRuntimeReadySetup.HwjAnimatorAndFacing.v5";

    private static readonly string[] RemovedHysRuntimeComponents =
    {
        "hys_Player_Attack",
        "hys_Player_State",
        "hys_Player_Animator",
        "hys_AnimatorReturnToEntry",
        "hys_Ghost_Animator",
        "hys_PlayerSkillEffectPlayer"
    };

    private static readonly string[] PlayerControllerPaths =
    {
        "Assets/05Anims/Player_Anims/hys_Player_Sword.controller",
        "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller",
        "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller",
        "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller",
        "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller"
    };

    private static readonly KeyValuePair<string, AnimatorControllerParameterType>[] HwJAnimatorParameters =
    {
        new KeyValuePair<string, AnimatorControllerParameterType>("Speed", AnimatorControllerParameterType.Float),
        new KeyValuePair<string, AnimatorControllerParameterType>("HorizontalSpeed", AnimatorControllerParameterType.Float),
        new KeyValuePair<string, AnimatorControllerParameterType>("VerticalSpeed", AnimatorControllerParameterType.Float),
        new KeyValuePair<string, AnimatorControllerParameterType>("IsMoving", AnimatorControllerParameterType.Bool),
        new KeyValuePair<string, AnimatorControllerParameterType>("IsGrounded", AnimatorControllerParameterType.Bool),
        new KeyValuePair<string, AnimatorControllerParameterType>("IsPossessed", AnimatorControllerParameterType.Bool),
        new KeyValuePair<string, AnimatorControllerParameterType>("IsSoul", AnimatorControllerParameterType.Bool),
        new KeyValuePair<string, AnimatorControllerParameterType>("IsDead", AnimatorControllerParameterType.Bool),
        new KeyValuePair<string, AnimatorControllerParameterType>("RuntimeState", AnimatorControllerParameterType.Int),
        new KeyValuePair<string, AnimatorControllerParameterType>("WeaponType", AnimatorControllerParameterType.Int),
        new KeyValuePair<string, AnimatorControllerParameterType>("AttackTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("JumpTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("DoubleJumpTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("DropJumpTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("DashTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("HitTrigger", AnimatorControllerParameterType.Trigger),
        new KeyValuePair<string, AnimatorControllerParameterType>("DeadTrigger", AnimatorControllerParameterType.Trigger)
    };

    static hys_HysSceneRuntimeReadySetup()
    {
        EditorApplication.delayCall += ApplyAutomaticallyToOpenHysScene;
    }

    [MenuItem("hys/RuntimeReady/HWJ 전용 플레이어와 애니메이션 설정")]
    public static void ApplyFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Apply(scene);
    }

    // 배치 모드 검증에서도 같은 설정을 재현할 수 있습니다.
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
        ConfigureHwJAnimatorParameters();
        Dictionary<string, GameObject> roots = GetRootsByName(scene);
        GameObject player = FindExistingPlayer(roots) ?? EnsurePrefabInstance(
            scene,
            roots,
            PlayerName,
            PlayerPrefabPath,
            new Vector3(-26.41f, 3.48f, 0f));

        ConfigurePlayerPossession(player);
        RemoveHysRuntimeAnimationComponents(player);
        ConfigureHwJOnlyAnimation(player);
        ConfigureExistingPossessionTargets(roots);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[hys RuntimeReady] HWJ 전용 애니메이션 구성을 완료했습니다. Player={player.name}");
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

    private static GameObject FindExistingPlayer(Dictionary<string, GameObject> roots)
    {
        if (roots.TryGetValue(PlayerName, out GameObject runtimePlayer))
        {
            return runtimePlayer;
        }

        roots.TryGetValue(LegacyPlayerName, out GameObject legacyPlayer);
        return legacyPlayer;
    }

    // 빙의 대상 감지 범위와 레이어는 기존 hys 씬 설정을 유지합니다.
    private static void ConfigurePlayerPossession(GameObject player)
    {
        HWJ_PossessionInteractionController interaction =
            player.GetComponent<HWJ_PossessionInteractionController>();
        if (interaction == null)
        {
            throw new InvalidOperationException("hys 플레이어에 HWJ_PossessionInteractionController가 없습니다.");
        }

        SerializedObject serializedInteraction = new SerializedObject(interaction);
        serializedInteraction.FindProperty("targetLayerMask").intValue = 1 << 3;
        SerializedProperty radius = serializedInteraction.FindProperty("detectionRadius");
        radius.floatValue = Mathf.Max(2.5f, radius.floatValue);
        serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
    }

    // Animator를 동시에 갱신하던 hys 컴포넌트를 제거해 HWJ 모션 시스템만 남깁니다.
    private static void RemoveHysRuntimeAnimationComponents(GameObject player)
    {
        MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || Array.IndexOf(RemovedHysRuntimeComponents, behaviour.GetType().Name) < 0)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(behaviour, true);
        }

        foreach (Component component in player.GetComponents<Component>())
        {
            if (component == null || component.GetType().FullName != "HWJ.PlayerAttackRouter")
            {
                continue;
            }

            SerializedObject router = new SerializedObject(component);
            SerializedProperty hysAttack = router.FindProperty("hysAttackComponent");
            if (hysAttack != null)
            {
                hysAttack.objectReferenceValue = null;
            }

            SerializedProperty attackSystem = router.FindProperty("currentAttackSystem");
            if (attackSystem != null)
            {
                attackSystem.intValue = 1;
            }

            router.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // 점프, 대쉬, 공격, 피격, 사망을 포함한 다섯 무기 프로필을 HWJ 시스템에 직접 연결합니다.
    private static void ConfigureHwJOnlyAnimation(GameObject player)
    {
        HWJ_CharacterMotionSystem motion = player.GetComponent<HWJ_CharacterMotionSystem>();
        if (motion == null)
        {
            throw new InvalidOperationException("hys 플레이어에 HWJ_CharacterMotionSystem이 없습니다.");
        }

        Animator animator = player.GetComponentInChildren<Animator>(true);
        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>(true);
        SerializedObject serializedMotion = new SerializedObject(motion);
        SetObject(serializedMotion, "animator", animator);
        SetObject(serializedMotion, "spriteRenderer", spriteRenderer);
        SetObject(serializedMotion, "body", player.GetComponent<Rigidbody2D>());
        SetObject(serializedMotion, "dataResolver", player.GetComponent<HWJ_RootObjectDataResolver>());
        SetObject(serializedMotion, "runtimeStatus", player.GetComponent<HWJ_RuntimeStatusSystem>());
        SetObject(serializedMotion, "playerMovement", player.GetComponent<HWJ_PlayerMovementSystem>());
        SetObject(serializedMotion, "soulSystem", player.GetComponent<HWJ_SoulSystem>());
        SetObject(serializedMotion, "possessionSystem", player.GetComponent<HWJ_PossessionSystem>());
        SerializedProperty useDirectFlip = serializedMotion.FindProperty("useInitialSpriteFlipAsRightFacing");
        if (useDirectFlip != null)
        {
            // 오른쪽은 flipX=false, 왼쪽은 flipX=true로 고정해 무기 원본의 서로 다른 초기 반전을 무시합니다.
            useDirectFlip.boolValue = false;
        }

        string[] profileNames = { "Sword", "Axe", "Bow", "Lance", "Shield" };
        SerializedProperty profiles = serializedMotion.FindProperty("weaponMotionProfiles");
        profiles.arraySize = profileNames.Length;
        for (int i = 0; i < profileNames.Length; i++)
        {
            profiles.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                $"{ProfileRoot}hys_RuntimeReady_Player_{profileNames[i]}MotionProfile.asset");
        }

        serializedMotion.ApplyModifiedPropertiesWithoutUndo();

        // 영혼 상태에는 무기 프로필을 적용하지 않고, 빙의했을 때만 해당 육체 컨트롤러로 전환합니다.
        HWJ_RootObjectDataResolver resolver = player.GetComponent<HWJ_RootObjectDataResolver>();
        SerializedObject serializedResolver = new SerializedObject(resolver);
        SetObject(serializedResolver, "rootObjectData", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SoulDataPath));
        serializedResolver.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(SerializedObject target, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    // 모든 hys 무기 Controller가 HWJ_CharacterMotionSystem의 파라미터 이름과 자료형을 공유하도록 맞춥니다.
    private static void ConfigureHwJAnimatorParameters()
    {
        foreach (string controllerPath in PlayerControllerPaths)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"hys Animator Controller를 찾지 못했습니다: {controllerPath}");
            }

            foreach (KeyValuePair<string, AnimatorControllerParameterType> spec in HwJAnimatorParameters)
            {
                AnimatorControllerParameter existing = Array.Find(
                    controller.parameters,
                    parameter => parameter.name == spec.Key);

                if (existing != null && existing.type == spec.Value)
                {
                    continue;
                }

                if (existing != null)
                {
                    controller.RemoveParameter(existing);
                }

                controller.AddParameter(spec.Key, spec.Value);
            }

            EditorUtility.SetDirty(controller);
        }
    }

    // hys 원본 스프라이트는 오른쪽을 바라보므로 빙의 시 flipX=false를 오른쪽 기준으로 사용합니다.
    private static void ConfigureExistingPossessionTargets(Dictionary<string, GameObject> roots)
    {
        foreach (KeyValuePair<string, GameObject> pair in roots)
        {
            if (!pair.Key.StartsWith("HWJ_Runtime_Enemy_Possessable_", StringComparison.Ordinal)
                || pair.Value == null)
            {
                continue;
            }

            HWJ_CharacterMotionSystem targetMotion = pair.Value.GetComponent<HWJ_CharacterMotionSystem>();
            SpriteRenderer targetRenderer = null;

            if (targetMotion != null)
            {
                SerializedObject serializedMotion = new SerializedObject(targetMotion);
                SerializedProperty rendererProperty = serializedMotion.FindProperty("spriteRenderer");
                targetRenderer = rendererProperty != null
                    ? rendererProperty.objectReferenceValue as SpriteRenderer
                    : null;
            }

            if (targetRenderer == null)
            {
                targetRenderer = pair.Value.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (targetRenderer != null)
            {
                targetRenderer.flipX = false;
                EditorUtility.SetDirty(targetRenderer);
            }
        }
    }

    private static GameObject EnsurePrefabInstance(
        Scene scene,
        Dictionary<string, GameObject> roots,
        string objectName,
        string prefabPath,
        Vector3 defaultPosition)
    {
        if (roots.TryGetValue(objectName, out GameObject existing))
        {
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
