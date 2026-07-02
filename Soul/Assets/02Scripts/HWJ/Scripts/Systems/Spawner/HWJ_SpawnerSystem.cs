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
    [SerializeField] private bool spawnOnStart = true;

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

    private void SpawnEntry(HWJ_SpawnEntryData entry)
    {
        HWJ_SpawnPoint point = FindPoint(entry.spawnPointType);

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

        Transform parent = point.SpawnParent != null ? point.SpawnParent : null;
        GameObject instance = objectPool != null
            ? objectPool.Spawn(prefab, point.Position, point.Rotation, parent)
            : HWJ_GameAccess.Spawn(prefab, point.Position, point.Rotation, parent);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && entry.rootObjectData != null)
        {
            resolver.SetRootObjectData(entry.rootObjectData);

            if (entry.spawnPointType == HWJ_SpawnPointType.PlayerStart && HWJ_GameAccess.HasManager)
            {
                HWJ_GameAccess.Manager.RegisterPlayer(resolver);
            }
        }
    }

    private HWJ_SpawnPoint FindPoint(HWJ_SpawnPointType spawnPointType)
    {
        if (spawnPoints == null)
        {
            return null;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && spawnPoints[i].SpawnPointType == spawnPointType)
            {
                return spawnPoints[i];
            }
        }

        return null;
    }
}
