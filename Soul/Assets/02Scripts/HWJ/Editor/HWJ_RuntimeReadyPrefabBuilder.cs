using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵과 최종 애니메이션 없이도 HWJ 런타임 기능을 바로 확인할 수 있는 프리팹 세트를 생성합니다.
/// 플레이어, 몬스터, 빙의, 정신력, 전투, 보상, 풀링, HUD를 서로 연결된 상태로 저장합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_RuntimeReadyPrefabBuilder
{
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string OutputRoot = AssetRoot + "/Prefabs/Generated/RuntimeReady";
    private const string EnemyOutputRoot = OutputRoot + "/Enemies";
    private const string CorpseOutputRoot = OutputRoot + "/Corpses";
    private const string RewardOutputRoot = OutputRoot + "/Rewards";
    private const string DatabaseRoot = AssetRoot + "/ScriptableObjects/Database";

    private const string GameplayDatabasePath = DatabaseRoot + "/HWJ_GameplayDatabase.asset";
    private const string ObjectPoolDataPath = DatabaseRoot + "/HWJ_ObjectPoolData_RuntimeReady.asset";
    private const string PlayerRootPath = AssetRoot + "/ScriptableObjects/RootObjects/HWJ_Player_Test_RootObjectData.asset";
    private const string InputBindingPath = AssetRoot + "/ScriptableObjects/Input/HWJ_DefaultPlayerInputBindings.asset";
    private const string LevelUpDataPath = AssetRoot + "/ScriptableObjects/LevelTables/HWJ_Player_Default_LevelUpData.asset";
    private const string GameOverDataPath = AssetRoot + "/ScriptableObjects/Systems/HWJ_GameOverData_Default.asset";

    private const string ExperienceOrbPath = AssetRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab";
    private const string HitEffectPath = AssetRoot + "/Prefabs/Generated/Effects/HWJ_Effect_HitImpact.prefab";
    private const string MarkerSpritePath = AssetRoot + "/Art/Generated/FunctionShowcase/HWJ_Showcase_Box.png";
    private const string PlayerSpritePath = AssetRoot + "/Art/Player/HWJ_GHOSTP_Frame00.png";

    private const string PlayerPrefabPath = OutputRoot + "/HWJ_Runtime_Player_Soul.prefab";
    private const string CorePrefabPath = OutputRoot + "/HWJ_Runtime_GameplayCore.prefab";
    private const string CameraPrefabPath = OutputRoot + "/HWJ_Runtime_Camera2D.prefab";
    private const string HudPrefabPath = OutputRoot + "/HWJ_Runtime_GameplayHUD.prefab";
    private const string RewardsPrefabPath = RewardOutputRoot + "/HWJ_Runtime_Rewards_All.prefab";
    private const string AllSystemsPrefabPath = OutputRoot + "/HWJ_Runtime_AllSystems_DropIn.prefab";

    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunRuntimeReadyPrefabBuild.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_RuntimeReadyPrefabReport.md";

    private static readonly EnemyPrefabSpec[] PossessableEnemySpecs =
    {
        new EnemyPrefabSpec("Sword", "HWJ_EnemyCorpse_Sword_RootObjectData.asset"),
        new EnemyPrefabSpec("Axe", "HWJ_EnemyCorpse_Axe_RootObjectData.asset"),
        new EnemyPrefabSpec("Bow", "HWJ_EnemyCorpse_Bow_RootObjectData.asset"),
        new EnemyPrefabSpec("Lance", "HWJ_EnemyCorpse_Lance_RootObjectData.asset"),
        new EnemyPrefabSpec("Shield", "HWJ_EnemyCorpse_Shield_RootObjectData.asset")
    };

    private static readonly EnemyPrefabSpec[] NoCorpseEnemySpecs =
    {
        new EnemyPrefabSpec("Sword", "HWJ_EnemyNoCorpse_Sword_RootObjectData.asset"),
        new EnemyPrefabSpec("Axe", "HWJ_EnemyNoCorpse_Axe_RootObjectData.asset"),
        new EnemyPrefabSpec("Bow", "HWJ_EnemyNoCorpse_Bow_RootObjectData.asset"),
        new EnemyPrefabSpec("Lance", "HWJ_EnemyNoCorpse_Lance_RootObjectData.asset"),
        new EnemyPrefabSpec("Shield", "HWJ_EnemyNoCorpse_Shield_RootObjectData.asset")
    };

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_RuntimeReadyPrefabBuilder()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Prefabs/Build Runtime Ready Gameplay Prefabs")]
    public static void BuildRuntimeReadyPrefabs()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ runtime-ready prefab build is already running.");
            return;
        }

        isRunning = true;
        List<string> report = new List<string>();

        try
        {
            EnsureFolders();
            RequireAsset<HWJ_GameplayDatabaseSO>(GameplayDatabasePath);
            RequireAsset<HWJ_RootObjectDataSO>(PlayerRootPath);
            RequireAsset<GameObject>(ExperienceOrbPath);
            ConfigurePossessableEnemyTypeData(report);

            SavePrefab(CreatePlayerPrefab(), PlayerPrefabPath, report);
            SavePrefab(CreateCameraPrefab(), CameraPrefabPath, report);
            SavePrefab(CreateHudPrefab(), HudPrefabPath, report);

            for (int i = 0; i < PossessableEnemySpecs.Length; i++)
            {
                EnemyPrefabSpec spec = PossessableEnemySpecs[i];
                SavePrefab(
                    CreateEnemyPrefab(spec, true, false),
                    GetLiveEnemyPrefabPath(spec),
                    report);
                SavePrefab(
                    CreateEnemyPrefab(spec, false, true),
                    GetCorpsePrefabPath(spec),
                    report);
            }

            for (int i = 0; i < NoCorpseEnemySpecs.Length; i++)
            {
                EnemyPrefabSpec spec = NoCorpseEnemySpecs[i];
                SavePrefab(
                    CreateEnemyPrefab(spec, true, false),
                    GetNoCorpseEnemyPrefabPath(spec),
                    report);
            }

            SavePrefab(CreateRewardCollectionPrefab(), RewardsPrefabPath, report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            HWJ_ObjectPoolDataSO poolData = CreateOrUpdatePoolData(report);
            WireEnemyExperienceOrbRewards(report);
            SavePrefab(CreateGameplayCorePrefab(poolData), CorePrefabPath, report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SavePrefab(CreateAllSystemsDropInPrefab(), AllSystemsPrefabPath, report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateGeneratedAssets(report);
            WriteReport("Success", report, null);
            Debug.Log($"HWJ runtime-ready prefabs created: {OutputRoot}");
        }
        catch (Exception exception)
        {
            WriteReport("Failed", report, exception);
            Debug.LogException(exception);
            throw;
        }
        finally
        {
            isRunning = false;
        }
    }

    [MenuItem("Tools/HWJ/Prefabs/Create Runtime Ready Prefab Build Flag")]
    public static void CreateBuildFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
        Debug.Log($"HWJ runtime-ready prefab flag created: {FlagFilePath}");
    }

    private static void PollFlagFile()
    {
        if (EditorApplication.timeSinceStartup < nextFlagCheckTime)
        {
            return;
        }

        nextFlagCheckTime = EditorApplication.timeSinceStartup + 2d;

        if (!File.Exists(FlagFilePath)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating)
        {
            return;
        }

        string request = File.ReadAllText(FlagFilePath).Trim();

        if (!string.Equals(request, "run", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Delete(FlagFilePath);
        BuildRuntimeReadyPrefabs();
    }

    /// <summary>
    /// 5종 무기 몬스터를 살아있을 때는 R 미니게임, 죽은 뒤에는 E 즉시 빙의 대상으로 사용하도록
    /// 각 EnemyTypeData SO의 고정 설정을 한 곳에서 맞춥니다.
    /// </summary>
    private static void ConfigurePossessableEnemyTypeData(List<string> report)
    {
        for (int i = 0; i < PossessableEnemySpecs.Length; i++)
        {
            EnemyPrefabSpec spec = PossessableEnemySpecs[i];
            HWJ_RootObjectDataSO rootData =
                RequireAsset<HWJ_RootObjectDataSO>(spec.RootObjectDataPath);

            if (!rootData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
                || enemyData.PossessionBody == null
                || enemyData.Role == null)
            {
                throw new InvalidOperationException(
                    $"Possessable enemy TypeData is missing: {spec.RootObjectDataPath}");
            }

            enemyData.Role.leavesCorpseOnDeath = true;
            enemyData.Role.isPossessableBody = true;
            enemyData.PossessionBody.canBePossessed = true;
            enemyData.PossessionBody.requiresDefeatedState = false;
            enemyData.PossessionBody.livePossessionMaxMental = 100f;
            enemyData.PossessionBody.livePossessionMentalCostOnSuccess = 10f;
            enemyData.PossessionBody.livePossessionMentalDrainInterval = 1f;
            enemyData.PossessionBody.livePossessionMentalDrainAmount = 1f;
            EditorUtility.SetDirty(enemyData);
            report.Add($"Configured live/corpse possession SO: {enemyData.name}");
        }
    }

    private static GameObject CreatePlayerPrefab()
    {
        GameObject root = new GameObject("HWJ_Runtime_Player_Soul");
        root.tag = "Player";
        SetLayerIfExists(root, "Player");

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 1.3f);
        collider.offset = new Vector2(0f, 0.05f);

        GameObject visual = CreatePlayerVisual(root.transform);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        Animator animator = visual.GetComponent<Animator>();

        root.AddComponent<HWJ_RuntimeObjectContext>();
        HWJ_RootObjectDataResolver resolver = root.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(RequireAsset<HWJ_RootObjectDataSO>(PlayerRootPath));
        HWJ_RuntimeStatusSystem status = root.AddComponent<HWJ_RuntimeStatusSystem>();
        HWJ_CombatSystem combat = root.AddComponent<HWJ_CombatSystem>();
        HWJ_CombatExecutionSystem combatExecution = root.AddComponent<HWJ_CombatExecutionSystem>();
        HWJ_SkillActionSystem skillAction = root.AddComponent<HWJ_SkillActionSystem>();
        root.AddComponent<HWJ_KnockbackSystem>();
        HWJ_CharacterMotionSystem motion = root.AddComponent<HWJ_CharacterMotionSystem>();
        HWJ_PlayerInputSystem input = root.AddComponent<HWJ_PlayerInputSystem>();
        HWJ_PlayerMovementSystem movement = root.AddComponent<HWJ_PlayerMovementSystem>();
        HWJ_PlayerAttackSystem attack = root.AddComponent<HWJ_PlayerAttackSystem>();
        HWJ_LevelUpSystem level = root.AddComponent<HWJ_LevelUpSystem>();
        root.AddComponent<HWJ_StatOrbProgressSystem>();
        HWJ_SkillUnlockSystem skillUnlock = root.AddComponent<HWJ_SkillUnlockSystem>();
        HWJ_SoulSystem soul = root.AddComponent<HWJ_SoulSystem>();
        root.AddComponent<HWJ_PossessedBodySystem>();
        HWJ_PossessionSystem possession = root.AddComponent<HWJ_PossessionSystem>();
        HWJ_LivePossessionMinigameController possessionMinigame =
            root.AddComponent<HWJ_LivePossessionMinigameController>();
        HWJ_PossessionInteractionController possessionInteraction =
            root.AddComponent<HWJ_PossessionInteractionController>();
        HWJ_BodyDiscoverySystem bodyDiscovery = root.AddComponent<HWJ_BodyDiscoverySystem>();
        HWJ_PossessionMentalSystem mental = root.AddComponent<HWJ_PossessionMentalSystem>();
        root.AddComponent<HWJ_CollapseSystem>();
        root.AddComponent<HWJ_InteractionSystem>();
        root.AddComponent<HWJ_CoreLoopCoordinator>();
        HWJ_HitEffectSystem hitEffect = root.AddComponent<HWJ_HitEffectSystem>();

        SerializedObject inputSo = new SerializedObject(input);
        SetObject(inputSo, "inputBindingData", RequireAsset<HWJ_PlayerInputBindingDataSO>(InputBindingPath));
        inputSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject movementSo = new SerializedObject(movement);
        SetLayerMask(movementSo, "groundLayer", LayerMask.GetMask("Ground"));
        SetBool(movementSo, "phaseThroughCollidersInSoul", true);
        SetBool(movementSo, "swapLayerInSoulState", true);
        SetString(movementSo, "soulLayerName", "Soul");
        SetString(movementSo, "bodyLayerName", "Player");
        SetBool(movementSo, "ignoreEnemyBodyCollision", true);
        movementSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject levelSo = new SerializedObject(level);
        SetObject(levelSo, "levelUpData", RequireAsset<HWJ_LevelUpDataSO>(LevelUpDataPath));
        SetBool(levelSo, "loadLevelDataFromDatabase", true);
        levelSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject skillUnlockSo = new SerializedObject(skillUnlock);
        SetObject(skillUnlockSo, "gameplayDatabase", RequireAsset<HWJ_GameplayDatabaseSO>(GameplayDatabasePath));
        SetBool(skillUnlockSo, "initializeStartingSkillsOnAwake", true);
        SetBool(skillUnlockSo, "autoUnlockLevelSkills", true);
        skillUnlockSo.ApplyModifiedPropertiesWithoutUndo();

        ConfigureSkillAction(skillAction, resolver, status, combat, combatExecution, possession, motion, body);
        ConfigureCombatExecution(combatExecution, true);
        ConfigurePlayerAttack(attack);
        ConfigureMotion(motion, animator, renderer, body, resolver, status, null, GetAllMotionProfiles());
        ConfigureHitEffect(hitEffect, status, resolver);
        ConfigurePlayerReferences(soul, possession, bodyDiscovery, mental);
        ConfigurePossessionInteraction(
            root,
            possession,
            possessionMinigame,
            possessionInteraction);
        CreateWorldBar(root.transform, status, mental, soul, resolver, new Vector3(0f, 1.1f, 0f));
        return root;
    }

    private static GameObject CreateEnemyPrefab(EnemyPrefabSpec spec, bool alive, bool directCorpse)
    {
        HWJ_RootObjectDataSO rootData = RequireAsset<HWJ_RootObjectDataSO>(spec.RootObjectDataPath);
        bool possessable = spec.IsPossessable;
        string prefix = directCorpse
            ? "HWJ_Runtime_Corpse_"
            : possessable ? "HWJ_Runtime_Enemy_Possessable_" : "HWJ_Runtime_Enemy_NoCorpse_";
        GameObject root = new GameObject(prefix + spec.WeaponName);
        SetLayerIfExists(root, "Enemy");

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = alive ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        body.gravityScale = alive ? 3f : 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 1.45f);
        collider.offset = new Vector2(0f, -0.05f);
        collider.isTrigger = directCorpse;

        GameObject visual = CreateRootDataVisual(root.transform, rootData);
        SpriteRenderer renderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        Animator animator = visual.GetComponentInChildren<Animator>(true);

        root.AddComponent<HWJ_RuntimeObjectContext>();
        HWJ_RootObjectDataResolver resolver = root.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(rootData);
        HWJ_RuntimeStatusSystem status = root.AddComponent<HWJ_RuntimeStatusSystem>();
        HWJ_CombatSystem combat = root.AddComponent<HWJ_CombatSystem>();
        HWJ_CombatExecutionSystem combatExecution = root.AddComponent<HWJ_CombatExecutionSystem>();
        HWJ_SkillActionSystem skillAction = root.AddComponent<HWJ_SkillActionSystem>();
        root.AddComponent<HWJ_KnockbackSystem>();
        HWJ_CharacterMotionSystem motion = root.AddComponent<HWJ_CharacterMotionSystem>();
        HWJ_HitEffectSystem hitEffect = root.AddComponent<HWJ_HitEffectSystem>();

        if (possessable)
        {
            root.AddComponent<HWJ_PossessionBodyState>();

            if (alive)
            {
                HWJ_LivePossessionMentalState liveMentalState =
                    root.AddComponent<HWJ_LivePossessionMentalState>();
                liveMentalState.LoadConfigurationFromTypeData();
            }
        }

        if (alive)
        {
            HWJ_EnemyNavigationSystem navigation = root.AddComponent<HWJ_EnemyNavigationSystem>();
            HWJ_EnemyAttackSystem enemyAttack = root.AddComponent<HWJ_EnemyAttackSystem>();
            HWJ_MonsterAISystem monsterAI = root.AddComponent<HWJ_MonsterAISystem>();
            root.AddComponent<HWJ_EnemyDeathLifecycleSystem>();
            ConfigureEnemyBehavior(navigation, enemyAttack, monsterAI);
        }

        SerializedObject statusSo = new SerializedObject(status);
        SetEnum(statusSo, "currentState", directCorpse ? (int)HWJ_RuntimeState.Dead : (int)HWJ_RuntimeState.Idle);
        statusSo.ApplyModifiedPropertiesWithoutUndo();

        ConfigureSkillAction(skillAction, resolver, status, combat, combatExecution, null, motion, body);
        ConfigureCombatExecution(combatExecution, false);
        ConfigureMotion(
            motion,
            animator,
            renderer,
            body,
            resolver,
            status,
            LoadMotionProfile(spec.WeaponName),
            new[] { LoadMotionProfile(spec.WeaponName) });
        ConfigureHitEffect(hitEffect, status, resolver);
        CreateWorldBar(root.transform, status, null, null, resolver, new Vector3(0f, 1.05f, 0f));
        return root;
    }

    private static GameObject CreateGameplayCorePrefab(HWJ_ObjectPoolDataSO poolData)
    {
        GameObject root = new GameObject("HWJ_Runtime_GameplayCore");
        GameObject poolObject = CreateChild(root.transform, "HWJ_ObjectPoolSystem");
        GameObject poolStorage = CreateChild(poolObject.transform, "PoolStorage");
        poolStorage.SetActive(true);
        HWJ_ObjectPoolSystem pool = poolObject.AddComponent<HWJ_ObjectPoolSystem>();
        GameObject spawnerObject = CreateChild(root.transform, "HWJ_SpawnerSystem");
        HWJ_SpawnerSystem spawner = spawnerObject.AddComponent<HWJ_SpawnerSystem>();
        HWJ_GameManager manager = root.AddComponent<HWJ_GameManager>();
        HWJ_GameOverWindowSystem gameOver = root.AddComponent<HWJ_GameOverWindowSystem>();

        SerializedObject poolSo = new SerializedObject(pool);
        SetObject(poolSo, "poolData", poolData);
        SetObject(poolSo, "poolRoot", poolStorage.transform);
        SetBool(poolSo, "prewarmOnAwake", true);
        poolSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerSo = new SerializedObject(manager);
        SetObject(managerSo, "database", RequireAsset<HWJ_GameplayDatabaseSO>(GameplayDatabasePath));
        SetObject(managerSo, "objectPool", pool);
        SetObject(managerSo, "spawner", spawner);
        SetBool(managerSo, "dontDestroyOnLoad", true);
        SetBool(managerSo, "autoFindPlayerResolverInScene", true);
        SetBool(managerSo, "autoAddMissingPlayerCoreSystems", true);
        SetBool(managerSo, "autoPlacePlayerAtSceneStart", true);
        SetBool(managerSo, "autoBindSceneCamerasToPlayer", true);
        SetBool(managerSo, "preservePlayerRuntimeAcrossScenes", true);
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject gameOverSo = new SerializedObject(gameOver);
        SetObject(gameOverSo, "gameOverData", RequireAsset<HWJ_GameOverDataSO>(GameOverDataPath));
        SetObject(gameOverSo, "objectPool", pool);
        gameOverSo.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject CreateCameraPrefab()
    {
        GameObject root = new GameObject("HWJ_Runtime_Camera2D");
        root.tag = "MainCamera";
        Camera camera = root.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.transform.position = new Vector3(0f, 1.5f, -10f);
        root.AddComponent<AudioListener>();
        HWJ_PlayerCameraFollowSystem follow = root.AddComponent<HWJ_PlayerCameraFollowSystem>();
        SerializedObject followSo = new SerializedObject(follow);
        SetBool(followSo, "autoFindPlayerTarget", true);
        followSo.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject CreateHudPrefab()
    {
        GameObject root = new GameObject("HWJ_Runtime_GameplayHUD");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        GameObject panel = CreateUiImage(root.transform, "StatusPanel", new Color(0.035f, 0.04f, 0.05f, 0.9f));
        SetTopLeftRect(panel.GetComponent<RectTransform>(), new Vector2(24f, -24f), new Vector2(900f, 250f));

        Text title = CreateUiText(panel.transform, "Title", "HWJ SYSTEM STATUS", 24, new Color(1f, 0.85f, 0.45f, 1f));
        SetTopLeftRect(title.rectTransform, new Vector2(18f, -14f), new Vector2(860f, 30f));
        Text state = CreateUiText(panel.transform, "State", "상태 확인 중", 22, new Color(0.65f, 0.9f, 1f, 1f));
        SetTopLeftRect(state.rectTransform, new Vector2(18f, -50f), new Vector2(260f, 28f));
        Text resource = CreateUiText(panel.transform, "Resources", "정신력/HP 확인 중", 18, Color.white);
        SetTopLeftRect(resource.rectTransform, new Vector2(18f, -82f), new Vector2(850f, 26f));
        Text growth = CreateUiText(panel.transform, "Growth", "경험치 확인 중", 18, new Color(0.65f, 1f, 0.72f, 1f));
        SetTopLeftRect(growth.rectTransform, new Vector2(18f, -112f), new Vector2(850f, 26f));
        Text action = CreateUiText(panel.transform, "Actions", "가능 행동 확인 중", 17, new Color(0.85f, 0.93f, 1f, 1f));
        SetTopLeftRect(action.rectTransform, new Vector2(18f, -142f), new Vector2(850f, 25f));
        Text skills = CreateUiText(panel.transform, "Skills", "빙의 스킬 확인 중", 17, new Color(0.9f, 0.75f, 1f, 1f));
        SetTopLeftRect(skills.rectTransform, new Vector2(18f, -172f), new Vector2(850f, 25f));
        Text objective = CreateUiText(panel.transform, "Objective", "시스템 연결 확인 중", 16, new Color(1f, 0.82f, 0.48f, 1f));
        SetTopLeftRect(objective.rectTransform, new Vector2(18f, -202f), new Vector2(850f, 24f));

        GameObject hpBack = CreateUiImage(panel.transform, "HpBack", new Color(0.12f, 0.12f, 0.14f, 1f));
        SetTopLeftRect(hpBack.GetComponent<RectTransform>(), new Vector2(300f, -52f), new Vector2(560f, 10f));
        GameObject hpFillObject = CreateUiFillImage(panel.transform, "HpFill", new Color(0.48f, 0.82f, 1f, 1f));
        SetTopLeftRect(hpFillObject.GetComponent<RectTransform>(), new Vector2(300f, -52f), new Vector2(560f, 10f));
        GameObject mentalBack = CreateUiImage(panel.transform, "MentalBack", new Color(0.12f, 0.12f, 0.14f, 1f));
        SetTopLeftRect(mentalBack.GetComponent<RectTransform>(), new Vector2(300f, -72f), new Vector2(560f, 10f));
        GameObject mentalFillObject = CreateUiFillImage(panel.transform, "MentalFill", new Color(0.72f, 0.35f, 1f, 1f));
        SetTopLeftRect(mentalFillObject.GetComponent<RectTransform>(), new Vector2(300f, -72f), new Vector2(560f, 10f));
        GameObject experienceBack = CreateUiImage(panel.transform, "ExperienceBack", new Color(0.12f, 0.12f, 0.14f, 1f));
        SetTopLeftRect(experienceBack.GetComponent<RectTransform>(), new Vector2(300f, -102f), new Vector2(560f, 8f));
        GameObject experienceFillObject = CreateUiFillImage(panel.transform, "ExperienceFill", new Color(0.3f, 0.95f, 0.5f, 1f));
        SetTopLeftRect(experienceFillObject.GetComponent<RectTransform>(), new Vector2(300f, -102f), new Vector2(560f, 8f));

        HWJ_DemoHudSystem hud = root.AddComponent<HWJ_DemoHudSystem>();
        SerializedObject hudSo = new SerializedObject(hud);
        SetObject(hudSo, "hpFillImage", hpFillObject.GetComponent<Image>());
        SetObject(hudSo, "possessionFillImage", mentalFillObject.GetComponent<Image>());
        SetObject(hudSo, "experienceFillImage", experienceFillObject.GetComponent<Image>());
        SetObject(hudSo, "stateText", state);
        SetObject(hudSo, "resourceText", resource);
        SetObject(hudSo, "growthText", growth);
        SetObject(hudSo, "objectiveText", objective);
        SetObject(hudSo, "actionText", action);
        SetObject(hudSo, "skillSlotText", skills);
        SetBool(hudSo, "autoResolveReferences", true);
        hudSo.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static GameObject CreateRewardCollectionPrefab()
    {
        GameObject root = new GameObject("HWJ_Runtime_Rewards_All");
        GameObject experienceOrb = InstantiatePrefabChild(
            RequireAsset<GameObject>(ExperienceOrbPath),
            root.transform,
            "ExperienceOrb_30",
            new Vector3(-3f, 0f, 0f));
        HWJ_ExperienceOrbPickupSystem experiencePickup =
            experienceOrb.GetComponent<HWJ_ExperienceOrbPickupSystem>();

        if (experiencePickup != null)
        {
            SerializedObject pickupSo = new SerializedObject(experiencePickup);
            SetInt(pickupSo, "experienceAmount", 30);
            SetFloat(pickupSo, "lifeTimeSeconds", 0f);
            SetBool(pickupSo, "autoResolvePlayerTarget", true);
            SetBool(pickupSo, "moveToTarget", true);
            pickupSo.ApplyModifiedPropertiesWithoutUndo();
        }

        string[] statOrbPaths =
        {
            AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab",
            AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackSpeedSmall_Prefab.prefab",
            AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_DefenseSmall_Prefab.prefab",
            AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_MaxHpSmall_Prefab.prefab",
            AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_MoveSpeedSmall_Prefab.prefab"
        };

        for (int i = 0; i < statOrbPaths.Length; i++)
        {
            GameObject orb = InstantiatePrefabChild(
                RequireAsset<GameObject>(statOrbPaths[i]),
                root.transform,
                "StatOrb_" + (i + 1),
                new Vector3(-1f + i, 0f, 0f));
            HWJ_StatOrbPickupSystem pickup = orb.GetComponent<HWJ_StatOrbPickupSystem>();

            if (pickup != null)
            {
                SerializedObject pickupSo = new SerializedObject(pickup);
                SetFloat(pickupSo, "lifeTimeSeconds", 0f);
                SetBool(pickupSo, "autoResolvePlayerTarget", true);
                SetBool(pickupSo, "moveToTarget", true);
                pickupSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return root;
    }

    private static GameObject CreateAllSystemsDropInPrefab()
    {
        GameObject root = new GameObject("HWJ_Runtime_AllSystems_DropIn");
        InstantiatePrefabChild(RequireAsset<GameObject>(CorePrefabPath), root.transform, "00_GameplayCore", Vector3.zero);
        InstantiatePrefabChild(
            RequireAsset<GameObject>(CameraPrefabPath),
            root.transform,
            "01_Camera",
            new Vector3(0f, 1.5f, -10f));
        InstantiatePrefabChild(RequireAsset<GameObject>(HudPrefabPath), root.transform, "02_HUD", Vector3.zero);

        GameObject player = InstantiatePrefabChild(
            RequireAsset<GameObject>(PlayerPrefabPath),
            root.transform,
            "10_Player",
            new Vector3(-9f, 0.75f, 0f));
        GameObject firstCorpse = InstantiatePrefabChild(
            RequireAsset<GameObject>(GetCorpsePrefabPath(PossessableEnemySpecs[0])),
            root.transform,
            "11_FirstPossessionCorpse_Sword",
            new Vector3(-6f, 0.75f, 0f));
        firstCorpse.transform.localRotation = Quaternion.identity;

        InstantiatePrefabChild(
            RequireAsset<GameObject>(RewardsPrefabPath),
            root.transform,
            "20_Rewards",
            new Vector3(-2f, 1f, 0f));

        for (int i = 0; i < PossessableEnemySpecs.Length; i++)
        {
            EnemyPrefabSpec spec = PossessableEnemySpecs[i];
            InstantiatePrefabChild(
                RequireAsset<GameObject>(GetLiveEnemyPrefabPath(spec)),
                root.transform,
                "30_Enemy_Possessable_" + spec.WeaponName,
                new Vector3(2f + i * 3f, 0.75f, 0f));
        }

        InstantiatePrefabChild(
            RequireAsset<GameObject>(GetNoCorpseEnemyPrefabPath(NoCorpseEnemySpecs[1])),
            root.transform,
            "40_Enemy_NoCorpse_Axe",
            new Vector3(18f, 0.75f, 0f));

        CreateTestFloor(root.transform);
        CreatePlayerStart(root.transform, player.GetComponent<HWJ_RootObjectDataResolver>());
        return root;
    }

    private static void CreatePlayerStart(Transform parent, HWJ_RootObjectDataResolver playerResolver)
    {
        GameObject startRoot = CreateChild(parent, "PlayerStart");
        startRoot.transform.localPosition = new Vector3(-9f, 0.75f, 0f);
        HWJ_SpawnPoint spawnPoint = startRoot.AddComponent<HWJ_SpawnPoint>();
        SerializedObject pointSo = new SerializedObject(spawnPoint);
        SetString(pointSo, "pointId", "runtime_ready_player_start");
        SetEnum(pointSo, "spawnPointType", (int)HWJ_SpawnPointType.PlayerStart);
        pointSo.ApplyModifiedPropertiesWithoutUndo();

        HWJ_PlayerStartSystem startSystem = startRoot.AddComponent<HWJ_PlayerStartSystem>();
        SerializedObject startSo = new SerializedObject(startSystem);
        SetObject(startSo, "playerResolver", playerResolver);
        SetObject(startSo, "playerStartPoint", spawnPoint);
        SetString(startSo, "playerStartPointId", "runtime_ready_player_start");
        SetBool(startSo, "placeOnStart", true);
        SetBool(startSo, "registerToGameManager", true);
        startSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTestFloor(Transform parent)
    {
        GameObject floor = new GameObject("ReplaceWithMap_TestFloor");
        floor.transform.SetParent(parent);
        floor.transform.localPosition = new Vector3(4.5f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(48f, 1f, 1f);
        SetLayerIfExists(floor, "Ground");

        BoxCollider2D collider = floor.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
        renderer.color = new Color(0.28f, 0.3f, 0.34f, 1f);
        renderer.sortingOrder = -100;
    }

    private static HWJ_ObjectPoolDataSO CreateOrUpdatePoolData(List<string> report)
    {
        HWJ_ObjectPoolDataSO poolData = AssetDatabase.LoadAssetAtPath<HWJ_ObjectPoolDataSO>(ObjectPoolDataPath);

        if (poolData == null)
        {
            poolData = ScriptableObject.CreateInstance<HWJ_ObjectPoolDataSO>();
            AssetDatabase.CreateAsset(poolData, ObjectPoolDataPath);
        }

        PoolSpec[] poolSpecs =
        {
            new PoolSpec("effect_hit", HitEffectPath, 6, 30),
            new PoolSpec("projectile_bow_air", AssetRoot + "/Prefabs/Generated/Projectiles/HWJ_Projectile_Bow_AirArrowShot.prefab", 4, 30),
            new PoolSpec("projectile_bow_rapid", AssetRoot + "/Prefabs/Generated/Projectiles/HWJ_Projectile_Bow_RapidShot.prefab", 6, 40),
            new PoolSpec("projectile_bow_charge", AssetRoot + "/Prefabs/Generated/Projectiles/HWJ_Projectile_Bow_LowChargeShot.prefab", 3, 20),
            new PoolSpec("projectile_sword_wave", AssetRoot + "/Prefabs/Generated/Projectiles/HWJ_Projectile_Sword_Wave.prefab", 4, 24),
            new PoolSpec("reward_experience", ExperienceOrbPath, 8, 50),
            new PoolSpec("reward_stat_attack", AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab", 3, 20),
            new PoolSpec("reward_stat_speed", AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackSpeedSmall_Prefab.prefab", 3, 20),
            new PoolSpec("reward_stat_defense", AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_DefenseSmall_Prefab.prefab", 3, 20),
            new PoolSpec("reward_stat_hp", AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_MaxHpSmall_Prefab.prefab", 3, 20),
            new PoolSpec("reward_stat_move", AssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_MoveSpeedSmall_Prefab.prefab", 3, 20)
        };

        SerializedObject poolSo = new SerializedObject(poolData);
        SerializedProperty entries = poolSo.FindProperty("entries");
        entries.arraySize = poolSpecs.Length;

        for (int i = 0; i < poolSpecs.Length; i++)
        {
            PoolSpec spec = poolSpecs[i];
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("poolId").stringValue = spec.Id;
            entry.FindPropertyRelative("prefab").objectReferenceValue = RequireAsset<GameObject>(spec.PrefabPath);
            entry.FindPropertyRelative("initialSize").intValue = spec.InitialSize;
            entry.FindPropertyRelative("maxSize").intValue = spec.MaxSize;
            entry.FindPropertyRelative("canExpand").boolValue = true;
        }

        poolSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(poolData);

        HWJ_GameplayDatabaseSO database = RequireAsset<HWJ_GameplayDatabaseSO>(GameplayDatabasePath);
        SerializedObject databaseSo = new SerializedObject(database);
        SerializedProperty databasePoolData = databaseSo.FindProperty("objectPoolData");

        // 이미 같은 PoolData가 연결돼 있으면 Database 전체를 다시 직렬화하지 않습니다.
        // 이렇게 해야 다른 시스템이 수동으로 유지 중인 참조가 불필요하게 바뀌지 않습니다.
        if (databasePoolData != null && databasePoolData.objectReferenceValue != poolData)
        {
            databasePoolData.objectReferenceValue = poolData;
            databaseSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        report.Add($"- Object pool data wired: `{ObjectPoolDataPath}` ({poolSpecs.Length} entries)");
        return poolData;
    }

    private static void WireEnemyExperienceOrbRewards(List<string> report)
    {
        GameObject experienceOrb = RequireAsset<GameObject>(ExperienceOrbPath);
        string enemyRoot = AssetRoot + "/ScriptableObjects/RootObjects/Enemies";
        string[] guids = AssetDatabase.FindAssets("t:HWJ_RootObjectDataSO", new[] { enemyRoot });
        int wiredCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_RootObjectDataSO rootData = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(path);

            if (rootData == null
                || rootData.Identity == null
                || rootData.Identity.objectType != HWJ_ObjectType.Enemy
                || rootData.Reward == null
                || rootData.Reward.experienceReward <= 0)
            {
                continue;
            }

            SerializedObject rootSo = new SerializedObject(rootData);
            SerializedProperty reward = rootSo.FindProperty("reward");
            reward.FindPropertyRelative("dropsExperienceOrb").boolValue = true;
            reward.FindPropertyRelative("experienceOrbPrefab").objectReferenceValue = experienceOrb;
            reward.FindPropertyRelative("experienceOrbSpawnRadius").floatValue = 0.35f;
            rootSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rootData);
            wiredCount++;
        }

        report.Add($"- Enemy experience-orb rewards wired: {wiredCount}");
    }

    private static void ConfigureEnemyBehavior(
        HWJ_EnemyNavigationSystem navigation,
        HWJ_EnemyAttackSystem enemyAttack,
        HWJ_MonsterAISystem monsterAI)
    {
        SerializedObject navigationSo = new SerializedObject(navigation);
        SetBool(navigationSo, "autoFindPlayerTarget", true);
        SetBool(navigationSo, "chaseOnlyBodyState", true);
        SetBool(navigationSo, "faceOnlyBodyState", true);
        SetBool(navigationSo, "horizontalMoveOnly", true);
        SetLayerMask(navigationSo, "groundLayer", LayerMask.GetMask("Ground"));
        navigationSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject attackSo = new SerializedObject(enemyAttack);
        SetBool(attackSo, "autoFindPlayerTarget", true);
        SetBool(attackSo, "autoAttackWhenNoBehaviorDriver", false);
        SetBool(attackSo, "attackOnlyBodyState", true);
        SetBool(attackSo, "useSkillCycle", true);
        SetBool(attackSo, "showSkillWarning", true);
        SetFloat(attackSo, "skillWarningDelaySeconds", 1f);
        attackSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject aiSo = new SerializedObject(monsterAI);
        SetBool(aiSo, "driveBehavior", true);
        SetBool(aiSo, "autoFindPlayerTarget", true);
        SetBool(aiSo, "targetOnlyBodyState", true);
        SetBool(aiSo, "horizontalMoveOnly", true);
        SetLayerMask(aiSo, "groundLayer", LayerMask.GetMask("Ground"));
        aiSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkillAction(
        HWJ_SkillActionSystem skillAction,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status,
        HWJ_CombatSystem combat,
        HWJ_CombatExecutionSystem combatExecution,
        HWJ_PossessionSystem possession,
        HWJ_CharacterMotionSystem motion,
        Rigidbody2D body)
    {
        SerializedObject skillSo = new SerializedObject(skillAction);
        SetObject(skillSo, "dataResolver", resolver);
        SetObject(skillSo, "runtimeStatus", status);
        SetObject(skillSo, "combatSystem", combat);
        SetObject(skillSo, "combatExecutionSystem", combatExecution);
        SetObject(skillSo, "possessionSystem", possession);
        SetObject(skillSo, "motionSystem", motion);
        SetObject(skillSo, "database", RequireAsset<HWJ_GameplayDatabaseSO>(GameplayDatabasePath));
        SetObject(skillSo, "body", body);
        skillSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCombatExecution(HWJ_CombatExecutionSystem combatExecution, bool playerAttacker)
    {
        SerializedObject combatSo = new SerializedObject(combatExecution);
        SetBool(combatSo, "useObjectTypeDefaultTargetFilter", false);
        SetBool(combatSo, "canDamagePlayer", !playerAttacker);
        SetBool(combatSo, "canDamageEnemy", playerAttacker);
        SetBool(combatSo, "canDamageBoss", playerAttacker);
        SetBool(combatSo, "canDamageNpc", false);
        SetFloat(combatSo, "fallbackAttackRange", playerAttacker ? 2.2f : 1.4f);
        combatSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePlayerAttack(HWJ_PlayerAttackSystem attack)
    {
        SerializedObject attackSo = new SerializedObject(attack);
        SetLayerMask(attackSo, "attackTargetLayer", LayerMask.GetMask("Enemy", "Boss"));
        SetFloat(attackSo, "minimumAttackRange", 2f);
        SetInt(attackSo, "possessedSkillSlotCount", 4);
        attackSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMotion(
        HWJ_CharacterMotionSystem motion,
        Animator animator,
        SpriteRenderer renderer,
        Rigidbody2D body,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status,
        HWJ_MotionProfileSO fallbackProfile,
        HWJ_MotionProfileSO[] weaponProfiles)
    {
        SerializedObject motionSo = new SerializedObject(motion);
        SetObject(motionSo, "animator", animator);
        SetObject(motionSo, "spriteRenderer", renderer);
        SetObject(motionSo, "body", body);
        SetObject(motionSo, "dataResolver", resolver);
        SetObject(motionSo, "runtimeStatus", status);
        SetObject(motionSo, "fallbackMotionProfile", fallbackProfile);
        SetObjectArray(motionSo.FindProperty("weaponMotionProfiles"), weaponProfiles);
        motionSo.ApplyModifiedPropertiesWithoutUndo();
        motion.RefreshFacingBaseline();
    }

    private static void ConfigureHitEffect(
        HWJ_HitEffectSystem hitEffect,
        HWJ_RuntimeStatusSystem status,
        HWJ_RootObjectDataResolver resolver)
    {
        SerializedObject hitSo = new SerializedObject(hitEffect);
        SetObject(hitSo, "runtimeStatus", status);
        SetObject(hitSo, "dataResolver", resolver);
        SetObject(hitSo, "hitEffectPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HitEffectPath));
        SetString(hitSo, "hitEffectSocketName", "Hit");
        SetVector2(hitSo, "fallbackOffset", new Vector2(0f, 0.2f));
        SetBool(hitSo, "ignoreZeroDamage", true);
        SetFloat(hitSo, "minSpawnIntervalSeconds", 0.03f);
        hitSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePlayerReferences(
        HWJ_SoulSystem soul,
        HWJ_PossessionSystem possession,
        HWJ_BodyDiscoverySystem bodyDiscovery,
        HWJ_PossessionMentalSystem mental)
    {
        SerializedObject soulSo = new SerializedObject(soul);
        SetBool(soulSo, "initializeStateFromPlayerData", true);
        soulSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject possessionSo = new SerializedObject(possession);
        SetObject(possessionSo, "ownerDataResolver", possession.GetComponent<HWJ_RootObjectDataResolver>());
        SetObject(possessionSo, "soulSystem", soul);
        SetObject(possessionSo, "runtimeStatus", possession.GetComponent<HWJ_RuntimeStatusSystem>());
        SetObject(possessionSo, "playerInput", possession.GetComponent<HWJ_PlayerInputSystem>());
        SetObject(possessionSo, "possessedBodySystem", possession.GetComponent<HWJ_PossessedBodySystem>());
        SetObject(possessionSo, "skillUnlockSystem", possession.GetComponent<HWJ_SkillUnlockSystem>());
        SetObject(possessionSo, "targetValidator", possession.GetComponent<HWJ_PossessionTargetValidator>());
        SetObject(possessionSo, "livePossessionSystem", possession.GetComponent<HWJ_LivePossessionSystem>());
        SetObject(possessionSo, "corpsePossessionSystem", possession.GetComponent<HWJ_CorpsePossessionSystem>());
        SetObject(possessionSo, "exitSystem", possession.GetComponent<HWJ_PossessionExitSystem>());
        SetObject(possessionSo, "snapshotSystem", possession.GetComponent<HWJ_PossessionSnapshotSystem>());
        SetObject(possessionSo, "bodyController", possession.GetComponent<HWJ_PossessionBodyController>());
        SetObject(possessionSo, "visualController", possession.GetComponent<HWJ_PossessionVisualController>());
        SetObject(possessionSo, "skillProvider", possession.GetComponent<HWJ_PossessedSkillProvider>());
        possessionSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bodyControllerSo = new SerializedObject(
            possession.GetComponent<HWJ_PossessionBodyController>());
        SetBool(bodyControllerSo, "moveOwnerToPossessedBody", true);
        SetBool(bodyControllerSo, "consumePossessedBody", true);
        SetBool(bodyControllerSo, "deactivateConsumedBody", true);
        bodyControllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject visualControllerSo = new SerializedObject(
            possession.GetComponent<HWJ_PossessionVisualController>());
        SetBool(visualControllerSo, "copyPossessedBodyVisual", true);
        visualControllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject exitSo = new SerializedObject(
            possession.GetComponent<HWJ_PossessionExitSystem>());
        SetBool(exitSo, "allowManualSoulExit", true);
        exitSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject discoverySo = new SerializedObject(bodyDiscovery);
        SetBool(discoverySo, "useSceneFallbackSearch", true);
        SetFloat(discoverySo, "fallbackRange", 2.5f);
        discoverySo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject mentalSo = new SerializedObject(mental);
        SetBool(mentalSo, "resetDecayWhenEnterBody", true);
        mentalSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePossessionInteraction(
        GameObject playerRoot,
        HWJ_PossessionSystem possession,
        HWJ_LivePossessionMinigameController minigame,
        HWJ_PossessionInteractionController interaction)
    {
        HWJ_PossessionTargetValidator validator =
            playerRoot.GetComponent<HWJ_PossessionTargetValidator>();

        GameObject canvasObject = CreateChild(
            playerRoot.transform,
            "HWJ_LivePossessionMinigameCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = CreateUiImage(
            canvasObject.transform,
            "PossessionMinigamePanel",
            new Color(0.035f, 0.04f, 0.055f, 0.94f));
        SetCenterRect(
            panel.GetComponent<RectTransform>(),
            new Vector2(0f, 30f),
            new Vector2(760f, 230f));

        Text title = CreateUiText(
            panel.transform,
            "Instruction",
            "SPACE 연타: 정신력 게이지를 100%까지 올리세요.",
            28,
            Color.white);
        title.alignment = TextAnchor.MiddleCenter;
        SetCenterRect(title.rectTransform, new Vector2(0f, 72f), new Vector2(700f, 42f));

        Text timer = CreateUiText(
            panel.transform,
            "Timer",
            "남은 시간: 5.0",
            24,
            new Color(1f, 0.86f, 0.45f, 1f));
        timer.alignment = TextAnchor.MiddleCenter;
        SetCenterRect(timer.rectTransform, new Vector2(0f, 32f), new Vector2(300f, 34f));

        GameObject sliderObject = new GameObject("MentalGauge");
        sliderObject.transform.SetParent(panel.transform);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.localScale = Vector3.one;
        SetCenterRect(sliderRect, new Vector2(0f, -18f), new Vector2(650f, 28f));

        GameObject sliderBackground = CreateUiImage(
            sliderObject.transform,
            "Background",
            new Color(0.11f, 0.12f, 0.16f, 1f));
        RectTransform backgroundRect = sliderBackground.GetComponent<RectTransform>();
        StretchRect(backgroundRect, Vector2.zero, Vector2.zero);

        GameObject fillObject = CreateUiImage(
            sliderObject.transform,
            "Fill",
            new Color(0.58f, 0.28f, 0.95f, 1f));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        StretchRect(fillRect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillObject.GetComponent<Image>();

        Text result = CreateUiText(
            panel.transform,
            "Result",
            "게이지: 50%",
            22,
            new Color(0.75f, 0.9f, 1f, 1f));
        result.alignment = TextAnchor.MiddleCenter;
        SetCenterRect(result.rectTransform, new Vector2(0f, -68f), new Vector2(500f, 34f));

        SerializedObject minigameSo = new SerializedObject(minigame);
        SetObject(minigameSo, "possessionSystem", possession);
        SetObject(minigameSo, "minigameRoot", panel);
        SetObject(minigameSo, "mentalGaugeSlider", slider);
        SetObject(minigameSo, "timerText", timer);
        SetObject(minigameSo, "instructionText", title);
        SetObject(minigameSo, "resultText", result);
        SetBool(minigameSo, "pauseEntireWorldDuringMinigame", false);
        minigameSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject interactionSo = new SerializedObject(interaction);
        SetObject(interactionSo, "possessionSystem", possession);
        SetObject(interactionSo, "targetValidator", validator);
        SetObject(interactionSo, "minigameController", minigame);
        SetObject(interactionSo, "detectionOrigin", playerRoot.transform);
        SetFloat(interactionSo, "detectionRadius", 2.5f);
        int enemyLayerMask = LayerMask.GetMask("Enemy");
        SetLayerMask(
            interactionSo,
            "targetLayerMask",
            enemyLayerMask != 0 ? enemyLayerMask : ~0);
        SetBool(interactionSo, "readKeyboardDirectly", true);
        interactionSo.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
    }

    private static GameObject CreatePlayerVisual(Transform parent)
    {
        GameObject visual = new GameObject(HWJ_PlayerStartSystem.DefaultPlayerVisualObjectName);
        visual.transform.SetParent(parent);
        visual.transform.localPosition = Vector3.zero;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        renderer.sortingOrder = 30;
        visual.AddComponent<Animator>();
        return visual;
    }

    private static GameObject CreateRootDataVisual(Transform parent, HWJ_RootObjectDataSO rootData)
    {
        GameObject modelPrefab = rootData != null && rootData.Model != null
            ? rootData.Model.modelPrefab
            : null;
        GameObject visual;

        if (modelPrefab != null)
        {
            visual = InstantiatePrefabChild(modelPrefab, parent, "HWJ_Visual", Vector3.zero);
        }
        else
        {
            visual = new GameObject("HWJ_Visual");
            visual.transform.SetParent(parent);
            visual.transform.localPosition = Vector3.zero;
            SpriteRenderer fallbackRenderer = visual.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
            fallbackRenderer.color = new Color(0.85f, 0.3f, 0.3f, 1f);
            fallbackRenderer.sortingOrder = 20;
        }

        DisableNestedPhysics(visual);
        Animator animator = visual.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }

        if (rootData != null
            && rootData.Model != null
            && rootData.Model.animatorController != null)
        {
            animator.runtimeAnimatorController = rootData.Model.animatorController;
        }

        return visual;
    }

    private static void DisableNestedPhysics(GameObject visual)
    {
        Collider2D[] colliders = visual.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody2D[] bodies = visual.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].simulated = false;
        }
    }

    private static void CreateWorldBar(
        Transform parent,
        HWJ_RuntimeStatusSystem status,
        HWJ_BodyDecaySystem mental,
        HWJ_SoulSystem soul,
        HWJ_RootObjectDataResolver resolver,
        Vector3 localPosition)
    {
        Sprite marker = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
        GameObject bar = CreateChild(parent, "HWJ_WorldHealthBar");
        bar.transform.localPosition = localPosition;

        GameObject background = CreateChild(bar.transform, "Background");
        background.transform.localScale = new Vector3(1.25f, 0.12f, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = marker;
        backgroundRenderer.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        backgroundRenderer.sortingOrder = 190;

        GameObject fill = CreateChild(bar.transform, "Fill");
        fill.transform.localScale = new Vector3(1.2f, 0.08f, 1f);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = marker;
        fillRenderer.color = new Color(0.25f, 0.95f, 0.45f, 1f);
        fillRenderer.sortingOrder = 191;

        HWJ_HealthBarSystem healthBar = bar.AddComponent<HWJ_HealthBarSystem>();
        SerializedObject barSo = new SerializedObject(healthBar);
        SetObject(barSo, "runtimeStatus", status);
        SetObject(barSo, "bodyDecaySystem", mental);
        SetObject(barSo, "soulSystem", soul);
        SetObject(barSo, "dataResolver", resolver);
        SetObject(barSo, "fillRoot", fill.transform);
        SetObject(barSo, "fillRenderer", fillRenderer);
        barSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HWJ_MotionProfileSO[] GetAllMotionProfiles()
    {
        return new[]
        {
            LoadMotionProfile("Sword"),
            LoadMotionProfile("Axe"),
            LoadMotionProfile("Bow"),
            LoadMotionProfile("Lance"),
            LoadMotionProfile("Shield")
        };
    }

    private static HWJ_MotionProfileSO LoadMotionProfile(string weaponName)
    {
        return AssetDatabase.LoadAssetAtPath<HWJ_MotionProfileSO>(
            AssetRoot + "/ScriptableObjects/MotionProfiles/HWJ_" + weaponName + "MotionProfile.asset");
    }

    private static void ValidateGeneratedAssets(List<string> report)
    {
        List<string> errors = new List<string>();
        ValidatePrefab(
            PlayerPrefabPath,
            errors,
            typeof(HWJ_RootObjectDataResolver),
            typeof(HWJ_RuntimeStatusSystem),
            typeof(HWJ_PlayerInputSystem),
            typeof(HWJ_PlayerMovementSystem),
            typeof(HWJ_PlayerAttackSystem),
            typeof(HWJ_InteractionSystem),
            typeof(HWJ_SoulSystem),
            typeof(HWJ_PossessionSystem),
            typeof(HWJ_PossessionTargetValidator),
            typeof(HWJ_LivePossessionSystem),
            typeof(HWJ_CorpsePossessionSystem),
            typeof(HWJ_LivePossessionMinigameController),
            typeof(HWJ_PossessionInteractionController),
            typeof(HWJ_PossessionMentalSystem));
        ValidatePrefab(
            CorePrefabPath,
            errors,
            typeof(HWJ_GameManager),
            typeof(HWJ_ObjectPoolSystem),
            typeof(HWJ_GameOverWindowSystem));
        ValidatePrefab(
            RewardsPrefabPath,
            errors,
            typeof(HWJ_ExperienceOrbPickupSystem),
            typeof(HWJ_StatOrbPickupSystem));
        ValidatePrefab(
            AllSystemsPrefabPath,
            errors,
            typeof(HWJ_GameManager),
            typeof(HWJ_RootObjectDataResolver),
            typeof(HWJ_DemoHudSystem));

        for (int i = 0; i < PossessableEnemySpecs.Length; i++)
        {
            ValidatePrefab(
                GetLiveEnemyPrefabPath(PossessableEnemySpecs[i]),
                errors,
                typeof(HWJ_RuntimeStatusSystem),
                typeof(HWJ_MonsterAISystem),
                typeof(HWJ_EnemyAttackSystem),
                typeof(HWJ_PossessionBodyState),
                typeof(HWJ_LivePossessionMentalState),
                typeof(HWJ_EnemyDeathLifecycleSystem));
            ValidatePrefab(
                GetCorpsePrefabPath(PossessableEnemySpecs[i]),
                errors,
                typeof(HWJ_RuntimeStatusSystem),
                typeof(HWJ_PossessionBodyState));

            HWJ_RootObjectDataSO rootData = RequireAsset<HWJ_RootObjectDataSO>(
                PossessableEnemySpecs[i].RootObjectDataPath);

            if (!rootData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
                || enemyData.PossessionBody == null
                || enemyData.PossessionBody.requiresDefeatedState
                || !Mathf.Approximately(
                    enemyData.PossessionBody.livePossessionMentalCostOnSuccess,
                    10f))
            {
                errors.Add(
                    $"Possession SO is not configured for live R/corpse E flow: "
                    + PossessableEnemySpecs[i].RootObjectDataPath);
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Runtime-ready prefab validation failed:\n" + string.Join("\n", errors));
        }

        report.Add("- Validation: all generated prefabs have required components and no missing scripts.");
    }

    private static void ValidatePrefab(string path, List<string> errors, params Type[] requiredTypes)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            errors.Add($"Missing prefab: {path}");
            return;
        }

        int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);

        if (missingScriptCount > 0)
        {
            errors.Add($"Missing scripts ({missingScriptCount}): {path}");
        }

        for (int i = 0; i < requiredTypes.Length; i++)
        {
            if (prefab.GetComponentInChildren(requiredTypes[i], true) == null)
            {
                errors.Add($"Missing {requiredTypes[i].Name}: {path}");
            }
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder(AssetRoot, "Prefabs");
        EnsureFolder(AssetRoot + "/Prefabs", "Generated");
        EnsureFolder(AssetRoot + "/Prefabs/Generated", "RuntimeReady");
        EnsureFolder(OutputRoot, "Enemies");
        EnsureFolder(OutputRoot, "Corpses");
        EnsureFolder(OutputRoot, "Rewards");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void SavePrefab(GameObject root, string path, List<string> report)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        report.Add($"- Prefab: `{path}`");
    }

    private static GameObject InstantiatePrefabChild(
        GameObject prefab,
        Transform parent,
        string objectName,
        Vector3 localPosition)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to instantiate prefab: {AssetDatabase.GetAssetPath(prefab)}");
        }

        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }

    private static GameObject CreateUiImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static GameObject CreateUiFillImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = CreateUiImage(parent, name, color);
        Image image = imageObject.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return imageObject;
    }

    private static Text CreateUiText(Transform parent, string name, string value, int size, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = ResolveDefaultUiFont();
        text.fontSize = size;
        text.alignment = TextAnchor.UpperLeft;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Font ResolveDefaultUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetCenterRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchRect(
        RectTransform rect,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetLayerIfExists(GameObject target, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
        {
            target.layer = layer;
        }
    }

    private static string GetLiveEnemyPrefabPath(EnemyPrefabSpec spec)
    {
        return EnemyOutputRoot + "/HWJ_Runtime_Enemy_Possessable_" + spec.WeaponName + ".prefab";
    }

    private static string GetNoCorpseEnemyPrefabPath(EnemyPrefabSpec spec)
    {
        return EnemyOutputRoot + "/HWJ_Runtime_Enemy_NoCorpse_" + spec.WeaponName + ".prefab";
    }

    private static string GetCorpsePrefabPath(EnemyPrefabSpec spec)
    {
        return CorpseOutputRoot + "/HWJ_Runtime_Corpse_" + spec.WeaponName + ".prefab";
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            throw new FileNotFoundException($"Required HWJ asset was not found: {path}");
        }

        return asset;
    }

    private static void WriteReport(string result, List<string> report, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Runtime Ready Prefab Report");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Result: {result}");
        builder.AppendLine($"- Output: `{OutputRoot}`");
        builder.AppendLine();
        builder.AppendLine("## Generated And Wired");
        builder.AppendLine();

        for (int i = 0; i < report.Count; i++)
        {
            builder.AppendLine(report[i]);
        }

        if (exception != null)
        {
            builder.AppendLine();
            builder.AppendLine("## Error");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(exception.ToString());
            builder.AppendLine("```");
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void SetObject(SerializedObject so, string name, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(SerializedObject so, string name, bool value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetString(SerializedObject so, string name, string value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string name, float value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetInt(SerializedObject so, string name, int value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetEnum(SerializedObject so, string name, int value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.enumValueIndex = value;
        }
    }

    private static void SetLayerMask(SerializedObject so, string name, LayerMask value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.intValue = value.value;
        }
    }

    private static void SetVector2(SerializedObject so, string name, Vector2 value)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
        {
            property.vector2Value = value;
        }
    }

    private static void SetObjectArray<T>(SerializedProperty property, T[] values)
        where T : UnityEngine.Object
    {
        if (property == null)
        {
            return;
        }

        property.arraySize = values != null ? values.Length : 0;

        for (int i = 0; values != null && i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private readonly struct EnemyPrefabSpec
    {
        public EnemyPrefabSpec(string weaponName, string rootObjectFileName)
        {
            WeaponName = weaponName;
            RootObjectDataPath = AssetRoot
                + "/ScriptableObjects/RootObjects/Enemies/"
                + rootObjectFileName;
            IsPossessable = rootObjectFileName.IndexOf("EnemyCorpse", StringComparison.Ordinal) >= 0;
        }

        public string WeaponName { get; }
        public string RootObjectDataPath { get; }
        public bool IsPossessable { get; }
    }

    private readonly struct PoolSpec
    {
        public PoolSpec(string id, string prefabPath, int initialSize, int maxSize)
        {
            Id = id;
            PrefabPath = prefabPath;
            InitialSize = initialSize;
            MaxSize = maxSize;
        }

        public string Id { get; }
        public string PrefabPath { get; }
        public int InitialSize { get; }
        public int MaxSize { get; }
    }
}
