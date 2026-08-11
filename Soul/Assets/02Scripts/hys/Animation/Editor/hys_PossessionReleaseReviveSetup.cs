#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 5종 무기의 사망 클립을 역프레임 부활 클립으로 만들고 플레이어/몬스터 Animator를 같은 규칙으로 연결합니다.
/// </summary>
[InitializeOnLoad]
public static class hys_PossessionReleaseReviveSetup
{
    private const string MenuPath = "Tools/hys/Animation/빙의 해제 부활 애니메이션 5종 설정";
    private const string SessionKey = "hys.PossessionReleaseReviveSetup.v2";

    private sealed class WeaponSpec
    {
        public string Weapon;
        public string PlayerControllerPath;
        public string PlayerRevivePath;
        public string MonsterControllerPath;
        public string MonsterRevivePath;
    }

    private static readonly WeaponSpec[] Weapons =
    {
        Weapon(
            "Sword",
            "Assets/05Anims/Player_Anims/hys_Player_Sword.controller",
            "Assets/05Anims/Player_Anims/hys_Sword_Revive.anim"),
        Weapon(
            "Axe",
            "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller",
            "Assets/05Anims/hys_Player_Anims/Axe/Clips/hys_Player_Axe_Revive.anim"),
        Weapon(
            "Bow",
            "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller",
            "Assets/05Anims/hys_Player_Anims/Bow/Clips/hys_Player_Bow_Revive.anim"),
        Weapon(
            "Lance",
            "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller",
            "Assets/05Anims/hys_Player_Anims/Lance/Clips/hys_Player_Lance_Revive.anim"),
        Weapon(
            "Shield",
            "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller",
            "Assets/05Anims/hys_Player_Anims/Shield/Clips/hys_Player_Shield_Revive.anim")
    };

    static hys_PossessionReleaseReviveSetup()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += ApplyOnce;
        }
    }

    [MenuItem(MenuPath)]
    public static void Apply()
    {
        foreach (WeaponSpec weapon in Weapons)
        {
            ConfigurePlayer(weapon);
            ConfigureMonster(weapon);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAll();
        Debug.Log("[hys Animator] 빙의 해제/정신력 고갈용 5종 Revive 클립과 Animator 설정을 완료했습니다.");
    }

    // Unity 배치 검증에서도 메뉴와 완전히 같은 생성 경로를 사용합니다.
    public static void ApplyFromCommandLine()
    {
        Apply();
    }

    private static void ApplyOnce()
    {
        EditorApplication.delayCall -= ApplyOnce;
        try
        {
            Apply();
            SessionState.SetBool(SessionKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigurePlayer(WeaponSpec weapon)
    {
        AnimatorController controller = LoadController(weapon.PlayerControllerPath, weapon.Weapon, "플레이어");
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState death = RequireState(root, $"hys_{weapon.Weapon}_Die", weapon.Weapon, "플레이어 Die");
        AnimationClip deathClip = RequireClip(death.motion, weapon.Weapon, "플레이어 Die");
        AnimationClip reviveClip = CreateOrUpdateReversedClip(deathClip, weapon.PlayerRevivePath);
        AnimatorState revive = EnsureState(root, $"hys_{weapon.Weapon}_Revive", reviveClip, new Vector3(1160f, 300f));
        AnimatorState idle = RequireState(root, $"hys_{weapon.Weapon}_Idle", weapon.Weapon, "플레이어 Idle");

        EnsureReleaseParameters(controller);
        ConfigureReleaseTransitions(root, revive, idle);
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureMonster(WeaponSpec weapon)
    {
        AnimatorController controller = LoadController(weapon.MonsterControllerPath, weapon.Weapon, "몬스터");
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState death = RequireState(root, $"hys_Monster_{weapon.Weapon}_Death", weapon.Weapon, "몬스터 Death");
        AnimationClip deathClip = RequireClip(death.motion, weapon.Weapon, "몬스터 Death");
        AnimationClip reviveClip = CreateOrUpdateReversedClip(deathClip, weapon.MonsterRevivePath);
        AnimatorState revive = EnsureState(root, $"hys_Monster_{weapon.Weapon}_Revive", reviveClip, new Vector3(680f, 420f));
        AnimatorState idle = RequireState(root, $"hys_Monster_{weapon.Weapon}_Idle", weapon.Weapon, "몬스터 Idle");

        EnsureReleaseParameters(controller);
        ConfigureReleaseTransitions(root, revive, idle);
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip CreateOrUpdateReversedClip(AnimationClip source, string targetPath)
    {
        AnimationClip generated = new AnimationClip
        {
            name = Path.GetFileNameWithoutExtension(targetPath),
            frameRate = source.frameRate,
            legacy = source.legacy,
            wrapMode = WrapMode.Once
        };
        // 원본 clip.length의 마지막 프레임 유지 구간은 제외해 Revive 첫 프레임이 0초부터 바로 보이게 합니다.
        float duration = ResolveLastKeyTime(source);

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
            if (sourceCurve == null)
            {
                continue;
            }

            Keyframe[] reversedKeys = new Keyframe[sourceCurve.length];
            for (int i = 0; i < sourceCurve.length; i++)
            {
                Keyframe sourceKey = sourceCurve.keys[sourceCurve.length - 1 - i];
                Keyframe reversed = new Keyframe(
                    duration - sourceKey.time,
                    sourceKey.value,
                    -sourceKey.outTangent,
                    -sourceKey.inTangent,
                    sourceKey.outWeight,
                    sourceKey.inWeight)
                {
                    weightedMode = SwapWeightedMode(sourceKey.weightedMode)
                };
                reversedKeys[i] = reversed;
            }
            AnimationUtility.SetEditorCurve(generated, binding, new AnimationCurve(reversedKeys));
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            ObjectReferenceKeyframe[] sourceKeys = AnimationUtility.GetObjectReferenceCurve(source, binding);
            ObjectReferenceKeyframe[] reversedKeys = new ObjectReferenceKeyframe[sourceKeys.Length];
            for (int i = 0; i < sourceKeys.Length; i++)
            {
                ObjectReferenceKeyframe sourceKey = sourceKeys[sourceKeys.Length - 1 - i];
                reversedKeys[i] = new ObjectReferenceKeyframe
                {
                    time = Mathf.Max(0f, duration - sourceKey.time),
                    value = sourceKey.value
                };
            }
            AnimationUtility.SetObjectReferenceCurve(generated, binding, reversedKeys);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(generated, settings);
        AnimationUtility.SetAnimationEvents(generated, Array.Empty<AnimationEvent>());

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath);
        if (existing == null)
        {
            EnsureAssetFolder(targetPath);
            AssetDatabase.CreateAsset(generated, targetPath);
            return generated;
        }

        EditorUtility.CopySerialized(generated, existing);
        existing.name = Path.GetFileNameWithoutExtension(targetPath);
        EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(generated);
        return existing;
    }

    private static WeightedMode SwapWeightedMode(WeightedMode mode)
    {
        switch (mode)
        {
            case WeightedMode.In:
                return WeightedMode.Out;
            case WeightedMode.Out:
                return WeightedMode.In;
            default:
                return mode;
        }
    }

    private static float ResolveLastKeyTime(AnimationClip source)
    {
        float lastKeyTime = 0f;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
            if (curve != null && curve.length > 0)
            {
                lastKeyTime = Mathf.Max(lastKeyTime, curve.keys[curve.length - 1].time);
            }
        }
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (keys.Length > 0)
            {
                lastKeyTime = Mathf.Max(lastKeyTime, keys[keys.Length - 1].time);
            }
        }
        return Mathf.Max(lastKeyTime, 1f / Mathf.Max(1f, source.frameRate));
    }

    private static void EnsureReleaseParameters(AnimatorController controller)
    {
        EnsureParameter(controller, "MentalNormalized", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MentalDepleted", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "PossessionReleased", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsReviving", AnimatorControllerParameterType.Bool);
    }

    private static void ConfigureReleaseTransitions(
        AnimatorStateMachine root,
        AnimatorState revive,
        AnimatorState idle)
    {
        RemoveAnyStateTransition(root, "hys_MentalDepletedToRevive");
        AnimatorStateTransition mental = root.AddAnyStateTransition(revive);
        mental.name = "hys_MentalDepletedToRevive";
        ConfigureImmediateTransition(mental);
        mental.AddCondition(AnimatorConditionMode.If, 0f, "MentalDepleted");

        RemoveAnyStateTransition(root, "hys_PossessionReleasedToRevive");
        AnimatorStateTransition released = root.AddAnyStateTransition(revive);
        released.name = "hys_PossessionReleasedToRevive";
        ConfigureImmediateTransition(released);
        released.AddCondition(AnimatorConditionMode.If, 0f, "PossessionReleased");

        RemoveStateTransition(revive, "hys_ReviveToIdle");
        AnimatorStateTransition toIdle = revive.AddTransition(idle);
        toIdle.name = "hys_ReviveToIdle";
        toIdle.hasExitTime = true;
        toIdle.exitTime = 0.98f;
        toIdle.duration = 0.03f;
        toIdle.hasFixedDuration = true;
        toIdle.canTransitionToSelf = false;
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static void ValidateAll()
    {
        var errors = new List<string>();
        foreach (WeaponSpec weapon in Weapons)
        {
            ValidateController(weapon.PlayerControllerPath, $"hys_{weapon.Weapon}_Revive", weapon.PlayerRevivePath, weapon.Weapon, errors);
            ValidateController(weapon.MonsterControllerPath, $"hys_Monster_{weapon.Weapon}_Revive", weapon.MonsterRevivePath, weapon.Weapon, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("hys Revive 검증 실패:\n" + string.Join("\n", errors));
        }
    }

    private static void ValidateController(
        string controllerPath,
        string stateName,
        string expectedClipPath,
        string weapon,
        ICollection<string> errors)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        AnimatorState state = controller != null ? FindState(controller.layers[0].stateMachine, stateName) : null;
        string actualPath = state != null && state.motion != null ? AssetDatabase.GetAssetPath(state.motion) : string.Empty;
        if (!string.Equals(actualPath, expectedClipPath, StringComparison.Ordinal))
        {
            errors.Add($"{weapon}: {stateName}의 클립이 다릅니다. Expected={expectedClipPath}, Actual={actualPath}");
        }
        if (state == null || state.motion == null || state.motion.name.IndexOf(weapon, StringComparison.OrdinalIgnoreCase) < 0)
        {
            errors.Add($"{weapon}: 다른 무기 Revive 모션이 연결되었거나 모션이 비어 있습니다.");
        }

        if (controller == null || state == null)
        {
            return;
        }

        ValidateParameter(controller, "MentalNormalized", AnimatorControllerParameterType.Float, weapon, errors);
        ValidateParameter(controller, "MentalDepleted", AnimatorControllerParameterType.Trigger, weapon, errors);
        ValidateParameter(controller, "PossessionReleased", AnimatorControllerParameterType.Trigger, weapon, errors);
        ValidateParameter(controller, "IsReviving", AnimatorControllerParameterType.Bool, weapon, errors);

        string deathStateName = stateName.StartsWith("hys_Monster_", StringComparison.Ordinal)
            ? stateName.Replace("_Revive", "_Death")
            : stateName.Replace("_Revive", "_Die");
        AnimatorState deathState = FindState(controller.layers[0].stateMachine, deathStateName);
        ValidateReversedSpriteFrames(
            deathState != null ? deathState.motion as AnimationClip : null,
            state.motion as AnimationClip,
            weapon,
            errors);
    }

    private static void ValidateParameter(
        AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType expectedType,
        string weapon,
        ICollection<string> errors)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName && parameter.type == expectedType)
            {
                return;
            }
        }
        errors.Add($"{weapon}: Animator 파라미터가 없거나 타입이 다릅니다: {parameterName} ({expectedType})");
    }

    private static void ValidateReversedSpriteFrames(
        AnimationClip deathClip,
        AnimationClip reviveClip,
        string weapon,
        ICollection<string> errors)
    {
        if (deathClip == null || reviveClip == null)
        {
            errors.Add($"{weapon}: Die/Revive 클립 비교 대상이 비어 있습니다.");
            return;
        }

        EditorCurveBinding[] deathBindings = AnimationUtility.GetObjectReferenceCurveBindings(deathClip);
        EditorCurveBinding[] reviveBindings = AnimationUtility.GetObjectReferenceCurveBindings(reviveClip);
        if (deathBindings.Length == 0 || reviveBindings.Length == 0)
        {
            errors.Add($"{weapon}: Die 또는 Revive 클립에 스프라이트 프레임 커브가 없습니다.");
            return;
        }

        foreach (EditorCurveBinding deathBinding in deathBindings)
        {
            EditorCurveBinding? reviveBinding = FindMatchingBinding(reviveBindings, deathBinding);
            if (!reviveBinding.HasValue)
            {
                errors.Add($"{weapon}: Revive 클립에 대응 커브가 없습니다: {deathBinding.path}/{deathBinding.propertyName}");
                continue;
            }

            ObjectReferenceKeyframe[] deathKeys = AnimationUtility.GetObjectReferenceCurve(deathClip, deathBinding);
            ObjectReferenceKeyframe[] reviveKeys = AnimationUtility.GetObjectReferenceCurve(reviveClip, reviveBinding.Value);
            bool reversed = deathKeys.Length == reviveKeys.Length
                && deathKeys.Length > 0
                && deathKeys[0].value == reviveKeys[reviveKeys.Length - 1].value
                && deathKeys[deathKeys.Length - 1].value == reviveKeys[0].value;
            if (!reversed)
            {
                errors.Add($"{weapon}: Revive 스프라이트 프레임이 Die의 역순이 아닙니다: {deathBinding.path}");
            }
        }
    }

    private static EditorCurveBinding? FindMatchingBinding(
        IEnumerable<EditorCurveBinding> bindings,
        EditorCurveBinding expected)
    {
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.path == expected.path
                && binding.propertyName == expected.propertyName
                && binding.type == expected.type)
            {
                return binding;
            }
        }
        return null;
    }

    private static AnimatorController LoadController(string path, string weapon, string label)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            throw new InvalidOperationException($"{weapon} {label} Controller를 찾지 못했습니다: {path}");
        }
        return controller;
    }

    private static AnimationClip RequireClip(Motion motion, string weapon, string label)
    {
        AnimationClip clip = motion as AnimationClip;
        if (clip == null || clip.name.IndexOf(weapon, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException($"{weapon} {label}에 자기 무기 AnimationClip이 연결되어 있지 않습니다.");
        }
        return clip;
    }

    private static AnimatorState RequireState(
        AnimatorStateMachine root,
        string name,
        string weapon,
        string label)
    {
        AnimatorState state = FindState(root, name);
        if (state == null)
        {
            throw new InvalidOperationException($"{weapon} {label} 상태를 찾지 못했습니다: {name}");
        }
        return state;
    }

    private static AnimatorState EnsureState(
        AnimatorStateMachine root,
        string name,
        Motion motion,
        Vector3 position)
    {
        AnimatorState state = FindState(root, name) ?? root.AddState(name, position);
        state.motion = motion;
        state.speed = 1f;
        state.writeDefaultValues = true;
        state.tag = "hys_Revive";
        return state;
    }

    private static AnimatorState FindState(AnimatorStateMachine root, string name)
    {
        foreach (ChildAnimatorState child in root.states)
        {
            if (child.state != null && child.state.name == name)
            {
                return child.state;
            }
        }
        return null;
    }

    private static void EnsureParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name && parameter.type == type)
            {
                return;
            }
            if (parameter.name == name)
            {
                controller.RemoveParameter(parameter);
                break;
            }
        }
        controller.AddParameter(name, type);
    }

    private static void RemoveAnyStateTransition(AnimatorStateMachine root, string name)
    {
        foreach (AnimatorStateTransition transition in root.anyStateTransitions)
        {
            if (transition != null && transition.name == name)
            {
                root.RemoveAnyStateTransition(transition);
            }
        }
    }

    private static void RemoveStateTransition(AnimatorState state, string name)
    {
        foreach (AnimatorStateTransition transition in state.transitions)
        {
            if (transition != null && transition.name == name)
            {
                state.RemoveTransition(transition);
            }
        }
    }

    private static void EnsureAssetFolder(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }
        throw new DirectoryNotFoundException($"Revive 클립 폴더가 없습니다: {folder}");
    }

    private static WeaponSpec Weapon(
        string weapon,
        string playerControllerPath,
        string playerRevivePath)
    {
        string monsterRoot = $"Assets/05Anims/hys_Enemy_Anims/{weapon}";
        return new WeaponSpec
        {
            Weapon = weapon,
            PlayerControllerPath = playerControllerPath,
            PlayerRevivePath = playerRevivePath,
            MonsterControllerPath = $"{monsterRoot}/hys_Monster_{weapon}.controller",
            MonsterRevivePath = $"{monsterRoot}/Clips/hys_Enemy_{weapon}_Revive.anim"
        };
    }
}
#endif
