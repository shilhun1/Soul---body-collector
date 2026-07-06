using UnityEngine;

/// <summary>
/// 게임 전반에서 사용하는 주요 데이터 에셋을 한 곳에 모아두는 데이터베이스입니다.
/// 각 시스템이 개별 SO를 직접 많이 들고 있지 않도록, ID 기반 조회의 중심으로 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_GameplayDatabase", menuName = "HWJ/Data/Gameplay Database")]
public class HWJ_GameplayDatabaseSO : ScriptableObject
{
    [Header("Object Data")]
    [SerializeField] private HWJ_RootObjectDataSO[] rootObjects;

    [Header("System Data")]
    [SerializeField] private HWJ_ObjectPoolDataSO objectPoolData;
    [SerializeField] private HWJ_SpawnTableDataSO[] spawnTables;
    [SerializeField] private HWJ_StatOrbDataSO[] statOrbs;
    [SerializeField] private HWJ_LevelUpDataSO[] levelTables;
    [SerializeField] private HWJ_SkillActionDataSO[] skillActions;
    [SerializeField] private HWJ_BossPatternDataSO[] bossPatterns;
    [SerializeField] private HWJ_HealthBarDataSO[] healthBars;
    [SerializeField] private HWJ_GameOverDataSO[] gameOverWindows;

    public HWJ_RootObjectDataSO[] RootObjects => rootObjects;
    public HWJ_ObjectPoolDataSO ObjectPoolData => objectPoolData;
    public HWJ_SpawnTableDataSO[] SpawnTables => spawnTables;
    public HWJ_StatOrbDataSO[] StatOrbs => statOrbs;
    public HWJ_LevelUpDataSO[] LevelTables => levelTables;
    public HWJ_SkillActionDataSO[] SkillActions => skillActions;
    public HWJ_BossPatternDataSO[] BossPatterns => bossPatterns;
    public HWJ_HealthBarDataSO[] HealthBars => healthBars;
    public HWJ_GameOverDataSO[] GameOverWindows => gameOverWindows;

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
}
