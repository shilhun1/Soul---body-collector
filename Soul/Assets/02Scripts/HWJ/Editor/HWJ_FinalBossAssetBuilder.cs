using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 최종보스 패턴 SO, Root/Type 데이터와 즉시 배치 가능한 런타임 프리팹을 생성합니다.
/// 기존 같은 경로의 에셋은 참조를 유지한 채 값만 갱신합니다.
/// </summary>
public static class HWJ_FinalBossAssetBuilder
{
    public const string TypeDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/TypeData/Boss/HWJ_FinalBoss_TypeData.asset";
    public const string RootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_FinalBoss_RootObjectData.asset";
    public const string PrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_FinalBoss_Runtime_Prefab.prefab";

    private const string PatternDataFolder =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/FinalBoss";
    private const string DefinitionFolder =
        "Assets/02Scripts/HWJ/ScriptableObjects/Systems/Boss/FinalBoss";

    private sealed class PatternSpec
    {
        public string AssetSuffix;
        public string PatternId;
        public HWJ_FinalBossPatternKind Kind;
        public bool PhaseOne;
        public float Cooldown;
        public float Preparation;
        public float Recovery;
        public float DamageMultiplier;
        public float MentalRatio;
        public float Stun;
        public int ProjectileCount;
        public float ProjectileInterval;
        public float ProjectileSpeed;
        public float ProjectileRange;
        public Vector2 ProjectileSize;
        public float HazardDuration;
        public Vector2 HazardSize;
        public float BarrierRatio;
        public int PortalCount;
        public int MonstersPerPortal;
        public int RadialCount;
        public string GestureTrigger;
    }

    [MenuItem("Tools/HWJ/Boss/Build Final Boss")]
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
        EnsureFolder(PatternDataFolder);
        EnsureFolder(DefinitionFolder);
        EnsureFolder("Assets/02Scripts/HWJ/Prefabs/Generated/Bosses");

        PatternSpec[] specs = CreatePatternSpecs();
        GameObject[] summonPrefabs = LoadSummonPrefabs();
        HWJ_RootObjectDataSO[] summonRoots = LoadSummonRootObjects();
        HWJ_FinalBossPatternDefinitionSO[] definitions =
            BuildDefinitions(specs, summonPrefabs, summonRoots);
        HWJ_BossPatternDataSO[] bossPatterns = BuildBossPatterns(specs);
        HWJ_BossTypeDataSO typeData = BuildTypeData();
        HWJ_RootObjectDataSO rootData = BuildRootData(typeData);
        GameObject prefab = BuildPrefab(rootData, definitions, bossPatterns);
        AssignModelPrefab(rootData, prefab);
        ValidateBuild(specs, definitions, bossPatterns, typeData, rootData, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HWJ][FinalBoss] 최종보스 데이터와 런타임 프리팹 생성을 완료했습니다.");
    }

    private static PatternSpec[] CreatePatternSpecs()
    {
        return new[]
        {
            Spec("P1_01_BlackOrbVolley", "boss.final.p1.black_orb_volley", HWJ_FinalBossPatternKind.Phase1BlackOrbVolley, true,
                5f, 2f, 0.8f, 1f, 0f, 0f, 3, 2f, 10f, 30f, new Vector2(0.8f, 0.8f), 0f, new Vector2(1f, 1f), 0f, 1, 3, 8, "Gesture_Orb"),
            Spec("P1_02_SpearLine", "boss.final.p1.spear_line", HWJ_FinalBossPatternKind.Phase1SpearLine, true,
                5f, 1.5f, 0.9f, 1f, 0f, 0f, 1, 0f, 18f, 32f, new Vector2(1.6f, 0.35f), 3f, new Vector2(32f, 0.35f), 0f, 1, 3, 8, "Gesture_Spear"),
            Spec("P1_03_Barrier", "boss.final.p1.barrier", HWJ_FinalBossPatternKind.Phase1Barrier, true,
                5f, 0.7f, 0.7f, 0f, 0f, 0f, 1, 0f, 0f, 1f, Vector2.one, 0f, Vector2.one, 0.05f, 1, 3, 8, "Gesture_Barrier"),
            Spec("P1_04_PortalSummon", "boss.final.p1.portal_summon", HWJ_FinalBossPatternKind.Phase1PortalSummon, true,
                5f, 0.8f, 1f, 0f, 0f, 0f, 1, 0.35f, 0f, 1f, Vector2.one, 0f, Vector2.one, 0f, 1, 3, 8, "Gesture_Portal"),
            Spec("P1_05_FloorFireLightning", "boss.final.p1.floor_fire_lightning", HWJ_FinalBossPatternKind.Phase1FloorFireLightning, true,
                7f, 3f, 0f, 1f, 0f, 1f, 12, 0.18f, 0f, 1f, Vector2.one, 4f, new Vector2(3f, 0.8f), 0f, 1, 3, 8, "Gesture_Lightning"),
            Spec("P2_01_WeaponBarrage", "boss.final.p2.weapon_barrage", HWJ_FinalBossPatternKind.Phase2WeaponBarrage, false,
                5f, 1f, 1f, 1.1f, 0f, 0f, 9, 0.25f, 14f, 32f, new Vector2(1.3f, 0.35f), 0f, new Vector2(18f, 0.8f), 0f, 1, 3, 8, "Gesture_Weapons"),
            Spec("P2_02_BarrierOrbVolley", "boss.final.p2.barrier_orb_volley", HWJ_FinalBossPatternKind.Phase2BarrierOrbVolley, false,
                6f, 0f, 0.8f, 1f, 0f, 0f, 5, 0.8f, 11f, 32f, new Vector2(0.75f, 0.75f), 0f, Vector2.one, 0.1f, 1, 3, 8, "Gesture_BarrierOrb"),
            Spec("P2_03_BlackFlameCharge", "boss.final.p2.black_flame_charge", HWJ_FinalBossPatternKind.Phase2BlackFlameCharge, false,
                6f, 1f, 1f, 0f, 0.1f, 0f, 1, 0f, 0f, 1f, new Vector2(2.1f, 3.2f), 0f, Vector2.one, 0f, 1, 3, 8, "Gesture_BlackFlame"),
            Spec("P2_04_DoublePortalSummon", "boss.final.p2.double_portal_summon", HWJ_FinalBossPatternKind.Phase2DoublePortalSummon, false,
                8f, 0.8f, 1f, 0f, 0f, 0f, 1, 0.35f, 0f, 1f, Vector2.one, 0f, Vector2.one, 0f, 2, 4, 8, "Gesture_DoublePortal"),
            Spec("P2_05_EightWayLightning", "boss.final.p2.eight_way_lightning", HWJ_FinalBossPatternKind.Phase2EightWayLightning, false,
                7f, 2f, 1f, 1.15f, 0f, 0.7f, 8, 0f, 0f, 18f, Vector2.one, 0.25f, new Vector2(18f, 0.55f), 0f, 1, 3, 8, "Gesture_EightWayLightning")
        };
    }

    private static PatternSpec Spec(
        string suffix,
        string id,
        HWJ_FinalBossPatternKind kind,
        bool phaseOne,
        float cooldown,
        float preparation,
        float recovery,
        float damage,
        float mental,
        float stun,
        int projectileCount,
        float projectileInterval,
        float projectileSpeed,
        float projectileRange,
        Vector2 projectileSize,
        float hazardDuration,
        Vector2 hazardSize,
        float barrierRatio,
        int portalCount,
        int monstersPerPortal,
        int radialCount,
        string gestureTrigger)
    {
        return new PatternSpec
        {
            AssetSuffix = suffix,
            PatternId = id,
            Kind = kind,
            PhaseOne = phaseOne,
            Cooldown = cooldown,
            Preparation = preparation,
            Recovery = recovery,
            DamageMultiplier = damage,
            MentalRatio = mental,
            Stun = stun,
            ProjectileCount = projectileCount,
            ProjectileInterval = projectileInterval,
            ProjectileSpeed = projectileSpeed,
            ProjectileRange = projectileRange,
            ProjectileSize = projectileSize,
            HazardDuration = hazardDuration,
            HazardSize = hazardSize,
            BarrierRatio = barrierRatio,
            PortalCount = portalCount,
            MonstersPerPortal = monstersPerPortal,
            RadialCount = radialCount,
            GestureTrigger = gestureTrigger
        };
    }

    private static HWJ_FinalBossPatternDefinitionSO[] BuildDefinitions(
        PatternSpec[] specs,
        GameObject[] summonPrefabs,
        HWJ_RootObjectDataSO[] summonRoots)
    {
        HWJ_FinalBossPatternDefinitionSO[] result =
            new HWJ_FinalBossPatternDefinitionSO[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            PatternSpec spec = specs[i];
            string path = $"{DefinitionFolder}/HWJ_FinalBoss_{spec.AssetSuffix}_Definition.asset";
            HWJ_FinalBossPatternDefinitionSO asset =
                LoadOrCreate<HWJ_FinalBossPatternDefinitionSO>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "patternId", spec.PatternId);
            SetEnum(serialized, "patternKind", (int)spec.Kind);
            SetString(serialized, "gestureTrigger", spec.GestureTrigger);
            SetFloat(serialized, "preparationSeconds", spec.Preparation);
            SetFloat(serialized, "animationEventTimeoutSeconds", Mathf.Max(0.1f, spec.Preparation));
            SetBool(serialized, "preferAnimationCastEvent", false);
            SetFloat(serialized, "recoverySeconds", spec.Recovery);
            SetFloat(serialized, "damageMultiplier", spec.DamageMultiplier);
            SetFloat(serialized, "possessionMentalDamageRatio", spec.MentalRatio);
            SetFloat(serialized, "stunSeconds", spec.Stun);
            SetFloat(serialized, "repeatHitIntervalSeconds", 0.5f);
            SetInt(serialized, "projectileCount", spec.ProjectileCount);
            SetFloat(serialized, "projectileIntervalSeconds", spec.ProjectileInterval);
            SetFloat(serialized, "projectileSpeed", spec.ProjectileSpeed);
            SetFloat(serialized, "projectileMaximumDistance", spec.ProjectileRange);
            SetVector2(serialized, "projectileSize", spec.ProjectileSize);
            SetFloat(serialized, "hazardDurationSeconds", spec.HazardDuration);
            SetVector2(serialized, "hazardSize", spec.HazardSize);
            SetInt(serialized, "radialAttackCount", spec.RadialCount);
            SetFloat(serialized, "barrierHpRatio", spec.BarrierRatio);
            SetFloat(serialized, "barrierDurationSeconds", 0f);
            SetInt(serialized, "portalCount", spec.PortalCount);
            SetInt(serialized, "monstersPerPortal", spec.MonstersPerPortal);
            SetFloat(serialized, "summonIntervalSeconds", 0.35f);
            SetVector2(serialized, "summonStepOffset", new Vector2(0.45f, 0f));
            SetFloat(serialized, "chargeSpeed", 18f);
            SetFloat(serialized, "chargeMaximumDistance", 24f);
            SetFloat(serialized, "returnToAnchorSeconds", 0.35f);

            bool isSummonPattern = spec.Kind == HWJ_FinalBossPatternKind.Phase1PortalSummon
                || spec.Kind == HWJ_FinalBossPatternKind.Phase2DoublePortalSummon;
            SetObjectArray(serialized, "summonMonsterPrefabs", isSummonPattern ? summonPrefabs : null);
            SetObjectArray(serialized, "summonMonsterRootObjects", isSummonPattern ? summonRoots : null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            result[i] = asset;
        }

        return result;
    }

    private static HWJ_BossPatternDataSO[] BuildBossPatterns(PatternSpec[] specs)
    {
        HWJ_BossPatternDataSO[] result = new HWJ_BossPatternDataSO[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            PatternSpec spec = specs[i];
            string path = $"{PatternDataFolder}/HWJ_FinalBoss_{spec.AssetSuffix}.asset";
            HWJ_BossPatternDataSO asset = LoadOrCreate<HWJ_BossPatternDataSO>(path);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "patternId", spec.PatternId);
            SetInt(serialized, "patternNumber", (int)spec.Kind);
            SetEnum(serialized, "trigger", (int)HWJ_BossPatternTrigger.Always);
            SetEnum(serialized, "rangeMode", (int)HWJ_BossPatternRangeMode.Any);
            SetBool(serialized, "usableInPhase1", spec.PhaseOne);
            SetBool(serialized, "usableInPhase2", !spec.PhaseOne);
            SetFloat(serialized, "hpRatio", 1f);
            SetFloat(serialized, "cooldownSeconds", spec.Cooldown);
            SetFloat(serialized, "defaultCooldownSeconds", spec.Cooldown);
            SetInt(serialized, "weight", 1);
            SetString(serialized, "animationId", spec.GestureTrigger);
            SetBool(serialized, "useStageOneSpecialExecution", false);
            SetBool(serialized, "useCustomPatternExecutor", true);
            SetString(serialized, "customPatternExecutorKey", HWJ_FinalBossPatternSystem.FinalBossExecutorKey);
            SetObjectArray(serialized, "skillActions", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            result[i] = asset;
        }

        return result;
    }

    private static HWJ_BossTypeDataSO BuildTypeData()
    {
        HWJ_BossTypeDataSO asset = LoadOrCreate<HWJ_BossTypeDataSO>(TypeDataPath);
        SerializedObject serialized = new SerializedObject(asset);
        SetEnum(serialized, "objectType", (int)HWJ_ObjectType.Boss);
        SetString(serialized, "typeId", "boss.final.01.sovereign");
        SetString(serialized, "displayName", "최종보스");
        SetEnum(serialized, "defaultWeaponType", (int)HWJ_WeaponType.None);
        SetString(serialized, "description", "제자리에서 손짓으로 1·2페이즈 패턴을 사용하는 고정형 최종보스입니다.");
        SetEnum(serialized, "bossRank", (int)HWJ_BossRank.FinalBoss);

        SerializedProperty fsm = RequireProperty(serialized, "fsm");
        SetRelativeEnum(fsm, "locomotionMode", (int)HWJ_BossLocomotionMode.StationaryCaster);
        SetRelativeBool(fsm, "autoStartWhenPlayerEntersRoom", true);
        SetRelativeBool(fsm, "useBossRoomBounds", true);
        SetRelativeVector2(fsm, "bossRoomOffset", Vector2.zero);
        SetRelativeVector2(fsm, "bossRoomSize", new Vector2(36f, 18f));
        SetRelativeFloat(fsm, "attackStartRange", 100f);
        SetRelativeFloat(fsm, "optimalAttackDistance", 0f);
        SetRelativeFloat(fsm, "closeSkillRange", 5f);
        SetRelativeFloat(fsm, "chaseMoveSpeedMultiplier", 0f);
        SetRelativeFloat(fsm, "phaseTwoHpRatio", 0.5f);
        SetRelativeFloat(fsm, "phaseTransitionSeconds", 3f);
        SetRelativeFloat(fsm, "cameraFocusSeconds", 2f);
        SetRelativeBool(fsm, "superArmorDuringAttack", true);
        SetRelativeBool(fsm, "superArmorDuringPhaseTransition", true);
        SetRelativeFloat(fsm, "groggyDurationSeconds", 4f);
        SetRelativeFloat(fsm, "groggyDamageMultiplier", 1.5f);

        SerializedProperty phases = RequireProperty(serialized, "phases");
        phases.arraySize = 2;
        ConfigurePhase(phases.GetArrayElementAtIndex(0), "phase_1", 1f, false);
        ConfigurePhase(phases.GetArrayElementAtIndex(1), "phase_2", 0.5f, true);

        SerializedProperty possession = RequireProperty(serialized, "possessionBody");
        SetRelativeBool(possession, "canPossess", false);
        SetRelativeBool(possession, "canBePossessed", false);
        SetRelativeBool(possession, "requiresDefeatedState", true);
        SetRelativeBool(possession, "transfersControlToBody", false);
        SetRelativeBool(possession, "loadsBodyStatsToPlayer", false);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static HWJ_RootObjectDataSO BuildRootData(HWJ_BossTypeDataSO typeData)
    {
        HWJ_RootObjectDataSO asset = LoadOrCreate<HWJ_RootObjectDataSO>(RootDataPath);
        SerializedObject serialized = new SerializedObject(asset);

        SerializedProperty identity = RequireProperty(serialized, "identity");
        SetRelativeString(identity, "objectId", "boss.final.01.sovereign");
        SetRelativeString(identity, "displayName", "최종보스");
        SetRelativeEnum(identity, "objectType", (int)HWJ_ObjectType.Boss);
        SetRelativeEnum(identity, "faction", (int)HWJ_Faction.Monster);
        SetRelativeEnum(identity, "weaponType", (int)HWJ_WeaponType.None);

        SerializedProperty status = RequireProperty(serialized, "status");
        SetRelativeInt(status, "level", 1);
        SetRelativeFloat(status, "maxHp", 10000f);
        SetRelativeFloat(status, "moveSpeed", 0f);
        SetRelativeFloat(status, "attackPower", 100f);
        SetRelativeFloat(status, "defense", 50f);
        SetRelativeFloat(status, "attackSpeed", 1f);
        SetRelativeFloat(status, "bodyWeight", 10f);

        SerializedProperty damage = RequireProperty(serialized, "damage");
        SetRelativeEnum(damage, "damageType", (int)HWJ_DamageType.Magical);
        SetRelativeFloat(damage, "baseDamage", 20f);
        SetRelativeFloat(damage, "criticalChance", 0f);
        SetRelativeFloat(damage, "criticalMultiplier", 1.5f);
        SetRelativeFloat(damage, "knockbackPower", 4f);
        SetRelativeFloat(damage, "hitStunSeconds", 0.15f);
        SetRelativeFloat(damage, "sameTargetHitCooldownSeconds", 0.08f);

        SerializedProperty received = RequireProperty(serialized, "receivedDamage");
        SetRelativeFloat(received, "damageMultiplier", 1f);
        SetRelativeFloat(received, "invincibleSecondsAfterHit", 0.06f);
        SetRelativeBool(received, "isInvincible", false);
        SetRelativeBool(received, "hasSuperArmor", false);
        SetRelativeFloat(received, "knockbackWeightMultiplier", 8f);

        SerializedProperty interaction = RequireProperty(serialized, "interaction");
        SetRelativeBool(interaction, "canInteract", false);
        SetRelativeBool(interaction, "canBeTargeted", true);

        SerializedProperty reward = RequireProperty(serialized, "reward");
        SetRelativeInt(reward, "experienceReward", 1000);
        SetRelativeInt(reward, "skillPointReward", 3);
        SetRelativeBool(reward, "dropsExperienceOrb", false);
        SetRelativeBool(reward, "dropsStatOrb", false);

        SetObject(serialized, "selectedTypeData", typeData);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GameObject BuildPrefab(
        HWJ_RootObjectDataSO rootData,
        HWJ_FinalBossPatternDefinitionSO[] definitions,
        HWJ_BossPatternDataSO[] patterns)
    {
        GameObject root = new GameObject("HWJ_FinalBoss_Runtime");

        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.8f, 3.2f);
            collider.offset = new Vector2(0f, 1.6f);

            HWJ_RootObjectDataResolver resolver = root.AddComponent<HWJ_RootObjectDataResolver>();
            resolver.SetRootObjectData(rootData);
            HWJ_RuntimeObjectContext runtimeContext = root.AddComponent<HWJ_RuntimeObjectContext>();
            HWJ_RuntimeStatusSystem status = root.AddComponent<HWJ_RuntimeStatusSystem>();
            HWJ_CombatSystem combat = root.AddComponent<HWJ_CombatSystem>();
            HWJ_CharacterMotionSystem motion = root.AddComponent<HWJ_CharacterMotionSystem>();
            HWJ_BossPatternSystem patternSystem = root.AddComponent<HWJ_BossPatternSystem>();
            HWJ_FinalBossBarrierSystem barrier = root.AddComponent<HWJ_FinalBossBarrierSystem>();
            HWJ_FinalBossPatternSystem finalBoss = root.AddComponent<HWJ_FinalBossPatternSystem>();
            HWJ_BossBrainSystem brain = root.AddComponent<HWJ_BossBrainSystem>();
            root.AddComponent<HWJ_BossDuplicateGuardSystem>();

            Transform visualRoot = CreateChild(root.transform, "HWJ_FinalBoss_VisualRoot");
            visualRoot.localPosition = Vector3.zero;
            SpriteRenderer renderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.16f, 0.05f, 0.2f, 1f);
            renderer.sortingOrder = 15;
            visualRoot.localScale = new Vector3(1.8f, 3.2f, 1f);
            Animator animator = visualRoot.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            HWJ_FinalBossGestureRelay relay = visualRoot.gameObject.AddComponent<HWJ_FinalBossGestureRelay>();

            Transform projectileSocket = CreateChild(visualRoot, "ProjectileSocket");
            projectileSocket.localPosition = new Vector3(0f, 0.55f, 0f);
            Transform portalLeft = CreateChild(root.transform, "PortalAnchor_Left");
            portalLeft.localPosition = new Vector3(-4f, 0f, 0f);
            Transform portalRight = CreateChild(root.transform, "PortalAnchor_Right");
            portalRight.localPosition = new Vector3(4f, 0f, 0f);

            SetReferences(runtimeContext,
                ("dataResolver", resolver),
                ("runtimeStatus", status));
            SetReferences(status,
                ("runtimeContext", runtimeContext),
                ("dataResolver", resolver),
                ("bossBrain", brain));
            SetReferences(combat,
                ("runtimeContext", runtimeContext),
                ("dataResolver", resolver),
                ("runtimeStatus", status),
                ("bossBrain", brain));
            SetReferences(motion,
                ("animator", animator),
                ("spriteRenderer", renderer),
                ("runtimeStatus", status));
            SetReferences(brain,
                ("dataResolver", resolver),
                ("runtimeStatus", status),
                ("patternSystem", patternSystem),
                ("motionSystem", motion),
                ("animator", animator),
                ("body", body));
            SetBool(brain, "autoFindPlayerTarget", true);
            SetBool(brain, "aiEnabled", true);
            SetBool(brain, "useTwoBarPhaseHealth", false);

            ConfigurePatternSystem(patternSystem, patterns, finalBoss);
            ConfigureFinalBossSystem(
                finalBoss,
                brain,
                status,
                combat,
                barrier,
                body,
                animator,
                visualRoot,
                projectileSocket,
                new[] { portalLeft, portalRight },
                definitions);
            SetReferences(barrier, ("runtimeStatus", status), ("visualRoot", root.transform));
            SetReferences(relay, ("patternSystem", finalBoss));

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return saved != null ? saved : throw new InvalidOperationException("최종보스 프리팹 저장 실패");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigurePatternSystem(
        HWJ_BossPatternSystem patternSystem,
        HWJ_BossPatternDataSO[] patterns,
        HWJ_FinalBossPatternSystem executor)
    {
        SerializedObject serialized = new SerializedObject(patternSystem);
        SetObjectArray(serialized, "patterns", patterns);
        SetObjectArray(serialized, "specialPatternExecutors", new UnityEngine.Object[] { executor });
        SetBool(serialized, "autoUsePatterns", false);
        SetBool(serialized, "preventSamePatternRepeat", true);
        SetBool(serialized, "excludeRecentTwoPatterns", true);
        SetBool(serialized, "preventConsecutivePatternCategories", true);
        SetObject(serialized, "stageOnePatternSystem", null);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFinalBossSystem(
        HWJ_FinalBossPatternSystem finalBoss,
        HWJ_BossBrainSystem brain,
        HWJ_RuntimeStatusSystem status,
        HWJ_CombatSystem combat,
        HWJ_FinalBossBarrierSystem barrier,
        Rigidbody2D body,
        Animator animator,
        Transform visualRoot,
        Transform projectileSocket,
        Transform[] portalAnchors,
        HWJ_FinalBossPatternDefinitionSO[] definitions)
    {
        SerializedObject serialized = new SerializedObject(finalBoss);
        SetObject(serialized, "bossBrain", brain);
        SetObject(serialized, "runtimeStatus", status);
        SetObject(serialized, "combatSystem", combat);
        SetObject(serialized, "barrierSystem", barrier);
        SetObject(serialized, "body", body);
        SetObject(serialized, "animator", animator);
        SetObject(serialized, "bossVisualRoot", visualRoot);
        SetObject(serialized, "projectileSocket", projectileSocket);
        SetObjectArray(serialized, "portalAnchors", portalAnchors);
        SetObjectArray(serialized, "patternDefinitions", definitions);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignModelPrefab(HWJ_RootObjectDataSO rootData, GameObject prefab)
    {
        SerializedObject serialized = new SerializedObject(rootData);
        SerializedProperty model = RequireProperty(serialized, "model");
        SetRelativeObject(model, "modelPrefab", prefab);
        SetRelativeString(model, "heartEffectSocketName", string.Empty);
        SetRelativeString(model, "hitEffectSocketName", "HWJ_FinalBoss_VisualRoot");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootData);
    }

    private static void ValidateBuild(
        PatternSpec[] specs,
        HWJ_FinalBossPatternDefinitionSO[] definitions,
        HWJ_BossPatternDataSO[] patterns,
        HWJ_BossTypeDataSO typeData,
        HWJ_RootObjectDataSO rootData,
        GameObject prefab)
    {
        if (specs.Length != 10 || definitions.Length != 10 || patterns.Length != 10)
        {
            throw new InvalidOperationException("최종보스는 패턴 10개가 모두 생성되어야 합니다.");
        }

        if (typeData == null
            || typeData.BossRank != HWJ_BossRank.FinalBoss
            || typeData.FSM.locomotionMode != HWJ_BossLocomotionMode.StationaryCaster)
        {
            throw new InvalidOperationException("최종보스 TypeData의 등급 또는 고정형 이동 정책이 잘못되었습니다.");
        }

        if (rootData == null
            || rootData.ObjectType != HWJ_ObjectType.Boss
            || rootData.SelectedTypeData != typeData)
        {
            throw new InvalidOperationException("최종보스 RootObjectData 연결이 잘못되었습니다.");
        }

        if (typeData.PossessionBody == null || typeData.PossessionBody.canBePossessed)
        {
            throw new InvalidOperationException("최종보스는 빙의 불가 상태여야 합니다.");
        }

        HWJ_FinalBossPatternDefinitionSO charge = definitions[7];
        HWJ_FinalBossPatternDefinitionSO doublePortal = definitions[8];

        if (!Mathf.Approximately(charge.PossessionMentalDamageRatio, 0.1f))
        {
            throw new InvalidOperationException("2-3 패턴의 현재 빙의 정신력 피해가 10%가 아닙니다.");
        }

        if (doublePortal.PortalCount != 2 || doublePortal.MonstersPerPortal != 4)
        {
            throw new InvalidOperationException("2-4 패턴은 포탈 2개에서 각각 4마리를 소환해야 합니다.");
        }

        if (prefab == null
            || prefab.GetComponent<HWJ_FinalBossPatternSystem>() == null
            || prefab.GetComponent<HWJ_FinalBossBarrierSystem>() == null
            || prefab.GetComponent<HWJ_BossBrainSystem>() == null
            || prefab.GetComponent<HWJ_BossPatternSystem>() == null)
        {
            throw new InvalidOperationException("최종보스 런타임 프리팹 연결이 불완전합니다.");
        }
    }

    private static GameObject[] LoadSummonPrefabs()
    {
        string root = "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies";
        string[] names = { "Sword", "Lance", "Axe", "Bow", "Shield" };
        List<GameObject> assets = new List<GameObject>();

        for (int i = 0; i < names.Length; i++)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{root}/HWJ_Runtime_Enemy_Possessable_{names[i]}.prefab");

            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        return assets.ToArray();
    }

    private static HWJ_RootObjectDataSO[] LoadSummonRootObjects()
    {
        string root = "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Enemies";
        string[] names = { "Sword", "Lance", "Axe", "Bow", "Shield" };
        List<HWJ_RootObjectDataSO> assets = new List<HWJ_RootObjectDataSO>();

        for (int i = 0; i < names.Length; i++)
        {
            HWJ_RootObjectDataSO asset = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(
                $"{root}/HWJ_EnemyCorpse_{names[i]}_RootObjectData.asset");

            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        return assets.ToArray();
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void ConfigurePhase(
        SerializedProperty phase,
        string phaseId,
        float startRatio,
        bool changesStage)
    {
        SetRelativeString(phase, "phaseId", phaseId);
        SetRelativeFloat(phase, "startHpRatio", startRatio);
        SetRelativeString(phase, "stageEventId", string.Empty);
        SetRelativeBool(phase, "changesStage", changesStage);
        SerializedProperty skillSet = phase.FindPropertyRelative("skillSet");

        if (skillSet != null)
        {
            SerializedProperty skills = skillSet.FindPropertyRelative("skills");

            if (skills != null)
            {
                skills.arraySize = 0;
            }
        }
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void SetReferences(UnityEngine.Object target, params (string, UnityEngine.Object)[] values)
    {
        SerializedObject serialized = new SerializedObject(target);

        for (int i = 0; i < values.Length; i++)
        {
            SetObject(serialized, values[i].Item1, values[i].Item2);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetBool(UnityEngine.Object target, string name, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SetBool(serialized, name, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static SerializedProperty RequireProperty(SerializedObject serialized, string name)
    {
        return serialized.FindProperty(name)
            ?? throw new InvalidOperationException($"{serialized.targetObject.GetType().Name}.{name} 필드를 찾지 못했습니다.");
    }

    private static SerializedProperty RequireRelative(SerializedProperty parent, string name)
    {
        return parent.FindPropertyRelative(name)
            ?? throw new InvalidOperationException($"{parent.propertyPath}.{name} 필드를 찾지 못했습니다.");
    }

    private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value) =>
        RequireProperty(serialized, name).objectReferenceValue = value;
    private static void SetBool(SerializedObject serialized, string name, bool value) =>
        RequireProperty(serialized, name).boolValue = value;
    private static void SetString(SerializedObject serialized, string name, string value) =>
        RequireProperty(serialized, name).stringValue = value;
    private static void SetFloat(SerializedObject serialized, string name, float value) =>
        RequireProperty(serialized, name).floatValue = value;
    private static void SetInt(SerializedObject serialized, string name, int value) =>
        RequireProperty(serialized, name).intValue = value;
    private static void SetEnum(SerializedObject serialized, string name, int value) =>
        RequireProperty(serialized, name).intValue = value;
    private static void SetVector2(SerializedObject serialized, string name, Vector2 value) =>
        RequireProperty(serialized, name).vector2Value = value;

    private static void SetObjectArray(
        SerializedObject serialized,
        string name,
        UnityEngine.Object[] values)
    {
        SerializedProperty property = RequireProperty(serialized, name);
        property.arraySize = values != null ? values.Length : 0;

        for (int i = 0; values != null && i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetRelativeObject(SerializedProperty parent, string name, UnityEngine.Object value) =>
        RequireRelative(parent, name).objectReferenceValue = value;
    private static void SetRelativeBool(SerializedProperty parent, string name, bool value) =>
        RequireRelative(parent, name).boolValue = value;
    private static void SetRelativeString(SerializedProperty parent, string name, string value) =>
        RequireRelative(parent, name).stringValue = value;
    private static void SetRelativeFloat(SerializedProperty parent, string name, float value) =>
        RequireRelative(parent, name).floatValue = value;
    private static void SetRelativeInt(SerializedProperty parent, string name, int value) =>
        RequireRelative(parent, name).intValue = value;
    private static void SetRelativeEnum(SerializedProperty parent, string name, int value) =>
        RequireRelative(parent, name).intValue = value;
    private static void SetRelativeVector2(SerializedProperty parent, string name, Vector2 value) =>
        RequireRelative(parent, name).vector2Value = value;
}
