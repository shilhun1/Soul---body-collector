using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the production fighter-boss clips/controller from validated transparent frames
/// and applies them to the existing runtime prefab. The previous controller and prefab
/// backup are never overwritten.
/// </summary>
[InitializeOnLoad]
public static class HWJ_FighterBossIntegrationBuilder
{
    public const string BossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";
    public const string BossScenePath =
        "Assets/01Scenes/HWJ_Stage1_04_MidBossBarracks.unity";
    public const string FramesRoot =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Sprites";
    public const string ClipsRoot =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Clips";
    public const string ControllersRoot =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Controllers";
    public const string ReportsRoot =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Reports";
    public const string ControllerPath =
        ControllersRoot + "/HWJ_FighterBoss_Integrated.controller";
    public const string ValidationReportPath =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Validation/BossSpriteGenerationValidation.json";

    private const string RootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";
    private const string BackupPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Backups/Bosses/HWJ_MidBoss1_Runtime_Prefab_PreFighterBossFirstPass.prefab";
    private const string PatternRoot =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns";
    private const string AutoBuildSessionKey = "HWJ.FighterBossIntegrationBuilder.AutoBuild.V9";
    private const string BuildContractMarker = "Attack flow contract: V9";
    private const string ExternalBuildFlagPath =
        @"C:\Docs\Generated\HWJ_BuildFighterBossIntegration.flag";
    private const string BuildReportPath =
        ReportsRoot + "/FighterBossIntegrationBuild.md";

    private sealed class ClipSpec
    {
        public string ClipName;
        public string SourceSequence;
        public float FrameRate;
        public bool Loop;
        public float MinimumDuration;
        public int TakeFrames;
        public int[] FrameIndices;
        public AnimationEvent[] Events;
    }

    private sealed class PatternSpec
    {
        public string AssetName;
        public string PatternId;
        public string StateName;
        public string ExecutorKey;
        public int Number;
        public bool PhaseOne;
        public float Cooldown;
        public float HpRatio;
        public int Weight;
    }

    private static double nextExternalBuildPoll;

    static HWJ_FighterBossIntegrationBuilder()
    {
        EditorApplication.delayCall += TryAutoBuildOnce;
        EditorApplication.update -= PollExternalBuildFlag;
        EditorApplication.update += PollExternalBuildFlag;
    }

    [MenuItem("Tools/HWJ/Boss/Build Integrated Fighter Boss")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildBatch()
    {
        try
        {
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Build()
    {
        ValidateGenerationReport();
        EnsureFolder(ClipsRoot);
        EnsureFolder(ControllersRoot);
        EnsureFolder(ReportsRoot);
        EnsurePrefabBackup();

        Dictionary<string, AnimationClip> clips = BuildClips();
        AnimatorController controller = BuildController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(
            ControllerPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
            ?? throw new InvalidOperationException("Integrated Animator Controller failed to reload.");
        HWJ_BossPatternDataSO[] patterns = BuildPatternData();
        ConfigureRootData(controller);
        ConfigureExistingBossPrefab(controller, clips, patterns);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(
            BossPrefabPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(
            RootDataPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateIntegratedAssets(controller, clips);
        WriteBuildReport(controller, clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HWJ] Integrated fighter boss build and validation completed.");
    }

    private static void TryAutoBuildOnce()
    {
        if (Application.isBatchMode
            || EditorApplication.isPlayingOrWillChangePlaymode
            || SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);

        AnimatorController existingController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        TextAsset existingReport = AssetDatabase.LoadAssetAtPath<TextAsset>(BuildReportPath);

        if (existingController != null
            && existingReport != null
            && existingReport.text.Contains(BuildContractMarker))
        {
            return;
        }

        try
        {
            Build();
        }
        catch (Exception exception)
        {
            Debug.LogError("[HWJ] Fighter boss auto-build stopped. Existing assets were kept.");
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// 이미 열린 Unity 프로젝트에 두 번째 에디터를 띄우지 않고 통합 빌드를 요청하는 경로입니다.
    /// 컴파일, 임포트, Play Mode가 모두 끝난 안전한 시점에 플래그를 한 번만 소비합니다.
    /// </summary>
    private static void PollExternalBuildFlag()
    {
        if (EditorApplication.timeSinceStartup < nextExternalBuildPoll)
        {
            return;
        }

        nextExternalBuildPoll = EditorApplication.timeSinceStartup + 1d;

        if (!File.Exists(ExternalBuildFlagPath)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        File.Delete(ExternalBuildFlagPath);
        Build();
    }

    private static Dictionary<string, AnimationClip> BuildClips()
    {
        List<ClipSpec> specs = new List<ClipSpec>
        {
            Spec("Boss_P1_Idle", "P1_Idle", 8f, true, 0f),
            Spec("Boss_P1_Move", "P1_Move", 10f, true, 0f),
            Spec("Boss_P1_Hurt", "P1_Attack_Combo", 10f, false, 0.3f, 3),
            // 공격 프레임 일부를 왕복시켜 별도 이펙트 없이 몸 자체로 준비 동작을 보여줍니다.
            CastSpec("Boss_P1_Cast_Combo", "P1_Attack_Combo", 0, 1, 0, 1),
            CastSpec("Boss_P1_Cast_Charge", "P1_Attack_Charge", 1, 2, 3, 2),
            CastSpec("Boss_P1_Cast_Uppercut", "P1_Attack_Uppercut", 1, 2, 3, 2),
            CastSpec("Boss_P1_Cast_GroundSlam", "P1_Attack_GroundSlam", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_EnhancedCombo", "P1_Attack_Combo", 0, 1, 2, 1),
            CastSpec("Boss_P2_Cast_DoubleCharge", "P1_Attack_Charge", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_ThunderUppercut", "P1_Attack_Uppercut", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_DarkGroundSlam", "P1_Attack_GroundSlam", 3, 4, 5, 4),
            CastSpec("Boss_P2_Cast_ShadowCombo", "P2_ShadowCombo", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_LightningCast", "P2_LightningCast", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_DarkWave", "P2_DarkWave", 2, 3, 4, 3),
            CastSpec("Boss_P2_Cast_SoulBind", "P2_SoulBind", 1, 2, 3, 2),
            CastSpec("Boss_P2_Cast_Ultimate", "P2_Ultimate", 1, 2, 3, 2),
            Spec(
                "Boss_P1_Attack_Combo",
                "P1_Attack_Combo",
                12f,
                false,
                1.68f,
                0,
                Events(
                    Event("Anim_AttackStart", 0f),
                    Event("Anim_EnableHitbox", 0.28f, 1),
                    Event("Anim_DisableHitbox", 0.39f),
                    Event("Anim_EnableHitbox", 0.55f, 2),
                    Event("Anim_DisableHitbox", 0.67f),
                    Event("Anim_EnableHitbox", 0.82f, 3),
                    Event("Anim_DisableHitbox", 0.97f),
                    Event("Anim_RecoveryStart", 0.98f),
                    Event("Anim_AttackEnd", 1.68f))),
            Spec(
                "Boss_P1_Attack_Charge",
                "P1_Attack_Charge",
                12f,
                false,
                1.9f,
                0,
                Events(
                    Event("Anim_AttackStart", 0f),
                    Event("Anim_EnableHitbox", 0.5f, 1),
                    Event("Anim_ApplyMovement", 0.58f),
                    Event("Anim_StopMovement", 1.1f),
                    Event("Anim_DisableHitbox", 1.1f),
                    Event("Anim_RecoveryStart", 1.11f),
                    Event("Anim_AttackEnd", 1.9f))),
            Spec(
                "Boss_P1_Attack_Uppercut",
                "P1_Attack_Uppercut",
                12f,
                false,
                1.8f,
                0,
                Events(
                    Event("Anim_AttackStart", 0f),
                    Event("Anim_EnableHitbox", 0.4f, 1),
                    Event("Anim_ApplyMovement", 0.46f),
                    Event("Anim_DisableHitbox", 0.74f),
                    Event("Anim_RecoveryStart", 0.76f),
                    Event("Anim_AttackEnd", 1.8f))),
            Spec(
                "Boss_P1_Attack_GroundSlam",
                "P1_Attack_GroundSlam",
                12f,
                false,
                1.7f,
                0,
                Events(
                    Event("Anim_AttackStart", 0f),
                    Event("Anim_SpawnGroundHazard", 0.6f),
                    Event("Anim_EnableHitbox", 0.6f, 1),
                    Event("Anim_CameraShakeHook", 0.62f),
                    Event("Anim_SFXHook", 0.62f),
                    Event("Anim_DisableHitbox", 0.79f),
                    Event("Anim_RecoveryStart", 0.8f),
                    Event("Anim_AttackEnd", 1.7f))),
            Spec(
                "Boss_PhaseBreak_Down",
                "P2_Death",
                8f,
                false,
                0.8f,
                4,
                Events(Event("Anim_PhaseStep", 0f, 0))),
            Spec(
                "Boss_PhaseBreak_Prayer",
                "P2_SoulBind",
                8f,
                false,
                0.9f,
                5,
                Events(Event("Anim_PhaseStep", 0f, 1))),
            Spec(
                "Boss_PhaseBreak_LightningHit",
                "P2_LightningCast",
                10f,
                false,
                0.8f,
                6,
                Events(
                    Event("Anim_PhaseStep", 0f, 2),
                    Event("Anim_CameraShakeHook", 0.45f),
                    Event("Anim_SFXHook", 0.45f))),
            Spec(
                "Boss_PhaseBreak_Transform",
                "P2_Ultimate",
                10f,
                false,
                1f,
                8,
                Events(Event("Anim_PhaseStep", 0f, 3))),
            Spec(
                "Boss_Phase2_Start",
                "P2_ShadowCombo",
                10f,
                false,
                0.9f,
                6,
                Events(Event("Anim_PhaseStep", 0f, 4))),
            Spec("Boss_P2_ShadowCombo", "P2_ShadowCombo", 12f, false, 2.6f, 0, P2Events(2.6f)),
            Spec("Boss_P2_LightningCast", "P2_LightningCast", 12f, false, 4.2f, 0, P2Events(4.2f)),
            Spec("Boss_P2_DarkWave", "P2_DarkWave", 12f, false, 2.25f, 0, P2Events(2.25f)),
            Spec("Boss_P2_SoulBind", "P2_SoulBind", 12f, false, 2.6f, 0, P2Events(2.6f)),
            Spec("Boss_P2_Ultimate", "P2_Ultimate", 12f, false, 5.2f, 0, P2Events(5.2f)),
            Spec(
                "Boss_P2_Death",
                "P2_Death",
                8f,
                false,
                1.75f,
                14,
                Events(
                    Event("DeathStart", 0f),
                    Event("DeathCameraShakeHook", 0.75f),
                    Event("DeathSFXHook", 0.75f),
                    Event("Anim_DeathComplete", 1.74f)))
        };

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        for (int i = 0; i < specs.Count; i++)
        {
            ClipSpec spec = specs[i];
            Sprite[] sprites = LoadValidatedSpriteSequence(spec.SourceSequence);

            if (spec.TakeFrames > 0 && spec.TakeFrames < sprites.Length)
            {
                sprites = sprites.Take(spec.TakeFrames).ToArray();
            }

            if (spec.FrameIndices != null && spec.FrameIndices.Length > 0)
            {
                Sprite[] selectedSprites = new Sprite[spec.FrameIndices.Length];

                for (int frameIndex = 0; frameIndex < spec.FrameIndices.Length; frameIndex++)
                {
                    int sourceIndex = spec.FrameIndices[frameIndex];

                    if (sourceIndex < 0 || sourceIndex >= sprites.Length)
                    {
                        throw new InvalidOperationException(
                            $"{spec.ClipName} requests missing frame {sourceIndex} from {spec.SourceSequence}.");
                    }

                    selectedSprites[frameIndex] = sprites[sourceIndex];
                }

                sprites = selectedSprites;
            }

            AnimationClip clip = CreateOrUpdateClip(spec, sprites);
            clips[spec.ClipName] = clip;
        }

        return clips;
    }

    private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Phase", AnimatorControllerParameterType.Int);
        controller.AddParameter("AttackId", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PhaseTransition", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorStateMachine baseMachine = root.AddStateMachine("Base", new Vector3(250f, 20f));
        AnimatorStateMachine phaseOneMachine =
            root.AddStateMachine("Phase1Attacks", new Vector3(500f, 20f));
        AnimatorStateMachine transitionMachine =
            root.AddStateMachine("Transition", new Vector3(750f, 20f));
        AnimatorStateMachine phaseTwoMachine =
            root.AddStateMachine("Phase2Attacks", new Vector3(1000f, 20f));
        AnimatorStateMachine deathMachine =
            root.AddStateMachine("Death", new Vector3(1250f, 20f));

        AnimatorState idle = AddState(baseMachine, "P1_Idle", clips["Boss_P1_Idle"]);
        AnimatorState move = AddState(baseMachine, "P1_Move", clips["Boss_P1_Move"]);
        AnimatorState hurt = AddState(baseMachine, "Hurt", clips["Boss_P1_Hurt"]);
        baseMachine.defaultState = idle;

        AnimatorStateTransition idleToMove = idle.AddTransition(move);
        ConfigureTransition(idleToMove, false, 0.05f);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        AnimatorStateTransition moveToIdle = move.AddTransition(idle);
        ConfigureTransition(moveToIdle, false, 0.05f);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
        AnimatorStateTransition hurtToIdle = hurt.AddTransition(idle);
        ConfigureTransition(hurtToIdle, true, 0.04f);
        AnimatorStateTransition anyToHurt = root.AddAnyStateTransition(hurt);
        ConfigureTransition(anyToHurt, false, 0.02f);
        anyToHurt.canTransitionToSelf = false;
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

        AddState(phaseOneMachine, "P1_Cast_Combo", clips["Boss_P1_Cast_Combo"]);
        AddState(phaseOneMachine, "P1_Cast_Charge", clips["Boss_P1_Cast_Charge"]);
        AddState(phaseOneMachine, "P1_Cast_Uppercut", clips["Boss_P1_Cast_Uppercut"]);
        AddState(phaseOneMachine, "P1_Cast_GroundSlam", clips["Boss_P1_Cast_GroundSlam"]);
        AddState(phaseOneMachine, "P1_Attack_Combo", clips["Boss_P1_Attack_Combo"]);
        AddState(phaseOneMachine, "P1_Attack_Charge", clips["Boss_P1_Attack_Charge"]);
        AddState(phaseOneMachine, "P1_Attack_Uppercut", clips["Boss_P1_Attack_Uppercut"]);
        AddState(phaseOneMachine, "P1_Attack_GroundSlam", clips["Boss_P1_Attack_GroundSlam"]);

        AddState(transitionMachine, "PhaseBreak_Down", clips["Boss_PhaseBreak_Down"]);
        AddState(transitionMachine, "PhaseBreak_Prayer", clips["Boss_PhaseBreak_Prayer"]);
        AddState(
            transitionMachine,
            "PhaseBreak_LightningHit",
            clips["Boss_PhaseBreak_LightningHit"]);
        AddState(
            transitionMachine,
            "PhaseBreak_Transform",
            clips["Boss_PhaseBreak_Transform"]);
        AddState(transitionMachine, "Phase2_Start", clips["Boss_Phase2_Start"]);

        AddState(phaseTwoMachine, "P2_Cast_EnhancedCombo", clips["Boss_P2_Cast_EnhancedCombo"]);
        AddState(phaseTwoMachine, "P2_Cast_DoubleCharge", clips["Boss_P2_Cast_DoubleCharge"]);
        AddState(phaseTwoMachine, "P2_Cast_ThunderUppercut", clips["Boss_P2_Cast_ThunderUppercut"]);
        AddState(phaseTwoMachine, "P2_Cast_DarkGroundSlam", clips["Boss_P2_Cast_DarkGroundSlam"]);
        AddState(phaseTwoMachine, "P2_Cast_ShadowCombo", clips["Boss_P2_Cast_ShadowCombo"]);
        AddState(phaseTwoMachine, "P2_Cast_LightningCast", clips["Boss_P2_Cast_LightningCast"]);
        AddState(phaseTwoMachine, "P2_Cast_DarkWave", clips["Boss_P2_Cast_DarkWave"]);
        AddState(phaseTwoMachine, "P2_Cast_SoulBind", clips["Boss_P2_Cast_SoulBind"]);
        AddState(phaseTwoMachine, "P2_Cast_Ultimate", clips["Boss_P2_Cast_Ultimate"]);
        AddState(phaseTwoMachine, "P2_EnhancedCombo", clips["Boss_P1_Attack_Combo"]);
        AddState(phaseTwoMachine, "P2_DoubleCharge", clips["Boss_P1_Attack_Charge"]);
        AddState(phaseTwoMachine, "P2_ThunderUppercut", clips["Boss_P1_Attack_Uppercut"]);
        AddState(phaseTwoMachine, "P2_DarkGroundSlam", clips["Boss_P1_Attack_GroundSlam"]);
        AddState(phaseTwoMachine, "P2_ShadowCombo", clips["Boss_P2_ShadowCombo"]);
        AddState(phaseTwoMachine, "P2_LightningCast", clips["Boss_P2_LightningCast"]);
        AddState(phaseTwoMachine, "P2_DarkWave", clips["Boss_P2_DarkWave"]);
        AddState(phaseTwoMachine, "P2_SoulBind", clips["Boss_P2_SoulBind"]);
        AddState(phaseTwoMachine, "P2_Ultimate", clips["Boss_P2_Ultimate"]);
        AddState(deathMachine, "P2_Death", clips["Boss_P2_Death"]);

        // Route the controller's Entry node directly to the nested idle state.
        // A zero-condition transition from a synthetic state is ignored by Unity.
        root.AddEntryTransition(idle);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static HWJ_BossPatternDataSO[] BuildPatternData()
    {
        PatternSpec[] specs =
        {
            Pattern("P1_Attack_Combo", "P1_Attack_Combo", "fighter_boss_combo", 1, true, 2.6f, 1f, 5),
            Pattern("P1_Attack_Charge", "P1_Attack_Charge", "fighter_boss_charge", 2, true, 3.8f, 1f, 5),
            Pattern("P1_Attack_Uppercut", "P1_Attack_Uppercut", "fighter_boss_uppercut", 3, true, 3.2f, 1f, 5),
            Pattern("P1_Attack_GroundSlam", "P1_Attack_GroundSlam", "fighter_boss_ground_slam", 4, true, 4.2f, 1f, 4),
            Pattern("P2_EnhancedCombo", "P2_EnhancedCombo", PhaseTwoExecutorKey(), 11, false, 3f, 1f, 5),
            Pattern("P2_DoubleCharge", "P2_DoubleCharge", PhaseTwoExecutorKey(), 12, false, 4.5f, 1f, 5),
            Pattern("P2_ThunderUppercut", "P2_ThunderUppercut", PhaseTwoExecutorKey(), 13, false, 4f, 1f, 5),
            Pattern("P2_DarkGroundSlam", "P2_DarkGroundSlam", PhaseTwoExecutorKey(), 14, false, 5f, 1f, 4),
            Pattern("P2_ShadowCombo", "P2_ShadowCombo", PhaseTwoExecutorKey(), 15, false, 6.5f, 1f, 4),
            Pattern("P2_LightningCast", "P2_LightningCast", PhaseTwoExecutorKey(), 16, false, 7f, 1f, 4),
            Pattern("P2_DarkWave", "P2_DarkWave", PhaseTwoExecutorKey(), 17, false, 4.5f, 1f, 5),
            Pattern("P2_SoulBind", "P2_SoulBind", PhaseTwoExecutorKey(), 18, false, 7f, 1f, 3),
            Pattern("P2_Ultimate", "P2_Ultimate", PhaseTwoExecutorKey(), 19, false, 18f, 0.35f, 2)
        };

        HWJ_BossPatternDataSO[] assets = new HWJ_BossPatternDataSO[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            PatternSpec spec = specs[i];
            string path = $"{PatternRoot}/HWJ_FighterBoss_{spec.AssetName}.asset";
            HWJ_BossPatternDataSO asset = AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HWJ_BossPatternDataSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "patternId", spec.PatternId);
            SetInt(serialized, "patternNumber", spec.Number);
            SetEnum(serialized, "trigger", spec.HpRatio < 1f
                ? (int)HWJ_BossPatternTrigger.HpBelowRatio
                : (int)HWJ_BossPatternTrigger.Always);
            SetEnum(serialized, "rangeMode", (int)HWJ_BossPatternRangeMode.Any);
            SetBool(serialized, "usableInPhase1", spec.PhaseOne);
            SetBool(serialized, "usableInPhase2", !spec.PhaseOne);
            SetFloat(serialized, "hpRatio", spec.HpRatio);
            SetFloat(serialized, "cooldownSeconds", spec.Cooldown);
            SetFloat(serialized, "defaultCooldownSeconds", spec.Cooldown);
            SetInt(serialized, "weight", spec.Weight);
            SetString(serialized, "animationId", spec.StateName);
            SetBool(serialized, "useStageOneSpecialExecution", false);
            SetBool(serialized, "useCustomPatternExecutor", true);
            SetString(serialized, "customPatternExecutorKey", spec.ExecutorKey);
            SerializedProperty actions = serialized.FindProperty("skillActions");

            if (actions != null)
            {
                actions.arraySize = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            assets[i] = asset;
        }

        AssetDatabase.SaveAssets();
        return assets;
    }

    private static void ConfigureExistingBossPrefab(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips,
        HWJ_BossPatternDataSO[] patterns)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);

        try
        {
            root.transform.localScale = Vector3.one;
            Rigidbody2D body = Require<Rigidbody2D>(root);
            Collider2D bodyCollider = Require<Collider2D>(root);
            SpriteRenderer renderer = Require<SpriteRenderer>(root);
            Animator animator = Require<Animator>(root);
            HWJ_BossBrainSystem brain = Require<HWJ_BossBrainSystem>(root);
            // FirstPass를 다시 생성하지 않고 통합 빌드만 실행해도 중복 방지 장치를 보장합니다.
            GetOrAdd<HWJ_BossDuplicateGuardSystem>(root);
            HWJ_RuntimeStatusSystem status = Require<HWJ_RuntimeStatusSystem>(root);
            HWJ_CombatSystem combat = Require<HWJ_CombatSystem>(root);
            HWJ_BossPatternSystem patternSystem = Require<HWJ_BossPatternSystem>(root);
            HWJ_FighterBossComboSystem combo = Require<HWJ_FighterBossComboSystem>(root);
            HWJ_FighterBossChargeSystem charge = Require<HWJ_FighterBossChargeSystem>(root);
            HWJ_FighterBossUppercutSystem uppercut = Require<HWJ_FighterBossUppercutSystem>(root);
            HWJ_FighterBossGroundSlamSystem slam = Require<HWJ_FighterBossGroundSlamSystem>(root);
            HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
                Require<HWJ_FighterBossPhaseTwoPatternSystem>(root);
            HWJ_FighterBossDeathSystem death = Require<HWJ_FighterBossDeathSystem>(root);
            HWJ_FighterBossAnimationEvents eventRouter =
                Require<HWJ_FighterBossAnimationEvents>(root);
            HWJ_CharacterMotionSystem motion = Require<HWJ_CharacterMotionSystem>(root);
            HWJ_FighterBossAnimatorSystem animatorSystem =
                GetOrAdd<HWJ_FighterBossAnimatorSystem>(root);
            HWJ_BossDialogueBubbleSystem dialogue =
                GetOrAdd<HWJ_BossDialogueBubbleSystem>(root);
            HWJ_BossCameraFocusSystem cameraFocus =
                GetOrAdd<HWJ_BossCameraFocusSystem>(root);
            HWJ_FighterBossHealthBarSystem healthBar =
                root.GetComponentInChildren<HWJ_FighterBossHealthBarSystem>(true);

            if (healthBar == null)
            {
                throw new InvalidOperationException("Fighter boss health bar is missing.");
            }

            dialogue.ConfigureFighterBossDefaults(healthBar.transform);
            cameraFocus.ConfigureFighterBossDialogueDefaults();
            EditorUtility.SetDirty(dialogue);
            EditorUtility.SetDirty(cameraFocus);

            Sprite[] idleSprites = LoadValidatedSpriteSequence("P1_Idle");
            renderer.sprite = idleSprites[0];
            renderer.sortingOrder = Mathf.Max(10, renderer.sortingOrder);
            renderer.flipX = false;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            SetReferences(animatorSystem,
                ("animator", animator),
                ("body", body),
                ("bossBrain", brain),
                ("runtimeStatus", status));
            SetReferences(eventRouter,
                ("comboSystem", combo),
                ("bossBrain", brain),
                ("deathSystem", death));
            SetReferences(combo, ("animator", animator), ("animatorSystem", animatorSystem));
            SetReferences(charge, ("animator", animator), ("animatorSystem", animatorSystem));
            SetReferences(
                uppercut,
                ("animator", animator),
                ("animatorSystem", animatorSystem),
                ("body", body),
                ("bodyCollider", bodyCollider));
            SetReferences(slam, ("animator", animator), ("animatorSystem", animatorSystem));
            SetReferences(
                phaseTwo,
                ("bossBrain", brain),
                ("runtimeStatus", status),
                ("combatSystem", combat),
                ("animatorSystem", animatorSystem),
                ("animator", animator),
                ("facingRenderer", renderer),
                ("body", body),
                ("bodyCollider", bodyCollider));
            SetReferences(
                death,
                ("animator", animator),
                ("animatorSystem", animatorSystem),
                ("phaseTwoPatternSystem", phaseTwo),
                ("dialogueBubbleSystem", dialogue),
                ("body", body),
                ("bodyCollider", bodyCollider));
            SetReferences(
                brain,
                ("patternSystem", patternSystem),
                ("motionSystem", motion),
                ("cameraFocusSystem", cameraFocus),
                ("dialogueBubbleSystem", dialogue),
                ("fighterComboSystem", combo),
                ("fighterChargeSystem", charge),
                ("fighterUppercutSystem", uppercut),
                ("fighterGroundSlamSystem", slam),
                ("fighterPhaseTwoPatternSystem", phaseTwo),
                ("fighterDeathSystem", death),
                ("fighterAnimatorSystem", animatorSystem),
                ("animator", animator),
                ("body", body));
            SetBool(brain, "useTwoBarPhaseHealth", true);
            SetBool(brain, "aiEnabled", true);
            SetFloat(combo, "castWaitSeconds", 0.65f);
            SetFloat(combo, "watchdogSeconds", 3.2f);
            SetFloat(charge, "castWaitSeconds", 0.75f);
            SetFloat(charge, "watchdogSeconds", 3.4f);
            SetFloat(uppercut, "castWaitSeconds", 0.65f);
            SetFloat(uppercut, "watchdogSeconds", 3f);
            SetFloat(slam, "castWaitSeconds", 0.85f);
            SetFloat(slam, "watchdogSeconds", 3.2f);
            SetFloat(death, "watchdogSeconds", 1.9f);
            ConfigurePatternSystem(patternSystem, patterns, combo, charge, uppercut, slam, phaseTwo);
            ConfigurePhaseTwoProfiles(phaseTwo);

            HWJ_FighterBossHitboxSystem[] hitboxes =
                root.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

            for (int i = 0; i < hitboxes.Length; i++)
            {
                hitboxes[i]?.Disarm();
            }

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigurePatternSystem(
        HWJ_BossPatternSystem patternSystem,
        HWJ_BossPatternDataSO[] patterns,
        params MonoBehaviour[] executors)
    {
        SerializedObject serialized = new SerializedObject(patternSystem);
        SerializedProperty patternArray = RequireProperty(serialized, "patterns");
        patternArray.arraySize = patterns.Length;

        for (int i = 0; i < patterns.Length; i++)
        {
            patternArray.GetArrayElementAtIndex(i).objectReferenceValue = patterns[i];
        }

        SerializedProperty executorArray = RequireProperty(serialized, "specialPatternExecutors");
        executorArray.arraySize = executors.Length;

        for (int i = 0; i < executors.Length; i++)
        {
            executorArray.GetArrayElementAtIndex(i).objectReferenceValue = executors[i];
        }

        SetBool(serialized, "autoUsePatterns", false);
        SetBool(serialized, "preventSamePatternRepeat", true);
        SetBool(serialized, "excludeRecentTwoPatterns", true);
        SetBool(serialized, "preventConsecutivePatternCategories", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(patternSystem);
    }

    private static void ConfigurePhaseTwoProfiles(HWJ_FighterBossPhaseTwoPatternSystem system)
    {
        string[] ids =
        {
            "P2_EnhancedCombo",
            "P2_DoubleCharge",
            "P2_ThunderUppercut",
            "P2_DarkGroundSlam",
            "P2_ShadowCombo",
            "P2_LightningCast",
            "P2_DarkWave",
            "P2_SoulBind",
            "P2_Ultimate"
        };
        float[] castWaitSeconds = { 0.55f, 0.7f, 0.65f, 0.85f, 0.55f, 0.9f, 0.75f, 0.8f, 1.2f };
        float[] watchdogSeconds = { 4f, 4.5f, 5f, 4f, 5f, 7.5f, 4f, 4.5f, 8.5f };
        SerializedObject serialized = new SerializedObject(system);
        SerializedProperty profiles = RequireProperty(serialized, "profiles");

        if (profiles.arraySize != ids.Length)
        {
            throw new InvalidOperationException(
                $"Existing fighter P2 profile count must be {ids.Length}, but was {profiles.arraySize}.");
        }

        for (int i = 0; i < profiles.arraySize; i++)
        {
            SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
            RequireRelative(profile, "patternId").stringValue = ids[i];
            RequireRelative(profile, "animatorState").stringValue = ids[i];
            RequireRelative(profile, "patternKind").enumValueIndex = i;
            RequireRelative(profile, "castWaitSeconds").floatValue = castWaitSeconds[i];
            RequireRelative(profile, "watchdogSeconds").floatValue = watchdogSeconds[i];

            HWJ_FighterBossHitboxSystem hitbox =
                RequireRelative(profile, "hitbox").objectReferenceValue
                as HWJ_FighterBossHitboxSystem;
            LineRenderer telegraph =
                RequireRelative(profile, "telegraphRenderer").objectReferenceValue
                as LineRenderer;

            if (hitbox == null || telegraph == null)
            {
                throw new InvalidOperationException($"P2 profile {ids[i]} has missing hitbox or telegraph.");
            }

            hitbox.gameObject.name = ids[i] + "_Hitbox";
            telegraph.gameObject.name = "TEMP_TELEGRAPH_" + ids[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(system);
    }

    private static void ConfigureRootData(AnimatorController controller)
    {
        HWJ_RootObjectDataSO rootData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(RootDataPath);

        if (rootData == null)
        {
            throw new InvalidOperationException("Fighter boss root data is missing.");
        }

        SerializedObject serialized = new SerializedObject(rootData);
        SerializedProperty controllerProperty = serialized.FindProperty("model.animatorController");

        if (controllerProperty == null)
        {
            throw new InvalidOperationException("RootObjectData.model.animatorController was not found.");
        }

        controllerProperty.objectReferenceValue = controller;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootData);
    }

    private static void ValidateIntegratedAssets(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);

        if (prefab == null)
        {
            throw new InvalidOperationException("Integrated boss prefab is missing.");
        }

        SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        Animator animator = prefab.GetComponent<Animator>();

        if (renderer == null || renderer.sprite == null)
        {
            throw new InvalidOperationException("Boss SpriteRenderer has a missing Sprite.");
        }

        if (!AssetDatabase.GetAssetPath(renderer.sprite).StartsWith(
            FramesRoot,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Boss uses a Sprite outside the validated frame folder.");
        }

        string appliedControllerPath = animator != null
            ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
            : string.Empty;

        // Asset identity can be represented by a stale in-memory instance immediately
        // after DeleteAsset/CreateAsset at the same path. The saved path/GUID is canonical.
        if (animator == null
            || !string.Equals(
                appliedControllerPath,
                AssetDatabase.GetAssetPath(controller),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Boss Animator is not using the integrated controller.");
        }

        if (prefab.transform.localScale != Vector3.one)
        {
            throw new InvalidOperationException("Boss prefab root scale must be (1,1,1).");
        }

        if (prefab.GetComponent<HWJ_BossDuplicateGuardSystem>() == null)
        {
            throw new InvalidOperationException("Boss prefab duplicate guard is missing.");
        }

        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) > 0)
        {
            throw new InvalidOperationException("Boss prefab contains a missing MonoBehaviour.");
        }

        HWJ_FighterBossHitboxSystem[] hitboxes =
            prefab.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            if (hitboxes[i].IsArmed || hitboxes[i].ColliderEnabled)
            {
                throw new InvalidOperationException(
                    $"Saved hitbox must be disabled: {hitboxes[i].name}");
            }
        }

        foreach (AnimationClip clip in clips.Values)
        {
            EditorCurveBinding[] transformCurves = AnimationUtility.GetCurveBindings(clip);

            if (transformCurves.Length > 0)
            {
                throw new InvalidOperationException(
                    $"{clip.name} contains an unexpected float/Transform curve.");
            }

            EditorCurveBinding[] objectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(clip);

            if (objectBindings.Length != 1
                || objectBindings[0].type != typeof(SpriteRenderer)
                || objectBindings[0].propertyName != "m_Sprite")
            {
                throw new InvalidOperationException(
                    $"{clip.name} must contain exactly one SpriteRenderer.m_Sprite curve.");
            }

            ObjectReferenceKeyframe[] keys =
                AnimationUtility.GetObjectReferenceCurve(clip, objectBindings[0]);

            for (int i = 0; i < keys.Length; i++)
            {
                string spritePath = AssetDatabase.GetAssetPath(keys[i].value);

                if (!spritePath.StartsWith(FramesRoot, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"{clip.name} references an unvalidated Sprite: {spritePath}");
                }
            }

            bool shouldLoop = clip.name == "Boss_P1_Idle"
                || clip.name == "Boss_P1_Move"
                || clip.name.Contains("_Cast_", StringComparison.Ordinal);
            bool loops = AnimationUtility.GetAnimationClipSettings(clip).loopTime;

            if (shouldLoop != loops)
            {
                throw new InvalidOperationException($"{clip.name} has an invalid Loop setting.");
            }
        }

        string[] requiredStates =
        {
            "P1_Idle", "P1_Move", "Hurt",
            "P1_Cast_Combo", "P1_Cast_Charge", "P1_Cast_Uppercut", "P1_Cast_GroundSlam",
            "P1_Attack_Combo", "P1_Attack_Charge", "P1_Attack_Uppercut",
            "P1_Attack_GroundSlam", "PhaseBreak_Down", "PhaseBreak_Prayer",
            "PhaseBreak_LightningHit", "PhaseBreak_Transform", "Phase2_Start",
            "P2_Cast_EnhancedCombo", "P2_Cast_DoubleCharge", "P2_Cast_ThunderUppercut",
            "P2_Cast_DarkGroundSlam", "P2_Cast_ShadowCombo", "P2_Cast_LightningCast",
            "P2_Cast_DarkWave", "P2_Cast_SoulBind", "P2_Cast_Ultimate",
            "P2_EnhancedCombo", "P2_DoubleCharge", "P2_ThunderUppercut",
            "P2_DarkGroundSlam", "P2_ShadowCombo", "P2_LightningCast",
            "P2_DarkWave", "P2_SoulBind", "P2_Ultimate", "P2_Death"
        };
        HashSet<string> stateNames = new HashSet<string>();
        CollectStateNames(controller.layers[0].stateMachine, stateNames);

        for (int i = 0; i < requiredStates.Length; i++)
        {
            if (!stateNames.Contains(requiredStates[i]))
            {
                throw new InvalidOperationException(
                    $"Animator state is missing: {requiredStates[i]}");
            }
        }
    }

    private static AnimationClip CreateOrUpdateClip(ClipSpec spec, Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            throw new InvalidOperationException($"{spec.SourceSequence} has no validated frames.");
        }

        string path = $"{ClipsRoot}/{spec.ClipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip { name = spec.ClipName };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = spec.FrameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();

        for (int i = 0; i < sprites.Length; i++)
        {
            keys.Add(new ObjectReferenceKeyframe
            {
                time = i / spec.FrameRate,
                value = sprites[i]
            });
        }

        float naturalDuration = sprites.Length / spec.FrameRate;
        float duration = Mathf.Max(naturalDuration, spec.MinimumDuration);

        if (duration > keys[keys.Count - 1].time + 0.001f)
        {
            keys.Add(new ObjectReferenceKeyframe
            {
                time = duration,
                value = sprites[sprites.Length - 1]
            });
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = spec.Loop;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, spec.Events ?? Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static Sprite[] LoadValidatedSpriteSequence(string sequence)
    {
        string folder = $"{FramesRoot}/{sequence}";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            throw new InvalidOperationException($"Validated frame folder is missing: {folder}");
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        Sprite[] sprites = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(sprite => sprite != null)
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new InvalidOperationException($"No imported Sprite was found in {folder}.");
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            string path = AssetDatabase.GetAssetPath(sprites[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null
                || importer.textureType != TextureImporterType.Sprite
                || importer.spritePixelsPerUnit != 32f
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || !importer.alphaIsTransparency)
            {
                throw new InvalidOperationException(
                    $"Sprite importer validation failed: {path}");
            }

            Vector2 pivot = sprites[i].pivot;
            float normalizedPivotX = pivot.x / sprites[i].rect.width;
            float normalizedPivotY = pivot.y / sprites[i].rect.height;

            if (Mathf.Abs(normalizedPivotX - 0.5f) > 0.01f
                || normalizedPivotY > 0.01f)
            {
                throw new InvalidOperationException($"Sprite pivot validation failed: {path}");
            }
        }

        return sprites;
    }

    private static void ValidateGenerationReport()
    {
        if (!File.Exists(ValidationReportPath))
        {
            throw new FileNotFoundException("Boss sprite validation report is missing.", ValidationReportPath);
        }

        string report = File.ReadAllText(ValidationReportPath);

        if (!report.Contains("\"stoppedAnimations\": 0")
            || !report.Contains("\"sourceFilesModified\": false")
            || !report.Contains("\"blackOrDarkBackgroundResidualPixels\": 0")
            || !report.Contains("\"titleResidualPixels\": 0")
            || !report.Contains("\"numberResidualPixels\": 0")
            || !report.Contains("\"gridResidualPixels\": 0"))
        {
            throw new InvalidOperationException(
                "Generated Sprite validation did not pass every residual-pixel rule.");
        }
    }

    private static void WriteBuildReport(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clips)
    {
        string[] lines =
        {
            "# Fighter Boss Integration Build",
            string.Empty,
            $"- Built: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"- Prefab: `{BossPrefabPath}`",
            $"- Scene reference: `{BossScenePath}`",
            $"- Controller: `{AssetDatabase.GetAssetPath(controller)}`",
            $"- {BuildContractMarker}",
            $"- Validated clips: {clips.Count}",
            "- Source controller overwritten: No",
            "- Source PNG modified: No",
            "- Sprite source: validated `_Generated/Sprites` only",
            "- Temporary validated-frame reuse: Hurt and five phase-transition clips",
            "- P2 enhanced clip reuse: four P1 attack clips, separate Animator states",
            "- Validation: Sprite reference, importer, pivot, PPU, loop, curve, prefab wiring, missing script, inactive hitbox"
        };
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Project root was not resolved.");
        string absolutePath = Path.Combine(
            projectRoot,
            ReportsRoot.Replace('/', Path.DirectorySeparatorChar),
            "FighterBossIntegrationBuild.md");
        File.WriteAllLines(absolutePath, lines);
        AssetDatabase.ImportAsset(
            BuildReportPath,
            ImportAssetOptions.ForceUpdate);
    }

    private static void EnsurePrefabBackup()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BackupPrefabPath) != null)
        {
            return;
        }

        EnsureFolder(Path.GetDirectoryName(BackupPrefabPath)?.Replace('\\', '/'));

        if (!AssetDatabase.CopyAsset(BossPrefabPath, BackupPrefabPath))
        {
            throw new InvalidOperationException("Could not create the fighter boss prefab backup.");
        }
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string stateName,
        Motion motion)
    {
        AnimatorState state = machine.AddState(stateName);
        state.motion = motion;
        state.writeDefaultValues = true;
        return state;
    }

    private static void ConfigureTransition(
        AnimatorStateTransition transition,
        bool hasExitTime,
        float duration)
    {
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 1f : 0f;
        transition.hasFixedDuration = true;
        transition.duration = Mathf.Max(0f, duration);
    }

    private static void CollectStateNames(
        AnimatorStateMachine stateMachine,
        HashSet<string> destination)
    {
        ChildAnimatorState[] states = stateMachine.states;

        for (int i = 0; i < states.Length; i++)
        {
            destination.Add(states[i].state.name);
        }

        ChildAnimatorStateMachine[] childMachines = stateMachine.stateMachines;

        for (int i = 0; i < childMachines.Length; i++)
        {
            CollectStateNames(childMachines[i].stateMachine, destination);
        }
    }

    private static ClipSpec Spec(
        string name,
        string sequence,
        float frameRate,
        bool loop,
        float minimumDuration,
        int takeFrames = 0,
        AnimationEvent[] events = null)
    {
        return new ClipSpec
        {
            ClipName = name,
            SourceSequence = sequence,
            FrameRate = frameRate,
            Loop = loop,
            MinimumDuration = minimumDuration,
            TakeFrames = takeFrames,
            Events = events
        };
    }

    private static ClipSpec CastSpec(string name, string sequence, params int[] frameIndices)
    {
        return new ClipSpec
        {
            ClipName = name,
            SourceSequence = sequence,
            FrameRate = 6f,
            Loop = true,
            MinimumDuration = 0f,
            FrameIndices = frameIndices,
            Events = Array.Empty<AnimationEvent>()
        };
    }

    private static PatternSpec Pattern(
        string assetName,
        string stateName,
        string executorKey,
        int number,
        bool phaseOne,
        float cooldown,
        float hpRatio,
        int weight)
    {
        return new PatternSpec
        {
            AssetName = assetName,
            PatternId = assetName,
            StateName = stateName,
            ExecutorKey = executorKey,
            Number = number,
            PhaseOne = phaseOne,
            Cooldown = cooldown,
            HpRatio = hpRatio,
            Weight = weight
        };
    }

    private static string PhaseTwoExecutorKey()
    {
        return "fighter_boss_phase_two";
    }

    private static AnimationEvent[] P2Events(float endTime)
    {
        return Events(
            Event("Anim_AttackStart", 0f),
            Event("Anim_CameraShakeHook", Mathf.Max(0.1f, endTime * 0.55f)),
            Event("Anim_SFXHook", Mathf.Max(0.1f, endTime * 0.55f)),
            Event("Anim_AttackEnd", endTime));
    }

    private static AnimationEvent Event(string functionName, float time, int intParameter = 0)
    {
        return new AnimationEvent
        {
            functionName = functionName,
            time = Mathf.Max(0f, time),
            intParameter = intParameter
        };
    }

    private static AnimationEvent[] Events(params AnimationEvent[] events)
    {
        return events;
    }

    private static T Require<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();

        if (component == null)
        {
            throw new InvalidOperationException(
                $"{root.name} is missing required component {typeof(T).Name}.");
        }

        return component;
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static void SetReferences(
        UnityEngine.Object target,
        params (string propertyName, UnityEngine.Object value)[] values)
    {
        SerializedObject serialized = new SerializedObject(target);

        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty property =
                serialized.FindProperty(values[i].propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{values[i].propertyName} was not found.");
            }

            property.objectReferenceValue = values[i].value;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SetBool(serialized, propertyName, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SetFloat(serialized, propertyName, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetString(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SetString(serialized, propertyName, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetBool(SerializedObject target, string propertyName, bool value)
    {
        RequireProperty(target, propertyName).boolValue = value;
    }

    private static void SetFloat(SerializedObject target, string propertyName, float value)
    {
        RequireProperty(target, propertyName).floatValue = value;
    }

    private static void SetInt(SerializedObject target, string propertyName, int value)
    {
        RequireProperty(target, propertyName).intValue = value;
    }

    private static void SetEnum(SerializedObject target, string propertyName, int value)
    {
        RequireProperty(target, propertyName).enumValueIndex = value;
    }

    private static void SetString(SerializedObject target, string propertyName, string value)
    {
        RequireProperty(target, propertyName).stringValue = value;
    }

    private static SerializedProperty RequireProperty(
        SerializedObject target,
        string propertyName)
    {
        SerializedProperty property = target.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"{target.targetObject.GetType().Name}.{propertyName} was not found.");
        }

        return property;
    }

    private static SerializedProperty RequireRelative(
        SerializedProperty target,
        string propertyName)
    {
        SerializedProperty property = target.FindPropertyRelative(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"{target.propertyPath}.{propertyName} was not found.");
        }

        return property;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }
}
