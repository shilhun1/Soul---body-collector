using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 최종 보스 Animator를 5개 상태와 하나의 Action Blend Tree로 생성합니다.
[InitializeOnLoad]
internal static class hys_FinalBossAnimatorGenerator
{
    private const string FolderPath = "Assets/05Anims/FinalBoss";
    private const string ClipFolder = FolderPath + "/";
    private const string ControllerPath = FolderPath + "/hys_FinalBoss.controller";
    private const string RootDataPath = "Assets/02Scripts/hys/Boss/Data/hys_FinalBoss_RootObjectData.asset";

    private static readonly (string clipName, int actionId, bool loop)[] ActionClips =
    {
        ("hys_FinalBoss_BlackOrbCast.anim", 1, false),
        ("hys_FinalBoss_SpearAim.anim", 2, true),
        ("hys_FinalBoss_SpearRelease.anim", 3, false),
        ("hys_FinalBoss_ShieldCast.anim", 4, false),
        ("hys_FinalBoss_PortalCast.anim", 5, false),
        ("hys_FinalBoss_UltimateCharge.anim", 6, true),
        ("hys_FinalBoss_UltimateRelease.anim", 7, false),
        ("hys_FinalBoss_WeaponSummon.anim", 8, false),
        ("hys_FinalBoss_WeaponAim.anim", 9, true),
        ("hys_FinalBoss_WeaponThrow.anim", 10, false),
        ("hys_FinalBoss_PhaseTwoShieldCast.anim", 11, false),
        ("hys_FinalBoss_FlameRise.anim", 12, false),
        ("hys_FinalBoss_FlameDash.anim", 13, true),
        ("hys_FinalBoss_FlameLand.anim", 14, false),
        ("hys_FinalBoss_DoublePortalCast.anim", 15, false),
        ("hys_FinalBoss_RadialCharge.anim", 16, true),
        ("hys_FinalBoss_RadialRelease.anim", 17, false),
        ("hys_FinalBoss_PhaseTransition.anim", 18, false),
        ("hys_FinalBoss_Hit.anim", 19, false)
    };

    private static readonly (string clipName, bool loop)[] CommonClips =
    {
        ("hys_FinalBoss_Idle.anim", true),
        ("hys_FinalBoss_Move.anim", true),
        ("hys_FinalBoss_Groggy.anim", true),
        ("hys_FinalBoss_Death.anim", false)
    };

    static hys_FinalBossAnimatorGenerator()
    {
        EditorApplication.delayCall += EnsureSimpleController;
    }

    [MenuItem("Tools/hys/Final Boss/단순 Animator 다시 생성")]
    private static void RebuildFromMenu()
    {
        RebuildController();
    }

    public static void GenerateFromCommandLine()
    {
        RebuildController();
    }

    private static void EnsureSimpleController()
    {
        EnsureFolder();
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || NeedsRebuild(controller))
        {
            RebuildController();
            return;
        }
        ConnectRootObjectData(controller);
    }

    private static bool NeedsRebuild(AnimatorController controller)
    {
        bool hasAction = false;
        bool hasActionId = false;
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            hasAction |= parameters[i].name == "Action";
            hasActionId |= parameters[i].name == "ActionId";
        }
        return !hasAction || !hasActionId
            || controller.layers.Length != 1
            || controller.layers[0].stateMachine.states.Length != 5;
    }

    private static void RebuildController()
    {
        EnsureFolder();
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        else ResetControllerContents(controller);

        AddParameters(controller);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        machine.name = "hys_FinalBoss_Simple";

        AnimatorState idle = AddState(machine, "hys_FinalBoss_Idle", new Vector3(100f, 80f), "hys_FinalBoss_Idle.anim");
        AnimatorState move = AddState(machine, "hys_FinalBoss_Move", new Vector3(360f, 80f), "hys_FinalBoss_Move.anim");
        AnimatorState action = AddState(machine, "hys_FinalBoss_Action", new Vector3(360f, 260f));
        AnimatorState groggy = AddState(machine, "hys_FinalBoss_Groggy", new Vector3(620f, 80f), "hys_FinalBoss_Groggy.anim");
        AnimatorState death = AddState(machine, "hys_FinalBoss_Death", new Vector3(620f, 260f), "hys_FinalBoss_Death.anim");

        action.tag = "hys_Action";
        action.motion = CreateActionBlendTree(controller);
        machine.defaultState = idle;
        AddLocomotionTransitions(idle, move);
        AddMainTransitions(machine, action, groggy, death);

        AnimatorStateTransition groggyReturn = groggy.AddTransition(idle);
        groggyReturn.hasExitTime = false;
        groggyReturn.duration = 0.05f;
        groggyReturn.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGroggy");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        ConnectRootObjectData(controller);
        AssetDatabase.Refresh();
    }

    private static void ResetControllerContents(AnimatorController controller)
    {
        // 컨트롤러 GUID를 유지하면서 내부 구조만 안전하게 다시 만듭니다.
        controller.layers = new AnimatorControllerLayer[0];
        while (controller.parameters.Length > 0) controller.RemoveParameter(0);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ControllerPath);
        for (int i = 0; i < subAssets.Length; i++)
            if (subAssets[i] != null && subAssets[i] != controller)
                Object.DestroyImmediate(subAssets[i], true);

        AnimatorStateMachine machine = new AnimatorStateMachine { name = "hys_FinalBoss_Simple" };
        AssetDatabase.AddObjectToAsset(machine, controller);
        controller.AddLayer(new AnimatorControllerLayer
        {
            name = "Base Layer",
            defaultWeight = 1f,
            stateMachine = machine
        });
    }

    private static BlendTree CreateActionBlendTree(AnimatorController controller)
    {
        BlendTree tree = new BlendTree
        {
            name = "hys_FinalBoss_ActionById",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "ActionId",
            useAutomaticThresholds = false,
            minThreshold = 1f,
            maxThreshold = 19f
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        for (int i = 0; i < ActionClips.Length; i++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + ActionClips[i].clipName);
            tree.AddChild(clip, ActionClips[i].actionId);
        }
        return tree;
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Action", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ActionId", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGroggy", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string stateName,
        Vector3 position, string clipName = null)
    {
        AnimatorState state = machine.AddState(stateName, position);
        state.writeDefaultValues = true;
        if (!string.IsNullOrEmpty(clipName))
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + clipName);
        return state;
    }

    private static void AddLocomotionTransitions(AnimatorState idle, AnimatorState move)
    {
        AnimatorStateTransition toMove = idle.AddTransition(move);
        toMove.hasExitTime = false;
        toMove.duration = 0.05f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.01f, "MoveSpeed");
        AnimatorStateTransition toIdle = move.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "MoveSpeed");
    }

    private static void AddMainTransitions(AnimatorStateMachine machine, AnimatorState action,
        AnimatorState groggy, AnimatorState death)
    {
        AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
        toDeath.hasExitTime = false;
        toDeath.duration = 0.03f;
        toDeath.canTransitionToSelf = false;
        toDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        AnimatorStateTransition toGroggy = machine.AddAnyStateTransition(groggy);
        toGroggy.hasExitTime = false;
        toGroggy.duration = 0.03f;
        toGroggy.canTransitionToSelf = false;
        toGroggy.AddCondition(AnimatorConditionMode.If, 0f, "IsGroggy");
        toGroggy.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        AnimatorStateTransition toAction = machine.AddAnyStateTransition(action);
        toAction.hasExitTime = false;
        toAction.duration = 0.03f;
        toAction.canTransitionToSelf = true;
        toAction.AddCondition(AnimatorConditionMode.If, 0f, "Action");
        toAction.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGroggy");
        toAction.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets/05Anims", "FinalBoss");
    }

    private static void EnsureAnimationClips()
    {
        for (int i = 0; i < CommonClips.Length; i++)
            EnsureAnimationClip(CommonClips[i].clipName, CommonClips[i].loop);
        for (int i = 0; i < ActionClips.Length; i++)
            EnsureAnimationClip(ActionClips[i].clipName, ActionClips[i].loop);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureAnimationClip(string clipName, bool loop)
    {
        string path = ClipFolder + clipName;
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) return;
        // 실제 Sprite 프레임은 나중에 넣고 지금은 연결 가능한 빈 클립만 생성합니다.
        AnimationClip clip = new AnimationClip
        {
            name = System.IO.Path.GetFileNameWithoutExtension(clipName),
            frameRate = 12f
        };
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = loop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.CreateAsset(clip, path);
    }

    private static void ConnectRootObjectData(AnimatorController controller)
    {
        Object rootData = AssetDatabase.LoadAssetAtPath<Object>(RootDataPath);
        if (rootData == null || controller == null) return;
        SerializedObject serializedRootData = new SerializedObject(rootData);
        SerializedProperty property = serializedRootData.FindProperty("model.animatorController");
        if (property == null || property.objectReferenceValue == controller) return;
        property.objectReferenceValue = controller;
        serializedRootData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootData);
        AssetDatabase.SaveAssets();
    }
}
