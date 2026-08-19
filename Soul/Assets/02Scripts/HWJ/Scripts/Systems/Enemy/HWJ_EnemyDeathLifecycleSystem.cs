using UnityEngine;

/// <summary>
/// 적이 사망한 뒤 빙의 가능한 육신을 유지할지, 제거할지를 EnemyTypeDataSO 기준으로 처리합니다.
/// 유지 대상이 아닌 적은 설정한 대기 시간 뒤 풀로 반환하거나 비활성화합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
[RequireComponent(typeof(HWJ_RuntimeStatusSystem))]
public class HWJ_EnemyDeathLifecycleSystem : MonoBehaviour
{
    [Header("핵심 참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;

    [Header("사망 처리")]
    [Tooltip("유지하지 않는 적을 비활성화하기 전까지 기다리는 시간입니다. 보상 생성과 피격 이펙트가 먼저 처리될 시간을 줍니다.")]
    [SerializeField] private float defeatedEnemyDespawnDelaySeconds = 0.2f;
    [Tooltip("빙의 가능한 시체가 플레이어와 물리적으로 밀리지 않도록 일반 콜라이더를 트리거로 전환합니다.")]
    [SerializeField] private bool makeCorpseCollidersTriggers = true;
    [Tooltip("사망 즉시 추적, 공격, AI, 스킬 행동을 멈춥니다.")]
    [SerializeField] private bool disableBehaviorOnDeath = true;
    [Tooltip("Dead 상태가 된 뒤 시체 빙의를 허용하기 전까지 기다리는 시간입니다. 쓰러지는 애니메이션이 끝나기 전에 빙의되는 것을 막습니다.")]
    [SerializeField] private float possessableCorpseReadyDelaySeconds = 1f;

    private bool deathPrepared;
    private bool despawnRequested;
    private float despawnTime;
    private float corpsePossessionReadyTime;

    public bool IsCorpsePossessionReady => deathPrepared && Time.time >= corpsePossessionReadyTime;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        deathPrepared = false;
        despawnRequested = false;
        despawnTime = 0f;
        corpsePossessionReadyTime = 0f;
    }

    private void Update()
    {
        CacheReferences();

        if (runtimeStatus == null || !runtimeStatus.IsDead)
        {
            return;
        }

        if (!deathPrepared)
        {
            PrepareDeathState();
        }

        if (despawnRequested && Time.time >= despawnTime)
        {
            DespawnDefeatedEnemy();
        }
    }

    private void PrepareDeathState()
    {
        deathPrepared = true;
        bool leavesPossessableCorpse = LeavesPossessableCorpse();

        if (disableBehaviorOnDeath)
        {
            SetBehaviorEnabled(false);
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            if (leavesPossessableCorpse)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
        }

        if (leavesPossessableCorpse)
        {
            corpsePossessionReadyTime = Time.time + Mathf.Max(0f, possessableCorpseReadyDelaySeconds);

            if (makeCorpseCollidersTriggers)
            {
                SetCollidersToTrigger();
            }

            return;
        }

        despawnRequested = true;
        despawnTime = Time.time + Mathf.Max(0f, defeatedEnemyDespawnDelaySeconds);
    }

    private bool LeavesPossessableCorpse()
    {
        return dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.Role != null
            && enemyData.Role.leavesCorpseOnDeath
            && enemyData.Role.isPossessableBody;
    }

    private void SetBehaviorEnabled(bool enabled)
    {
        HWJ_EnemyNavigationSystem navigation = GetComponent<HWJ_EnemyNavigationSystem>();
        HWJ_EnemyAttackSystem attack = GetComponent<HWJ_EnemyAttackSystem>();
        HWJ_MonsterAISystem monsterAI = GetComponent<HWJ_MonsterAISystem>();
        HWJ_SkillActionSystem skillAction = GetComponent<HWJ_SkillActionSystem>();

        if (navigation != null)
        {
            navigation.enabled = enabled;
        }

        if (attack != null)
        {
            attack.enabled = enabled;
        }

        if (monsterAI != null)
        {
            monsterAI.enabled = enabled;
        }

        if (skillAction != null)
        {
            skillAction.enabled = enabled;
        }
    }

    private void SetCollidersToTrigger()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].isTrigger = true;
            }
        }
    }

    private void DespawnDefeatedEnemy()
    {
        despawnRequested = false;
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

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }
}
