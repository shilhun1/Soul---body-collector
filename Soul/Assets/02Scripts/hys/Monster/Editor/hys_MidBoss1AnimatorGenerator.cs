using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 중간보스 1 Animator를 5개 상태와 하나의 Action Blend Tree로 단순하게 구성합니다.
[InitializeOnLoad]
internal static class hys_MidBoss1AnimatorGenerator
{
    private const string ControllerPath = "Assets/05Anims/MidBoss_01/hys_MidBoss1.controller";
    private const string RootDataPath = "Assets/02Scripts/hys/Monster/hys_Stage1MidBoss_RootObjectData.asset";
    private const string ClipFolder = "Assets/05Anims/MidBoss_01/";

    private static readonly (string clipName, int actionId, bool loop)[] ActionClips =
    {
        ("hys_MidBoss_Pattern1_CloneVolley.anim", 1, false),
        ("hys_MidBoss_Pattern2_TrackingArrows.anim", 2, true),
        ("hys_MidBoss_Pattern3_ArrowRainZone.anim", 3, true),
        ("hys_MidBoss_Pattern4_TeleportHorizontalArrow.anim", 4, false),
        ("hys_MidBoss_Pattern5_FinalArrowRain.anim", 5, true),
        ("hys_MidBoss_Phase2Laser.anim", 6, false),
        ("hys_MidBoss_Hit.anim", 7, false),
        ("hys_MidBoss_Phase.anim", 8, false)
    };

    private static readonly (string clipName, bool loop)[] CommonClips =
    {
        ("hys_MidBoss_Idle.anim", true),
        ("hys_MidBoss_Move.anim", true),
        ("hys_MidBoss_Groggy.anim", true),
        ("hys_MidBoss_Death.anim", false)
    };

    static hys_MidBoss1AnimatorGenerator()
    {
        EditorApplication.delayCall += EnsureSimpleController;
    }

    [MenuItem("Tools/hys/MidBoss 1/단순 Animator 다시 생성")]
    private static void RebuildFromMenu()
    {
        RebuildController();
    }

    // Unity 외부 검증이나 자동화에서도 같은 단순 구조를 생성합니다.
    public static void GenerateFromCommandLine()
    {
        RebuildController();
    }

    private static void EnsureSimpleController()
    {
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || NeedsSimplification(controller))
        {
            RebuildController();
            return;
        }

        ConnectRootObjectData(controller);
    }

    private static bool NeedsSimplification(AnimatorController controller)
    {
        bool hasActionId = false;
        bool hasLegacyPatternTrigger = false;
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            hasActionId |= parameters[i].name == "ActionId";
            hasLegacyPatternTrigger |= parameters[i].name == "Pattern1";
        }

        return !hasActionId
            || hasLegacyPatternTrigger
            || controller.layers.Length != 1
            || controller.layers[0].stateMachine.states.Length != 5;
    }

    private static void RebuildController()
    {
        EnsureAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            ResetControllerContents(controller);
        }

        AddParameters(controller);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        machine.name = "hys_MidBoss1_Simple";

        AnimatorState idle = AddState(machine, "hys_MidBoss1_Idle", new Vector3(100f, 80f), "hys_MidBoss_Idle.anim");
        AnimatorState move = AddState(machine, "hys_MidBoss1_Move", new Vector3(360f, 80f), "hys_MidBoss_Move.anim");
        AnimatorState action = AddState(machine, "hys_MidBoss1_Action", new Vector3(360f, 260f));
        AnimatorState groggy = AddState(machine, "hys_MidBoss1_Groggy", new Vector3(620f, 80f), "hys_MidBoss_Groggy.anim");
        AnimatorState death = AddState(machine, "hys_MidBoss1_Death", new Vector3(620f, 260f), "hys_MidBoss_Death.anim");

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
        // 컨트롤러 파일과 GUID는 유지하고 내부 상태만 교체해 기존 씬/데이터 참조를 보호합니다.
        controller.layers = new AnimatorControllerLayer[0];
        while (controller.parameters.Length > 0)
        {
            controller.RemoveParameter(0);
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ControllerPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] != null && subAssets[i] != controller)
            {
                Object.DestroyImmediate(subAssets[i], true);
            }
        }

        AnimatorStateMachine machine = new AnimatorStateMachine
        {
            name = "hys_MidBoss1_Simple"
        };
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
        BlendTree blendTree = new BlendTree
        {
            name = "hys_MidBoss1_ActionById",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "ActionId",
            useAutomaticThresholds = false,
            minThreshold = 1f,
            maxThreshold = 8f
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        for (int i = 0; i < ActionClips.Length; i++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ClipFolder + ActionClips[i].clipName);
            blendTree.AddChild(clip, ActionClips[i].actionId);
        }

        return blendTree;
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Action", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ActionId", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGroggy", AnimatorControllerParameterType.Bool);
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
        {
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + clipName);
        }

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
        actionTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        actionTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGroggy");
    }

    private static void EnsureAnimationClips()
    {
        for (int i = 0; i < CommonClips.Length; i++)
        {
            EnsureAnimationClip(CommonClips[i].clipName, CommonClips[i].loop);
        }

        for (int i = 0; i < ActionClips.Length; i++)
        {
            EnsureAnimationClip(ActionClips[i].clipName, ActionClips[i].loop);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureAnimationClip(string clipName, bool loop)
    {
        string path = ClipFolder + clipName;
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
        {
            return;
        }

        // 프레임은 나중에 추가하고 지금은 상태 연결용 빈 클립만 만듭니다.
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
        if (rootData == null || controller == null)
        {
            return;
        }

        SerializedObject serializedRootData = new SerializedObject(rootData);
        SerializedProperty controllerProperty = serializedRootData.FindProperty("model.animatorController");
        if (controllerProperty == null || controllerProperty.objectReferenceValue == controller)
        {
            return;
        }

        controllerProperty.objectReferenceValue = controller;
        serializedRootData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootData);
        AssetDatabase.SaveAssets();
    }
}
