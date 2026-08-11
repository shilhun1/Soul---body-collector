#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 기존 hys 플레이어 Animator 구조를 유지하면서 HWJ가 사용할 전체 상태 경로와 Entry 허브를 설정합니다.
/// 비-hys 프리팹과 씬은 수정하지 않고 다섯 무기 Controller와 hys MotionProfile만 갱신합니다.
/// </summary>
[InitializeOnLoad]
public static class hys_HWJPlayerAnimatorControllerSetup
{
    private const string SessionKey = "hys.HWJPlayerAnimatorControllerSetup.FlatRuntimeStates.v2";
    private const string ProfileRoot =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/hys_Animation_RuntimeReady/Profiles";

    private sealed class WeaponSpec
    {
        public string Weapon;
        public string ControllerPath;
    }

    private readonly struct ConditionSpec
    {
        public readonly AnimatorConditionMode Mode;
        public readonly float Threshold;
        public readonly string Parameter;

        public ConditionSpec(AnimatorConditionMode mode, float threshold, string parameter)
        {
            Mode = mode;
            Threshold = threshold;
            Parameter = parameter;
        }
    }

    private static readonly WeaponSpec[] Weapons =
    {
        Weapon("Sword", "Assets/05Anims/Player_Anims/hys_Player_Sword.controller"),
        Weapon("Axe", "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller"),
        Weapon("Bow", "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller"),
        Weapon("Lance", "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller"),
        Weapon("Shield", "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller")
    };

    private static readonly KeyValuePair<string, AnimatorControllerParameterType>[] RequiredParameters =
    {
        Parameter("Speed", AnimatorControllerParameterType.Float),
        Parameter("HorizontalSpeed", AnimatorControllerParameterType.Float),
        Parameter("VerticalSpeed", AnimatorControllerParameterType.Float),
        Parameter("IsMoving", AnimatorControllerParameterType.Bool),
        Parameter("IsGrounded", AnimatorControllerParameterType.Bool),
        Parameter("IsPossessed", AnimatorControllerParameterType.Bool),
        Parameter("IsSoul", AnimatorControllerParameterType.Bool),
        Parameter("IsDead", AnimatorControllerParameterType.Bool),
        Parameter("RuntimeState", AnimatorControllerParameterType.Int),
        Parameter("WeaponType", AnimatorControllerParameterType.Int),
        Parameter("AttackTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("JumpTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("DoubleJumpTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("DropJumpTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("DashTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("HitTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("DeadTrigger", AnimatorControllerParameterType.Trigger),
        Parameter("MentalNormalized", AnimatorControllerParameterType.Float),
        Parameter("MentalDepleted", AnimatorControllerParameterType.Trigger),
        Parameter("PossessionReleased", AnimatorControllerParameterType.Trigger),
        Parameter("IsReviving", AnimatorControllerParameterType.Bool)
    };

    static hys_HWJPlayerAnimatorControllerSetup()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += ApplyOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/HWJ 공용 플레이어 Animator 설정")]
    public static void Apply()
    {
        foreach (WeaponSpec weapon in Weapons)
        {
            ConfigureController(weapon);
            ConfigureProfile(weapon);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hys Animator] HWJ 공용 플레이어 Animator 5종과 MotionProfile 설정을 완료했습니다.");
    }

    // Unity 배치 검증에서 메뉴 동작과 동일한 진입점을 사용합니다.
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

    private static void ConfigureController(WeaponSpec weapon)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(weapon.ControllerPath);
        if (controller == null)
        {
            throw new InvalidOperationException($"hys Controller를 찾지 못했습니다: {weapon.ControllerPath}");
        }

        EnsureParameters(controller);
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(root, $"hys_{weapon.Weapon}_Idle");
        AnimatorState run = FindState(root, $"hys_{weapon.Weapon}_Run");
        AnimatorStateMachine attack = FindStateMachine(root, "Attack");
        AnimatorStateMachine jump = FindStateMachine(root, "Jump");

        RequireState(idle, weapon, "Idle");
        RequireState(run, weapon, "Run");
        RequireStateMachine(attack, weapon, "Attack");
        RequireStateMachine(jump, weapon, "Jump");
        AnimatorState attack1Source = FindState(attack, $"hys_{weapon.Weapon}_Attack1");
        AnimatorState attack2Source = FindState(attack, $"hys_{weapon.Weapon}_Attack2");
        AnimatorState jumpStartSource = FindState(jump, $"hys_{weapon.Weapon}_Jump_Start");
        AnimatorState jumpApexSource = FindState(jump, $"hys_{weapon.Weapon}_Jump_Apex");
        AnimatorState jumpFallSource = FindState(jump, $"hys_{weapon.Weapon}_Jump_Fall");
        RequireState(attack1Source, weapon, "Attack1");
        RequireState(attack2Source, weapon, "Attack2");
        RequireState(jumpStartSource, weapon, "Jump_Start");
        RequireState(jumpApexSource, weapon, "Jump_Apex");
        RequireState(jumpFallSource, weapon, "Jump_Fall");
        RequireState(FindState(root, $"hys_{weapon.Weapon}_Dash"), weapon, "Dash");
        RequireState(FindState(root, $"hys_{weapon.Weapon}_Hit"), weapon, "Hit");
        RequireState(FindState(root, $"hys_{weapon.Weapon}_Die"), weapon, "Die");
        RequireMinimumSkillStates(root, weapon, 3);

        string entryName = $"hys_{weapon.Weapon}_Entry";
        AnimatorState entry = FindState(root, entryName);
        if (entry == null)
        {
            entry = root.AddState(entryName, new Vector3(460f, 300f, 0f));
        }

        // Entry 허브는 Idle과 같은 클립을 사용해 한 프레임의 시각적 튐도 만들지 않습니다.
        entry.motion = idle.motion;
        entry.writeDefaultValues = idle.writeDefaultValues;
        root.defaultState = entry;
        EnsureEntryToIdleTransition(entry, idle);

        // HWJ CrossFade는 하위 StateMachine 경로를 찾지 못하는 경우가 있어 공용 런타임 상태를 최상위에 둡니다.
        AnimatorState attack1 = EnsureTopLevelRuntimeState(root, attack1Source, new Vector3(640f, 40f, 0f));
        AnimatorState attack2 = EnsureTopLevelRuntimeState(root, attack2Source, new Vector3(820f, 40f, 0f));
        AnimatorState jumpStart = EnsureTopLevelRuntimeState(root, jumpStartSource, new Vector3(640f, 500f, 0f));
        AnimatorState jumpApex = EnsureTopLevelRuntimeState(root, jumpApexSource, new Vector3(820f, 500f, 0f));
        AnimatorState jumpFall = EnsureTopLevelRuntimeState(root, jumpFallSource, new Vector3(1000f, 500f, 0f));

        ConfigureRuntimeTransitions(entry, idle, run, attack1, attack2, jumpStart, jumpApex, jumpFall);

        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureProfile(WeaponSpec weapon)
    {
        string path = $"{ProfileRoot}/hys_RuntimeReady_Player_{weapon.Weapon}MotionProfile.asset";
        HWJ_MotionProfileSO profile = AssetDatabase.LoadAssetAtPath<HWJ_MotionProfileSO>(path);
        if (profile == null)
        {
            throw new InvalidOperationException($"hys MotionProfile을 찾지 못했습니다: {path}");
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(weapon.ControllerPath);
        SerializedObject serialized = new SerializedObject(profile);
        serialized.FindProperty("animatorController").objectReferenceValue = controller;
        SerializedProperty motions = serialized.FindProperty("motions");
        for (int i = 0; i < motions.arraySize; i++)
        {
            SerializedProperty motion = motions.GetArrayElementAtIndex(i);
            SerializedProperty stateProperty = motion.FindPropertyRelative("animatorStateName");
            if (string.IsNullOrEmpty(stateProperty.stringValue))
            {
                continue;
            }

            string shortName = stateProperty.stringValue;
            int lastSeparator = shortName.LastIndexOf('.');
            if (lastSeparator >= 0)
            {
                shortName = shortName.Substring(lastSeparator + 1);
            }

            // 공격과 점프도 HWJ가 확실히 찾을 수 있는 최상위 런타임 상태를 사용합니다.
            stateProperty.stringValue = $"Base Layer.{shortName}";

            // HWJ가 문자열 경로로 직접 CrossFade하므로 잘못된 경로는 저장 전에 즉시 차단합니다.
            if (!StatePathExists(controller.layers[0].stateMachine, stateProperty.stringValue))
            {
                throw new InvalidOperationException(
                    $"{weapon.Weapon} MotionProfile의 Animator 상태 경로가 없습니다: {stateProperty.stringValue}");
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    private static void EnsureParameters(AnimatorController controller)
    {
        foreach (KeyValuePair<string, AnimatorControllerParameterType> required in RequiredParameters)
        {
            AnimatorControllerParameter existing = Array.Find(
                controller.parameters,
                parameter => parameter.name == required.Key);
            if (existing != null && existing.type == required.Value)
            {
                continue;
            }

            if (existing != null)
            {
                controller.RemoveParameter(existing);
            }

            controller.AddParameter(required.Key, required.Value);
        }
    }

    private static void EnsureEntryToIdleTransition(AnimatorState entry, AnimatorState idle)
    {
        foreach (AnimatorStateTransition transition in entry.transitions)
        {
            if (transition.destinationState == idle)
            {
                transition.hasExitTime = true;
                transition.exitTime = 0.01f;
                transition.duration = 0f;
                transition.hasFixedDuration = true;
                transition.canTransitionToSelf = false;
                return;
            }
        }

        AnimatorStateTransition toIdle = entry.AddTransition(idle);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 0.01f;
        toIdle.duration = 0f;
        toIdle.hasFixedDuration = true;
        toIdle.canTransitionToSelf = false;
    }

    private static AnimatorState EnsureTopLevelRuntimeState(
        AnimatorStateMachine root,
        AnimatorState source,
        Vector3 position)
    {
        AnimatorState state = FindState(root, source.name);
        if (state == null)
        {
            state = root.AddState(source.name, position);
        }

        state.motion = source.motion;
        state.speed = source.speed;
        state.cycleOffset = source.cycleOffset;
        state.mirror = source.mirror;
        state.iKOnFeet = source.iKOnFeet;
        state.writeDefaultValues = source.writeDefaultValues;
        state.tag = source.tag;
        return state;
    }

    private static void ConfigureRuntimeTransitions(
        AnimatorState entry,
        AnimatorState idle,
        AnimatorState run,
        AnimatorState attack1,
        AnimatorState attack2,
        AnimatorState jumpStart,
        AnimatorState jumpApex,
        AnimatorState jumpFall)
    {
        ReplaceExitTransition(attack1, entry, "hys_HWJ_Attack1Exit");
        ReplaceExitTransition(attack2, entry, "hys_HWJ_Attack2Exit");

        ReplaceConditionTransition(
            jumpStart,
            jumpApex,
            "hys_HWJ_JumpStartToApex",
            Condition(AnimatorConditionMode.Less, 0.35f, "VerticalSpeed"));
        ReplaceConditionTransition(
            jumpApex,
            jumpFall,
            "hys_HWJ_JumpApexToFall",
            Condition(AnimatorConditionMode.Less, -0.35f, "VerticalSpeed"));
        ReplaceConditionTransition(
            jumpFall,
            idle,
            "hys_HWJ_JumpFallToIdle",
            Condition(AnimatorConditionMode.If, 0f, "IsGrounded"),
            Condition(AnimatorConditionMode.IfNot, 0f, "IsMoving"));
        ReplaceConditionTransition(
            jumpFall,
            run,
            "hys_HWJ_JumpFallToRun",
            Condition(AnimatorConditionMode.If, 0f, "IsGrounded"),
            Condition(AnimatorConditionMode.If, 0f, "IsMoving"));
        ReplaceConditionTransition(
            idle,
            run,
            "hys_HWJ_IdleToRun",
            Condition(AnimatorConditionMode.If, 0f, "IsMoving"),
            Condition(AnimatorConditionMode.If, 0f, "IsGrounded"));
        ReplaceConditionTransition(
            run,
            idle,
            "hys_HWJ_RunToIdle",
            Condition(AnimatorConditionMode.IfNot, 0f, "IsMoving"),
            Condition(AnimatorConditionMode.If, 0f, "IsGrounded"));
    }

    private static void ReplaceExitTransition(
        AnimatorState source,
        AnimatorState destination,
        string transitionName)
    {
        RemoveNamedTransition(source, transitionName);
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.name = transitionName;
        transition.hasExitTime = true;
        transition.exitTime = 0.98f;
        transition.hasFixedDuration = true;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
    }

    private static void ReplaceConditionTransition(
        AnimatorState source,
        AnimatorState destination,
        string transitionName,
        params ConditionSpec[] conditions)
    {
        RemoveNamedTransition(source, transitionName);
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.name = transitionName;
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
        foreach (ConditionSpec condition in conditions)
        {
            transition.AddCondition(condition.Mode, condition.Threshold, condition.Parameter);
        }
    }

    private static void RemoveNamedTransition(AnimatorState state, string transitionName)
    {
        foreach (AnimatorStateTransition transition in state.transitions)
        {
            if (transition != null && transition.name == transitionName)
            {
                state.RemoveTransition(transition);
            }
        }
    }

    private static ConditionSpec Condition(
        AnimatorConditionMode mode,
        float threshold,
        string parameter)
    {
        return new ConditionSpec(mode, threshold, parameter);
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string stateName)
    {
        if (machine == null)
        {
            return null;
        }

        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state != null && child.state.name == stateName)
            {
                return child.state;
            }
        }

        return null;
    }

    private static AnimatorStateMachine FindStateMachine(AnimatorStateMachine root, string machineName)
    {
        foreach (ChildAnimatorStateMachine child in root.stateMachines)
        {
            if (child.stateMachine != null && child.stateMachine.name == machineName)
            {
                return child.stateMachine;
            }
        }

        return null;
    }

    private static void RequireState(AnimatorState state, WeaponSpec weapon, string label)
    {
        if (state == null)
        {
            throw new InvalidOperationException($"{weapon.Weapon} Controller에 {label} 상태가 없습니다.");
        }
    }

    private static void RequireStateMachine(AnimatorStateMachine machine, WeaponSpec weapon, string label)
    {
        if (machine == null)
        {
            throw new InvalidOperationException($"{weapon.Weapon} Controller에 {label} StateMachine이 없습니다.");
        }
    }

    private static void RequireMinimumSkillStates(
        AnimatorStateMachine root,
        WeaponSpec weapon,
        int minimumCount)
    {
        int skillCount = 0;
        string prefix = $"PlayerSkill_{weapon.Weapon}_";
        foreach (ChildAnimatorState child in root.states)
        {
            if (child.state != null
                && child.state.name.StartsWith(prefix, StringComparison.Ordinal))
            {
                skillCount++;
            }
        }

        if (skillCount < minimumCount)
        {
            throw new InvalidOperationException(
                $"{weapon.Weapon} Controller의 플레이어 스킬 상태가 {skillCount}개뿐입니다. 최소 {minimumCount}개가 필요합니다.");
        }
    }

    private static bool StatePathExists(AnimatorStateMachine root, string fullPath)
    {
        const string rootPrefix = "Base Layer.";
        if (root == null || string.IsNullOrEmpty(fullPath) || !fullPath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string relativePath = fullPath.Substring(rootPrefix.Length);
        string[] parts = relativePath.Split('.');
        if (parts.Length == 1)
        {
            return FindState(root, parts[0]) != null;
        }

        if (parts.Length == 2)
        {
            AnimatorStateMachine childMachine = FindStateMachine(root, parts[0]);
            return FindState(childMachine, parts[1]) != null;
        }

        return false;
    }

    private static WeaponSpec Weapon(string weapon, string controllerPath)
    {
        return new WeaponSpec { Weapon = weapon, ControllerPath = controllerPath };
    }

    private static KeyValuePair<string, AnimatorControllerParameterType> Parameter(
        string name,
        AnimatorControllerParameterType type)
    {
        return new KeyValuePair<string, AnimatorControllerParameterType>(name, type);
    }
}
#endif
