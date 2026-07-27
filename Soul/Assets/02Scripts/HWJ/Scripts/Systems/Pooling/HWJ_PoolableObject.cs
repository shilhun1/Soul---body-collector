using System.Collections;
using UnityEngine;

/// <summary>
/// 풀에서 생성된 오브젝트가 자기 풀로 돌아갈 수 있게 해주는 컴포넌트입니다.
/// 투사체, 이펙트, 스폰 오브젝트 프리팹에 붙이면 Destroy 대신 ReturnToPool을 사용할 수 있습니다.
/// </summary>
public class HWJ_PoolableObject : MonoBehaviour
{
    private HWJ_ObjectPoolSystem ownerPool;
    private GameObject prefabKey;

    public GameObject PrefabKey => prefabKey;

    /// <summary>
    /// ObjectPoolSystem이 생성 직후 이 오브젝트의 원본 프리팹과 소유 풀을 기록합니다.
    /// 외부 시스템에서 직접 호출할 필요는 없습니다.
    /// </summary>
    public void SetPoolInfo(HWJ_ObjectPoolSystem ownerPool, GameObject prefabKey)
    {
        this.ownerPool = ownerPool;
        this.prefabKey = prefabKey;
    }

    /// <summary>
    /// 풀에서 꺼냈을 때 호출됩니다.
    /// 필요한 경우 파생 컴포넌트나 다른 스크립트가 이 타이밍에 상태를 초기화하면 됩니다.
    /// </summary>
    public void OnSpawnedFromPool()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 풀에 들어가기 직전에 호출됩니다.
    /// Rigidbody나 이펙트 잔여 상태 정리는 필요한 시스템에서 이 타이밍에 확장합니다.
    /// </summary>
    public void OnDespawnedToPool()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 이 오브젝트를 소유 풀로 되돌립니다.
    /// 풀 정보가 없으면 안전하게 비활성화만 합니다.
    /// </summary>
    public void ReturnToPool()
    {
        if (ownerPool == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ownerPool.Despawn(gameObject);
    }
}

/// <summary>
/// One-shot attack effects use this to reset their Animator when spawned and return to the pool after playback.
/// This keeps slash effects reusable through HWJ_ObjectPoolSystem instead of leaving spawned objects in the scene.
/// </summary>
public class HWJ_EffectAutoReturnSystem : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_PoolableObject poolableObject;
    [SerializeField] private float lifetimeSeconds = 0.65f;

    private Coroutine lifetimeRoutine;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
        }

        lifetimeRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    private void OnDisable()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }
    }

    public void SetLifetime(float lifetimeSeconds)
    {
        this.lifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, lifetimeSeconds));

        if (poolableObject != null)
        {
            poolableObject.ReturnToPool();
            yield break;
        }

        gameObject.SetActive(false);
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (poolableObject == null)
        {
            poolableObject = GetComponent<HWJ_PoolableObject>();
        }
    }
}

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
