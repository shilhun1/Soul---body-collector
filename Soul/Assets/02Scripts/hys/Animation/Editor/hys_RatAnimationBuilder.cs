#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// hys 쥐 PNG 프레임을 픽셀아트로 가져오고 RUN1 시작 동작과 RUN2 반복 동작을 포함한 Animator를 생성합니다.
/// 열린 hys 씬의 쥐 인스턴스만 연결하며 HWJ 원본 프리팹은 수정하지 않습니다.
/// </summary>
[InitializeOnLoad]
public static class hys_RatAnimationBuilder
{
    private const string Root = "Assets/05Anims/hys_Enemy_Anims/Rat";
    private const string SpriteRoot = Root + "/Sprites";
    private const string ClipRoot = Root + "/Clips";
    private const string ControllerPath = Root + "/hys_Monster_Rat.controller";
    private const string HysScenePath = "Assets/01Scenes/hys.unity";
    private const string SessionKey = "hys.RatAnimationBuilder.v1";

    private sealed class AnimationSpec
    {
        public string Name;
        public float[] Durations;
        public bool Loop;
    }

    private static readonly AnimationSpec[] Specs =
    {
        Spec("Idle", true, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f),
        Spec("RunStart", false, 0.2f, 0.2f, 0.2f),
        Spec("RunLoop", true, 0.2f, 0.2f, 0.2f, 0.2f),
        Spec("Attack", false, 0.2f, 0.2f, 0.2f, 0.2f),
        Spec("Hurt", false, 0.2f, 0.5f, 0.2f),
        Spec("Die", false, 0.2f, 0.2f, 0.2f, 0.2f)
    };

    static hys_RatAnimationBuilder()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += SetupOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/쥐 애니메이션 생성 및 hys 씬 연결")]
    public static void Generate()
    {
        EditorApplication.delayCall -= SetupOnce;
        SessionState.SetBool(SessionKey, true);
        EnsureFolder(ClipRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (AnimationSpec spec in Specs)
        {
            Sprite[] sprites = LoadAndConfigureSprites(spec);
            clips[spec.Name] = CreateOrUpdateClip(spec, sprites);
        }

        AnimatorController controller = CreateOrUpdateController(clips);
        int wiredCount = WireOpenHysScene(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[hys Rat Animation] Idle, RUN1, RUN2, Attack, Hurt, Die 생성 완료 - hys 씬 연결 {wiredCount}개");
    }

    [MenuItem("Tools/hys/Animation/hys 씬 열기")]
    public static void OpenHysScene()
    {
        // 검증 뒤 다른 씬이 열렸을 때 저장된 hys 씬으로 안전하게 돌아옵니다.
        EditorSceneManager.OpenScene(HysScenePath, OpenSceneMode.Single);
    }

    private static void SetupOnce()
    {
        EditorApplication.delayCall -= SetupOnce;
        try
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Generate();
            }
            else
            {
                int wiredCount = WireOpenHysScene(controller);
                Debug.Log($"[hys Rat Animation] 기존 Controller 확인 - hys 씬 추가 연결 {wiredCount}개");
            }

            SessionState.SetBool(SessionKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Sprite[] LoadAndConfigureSprites(AnimationSpec spec)
    {
        string folder = $"{SpriteRoot}/{spec.Name}";
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileName(path).StartsWith($"hys_Rat_{spec.Name}_", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (paths.Length != spec.Durations.Length)
        {
            throw new InvalidOperationException(
                $"{spec.Name} 프레임 수가 다릅니다. 예상 {spec.Durations.Length}, 실제 {paths.Length}");
        }

        Sprite[] sprites = new Sprite[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            ConfigureTexture(paths[i]);
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprites[i] == null)
            {
                throw new InvalidOperationException($"Sprite를 불러오지 못했습니다: {paths[i]}");
            }
        }

        return sprites;
    }

    private static void ConfigureTexture(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"TextureImporter를 찾지 못했습니다: {assetPath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        // 바닥 중심 Pivot으로 맞춰 모션이 바뀌어도 발 위치가 흔들리지 않게 합니다.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateOrUpdateClip(AnimationSpec spec, Sprite[] sprites)
    {
        string path = $"{ClipRoot}/hys_Rat_{spec.Name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = $"hys_Rat_{spec.Name}";
        clip.frameRate = 100f;

        List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
        float time = 0f;
        for (int i = 0; i < sprites.Length; i++)
        {
            keys.Add(new ObjectReferenceKeyframe { time = time, value = sprites[i] });
            time += spec.Durations[i];
        }

        // 마지막 프레임도 GIF에 지정된 시간만큼 표시합니다.
        keys.Add(new ObjectReferenceKeyframe { time = time, value = sprites[sprites.Length - 1] });
        AnimationUtility.SetObjectReferenceCurve(
            clip,
            new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            },
            keys.ToArray());

        // 기존 조립형 쥐의 귀와 꼬리를 숨겨 완성형 GIF와 겹치지 않게 합니다.
        SetRendererEnabledCurve(clip, "LeftEar", time, false);
        SetRendererEnabledCurve(clip, "RightEar", time, false);
        SetRendererEnabledCurve(clip, "Tail", time, false);

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = spec.Loop;
        }

        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void SetRendererEnabledCurve(AnimationClip clip, string path, float length, bool enabled)
    {
        AnimationUtility.SetEditorCurve(
            clip,
            new EditorCurveBinding
            {
                path = path,
                type = typeof(SpriteRenderer),
                propertyName = "m_Enabled"
            },
            AnimationCurve.Constant(0f, length, enabled ? 1f : 0f));
    }

    private static AnimatorController CreateOrUpdateController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController oldController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (oldController != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HitTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DeadTrigger", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "hys_Rat_Idle", clips["Idle"], new Vector3(80f, 80f));
        AnimatorState runStart = AddState(machine, "hys_Rat_RunStart", clips["RunStart"], new Vector3(310f, 40f));
        AnimatorState runLoop = AddState(machine, "hys_Rat_RunLoop", clips["RunLoop"], new Vector3(540f, 40f));
        AnimatorState attack = AddState(machine, "hys_Rat_Attack", clips["Attack"], new Vector3(220f, 230f));
        AnimatorState hurt = AddState(machine, "hys_Rat_Hurt", clips["Hurt"], new Vector3(440f, 230f));
        AnimatorState die = AddState(machine, "hys_Rat_Die", clips["Die"], new Vector3(660f, 230f));
        machine.defaultState = idle;

        AddBoolTransition(idle, runStart, "IsMoving", true);
        AddBoolTransition(runStart, idle, "IsMoving", false);
        AnimatorStateTransition startToLoop = runStart.AddTransition(runLoop);
        startToLoop.hasExitTime = true;
        startToLoop.exitTime = 1f;
        startToLoop.duration = 0f;
        AddBoolTransition(runLoop, idle, "IsMoving", false);

        // 죽음 전환을 먼저 등록해 다른 Trigger보다 우선 처리합니다.
        AddAnyTriggerTransition(machine, die, "DeadTrigger", false);
        AddAnyBoolTransition(machine, die, "IsDead", true);
        AddAnyTriggerTransition(machine, hurt, "HitTrigger", true);
        AddAnyTriggerTransition(machine, attack, "AttackTrigger", true);
        AddExitTransition(attack, idle);
        AddExitTransition(hurt, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static int WireOpenHysScene(AnimatorController controller)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != HysScenePath)
        {
            Debug.LogWarning($"[hys Rat Animation] hys 씬이 열려 있지 않아 인스턴스 연결은 생략했습니다: {scene.path}");
            return 0;
        }

        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        int count = 0;
        int restoredCount = 0;
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.gameObject.scene != scene)
            {
                continue;
            }

            if (!IsRatAnimator(animator))
            {
                if (NeedsIncorrectAssignmentRestore(animator, controller))
                {
                    RestoreIncorrectAssignment(animator);
                    restoredCount++;
                }

                continue;
            }

            if (animator.runtimeAnimatorController == controller)
            {
                continue;
            }

            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (restoredCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[hys Rat Animation] 잘못 연결된 다른 몬스터 Animator {restoredCount}개를 원래 상태로 복원했습니다.");
        }

        return count;
    }

    private static bool NeedsIncorrectAssignmentRestore(Animator animator, AnimatorController ratController)
    {
        if (animator.runtimeAnimatorController == ratController)
        {
            return true;
        }

        if (IsSlimeAnimator(animator))
        {
            return animator.runtimeAnimatorController == null
                || animator.runtimeAnimatorController.name != "hys_Monster_Slime";
        }

        if (!IsPossessableSwordAnimator(animator) || animator.runtimeAnimatorController != null)
        {
            return false;
        }

        SerializedObject serializedAnimator = new SerializedObject(animator);
        SerializedProperty controllerProperty = serializedAnimator.FindProperty("m_Controller");
        return controllerProperty != null && controllerProperty.prefabOverride;
    }

    private static bool IsRatAnimator(Animator animator)
    {
        // RuntimeReady 프리팹은 모델 이름을 HWJ_Visual로 바꾸므로 부모의 Rat 이름까지 확인합니다.
        Transform current = animator.transform;
        while (current != null)
        {
            if (IsRatAssetName(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
        string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
        return IsRatAssetName(Path.GetFileNameWithoutExtension(sourcePath));
    }

    private static bool IsRatAssetName(string value)
    {
        return !string.IsNullOrEmpty(value)
            && (value.Equals("Rat", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("_Rat", StringComparison.OrdinalIgnoreCase)
                || value.IndexOf("General_Rat", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void RestoreIncorrectAssignment(Animator animator)
    {
        if (IsSlimeAnimator(animator))
        {
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/05Anims/hys_Enemy_Anims/Slime/hys_Monster_Slime.controller");
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return;
        }

        // 그 외 몬스터는 Rat 작업 전 프리팹에서 상속하던 Controller 값으로 되돌립니다.
        SerializedObject serializedAnimator = new SerializedObject(animator);
        SerializedProperty controllerProperty = serializedAnimator.FindProperty("m_Controller");
        if (controllerProperty != null && controllerProperty.prefabOverride)
        {
            PrefabUtility.RevertPropertyOverride(controllerProperty, InteractionMode.AutomatedAction);
        }
    }

    private static bool IsSlimeAnimator(Animator animator)
    {
        Transform current = animator.transform;
        while (current != null)
        {
            if (current.name.EndsWith("_Slime", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
        string sourceName = source != null
            ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source))
            : string.Empty;
        return sourceName.EndsWith("_Slime", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPossessableSwordAnimator(Animator animator)
    {
        Transform current = animator.transform;
        while (current != null)
        {
            if (current.name.IndexOf("Possessable_Sword", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
        string sourceName = source != null
            ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source))
            : string.Empty;
        return sourceName.IndexOf("Possessable_Sword", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string name,
        Motion motion,
        Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);
        state.motion = motion;
        return state;
    }

    private static void AddBoolTransition(
        AnimatorState source,
        AnimatorState destination,
        string parameter,
        bool expected)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(
            expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            parameter);
    }

    private static void AddAnyBoolTransition(
        AnimatorStateMachine machine,
        AnimatorState destination,
        string parameter,
        bool expected)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.canTransitionToSelf = false;
        transition.AddCondition(
            expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            parameter);
    }

    private static void AddAnyTriggerTransition(
        AnimatorStateMachine machine,
        AnimatorState destination,
        string trigger,
        bool requireAlive)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        if (requireAlive)
        {
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        }
    }

    private static void AddExitTransition(AnimatorState source, AnimatorState destination)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = 0f;
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
    }

    private static AnimationSpec Spec(string name, bool loop, params float[] durations)
    {
        return new AnimationSpec
        {
            Name = name,
            Loop = loop,
            Durations = durations
        };
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
