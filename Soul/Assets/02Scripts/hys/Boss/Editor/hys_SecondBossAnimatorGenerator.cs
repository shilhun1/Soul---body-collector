using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 중간보스2 Animator를 5개 상태와 하나의 Action Blend Tree로 생성합니다.
[InitializeOnLoad]
internal static class hys_SecondBossAnimatorGenerator
{
    private const string ControllerPath = "Assets/05Anims/MidBoss_02/hys_MidBoss2.controller";
    private const string ClipFolder = "Assets/05Anims/MidBoss_02/";

    private static readonly (string clipName, int actionId, bool loop)[] ActionClips =
    {
        ("hys_MidBoss2_Dash.anim", 1, true),
        ("hys_MidBoss2_ReverseSlash.anim", 2, false),
        ("hys_MidBoss2_SummonCommand.anim", 3, false),
        ("hys_MidBoss2_TwoHandPrepare.anim", 4, true),
        ("hys_MidBoss2_WideSlash.anim", 5, false),
        ("hys_MidBoss2_PlantSword.anim", 6, false),
        ("hys_MidBoss2_ShockwaveChannel.anim", 7, true),
        ("hys_MidBoss2_DashSwordWave.anim", 8, true),
        ("hys_MidBoss2_MagicCast.anim", 9, true),
        ("hys_MidBoss2_MagicRelease.anim", 10, false),
        ("hys_MidBoss2_Hit.anim", 11, false)
    };

    private static readonly (string clipName, bool loop)[] CommonClips =
    {
        ("hys_MidBoss2_Idle.anim", true),
        ("hys_MidBoss2_Move.anim", true),
        ("hys_MidBoss2_Groggy.anim", true),
        ("hys_MidBoss2_Death.anim", false)
    };

    static hys_SecondBossAnimatorGenerator()
    {
        EditorApplication.delayCall += EnsureSimpleController;
    }

    [MenuItem("Tools/hys/MidBoss 2/단순 Animator 다시 생성")]
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
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || NeedsRebuild(controller))
        {
            RebuildController();
            return;
        }

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
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        else
            ResetControllerContents(controller);

        AddParameters(controller);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        machine.name = "hys_MidBoss2_Simple";

        AnimatorState idle = AddState(machine, "hys_MidBoss2_Idle", new Vector3(100f, 80f), "hys_MidBoss2_Idle.anim");
        AnimatorState move = AddState(machine, "hys_MidBoss2_Move", new Vector3(360f, 80f), "hys_MidBoss2_Move.anim");
        AnimatorState action = AddState(machine, "hys_MidBoss2_Action", new Vector3(360f, 260f));
        AnimatorState groggy = AddState(machine, "hys_MidBoss2_Groggy", new Vector3(620f, 80f), "hys_MidBoss2_Groggy.anim");
        AnimatorState death = AddState(machine, "hys_MidBoss2_Death", new Vector3(620f, 260f), "hys_MidBoss2_Death.anim");

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
        AssetDatabase.Refresh();
    }

    private static void ResetControllerContents(AnimatorController controller)
    {
        // 파일 GUID를 유지하면서 내부 상태만 교체해 기존 참조가 끊기지 않게 합니다.
        controller.layers = new AnimatorControllerLayer[0];
        while (controller.parameters.Length > 0) controller.RemoveParameter(0);

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ControllerPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] != null && subAssets[i] != controller)
                Object.DestroyImmediate(subAssets[i], true);
        }

        AnimatorStateMachine machine = new AnimatorStateMachine { name = "hys_MidBoss2_Simple" };
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
            name = "hys_MidBoss2_ActionById",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "ActionId",
            useAutomaticThresholds = false,
            minThreshold = 1f,
            maxThreshold = 11f
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

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string stateName,
        Vector3 position,
        string clipName = null)
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

    private static void AddMainTransitions(
        AnimatorStateMachine machine,
        AnimatorState action,
        AnimatorState groggy,
        AnimatorState death)
    {
        AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(death);
        deathTransition.hasExitTime = false;
        deathTransition.duration = 0.03f;
        deathTransition.canTransitionToSelf = false;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        AnimatorStateTransition groggyTransition = machine.AddAnyStateTransition(groggy);
        groggyTransition.hasExitTime = false;
        groggyTransition.duration = 0.03f;
        groggyTransition.canTransitionToSelf = false;
        groggyTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsGroggy");
        groggyTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        AnimatorStateTransition actionTransition = machine.AddAnyStateTransition(action);
        actionTransition.hasExitTime = false;
        actionTransition.duration = 0.03f;
        actionTransition.canTransitionToSelf = true;
        actionTransition.AddCondition(AnimatorConditionMode.If, 0f, "Action");
        actionTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGroggy");
        actionTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
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

}
