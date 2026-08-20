#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// HWJ 플레이어 스킬의 실제 지속 시간에 맞춰 무기별 도트 모션을 조합하고
/// 외부 이펙트 이벤트가 없는 전용 AnimationClip과 Animator 상태를 구성합니다.
/// </summary>
[InitializeOnLoad]
public static class hys_PlayerSkillAnimationGenerator
{
    private const string SessionKey = "hys.PlayerSkillAnimationGenerator.v3";
    private const float FrameRate = 12f;
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
        public string[] MotionSequence;
    }

    private sealed class SpriteFrame
    {
        public int Index;
        public string Path;
        public Sprite Sprite;
    }

    private static readonly WeaponSpec[] WeaponSpecs =
    {
        CreateWeapon(
            "Sword",
            "Assets/05Anims/Player_Anims/hys_Player_Sword.controller",
            Skill("ReapSlash", "PlayerSkill_Sword_ReapSlash", 1.30f,
                "ForwardAttack1:0,1,5-7"),
            Skill("DashSlash", "PlayerSkill_Sword_DashSlash", 1.72f,
                "Run", "ForwardAttack1:0,1,5-7"),
            Skill("ForceSlash", "PlayerSkill_Sword_ForceSlash", 1.33f,
                "ForwardAttack2:0,1,4-7"),
            Skill("FinalSlash", "PlayerSkill_Sword_FinalSlash", 4.26f,
                "ForwardAttack1:0,1,5-7", "Run", "ForwardAttack2:0,1,4-7", "ForwardAttack1:0,1,5-7")),
        CreateWeapon(
            "Axe",
            "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller",
            Skill("SlashAxe", "PlayerSkill_Axe_SlashAxe", 1.58f,
                "Attack1:0-3"),
            Skill("FlameSlash", "PlayerSkill_Axe_FlameSlash", 2.60f,
                "Attack2:0-3", "Attack1:0-3"),
            Skill("Whirlwind", "PlayerSkill_Axe_Whirlwind", 2.63f,
                "Attack1:0-3", "Attack2:0-3", "Attack1:0-3"),
            Skill("EarthBreaker", "PlayerSkill_Axe_EarthBreaker", 4.16f,
                "JumpStart", "JumpApex", "JumpFall", "PlungeLand:3")),
        CreateWeapon(
            "Bow",
            "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller",
            Skill("EvasionTriple", "PlayerSkill_Bow_EvasionTriple", 1.80f,
                "Bow_Dash", "Bow_Attack1", "Bow_Attack1", "Bow_Attack1"),
            Skill("PentaStrike", "PlayerSkill_Bow_PentaStrike", 3.46f,
                "Bow_Attack1", "Bow_Attack2:0-2,5", "Bow_Attack1", "Bow_Attack2:0-2,5", "Bow_Attack1"),
            Skill("GrandPierce", "PlayerSkill_Bow_GrandPierce", 2.96f,
                "Bow_Attack2:0-2,5", "Bow_Attack2:0-2,5"),
            Skill("ArrowsRain", "PlayerSkill_Bow_ArrowsRain", 1.90f,
                "Bow_JumpStart", "Bow_Attack1", "Bow_JumpFall")),
        CreateWeapon(
            "Lance",
            "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller",
            Skill("PiercingDrive", "PlayerSkill_Lance_PiercingDrive", 0.50f,
                "Dash", "Attack1"),
            Skill("RapidStinger", "PlayerSkill_Lance_RapidStinger", 2.06f,
                "Attack1", "Attack1", "Attack1", "Attack1", "Attack1"),
            Skill("RisingSpear", "PlayerSkill_Lance_RisingSpear", 2.44f,
                "Attack2", "JumpStart", "JumpApex"),
            Skill("BurstLance", "PlayerSkill_Lance_BurstLance", 4.02f,
                "Attack1", "Attack2", "Dash", "Attack1", "Attack2")),
        CreateWeapon(
            "Shield",
            "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller",
            Skill("ShieldSlam", "PlayerSkill_Shield_ShieldSlam", 1.32f,
                "Run", "Attack"),
            Skill("GroundStrike", "PlayerSkill_Shield_GroundStrike", 1.48f,
                "JumpStart", "JumpFall", "PlungeLand:3"),
            Skill("DarkBarrier", "PlayerSkill_Shield_DarkBarrier", 0.58f,
                "Hit:2", "Idle"),
            Skill("GroundQuake", "PlayerSkill_Shield_GroundQuake", 2.68f,
                "JumpStart", "JumpApex", "JumpFall", "PlungeLand:3", "Attack"))
    };

    static hys_PlayerSkillAnimationGenerator()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += GenerateOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/플레이어 스킬 애니메이션 갱신")]
    public static void Generate()
    {
        int createdClips = 0;
        int updatedClips = 0;
        int updatedControllers = 0;

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
                    clip = CreateSkillClip(weapon.Weapon, skill);
                    AssetDatabase.CreateAsset(clip, clipPath);
                    createdClips++;
                }

                if (RebuildSkillFrames(clip, weapon, skill))
                {
                    updatedClips++;
                }

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
        Debug.Log($"[hys Skill Animation] 완료 - 신규 클립 {createdClips}개, 프레임 갱신 {updatedClips}개, Animator 갱신 {updatedControllers}개");
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

    private static AnimationClip CreateSkillClip(string weapon, SkillSpec skill)
    {
        return new AnimationClip
        {
            name = $"hys_Player_{weapon}_{skill.Name}",
            frameRate = FrameRate,
            wrapMode = WrapMode.Once
        };
    }

    private static bool RebuildSkillFrames(AnimationClip clip, WeaponSpec weapon, SkillSpec skill)
    {
        List<Sprite> sprites = new List<Sprite>();

        foreach (string motionSpec in skill.MotionSequence)
        {
            sprites.AddRange(LoadMotionSprites(weapon.Weapon, motionSpec));
        }

        if (sprites.Count == 0)
        {
            Debug.LogError($"[hys Skill Animation] 사용할 프레임이 없습니다: {weapon.Weapon}/{skill.Name}");
            return false;
        }

        clip.ClearCurves();
        clip.frameRate = FrameRate;
        clip.wrapMode = WrapMode.Once;

        float frameDuration = 1f / FrameRate;
        float duration = Mathf.Max(frameDuration, skill.Duration);
        // Unity는 마지막 스프라이트 키 뒤에 한 프레임을 더해 클립 길이를 계산하므로
        // 마지막 키를 종료 시각보다 정확히 한 프레임 앞에 배치합니다.
        float lastFrameTime = Mathf.Max(0f, duration - frameDuration);
        float frameInterval = sprites.Count > 1
            ? lastFrameTime / (sprites.Count - 1)
            : 0f;
        ObjectReferenceKeyframe[] spriteKeys = new ObjectReferenceKeyframe[sprites.Count];

        for (int i = 0; i < sprites.Count; i++)
        {
            spriteKeys[i] = new ObjectReferenceKeyframe
            {
                time = i * frameInterval,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(SpriteRenderer),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys);

        // 마지막 프레임이 스킬 데이터의 종료 시각까지 유지되도록 길이 곡선을 함께 둡니다.
        EditorCurveBinding durationBinding = EditorCurveBinding.FloatCurve(
            string.Empty,
            typeof(SpriteRenderer),
            "m_Color.a");
        AnimationUtility.SetEditorCurve(
            clip,
            durationBinding,
            AnimationCurve.Constant(0f, duration, 1f));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 스프라이트에 포함되지 않은 별도 이펙트 호출은 생성하지 않습니다.
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(clip);
        return true;
    }

    private static List<Sprite> LoadMotionSprites(string weapon, string motionSpec)
    {
        string[] motionParts = motionSpec.Split(':');
        string motionName = motionParts[0];
        HashSet<int> selectedFrames = motionParts.Length > 1
            ? ParseFrameSelection(motionParts[1])
            : null;
        string spriteFolder = $"Assets/05Anims/hys_Player_Anims/{weapon}/Sprites";
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
        List<SpriteFrame> frames = new List<SpriteFrame>();
        string marker = $"_{motionName}_";

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            int markerIndex = fileName.LastIndexOf(marker, StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                continue;
            }

            string suffix = fileName.Substring(markerIndex + marker.Length);

            if (!int.TryParse(suffix, out int frameIndex))
            {
                continue;
            }

            if (selectedFrames != null && !selectedFrames.Contains(frameIndex))
            {
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                frames.Add(new SpriteFrame
                {
                    Index = frameIndex,
                    Path = path,
                    Sprite = sprite
                });
            }
        }

        frames.Sort((left, right) =>
        {
            int indexCompare = left.Index.CompareTo(right.Index);
            return indexCompare != 0
                ? indexCompare
                : string.CompareOrdinal(left.Path, right.Path);
        });

        List<Sprite> result = new List<Sprite>(frames.Count);

        foreach (SpriteFrame frame in frames)
        {
            result.Add(frame.Sprite);
        }

        return result;
    }

    private static HashSet<int> ParseFrameSelection(string selection)
    {
        HashSet<int> result = new HashSet<int>();
        string[] segments = selection.Split(',');

        foreach (string segment in segments)
        {
            string[] range = segment.Split('-');

            if (!int.TryParse(range[0], out int start))
            {
                continue;
            }

            int end = start;

            if (range.Length > 1 && !int.TryParse(range[1], out end))
            {
                end = start;
            }

            for (int frame = Mathf.Min(start, end); frame <= Mathf.Max(start, end); frame++)
            {
                result.Add(frame);
            }
        }

        return result;
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

        if (skillState.motion != clip)
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
        params string[] motionSequence)
    {
        return new SkillSpec
        {
            Name = name,
            MotionKey = motionKey,
            Duration = duration,
            MotionSequence = motionSequence
        };
    }
}
#endif
