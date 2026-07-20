using UnityEngine;

/// <summary>
/// 씬에 배치된 능력치 구슬의 상호작용 컴포넌트입니다.
/// StatOrbDataSO를 RuntimeStatusSystem에 적용하고, 수집 후 오브젝트를 제거합니다.
/// </summary>
public class HWJ_StatOrbPickupSystem : MonoBehaviour
{
    [Header("능력치 구슬")]
    [SerializeField] private HWJ_StatOrbDataSO statOrbData;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("흡수 이동")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private HWJ_RuntimeStatusSystem targetStatus;
    [SerializeField] private bool moveToTarget = true;
    [SerializeField] private float attractionStartDistance = 3f;
    [SerializeField] private float collectDistance = 0.35f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float initialDelaySeconds = 0.15f;
    [SerializeField] private float lifeTimeSeconds = 12f;

    private bool isCollected;
    private float spawnedTime;
    private float currentMoveSpeed;

    public HWJ_StatOrbDataSO StatOrbData => statOrbData;
    public bool IsCollected => isCollected;

    private void OnEnable()
    {
        spawnedTime = Time.time;
        currentMoveSpeed = Mathf.Max(0f, moveSpeed);
        isCollected = false;
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        if (lifeTimeSeconds > 0f && Time.time - spawnedTime >= lifeTimeSeconds)
        {
            DespawnOrb();
            return;
        }

        if (!moveToTarget || targetTransform == null || targetStatus == null)
        {
            return;
        }

        if (Time.time - spawnedTime < initialDelaySeconds)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, targetTransform.position);

        if (distance <= collectDistance)
        {
            TryCollect(targetStatus);
            return;
        }

        if (distance > attractionStartDistance)
        {
            return;
        }

        currentMoveSpeed += Mathf.Max(0f, acceleration) * Time.deltaTime;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetTransform.position,
            currentMoveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        TryCollect(other.gameObject);
    }

    public void Initialize(
        HWJ_StatOrbDataSO data,
        HWJ_RuntimeStatusSystem status,
        Transform target,
        HWJ_ObjectPoolSystem pool = null)
    {
        statOrbData = data;
        targetStatus = status;
        targetTransform = target;

        if (pool != null)
        {
            objectPool = pool;
        }

        spawnedTime = Time.time;
        currentMoveSpeed = Mathf.Max(0f, moveSpeed);
        isCollected = false;
    }

    public bool TryCollect(GameObject collector)
    {
        if (collector == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem collectorStatus = collector.GetComponent<HWJ_RuntimeStatusSystem>();

        if (collectorStatus == null)
        {
            collectorStatus = collector.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        return TryCollect(collectorStatus);
    }

    /// <summary>
    /// 대상 런타임 상태에 구슬 효과를 적용합니다.
    /// 플레이어뿐 아니라 버프를 받을 수 있는 다른 오브젝트에도 재사용할 수 있습니다.
    /// </summary>
    public bool TryCollect(HWJ_RuntimeStatusSystem collectorStatus)
    {
        if (isCollected || statOrbData == null || collectorStatus == null)
        {
            return false;
        }

        if (!collectorStatus.TryApplyStatOrb(statOrbData))
        {
            return false;
        }

        isCollected = true;

        SpawnCollectEffect();

        if (destroyOnCollect)
        {
            DespawnOrb();
        }

        return true;
    }

    private void SpawnCollectEffect()
    {
        if (statOrbData == null || statOrbData.CollectEffectPrefab == null)
        {
            return;
        }

        if (objectPool != null)
        {
            objectPool.Spawn(statOrbData.CollectEffectPrefab, transform.position, Quaternion.identity);
            return;
        }

        GameObject effectObject = HWJ_GameAccess.Spawn(statOrbData.CollectEffectPrefab, transform.position, Quaternion.identity);

        if (effectObject == null)
        {
            Instantiate(statOrbData.CollectEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void DespawnOrb()
    {
        HWJ_PoolableObject poolableObject = GetComponent<HWJ_PoolableObject>();

        if (poolableObject != null)
        {
            poolableObject.ReturnToPool();
            return;
        }

        if (objectPool != null)
        {
            objectPool.Despawn(gameObject);
            return;
        }

        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Despawn(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
