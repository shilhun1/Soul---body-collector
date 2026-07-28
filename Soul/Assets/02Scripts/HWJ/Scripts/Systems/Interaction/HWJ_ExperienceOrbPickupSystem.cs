using UnityEngine;

/// <summary>
/// Runtime pickup for experience reward orbs.
/// It grants XP on contact and softly pulls itself toward the active player when close enough.
/// </summary>
public class HWJ_ExperienceOrbPickupSystem : MonoBehaviour
{
    [Header("Collect")]
    [SerializeField] private float collectDelaySeconds = 0.35f;

    [Header("Experience Orb")]
    [SerializeField] private int experienceAmount;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private GameObject collectEffectPrefab;

    [Header("Attraction")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private HWJ_LevelUpSystem targetLevelSystem;
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

    public int ExperienceAmount => Mathf.Max(0, experienceAmount);
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

        if (!moveToTarget || targetTransform == null || targetLevelSystem == null)
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
            TryCollect(targetLevelSystem);
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
        int amount,
        HWJ_LevelUpSystem levelSystem,
        Transform target,
        GameObject collectEffect = null)
    {
        experienceAmount = Mathf.Max(0, amount);
        targetLevelSystem = levelSystem;
        targetTransform = target;

        if (collectEffect != null)
        {
            collectEffectPrefab = collectEffect;
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

        HWJ_LevelUpSystem levelSystem = collector.GetComponent<HWJ_LevelUpSystem>();

        if (levelSystem == null)
        {
            levelSystem = collector.GetComponentInParent<HWJ_LevelUpSystem>();
        }

        return TryCollect(levelSystem);
    }

    public bool TryCollect(HWJ_LevelUpSystem levelSystem)
    {
        if (!CanCollectNow() || isCollected || levelSystem == null || ExperienceAmount <= 0)
        {
            return false;
        }

        isCollected = true;
        levelSystem.AddExperience(ExperienceAmount);
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

        if (targetTransform != null && targetLevelSystem == null)
        {
            targetLevelSystem = targetTransform.GetComponent<HWJ_LevelUpSystem>();

            if (targetLevelSystem == null)
            {
                targetLevelSystem = targetTransform.GetComponentInParent<HWJ_LevelUpSystem>();
            }
        }

        if (targetTransform != null && targetLevelSystem != null)
        {
            return;
        }

        if (!force && Time.time < nextTargetSearchTime)
        {
            return;
        }

        nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchIntervalSeconds);

        if (HWJ_PickupTargetUtility.TryResolvePlayerLevelTarget(
            out HWJ_LevelUpSystem resolvedLevelSystem,
            out Transform resolvedTargetTransform))
        {
            targetLevelSystem = resolvedLevelSystem;
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
        if (collectEffectPrefab == null)
        {
            return;
        }

        GameObject effectObject = HWJ_GameAccess.Spawn(collectEffectPrefab, transform.position, Quaternion.identity);

        if (effectObject == null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
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

        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Despawn(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}

/// <summary>
/// Shared lookup helpers for pickup objects that should fly toward the active player.
/// This lives with the pickup systems so the current generated HWJ runtime project includes it immediately.
/// </summary>
public static class HWJ_PickupTargetUtility
{
    public static bool TryResolvePlayerLevelTarget(out HWJ_LevelUpSystem levelSystem, out Transform targetTransform)
    {
        levelSystem = null;
        targetTransform = null;

        if (TryResolvePlayerResolver(out HWJ_RootObjectDataResolver resolver))
        {
            levelSystem = resolver.GetComponent<HWJ_LevelUpSystem>();
            targetTransform = resolver.transform;
            return levelSystem != null;
        }

        return false;
    }

    public static bool TryResolvePlayerStatusTarget(out HWJ_RuntimeStatusSystem statusSystem, out Transform targetTransform)
    {
        statusSystem = null;
        targetTransform = null;

        if (TryResolvePlayerResolver(out HWJ_RootObjectDataResolver resolver))
        {
            statusSystem = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
            targetTransform = resolver.transform;
            return statusSystem != null;
        }

        return false;
    }

    private static bool TryResolvePlayerResolver(out HWJ_RootObjectDataResolver resolver)
    {
        resolver = null;

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            resolver = HWJ_GameAccess.Manager.PlayerResolver;
            return true;
        }

        HWJ_RootObjectDataResolver[] resolvers = Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver candidate = resolvers[i];

            if (candidate != null && candidate.ObjectType == HWJ_ObjectType.Player)
            {
                resolver = candidate;
                return true;
            }
        }

        return false;
    }
}
