using System.Collections;
using System.Collections.Generic;
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
    public IEnumerator RuntimeStatusSystem_LimitsConsecutiveHitReactionsWithoutBlockingDamage()
    {
        GameObject target = CreateCombatObject(
            "HitReactionLimitedEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_ReceivedDamageData reactionData = new HWJ_ReceivedDamageData
        {
            hitStunSeconds = 0.05f,
            maxHitReactionsPerWindow = 1,
            hitReactionWindowSeconds = 0.2f,
            hitReactionLimitImmuneSeconds = 0.15f
        };
        HWJ_DamageData sourceDamage = new HWJ_DamageData
        {
            hitStunSeconds = 0.05f,
            sameTargetHitCooldownSeconds = 0f
        };

        SetPrivateField(targetResolver.RootObjectData, "receivedDamage", reactionData);

        yield return null;

        targetStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(19f, targetStatus.CurrentHp, 0.001f);
        Assert.IsTrue(targetStatus.IsHitStunned);

        yield return new WaitForSeconds(0.07f);

        Assert.IsFalse(targetStatus.IsHitStunned);
        targetStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(18f, targetStatus.CurrentHp, 0.001f);
        Assert.IsFalse(targetStatus.IsHitStunned);
        Assert.IsTrue(targetStatus.IsHitReactionLimited);
        Assert.IsTrue(targetStatus.ShouldIgnoreKnockback);

        yield return new WaitForSeconds(0.25f);

        Assert.IsFalse(targetStatus.IsHitReactionLimited);
        targetStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(17f, targetStatus.CurrentHp, 0.001f);
        Assert.IsTrue(targetStatus.IsHitStunned);

        Object.Destroy(target);
    }

    [UnityTest]
    public IEnumerator RuntimeStatusSystem_SuperArmorBlocksHitStunButKeepsDamage()
    {
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        SetPrivateField(enemyData.State, "hasSuperArmor", true);
        GameObject target = CreateCombatObject(
            "SuperArmorEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            enemyData);
        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_DamageData sourceDamage = new HWJ_DamageData
        {
            hitStunSeconds = 0.2f,
            sameTargetHitCooldownSeconds = 0f
        };

        yield return null;

        targetStatus.ApplyDamage(3f, null, sourceDamage);

        Assert.AreEqual(17f, targetStatus.CurrentHp, 0.001f);
        Assert.IsTrue(targetStatus.HasSuperArmor);
        Assert.IsFalse(targetStatus.IsHitStunned);
        Assert.AreNotEqual(HWJ_RuntimeState.Hit, targetStatus.CurrentState);

        Object.Destroy(target);
    }

    [UnityTest]
    public IEnumerator BossBrain_GroggyCountsSuperArmorHitsWithoutApplyingHitStun()
    {
        HWJ_BossTypeDataSO bossData = CreateBossTypeData();
        SetPrivateField(bossData.FSM, "superArmorDuringAttack", true);
        SetPrivateField(bossData.FSM, "countGroggyHitsDuringSuperArmor", true);
        SetPrivateField(bossData.FSM, "groggyHitCountThreshold", 2);
        SetPrivateField(bossData.FSM, "groggyHitWindowSeconds", 1f);
        SetPrivateField(bossData.FSM, "groggyDurationSeconds", 0.5f);
        GameObject boss = CreateCombatObject(
            "GroggySuperArmorBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            bossData);
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_RuntimeStatusSystem bossStatus = boss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RootObjectDataResolver bossResolver = boss.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_DamageData sourceDamage = new HWJ_DamageData
        {
            hitStunSeconds = 0.2f,
            sameTargetHitCooldownSeconds = 0f
        };

        SetPrivateField(bossBrain, "dataResolver", bossResolver);
        SetPrivateField(bossBrain, "runtimeStatus", bossStatus);
        SetPrivateField(bossBrain, "currentState", HWJ_BossFSMState.Attack);
        SetPrivateField(bossStatus, "bossBrain", bossBrain);

        bossStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(19f, bossStatus.CurrentHp, 0.001f);
        Assert.IsFalse(bossStatus.IsHitStunned);
        Assert.AreEqual(HWJ_BossFSMState.Attack, bossBrain.CurrentState);

        bossStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(18f, bossStatus.CurrentHp, 0.001f);
        Assert.AreEqual(HWJ_BossFSMState.Groggy, bossBrain.CurrentState);
        Assert.IsFalse(bossStatus.IsHitStunned);

        Object.Destroy(boss);
        Object.Destroy(bossData);
        yield break;
    }

    [UnityTest]
    public IEnumerator BossBrain_GroggyClearsHitReactionLimitAndSkipsNormalHitReaction()
    {
        HWJ_BossTypeDataSO bossData = CreateBossTypeData();
        SetPrivateField(bossData.FSM, "groggyHitCountThreshold", 2);
        SetPrivateField(bossData.FSM, "groggyHitWindowSeconds", 1f);
        SetPrivateField(bossData.FSM, "groggyDurationSeconds", 0.5f);
        SetPrivateField(bossData.FSM, "clearHitReactionLimitOnGroggy", true);
        GameObject boss = CreateCombatObject(
            "GroggyReactionLimitBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            bossData);
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_RuntimeStatusSystem bossStatus = boss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_RootObjectDataResolver bossResolver = boss.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_ReceivedDamageData reactionData = new HWJ_ReceivedDamageData
        {
            hitStunSeconds = 0.02f,
            maxHitReactionsPerWindow = 1,
            hitReactionWindowSeconds = 1f,
            hitReactionLimitImmuneSeconds = 0.5f
        };
        HWJ_DamageData sourceDamage = new HWJ_DamageData
        {
            hitStunSeconds = 0.02f,
            sameTargetHitCooldownSeconds = 0f
        };

        SetPrivateField(bossResolver.RootObjectData, "receivedDamage", reactionData);
        SetPrivateField(bossBrain, "dataResolver", bossResolver);
        SetPrivateField(bossBrain, "runtimeStatus", bossStatus);
        SetPrivateField(bossBrain, "currentState", HWJ_BossFSMState.Idle);
        SetPrivateField(bossStatus, "bossBrain", bossBrain);

        yield return null;

        bossStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.IsTrue(bossStatus.IsHitStunned);

        yield return new WaitForSeconds(0.04f);

        bossStatus.ApplyDamage(1f, null, sourceDamage);
        Assert.AreEqual(HWJ_BossFSMState.Groggy, bossBrain.CurrentState);
        Assert.IsFalse(bossStatus.IsHitReactionLimited);
        Assert.IsFalse(bossStatus.ShouldIgnoreKnockback);

        Object.Destroy(boss);
        Object.Destroy(bossData);
    }

    [UnityTest]
    public IEnumerator RuntimeStatusSystem_HitStunImmunityBlocksStunButKeepsDamage()
    {
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        SetPrivateField(enemyData.State, "immuneToHitStun", true);
        GameObject target = CreateCombatObject(
            "HitStunImmuneEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            enemyData);
        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_DamageData sourceDamage = new HWJ_DamageData
        {
            hitStunSeconds = 0.2f,
            sameTargetHitCooldownSeconds = 0f
        };

        yield return null;

        targetStatus.ApplyDamage(3f, null, sourceDamage);

        Assert.AreEqual(17f, targetStatus.CurrentHp, 0.001f);
        Assert.IsFalse(targetStatus.HasSuperArmor);
        Assert.IsFalse(targetStatus.IsHitStunned);
        Assert.AreEqual(HWJ_RuntimeState.Hit, targetStatus.CurrentState);
        Assert.IsTrue(targetStatus.CanMove);

        Object.Destroy(target);
    }

    [UnityTest]
    public IEnumerator CombatSystem_KnockbackWeightReducesAppliedVelocity()
    {
        GameObject source = CreateCombatObject(
            "KnockbackSource",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_CombatSystem sourceCombat = source.GetComponent<HWJ_CombatSystem>();
        HWJ_RootObjectDataResolver sourceResolver = source.GetComponent<HWJ_RootObjectDataResolver>();
        sourceResolver.Damage.knockbackPower = 10f;
        sourceResolver.Damage.hitStunSeconds = 0.2f;
        sourceResolver.Damage.sameTargetHitCooldownSeconds = 0f;

        GameObject lightTarget = CreateCombatObject(
            "LightKnockbackTarget",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        GameObject heavyTarget = CreateCombatObject(
            "HeavyKnockbackTarget",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            20f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        Rigidbody2D lightBody = lightTarget.AddComponent<Rigidbody2D>();
        Rigidbody2D heavyBody = heavyTarget.AddComponent<Rigidbody2D>();
        lightBody.gravityScale = 0f;
        heavyBody.gravityScale = 0f;
        lightTarget.transform.position = Vector3.right;
        heavyTarget.transform.position = Vector3.right;
        lightTarget.GetComponent<HWJ_RootObjectDataResolver>().Status.bodyWeight = 1f;
        heavyTarget.GetComponent<HWJ_RootObjectDataResolver>().Status.bodyWeight = 4f;

        yield return null;

        Assert.IsTrue(sourceCombat.TryDealDamageTo(lightTarget.GetComponent<HWJ_RootObjectDataResolver>(), out _));
        Assert.IsTrue(sourceCombat.TryDealDamageTo(heavyTarget.GetComponent<HWJ_RootObjectDataResolver>(), out _));

        yield return new WaitForFixedUpdate();

        Assert.Greater(Mathf.Abs(lightBody.linearVelocity.x), Mathf.Abs(heavyBody.linearVelocity.x));
        Assert.Greater(Mathf.Abs(heavyBody.linearVelocity.x), 0f);

        Object.Destroy(source);
        Object.Destroy(lightTarget);
        Object.Destroy(heavyTarget);
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
    public IEnumerator SaveMigrationService_MigratesPrototypeSchemaToCurrent()
    {
        HWJ_GameSaveData legacySaveData = CreateValidSaveDataForValidation("migration.legacy");
        legacySaveData.schemaVersion = 0;
        legacySaveData.player.growth.unlockedSkillIds = null;
        legacySaveData.progression.defeatedEnemyRewardIds = null;

        yield return null;

        HWJ_SaveMigrationResult migrationResult = HWJ_SaveMigrationService.MigrateToCurrent(legacySaveData);

        Assert.IsTrue(migrationResult.Succeeded, migrationResult.Message);
        Assert.IsTrue(migrationResult.MigrationRequired);
        Assert.IsTrue(migrationResult.Migrated);
        Assert.AreEqual(HWJ_SaveMigrationFailureCode.None, migrationResult.FailureCode);
        Assert.AreEqual(0, migrationResult.SourceVersion);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, migrationResult.TargetVersion);
        Assert.AreSame(legacySaveData, migrationResult.SaveData);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, legacySaveData.schemaVersion);
        Assert.NotNull(legacySaveData.player.growth.unlockedSkillIds);
        Assert.NotNull(legacySaveData.progression.defeatedEnemyRewardIds);
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
    public IEnumerator SaveDataRuntimeValidator_AcceptsValidCurrentSaveData()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.valid");

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsTrue(validationResult.Succeeded, validationResult.Message);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.None, validationResult.FailureCode);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsHpAboveMaximum()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.hp");
        saveData.player.stats.currentHp = 11f;
        saveData.player.stats.maxHp = 10f;

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.InvalidHpRange, validationResult.FailureCode);
        Assert.AreEqual("player.stats.currentHp", validationResult.FieldName);
        StringAssert.Contains("Current value cannot exceed max value", validationResult.Message);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsPossessedStateWithoutActiveBody()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.possessed.conflict");
        saveData.player.body.playerExistenceState = HWJ_PlayerExistenceState.Possessed;
        saveData.player.body.hasActivePossessedBody = false;
        saveData.player.body.possessedBodyDefinitionDataId = string.Empty;

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.PossessedBodyStateConflict, validationResult.FailureCode);
        Assert.AreEqual("player.body.hasActivePossessedBody", validationResult.FieldName);
        StringAssert.Contains("Possessed", validationResult.Message);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsDuplicateInputBindingOverrides()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.input.duplicate");
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Jump,
            hasKeyboard = true,
            keyboardKeyCode = (int)KeyCode.Z,
            hasMouse = false,
            mouseButton = HWJ_InputMouseButton.None
        });
        saveData.settings.inputBindings.bindingOverrides.Add(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Jump,
            hasKeyboard = true,
            keyboardKeyCode = (int)KeyCode.X,
            hasMouse = false,
            mouseButton = HWJ_InputMouseButton.None
        });

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.DuplicateInputBinding, validationResult.FailureCode);
        Assert.AreEqual("settings.inputBindings.bindingOverrides[1].actionId", validationResult.FieldName);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsDuplicateInputBindingKeys()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.input.duplicate.keys");
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Jump,
            hasKeyboard = true,
            keyboardKeyCode = (int)KeyCode.Z,
            hasMouse = false,
            mouseButton = HWJ_InputMouseButton.None
        });
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Dash,
            hasKeyboard = true,
            keyboardKeyCode = (int)KeyCode.Z,
            hasMouse = false,
            mouseButton = HWJ_InputMouseButton.None
        });

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.DuplicateInputBinding, validationResult.FailureCode);
        Assert.AreEqual("settings.inputBindings.bindingOverrides[1].keyboardKeyCode", validationResult.FieldName);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsDuplicateInputBindingMouseButtons()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.input.duplicate.mouse");
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Attack,
            hasKeyboard = false,
            keyboardKeyCode = 0,
            hasMouse = true,
            mouseButton = HWJ_InputMouseButton.Right
        });
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Interact,
            hasKeyboard = false,
            keyboardKeyCode = 0,
            hasMouse = true,
            mouseButton = HWJ_InputMouseButton.Right
        });

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.DuplicateInputBinding, validationResult.FailureCode);
        Assert.AreEqual("settings.inputBindings.bindingOverrides[1].mouseButton", validationResult.FieldName);
    }

    [UnityTest]
    public IEnumerator SaveDataRuntimeValidator_DetectsEmptyKeyboardInputBinding()
    {
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation("validation.input.empty.keyboard");
        saveData.settings.inputBindings.AddOrReplace(new HWJ_SaveInputBindingOverrideData
        {
            actionId = HWJ_PlayerInputActionId.Jump,
            hasKeyboard = true,
            keyboardKeyCode = (int)KeyCode.None,
            hasMouse = false,
            mouseButton = HWJ_InputMouseButton.None
        });

        yield return null;

        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        Assert.IsFalse(validationResult.Succeeded);
        Assert.AreEqual(HWJ_SaveDataValidationFailureCode.InvalidInputBinding, validationResult.FailureCode);
        Assert.AreEqual("settings.inputBindings.bindingOverrides[0].keyboardKeyCode", validationResult.FieldName);
    }

    [UnityTest]
    public IEnumerator SaveService_SaveDataRejectsInvalidRuntimeDtoWithFieldCode()
    {
        string saveSlotId = "playmode_invalid_save_validation_" + System.Guid.NewGuid().ToString("N");
        GameObject saveObject = new GameObject("InvalidRuntimeSaveValidationService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        string saveFilePath = saveService.GetSaveFilePath(saveSlotId);
        HWJ_GameSaveData saveData = CreateValidSaveDataForValidation(saveSlotId);
        saveData.progression.unlockedRegionIds.Clear();
        saveData.progression.unlockedRegionIds.Add("region.01");
        saveData.progression.unlockedRegionIds.Add("region.01");

        yield return null;

        HWJ_SaveOperationResult saveResult = saveService.SaveData(saveData, saveSlotId);

        Assert.IsFalse(saveResult.Succeeded);
        Assert.AreEqual(HWJ_SaveOperationFailureCode.InvalidSaveData, saveResult.FailureCode);
        StringAssert.Contains("DuplicateStableId", saveResult.Message);
        StringAssert.Contains("progression.unlockedRegionIds[1]", saveResult.Message);
        Assert.IsFalse(File.Exists(saveFilePath));

        saveObject.SetActive(false);
        Object.Destroy(saveObject);
    }

    [UnityTest]
    public IEnumerator SaveDataMappingContract_CoversRuntimeSnapshotFields()
    {
        yield return null;

        AssertSnapshotFieldsCoveredBySaveDto<HWJ_RuntimeStatSnapshot, HWJ_SaveRuntimeStatData>();
        AssertSnapshotFieldsCoveredBySaveDto<HWJ_RuntimeBodySnapshot, HWJ_SaveBodyRuntimeData>();
        AssertSnapshotFieldsCoveredBySaveDto<HWJ_RuntimeStageFlowSnapshot, HWJ_SaveStageRuntimeData>();
        AssertSnapshotFieldsCoveredBySaveDto<HWJ_RuntimeGrowthSnapshot, HWJ_SaveGrowthRuntimeData>("unlockedSkillIds");
        AssertRuntimeObjectSnapshotMappingContract();
    }

    [UnityTest]
    public IEnumerator SaveService_LoadDataMigratesPrototypeSchemaThroughMigrationService()
    {
        string saveSlotId = "playmode_legacy_migration_" + System.Guid.NewGuid().ToString("N");
        GameObject saveObject = new GameObject("LegacyMigrationSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        string saveFilePath = saveService.GetSaveFilePath(saveSlotId);
        string directoryPath = Path.GetDirectoryName(saveFilePath);
        HWJ_GameSaveData legacySaveData = CreateValidSaveDataForValidation(saveSlotId);
        legacySaveData.schemaVersion = 0;
        legacySaveData.player.growth.unlockedSkillIds = null;
        legacySaveData.progression.defeatedEnemyRewardIds = null;

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(legacySaveData, true));

        yield return null;

        HWJ_SaveOperationResult loadResult = saveService.LoadData(
            out HWJ_GameSaveData loadedSaveData,
            saveSlotId);

        Assert.IsTrue(loadResult.Succeeded, loadResult.Message);
        Assert.AreEqual(HWJ_SaveOperationFailureCode.None, loadResult.FailureCode);
        Assert.NotNull(loadedSaveData);
        Assert.AreEqual(HWJ_SaveSchema.CurrentVersion, loadedSaveData.schemaVersion);
        Assert.NotNull(loadedSaveData.player.growth.unlockedSkillIds);
        Assert.NotNull(loadedSaveData.progression.defeatedEnemyRewardIds);

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
    public IEnumerator BodyDecaySystem_DecreasesPossessedBodyHpOnTimeDecayTick()
    {
        GameObject player = CreatePlayerObject("PlayerTimeDecayHp", true, 100f, 1f);
        HWJ_PlayerTypeDataSO playerData = player.GetComponent<HWJ_RootObjectDataResolver>().TypeData as HWJ_PlayerTypeDataSO;
        playerData.BodyDecay.decayTickSeconds = 0.01f;
        playerData.BodyDecay.decayAmountPerTick = 5f;
        GameObject enemy = CreateCombatObject(
            "TimeDecayBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            100f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(999f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        Assert.IsTrue(possession.TryGetPossessedBodyRuntimeState(out HWJ_PossessedBodyRuntimeState bodyState));
        Assert.AreEqual(100f, bodyState.CurrentHp, 0.001f);

        yield return new WaitForSeconds(0.05f);

        Assert.Greater(player.GetComponent<HWJ_BodyDecaySystem>().CurrentDecayValue, 0f);
        Assert.Less(bodyState.CurrentHp, 100f);
        Assert.AreEqual(bodyState.CurrentHp, player.GetComponent<HWJ_RuntimeStatusSystem>().CurrentHp, 0.001f);

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
    public IEnumerator PlayerAttackSystem_UsesSkillActionDecayBalanceWhenConfigured()
    {
        GameObject player = CreatePlayerObject("PlayerSkillActionDecayBalance", true, 10f, 1f, true, 1.5f, 2.5f);
        HWJ_SkillActionSystem skillActionSystem = player.AddComponent<HWJ_SkillActionSystem>();
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.custom_decay",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillAction, "usesCustomBodyDecayAmount", true);
        SetPrivateField(skillAction, "customBodyDecayAmount", 4f);
        SetPrivateField(skillAction, "additionalBodyDecayAmount", 0.5f);
        SetPrivateField(skillActionSystem, "localSkillActions", new[] { skillAction });
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        enemyData.SkillCycle.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.custom_decay",
                startsUnlocked = true
            }
        };
        GameObject enemy = CreateCombatObject(
            "CustomDecaySkillBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            enemyData);

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_BodyDecaySystem bodyDecay = player.GetComponent<HWJ_BodyDecaySystem>();
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        bodyDecay.RestoreDecaySnapshot(1f);

        Assert.IsTrue(attackSystem.TryPossessedSkillSlot(0));
        Assert.AreEqual(5.5f, bodyDecay.CurrentDecayValue, 0.001f);

        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator BodyDecaySystem_AppliesSkillActionComboAndChargeDecay()
    {
        GameObject player = CreatePlayerObject("PlayerSkillActionComboChargeDecay", true, 20f, 1f, true, 0f, 2f);
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.combo_charge_decay",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillAction, "additionalBodyDecayAmount", 0.5f);
        SetPrivateField(skillAction, "usesComboBodyDecayMultiplier", true);
        SetPrivateField(skillAction, "comboBodyDecayMultiplier", 2f);
        SetPrivateField(skillAction, "chargeBodyDecayPerSecond", 1f);
        SetPrivateField(skillAction, "maxChargeBodyDecayAmount", 1.5f);
        GameObject enemy = CreateCombatObject(
            "ComboChargeDecayBody",
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
        bodyDecay.RestoreDecaySnapshot(1f);

        bodyDecay.ApplySkillDecay(skillAction, 2f);

        Assert.AreEqual(7.5f, bodyDecay.CurrentDecayValue, 0.001f);

        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_PassesComboStepAndChargeSecondsToBodyDecay()
    {
        GameObject player = CreatePlayerObject("PlayerAttackComboChargeDecay", true, 30f, 1f, true, 2f, 0f);
        Assert.IsTrue(player.GetComponent<HWJ_RootObjectDataResolver>().TryGetTypeData(out HWJ_PlayerTypeDataSO playerData));
        playerData.Attack.attackIntervalSeconds = 0f;
        playerData.Attack.comboResetSeconds = 1f;

        HWJ_SkillActionSystem skillActionSystem = player.AddComponent<HWJ_SkillActionSystem>();
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.basic_combo_charge_decay",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillAction, "additionalBodyDecayAmount", 0.5f);
        SetPrivateField(skillAction, "usesComboBodyDecayMultiplier", true);
        SetPrivateField(skillAction, "comboBodyDecayMultiplier", 2f);
        SetPrivateField(skillAction, "chargeBodyDecayPerSecond", 1f);
        SetPrivateField(skillAction, "maxChargeBodyDecayAmount", 1f);
        SetPrivateField(skillActionSystem, "localSkillActions", new[] { skillAction });

        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        enemyData.SkillCycle.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.basic_combo_charge_decay",
                startsUnlocked = true
            }
        };
        GameObject enemy = CreateCombatObject(
            "BasicComboChargeDecayBody",
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

        Assert.IsTrue(attackSystem.TryBasicAttack());
        Assert.AreEqual(3.5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(1, attackSystem.CurrentBasicAttackComboStep);
        Assert.AreEqual(1, abilityUsedCount);
        Assert.AreEqual(1, lastAbilityEvent.ComboStep);
        Assert.AreEqual(0f, lastAbilityEvent.ChargeSeconds, 0.001f);

        Assert.IsTrue(attackSystem.TryBasicAttack(2f));
        Assert.AreEqual(9.5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(2, attackSystem.CurrentBasicAttackComboStep);
        Assert.AreEqual(2f, attackSystem.LastResolvedAttackChargeSeconds, 0.001f);
        Assert.AreEqual(2, abilityUsedCount);
        Assert.AreEqual(2, lastAbilityEvent.ComboStep);
        Assert.AreEqual(2f, lastAbilityEvent.ChargeSeconds, 0.001f);
        Assert.AreEqual(9.5f, lastAbilityEvent.DecayValueAfterUse, 0.001f);

        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_ReleasesChargedBasicAttackWithCappedChargeSeconds()
    {
        GameObject player = CreatePlayerObject("PlayerAttackChargeReleaseDecay", true, 30f, 1f, true, 2f, 0f);
        Assert.IsTrue(player.GetComponent<HWJ_RootObjectDataResolver>().TryGetTypeData(out HWJ_PlayerTypeDataSO playerData));
        playerData.Attack.attackIntervalSeconds = 0f;
        playerData.Attack.comboResetSeconds = 1f;

        HWJ_SkillActionSystem skillActionSystem = player.AddComponent<HWJ_SkillActionSystem>();
        HWJ_SkillActionDataSO skillAction = CreateSkillActionData(
            "skill.test.basic_charge_release_decay",
            HWJ_SkillActionType.Buff);
        SetPrivateField(skillAction, "additionalBodyDecayAmount", 0.5f);
        SetPrivateField(skillAction, "chargeBodyDecayPerSecond", 1f);
        SetPrivateField(skillAction, "maxChargeBodyDecayAmount", 1.5f);
        SetPrivateField(skillActionSystem, "localSkillActions", new[] { skillAction });

        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        SetPrivateField(attackSystem, "maxBasicAttackChargeSeconds", 1.5f);
        HWJ_EnemyTypeDataSO enemyData = CreateEnemyTypeData(true);
        enemyData.SkillCycle.skills = new[]
        {
            new HWJ_SkillEntryData
            {
                skillId = "skill.test.basic_charge_release_decay",
                startsUnlocked = true
            }
        };
        GameObject enemy = CreateCombatObject(
            "BasicChargeReleaseDecayBody",
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

        Assert.IsTrue(attackSystem.BeginBasicAttackCharge());
        SetPrivateField(attackSystem, "basicAttackChargeStartTime", Time.time - 3f);

        Assert.IsTrue(attackSystem.ReleaseBasicAttackCharge());
        Assert.IsFalse(attackSystem.IsChargingBasicAttack);
        Assert.AreEqual(5f, bodyDecay.CurrentDecayValue, 0.001f);
        Assert.AreEqual(1.5f, attackSystem.LastResolvedAttackChargeSeconds, 0.001f);
        Assert.AreEqual(1, abilityUsedCount);
        Assert.AreEqual(1, lastAbilityEvent.ComboStep);
        Assert.AreEqual(1.5f, lastAbilityEvent.ChargeSeconds, 0.001f);
        Assert.AreEqual(5f, lastAbilityEvent.DecayValueAfterUse, 0.001f);

        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(skillAction);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_BlocksChargeStartWithoutPossessedBody()
    {
        GameObject player = CreatePlayerObject("PlayerChargeWithoutBody", true, 30f, 1f, true, 2f, 0f);
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();

        yield return null;

        Assert.IsFalse(attackSystem.BeginBasicAttackCharge());
        Assert.IsFalse(attackSystem.IsChargingBasicAttack);
        Assert.AreEqual("Charge requires a possessed body.", attackSystem.LastAttackResult);

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_CancelsChargeWhenAttackBecomesLocked()
    {
        GameObject player = CreatePlayerObject("PlayerChargeAttackLocked", true, 30f, 1f, true, 2f, 0f);
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        GameObject enemy = CreateCombatObject(
            "ChargeLockedBody",
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
        Assert.IsTrue(possession.TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        Assert.IsTrue(attackSystem.BeginBasicAttackCharge());

        player.GetComponent<HWJ_RuntimeStatusSystem>().LockAttack(1f);
        yield return null;

        Assert.IsFalse(attackSystem.IsChargingBasicAttack);
        Assert.AreEqual("Basic attack charge cancelled: attack is locked.", attackSystem.LastAttackResult);

        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator PlayerAttackSystem_RaisesChargeEventAndUpdatesGaugeRatio()
    {
        GameObject player = CreatePlayerObject("PlayerChargeGauge", true, 30f, 1f, true, 2f, 0f);
        HWJ_PlayerAttackSystem attackSystem = player.AddComponent<HWJ_PlayerAttackSystem>();
        SetPrivateField(attackSystem, "maxBasicAttackChargeSeconds", 2f);
        GameObject enemy = CreateCombatObject(
            "ChargeGaugeBody",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            1f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        int chargeEventCount = 0;
        HWJ_BasicAttackChargeEvent lastChargeEvent = default(HWJ_BasicAttackChargeEvent);

        void OnChargeChanged(HWJ_BasicAttackChargeEvent chargeEvent)
        {
            if (chargeEvent.AttackSystem != attackSystem)
            {
                return;
            }

            chargeEventCount++;
            lastChargeEvent = chargeEvent;
        }

        HWJ_GameplayEvents.BasicAttackChargeChanged += OnChargeChanged;

        yield return null;

        enemy.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        Assert.IsTrue(player.GetComponent<HWJ_PossessionSystem>().TryPossess(enemy.GetComponent<HWJ_RootObjectDataResolver>()));

        GameObject gaugeObject = new GameObject("ChargeGauge");
        GameObject fillObject = new GameObject("ChargeGaugeFill");
        fillObject.transform.SetParent(gaugeObject.transform);
        fillObject.transform.localScale = Vector3.one;
        HWJ_ChargeGaugeSystem gaugeSystem = gaugeObject.AddComponent<HWJ_ChargeGaugeSystem>();
        SetPrivateField(gaugeSystem, "playerAttackSystem", attackSystem);
        SetPrivateField(gaugeSystem, "fillRoot", fillObject.transform);

        Assert.IsTrue(attackSystem.BeginBasicAttackCharge());
        Assert.AreEqual(1, chargeEventCount);
        Assert.IsTrue(lastChargeEvent.IsCharging);
        Assert.AreEqual(0f, lastChargeEvent.ChargeRatio, 0.001f);

        SetPrivateField(attackSystem, "basicAttackChargeStartTime", Time.time - 1f);
        yield return null;

        Assert.AreEqual(0.5f, attackSystem.CurrentBasicAttackChargeRatio, 0.05f);
        Assert.AreEqual(0.5f, gaugeSystem.CurrentRatio, 0.05f);
        Assert.AreEqual(0.5f, fillObject.transform.localScale.x, 0.05f);

        attackSystem.CancelCurrentAttack();
        Assert.AreEqual(2, chargeEventCount);
        Assert.IsFalse(lastChargeEvent.IsCharging);
        Assert.GreaterOrEqual(lastChargeEvent.ChargeRatio, 0.45f);

        HWJ_GameplayEvents.BasicAttackChargeChanged -= OnChargeChanged;
        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(gaugeObject);
    }

    [UnityTest]
    public IEnumerator PlayerInputBindingData_ProvidesDefaultKeyboardAndMouseBindings()
    {
        HWJ_PlayerInputBindingDataSO bindingData = ScriptableObject.CreateInstance<HWJ_PlayerInputBindingDataSO>();

        Assert.AreEqual(13, bindingData.BindingCount);
        Assert.IsTrue(bindingData.TryGetBindingAt(0, out HWJ_PlayerInputBindingEntry firstBinding));
        Assert.AreEqual(HWJ_PlayerInputActionId.MoveLeft, firstBinding.ActionId);
        Assert.IsTrue(bindingData.TryGetBinding(
            HWJ_PlayerInputActionId.Attack,
            out HWJ_PlayerInputBindingEntry attackBinding));
        Assert.AreEqual(HWJ_InputMouseButton.Left, attackBinding.MouseButton);
        Assert.AreEqual(KeyCode.None, attackBinding.KeyboardKey);
        Assert.AreEqual(KeyCode.Space, bindingData.GetKeyboardKey(HWJ_PlayerInputActionId.Jump, KeyCode.None));
        Assert.AreEqual(
            HWJ_InputMouseButton.Left,
            bindingData.GetMouseButton(HWJ_PlayerInputActionId.Attack, HWJ_InputMouseButton.None));
        Assert.AreEqual(KeyCode.Alpha4, bindingData.GetKeyboardKey(HWJ_PlayerInputActionId.SkillSlot4, KeyCode.None));

        Object.Destroy(bindingData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerInputSystem_UsesRuntimeBindingsWithoutMutatingSourceData()
    {
        GameObject inputObject = new GameObject("PlayerInputRuntimeBinding");
        HWJ_PlayerInputSystem inputSystem = inputObject.AddComponent<HWJ_PlayerInputSystem>();
        HWJ_PlayerInputBindingDataSO bindingData = ScriptableObject.CreateInstance<HWJ_PlayerInputBindingDataSO>();
        inputSystem.SetInputBindingData(bindingData);

        Assert.IsTrue(inputSystem.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Jump,
            out HWJ_PlayerInputRuntimeBinding defaultBinding));
        Assert.AreEqual(KeyCode.Space, defaultBinding.KeyboardKey);
        Assert.AreEqual(0, inputSystem.RuntimeBindingOverrideCount);

        Assert.IsTrue(inputSystem.SetKeyboardBinding(HWJ_PlayerInputActionId.Jump, KeyCode.Z));
        Assert.IsTrue(inputSystem.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Jump,
            out HWJ_PlayerInputRuntimeBinding runtimeBinding));
        Assert.AreEqual(KeyCode.Z, runtimeBinding.KeyboardKey);
        Assert.AreEqual(1, inputSystem.RuntimeBindingOverrideCount);

        Assert.AreEqual(
            KeyCode.Space,
            bindingData.GetKeyboardKey(HWJ_PlayerInputActionId.Jump, KeyCode.None),
            "Runtime rebinding must not change the ScriptableObject source data.");

        Assert.IsTrue(inputSystem.ClearRuntimeBinding(HWJ_PlayerInputActionId.Jump));
        Assert.IsTrue(inputSystem.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Jump,
            out HWJ_PlayerInputRuntimeBinding restoredBinding));
        Assert.AreEqual(KeyCode.Space, restoredBinding.KeyboardKey);
        Assert.AreEqual(0, inputSystem.RuntimeBindingOverrideCount);

        Object.Destroy(inputObject);
        Object.Destroy(bindingData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerInputSystem_ReturnsExplicitRebindFailureCodes()
    {
        GameObject inputObject = new GameObject("PlayerInputRebindFailureCodes");
        HWJ_PlayerInputSystem inputSystem = inputObject.AddComponent<HWJ_PlayerInputSystem>();

        HWJ_PlayerInputRebindResult invalidActionResult = inputSystem.TrySetKeyboardBinding(
            (HWJ_PlayerInputActionId)999,
            KeyCode.Z);
        Assert.IsFalse(invalidActionResult.Succeeded);
        Assert.AreEqual(HWJ_PlayerInputRebindFailureCode.InvalidActionId, invalidActionResult.FailureCode);

        HWJ_PlayerInputRebindResult emptyKeyboardResult = inputSystem.TrySetKeyboardBinding(
            HWJ_PlayerInputActionId.Jump,
            KeyCode.None);
        Assert.IsFalse(emptyKeyboardResult.Succeeded);
        Assert.AreEqual(HWJ_PlayerInputRebindFailureCode.EmptyKeyboardKey, emptyKeyboardResult.FailureCode);

        HWJ_PlayerInputRebindResult invalidKeyboardResult = inputSystem.TrySetKeyboardBinding(
            HWJ_PlayerInputActionId.Jump,
            (KeyCode)(-12345));
        Assert.IsFalse(invalidKeyboardResult.Succeeded);
        Assert.AreEqual(HWJ_PlayerInputRebindFailureCode.InvalidKeyboardKey, invalidKeyboardResult.FailureCode);

        HWJ_PlayerInputRebindResult duplicateKeyboardResult = inputSystem.TrySetKeyboardBinding(
            HWJ_PlayerInputActionId.Jump,
            KeyCode.LeftArrow);
        Assert.IsFalse(duplicateKeyboardResult.Succeeded);
        Assert.AreEqual(HWJ_PlayerInputRebindFailureCode.BindingAlreadyUsed, duplicateKeyboardResult.FailureCode);
        Assert.AreEqual(HWJ_PlayerInputActionId.MoveLeft, duplicateKeyboardResult.ConflictActionId);
        Assert.AreEqual(0, inputSystem.RuntimeBindingOverrideCount);

        inputSystem.SetAllowDuplicateRuntimeBindings(true);
        HWJ_PlayerInputRebindResult allowedDuplicateResult = inputSystem.TrySetKeyboardBinding(
            HWJ_PlayerInputActionId.Jump,
            KeyCode.LeftArrow);
        Assert.IsTrue(allowedDuplicateResult.Succeeded);
        Assert.AreEqual(HWJ_PlayerInputRebindFailureCode.None, allowedDuplicateResult.FailureCode);
        Assert.AreEqual(1, inputSystem.RuntimeBindingOverrideCount);

        Object.Destroy(inputObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerInputSystem_ExportsAndAppliesSaveInputBindings()
    {
        GameObject sourceObject = new GameObject("SourcePlayerInputSaveBinding");
        GameObject targetObject = new GameObject("TargetPlayerInputSaveBinding");
        HWJ_PlayerInputSystem sourceInput = sourceObject.AddComponent<HWJ_PlayerInputSystem>();
        HWJ_PlayerInputSystem targetInput = targetObject.AddComponent<HWJ_PlayerInputSystem>();

        Assert.IsTrue(sourceInput.SetKeyboardBinding(HWJ_PlayerInputActionId.Jump, KeyCode.Z));
        Assert.IsTrue(sourceInput.SetMouseBinding(HWJ_PlayerInputActionId.Attack, HWJ_InputMouseButton.Right));

        HWJ_SaveInputBindingData saveInputBindingData = sourceInput.CreateSaveInputBindingData();

        Assert.NotNull(saveInputBindingData);
        Assert.AreEqual(2, saveInputBindingData.OverrideCount);

        targetInput.ApplySaveInputBindingData(saveInputBindingData);

        Assert.IsTrue(targetInput.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Jump,
            out HWJ_PlayerInputRuntimeBinding restoredJumpBinding));
        Assert.AreEqual(KeyCode.Z, restoredJumpBinding.KeyboardKey);
        Assert.IsTrue(targetInput.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Attack,
            out HWJ_PlayerInputRuntimeBinding restoredAttackBinding));
        Assert.AreEqual(HWJ_InputMouseButton.Right, restoredAttackBinding.MouseButton);

        Object.Destroy(sourceObject);
        Object.Destroy(targetObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameManager_ReceivesPlayerInputRegistration()
    {
        GameObject managerObject = new GameObject("GameManagerInputRegistry");
        HWJ_GameManager manager = managerObject.AddComponent<HWJ_GameManager>();
        GameObject inputObject = new GameObject("RegisteredPlayerInput");
        HWJ_PlayerInputSystem inputSystem = inputObject.AddComponent<HWJ_PlayerInputSystem>();

        yield return null;

        Assert.AreSame(inputSystem, manager.PlayerInput);
        Assert.AreSame(inputSystem, HWJ_GameAccess.PlayerInput);

        Object.Destroy(inputObject);
        yield return null;

        Assert.IsNull(manager.PlayerInput);

        Object.Destroy(managerObject);
        yield return null;

        Assert.IsNull(HWJ_GameManager.Instance);
    }

    [UnityTest]
    public IEnumerator GameManager_AutoFindsScenePlayerAndAddsCoreSystems()
    {
        GameObject player = CreateCombatObject(
            "ScenePlacedPlayerWithoutCoreSystems",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_RootObjectDataResolver playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.IsNull(player.GetComponent<HWJ_SoulSystem>());
        Assert.IsNull(player.GetComponent<HWJ_PossessionSystem>());
        Assert.IsNull(player.GetComponent<HWJ_BodyDiscoverySystem>());
        Assert.IsNull(player.GetComponent<HWJ_CollapseSystem>());

        GameObject managerObject = new GameObject("GameManagerPlayerBootstrap");
        HWJ_GameManager manager = managerObject.AddComponent<HWJ_GameManager>();

        yield return null;

        Assert.AreSame(playerResolver, manager.PlayerResolver);
        Assert.NotNull(player.GetComponent<HWJ_SoulSystem>());
        Assert.NotNull(player.GetComponent<HWJ_PossessedBodySystem>());
        Assert.NotNull(player.GetComponent<HWJ_PossessionSystem>());
        Assert.NotNull(player.GetComponent<HWJ_BodyDecaySystem>());
        Assert.NotNull(player.GetComponent<HWJ_BodyDiscoverySystem>());
        Assert.NotNull(player.GetComponent<HWJ_CollapseSystem>());
        Assert.AreSame(player.GetComponent<HWJ_PossessionSystem>(), manager.PlayerPossession);
        Assert.AreSame(player.GetComponent<HWJ_BodyDecaySystem>(), manager.PlayerBodyDecay);

        Object.Destroy(managerObject);
        Object.Destroy(player);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SaveService_CapturesAndAppliesInputBindingOverrides()
    {
        GameObject sourcePlayer = CreatePlayerObject("SourceInputSavePlayer", true);
        HWJ_PlayerInputSystem sourceInput = sourcePlayer.AddComponent<HWJ_PlayerInputSystem>();
        Assert.IsTrue(sourceInput.SetKeyboardBinding(HWJ_PlayerInputActionId.Jump, KeyCode.Z));
        GameObject sourceSaveObject = new GameObject("SourceInputSaveService");
        HWJ_SaveService sourceSaveService = sourceSaveObject.AddComponent<HWJ_SaveService>();
        sourceSaveService.SetRuntimeSources(
            sourcePlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            null,
            null);

        yield return null;

        HWJ_SaveOperationResult captureResult = sourceSaveService.TryCreateCurrentSaveData(
            out HWJ_GameSaveData saveData,
            "input_binding_capture");

        Assert.IsTrue(captureResult.Succeeded, captureResult.Message);
        Assert.NotNull(saveData.settings);
        Assert.NotNull(saveData.settings.inputBindings);
        Assert.AreEqual(1, saveData.settings.inputBindings.OverrideCount);

        GameObject targetPlayer = CreatePlayerObject("TargetInputSavePlayer", true);
        HWJ_PlayerInputSystem targetInput = targetPlayer.AddComponent<HWJ_PlayerInputSystem>();
        GameObject targetSaveObject = new GameObject("TargetInputSaveService");
        HWJ_SaveService targetSaveService = targetSaveObject.AddComponent<HWJ_SaveService>();
        targetSaveService.SetRuntimeSources(
            targetPlayer.GetComponent<HWJ_RuntimeObjectContext>(),
            null,
            null);

        HWJ_SaveOperationResult applyResult = targetSaveService.ApplySaveDataToRuntime(saveData);

        Assert.IsTrue(applyResult.Succeeded, applyResult.Message);
        Assert.IsTrue(targetInput.TryGetEffectiveBinding(
            HWJ_PlayerInputActionId.Jump,
            out HWJ_PlayerInputRuntimeBinding restoredBinding));
        Assert.AreEqual(KeyCode.Z, restoredBinding.KeyboardKey);

        Object.Destroy(sourcePlayer);
        Object.Destroy(sourceSaveObject);
        Object.Destroy(targetPlayer);
        Object.Destroy(targetSaveObject);
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
    public IEnumerator StageEnemyCountSystem_CompletesObjectiveWhenAllMapEnemiesAreDead()
    {
        GameObject stageObject = new GameObject("StageEnemyCountGate");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        HWJ_StageEnemyCountSystem enemyCounter = stageObject.AddComponent<HWJ_StageEnemyCountSystem>();
        GameObject enemyA = CreateCombatObject(
            "StageEnemyCountA",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));
        GameObject enemyB = CreateCombatObject(
            "StageEnemyCountB",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateEnemyTypeData(true));

        stage.SetStageIds("stage.enemy.count", "region.enemy.count.next");
        SetPrivateField(enemyCounter, "stageProgressionSystem", stage);
        SetPrivateField(enemyCounter, "rescanIntervalSeconds", 0.01f);

        yield return null;

        enemyCounter.ForceScan("test_initial");
        Assert.AreEqual(2, enemyCounter.RemainingAliveEnemyCount);
        Assert.IsFalse(stage.ObjectiveComplete);
        Assert.AreEqual(HWJ_StageFlowState.Combat, stage.CurrentState);

        enemyA.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        Assert.AreEqual(1, enemyCounter.RemainingAliveEnemyCount);
        Assert.IsFalse(stage.ObjectiveComplete);

        enemyB.GetComponent<HWJ_RuntimeStatusSystem>().ApplyDamage(99f);
        yield return null;

        enemyCounter.ForceScan("test_final");
        Assert.AreEqual(0, enemyCounter.RemainingAliveEnemyCount);
        Assert.IsTrue(stage.ObjectiveComplete);
        Assert.IsTrue(enemyCounter.ObjectiveCompletedByThisSystem);
        Assert.AreEqual(HWJ_StageFlowState.ObjectiveComplete, stage.CurrentState);

        Object.Destroy(stageObject);
        Object.Destroy(enemyA);
        Object.Destroy(enemyB);
    }

    [UnityTest]
    public IEnumerator StageProgressionSystem_AppliesStageAndRegionDefinitionData()
    {
        GameObject stageObject = new GameObject("StageProgressionDefinitionApply");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        HWJ_StageDefinitionDataSO stageData = CreateStageDefinitionData(
            "stage.region01.boss",
            "region.region01",
            "stage.region01.clear",
            true,
            "boss.region01.guardian",
            true,
            "region.region02");
        HWJ_RegionDefinitionDataSO regionData = CreateRegionDefinitionData(
            "region.region01",
            "stage.region01.01",
            "region.region02",
            new[] { "stage.region01.01", "stage.region01.boss", "stage.region01.clear" });

        yield return null;

        HWJ_StageDefinitionApplyResult applyResult = stage.TryApplyStageDefinition(
            stageData,
            regionData,
            null,
            false,
            true);

        Assert.IsTrue(applyResult.Succeeded);
        Assert.AreEqual(HWJ_StageDefinitionApplyFailureCode.None, applyResult.FailureCode);
        Assert.AreEqual("stage.region01.boss", stage.StageId);
        Assert.AreEqual("region.region01", stage.CurrentRegionId);
        Assert.AreEqual("stage.region01.clear", stage.NextStageId);
        Assert.AreEqual("region.region02", stage.NextRegionId);
        Assert.IsTrue(stage.CurrentStageHasBoss);
        Assert.AreEqual("boss.region01.guardian", stage.BossId);
        Assert.IsTrue(stage.UnlocksRegionOnClear);
        Assert.AreEqual("region.region02", stage.UnlockRegionId);
        Assert.AreEqual(HWJ_StageFlowState.Entering, stage.CurrentState);

        Object.Destroy(stageObject);
        Object.Destroy(stageData);
        Object.Destroy(regionData);
    }

    [UnityTest]
    public IEnumerator StageProgressionSystem_RejectsDefinitionWhenProgressionRequirementMissing()
    {
        GameObject stageObject = new GameObject("StageProgressionDefinitionRequirement");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        HWJ_StageDefinitionDataSO stageData = CreateStageDefinitionData(
            "stage.region02.01",
            "region.region02",
            null,
            false,
            null,
            false,
            null,
            new[] { "stage.region01.boss" },
            new[] { "region.region02" });
        HWJ_SaveProgressionData progressionData = new HWJ_SaveProgressionData();

        yield return null;

        HWJ_StageDefinitionApplyResult blockedResult = stage.TryApplyStageDefinition(
            stageData,
            null,
            progressionData,
            true,
            false);

        Assert.IsFalse(blockedResult.Succeeded);
        Assert.AreEqual(HWJ_StageDefinitionApplyFailureCode.MissingClearedStageRequirement, blockedResult.FailureCode);
        Assert.AreEqual("stage.region01.boss", blockedResult.MissingRequirementId);
        Assert.AreEqual(HWJ_StageDefinitionApplyFailureCode.MissingClearedStageRequirement, stage.LastDefinitionApplyFailureCode);

        progressionData.AddClearedStageId("stage.region01.boss");
        progressionData.AddUnlockedRegionId("region.region02");

        HWJ_StageDefinitionApplyResult applyResult = stage.TryApplyStageDefinition(
            stageData,
            null,
            progressionData,
            true,
            false);

        Assert.IsTrue(applyResult.Succeeded);
        Assert.AreEqual("stage.region02.01", stage.StageId);
        Assert.AreEqual("region.region02", stage.CurrentRegionId);

        Object.Destroy(stageObject);
        Object.Destroy(stageData);
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
    public IEnumerator BossFlowSystem_UsesStageDefinitionForBossDefeatAndRegionUnlock()
    {
        GameObject saveObject = new GameObject("BossFlowDefinitionSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        GameObject stageObject = new GameObject("BossFlowDefinitionStage");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        HWJ_StageDefinitionDataSO stageData = CreateStageDefinitionData(
            "stage.region01.boss",
            "region.region01",
            null,
            true,
            "boss.region01.guardian",
            true,
            "region.region02");
        GameObject boss = CreateCombatObject(
            "BossFlowDefinitionBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            100f,
            5f,
            5f,
            CreateBossTypeData());
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_BossFlowSystem bossFlow = boss.AddComponent<HWJ_BossFlowSystem>();

        SetPrivateField(bossFlow, "stageProgressionSystem", stage);

        yield return null;

        Assert.IsTrue(stage.TryApplyStageDefinition(stageData).Succeeded);
        Assert.IsTrue(stage.TryEnterExploring().Succeeded);
        Assert.IsTrue(stage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(stage.TryUnlockBoss().Succeeded);
        Assert.IsTrue(stage.TryStartBossBattle().Succeeded);
        bossBrain.StartBossEncounter();

        HWJ_BossFlowResult bossDefeatResult = bossFlow.TryMarkBossDefeated();

        Assert.IsTrue(bossDefeatResult.Succeeded);
        Assert.AreEqual(HWJ_BossFlowOperationType.UnlockRegionAfterBoss, bossDefeatResult.OperationType);
        Assert.AreEqual(HWJ_StageFlowState.RegionTransition, stage.CurrentState);
        Assert.IsFalse(bossBrain.EncounterStarted);
        CollectionAssert.Contains(saveService.TrackedProgression.clearedStageIds, "stage.region01.boss");
        CollectionAssert.Contains(saveService.TrackedProgression.defeatedBossIds, "boss.region01.guardian");
        CollectionAssert.Contains(saveService.TrackedProgression.unlockedRegionIds, "region.region02");

        Object.Destroy(saveObject);
        Object.Destroy(stageObject);
        Object.Destroy(stageData);
        Object.Destroy(boss);
    }

    [UnityTest]
    public IEnumerator BossFlowSystem_CombatDeathCompletesFlowAndBlocksDuplicateBossReward()
    {
        string bossObjectId = "boss.region01.guardian";
        string bossRewardClaimId = bossObjectId + ":boss.encounter.001";
        GameObject saveObject = new GameObject("BossCombatDeathSaveService");
        HWJ_SaveService saveService = saveObject.AddComponent<HWJ_SaveService>();
        GameObject player = CreateCombatObject(
            "BossCombatRewardPlayer",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            20f,
            5f,
            5f,
            CreatePlayerTypeData(true));
        HWJ_LevelUpSystem playerLevel = player.AddComponent<HWJ_LevelUpSystem>();
        HWJ_LevelUpDataSO levelData = CreateLevelUpData("test.level.boss.reward", 3, 1, new[] { 10, 20 });
        playerLevel.SetLevelUpData(levelData);
        GameObject stageObject = new GameObject("BossCombatDeathStage");
        HWJ_StageProgressionSystem stage = stageObject.AddComponent<HWJ_StageProgressionSystem>();
        HWJ_StageDefinitionDataSO stageData = CreateStageDefinitionData(
            "stage.region01.boss",
            "region.region01",
            null,
            true,
            bossObjectId,
            true,
            "region.region02");
        GameObject boss = CreateCombatObject(
            "BossCombatDeathBoss",
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            5f,
            0f,
            0f,
            CreateBossTypeData());
        HWJ_RootObjectDataResolver bossResolver = boss.GetComponent<HWJ_RootObjectDataResolver>();
        bossResolver.RootObjectData.Identity.objectId = bossObjectId;
        bossResolver.Reward.experienceReward = 8;
        boss.AddComponent<HWJ_RuntimeSaveIdentity>()
            .SetManualIdentity("boss.encounter.001", bossObjectId);
        HWJ_BossBrainSystem bossBrain = boss.AddComponent<HWJ_BossBrainSystem>();
        HWJ_BossFlowSystem bossFlow = boss.AddComponent<HWJ_BossFlowSystem>();

        SetPrivateField(bossFlow, "stageProgressionSystem", stage);

        yield return null;

        Assert.IsTrue(stage.TryApplyStageDefinition(stageData).Succeeded);
        Assert.IsTrue(stage.TryEnterExploring().Succeeded);
        Assert.IsTrue(stage.TryMarkObjectiveComplete().Succeeded);
        Assert.IsTrue(stage.TryUnlockBoss().Succeeded);
        Assert.IsTrue(stage.TryStartBossBattle().Succeeded);
        bossBrain.StartBossEncounter();

        HWJ_CombatSystem playerCombat = player.GetComponent<HWJ_CombatSystem>();

        Assert.IsTrue(playerCombat.TryDealDamageTo(bossResolver, out float finalDamage));
        Assert.AreEqual(10f, finalDamage, 0.001f);
        Assert.AreEqual(HWJ_StageFlowState.RegionTransition, stage.CurrentState);
        Assert.IsFalse(bossBrain.EncounterStarted);
        Assert.IsTrue(bossFlow.LastFlowResult.Succeeded, bossFlow.LastFlowMessage);
        Assert.AreEqual(HWJ_BossFlowOperationType.UnlockRegionAfterBoss, bossFlow.LastFlowResult.OperationType);
        Assert.IsTrue(saveService.IsRewardClaimed(bossRewardClaimId));
        CollectionAssert.Contains(saveService.TrackedProgression.clearedStageIds, "stage.region01.boss");
        CollectionAssert.Contains(saveService.TrackedProgression.defeatedBossIds, bossObjectId);
        CollectionAssert.Contains(saveService.TrackedProgression.unlockedRegionIds, "region.region02");
        Assert.AreEqual(8, playerLevel.CurrentExperience);

        HWJ_RewardGrantResult duplicateRewardResult = HWJ_RewardUtility.TryGrantKillRewardDetailed(
            bossResolver,
            player.GetComponent<HWJ_RootObjectDataResolver>(),
            boss.transform.position);

        Assert.IsFalse(duplicateRewardResult.Succeeded);
        Assert.AreEqual(HWJ_RewardGrantFailureCode.AlreadyClaimed, duplicateRewardResult.FailureCode);
        Assert.AreEqual(bossRewardClaimId, duplicateRewardResult.RewardClaimId);
        Assert.AreEqual(8, playerLevel.CurrentExperience);

        saveObject.SetActive(false);
        Object.Destroy(saveObject);
        Object.Destroy(player);
        Object.Destroy(stageObject);
        Object.Destroy(stageData);
        Object.Destroy(boss);
        Object.Destroy(levelData);
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

    [UnityTest]
    public IEnumerator MonsterAISystem_StoresActionFailureReasonWhenAttackSystemMissing()
    {
        GameObject player = CreatePlayerObject("MonsterAIActionTarget", false, startAsSoul: false);
        GameObject enemy = CreateCombatObject(
            "MonsterAIActionEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true));
        HWJ_MonsterAISystem monsterAI = enemy.AddComponent<HWJ_MonsterAISystem>();
        int actionEventCount = 0;
        HWJ_EnemyAIActionEvent lastActionEvent = default(HWJ_EnemyAIActionEvent);

        void OnEnemyAIActionResolved(HWJ_EnemyAIActionEvent actionEvent)
        {
            actionEventCount++;
            lastActionEvent = actionEvent;
        }

        player.transform.position = Vector3.zero;
        enemy.transform.position = Vector3.right * 0.5f;
        monsterAI.SetTarget(player.transform);

        yield return null;

        HWJ_GameplayEvents.EnemyAIActionResolved += OnEnemyAIActionResolved;
        monsterAI.TrySetAIState(HWJ_MonsterAIState.Attack, 0f);
        SetPrivateField(monsterAI, "nextDecisionTime", 0f);

        yield return null;

        Assert.IsFalse(monsterAI.LastActionResult.Succeeded);
        Assert.AreEqual(HWJ_EnemyAIActionType.Attack, monsterAI.LastActionResult.ActionType);
        Assert.AreEqual(HWJ_EnemyAIActionFailureCode.MissingAttackSystem, monsterAI.LastActionFailureCode);
        Assert.AreEqual(HWJ_MonsterAIState.Recovery, monsterAI.LastActionResult.NextState);
        Assert.AreEqual(1, actionEventCount);
        Assert.AreSame(monsterAI, lastActionEvent.MonsterAI);
        Assert.IsFalse(lastActionEvent.ActionResult.Succeeded);
        Assert.AreEqual(HWJ_EnemyAIActionFailureCode.MissingAttackSystem, lastActionEvent.ActionResult.FailureCode);

        HWJ_GameplayEvents.EnemyAIActionResolved -= OnEnemyAIActionResolved;
        Object.Destroy(player);
        Object.Destroy(enemy);
    }

    [UnityTest]
    public IEnumerator EnemyAIActionDebugLogger_RecordsFailureEventsWithoutChangingAI()
    {
        GameObject player = CreatePlayerObject("MonsterAIActionDebugTarget", false, startAsSoul: false);
        GameObject enemy = CreateCombatObject(
            "MonsterAIActionDebugEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true));
        GameObject loggerObject = new GameObject("EnemyAIActionDebugLogger");
        HWJ_MonsterAISystem monsterAI = enemy.AddComponent<HWJ_MonsterAISystem>();
        HWJ_EnemyAIActionDebugLogger logger = loggerObject.AddComponent<HWJ_EnemyAIActionDebugLogger>();

        player.transform.position = Vector3.zero;
        enemy.transform.position = Vector3.right * 0.5f;
        monsterAI.SetTarget(player.transform);

        yield return null;

        logger.Clear();
        monsterAI.TrySetAIState(HWJ_MonsterAIState.Attack, 0f);
        SetPrivateField(monsterAI, "nextDecisionTime", 0f);

        yield return null;

        Assert.AreEqual(1, logger.ReceivedEventCount);
        Assert.AreEqual("MonsterAIActionDebugEnemy", logger.LastMonsterName);
        Assert.AreEqual(HWJ_EnemyAIActionType.Attack, logger.LastActionType);
        Assert.AreEqual(HWJ_EnemyAIActionFailureCode.MissingAttackSystem, logger.LastFailureCode);
        Assert.AreEqual(monsterAI.LastActionResult.State, logger.LastState);
        Assert.IsFalse(monsterAI.LastActionResult.Succeeded);

        logger.Clear();
        Assert.AreEqual(0, logger.ReceivedEventCount);
        Assert.AreEqual(HWJ_EnemyAIActionType.None, logger.LastActionType);

        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(loggerObject);
    }

    [UnityTest]
    public IEnumerator EnemyAIActionDebugOverlay_RecordsRecentFailureEntries()
    {
        GameObject player = CreatePlayerObject("MonsterAIActionOverlayTarget", false, startAsSoul: false);
        GameObject enemy = CreateCombatObject(
            "MonsterAIActionOverlayEnemy",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            10f,
            1f,
            1f,
            CreateEnemyTypeData(true));
        GameObject overlayObject = new GameObject("EnemyAIActionDebugOverlay");
        HWJ_MonsterAISystem monsterAI = enemy.AddComponent<HWJ_MonsterAISystem>();
        HWJ_EnemyAIActionDebugOverlay overlay = overlayObject.AddComponent<HWJ_EnemyAIActionDebugOverlay>();

        player.transform.position = Vector3.zero;
        enemy.transform.position = Vector3.right * 0.5f;
        monsterAI.SetTarget(player.transform);

        yield return null;

        overlay.Clear();
        monsterAI.TrySetAIState(HWJ_MonsterAIState.Attack, 0f);
        SetPrivateField(monsterAI, "nextDecisionTime", 0f);

        yield return null;

        Assert.AreEqual(1, overlay.EntryCount);
        HWJ_EnemyAIActionDebugEntry entry = overlay.GetEntry(0);
        Assert.IsNotNull(entry);
        Assert.AreEqual("MonsterAIActionOverlayEnemy", entry.MonsterName);
        Assert.AreEqual(HWJ_EnemyAIActionType.Attack, entry.ActionType);
        Assert.AreEqual(HWJ_EnemyAIActionFailureCode.MissingAttackSystem, entry.FailureCode);
        Assert.IsFalse(entry.Succeeded);

        overlay.SetOverlayVisible(false);
        Assert.IsFalse(overlay.ShowOverlay);
        overlay.Clear();
        Assert.AreEqual(0, overlay.EntryCount);

        Object.Destroy(player);
        Object.Destroy(enemy);
        Object.Destroy(overlayObject);
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

    private static HWJ_GameSaveData CreateValidSaveDataForValidation(string saveId)
    {
        HWJ_GameSaveData saveData = new HWJ_GameSaveData
        {
            schemaVersion = HWJ_SaveSchema.CurrentVersion,
            saveId = saveId,
            createdUtc = "2026-07-15T00:00:00.0000000Z",
            savedUtc = "2026-07-15T00:00:00.0000000Z",
            player = new HWJ_SavePlayerRuntimeData
            {
                playerRootObjectId = "player.validation",
                objectType = HWJ_ObjectType.Player,
                faction = HWJ_Faction.Player,
                weaponType = HWJ_WeaponType.None,
                stats = new HWJ_SaveRuntimeStatData
                {
                    runtimeState = HWJ_RuntimeState.Soul,
                    currentHp = 10f,
                    maxHp = 10f,
                    soulHp = 10f,
                    soulMaxHp = 10f,
                    possessedBodyHp = 0f,
                    moveSpeed = 5f,
                    attackPower = 1f,
                    defense = 0f,
                    attackSpeed = 1f
                },
                body = new HWJ_SaveBodyRuntimeData
                {
                    soulState = HWJ_SoulRuntimeState.Soul,
                    playerExistenceState = HWJ_PlayerExistenceState.Spirit,
                    hasActivePossessedBody = false,
                    hasPossessedBodyRuntimeState = false,
                    possessedBodyCurrentHp = 0f,
                    possessedBodyMaxHp = 0f,
                    currentDecayValue = 0f,
                    maxDecayValue = 0f,
                    currentDecayRatio = 0f,
                    remainingDecayValue = 0f,
                    remainingDecayRatio = 0f,
                    decayDangerLevel = HWJ_DecayDangerLevel.Stable,
                    isDecaying = false
                },
                growth = new HWJ_SaveGrowthRuntimeData
                {
                    currentLevel = 1,
                    currentExperience = 0,
                    skillPoint = 0
                }
            },
            stage = new HWJ_SaveStageRuntimeData
            {
                stageId = "stage.validation",
                nextRegionId = "region.validation.next",
                currentState = HWJ_StageFlowState.Exploring,
                objectiveComplete = false,
                bossUnlocked = false,
                bossBattleStarted = false,
                bossDefeated = false,
                regionUnlocked = false,
                transitionLocked = false
            },
            progression = new HWJ_SaveProgressionData()
        };

        saveData.player.growth.unlockedSkillIds.Add("skill.validation.basic");
        saveData.progression.defeatedEnemyRewardIds.Add("reward.validation.001");
        saveData.progression.defeatedBossIds.Add("boss.validation.001");
        saveData.progression.clearedStageIds.Add("stage.validation.001");
        saveData.progression.unlockedRegionIds.Add("region.validation.001");
        return saveData;
    }

    private static void AssertSnapshotFieldsCoveredBySaveDto<TSnapshot, TSaveData>(params string[] allowedTypeMismatchFieldNames)
    {
        Dictionary<string, FieldInfo> snapshotFields = GetPublicInstanceFieldMap(typeof(TSnapshot));
        Dictionary<string, FieldInfo> saveFields = GetPublicInstanceFieldMap(typeof(TSaveData));
        HashSet<string> allowedTypeMismatches = new HashSet<string>(allowedTypeMismatchFieldNames ?? new string[0]);

        CollectionAssert.AreEquivalent(
            snapshotFields.Keys,
            saveFields.Keys,
            $"{typeof(TSaveData).Name} fields must match {typeof(TSnapshot).Name} fields so new runtime snapshot fields cannot be silently skipped.");

        foreach (KeyValuePair<string, FieldInfo> snapshotField in snapshotFields)
        {
            FieldInfo saveField = saveFields[snapshotField.Key];

            if (allowedTypeMismatches.Contains(snapshotField.Key))
            {
                continue;
            }

            Assert.AreEqual(
                snapshotField.Value.FieldType,
                saveField.FieldType,
                $"{typeof(TSaveData).Name}.{snapshotField.Key} type must match {typeof(TSnapshot).Name}.{snapshotField.Key}.");
        }
    }

    private static void AssertRuntimeObjectSnapshotMappingContract()
    {
        Dictionary<string, FieldInfo> runtimeObjectFields = GetPublicInstanceFieldMap(typeof(HWJ_RuntimeObjectSnapshot));
        Dictionary<string, FieldInfo> savePlayerFields = GetPublicInstanceFieldMap(typeof(HWJ_SavePlayerRuntimeData));

        CollectionAssert.AreEquivalent(
            new[]
            {
                "rootObjectData",
                "objectType",
                "faction",
                "weaponType",
                "sourceStatusData",
                "sourceDamageData",
                "sourceReceivedDamageData",
                "runtimeStats",
                "runtimeBody",
                "runtimeGrowth"
            },
            runtimeObjectFields.Keys,
            "HWJ_RuntimeObjectSnapshot field contract changed. Update HWJ_SaveDataFactory and this mapping contract.");
        CollectionAssert.AreEquivalent(
            new[]
            {
                "playerRootObjectId",
                "objectType",
                "faction",
                "weaponType",
                "stats",
                "body",
                "growth"
            },
            savePlayerFields.Keys,
            "HWJ_SavePlayerRuntimeData field contract changed. Update HWJ_SaveDataFactory and this mapping contract.");
    }

    private static Dictionary<string, FieldInfo> GetPublicInstanceFieldMap(System.Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        Dictionary<string, FieldInfo> fieldMap = new Dictionary<string, FieldInfo>();

        for (int i = 0; i < fields.Length; i++)
        {
            fieldMap.Add(fields[i].Name, fields[i]);
        }

        return fieldMap;
    }

    private static HWJ_StageDefinitionDataSO CreateStageDefinitionData(
        string stageId,
        string regionId,
        string nextStageId,
        bool hasBoss,
        string bossId,
        bool unlocksRegionOnClear,
        string unlockRegionId,
        string[] requiredClearedStageIds = null,
        string[] requiredUnlockedRegionIds = null)
    {
        HWJ_StageDefinitionDataSO stageData = ScriptableObject.CreateInstance<HWJ_StageDefinitionDataSO>();
        SetPrivateField(stageData, "stageId", stageId);
        SetPrivateField(stageData, "displayName", stageId);
        SetPrivateField(stageData, "regionId", regionId);
        SetPrivateField(stageData, "nextStageId", nextStageId);
        SetPrivateField(stageData, "hasBoss", hasBoss);
        SetPrivateField(stageData, "bossId", bossId);
        SetPrivateField(stageData, "unlocksRegionOnClear", unlocksRegionOnClear);
        SetPrivateField(stageData, "unlockRegionId", unlockRegionId);
        SetPrivateField(stageData, "requiredClearedStageIds", requiredClearedStageIds);
        SetPrivateField(stageData, "requiredUnlockedRegionIds", requiredUnlockedRegionIds);
        return stageData;
    }

    private static HWJ_RegionDefinitionDataSO CreateRegionDefinitionData(
        string regionId,
        string firstStageId,
        string nextRegionId,
        string[] stageIds,
        string[] requiredClearedStageIds = null,
        string[] requiredUnlockedRegionIds = null)
    {
        HWJ_RegionDefinitionDataSO regionData = ScriptableObject.CreateInstance<HWJ_RegionDefinitionDataSO>();
        SetPrivateField(regionData, "regionId", regionId);
        SetPrivateField(regionData, "displayName", regionId);
        SetPrivateField(regionData, "firstStageId", firstStageId);
        SetPrivateField(regionData, "nextRegionId", nextRegionId);
        SetPrivateField(regionData, "stageIds", stageIds);
        SetPrivateField(regionData, "requiredClearedStageIds", requiredClearedStageIds);
        SetPrivateField(regionData, "requiredUnlockedRegionIds", requiredUnlockedRegionIds);
        return regionData;
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
