#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// hys 슬라임 PNG 프레임을 픽셀아트 설정으로 가져오고 애니메이션 클립과 컨트롤러를 생성합니다.
/// GIF 원본의 프레임별 재생 시간을 유지하며 HWJ 프리팹은 수정하지 않습니다.
/// </summary>
[InitializeOnLoad]
public static class hys_SlimeAnimationBuilder
{
    private const string Root = "Assets/05Anims/hys_Enemy_Anims/Slime";
    private const string SpriteRoot = Root + "/Sprites";
    private const string ClipRoot = Root + "/Clips";
    private const string ControllerPath = Root + "/hys_Monster_Slime.controller";
    private const string SessionKey = "hys.SlimeAnimationBuilder.v1";

    private sealed class AnimationSpec
    {
        public string Name;
        public float[] Durations;
        public bool Loop;
    }

    private static readonly AnimationSpec[] Specs =
    {
        Spec("Idle", true, 0.2f, 0.2f, 0.2f, 0.2f),
        Spec("Walk", true, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f),
        Spec("Hit", false, 0.25f, 0.12f, 0.12f, 0.12f),
        Spec("Die", false, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f)
    };

    static hys_SlimeAnimationBuilder()
    {
        if (!SessionState.GetBool(SessionKey, false)
            && AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null)
        {
            EditorApplication.delayCall += GenerateOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/슬라임 애니메이션 생성")]
    public static void Generate()
    {
        EnsureFolder(ClipRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (AnimationSpec spec in Specs)
        {
            Sprite[] sprites = LoadAndConfigureSprites(spec);
            clips[spec.Name] = CreateOrUpdateClip(spec, sprites);
        }

        CreateOrUpdateController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hys Slime Animation] Idle, Walk, Hit, Die 클립과 슬라임 Animator Controller를 생성했습니다.");
    }

    private static void GenerateOnce()
    {
        EditorApplication.delayCall -= GenerateOnce;
        try
        {
            Generate();
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
            .Where(path => Path.GetFileName(path).StartsWith($"hys_Slime_{spec.Name}_", StringComparison.Ordinal))
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

        // Unity 6에서는 Sprite 정렬과 Pivot을 TextureImporterSettings를 통해 적용합니다.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateOrUpdateClip(AnimationSpec spec, Sprite[] sprites)
    {
        string path = $"{ClipRoot}/hys_Slime_{spec.Name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = $"hys_Slime_{spec.Name}";
        clip.frameRate = 100f;

        List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
        float time = 0f;
        for (int i = 0; i < sprites.Length; i++)
        {
            keys.Add(new ObjectReferenceKeyframe { time = time, value = sprites[i] });
            time += spec.Durations[i];
        }

        // 마지막 프레임도 GIF에 기록된 시간만큼 유지되도록 종료 키를 하나 더 둡니다.
        keys.Add(new ObjectReferenceKeyframe { time = time, value = sprites[sprites.Length - 1] });

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

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

    private static void CreateOrUpdateController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController oldController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (oldController != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("HitTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DeadTrigger", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "hys_Slime_Idle", clips["Idle"], new Vector3(100f, 80f));
        AnimatorState walk = AddState(machine, "hys_Slime_Walk", clips["Walk"], new Vector3(350f, 80f));
        AnimatorState hit = AddState(machine, "hys_Slime_Hit", clips["Hit"], new Vector3(225f, 220f));
        AnimatorState die = AddState(machine, "hys_Slime_Die", clips["Die"], new Vector3(500f, 220f));
        machine.defaultState = idle;

        AddBoolTransition(idle, walk, "IsMoving", true);
        AddBoolTransition(walk, idle, "IsMoving", false);

        AnimatorStateTransition hitTransition = machine.AddAnyStateTransition(hit);
        ConfigureImmediateTransition(hitTransition);
        hitTransition.canTransitionToSelf = false;
        hitTransition.AddCondition(AnimatorConditionMode.If, 0f, "HitTrigger");
        hitTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        AnimatorStateTransition hitExit = hit.AddTransition(idle);
        hitExit.hasExitTime = true;
        hitExit.exitTime = 1f;
        hitExit.duration = 0f;

        AnimatorStateTransition deadTriggerTransition = machine.AddAnyStateTransition(die);
        ConfigureImmediateTransition(deadTriggerTransition);
        deadTriggerTransition.canTransitionToSelf = false;
        deadTriggerTransition.AddCondition(AnimatorConditionMode.If, 0f, "DeadTrigger");

        AnimatorStateTransition deadStateTransition = machine.AddAnyStateTransition(die);
        ConfigureImmediateTransition(deadStateTransition);
        deadStateTransition.canTransitionToSelf = false;
        deadStateTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        EditorUtility.SetDirty(controller);
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
