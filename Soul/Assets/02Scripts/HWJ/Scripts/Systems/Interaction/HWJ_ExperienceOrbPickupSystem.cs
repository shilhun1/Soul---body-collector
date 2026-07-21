using UnityEngine;

/// <summary>
/// Experience reward orb that can move toward a target and grants XP once collected.
/// The orb stores runtime XP only; definition data remains on the defeated object's RewardData.
/// </summary>
public class HWJ_ExperienceOrbPickupSystem : MonoBehaviour
{
    [Header("구슬 먹는 시간")]
    [SerializeField] private float collectDelaySeconds;

    [Header("경험치 구슬")]
    [SerializeField] private int experienceAmount;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private GameObject collectEffectPrefab;

    [Header("흡수 이동")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private HWJ_LevelUpSystem targetLevelSystem;
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

    public int ExperienceAmount => Mathf.Max(0, experienceAmount);
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

        HWJ_LevelUpSystem levelSystem = collector.GetComponent<HWJ_LevelUpSystem>();

        if (levelSystem == null)
        {
            levelSystem = collector.GetComponentInParent<HWJ_LevelUpSystem>();
        }

        return TryCollect(levelSystem);
    }

    public bool TryCollect(HWJ_LevelUpSystem levelSystem)
    {
        if (!CancollectNow() || isCollected || levelSystem == null || ExperienceAmount <= 0)
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
    private bool CancollectNow()
    {
        return Time.time - spawnedTime >= Mathf.Max(0f, collectDelaySeconds);
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
