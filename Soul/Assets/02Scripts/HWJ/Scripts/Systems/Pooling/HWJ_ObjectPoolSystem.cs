using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiate/Destroy 사용을 줄이기 위한 공통 오브젝트 풀 시스템입니다.
/// 스포너, 스킬, 이펙트, 능력치 구슬, UI 창 생성이 이 시스템을 통해 오브젝트를 재사용합니다.
/// </summary>
public class HWJ_ObjectPoolSystem : MonoBehaviour
{
    [SerializeField] private HWJ_ObjectPoolDataSO poolData;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private bool prewarmOnAwake = true;

    private readonly Dictionary<GameObject, Queue<GameObject>> pooledObjects = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, HWJ_PoolEntryData> poolEntries = new Dictionary<GameObject, HWJ_PoolEntryData>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
    private readonly Dictionary<GameObject, int> createdCounts = new Dictionary<GameObject, int>();

    private bool isPrewarmed;

    public HWJ_ObjectPoolDataSO PoolData => poolData;

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        if (prewarmOnAwake)
        {
            Prewarm();
        }
    }

    /// <summary>
    /// PoolData에 등록된 프리팹들을 미리 생성해 둡니다.
    /// 씬 시작 시 프레임 드랍을 줄이기 위해 GameManager 초기화 단계에서 호출할 수 있습니다.
    /// </summary>
    public void Prewarm()
    {
        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        if (isPrewarmed)
        {
            return;
        }

        if (poolData == null || poolData.Entries == null)
        {
            return;
        }

        isPrewarmed = true;

        for (int i = 0; i < poolData.Entries.Length; i++)
        {
            HWJ_PoolEntryData entry = poolData.Entries[i];

            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            RegisterEntry(entry);

            int maxSize = Mathf.Max(1, entry.maxSize);
            int count = Mathf.Clamp(entry.initialSize, 0, maxSize);

            for (int j = 0; j < count; j++)
            {
                GameObject instance = CreateInstance(entry.prefab);
                Despawn(instance);
            }
        }
    }

    /// <summary>
    /// GameManager가 GameplayDatabase에 연결된 풀 데이터를 전달할 때 사용합니다.
    /// 인스펙터에 PoolData를 직접 넣지 않아도 데이터베이스 기준으로 풀을 초기화할 수 있습니다.
    /// </summary>
    public void SetPoolData(HWJ_ObjectPoolDataSO poolData)
    {
        if (this.poolData == poolData)
        {
            return;
        }

        this.poolData = poolData;
        isPrewarmed = false;
    }

    /// <summary>
    /// 프리팹을 풀에서 꺼내 지정 위치에 배치합니다.
    /// 등록되지 않은 프리팹도 1회성 풀로 등록해서 사용할 수 있습니다.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        EnsureRegistered(prefab);
        Queue<GameObject> queue = pooledObjects[prefab];
        GameObject instance = queue.Count > 0 ? queue.Dequeue() : TryCreateExpandableInstance(prefab);

        if (instance == null)
        {
            return null;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, false);
        instanceTransform.SetPositionAndRotation(position, rotation);

        HWJ_PoolableObject poolableObject = instance.GetComponent<HWJ_PoolableObject>();

        if (poolableObject != null)
        {
            poolableObject.OnSpawnedFromPool();
        }
        else
        {
            instance.SetActive(true);
        }

        return instance;
    }

    /// <summary>
    /// 풀에서 나온 오브젝트를 다시 비활성화하고 큐에 넣습니다.
    /// 풀 출처를 알 수 없는 오브젝트는 비활성화만 해서 Destroy 호출을 피합니다.
    /// </summary>
    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!instanceToPrefab.TryGetValue(instance, out GameObject prefab) || prefab == null)
        {
            instance.SetActive(false);
            return;
        }

        HWJ_PoolableObject poolableObject = instance.GetComponent<HWJ_PoolableObject>();

        if (poolableObject != null)
        {
            poolableObject.OnDespawnedToPool();
        }
        else
        {
            instance.SetActive(false);
        }

        instance.transform.SetParent(poolRoot, false);
        pooledObjects[prefab].Enqueue(instance);
    }

    private void EnsureRegistered(GameObject prefab)
    {
        if (pooledObjects.ContainsKey(prefab))
        {
            return;
        }

        if (poolData != null && poolData.TryGetEntry(prefab, out HWJ_PoolEntryData entry))
        {
            RegisterEntry(entry);
            return;
        }

        RegisterEntry(new HWJ_PoolEntryData
        {
            poolId = prefab.name,
            prefab = prefab,
            initialSize = 0,
            maxSize = 30,
            canExpand = true
        });
    }

    private void RegisterEntry(HWJ_PoolEntryData entry)
    {
        if (entry == null || entry.prefab == null || pooledObjects.ContainsKey(entry.prefab))
        {
            return;
        }

        pooledObjects.Add(entry.prefab, new Queue<GameObject>());
        poolEntries.Add(entry.prefab, entry);
        createdCounts.Add(entry.prefab, 0);
    }

    private GameObject TryCreateExpandableInstance(GameObject prefab)
    {
        HWJ_PoolEntryData entry = poolEntries[prefab];
        int currentCount = createdCounts[prefab];
        int maxSize = Mathf.Max(1, entry.maxSize);

        if (!entry.canExpand || currentCount >= maxSize)
        {
            return null;
        }

        return CreateInstance(prefab);
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, poolRoot);
        instance.SetActive(false);
        instanceToPrefab[instance] = prefab;
        createdCounts[prefab] = createdCounts[prefab] + 1;

        HWJ_PoolableObject poolableObject = instance.GetComponent<HWJ_PoolableObject>();

        if (poolableObject == null)
        {
            poolableObject = instance.AddComponent<HWJ_PoolableObject>();
        }

        poolableObject.SetPoolInfo(this, prefab);
        return instance;
    }
}
