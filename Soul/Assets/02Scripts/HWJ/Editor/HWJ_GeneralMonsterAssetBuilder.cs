using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 일반 몬스터 기획서의 슬라임과 쥐 데이터를 생성하고 기존 생체 빙의 몬스터를 공통 AI 구조로 이관합니다.
/// </summary>
public static class HWJ_GeneralMonsterAssetBuilder
{
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string GeneralTypeRoot = AssetRoot + "/ScriptableObjects/TypeData/Enemy/General";
    private const string EnemyRootObjectRoot = AssetRoot + "/ScriptableObjects/RootObjects/Enemies";
    private const string ModelRoot = AssetRoot + "/Prefabs/Generated/Models";
    private const string RuntimeEnemyRoot = AssetRoot + "/Prefabs/Generated/RuntimeReady/Enemies";
    private const string DatabasePath = AssetRoot + "/ScriptableObjects/Database/HWJ_GameplayDatabase.asset";
    private const string SpawnTablePath = AssetRoot + "/ScriptableObjects/HWJ_SpawnTableData.asset";
    private const string AllSystemsPrefabPath = AssetRoot + "/Prefabs/Generated/RuntimeReady/HWJ_Runtime_AllSystems_DropIn.prefab";
    private const string TitleDataPath = AssetRoot + "/ScriptableObjects/Systems/HWJ_TitleScreenData_Default.asset";
    private const string TitlePrefabPath = AssetRoot + "/Prefabs/Generated/UI/HWJ_UI_TitleScreen.prefab";
    private const string MarkerSpritePath = AssetRoot + "/Art/Generated/FunctionShowcase/HWJ_Showcase_Box.png";
    private const string ExperienceOrbPath = AssetRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab";
    private const string ReportPath = AssetRoot + "/Docs/Generated/HWJ_일반몬스터_생성보고서.md";

    private static readonly HWJ_GeneralMonsterSpec[] GeneralMonsterSpecs =
    {
        new HWJ_GeneralMonsterSpec(
            "Slime",
            "enemy.general.slime",
            "일반 슬라임",
            60f,
            2.2f,
            8f,
            1f,
            1.1f,
            60f,
            HWJ_EnemyBasicAttackMode.Contact,
            0f,
            1.2f,
            new Color(0.32f, 0.82f, 0.38f, 1f)),
        new HWJ_GeneralMonsterSpec(
            "Rat",
            "enemy.general.rat",
            "일반 쥐",
            45f,
            3.4f,
            10f,
            0f,
            1.25f,
            50f,
            HWJ_EnemyBasicAttackMode.AnimationEvent,
            0.25f,
            1.5f,
            new Color(0.58f, 0.38f, 0.32f, 1f))
    };

    private static readonly string[] ExistingWeaponNames =
    {
        "Sword",
        "Axe",
        "Bow",
        "Lance",
        "Shield"
    };

    [MenuItem("Tools/HWJ/Monsters/일반 몬스터 생성 및 갱신")]
    public static void BuildGeneralMonsters()
    {
        List<string> report = new List<string>();

        try
        {
            EnsureFolders();
            ConfigureExistingPossessableEnemyData(report);
            HWJ_TitleScreenDataSO titleScreenData = RepairTitleScreenData(report);

            HWJ_RootObjectDataSO[] newRootObjects = new HWJ_RootObjectDataSO[GeneralMonsterSpecs.Length];
            GameObject[] newRuntimePrefabs = new GameObject[GeneralMonsterSpecs.Length];

            for (int i = 0; i < GeneralMonsterSpecs.Length; i++)
            {
                HWJ_GeneralMonsterSpec spec = GeneralMonsterSpecs[i];
                HWJ_EnemyTypeDataSO typeData = CreateOrUpdateTypeData(spec, report);
                GameObject modelPrefab = CreateOrUpdateModelPrefab(spec, report);
                HWJ_RootObjectDataSO rootObjectData = CreateOrUpdateRootObjectData(
                    spec,
                    typeData,
                    modelPrefab,
                    report);
                GameObject runtimePrefab = CreateOrUpdateRuntimePrefab(
                    spec,
                    rootObjectData,
                    modelPrefab,
                    report);

                newRootObjects[i] = rootObjectData;
                newRuntimePrefabs[i] = runtimePrefab;
            }

            MigrateExistingPossessableEnemyPrefabs(report);
            UpdateGameplayDatabase(newRootObjects, titleScreenData, report);
            UpdateSpawnTable(newRootObjects, newRuntimePrefabs, report);
            UpdateAllSystemsDropIn(newRuntimePrefabs, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateResult(newRootObjects, newRuntimePrefabs, report);
            WriteReport("완료", report, null);
            Debug.Log("[HWJ] 일반 몬스터 생성과 갱신이 완료되었습니다.");
        }
        catch (Exception exception)
        {
            WriteReport("실패", report, exception);
            Debug.LogException(exception);
            throw;
        }
    }

    /// <summary>
    /// Unity 배치 모드에서 동일한 생성 작업을 실행하는 진입점입니다.
    /// </summary>
    public static void BuildFromCommandLine()
    {
        BuildGeneralMonsters();
    }

    private static HWJ_EnemyTypeDataSO CreateOrUpdateTypeData(
        HWJ_GeneralMonsterSpec spec,
        List<string> report)
    {
        string path = GetTypeDataPath(spec);
        HWJ_EnemyTypeDataSO typeData = AssetDatabase.LoadAssetAtPath<HWJ_EnemyTypeDataSO>(path);

        if (typeData == null)
        {
            typeData = ScriptableObject.CreateInstance<HWJ_EnemyTypeDataSO>();
            AssetDatabase.CreateAsset(typeData, path);
        }

        SerializedObject serializedData = new SerializedObject(typeData);
        SetEnum(serializedData, "objectType", (int)HWJ_ObjectType.Enemy);
        SetString(serializedData, "typeId", spec.StableId);
        SetString(serializedData, "displayName", spec.DisplayName);
        SetEnum(serializedData, "defaultWeaponType", (int)HWJ_WeaponType.None);
        SetString(
            serializedData,
            "description",
            "일반 몬스터 공통 FSM을 사용합니다. 애니메이션과 이펙트는 후속 아트 작업에서 연결합니다.");

        SerializedProperty role = RequireProperty(serializedData, "role");
        SetRelativeEnum(role, "weaponType", (int)HWJ_WeaponType.None);
        SetRelativeBool(role, "isElite", false);
        SetRelativeBool(role, "leavesCorpseOnDeath", false);
        SetRelativeBool(role, "isPossessableBody", true);
        SetRelativeBool(role, "guaranteesStatOrb", false);

        SerializedProperty tracking = RequireProperty(serializedData, "tracking");
        SetRelativeFloat(tracking, "trackingRange", 8f);
        SetRelativeFloat(tracking, "loseTargetRange", 8f);

        ConfigureCommonPatrolVisionAndReturn(serializedData);

        SerializedProperty ai = RequireProperty(serializedData, "ai");
        SetRelativeString(ai, "aiProfileId", "ai.general." + spec.CodeName.ToLowerInvariant());
        SetRelativeFloat(ai, "decisionIntervalSeconds", 0.1f);
        SetRelativeString(ai, "behaviorTreeId", string.Empty);
        SetRelativeFloat(ai, "idleSeconds", 0.35f);
        SetRelativeFloat(ai, "detectSeconds", 0.15f);
        SetRelativeFloat(ai, "attackPrepareSeconds", spec.AttackMode == HWJ_EnemyBasicAttackMode.Contact ? 0.05f : 0.25f);
        SetRelativeFloat(ai, "attackRecoverySeconds", 0.4f);
        SetRelativeFloat(ai, "repathSeconds", 0.15f);
        SetRelativeFloat(ai, "defaultSkillCooldownSeconds", 3f);
        SetRelativeFloat(ai, "basicAttackIntervalSeconds", spec.BasicAttackIntervalSeconds);
        SetRelativeInt(ai, "skillCycleCount", 0);
        SetRelativeFloat(ai, "skillCycleResetDelaySeconds", 5f);

        SerializedProperty navigation = RequireProperty(serializedData, "navigation");
        SetRelativeFloat(navigation, "stoppingDistance", spec.AttackRange * 0.8f);
        SetRelativeFloat(navigation, "pathRefreshSeconds", 0.15f);
        SetRelativeBool(navigation, "canUsePlatformDrop", false);
        SetRelativeBool(navigation, "avoidLedges", true);
        SetRelativeFloat(navigation, "ledgeCheckForwardDistance", 0.45f);
        SetRelativeFloat(navigation, "ledgeCheckDownDistance", 1.2f);
        SetRelativeFloat(navigation, "wallCheckDistance", 0.18f);

        SerializedProperty state = RequireProperty(serializedData, "state");
        SetRelativeEnum(state, "startState", (int)HWJ_RuntimeState.Idle);
        SetRelativeFloat(state, "attackRange", spec.AttackRange);
        SetRelativeFloat(state, "returnToIdleDelaySeconds", 0.5f);
        SetRelativeBool(state, "stopWhenHit", true);
        SetRelativeBool(state, "hasSuperArmor", false);
        SetRelativeBool(state, "immuneToHitStun", false);

        SerializedProperty basicAttack = RequireProperty(serializedData, "basicAttack");
        SetRelativeEnum(basicAttack, "mode", (int)spec.AttackMode);
        SetRelativeString(basicAttack, "motionKey", spec.CodeName == "Rat" ? "Bite" : "ContactAttack");
        SetRelativeFloat(basicAttack, "fallbackHitDelaySeconds", spec.FallbackHitDelaySeconds);
        SetRelativeFloat(basicAttack, "hitRangeTolerance", 0.2f);
        SetRelativeFloat(basicAttack, "damageMultiplier", 1f);
        SetRelativeFloat(basicAttack, "contactDamageIntervalSeconds", spec.BasicAttackIntervalSeconds);

        RequireProperty(serializedData, "skillCycle").FindPropertyRelative("skills").arraySize = 0;
        RequireProperty(serializedData, "playerPossessionSkillSet").FindPropertyRelative("skills").arraySize = 0;

        SerializedProperty possession = RequireProperty(serializedData, "possessionBody");
        SetRelativeBool(possession, "canPossess", false);
        SetRelativeBool(possession, "canBePossessed", true);
        SetRelativeString(possession, "heartSocketName", "Heart");
        SetRelativeFloat(possession, "possessionRange", 2f);
        SetRelativeFloat(possession, "spiritMentalCostOnPossession", 5f);
        SetRelativeFloat(possession, "livePossessionMaxMental", spec.PossessionMental);
        SetRelativeFloat(possession, "livePossessionMentalCostOnSuccess", 10f);
        SetRelativeFloat(possession, "livePossessionMentalDrainInterval", 1f);
        SetRelativeFloat(possession, "livePossessionMentalDrainAmount", 1f);
        SetRelativeFloat(possession, "livePossessionSuccessChance", 1f);
        SetRelativeFloat(possession, "livePossessionFailureSpiritMentalCost", 10f);
        SetRelativeFloat(possession, "livePossessionFailureControlLockSeconds", 0.5f);
        SetRelativeFloat(possession, "livePossessionFailureKnockbackPower", 3f);
        SetRelativeFloat(possession, "livePossessionFailureKnockbackSeconds", 0.2f);
        SetRelativeString(possession, "livePossessionFailureMessage", "대상의 정신력에 밀려 빙의에 실패했다.");
        SetRelativeBool(possession, "requiresDefeatedState", false);
        SetRelativeBool(possession, "transfersControlToBody", true);
        SetRelativeBool(possession, "loadsBodyStatsToPlayer", true);
        // Living-body mental is managed by HWJ_LivePossessionMentalState.
        // The legacy body-decay override is reserved for corpse possession data.
        SetRelativeBool(possession, "overrideBodyDecayOnPossession", false);

        serializedData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(typeData);
        report.Add("일반 몬스터 TypeData 생성/갱신: " + path);
        return typeData;
    }

    private static void ConfigureCommonPatrolVisionAndReturn(SerializedObject serializedData)
    {
        SerializedProperty patrol = RequireProperty(serializedData, "patrol");
        SetRelativeBool(patrol, "enabled", true);
        SetRelativeFloat(patrol, "radius", 2.5f);
        SetRelativeFloat(patrol, "speedMultiplier", 0.55f);
        SetRelativeFloat(patrol, "turnPauseSeconds", 0.15f);

        SerializedProperty vision = RequireProperty(serializedData, "vision");
        SetRelativeFloat(vision, "viewDistance", 3f);
        SetRelativeFloat(vision, "viewAngle", 180f);
        SetRelativeBool(vision, "requireSamePlatform", true);
        SetRelativeFloat(vision, "samePlatformHeightTolerance", 0.4f);
        SetRelativeFloat(vision, "groundProbeDistance", 3f);
        SetRelativeBool(vision, "requireClearLineOfSight", true);

        SerializedProperty returnBehavior = RequireProperty(serializedData, "returnBehavior");
        SetRelativeFloat(returnBehavior, "maxChaseDistanceFromSpawn", 8f);
        SetRelativeFloat(returnBehavior, "speedMultiplier", 1f);
        SetRelativeFloat(returnBehavior, "arrivalDistance", 0.1f);
        SetRelativeBool(returnBehavior, "waitBelowDifferentPlatform", true);
    }

    private static GameObject CreateOrUpdateModelPrefab(
        HWJ_GeneralMonsterSpec spec,
        List<string> report)
    {
        Sprite marker = RequireAsset<Sprite>(MarkerSpritePath);
        GameObject modelRoot = new GameObject("HWJ_Model_Enemy_General_" + spec.CodeName);
        SpriteRenderer bodyRenderer = modelRoot.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = marker;
        bodyRenderer.color = spec.PlaceholderColor;
        bodyRenderer.sortingOrder = 20;
        modelRoot.AddComponent<Animator>();
        modelRoot.AddComponent<HWJ_EnemyAttackAnimationRelay>();

        if (spec.CodeName == "Slime")
        {
            modelRoot.transform.localScale = new Vector3(1.15f, 0.7f, 1f);
            CreateMarkerPart(modelRoot.transform, "LeftEye", marker, Color.black, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.12f, 0.16f, 1f), 21);
            CreateMarkerPart(modelRoot.transform, "RightEye", marker, Color.black, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.12f, 0.16f, 1f), 21);
        }
        else
        {
            modelRoot.transform.localScale = new Vector3(0.95f, 0.65f, 1f);
            CreateMarkerPart(modelRoot.transform, "LeftEar", marker, new Color(0.32f, 0.18f, 0.16f, 1f), new Vector3(-0.26f, 0.45f, 0f), new Vector3(0.22f, 0.26f, 1f), 19);
            CreateMarkerPart(modelRoot.transform, "RightEar", marker, new Color(0.32f, 0.18f, 0.16f, 1f), new Vector3(0.26f, 0.45f, 0f), new Vector3(0.22f, 0.26f, 1f), 19);
            CreateMarkerPart(modelRoot.transform, "Tail", marker, new Color(0.78f, 0.5f, 0.48f, 1f), new Vector3(-0.65f, -0.05f, 0f), new Vector3(0.55f, 0.08f, 1f), 19);
        }

        CreateSocket(modelRoot.transform, "Heart", new Vector3(0f, 0.15f, 0f));
        CreateSocket(modelRoot.transform, "Hit", Vector3.zero);
        CreateSocket(modelRoot.transform, "Eyes", new Vector3(0f, 0.2f, 0f));

        string path = GetModelPrefabPath(spec);
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(modelRoot, path);
        UnityEngine.Object.DestroyImmediate(modelRoot);

        if (savedPrefab == null)
        {
            throw new InvalidOperationException("일반 몬스터 모델 프리팹 저장 실패: " + path);
        }

        report.Add("임시 모델 프리팹 생성/갱신: " + path);
        return savedPrefab;
    }

    private static HWJ_RootObjectDataSO CreateOrUpdateRootObjectData(
        HWJ_GeneralMonsterSpec spec,
        HWJ_EnemyTypeDataSO typeData,
        GameObject modelPrefab,
        List<string> report)
    {
        string path = GetRootObjectDataPath(spec);
        HWJ_RootObjectDataSO rootData = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(path);

        if (rootData == null)
        {
            rootData = ScriptableObject.CreateInstance<HWJ_RootObjectDataSO>();
            AssetDatabase.CreateAsset(rootData, path);
        }

        SerializedObject serializedData = new SerializedObject(rootData);
        SerializedProperty identity = RequireProperty(serializedData, "identity");
        SetRelativeString(identity, "objectId", spec.StableId);
        SetRelativeString(identity, "displayName", spec.DisplayName);
        SetRelativeEnum(identity, "objectType", (int)HWJ_ObjectType.Enemy);
        SetRelativeEnum(identity, "faction", (int)HWJ_Faction.Monster);
        SetRelativeEnum(identity, "weaponType", (int)HWJ_WeaponType.None);
        identity.FindPropertyRelative("abilityTags").arraySize = 0;

        SerializedProperty status = RequireProperty(serializedData, "status");
        SetRelativeInt(status, "level", 1);
        SetRelativeFloat(status, "maxHp", spec.MaximumHp);
        SetRelativeFloat(status, "moveSpeed", spec.MoveSpeed);
        SetRelativeFloat(status, "attackPower", spec.AttackPower);
        SetRelativeFloat(status, "defense", spec.Defense);
        SetRelativeFloat(status, "attackSpeed", 1f);
        SetRelativeFloat(status, "bodyWeight", spec.CodeName == "Slime" ? 1.2f : 0.7f);

        SerializedProperty model = RequireProperty(serializedData, "model");
        SetRelativeObject(model, "modelPrefab", modelPrefab);
        SetRelativeObject(model, "animatorController", null);
        SetRelativeString(model, "heartEffectSocketName", "Heart");
        SetRelativeString(model, "hitEffectSocketName", "Hit");
        SetRelativeString(model, "possessedEyeSocketName", "Eyes");

        SerializedProperty damage = RequireProperty(serializedData, "damage");
        SetRelativeEnum(damage, "damageType", (int)HWJ_DamageType.Physical);
        SetRelativeFloat(damage, "baseDamage", 0f);
        SetRelativeFloat(damage, "criticalChance", 0f);
        SetRelativeFloat(damage, "criticalMultiplier", 1.5f);
        SetRelativeFloat(damage, "knockbackPower", spec.CodeName == "Slime" ? 1.5f : 2.5f);
        SetRelativeFloat(damage, "hitStunSeconds", 0.12f);
        SetRelativeFloat(damage, "hitStopSeconds", 0f);
        SetRelativeFloat(damage, "sameTargetHitCooldownSeconds", 0.08f);

        SerializedProperty receivedDamage = RequireProperty(serializedData, "receivedDamage");
        SetRelativeFloat(receivedDamage, "damageMultiplier", 1f);
        SetRelativeFloat(receivedDamage, "invincibleSecondsAfterHit", 0.1f);
        SetRelativeBool(receivedDamage, "isInvincible", false);
        SetRelativeBool(receivedDamage, "ignoreTrapDamage", false);
        SetRelativeBool(receivedDamage, "ignoreEnemyDamage", false);
        SetRelativeFloat(receivedDamage, "hitStunSeconds", 0.18f);
        SetRelativeFloat(receivedDamage, "hitReactionImmuneSeconds", 0.08f);
        SetRelativeInt(receivedDamage, "maxHitReactionsPerWindow", 3);
        SetRelativeFloat(receivedDamage, "hitReactionWindowSeconds", 1f);
        SetRelativeFloat(receivedDamage, "hitReactionLimitImmuneSeconds", 0.2f);
        SetRelativeFloat(receivedDamage, "knockbackWeightMultiplier", spec.CodeName == "Slime" ? 1.2f : 0.75f);
        SetRelativeBool(receivedDamage, "hasSuperArmor", false);
        SetRelativeBool(receivedDamage, "ignoreHitStun", false);
        SetRelativeBool(receivedDamage, "ignoreKnockback", false);

        SerializedProperty interaction = RequireProperty(serializedData, "interaction");
        SetRelativeBool(interaction, "canInteract", true);
        SetRelativeBool(interaction, "canBeTargeted", true);
        SetRelativeFloat(interaction, "interactionRange", 2f);

        SerializedProperty reward = RequireProperty(serializedData, "reward");
        SetRelativeInt(reward, "experienceReward", 10);
        SetRelativeInt(reward, "skillPointReward", 0);
        SetRelativeBool(reward, "dropsExperienceOrb", true);
        SetRelativeObject(reward, "experienceOrbPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ExperienceOrbPath));
        SetRelativeFloat(reward, "experienceOrbSpawnRadius", 0.35f);
        SetRelativeBool(reward, "dropsStatOrb", false);
        SetRelativeFloat(reward, "statOrbDropChance", 0f);
        SetRelativeFloat(reward, "statOrbSpawnRadius", 0.5f);
        SetRelativeString(reward, "statOrbId", string.Empty);
        reward.FindPropertyRelative("statOrbCandidates").arraySize = 0;
        SetRelativeBool(reward, "useAllRegisteredStatOrbsWhenEmpty", false);

        SetObject(serializedData, "selectedTypeData", typeData);
        serializedData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootData);
        report.Add("RootObjectData 생성/갱신: " + path);
        return rootData;
    }

    private static GameObject CreateOrUpdateRuntimePrefab(
        HWJ_GeneralMonsterSpec spec,
        HWJ_RootObjectDataSO rootData,
        GameObject modelPrefab,
        List<string> report)
    {
        GameObject root = new GameObject("HWJ_Runtime_Enemy_General_" + spec.CodeName);
        SetLayerIfExists(root, "Enemy");

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D bodyCollider = root.AddComponent<BoxCollider2D>();
        bodyCollider.size = spec.CodeName == "Slime"
            ? new Vector2(1.1f, 0.7f)
            : new Vector2(0.9f, 0.7f);
        bodyCollider.offset = new Vector2(0f, -0.15f);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
        visual.name = "HWJ_Visual";
        visual.transform.localPosition = spec.CodeName == "Slime"
            ? new Vector3(0f, -0.12f, 0f)
            : new Vector3(0f, -0.05f, 0f);
        visual.transform.localRotation = Quaternion.identity;

        SpriteRenderer renderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        HWJ_EnemyAttackAnimationRelay relay = visual.GetComponentInChildren<HWJ_EnemyAttackAnimationRelay>(true);

        if (relay == null)
        {
            relay = visual.AddComponent<HWJ_EnemyAttackAnimationRelay>();
        }

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
        root.AddComponent<HWJ_PossessionBodyState>();
        HWJ_LivePossessionMentalState liveMental = root.AddComponent<HWJ_LivePossessionMentalState>();
        HWJ_EnemyNavigationSystem navigation = root.AddComponent<HWJ_EnemyNavigationSystem>();
        HWJ_EnemyAttackSystem enemyAttack = root.AddComponent<HWJ_EnemyAttackSystem>();
        HWJ_EnemyPerceptionSystem perception = root.AddComponent<HWJ_EnemyPerceptionSystem>();
        HWJ_MonsterAISystem monsterAI = root.AddComponent<HWJ_MonsterAISystem>();
        HWJ_EnemyDeathLifecycleSystem deathLifecycle = root.AddComponent<HWJ_EnemyDeathLifecycleSystem>();

        SetEnum(new SerializedObject(status), "currentState", (int)HWJ_RuntimeState.Idle, true);
        ConfigureSkillAction(skillAction, resolver, status, combat, combatExecution, motion, body);
        ConfigureCombatExecution(combatExecution);
        ConfigureMotion(motion, animator, renderer, body, resolver, status);
        ConfigureHitEffect(hitEffect, status, resolver);
        ConfigureEnemyBehavior(
            navigation,
            enemyAttack,
            perception,
            monsterAI,
            resolver,
            status,
            skillAction,
            motion,
            body,
            false);

        SerializedObject deathSo = new SerializedObject(deathLifecycle);
        SetObject(deathSo, "dataResolver", resolver);
        SetObject(deathSo, "runtimeStatus", status);
        SetFloat(deathSo, "defeatedEnemyDespawnDelaySeconds", 0.05f);
        deathSo.ApplyModifiedPropertiesWithoutUndo();

        liveMental.LoadConfigurationFromTypeData();
        CreateWorldHealthBar(root.transform, status, resolver, spec.CodeName == "Slime" ? 0.8f : 0.9f);

        string path = GetRuntimePrefabPath(spec);
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);

        if (savedPrefab == null)
        {
            throw new InvalidOperationException("일반 몬스터 실행 프리팹 저장 실패: " + path);
        }

        report.Add("실행 프리팹 생성/갱신: " + path);
        return savedPrefab;
    }

    private static void ConfigureExistingPossessableEnemyData(List<string> report)
    {
        for (int i = 0; i < ExistingWeaponNames.Length; i++)
        {
            string weaponName = ExistingWeaponNames[i];
            string typePath = AssetRoot
                + "/ScriptableObjects/TypeData/Enemy/Possessable/HWJ_EnemyCorpse_"
                + weaponName
                + "_TypeData.asset";
            HWJ_EnemyTypeDataSO enemyData = AssetDatabase.LoadAssetAtPath<HWJ_EnemyTypeDataSO>(typePath);

            if (enemyData == null)
            {
                throw new InvalidOperationException("기존 빙의 가능 몬스터 TypeData 누락: " + typePath);
            }

            SerializedObject serializedData = new SerializedObject(enemyData);
            ConfigureCommonPatrolVisionAndReturn(serializedData);
            SerializedProperty role = RequireProperty(serializedData, "role");
            SetRelativeBool(role, "leavesCorpseOnDeath", false);
            SetRelativeBool(role, "isPossessableBody", true);
            SerializedProperty possession = RequireProperty(serializedData, "possessionBody");
            SetRelativeBool(possession, "canBePossessed", true);
            SetRelativeBool(possession, "requiresDefeatedState", false);
            SerializedProperty basicAttack = RequireProperty(serializedData, "basicAttack");
            SetRelativeEnum(basicAttack, "mode", (int)HWJ_EnemyBasicAttackMode.Immediate);
            SetRelativeString(basicAttack, "motionKey", "BasicAttack");
            SetRelativeFloat(basicAttack, "damageMultiplier", 1f);
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyData);
            report.Add("기존 무기 몬스터 공통 AI 데이터 이관: " + typePath);
        }
    }

    private static void MigrateExistingPossessableEnemyPrefabs(List<string> report)
    {
        for (int i = 0; i < ExistingWeaponNames.Length; i++)
        {
            string path = RuntimeEnemyRoot
                + "/HWJ_Runtime_Enemy_Possessable_"
                + ExistingWeaponNames[i]
                + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                report.Add("경고: 기존 무기 몬스터 프리팹을 찾지 못해 이관 생략: " + path);
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                HWJ_RootObjectDataResolver resolver = RequireComponent<HWJ_RootObjectDataResolver>(root, path);
                HWJ_RuntimeStatusSystem status = RequireComponent<HWJ_RuntimeStatusSystem>(root, path);
                HWJ_SkillActionSystem skillAction = RequireComponent<HWJ_SkillActionSystem>(root, path);
                HWJ_CharacterMotionSystem motion = RequireComponent<HWJ_CharacterMotionSystem>(root, path);
                Rigidbody2D body = RequireComponent<Rigidbody2D>(root, path);
                HWJ_EnemyNavigationSystem navigation = GetOrAdd<HWJ_EnemyNavigationSystem>(root);
                HWJ_EnemyAttackSystem enemyAttack = GetOrAdd<HWJ_EnemyAttackSystem>(root);
                HWJ_EnemyPerceptionSystem perception = GetOrAdd<HWJ_EnemyPerceptionSystem>(root);
                HWJ_MonsterAISystem monsterAI = GetOrAdd<HWJ_MonsterAISystem>(root);
                GetOrAdd<HWJ_EnemyDeathLifecycleSystem>(root);
                Animator animator = root.GetComponentInChildren<Animator>(true);

                if (animator != null && animator.GetComponent<HWJ_EnemyAttackAnimationRelay>() == null)
                {
                    animator.gameObject.AddComponent<HWJ_EnemyAttackAnimationRelay>();
                }

                ConfigureEnemyBehavior(
                    navigation,
                    enemyAttack,
                    perception,
                    monsterAI,
                    resolver,
                    status,
                    skillAction,
                    motion,
                    body,
                    true);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.Add("기존 무기 몬스터 프리팹 공통 인식 시스템 이관: " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ConfigureEnemyBehavior(
        HWJ_EnemyNavigationSystem navigation,
        HWJ_EnemyAttackSystem enemyAttack,
        HWJ_EnemyPerceptionSystem perception,
        HWJ_MonsterAISystem monsterAI,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status,
        HWJ_SkillActionSystem skillAction,
        HWJ_CharacterMotionSystem motion,
        Rigidbody2D body,
        bool useSkillCycle)
    {
        LayerMask groundMask = HWJ_PhysicsLayerUtility.CreateGroundMaskForCurrentProject();

        SerializedObject navigationSo = new SerializedObject(navigation);
        SetObject(navigationSo, "dataResolver", resolver);
        SetObject(navigationSo, "runtimeStatus", status);
        SetObject(navigationSo, "skillActionSystem", skillAction);
        SetObject(navigationSo, "motionSystem", motion);
        SetObject(navigationSo, "monsterAI", monsterAI);
        SetObject(navigationSo, "body", body);
        SetBool(navigationSo, "autoFindPlayerTarget", true);
        SetBool(navigationSo, "chaseOnlyBodyState", true);
        SetBool(navigationSo, "faceOnlyBodyState", true);
        SetBool(navigationSo, "horizontalMoveOnly", true);
        SetLayerMask(navigationSo, "groundLayer", groundMask);
        navigationSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject attackSo = new SerializedObject(enemyAttack);
        SetObject(attackSo, "dataResolver", resolver);
        SetObject(attackSo, "runtimeStatus", status);
        SetObject(attackSo, "skillActionSystem", skillAction);
        SetObject(attackSo, "monsterAI", monsterAI);
        SetObject(attackSo, "motionSystem", motion);
        SetObject(attackSo, "animator", motion.GetComponentInChildren<Animator>(true));
        SetBool(attackSo, "autoFindPlayerTarget", true);
        SetBool(attackSo, "autoAttackWhenNoBehaviorDriver", false);
        SetBool(attackSo, "attackOnlyBodyState", true);
        SetBool(attackSo, "useSkillCycle", useSkillCycle);
        SetBool(attackSo, "showSkillWarning", useSkillCycle);
        SetFloat(attackSo, "skillWarningDelaySeconds", 1f);
        attackSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject perceptionSo = new SerializedObject(perception);
        SetObject(perceptionSo, "dataResolver", resolver);
        SetObject(perceptionSo, "motionSystem", motion);
        SetBool(perceptionSo, "autoFindPlayerTarget", true);
        SetLayerMask(perceptionSo, "groundLayer", groundMask);
        perceptionSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject aiSo = new SerializedObject(monsterAI);
        SetObject(aiSo, "dataResolver", resolver);
        SetObject(aiSo, "runtimeStatus", status);
        SetObject(aiSo, "enemyAttackSystem", enemyAttack);
        SetObject(aiSo, "perceptionSystem", perception);
        SetObject(aiSo, "skillActionSystem", skillAction);
        SetObject(aiSo, "motionSystem", motion);
        SetObject(aiSo, "body", body);
        SetBool(aiSo, "driveBehavior", true);
        SetBool(aiSo, "autoFindPlayerTarget", true);
        SetBool(aiSo, "targetOnlyBodyState", true);
        SetBool(aiSo, "horizontalMoveOnly", true);
        SetLayerMask(aiSo, "groundLayer", groundMask);
        aiSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkillAction(
        HWJ_SkillActionSystem skillAction,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status,
        HWJ_CombatSystem combat,
        HWJ_CombatExecutionSystem combatExecution,
        HWJ_CharacterMotionSystem motion,
        Rigidbody2D body)
    {
        SerializedObject serializedAction = new SerializedObject(skillAction);
        SetObject(serializedAction, "dataResolver", resolver);
        SetObject(serializedAction, "runtimeStatus", status);
        SetObject(serializedAction, "combatSystem", combat);
        SetObject(serializedAction, "combatExecutionSystem", combatExecution);
        SetObject(serializedAction, "motionSystem", motion);
        SetObject(serializedAction, "database", RequireAsset<HWJ_GameplayDatabaseSO>(DatabasePath));
        SetObject(serializedAction, "body", body);
        serializedAction.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCombatExecution(HWJ_CombatExecutionSystem combatExecution)
    {
        SerializedObject serializedCombat = new SerializedObject(combatExecution);
        SetBool(serializedCombat, "useObjectTypeDefaultTargetFilter", false);
        SetBool(serializedCombat, "canDamagePlayer", true);
        SetBool(serializedCombat, "canDamageEnemy", false);
        SetBool(serializedCombat, "canDamageBoss", false);
        SetBool(serializedCombat, "canDamageNpc", false);
        SetFloat(serializedCombat, "fallbackAttackRange", 1.4f);
        serializedCombat.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMotion(
        HWJ_CharacterMotionSystem motion,
        Animator animator,
        SpriteRenderer renderer,
        Rigidbody2D body,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status)
    {
        SerializedObject serializedMotion = new SerializedObject(motion);
        SetObject(serializedMotion, "animator", animator);
        SetObject(serializedMotion, "spriteRenderer", renderer);
        SetObject(serializedMotion, "body", body);
        SetObject(serializedMotion, "dataResolver", resolver);
        SetObject(serializedMotion, "runtimeStatus", status);
        SerializedProperty profiles = serializedMotion.FindProperty("weaponMotionProfiles");

        if (profiles != null)
        {
            profiles.arraySize = 0;
        }

        serializedMotion.ApplyModifiedPropertiesWithoutUndo();
        motion.RefreshFacingBaseline();
    }

    private static void ConfigureHitEffect(
        HWJ_HitEffectSystem hitEffect,
        HWJ_RuntimeStatusSystem status,
        HWJ_RootObjectDataResolver resolver)
    {
        SerializedObject serializedEffect = new SerializedObject(hitEffect);
        SetObject(serializedEffect, "runtimeStatus", status);
        SetObject(serializedEffect, "dataResolver", resolver);
        serializedEffect.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void UpdateGameplayDatabase(
        HWJ_RootObjectDataSO[] newRootObjects,
        HWJ_TitleScreenDataSO titleScreenData,
        List<string> report)
    {
        HWJ_GameplayDatabaseSO database = RequireAsset<HWJ_GameplayDatabaseSO>(DatabasePath);
        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty roots = RequireProperty(serializedDatabase, "rootObjects");

        for (int i = roots.arraySize - 1; i >= 0; i--)
        {
            UnityEngine.Object value = roots.GetArrayElementAtIndex(i).objectReferenceValue;
            string path = value != null ? AssetDatabase.GetAssetPath(value) : string.Empty;
            bool isReplacement = Array.IndexOf(newRootObjects, value) >= 0;

            if (isReplacement || value == null)
            {
                roots.DeleteArrayElementAtIndex(i);
            }
        }

        for (int i = 0; i < newRootObjects.Length; i++)
        {
            int newIndex = roots.arraySize;
            roots.InsertArrayElementAtIndex(newIndex);
            roots.GetArrayElementAtIndex(newIndex).objectReferenceValue = newRootObjects[i];
        }

        SerializedProperty titleScreens = RequireProperty(serializedDatabase, "titleScreens");

        for (int i = titleScreens.arraySize - 1; i >= 0; i--)
        {
            UnityEngine.Object value = titleScreens.GetArrayElementAtIndex(i).objectReferenceValue;

            if (value == null || value == titleScreenData)
            {
                titleScreens.DeleteArrayElementAtIndex(i);
            }
        }

        int titleIndex = titleScreens.arraySize;
        titleScreens.InsertArrayElementAtIndex(titleIndex);
        titleScreens.GetArrayElementAtIndex(titleIndex).objectReferenceValue = titleScreenData;

        serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        report.Add("GameplayDatabase에 슬라임/쥐 등록 완료");
    }

    private static HWJ_TitleScreenDataSO RepairTitleScreenData(List<string> report)
    {
        HWJ_TitleScreenDataSO titleData =
            AssetDatabase.LoadAssetAtPath<HWJ_TitleScreenDataSO>(TitleDataPath);

        if (titleData == null)
        {
            UnityEngine.Object brokenAsset = AssetDatabase.LoadMainAssetAtPath(TitleDataPath);

            if (brokenAsset != null && !AssetDatabase.DeleteAsset(TitleDataPath))
            {
                throw new InvalidOperationException("잘못된 TitleScreenData 에셋 삭제 실패: " + TitleDataPath);
            }

            titleData = ScriptableObject.CreateInstance<HWJ_TitleScreenDataSO>();
            AssetDatabase.CreateAsset(titleData, TitleDataPath);
            report.Add("파일명/클래스 불일치 TitleScreenData를 정상 SO로 재생성: " + TitleDataPath);
        }

        SerializedObject serializedTitle = new SerializedObject(titleData);
        SetString(serializedTitle, "titleScreenId", "default_title");
        SetObject(serializedTitle, "windowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefabPath));
        SetString(serializedTitle, "firstGameplaySceneName", string.Empty);
        SetBool(serializedTitle, "pauseGameWhileShown", true);
        serializedTitle.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(titleData);
        return titleData;
    }

    private static void UpdateSpawnTable(
        HWJ_RootObjectDataSO[] newRootObjects,
        GameObject[] newRuntimePrefabs,
        List<string> report)
    {
        HWJ_SpawnTableDataSO spawnTable = RequireAsset<HWJ_SpawnTableDataSO>(SpawnTablePath);
        SerializedObject serializedTable = new SerializedObject(spawnTable);
        SerializedProperty entries = RequireProperty(serializedTable, "entries");

        for (int i = entries.arraySize - 1; i >= 0; i--)
        {
            string spawnId = entries.GetArrayElementAtIndex(i)
                .FindPropertyRelative("spawnId")
                .stringValue;

            if (spawnId == "Enemy_General_Slime" || spawnId == "Enemy_General_Rat")
            {
                entries.DeleteArrayElementAtIndex(i);
            }
        }

        for (int i = 0; i < GeneralMonsterSpecs.Length; i++)
        {
            HWJ_GeneralMonsterSpec spec = GeneralMonsterSpecs[i];
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            SetRelativeString(entry, "spawnId", "Enemy_General_" + spec.CodeName);
            SetRelativeString(entry, "spawnPointId", "Enemy_General_" + spec.CodeName);
            SetRelativeEnum(entry, "spawnPointType", (int)HWJ_SpawnPointType.Enemy);
            SetRelativeObject(entry, "rootObjectData", newRootObjects[i]);
            SetRelativeObject(entry, "prefabOverride", newRuntimePrefabs[i]);
            SetRelativeInt(entry, "spawnCount", 1);
            SetRelativeFloat(entry, "spawnDelaySeconds", 0f);
            SetRelativeBool(entry, "useSequentialSpawnWhenMultiple", true);
            SetRelativeBool(entry, "waitUntilCurrentSpawnedMonstersDefeated", true);
            SetRelativeFloat(entry, "nextSpawnMaxWaitSeconds", 5f);
            SetRelativeBool(entry, "skipSpawnWhenRewardClaimed", true);
            SetRelativeVector2(entry, "spawnOffset", Vector2.zero);
            SetRelativeBool(entry, "randomizePoint", false);
            SetRelativeBool(entry, "spawnOnStart", false);
        }

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawnTable);
        report.Add("기본 SpawnTable에 수동 활성화용 슬라임/쥐 항목 등록 완료");
    }

    private static void UpdateAllSystemsDropIn(
        GameObject[] newRuntimePrefabs,
        List<string> report)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(AllSystemsPrefabPath) == null)
        {
            report.Add("경고: AllSystems 드롭인 프리팹이 없어 갱신을 생략했습니다.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(AllSystemsPrefabPath);

        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);

                if (child.name.IndexOf("FirstPossessionCorpse", StringComparison.OrdinalIgnoreCase) >= 0
                    || child.name == "31_Enemy_General_Slime"
                    || child.name == "32_Enemy_General_Rat")
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            for (int i = 0; i < newRuntimePrefabs.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    newRuntimePrefabs[i],
                    root.transform);
                instance.name = i == 0
                    ? "31_Enemy_General_Slime"
                    : "32_Enemy_General_Rat";
                instance.transform.localPosition = new Vector3(18f + i * 3f, 0.75f, 0f);
                instance.transform.localRotation = Quaternion.identity;
            }

            PrefabUtility.SaveAsPrefabAsset(root, AllSystemsPrefabPath);
            report.Add("AllSystems 드롭인에서 이전 시체 테스트 제거 및 슬라임/쥐 배치 완료");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ValidateResult(
        HWJ_RootObjectDataSO[] newRootObjects,
        GameObject[] newRuntimePrefabs,
        List<string> report)
    {
        List<string> errors = new List<string>();

        for (int i = 0; i < GeneralMonsterSpecs.Length; i++)
        {
            HWJ_GeneralMonsterSpec spec = GeneralMonsterSpecs[i];
            HWJ_RootObjectDataSO rootData = newRootObjects[i];
            GameObject runtimePrefab = newRuntimePrefabs[i];

            if (rootData == null || rootData.Identity == null || rootData.Identity.objectId != spec.StableId)
            {
                errors.Add(spec.CodeName + " RootObjectData의 안정 ID가 올바르지 않습니다.");
            }

            if (rootData == null
                || !rootData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyType)
                || enemyType.PossessionBody == null
                || !enemyType.PossessionBody.canBePossessed
                || enemyType.PossessionBody.requiresDefeatedState
                || enemyType.Role.leavesCorpseOnDeath)
            {
                errors.Add(spec.CodeName + " 생체 빙의/HP 0 제거 데이터가 올바르지 않습니다.");
            }

            ValidateRuntimePrefab(runtimePrefab, spec.CodeName, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "일반 몬스터 결과 검증 실패:\n- " + string.Join("\n- ", errors));
        }

        report.Add("검증 통과: 슬라임/쥐 필수 컴포넌트와 생체 빙의 설정 확인");
    }

    private static void ValidateRuntimePrefab(
        GameObject prefab,
        string codeName,
        List<string> errors)
    {
        Type[] requiredTypes =
        {
            typeof(HWJ_RootObjectDataResolver),
            typeof(HWJ_RuntimeStatusSystem),
            typeof(HWJ_CombatExecutionSystem),
            typeof(HWJ_PossessionBodyState),
            typeof(HWJ_LivePossessionMentalState),
            typeof(HWJ_EnemyNavigationSystem),
            typeof(HWJ_EnemyAttackSystem),
            typeof(HWJ_EnemyPerceptionSystem),
            typeof(HWJ_MonsterAISystem),
            typeof(HWJ_EnemyDeathLifecycleSystem)
        };

        if (prefab == null)
        {
            errors.Add(codeName + " 실행 프리팹이 없습니다.");
            return;
        }

        for (int i = 0; i < requiredTypes.Length; i++)
        {
            if (prefab.GetComponent(requiredTypes[i]) == null)
            {
                errors.Add(codeName + " 실행 프리팹 필수 컴포넌트 누락: " + requiredTypes[i].Name);
            }
        }

        Component[] components = prefab.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                errors.Add(codeName + " 실행 프리팹에 Missing Script가 있습니다.");
                break;
            }
        }
    }

    private static void CreateWorldHealthBar(
        Transform parent,
        HWJ_RuntimeStatusSystem status,
        HWJ_RootObjectDataResolver resolver,
        float localY)
    {
        Sprite marker = RequireAsset<Sprite>(MarkerSpritePath);
        GameObject bar = CreateChild(parent, "HWJ_WorldHealthBar", new Vector3(0f, localY, 0f));
        GameObject background = CreateChild(bar.transform, "Background", Vector3.zero);
        background.transform.localScale = new Vector3(1.25f, 0.12f, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = marker;
        backgroundRenderer.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        backgroundRenderer.sortingOrder = 190;

        GameObject fill = CreateChild(bar.transform, "Fill", Vector3.zero);
        fill.transform.localScale = new Vector3(1.2f, 0.08f, 1f);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = marker;
        fillRenderer.color = new Color(0.25f, 0.95f, 0.45f, 1f);
        fillRenderer.sortingOrder = 191;

        HWJ_HealthBarSystem healthBar = bar.AddComponent<HWJ_HealthBarSystem>();
        SerializedObject serializedBar = new SerializedObject(healthBar);
        SetObject(serializedBar, "runtimeStatus", status);
        SetObject(serializedBar, "dataResolver", resolver);
        SetObject(serializedBar, "fillRoot", fill.transform);
        SetObject(serializedBar, "fillRenderer", fillRenderer);
        serializedBar.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateMarkerPart(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        Vector3 localPosition,
        Vector3 localScale,
        int sortingOrder)
    {
        GameObject part = CreateChild(parent, name, localPosition);
        part.transform.localScale = localScale;
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return part;
    }

    private static void CreateSocket(Transform parent, string name, Vector3 localPosition)
    {
        CreateChild(parent, name, localPosition);
    }

    private static GameObject CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        return child;
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath(GeneralTypeRoot);
        EnsureFolderPath(EnemyRootObjectRoot);
        EnsureFolderPath(ModelRoot);
        EnsureFolderPath(RuntimeEnemyRoot);
        EnsureFolderPath(AssetRoot + "/Docs/Generated");
    }

    private static void EnsureFolderPath(string path)
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

    private static void WriteReport(
        string result,
        List<string> report,
        Exception exception)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 일반 몬스터 생성 보고서");
        builder.AppendLine();
        builder.AppendLine("- 결과: " + result);
        builder.AppendLine("- 생성 시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("- 대상: 일반 슬라임, 일반 쥐, 기존 5종 무기 몬스터 공통 AI 이관");
        builder.AppendLine();
        builder.AppendLine("## 처리 내역");

        for (int i = 0; i < report.Count; i++)
        {
            builder.AppendLine("- " + report[i]);
        }

        if (exception != null)
        {
            builder.AppendLine();
            builder.AppendLine("## 오류");
            builder.AppendLine("```text");
            builder.AppendLine(exception.ToString());
            builder.AppendLine("```");
        }

        string absolutePath = Path.GetFullPath(ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, builder.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
    }

    private static string GetTypeDataPath(HWJ_GeneralMonsterSpec spec)
    {
        return GeneralTypeRoot + "/HWJ_Enemy_General_" + spec.CodeName + "_TypeData.asset";
    }

    private static string GetRootObjectDataPath(HWJ_GeneralMonsterSpec spec)
    {
        return EnemyRootObjectRoot + "/HWJ_Enemy_General_" + spec.CodeName + "_RootObjectData.asset";
    }

    private static string GetModelPrefabPath(HWJ_GeneralMonsterSpec spec)
    {
        return ModelRoot + "/HWJ_Model_Enemy_General_" + spec.CodeName + ".prefab";
    }

    private static string GetRuntimePrefabPath(HWJ_GeneralMonsterSpec spec)
    {
        return RuntimeEnemyRoot + "/HWJ_Runtime_Enemy_General_" + spec.CodeName + ".prefab";
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            throw new InvalidOperationException("필수 자산 누락: " + path);
        }

        return asset;
    }

    private static T RequireComponent<T>(GameObject root, string path) where T : Component
    {
        T component = root.GetComponent<T>();

        if (component == null)
        {
            throw new InvalidOperationException(path + " 필수 컴포넌트 누락: " + typeof(T).Name);
        }

        return component;
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static SerializedProperty RequireProperty(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                serializedObject.targetObject.name + " 직렬화 필드 누락: " + propertyName);
        }

        return property;
    }

    private static SerializedProperty RequireRelative(SerializedProperty parent, string propertyName)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(parent.propertyPath + " 하위 필드 누락: " + propertyName);
        }

        return property;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        RequireProperty(serializedObject, propertyName).objectReferenceValue = value;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        RequireProperty(serializedObject, propertyName).stringValue = value;
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        RequireProperty(serializedObject, propertyName).floatValue = value;
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        RequireProperty(serializedObject, propertyName).boolValue = value;
    }

    private static void SetEnum(
        SerializedObject serializedObject,
        string propertyName,
        int value,
        bool applyImmediately = false)
    {
        RequireProperty(serializedObject, propertyName).enumValueIndex = value;

        if (applyImmediately)
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetLayerMask(
        SerializedObject serializedObject,
        string propertyName,
        LayerMask value)
    {
        RequireProperty(serializedObject, propertyName).intValue = value.value;
    }

    private static void SetRelativeObject(SerializedProperty parent, string propertyName, UnityEngine.Object value)
    {
        RequireRelative(parent, propertyName).objectReferenceValue = value;
    }

    private static void SetRelativeString(SerializedProperty parent, string propertyName, string value)
    {
        RequireRelative(parent, propertyName).stringValue = value;
    }

    private static void SetRelativeFloat(SerializedProperty parent, string propertyName, float value)
    {
        RequireRelative(parent, propertyName).floatValue = value;
    }

    private static void SetRelativeInt(SerializedProperty parent, string propertyName, int value)
    {
        RequireRelative(parent, propertyName).intValue = value;
    }

    private static void SetRelativeBool(SerializedProperty parent, string propertyName, bool value)
    {
        RequireRelative(parent, propertyName).boolValue = value;
    }

    private static void SetRelativeEnum(SerializedProperty parent, string propertyName, int value)
    {
        RequireRelative(parent, propertyName).enumValueIndex = value;
    }

    private static void SetRelativeVector2(SerializedProperty parent, string propertyName, Vector2 value)
    {
        RequireRelative(parent, propertyName).vector2Value = value;
    }

    private static void SetLayerIfExists(GameObject target, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
        {
            target.layer = layer;
        }
        else
        {
            Debug.LogWarning("[HWJ] 레이어를 찾지 못했습니다: " + layerName, target);
        }
    }

    private readonly struct HWJ_GeneralMonsterSpec
    {
        public HWJ_GeneralMonsterSpec(
            string codeName,
            string stableId,
            string displayName,
            float maximumHp,
            float moveSpeed,
            float attackPower,
            float defense,
            float attackRange,
            float possessionMental,
            HWJ_EnemyBasicAttackMode attackMode,
            float fallbackHitDelaySeconds,
            float basicAttackIntervalSeconds,
            Color placeholderColor)
        {
            CodeName = codeName;
            StableId = stableId;
            DisplayName = displayName;
            MaximumHp = maximumHp;
            MoveSpeed = moveSpeed;
            AttackPower = attackPower;
            Defense = defense;
            AttackRange = attackRange;
            PossessionMental = possessionMental;
            AttackMode = attackMode;
            FallbackHitDelaySeconds = fallbackHitDelaySeconds;
            BasicAttackIntervalSeconds = basicAttackIntervalSeconds;
            PlaceholderColor = placeholderColor;
        }

        public string CodeName { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public float MaximumHp { get; }
        public float MoveSpeed { get; }
        public float AttackPower { get; }
        public float Defense { get; }
        public float AttackRange { get; }
        public float PossessionMental { get; }
        public HWJ_EnemyBasicAttackMode AttackMode { get; }
        public float FallbackHitDelaySeconds { get; }
        public float BasicAttackIntervalSeconds { get; }
        public Color PlaceholderColor { get; }
    }
}
