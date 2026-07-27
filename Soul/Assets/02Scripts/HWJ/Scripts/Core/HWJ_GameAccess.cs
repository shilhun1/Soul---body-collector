using UnityEngine;

/// <summary>
/// 외부 스크립트가 HWJ_GameManager에 안전하게 접근하기 위한 정적 도우미입니다.
/// 무분별한 FindObject 호출 대신, 읽기/스폰/반환 같은 공통 요청만 이 통로로 모읍니다.
/// </summary>
public static class HWJ_GameAccess
{
    public static bool HasManager => HWJ_GameManager.Instance != null;
    public static HWJ_GameManager Manager => HWJ_GameManager.Instance;
    public static HWJ_GameplayDatabaseSO Database => HasManager ? Manager.Database : null;
    public static HWJ_ObjectPoolSystem ObjectPool => HasManager ? Manager.ObjectPool : null;
    public static HWJ_RootObjectDataResolver PlayerResolver => HasManager ? Manager.PlayerResolver : null;
    public static HWJ_PlayerInputSystem PlayerInput => HasManager ? Manager.PlayerInput : null;
    public static HWJ_BodyDecaySystem PlayerPossessionMental => HasManager ? Manager.PlayerPossessionMental : null;

    /// <summary>
    /// GameManager가 있으면 풀 기반 생성으로 연결하고, 없으면 null을 반환합니다.
    /// 외부 스크립트는 Instantiate를 직접 호출하기 전에 이 메서드를 우선 사용합니다.
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return HasManager ? Manager.Spawn(prefab, position, rotation, parent) : null;
    }

    /// <summary>
    /// GameManager가 있으면 풀 반환으로 연결합니다.
    /// GameManager가 없는 경우에는 호출자가 직접 대체 처리를 결정할 수 있도록 아무 작업도 하지 않습니다.
    /// </summary>
    public static void Despawn(GameObject instance)
    {
        if (HasManager)
        {
            Manager.Despawn(instance);
        }
    }

    /// <summary>
    /// 공통 데이터베이스에서 RootObjectData를 조회합니다.
    /// 시스템들이 데이터베이스 필드를 직접 들고 있지 않아도 ID 기반 조회를 할 수 있습니다.
    /// </summary>
    public static bool TryGetRootObject(string objectId, out HWJ_RootObjectDataSO rootObjectData)
    {
        rootObjectData = null;
        return HasManager && Manager.TryGetRootObject(objectId, out rootObjectData);
    }

    /// <summary>
    /// 공통 데이터베이스에서 능력치 구슬 데이터를 조회합니다.
    /// </summary>
    public static bool TryGetStatOrb(string orbId, out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;
        return HasManager && Manager.TryGetStatOrb(orbId, out statOrbData);
    }

    public static bool TryGetLevelTable(string tableId, out HWJ_LevelUpDataSO levelTable)
    {
        levelTable = null;
        return HasManager && Manager.TryGetLevelTable(tableId, out levelTable);
    }

    public static bool TryGetTitleScreen(string titleScreenId, out HWJ_TitleScreenDataSO titleScreenData)
    {
        titleScreenData = null;
        return HasManager && Manager.TryGetTitleScreen(titleScreenId, out titleScreenData);
    }

    /// <summary>
    /// 공통 데이터베이스에서 스킬 행동 데이터를 조회합니다.
    /// </summary>
    public static bool TryGetSkillAction(string skillActionId, out HWJ_SkillActionDataSO skillActionData)
    {
        skillActionData = null;
        return HasManager && Manager.TryGetSkillAction(skillActionId, out skillActionData);
    }

    public static bool TryGetSkillNode(string nodeId, out HWJ_SkillNodeDataSO skillNodeData)
    {
        skillNodeData = null;
        return HasManager && Manager.TryGetSkillNode(nodeId, out skillNodeData);
    }

    public static bool TryGetSkillNodeBySkillAction(string skillActionId, out HWJ_SkillNodeDataSO skillNodeData)
    {
        skillNodeData = null;
        return HasManager && Manager.TryGetSkillNodeBySkillAction(skillActionId, out skillNodeData);
    }

    public static bool TryGetGameplayRule(string ruleId, out HWJ_GameplayRuleSO gameplayRule)
    {
        gameplayRule = null;

        if (!HasManager)
        {
            return false;
        }

        return Manager.TryGetGameplayRule(ruleId, out gameplayRule);
    }

    public static bool TryGetRuleExecutionCore(string executionCoreId, out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (!HasManager)
        {
            return false;
        }

        return Manager.TryGetRuleExecutionCore(executionCoreId, out executionCore);
    }

    public static bool TryValidateGameplayDatabase(out HWJ_GameDataRegistryReport report)
    {
        report = null;

        if (!HasManager)
        {
            return false;
        }

        return Manager.TryValidateGameplayDatabase(out report);
    }
}
