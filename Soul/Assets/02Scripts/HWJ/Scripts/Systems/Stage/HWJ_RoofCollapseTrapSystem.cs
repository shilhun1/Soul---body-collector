using System.Collections;
using UnityEngine;

/// <summary>
/// Stage1-1 발표용 지붕 낙석 함정입니다.
/// 플레이어가 트리거에 들어오면 경고 표시 후 실제 잔해 Sprite를 떨어뜨립니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Stage/Roof Collapse Trap System")]
public sealed class HWJ_RoofCollapseTrapSystem : MonoBehaviour
{
    [Header("시각 자료")]
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private SpriteRenderer warningVisual;
    [SerializeField] private Transform[] debrisSpawnPoints;

    [Header("낙석 판정")]
    [SerializeField] private float damage = 18f;
    [SerializeField] private float warningSeconds = 0.8f;
    [SerializeField] private float debrisLifetimeSeconds = 4f;
    [SerializeField] private float debrisFallGravity = 3.8f;
    [SerializeField] private float knockbackPower = 11f;
    [SerializeField] private bool oneShot = true;

    [Header("런타임 확인")]
    [SerializeField] private bool hasTriggered;
    [SerializeField] private bool isRunning;

    private void Awake()
    {
        if (warningVisual != null)
        {
            warningVisual.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanStartCollapse(other))
        {
            return;
        }

        StartCoroutine(CollapseRoutine());
    }

    private bool CanStartCollapse(Collider2D other)
    {
        if (isRunning || oneShot && hasTriggered)
        {
            return false;
        }

        return other != null && other.CompareTag("Player");
    }

    private IEnumerator CollapseRoutine()
    {
        hasTriggered = true;
        isRunning = true;

        if (warningVisual != null)
        {
            warningVisual.enabled = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, warningSeconds));

        if (warningVisual != null)
        {
            warningVisual.enabled = false;
        }

        SpawnDebrisObjects();
        isRunning = false;
    }

    private void SpawnDebrisObjects()
    {
        if (debrisSpawnPoints == null || debrisSpawnPoints.Length == 0)
        {
            SpawnDebris(transform.position + Vector3.down * 0.4f, 0);
            return;
        }

        for (int i = 0; i < debrisSpawnPoints.Length; i++)
        {
            if (debrisSpawnPoints[i] == null)
            {
                continue;
            }

            SpawnDebris(debrisSpawnPoints[i].position, i);
        }
    }

    private void SpawnDebris(Vector3 spawnPosition, int index)
    {
        GameObject debrisObject = new GameObject($"{gameObject.name}_FallingDebris_{index + 1:00}");
        debrisObject.transform.position = spawnPosition;
        debrisObject.transform.SetParent(transform.parent);

        SpriteRenderer renderer = debrisObject.AddComponent<SpriteRenderer>();
        renderer.sprite = debrisSprite;
        renderer.sortingOrder = 31;

        Rigidbody2D body = debrisObject.AddComponent<Rigidbody2D>();
        body.gravityScale = Mathf.Max(0.1f, debrisFallGravity);
        body.freezeRotation = false;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = debrisObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one * 0.85f;

        HWJ_RoofDebrisDamageSystem damageSystem = debrisObject.AddComponent<HWJ_RoofDebrisDamageSystem>();
        damageSystem.Configure(damage, knockbackPower, debrisLifetimeSeconds);
    }
}

/// <summary>
/// 낙하 잔해가 플레이어 또는 적에게 닿았을 때 피해와 넉백을 적용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HWJ_RoofDebrisDamageSystem : MonoBehaviour
{
    [SerializeField] private float damage = 18f;
    [SerializeField] private float knockbackPower = 11f;
    [SerializeField] private float lifetimeSeconds = 4f;
    [SerializeField] private bool consumed;

    public void Configure(float newDamage, float newKnockbackPower, float newLifetimeSeconds)
    {
        damage = Mathf.Max(0f, newDamage);
        knockbackPower = Mathf.Max(0f, newKnockbackPower);
        lifetimeSeconds = Mathf.Max(0.1f, newLifetimeSeconds);
    }

    private void Start()
    {
        Destroy(gameObject, lifetimeSeconds);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ApplyHit(collision != null ? collision.gameObject : null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ApplyHit(other != null ? other.gameObject : null);
    }

    private void ApplyHit(GameObject target)
    {
        if (consumed || target == null)
        {
            return;
        }

        if (!target.CompareTag("Player") && !target.CompareTag("Enemy"))
        {
            return;
        }

        HWJ_SoulSystem soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return;
        }

        consumed = true;

        HWJ_RuntimeStatusSystem statusSystem = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();

        if (statusSystem != null)
        {
            statusSystem.ApplyDamage(damage);
        }

        HWJ_KnockbackSystem knockbackSystem = target.GetComponentInParent<HWJ_KnockbackSystem>();

        if (knockbackSystem != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            direction.y = Mathf.Max(0.35f, direction.y);
            knockbackSystem.PlayKnockback(direction, knockbackPower, 0.25f);
        }

        Destroy(gameObject);
    }
}
