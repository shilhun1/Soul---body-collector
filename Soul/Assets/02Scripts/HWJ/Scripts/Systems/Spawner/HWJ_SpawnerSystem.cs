using System.Collections;
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

        for (int i = 0; i < count; i++)
        {
            SpawnOne(entry, point);
        }
    }

    private void SpawnOne(HWJ_SpawnEntryData entry, HWJ_SpawnPoint point)
    {
        GameObject prefab = entry.prefabOverride;

        if (prefab == null && entry.rootObjectData != null && entry.rootObjectData.Model != null)
        {
            prefab = entry.rootObjectData.Model.modelPrefab;
        }

        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = point.Position + (Vector3)entry.spawnOffset;
        Transform parent = point.SpawnParent != null ? point.SpawnParent : null;
        GameObject instance = SpawnPrefab(prefab, spawnPosition, point.Rotation, parent);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && entry.rootObjectData != null)
        {
            resolver.SetRootObjectData(entry.rootObjectData);
            RefreshSpawnedRuntimeData(instance);

            if (entry.spawnPointType == HWJ_SpawnPointType.PlayerStart && HWJ_GameAccess.HasManager)
            {
                HWJ_GameAccess.Manager.RegisterPlayer(resolver);
            }
        }

        WireSpawnedObject(instance);
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
