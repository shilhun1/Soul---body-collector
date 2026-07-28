using UnityEngine;

/// <summary>
/// Runtime pickup for stat orbs.
/// It applies StatOrbDataSO to the player's runtime status and can softly pull itself toward the player.
/// </summary>
public class HWJ_StatOrbPickupSystem : MonoBehaviour
{
    [Header("Collect")]
    [SerializeField] private float collectDelaySeconds = 0.35f;

    [Header("Stat Orb")]
    [SerializeField] private HWJ_StatOrbDataSO statOrbData;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Attraction")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private HWJ_RuntimeStatusSystem targetStatus;
    [SerializeField] private bool moveToTarget = true;
    [SerializeField] private bool autoResolvePlayerTarget = true;
    [SerializeField] private float attractionStartDistance = 4f;
    [SerializeField] private float collectDistance = 0.45f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.25f;
    [SerializeField] private float moveSpeed = 2.25f;
    [SerializeField] private float acceleration = 5.5f;
    [SerializeField] private float initialDelaySeconds = 0.25f;
    [SerializeField] private float lifeTimeSeconds = 12f;

    [Header("Visual")]
    [SerializeField] private bool controlAnimatorSpeed = true;
    [SerializeField] private float animationSpeedMultiplier = 0.55f;

    private bool isCollected;
    private float spawnedTime;
    private float currentMoveSpeed;
    private float nextTargetSearchTime;

    public HWJ_StatOrbDataSO StatOrbData => statOrbData;
    public bool IsCollected => isCollected;

    private void OnEnable()
    {
        ResetRuntimeState();
        ApplyAnimatorSpeed();
        TryResolvePlayerTargetIfNeeded(true);
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

        TryResolvePlayerTargetIfNeeded(false);

        if (!moveToTarget || targetTransform == null || targetStatus == null)
        {
            return;
        }

        if (statOrbData != null && !targetStatus.CanApplyStatOrb(statOrbData))
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

        ResetRuntimeState();
        ApplyAnimatorSpeed();
        TryResolvePlayerTargetIfNeeded(true);
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
    /// Applies the orb to the requested runtime status, then returns this pickup through pooling when possible.
    /// </summary>
    public bool TryCollect(HWJ_RuntimeStatusSystem collectorStatus)
    {
        if (!CanCollectNow() || isCollected || statOrbData == null || collectorStatus == null)
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

    private void ResetRuntimeState()
    {
        spawnedTime = Time.time;
        currentMoveSpeed = Mathf.Max(0f, moveSpeed);
        nextTargetSearchTime = 0f;
        isCollected = false;
    }

    private bool CanCollectNow()
    {
        return Time.time - spawnedTime >= Mathf.Max(0f, collectDelaySeconds);
    }

    private void TryResolvePlayerTargetIfNeeded(bool force)
    {
        if (!autoResolvePlayerTarget)
        {
            return;
        }

        if (targetTransform != null && targetStatus == null)
        {
            targetStatus = targetTransform.GetComponent<HWJ_RuntimeStatusSystem>();

            if (targetStatus == null)
            {
                targetStatus = targetTransform.GetComponentInParent<HWJ_RuntimeStatusSystem>();
            }
        }

        if (targetTransform != null && targetStatus != null)
        {
            return;
        }

        if (!force && Time.time < nextTargetSearchTime)
        {
            return;
        }

        nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchIntervalSeconds);

        if (HWJ_PickupTargetUtility.TryResolvePlayerStatusTarget(
            out HWJ_RuntimeStatusSystem resolvedStatus,
            out Transform resolvedTargetTransform))
        {
            targetStatus = resolvedStatus;
            targetTransform = resolvedTargetTransform;
        }
    }

    private void ApplyAnimatorSpeed()
    {
        if (!controlAnimatorSpeed)
        {
            return;
        }

        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.speed = Mathf.Max(0.05f, animationSpeedMultiplier);
        }
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
