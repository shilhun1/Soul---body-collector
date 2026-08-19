using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 중간보스 물리 패턴을 Play Mode에서 빠르게 확인하기 위한 Editor 전용 창입니다.
/// 실제 런타임 전투 로직은 HWJ_BossBrainSystem과 HWJ_MidBossPatternSystem이 담당합니다.
/// </summary>
public sealed class HWJ_MidBossPhysicalTestWindow : EditorWindow
{
    private const double PatternSequenceGapSeconds = 0.75d;
    private const string MidBoss1PrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";
    private const string MidBoss1RootPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";
    private const string MidBoss1TypePath =
        "Assets/02Scripts/HWJ/ScriptableObjects/TypeData/Boss/HWJ_MidBoss1_TypeData.asset";
    private const string MidBoss2PrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss2_Runtime_Prefab.prefab";
    private const string MidBoss2RootPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss2_RootObjectData.asset";
    private const string MidBoss2TypePath =
        "Assets/02Scripts/HWJ/ScriptableObjects/TypeData/Boss/HWJ_MidBoss2_TypeData.asset";
    private const string GameplayDatabasePath =
        "Assets/02Scripts/HWJ/ScriptableObjects/Database/HWJ_GameplayDatabase.asset";
    private const string MidBossPatternFolder =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns";
    private const string MidBoss1ObjectId = "boss.mid.01.captain";
    private const string MidBoss2ObjectId = "boss.mid.02.captain";
    private const string MidBossExecutorKey = "mid_boss_physical";
    private const float MidBoss2ExpectedMaxHp = 7500f;
    private const float MidBoss2ExpectedAttackPower = 100f;
    private const float MidBoss2ExpectedDefense = 40f;
    private const float MidBoss2ExpectedMoveSpeed = 15f;
    private const float MidBoss2Pattern1HpLossIntervalRatio = 0.15f;
    private const int MidBoss2Pattern1SummonCount = 4;
    private const float MidBoss2Pattern1WhistleSeconds = 2f;
    private const float MidBoss2Pattern4FixedDamageRatio = 0.4f;
    private const float MidBoss2Pattern4GroggySeconds = 3f;
    private const float MidBoss2Pattern6VanishDelaySeconds = 2f;
    private const string MidBoss2PhaseTwoModelId = "mid_boss_02_phase_2";
    private const string MidBossPatternExecutionCoreId = "boss_pattern_execution";
    private const string MidBossPatternRuleId = "boss_can_use_combat_pattern";

    private static readonly string[] MidBossPhysicalPatternPaths =
    {
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern1.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern2.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern3.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern4.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern5.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern6.asset",
        MidBossPatternFolder + "/HWJ_MidBossPhysical_Pattern7.asset",
    };

    private static readonly MidBossPhysicalPatternSpec[] MidBossPhysicalPatternSpecs =
    {
        new MidBossPhysicalPatternSpec(
            1,
            "mid_boss_physical_pattern_1",
            "mid_boss_whistle_summon",
            HWJ_BossPatternRangeMode.Any,
            true,
            true),
        new MidBossPhysicalPatternSpec(
            2,
            "mid_boss_physical_pattern_2",
            "mid_boss_dash_double_slash",
            HWJ_BossPatternRangeMode.Close,
            true,
            false),
        new MidBossPhysicalPatternSpec(
            3,
            "mid_boss_physical_pattern_3",
            "mid_boss_slash_full_wave",
            HWJ_BossPatternRangeMode.Any,
            true,
            false),
        new MidBossPhysicalPatternSpec(
            4,
            "mid_boss_physical_pattern_4",
            "mid_boss_fixed_damage_dash",
            HWJ_BossPatternRangeMode.Any,
            true,
            false),
        new MidBossPhysicalPatternSpec(
            5,
            "mid_boss_physical_pattern_5",
            "mid_boss_red_sword_combo",
            HWJ_BossPatternRangeMode.Close,
            false,
            true),
        new MidBossPhysicalPatternSpec(
            6,
            "mid_boss_physical_pattern_6",
            "mid_boss_vanish_backstab",
            HWJ_BossPatternRangeMode.Any,
            false,
            true),
        new MidBossPhysicalPatternSpec(
            7,
            "mid_boss_physical_pattern_7",
            "mid_boss_jump_slam",
            HWJ_BossPatternRangeMode.Any,
            false,
            true),
    };

    private static readonly string[] MidBoss2FighterResidueChildNames =
    {
        "ComboHitbox",
        "ChargeShoulderHitbox",
        "UppercutHitbox",
        "GroundSlamHitbox",
        "P2_EnhancedCombo_Hitbox",
        "P2_DoubleCharge_Hitbox",
        "P2_ThunderUppercut_Hitbox",
        "P2_DarkGroundSlam_Hitbox",
        "P2_ShadowCombo_Hitbox",
        "P2_LightningCast_Hitbox",
        "P2_DarkWave_Hitbox",
        "P2_SoulBind_Hitbox",
        "P2_Ultimate_Hitbox",
        "TEMP_TELEGRAPH_P1_Combo",
        "TEMP_TELEGRAPH_P1_Charge",
        "TEMP_TELEGRAPH_P1_Uppercut",
        "TEMP_TELEGRAPH_P1_GroundSlam",
        "TEMP_TELEGRAPH_P2_EnhancedCombo",
        "TEMP_TELEGRAPH_P2_DoubleCharge",
        "TEMP_TELEGRAPH_P2_ThunderUppercut",
        "TEMP_TELEGRAPH_P2_DarkGroundSlam",
        "TEMP_TELEGRAPH_P2_ShadowCombo",
        "TEMP_TELEGRAPH_P2_LightningCast",
        "TEMP_TELEGRAPH_P2_DarkWave",
        "TEMP_TELEGRAPH_P2_SoulBind",
        "TEMP_TELEGRAPH_P2_Ultimate",
    };

    private static readonly string[] MidBoss2PlayModeChecklistItems =
    {
        "중간보스2 에셋 + 현재 씬 플로우 검증 성공 메시지를 Console에서 확인했다.",
        "Play Mode에서 씬의 중간보스2 찾기 버튼으로 boss.mid.02.captain 보스를 찾았다.",
        "타겟 자동 연결 후 타겟 이름과 타겟 보스방 내부 값이 정상으로 표시된다.",
        "전투 시작 후 FSM이 전투 상태로 진입하고 AI 활성화 상태를 확인했다.",
        "패턴 1: 소환을 실행해 빙의 가능 몬스터 4마리가 보스 위치 기준으로 생성되는지 확인했다.",
        "패턴 2, 3, 4를 1페이즈에서 실행해 예고 방향, 이동, 피해 판정이 따라오지 않는지 확인했다.",
        "HP 50%로 설정 후 10초 전환과 2페이즈 진입을 확인했다.",
        "패턴 5, 6, 7을 2페이즈에서 실행해 예고, 이동, 피해 판정을 확인했다.",
        "패턴 물리 오버라이드 중 현재 중력이 0으로 바뀌고 종료 후 복구 예정 중력으로 돌아오는지 확인했다.",
        "HP 1로 설정 후 처치했을 때 BossFlowSystem을 통해 스테이지 진행/보상 흐름으로 넘어가는지 확인했다.",
        "보상 지급 준비 검사 성공 로그를 확인했다.",
        "보상 직접 지급 테스트 성공 또는 AlreadyClaimed 중복 차단 로그를 확인했다.",
    };

    private static readonly string[] MidBoss2PatternVerificationItems =
    {
        "패턴 1 - 먼 맵 끝 이동, 2초 호루라기, 빙의 가능 몬스터 4마리 소환",
        "패턴 2 - 플레이어 방향 대쉬, 가로 베기, 0.5초 간격 세로 베기",
        "패턴 3 - 돌진 가로 베기, 먼 사이드 이동, 1.5초 차징, 전범위 검기",
        "패턴 4 - 조준 후 빠른 돌진, 최대 체력 40% 고정 피해, 3초 그로기",
        "패턴 5 - 붉은 검 연출, 2페이즈 4연속 공격, 마지막 충격파",
        "패턴 6 - 2초 대기, 은신, 플레이어 뒤 순간이동, 복귀 후 검기",
        "패턴 7 - 높게 상승, 플레이어 위치 낙하, 반맵 충격파",
    };

    private GameObject midBossPrefabAsset;
    private HWJ_BossBrainSystem runtimeBoss;
    private Vector2 scrollPosition;
    private bool showPlayModeChecklist = true;
    private bool showPatternVerificationChecklist = true;
    private readonly bool[] playModeChecklistStates = new bool[MidBoss2PlayModeChecklistItems.Length];
    private readonly bool[] patternVerificationStates = new bool[MidBoss2PatternVerificationItems.Length];
    private int lastManualPhysicalPatternNumber;
    private bool isPatternSequenceRunning;
    private int nextSequencePatternNumber = 1;
    private int runningSequencePatternNumber;
    private double nextPatternSequenceTime;
    private double nextCorpseDebugRefreshTime;
    private bool cachedHasPossessableCorpse;
    private int cachedCorpseDebugBossInstanceId;

    private readonly struct MidBossPhysicalPatternSpec
    {
        public readonly int PatternNumber;
        public readonly string PatternId;
        public readonly string AnimationId;
        public readonly HWJ_BossPatternRangeMode RangeMode;
        public readonly bool UsableInPhase1;
        public readonly bool UsableInPhase2;

        public MidBossPhysicalPatternSpec(
            int patternNumber,
            string patternId,
            string animationId,
            HWJ_BossPatternRangeMode rangeMode,
            bool usableInPhase1,
            bool usableInPhase2)
        {
            PatternNumber = patternNumber;
            PatternId = patternId;
            AnimationId = animationId;
            RangeMode = rangeMode;
            UsableInPhase1 = usableInPhase1;
            UsableInPhase2 = usableInPhase2;
        }
    }

    [MenuItem("Tools/HWJ/Boss/Mid Boss Physical Test Window")]
    public static void Open()
    {
        GetWindow<HWJ_MidBossPhysicalTestWindow>("Mid Boss Physical Test");
    }

    [MenuItem("Tools/HWJ/Boss/Validate Mid Boss 2 Assets")]
    public static void ValidateMidBoss2Assets()
    {
        List<string> errors = new List<string>();
        CollectMidBoss2ValidationErrors(errors);

        if (errors.Count == 0)
        {
            Debug.Log("[HWJ] 중간보스2 에셋 검증 성공: RootData, TypeData, Database, Prefab, 물리 패턴 1~7 연결이 정상입니다.");
            return;
        }

        Debug.LogError("[HWJ] 중간보스2 에셋 검증 실패:\n- " + string.Join("\n- ", errors));
    }

    public static void RunMidBoss2AssetValidationBatch()
    {
        List<string> errors = new List<string>();
        CollectMidBoss2ValidationErrors(errors);
        ThrowIfValidationFailed("중간보스2", errors);
        Debug.Log("[HWJ] 중간보스2 배치 검증 성공: 에셋과 프리팹 연결이 정상입니다.");
    }

    [MenuItem("Tools/HWJ/Boss/Validate All Mid Boss Assets")]
    public static void ValidateAllMidBossAssets()
    {
        List<string> errors = new List<string>();
        CollectMidBoss1ValidationErrors(errors);
        CollectMidBoss2ValidationErrors(errors);

        if (errors.Count == 0)
        {
            Debug.Log("[HWJ] 중간보스 전체 에셋 검증 성공: 중간보스1 격투가형, 중간보스2 물리형 연결이 정상입니다.");
            return;
        }

        Debug.LogError("[HWJ] 중간보스 전체 에셋 검증 실패:\n- " + string.Join("\n- ", errors));
    }

    public static void RunAllMidBossAssetValidationBatch()
    {
        List<string> errors = new List<string>();
        CollectMidBoss1ValidationErrors(errors);
        CollectMidBoss2ValidationErrors(errors);
        ThrowIfValidationFailed("중간보스 전체", errors);
        Debug.Log("[HWJ] 중간보스 전체 배치 검증 성공: 중간보스1/2 에셋과 프리팹 연결이 정상입니다.");
    }

    [MenuItem("Tools/HWJ/Boss/Validate Active Scene Mid Boss 2 Flow")]
    public static void ValidateActiveSceneMidBoss2Flow()
    {
        List<string> errors = new List<string>();
        CollectActiveSceneMidBoss2FlowValidationErrors(errors);

        if (errors.Count == 0)
        {
            Debug.Log("[HWJ] 현재 씬 중간보스2 플로우 검증 성공: BossFlowSystem이 보스 사망을 스테이지 진행으로 넘길 수 있습니다.");
            return;
        }

        Debug.LogError("[HWJ] 현재 씬 중간보스2 플로우 검증 실패:\n- " + string.Join("\n- ", errors));
    }

    public static void RunActiveSceneMidBoss2FlowValidationBatch()
    {
        List<string> errors = new List<string>();
        CollectActiveSceneMidBoss2FlowValidationErrors(errors);
        ThrowIfValidationFailed("현재 씬 중간보스2 플로우", errors);
        Debug.Log("[HWJ] 현재 씬 중간보스2 플로우 배치 검증 성공입니다.");
    }

    [MenuItem("Tools/HWJ/Boss/Validate Mid Boss 2 Setup And Active Scene")]
    public static void ValidateMidBoss2SetupAndActiveScene()
    {
        List<string> errors = new List<string>();
        CollectMidBoss2ValidationErrors(errors);
        CollectActiveSceneMidBoss2FlowValidationErrors(errors);

        if (errors.Count == 0)
        {
            Debug.Log("[HWJ] 중간보스2 에셋 + 현재 씬 플로우 검증 성공: Play Mode 패턴 확인 전 기본 배치 조건이 정상입니다.");
            return;
        }

        Debug.LogError("[HWJ] 중간보스2 에셋 + 현재 씬 플로우 검증 실패:\n- " + string.Join("\n- ", errors));
    }

    public static void RunMidBoss2SetupAndActiveSceneValidationBatch()
    {
        List<string> errors = new List<string>();
        CollectMidBoss2ValidationErrors(errors);
        CollectActiveSceneMidBoss2FlowValidationErrors(errors);
        ThrowIfValidationFailed("중간보스2 에셋 + 현재 씬 플로우", errors);
        Debug.Log("[HWJ] 중간보스2 에셋 + 현재 씬 플로우 배치 검증 성공입니다.");
    }

    [MenuItem("Tools/HWJ/Boss/Clean Mid Boss 2 Fighter Residue")]
    public static void CleanMidBoss2FighterResidue()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MidBoss2PrefabPath);
        List<string> removedItems = new List<string>();

        try
        {
            RemoveRootComponentIfPresent<HWJ_FighterBossComboSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossChargeSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossUppercutSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossGroundSlamSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossPhaseTwoPatternSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossDeathSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossAnimatorSystem>(prefabRoot, removedItems);
            RemoveRootComponentIfPresent<HWJ_FighterBossAnimationEvents>(prefabRoot, removedItems);
            RemoveChildComponentsIfPresent<HWJ_FighterBossHitboxSystem>(prefabRoot, removedItems);
            RemoveChildComponentsIfPresent<HWJ_FighterBossHealthBarSystem>(prefabRoot, removedItems);
            RemoveNamedChildObjects(prefabRoot, MidBoss2FighterResidueChildNames, removedItems);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MidBoss2PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log(removedItems.Count == 0
            ? "[HWJ] 중간보스2 프리팹에서 제거할 FighterBoss 잔여물이 없습니다."
            : "[HWJ] 중간보스2 FighterBoss 잔여물 정리 완료:\n- " + string.Join("\n- ", removedItems));
    }

    private void OnEnable()
    {
        midBossPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MidBoss2PrefabPath);
        FindRuntimeMidBoss();
    }

    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying && runtimeBoss == null)
        {
            FindRuntimeMidBoss();
        }

        TickPatternSequenceRunner();
        Repaint();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("중간보스 물리 패턴 테스트", EditorStyles.boldLabel);
        midBossPrefabAsset = (GameObject)EditorGUILayout.ObjectField(
            "중간보스2 프리팹",
            midBossPrefabAsset,
            typeof(GameObject),
            false);
        runtimeBoss = (HWJ_BossBrainSystem)EditorGUILayout.ObjectField(
            "실행 중 보스",
            runtimeBoss,
            typeof(HWJ_BossBrainSystem),
            true);

        if (GUILayout.Button("씬의 물리 중간보스 찾기"))
        {
            FindRuntimeMidBoss();
        }

        if (GUILayout.Button("씬의 중간보스2 찾기"))
        {
            FindRuntimeBossByObjectId(MidBoss2ObjectId);
        }

        if (GUILayout.Button("중간보스2 에셋 검증"))
        {
            ValidateMidBoss2Assets();
        }

        if (GUILayout.Button("중간보스 전체 에셋 검증"))
        {
            ValidateAllMidBossAssets();
        }

        if (GUILayout.Button("현재 씬 중간보스2 플로우 검증"))
        {
            ValidateActiveSceneMidBoss2Flow();
        }

        if (GUILayout.Button("중간보스2 에셋 + 현재 씬 플로우 검증"))
        {
            ValidateMidBoss2SetupAndActiveScene();
        }

        if (GUILayout.Button("중간보스2 Play Mode 빠른 테스트 준비"))
        {
            PrepareMidBoss2PlayModeTest();
        }

        DrawPlayModeStepGuide();
        DrawPlayModeChecklist();
        DrawRuntimeStatus();
        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || runtimeBoss == null))
        {
            DrawControlButtons();
            EditorGUILayout.Space(8f);
            DrawPatternButtons();
            DrawPatternVerificationChecklist();
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "패턴 강제 실행은 Play Mode에서만 사용할 수 있습니다.",
                MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawPlayModeStepGuide()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "권장 테스트 순서:\n" +
            "1. Play Mode 시작\n" +
            "2. 씬의 중간보스2 찾기\n" +
            "3. 중간보스2 에셋 + 현재 씬 플로우 검증\n" +
            "4. 중간보스2 Play Mode 빠른 테스트 준비\n" +
            "5. 패턴 1~7 순차 실행 또는 패턴별 수동 실행\n" +
            "6. 50% 페이즈 전환 테스트 준비\n" +
            "7. 2페이즈 AI 패턴 1회 실행\n" +
            "8. HP 0 사망 이벤트 테스트\n" +
            "9. 보상 지급 준비 검사\n" +
            "10. 최종 확인 리포트 출력",
            MessageType.Info);
        EditorGUILayout.HelpBox(
            "주의: 보상 직접 지급 테스트와 BossFlow 직접 완료 테스트는 실제 Play Mode 상태를 변경합니다. " +
            "최종 확인 전에 필요한 경우에만 한 번 실행하세요.",
            MessageType.Warning);
    }

    private void DrawPlayModeChecklist()
    {
        EditorGUILayout.Space(8f);
        showPlayModeChecklist = EditorGUILayout.Foldout(
            showPlayModeChecklist,
            "중간보스2 Play Mode 최종 확인 체크리스트",
            true);

        if (!showPlayModeChecklist)
        {
            return;
        }

        int completedCount = 0;

        for (int i = 0; i < playModeChecklistStates.Length; i++)
        {
            playModeChecklistStates[i] = EditorGUILayout.ToggleLeft(
                $"{i + 1}. {MidBoss2PlayModeChecklistItems[i]}",
                playModeChecklistStates[i]);

            if (playModeChecklistStates[i])
            {
                completedCount++;
            }
        }

        EditorGUILayout.LabelField(
            "확인 진행도",
            $"{completedCount} / {MidBoss2PlayModeChecklistItems.Length}");

        DrawTwoButtons(
            "최종 확인 리포트 출력",
            LogFinalVerificationReport,
            "Play Mode 체크리스트 초기화",
            ResetPlayModeChecklist);
    }

    private void ResetPlayModeChecklist()
    {
        for (int i = 0; i < playModeChecklistStates.Length; i++)
        {
            playModeChecklistStates[i] = false;
        }
    }

    private void LogFinalVerificationReport()
    {
        List<string> assetErrors = new List<string>();
        List<string> sceneErrors = new List<string>();
        CollectMidBoss2ValidationErrors(assetErrors);
        CollectActiveSceneMidBoss2FlowValidationErrors(sceneErrors);

        int playModeCompletedCount = CountCheckedItems(playModeChecklistStates);
        int patternCompletedCount = CountCheckedItems(patternVerificationStates);
        bool playModeChecklistComplete =
            playModeCompletedCount == MidBoss2PlayModeChecklistItems.Length;
        bool patternChecklistComplete =
            patternCompletedCount == MidBoss2PatternVerificationItems.Length;
        bool finalVerificationComplete = assetErrors.Count == 0
            && sceneErrors.Count == 0
            && playModeChecklistComplete
            && patternChecklistComplete;

        StringBuilder builder = new StringBuilder(2048);
        builder.AppendLine("[HWJ] 중간보스2 최종 확인 리포트");
        builder.AppendLine($"- 에셋 검증: {(assetErrors.Count == 0 ? "통과" : $"실패 {assetErrors.Count}개")}");
        builder.AppendLine($"- 현재 씬 BossFlow 검증: {(sceneErrors.Count == 0 ? "통과" : $"실패 {sceneErrors.Count}개")}");
        builder.AppendLine($"- Play Mode 체크리스트: {playModeCompletedCount} / {MidBoss2PlayModeChecklistItems.Length}");
        builder.AppendLine($"- 패턴 수동 확인 기록: {patternCompletedCount} / {MidBoss2PatternVerificationItems.Length}");

        AppendErrorSection(builder, "에셋 검증 오류", assetErrors);
        AppendErrorSection(builder, "현재 씬 BossFlow 검증 오류", sceneErrors);
        AppendMissingChecklistSection(
            builder,
            "미확인 Play Mode 항목",
            MidBoss2PlayModeChecklistItems,
            playModeChecklistStates);
        AppendMissingChecklistSection(
            builder,
            "미확인 패턴 항목",
            MidBoss2PatternVerificationItems,
            patternVerificationStates);

        builder.Append("판정: ");
        builder.AppendLine(finalVerificationComplete
            ? "애니메이션, 이펙트, 사운드를 제외한 중간보스2 검증 항목이 모두 확인됐습니다."
            : "아직 완료 확정 불가입니다. 위 미확인 항목을 먼저 확인해야 합니다.");

        string report = builder.ToString();

        if (assetErrors.Count > 0 || sceneErrors.Count > 0)
        {
            Debug.LogError(report);
            return;
        }

        if (!finalVerificationComplete)
        {
            Debug.LogWarning(report);
            return;
        }

        Debug.Log(report);
    }

    private static int CountCheckedItems(bool[] checkedStates)
    {
        int count = 0;

        for (int i = 0; i < checkedStates.Length; i++)
        {
            if (checkedStates[i])
            {
                count++;
            }
        }

        return count;
    }

    private static void AppendErrorSection(
        StringBuilder builder,
        string sectionTitle,
        List<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        builder.AppendLine(sectionTitle + ":");

        for (int i = 0; i < errors.Count; i++)
        {
            builder.AppendLine($"  - {errors[i]}");
        }
    }

    private static void AppendMissingChecklistSection(
        StringBuilder builder,
        string sectionTitle,
        string[] itemLabels,
        bool[] checkedStates)
    {
        bool hasMissingItem = false;

        for (int i = 0; i < itemLabels.Length; i++)
        {
            if (checkedStates[i])
            {
                continue;
            }

            if (!hasMissingItem)
            {
                builder.AppendLine(sectionTitle + ":");
                hasMissingItem = true;
            }

            builder.AppendLine($"  - {i + 1}. {itemLabels[i]}");
        }
    }

    private void DrawRuntimeStatus()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);

        if (runtimeBoss == null)
        {
            EditorGUILayout.LabelField("보스 이름", "-");
            EditorGUILayout.LabelField("보스 ID", "-");
            EditorGUILayout.LabelField("FSM", "-");
            EditorGUILayout.LabelField("페이즈", "-");
            EditorGUILayout.LabelField("HP", "-");
            EditorGUILayout.LabelField("AI", "-");
            EditorGUILayout.LabelField("타겟", "-");
            EditorGUILayout.LabelField("타겟 X 거리", "-");
            EditorGUILayout.LabelField("공격 시작 거리", "-");
            EditorGUILayout.LabelField("근거리 판정", "-");
            EditorGUILayout.LabelField("최적 거리", "-");
            EditorGUILayout.LabelField("타겟 보스방 내부", "-");
            EditorGUILayout.LabelField("패턴 실행 중", "-");
            EditorGUILayout.LabelField("특수 패턴 실행 중", "-");
            EditorGUILayout.LabelField("패턴 1 충전", "-");
            EditorGUILayout.LabelField("패턴 1 다음 HP 기준", "-");
            EditorGUILayout.LabelField("빙의 가능 시체 감지", "-");
            EditorGUILayout.LabelField("패턴 물리 오버라이드", "-");
            EditorGUILayout.LabelField("Rigidbody2D", "-");
            EditorGUILayout.LabelField("현재 중력", "-");
            EditorGUILayout.LabelField("복구 예정 중력", "-");
            EditorGUILayout.LabelField("최근 실행 패턴", "-");
            EditorGUILayout.LabelField("최근 패턴 기록", "-");
            EditorGUILayout.LabelField("패턴 선택 결과", "-");
            EditorGUILayout.LabelField("패턴 실행 결과", "-");
            EditorGUILayout.LabelField("패턴 규칙 결과", "-");
            return;
        }

        HWJ_RuntimeStatusSystem runtimeStatus = runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_BossPatternSystem bossPattern = runtimeBoss.GetComponent<HWJ_BossPatternSystem>();
        HWJ_MidBossPatternSystem midBossPattern = runtimeBoss.GetComponent<HWJ_MidBossPatternSystem>();
        HWJ_RootObjectDataResolver dataResolver = runtimeBoss.GetComponent<HWJ_RootObjectDataResolver>();
        Rigidbody2D rigidbody2D = runtimeBoss.GetComponent<Rigidbody2D>();
        HWJ_BossTypeDataSO bossData = null;

        if (dataResolver != null)
        {
            dataResolver.TryGetTypeData(out bossData);
        }

        Transform target = runtimeBoss.Target;
        float distanceX = target != null ? Mathf.Abs(target.position.x - runtimeBoss.transform.position.x) : 0f;
        float attackStartRange = bossData != null ? ResolveAttackStartRange(bossData) : 0f;
        float closeSkillRange = bossData != null ? Mathf.Max(0.1f, bossData.FSM.closeSkillRange) : 0f;
        float optimalDistance = bossData != null ? ResolveOptimalDistance(bossData) : 0f;
        bool isCloseRange = target != null && bossData != null && distanceX <= closeSkillRange;
        bool isInsideBossRoom = target != null && IsInsideRect(target.position, runtimeBoss.BossRoomCenter, runtimeBoss.BossRoomSize);

        EditorGUILayout.LabelField("보스 이름", runtimeBoss.name);
        EditorGUILayout.LabelField(
            "보스 ID",
            dataResolver != null && dataResolver.Identity != null
                ? dataResolver.Identity.objectId
                : "-");
        EditorGUILayout.LabelField("FSM", runtimeBoss.CurrentState.ToString());
        EditorGUILayout.LabelField("페이즈", runtimeBoss.CurrentPhaseNumber.ToString());
        EditorGUILayout.LabelField(
            "HP",
            runtimeStatus != null ? $"{runtimeStatus.CurrentHp:0.##} / {runtimeStatus.MaxHp:0.##}" : "-");
        EditorGUILayout.LabelField("AI", runtimeBoss.AIEnabled ? "On" : "Off");
        EditorGUILayout.LabelField("타겟", target != null ? target.name : "-");
        EditorGUILayout.LabelField("타겟 X 거리", target != null ? $"{distanceX:0.##}" : "-");
        EditorGUILayout.LabelField("공격 시작 거리", bossData != null ? $"{attackStartRange:0.##}" : "-");
        EditorGUILayout.LabelField("근거리 판정", bossData != null ? FormatYesNo(isCloseRange) : "-");
        EditorGUILayout.LabelField("최적 거리", bossData != null ? $"{optimalDistance:0.##}" : "-");
        EditorGUILayout.LabelField("타겟 보스방 내부", target != null ? FormatYesNo(isInsideBossRoom) : "-");
        EditorGUILayout.LabelField(
            "패턴 실행 중",
            midBossPattern != null && midBossPattern.IsPatternRunning ? "Yes" : "No");
        EditorGUILayout.LabelField(
            "특수 패턴 실행 중",
            bossPattern != null && bossPattern.IsSpecialPatternRunning ? "Yes" : "No");
        EditorGUILayout.LabelField(
            "패턴 1 충전",
            midBossPattern != null ? midBossPattern.PendingPattern1Charges.ToString() : "-");
        EditorGUILayout.LabelField(
            "패턴 1 다음 HP 기준",
            midBossPattern != null ? $"{midBossPattern.NextPattern1HpRatioThreshold * 100f:0.#}%" : "-");
        EditorGUILayout.LabelField(
            "빙의 가능 시체 감지",
            FormatPossessableCorpseStatus(midBossPattern));
        EditorGUILayout.LabelField(
            "패턴 물리 오버라이드",
            midBossPattern != null ? FormatYesNo(midBossPattern.IsPatternPhysicsOverridden) : "-");
        EditorGUILayout.LabelField(
            "Rigidbody2D",
            rigidbody2D != null
                ? $"{rigidbody2D.bodyType}, Simulated={FormatYesNo(rigidbody2D.simulated)}, Constraints={rigidbody2D.constraints}"
                : "-");
        EditorGUILayout.LabelField(
            "현재 중력",
            midBossPattern != null ? $"{midBossPattern.CurrentPatternGravityScale:0.###}" : "-");
        EditorGUILayout.LabelField(
            "복구 예정 중력",
            midBossPattern != null ? $"{midBossPattern.CachedPatternGravityScale:0.###}" : "-");
        EditorGUILayout.LabelField(
            "최근 실행 패턴",
            bossPattern != null && !string.IsNullOrEmpty(bossPattern.LastExecutedPatternKey)
                ? bossPattern.LastExecutedPatternKey
                : "-");
        EditorGUILayout.LabelField(
            "최근 패턴 기록",
            FormatRecentPatternKeys(bossPattern));
        EditorGUILayout.LabelField(
            "패턴 선택 결과",
            bossPattern != null && !string.IsNullOrEmpty(bossPattern.LastPatternSelectionResult)
                ? bossPattern.LastPatternSelectionResult
                : "-");
        EditorGUILayout.LabelField(
            "패턴 실행 결과",
            bossPattern != null && !string.IsNullOrEmpty(bossPattern.LastPatternExecutionResult)
                ? bossPattern.LastPatternExecutionResult
                : "-");
        EditorGUILayout.LabelField(
            "패턴 규칙 결과",
            bossPattern != null && !string.IsNullOrEmpty(bossPattern.LastPatternResult)
                ? bossPattern.LastPatternResult
                : "-");
    }

    private static float ResolveAttackStartRange(HWJ_BossTypeDataSO bossData)
    {
        if (bossData == null)
        {
            return 0f;
        }

        if (bossData.FSM.attackStartRange > 0f)
        {
            return bossData.FSM.attackStartRange;
        }

        return Mathf.Max(1f, bossData.Navigation.stoppingDistance);
    }

    private static float ResolveOptimalDistance(HWJ_BossTypeDataSO bossData)
    {
        if (bossData == null)
        {
            return 0f;
        }

        return bossData.FSM.optimalAttackDistance > 0f
            ? bossData.FSM.optimalAttackDistance
            : ResolveAttackStartRange(bossData);
    }

    private static bool IsInsideRect(Vector3 position, Vector2 center, Vector2 size)
    {
        Vector2 halfSize = size * 0.5f;
        return Mathf.Abs(position.x - center.x) <= halfSize.x
            && Mathf.Abs(position.y - center.y) <= halfSize.y;
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private string FormatPossessableCorpseStatus(HWJ_MidBossPatternSystem midBossPattern)
    {
        if (midBossPattern == null)
        {
            return "-";
        }

        int bossInstanceId = midBossPattern.GetInstanceID();
        double now = EditorApplication.timeSinceStartup;

        if (cachedCorpseDebugBossInstanceId != bossInstanceId || now >= nextCorpseDebugRefreshTime)
        {
            cachedCorpseDebugBossInstanceId = bossInstanceId;
            cachedHasPossessableCorpse = midBossPattern.HasPossessableCorpseInBossRoomForDebug();
            nextCorpseDebugRefreshTime = now + 0.25d;
        }

        return FormatYesNo(cachedHasPossessableCorpse);
    }

    private bool TryResolveRuntimeBossData(out HWJ_BossTypeDataSO bossData)
    {
        bossData = null;

        if (runtimeBoss == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver dataResolver =
            runtimeBoss.GetComponent<HWJ_RootObjectDataResolver>();

        return dataResolver != null && dataResolver.TryGetTypeData(out bossData);
    }

    private void DrawControlButtons()
    {
        EditorGUILayout.LabelField("전투 제어", EditorStyles.boldLabel);

        bool nextAiState = EditorGUILayout.Toggle("AI 활성화", runtimeBoss.AIEnabled);

        if (nextAiState != runtimeBoss.AIEnabled)
        {
            runtimeBoss.SetAIEnabled(nextAiState);
        }

        DrawTwoButtons(
            "타겟 자동 연결",
            () => ResolveTarget(),
            "AI 패턴 1회 실행",
            TryRunAiPatternOnce);
        DrawTwoButtons(
            "전투 시작",
            () => runtimeBoss.StartBossEncounter(),
            "현재 패턴 취소",
            CancelCurrentPattern);
        DrawTwoButtons(
            "HP 50%로 설정",
            () => SetHealthRatio(0.5f),
            "HP 1로 설정",
            () => SetHealthValue(1f));
        DrawTwoButtons(
            "50% 페이즈 전환 테스트 준비",
            PreparePhaseTransitionTest,
            "2페이즈 AI 패턴 1회 실행",
            TryRunPhaseTwoAiPatternOnce);
        DrawTwoButtons(
            "HP 풀회복",
            () => runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>()?.ResetForEncounter(true),
            "그로기 3초",
            () => runtimeBoss.ForceGroggy(3f));
        DrawTwoButtons(
            "HP 0 사망 이벤트 테스트",
            RunBossDeathEventBridgeTest,
            "BossFlow 직접 완료 테스트",
            RunBossFlowDirectDefeatTest);
        DrawTwoButtons(
            "보상 지급 준비 검사",
            ValidateBossRewardReadiness,
            "보상 직접 지급 테스트",
            RunBossRewardGrantTest);

        if (GUILayout.Button("패턴 기록과 쿨타임 초기화"))
        {
            runtimeBoss.GetComponent<HWJ_BossPatternSystem>()?.ResetPatternHistory();
        }

        if (GUILayout.Button("중간보스2 Play Mode 준비 상태 검사"))
        {
            ValidateMidBoss2RuntimeReadiness();
        }
    }

    private void TryRunAiPatternOnce()
    {
        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] 실행 중인 중간보스를 찾지 못했습니다.");
            return;
        }

        if (runtimeBoss.CurrentState == HWJ_BossFSMState.PhaseTransition
            || runtimeBoss.CurrentState == HWJ_BossFSMState.Groggy
            || runtimeBoss.CurrentState == HWJ_BossFSMState.Dead)
        {
            Debug.LogWarning($"[HWJ] 현재 보스 상태({runtimeBoss.CurrentState})에서는 AI 패턴을 실행하지 않습니다.");
            return;
        }

        Transform target = ResolveTarget();

        if (target == null)
        {
            Debug.LogWarning("[HWJ] AI 패턴 실행 실패: 타겟 플레이어를 찾지 못했습니다.");
            return;
        }

        HWJ_BossPatternSystem bossPattern = runtimeBoss.GetComponent<HWJ_BossPatternSystem>();

        if (bossPattern == null)
        {
            Debug.LogWarning("[HWJ] AI 패턴 실행 실패: HWJ_BossPatternSystem이 없습니다.");
            return;
        }

        if (!TryResolveRuntimeBossData(out HWJ_BossTypeDataSO bossData))
        {
            Debug.LogWarning("[HWJ] AI 패턴 실행 실패: Boss TypeData를 읽지 못했습니다.");
            return;
        }

        float distanceX = Mathf.Abs(target.position.x - runtimeBoss.transform.position.x);
        float attackStartRange = ResolveAttackStartRange(bossData);
        float closeSkillRange = Mathf.Max(0.1f, bossData.FSM.closeSkillRange);
        bool isCloseRange = distanceX <= closeSkillRange;

        if (distanceX > attackStartRange)
        {
            Debug.LogWarning(
                $"[HWJ] AI 기준으로는 아직 패턴을 시도하지 않습니다. " +
                $"타겟 X 거리={distanceX:0.##}, 공격 시작 거리={attackStartRange:0.##}");
            return;
        }

        if (!bossPattern.TryUseAvailablePattern(target, runtimeBoss.CurrentPhaseNumber, isCloseRange))
        {
            Debug.LogWarning(
                $"[HWJ] AI 패턴 선택 실패. 페이즈={runtimeBoss.CurrentPhaseNumber}, " +
                $"근거리={FormatYesNo(isCloseRange)}, 선택 결과={bossPattern.LastPatternSelectionResult}, " +
                $"실행 결과={bossPattern.LastPatternExecutionResult}, 규칙 결과={bossPattern.LastPatternResult}");
            return;
        }

        Debug.Log(
            $"[HWJ] AI 패턴 1회 실행 성공: {bossPattern.LastExecutedPatternKey}, " +
            $"페이즈={runtimeBoss.CurrentPhaseNumber}, 근거리={FormatYesNo(isCloseRange)}, " +
            $"실행 결과={bossPattern.LastPatternExecutionResult}");
    }

    private void PreparePhaseTransitionTest()
    {
        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] 페이즈 전환 테스트 실패: 실행 중인 중간보스를 찾지 못했습니다.");
            return;
        }

        CancelCurrentPattern();
        ResolveTarget();
        runtimeBoss.SetAIEnabled(false);
        runtimeBoss.GetComponent<HWJ_BossPatternSystem>()?.ResetPatternHistory();
        runtimeBoss.StartBossEncounter();
        SetHealthRatio(0.49f);

        Debug.Log(
            "[HWJ] 중간보스2 50% 페이즈 전환 테스트 준비 완료: " +
            "HP를 49%로 낮췄습니다. 다음 Update에서 보스가 중앙 이동, 대사, 무적, 10초 전환 루트로 진입하는지 확인하세요.");
    }

    private void TryRunPhaseTwoAiPatternOnce()
    {
        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] 2페이즈 AI 패턴 실행 실패: 실행 중인 중간보스를 찾지 못했습니다.");
            return;
        }

        if (runtimeBoss.CurrentPhaseNumber != 2)
        {
            Debug.LogWarning($"[HWJ] 현재 보스 페이즈가 2가 아닙니다. 현재 페이즈={runtimeBoss.CurrentPhaseNumber}");
            return;
        }

        TryRunAiPatternOnce();
    }

    private void RunBossDeathEventBridgeTest()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HWJ] HP 0 사망 이벤트 테스트는 Play Mode에서만 실행할 수 있습니다.");
            return;
        }

        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] HP 0 사망 이벤트 테스트 실패: 먼저 씬의 중간보스2를 찾아야 합니다.");
            return;
        }

        HWJ_RuntimeStatusSystem runtimeStatus =
            runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus == null)
        {
            Debug.LogWarning("[HWJ] HP 0 사망 이벤트 테스트 실패: 중간보스2에 RuntimeStatusSystem이 없습니다.");
            return;
        }

        HWJ_BossFlowSystem bossFlow = FindRuntimeMidBoss2BossFlow(runtimeBoss);

        if (bossFlow == null)
        {
            Debug.LogWarning("[HWJ] HP 0 사망 이벤트 테스트 실패: 현재 씬에서 중간보스2를 참조하는 BossFlowSystem을 찾지 못했습니다.");
            return;
        }

        CancelCurrentPattern();
        runtimeStatus.ResetForEncounter(true);
        runtimeBoss.StartBossEncounter();
        runtimeStatus.SetCurrentHpForDebug(1f);
        runtimeStatus.ApplyDamage(Mathf.Max(1f, runtimeStatus.MaxHp + 1f), runtimeBoss, null);

        Debug.Log(
            "[HWJ] HP 0 사망 이벤트 테스트 실행 완료: " +
            $"보스 사망={FormatYesNo(runtimeStatus.IsDead)}, " +
            $"BossFlow 메시지={bossFlow.LastCombatDeathBridgeMessage}, " +
            $"마지막 플로우={bossFlow.LastFlowMessage}");
    }

    private void RunBossFlowDirectDefeatTest()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HWJ] BossFlow 직접 완료 테스트는 Play Mode에서만 실행할 수 있습니다.");
            return;
        }

        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] BossFlow 직접 완료 테스트 실패: 먼저 씬의 중간보스2를 찾아야 합니다.");
            return;
        }

        HWJ_BossFlowSystem bossFlow = FindRuntimeMidBoss2BossFlow(runtimeBoss);

        if (bossFlow == null)
        {
            Debug.LogWarning("[HWJ] BossFlow 직접 완료 테스트 실패: 현재 씬에서 중간보스2를 참조하는 BossFlowSystem을 찾지 못했습니다.");
            return;
        }

        HWJ_BossFlowResult result = bossFlow.TryMarkBossDefeated(
            "MidBoss2 direct flow test from HWJ test window.",
            false,
            null,
            true);

        if (result.Succeeded)
        {
            Debug.Log($"[HWJ] BossFlow 직접 완료 테스트 성공: {result.Message}");
            return;
        }

        Debug.LogWarning(
            $"[HWJ] BossFlow 직접 완료 테스트 실패: {result.FailureCode}, {result.Message}");
    }

    private void ValidateBossRewardReadiness()
    {
        if (!TryResolveRewardTestTargets(
            out HWJ_RootObjectDataResolver bossResolver,
            out HWJ_RootObjectDataResolver playerResolver,
            out Vector3 rewardPosition,
            out string failureMessage))
        {
            Debug.LogWarning($"[HWJ] 중간보스2 보상 지급 준비 검사 실패: {failureMessage}");
            return;
        }

        HWJ_RewardData rewardData = bossResolver.Reward;

        if (rewardData == null)
        {
            Debug.LogWarning("[HWJ] 중간보스2 보상 지급 준비 검사 실패: 중간보스2 RootObjectData에 Reward 데이터가 없습니다.");
            return;
        }

        HWJ_SaveIdentityValidationResult identityResult =
            HWJ_RuntimeSaveIdentity.ValidateRewardClaimIdentity(bossResolver);

        if (!identityResult.Succeeded)
        {
            Debug.LogWarning($"[HWJ] 중간보스2 보상 지급 준비 검사 실패: {identityResult.Message}");
            return;
        }

        HWJ_LevelUpSystem playerLevel = ResolvePlayerLevelForRewardTest(playerResolver);

        if (playerLevel == null
            && (rewardData.experienceReward > 0 || rewardData.skillPointReward > 0))
        {
            Debug.LogWarning("[HWJ] 중간보스2 보상 지급 준비 검사 실패: 플레이어 경험치/스킬포인트를 받을 HWJ_LevelUpSystem이 없습니다.");
            return;
        }

        Debug.Log(
            "[HWJ] 중간보스2 보상 지급 준비 검사 성공: " +
            $"보상 위치={rewardPosition}, " +
            $"ClaimId={identityResult.RewardClaimId}, " +
            $"EXP={rewardData.experienceReward}, SP={rewardData.skillPointReward}, " +
            $"PlayerLevel={FormatYesNo(playerLevel != null)}");
    }

    private void RunBossRewardGrantTest()
    {
        if (!TryResolveRewardTestTargets(
            out HWJ_RootObjectDataResolver bossResolver,
            out HWJ_RootObjectDataResolver playerResolver,
            out Vector3 rewardPosition,
            out string failureMessage))
        {
            Debug.LogWarning($"[HWJ] 중간보스2 보상 직접 지급 테스트 실패: {failureMessage}");
            return;
        }

        HWJ_RewardGrantResult result = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            bossResolver,
            playerResolver,
            rewardPosition);

        if (result.Succeeded)
        {
            Debug.Log(
                "[HWJ] 중간보스2 보상 직접 지급 테스트 성공: " +
                $"EXP={result.ExperienceGranted}, SP={result.SkillPointGranted}, " +
                $"ExpOrb={FormatYesNo(result.ExperienceOrbSpawned)}, " +
                $"StatOrb={FormatYesNo(result.StatOrbGranted)}, " +
                $"ClaimId={result.RewardClaimId}, Message={result.Message}");
            return;
        }

        Debug.LogWarning(
            "[HWJ] 중간보스2 보상 직접 지급 테스트 실패: " +
            $"{result.FailureCode}, ClaimId={result.RewardClaimId}, Message={result.Message}");
    }

    private bool TryResolveRewardTestTargets(
        out HWJ_RootObjectDataResolver bossResolver,
        out HWJ_RootObjectDataResolver playerResolver,
        out Vector3 rewardPosition,
        out string failureMessage)
    {
        bossResolver = null;
        playerResolver = null;
        rewardPosition = Vector3.zero;
        failureMessage = null;

        if (!EditorApplication.isPlaying)
        {
            failureMessage = "Play Mode에서만 실행할 수 있습니다.";
            return false;
        }

        if (runtimeBoss == null)
        {
            failureMessage = "먼저 씬의 중간보스2를 찾아야 합니다.";
            return false;
        }

        bossResolver = runtimeBoss.GetComponent<HWJ_RootObjectDataResolver>();

        if (bossResolver == null || bossResolver.ObjectType != HWJ_ObjectType.Boss)
        {
            failureMessage = "중간보스2에서 Boss 타입 RootObjectDataResolver를 찾지 못했습니다.";
            return false;
        }

        playerResolver = FindRuntimePlayerResolver(runtimeBoss.gameObject.scene);

        if (playerResolver == null)
        {
            failureMessage = "현재 씬에서 Player 타입 RootObjectDataResolver를 찾지 못했습니다.";
            return false;
        }

        rewardPosition = runtimeBoss.transform.position;
        return true;
    }

    private static HWJ_RootObjectDataResolver FindRuntimePlayerResolver(Scene scene)
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver != null
                && resolver.ObjectType == HWJ_ObjectType.Player
                && (!scene.IsValid() || resolver.gameObject.scene == scene))
            {
                return resolver;
            }
        }

        return null;
    }

    private static HWJ_LevelUpSystem ResolvePlayerLevelForRewardTest(HWJ_RootObjectDataResolver playerResolver)
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerLevel != null)
        {
            return HWJ_GameAccess.Manager.PlayerLevel;
        }

        if (playerResolver == null)
        {
            return null;
        }

        HWJ_LevelUpSystem playerLevel = playerResolver.GetComponent<HWJ_LevelUpSystem>();

        if (playerLevel != null)
        {
            return playerLevel;
        }

        playerLevel = playerResolver.GetComponentInParent<HWJ_LevelUpSystem>();

        if (playerLevel != null)
        {
            return playerLevel;
        }

        return playerResolver.GetComponentInChildren<HWJ_LevelUpSystem>(true);
    }

    private static HWJ_BossFlowSystem FindRuntimeMidBoss2BossFlow(HWJ_BossBrainSystem boss)
    {
        if (boss == null)
        {
            return null;
        }

        HWJ_RootObjectDataResolver bossResolver =
            boss.GetComponent<HWJ_RootObjectDataResolver>();

        HWJ_BossFlowSystem[] bossFlows = FindObjectsByType<HWJ_BossFlowSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < bossFlows.Length; i++)
        {
            HWJ_BossFlowSystem bossFlow = bossFlows[i];

            if (bossFlow == null || bossFlow.gameObject.scene != boss.gameObject.scene)
            {
                continue;
            }

            SerializedObject serializedFlow = new SerializedObject(bossFlow);
            SerializedProperty bossResolverProperty = serializedFlow.FindProperty("bossResolver");
            SerializedProperty bossBrainProperty = serializedFlow.FindProperty("bossBrainSystem");

            if (bossResolverProperty != null
                && bossResolverProperty.objectReferenceValue == bossResolver)
            {
                return bossFlow;
            }

            if (bossBrainProperty != null
                && bossBrainProperty.objectReferenceValue == boss)
            {
                return bossFlow;
            }
        }

        return null;
    }

    private void ValidateMidBoss2RuntimeReadiness()
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        if (!EditorApplication.isPlaying)
        {
            errors.Add("Play Mode가 아닙니다. 중간보스2 런타임 준비 상태 검사는 Play Mode에서 실행해야 합니다.");
        }

        if (runtimeBoss == null)
        {
            errors.Add("실행 중 보스 참조가 없습니다. 먼저 '씬의 중간보스2 찾기'를 눌러야 합니다.");
        }
        else
        {
            ValidateRuntimeBossCoreState(errors, warnings);
        }

        CollectActiveSceneMidBoss2FlowValidationErrors(errors);

        if (errors.Count > 0)
        {
            Debug.LogError(
                "[HWJ] 중간보스2 Play Mode 준비 상태 검사 실패:\n- " +
                string.Join("\n- ", errors) +
                FormatWarningSection(warnings));
            return;
        }

        Debug.Log(
            "[HWJ] 중간보스2 Play Mode 준비 상태 검사 성공: " +
            "현재 씬의 보스 참조, 타겟, 런타임 상태, 패턴 시스템, 물리 설정, BossFlow 연결을 확인했습니다." +
            FormatWarningSection(warnings));
    }

    private void ValidateRuntimeBossCoreState(List<string> errors, List<string> warnings)
    {
        HWJ_RootObjectDataResolver dataResolver =
            runtimeBoss.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RuntimeStatusSystem runtimeStatus =
            runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_BossPatternSystem bossPattern =
            runtimeBoss.GetComponent<HWJ_BossPatternSystem>();
        HWJ_MidBossPatternSystem midBossPattern =
            runtimeBoss.GetComponent<HWJ_MidBossPatternSystem>();
        Rigidbody2D rigidbody2D = runtimeBoss.GetComponent<Rigidbody2D>();

        if (dataResolver == null || dataResolver.Identity == null)
        {
            errors.Add("중간보스2 런타임 오브젝트에 RootObjectDataResolver 또는 Identity가 없습니다.");
        }
        else
        {
            if (dataResolver.Identity.objectId != MidBoss2ObjectId)
            {
                errors.Add($"실행 중 보스 ID가 {MidBoss2ObjectId}가 아닙니다. 현재 ID={dataResolver.Identity.objectId}");
            }

            if (dataResolver.Identity.objectType != HWJ_ObjectType.Boss)
            {
                errors.Add("중간보스2 Identity의 Object Type이 Boss가 아닙니다.");
            }
        }

        if (!TryResolveRuntimeBossData(out HWJ_BossTypeDataSO bossData))
        {
            errors.Add("중간보스2 Boss TypeData를 읽을 수 없습니다.");
        }
        else
        {
            float attackStartRange = ResolveAttackStartRange(bossData);

            if (attackStartRange <= 0f)
            {
                errors.Add("중간보스2 공격 시작 거리가 0 이하입니다.");
            }

            if (bossData.FSM.phaseTransitionSeconds < 9.5f)
            {
                warnings.Add("페이즈 전환 시간이 기획 기준 10초보다 짧습니다.");
            }
        }

        if (runtimeStatus == null)
        {
            errors.Add("중간보스2 런타임 상태 시스템이 없습니다.");
        }
        else
        {
            if (runtimeStatus.MaxHp <= 0f)
            {
                errors.Add("중간보스2 Max HP가 0 이하입니다.");
            }

            if (runtimeStatus.IsDead || runtimeStatus.CurrentHp <= 0f)
            {
                errors.Add("중간보스2가 이미 사망 상태입니다.");
            }
        }

        if (runtimeBoss.Target == null)
        {
            warnings.Add("타겟이 없습니다. 자동 전투/거리 기반 패턴을 확인하려면 '타겟 자동 연결'을 먼저 눌러야 합니다.");
        }
        else if (!IsInsideRect(runtimeBoss.Target.position, runtimeBoss.BossRoomCenter, runtimeBoss.BossRoomSize))
        {
            warnings.Add("현재 타겟이 보스방 범위 밖에 있습니다. 보스방 범위 설정 또는 타겟 위치를 확인해야 합니다.");
        }

        if (bossPattern == null)
        {
            errors.Add("중간보스2에 HWJ_BossPatternSystem이 없습니다.");
        }

        if (midBossPattern == null)
        {
            errors.Add("중간보스2에 HWJ_MidBossPatternSystem이 없습니다.");
        }
        else if (midBossPattern.IsPatternRunning)
        {
            warnings.Add("현재 특수 패턴이 실행 중입니다. 새 패턴 테스트 전에는 '현재 패턴 취소'로 정리하는 것이 좋습니다.");
        }

        if (rigidbody2D == null)
        {
            errors.Add("중간보스2에 Rigidbody2D가 없습니다.");
        }
        else
        {
            if (rigidbody2D.bodyType != RigidbodyType2D.Dynamic)
            {
                errors.Add("중간보스2 Rigidbody2D Body Type은 Dynamic이어야 합니다.");
            }

            if (!rigidbody2D.simulated)
            {
                errors.Add("중간보스2 Rigidbody2D Simulated가 꺼져 있습니다.");
            }

            if ((rigidbody2D.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                errors.Add("중간보스2 Rigidbody2D Freeze Rotation이 켜져 있지 않습니다.");
            }
        }

        if (runtimeBoss.CurrentState == HWJ_BossFSMState.Dead)
        {
            errors.Add("중간보스2 FSM이 Dead 상태입니다.");
        }
        else if (runtimeBoss.CurrentState == HWJ_BossFSMState.PhaseTransition)
        {
            warnings.Add("현재 페이즈 전환 상태입니다. 일반 패턴 테스트는 전환이 끝난 뒤 진행해야 합니다.");
        }
    }

    private static string FormatWarningSection(List<string> warnings)
    {
        return warnings.Count > 0
            ? "\n\n경고:\n- " + string.Join("\n- ", warnings)
            : string.Empty;
    }

    private void PrepareMidBoss2PlayModeTest()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HWJ] 중간보스2 빠른 테스트 준비는 Play Mode에서만 사용할 수 있습니다.");
            return;
        }

        FindRuntimeBossByObjectId(MidBoss2ObjectId);

        if (runtimeBoss == null)
        {
            Debug.LogWarning("[HWJ] 중간보스2 빠른 테스트 준비 실패: 현재 씬에서 boss.mid.02.captain 보스를 찾지 못했습니다.");
            return;
        }

        CancelCurrentPattern();
        runtimeBoss.SetAIEnabled(false);

        HWJ_RuntimeStatusSystem runtimeStatus = runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus != null)
        {
            runtimeStatus.ResetForEncounter(true);
        }

        runtimeBoss.GetComponent<HWJ_BossPatternSystem>()?.ResetPatternHistory();

        Transform target = ResolveTarget();
        runtimeBoss.StartBossEncounter();

        Debug.Log(
            "[HWJ] 중간보스2 Play Mode 빠른 테스트 준비 완료: " +
            "HP를 풀회복하고, AI를 끄고, 패턴 기록을 초기화하고, 전투를 시작했습니다. " +
            $"타겟={(target != null ? target.name : "없음")}. " +
            "이후 패턴 1~7 버튼으로 수동 확인하거나 AI 활성화를 켜서 자동 패턴을 확인하세요.");

        ValidateMidBoss2RuntimeReadiness();
    }

    private void DrawPatternButtons()
    {
        EditorGUILayout.LabelField("물리 패턴 강제 실행", EditorStyles.boldLabel);

        DrawTwoButtons(
            "패턴 1: 소환",
            () => { ForcePhysicalPattern(1); },
            "패턴 2: 대쉬 2연 베기",
            () => { ForcePhysicalPattern(2); });
        DrawTwoButtons(
            "패턴 3: 베기 + 검기",
            () => { ForcePhysicalPattern(3); },
            "패턴 4: 고정 피해 돌진",
            () => { ForcePhysicalPattern(4); });
        DrawTwoButtons(
            "패턴 5: 붉은 검 연속 공격",
            () => { ForcePhysicalPattern(5); },
            "패턴 6: 은신 기습",
            () => { ForcePhysicalPattern(6); });

        if (GUILayout.Button("패턴 7: 점프 내려찍기"))
        {
            ForcePhysicalPattern(7);
        }

        EditorGUILayout.LabelField("패턴 순차 실행 상태", FormatPatternSequenceStatus());
        DrawTwoButtons(
            "패턴 1~7 순차 실행 시작",
            StartPatternSequence,
            "패턴 순차 실행 중지",
            StopPatternSequence);

        EditorGUILayout.HelpBox(
            "패턴 1은 HP가 15%씩 감소해 사용 기회를 얻고, 보스방 안에 빙의 가능한 시체가 없어야 성공합니다.",
            MessageType.None);
    }

    private string FormatPatternSequenceStatus()
    {
        if (!isPatternSequenceRunning)
        {
            return "대기";
        }

        if (runningSequencePatternNumber > 0)
        {
            return $"패턴 {runningSequencePatternNumber} 실행 확인 중";
        }

        return nextSequencePatternNumber <= MidBoss2PatternVerificationItems.Length
            ? $"패턴 {nextSequencePatternNumber} 실행 대기"
            : "완료 처리 중";
    }

    private void StartPatternSequence()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HWJ] 패턴 1~7 순차 실행은 Play Mode에서만 사용할 수 있습니다.");
            return;
        }

        PrepareMidBoss2PlayModeTest();

        if (runtimeBoss == null)
        {
            return;
        }

        ResetPatternVerificationChecklist();
        isPatternSequenceRunning = true;
        nextSequencePatternNumber = 1;
        runningSequencePatternNumber = 0;
        nextPatternSequenceTime = EditorApplication.timeSinceStartup;

        Debug.Log("[HWJ] 중간보스2 패턴 1~7 순차 실행을 시작합니다. 화면에서 각 패턴이 정상인지 직접 확인하세요.");
    }

    private void StopPatternSequence()
    {
        if (!isPatternSequenceRunning)
        {
            return;
        }

        isPatternSequenceRunning = false;
        runningSequencePatternNumber = 0;
        nextSequencePatternNumber = 1;
        CancelCurrentPattern();
        Debug.Log("[HWJ] 중간보스2 패턴 순차 실행을 중지했습니다.");
    }

    private void TickPatternSequenceRunner()
    {
        if (!isPatternSequenceRunning)
        {
            return;
        }

        if (!EditorApplication.isPlaying || runtimeBoss == null)
        {
            isPatternSequenceRunning = false;
            runningSequencePatternNumber = 0;
            Debug.LogWarning("[HWJ] Play Mode가 아니거나 실행 중 보스가 없어 패턴 순차 실행을 중지했습니다.");
            return;
        }

        HWJ_MidBossPatternSystem midBossPattern =
            runtimeBoss.GetComponent<HWJ_MidBossPatternSystem>();

        if (midBossPattern == null)
        {
            isPatternSequenceRunning = false;
            runningSequencePatternNumber = 0;
            Debug.LogWarning("[HWJ] HWJ_MidBossPatternSystem이 없어 패턴 순차 실행을 중지했습니다.");
            return;
        }

        if (runningSequencePatternNumber > 0)
        {
            if (midBossPattern.IsPatternRunning)
            {
                return;
            }

            patternVerificationStates[runningSequencePatternNumber - 1] = true;
            Debug.Log($"[HWJ] 중간보스2 패턴 {runningSequencePatternNumber} 순차 실행 확인 기록을 체크했습니다.");
            runningSequencePatternNumber = 0;
            nextPatternSequenceTime = EditorApplication.timeSinceStartup + PatternSequenceGapSeconds;
            Repaint();
            return;
        }

        if (runtimeBoss.CurrentState == HWJ_BossFSMState.Groggy
            || runtimeBoss.CurrentState == HWJ_BossFSMState.PhaseTransition)
        {
            return;
        }

        if (nextSequencePatternNumber > MidBoss2PatternVerificationItems.Length)
        {
            isPatternSequenceRunning = false;
            Debug.Log("[HWJ] 중간보스2 패턴 1~7 순차 실행이 끝났습니다. 실제 화면에서 문제가 없었는지 최종 체크하세요.");
            return;
        }

        if (EditorApplication.timeSinceStartup < nextPatternSequenceTime)
        {
            return;
        }

        int patternNumber = nextSequencePatternNumber;

        if (!ForcePhysicalPattern(patternNumber))
        {
            isPatternSequenceRunning = false;
            runningSequencePatternNumber = 0;
            Debug.LogWarning($"[HWJ] 패턴 {patternNumber} 실행 실패로 순차 실행을 중지했습니다.");
            return;
        }

        runningSequencePatternNumber = patternNumber;
        nextSequencePatternNumber++;
    }

    private void DrawPatternVerificationChecklist()
    {
        EditorGUILayout.Space(8f);
        showPatternVerificationChecklist = EditorGUILayout.Foldout(
            showPatternVerificationChecklist,
            "패턴 1~7 수동 확인 기록",
            true);

        if (!showPatternVerificationChecklist)
        {
            return;
        }

        EditorGUILayout.LabelField(
            "자동 판정이 아니라 Play Mode에서 직접 보고 확인한 패턴을 기록하는 용도입니다.",
            EditorStyles.miniLabel);

        int completedCount = 0;

        for (int i = 0; i < patternVerificationStates.Length; i++)
        {
            patternVerificationStates[i] = EditorGUILayout.ToggleLeft(
                $"{i + 1}. {MidBoss2PatternVerificationItems[i]}",
                patternVerificationStates[i]);

            if (patternVerificationStates[i])
            {
                completedCount++;
            }
        }

        EditorGUILayout.LabelField(
            "패턴 확인 진행도",
            $"{completedCount} / {MidBoss2PatternVerificationItems.Length}");

        DrawTwoButtons(
            "최근 수동 패턴 확인 처리",
            MarkLastManualPatternAsVerified,
            "패턴 확인 기록 초기화",
            ResetPatternVerificationChecklist);
    }

    private void MarkLastManualPatternAsVerified()
    {
        if (lastManualPhysicalPatternNumber <= 0
            || lastManualPhysicalPatternNumber > MidBoss2PatternVerificationItems.Length)
        {
            Debug.LogWarning("[HWJ] 최근 수동 실행 패턴이 없어 확인 처리할 수 없습니다.");
            return;
        }

        patternVerificationStates[lastManualPhysicalPatternNumber - 1] = true;
        Debug.Log($"[HWJ] 중간보스2 패턴 {lastManualPhysicalPatternNumber} 확인 기록을 체크했습니다.");
    }

    private void ResetPatternVerificationChecklist()
    {
        for (int i = 0; i < patternVerificationStates.Length; i++)
        {
            patternVerificationStates[i] = false;
        }

        lastManualPhysicalPatternNumber = 0;
    }

    private bool ForcePhysicalPattern(int patternNumber)
    {
        HWJ_MidBossPatternSystem midBossPattern = runtimeBoss != null
            ? runtimeBoss.GetComponent<HWJ_MidBossPatternSystem>()
            : null;

        if (midBossPattern == null)
        {
            Debug.LogWarning("[HWJ] 씬의 보스에 HWJ_MidBossPatternSystem이 없습니다.");
            return false;
        }

        HWJ_BossPatternDataSO patternData = LoadPhysicalPattern(patternNumber);

        if (patternData == null)
        {
            Debug.LogWarning($"[HWJ] 물리 중간보스 패턴 {patternNumber} 데이터를 찾지 못했습니다.");
            return false;
        }

        // 이전 임시 판정과 경고선을 정리한 뒤 새 패턴 하나만 실행해 테스트 결과를 명확하게 봅니다.
        CancelCurrentPattern();

        if (patternNumber == 1)
        {
            PreparePatternOneSummonTestWindow();
        }

        if (!midBossPattern.TryExecutePattern(patternData, ResolveTarget()))
        {
            Debug.LogWarning($"[HWJ] 물리 중간보스 패턴 {patternNumber} 실행 조건을 만족하지 못했습니다.");
            return false;
        }

        lastManualPhysicalPatternNumber = patternNumber;
        return true;
    }

    private void PreparePatternOneSummonTestWindow()
    {
        HWJ_RuntimeStatusSystem runtimeStatus = runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return;
        }

        // 패턴 1은 HP가 15% 깎여야 소환 기회를 얻는다. 테스트 버튼에서는 그 조건만 빠르게 맞춘다.
        float currentRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;

        if (currentRatio > 0.84f)
        {
            runtimeStatus.SetCurrentHpForDebug(runtimeStatus.MaxHp * 0.84f);
        }
    }

    private static HWJ_BossPatternDataSO LoadPhysicalPattern(int patternNumber)
    {
        string assetPath = $"{MidBossPatternFolder}/HWJ_MidBossPhysical_Pattern{patternNumber}.asset";
        return AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(assetPath);
    }

    private void SetHealthRatio(float ratio)
    {
        HWJ_RuntimeStatusSystem runtimeStatus = runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return;
        }

        SetHealthValue(runtimeStatus.MaxHp * Mathf.Clamp01(ratio));
    }

    private void SetHealthValue(float value)
    {
        runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>()?.SetCurrentHpForDebug(value);
    }

    private static string FormatRecentPatternKeys(HWJ_BossPatternSystem bossPattern)
    {
        if (bossPattern == null || bossPattern.RecentPatternKeys == null || bossPattern.RecentPatternKeys.Length == 0)
        {
            return "-";
        }

        return string.Join(" -> ", bossPattern.RecentPatternKeys);
    }

    private void CancelCurrentPattern()
    {
        if (runtimeBoss == null)
        {
            return;
        }

        runtimeBoss.GetComponent<HWJ_MidBossPatternSystem>()?.CancelActivePattern();

        HWJ_FighterBossHitboxSystem[] hitboxes =
            runtimeBoss.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i]?.Disarm();
        }
    }

    private Transform ResolveTarget()
    {
        if (runtimeBoss != null && runtimeBoss.Target != null)
        {
            return runtimeBoss.Target;
        }

        HWJ_RootObjectDataResolver[] resolvers =
            FindObjectsByType<HWJ_RootObjectDataResolver>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                runtimeBoss?.SetTarget(resolvers[i].transform);
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void FindRuntimeMidBoss()
    {
        HWJ_MidBossPatternSystem[] patterns = FindObjectsByType<HWJ_MidBossPatternSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        HWJ_MidBossPatternSystem selectedPattern =
            patterns.FirstOrDefault(pattern => pattern != null
                && pattern.GetComponent<HWJ_BossBrainSystem>() != null);

        runtimeBoss = selectedPattern != null
            ? selectedPattern.GetComponent<HWJ_BossBrainSystem>()
            : null;
    }

    private void FindRuntimeBossByObjectId(string objectId)
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver.Identity == null
                || resolver.Identity.objectId != objectId)
            {
                continue;
            }

            runtimeBoss = resolver.GetComponent<HWJ_BossBrainSystem>();

            if (runtimeBoss != null)
            {
                return;
            }
        }

        runtimeBoss = null;
        Debug.LogWarning($"[HWJ] 씬에서 {objectId} 보스를 찾지 못했습니다.");
    }

    private static void CollectMidBoss1ValidationErrors(List<string> errors)
    {
        HWJ_RootObjectDataSO rootData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(MidBoss1RootPath);
        HWJ_BossTypeDataSO typeData =
            AssetDatabase.LoadAssetAtPath<HWJ_BossTypeDataSO>(MidBoss1TypePath);
        HWJ_GameplayDatabaseSO gameplayDatabase =
            AssetDatabase.LoadAssetAtPath<HWJ_GameplayDatabaseSO>(GameplayDatabasePath);
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MidBoss1PrefabPath);

        ValidateRootAndTypeData(
            "중간보스1",
            rootData,
            typeData,
            gameplayDatabase,
            MidBoss1RootPath,
            MidBoss1TypePath,
            MidBoss1ObjectId,
            errors);
        ValidateMidBoss1Prefab(prefabAsset, rootData, errors);
    }

    private static void CollectMidBoss2ValidationErrors(List<string> errors)
    {
        HWJ_RootObjectDataSO rootData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(MidBoss2RootPath);
        HWJ_BossTypeDataSO typeData =
            AssetDatabase.LoadAssetAtPath<HWJ_BossTypeDataSO>(MidBoss2TypePath);
        HWJ_GameplayDatabaseSO gameplayDatabase =
            AssetDatabase.LoadAssetAtPath<HWJ_GameplayDatabaseSO>(GameplayDatabasePath);
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MidBoss2PrefabPath);

        ValidateRootAndTypeData(
            "중간보스2",
            rootData,
            typeData,
            gameplayDatabase,
            MidBoss2RootPath,
            MidBoss2TypePath,
            MidBoss2ObjectId,
            errors);
        ValidateMidBoss2RootStats(rootData, errors);
        ValidateMidBoss2RootGameplayReferences(rootData, errors);
        ValidateMidBoss2CombatSettings(typeData, errors);
        ValidatePhysicalPatternAssets(errors);
        ValidateMidBoss2Prefab(prefabAsset, rootData, gameplayDatabase, errors);
    }

    private static void CollectActiveSceneMidBoss2FlowValidationErrors(List<string> errors)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            errors.Add("활성 씬을 읽을 수 없습니다.");
            return;
        }

        HWJ_RootObjectDataResolver[] midBossResolvers =
            FindSceneResolversByObjectId(activeScene, MidBoss2ObjectId);

        if (midBossResolvers.Length == 0)
        {
            errors.Add($"현재 씬({activeScene.name})에 {MidBoss2ObjectId} 보스가 없습니다.");
            return;
        }

        if (midBossResolvers.Length > 1)
        {
            errors.Add($"현재 씬({activeScene.name})에 {MidBoss2ObjectId} 보스가 {midBossResolvers.Length}개 있습니다. 중간보스2는 한 씬에 하나만 두는 것을 기준으로 검증합니다.");
        }

        HWJ_RootObjectDataResolver midBossResolver = midBossResolvers[0];
        HWJ_BossBrainSystem midBossBrain = midBossResolver.GetComponent<HWJ_BossBrainSystem>();

        if (midBossBrain == null)
        {
            errors.Add("현재 씬 중간보스2에 HWJ_BossBrainSystem이 없습니다.");
        }

        if (!midBossResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            errors.Add("현재 씬 중간보스2에서 BossTypeData를 읽을 수 없습니다.");
        }

        HWJ_BossFlowSystem[] bossFlows = FindObjectsByType<HWJ_BossFlowSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<HWJ_BossFlowSystem> sceneBossFlows = new List<HWJ_BossFlowSystem>();

        for (int i = 0; i < bossFlows.Length; i++)
        {
            if (bossFlows[i] != null && bossFlows[i].gameObject.scene == activeScene)
            {
                sceneBossFlows.Add(bossFlows[i]);
            }
        }

        if (sceneBossFlows.Count == 0)
        {
            errors.Add("현재 씬에 HWJ_BossFlowSystem이 없습니다. 보스를 처치해도 스테이지 클리어로 이어지지 않습니다.");
            return;
        }

        int matchingFlowCount = 0;

        for (int i = 0; i < sceneBossFlows.Count; i++)
        {
            ValidateSceneBossFlowForMidBoss2(
                sceneBossFlows[i],
                midBossResolver,
                midBossBrain,
                bossData,
                errors,
                ref matchingFlowCount);
        }

        if (matchingFlowCount == 0)
        {
            errors.Add("현재 씬의 HWJ_BossFlowSystem 중 중간보스2를 참조하는 항목이 없습니다.");
        }

        if (matchingFlowCount > 1)
        {
            errors.Add($"현재 씬에서 중간보스2를 참조하는 HWJ_BossFlowSystem이 {matchingFlowCount}개입니다. 보스 처치 처리가 중복될 수 있습니다.");
        }
    }

    private static HWJ_RootObjectDataResolver[] FindSceneResolversByObjectId(
        Scene scene,
        string objectId)
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<HWJ_RootObjectDataResolver> matches = new List<HWJ_RootObjectDataResolver>();

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver.gameObject.scene != scene
                || resolver.RootObjectData == null
                || resolver.RootObjectData.Identity == null
                || resolver.RootObjectData.Identity.objectId != objectId)
            {
                continue;
            }

            matches.Add(resolver);
        }

        return matches.ToArray();
    }

    private static void ValidateSceneBossFlowForMidBoss2(
        HWJ_BossFlowSystem bossFlow,
        HWJ_RootObjectDataResolver midBossResolver,
        HWJ_BossBrainSystem midBossBrain,
        HWJ_BossTypeDataSO bossData,
        List<string> errors,
        ref int matchingFlowCount)
    {
        if (bossFlow == null)
        {
            return;
        }

        SerializedObject serializedFlow = new SerializedObject(bossFlow);
        SerializedProperty bossResolverProperty = serializedFlow.FindProperty("bossResolver");
        SerializedProperty bossBrainProperty = serializedFlow.FindProperty("bossBrainSystem");
        SerializedProperty stageProperty = serializedFlow.FindProperty("stageProgressionSystem");
        SerializedProperty autoCompleteProperty = serializedFlow.FindProperty("autoCompleteBossFlowOnCombatDeath");
        SerializedProperty requireBossBattleProperty = serializedFlow.FindProperty("requireBossBattleStateForCombatDeath");
        SerializedProperty entryTriggerProperty = serializedFlow.FindProperty("bossEntryTriggerActive");

        HWJ_RootObjectDataResolver assignedBossResolver =
            bossResolverProperty != null ? bossResolverProperty.objectReferenceValue as HWJ_RootObjectDataResolver : null;
        HWJ_BossBrainSystem assignedBossBrain =
            bossBrainProperty != null ? bossBrainProperty.objectReferenceValue as HWJ_BossBrainSystem : null;

        bool referencesMidBoss = assignedBossResolver == midBossResolver
            || assignedBossBrain == midBossBrain
            || bossFlow.GetComponent<HWJ_RootObjectDataResolver>() == midBossResolver
            || bossFlow.GetComponent<HWJ_BossBrainSystem>() == midBossBrain;

        if (!referencesMidBoss)
        {
            return;
        }

        matchingFlowCount++;
        string flowPath = GetHierarchyPath(bossFlow.transform);

        if (stageProperty == null || stageProperty.objectReferenceValue == null)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})에 StageProgressionSystem 참조가 없습니다.");
        }

        if (bossResolverProperty == null || assignedBossResolver != midBossResolver)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})의 Boss Resolver가 중간보스2를 직접 참조해야 합니다.");
        }

        if (bossBrainProperty == null || assignedBossBrain != midBossBrain)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})의 Boss Brain이 중간보스2를 직접 참조해야 합니다.");
        }

        if (autoCompleteProperty != null && !autoCompleteProperty.boolValue)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})는 Auto Complete Boss Flow On Combat Death가 켜져 있어야 합니다.");
        }

        bool autoStartsFromBossBrain = bossData != null && bossData.FSM.autoStartWhenPlayerEntersRoom;

        if (autoStartsFromBossBrain
            && requireBossBattleProperty != null
            && requireBossBattleProperty.boolValue)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})는 보스 브레인 자동 시작과 함께 쓰이므로 Require Boss Battle State For Combat Death가 꺼져 있어야 합니다.");
        }

        if (bossData != null
            && bossData.EntryRequirements != null
            && bossData.EntryRequirements.requireEntryTriggerActive
            && entryTriggerProperty != null
            && !entryTriggerProperty.boolValue)
        {
            errors.Add($"중간보스2 BossFlow({flowPath})는 보스 입장 트리거가 필요하므로 Boss Entry Trigger Active가 켜져 있어야 합니다.");
        }
    }

    private static void ValidateRootAndTypeData(
        string label,
        HWJ_RootObjectDataSO rootData,
        HWJ_BossTypeDataSO typeData,
        HWJ_GameplayDatabaseSO gameplayDatabase,
        string rootPath,
        string typePath,
        string objectId,
        List<string> errors)
    {
        if (rootData == null)
        {
            errors.Add($"[{label}] RootObjectData가 없습니다: {rootPath}");
            return;
        }

        if (rootData.Identity == null || rootData.Identity.objectId != objectId)
        {
            errors.Add($"[{label}] RootObjectData ID가 {objectId}가 아닙니다.");
        }

        if (rootData.ObjectType != HWJ_ObjectType.Boss)
        {
            errors.Add($"[{label}] RootObjectData의 ObjectType이 Boss가 아닙니다.");
        }

        if (rootData.Status == null || rootData.Status.maxHp <= 0f)
        {
            errors.Add($"[{label}] RootObjectData의 HP가 0 이하입니다.");
        }

        if (typeData == null)
        {
            errors.Add($"[{label}] Boss TypeData가 없습니다: {typePath}");
            return;
        }

        if (typeData.TypeId != objectId)
        {
            errors.Add($"[{label}] Boss TypeData ID가 {objectId}가 아닙니다.");
        }

        if (rootData.SelectedTypeData != typeData)
        {
            errors.Add($"[{label}] RootObjectData의 선택된 TypeData가 해당 Boss TypeData가 아닙니다.");
        }

        if (typeData.PossessionBody != null
            && (typeData.PossessionBody.canPossess || typeData.PossessionBody.canBePossessed))
        {
            errors.Add($"[{label}] 보스는 빙의 대상이 아니어야 하므로 canPossess/canBePossessed가 꺼져 있어야 합니다.");
        }

        if (gameplayDatabase == null)
        {
            errors.Add($"[{label}] GameplayDatabase가 없습니다: {GameplayDatabasePath}");
            return;
        }

        bool registered = gameplayDatabase.RootObjects != null
            && gameplayDatabase.RootObjects.Contains(rootData);

        if (!registered)
        {
            errors.Add($"[{label}] GameplayDatabase RootObjects에 RootObjectData가 등록되어 있지 않습니다.");
        }
    }

    private static void ValidateMidBoss2RootStats(
        HWJ_RootObjectDataSO rootData,
        List<string> errors)
    {
        if (rootData == null || rootData.Status == null || rootData.Damage == null)
        {
            return;
        }

        if (!Mathf.Approximately(rootData.Status.maxHp, MidBoss2ExpectedMaxHp))
        {
            errors.Add($"[중간보스2] 기획 기준 HP는 {MidBoss2ExpectedMaxHp:0}이어야 합니다.");
        }

        if (!Mathf.Approximately(rootData.Status.attackPower, MidBoss2ExpectedAttackPower))
        {
            errors.Add($"[중간보스2] 기획 기준 공격력은 {MidBoss2ExpectedAttackPower:0}이어야 합니다.");
        }

        if (!Mathf.Approximately(rootData.Status.defense, MidBoss2ExpectedDefense))
        {
            errors.Add($"[중간보스2] 기획 기준 방어력은 {MidBoss2ExpectedDefense:0}이어야 합니다.");
        }

        if (!Mathf.Approximately(rootData.Status.moveSpeed, MidBoss2ExpectedMoveSpeed))
        {
            errors.Add($"[중간보스2] 기획 기준 이동속도는 {MidBoss2ExpectedMoveSpeed:0}이어야 합니다.");
        }

        if (!Mathf.Approximately(rootData.Damage.baseDamage, MidBoss2ExpectedAttackPower))
        {
            errors.Add($"[중간보스2] 기본 데미지는 공격력과 같은 {MidBoss2ExpectedAttackPower:0}이어야 합니다.");
        }
    }

    private static void ValidateMidBoss2RootGameplayReferences(
        HWJ_RootObjectDataSO rootData,
        List<string> errors)
    {
        if (rootData == null)
        {
            return;
        }

        // 애니메이션/이펙트/사운드는 후속 작업이므로 여기서는 게임 실행에 필요한 참조만 검사합니다.
        if (rootData.Model == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Model 데이터가 없습니다.");
        }
        else if (rootData.Model.modelPrefab == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Model Prefab이 비어 있습니다. 아트 교체 전이라도 임시 모델 프리팹은 필요합니다.");
        }

        if (rootData.Damage == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Damage 데이터가 없습니다.");
        }
        else
        {
            if (rootData.Damage.damageType != HWJ_DamageType.Physical)
            {
                errors.Add("[중간보스2] 1페이즈/2페이즈 컨셉이 물리 공격이므로 Damage Type은 Physical이어야 합니다.");
            }

            if (rootData.Damage.baseDamage <= 0f)
            {
                errors.Add("[중간보스2] 기본 데미지는 0보다 커야 합니다.");
            }
        }

        if (rootData.ReceivedDamage == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Received Damage 데이터가 없습니다.");
        }

        if (rootData.Interaction == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Interaction 데이터가 없습니다.");
        }
        else
        {
            if (!rootData.Interaction.canBeTargeted)
            {
                errors.Add("[중간보스2] 플레이어 공격 대상이 되어야 하므로 Can Be Targeted가 켜져 있어야 합니다.");
            }

            if (rootData.Interaction.canInteract)
            {
                errors.Add("[중간보스2] 보스는 빙의/대화 상호작용 대상이 아니므로 Can Interact가 꺼져 있어야 합니다.");
            }
        }

        if (rootData.Reward == null)
        {
            errors.Add("[중간보스2] RootObjectData의 Reward 데이터가 없습니다.");
        }
        else
        {
            if (rootData.Reward.experienceReward < 0)
            {
                errors.Add("[중간보스2] 경험치 보상은 음수가 될 수 없습니다.");
            }

            if (rootData.Reward.skillPointReward < 0)
            {
                errors.Add("[중간보스2] 스킬 포인트 보상은 음수가 될 수 없습니다.");
            }
        }
    }

    private static void ValidateMidBoss2CombatSettings(
        HWJ_BossTypeDataSO typeData,
        List<string> errors)
    {
        if (typeData == null)
        {
            return;
        }

        if (typeData.FSM.bossRoomSize.x <= 0f || typeData.FSM.bossRoomSize.y <= 0f)
        {
            errors.Add("[중간보스2] 보스방 크기가 0 이하입니다.");
        }

        if (typeData.FSM.attackStartRange <= 0f)
        {
            errors.Add("[중간보스2] 공격 시작 거리가 0 이하입니다.");
        }

        if (typeData.FSM.closeSkillRange <= 0f)
        {
            errors.Add("[중간보스2] 근거리 스킬 거리가 0 이하입니다.");
        }

        if (typeData.FSM.attackStartRange < typeData.FSM.closeSkillRange)
        {
            errors.Add("[중간보스2] 공격 시작 거리는 근거리 스킬 거리보다 작으면 안 됩니다.");
        }

        if (typeData.FSM.optimalAttackDistance <= 0f)
        {
            errors.Add("[중간보스2] 최적 공격 거리가 0 이하입니다.");
        }

        if (typeData.FSM.phaseTwoHpRatio <= 0f || typeData.FSM.phaseTwoHpRatio >= 1f)
        {
            errors.Add("[중간보스2] 2페이즈 HP 비율은 0보다 크고 1보다 작아야 합니다.");
        }

        if (typeData.FSM.phaseTransitionSeconds < 9.5f)
        {
            errors.Add("[중간보스2] 페이즈 전환 시간은 현재 기획 기준상 10초 정도로 설정해야 합니다.");
        }

        if (typeData.PhaseTransform == null || !typeData.PhaseTransform.canTransform)
        {
            errors.Add("[중간보스2] 50% HP에서 2페이즈 변신을 해야 하므로 Phase Transform의 Can Transform이 켜져 있어야 합니다.");
            return;
        }

        if (!Mathf.Approximately(typeData.PhaseTransform.transformHpRatio, 0.5f))
        {
            errors.Add("[중간보스2] Phase Transform HP 비율은 0.5여야 합니다.");
        }

        if (typeData.PhaseTransform.transformedModelId != MidBoss2PhaseTwoModelId)
        {
            errors.Add($"[중간보스2] 2페이즈 변신 모델 ID는 {MidBoss2PhaseTwoModelId}여야 합니다.");
        }
    }

    private static void ValidatePhysicalPatternAssets(List<string> errors)
    {
        if (MidBossPhysicalPatternSpecs.Length != MidBossPhysicalPatternPaths.Length)
        {
            errors.Add("물리 패턴 검증 기준 수와 에셋 경로 수가 다릅니다.");
            return;
        }

        for (int i = 0; i < MidBossPhysicalPatternPaths.Length; i++)
        {
            MidBossPhysicalPatternSpec spec = MidBossPhysicalPatternSpecs[i];
            HWJ_BossPatternDataSO pattern =
                AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(MidBossPhysicalPatternPaths[i]);

            if (pattern == null)
            {
                errors.Add($"물리 패턴 {i + 1} 에셋이 없습니다: {MidBossPhysicalPatternPaths[i]}");
                continue;
            }

            if (pattern.PatternNumber != spec.PatternNumber)
            {
                errors.Add($"물리 패턴 {i + 1}의 PatternNumber가 {spec.PatternNumber}이 아닙니다.");
            }

            if (pattern.PatternId != spec.PatternId)
            {
                errors.Add($"물리 패턴 {i + 1}의 PatternId가 {spec.PatternId}가 아닙니다.");
            }

            if (pattern.Trigger != HWJ_BossPatternTrigger.Always)
            {
                errors.Add($"물리 패턴 {i + 1}은 기본 자동 선택 패턴이므로 Trigger가 Always여야 합니다.");
            }

            if (pattern.RangeMode != spec.RangeMode)
            {
                errors.Add($"물리 패턴 {i + 1}의 거리 조건이 {spec.RangeMode}가 아닙니다.");
            }

            if (pattern.IsPhaseAllowed(1) != spec.UsableInPhase1)
            {
                errors.Add($"물리 패턴 {i + 1}의 1페이즈 사용 여부가 기획과 다릅니다.");
            }

            if (pattern.IsPhaseAllowed(2) != spec.UsableInPhase2)
            {
                errors.Add($"물리 패턴 {i + 1}의 2페이즈 사용 여부가 기획과 다릅니다.");
            }

            if (!Mathf.Approximately(pattern.EffectiveCooldownSeconds, 5f))
            {
                errors.Add($"물리 패턴 {i + 1}의 유효 쿨타임이 5초가 아닙니다.");
            }

            if (pattern.AnimationId != spec.AnimationId)
            {
                errors.Add($"물리 패턴 {i + 1}의 AnimationId가 {spec.AnimationId}가 아닙니다.");
            }

            if (!pattern.UseCustomPatternExecutor || pattern.CustomPatternExecutorKey != MidBossExecutorKey)
            {
                errors.Add($"물리 패턴 {i + 1}의 Custom Executor Key가 {MidBossExecutorKey}가 아닙니다.");
            }
        }
    }

    private static void ValidateMidBoss1Prefab(
        GameObject prefabAsset,
        HWJ_RootObjectDataSO expectedRootData,
        List<string> errors)
    {
        if (prefabAsset == null)
        {
            errors.Add($"[중간보스1] 프리팹이 없습니다: {MidBoss1PrefabPath}");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MidBoss1PrefabPath);

        try
        {
            HWJ_RootObjectDataResolver resolver = prefabRoot.GetComponent<HWJ_RootObjectDataResolver>();
            HWJ_BossBrainSystem brain = prefabRoot.GetComponent<HWJ_BossBrainSystem>();
            HWJ_BossPatternSystem patternSystem = prefabRoot.GetComponent<HWJ_BossPatternSystem>();

            if (resolver == null || resolver.RootObjectData != expectedRootData)
            {
                errors.Add("[중간보스1] 프리팹의 RootObjectDataResolver가 중간보스1 RootObjectData를 참조하지 않습니다.");
            }

            if (brain == null)
            {
                errors.Add("[중간보스1] 프리팹에 HWJ_BossBrainSystem이 없습니다.");
            }
            else if (!brain.UsesTwoBarPhaseHealth)
            {
                errors.Add("[중간보스1] 격투가형 보스는 두 줄 체력 전환을 사용해야 하므로 Use Two Bar Phase Health가 켜져 있어야 합니다.");
            }

            if (patternSystem == null)
            {
                errors.Add("[중간보스1] 프리팹에 HWJ_BossPatternSystem이 없습니다.");
            }
            else
            {
                ValidateNonEmptyPatternList("[중간보스1]", patternSystem, errors);
                ValidateNonEmptySpecialExecutorList("[중간보스1]", patternSystem, errors);
            }

            ValidateRequiredFighterComponent<HWJ_FighterBossComboSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossChargeSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossUppercutSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossGroundSlamSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossPhaseTwoPatternSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossDeathSystem>("[중간보스1]", prefabRoot, errors);
            ValidateRequiredFighterComponent<HWJ_FighterBossAnimatorSystem>("[중간보스1]", prefabRoot, errors);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateMidBoss2Prefab(
        GameObject prefabAsset,
        HWJ_RootObjectDataSO expectedRootData,
        HWJ_GameplayDatabaseSO gameplayDatabase,
        List<string> errors)
    {
        if (prefabAsset == null)
        {
            errors.Add($"중간보스2 프리팹이 없습니다: {MidBoss2PrefabPath}");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MidBoss2PrefabPath);

        try
        {
            HWJ_RootObjectDataResolver resolver = prefabRoot.GetComponent<HWJ_RootObjectDataResolver>();
            HWJ_RuntimeSaveIdentity saveIdentity = prefabRoot.GetComponent<HWJ_RuntimeSaveIdentity>();
            HWJ_BossBrainSystem brain = prefabRoot.GetComponent<HWJ_BossBrainSystem>();
            HWJ_BossPatternSystem patternSystem = prefabRoot.GetComponent<HWJ_BossPatternSystem>();
            HWJ_MidBossPatternSystem midBossPattern = prefabRoot.GetComponent<HWJ_MidBossPatternSystem>();

            if (resolver == null || resolver.RootObjectData != expectedRootData)
            {
                errors.Add("프리팹의 RootObjectDataResolver가 중간보스2 RootObjectData를 참조하지 않습니다.");
            }

            ValidateMidBoss2SaveIdentity(resolver, saveIdentity, errors);

            if (brain == null)
            {
                errors.Add("프리팹에 HWJ_BossBrainSystem이 없습니다.");
            }
            else if (brain.UsesTwoBarPhaseHealth)
            {
                errors.Add("중간보스2는 50% 페이즈 전환을 사용해야 하므로 Use Two Bar Phase Health가 꺼져 있어야 합니다.");
            }

            if (patternSystem == null)
            {
                errors.Add("프리팹에 HWJ_BossPatternSystem이 없습니다.");
            }
            else
            {
                ValidatePrefabPatternList(patternSystem, errors);
                ValidateMidBoss2PatternSystemSettings(patternSystem, midBossPattern, gameplayDatabase, errors);
            }

            if (midBossPattern == null || !midBossPattern.enabled)
            {
                errors.Add("프리팹의 HWJ_MidBossPatternSystem이 없거나 꺼져 있습니다.");
            }
            else
            {
                ValidateSummonSetup(midBossPattern, errors);
            }

            ValidateMidBoss2RequiredRuntimeComponents(prefabRoot, errors);
            ValidateMidBoss2RigidbodySettings(prefabRoot, errors);
            ValidateMidBoss2DialogueText(prefabRoot, errors);
            ValidateMidBoss2CameraFraming(prefabRoot, errors);
            ValidateNoFighterBossResidue(prefabRoot, errors);
            ValidateNoMonsterRuntimeResidue(prefabRoot, errors);
            ValidateGenericBossHealthBar(prefabRoot, errors);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateNonEmptyPatternList(
        string label,
        HWJ_BossPatternSystem patternSystem,
        List<string> errors)
    {
        SerializedObject serializedPatternSystem = new SerializedObject(patternSystem);
        SerializedProperty patterns = serializedPatternSystem.FindProperty("patterns");

        if (patterns == null || !patterns.isArray || patterns.arraySize == 0)
        {
            errors.Add($"{label} HWJ_BossPatternSystem의 Patterns 배열이 비어 있습니다.");
        }
    }

    private static void ValidateNonEmptySpecialExecutorList(
        string label,
        HWJ_BossPatternSystem patternSystem,
        List<string> errors)
    {
        SerializedObject serializedPatternSystem = new SerializedObject(patternSystem);
        SerializedProperty executors = serializedPatternSystem.FindProperty("specialPatternExecutors");

        if (executors == null || !executors.isArray || executors.arraySize == 0)
        {
            errors.Add($"{label} HWJ_BossPatternSystem의 Special Pattern Executors 배열이 비어 있습니다.");
        }
    }

    private static void ValidateRequiredFighterComponent<T>(
        string label,
        GameObject prefabRoot,
        List<string> errors) where T : Component
    {
        if (prefabRoot.GetComponent<T>() == null)
        {
            errors.Add($"{label} 프리팹에 {typeof(T).Name} 컴포넌트가 없습니다.");
        }
    }

    private static void ValidateMidBoss2RequiredRuntimeComponents(
        GameObject prefabRoot,
        List<string> errors)
    {
        ValidateRequiredRuntimeComponent<Rigidbody2D>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<Collider2D>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<SpriteRenderer>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<Animator>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_RootObjectDataResolver>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_RuntimeSaveIdentity>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_RuntimeStatusSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_CombatSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_CombatExecutionSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_SkillActionSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_BossPatternSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_BossBrainSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_MidBossPatternSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_CharacterMotionSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_BossDialogueBubbleSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_BossDuplicateGuardSystem>(prefabRoot, errors);
        ValidateRequiredRuntimeComponent<HWJ_BossCameraFocusSystem>(prefabRoot, errors);
    }

    private static void ValidateMidBoss2CameraFraming(
        GameObject prefabRoot,
        List<string> errors)
    {
        HWJ_BossCameraFocusSystem cameraFocus =
            prefabRoot.GetComponent<HWJ_BossCameraFocusSystem>();

        if (cameraFocus == null)
        {
            return;
        }

        if (!Mathf.Approximately(cameraFocus.DialogueFocusOrthographicSize, 5.5f))
        {
            errors.Add("[중간보스2] 대화 카메라 Orthographic Size는 넓은 구도 기준인 5.5여야 합니다.");
        }

        if (!Mathf.Approximately(cameraFocus.DialogueFocusFieldOfView, 36f))
        {
            errors.Add("[중간보스2] 대화 카메라 Field Of View는 넓은 구도 기준인 36이어야 합니다.");
        }
    }

    private static void ValidateRequiredRuntimeComponent<T>(
        GameObject prefabRoot,
        List<string> errors) where T : Component
    {
        if (prefabRoot.GetComponent<T>() == null)
        {
            errors.Add($"중간보스2 프리팹에 필수 런타임 컴포넌트 {typeof(T).Name}가 없습니다.");
        }
    }

    private static void ValidateMidBoss2DialogueText(
        GameObject prefabRoot,
        List<string> errors)
    {
        HWJ_BossDialogueBubbleSystem dialogueSystem =
            prefabRoot.GetComponent<HWJ_BossDialogueBubbleSystem>();

        if (dialogueSystem == null)
        {
            return;
        }

        SerializedObject serializedDialogue = new SerializedObject(dialogueSystem);
        List<string> dialogueLines = new List<string>();
        AddStringArrayValues(serializedDialogue, "introDialogueSequence", dialogueLines);
        AddStringArrayValues(serializedDialogue, "phaseTwoDialogueSequence", dialogueLines);
        AddStringArrayValues(serializedDialogue, "deathDialogueSequence", dialogueLines);

        SerializedProperty soulLostDialogue = serializedDialogue.FindProperty("soulLostDialogue");

        if (soulLostDialogue != null && !string.IsNullOrWhiteSpace(soulLostDialogue.stringValue))
        {
            dialogueLines.Add(soulLostDialogue.stringValue);
        }

        if (dialogueLines.Count == 0)
        {
            errors.Add("중간보스2 대사 데이터가 비어 있습니다.");
            return;
        }

        bool containsSwordConcept = false;

        for (int i = 0; i < dialogueLines.Count; i++)
        {
            string line = dialogueLines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Contains("주먹") || line.Contains("격투"))
            {
                errors.Add("중간보스2는 검 물리 보스이므로 대사에 주먹/격투가 컨셉이 남아 있으면 안 됩니다.");
                return;
            }

            containsSwordConcept |= line.Contains("검") || line.Contains("기사");
        }

        if (!containsSwordConcept)
        {
            errors.Add("중간보스2 대사에는 검 또는 기사단장 컨셉을 확인할 수 있는 문장이 하나 이상 있어야 합니다.");
        }
    }

    private static void AddStringArrayValues(
        SerializedObject serializedObject,
        string propertyName,
        List<string> values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || !property.isArray)
        {
            return;
        }

        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(i);

            if (item != null && !string.IsNullOrWhiteSpace(item.stringValue))
            {
                values.Add(item.stringValue);
            }
        }
    }

    private static void ValidateMidBoss2RigidbodySettings(
        GameObject prefabRoot,
        List<string> errors)
    {
        Rigidbody2D body = prefabRoot.GetComponent<Rigidbody2D>();

        if (body == null)
        {
            return;
        }

        if (body.bodyType != RigidbodyType2D.Dynamic)
        {
            errors.Add("중간보스2 Rigidbody2D는 전투 중 피격/지형 충돌을 받기 위해 Dynamic이어야 합니다.");
        }

        if (!body.simulated)
        {
            errors.Add("중간보스2 Rigidbody2D의 Simulated가 꺼져 있으면 전투/충돌 테스트가 동작하지 않습니다.");
        }

        if (body.gravityScale <= 0f)
        {
            errors.Add("중간보스2 Rigidbody2D의 기본 Gravity Scale은 0보다 커야 합니다. 특수 패턴 중에만 코드가 임시로 0으로 바꿉니다.");
        }

        if ((body.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
        {
            errors.Add("중간보스2 Rigidbody2D는 충돌 중 넘어지지 않도록 Freeze Rotation이 켜져 있어야 합니다.");
        }
    }

    private static void ValidateMidBoss2SaveIdentity(
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeSaveIdentity saveIdentity,
        List<string> errors)
    {
        if (saveIdentity == null)
        {
            errors.Add("중간보스2 프리팹에는 보스 처치 보상 중복 방지를 위한 HWJ_RuntimeSaveIdentity가 있어야 합니다.");
            return;
        }

        HWJ_SaveIdentityValidationResult identityResult =
            HWJ_RuntimeSaveIdentity.ValidateRewardClaimIdentity(saveIdentity, resolver);

        if (!identityResult.Succeeded)
        {
            errors.Add($"중간보스2 저장 ID가 보상 Claim ID를 만들 수 없습니다: {identityResult.Message}");
            return;
        }

        if (identityResult.RootObjectId != MidBoss2ObjectId)
        {
            errors.Add($"중간보스2 저장 ID의 RootObjectId는 {MidBoss2ObjectId}이어야 합니다.");
        }

        if (identityResult.RewardClaimId != MidBoss2ObjectId + ":boss_mid_02_captain_scene")
        {
            errors.Add("중간보스2 보상 Claim ID가 기획 기준과 다릅니다. Stable Instance Id는 boss_mid_02_captain_scene이어야 합니다.");
        }
    }

    private static void ValidateNoFighterBossResidue(
        GameObject prefabRoot,
        List<string> errors)
    {
        ValidateNoUnexpectedComponent<HWJ_FighterBossComboSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossChargeSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossUppercutSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossGroundSlamSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossPhaseTwoPatternSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossDeathSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossAnimatorSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_FighterBossAnimationEvents>(prefabRoot, errors);

        HWJ_FighterBossHitboxSystem[] hitboxes =
            prefabRoot.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        if (hitboxes.Length > 0)
        {
            errors.Add($"중간보스2는 물리형 보스이므로 FighterBoss 전용 히트박스가 남아 있으면 안 됩니다. 현재 {hitboxes.Length}개가 남아 있습니다.");
        }

        HWJ_FighterBossHealthBarSystem[] legacyHealthBars =
            prefabRoot.GetComponentsInChildren<HWJ_FighterBossHealthBarSystem>(true);

        if (legacyHealthBars.Length > 0)
        {
            errors.Add("중간보스2 체력바는 HWJ_BossHealthBarSystem을 사용해야 하므로 HWJ_FighterBossHealthBarSystem이 남아 있으면 안 됩니다.");
        }

        ValidateNoUnexpectedChildObjects(prefabRoot, MidBoss2FighterResidueChildNames, errors);
    }

    private static void ValidateNoMonsterRuntimeResidue(
        GameObject prefabRoot,
        List<string> errors)
    {
        ValidateNoUnexpectedComponent<HWJ_EnemyAttackSystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_MonsterAISystem>(prefabRoot, errors);
        ValidateNoUnexpectedComponent<HWJ_EnemyNavigationSystem>(prefabRoot, errors);
    }

    private static void ValidateGenericBossHealthBar(
        GameObject prefabRoot,
        List<string> errors)
    {
        HWJ_BossHealthBarSystem healthBar =
            prefabRoot.GetComponentInChildren<HWJ_BossHealthBarSystem>(true);

        if (healthBar == null)
        {
            errors.Add("중간보스2 프리팹에는 범용 보스 체력바인 HWJ_BossHealthBarSystem이 있어야 합니다.");
        }
    }

    private static void ValidateNoUnexpectedComponent<T>(
        GameObject prefabRoot,
        List<string> errors) where T : Component
    {
        if (prefabRoot.GetComponent<T>() != null)
        {
            errors.Add($"중간보스2는 물리형 보스이므로 {typeof(T).Name} 컴포넌트가 남아 있으면 안 됩니다.");
        }
    }

    private static void ValidateNoUnexpectedChildObjects(
        GameObject prefabRoot,
        IReadOnlyCollection<string> childNames,
        List<string> errors)
    {
        Transform[] children = prefabRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null
                || child == prefabRoot.transform
                || !childNames.Contains(child.name))
            {
                continue;
            }

            errors.Add($"중간보스2에 FighterBoss 잔여 오브젝트가 남아 있습니다: {GetHierarchyPath(child)}");
        }
    }

    private static void RemoveRootComponentIfPresent<T>(
        GameObject prefabRoot,
        List<string> removedItems) where T : Component
    {
        T component = prefabRoot.GetComponent<T>();

        if (component == null)
        {
            return;
        }

        removedItems.Add($"{typeof(T).Name} 컴포넌트");
        UnityEngine.Object.DestroyImmediate(component);
    }

    private static void RemoveChildComponentsIfPresent<T>(
        GameObject prefabRoot,
        List<string> removedItems) where T : Component
    {
        T[] components = prefabRoot.GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component == null)
            {
                continue;
            }

            removedItems.Add($"{typeof(T).Name}: {GetHierarchyPath(component.transform)}");
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void RemoveNamedChildObjects(
        GameObject prefabRoot,
        IReadOnlyCollection<string> childNames,
        List<string> removedItems)
    {
        Transform[] children = prefabRoot.GetComponentsInChildren<Transform>(true);

        for (int i = children.Length - 1; i >= 0; i--)
        {
            Transform child = children[i];

            if (child == null
                || child == prefabRoot.transform
                || !childNames.Contains(child.name))
            {
                continue;
            }

            removedItems.Add($"잔여 오브젝트: {GetHierarchyPath(child)}");
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "-";
        }

        Stack<string> names = new Stack<string>();
        Transform current = transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static void ValidatePrefabPatternList(
        HWJ_BossPatternSystem patternSystem,
        List<string> errors)
    {
        SerializedObject serializedPatternSystem = new SerializedObject(patternSystem);
        SerializedProperty patterns = serializedPatternSystem.FindProperty("patterns");

        if (patterns == null || !patterns.isArray)
        {
            errors.Add("HWJ_BossPatternSystem의 Patterns 배열을 읽을 수 없습니다.");
            return;
        }

        if (patterns.arraySize != MidBossPhysicalPatternPaths.Length)
        {
            errors.Add($"프리팹의 Patterns 수가 {MidBossPhysicalPatternPaths.Length}개가 아닙니다.");
            return;
        }

        for (int i = 0; i < MidBossPhysicalPatternPaths.Length; i++)
        {
            HWJ_BossPatternDataSO expectedPattern =
                AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(MidBossPhysicalPatternPaths[i]);
            UnityEngine.Object assignedPattern =
                patterns.GetArrayElementAtIndex(i).objectReferenceValue;

            if (assignedPattern != expectedPattern)
            {
                errors.Add($"프리팹 Patterns[{i}]가 HWJ_MidBossPhysical_Pattern{i + 1}이 아닙니다.");
            }
        }
    }

    private static void ValidateMidBoss2PatternSystemSettings(
        HWJ_BossPatternSystem patternSystem,
        HWJ_MidBossPatternSystem midBossPattern,
        HWJ_GameplayDatabaseSO gameplayDatabase,
        List<string> errors)
    {
        SerializedObject serializedPatternSystem = new SerializedObject(patternSystem);

        SerializedProperty autoUsePatterns =
            serializedPatternSystem.FindProperty("autoUsePatterns");

        if (autoUsePatterns != null && autoUsePatterns.boolValue)
        {
            errors.Add("중간보스2는 BossBrainSystem이 패턴 타이밍을 제어해야 하므로 Auto Use Patterns가 꺼져 있어야 합니다.");
        }

        SerializedProperty stageOnePatternSystem =
            serializedPatternSystem.FindProperty("stageOnePatternSystem");

        if (stageOnePatternSystem != null && stageOnePatternSystem.objectReferenceValue != null)
        {
            errors.Add("중간보스2는 1스테이지 활 보스 전용 패턴 시스템을 사용하지 않으므로 Stage One Pattern System이 비어 있어야 합니다.");
        }

        SerializedProperty specialPatternExecutors =
            serializedPatternSystem.FindProperty("specialPatternExecutors");

        if (specialPatternExecutors == null || !specialPatternExecutors.isArray)
        {
            errors.Add("중간보스2 HWJ_BossPatternSystem의 Special Pattern Executors 배열을 읽을 수 없습니다.");
            return;
        }

        bool hasMidBossExecutor = false;

        for (int i = 0; i < specialPatternExecutors.arraySize; i++)
        {
            UnityEngine.Object executor =
                specialPatternExecutors.GetArrayElementAtIndex(i).objectReferenceValue;

            if (midBossPattern != null && executor == midBossPattern)
            {
                hasMidBossExecutor = true;
                break;
            }
        }

        if (!hasMidBossExecutor)
        {
            errors.Add("중간보스2 HWJ_BossPatternSystem의 Special Pattern Executors에 HWJ_MidBossPatternSystem이 연결되어 있어야 합니다.");
        }

        SerializedProperty useGameplayPatternRule =
            serializedPatternSystem.FindProperty("useGameplayPatternRule");

        if (useGameplayPatternRule != null && !useGameplayPatternRule.boolValue)
        {
            errors.Add("중간보스2는 공통 보스 패턴 규칙을 사용해야 하므로 Use Gameplay Pattern Rule이 켜져 있어야 합니다.");
        }

        ValidateMidBoss2PatternRuleReferences(serializedPatternSystem, gameplayDatabase, errors);
        ValidateMidBoss2PatternTuning(midBossPattern, errors);
    }

    private static void ValidateMidBoss2PatternRuleReferences(
        SerializedObject serializedPatternSystem,
        HWJ_GameplayDatabaseSO gameplayDatabase,
        List<string> errors)
    {
        SerializedProperty executionCoreReference =
            serializedPatternSystem.FindProperty("bossPatternExecutionCore");
        SerializedProperty executionCoreId =
            serializedPatternSystem.FindProperty("bossPatternExecutionCoreId");
        SerializedProperty ruleReference =
            serializedPatternSystem.FindProperty("bossPatternRule");
        SerializedProperty ruleId =
            serializedPatternSystem.FindProperty("bossPatternRuleId");

        if (executionCoreId == null || executionCoreId.stringValue != MidBossPatternExecutionCoreId)
        {
            errors.Add($"중간보스2 BossPatternSystem의 RuleExecutionCore ID는 {MidBossPatternExecutionCoreId}이어야 합니다.");
        }

        if (ruleId == null || ruleId.stringValue != MidBossPatternRuleId)
        {
            errors.Add($"중간보스2 BossPatternSystem의 GameplayRule ID는 {MidBossPatternRuleId}이어야 합니다.");
        }

        HWJ_RuleExecutionCoreSO directExecutionCore =
            executionCoreReference != null ? executionCoreReference.objectReferenceValue as HWJ_RuleExecutionCoreSO : null;

        if (directExecutionCore != null && directExecutionCore.ExecutionCoreId != MidBossPatternExecutionCoreId)
        {
            errors.Add($"중간보스2 BossPatternSystem의 직접 연결 RuleExecutionCore ID가 {MidBossPatternExecutionCoreId}가 아닙니다.");
        }

        HWJ_GameplayRuleSO directRule =
            ruleReference != null ? ruleReference.objectReferenceValue as HWJ_GameplayRuleSO : null;

        if (directRule != null && directRule.RuleId != MidBossPatternRuleId)
        {
            errors.Add($"중간보스2 BossPatternSystem의 직접 연결 GameplayRule ID가 {MidBossPatternRuleId}가 아닙니다.");
        }

        if (gameplayDatabase == null)
        {
            errors.Add("중간보스2 BossPatternSystem의 규칙 ID를 확인할 GameplayDatabase가 없습니다.");
            return;
        }

        if (!gameplayDatabase.TryGetRuleExecutionCore(MidBossPatternExecutionCoreId, out HWJ_RuleExecutionCoreSO resolvedExecutionCore)
            || resolvedExecutionCore == null)
        {
            errors.Add($"GameplayDatabase에서 중간보스2 패턴 실행 코어 {MidBossPatternExecutionCoreId}를 찾을 수 없습니다.");
        }

        if (!gameplayDatabase.TryGetGameplayRule(MidBossPatternRuleId, out HWJ_GameplayRuleSO resolvedRule)
            || resolvedRule == null)
        {
            errors.Add($"GameplayDatabase에서 중간보스2 패턴 규칙 {MidBossPatternRuleId}를 찾을 수 없습니다.");
        }
    }

    private static void ValidateMidBoss2PatternTuning(
        HWJ_MidBossPatternSystem midBossPattern,
        List<string> errors)
    {
        if (midBossPattern == null)
        {
            return;
        }

        SerializedObject serializedMidBossPattern = new SerializedObject(midBossPattern);

        ValidateSerializedString(
            serializedMidBossPattern,
            "executorKey",
            MidBossExecutorKey,
            "중간보스2 패턴 실행 키",
            errors);
        ValidateSerializedFloat(
            serializedMidBossPattern,
            "pattern1HpLossIntervalRatio",
            MidBoss2Pattern1HpLossIntervalRatio,
            "패턴 1 체력 감소 사용 간격",
            errors);
        ValidateSerializedInt(
            serializedMidBossPattern,
            "pattern1SummonCount",
            MidBoss2Pattern1SummonCount,
            "패턴 1 소환 몬스터 수",
            errors);
        ValidateSerializedFloat(
            serializedMidBossPattern,
            "pattern1WhistleSeconds",
            MidBoss2Pattern1WhistleSeconds,
            "패턴 1 호루라기 대기 시간",
            errors);
        ValidateSerializedFloat(
            serializedMidBossPattern,
            "pattern4FixedMaxHpDamageRatio",
            MidBoss2Pattern4FixedDamageRatio,
            "패턴 4 최대 체력 고정 피해 비율",
            errors);
        ValidateSerializedFloat(
            serializedMidBossPattern,
            "pattern4GroggySeconds",
            MidBoss2Pattern4GroggySeconds,
            "패턴 4 그로기 시간",
            errors);
        ValidateSerializedFloat(
            serializedMidBossPattern,
            "pattern6VanishDelaySeconds",
            MidBoss2Pattern6VanishDelaySeconds,
            "패턴 6 은신 전 대기 시간",
            errors);
    }

    private static void ValidateSerializedString(
        SerializedObject serializedObject,
        string propertyName,
        string expectedValue,
        string label,
        List<string> errors)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            errors.Add($"{label} 검증 필드를 찾을 수 없습니다: {propertyName}");
            return;
        }

        if (property.stringValue != expectedValue)
        {
            errors.Add($"{label} 값은 {expectedValue}이어야 합니다. 현재 값: {property.stringValue}");
        }
    }

    private static void ValidateSerializedFloat(
        SerializedObject serializedObject,
        string propertyName,
        float expectedValue,
        string label,
        List<string> errors)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            errors.Add($"{label} 검증 필드를 찾을 수 없습니다: {propertyName}");
            return;
        }

        if (!Mathf.Approximately(property.floatValue, expectedValue))
        {
            errors.Add($"{label} 값은 {expectedValue:0.###}이어야 합니다. 현재 값: {property.floatValue:0.###}");
        }
    }

    private static void ValidateSerializedInt(
        SerializedObject serializedObject,
        string propertyName,
        int expectedValue,
        string label,
        List<string> errors)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            errors.Add($"{label} 검증 필드를 찾을 수 없습니다: {propertyName}");
            return;
        }

        if (property.intValue != expectedValue)
        {
            errors.Add($"{label} 값은 {expectedValue}이어야 합니다. 현재 값: {property.intValue}");
        }
    }

    private static void ValidateSummonSetup(
        HWJ_MidBossPatternSystem midBossPattern,
        List<string> errors)
    {
        SerializedObject serializedMidBossPattern = new SerializedObject(midBossPattern);
        SerializedProperty summonCountProperty = serializedMidBossPattern.FindProperty("pattern1SummonCount");
        SerializedProperty prefabList = serializedMidBossPattern.FindProperty("possessableMonsterPrefabs");
        SerializedProperty rootList = serializedMidBossPattern.FindProperty("possessableMonsterRootObjects");

        int summonCount = summonCountProperty != null
            ? Mathf.Max(0, summonCountProperty.intValue)
            : 4;

        if (prefabList == null || !prefabList.isArray)
        {
            errors.Add("HWJ_MidBossPatternSystem의 소환 몬스터 프리팹 목록을 읽을 수 없습니다.");
            return;
        }

        if (rootList == null || !rootList.isArray)
        {
            errors.Add("HWJ_MidBossPatternSystem의 소환 몬스터 RootObjectData 목록을 읽을 수 없습니다.");
            return;
        }

        if (prefabList.arraySize < summonCount)
        {
            errors.Add($"소환 몬스터 프리팹 수가 패턴 1 소환 수({summonCount})보다 적습니다.");
        }

        if (rootList.arraySize < summonCount)
        {
            errors.Add($"소환 몬스터 RootObjectData 수가 패턴 1 소환 수({summonCount})보다 적습니다.");
        }

        int pairCount = Mathf.Min(summonCount, prefabList.arraySize, rootList.arraySize);

        for (int i = 0; i < pairCount; i++)
        {
            GameObject summonPrefab =
                prefabList.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            HWJ_RootObjectDataSO summonRoot =
                rootList.GetArrayElementAtIndex(i).objectReferenceValue as HWJ_RootObjectDataSO;

            if (summonPrefab == null)
            {
                errors.Add($"소환 몬스터 프리팹 {i + 1}번이 비어 있습니다.");
                continue;
            }

            if (summonRoot == null)
            {
                errors.Add($"소환 몬스터 RootObjectData {i + 1}번이 비어 있습니다.");
                continue;
            }

            if (summonRoot.ObjectType != HWJ_ObjectType.Enemy)
            {
                errors.Add($"소환 몬스터 RootObjectData {i + 1}번은 Enemy 타입이어야 합니다.");
            }

            if (!summonRoot.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyTypeData)
                || enemyTypeData.PossessionBody == null
                || !enemyTypeData.PossessionBody.canBePossessed)
            {
                errors.Add($"소환 몬스터 RootObjectData {i + 1}번은 빙의 가능한 Enemy TypeData를 가져야 합니다.");
                continue;
            }

            if (enemyTypeData.PossessionBody.requiresDefeatedState)
            {
                errors.Add($"소환 몬스터 RootObjectData {i + 1}번은 보스 패턴 1에서 즉시 빙의 가능한 대상이어야 하므로 Requires Defeated State가 꺼져 있어야 합니다.");
            }

            HWJ_RootObjectDataResolver prefabResolver =
                summonPrefab.GetComponent<HWJ_RootObjectDataResolver>();

            if (prefabResolver == null)
            {
                errors.Add($"소환 몬스터 프리팹 {i + 1}번에 HWJ_RootObjectDataResolver가 없습니다.");
                continue;
            }

            if (prefabResolver.RootObjectData != summonRoot)
            {
                errors.Add($"소환 몬스터 프리팹 {i + 1}번과 RootObjectData {i + 1}번이 서로 다른 데이터를 참조합니다.");
            }
        }
    }

    private static void ThrowIfValidationFailed(
        string validationLabel,
        List<string> errors)
    {
        if (errors == null || errors.Count == 0)
        {
            return;
        }

        string message = $"[HWJ] {validationLabel} 배치 검증 실패:\n- " + string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidOperationException(message);
    }

    private static void DrawTwoButtons(
        string leftLabel,
        Action leftAction,
        string rightLabel,
        Action rightAction)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(leftLabel))
        {
            leftAction?.Invoke();
        }

        if (GUILayout.Button(rightLabel))
        {
            rightAction?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }
}
