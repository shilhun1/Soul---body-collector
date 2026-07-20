using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only validator for HWJ definition assets. It reports data problems without mutating source assets.
/// </summary>
public static class HWJ_GameDataValidator
{
    private const string SearchRoot = "Assets/02Scripts/HWJ";
    private const string MenuPath = "Tools/Game/Validation/Validate All Game Data";
    private const string SceneSaveIdentityMenuPath = "Tools/Game/Validation/Validate Scene Save Identities";
    private const string SceneBossSetupMenuPath = "Tools/Game/Validation/Validate Scene Boss Setups";

    [MenuItem(MenuPath)]
    public static void ValidateAllGameDataFromMenu()
    {
        HWJ_EditorValidationReport validationReport = ValidateAllGameData();
        LogReport(validationReport);
    }

    [MenuItem(SceneSaveIdentityMenuPath)]
    public static void ValidateOpenSceneSaveIdentitiesFromMenu()
    {
        HWJ_EditorValidationReport validationReport = ValidateOpenSceneSaveIdentities();
        LogReport(validationReport);
    }

    [MenuItem(SceneBossSetupMenuPath)]
    public static void ValidateOpenSceneBossSetupsFromMenu()
    {
        HWJ_EditorValidationReport validationReport = ValidateOpenSceneBossSetups();
        LogReport(validationReport);
    }

    public static HWJ_EditorValidationReport ValidateAllGameData()
    {
        List<HWJ_EditorValidationIssue> validationIssues = new List<HWJ_EditorValidationIssue>();
        HashSet<string> skillActionIds = CollectSkillActionIds();
        HashSet<string> rootObjectIds = CollectRootObjectIds();
        Dictionary<string, HWJ_ObjectType> rootObjectTypeById = CollectRootObjectTypeById();
        HashSet<string> playerSkillIds = CollectPlayerSkillIds();
        HashSet<string> ruleExecutionCoreIds = CollectRuleExecutionCoreIds();

        // Keep each domain check separate so the validator can grow without becoming a hidden game manager.
        ValidateGameplayDatabases(validationIssues);
        ValidateRootObjects(validationIssues);
        ValidateObjectTypeData(validationIssues, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds);
        ValidateSkillActions(validationIssues);
        ValidateSkillNodes(validationIssues, skillActionIds);
        ValidateLevelTables(validationIssues);
        ValidateSpawnTables(validationIssues);
        ValidateStatOrbs(validationIssues);
        ValidateBossPatterns(validationIssues);
        ValidateStageAndRegionDefinitions(validationIssues, rootObjectTypeById);
        ValidateRules(validationIssues);
        ValidatePlayerInputBindings(validationIssues);
        ValidateSaveDtoTypes(validationIssues);

        return HWJ_EditorValidationReport.Create(validationIssues);
    }

    public static HWJ_EditorValidationReport ValidateOpenSceneSaveIdentities()
    {
        List<HWJ_EditorValidationIssue> validationIssues = new List<HWJ_EditorValidationIssue>();
        ValidateOpenSceneSaveIdentities(validationIssues);
        return HWJ_EditorValidationReport.Create(validationIssues);
    }

    public static HWJ_EditorValidationReport ValidateOpenSceneBossSetups()
    {
        List<HWJ_EditorValidationIssue> validationIssues = new List<HWJ_EditorValidationIssue>();
        ValidateOpenSceneBossSetups(validationIssues);
        return HWJ_EditorValidationReport.Create(validationIssues);
    }

    // Reuse the runtime database report instead of duplicating its stable ID checks.
    private static void ValidateGameplayDatabases(List<HWJ_EditorValidationIssue> validationIssues)
    {
        HWJ_GameplayDatabaseSO[] databases = LoadAssets<HWJ_GameplayDatabaseSO>();

        for (int i = 0; i < databases.Length; i++)
        {
            HWJ_GameplayDatabaseSO databaseAsset = databases[i];
            string assetPath = GetAssetPath(databaseAsset);
            HWJ_GameDataRegistryReport registryReport = databaseAsset.ValidateRegistryIds();

            if (registryReport == null || registryReport.entries == null)
            {
                continue;
            }

            for (int entryIndex = 0; entryIndex < registryReport.entries.Length; entryIndex++)
            {
                HWJ_GameDataValidationEntry registryEntry = registryReport.entries[entryIndex];
                AddIssue(
                    validationIssues,
                    registryEntry.Severity,
                    registryEntry.ValidationCode,
                    registryEntry.RequirementId,
                    assetPath,
                    registryEntry.FieldName,
                    $"{registryEntry.DataCategory} {registryEntry.AssetName}: {registryEntry.Problem}",
                    registryEntry.Fix);
            }
        }
    }

    private static void ValidateRootObjects(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_RootObjectDataSO[] rootObjects = LoadAssets<HWJ_RootObjectDataSO>();
        HashSet<string> statOrbIds = CollectStatOrbIds();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            HWJ_RootObjectDataSO rootObject = rootObjects[i];
            string assetPath = GetAssetPath(rootObject);
            string rootObjectId = rootObject.Identity != null ? rootObject.Identity.objectId : null;

            ValidateStableId(validationIssues, firstPathById, "REQ-7", assetPath, "Identity.objectId", rootObjectId);

            if (rootObject.Identity == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ROOT_IDENTITY_MISSING", "REQ-7", assetPath, "Identity", "RootObjectData identity block is missing.", "Create an identity block with a stable object ID.");
            }

            if (rootObject.Status == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STATUS_MISSING", "REQ-7", assetPath, "Status", "RootObjectData status block is missing.", "Create a status block with HP, movement, and combat values.");
            }
            else
            {
                if (rootObject.Status.maxHp <= 0f)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "MAX_HP_INVALID", "REQ-14", assetPath, "Status.maxHp", "Max HP must be greater than 0.", "Set maxHp to a positive value.");
                }

                if (rootObject.Status.attackSpeed < 0f)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ATTACK_SPEED_NEGATIVE", "REQ-14", assetPath, "Status.attackSpeed", "Attack speed cannot be negative.", "Set attackSpeed to 0 or a positive value.");
                }
            }

            if (rootObject.Damage != null)
            {
                ValidateNonNegative(validationIssues, rootObject.Damage.baseDamage, "REQ-14", assetPath, "Damage.baseDamage", "DAMAGE_BASE_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.Damage.criticalChance, "REQ-14", assetPath, "Damage.criticalChance", "CRITICAL_CHANCE_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.Damage.criticalMultiplier, "REQ-14", assetPath, "Damage.criticalMultiplier", "CRITICAL_MULTIPLIER_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.Damage.knockbackPower, "REQ-14", assetPath, "Damage.knockbackPower", "KNOCKBACK_NEGATIVE");
            }

            if (rootObject.ReceivedDamage != null)
            {
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.damageMultiplier, "REQ-14", assetPath, "ReceivedDamage.damageMultiplier", "RECEIVED_DAMAGE_MULTIPLIER_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.invincibleSecondsAfterHit, "REQ-14", assetPath, "ReceivedDamage.invincibleSecondsAfterHit", "RECEIVED_DAMAGE_INVINCIBLE_SECONDS_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.hitStunSeconds, "REQ-14", assetPath, "ReceivedDamage.hitStunSeconds", "RECEIVED_DAMAGE_HIT_STUN_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.hitReactionImmuneSeconds, "REQ-14", assetPath, "ReceivedDamage.hitReactionImmuneSeconds", "RECEIVED_DAMAGE_HIT_REACTION_IMMUNE_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.hitReactionWindowSeconds, "REQ-14", assetPath, "ReceivedDamage.hitReactionWindowSeconds", "RECEIVED_DAMAGE_HIT_REACTION_WINDOW_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.hitReactionLimitImmuneSeconds, "REQ-14", assetPath, "ReceivedDamage.hitReactionLimitImmuneSeconds", "RECEIVED_DAMAGE_HIT_REACTION_LIMIT_IMMUNE_NEGATIVE");
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.knockbackWeightMultiplier, "REQ-14", assetPath, "ReceivedDamage.knockbackWeightMultiplier", "KNOCKBACK_WEIGHT_NEGATIVE");

                if (rootObject.ReceivedDamage.maxHitReactionsPerWindow < 0)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "RECEIVED_DAMAGE_MAX_HIT_REACTIONS_NEGATIVE", "REQ-14", assetPath, "ReceivedDamage.maxHitReactionsPerWindow", "Max hit reactions per window cannot be negative.", "Set maxHitReactionsPerWindow to 0 for unlimited reactions or a positive limit.");
                }
            }

            if (rootObject.SelectedTypeData == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "TYPE_DATA_MISSING", "REQ-7", assetPath, "SelectedTypeData", "RootObjectData has no selected type data.", "Assign a Player, Enemy, NPC, or Boss type data asset.");
            }
            else if (rootObject.Identity != null && rootObject.SelectedTypeData.ObjectType != rootObject.Identity.objectType)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "TYPE_DATA_MISMATCH", "REQ-7", assetPath, "SelectedTypeData", "Selected type data object type does not match Identity.objectType.", "Assign matching type data or update Identity.objectType.");
            }

            if (rootObject.Model == null || rootObject.Model.modelPrefab == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "MODEL_PREFAB_MISSING", "REQ-14", assetPath, "Model.modelPrefab", "RootObjectData has no model prefab.", "Assign the prefab used by spawners and scene setup.");
            }

            if (rootObject.Reward != null)
            {
                ValidateRewardData(validationIssues, rootObject.Reward, statOrbIds, assetPath);
            }
        }
    }

    private static void ValidateRewardData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RewardData rewardData,
        HashSet<string> statOrbIds,
        string assetPath)
    {
        if (rewardData.dropsStatOrb)
        {
            bool hasFixedStatOrbId = !string.IsNullOrWhiteSpace(rewardData.statOrbId);
            bool hasRandomCandidates = rewardData.statOrbCandidates != null && rewardData.statOrbCandidates.Length > 0;

            if (!hasFixedStatOrbId && !hasRandomCandidates && !rewardData.useAllRegisteredStatOrbsWhenEmpty)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_REWARD_SOURCE_MISSING", "REQ-14", assetPath, "Reward.statOrbId", "Reward is configured to drop a stat orb but has no fixed ID, random candidates, or database fallback.", "Set statOrbId, add statOrbCandidates, or enable useAllRegisteredStatOrbsWhenEmpty.");
            }
        }

        if (rewardData.statOrbDropChance < 0f || rewardData.statOrbDropChance > 1f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_DROP_CHANCE_INVALID", "REQ-14", assetPath, "Reward.statOrbDropChance", "Stat orb drop chance must be between 0 and 1.", "Clamp statOrbDropChance into 0..1.");
        }

        if (rewardData.statOrbSpawnRadius < 0f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_SPAWN_RADIUS_NEGATIVE", "REQ-14", assetPath, "Reward.statOrbSpawnRadius", "Stat orb spawn radius cannot be negative.", "Set statOrbSpawnRadius to 0 or a positive value.");
        }

        if (!string.IsNullOrWhiteSpace(rewardData.statOrbId)
            && statOrbIds != null
            && !statOrbIds.Contains(rewardData.statOrbId.Trim()))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_REWARD_ID_UNKNOWN", "REQ-14", assetPath, "Reward.statOrbId", $"Stat orb ID '{rewardData.statOrbId}' does not exist.", "Use an OrbId from a HWJ_StatOrbDataSO asset.");
        }

        if (rewardData.statOrbCandidates != null)
        {
            for (int i = 0; i < rewardData.statOrbCandidates.Length; i++)
            {
                HWJ_StatOrbRewardEntry candidate = rewardData.statOrbCandidates[i];
                string fieldPrefix = $"Reward.statOrbCandidates[{i}]";

                if (candidate == null)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_CANDIDATE_NULL", "REQ-14", assetPath, fieldPrefix, "Stat orb random candidate slot is empty.", "Remove the empty slot or assign candidate values.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(candidate.statOrbId))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_CANDIDATE_ID_MISSING", "REQ-14", assetPath, fieldPrefix + ".statOrbId", "Stat orb random candidate ID is empty.", "Set statOrbId to an existing stat orb ID.");
                }
                else if (statOrbIds != null && !statOrbIds.Contains(candidate.statOrbId.Trim()))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_CANDIDATE_ID_UNKNOWN", "REQ-14", assetPath, fieldPrefix + ".statOrbId", $"Stat orb candidate ID '{candidate.statOrbId}' does not exist.", "Use an OrbId from a HWJ_StatOrbDataSO asset.");
                }

                if (candidate.weight <= 0)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_CANDIDATE_WEIGHT_INVALID", "REQ-14", assetPath, fieldPrefix + ".weight", "Stat orb candidate weight must be greater than 0.", "Set weight to at least 1.");
                }
            }
        }

        if (rewardData.dropsExperienceOrb && rewardData.experienceOrbPrefab == null)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "EXPERIENCE_ORB_PREFAB_MISSING", "REQ-14", assetPath, "Reward.experienceOrbPrefab", "Reward is configured to drop an experience orb but the orb prefab is missing.", "Assign a prefab with HWJ_ExperienceOrbPickupSystem or disable dropsExperienceOrb.");
        }
    }

    private static void ValidateObjectTypeData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> skillActionIds,
        HashSet<string> rootObjectIds,
        HashSet<string> playerSkillIds,
        HashSet<string> ruleExecutionCoreIds)
    {
        Dictionary<string, string> firstPathByTypeId = new Dictionary<string, string>();

        ValidateTypedAssets(LoadAssets<HWJ_PlayerTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds);
        ValidateTypedAssets(LoadAssets<HWJ_EnemyTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds);
        ValidateTypedAssets(LoadAssets<HWJ_BossTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds);
        ValidateTypedAssets(LoadAssets<HWJ_NPCTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds);
    }

    private static void ValidateTypedAssets<T>(
        T[] typedAssets,
        Dictionary<string, string> firstPathByTypeId,
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> skillActionIds,
        HashSet<string> rootObjectIds,
        HashSet<string> playerSkillIds,
        HashSet<string> ruleExecutionCoreIds) where T : HWJ_ObjectTypeDataSO
    {
        for (int i = 0; i < typedAssets.Length; i++)
        {
            T typeData = typedAssets[i];
            string assetPath = GetAssetPath(typeData);

            ValidateStableId(validationIssues, firstPathByTypeId, "REQ-7", assetPath, "TypeId", typeData.TypeId);

            if (typeData is HWJ_PlayerTypeDataSO playerType)
            {
                ValidateBodyDecayData(validationIssues, playerType.BodyDecay, assetPath, "BodyDecay");
                ValidateSkillSet(validationIssues, playerType.SkillSet, skillActionIds, assetPath, "SkillSet", "REQ-7");
            }
            else if (typeData is HWJ_EnemyTypeDataSO enemyType)
            {
                ValidateEnemyAIData(validationIssues, enemyType, assetPath);
                ValidateSkillSet(validationIssues, enemyType.SkillCycle, skillActionIds, assetPath, "SkillCycle", "REQ-7");
            }
            else if (typeData is HWJ_BossTypeDataSO bossType)
            {
                ValidateBossTypeData(validationIssues, bossType, skillActionIds, rootObjectIds, playerSkillIds, ruleExecutionCoreIds, assetPath);
            }
        }
    }

    private static void ValidateBodyDecayData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BodyDecayData bodyDecayData,
        string assetPath,
        string fieldPrefix)
    {
        if (bodyDecayData == null)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "DECAY_DATA_MISSING", "REQ-10", assetPath, fieldPrefix, "Body decay data is missing.", "Create body decay data for possessed body runtime.");
            return;
        }

        if (bodyDecayData.maxDecayValue <= 0f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "DECAY_MAX_INVALID", "REQ-10", assetPath, $"{fieldPrefix}.maxDecayValue", "Max decay value must be greater than 0.", "Set maxDecayValue to a positive value.");
        }

        if (bodyDecayData.initialDecayValue < 0f || bodyDecayData.initialDecayValue > bodyDecayData.maxDecayValue)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "DECAY_INITIAL_OUT_OF_RANGE", "REQ-10", assetPath, $"{fieldPrefix}.initialDecayValue", "Initial decay must be between 0 and maxDecayValue.", "Clamp initialDecayValue into the valid decay range.");
        }

        ValidateNonNegative(validationIssues, bodyDecayData.decayTickSeconds, "REQ-10", assetPath, $"{fieldPrefix}.decayTickSeconds", "DECAY_TICK_NEGATIVE");
        ValidateNonNegative(validationIssues, bodyDecayData.decayAmountPerTick, "REQ-10", assetPath, $"{fieldPrefix}.decayAmountPerTick", "DECAY_TICK_AMOUNT_NEGATIVE");
        ValidateNonNegative(validationIssues, bodyDecayData.moveDecayPerSecond, "REQ-10", assetPath, $"{fieldPrefix}.moveDecayPerSecond", "DECAY_MOVE_NEGATIVE");
        ValidateNonNegative(validationIssues, bodyDecayData.basicAttackDecayAmount, "REQ-10", assetPath, $"{fieldPrefix}.basicAttackDecayAmount", "DECAY_ATTACK_NEGATIVE");
        ValidateNonNegative(validationIssues, bodyDecayData.skillDecayAmount, "REQ-10", assetPath, $"{fieldPrefix}.skillDecayAmount", "DECAY_SKILL_NEGATIVE");
        ValidateNonNegative(validationIssues, bodyDecayData.hitDecayPenalty, "REQ-10", assetPath, $"{fieldPrefix}.hitDecayPenalty", "DECAY_HIT_NEGATIVE");
    }

    private static void ValidateEnemyAIData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_EnemyTypeDataSO enemyType,
        string assetPath)
    {
        if (enemyType.Tracking != null)
        {
            ValidateNonNegative(validationIssues, enemyType.Tracking.trackingRange, "REQ-14", assetPath, "Tracking.trackingRange", "TRACKING_RANGE_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.Tracking.loseTargetRange, "REQ-14", assetPath, "Tracking.loseTargetRange", "LOSE_TARGET_RANGE_NEGATIVE");

            if (enemyType.Tracking.trackingRange <= 0f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_TRACKING_RANGE_ZERO", "REQ-14", assetPath, "Tracking.trackingRange", "Enemy tracking range is 0, so the AI can only detect targets through fallback runtime values.", "Set trackingRange above 0 for authored enemy detection data.");
            }

            if (enemyType.Tracking.trackingRange > 0f
                && enemyType.Tracking.loseTargetRange > 0f
                && enemyType.Tracking.loseTargetRange < enemyType.Tracking.trackingRange)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_LOSE_RANGE_SMALLER_THAN_TRACKING", "REQ-14", assetPath, "Tracking.loseTargetRange", "Lose target range is smaller than tracking range and may cause unstable target retention.", "Set loseTargetRange greater than or equal to trackingRange, or clear it if not used.");
            }
        }

        if (enemyType.AI != null)
        {
            ValidateNonNegative(validationIssues, enemyType.AI.decisionIntervalSeconds, "REQ-14", assetPath, "AI.decisionIntervalSeconds", "ENEMY_AI_DECISION_INTERVAL_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.idleSeconds, "REQ-14", assetPath, "AI.idleSeconds", "ENEMY_AI_IDLE_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.detectSeconds, "REQ-14", assetPath, "AI.detectSeconds", "ENEMY_AI_DETECT_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.attackPrepareSeconds, "REQ-14", assetPath, "AI.attackPrepareSeconds", "ENEMY_AI_ATTACK_PREPARE_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.attackRecoverySeconds, "REQ-14", assetPath, "AI.attackRecoverySeconds", "ENEMY_AI_ATTACK_RECOVERY_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.repathSeconds, "REQ-14", assetPath, "AI.repathSeconds", "ENEMY_AI_REPATH_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.defaultSkillCooldownSeconds, "REQ-14", assetPath, "AI.defaultSkillCooldownSeconds", "ENEMY_AI_DEFAULT_SKILL_COOLDOWN_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.basicAttackIntervalSeconds, "REQ-14", assetPath, "AI.basicAttackIntervalSeconds", "ENEMY_AI_BASIC_ATTACK_INTERVAL_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.AI.skillCycleResetDelaySeconds, "REQ-14", assetPath, "AI.skillCycleResetDelaySeconds", "ENEMY_AI_SKILL_CYCLE_RESET_DELAY_NEGATIVE");

            if (Mathf.Approximately(enemyType.AI.decisionIntervalSeconds, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_AI_DECISION_INTERVAL_ZERO", "REQ-14", assetPath, "AI.decisionIntervalSeconds", "Enemy decision interval is 0 and can make AI evaluate every frame.", "Use a small positive interval such as 0.05 or 0.1 unless per-frame AI is intentional.");
            }

            if (enemyType.AI.skillCycleCount < 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ENEMY_AI_SKILL_CYCLE_COUNT_NEGATIVE", "REQ-14", assetPath, "AI.skillCycleCount", "Enemy skill cycle count cannot be negative.", "Set skillCycleCount to 0 or a positive value. Use 3 for the fixed 1 -> 2 -> 3 monster skill loop.");
            }

            if (Mathf.Approximately(enemyType.AI.attackPrepareSeconds, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_AI_ATTACK_PREPARE_ZERO", "REQ-14", assetPath, "AI.attackPrepareSeconds", "Enemy attack prepare time is 0, so attacks can start without a readable tell.", "Set attackPrepareSeconds above 0 for enemies that need visible anticipation.");
            }

            if (Mathf.Approximately(enemyType.AI.attackRecoverySeconds, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_AI_ATTACK_RECOVERY_ZERO", "REQ-14", assetPath, "AI.attackRecoverySeconds", "Enemy attack recovery time is 0, so repeated attacks may feel unfair.", "Set attackRecoverySeconds above 0 unless instant recovery is intentional.");
            }
        }

        if (enemyType.Navigation != null)
        {
            ValidateNonNegative(validationIssues, enemyType.Navigation.stoppingDistance, "REQ-14", assetPath, "Navigation.stoppingDistance", "ENEMY_NAV_STOPPING_DISTANCE_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.Navigation.pathRefreshSeconds, "REQ-14", assetPath, "Navigation.pathRefreshSeconds", "ENEMY_NAV_PATH_REFRESH_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.Navigation.ledgeCheckForwardDistance, "REQ-14", assetPath, "Navigation.ledgeCheckForwardDistance", "ENEMY_NAV_LEDGE_FORWARD_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.Navigation.ledgeCheckDownDistance, "REQ-14", assetPath, "Navigation.ledgeCheckDownDistance", "ENEMY_NAV_LEDGE_DOWN_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.Navigation.wallCheckDistance, "REQ-14", assetPath, "Navigation.wallCheckDistance", "ENEMY_NAV_WALL_CHECK_NEGATIVE");

            if (enemyType.Navigation.avoidLedges)
            {
                if (Mathf.Approximately(enemyType.Navigation.ledgeCheckForwardDistance, 0f))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_NAV_LEDGE_FORWARD_ZERO", "REQ-14", assetPath, "Navigation.ledgeCheckForwardDistance", "avoidLedges is enabled but ledge forward check distance is 0.", "Set ledgeCheckForwardDistance above 0 so the enemy checks ground ahead.");
                }

                if (Mathf.Approximately(enemyType.Navigation.ledgeCheckDownDistance, 0f))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_NAV_LEDGE_DOWN_ZERO", "REQ-14", assetPath, "Navigation.ledgeCheckDownDistance", "avoidLedges is enabled but ledge down check distance is 0.", "Set ledgeCheckDownDistance above 0 so the enemy can detect floor below.");
                }
            }
        }

        if (enemyType.State != null)
        {
            ValidateNonNegative(validationIssues, enemyType.State.attackRange, "REQ-14", assetPath, "State.attackRange", "ATTACK_RANGE_NEGATIVE");
            ValidateNonNegative(validationIssues, enemyType.State.returnToIdleDelaySeconds, "REQ-14", assetPath, "State.returnToIdleDelaySeconds", "ENEMY_STATE_RETURN_TO_IDLE_NEGATIVE");

            if (enemyType.State.attackRange <= 0f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_ATTACK_RANGE_ZERO", "REQ-14", assetPath, "State.attackRange", "Enemy attack range is 0, so the AI depends on fallback runtime attack range.", "Set attackRange above 0 for authored enemy combat data.");
            }

            if (enemyType.State.immuneToHitStun && !enemyType.State.hasSuperArmor)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_HITSTUN_IMMUNE_WITHOUT_SUPER_ARMOR", "REQ-14", assetPath, "State.immuneToHitStun", "Enemy is immune to hit stun without super armor, which can make reaction rules unclear.", "Enable hasSuperArmor or document why hit stun immunity is separate.");
            }
        }

        if (enemyType.PossessionBody != null
            && enemyType.PossessionBody.canBePossessed
            && !enemyType.PossessionBody.requiresDefeatedState)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "ENEMY_POSSESSION_WITHOUT_DEFEATED_REQUIREMENT", "REQ-9", assetPath, "PossessionBody.requiresDefeatedState", "Enemy body is possessable without requiring defeated state.", "Keep requiresDefeatedState enabled unless live possession is an intentional special case.");
        }
    }

    private static void ValidateBossTypeData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossTypeDataSO bossType,
        HashSet<string> skillActionIds,
        HashSet<string> rootObjectIds,
        HashSet<string> playerSkillIds,
        HashSet<string> ruleExecutionCoreIds,
        string assetPath)
    {
        ValidateSkillSet(validationIssues, bossType.SkillCycle, skillActionIds, assetPath, "SkillCycle", "REQ-7");
        ValidateBossEntryRequirements(validationIssues, bossType.EntryRequirements, rootObjectIds, playerSkillIds, ruleExecutionCoreIds, assetPath);

        if (bossType.FSM != null)
        {
            if (bossType.FSM.phaseTwoHpRatio <= 0f || bossType.FSM.phaseTwoHpRatio >= 1f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_PHASE_RATIO_SUSPICIOUS", "REQ-12", assetPath, "FSM.phaseTwoHpRatio", "Phase two HP ratio should normally be between 0 and 1.", "Set a ratio such as 0.5 for a 50 percent phase transition.");
            }

            ValidateNonNegative(validationIssues, bossType.FSM.groggyHitWindowSeconds, "REQ-14", assetPath, "FSM.groggyHitWindowSeconds", "BOSS_GROGGY_WINDOW_NEGATIVE");
            ValidateNonNegative(validationIssues, bossType.FSM.groggyDurationSeconds, "REQ-14", assetPath, "FSM.groggyDurationSeconds", "BOSS_GROGGY_DURATION_NEGATIVE");
            ValidateNonNegative(validationIssues, bossType.FSM.groggyEndKnockbackPower, "REQ-14", assetPath, "FSM.groggyEndKnockbackPower", "BOSS_GROGGY_KNOCKBACK_NEGATIVE");

            if (bossType.FSM.groggyHitCountThreshold < 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_GROGGY_THRESHOLD_NEGATIVE", "REQ-14", assetPath, "FSM.groggyHitCountThreshold", "Groggy hit count threshold cannot be negative.", "Set groggyHitCountThreshold to 0 to disable groggy by hit count, or a positive value to enable it.");
            }

            if (bossType.FSM.groggyHitCountThreshold > 0 && bossType.FSM.groggyDurationSeconds <= 0f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_GROGGY_DURATION_ZERO", "REQ-14", assetPath, "FSM.groggyDurationSeconds", "Groggy hit count is enabled but groggy duration is 0.", "Set groggyDurationSeconds above 0 so groggy has a readable disabled window.");
            }

            if (bossType.FSM.groggyHitCountThreshold > 0
                && !bossType.FSM.countGroggyHitsDuringSuperArmor
                && !bossType.FSM.countGroggyHitsWhileHitReactionLimited)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_GROGGY_HIT_SOURCES_RESTRICTED", "REQ-14", assetPath, "FSM.countGroggyHitsDuringSuperArmor", "Groggy hit count is enabled but both super armor hits and hit-reaction-limited hits are excluded.", "Confirm normal hit reactions are enough to trigger groggy, or enable one of the groggy hit source options.");
            }
        }

        if (bossType.Phases == null || bossType.Phases.Length == 0)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_PHASES_MISSING", "REQ-12", assetPath, "Phases", "Boss type has no phase data.", "Add phase definitions before using the boss flow system.");
            return;
        }

        HashSet<string> phaseIds = new HashSet<string>();

        for (int i = 0; i < bossType.Phases.Length; i++)
        {
            HWJ_BossPhaseData phaseData = bossType.Phases[i];
            string fieldPrefix = $"Phases[{i}]";

            if (phaseData == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_PHASE_NULL", "REQ-12", assetPath, fieldPrefix, "Boss phase entry is null.", "Remove the empty phase slot or create a valid phase.");
                continue;
            }

            ValidateInlineId(validationIssues, phaseIds, "REQ-12", assetPath, $"{fieldPrefix}.phaseId", phaseData.phaseId);
            ValidateSkillSet(validationIssues, phaseData.skillSet, skillActionIds, assetPath, $"{fieldPrefix}.skillSet", "REQ-12");
        }
    }

    private static void ValidateBossEntryRequirements(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossEntryRequirementData requirements,
        HashSet<string> rootObjectIds,
        HashSet<string> playerSkillIds,
        HashSet<string> ruleExecutionCoreIds,
        string assetPath)
    {
        if (requirements == null)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_ENTRY_REQUIREMENTS_MISSING", "REQ-12", assetPath, "EntryRequirements", "Boss entry requirement data is missing.", "Create entry requirement data for this boss type.");
            return;
        }

        ValidateNonNegative(validationIssues, requirements.minimumCurrentHp, "REQ-12", assetPath, "EntryRequirements.minimumCurrentHp", "BOSS_ENTRY_MIN_HP_NEGATIVE");

        if (requirements.maximumCurrentDecayRatio < 0f || requirements.maximumCurrentDecayRatio > 1f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_ENTRY_DECAY_RATIO_OUT_OF_RANGE", "REQ-12", assetPath, "EntryRequirements.maximumCurrentDecayRatio", "Maximum current decay ratio must be between 0 and 1.", "Clamp maximumCurrentDecayRatio into 0..1.");
        }

        HashSet<string> allowedBodyIds = ValidateConfiguredIdList(
            validationIssues,
            requirements.allowedBodyObjectIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.allowedBodyObjectIds",
            "BOSS_ENTRY_ALLOWED_BODY_ID_EMPTY",
            "BOSS_ENTRY_ALLOWED_BODY_ID_DUPLICATE");
        HashSet<string> blockedBodyIds = ValidateConfiguredIdList(
            validationIssues,
            requirements.blockedBodyObjectIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.blockedBodyObjectIds",
            "BOSS_ENTRY_BLOCKED_BODY_ID_EMPTY",
            "BOSS_ENTRY_BLOCKED_BODY_ID_DUPLICATE");

        ValidateKnownIds(
            validationIssues,
            allowedBodyIds,
            rootObjectIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.allowedBodyObjectIds",
            "BOSS_ENTRY_ALLOWED_BODY_ID_UNKNOWN",
            "Allowed possessed body ID does not exist in RootObjectData assets.",
            "Create a matching RootObjectData asset or update the allowed body ID.");
        ValidateKnownIds(
            validationIssues,
            blockedBodyIds,
            rootObjectIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.blockedBodyObjectIds",
            "BOSS_ENTRY_BLOCKED_BODY_ID_UNKNOWN",
            "Blocked possessed body ID does not exist in RootObjectData assets.",
            "Create a matching RootObjectData asset or update the blocked body ID.");

        foreach (string allowedBodyId in allowedBodyIds)
        {
            if (blockedBodyIds.Contains(allowedBodyId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_ENTRY_BODY_ID_ALLOWED_AND_BLOCKED", "REQ-12", assetPath, "EntryRequirements.allowedBodyObjectIds", $"Body ID '{allowedBodyId}' is both allowed and blocked.", "Remove the ID from either the allowed list or the blocked list.");
            }
        }

        HashSet<string> requiredSkillIds = ValidateConfiguredIdList(
            validationIssues,
            requirements.requiredUnlockedSkillIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.requiredUnlockedSkillIds",
            "BOSS_ENTRY_REQUIRED_SKILL_ID_EMPTY",
            "BOSS_ENTRY_REQUIRED_SKILL_ID_DUPLICATE");
        ValidateKnownIds(
            validationIssues,
            requiredSkillIds,
            playerSkillIds,
            "REQ-12",
            assetPath,
            "EntryRequirements.requiredUnlockedSkillIds",
            "BOSS_ENTRY_REQUIRED_SKILL_ID_UNKNOWN",
            "Required unlocked skill ID does not exist in player skill definitions.",
            "Add this skill to a player SkillSet or update the required skill ID.",
            HWJ_GameDataValidationSeverity.Warning);

        ValidateAdditionalBossEntryRule(
            validationIssues,
            requirements,
            ruleExecutionCoreIds,
            assetPath);
    }

    private static void ValidateAdditionalBossEntryRule(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossEntryRequirementData requirements,
        HashSet<string> ruleExecutionCoreIds,
        string assetPath)
    {
        if (requirements.additionalRuleExecutionCore != null)
        {
            if (!string.IsNullOrWhiteSpace(requirements.additionalRuleExecutionCoreId)
                && requirements.additionalRuleExecutionCore.ExecutionCoreId != requirements.additionalRuleExecutionCoreId)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_ENTRY_RULE_REFERENCE_ID_MISMATCH", "REQ-12", assetPath, "EntryRequirements.additionalRuleExecutionCoreId", "Additional rule asset ID does not match the configured rule execution core ID. Runtime uses the direct asset reference first.", "Clear additionalRuleExecutionCoreId or make it match the referenced rule execution core asset.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(requirements.additionalRuleExecutionCoreId))
        {
            return;
        }

        if (ruleExecutionCoreIds == null || !ruleExecutionCoreIds.Contains(requirements.additionalRuleExecutionCoreId))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_ENTRY_RULE_ID_UNKNOWN", "REQ-12", assetPath, "EntryRequirements.additionalRuleExecutionCoreId", "Additional rule execution core ID was set but no matching RuleExecutionCore asset was found.", "Assign a direct rule execution core asset, create a matching ID, or clear the ID if no additional rule is intended.");
        }
    }

    private static void ValidateSkillActions(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_SkillActionDataSO[] skillActions = LoadAssets<HWJ_SkillActionDataSO>();

        for (int i = 0; i < skillActions.Length; i++)
        {
            HWJ_SkillActionDataSO skillAction = skillActions[i];
            string assetPath = GetAssetPath(skillAction);

            ValidateStableId(validationIssues, firstPathById, "REQ-7", assetPath, "SkillActionId", skillAction.SkillActionId);

            if (skillAction.ActionType == HWJ_SkillActionType.None)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_ACTION_TYPE_NONE", "REQ-14", assetPath, "ActionType", "Skill action type is None.", "Choose Melee, Projectile, Buff, Dash, or Area.");
            }

            ValidateNonNegative(validationIssues, skillAction.AuthoredAttackPower, "REQ-14", assetPath, "authoredAttackPower", "SKILL_AUTHORED_ATTACK_POWER_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.DamageMultiplier, "REQ-14", assetPath, "damageMultiplier", "SKILL_DAMAGE_MULTIPLIER_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.Range, "REQ-14", assetPath, "range", "SKILL_RANGE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitRange, "REQ-14", assetPath, "hitRange", "SKILL_HIT_RANGE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.DurationSeconds, "REQ-14", assetPath, "durationSeconds", "SKILL_DURATION_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.CooldownSeconds, "REQ-14", assetPath, "cooldownSeconds", "SKILL_COOLDOWN_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitStartSeconds, "REQ-14", assetPath, "hitStartSeconds", "SKILL_HIT_START_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitActiveSeconds, "REQ-14", assetPath, "hitActiveSeconds", "SKILL_HIT_ACTIVE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.RecoverySeconds, "REQ-14", assetPath, "recoverySeconds", "SKILL_RECOVERY_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.MovementLockSeconds, "REQ-14", assetPath, "movementLockSeconds", "SKILL_MOVEMENT_LOCK_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.DashCancelStartSeconds, "REQ-14", assetPath, "dashCancelStartSeconds", "SKILL_DASH_CANCEL_START_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.MoveDistance, "REQ-14", assetPath, "moveDistance", "SKILL_MOVE_DISTANCE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.MoveSpeed, "REQ-14", assetPath, "moveSpeed", "SKILL_MOVE_SPEED_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitIntervalSeconds, "REQ-14", assetPath, "hitIntervalSeconds", "SKILL_HIT_INTERVAL_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.KnockbackDistance, "REQ-14", assetPath, "knockbackDistance", "SKILL_KNOCKBACK_DISTANCE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.StaggerSeconds, "REQ-14", assetPath, "staggerSeconds", "SKILL_STAGGER_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.LaunchHeight, "REQ-14", assetPath, "launchHeight", "SKILL_LAUNCH_HEIGHT_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.InvincibilitySeconds, "REQ-14", assetPath, "invincibilitySeconds", "SKILL_INVINCIBILITY_SECONDS_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.CustomBodyDecayAmount, "REQ-10", assetPath, "customBodyDecayAmount", "SKILL_CUSTOM_BODY_DECAY_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.AdditionalBodyDecayAmount, "REQ-10", assetPath, "additionalBodyDecayAmount", "SKILL_ADDITIONAL_BODY_DECAY_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.ComboBodyDecayMultiplier, "REQ-10", assetPath, "comboBodyDecayMultiplier", "SKILL_COMBO_BODY_DECAY_MULTIPLIER_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.ChargeBodyDecayPerSecond, "REQ-10", assetPath, "chargeBodyDecayPerSecond", "SKILL_CHARGE_BODY_DECAY_PER_SECOND_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.MaxChargeBodyDecayAmount, "REQ-10", assetPath, "maxChargeBodyDecayAmount", "SKILL_MAX_CHARGE_BODY_DECAY_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.MotionStepIntervalSeconds, "REQ-14", assetPath, "motionStepIntervalSeconds", "SKILL_MOTION_STEP_INTERVAL_NEGATIVE");

            if (skillAction.HitCount <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_HIT_COUNT_INVALID", "REQ-14", assetPath, "hitCount", "Skill hit count must be at least 1.", "Set hitCount to 1 or higher.");
            }

            if (skillAction.ActionType == HWJ_SkillActionType.Projectile && skillAction.ProjectilePrefab == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "PROJECTILE_PREFAB_MISSING", "REQ-14", assetPath, "projectilePrefab", "Projectile skill has no projectile prefab.", "Assign the projectile prefab used by the skill action.");
            }

            if (skillAction.UsesCustomBodyDecayAmount
                && Mathf.Approximately(skillAction.CustomBodyDecayAmount, 0f)
                && Mathf.Approximately(skillAction.AdditionalBodyDecayAmount, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_CUSTOM_BODY_DECAY_ZERO", "REQ-10", assetPath, "customBodyDecayAmount", "Skill action overrides body decay but the custom and additional decay amounts are both 0.", "Set a positive customBodyDecayAmount, add additionalBodyDecayAmount, or disable usesCustomBodyDecayAmount.");
            }

            if (skillAction.UsesComboBodyDecayMultiplier
                && Mathf.Approximately(skillAction.ComboBodyDecayMultiplier, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_COMBO_BODY_DECAY_MULTIPLIER_ZERO", "REQ-10", assetPath, "comboBodyDecayMultiplier", "Skill action uses a combo body decay multiplier of 0, so the non-charge decay cost is removed.", "Set comboBodyDecayMultiplier above 0 or disable usesComboBodyDecayMultiplier.");
            }

            if (skillAction.MaxChargeBodyDecayAmount > 0f
                && Mathf.Approximately(skillAction.ChargeBodyDecayPerSecond, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_CHARGE_BODY_DECAY_CAP_UNUSED", "REQ-10", assetPath, "maxChargeBodyDecayAmount", "Skill action has a max charge decay amount but charge decay per second is 0.", "Set chargeBodyDecayPerSecond above 0 or clear maxChargeBodyDecayAmount.");
            }

            ValidateSkillActionTiming(validationIssues, skillAction, assetPath);
        }
    }

    private static void ValidateSkillNodes(
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> skillActionIds)
    {
        Dictionary<string, string> firstPathByNodeId = new Dictionary<string, string>();
        Dictionary<string, HWJ_SkillNodeDataSO> nodeById = new Dictionary<string, HWJ_SkillNodeDataSO>();
        Dictionary<string, string> assetPathByNodeId = new Dictionary<string, string>();
        HWJ_SkillNodeDataSO[] skillNodes = LoadAssets<HWJ_SkillNodeDataSO>();

        for (int i = 0; i < skillNodes.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodes[i];

            if (skillNode == null)
            {
                continue;
            }

            string assetPath = GetAssetPath(skillNode);
            ValidateStableId(validationIssues, firstPathByNodeId, "REQ-SKILL-NODE", assetPath, "NodeId", skillNode.NodeId);

            if (!string.IsNullOrWhiteSpace(skillNode.NodeId) && !nodeById.ContainsKey(skillNode.NodeId))
            {
                nodeById.Add(skillNode.NodeId, skillNode);
                assetPathByNodeId.Add(skillNode.NodeId, assetPath);
            }

            if (string.IsNullOrWhiteSpace(skillNode.PossessedBodyId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_BODY_ID_MISSING", "REQ-SKILL-NODE", assetPath, "possessedBodyId", "Skill node has no possessed body ID.", "Assign the planning body ID such as 1101, 1201, 1301, 1401, or 1501.");
            }

            if (skillNode.WeaponType == HWJ_WeaponType.None)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_WEAPON_NONE", "REQ-SKILL-NODE", assetPath, "weaponType", "Skill node has no weapon type, so it cannot be matched to the possessed body weapon.", "Choose Sword, Lance, Axe, Bow, Shield, or another concrete weapon type.");
            }

            if (skillNode.AuthoredSkillStep <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_STEP_INVALID", "REQ-SKILL-NODE", assetPath, "skillStep", "Skill node step must be greater than 0.", "Set the common unlock step to 1 or higher.");
            }

            if (skillNode.AuthoredRequiredLevel <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_REQUIRED_LEVEL_INVALID", "REQ-SKILL-NODE", assetPath, "requiredLevel", "Skill node required level must be greater than 0.", "Set requiredLevel to 1 or higher.");
            }

            if (skillNode.AuthoredSkillPointCost < 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_COST_NEGATIVE", "REQ-SKILL-NODE", assetPath, "skillPointCost", "Skill node cost cannot be negative.", "Set skillPointCost to 0 or a positive number.");
            }

            string resolvedSkillActionId = skillNode.SkillActionId;

            if (string.IsNullOrWhiteSpace(resolvedSkillActionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_ACTION_ID_MISSING", "REQ-SKILL-NODE", assetPath, "skillActionId", "Skill node is not linked to a skill action ID.", "Assign skillActionId or a HWJ_SkillActionDataSO asset.");
            }
            else if (skillActionIds != null && !skillActionIds.Contains(resolvedSkillActionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_ACTION_ID_UNKNOWN", "REQ-SKILL-NODE", assetPath, "skillActionId", $"Skill node references unknown skill action ID '{resolvedSkillActionId}'.", "Create a matching HWJ_SkillActionDataSO, register the correct action asset, or update the ID.");
            }

            if (skillNode.SkillAction != null
                && !string.IsNullOrWhiteSpace(skillNode.AuthoredSkillActionId)
                && skillNode.SkillAction.SkillActionId != skillNode.AuthoredSkillActionId)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_NODE_ACTION_ID_ASSET_MISMATCH", "REQ-SKILL-NODE", assetPath, "skillActionId", "Skill node has both a skill action asset and a different typed skillActionId. Runtime uses the asset ID first.", "Make the typed skillActionId match the linked asset ID, or clear the typed ID.");
            }

            if (skillNode.AuthoredSkillStep == 1 && skillNode.HasPrerequisite)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_NODE_STEP_ONE_PREREQUISITE", "REQ-SKILL-NODE", assetPath, "prerequisiteNodeId", "Step 1 skill node has a prerequisite. Starting nodes are expected to have none.", "Clear prerequisiteNodeId or change the skill step.");
            }

            if (skillNode.AuthoredSkillStep > 1 && !skillNode.HasPrerequisite)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_NODE_HIGH_STEP_NO_PREREQUISITE", "REQ-SKILL-NODE", assetPath, "prerequisiteNodeId", "Skill node above step 1 has no prerequisite.", "Assign the previous node ID unless this node is intentionally independent.");
            }
        }

        ValidateSkillNodePrerequisites(validationIssues, nodeById, assetPathByNodeId);
    }

    private static void ValidateSkillNodePrerequisites(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, HWJ_SkillNodeDataSO> nodeById,
        Dictionary<string, string> assetPathByNodeId)
    {
        if (nodeById == null)
        {
            return;
        }

        foreach (KeyValuePair<string, HWJ_SkillNodeDataSO> pair in nodeById)
        {
            HWJ_SkillNodeDataSO skillNode = pair.Value;

            if (skillNode == null || !skillNode.HasPrerequisite)
            {
                continue;
            }

            string assetPath = GetAssetPath(skillNode);
            if (assetPathByNodeId != null && assetPathByNodeId.TryGetValue(pair.Key, out string storedAssetPath))
            {
                assetPath = storedAssetPath;
            }

            string prerequisiteNodeId = skillNode.PrerequisiteNodeId;

            if (prerequisiteNodeId == skillNode.NodeId)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_PREREQUISITE_SELF", "REQ-SKILL-NODE", assetPath, "prerequisiteNodeId", "Skill node points to itself as a prerequisite.", "Assign a previous node ID or clear the prerequisite.");
                continue;
            }

            if (!nodeById.TryGetValue(prerequisiteNodeId, out HWJ_SkillNodeDataSO prerequisiteNode) || prerequisiteNode == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_PREREQUISITE_MISSING", "REQ-SKILL-NODE", assetPath, "prerequisiteNodeId", $"Prerequisite skill node '{prerequisiteNodeId}' does not exist.", "Create the prerequisite node asset or update prerequisiteNodeId.");
                continue;
            }

            if (prerequisiteNode.SkillStep >= skillNode.SkillStep)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_NODE_PREREQUISITE_STEP_ORDER", "REQ-SKILL-NODE", assetPath, "skillStep", "Prerequisite node step is not lower than the current node step.", "Use a prerequisite from an earlier common unlock step.");
            }

            if (HasSkillNodeCycle(skillNode, nodeById))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_NODE_PREREQUISITE_CYCLE", "REQ-SKILL-NODE", assetPath, "prerequisiteNodeId", "Skill node prerequisite chain contains a cycle.", "Break the cycle by assigning a one-way previous node chain.");
            }
        }
    }

    private static bool HasSkillNodeCycle(
        HWJ_SkillNodeDataSO startNode,
        Dictionary<string, HWJ_SkillNodeDataSO> nodeById)
    {
        if (startNode == null || nodeById == null)
        {
            return false;
        }

        HashSet<string> visitedNodeIds = new HashSet<string>();
        HWJ_SkillNodeDataSO currentNode = startNode;

        while (currentNode != null && currentNode.HasPrerequisite)
        {
            if (string.IsNullOrWhiteSpace(currentNode.NodeId))
            {
                return false;
            }

            if (!visitedNodeIds.Add(currentNode.NodeId))
            {
                return true;
            }

            if (!nodeById.TryGetValue(currentNode.PrerequisiteNodeId, out currentNode))
            {
                return false;
            }
        }

        return false;
    }

    private static void ValidateSkillActionTiming(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_SkillActionDataSO skillAction,
        string assetPath)
    {
        bool timedHitSkill = skillAction.ActionType == HWJ_SkillActionType.Melee || skillAction.ActionType == HWJ_SkillActionType.Area;
        bool dashSkill = skillAction.ActionType == HWJ_SkillActionType.Dash;
        bool projectileSkill = skillAction.ActionType == HWJ_SkillActionType.Projectile;
        float safeHitStartSeconds = Mathf.Max(0f, skillAction.HitStartSeconds);
        float safeHitActiveSeconds = Mathf.Max(0f, skillAction.HitActiveSeconds);
        float safeRecoverySeconds = Mathf.Max(0f, skillAction.RecoverySeconds);
        float safeDurationSeconds = Mathf.Max(0f, skillAction.DurationSeconds);
        float hitWindowEndSeconds = safeHitStartSeconds + safeHitActiveSeconds;
        float fullTimedActionSeconds = hitWindowEndSeconds + safeRecoverySeconds;

        // Runtime clamps several timing values, but validation should keep authored data explicit.
        if (timedHitSkill && Mathf.Approximately(skillAction.HitActiveSeconds, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_HIT_ACTIVE_ZERO", "REQ-14", assetPath, "hitActiveSeconds", "Timed hit skill has no explicit active hit window.", "Set hitActiveSeconds above 0 so the hit frame duration is intentional.");
        }

        if ((timedHitSkill || dashSkill) && Mathf.Approximately(skillAction.Range, 0f) && Mathf.Approximately(skillAction.HitRange, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_HIT_RANGE_MISSING", "REQ-14", assetPath, "hitRange", "Damage skill has neither range nor hitRange configured.", "Set range or hitRange so the attack volume is clear to other systems.");
        }

        if (!projectileSkill && skillAction.HitCount > 1 && Mathf.Approximately(skillAction.HitIntervalSeconds, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MULTI_HIT_INTERVAL_MISSING", "REQ-14", assetPath, "hitIntervalSeconds", "Multi-hit skill has no interval between hit checks.", "Set hitIntervalSeconds above 0 to make repeated hits deterministic.");
        }

        if (timedHitSkill && skillAction.HitCount > 1 && skillAction.HitIntervalSeconds > 0f && safeHitActiveSeconds > 0f)
        {
            float requiredMultiHitWindowSeconds = (skillAction.HitCount - 1) * skillAction.HitIntervalSeconds;

            if (requiredMultiHitWindowSeconds > safeHitActiveSeconds)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MULTI_HIT_WINDOW_TOO_SHORT", "REQ-14", assetPath, "hitActiveSeconds", "Hit active duration is too short to cover the configured multi-hit count and interval.", "Increase hitActiveSeconds, reduce hitCount, or reduce hitIntervalSeconds.");
            }
        }

        if (timedHitSkill && safeDurationSeconds > 0f && safeDurationSeconds < hitWindowEndSeconds)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_DURATION_SHORTER_THAN_HIT_WINDOW", "REQ-14", assetPath, "durationSeconds", "Skill duration ends before the configured hit window finishes.", "Increase durationSeconds or reduce hitStartSeconds/hitActiveSeconds.");
        }

        if (timedHitSkill && skillAction.CanDashCancel && fullTimedActionSeconds > 0f && skillAction.DashCancelStartSeconds > fullTimedActionSeconds)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_DASH_CANCEL_AFTER_ACTION", "REQ-14", assetPath, "dashCancelStartSeconds", "Dash cancel starts after the configured action timing has already ended.", "Move dashCancelStartSeconds inside the hit/recovery timing or disable dash cancel.");
        }
        else if (timedHitSkill && !skillAction.CanDashCancel && skillAction.DashCancelStartSeconds > 0f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_DASH_CANCEL_TIME_UNUSED", "REQ-14", assetPath, "dashCancelStartSeconds", "Dash cancel time is configured while dash cancel is disabled.", "Clear dashCancelStartSeconds or enable canDashCancel.");
        }

        if (timedHitSkill && skillAction.MovementLockSeconds > 0f && skillAction.MovementLockSeconds < hitWindowEndSeconds)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MOVEMENT_LOCK_ENDS_DURING_HIT", "REQ-14", assetPath, "movementLockSeconds", "Movement lock ends before the active hit window is finished.", "Keep movementLockSeconds at least through hitStartSeconds plus hitActiveSeconds.");
        }

        if (timedHitSkill && fullTimedActionSeconds > 0f && skillAction.MovementLockSeconds > fullTimedActionSeconds)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MOVEMENT_LOCK_AFTER_ACTION", "REQ-14", assetPath, "movementLockSeconds", "Movement lock is longer than the configured hit and recovery timing.", "Confirm this is intentional or reduce movementLockSeconds.");
        }

        if (dashSkill && Mathf.Approximately(skillAction.MoveDistance, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_DASH_DISTANCE_MISSING", "REQ-14", assetPath, "moveDistance", "Dash skill has no positive move distance and will not perform a clear dash movement.", "Set moveDistance above 0 for dash skills.");
        }

        if (dashSkill && Mathf.Approximately(skillAction.MoveSpeed, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_DASH_SPEED_MISSING", "REQ-14", assetPath, "moveSpeed", "Dash skill has no positive move speed and depends on runtime fallback speed.", "Set moveSpeed above 0 for dash skills.");
        }

        if (projectileSkill && skillAction.HitCount > 1 && Mathf.Approximately(skillAction.HitIntervalSeconds, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_PROJECTILE_BURST_INTERVAL_MISSING", "REQ-14", assetPath, "hitIntervalSeconds", "Projectile burst count is greater than one but shot interval is 0.", "Set hitIntervalSeconds above 0 unless all projectiles should fire at once.");
        }

        string[] motionSequenceKeys = skillAction.MotionSequenceKeys;

        if (motionSequenceKeys == null || motionSequenceKeys.Length == 0)
        {
            return;
        }

        if (Mathf.Approximately(skillAction.MotionStepIntervalSeconds, 0f))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MOTION_STEP_INTERVAL_ZERO", "REQ-14", assetPath, "motionStepIntervalSeconds", "Motion sequence has no positive step interval.", "Set motionStepIntervalSeconds above 0 so sequence timing is explicit.");
        }

        for (int motionIndex = 0; motionIndex < motionSequenceKeys.Length; motionIndex++)
        {
            if (string.IsNullOrWhiteSpace(motionSequenceKeys[motionIndex]))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SKILL_MOTION_SEQUENCE_KEY_EMPTY", "REQ-14", assetPath, $"motionSequenceKeys[{motionIndex}]", "Motion sequence contains an empty motion key.", "Remove the empty slot or assign a valid motion key.");
            }
        }
    }

    private static void ValidateLevelTables(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_LevelUpDataSO[] levelTables = LoadAssets<HWJ_LevelUpDataSO>();

        for (int i = 0; i < levelTables.Length; i++)
        {
            HWJ_LevelUpDataSO levelTable = levelTables[i];
            string assetPath = GetAssetPath(levelTable);

            ValidateStableId(validationIssues, firstPathById, "REQ-7", assetPath, "TableId", levelTable.TableId);

            if (levelTable.MaxLevel <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "LEVEL_MAX_INVALID", "REQ-14", assetPath, "maxLevel", "Max level must be greater than 0.", "Set maxLevel to a positive value.");
            }

            SerializedObject serializedLevelTable = new SerializedObject(levelTable);
            SerializedProperty skillPointPerLevel = serializedLevelTable.FindProperty("skillPointPerLevel");
            SerializedProperty skillPointRewardsByLevelUp = serializedLevelTable.FindProperty("skillPointRewardsByLevelUp");
            SerializedProperty experienceArray = serializedLevelTable.FindProperty("experienceToNextLevel");
            int expectedTransitionCount = Mathf.Max(0, levelTable.MaxLevel - 1);

            if (skillPointPerLevel != null && skillPointPerLevel.intValue < 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "LEVEL_SKILL_POINT_PER_LEVEL_NEGATIVE", "REQ-14", assetPath, "skillPointPerLevel", "Skill point per level cannot be negative.", "Set skillPointPerLevel to 0 or higher.");
            }

            if (skillPointRewardsByLevelUp != null && skillPointRewardsByLevelUp.isArray)
            {
                if (expectedTransitionCount > 0
                    && skillPointRewardsByLevelUp.arraySize > 0
                    && skillPointRewardsByLevelUp.arraySize < expectedTransitionCount)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "LEVEL_SKILL_POINT_TABLE_SHORT", "REQ-14", assetPath, "skillPointRewardsByLevelUp", "Skill point reward table is shorter than the level transition count.", "Fill one skill point reward value per level transition or leave the table empty to use skillPointPerLevel.");
                }

                for (int rewardIndex = 0; rewardIndex < skillPointRewardsByLevelUp.arraySize; rewardIndex++)
                {
                    int skillPointReward = skillPointRewardsByLevelUp.GetArrayElementAtIndex(rewardIndex).intValue;

                    if (skillPointReward < 0)
                    {
                        AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "LEVEL_SKILL_POINT_REWARD_NEGATIVE", "REQ-14", assetPath, $"skillPointRewardsByLevelUp[{rewardIndex}]", "Skill point reward cannot be negative.", "Set the reward to 0 or higher.");
                    }
                }
            }

            if (experienceArray == null || !experienceArray.isArray || experienceArray.arraySize == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "EXPERIENCE_TABLE_EMPTY", "REQ-14", assetPath, "experienceToNextLevel", "Experience table is empty.", "Add experience requirements for each level transition.");
                continue;
            }

            if (expectedTransitionCount > 0 && experienceArray.arraySize < expectedTransitionCount)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "EXPERIENCE_TABLE_SHORT", "REQ-14", assetPath, "experienceToNextLevel", "Experience table is shorter than the level transition count.", "Fill one experience requirement per level transition.");
            }

            int previousExperience = 0;

            for (int levelIndex = 0; levelIndex < experienceArray.arraySize; levelIndex++)
            {
                int requiredExperience = experienceArray.GetArrayElementAtIndex(levelIndex).intValue;
                string fieldName = $"experienceToNextLevel[{levelIndex}]";

                if (requiredExperience <= 0)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "EXPERIENCE_REQUIREMENT_INVALID", "REQ-14", assetPath, fieldName, "Experience requirement must be greater than 0.", "Set a positive experience requirement.");
                }

                if (levelIndex > 0 && requiredExperience < previousExperience)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "EXPERIENCE_REQUIREMENT_INVERSION", "REQ-14", assetPath, fieldName, "Experience requirements decrease compared to the previous level.", "Keep later level requirements greater than or equal to earlier ones.");
                }

                previousExperience = requiredExperience;
            }
        }
    }

    private static void ValidateSpawnTables(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathByTableId = new Dictionary<string, string>();
        HWJ_SpawnTableDataSO[] spawnTables = LoadAssets<HWJ_SpawnTableDataSO>();

        for (int i = 0; i < spawnTables.Length; i++)
        {
            HWJ_SpawnTableDataSO spawnTable = spawnTables[i];
            string assetPath = GetAssetPath(spawnTable);

            ValidateStableId(validationIssues, firstPathByTableId, "REQ-7", assetPath, "TableId", spawnTable.TableId);

            if (spawnTable.Entries == null || spawnTable.Entries.Length == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "SPAWN_ENTRIES_EMPTY", "REQ-14", assetPath, "Entries", "Spawn table has no entries.", "Add spawn entries or remove the unused table.");
                continue;
            }

            HashSet<string> spawnIds = new HashSet<string>();

            for (int entryIndex = 0; entryIndex < spawnTable.Entries.Length; entryIndex++)
            {
                HWJ_SpawnEntryData spawnEntry = spawnTable.Entries[entryIndex];
                string fieldPrefix = $"Entries[{entryIndex}]";

                if (spawnEntry == null)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SPAWN_ENTRY_NULL", "REQ-14", assetPath, fieldPrefix, "Spawn entry is null.", "Remove the empty slot or create a valid spawn entry.");
                    continue;
                }

                ValidateInlineId(validationIssues, spawnIds, "REQ-7", assetPath, $"{fieldPrefix}.spawnId", spawnEntry.spawnId);

                if (spawnEntry.spawnCount <= 0)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SPAWN_COUNT_INVALID", "REQ-14", assetPath, $"{fieldPrefix}.spawnCount", "Spawn count must be greater than 0.", "Set spawnCount to a positive value.");
                }

                if (spawnEntry.rootObjectData == null && spawnEntry.prefabOverride == null)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SPAWN_TARGET_MISSING", "REQ-14", assetPath, fieldPrefix, "Spawn entry has neither RootObjectData nor prefab override.", "Assign rootObjectData or prefabOverride.");
                }

                if (spawnEntry.rootObjectData != null
                    && spawnEntry.prefabOverride == null
                    && (spawnEntry.rootObjectData.Model == null || spawnEntry.rootObjectData.Model.modelPrefab == null))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SPAWN_PREFAB_MISSING", "REQ-14", assetPath, $"{fieldPrefix}.rootObjectData", "Spawn entry root data has no model prefab and no prefab override.", "Assign prefabOverride or RootObjectData.Model.modelPrefab.");
                }
            }
        }
    }

    private static void ValidateStatOrbs(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_StatOrbDataSO[] statOrbs = LoadAssets<HWJ_StatOrbDataSO>();

        for (int i = 0; i < statOrbs.Length; i++)
        {
            HWJ_StatOrbDataSO statOrb = statOrbs[i];
            string assetPath = GetAssetPath(statOrb);

            ValidateStableId(validationIssues, firstPathById, "REQ-7", assetPath, "OrbId", statOrb.OrbId);

            if (Mathf.Approximately(statOrb.Amount, 0f))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "STAT_ORB_AMOUNT_ZERO", "REQ-14", assetPath, "amount", "Stat orb amount is 0.", "Set a non-zero amount unless this orb is intentionally cosmetic.");
            }

            if (statOrb.ConfiguredMaxStackCount <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_STACK_LIMIT_INVALID", "REQ-14", assetPath, "maxStackCount", "Stat orb max stack count must be greater than 0.", "Set maxStackCount to at least 1.");
            }

            if (statOrb.OrbPrefab == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "STAT_ORB_PREFAB_MISSING", "REQ-14", assetPath, "orbPrefab", "Stat orb has no pickup prefab.", "Assign the pickup prefab used by reward drops.");
            }
        }
    }

    private static void ValidateBossPatterns(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_BossPatternDataSO[] bossPatterns = LoadAssets<HWJ_BossPatternDataSO>();

        for (int i = 0; i < bossPatterns.Length; i++)
        {
            HWJ_BossPatternDataSO bossPattern = bossPatterns[i];
            string assetPath = GetAssetPath(bossPattern);

            ValidateStableId(validationIssues, firstPathById, "REQ-12", assetPath, "PatternId", bossPattern.PatternId);

            if (bossPattern.Weight <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_PATTERN_WEIGHT_INVALID", "REQ-12", assetPath, "weight", "Boss pattern weight must be greater than 0.", "Set weight to a positive value.");
            }

            if (bossPattern.HpRatio < 0f || bossPattern.HpRatio > 1f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_PATTERN_HP_RATIO_INVALID", "REQ-12", assetPath, "hpRatio", "Boss pattern HP ratio must be between 0 and 1.", "Clamp hpRatio into 0..1.");
            }

            if (bossPattern.SkillActions == null || bossPattern.SkillActions.Length == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_PATTERN_SKILLS_EMPTY", "REQ-12", assetPath, "skillActions", "Boss pattern has no skill actions.", "Assign at least one skill action or confirm this pattern is handled fully by custom code.");
            }
            else
            {
                for (int actionIndex = 0; actionIndex < bossPattern.SkillActions.Length; actionIndex++)
                {
                    if (bossPattern.SkillActions[actionIndex] == null)
                    {
                        AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "BOSS_PATTERN_SKILL_NULL", "REQ-12", assetPath, $"skillActions[{actionIndex}]", "Boss pattern has an empty skill action slot.", "Remove the empty slot or assign a skill action.");
                    }
                }
            }
        }
    }

    private static void ValidateStageAndRegionDefinitions(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, HWJ_ObjectType> rootObjectTypeById)
    {
        // Build ID sets first so reference checks can report missing links without changing source assets.
        HWJ_StageDefinitionDataSO[] stageDefinitions = LoadAssets<HWJ_StageDefinitionDataSO>();
        HWJ_RegionDefinitionDataSO[] regionDefinitions = LoadAssets<HWJ_RegionDefinitionDataSO>();
        Dictionary<string, string> firstPathByStageId = new Dictionary<string, string>();
        Dictionary<string, string> firstPathByRegionId = new Dictionary<string, string>();
        Dictionary<string, HWJ_StageDefinitionDataSO> stageById = new Dictionary<string, HWJ_StageDefinitionDataSO>();
        HashSet<string> stageIds = new HashSet<string>();
        HashSet<string> regionIds = new HashSet<string>();

        for (int i = 0; i < stageDefinitions.Length; i++)
        {
            HWJ_StageDefinitionDataSO stageDefinition = stageDefinitions[i];
            string assetPath = GetAssetPath(stageDefinition);
            string stageId = NormalizeId(stageDefinition.StageId);

            ValidateStableId(validationIssues, firstPathByStageId, "REQ-14", assetPath, "StageId", stageDefinition.StageId);

            if (!string.IsNullOrEmpty(stageId) && !stageById.ContainsKey(stageId))
            {
                stageIds.Add(stageId);
                stageById.Add(stageId, stageDefinition);
            }
        }

        for (int i = 0; i < regionDefinitions.Length; i++)
        {
            HWJ_RegionDefinitionDataSO regionDefinition = regionDefinitions[i];
            string assetPath = GetAssetPath(regionDefinition);
            string regionId = NormalizeId(regionDefinition.RegionId);

            ValidateStableId(validationIssues, firstPathByRegionId, "REQ-14", assetPath, "RegionId", regionDefinition.RegionId);

            if (!string.IsNullOrEmpty(regionId) && !regionIds.Contains(regionId))
            {
                regionIds.Add(regionId);
            }
        }

        ValidateStageDefinitions(validationIssues, stageDefinitions, stageIds, regionIds, rootObjectTypeById);
        ValidateRegionDefinitions(validationIssues, regionDefinitions, stageById, stageIds, regionIds);
        ValidateReachableStagesInRegions(validationIssues, regionDefinitions, stageById);
    }

    private static void ValidateStageDefinitions(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_StageDefinitionDataSO[] stageDefinitions,
        HashSet<string> stageIds,
        HashSet<string> regionIds,
        Dictionary<string, HWJ_ObjectType> rootObjectTypeById)
    {
        Dictionary<string, string> firstStagePathByBossId = new Dictionary<string, string>();

        for (int i = 0; i < stageDefinitions.Length; i++)
        {
            HWJ_StageDefinitionDataSO stageDefinition = stageDefinitions[i];
            string assetPath = GetAssetPath(stageDefinition);
            string stageId = NormalizeId(stageDefinition.StageId);
            string regionId = NormalizeId(stageDefinition.RegionId);
            string nextStageId = NormalizeId(stageDefinition.NextStageId);
            string bossId = NormalizeId(stageDefinition.BossId);
            string unlockRegionId = NormalizeId(stageDefinition.UnlockRegionId);

            if (string.IsNullOrEmpty(regionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_REGION_ID_MISSING", "REQ-14", assetPath, "RegionId", "Stage definition has no region ID.", "Assign the region ID that owns this stage.");
            }
            else if (!regionIds.Contains(regionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_REGION_ID_UNKNOWN", "REQ-14", assetPath, "RegionId", $"Stage region ID '{regionId}' does not exist.", "Create a matching RegionDefinition asset or update RegionId.");
            }

            if (!string.IsNullOrEmpty(nextStageId))
            {
                if (nextStageId == stageId)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_NEXT_STAGE_SELF_REFERENCE", "REQ-14", assetPath, "NextStageId", "Stage cannot point to itself as the next stage.", "Clear NextStageId or assign a different stage ID.");
                }
                else if (!stageIds.Contains(nextStageId))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_NEXT_STAGE_ID_UNKNOWN", "REQ-14", assetPath, "NextStageId", $"Next stage ID '{nextStageId}' does not exist.", "Create a matching StageDefinition asset or update NextStageId.");
                }
            }

            if (stageDefinition.HasBoss && string.IsNullOrEmpty(bossId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_BOSS_ID_MISSING", "REQ-12", assetPath, "BossId", "Stage is marked as having a boss but boss ID is empty.", "Assign a stable boss ID or disable HasBoss.");
            }
            else if (stageDefinition.HasBoss)
            {
                ValidateStageBossRootObjectId(validationIssues, rootObjectTypeById, assetPath, bossId);
                ValidateUniqueStageBossId(validationIssues, firstStagePathByBossId, assetPath, bossId);
            }
            else if (!stageDefinition.HasBoss && !string.IsNullOrEmpty(bossId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "STAGE_BOSS_ID_UNUSED", "REQ-12", assetPath, "BossId", "Stage has a boss ID but HasBoss is disabled.", "Enable HasBoss or clear BossId.");
            }

            if (stageDefinition.UnlocksRegionOnClear && string.IsNullOrEmpty(unlockRegionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_UNLOCK_REGION_ID_MISSING", "REQ-14", assetPath, "UnlockRegionId", "Stage unlocks a region on clear but unlock region ID is empty.", "Assign UnlockRegionId or disable UnlocksRegionOnClear.");
            }
            else if (!string.IsNullOrEmpty(unlockRegionId) && !regionIds.Contains(unlockRegionId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_UNLOCK_REGION_ID_UNKNOWN", "REQ-14", assetPath, "UnlockRegionId", $"Unlock region ID '{unlockRegionId}' does not exist.", "Create a matching RegionDefinition asset or update UnlockRegionId.");
            }

            ValidateStageRequirementIds(validationIssues, stageDefinition, stageIds, regionIds, assetPath, stageId);
        }
    }

    private static void ValidateUniqueStageBossId(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, string> firstStagePathByBossId,
        string assetPath,
        string bossId)
    {
        if (string.IsNullOrEmpty(bossId))
        {
            return;
        }

        if (!firstStagePathByBossId.TryGetValue(bossId, out string firstAssetPath))
        {
            firstStagePathByBossId.Add(bossId, assetPath);
            return;
        }

        AddIssue(
            validationIssues,
            HWJ_GameDataValidationSeverity.Warning,
            "STAGE_BOSS_ID_DUPLICATE",
            "REQ-12",
            assetPath,
            "BossId",
            $"Boss ID '{bossId}' is already used by another stage definition: {firstAssetPath}.",
            "Use a unique boss progression ID per boss encounter, or document that these stages intentionally share boss defeat progress.");
    }

    private static void ValidateStageBossRootObjectId(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, HWJ_ObjectType> rootObjectTypeById,
        string assetPath,
        string bossId)
    {
        if (string.IsNullOrEmpty(bossId))
        {
            return;
        }

        if (rootObjectTypeById == null || !rootObjectTypeById.TryGetValue(bossId, out HWJ_ObjectType objectType))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "STAGE_BOSS_ID_UNKNOWN",
                "REQ-12",
                assetPath,
                "BossId",
                $"Stage boss ID '{bossId}' does not exist in RootObjectData assets.",
                "Create a matching Boss RootObjectData asset or update BossId.");
            return;
        }

        if (objectType != HWJ_ObjectType.Boss)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "STAGE_BOSS_ID_NOT_BOSS",
                "REQ-12",
                assetPath,
                "BossId",
                $"Stage boss ID '{bossId}' points to a RootObjectData with object type {objectType}.",
                "Assign a RootObjectData whose Identity/SelectedTypeData object type is Boss.");
        }
    }

    private static void ValidateStageRequirementIds(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_StageDefinitionDataSO stageDefinition,
        HashSet<string> stageIds,
        HashSet<string> regionIds,
        string assetPath,
        string stageId)
    {
        HashSet<string> requiredStageIds = ValidateConfiguredIdList(
            validationIssues,
            stageDefinition.RequiredClearedStageIds,
            "REQ-14",
            assetPath,
            "RequiredClearedStageIds",
            "STAGE_REQUIRED_STAGE_ID_EMPTY",
            "STAGE_REQUIRED_STAGE_ID_DUPLICATE");
        ValidateKnownIds(
            validationIssues,
            requiredStageIds,
            stageIds,
            "REQ-14",
            assetPath,
            "RequiredClearedStageIds",
            "STAGE_REQUIRED_STAGE_ID_UNKNOWN",
            "Required cleared stage ID does not exist.",
            "Create a matching StageDefinition asset or update RequiredClearedStageIds.");

        if (!string.IsNullOrEmpty(stageId) && requiredStageIds.Contains(stageId))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAGE_REQUIRED_STAGE_SELF_REFERENCE", "REQ-14", assetPath, "RequiredClearedStageIds", "Stage cannot require itself to be cleared.", "Remove this stage ID from RequiredClearedStageIds.");
        }

        HashSet<string> requiredRegionIds = ValidateConfiguredIdList(
            validationIssues,
            stageDefinition.RequiredUnlockedRegionIds,
            "REQ-14",
            assetPath,
            "RequiredUnlockedRegionIds",
            "STAGE_REQUIRED_REGION_ID_EMPTY",
            "STAGE_REQUIRED_REGION_ID_DUPLICATE");
        ValidateKnownIds(
            validationIssues,
            requiredRegionIds,
            regionIds,
            "REQ-14",
            assetPath,
            "RequiredUnlockedRegionIds",
            "STAGE_REQUIRED_REGION_ID_UNKNOWN",
            "Required unlocked region ID does not exist.",
            "Create a matching RegionDefinition asset or update RequiredUnlockedRegionIds.");
    }

    private static void ValidateRegionDefinitions(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RegionDefinitionDataSO[] regionDefinitions,
        Dictionary<string, HWJ_StageDefinitionDataSO> stageById,
        HashSet<string> stageIds,
        HashSet<string> regionIds)
    {
        for (int i = 0; i < regionDefinitions.Length; i++)
        {
            HWJ_RegionDefinitionDataSO regionDefinition = regionDefinitions[i];
            string assetPath = GetAssetPath(regionDefinition);
            string regionId = NormalizeId(regionDefinition.RegionId);
            string firstStageId = NormalizeId(regionDefinition.FirstStageId);
            string nextRegionId = NormalizeId(regionDefinition.NextRegionId);

            if (string.IsNullOrEmpty(firstStageId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_FIRST_STAGE_ID_MISSING", "REQ-14", assetPath, "FirstStageId", "Region definition has no first stage ID.", "Assign the first stage ID for this region.");
            }
            else if (!stageIds.Contains(firstStageId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_FIRST_STAGE_ID_UNKNOWN", "REQ-14", assetPath, "FirstStageId", $"First stage ID '{firstStageId}' does not exist.", "Create a matching StageDefinition asset or update FirstStageId.");
            }

            if (!string.IsNullOrEmpty(nextRegionId))
            {
                if (nextRegionId == regionId)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_NEXT_REGION_SELF_REFERENCE", "REQ-14", assetPath, "NextRegionId", "Region cannot point to itself as the next region.", "Clear NextRegionId or assign a different region ID.");
                }
                else if (!regionIds.Contains(nextRegionId))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_NEXT_REGION_ID_UNKNOWN", "REQ-14", assetPath, "NextRegionId", $"Next region ID '{nextRegionId}' does not exist.", "Create a matching RegionDefinition asset or update NextRegionId.");
                }
            }

            HashSet<string> declaredStageIds = ValidateConfiguredIdList(
                validationIssues,
                regionDefinition.StageIds,
                "REQ-14",
                assetPath,
                "StageIds",
                "REGION_STAGE_ID_EMPTY",
                "REGION_STAGE_ID_DUPLICATE");
            ValidateKnownIds(
                validationIssues,
                declaredStageIds,
                stageIds,
                "REQ-14",
                assetPath,
                "StageIds",
                "REGION_STAGE_ID_UNKNOWN",
                "Region stage ID does not exist.",
                "Create a matching StageDefinition asset or update StageIds.");

            if (declaredStageIds.Count == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "REGION_STAGE_LIST_EMPTY", "REQ-14", assetPath, "StageIds", "Region has no declared stage IDs.", "Add the stages that belong to this region.");
            }

            if (!string.IsNullOrEmpty(firstStageId) && declaredStageIds.Count > 0 && !declaredStageIds.Contains(firstStageId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_FIRST_STAGE_NOT_IN_STAGE_LIST", "REQ-14", assetPath, "FirstStageId", "Region first stage is not included in StageIds.", "Add FirstStageId to StageIds or update FirstStageId.");
            }

            ValidateRegionStageOwnership(validationIssues, stageById, declaredStageIds, assetPath, regionId);
            ValidateRegionRequirementIds(validationIssues, regionDefinition, stageIds, regionIds, assetPath, regionId);
        }
    }

    private static void ValidateRegionStageOwnership(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, HWJ_StageDefinitionDataSO> stageById,
        HashSet<string> declaredStageIds,
        string assetPath,
        string regionId)
    {
        foreach (string declaredStageId in declaredStageIds)
        {
            if (!stageById.TryGetValue(declaredStageId, out HWJ_StageDefinitionDataSO stageDefinition) || stageDefinition == null)
            {
                continue;
            }

            string stageRegionId = NormalizeId(stageDefinition.RegionId);

            if (stageRegionId != regionId)
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "REGION_STAGE_OWNERSHIP_MISMATCH",
                    "REQ-14",
                    assetPath,
                    "StageIds",
                    $"Stage ID '{declaredStageId}' belongs to region '{stageRegionId}', not '{regionId}'.",
                    "Move the stage ID to the matching region or update the stage RegionId.");
            }
        }
    }

    private static void ValidateRegionRequirementIds(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RegionDefinitionDataSO regionDefinition,
        HashSet<string> stageIds,
        HashSet<string> regionIds,
        string assetPath,
        string regionId)
    {
        HashSet<string> requiredStageIds = ValidateConfiguredIdList(
            validationIssues,
            regionDefinition.RequiredClearedStageIds,
            "REQ-14",
            assetPath,
            "RequiredClearedStageIds",
            "REGION_REQUIRED_STAGE_ID_EMPTY",
            "REGION_REQUIRED_STAGE_ID_DUPLICATE");
        ValidateKnownIds(
            validationIssues,
            requiredStageIds,
            stageIds,
            "REQ-14",
            assetPath,
            "RequiredClearedStageIds",
            "REGION_REQUIRED_STAGE_ID_UNKNOWN",
            "Required cleared stage ID does not exist.",
            "Create a matching StageDefinition asset or update RequiredClearedStageIds.");

        HashSet<string> requiredRegionIds = ValidateConfiguredIdList(
            validationIssues,
            regionDefinition.RequiredUnlockedRegionIds,
            "REQ-14",
            assetPath,
            "RequiredUnlockedRegionIds",
            "REGION_REQUIRED_REGION_ID_EMPTY",
            "REGION_REQUIRED_REGION_ID_DUPLICATE");
        ValidateKnownIds(
            validationIssues,
            requiredRegionIds,
            regionIds,
            "REQ-14",
            assetPath,
            "RequiredUnlockedRegionIds",
            "REGION_REQUIRED_REGION_ID_UNKNOWN",
            "Required unlocked region ID does not exist.",
            "Create a matching RegionDefinition asset or update RequiredUnlockedRegionIds.");

        if (!string.IsNullOrEmpty(regionId) && requiredRegionIds.Contains(regionId))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "REGION_REQUIRED_REGION_SELF_REFERENCE", "REQ-14", assetPath, "RequiredUnlockedRegionIds", "Region cannot require itself to be unlocked.", "Remove this region ID from RequiredUnlockedRegionIds.");
        }
    }

    private static void ValidateReachableStagesInRegions(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RegionDefinitionDataSO[] regionDefinitions,
        Dictionary<string, HWJ_StageDefinitionDataSO> stageById)
    {
        for (int i = 0; i < regionDefinitions.Length; i++)
        {
            HWJ_RegionDefinitionDataSO regionDefinition = regionDefinitions[i];
            string assetPath = GetAssetPath(regionDefinition);
            HashSet<string> declaredStageIds = BuildNormalizedIdSet(regionDefinition.StageIds);
            HashSet<string> reachableStageIds = BuildReachableStageIds(regionDefinition, stageById, declaredStageIds);

            foreach (string declaredStageId in declaredStageIds)
            {
                if (!reachableStageIds.Contains(declaredStageId))
                {
                    AddIssue(
                        validationIssues,
                        HWJ_GameDataValidationSeverity.Warning,
                        "REGION_STAGE_UNREACHABLE",
                        "REQ-14",
                        assetPath,
                        "StageIds",
                        $"Stage ID '{declaredStageId}' is declared in this region but is not reachable from FirstStageId through NextStageId links.",
                        "Connect the stage through NextStageId or confirm the stage is intentionally reached by custom logic.");
                }
            }
        }
    }

    private static HashSet<string> BuildReachableStageIds(
        HWJ_RegionDefinitionDataSO regionDefinition,
        Dictionary<string, HWJ_StageDefinitionDataSO> stageById,
        HashSet<string> declaredStageIds)
    {
        // Follow only declared in-region links to avoid treating cross-region jumps as normal stage reachability.
        HashSet<string> reachableStageIds = new HashSet<string>();
        string currentStageId = NormalizeId(regionDefinition.FirstStageId);

        while (!string.IsNullOrEmpty(currentStageId)
            && declaredStageIds.Contains(currentStageId)
            && reachableStageIds.Add(currentStageId)
            && stageById.TryGetValue(currentStageId, out HWJ_StageDefinitionDataSO stageDefinition)
            && stageDefinition != null)
        {
            currentStageId = NormalizeId(stageDefinition.NextStageId);
        }

        return reachableStageIds;
    }

    private static HashSet<string> BuildNormalizedIdSet(string[] ids)
    {
        HashSet<string> normalizedIds = new HashSet<string>();

        if (ids == null)
        {
            return normalizedIds;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            string normalizedId = NormalizeId(ids[i]);

            if (!string.IsNullOrEmpty(normalizedId))
            {
                normalizedIds.Add(normalizedId);
            }
        }

        return normalizedIds;
    }

    private static void ValidateRules(List<HWJ_EditorValidationIssue> validationIssues)
    {
        HashSet<string> gameplayRuleIds = CollectGameplayRuleIds();
        ValidateConditionAssets(validationIssues);
        ValidateGameplayRuleAssets(validationIssues);
        ValidateRuleExecutionCores(validationIssues, gameplayRuleIds);
    }

    private static void ValidatePlayerInputBindings(List<HWJ_EditorValidationIssue> validationIssues)
    {
        HWJ_PlayerInputBindingDataSO[] inputBindingAssets = LoadAssets<HWJ_PlayerInputBindingDataSO>();

        if (inputBindingAssets.Length == 0)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_ASSET_MISSING",
                "REQ-13",
                SearchRoot,
                "HWJ_PlayerInputBindingDataSO",
                "No player input binding definition asset exists under the HWJ data folder.",
                "Create a HWJ_PlayerInputBindingDataSO asset so default controls are explicit data.");
            return;
        }

        for (int assetIndex = 0; assetIndex < inputBindingAssets.Length; assetIndex++)
        {
            ValidatePlayerInputBindingAsset(validationIssues, inputBindingAssets[assetIndex]);
        }
    }

    private static void ValidatePlayerInputBindingAsset(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_PlayerInputBindingDataSO inputBindingAsset)
    {
        string assetPath = GetAssetPath(inputBindingAsset);

        if (inputBindingAsset == null)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_ASSET_NULL",
                "REQ-13",
                assetPath,
                "HWJ_PlayerInputBindingDataSO",
                "Input binding asset reference is null.",
                "Remove the null asset entry or create a valid input binding asset.");
            return;
        }

        if (inputBindingAsset.BindingCount <= 0)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDINGS_EMPTY",
                "REQ-13",
                assetPath,
                "bindings",
                "Input binding asset has no action bindings.",
                "Add one binding for each HWJ_PlayerInputActionId used by the player input system.");
            return;
        }

        HashSet<HWJ_PlayerInputActionId> seenActionIds = new HashSet<HWJ_PlayerInputActionId>();
        Dictionary<KeyCode, HWJ_PlayerInputActionId> firstActionByKeyboardKey = new Dictionary<KeyCode, HWJ_PlayerInputActionId>();
        Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId> firstActionByMouseButton = new Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId>();

        for (int bindingIndex = 0; bindingIndex < inputBindingAsset.BindingCount; bindingIndex++)
        {
            string fieldPrefix = $"bindings[{bindingIndex}]";

            if (!inputBindingAsset.TryGetBindingAt(bindingIndex, out HWJ_PlayerInputBindingEntry bindingEntry))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "INPUT_BINDING_ENTRY_NULL",
                    "REQ-13",
                    assetPath,
                    fieldPrefix,
                    "Input binding entry is null.",
                    "Remove the empty slot or create a valid binding entry.");
                continue;
            }

            ValidatePlayerInputBindingEntry(
                validationIssues,
                assetPath,
                fieldPrefix,
                bindingEntry,
                seenActionIds,
                firstActionByKeyboardKey,
                firstActionByMouseButton);
        }

        ValidateRequiredPlayerInputActions(validationIssues, assetPath, seenActionIds);
    }

    private static void ValidatePlayerInputBindingEntry(
        List<HWJ_EditorValidationIssue> validationIssues,
        string assetPath,
        string fieldPrefix,
        HWJ_PlayerInputBindingEntry bindingEntry,
        HashSet<HWJ_PlayerInputActionId> seenActionIds,
        Dictionary<KeyCode, HWJ_PlayerInputActionId> firstActionByKeyboardKey,
        Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId> firstActionByMouseButton)
    {
        HWJ_PlayerInputActionId actionId = bindingEntry.ActionId;
        KeyCode keyboardKey = bindingEntry.KeyboardKey;
        HWJ_InputMouseButton mouseButton = bindingEntry.MouseButton;
        bool hasValidActionId = System.Enum.IsDefined(typeof(HWJ_PlayerInputActionId), actionId);
        bool hasValidKeyboardKey = System.Enum.IsDefined(typeof(KeyCode), keyboardKey);
        bool hasValidMouseButton = System.Enum.IsDefined(typeof(HWJ_InputMouseButton), mouseButton);

        if (!hasValidActionId)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_ACTION_INVALID",
                "REQ-13",
                assetPath,
                $"{fieldPrefix}.actionId",
                $"Input binding action ID value '{(int)actionId}' is not defined.",
                "Choose a valid HWJ_PlayerInputActionId value.");
        }
        else if (!seenActionIds.Add(actionId))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_ACTION_DUPLICATE",
                "REQ-13",
                assetPath,
                $"{fieldPrefix}.actionId",
                $"Input action '{actionId}' is bound more than once in the same asset.",
                "Keep one default binding per action and use runtime overrides for user changes.");
        }

        if (!hasValidKeyboardKey)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_KEY_INVALID",
                "REQ-13",
                assetPath,
                $"{fieldPrefix}.keyboardKey",
                $"Keyboard key value '{(int)keyboardKey}' is not a valid Unity KeyCode.",
                "Choose a valid KeyCode value or use KeyCode.None when the action uses mouse input.");
        }

        if (!hasValidMouseButton)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_MOUSE_INVALID",
                "REQ-13",
                assetPath,
                $"{fieldPrefix}.mouseButton",
                $"Mouse button value '{(int)mouseButton}' is not defined.",
                "Choose a valid HWJ_InputMouseButton value or use None when the action uses keyboard input.");
        }

        if (keyboardKey == KeyCode.None && mouseButton == HWJ_InputMouseButton.None)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_EMPTY",
                "REQ-13",
                assetPath,
                fieldPrefix,
                $"Input action '{actionId}' has neither keyboard nor mouse input.",
                "Assign a keyboard key or mouse button so the action can be triggered.");
        }

        ValidatePlayerInputBindingConflict(
            validationIssues,
            assetPath,
            fieldPrefix,
            actionId,
            keyboardKey,
            mouseButton,
            hasValidActionId,
            hasValidKeyboardKey,
            hasValidMouseButton,
            firstActionByKeyboardKey,
            firstActionByMouseButton);
    }

    private static void ValidatePlayerInputBindingConflict(
        List<HWJ_EditorValidationIssue> validationIssues,
        string assetPath,
        string fieldPrefix,
        HWJ_PlayerInputActionId actionId,
        KeyCode keyboardKey,
        HWJ_InputMouseButton mouseButton,
        bool hasValidActionId,
        bool hasValidKeyboardKey,
        bool hasValidMouseButton,
        Dictionary<KeyCode, HWJ_PlayerInputActionId> firstActionByKeyboardKey,
        Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId> firstActionByMouseButton)
    {
        if (!hasValidActionId)
        {
            return;
        }

        if (hasValidKeyboardKey && keyboardKey != KeyCode.None)
        {
            if (firstActionByKeyboardKey.TryGetValue(keyboardKey, out HWJ_PlayerInputActionId existingActionId))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "INPUT_BINDING_KEY_DUPLICATE",
                    "REQ-13",
                    assetPath,
                    $"{fieldPrefix}.keyboardKey",
                    $"Keyboard key '{keyboardKey}' is already used by input action '{existingActionId}'.",
                    "Assign a unique default keyboard key or document and enable duplicate runtime binding policy explicitly.");
            }
            else
            {
                firstActionByKeyboardKey.Add(keyboardKey, actionId);
            }
        }

        if (hasValidMouseButton && mouseButton != HWJ_InputMouseButton.None)
        {
            if (firstActionByMouseButton.TryGetValue(mouseButton, out HWJ_PlayerInputActionId existingActionId))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "INPUT_BINDING_MOUSE_DUPLICATE",
                    "REQ-13",
                    assetPath,
                    $"{fieldPrefix}.mouseButton",
                    $"Mouse button '{mouseButton}' is already used by input action '{existingActionId}'.",
                    "Assign a unique default mouse button or document and enable duplicate runtime binding policy explicitly.");
            }
            else
            {
                firstActionByMouseButton.Add(mouseButton, actionId);
            }
        }
    }

    private static void ValidateRequiredPlayerInputActions(
        List<HWJ_EditorValidationIssue> validationIssues,
        string assetPath,
        HashSet<HWJ_PlayerInputActionId> seenActionIds)
    {
        System.Array requiredActionValues = System.Enum.GetValues(typeof(HWJ_PlayerInputActionId));

        for (int i = 0; i < requiredActionValues.Length; i++)
        {
            HWJ_PlayerInputActionId requiredActionId = (HWJ_PlayerInputActionId)requiredActionValues.GetValue(i);

            if (seenActionIds.Contains(requiredActionId))
            {
                continue;
            }

            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "INPUT_BINDING_ACTION_MISSING",
                "REQ-13",
                assetPath,
                "bindings",
                $"Input binding asset does not define action '{requiredActionId}'.",
                "Add a default binding entry for every HWJ_PlayerInputActionId value.");
        }
    }

    private static void ValidateConditionAssets(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_GameplayConditionSO[] conditions = LoadAssets<HWJ_GameplayConditionSO>();
        Dictionary<HWJ_GameplayConditionSO, string> pathByCondition = new Dictionary<HWJ_GameplayConditionSO, string>();

        for (int i = 0; i < conditions.Length; i++)
        {
            HWJ_GameplayConditionSO condition = conditions[i];
            string assetPath = GetAssetPath(condition);
            ValidateStableId(validationIssues, firstPathById, "REQ-14", assetPath, "ConditionId", condition.ConditionId);

            if (condition != null && !pathByCondition.ContainsKey(condition))
            {
                pathByCondition.Add(condition, assetPath);
            }

            ValidateConditionGroup(validationIssues, condition, assetPath);
        }

        ValidateConditionGroupCycles(validationIssues, conditions, pathByCondition);
    }

    private static void ValidateGameplayRuleAssets(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_GameplayRuleSO[] rules = LoadAssets<HWJ_GameplayRuleSO>();

        for (int i = 0; i < rules.Length; i++)
        {
            HWJ_GameplayRuleSO rule = rules[i];
            string assetPath = GetAssetPath(rule);

            ValidateStableId(validationIssues, firstPathById, "REQ-14", assetPath, "RuleId", rule.RuleId);

            if (rule.RootCondition == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "RULE_ROOT_CONDITION_MISSING", "REQ-14", assetPath, "rootCondition", "Gameplay rule has no root condition.", "Assign a condition asset or confirm passWhenNoCondition is intentionally configured.");
            }
        }
    }

    private static void ValidateRuleExecutionCores(
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> gameplayRuleIds)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_RuleExecutionCoreSO[] executionCores = LoadAssets<HWJ_RuleExecutionCoreSO>();

        for (int i = 0; i < executionCores.Length; i++)
        {
            HWJ_RuleExecutionCoreSO executionCore = executionCores[i];
            string assetPath = GetAssetPath(executionCore);

            ValidateStableId(validationIssues, firstPathById, "REQ-14", assetPath, "ExecutionCoreId", executionCore.ExecutionCoreId);

            if (executionCore.Rules == null || executionCore.Rules.Length == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "RULE_EXECUTION_EMPTY", "REQ-14", assetPath, "rules", "Rule execution core has no rule entries.", "Add rule entries or confirm passWhenNoRules is intentionally configured.");
                continue;
            }

            HashSet<string> entryIds = new HashSet<string>();
            HashSet<string> ruleKeysInCore = new HashSet<string>();

            for (int entryIndex = 0; entryIndex < executionCore.Rules.Length; entryIndex++)
            {
                HWJ_RuleExecutionEntry executionEntry = executionCore.Rules[entryIndex];
                string fieldPrefix = $"rules[{entryIndex}]";

                if (executionEntry == null)
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "RULE_EXECUTION_ENTRY_NULL", "REQ-14", assetPath, fieldPrefix, "Rule execution entry is null.", "Remove the empty entry or configure a valid rule entry.");
                    continue;
                }

                if (!string.IsNullOrEmpty(executionEntry.EntryId))
                {
                    ValidateInlineId(validationIssues, entryIds, "REQ-14", assetPath, $"{fieldPrefix}.entryId", executionEntry.EntryId);
                }

                if (executionEntry.Enabled
                    && executionEntry.Rule != null
                    && executionEntry.Rule.Rule == null
                    && string.IsNullOrWhiteSpace(executionEntry.Rule.RuleId))
                {
                    AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "RULE_REFERENCE_MISSING", "REQ-14", assetPath, $"{fieldPrefix}.rule", "Enabled rule execution entry has no rule asset and no rule ID.", "Assign a GameplayRule asset or a valid ruleId.");
                    continue;
                }

                ValidateRuleExecutionReference(
                    validationIssues,
                    executionEntry,
                    gameplayRuleIds,
                    ruleKeysInCore,
                    assetPath,
                    fieldPrefix);
            }
        }
    }

    private static void ValidateConditionGroup(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_GameplayConditionSO condition,
        string assetPath)
    {
        if (!(condition is HWJ_ConditionGroupSO conditionGroup))
        {
            return;
        }

        HWJ_GameplayConditionSO[] childConditions = conditionGroup.Conditions;

        if (childConditions == null || childConditions.Length == 0)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Warning,
                "CONDITION_GROUP_EMPTY",
                "REQ-14",
                assetPath,
                "conditions",
                "Condition group has no child conditions.",
                "Add child conditions or confirm passWhenEmpty is intentionally configured.");
            return;
        }

        HashSet<HWJ_GameplayConditionSO> seenChildConditions = new HashSet<HWJ_GameplayConditionSO>();

        for (int conditionIndex = 0; conditionIndex < childConditions.Length; conditionIndex++)
        {
            HWJ_GameplayConditionSO childCondition = childConditions[conditionIndex];
            string fieldName = $"conditions[{conditionIndex}]";

            if (childCondition == null)
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "CONDITION_GROUP_CHILD_NULL",
                    "REQ-14",
                    assetPath,
                    fieldName,
                    "Condition group has an empty child condition slot.",
                    "Remove the empty slot or assign a valid condition asset.");
                continue;
            }

            if (!seenChildConditions.Add(childCondition))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Warning,
                    "CONDITION_GROUP_CHILD_DUPLICATE",
                    "REQ-14",
                    assetPath,
                    fieldName,
                    $"Condition group references child condition '{GetConditionLabel(childCondition)}' more than once.",
                    "Remove duplicate child condition references unless repeated evaluation is intentional.");
            }
        }
    }

    private static void ValidateConditionGroupCycles(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_GameplayConditionSO[] conditions,
        Dictionary<HWJ_GameplayConditionSO, string> pathByCondition)
    {
        HashSet<HWJ_GameplayConditionSO> visitedConditions = new HashSet<HWJ_GameplayConditionSO>();
        HashSet<HWJ_GameplayConditionSO> reportedConditions = new HashSet<HWJ_GameplayConditionSO>();

        for (int i = 0; i < conditions.Length; i++)
        {
            ValidateConditionGroupCycle(
                validationIssues,
                conditions[i],
                pathByCondition,
                visitedConditions,
                reportedConditions,
                new List<HWJ_GameplayConditionSO>(),
                new HashSet<HWJ_GameplayConditionSO>());
        }
    }

    private static void ValidateConditionGroupCycle(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_GameplayConditionSO condition,
        Dictionary<HWJ_GameplayConditionSO, string> pathByCondition,
        HashSet<HWJ_GameplayConditionSO> visitedConditions,
        HashSet<HWJ_GameplayConditionSO> reportedConditions,
        List<HWJ_GameplayConditionSO> conditionStack,
        HashSet<HWJ_GameplayConditionSO> stackLookup)
    {
        if (condition == null)
        {
            return;
        }

        if (stackLookup.Contains(condition))
        {
            if (reportedConditions.Add(condition))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "CONDITION_GROUP_CYCLE",
                    "REQ-14",
                    GetConditionAssetPath(pathByCondition, condition),
                    "conditions",
                    $"Condition group cycle detected: {BuildConditionCyclePath(conditionStack, condition)}.",
                    "Remove the recursive condition group reference so rule evaluation cannot recurse forever.");
            }

            return;
        }

        if (visitedConditions.Contains(condition))
        {
            return;
        }

        if (!(condition is HWJ_ConditionGroupSO conditionGroup))
        {
            visitedConditions.Add(condition);
            return;
        }

        conditionStack.Add(condition);
        stackLookup.Add(condition);

        HWJ_GameplayConditionSO[] childConditions = conditionGroup.Conditions;

        if (childConditions != null)
        {
            for (int childIndex = 0; childIndex < childConditions.Length; childIndex++)
            {
                ValidateConditionGroupCycle(
                    validationIssues,
                    childConditions[childIndex],
                    pathByCondition,
                    visitedConditions,
                    reportedConditions,
                    conditionStack,
                    stackLookup);
            }
        }

        stackLookup.Remove(condition);
        conditionStack.RemoveAt(conditionStack.Count - 1);
        visitedConditions.Add(condition);
    }

    private static void ValidateRuleExecutionReference(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RuleExecutionEntry executionEntry,
        HashSet<string> gameplayRuleIds,
        HashSet<string> ruleKeysInCore,
        string assetPath,
        string fieldPrefix)
    {
        if (executionEntry == null || executionEntry.Rule == null || !executionEntry.Enabled)
        {
            return;
        }

        HWJ_GameplayRuleReference ruleReference = executionEntry.Rule;
        HWJ_GameplayRuleSO directRule = ruleReference.Rule;
        string configuredRuleId = NormalizeId(ruleReference.RuleId);
        string directRuleId = directRule != null ? NormalizeId(directRule.RuleId) : string.Empty;

        if (directRule != null && string.IsNullOrEmpty(directRuleId))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "RULE_REFERENCE_DIRECT_ASSET_ID_MISSING",
                "REQ-14",
                assetPath,
                $"{fieldPrefix}.rule",
                "Rule execution entry references a GameplayRule asset with an empty RuleId.",
                "Assign a stable RuleId on the referenced rule asset.");
        }

        if (directRule != null
            && !string.IsNullOrEmpty(configuredRuleId)
            && !string.IsNullOrEmpty(directRuleId)
            && configuredRuleId != directRuleId)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Warning,
                "RULE_REFERENCE_ID_MISMATCH",
                "REQ-14",
                assetPath,
                $"{fieldPrefix}.ruleId",
                $"Rule execution entry direct asset ID '{directRuleId}' does not match configured rule ID '{configuredRuleId}'. Runtime uses the direct asset first.",
                "Clear ruleId or make it match the referenced GameplayRule asset.");
        }

        if (directRule == null && !string.IsNullOrEmpty(configuredRuleId))
        {
            if (gameplayRuleIds == null || !gameplayRuleIds.Contains(configuredRuleId))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "RULE_REFERENCE_ID_UNKNOWN",
                    "REQ-14",
                    assetPath,
                    $"{fieldPrefix}.ruleId",
                    $"Rule execution entry references unknown rule ID '{configuredRuleId}'.",
                    "Create a GameplayRule asset with this RuleId or update the reference.");
            }
        }

        string ruleKey = GetRuleExecutionReferenceKey(ruleReference);

        if (string.IsNullOrEmpty(ruleKey))
        {
            return;
        }

        if (!ruleKeysInCore.Add(ruleKey))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Warning,
                "RULE_EXECUTION_DUPLICATE_RULE_REFERENCE",
                "REQ-14",
                assetPath,
                $"{fieldPrefix}.rule",
                "Rule execution core references the same gameplay rule more than once.",
                "Remove duplicate rule entries unless repeated evaluation is intentional.");
        }
    }

    private static string GetRuleExecutionReferenceKey(HWJ_GameplayRuleReference ruleReference)
    {
        if (ruleReference == null)
        {
            return null;
        }

        if (ruleReference.Rule != null)
        {
            string directRuleId = NormalizeId(ruleReference.Rule.RuleId);
            return !string.IsNullOrEmpty(directRuleId)
                ? "rule:" + directRuleId
                : "asset:" + GetAssetPath(ruleReference.Rule);
        }

        string configuredRuleId = NormalizeId(ruleReference.RuleId);
        return !string.IsNullOrEmpty(configuredRuleId) ? "rule:" + configuredRuleId : null;
    }

    private static string GetConditionAssetPath(
        Dictionary<HWJ_GameplayConditionSO, string> pathByCondition,
        HWJ_GameplayConditionSO condition)
    {
        return condition != null
            && pathByCondition != null
            && pathByCondition.TryGetValue(condition, out string assetPath)
            ? assetPath
            : GetAssetPath(condition);
    }

    private static string BuildConditionCyclePath(
        List<HWJ_GameplayConditionSO> conditionStack,
        HWJ_GameplayConditionSO repeatedCondition)
    {
        if (conditionStack == null || repeatedCondition == null)
        {
            return "Unknown cycle";
        }

        int cycleStartIndex = conditionStack.IndexOf(repeatedCondition);
        int startIndex = cycleStartIndex >= 0 ? cycleStartIndex : 0;
        List<string> labels = new List<string>();

        for (int i = startIndex; i < conditionStack.Count; i++)
        {
            labels.Add(GetConditionLabel(conditionStack[i]));
        }

        labels.Add(GetConditionLabel(repeatedCondition));
        return string.Join(" -> ", labels);
    }

    private static string GetConditionLabel(HWJ_GameplayConditionSO condition)
    {
        if (condition == null)
        {
            return "Missing Condition";
        }

        return !string.IsNullOrWhiteSpace(condition.ConditionId) ? condition.ConditionId : condition.name;
    }

    private static void ValidateSaveDtoTypes(List<HWJ_EditorValidationIssue> validationIssues)
    {
        List<System.Type> saveDtoTypes = new List<System.Type>();
        System.Type[] assemblyTypes = typeof(HWJ_GameSaveData).Assembly.GetTypes();

        for (int i = 0; i < assemblyTypes.Length; i++)
        {
            if (IsSaveDtoType(assemblyTypes[i]))
            {
                saveDtoTypes.Add(assemblyTypes[i]);
            }
        }

        saveDtoTypes.Sort((left, right) => string.Compare(left.FullName, right.FullName, System.StringComparison.Ordinal));

        for (int i = 0; i < saveDtoTypes.Count; i++)
        {
            ValidateSaveDtoType(validationIssues, saveDtoTypes[i]);
        }
    }

    private static bool IsSaveDtoType(System.Type candidateType)
    {
        if (candidateType == null
            || candidateType.IsAbstract
            || candidateType.ContainsGenericParameters
            || !candidateType.IsSerializable)
        {
            return false;
        }

        string typeName = candidateType.Name;

        return typeName == nameof(HWJ_GameSaveData)
            || (typeName.StartsWith("HWJ_Save", System.StringComparison.Ordinal)
                && typeName.EndsWith("Data", System.StringComparison.Ordinal));
    }

    private static void ValidateSaveDtoType(
        List<HWJ_EditorValidationIssue> validationIssues,
        System.Type saveDtoType)
    {
        if (saveDtoType == null)
        {
            return;
        }

        // Save DTOs must stay file-serializable. Runtime snapshots may hold Unity references, but save DTO fields may not.
        System.Reflection.FieldInfo[] fields = saveDtoType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        for (int i = 0; i < fields.Length; i++)
        {
            System.Reflection.FieldInfo field = fields[i];

            if (field == null || field.IsNotSerialized)
            {
                continue;
            }

            if (!TryGetUnityObjectReferenceType(field.FieldType, out System.Type unityObjectType))
            {
                continue;
            }

            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SAVE_DTO_UNITY_OBJECT_REFERENCE",
                "REQ-7",
                "Code: Assets/02Scripts/HWJ/Scripts/Data/Save/HWJ_SaveData.cs",
                $"{saveDtoType.Name}.{field.Name}",
                $"Save DTO field stores Unity object reference type '{unityObjectType.Name}'.",
                "Store stable string IDs and serializable values instead of UnityEngine.Object references.");
        }
    }

    private static void ValidateOpenSceneSaveIdentities(List<HWJ_EditorValidationIssue> validationIssues)
    {
        HWJ_RootObjectDataResolver[] sceneResolvers = UnityEngine.Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Dictionary<string, string> firstPathByRewardClaimId = new Dictionary<string, string>();

        for (int i = 0; i < sceneResolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = sceneResolvers[i];

            if (resolver == null || resolver.gameObject == null || EditorUtility.IsPersistent(resolver.gameObject))
            {
                continue;
            }

            if (!RequiresSceneSaveIdentity(resolver.ObjectType))
            {
                continue;
            }

            string sceneObjectPath = GetSceneObjectValidationPath(resolver.gameObject);
            HWJ_SaveIdentityValidationResult validationResult = HWJ_RuntimeSaveIdentity.ValidateRewardClaimIdentity(resolver);

            if (!validationResult.Succeeded)
            {
                AddSceneSaveIdentityIssue(validationIssues, sceneObjectPath, validationResult);
                continue;
            }

            string rewardClaimId = validationResult.RewardClaimId;

            if (string.IsNullOrEmpty(rewardClaimId))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_REWARD_ID_MISSING",
                    "REQ-14",
                    sceneObjectPath,
                    "RewardClaimId",
                    "Scene enemy or boss save identity validated but produced an empty reward claim ID.",
                    "Assign a stableInstanceId and root object ID that can produce rootObjectId:stableInstanceId.");
                continue;
            }

            if (firstPathByRewardClaimId.TryGetValue(rewardClaimId, out string firstSceneObjectPath))
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_DUPLICATE_REWARD_CLAIM_ID",
                    "INV-007",
                    sceneObjectPath,
                    "RewardClaimId",
                    $"Scene enemy or boss duplicates reward claim ID '{rewardClaimId}' already used by {firstSceneObjectPath}.",
                    "Give each manually placed enemy or boss a unique stableInstanceId.");
                continue;
            }

            firstPathByRewardClaimId.Add(rewardClaimId, sceneObjectPath);
        }
    }

    private static void ValidateOpenSceneBossSetups(List<HWJ_EditorValidationIssue> validationIssues)
    {
        HWJ_BossFlowSystem[] bossFlows = FindSceneComponents<HWJ_BossFlowSystem>();
        HWJ_BossBrainSystem[] bossBrains = FindSceneComponents<HWJ_BossBrainSystem>();
        HWJ_RootObjectDataResolver[] resolvers = FindSceneComponents<HWJ_RootObjectDataResolver>();
        HWJ_StageProgressionSystem[] stageSystems = FindSceneComponents<HWJ_StageProgressionSystem>();
        HashSet<int> controlledBossResolverIds = new HashSet<int>();
        HashSet<int> controlledBossBrainIds = new HashSet<int>();
        HashSet<int> controlledStageIds = new HashSet<int>();
        Dictionary<int, string> firstFlowPathByResolverId = new Dictionary<int, string>();

        for (int i = 0; i < bossFlows.Length; i++)
        {
            ValidateSceneBossFlow(
                validationIssues,
                bossFlows[i],
                stageSystems,
                controlledBossResolverIds,
                controlledBossBrainIds,
                controlledStageIds,
                firstFlowPathByResolverId);
        }

        ValidateBossResolversHaveFlow(validationIssues, resolvers, controlledBossResolverIds);
        ValidateBossBrainsHaveFlow(validationIssues, bossBrains, controlledBossBrainIds);
        ValidateBossStagesHaveFlow(validationIssues, stageSystems, controlledStageIds);
    }

    private static void ValidateSceneBossFlow(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossFlowSystem bossFlow,
        HWJ_StageProgressionSystem[] sceneStageSystems,
        HashSet<int> controlledBossResolverIds,
        HashSet<int> controlledBossBrainIds,
        HashSet<int> controlledStageIds,
        Dictionary<int, string> firstFlowPathByResolverId)
    {
        if (bossFlow == null || bossFlow.gameObject == null || EditorUtility.IsPersistent(bossFlow.gameObject))
        {
            return;
        }

        string flowPath = GetSceneObjectValidationPath(bossFlow.gameObject);
        HWJ_StageProgressionSystem stageSystem = ResolveSceneBossStageReference(
            validationIssues,
            bossFlow,
            sceneStageSystems,
            flowPath);
        HWJ_BossBrainSystem bossBrain = ResolveSceneBossBrainReference(bossFlow);
        HWJ_RootObjectDataResolver bossResolver = ResolveSceneBossResolverReference(bossFlow, bossBrain);

        if (stageSystem != null)
        {
            controlledStageIds.Add(stageSystem.GetInstanceID());
        }

        if (bossBrain == null)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_BRAIN_REFERENCE_MISSING",
                "REQ-12",
                flowPath,
                "bossBrainSystem",
                "BossFlowSystem cannot resolve a BossBrainSystem for the boss encounter.",
                "Assign bossBrainSystem or place HWJ_BossFlowSystem on the boss object with HWJ_BossBrainSystem.");
        }
        else
        {
            controlledBossBrainIds.Add(bossBrain.GetInstanceID());
        }

        if (bossResolver == null)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_RESOLVER_REFERENCE_MISSING",
                "REQ-12",
                flowPath,
                "bossResolver",
                "BossFlowSystem cannot resolve a RootObjectDataResolver for the boss.",
                "Assign bossResolver or place HWJ_RootObjectDataResolver on the boss object.");
            return;
        }

        int bossResolverId = bossResolver.GetInstanceID();
        controlledBossResolverIds.Add(bossResolverId);

        if (firstFlowPathByResolverId.TryGetValue(bossResolverId, out string firstFlowPath))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_RESOLVER_CONTROLLED_BY_MULTIPLE_FLOWS",
                "REQ-12",
                flowPath,
                "bossResolver",
                $"Boss resolver is already controlled by another BossFlowSystem at {firstFlowPath}.",
                "Keep one BossFlowSystem responsible for one boss resolver.");
        }
        else
        {
            firstFlowPathByResolverId.Add(bossResolverId, flowPath);
        }

        ValidateSceneBossResolver(validationIssues, flowPath, stageSystem, bossResolver);
    }

    private static HWJ_StageProgressionSystem ResolveSceneBossStageReference(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossFlowSystem bossFlow,
        HWJ_StageProgressionSystem[] sceneStageSystems,
        string flowPath)
    {
        HWJ_StageProgressionSystem explicitStage = GetSerializedObjectReference<HWJ_StageProgressionSystem>(
            bossFlow,
            "stageProgressionSystem");

        if (explicitStage != null)
        {
            return explicitStage;
        }

        HWJ_StageProgressionSystem parentStage = bossFlow.GetComponentInParent<HWJ_StageProgressionSystem>();

        if (parentStage != null)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_BOSS_STAGE_REFERENCE_PARENT_FALLBACK",
                "REQ-12",
                flowPath,
                "stageProgressionSystem",
                "BossFlowSystem has no explicit stage reference and will rely on its parent StageProgressionSystem.",
                "Assign stageProgressionSystem explicitly for production boss scenes.");
            return parentStage;
        }

        if (sceneStageSystems == null || sceneStageSystems.Length == 0)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_STAGE_REFERENCE_MISSING",
                "REQ-12",
                flowPath,
                "stageProgressionSystem",
                "BossFlowSystem cannot find a StageProgressionSystem in the open scene.",
                "Add HWJ_StageProgressionSystem to the boss scene and assign it to boss flow.");
            return null;
        }

        if (sceneStageSystems.Length > 1)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_STAGE_REFERENCE_AMBIGUOUS",
                "REQ-12",
                flowPath,
                "stageProgressionSystem",
                "BossFlowSystem has no explicit stage reference and the scene has multiple StageProgressionSystem instances.",
                "Assign the intended stageProgressionSystem explicitly.");
            return null;
        }

        AddIssue(
            validationIssues,
            HWJ_GameDataValidationSeverity.Warning,
            "SCENE_BOSS_STAGE_REFERENCE_SINGLE_SCENE_FALLBACK",
            "REQ-12",
            flowPath,
            "stageProgressionSystem",
            "BossFlowSystem has no explicit stage reference and will rely on the only StageProgressionSystem in the scene.",
            "Assign stageProgressionSystem explicitly for production boss scenes.");
        return sceneStageSystems[0];
    }

    private static HWJ_BossBrainSystem ResolveSceneBossBrainReference(HWJ_BossFlowSystem bossFlow)
    {
        HWJ_BossBrainSystem explicitBossBrain = GetSerializedObjectReference<HWJ_BossBrainSystem>(
            bossFlow,
            "bossBrainSystem");
        return explicitBossBrain != null ? explicitBossBrain : bossFlow.GetComponent<HWJ_BossBrainSystem>();
    }

    private static HWJ_RootObjectDataResolver ResolveSceneBossResolverReference(
        HWJ_BossFlowSystem bossFlow,
        HWJ_BossBrainSystem bossBrain)
    {
        HWJ_RootObjectDataResolver explicitBossResolver = GetSerializedObjectReference<HWJ_RootObjectDataResolver>(
            bossFlow,
            "bossResolver");

        if (explicitBossResolver != null)
        {
            return explicitBossResolver;
        }

        if (bossBrain != null)
        {
            HWJ_RootObjectDataResolver brainResolver = bossBrain.GetComponent<HWJ_RootObjectDataResolver>();

            if (brainResolver != null)
            {
                return brainResolver;
            }
        }

        return bossFlow.GetComponent<HWJ_RootObjectDataResolver>();
    }

    private static void ValidateSceneBossResolver(
        List<HWJ_EditorValidationIssue> validationIssues,
        string flowPath,
        HWJ_StageProgressionSystem stageSystem,
        HWJ_RootObjectDataResolver bossResolver)
    {
        string bossResolverPath = GetSceneObjectValidationPath(bossResolver.gameObject);

        if (bossResolver.RootObjectData == null)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_ROOT_OBJECT_MISSING",
                "REQ-7",
                bossResolverPath,
                "RootObjectData",
                "Boss resolver has no RootObjectData assigned.",
                "Assign a Boss RootObjectData asset to the boss resolver.");
            return;
        }

        if (bossResolver.ObjectType != HWJ_ObjectType.Boss)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_RESOLVER_NOT_BOSS_TYPE",
                "REQ-12",
                bossResolverPath,
                "RootObjectData.ObjectType",
                "BossFlowSystem points to a resolver whose RootObjectData is not Boss type.",
                "Assign a Boss RootObjectData asset or update the boss flow reference.");
        }

        string bossRootObjectId = GetRootObjectStableId(bossResolver);
        string expectedBossId = GetExpectedStageBossId(stageSystem);

        if (string.IsNullOrEmpty(bossRootObjectId))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_ROOT_OBJECT_ID_MISSING",
                "REQ-7",
                bossResolverPath,
                "RootObjectData.Identity.objectId",
                "Boss RootObjectData has no stable object ID.",
                "Assign a stable boss object ID such as boss.region01.guardian.");
        }

        ValidateSceneBossStageId(validationIssues, flowPath, stageSystem, expectedBossId, bossRootObjectId);
        ValidateSceneBossSaveIdentity(validationIssues, bossResolverPath, expectedBossId, bossResolver);
    }

    private static void ValidateSceneBossStageId(
        List<HWJ_EditorValidationIssue> validationIssues,
        string flowPath,
        HWJ_StageProgressionSystem stageSystem,
        string expectedBossId,
        string bossRootObjectId)
    {
        if (stageSystem == null)
        {
            return;
        }

        string stagePath = GetSceneObjectValidationPath(stageSystem.gameObject);

        if (!IsStageConfiguredForBoss(stageSystem))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_STAGE_NOT_CONFIGURED_FOR_BOSS",
                "REQ-12",
                stagePath,
                "StageDefinition.HasBoss",
                "BossFlowSystem is present but the referenced stage is not configured as a boss stage.",
                "Assign a StageDefinition with HasBoss enabled or set the stage boss fields.");
            return;
        }

        if (string.IsNullOrEmpty(expectedBossId))
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_STAGE_BOSS_ID_MISSING",
                "REQ-12",
                stagePath,
                "BossId",
                "Referenced boss stage has no boss ID.",
                "Set the stage BossId to the same stable ID used by the boss RootObjectData.");
            return;
        }

        if (!string.IsNullOrEmpty(bossRootObjectId) && bossRootObjectId != expectedBossId)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_STAGE_BOSS_ID_MISMATCH",
                "REQ-12",
                flowPath,
                "bossResolver",
                $"Stage boss ID '{expectedBossId}' does not match boss RootObjectData ID '{bossRootObjectId}'.",
                "Use the same stable boss ID in StageDefinition.BossId and Boss RootObjectData.Identity.objectId.");
        }
    }

    private static void ValidateSceneBossSaveIdentity(
        List<HWJ_EditorValidationIssue> validationIssues,
        string bossResolverPath,
        string expectedBossId,
        HWJ_RootObjectDataResolver bossResolver)
    {
        HWJ_SaveIdentityValidationResult saveIdentityResult = HWJ_RuntimeSaveIdentity.ValidateRewardClaimIdentity(bossResolver);

        if (!saveIdentityResult.Succeeded)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_SAVE_IDENTITY_INVALID",
                "REQ-14",
                bossResolverPath,
                saveIdentityResult.FailureCode.ToString(),
                saveIdentityResult.Message,
                "Add HWJ_RuntimeSaveIdentity and assign a stableInstanceId before relying on boss reward claims.");
            return;
        }

        string saveRootObjectId = NormalizeId(HWJ_RuntimeSaveIdentity.ResolveRootObjectId(
            saveIdentityResult.IdentityComponent,
            bossResolver));

        if (!string.IsNullOrEmpty(expectedBossId) && saveRootObjectId != expectedBossId)
        {
            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_SAVE_ROOT_ID_MISMATCH",
                "REQ-12",
                bossResolverPath,
                "HWJ_RuntimeSaveIdentity.rootObjectId",
                $"Boss save identity root ID '{saveRootObjectId}' does not match stage boss ID '{expectedBossId}'.",
                "Clear the override or set it to the same stable ID used by StageDefinition.BossId.");
        }
    }

    private static void ValidateBossResolversHaveFlow(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_RootObjectDataResolver[] resolvers,
        HashSet<int> controlledBossResolverIds)
    {
        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null
                || resolver.gameObject == null
                || EditorUtility.IsPersistent(resolver.gameObject)
                || resolver.ObjectType != HWJ_ObjectType.Boss
                || controlledBossResolverIds.Contains(resolver.GetInstanceID()))
            {
                continue;
            }

            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_FLOW_MISSING_FOR_BOSS",
                "REQ-12",
                GetSceneObjectValidationPath(resolver.gameObject),
                "HWJ_BossFlowSystem",
                "Boss RootObjectData exists in the open scene but no BossFlowSystem controls it.",
                "Add HWJ_BossFlowSystem or assign this boss resolver to an existing boss flow.");
        }
    }

    private static void ValidateBossBrainsHaveFlow(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossBrainSystem[] bossBrains,
        HashSet<int> controlledBossBrainIds)
    {
        for (int i = 0; i < bossBrains.Length; i++)
        {
            HWJ_BossBrainSystem bossBrain = bossBrains[i];

            if (bossBrain == null
                || bossBrain.gameObject == null
                || EditorUtility.IsPersistent(bossBrain.gameObject)
                || controlledBossBrainIds.Contains(bossBrain.GetInstanceID()))
            {
                continue;
            }

            HWJ_RootObjectDataResolver resolver = bossBrain.GetComponent<HWJ_RootObjectDataResolver>();

            if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Boss)
            {
                continue;
            }

            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Warning,
                "SCENE_BOSS_BRAIN_NOT_CONTROLLED_BY_FLOW",
                "REQ-12",
                GetSceneObjectValidationPath(bossBrain.gameObject),
                "HWJ_BossFlowSystem",
                "BossBrainSystem exists on a Boss object but is not referenced by any BossFlowSystem.",
                "Assign this boss brain to the scene BossFlowSystem so boss battle start events activate it.");
        }
    }

    private static void ValidateBossStagesHaveFlow(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_StageProgressionSystem[] stageSystems,
        HashSet<int> controlledStageIds)
    {
        for (int i = 0; i < stageSystems.Length; i++)
        {
            HWJ_StageProgressionSystem stageSystem = stageSystems[i];

            if (stageSystem == null
                || stageSystem.gameObject == null
                || EditorUtility.IsPersistent(stageSystem.gameObject)
                || !IsStageConfiguredForBoss(stageSystem)
                || controlledStageIds.Contains(stageSystem.GetInstanceID()))
            {
                continue;
            }

            AddIssue(
                validationIssues,
                HWJ_GameDataValidationSeverity.Error,
                "SCENE_BOSS_FLOW_MISSING_FOR_STAGE",
                "REQ-12",
                GetSceneObjectValidationPath(stageSystem.gameObject),
                "HWJ_BossFlowSystem",
                "Stage is configured as a boss stage but no BossFlowSystem references it.",
                "Add or assign a BossFlowSystem for this boss stage.");
        }
    }

    private static bool IsStageConfiguredForBoss(HWJ_StageProgressionSystem stageSystem)
    {
        if (stageSystem == null)
        {
            return false;
        }

        if (stageSystem.StageDefinition != null)
        {
            return stageSystem.StageDefinition.HasBoss;
        }

        return stageSystem.CurrentStageHasBoss;
    }

    private static string GetExpectedStageBossId(HWJ_StageProgressionSystem stageSystem)
    {
        if (stageSystem == null)
        {
            return string.Empty;
        }

        if (stageSystem.StageDefinition != null)
        {
            return stageSystem.StageDefinition.HasBoss
                ? NormalizeId(stageSystem.StageDefinition.BossId)
                : string.Empty;
        }

        return stageSystem.CurrentStageHasBoss ? NormalizeId(stageSystem.BossId) : string.Empty;
    }

    private static string GetRootObjectStableId(HWJ_RootObjectDataResolver resolver)
    {
        return resolver != null
            && resolver.RootObjectData != null
            && resolver.RootObjectData.Identity != null
            ? NormalizeId(resolver.RootObjectData.Identity.objectId)
            : string.Empty;
    }

    private static void AddSceneSaveIdentityIssue(
        List<HWJ_EditorValidationIssue> validationIssues,
        string sceneObjectPath,
        HWJ_SaveIdentityValidationResult validationResult)
    {
        switch (validationResult.FailureCode)
        {
            case HWJ_SaveIdentityValidationFailureCode.MissingIdentityComponent:
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_COMPONENT_MISSING",
                    "REQ-14",
                    sceneObjectPath,
                    "HWJ_RuntimeSaveIdentity",
                    "Scene enemy or boss has no runtime save identity component.",
                    "Add HWJ_RuntimeSaveIdentity and assign a unique stableInstanceId for manually placed enemies and bosses.");
                break;
            case HWJ_SaveIdentityValidationFailureCode.MissingStableInstanceId:
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_STABLE_ID_MISSING",
                    "REQ-14",
                    sceneObjectPath,
                    "stableInstanceId",
                    "Scene enemy or boss runtime save identity has an empty stable instance ID.",
                    "Set stableInstanceId to a unique, stable string that will not change when the object is renamed.");
                break;
            case HWJ_SaveIdentityValidationFailureCode.MissingRootObjectId:
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_ROOT_ID_MISSING",
                    "REQ-7",
                    sceneObjectPath,
                    "rootObjectId",
                    "Scene enemy or boss cannot resolve a root object ID for reward claim storage.",
                    "Assign RootObjectData with a stable identity objectId or set rootObjectId on HWJ_RuntimeSaveIdentity.");
                break;
            default:
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SCENE_SAVE_IDENTITY_INVALID",
                    "REQ-14",
                    sceneObjectPath,
                    validationResult.FailureCode.ToString(),
                    validationResult.Message,
                    "Fix the scene enemy or boss save identity before relying on saved reward claims.");
                break;
        }
    }

    private static bool RequiresSceneSaveIdentity(HWJ_ObjectType objectType)
    {
        return objectType == HWJ_ObjectType.Enemy || objectType == HWJ_ObjectType.Boss;
    }

    private static string GetSceneObjectValidationPath(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return "Missing scene object";
        }

        string scenePath = targetObject.scene.IsValid() ? targetObject.scene.path : null;

        if (string.IsNullOrEmpty(scenePath))
        {
            scenePath = string.IsNullOrEmpty(targetObject.scene.name) ? "Unsaved Scene" : targetObject.scene.name;
        }

        return $"{scenePath}/{GetHierarchyPath(targetObject.transform)}";
    }

    private static string GetHierarchyPath(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return "Missing Transform";
        }

        string hierarchyPath = targetTransform.name;
        Transform parentTransform = targetTransform.parent;

        while (parentTransform != null)
        {
            hierarchyPath = parentTransform.name + "/" + hierarchyPath;
            parentTransform = parentTransform.parent;
        }

        return hierarchyPath;
    }

    private static T[] FindSceneComponents<T>() where T : Component
    {
        T[] foundComponents = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<T> sceneComponents = new List<T>();

        for (int i = 0; i < foundComponents.Length; i++)
        {
            T foundComponent = foundComponents[i];

            if (foundComponent == null
                || foundComponent.gameObject == null
                || EditorUtility.IsPersistent(foundComponent.gameObject))
            {
                continue;
            }

            sceneComponents.Add(foundComponent);
        }

        return sceneComponents.ToArray();
    }

    private static T GetSerializedObjectReference<T>(UnityEngine.Object targetObject, string propertyName)
        where T : UnityEngine.Object
    {
        if (targetObject == null || string.IsNullOrEmpty(propertyName))
        {
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(targetObject);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static bool TryGetUnityObjectReferenceType(System.Type checkedType, out System.Type unityObjectType)
    {
        unityObjectType = null;

        if (checkedType == null)
        {
            return false;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(checkedType))
        {
            unityObjectType = checkedType;
            return true;
        }

        if (checkedType.IsArray)
        {
            return TryGetUnityObjectReferenceType(checkedType.GetElementType(), out unityObjectType);
        }

        if (!checkedType.IsGenericType)
        {
            return false;
        }

        System.Type[] genericArguments = checkedType.GetGenericArguments();

        for (int i = 0; i < genericArguments.Length; i++)
        {
            if (TryGetUnityObjectReferenceType(genericArguments[i], out unityObjectType))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateSkillSet(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_SkillSetData skillSet,
        HashSet<string> skillActionIds,
        string assetPath,
        string fieldPrefix,
        string requirementId)
    {
        if (skillSet == null || skillSet.skills == null)
        {
            return;
        }

        HashSet<string> skillIds = new HashSet<string>();

        for (int i = 0; i < skillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData skillEntry = skillSet.skills[i];
            string fieldName = $"{fieldPrefix}.skills[{i}]";

            if (skillEntry == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_ENTRY_NULL", requirementId, assetPath, fieldName, "Skill entry is null.", "Remove the empty slot or add a valid skill entry.");
                continue;
            }

            ValidateInlineId(validationIssues, skillIds, requirementId, assetPath, $"{fieldName}.skillId", skillEntry.skillId);

            if (!string.IsNullOrWhiteSpace(skillEntry.skillId)
                && skillActionIds != null
                && !skillActionIds.Contains(skillEntry.skillId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_ACTION_REFERENCE_MISSING", requirementId, assetPath, $"{fieldName}.skillId", $"Skill action ID '{skillEntry.skillId}' does not exist.", "Create a matching HWJ_SkillActionDataSO or update the skillId.");
            }

            ValidateNonNegative(validationIssues, skillEntry.cooldownSeconds, requirementId, assetPath, $"{fieldName}.cooldownSeconds", "SKILL_ENTRY_COOLDOWN_NEGATIVE");
            ValidateNonNegative(validationIssues, skillEntry.useIntervalSeconds, requirementId, assetPath, $"{fieldName}.useIntervalSeconds", "SKILL_ENTRY_INTERVAL_NEGATIVE");
            ValidateNonNegative(validationIssues, skillEntry.unlockLevel, requirementId, assetPath, $"{fieldName}.unlockLevel", "SKILL_ENTRY_UNLOCK_LEVEL_NEGATIVE");
            ValidateNonNegative(validationIssues, skillEntry.requiredSkillPoint, requirementId, assetPath, $"{fieldName}.requiredSkillPoint", "SKILL_ENTRY_REQUIRED_POINT_NEGATIVE");

            if (skillEntry.startsUnlocked && skillEntry.requiredSkillPoint > 0)
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Error,
                    "SKILL_ENTRY_START_UNLOCK_COST_CONFLICT",
                    requirementId,
                    assetPath,
                    $"{fieldName}.requiredSkillPoint",
                    "Skill starts unlocked but also requires skill points. Runtime starting unlock bypasses this cost.",
                    "Set requiredSkillPoint to 0, or disable startsUnlocked and unlock it through progression.");
            }

            if (skillEntry.startsUnlocked && skillEntry.unlockLevel > 1)
            {
                AddIssue(
                    validationIssues,
                    HWJ_GameDataValidationSeverity.Warning,
                    "SKILL_ENTRY_START_UNLOCK_LEVEL_CONFLICT",
                    requirementId,
                    assetPath,
                    $"{fieldName}.unlockLevel",
                    "Skill starts unlocked but also has a later unlock level. Runtime starting unlock bypasses this level gate.",
                    "Set unlockLevel to 0 or 1, or disable startsUnlocked and unlock it through level progression.");
            }
        }
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private static HashSet<string> ValidateConfiguredIdList(
        List<HWJ_EditorValidationIssue> validationIssues,
        string[] configuredIds,
        string requirementId,
        string assetPath,
        string fieldPrefix,
        string emptyValidationCode,
        string duplicateValidationCode)
    {
        HashSet<string> ids = new HashSet<string>();

        if (configuredIds == null)
        {
            return ids;
        }

        for (int i = 0; i < configuredIds.Length; i++)
        {
            string rawId = configuredIds[i];
            string fieldName = $"{fieldPrefix}[{i}]";

            if (string.IsNullOrWhiteSpace(rawId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, emptyValidationCode, requirementId, assetPath, fieldName, "Configured ID entry is empty.", "Remove the empty slot or assign a valid stable ID.");
                continue;
            }

            string normalizedId = rawId.Trim();

            if (!ids.Add(normalizedId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, duplicateValidationCode, requirementId, assetPath, fieldName, $"Configured ID '{normalizedId}' is duplicated in the same list.", "Keep each configured ID unique.");
            }
        }

        return ids;
    }

    private static void ValidateKnownIds(
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> configuredIds,
        HashSet<string> knownIds,
        string requirementId,
        string assetPath,
        string fieldPrefix,
        string validationCode,
        string problem,
        string fix,
        HWJ_GameDataValidationSeverity severity = HWJ_GameDataValidationSeverity.Error)
    {
        if (configuredIds == null || knownIds == null)
        {
            return;
        }

        foreach (string configuredId in configuredIds)
        {
            if (!knownIds.Contains(configuredId))
            {
                AddIssue(validationIssues, severity, validationCode, requirementId, assetPath, fieldPrefix, $"{problem} ID:'{configuredId}'.", fix);
            }
        }
    }

    private static HashSet<string> CollectSkillActionIds()
    {
        HashSet<string> skillActionIds = new HashSet<string>();
        HWJ_SkillActionDataSO[] skillActions = LoadAssets<HWJ_SkillActionDataSO>();

        for (int i = 0; i < skillActions.Length; i++)
        {
            if (skillActions[i] != null && !string.IsNullOrWhiteSpace(skillActions[i].SkillActionId))
            {
                skillActionIds.Add(skillActions[i].SkillActionId);
            }
        }

        return skillActionIds;
    }

    private static HashSet<string> CollectRootObjectIds()
    {
        HashSet<string> rootObjectIds = new HashSet<string>();
        HWJ_RootObjectDataSO[] rootObjects = LoadAssets<HWJ_RootObjectDataSO>();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] != null
                && rootObjects[i].Identity != null
                && !string.IsNullOrWhiteSpace(rootObjects[i].Identity.objectId))
            {
                rootObjectIds.Add(rootObjects[i].Identity.objectId.Trim());
            }
        }

        return rootObjectIds;
    }

    private static Dictionary<string, HWJ_ObjectType> CollectRootObjectTypeById()
    {
        Dictionary<string, HWJ_ObjectType> rootObjectTypeById = new Dictionary<string, HWJ_ObjectType>();
        HWJ_RootObjectDataSO[] rootObjects = LoadAssets<HWJ_RootObjectDataSO>();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            HWJ_RootObjectDataSO rootObject = rootObjects[i];

            if (rootObject == null
                || rootObject.Identity == null
                || string.IsNullOrWhiteSpace(rootObject.Identity.objectId))
            {
                continue;
            }

            string rootObjectId = rootObject.Identity.objectId.Trim();

            if (!rootObjectTypeById.ContainsKey(rootObjectId))
            {
                rootObjectTypeById.Add(rootObjectId, rootObject.ObjectType);
            }
        }

        return rootObjectTypeById;
    }

    private static HashSet<string> CollectPlayerSkillIds()
    {
        HashSet<string> playerSkillIds = new HashSet<string>();
        HWJ_PlayerTypeDataSO[] playerTypes = LoadAssets<HWJ_PlayerTypeDataSO>();

        for (int i = 0; i < playerTypes.Length; i++)
        {
            HWJ_SkillSetData skillSet = playerTypes[i] != null ? playerTypes[i].SkillSet : null;

            if (skillSet == null || skillSet.skills == null)
            {
                continue;
            }

            for (int skillIndex = 0; skillIndex < skillSet.skills.Length; skillIndex++)
            {
                HWJ_SkillEntryData skillEntry = skillSet.skills[skillIndex];

                if (skillEntry != null && !string.IsNullOrWhiteSpace(skillEntry.skillId))
                {
                    playerSkillIds.Add(skillEntry.skillId.Trim());
                }
            }
        }

        return playerSkillIds;
    }

    private static HashSet<string> CollectStatOrbIds()
    {
        HashSet<string> statOrbIds = new HashSet<string>();
        HWJ_StatOrbDataSO[] statOrbs = LoadAssets<HWJ_StatOrbDataSO>();

        for (int i = 0; i < statOrbs.Length; i++)
        {
            if (statOrbs[i] != null && !string.IsNullOrWhiteSpace(statOrbs[i].OrbId))
            {
                statOrbIds.Add(statOrbs[i].OrbId.Trim());
            }
        }

        return statOrbIds;
    }

    private static HashSet<string> CollectGameplayRuleIds()
    {
        HashSet<string> gameplayRuleIds = new HashSet<string>();
        HWJ_GameplayRuleSO[] gameplayRules = LoadAssets<HWJ_GameplayRuleSO>();

        for (int i = 0; i < gameplayRules.Length; i++)
        {
            if (gameplayRules[i] != null && !string.IsNullOrWhiteSpace(gameplayRules[i].RuleId))
            {
                gameplayRuleIds.Add(gameplayRules[i].RuleId.Trim());
            }
        }

        return gameplayRuleIds;
    }

    private static HashSet<string> CollectRuleExecutionCoreIds()
    {
        HashSet<string> executionCoreIds = new HashSet<string>();
        HWJ_RuleExecutionCoreSO[] executionCores = LoadAssets<HWJ_RuleExecutionCoreSO>();

        for (int i = 0; i < executionCores.Length; i++)
        {
            if (executionCores[i] != null && !string.IsNullOrWhiteSpace(executionCores[i].ExecutionCoreId))
            {
                executionCoreIds.Add(executionCores[i].ExecutionCoreId.Trim());
            }
        }

        return executionCoreIds;
    }

    private static void ValidateStableId(
        List<HWJ_EditorValidationIssue> validationIssues,
        Dictionary<string, string> firstPathById,
        string requirementId,
        string assetPath,
        string fieldName,
        string idValue)
    {
        if (string.IsNullOrWhiteSpace(idValue))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ID_MISSING", requirementId, assetPath, fieldName, "Stable ID is empty.", "Assign a non-empty stable string ID.");
            return;
        }

        if (firstPathById == null)
        {
            return;
        }

        if (firstPathById.TryGetValue(idValue, out string firstAssetPath))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ID_DUPLICATE", requirementId, assetPath, fieldName, $"Stable ID duplicates {firstAssetPath}.", "Assign a unique stable string ID.");
            return;
        }

        firstPathById[idValue] = assetPath;
    }

    private static void ValidateInlineId(
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> idsInAsset,
        string requirementId,
        string assetPath,
        string fieldName,
        string idValue)
    {
        if (string.IsNullOrWhiteSpace(idValue))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ID_MISSING", requirementId, assetPath, fieldName, "Inline stable ID is empty.", "Assign a non-empty stable string ID.");
            return;
        }

        if (idsInAsset == null)
        {
            return;
        }

        if (!idsInAsset.Add(idValue))
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "ID_DUPLICATE", requirementId, assetPath, fieldName, "Inline stable ID is duplicated inside the same asset.", "Use a unique ID within this asset.");
        }
    }

    private static void ValidateNonNegative(
        List<HWJ_EditorValidationIssue> validationIssues,
        float value,
        string requirementId,
        string assetPath,
        string fieldName,
        string validationCode)
    {
        if (value < 0f)
        {
            AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, validationCode, requirementId, assetPath, fieldName, "Value cannot be negative.", "Set the value to 0 or a positive number.");
        }
    }

    private static T[] LoadAssets<T>() where T : UnityEngine.Object
    {
        string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { SearchRoot });
        List<T> loadedAssets = new List<T>();

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (loadedAsset != null)
            {
                loadedAssets.Add(loadedAsset);
            }
        }

        return loadedAssets.ToArray();
    }

    private static string GetAssetPath(UnityEngine.Object asset)
    {
        return asset != null ? AssetDatabase.GetAssetPath(asset) : "Missing asset";
    }

    private static void AddIssue(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_GameDataValidationSeverity severity,
        string validationCode,
        string requirementId,
        string assetPath,
        string fieldName,
        string problem,
        string fix)
    {
        validationIssues.Add(new HWJ_EditorValidationIssue(
            severity,
            validationCode,
            requirementId,
            assetPath,
            fieldName,
            problem,
            fix));
    }

    private static void LogReport(HWJ_EditorValidationReport validationReport)
    {
        if (validationReport.Issues.Count == 0)
        {
            Debug.Log("[HWJ Validation] No game data issues found.");
            return;
        }

        for (int i = 0; i < validationReport.Issues.Count; i++)
        {
            HWJ_EditorValidationIssue issue = validationReport.Issues[i];
            string line = issue.ToLogLine();

            if (issue.Severity == HWJ_GameDataValidationSeverity.Error)
            {
                Debug.LogError(line);
            }
            else if (issue.Severity == HWJ_GameDataValidationSeverity.Warning)
            {
                Debug.LogWarning(line);
            }
            else
            {
                Debug.Log(line);
            }
        }

        string summary = $"[HWJ Validation] Errors:{validationReport.ErrorCount}, Warnings:{validationReport.WarningCount}, Info:{validationReport.InfoCount}";

        if (validationReport.ErrorCount > 0)
        {
            Debug.LogError(summary);
        }
        else
        {
            Debug.Log(summary);
        }
    }

    public readonly struct HWJ_EditorValidationIssue
    {
        public readonly HWJ_GameDataValidationSeverity Severity;
        public readonly string ValidationCode;
        public readonly string RequirementId;
        public readonly string AssetPath;
        public readonly string FieldName;
        public readonly string Problem;
        public readonly string Fix;

        public HWJ_EditorValidationIssue(
            HWJ_GameDataValidationSeverity severity,
            string validationCode,
            string requirementId,
            string assetPath,
            string fieldName,
            string problem,
            string fix)
        {
            Severity = severity;
            ValidationCode = validationCode;
            RequirementId = requirementId;
            AssetPath = assetPath;
            FieldName = fieldName;
            Problem = problem;
            Fix = fix;
        }

        public string ToLogLine()
        {
            return $"[HWJ Validation][{Severity}][{ValidationCode}][{RequirementId}] Asset:{AssetPath} Field:{FieldName} Problem:{Problem} Fix:{Fix}";
        }
    }

    public sealed class HWJ_EditorValidationReport
    {
        public readonly List<HWJ_EditorValidationIssue> Issues;
        public readonly int ErrorCount;
        public readonly int WarningCount;
        public readonly int InfoCount;
        public bool IsValid => ErrorCount == 0;

        private HWJ_EditorValidationReport(
            List<HWJ_EditorValidationIssue> issues,
            int errorCount,
            int warningCount,
            int infoCount)
        {
            Issues = issues;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            InfoCount = infoCount;
        }

        public static HWJ_EditorValidationReport Create(List<HWJ_EditorValidationIssue> sourceIssues)
        {
            List<HWJ_EditorValidationIssue> reportIssues = sourceIssues ?? new List<HWJ_EditorValidationIssue>();
            int errorCount = 0;
            int warningCount = 0;
            int infoCount = 0;

            for (int i = 0; i < reportIssues.Count; i++)
            {
                switch (reportIssues[i].Severity)
                {
                    case HWJ_GameDataValidationSeverity.Error:
                        errorCount++;
                        break;
                    case HWJ_GameDataValidationSeverity.Warning:
                        warningCount++;
                        break;
                    default:
                        infoCount++;
                        break;
                }
            }

            return new HWJ_EditorValidationReport(reportIssues, errorCount, warningCount, infoCount);
        }

        public string ToSummaryText()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Errors:");
            builder.Append(ErrorCount);
            builder.Append(", Warnings:");
            builder.Append(WarningCount);
            builder.Append(", Info:");
            builder.Append(InfoCount);
            return builder.ToString();
        }
    }
}
