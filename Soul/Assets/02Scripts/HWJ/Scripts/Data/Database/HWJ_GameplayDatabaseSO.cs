using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전반에서 사용하는 주요 데이터 에셋을 한 곳에 모아두는 데이터베이스입니다.
/// 각 시스템이 개별 SO를 직접 많이 들고 있지 않도록, ID 기반 조회의 중심으로 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_GameplayDatabase", menuName = "HWJ/Data/Gameplay Database")]
public class HWJ_GameplayDatabaseSO : ScriptableObject
{
    [Header("오브젝트 데이터")]
    [Tooltip("플레이어, 적, NPC, 보스의 RootObjectDataSO 목록입니다.")]
    [InspectorName("RootObjectData 목록")]
    [SerializeField] private HWJ_RootObjectDataSO[] rootObjects;

    [Header("시스템 데이터")]
    [Tooltip("오브젝트 풀 전체 설정입니다.")]
    [InspectorName("오브젝트 풀 데이터")]
    [SerializeField] private HWJ_ObjectPoolDataSO objectPoolData;
    [Tooltip("스테이지/구간별 스폰 테이블 목록입니다.")]
    [InspectorName("스폰 테이블 목록")]
    [SerializeField] private HWJ_SpawnTableDataSO[] spawnTables;
    [Tooltip("스탯 구슬 보상 데이터 목록입니다.")]
    [InspectorName("스탯 구슬 목록")]
    [SerializeField] private HWJ_StatOrbDataSO[] statOrbs;
    [Tooltip("레벨업 경험치 테이블 목록입니다.")]
    [InspectorName("레벨 테이블 목록")]
    [SerializeField] private HWJ_LevelUpDataSO[] levelTables;
    [Tooltip("플레이어, 몬스터, 보스가 실행하는 스킬 액션 목록입니다.")]
    [InspectorName("스킬 액션 목록")]
    [SerializeField] private HWJ_SkillActionDataSO[] skillActions;
    [Tooltip("보스 패턴 데이터 목록입니다.")]
    [InspectorName("보스 패턴 목록")]
    [SerializeField] private HWJ_BossPatternDataSO[] bossPatterns;
    [Tooltip("체력바 표시 설정 목록입니다.")]
    [InspectorName("체력바 설정 목록")]
    [SerializeField] private HWJ_HealthBarDataSO[] healthBars;
    [Tooltip("게임오버 창 설정 목록입니다.")]
    [InspectorName("게임오버 설정 목록")]
    [SerializeField] private HWJ_GameOverDataSO[] gameOverWindows;

    [Header("조건과 규칙 데이터")]
    [Tooltip("공통 조건 SO 목록입니다.")]
    [InspectorName("조건 목록")]
    [SerializeField] private HWJ_GameplayConditionSO[] conditions;
    [Tooltip("조건을 묶어 만든 게임플레이 규칙 목록입니다.")]
    [InspectorName("게임플레이 규칙 목록")]
    [SerializeField] private HWJ_GameplayRuleSO[] gameplayRules;
    [Tooltip("여러 규칙을 실행 순서와 정책으로 묶은 실행 코어 목록입니다.")]
    [InspectorName("규칙 실행 코어 목록")]
    [SerializeField] private HWJ_RuleExecutionCoreSO[] ruleExecutionCores;

    public HWJ_RootObjectDataSO[] RootObjects => rootObjects;
    public HWJ_ObjectPoolDataSO ObjectPoolData => objectPoolData;
    public HWJ_SpawnTableDataSO[] SpawnTables => spawnTables;
    public HWJ_StatOrbDataSO[] StatOrbs => statOrbs;
    public HWJ_LevelUpDataSO[] LevelTables => levelTables;
    public HWJ_SkillActionDataSO[] SkillActions => skillActions;
    public HWJ_BossPatternDataSO[] BossPatterns => bossPatterns;
    public HWJ_HealthBarDataSO[] HealthBars => healthBars;
    public HWJ_GameOverDataSO[] GameOverWindows => gameOverWindows;
    public HWJ_GameplayConditionSO[] Conditions => conditions;
    public HWJ_GameplayRuleSO[] GameplayRules => gameplayRules;
    public HWJ_RuleExecutionCoreSO[] RuleExecutionCores => ruleExecutionCores;

    /// <summary>
    /// RootObjectData의 Identity.objectId로 오브젝트 데이터를 찾습니다.
    /// 스포너가 ID만 가지고 프리팹/데이터를 찾을 때 사용합니다.
    /// </summary>
    public bool TryGetRootObject(string objectId, out HWJ_RootObjectDataSO rootObjectData)
    {
        rootObjectData = null;

        if (rootObjects == null)
        {
            return false;
        }

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] != null && rootObjects[i].Identity != null && rootObjects[i].Identity.objectId == objectId)
            {
                rootObjectData = rootObjects[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 스폰 테이블 ID로 스폰 데이터를 찾습니다.
    /// 스테이지 매니저나 스폰 시스템이 구간별 스폰 목록을 가져올 때 사용합니다.
    /// </summary>
    public bool TryGetSpawnTable(string tableId, out HWJ_SpawnTableDataSO spawnTable)
    {
        spawnTable = null;

        if (spawnTables == null)
        {
            return false;
        }

        for (int i = 0; i < spawnTables.Length; i++)
        {
            if (spawnTables[i] != null && spawnTables[i].TableId == tableId)
            {
                spawnTable = spawnTables[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 능력치 구슬 ID로 구슬 데이터를 찾습니다.
    /// 상호작용이나 보상 시스템이 구슬 효과를 적용할 때 사용합니다.
    /// </summary>
    public bool TryGetStatOrb(string orbId, out HWJ_StatOrbDataSO statOrb)
    {
        statOrb = null;

        if (statOrbs == null)
        {
            return false;
        }

        for (int i = 0; i < statOrbs.Length; i++)
        {
            if (statOrbs[i] != null && statOrbs[i].OrbId == orbId)
            {
                statOrb = statOrbs[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 스킬 행동 ID로 스킬 데이터를 찾습니다.
    /// 플레이어, 적, 보스 스킬 실행 시스템이 공통으로 사용합니다.
    /// </summary>
    public bool TryGetSkillAction(string skillActionId, out HWJ_SkillActionDataSO skillAction)
    {
        skillAction = null;

        if (skillActions == null)
        {
            return false;
        }

        for (int i = 0; i < skillActions.Length; i++)
        {
            if (skillActions[i] != null && skillActions[i].SkillActionId == skillActionId)
            {
                skillAction = skillActions[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetLevelTable(string tableId, out HWJ_LevelUpDataSO levelTable)
    {
        levelTable = null;

        if (levelTables == null)
        {
            return false;
        }

        for (int i = 0; i < levelTables.Length; i++)
        {
            if (levelTables[i] != null && levelTables[i].TableId == tableId)
            {
                levelTable = levelTables[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetCondition(string conditionId, out HWJ_GameplayConditionSO condition)
    {
        condition = null;

        if (conditions == null)
        {
            return false;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i] != null && conditions[i].ConditionId == conditionId)
            {
                condition = conditions[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetGameplayRule(string ruleId, out HWJ_GameplayRuleSO rule)
    {
        rule = null;

        if (gameplayRules == null)
        {
            return false;
        }

        for (int i = 0; i < gameplayRules.Length; i++)
        {
            if (gameplayRules[i] != null && gameplayRules[i].RuleId == ruleId)
            {
                rule = gameplayRules[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetRuleExecutionCore(string executionCoreId, out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (ruleExecutionCores == null)
        {
            return false;
        }

        for (int i = 0; i < ruleExecutionCores.Length; i++)
        {
            if (ruleExecutionCores[i] != null && ruleExecutionCores[i].ExecutionCoreId == executionCoreId)
            {
                executionCore = ruleExecutionCores[i];
                return true;
            }
        }

        return false;
    }

    public HWJ_GameDataRegistryReport ValidateRegistryIds()
    {
        List<HWJ_GameDataValidationEntry> entries = new List<HWJ_GameDataValidationEntry>();

        ValidateStableIds(
            entries,
            "RootObject",
            rootObjects,
            "REQ-7",
            "Identity.objectId",
            data => data != null && data.Identity != null ? data.Identity.objectId : null);
        ValidateStableIds(
            entries,
            "SpawnTable",
            spawnTables,
            "REQ-7",
            "TableId",
            data => data != null ? data.TableId : null);
        ValidateStableIds(
            entries,
            "StatOrb",
            statOrbs,
            "REQ-7",
            "OrbId",
            data => data != null ? data.OrbId : null);
        ValidateStableIds(
            entries,
            "LevelTable",
            levelTables,
            "REQ-7",
            "TableId",
            data => data != null ? data.TableId : null);
        ValidateStableIds(
            entries,
            "SkillAction",
            skillActions,
            "REQ-7",
            "SkillActionId",
            data => data != null ? data.SkillActionId : null);
        ValidateStableIds(
            entries,
            "BossPattern",
            bossPatterns,
            "REQ-7",
            "PatternId",
            data => data != null ? data.PatternId : null);
        ValidateStableIds(
            entries,
            "HealthBar",
            healthBars,
            "REQ-7",
            "HealthBarId",
            data => data != null ? data.HealthBarId : null);
        ValidateStableIds(
            entries,
            "GameOverWindow",
            gameOverWindows,
            "REQ-7",
            "GameOverId",
            data => data != null ? data.GameOverId : null);
        ValidateStableIds(
            entries,
            "Condition",
            conditions,
            "REQ-7",
            "ConditionId",
            data => data != null ? data.ConditionId : null);
        ValidateStableIds(
            entries,
            "GameplayRule",
            gameplayRules,
            "REQ-7",
            "RuleId",
            data => data != null ? data.RuleId : null);
        ValidateStableIds(
            entries,
            "RuleExecutionCore",
            ruleExecutionCores,
            "REQ-7",
            "ExecutionCoreId",
            data => data != null ? data.ExecutionCoreId : null);
        ValidateObjectPoolIds(entries);
        ValidateSpawnEntryIds(entries);

        return HWJ_GameDataRegistryReport.FromEntries(entries);
    }

    private static void ValidateStableIds<T>(
        List<HWJ_GameDataValidationEntry> entries,
        string dataCategory,
        T[] assets,
        string requirementId,
        string fieldName,
        Func<T, string> idSelector) where T : UnityEngine.Object
    {
        if (entries == null || assets == null)
        {
            return;
        }

        Dictionary<string, string> firstAssetById = new Dictionary<string, string>();

        for (int i = 0; i < assets.Length; i++)
        {
            T asset = assets[i];

            if (asset == null)
            {
                entries.Add(new HWJ_GameDataValidationEntry(
                    HWJ_GameDataValidationSeverity.Error,
                    "ID_NULL_ASSET",
                    requirementId,
                    dataCategory,
                    $"Index {i}",
                    fieldName,
                    null,
                    "Database list contains an empty asset reference.",
                    "Remove the empty slot or assign a valid data asset."));
                continue;
            }

            string id = idSelector != null ? idSelector(asset) : null;
            AddIdValidation(entries, firstAssetById, dataCategory, asset.name, fieldName, id, requirementId);
        }
    }

    private void ValidateObjectPoolIds(List<HWJ_GameDataValidationEntry> entries)
    {
        if (objectPoolData == null || objectPoolData.Entries == null)
        {
            return;
        }

        Dictionary<string, string> firstAssetById = new Dictionary<string, string>();
        HWJ_PoolEntryData[] entriesData = objectPoolData.Entries;

        for (int i = 0; i < entriesData.Length; i++)
        {
            HWJ_PoolEntryData entry = entriesData[i];
            string assetName = $"{objectPoolData.name}[{i}]";

            if (entry == null)
            {
                entries.Add(new HWJ_GameDataValidationEntry(
                    HWJ_GameDataValidationSeverity.Error,
                    "ID_NULL_ASSET",
                    "REQ-7",
                    "ObjectPoolEntry",
                    assetName,
                    "poolId",
                    null,
                    "Object pool list contains an empty entry.",
                    "Remove the empty slot or assign a valid pool entry."));
                continue;
            }

            AddIdValidation(
                entries,
                firstAssetById,
                "ObjectPoolEntry",
                assetName,
                "poolId",
                entry.poolId,
                "REQ-7");
        }
    }

    private void ValidateSpawnEntryIds(List<HWJ_GameDataValidationEntry> entries)
    {
        if (spawnTables == null)
        {
            return;
        }

        for (int tableIndex = 0; tableIndex < spawnTables.Length; tableIndex++)
        {
            HWJ_SpawnTableDataSO spawnTable = spawnTables[tableIndex];

            if (spawnTable == null || spawnTable.Entries == null)
            {
                continue;
            }

            Dictionary<string, string> firstAssetById = new Dictionary<string, string>();
            HWJ_SpawnEntryData[] spawnEntries = spawnTable.Entries;

            for (int entryIndex = 0; entryIndex < spawnEntries.Length; entryIndex++)
            {
                HWJ_SpawnEntryData spawnEntry = spawnEntries[entryIndex];
                string assetName = $"{spawnTable.name}[{entryIndex}]";

                if (spawnEntry == null)
                {
                    entries.Add(new HWJ_GameDataValidationEntry(
                        HWJ_GameDataValidationSeverity.Error,
                        "ID_NULL_ASSET",
                        "REQ-7",
                        "SpawnEntry",
                        assetName,
                        "spawnId",
                        null,
                        "Spawn table contains an empty spawn entry.",
                        "Remove the empty slot or assign a valid spawn entry."));
                    continue;
                }

                AddIdValidation(
                    entries,
                    firstAssetById,
                    "SpawnEntry",
                    assetName,
                    "spawnId",
                    spawnEntry.spawnId,
                    "REQ-7");
            }
        }
    }

    private static void AddIdValidation(
        List<HWJ_GameDataValidationEntry> entries,
        Dictionary<string, string> firstAssetById,
        string dataCategory,
        string assetName,
        string fieldName,
        string id,
        string requirementId)
    {
        if (entries == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            entries.Add(new HWJ_GameDataValidationEntry(
                HWJ_GameDataValidationSeverity.Error,
                "ID_MISSING",
                requirementId,
                dataCategory,
                assetName,
                fieldName,
                id,
                "Stable data ID is empty.",
                "Assign a non-empty stable string ID that will not change when the file name changes."));
            return;
        }

        if (firstAssetById == null)
        {
            return;
        }

        if (firstAssetById.TryGetValue(id, out string firstAssetName))
        {
            entries.Add(new HWJ_GameDataValidationEntry(
                HWJ_GameDataValidationSeverity.Error,
                "ID_DUPLICATE",
                requirementId,
                dataCategory,
                assetName,
                fieldName,
                id,
                $"Stable data ID duplicates {firstAssetName}.",
                "Assign a unique stable string ID."));
            return;
        }

        firstAssetById[id] = assetName;
    }
}
