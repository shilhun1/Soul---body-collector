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

    [MenuItem(MenuPath)]
    public static void ValidateAllGameDataFromMenu()
    {
        HWJ_EditorValidationReport validationReport = ValidateAllGameData();
        LogReport(validationReport);
    }

    public static HWJ_EditorValidationReport ValidateAllGameData()
    {
        List<HWJ_EditorValidationIssue> validationIssues = new List<HWJ_EditorValidationIssue>();
        HashSet<string> skillActionIds = CollectSkillActionIds();

        // Keep each domain check separate so the validator can grow without becoming a hidden game manager.
        ValidateGameplayDatabases(validationIssues);
        ValidateRootObjects(validationIssues);
        ValidateObjectTypeData(validationIssues, skillActionIds);
        ValidateSkillActions(validationIssues);
        ValidateLevelTables(validationIssues);
        ValidateSpawnTables(validationIssues);
        ValidateStatOrbs(validationIssues);
        ValidateBossPatterns(validationIssues);
        ValidateRules(validationIssues);

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
                ValidateNonNegative(validationIssues, rootObject.ReceivedDamage.knockbackWeightMultiplier, "REQ-14", assetPath, "ReceivedDamage.knockbackWeightMultiplier", "KNOCKBACK_WEIGHT_NEGATIVE");
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

            if (rootObject.Reward != null && rootObject.Reward.dropsStatOrb && string.IsNullOrWhiteSpace(rootObject.Reward.statOrbId))
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "STAT_ORB_REWARD_ID_MISSING", "REQ-14", assetPath, "Reward.statOrbId", "Reward is configured to drop a stat orb but statOrbId is empty.", "Set statOrbId to an existing stat orb ID.");
            }
        }
    }

    private static void ValidateObjectTypeData(List<HWJ_EditorValidationIssue> validationIssues, HashSet<string> skillActionIds)
    {
        Dictionary<string, string> firstPathByTypeId = new Dictionary<string, string>();

        ValidateTypedAssets(LoadAssets<HWJ_PlayerTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds);
        ValidateTypedAssets(LoadAssets<HWJ_EnemyTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds);
        ValidateTypedAssets(LoadAssets<HWJ_BossTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds);
        ValidateTypedAssets(LoadAssets<HWJ_NPCTypeDataSO>(), firstPathByTypeId, validationIssues, skillActionIds);
    }

    private static void ValidateTypedAssets<T>(
        T[] typedAssets,
        Dictionary<string, string> firstPathByTypeId,
        List<HWJ_EditorValidationIssue> validationIssues,
        HashSet<string> skillActionIds) where T : HWJ_ObjectTypeDataSO
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
                ValidateBossTypeData(validationIssues, bossType, skillActionIds, assetPath);
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
        }

        if (enemyType.State != null)
        {
            ValidateNonNegative(validationIssues, enemyType.State.attackRange, "REQ-14", assetPath, "State.attackRange", "ATTACK_RANGE_NEGATIVE");
        }
    }

    private static void ValidateBossTypeData(
        List<HWJ_EditorValidationIssue> validationIssues,
        HWJ_BossTypeDataSO bossType,
        HashSet<string> skillActionIds,
        string assetPath)
    {
        ValidateSkillSet(validationIssues, bossType.SkillCycle, skillActionIds, assetPath, "SkillCycle", "REQ-7");

        if (bossType.FSM != null)
        {
            if (bossType.FSM.phaseTwoHpRatio <= 0f || bossType.FSM.phaseTwoHpRatio >= 1f)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "BOSS_PHASE_RATIO_SUSPICIOUS", "REQ-12", assetPath, "FSM.phaseTwoHpRatio", "Phase two HP ratio should normally be between 0 and 1.", "Set a ratio such as 0.5 for a 50 percent phase transition.");
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

            ValidateNonNegative(validationIssues, skillAction.DamageMultiplier, "REQ-14", assetPath, "damageMultiplier", "SKILL_DAMAGE_MULTIPLIER_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.CooldownSeconds, "REQ-14", assetPath, "cooldownSeconds", "SKILL_COOLDOWN_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitStartSeconds, "REQ-14", assetPath, "hitStartSeconds", "SKILL_HIT_START_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.HitActiveSeconds, "REQ-14", assetPath, "hitActiveSeconds", "SKILL_HIT_ACTIVE_NEGATIVE");
            ValidateNonNegative(validationIssues, skillAction.RecoverySeconds, "REQ-14", assetPath, "recoverySeconds", "SKILL_RECOVERY_NEGATIVE");

            if (skillAction.HitCount <= 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Error, "SKILL_HIT_COUNT_INVALID", "REQ-14", assetPath, "hitCount", "Skill hit count must be at least 1.", "Set hitCount to 1 or higher.");
            }

            if (skillAction.ActionType == HWJ_SkillActionType.Projectile && skillAction.ProjectilePrefab == null)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "PROJECTILE_PREFAB_MISSING", "REQ-14", assetPath, "projectilePrefab", "Projectile skill has no projectile prefab.", "Assign the projectile prefab used by the skill action.");
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
            SerializedProperty experienceArray = serializedLevelTable.FindProperty("experienceToNextLevel");

            if (experienceArray == null || !experienceArray.isArray || experienceArray.arraySize == 0)
            {
                AddIssue(validationIssues, HWJ_GameDataValidationSeverity.Warning, "EXPERIENCE_TABLE_EMPTY", "REQ-14", assetPath, "experienceToNextLevel", "Experience table is empty.", "Add experience requirements for each level transition.");
                continue;
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

    private static void ValidateRules(List<HWJ_EditorValidationIssue> validationIssues)
    {
        ValidateConditionAssets(validationIssues);
        ValidateGameplayRuleAssets(validationIssues);
        ValidateRuleExecutionCores(validationIssues);
    }

    private static void ValidateConditionAssets(List<HWJ_EditorValidationIssue> validationIssues)
    {
        Dictionary<string, string> firstPathById = new Dictionary<string, string>();
        HWJ_GameplayConditionSO[] conditions = LoadAssets<HWJ_GameplayConditionSO>();

        for (int i = 0; i < conditions.Length; i++)
        {
            HWJ_GameplayConditionSO condition = conditions[i];
            ValidateStableId(validationIssues, firstPathById, "REQ-14", GetAssetPath(condition), "ConditionId", condition.ConditionId);
        }
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

    private static void ValidateRuleExecutionCores(List<HWJ_EditorValidationIssue> validationIssues)
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
                }
            }
        }
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
