using UnityEngine;

/// <summary>
/// Spawns a one-shot visual effect when this object's RuntimeStatus receives confirmed damage.
/// This keeps hit feedback outside attack scripts, so every damage source can share the same effect path.
/// </summary>
public class HWJ_HitEffectSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform hitEffectSocket;
    [SerializeField] private string hitEffectSocketName = "Hit";
    [SerializeField] private Vector2 fallbackOffset = new Vector2(0f, 0.2f);
    [SerializeField] private bool ignoreZeroDamage = true;
    [SerializeField] private float minSpawnIntervalSeconds = 0.03f;
    [SerializeField] private float fallbackDestroyDelaySeconds = 1.25f;

    private float nextAllowedSpawnTime;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        HWJ_GameplayEvents.DamageApplied += HandleDamageApplied;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.DamageApplied -= HandleDamageApplied;
    }

    public void SetHitEffectPrefab(GameObject prefab)
    {
        hitEffectPrefab = prefab;
    }

    private void HandleDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (hitEffectPrefab == null || damageEvent.TargetStatus == null)
        {
            return;
        }

        if (runtimeStatus == null)
        {
            CacheReferences();
        }

        if (damageEvent.TargetStatus != runtimeStatus)
        {
            return;
        }

        if (ignoreZeroDamage && damageEvent.Damage <= 0f)
        {
            return;
        }

        if (Time.time < nextAllowedSpawnTime)
        {
            return;
        }

        nextAllowedSpawnTime = Time.time + Mathf.Max(0f, minSpawnIntervalSeconds);
        SpawnHitEffect();
    }

    private void SpawnHitEffect()
    {
        Vector3 spawnPosition = ResolveSpawnPosition();
        GameObject effectObject = null;
        bool spawnedThroughManager = false;

        if (HWJ_GameAccess.HasManager)
        {
            effectObject = HWJ_GameAccess.Spawn(hitEffectPrefab, spawnPosition, Quaternion.identity);
            spawnedThroughManager = effectObject != null;
        }

        if (effectObject == null)
        {
            effectObject = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);
        }

        if (effectObject == null)
        {
            return;
        }

        effectObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        // Without a GameManager/ObjectPool in quick scene tests, destroy the temporary fallback instance.
        if (!spawnedThroughManager)
        {
            Destroy(effectObject, Mathf.Max(0.05f, fallbackDestroyDelaySeconds));
        }
    }

    private Vector3 ResolveSpawnPosition()
    {
        Transform socket = ResolveHitSocket();

        if (socket != null)
        {
            return socket.position;
        }

        Transform targetTransform = runtimeStatus != null ? runtimeStatus.transform : transform;
        return targetTransform.position + new Vector3(fallbackOffset.x, fallbackOffset.y, 0f);
    }

    private Transform ResolveHitSocket()
    {
        if (hitEffectSocket != null)
        {
            return hitEffectSocket;
        }

        string resolvedSocketName = ResolveHitSocketName();

        if (string.IsNullOrEmpty(resolvedSocketName))
        {
            return null;
        }

        Transform searchRoot = runtimeStatus != null ? runtimeStatus.transform : transform;
        hitEffectSocket = FindChildByName(searchRoot, resolvedSocketName);
        return hitEffectSocket;
    }

    private string ResolveHitSocketName()
    {
        if (dataResolver != null
            && dataResolver.Model != null
            && !string.IsNullOrEmpty(dataResolver.Model.hitEffectSocketName))
        {
            return dataResolver.Model.hitEffectSocketName;
        }

        return hitEffectSocketName;
    }

    private void CacheReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponentInChildren<HWJ_RuntimeStatusSystem>();
        }

        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (dataResolver == null && runtimeStatus != null)
        {
            dataResolver = runtimeStatus.GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (dataResolver == null)
        {
            dataResolver = GetComponentInParent<HWJ_RootObjectDataResolver>();
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChildByName(child, childName);

            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
