using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpawnTableDataSO와 SpawnPoint를 이용해 오브젝트를 생성하는 기본 스포너입니다.
/// 생성된 오브젝트에 RootObjectDataResolver가 있으면 스폰 항목의 RootObjectData를 주입합니다.
/// </summary>
public class HWJ_SpawnerSystem : MonoBehaviour
{
    [SerializeField] private HWJ_SpawnTableDataSO spawnTable;
    [SerializeField] private HWJ_SpawnPoint[] spawnPoints;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private bool autoCollectSpawnPoints = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private string lastSpawnResult;

    private readonly List<HWJ_RuntimeStatusSystem> sequentialSpawnedMonsters = new List<HWJ_RuntimeStatusSystem>();

    public string LastSpawnResult => lastSpawnResult;

    private void Awake()
    {
        if (autoCollectSpawnPoints)
        {
            CollectSpawnPoints();
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllStartEntries();
        }
    }

    /// <summary>
    /// spawnOnStart가 켜진 모든 스폰 항목을 실행합니다.
    /// 스테이지 시작 시 플레이어 시작 지점, 적 배치, NPC 배치에 사용합니다.
    /// </summary>
    public void SpawnAllStartEntries()
    {
        if (spawnTable == null || spawnTable.Entries == null)
        {
            SetLastSpawnResult("Spawn failed: missing spawn table or entries.", true);
            return;
        }

        for (int i = 0; i < spawnTable.Entries.Length; i++)
        {
            if (spawnTable.Entries[i] != null && spawnTable.Entries[i].spawnOnStart)
            {
                SpawnEntry(spawnTable.Entries[i]);
            }
        }
    }

    /// <summary>
    /// 스폰 ID로 특정 스폰 항목만 실행합니다.
    /// 이벤트 스폰, 보스 페이즈 스폰, 보상 구슬 생성에 사용합니다.
    /// </summary>
    public void SpawnById(string spawnId)
    {
        if (spawnTable != null && spawnTable.TryGetEntry(spawnId, out HWJ_SpawnEntryData entry))
        {
            SpawnEntry(entry);
        }
    }

    public void CollectSpawnPoints()
    {
        spawnPoints = FindObjectsByType<HWJ_SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void SpawnEntry(HWJ_SpawnEntryData entry)
    {
        HWJ_SpawnPoint point = FindPoint(entry);

        if (point == null)
        {
            SetLastSpawnResult($"Spawn failed: missing spawn point for {entry.spawnId}.", true);
            return;
        }

        StartCoroutine(SpawnEntryRoutine(entry, point));
    }

    private IEnumerator SpawnEntryRoutine(HWJ_SpawnEntryData entry, HWJ_SpawnPoint point)
    {
        if (entry.spawnDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(entry.spawnDelaySeconds);
        }

        int count = Mathf.Max(1, entry.spawnCount);
        bool useSequentialSpawn = ShouldUseSequentialSpawn(entry, count);

        for (int i = 0; i < count; i++)
        {
            if (ShouldSkipSpawnBySavedProgression(entry, point, i))
            {
                SetLastSpawnResult($"Spawn skipped: reward already claimed for {entry.spawnId}:{i}.", false);
                continue;
            }

            GameObject instance = SpawnOne(entry, point, i);

            if (instance == null)
            {
                yield break;
            }

            if (useSequentialSpawn)
            {
                TrackSequentialSpawnedMonster(instance);
            }

            if (useSequentialSpawn && i < count - 1)
            {
                yield return WaitForNextSequentialSpawn(entry);
            }
        }
    }

    private GameObject SpawnOne(HWJ_SpawnEntryData entry, HWJ_SpawnPoint point, int spawnIndex)
    {
        GameObject prefab = entry.prefabOverride;

        if (prefab == null && entry.rootObjectData != null && entry.rootObjectData.Model != null)
        {
            prefab = entry.rootObjectData.Model.modelPrefab;
        }

        if (prefab == null)
        {
            string rootName = entry.rootObjectData != null ? entry.rootObjectData.name : "None";
            SetLastSpawnResult(
                $"Spawn failed: {entry.spawnId} has no Prefab Override and RootObjectData {rootName} has no Model Prefab.",
                true);
            return null;
        }

        Vector3 spawnPosition = point.Position + (Vector3)entry.spawnOffset;
        Transform parent = point.SpawnParent != null ? point.SpawnParent : null;
        GameObject instance = SpawnPrefab(prefab, spawnPosition, point.Rotation, parent);

        if (instance == null)
        {
            SetLastSpawnResult($"Spawn failed: prefab spawn returned null for {entry.spawnId}.", true);
            return null;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && entry.rootObjectData != null)
        {
            resolver.SetRootObjectData(entry.rootObjectData);
            AssignRuntimeSaveIdentity(instance, entry, point, spawnIndex, resolver);
            RefreshSpawnedRuntimeData(instance);

            if (entry.spawnPointType == HWJ_SpawnPointType.PlayerStart && HWJ_GameAccess.HasManager)
            {
                HWJ_GameAccess.Manager.RegisterPlayer(resolver);
            }
        }

        WireSpawnedObject(instance);
        SetLastSpawnResult($"Spawned {instance.name} from {entry.spawnId}.", false);
        return instance;
    }

    private bool ShouldSkipSpawnBySavedProgression(HWJ_SpawnEntryData entry, HWJ_SpawnPoint point, int spawnIndex)
    {
        if (entry == null || !entry.skipSpawnWhenRewardClaimed)
        {
            return false;
        }

        if (entry.rootObjectData == null
            || (entry.rootObjectData.ObjectType != HWJ_ObjectType.Enemy
                && entry.rootObjectData.ObjectType != HWJ_ObjectType.Boss))
        {
            return false;
        }

        if (!HWJ_SaveService.TryGetActiveService(out HWJ_SaveService activeSaveService))
        {
            return false;
        }

        string rootObjectId = HWJ_RuntimeSaveIdentity.NormalizeIdPart(ResolveRootObjectId(entry.rootObjectData));
        string stableInstanceId = HWJ_RuntimeSaveIdentity.CreateSpawnStableId(
            entry.spawnId,
            point != null ? point.PointId : entry.spawnPointId,
            rootObjectId,
            spawnIndex);
        string rewardClaimId = rootObjectId + ":" + stableInstanceId;
        return activeSaveService.IsRewardClaimed(rewardClaimId);
    }

    private static void AssignRuntimeSaveIdentity(
        GameObject instance,
        HWJ_SpawnEntryData entry,
        HWJ_SpawnPoint point,
        int spawnIndex,
        HWJ_RootObjectDataResolver resolver)
    {
        if (instance == null || entry == null)
        {
            return;
        }

        HWJ_RuntimeSaveIdentity saveIdentity = instance.GetComponent<HWJ_RuntimeSaveIdentity>();

        if (saveIdentity == null)
        {
            saveIdentity = instance.AddComponent<HWJ_RuntimeSaveIdentity>();
        }

        string rootObjectId = resolver != null && resolver.RootObjectData != null
            ? ResolveRootObjectId(resolver.RootObjectData)
            : ResolveRootObjectId(entry.rootObjectData);
        saveIdentity.AssignSpawnIdentity(
            entry.spawnId,
            point != null ? point.PointId : entry.spawnPointId,
            rootObjectId,
            spawnIndex);
    }

    private static string ResolveRootObjectId(HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null)
        {
            return null;
        }

        if (rootObjectData.Identity != null && !string.IsNullOrEmpty(rootObjectData.Identity.objectId))
        {
            return rootObjectData.Identity.objectId;
        }

        return rootObjectData.name;
    }

    private void SetLastSpawnResult(string message, bool warning)
    {
        lastSpawnResult = message;

        if (warning)
        {
            Debug.LogWarning(message, this);
        }
    }

    private bool ShouldUseSequentialSpawn(HWJ_SpawnEntryData entry, int count)
    {
        if (entry == null || count <= 1 || !entry.useSequentialSpawnWhenMultiple)
        {
            return false;
        }

        if (entry.spawnPointType == HWJ_SpawnPointType.Enemy || entry.spawnPointType == HWJ_SpawnPointType.Boss)
        {
            return true;
        }

        if (entry.rootObjectData == null)
        {
            return false;
        }

        return entry.rootObjectData.ObjectType == HWJ_ObjectType.Enemy
            || entry.rootObjectData.ObjectType == HWJ_ObjectType.Boss;
    }

    private void TrackSequentialSpawnedMonster(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver == null
            || (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss))
        {
            return;
        }

        HWJ_RuntimeStatusSystem runtimeStatus = instance.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus != null && !sequentialSpawnedMonsters.Contains(runtimeStatus))
        {
            sequentialSpawnedMonsters.Add(runtimeStatus);
        }
    }

    private IEnumerator WaitForNextSequentialSpawn(HWJ_SpawnEntryData entry)
    {
        float elapsedSeconds = 0f;
        float maxWaitSeconds = Mathf.Max(0f, entry.nextSpawnMaxWaitSeconds);
        bool hasTimeout = maxWaitSeconds > 0f;

        if (!entry.waitUntilCurrentSpawnedMonstersDefeated && !hasTimeout)
        {
            yield break;
        }

        while (true)
        {
            bool defeatedConditionMet = entry.waitUntilCurrentSpawnedMonstersDefeated
                && AreAllSequentialSpawnedMonstersDefeated();
            bool timeoutConditionMet = hasTimeout && elapsedSeconds >= maxWaitSeconds;

            if (defeatedConditionMet || timeoutConditionMet)
            {
                yield break;
            }

            elapsedSeconds += Time.deltaTime;
            yield return null;
        }
    }

    private bool AreAllSequentialSpawnedMonstersDefeated()
    {
        for (int i = sequentialSpawnedMonsters.Count - 1; i >= 0; i--)
        {
            HWJ_RuntimeStatusSystem runtimeStatus = sequentialSpawnedMonsters[i];

            if (runtimeStatus == null || !runtimeStatus.gameObject.activeInHierarchy || runtimeStatus.IsDead)
            {
                sequentialSpawnedMonsters.RemoveAt(i);
            }
        }

        return sequentialSpawnedMonsters.Count == 0;
    }

    private GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (objectPool != null)
        {
            return objectPool.Spawn(prefab, position, rotation, parent);
        }

        if (HWJ_GameAccess.HasManager)
        {
            return HWJ_GameAccess.Spawn(prefab, position, rotation, parent);
        }

        return Instantiate(prefab, position, rotation, parent);
    }

    private void WireSpawnedObject(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_EnemyNavigationSystem enemyNavigation = instance.GetComponent<HWJ_EnemyNavigationSystem>();

        if (enemyNavigation != null)
        {
            enemyNavigation.SetTarget(FindPlayerTransform());
        }
    }

    private void RefreshSpawnedRuntimeData(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_PossessionBodyState possessionBodyState = instance.GetComponent<HWJ_PossessionBodyState>();

        if (possessionBodyState != null)
        {
            possessionBodyState.ResetConsumed();
        }

        HWJ_RuntimeStatusSystem runtimeStatus = instance.GetComponent<HWJ_RuntimeStatusSystem>();

        if (runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(true);
        }

        HWJ_MonsterAISystem monsterAI = instance.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.RefreshData();
        }
    }

    private Transform FindPlayerTransform()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private HWJ_SpawnPoint FindPoint(HWJ_SpawnEntryData entry)
    {
        if (spawnPoints == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(entry.spawnPointId))
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && spawnPoints[i].PointId == entry.spawnPointId)
                {
                    return spawnPoints[i];
                }
            }
        }

        HWJ_SpawnPoint firstMatchedPoint = null;
        int matchedCount = 0;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null || spawnPoints[i].SpawnPointType != entry.spawnPointType)
            {
                continue;
            }

            if (firstMatchedPoint == null)
            {
                firstMatchedPoint = spawnPoints[i];
            }

            matchedCount++;

            if (entry.randomizePoint && Random.Range(0, matchedCount) == 0)
            {
                firstMatchedPoint = spawnPoints[i];
            }
        }

        return firstMatchedPoint;
    }
}
