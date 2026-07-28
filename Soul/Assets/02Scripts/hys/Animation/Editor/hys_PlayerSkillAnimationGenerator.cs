#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 플레이어 5종의 스킬 모션 키에 맞춰 비어 있는 전용 클립과 Animator 상태를 준비합니다.
/// 스프라이트가 전달되면 생성된 클립의 SpriteRenderer.Sprite 트랙만 채우면 됩니다.
/// </summary>
[InitializeOnLoad]
public static class hys_PlayerSkillAnimationGenerator
{
    private const string SessionKey = "hys.PlayerSkillAnimationGenerator.v2";
    private const float TransitionSeconds = 0.03f;

    private sealed class WeaponSpec
    {
        public string Weapon;
        public string ControllerPath;
        public SkillSpec[] Skills;
    }

    private sealed class SkillSpec
    {
        public string Name;
        public string MotionKey;
        public float Duration;
        public EffectEventSpec[] Effects;
    }

    private sealed class EffectEventSpec
    {
        public float Time;
        public string Command;
    }

    private static readonly WeaponSpec[] WeaponSpecs =
    {
        CreateWeapon(
            "Sword",
            "Assets/05Anims/Player_Anims/hys_Player_Sword.controller",
            Skill("ReapSlash", "PlayerSkill_Sword_ReapSlash", 1.30f,
                Effect(0.15f, "CircularSlash|2.6|1.0|0.1|0|18")),
            Skill("DashSlash", "PlayerSkill_Sword_DashSlash", 1.72f,
                Effect(0.22f, "HorizontalWave|2.8|1.35|0.05|0|20")),
            Skill("ForceSlash", "PlayerSkill_Sword_ForceSlash", 1.33f,
                Effect(0.25f, "HorizontalWave|3.3|1.5|0.1|0|18")),
            Skill("FinalSlash", "PlayerSkill_Sword_FinalSlash", 4.26f,
                Effect(0.35f, "RotatingSlash|2.5|0.7|0.2|0|20"),
                Effect(1.25f, "CircularSlash|3.0|0.8|0.15|0|20"),
                Effect(2.25f, "HorizontalWave|3.5|1.6|0.1|0|20"),
                Effect(3.20f, "CircularSlash|3.7|0.7|0.2|0|22"))),
        CreateWeapon(
            "Axe",
            "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller",
            Skill("SlashAxe", "PlayerSkill_Axe_SlashAxe", 1.58f,
                Effect(0.28f, "CircularSlash|3.0|0.9|0.1|-12|17")),
            Skill("FlameSlash", "PlayerSkill_Axe_FlameSlash", 2.60f,
                Effect(0.42f, "RotatingSlash|3.2|0.8|0.2|-15|18")),
            Skill("Whirlwind", "PlayerSkill_Axe_Whirlwind", 2.63f,
                Effect(0.15f, "CircularSlash|3.0|0.2|0.15|0|20"),
                Effect(0.95f, "CircularSlash|3.2|0.2|0.15|120|20"),
                Effect(1.75f, "CircularSlash|3.4|0.2|0.15|240|20")),
            Skill("EarthBreaker", "PlayerSkill_Axe_EarthBreaker", 4.16f,
                Effect(1.10f, "VerticalStrike|3.5|0.75|0.2|0|17"),
                Effect(2.60f, "VerticalStrike|4.0|0.9|0.2|0|19"))),
        CreateWeapon(
            "Bow",
            "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller",
            Skill("EvasionTriple", "PlayerSkill_Bow_EvasionTriple", 1.80f,
                Effect(0.18f, "RotatingSlash|1.8|0.4|0.2|0|20"),
                Effect(0.62f, "HorizontalWave|1.8|1.0|0.15|0|22"),
                Effect(1.04f, "HorizontalWave|1.8|1.0|0.15|0|22")),
            Skill("PentaStrike", "PlayerSkill_Bow_PentaStrike", 3.46f,
                Effect(0.30f, "HorizontalWave|1.7|1.2|0.25|0|22"),
                Effect(0.78f, "HorizontalWave|1.7|1.2|0.15|-8|22"),
                Effect(1.26f, "HorizontalWave|1.7|1.2|0.05|6|22"),
                Effect(1.74f, "HorizontalWave|1.7|1.2|0.18|-5|22"),
                Effect(2.22f, "HorizontalWave|2.0|1.3|0.12|0|22")),
            Skill("GrandPierce", "PlayerSkill_Bow_GrandPierce", 2.96f,
                Effect(0.68f, "HorizontalWave|4.3|1.8|0.15|0|20")),
            Skill("ArrowsRain", "PlayerSkill_Bow_ArrowsRain", 1.90f,
                Effect(0.22f, "VerticalStrike|2.0|0.5|0.8|180|22"),
                Effect(0.58f, "VerticalStrike|2.2|1.0|0.8|180|22"),
                Effect(0.94f, "VerticalStrike|2.4|1.5|0.8|180|22"))),
        CreateWeapon(
            "Lance",
            "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller",
            Skill("PiercingDrive", "PlayerSkill_Lance_PiercingDrive", 0.50f,
                Effect(0.04f, "HorizontalWave|2.3|1.4|0.12|0|28")),
            Skill("RapidStinger", "PlayerSkill_Lance_RapidStinger", 2.06f,
                Effect(0.12f, "HorizontalWave|1.5|1.1|0.2|0|26"),
                Effect(0.48f, "HorizontalWave|1.5|1.1|0.1|-5|26"),
                Effect(0.84f, "HorizontalWave|1.5|1.1|0.25|5|26"),
                Effect(1.20f, "HorizontalWave|1.5|1.1|0.12|-3|26"),
                Effect(1.56f, "HorizontalWave|1.8|1.2|0.18|0|26")),
            Skill("RisingSpear", "PlayerSkill_Lance_RisingSpear", 2.44f,
                Effect(0.58f, "VerticalStrike|3.2|0.65|0.5|180|19")),
            Skill("BurstLance", "PlayerSkill_Lance_BurstLance", 4.02f,
                Effect(0.62f, "RotatingSlash|2.4|0.5|0.2|0|20"),
                Effect(1.58f, "HorizontalWave|3.4|1.6|0.15|0|22"),
                Effect(2.62f, "CircularSlash|3.5|0.8|0.2|0|22"))),
        CreateWeapon(
            "Shield",
            "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller",
            Skill("ShieldSlam", "PlayerSkill_Shield_ShieldSlam", 1.32f,
                Effect(0.32f, "CircularSlash|2.4|0.8|0.1|0|18")),
            Skill("GroundStrike", "PlayerSkill_Shield_GroundStrike", 1.48f,
                Effect(0.42f, "VerticalStrike|3.0|0.65|0.1|0|18")),
            Skill("DarkBarrier", "PlayerSkill_Shield_DarkBarrier", 0.58f,
                Effect(0.04f, "CircularSlash|3.4|0.0|0.2|0|28")),
            Skill("GroundQuake", "PlayerSkill_Shield_GroundQuake", 2.68f,
                Effect(0.48f, "VerticalStrike|3.2|0.55|0.1|0|19"),
                Effect(1.28f, "VerticalStrike|3.6|0.85|0.1|0|20"),
                Effect(1.98f, "CircularSlash|3.8|0.25|0.15|0|22")))
    };

    static hys_PlayerSkillAnimationGenerator()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += GenerateOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/플레이어 스킬 애니메이션 골격 생성")]
    public static void Generate()
    {
        int createdClips = 0;
        int updatedControllers = 0;

        ConfigureEffectTextures();

        foreach (WeaponSpec weapon in WeaponSpecs)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(weapon.ControllerPath);

            if (controller == null)
            {
                Debug.LogError($"[hys Skill Animation] Animator를 찾을 수 없습니다: {weapon.ControllerPath}");
                continue;
            }

            string skillFolder = $"Assets/05Anims/hys_Player_Anims/{weapon.Weapon}/Skills";
            EnsureFolder(skillFolder);

            bool controllerChanged = false;

            foreach (SkillSpec skill in weapon.Skills)
            {
                string clipPath = $"{skillFolder}/hys_Player_{weapon.Weapon}_{skill.Name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                if (clip == null)
                {
                    clip = CreatePlaceholderClip(weapon.Weapon, skill);
                    AssetDatabase.CreateAsset(clip, clipPath);
                    createdClips++;
                }

                EnsureSkillEvents(clip, skill);

                controllerChanged |= EnsureSkillState(controller, skill.MotionKey, clip);
            }

            if (controllerChanged)
            {
                EditorUtility.SetDirty(controller);
                updatedControllers++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[hys Skill Animation] 생성 완료 - 새 클립 {createdClips}개, 갱신 Animator {updatedControllers}개");
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

    private static AnimationClip CreatePlaceholderClip(string weapon, SkillSpec skill)
    {
        AnimationClip clip = new AnimationClip
        {
            name = $"hys_Player_{weapon}_{skill.Name}",
            frameRate = 12f,
            wrapMode = WrapMode.Once
        };

        // 빈 클립도 스킬 실행 시간만큼 유지되도록 투명도 1의 안전한 곡선을 넣습니다.
        float duration = Mathf.Max(1f / clip.frameRate, skill.Duration);
        EditorCurveBinding durationBinding = EditorCurveBinding.FloatCurve(
            string.Empty,
            typeof(SpriteRenderer),
            "m_Color.a");
        AnimationCurve durationCurve = AnimationCurve.Constant(0f, duration, 1f);
        AnimationUtility.SetEditorCurve(clip, durationBinding, durationCurve);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static void EnsureSkillEvents(AnimationClip clip, SkillSpec skill)
    {
        EffectEventSpec[] effects = skill.Effects ?? Array.Empty<EffectEventSpec>();
        AnimationEvent[] events = new AnimationEvent[effects.Length];
        for (int i = 0; i < effects.Length; i++)
        {
            events[i] = new AnimationEvent
            {
                time = Mathf.Clamp(effects[i].Time, 0f, Mathf.Max(0f, skill.Duration - 0.01f)),
                functionName = "hys_PlaySkillEffect",
                stringParameter = effects[i].Command
            };
        }

        // 캐릭터 스프라이트 트랙은 유지하고 이펙트 호출 이벤트만 갱신합니다.
        AnimationUtility.SetAnimationEvents(clip, events);
        EditorUtility.SetDirty(clip);
    }

    private static void ConfigureEffectTextures()
    {
        string[] texturePaths =
        {
            "Assets/Resources/hys/PlayerSkillEffects/hys_SkillEffect_CircularSlash.png",
            "Assets/Resources/hys/PlayerSkillEffects/hys_SkillEffect_VerticalStrike.png",
            "Assets/Resources/hys/PlayerSkillEffects/hys_SkillEffect_HorizontalWave.png",
            "Assets/Resources/hys/PlayerSkillEffects/hys_SkillEffect_RotatingSlash.png"
        };

        AssetDatabase.Refresh();
        foreach (string texturePath in texturePaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[hys Skill Animation] 이펙트 텍스처를 찾지 못했습니다: {texturePath}");
                continue;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static bool EnsureSkillState(
        AnimatorController controller,
        string motionKey,
        AnimationClip clip)
    {
        bool changed = false;

        if (!HasParameter(controller, motionKey))
        {
            controller.AddParameter(motionKey, AnimatorControllerParameterType.Trigger);
            changed = true;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState skillState = FindState(stateMachine, motionKey);

        if (skillState == null)
        {
            skillState = stateMachine.AddState(motionKey);
            changed = true;
        }

        if (skillState.motion == null)
        {
            skillState.motion = clip;
            changed = true;
        }

        if (!HasAnyStateTransition(stateMachine, skillState, motionKey))
        {
            AnimatorStateTransition enterTransition = stateMachine.AddAnyStateTransition(skillState);
            enterTransition.hasExitTime = false;
            enterTransition.hasFixedDuration = true;
            enterTransition.duration = TransitionSeconds;
            enterTransition.canTransitionToSelf = false;
            enterTransition.AddCondition(AnimatorConditionMode.If, 0f, motionKey);
            changed = true;
        }

        AnimatorState defaultState = stateMachine.defaultState;

        if (defaultState != null && defaultState != skillState && !HasExitTransition(skillState, defaultState))
        {
            AnimatorStateTransition exitTransition = skillState.AddTransition(defaultState);
            exitTransition.hasExitTime = true;
            exitTransition.exitTime = 1f;
            exitTransition.hasFixedDuration = true;
            exitTransition.duration = 0.05f;
            changed = true;
        }

        return changed;
    }

    private static bool HasParameter(AnimatorController controller, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state != null && childState.state.name == stateName)
            {
                return childState.state;
            }
        }

        return null;
    }

    private static bool HasAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string parameterName)
    {
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState != destination)
            {
                continue;
            }

            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == parameterName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasExitTransition(AnimatorState source, AnimatorState destination)
    {
        foreach (AnimatorStateTransition transition in source.transitions)
        {
            if (transition.destinationState == destination && transition.hasExitTime)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static WeaponSpec CreateWeapon(string weapon, string controllerPath, params SkillSpec[] skills)
    {
        return new WeaponSpec
        {
            Weapon = weapon,
            ControllerPath = controllerPath,
            Skills = skills
        };
    }

    private static SkillSpec Skill(
        string name,
        string motionKey,
        float duration,
        params EffectEventSpec[] effects)
    {
        return new SkillSpec
        {
            Name = name,
            MotionKey = motionKey,
            Duration = duration,
            Effects = effects
        };
    }

    private static EffectEventSpec Effect(float time, string command)
    {
        return new EffectEventSpec
        {
            Time = time,
            Command = command
        };
    }
}
#endif
