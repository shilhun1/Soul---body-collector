using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class HWJ_CoreSystemsPlayModeTests
{
    [UnityTest]
    public IEnumerator CombatSystem_AppliesDamageWithoutMutatingSourceData()
    {
        GameObject source = CreateCombatObject(
            "Source",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        GameObject target = CreateCombatObject(
            "Target",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        HWJ_CombatSystem sourceCombat = source.GetComponent<HWJ_CombatSystem>();
        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.IsTrue(sourceCombat.TryDealDamageTo(targetResolver, out float finalDamage));
        Assert.AreEqual(10f, finalDamage, 0.001f);
        Assert.AreEqual(10f, targetStatus.CurrentHp, 0.001f);
        Assert.AreEqual(20f, targetResolver.Status.maxHp, 0.001f);

        Object.Destroy(source);
        Object.Destroy(target);
    }

    [UnityTest]
    public IEnumerator CombatSystem_AppliesDefenseForPlayerAndMonster()
    {
        GameObject player = CreateCombatObject(
            "PlayerWithDefense",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true),
            4f);
        GameObject monster = CreateCombatObject(
            "MonsterWithDefense",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            5f,
            5f,
            CreateEnemyTypeData(true),
            3f);

        yield return null;

        HWJ_CombatSystem playerCombat = player.GetComponent<HWJ_CombatSystem>();
        HWJ_CombatSystem monsterCombat = monster.GetComponent<HWJ_CombatSystem>();
        HWJ_RuntimeStatusSystem playerStatus = player.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RuntimeStatusSystem monsterStatus = monster.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RootObjectDataResolver playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RootObjectDataResolver monsterResolver = monster.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.AreEqual(4f, playerCombat.Defense, 0.001f);
        Assert.AreEqual(3f, monsterCombat.Defense, 0.001f);
        Assert.IsTrue(playerCombat.TryDealDamageTo(monsterResolver, out float playerDamage));
        Assert.AreEqual(7f, playerDamage, 0.001f);
        Assert.AreEqual(13f, monsterStatus.CurrentHp, 0.001f);

        Assert.IsTrue(monsterCombat.TryDealDamageTo(playerResolver, out float monsterDamage));
        Assert.AreEqual(6f, monsterDamage, 0.001f);
        Assert.AreEqual(14f, playerStatus.CurrentHp, 0.001f);

        Object.Destroy(player);
        Object.Destroy(monster);
    }

    [UnityTest]
    public IEnumerator RewardUtility_GrantsKillRewardOnceAndRaisesRewardEvents()
    {
        GameObject player = CreateCombatObject(
            "RewardPlayer",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_LevelUpSystem level = player.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO levelData = CreateLevelUpData("test.level.reward", 3, 1, new[] { 10, 20 });
        level.SetLevelUpData(levelData);
        GameObject enemy = CreateCombatObject(
            "RewardEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();
        enemyResolver.Reward.experienceReward = 15;
        enemyResolver.Reward.skillPointReward = 2;
        int enemyDefeatedCount = 0;
        int rewardGrantedCount = 0;
        int experienceChangedCount = 0;
        int levelChangedCount = 0;
        HWJ_RewardGrantedEvent lastRewardEvent = default(HWJ_RewardGrantedEvent);

        void OnEnemyDefeated(HWJ_EnemyDefeatedEvent defeatedEvent)
        {
            enemyDefeatedCount++;
        }

        void OnRewardGranted(HWJ_RewardGrantedEvent rewardEvent)
        {
            rewardGrantedCount++;
            lastRewardEvent = rewardEvent;
        }

        void OnExperienceChanged(HWJ_ExperienceChangedEvent experienceEvent)
        {
            experienceChangedCount++;
        }

        void OnLevelChanged(HWJ_PlayerLevelChangedEvent levelEvent)
        {
            levelChangedCount++;
        }

        HWJ_GameplayEvents.EnemyDefeated += OnEnemyDefeated;
        HWJ_GameplayEvents.RewardGranted += OnRewardGranted;
        HWJ_GameplayEvents.ExperienceChanged += OnExperienceChanged;
        HWJ_GameplayEvents.PlayerLevelChanged += OnLevelChanged;

        yield return null;

        HWJ_CombatSystem playerCombat = player.GetComponent<HWJ_CombatSystem>();
        HWJ_RootObjectDataResolver playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.IsTrue(playerCombat.TryDealDamageTo(enemyResolver, out float finalDamage));
        Assert.AreEqual(10f, finalDamage, 0.001f);
        Assert.AreEqual(2, level.CurrentLevel);
        Assert.AreEqual(5, level.CurrentExperience);
        Assert.AreEqual(3, level.SkillPoint);
        Assert.AreEqual(1, enemyDefeatedCount);
        Assert.AreEqual(1, rewardGrantedCount);
        Assert.AreEqual(1, experienceChangedCount);
        Assert.AreEqual(1, levelChangedCount);
        Assert.IsTrue(lastRewardEvent.RewardResult.Succeeded);
        Assert.AreEqual(15, lastRewardEvent.RewardResult.ExperienceGranted);
        Assert.AreEqual(2, lastRewardEvent.RewardResult.SkillPointGranted);

        HWJ_RewardGrantResult duplicateResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            enemyResolver,
            playerResolver,
            enemy.transform.position);

        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_RewardGrantFailureCode.AlreadyClaimed, duplicateResult.FailureCode);
        Assert.AreEqual(2, level.CurrentLevel);
        Assert.AreEqual(5, level.CurrentExperience);
        Assert.AreEqual(3, level.SkillPoint);
        Assert.AreEqual(1, enemyDefeatedCount);
        Assert.AreEqual(1, rewardGrantedCount);

        HWJ_GameplayEvents.EnemyDefeated -= OnEnemyDefeated;
        HWJ_GameplayEvents.RewardGranted -= OnRewardGranted;
        HWJ_GameplayEvents.ExperienceChanged -= OnExperienceChanged;
        HWJ_GameplayEvents.PlayerLevelChanged -= OnLevelChanged;
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(levelData);
    }

    [UnityTest]
    public IEnumerator RewardUtility_SavedProgressionBlocksDuplicateRewardClaim()
    {
        GameObject saveObject = new GameObject("RewardSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        GameObject player = CreateCombatObject(
            "SavedRewardPlayer",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_LevelUpSystem level = player.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO levelData = CreateLevelUpData("test.level.saved.reward", 3, 1, new[] { 10, 20 });
        level.SetLevelUpData(levelData);
        GameObject firstEnemy = CreateCombatObject(
            "SavedRewardEnemyA",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        GameObject secondEnemy = CreateCombatObject(
            "SavedRewardEnemyB",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataResolver playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RootObjectDataResolver firstEnemyResolver = firstEnemy.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RootObjectDataResolver secondEnemyResolver = secondEnemy.GetComponent<HWJ_RootObjectDataResolver>();
        firstEnemyResolver.Reward.experienceReward = 5;
        secondEnemyResolver.Reward.experienceReward = 5;
        firstEnemy.AddComponent<HWJ_RuntimeSaveIdentity>()
            .SetManualIdentity("enemy.reward.001", "enemy.saved.reward");
        secondEnemy.AddComponent<HWJ_RuntimeSaveIdentity>()
            .SetManualIdentity("enemy.reward.001", "enemy.saved.reward");

        yield return null;

        HWJ_RewardGrantResult firstResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            firstEnemyResolver,
            playerResolver,
            firstEnemy.transform.position);

        Assert.IsTrue(firstResult.Succeeded);
        Assert.AreEqual("enemy.saved.reward:enemy.reward.001", firstResult.RewardClaimId);
        Assert.IsTrue(saveService.IsRewardClaimed(firstResult.RewardClaimId));
        Assert.AreEqual(5, level.CurrentExperience);

        HWJ_RewardGrantResult duplicateResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            secondEnemyResolver,
            playerResolver,
            secondEnemy.transform.position);

        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_RewardGrantFailureCode.AlreadyClaimed, duplicateResult.FailureCode);
        Assert.AreEqual(firstResult.RewardClaimId, duplicateResult.RewardClaimId);
        Assert.AreEqual(5, level.CurrentExperience);

        saveObject.SetActive(false);
        Object.Destroy(saveObject);
        Object.Destroy(player);
        Object.Destroy(firstEnemy);
        Object.Destroy(secondEnemy);
        Object.Destroy(levelData);
    }

    [UnityTest]
    public IEnumerator SpawnerSystem_AssignsStableSaveIdentityAndSkipsClaimedSpawnIndex()
    {
        GameObject saveObject = new GameObject("SpawnerSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        GameObject prefab = new GameObject("SaveIdentityEnemyPrefab");
        prefab.AddComponent<HWJ_RootObjectDataResolver>();
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        HWJ_RootObjectDataSO rootObjectData = CreateRootObjectData(
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            enemyData,
            0f,
            "enemy.spawn.saved");
        GameObject pointObject = new GameObject("SaveIdentitySpawnPoint");
        HWJ_SpawnPoint spawnPoint = pointObject.AddComponent<HWJ_SpawnPoint>();
        SetPrivateField(spawnPoint, "pointId", "point.saved.enemy");
        SetPrivateField(spawnPoint, "spawnPointType", HWJ_SpawnPointType.Enemy);
        HWJ_SpawnEntryData spawnEntry = new HWJ_SpawnEntryData
        {
            spawnId = "spawn.saved.enemy",
            spawnPointId = "point.saved.enemy",
            spawnPointType = HWJ_SpawnPointType.Enemy,
            rootObjectData = rootObjectData,
            prefabOverride = prefab,
            spawnCount = 2,
            skipSpawnWhenRewardClaimed = true,
            spawnOnStart = false
        };
        HWJ_SpawnTableDataSO spawnTable = ScriptableObject.CreateInstance<HWJ_SpawnTableDataSO>();
        SetPrivateField(spawnTable, "tableId", "table.saved.spawn");
        SetPrivateField(spawnTable, "entries", new[] { spawnEntry });
        GameObject spawnerObject = new GameObject("SaveIdentitySpawner");
        HWJ_SpawnerSystem spawner = spawnerObject.AddComponent<HWJ_SpawnerSystem>();
        SetPrivateField(spawner, "spawnTable", spawnTable);
        SetPrivateField(spawner, "spawnPoints", new[] { spawnPoint });
        SetPrivateField(spawner, "spawnOnStart", false);
        SetPrivateField(spawner, "autoCollectSpawnPoints", false);
        string claimedStableId = HWJ_RuntimeSaveIdentity.CreateSpawnStableId(
            "spawn.saved.enemy",
            "point.saved.enemy",
            "enemy.spawn.saved",
            0);
        string claimedRewardId = "enemy.spawn.saved:" + claimedStableId;

        Assert.IsTrue(saveService.TryClaimReward(claimedRewardId, false, null));

        spawner.SpawnById("spawn.saved.enemy");
        yield return null;

        HWJ_RuntimeSaveIdentity[] saveIdentities = Object.FindObjectsByType<HWJ_RuntimeSaveIdentity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        HWJ_RuntimeSaveIdentity spawnedIdentity = null;

        for (int i = 0; i < saveIdentities.Length; i++)
        {
            if (saveIdentities[i] != null && saveIdentities[i].RootObjectId == "enemy.spawn.saved")
            {
                spawnedIdentity = saveIdentities[i];
                break;
            }
        }

        Assert.NotNull(spawnedIdentity);
        Assert.AreEqual(
            HWJ_RuntimeSaveIdentity.CreateSpawnStableId(
                "spawn.saved.enemy",
                "point.saved.enemy",
                "enemy.spawn.saved",
                1),
            spawnedIdentity.StableInstanceId);
        Assert.AreEqual("enemy.spawn.saved", spawnedIdentity.RootObjectId);

        Object.Destroy(spawnedIdentity.gameObject);
        saveObject.SetActive(false);
        Object.Destroy(saveObject);
        Object.Destroy(prefab);
        Object.Destroy(pointObject);
        Object.Destroy(spawnerObject);
        Object.Destroy(spawnTable);
        Object.Destroy(rootObjectData);
        Object.Destroy(enemyData);
    }

    [UnityTest]
    public IEnumerator SaveService_LoadAndApplyRestoresRewardClaimsForRewardUtility()
    {
        string saveSlotId = "playmode_reward_reload_" + System.Guid.NewGuid().ToString("N");
        string rewardClaimId = "enemy.saved.reload:enemy.reward.reload";
        GameObject sourcePlayer = CreatePlayerObject("SaveSourcePlayer", true);
        HWJ_LevelUpSystem sourceLevel = sourcePlayer.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO sourceLevelData = CreateLevelUpData("test.level.save.source", 3, 1, new[] { 10, 20 });
        sourceLevel.SetLevelUpData(sourceLevelData);
        GameObject sourceSaveObject = new GameObject("SourceSaveService");
        HWJ_SaveService sourceSaveService = sourceSaveObject.AddComponent<HWJ_SaveService>();
        sourceSaveService.SetRuntimeSources(
            sourcePlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            null,
            null);
        string saveFilePath = sourceSaveService.GetSaveFilePath(saveSlotId);

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }

        yield return null;

        Assert.IsTrue(sourceSaveService.TryClaimReward(rewardClaimId, false, null));

        // The claim must survive an actual save file round trip, not only the current service instance.
        HWJ_SaveOperationResult saveResult = sourceSaveService.SaveCurrentGame(saveSlotId);

        Assert.IsTrue(saveResult.Succeeded, saveResult.Message);
        Assert.IsTrue(File.Exists(saveFilePath));

        sourceSaveObject.SetActive(false);
        Object.Destroy(sourceSaveObject);
        Object.Destroy(sourcePlayer);
        Object.Destroy(sourceLevelData);

        yield return null;

        GameObject loadedPlayer = CreatePlayerObject("LoadedSavePlayer", true);
        HWJ_LevelUpSystem loadedLevel = loadedPlayer.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO loadedLevelData = CreateLevelUpData("test.level.save.loaded", 3, 1, new[] { 10, 20 });
        loadedLevel.SetLevelUpData(loadedLevelData);
        GameObject loadedSaveObject = new GameObject("LoadedSaveService");
        HWJ_SaveService loadedSaveService = loadedSaveObject.AddComponent<HWJ_SaveService>();
        loadedSaveService.SetRuntimeSources(
            loadedPlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            null,
            null);

        yield return null;

        HWJ_SaveOperationResult loadResult = loadedSaveService.LoadAndApply(saveSlotId);

        Assert.IsTrue(loadResult.Succeeded, loadResult.Message);
        Assert.IsTrue(loadedSaveService.IsRewardClaimed(rewardClaimId));

        GameObject duplicateEnemy = CreateCombatObject(
            "LoadedDuplicateRewardEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataResolver duplicateEnemyResolver = duplicateEnemy.GetComponent<HWJ_RootObjectDataResolver>();
        duplicateEnemyResolver.Reward.experienceReward = 5;
        duplicateEnemy.AddComponent<HWJ_RuntimeSaveIdentity>()
            .SetManualIdentity("enemy.reward.reload", "enemy.saved.reload");

        // RewardUtility is the gameplay-facing path that other combat systems call after a kill.
        HWJ_RewardGrantResult duplicateResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            duplicateEnemyResolver,
            loadedPlayer.GetComponent<HWJ_RootObjectDataResolver>(),
            duplicateEnemy.transform.position);

        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_RewardGrantFailureCode.AlreadyClaimed, duplicateResult.FailureCode);
        Assert.AreEqual(rewardClaimId, duplicateResult.RewardClaimId);
        Assert.AreEqual(0, loadedLevel.CurrentExperience);

        loadedSaveObject.SetActive(false);
        Object.Destroy(loadedSaveObject);
        Object.Destroy(loadedPlayer);
        Object.Destroy(duplicateEnemy);
        Object.Destroy(loadedLevelData);

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    [UnityTest]
    public IEnumerator SaveService_RoundTripRestoresGrowthStagePossessionAndDecay()
    {
        string saveSlotId = "playmode_full_roundtrip_" + System.Guid.NewGuid().ToString("N");
        string possessedBodyId = "body.save.roundtrip";
        GameObject sourcePlayer = CreatePlayerObject("SaveRoundTripSourcePlayer", true, 12f, 1f);
        HWJ_LevelUpSystem sourceLevel = sourcePlayer.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO sourceLevelData = CreateLevelUpData("test.level.save.roundtrip.source", 5, 1, new[] { 10, 20, 30, 40 });
        sourceLevel.SetLevelUpData(sourceLevelData);
        HWJ_SkillUnlockSystem sourceSkillUnlock = sourcePlayer.AddComponent<HWJ_SkillUnlockSystem>();
        sourceLevel.RestoreProgress(3, 7, 2);
        Assert.IsTrue(sourceSkillUnlock.ForceUnlockSkill("skill.save.roundtrip").Succeeded);

        GameObject sourceStageObject = new GameObject("SaveRoundTripSourceStage");
        HWJ_StageProgressionSystem sourceStage = sourceStageObject.AddComponent<HWJ_StageProgressionSystem>();
        sourceStage.SetStageIds("stage.save.roundtrip", "region.save.next");
        Assert.IsTrue(sourceStage.TryEnterExploring().Succeeded);
        Assert.IsTrue(sourceStage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(sourceStage.TryUnlockBoss().Succeeded);

        GameObject possessedBody = CreateCombatObject(
            "SaveRoundTripPossessedBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            9f,
            2f,
            3f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataSO possessedBodyRootData = possessedBody.GetComponent<HWJ_RootObjectDataResolver>().RootObjectData;
        possessedBodyRootData.name = "HWJ_Test_SaveRoundTripBody";
        possessedBodyRootData.Identity.objectId = possessedBodyId;
        HWJ_GameplayDatabaseSO runtimeDatabase = ScriptableObject.CreateInstance<HWJ_GameplayDatabaseSO>();
        SetPrivateField(runtimeDatabase, "rootObjects", new[] { possessedBodyRootData });

        possessedBody.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem sourcePossession = sourcePlayer.GetComponent<HWJ_PossessionSystem>();
        Assert.IsTrue(sourcePossession.TryPossess(possessedBody.GetComponent<HWJ_RootObjectDataResolver>()));
        Assert.IsTrue(sourcePlayer.GetComponent<HWJ_PossessedBodySystem>().SetCurrentHp(6f));
        sourcePlayer.GetComponent<HWJ_BodyDecaySystem>().RestoreDecaySnapshot(4f);

        GameObject sourceSaveObject = new GameObject("SaveRoundTripSourceSaveService");
        HWJ_SaveService sourceSaveService = sourceSaveObject.AddComponent<HWJ_SaveService>();
        sourceSaveService.SetRuntimeSources(
            sourcePlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            sourceStage,
            runtimeDatabase);
        string saveFilePath = sourceSaveService.GetSaveFilePath(saveSlotId);

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }

        // This verifies the real file path, not only in-memory DTO creation.
        HWJ_SaveOperationResult saveResult = sourceSaveService.SaveCurrentGame(saveSlotId);

        Assert.IsTrue(saveResult.Succeeded, saveResult.Message);
        Assert.IsTrue(File.Exists(saveFilePath));
        Assert.AreEqual(possessedBodyId, saveResult.SaveData.player.body.possessedBodyDefinitionDataId);
        Assert.AreEqual(4f, saveResult.SaveData.player.body.currentDecayValue, 0.001f);
        Assert.AreEqual(3, saveResult.SaveData.player.growth.currentLevel);

        sourceSaveObject.SetActive(false);
        Object.Destroy(sourceSaveObject);
        Object.Destroy(sourcePlayer);
        Object.Destroy(sourceStageObject);
        Object.Destroy(possessedBody);

        yield return null;

        GameObject loadedPlayer = CreatePlayerObject("SaveRoundTripLoadedPlayer", true, 12f, 1f);
        HWJ_LevelUpSystem loadedLevel = loadedPlayer.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO loadedLevelData = CreateLevelUpData("test.level.save.roundtrip.loaded", 5, 1, new[] { 10, 20, 30, 40 });
        loadedLevel.SetLevelUpData(loadedLevelData);
        HWJ_SkillUnlockSystem loadedSkillUnlock = loadedPlayer.AddComponent<HWJ_SkillUnlockSystem>();
        GameObject loadedStageObject = new GameObject("SaveRoundTripLoadedStage");
        HWJ_StageProgressionSystem loadedStage = loadedStageObject.AddComponent<HWJ_StageProgressionSystem>();
        GameObject loadedSaveObject = new GameObject("SaveRoundTripLoadedSaveService");
        HWJ_SaveService loadedSaveService = loadedSaveObject.AddComponent<HWJ_SaveService>();
        loadedSaveService.SetRuntimeSources(
            loadedPlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            loadedStage,
            runtimeDatabase);

        yield return null;

        HWJ_SaveOperationResult loadResult = loadedSaveService.LoadAndApply(saveSlotId);

        Assert.IsTrue(loadResult.Succeeded, loadResult.Message);
        Assert.AreEqual(3, loadedLevel.CurrentLevel);
        Assert.AreEqual(7, loadedLevel.CurrentExperience);
        Assert.AreEqual(2, loadedLevel.SkillPoint);
        Assert.IsTrue(loadedSkillUnlock.IsSkillUnlocked("skill.save.roundtrip"));
        Assert.AreEqual(HWJ_StageFlowState.BossReady, loadedStage.CurrentState);
        Assert.IsTrue(loadedStage.ObjectiveComplete);
        Assert.IsTrue(loadedStage.BossUnlocked);
        Assert.IsTrue(loadedPlayer.GetComponent<HWJ_PossessionSystem>().HasActivePossessedBody);
        Assert.AreEqual(HWJ_PlayerExistenceState.Possessed, loadedPlayer.GetComponent<HWJ_SoulSystem>().CurrentExistenceState);
        Assert.AreEqual(4f, loadedPlayer.GetComponent<HWJ_BodyDecaySystem>().CurrentDecayValue, 0.001f);
        Assert.IsTrue(loadedPlayer.GetComponent<HWJ_PossessedBodySystem>().TryGetCurrentBodyState(
            out HWJ_PossessedBodyRuntimeState loadedBodyState));
        Assert.AreEqual(possessedBodyId, loadedBodyState.DefinitionDataId);
        Assert.AreEqual(6f, loadedBodyState.CurrentHp, 0.001f);

        loadedSaveObject.SetActive(false);
        Object.Destroy(loadedSaveObject);
        Object.Destroy(loadedPlayer);
        Object.Destroy(loadedStageObject);
        Object.Destroy(sourceLevelData);
        Object.Destroy(loadedLevelData);
        Object.Destroy(runtimeDatabase);
        Object.Destroy(possessedBodyRootData);

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    [UnityTest]
    public IEnumerator SaveMigrationService_CurrentSchemaSkipsMigration()
    {
        HWJ_GameSaveData saveData = new HWJ_GameSaveData
        {
            schemaVersion = HWJ_SaveSchema.CurrentVersion,
            saveId = "migration.current"
        };

        yield return null;

        HWJ_SaveMigrationResult migrationResult = HWJ_SaveMigrationService.MigrateToCurrent(saveData);

        Assert.IsTrue(migrationResult.Succeeded, migrationResult.Message);
        Assert.IsFalse(migrationResult.MigrationRequired);
        Assert.IsFalse(migrationResult.Migrated);
        Assert.AreEqual(HWJ_SaveMigrationFailureCode.None, migrationResult.FailureCode);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, migrationResult.SourceVersion);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, migrationResult.TargetVersion);
        Assert.AreSame(saveData, migrationResult.SaveData);
    }

    [UnityTest]
    public IEnumerator SaveMigrationService_LegacySchemaFailsWithExplicitUnsupportedPath()
    {
        HWJ_GameSaveData legacySaveData = new HWJ_GameSaveData
        {
            schemaVersion = 0,
            saveId = "migration.legacy"
        };

        yield return null;

        HWJ_SaveMigrationResult migrationResult = HWJ_SaveMigrationService.MigrateToCurrent(legacySaveData);

        Assert.IsFalse(migrationResult.Succeeded);
        Assert.IsTrue(migrationResult.MigrationRequired);
        Assert.IsFalse(migrationResult.Migrated);
        Assert.AreEqual(HWJ_SaveMigrationFailureCode.UnsupportedLegacyVersion, migrationResult.FailureCode);
        Assert.AreEqual(0, migrationResult.SourceVersion);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, migrationResult.TargetVersion);
        Assert.AreSame(legacySaveData, migrationResult.SaveData);
    }

    [UnityTest]
    public IEnumerator SaveMigrationService_FutureSchemaFailsWithoutMigrationRequired()
    {
        HWJ_GameSaveData futureSaveData = new HWJ_GameSaveData
        {
            schemaVersion = HWJ_SaveSchema.CurrentVersion + 1,
            saveId = "migration.future"
        };

        yield return null;

        HWJ_SaveMigrationResult migrationResult = HWJ_SaveMigrationService.MigrateToCurrent(futureSaveData);

        Assert.IsFalse(migrationResult.Succeeded);
        Assert.IsFalse(migrationResult.MigrationRequired);
        Assert.IsFalse(migrationResult.Migrated);
        Assert.AreEqual(HWJ_SaveMigrationFailureCode.UnsupportedFutureVersion, migrationResult.FailureCode);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion + 1, migrationResult.SourceVersion);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, migrationResult.TargetVersion);
        Assert.AreSame(futureSaveData, migrationResult.SaveData);
    }

    [UnityTest]
    public IEnumerator SaveService_LoadDataFailsLegacySchemaThroughMigrationService()
    {
        string saveSlotId = "playmode_legacy_migration_" + System.Guid.NewGuid().ToString("N");
        GameObject saveObject = new GameObject("LegacyMigrationSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        string saveFilePath = saveService.GetSaveFilePath(saveSlotId);
        string directoryPath = Path.GetDirectoryName(saveFilePath);
        HWJ_GameSaveData legacySaveData = new HWJ_GameSaveData
        {
            schemaVersion = 0,
            saveId = saveSlotId
        };
        legacySaveData.EnsureDefaults();

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(legacySaveData, true));

        yield return null;

        HWJ_SaveOperationResult loadResult = saveService.LoadData(
            out HWJ_GameSaveData loadedSaveData,
            saveSlotId);

        Assert.IsFalse(loadResult.Succeeded);
        Assert.AreEqual(HWJ_SaveOperationFailureCode.MigrationRequired, loadResult.FailureCode);
        Assert.NotNull(loadedSaveData);
        Assert.AreEqual(0, loadedSaveData.schemaVersion);
        StringAssert.Contains("no migration path", loadResult.Message);

        saveObject.SetActive(false);
        Object.Destroy(saveObject);

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    [UnityTest]
    public IEnumerator RuntimeSaveIdentity_ManualSceneIdentityBuildsRewardClaimId()
    {
        GameObject enemy = CreateCombatObject(
            "ManualSceneRewardEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RuntimeSaveIdentity saveIdentity = enemy.AddComponent<HWJ_RuntimeSaveIdentity>();

        saveIdentity.SetManualIdentity(" scene.enemy.001 ", " enemy.scene.manual ");

        yield return null;

        HWJ_SaveIdentityValidationResult validationResult = saveIdentity.ValidateForRewardClaim(enemyResolver);

        Assert.IsTrue(validationResult.Succeeded, validationResult.Message);
        Assert.AreEqual(HWJ_SaveIdentityValidationFailureCode.None, validationResult.FailureCode);
        Assert.AreEqual("scene.enemy.001", validationResult.StableInstanceId);
        Assert.AreEqual("enemy.scene.manual", validationResult.RootObjectId);
        Assert.AreEqual("enemy.scene.manual:scene.enemy.001", validationResult.RewardClaimId);
        Assert.AreEqual(validationResult.RewardClaimId, saveIdentity.GetRewardClaimId(enemyResolver));

        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator RewardUtility_WithActiveSaveServiceRequiresManualSceneSaveIdentity()
    {
        GameObject saveObject = new GameObject("ManualIdentitySaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        GameObject player = CreateCombatObject(
            "ManualIdentityRewardPlayer",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_LevelUpSystem playerLevel = player.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO levelData = CreateLevelUpData("test.level.manual.identity", 3, 1, new[] { 10, 20 });
        playerLevel.SetLevelUpData(levelData);
        GameObject enemy = CreateCombatObject(
            "ManualIdentityRewardEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();
        enemyResolver.Reward.experienceReward = 5;

        yield return null;

        // 저장 서비스가 켜진 씬에서는 직접 배치 적도 영구 저장 ID를 가져야 합니다.
        HWJ_RewardGrantResult missingIdentityResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            enemyResolver,
            player.GetComponent<HWJ_RootObjectDataResolver>(),
            enemy.transform.position);

        Assert.IsFalse(missingIdentityResult.Succeeded);
        Assert.AreEqual(HWJ_RewardGrantFailureCode.MissingSaveIdentity, missingIdentityResult.FailureCode);
        Assert.AreEqual(0, playerLevel.CurrentExperience);

        enemy.AddComponent<HWJ_RuntimeSaveIdentity>()
            .SetManualIdentity("scene.enemy.002", "enemy.scene.manual");

        HWJ_RewardGrantResult claimedResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            enemyResolver,
            player.GetComponent<HWJ_RootObjectDataResolver>(),
            enemy.transform.position);

        Assert.IsTrue(claimedResult.Succeeded, claimedResult.Message);
        Assert.AreEqual("enemy.scene.manual:scene.enemy.002", claimedResult.RewardClaimId);
        Assert.IsTrue(saveService.IsRewardClaimed(claimedResult.RewardClaimId));
        Assert.AreEqual(5, playerLevel.CurrentExperience);

        saveObject.SetActive(false);
        Object.Destroy(saveObject);
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(levelData);
    }

    [UnityTest]
    public IEnumerator SkillUnlockSystem_AutoUnlocksLevelSkillsAndManualUnlockSpendsSkillPoint()
    {
        GameObject player = CreatePlayerObject("SkillUnlockPlayer", true);
        HWJ_PlayerTypeDataSO playerData = player.GetComponent<HWJ_RootObjectDataResolver>().TypeData as HWJ_PlayerTypeDataSO;
        playerData.SkillSet.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.start",
                startsUnlocked = true
            },
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.level2",
                unlockLevel = 2
            },
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.manual",
                unlockLevel = 2,
                requiredSkillPoint = 1
            }
        };
        HWJ_LevelUpSystem levelProgress = player.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO levelData = CreateLevelUpData("test.level.unlock", 3, 1, new[] { 10, 20 });
        levelProgress.SetLevelUpData(levelData);
        HWJ_SkillUnlockSystem unlockState = player.AddComponent<HWJ_SkillUnlockSystem>();
        int skillUnlockedCount = 0;
        HWJ_SkillUnlockedEvent lastSkillEvent = default(HWJ_SkillUnlockedEvent);

        void OnSkillUnlocked(HWJ_SkillUnlockedEvent skillEvent)
        {
            skillUnlockedCount++;
            lastSkillEvent = skillEvent;
        }

        HWJ_GameplayEvents.SkillUnlocked += OnSkillUnlocked;

        yield return null;

        Assert.IsTrue(unlockState.IsSkillUnlocked("skill.test.start"));
        Assert.IsFalse(unlockState.IsSkillUnlocked("skill.test.level2"));
        Assert.IsFalse(unlockState.IsSkillUnlocked("skill.test.manual"));

        levelProgress.AddExperience(10);

        Assert.AreEqual(2, levelProgress.CurrentLevel);
        Assert.AreEqual(1, levelProgress.SkillPoint);
        Assert.IsTrue(unlockState.IsSkillUnlocked("skill.test.level2"));
        Assert.IsFalse(unlockState.IsSkillUnlocked("skill.test.manual"));
        Assert.AreEqual(1, skillUnlockedCount);
        Assert.AreEqual("skill.test.level2", lastSkillEvent.SkillId);
        Assert.IsFalse(lastSkillEvent.SpentSkillPoint);

        HWJ_SkillUnlockResult manualResult = unlockState.TryUnlockSkill("skill.test.manual");

        Assert.IsTrue(manualResult.Succeeded);
        Assert.AreEqual(HWJ_SkillUnlockFailureCode.None, manualResult.FailureCode);
        Assert.AreEqual(0, levelProgress.SkillPoint);
        Assert.IsTrue(unlockState.IsSkillUnlocked("skill.test.manual"));
        Assert.AreEqual(2, skillUnlockedCount);
        Assert.AreEqual("skill.test.manual", lastSkillEvent.SkillId);
        Assert.IsTrue(lastSkillEvent.SpentSkillPoint);

        HWJ_SkillUnlockResult duplicateResult = unlockState.TryUnlockSkill("skill.test.manual");

        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_SkillUnlockFailureCode.AlreadyUnlocked, duplicateResult.FailureCode);
        Assert.AreEqual(0, levelProgress.SkillPoint);
        Assert.AreEqual(2, skillUnlockedCount);

        HWJ_GameplayEvents.SkillUnlocked -= OnSkillUnlocked;
        Object.Destroy(player);
        Object.Destroy(levelData);
    }

    [UnityTest]
    public IEnumerator SkillActionSystem_BlocksTrackedPlayerSkillUntilUnlocked()
    {
        GameObject player = CreatePlayerObject("LockedSkillPlayer", true);
        HWJ_PlayerTypeDataSO playerData = player.GetComponent<HWJ_RootObjectDataResolver>().TypeData as HWJ_PlayerTypeDataSO;
        playerData.SkillSet.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.locked",
                unlockLevel = 2
            }
        };
        HWJ_SkillUnlockSystem unlockState = player.AddComponent<HWJ_SkillUnlockSystem>();
        HWJ_SkillActionSystem skillActionSystem = player.AddComponent<HWJ_SkillActionSystem>();
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.locked",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillActionSystem, "localSkillActions", new[] { skillAction });

        yield return null;

        Assert.IsFalse(skillActionSystem.TryUseSkill("skill.test.locked"));
        Assert.AreEqual("Skill failed: skill.test.locked is locked.", skillActionSystem.LastSkillResult);

        Assert.IsTrue(unlockState.ForceUnlockSkill("skill.test.locked").Succeeded);
        Assert.IsTrue(skillActionSystem.TryUseSkill("skill.test.locked"));
        Assert.IsTrue(unlockState.IsSkillUnlocked("skill.test.locked"));

        Object.Destroy(player);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator PossessionSystem_AllowsDefeatedPossessableEnemyBody()
    {
        GameObject player = CreatePlayerObject("Player", true);
        GameObject enemy = CreateCombatObject(
            "EnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        HWJ_RuntimeStatusSystem enemyStatus = enemy.GetComponent<HWJ_RuntimeStatusSystem>();
        enemyStatus.ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.IsTrue(possession.TryPossess(enemyResolver));
        Assert.IsTrue(possession.HasActivePossessedBody);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator SoulSystem_InitialSoulStateMapsToSpiritExistenceState()
    {
        GameObject player = CreatePlayerObject("PlayerStartSpirit", true, 10f, 1f, true);

        yield return null;

        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();

        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);
        Assert.AreEqual(HWJ_PlayerExistenceState.Spirit, soul.CurrentExistenceState);
        Assert.IsTrue(soul.IsSpiritExistence);

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator SoulSystem_InitialBodyStateMapsToPossessedExistenceState()
    {
        GameObject player = CreatePlayerObject("PlayerStartBody", true, 10f, 1f, false);

        yield return null;

        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();

        Assert.AreEqual(HWJ_SoulRuntimeState.Body, soul.CurrentState);
        Assert.AreEqual(HWJ_PlayerExistenceState.Possessed, soul.CurrentExistenceState);
        Assert.IsTrue(soul.IsPossessedExistence);

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator SoulSystem_BodyToSoulMapsToCollapsingExistenceState()
    {
        GameObject player = CreatePlayerObject("PlayerCollapseBridge", true, 10f, 1f, false);

        yield return null;

        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();
        soul.EnterSoulState(false);

        Assert.AreEqual(HWJ_SoulRuntimeState.BodyToSoul, soul.CurrentState);
        Assert.AreEqual(HWJ_PlayerExistenceState.Collapsing, soul.CurrentExistenceState);
        Assert.IsTrue(soul.IsTransitioningExistence);

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator PossessionSystem_ReturnsFailureCodeForMissingTarget()
    {
        GameObject player = CreatePlayerObject("PlayerMissingTarget", true);

        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_PossessionResult result = possession.EvaluatePossession(null);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(HWJ_PossessionFailureCode.InvalidTarget, result.FailureCode);
        Assert.AreEqual(HWJ_PossessionFailureCode.InvalidTarget, possession.LastPossessionFailureCode);

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator PossessionSystem_ReturnsFailureCodeWhenNotInSoulState()
    {
        GameObject player = CreatePlayerObject("PlayerNotSoul", true, 10f, 1f, false);
        GameObject enemy = CreateCombatObject(
            "EnemyCorpseForNotSoul",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_PossessionResult result = possession.EvaluatePossession(enemy.GetComponent<HWJ_RootObjectDataResolver>());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(HWJ_PossessionFailureCode.NotInSpiritState, result.FailureCode);
        Assert.AreEqual(HWJ_PossessionFailureCode.NotInSpiritState, possession.LastPossessionFailureCode);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PossessionSystem_ReturnsFailureCodeForConsumedBody()
    {
        GameObject player = CreatePlayerObject("PlayerConsumedBody", true);
        GameObject enemy = CreateCombatObject(
            "ConsumedEnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        HWJ_PossessionBodyState bodyState = enemy.AddComponent<HWJ_PossessionBodyState>();
        bodyState.MarkConsumed();
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_PossessionResult result = possession.EvaluatePossession(enemy.GetComponent<HWJ_RootObjectDataResolver>());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(HWJ_PossessionFailureCode.TargetAlreadyPossessed, result.FailureCode);
        Assert.AreEqual(HWJ_PossessionFailureCode.TargetAlreadyPossessed, possession.LastPossessionFailureCode);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator BodyDecaySystem_IncreasesDecayWhenPossessedBodyIsHit()
    {
        GameObject player = CreatePlayerObject("PlayerWithDecay", true, 5f, 2f);
        GameObject enemy = CreateCombatObject(
            "DecayBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        HWJ_RuntimeStatusSystem enemyStatus = enemy.GetComponent<HWJ_RuntimeStatusSystem>();
        enemyStatus.ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_BodyDecaySystem bodyDecay = player.GetComponent<HWJ_BodyDecaySystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));

        bodyDecay.RestoreDecaySnapshot(1f);
        bodyDecay.ApplyHitDecayPenalty(2f);

        Assert.AreEqual(3f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(2f, bodyDecay.RemainingDecayValue, 0.001f);
        Assert.IsTrue(possession.TryGetPossessedBodyRuntimeState(out HWJ_PossessedBodyRuntimeState bodyState));
        Assert.AreEqual(3f, bodyState.CurrentDecayValue, 0.001f);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PossessedBodySystem_CreatesRuntimeStateWithoutMutatingSourceData()
    {
        GameObject player = CreatePlayerObject("PlayerRuntimeBodyState", true, 12f, 2f);
        GameObject enemy = CreateCombatObject(
            "RuntimeBodyEnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            14f,
            3f,
            3f,
            CreateEnemyTypeData(true));

        yield return null;

        HWJ_RootObjectDataResolver playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_PlayerTypeDataSO playerData = playerResolver.TypeData as HWJ_PlayerTypeDataSO;
        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_PossessedBodySystem possessedBody = player.GetComponent<HWJ_PossessedBodySystem>();

        Assert.IsTrue(possession.TryPossess(enemyResolver));
        Assert.IsTrue(possessedBody.TryGetCurrentBodyState(out HWJ_PossessedBodyRuntimeState bodyState));
        Assert.AreEqual("Enemy", bodyState.DefinitionDataId);
        Assert.AreEqual(14f, bodyState.MaxHp, 0.001f);
        Assert.AreEqual(14f, bodyState.CurrentHp, 0.001f);
        Assert.AreEqual(12f, bodyState.MaxDecayValue, 0.001f);
        Assert.AreEqual(0f, bodyState.CurrentDecayValue, 0.001f);

        bodyState.SetCurrentHp(5f);
        bodyState.SetCurrentDecayValue(4f);

        Assert.AreEqual(5f, bodyState.CurrentHp, 0.001f);
        Assert.AreEqual(4f, bodyState.CurrentDecayValue, 0.001f);
        Assert.AreEqual(14f, enemyResolver.Status.maxHp, 0.001f);
        Assert.AreEqual(12f, playerData.BodyDecay.maxDecayValue, 0.001f);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PossessedBodySystem_ClampsRuntimeHpAndDecay()
    {
        GameObject player = CreatePlayerObject("PlayerRuntimeBodyClamp", true, 8f, 1f);
        GameObject enemy = CreateCombatObject(
            "RuntimeBodyClampEnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            3f,
            3f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        Assert.IsTrue(possession.TryGetPossessedBodyRuntimeState(out HWJ_PossessedBodyRuntimeState bodyState));

        bodyState.SetCurrentHp(99f);
        Assert.AreEqual(10f, bodyState.CurrentHp, 0.001f);

        bodyState.SetCurrentDecayValue(99f);
        Assert.AreEqual(8f, bodyState.CurrentDecayValue, 0.001f);
        Assert.AreEqual(0f, bodyState.CurrentHp, 0.001f);
        Assert.IsTrue(bodyState.IsCollapsed);

        bodyState.SetCurrentHp(-5f);
        bodyState.SetCurrentDecayValue(-5f);
        Assert.AreEqual(0f, bodyState.CurrentHp, 0.001f);
        Assert.AreEqual(0f, bodyState.CurrentDecayValue, 0.001f);
        Assert.IsTrue(bodyState.IsCollapsed);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator BodyDecaySystem_CollapsesWhenDecayReachesMax()
    {
        GameObject player = CreatePlayerObject("PlayerDecayCollapse", true, 5f, 1f);
        GameObject enemy = CreateCombatObject(
            "DecayCollapseBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_BodyDecaySystem bodyDecay = player.GetComponent<HWJ_BodyDecaySystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));

        bodyDecay.RestoreDecaySnapshot(4f);
        bodyDecay.ApplyActionDecay(1f);

        Assert.AreEqual(5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(HWJ_DecayDangerLevel.Collapsed, bodyDecay.CurrentDangerLevel);
        Assert.AreEqual(HWJ_SoulRuntimeState.BodyToSoul, player.GetComponent<HWJ_SoulSystem>().CurrentState);
        Assert.AreEqual(
            HWJ_BodyCollapseReason.DecayMaxed,
            player.GetComponent<HWJ_CollapseSystem>().LastCollapseReason);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator CollapseSystem_CollapsesBodyOnceAndBlocksDuplicateRequest()
    {
        GameObject player = CreatePlayerObject("PlayerCollapseSystem", true, 5f, 1f);
        GameObject enemy = CreateCombatObject(
            "CollapseSystemBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int startedCount = 0;
        int completedCount = 0;

        void OnStarted(HWJ_BodyCollapseEvent collapseEvent)
        {
            startedCount++;
        }

        void OnCompleted(HWJ_BodyCollapseEvent collapseEvent)
        {
            completedCount++;
        }

        HWJ_GameplayEvents.BodyCollapseStarted += OnStarted;
        HWJ_GameplayEvents.BodyCollapsed += OnCompleted;

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_CollapseSystem collapseSystem = player.GetComponent<HWJ_CollapseSystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));

        HWJ_BodyCollapseResult firstResult = collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.Forced);
        HWJ_BodyCollapseResult duplicateResult = collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.Forced);

        Assert.IsTrue(firstResult.Succeeded);
        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_BodyCollapseFailureCode.AlreadyCollapsing, duplicateResult.FailureCode);
        Assert.AreEqual(1, startedCount);
        Assert.AreEqual(1, completedCount);
        Assert.IsFalse(possession.HasActivePossessedBody);
        Assert.AreEqual(HWJ_SoulRuntimeState.BodyToSoul, player.GetComponent<HWJ_SoulSystem>().CurrentState);

        HWJ_GameplayEvents.BodyCollapseStarted -= OnStarted;
        HWJ_GameplayEvents.BodyCollapsed -= OnCompleted;
        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_BlocksSoulAttackAndAppliesBasicAttackDecayWhenPossessed()
    {
        GameObject player = CreatePlayerObject("PlayerBasicAttackDecay", true, 10f, 1f, true, 1.5f, 2.5f);
        player.AddComponent<HWJ_CombatExecutionSystem>();
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        GameObject enemy = CreateCombatObject(
            "BasicAttackDecayBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int abilityUsedCount = 0;
        HWJ_AbilityUsedEvent lastAbilityEvent = default(HWJ_AbilityUsedEvent);

        void OnAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
        {
            abilityUsedCount++;
            lastAbilityEvent = abilityEvent;
        }

        HWJ_GameplayEvents.AbilityUsed += OnAbilityUsed;

        yield return null;

        Assert.IsFalse(attackSystem.TryBasicAttack());
        Assert.AreEqual("Attack requires a possessed body.", attackSystem.LastAttackResult);

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_BodyDecaySystem bodyDecay = player.GetComponent<HWJ_BodyDecaySystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        bodyDecay.RestoreDecaySnapshot(1f);

        Assert.IsTrue(attackSystem.TryBasicAttack());
        Assert.AreEqual(2.5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(1, abilityUsedCount);
        Assert.AreEqual("basic_attack", lastAbilityEvent.AbilityId);
        Assert.IsTrue(lastAbilityEvent.IsBasicAttack);
        Assert.AreEqual(2.5f, lastAbilityEvent.DecayValueAfterUse, 0.001f);

        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_AppliesSkillDecayWhenPossessedSkillSucceeds()
    {
        GameObject player = CreatePlayerObject("PlayerPossessedSkillDecay", true, 10f, 1f, true, 1.5f, 2.5f);
        HWJ_SkillActionSystem skillActionSystem = player.AddComponent<HWJ_SkillActionSystem>();
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.possessed_buff",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillActionSystem, "localSkillActions", new[] { skillAction });
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        enemyData.SkillCycle.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.possessed_buff",
                startsUnlocked = true
            }
        };
        GameObject enemy = CreateCombatObject(
            "PossessedSkillDecayBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            enemyData);
        int abilityUsedCount = 0;
        HWJ_AbilityUsedEvent lastAbilityEvent = default(HWJ_AbilityUsedEvent);

        void OnAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
        {
            abilityUsedCount++;
            lastAbilityEvent = abilityEvent;
        }

        HWJ_GameplayEvents.AbilityUsed += OnAbilityUsed;

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_BodyDecaySystem bodyDecay = player.GetComponent<HWJ_BodyDecaySystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        bodyDecay.RestoreDecaySnapshot(1f);

        Assert.IsTrue(attackSystem.TryPossessedSkillSlot(0));
        Assert.AreEqual(3.5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(1, abilityUsedCount);
        Assert.AreEqual("skill.test.possessed_buff", lastAbilityEvent.AbilityId);
        Assert.IsFalse(lastAbilityEvent.IsBasicAttack);
        Assert.AreEqual(3.5f, lastAbilityEvent.DecayValueAfterUse, 0.001f);

        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator BodyDiscoverySystem_SelectsNearestValidDefeatedBody()
    {
        GameObject player = CreatePlayerObject("PlayerBodyDiscovery", true);
        GameObject farEnemy = CreateCombatObject(
            "FarEnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        GameObject nearEnemy = CreateCombatObject(
            "NearEnemyCorpse",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        player.transform.position = Vector3.zero;
        farEnemy.transform.position = new Vector3(1.2f, 0f, 0f);
        nearEnemy.transform.position = new Vector3(0.4f, 0f, 0f);

        yield return null;

        farEnemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        nearEnemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_BodyDiscoverySystem discovery = player.GetComponent<HWJ_BodyDiscoverySystem>();

        Assert.IsTrue(discovery.TryGetBestTarget(out HWJ_RootObjectDataResolver target, out HWJ_BodyDiscoveryResult result));
        Assert.AreSame(nearEnemy.GetComponent<HWJ_RootObjectDataResolver>(), target);
        Assert.IsTrue(result.HasTarget);
        Assert.AreEqual(2, result.Candidates.Length);

        Object.Destroy(player);
        Object.Destroy(farEnemy);
        Object.Destroy(nearEnemy);
    }

    [UnityTest]
    public IEnumerator BodyDiscoverySystem_IgnoresAliveEnemyBody()
    {
        GameObject player = CreatePlayerObject("PlayerBodyDiscoveryAliveFilter", true);
        GameObject aliveEnemy = CreateCombatObject(
            "AliveEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        player.transform.position = Vector3.zero;
        aliveEnemy.transform.position = new Vector3(0.4f, 0f, 0f);

        yield return null;

        HWJ_BodyDiscoverySystem discovery = player.GetComponent<HWJ_BodyDiscoverySystem>();

        Assert.IsFalse(discovery.TryGetBestTarget(out HWJ_RootObjectDataResolver target, out HWJ_BodyDiscoveryResult result));
        Assert.IsNull(target);
        Assert.IsFalse(result.HasTarget);
        Assert.AreEqual(0, result.Candidates.Length);
        Assert.AreEqual(1, result.RejectedCount);

        Object.Destroy(player);
        Object.Destroy(aliveEnemy);
    }

    [UnityTest]
    public IEnumerator GameplayDatabase_ReportsMissingAndDuplicateStableIds()
    {
        HWJ_GameplayDatabaseSO database = ScriptableObject.CreateInstance<HWJ_GameplayDatabaseSO>();
        HWJ_RootObjectDataSO first = CreateRootObjectData(
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true),
            0f,
            "enemy.duplicate");
        HWJ_RootObjectDataSO duplicate = CreateRootObjectData(
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true),
            0f,
            "enemy.duplicate");
        HWJ_RootObjectDataSO missing = CreateRootObjectData(
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true),
            0f,
            string.Empty);

        first.name = "FirstEnemy";
        duplicate.name = "DuplicateEnemy";
        missing.name = "MissingIdEnemy";
        SetPrivateField(database, "rootObjects", new[] { first, duplicate, missing });

        yield return null;

        HWJ_GameDataRegistryReport report = database.ValidateRegistryIds();

        Assert.IsFalse(report.IsValid);
        Assert.AreEqual(2, report.errorCount);
        Assert.IsTrue(report.HasEntry("ID_DUPLICATE"));
        Assert.IsTrue(report.HasEntry("ID_MISSING"));

        Object.Destroy(database);
        Object.Destroy(first);
        Object.Destroy(duplicate);
        Object.Destroy(missing);
    }

    [UnityTest]
    public IEnumerator StageProgressionSystem_TracksStageFlowSeparatelyFromPlayerExistence()
    {
        GameObject player = CreatePlayerObject("PlayerStageSeparateState", true);
        GameObject stageObject = new GameObject("StageProgression");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        stage.SetStageIds("stage.region01.01", "region.region02");

        yield return null;

        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();
        Assert.AreEqual(HWJ_PlayerExistenceState.Spirit, soul.CurrentExistenceState);
        Assert.AreEqual(HWJ_StageFlowState.Entering, stage.CurrentState);

        HWJ_StageFlowTransitionResult exploringResult = stage.TryEnterExploring();
        HWJ_StageFlowTransitionResult combatResult = stage.TryEnterCombat();

        Assert.IsTrue(exploringResult.Succeeded);
        Assert.IsTrue(combatResult.Succeeded);
        Assert.AreEqual(HWJ_StageFlowState.Combat, stage.CurrentState);
        Assert.AreEqual(HWJ_PlayerExistenceState.Spirit, soul.CurrentExistenceState);

        Object.Destroy(player);
        Object.Destroy(stageObject);
    }

    [UnityTest]
    public IEnumerator StageProgressionSystem_BossBattleRequiresObjectiveAndBossReady()
    {
        GameObject stageObject = new GameObject("StageProgressionBossGate");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        stage.SetStageIds("stage.region01.boss", "region.region02");
        int stageFlowChangedCount = 0;
        int bossBattleStartedCount = 0;

        void OnStageFlowChanged(HWJ_StageFlowStateChangedEvent stateEvent)
        {
            stageFlowChangedCount++;
        }

        void OnBossBattleStarted(HWJ_StageProgressionEvent progressionEvent)
        {
            bossBattleStartedCount++;
        }

        HWJ_GameplayEvents.StageFlowStateChanged += OnStageFlowChanged;
        HWJ_GameplayEvents.BossBattleStarted += OnBossBattleStarted;

        yield return null;

        Assert.IsTrue(stage.TryEnterExploring().Succeeded);

        HWJ_StageFlowTransitionResult blockedBattleResult = stage.TryStartBossBattle();
        Assert.IsFalse(blockedBattleResult.Succeeded);
        Assert.AreEqual(HWJ_StageFlowTransitionFailureCode.ObjectiveNotComplete, blockedBattleResult.FailureCode);
        Assert.AreEqual(HWJ_StageFlowState.Exploring, stage.CurrentState);

        Assert.IsTrue(stage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(stage.TryUnlockBoss().Succeeded);
        Assert.IsTrue(stage.TryStartBossBattle().Succeeded);
        Assert.AreEqual(HWJ_StageFlowState.BossBattle, stage.CurrentState);
        Assert.AreEqual(1, bossBattleStartedCount);

        Assert.IsTrue(stage.TryMarkBossDefeated().Succeeded);
        Assert.AreEqual(HWJ_StageFlowState.StageClear, stage.CurrentState);
        Assert.IsTrue(stage.TryUnlockRegion("region.region02").Succeeded);
        Assert.AreEqual(HWJ_StageFlowState.RegionTransition, stage.CurrentState);

        HWJ_RuntimeStageFlowSnapshot snapshot = stage.CreateSnapshot();
        Assert.AreEqual("stage.region01.boss", snapshot.stageId);
        Assert.AreEqual(HWJ_StageFlowState.RegionTransition, snapshot.currentState);
        Assert.IsTrue(snapshot.objectiveComplete);
        Assert.IsTrue(snapshot.bossUnlocked);
        Assert.IsTrue(snapshot.bossBattleStarted);
        Assert.IsTrue(snapshot.bossDefeated);
        Assert.IsTrue(snapshot.regionUnlocked);
        Assert.GreaterOrEqual(stageFlowChangedCount, 6);

        HWJ_GameplayEvents.StageFlowStateChanged -= OnStageFlowChanged;
        HWJ_GameplayEvents.BossBattleStarted -= OnBossBattleStarted;
        Object.Destroy(stageObject);
    }

    [UnityTest]
    public IEnumerator BossFlowSystem_StartsBossBattleOnlyWithValidPossessedBody()
    {
        GameObject player = CreatePlayerObject("BossFlowPlayer", true);
        GameObject stageObject = new GameObject("BossFlowStage");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        stage.SetStageIds("stage.region01.boss", "region.region02");
        GameObject boss = CreateCombatObject(
            "BossFlowBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            100f,
            5f,
            5f,
            CreateBossTypeData());
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_BossFlowSystem bossFlow = boss.AddComponent<HWJ_BossFlowSystem>();
        GameObject body = CreateCombatObject(
            "BossFlowPossessableBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int bossBattleStartedCount = 0;

        void OnBossBattleStarted(HWJ_StageProgressionEvent progressionEvent)
        {
            bossBattleStartedCount++;
        }

        SetPrivateField(bossFlow, "stageProgressionSystem", stage);
        SetPrivateField(bossFlow, "playerResolver", player.GetComponent<HWJ_RootObjectDataResolver>());
        SetPrivateField(bossFlow, "playerSoulSystem", player.GetComponent<HWJ_SoulSystem>());
        SetPrivateField(bossFlow, "playerPossessionSystem", player.GetComponent<HWJ_PossessionSystem>());
        SetPrivateField(bossFlow, "playerCollapseSystem", player.GetComponent<HWJ_CollapseSystem>());
        SetPrivateField(bossFlow, "playerStatusSystem", player.GetComponent<HWJ_RuntimeStatusSystem>());
        SetPrivateField(bossFlow, "playerBodyDecaySystem", player.GetComponent<HWJ_BodyDecaySystem>());
        HWJ_GameplayEvents.BossBattleStarted += OnBossBattleStarted;

        yield return null;

        Assert.IsTrue(stage.TryEnterExploring().Succeeded);

        HWJ_BossFlowResult objectiveBlockedResult = bossFlow.EvaluateBossBattleStart();
        Assert.IsFalse(objectiveBlockedResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowFailureCode.ObjectiveNotComplete, objectiveBlockedResult.FailureCode);

        Assert.IsTrue(stage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(stage.TryUnlockBoss().Succeeded);

        HWJ_BossFlowResult possessionBlockedResult = bossFlow.EvaluateBossBattleStart();
        Assert.IsFalse(possessionBlockedResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowFailureCode.PlayerNotPossessed, possessionBlockedResult.FailureCode);

        body.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        Assert.IsTrue(player.GetComponent<HWJ_PossessionSystem>().TryPossess(
            body.GetComponent<HWJ_RootObjectDataResolver>()));
        yield return null;

        HWJ_BossFlowResult validResult = bossFlow.EvaluateBossBattleStart();
        Assert.IsTrue(validResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowFailureCode.None, validResult.FailureCode);

        HWJ_BossFlowResult startResult = bossFlow.TryStartBossBattle();
        Assert.IsTrue(startResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowOperationType.StartBossBattle, startResult.OperationType);
        Assert.AreEqual(HWJ_StageFlowState.BossBattle, stage.CurrentState);
        Assert.IsTrue(bossBrain.EncounterStarted);
        Assert.AreEqual(1, bossBattleStartedCount);

        HWJ_BossFlowResult duplicateStartResult = bossFlow.TryStartBossBattle();
        Assert.IsFalse(duplicateStartResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowFailureCode.BossBattleAlreadyStarted, duplicateStartResult.FailureCode);

        HWJ_GameplayEvents.BossBattleStarted -= OnBossBattleStarted;
        Object.Destroy(player);
        Object.Destroy(stageObject);
        Object.Destroy(boss);
        Object.Destroy(body);
    }

    [UnityTest]
    public IEnumerator CoreLoopCoordinator_PossessesDiscoveredBodyThroughExistingSystems()
    {
        GameObject player = CreatePlayerObject("CoreLoopPossessionPlayer", true);
        HWJ_CoreLoopCoordinator coordinator = player.AddComponent<HWJ_CoreLoopCoordinator>();
        GameObject enemy = CreateCombatObject(
            "CoreLoopPossessionBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int coreLoopEventCount = 0;
        HWJ_CoreLoopOperationEvent lastCoreLoopEvent = default(HWJ_CoreLoopOperationEvent);

        void OnCoreLoopOperationCompleted(HWJ_CoreLoopOperationEvent operationEvent)
        {
            coreLoopEventCount++;
            lastCoreLoopEvent = operationEvent;
        }

        HWJ_GameplayEvents.CoreLoopOperationCompleted += OnCoreLoopOperationCompleted;

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_CoreLoopOperationResult result = coordinator.TryPossessBestDiscoveredBody();

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopOperationType.PossessDiscoveredBody, result.OperationType);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.None, result.FailureCode);
        Assert.AreSame(enemy.GetComponent<HWJ_RootObjectDataResolver>(), result.TargetResolver);
        Assert.IsTrue(player.GetComponent<HWJ_PossessionSystem>().HasActivePossessedBody);
        Assert.AreEqual(1, coreLoopEventCount);
        Assert.IsTrue(lastCoreLoopEvent.OperationResult.Succeeded);

        HWJ_GameplayEvents.CoreLoopOperationCompleted -= OnCoreLoopOperationCompleted;
        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator CoreLoopCoordinator_StartsBossBattleThroughBossFlow()
    {
        GameObject player = CreatePlayerObject("CoreLoopBossFlowPlayer", true);
        HWJ_CoreLoopCoordinator coordinator = player.AddComponent<HWJ_CoreLoopCoordinator>();
        GameObject stageObject = new GameObject("CoreLoopBossFlowStage");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        stage.SetStageIds("stage.coreloop.boss", "region.coreloop.next");
        GameObject boss = CreateCombatObject(
            "CoreLoopBossFlowBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            100f,
            5f,
            5f,
            CreateBossTypeData());
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_BossFlowSystem bossFlow = boss.AddComponent<HWJ_BossFlowSystem>();
        GameObject body = CreateCombatObject(
            "CoreLoopBossFlowBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int coreLoopEventCount = 0;
        HWJ_CoreLoopOperationEvent lastCoreLoopEvent = default(HWJ_CoreLoopOperationEvent);

        void OnCoreLoopOperationCompleted(HWJ_CoreLoopOperationEvent operationEvent)
        {
            coreLoopEventCount++;
            lastCoreLoopEvent = operationEvent;
        }

        SetPrivateField(coordinator, "bossFlowSystem", bossFlow);
        SetPrivateField(coordinator, "stageProgressionSystem", stage);
        SetPrivateField(bossFlow, "stageProgressionSystem", stage);
        SetPrivateField(bossFlow, "playerResolver", player.GetComponent<HWJ_RootObjectDataResolver>());
        SetPrivateField(bossFlow, "playerSoulSystem", player.GetComponent<HWJ_SoulSystem>());
        SetPrivateField(bossFlow, "playerPossessionSystem", player.GetComponent<HWJ_PossessionSystem>());
        SetPrivateField(bossFlow, "playerCollapseSystem", player.GetComponent<HWJ_CollapseSystem>());
        SetPrivateField(bossFlow, "playerStatusSystem", player.GetComponent<HWJ_RuntimeStatusSystem>());
        SetPrivateField(bossFlow, "playerBodyDecaySystem", player.GetComponent<HWJ_BodyDecaySystem>());
        HWJ_GameplayEvents.CoreLoopOperationCompleted += OnCoreLoopOperationCompleted;

        yield return null;

        Assert.IsTrue(stage.TryEnterExploring().Succeeded);
        Assert.IsTrue(stage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(stage.TryUnlockBoss().Succeeded);

        HWJ_CoreLoopOperationResult blockedResult = coordinator.TryStartBossBattle();
        Assert.IsFalse(blockedResult.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.BossFlowFailed, blockedResult.FailureCode);
        Assert.AreEqual(HWJ_BossFlowFailureCode.PlayerNotPossessed, blockedResult.BossFlowFailureCode);
        Assert.AreEqual(HWJ_BossFlowFailureCode.PlayerNotPossessed, blockedResult.BossFlowResult.FailureCode);

        body.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        Assert.IsTrue(player.GetComponent<HWJ_PossessionSystem>().TryPossess(
            body.GetComponent<HWJ_RootObjectDataResolver>()));
        yield return null;

        HWJ_CoreLoopOperationResult startResult = coordinator.TryStartBossBattle();
        Assert.IsTrue(startResult.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopOperationType.StartBossBattle, startResult.OperationType);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.None, startResult.FailureCode);
        Assert.AreEqual(HWJ_BossFlowFailureCode.None, startResult.BossFlowFailureCode);
        Assert.IsTrue(startResult.BossFlowResult.Succeeded);
        Assert.AreEqual(HWJ_StageFlowState.BossBattle, stage.CurrentState);
        Assert.IsTrue(bossBrain.EncounterStarted);
        Assert.AreEqual(2, coreLoopEventCount);
        Assert.IsTrue(lastCoreLoopEvent.OperationResult.Succeeded);

        HWJ_GameplayEvents.CoreLoopOperationCompleted -= OnCoreLoopOperationCompleted;
        Object.Destroy(player);
        Object.Destroy(stageObject);
        Object.Destroy(boss);
        Object.Destroy(body);
    }

    [UnityTest]
    public IEnumerator CoreLoopCoordinator_BlocksPossessionCollapseAndStageTransitionDuringSceneTransition()
    {
        GameObject player = CreatePlayerObject("CoreLoopTransitionGuardPlayer", true);
        HWJ_CoreLoopCoordinator coordinator = player.AddComponent<HWJ_CoreLoopCoordinator>();
        GameObject stageObject = new GameObject("CoreLoopTransitionGuardStage");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        stage.SetStageIds("stage.guard", "region.guard.next");
        SetPrivateField(coordinator, "stageProgressionSystem", stage);
        GameObject enemy = CreateCombatObject(
            "CoreLoopTransitionGuardBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_CoreLoopOperationResult lockResult = coordinator.SetSceneTransitionInProgress(true);
        HWJ_CoreLoopOperationResult possessionResult = coordinator.TryPossessBestDiscoveredBody();
        HWJ_CoreLoopOperationResult collapseResult = coordinator.RequestBodyCollapse(HWJ_BodyCollapseReason.Forced);
        HWJ_CoreLoopOperationResult stageResult = coordinator.TryEnterExploring();

        Assert.IsTrue(lockResult.Succeeded);
        Assert.IsTrue(coordinator.SceneTransitionInProgress);
        Assert.IsTrue(stage.TransitionLocked);
        Assert.IsFalse(possessionResult.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.TransitionInProgress, possessionResult.FailureCode);
        Assert.IsFalse(collapseResult.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.TransitionInProgress, collapseResult.FailureCode);
        Assert.IsFalse(stageResult.Succeeded);
        Assert.AreEqual(HWJ_CoreLoopFailureCode.TransitionInProgress, stageResult.FailureCode);
        Assert.IsFalse(player.GetComponent<HWJ_PossessionSystem>().HasActivePossessedBody);

        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(stageObject);
    }

    [UnityTest]
    public IEnumerator MonsterAISystem_ReturnsTransitionResultAndRaisesTransitionEvent()
    {
        GameObject enemy = CreateCombatObject(
            "MonsterAITransitionEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true));
        HWJ_MonsterAISystem monsterAI = enemy.AddComponent<HWJ_MonsterAISystem>();
        int transitionEventCount = 0;
        HWJ_EnemyAITransitionEvent lastTransitionEvent = default(HWJ_EnemyAITransitionEvent);

        void OnEnemyAITransitioned(HWJ_EnemyAITransitionEvent transitionEvent)
        {
            transitionEventCount++;
            lastTransitionEvent = transitionEvent;
        }

        HWJ_GameplayEvents.EnemyAIStateTransitioned += OnEnemyAITransitioned;

        yield return null;

        HWJ_EnemyAITransitionResult firstResult = monsterAI.TrySetAIState(HWJ_MonsterAIState.Detect, 0.25f);
        HWJ_EnemyAITransitionResult duplicateResult = monsterAI.TrySetAIState(HWJ_MonsterAIState.Detect, 0.25f);
        HWJ_EnemyAITransitionResult invalidResult = monsterAI.TrySetAIState((HWJ_MonsterAIState)999, 0f);

        Assert.IsTrue(firstResult.Succeeded);
        Assert.AreEqual(HWJ_MonsterAIState.Idle, firstResult.PreviousState);
        Assert.AreEqual(HWJ_MonsterAIState.Detect, firstResult.CurrentState);
        Assert.AreEqual(HWJ_EnemyAITransitionFailureCode.None, firstResult.FailureCode);
        Assert.AreEqual(1, transitionEventCount);
        Assert.AreSame(monsterAI, lastTransitionEvent.MonsterAI);
        Assert.IsTrue(lastTransitionEvent.TransitionResult.Succeeded);

        Assert.IsFalse(duplicateResult.Succeeded);
        Assert.AreEqual(HWJ_EnemyAITransitionFailureCode.SameStateTimerActive, duplicateResult.FailureCode);
        Assert.AreEqual(1, transitionEventCount);

        Assert.IsFalse(invalidResult.Succeeded);
        Assert.AreEqual(HWJ_EnemyAITransitionFailureCode.InvalidState, invalidResult.FailureCode);
        Assert.AreEqual(HWJ_EnemyAITransitionFailureCode.InvalidState, monsterAI.LastTransitionFailureCode);

        HWJ_GameplayEvents.EnemyAIStateTransitioned -= OnEnemyAITransitioned;
        Object.Destroy(enemy);
    }

    private static GameObject CreatePlayerObject(
        string name,
        bool canPossess,
        float maxDecayValue = 10f,
        float hitDecayPenalty = 1f,
        bool startAsSoul = true,
        float basicAttackDecayAmount = 0f,
        float skillDecayAmount = 0f)
    {
        HWJ_PlayerTypeDataSO playerTypeData = CreatePlayerTypeData(
            canPossess,
            maxDecayValue,
            hitDecayPenalty,
            startAsSoul,
            basicAttackDecayAmount,
            skillDecayAmount);
        GameObject player = CreateCombatObject(
            name,
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            playerTypeData);

        player.AddComponent<HWJ_SoulSystem>();
        player.AddComponent<HWJ_PossessedBodySystem>();
        player.AddComponent<HWJ_PossessionSystem>();
        player.AddComponent<HWJ_BodyDiscoverySystem>();
        player.AddComponent<HWJ_BodyDecaySystem>();
        player.AddComponent<HWJ_CollapseSystem>();
        return player;
    }

    private static GameObject CreateCombatObject(
        string name,
        HWJ_ObjectType objectType,
        HWJ_Faction faction,
        float maxHp,
        float attackPower,
        float baseDamage,
        HWJ_ObjectTypeDataSO typeData,
        float defense = 0f)
    {
        GameObject gameObject = new GameObject(name);
        HWJ_RootObjectDataResolver resolver = gameObject.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(CreateRootObjectData(objectType, faction, maxHp, attackPower, baseDamage, typeData, defense));
        gameObject.AddComponent<HWJ_RuntimeObjectContext>();
        gameObject.AddComponent<HWJ_RuntimeStatusSystem>();
        gameObject.AddComponent<HWJ_CombatSystem>();
        return gameObject;
    }

    private static HWJ_RootObjectDataSO CreateRootObjectData(
        HWJ_ObjectType objectType,
        HWJ_Faction faction,
        float maxHp,
        float attackPower,
        float baseDamage,
        HWJ_ObjectTypeDataSO typeData,
        float defense = 0f,
        string objectId = null)
    {
        HWJ_RootObjectDataSO rootData = ScriptableObject.CreateInstance<HWJ_RootObjectDataSO>();
        HWJ_IdentityData identity = new HWJ_IdentityData
        {
            objectId = objectId ?? objectType.ToString(),
            displayName = objectType.ToString(),
            objectType = objectType,
            faction = faction
        };
        HWJ_StatusData status = new HWJ_StatusData
        {
            maxHp = maxHp,
            attackPower = attackPower,
            moveSpeed = 5f,
            defense = defense,
            attackSpeed = 1f,
            bodyWeight = 1f
        };
        HWJ_DamageData damage = new HWJ_DamageData
        {
            damageType = HWJ_DamageType.Physical,
            baseDamage = baseDamage
        };

        SetPrivateField(rootData, "identity", identity);
        SetPrivateField(rootData, "status", status);
        SetPrivateField(rootData, "damage", damage);
        SetPrivateField(rootData, "receivedDamage", new HWJ_ReceivedDamageData());
        SetPrivateField(rootData, "selectedTypeData", typeData);
        return rootData;
    }

    private static HWJ_PlayerTypeDataSO CreatePlayerTypeData(
        bool canPossess,
        float maxDecayValue = 10f,
        float hitDecayPenalty = 1f,
        bool startAsSoul = true,
        float basicAttackDecayAmount = 0f,
        float skillDecayAmount = 0f)
    {
        HWJ_PlayerTypeDataSO playerData = ScriptableObject.CreateInstance<HWJ_PlayerTypeDataSO>();
        SetPrivateField(playerData, "objectType", HWJ_ObjectType.Player);
        SetPrivateField(playerData.Possession, "canPossess", canPossess);
        SetPrivateField(playerData.BodyDecay, "maxDecayValue", maxDecayValue);
        SetPrivateField(playerData.BodyDecay, "hitDecayPenalty", hitDecayPenalty);
        SetPrivateField(playerData.BodyDecay, "basicAttackDecayAmount", basicAttackDecayAmount);
        SetPrivateField(playerData.BodyDecay, "skillDecayAmount", skillDecayAmount);
        SetPrivateField(playerData.SoulState, "startAsSoul", startAsSoul);
        return playerData;
    }

    private static HWJ_EnemyTypeDataSO CreateEnemyTypeData(bool canBePossessed)
    {
        HWJ_EnemyTypeDataSO enemyData = ScriptableObject.CreateInstance<HWJ_EnemyTypeDataSO>();
        SetPrivateField(enemyData, "objectType", HWJ_ObjectType.Enemy);
        SetPrivateField(enemyData.PossessionBody, "canBePossessed", canBePossessed);
        SetPrivateField(enemyData.PossessionBody, "requiresDefeatedState", true);
        SetPrivateField(enemyData.PossessionBody, "loadsBodyStatsToPlayer", true);
        return enemyData;
    }

    private static HWJ_BossTypeDataSO CreateBossTypeData()
    {
        HWJ_BossTypeDataSO bossData = ScriptableObject.CreateInstance<HWJ_BossTypeDataSO>();
        SetPrivateField(bossData, "objectType", HWJ_ObjectType.Boss);
        SetPrivateField(bossData.EntryRequirements, "minimumCurrentHp", 1f);
        SetPrivateField(bossData.EntryRequirements, "maximumCurrentDecayRatio", 1f);
        return bossData;
    }

    private static HWJ_SkillActionDataSO CreateSkillActionData(
        string skillActionId,
        HWJ_SkillActionType actionType)
    {
        HWJ_SkillActionDataSO skillAction = ScriptableObject.CreateInstance<HWJ_SkillActionDataSO>();
        SetPrivateField(skillAction, "skillActionId", skillActionId);
        SetPrivateField(skillAction, "actionType", actionType);
        return skillAction;
    }

    private static HWJ_LevelUpDataSO CreateLevelUpData(
        string tableId,
        int maxLevel,
        int skillPointPerLevel,
        int[] experienceToNextLevel)
    {
        HWJ_LevelUpDataSO levelData = ScriptableObject.CreateInstance<HWJ_LevelUpDataSO>();
        SetPrivateField(levelData, "tableId", tableId);
        SetPrivateField(levelData, "maxLevel", maxLevel);
        SetPrivateField(levelData, "skillPointPerLevel", skillPointPerLevel);
        SetPrivateField(levelData, "experienceToNextLevel", experienceToNextLevel);
        return levelData;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        System.Type type = target.GetType();

        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            type = type.BaseType;
        }

        Assert.NotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
