using UnityEngine;

public class HWJ_EnemyAttackSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatExecutionSystem combatExecutionSystem;
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool attackOnlyBodyState = true;
    [SerializeField] private float fallbackAttackRange = 1.2f;
    [SerializeField] private float fallbackAttackIntervalSeconds = 1f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.5f;
    [SerializeField] private string lastAttackResult;
    [SerializeField] private float lastDamageApplied;

    private float nextAttackTime;
    private float nextTargetSearchTime;

    public string LastAttackResult => lastAttackResult;
    public float LastDamageApplied => lastDamageApplied;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatExecutionSystem == null)
        {
            combatExecutionSystem = GetComponent<HWJ_CombatExecutionSystem>();
        }
    }

    private void Update()
    {
        if (target == null && autoFindPlayerTarget && Time.time >= nextTargetSearchTime)
        {
            target = FindPlayerTarget();
            nextTargetSearchTime = Time.time + targetSearchIntervalSeconds;
        }

        TryAutoAttack();
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public bool TryAutoAttack()
    {
        lastDamageApplied = 0f;

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastAttackResult = "Attack failed: enemy is dead.";
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.CurrentState == HWJ_RuntimeState.Hit)
        {
            lastAttackResult = "Attack failed: enemy is hit.";
            return false;
        }

        if (target == null)
        {
            lastAttackResult = "Attack failed: missing target.";
            return false;
        }

        if (attackOnlyBodyState && !CanAttackTargetState())
        {
            lastAttackResult = "Attack failed: target is not in body state.";
            return false;
        }

        if (Time.time < nextAttackTime)
        {
            lastAttackResult = "Attack failed: cooldown.";
            return false;
        }

        float attackRange = GetAttackRange();
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            lastAttackResult = "Attack failed: target out of range.";
            return false;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (targetResolver == null)
        {
            lastAttackResult = "Attack failed: target has no data resolver.";
            return false;
        }

        nextAttackTime = Time.time + GetAttackIntervalSeconds();

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(HWJ_RuntimeState.Attack);
        }

        if (combatExecutionSystem != null && combatExecutionSystem.TryExecuteAttackTo(targetResolver))
        {
            lastDamageApplied = combatExecutionSystem.LastDamageApplied;
            lastAttackResult = combatExecutionSystem.LastExecutionResult;
            return true;
        }

        lastAttackResult = combatExecutionSystem != null
            ? combatExecutionSystem.LastExecutionResult
            : "Attack failed: missing combat execution system.";
        return false;
    }

    private float GetAttackRange()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State.attackRange > 0f)
        {
            return enemyData.State.attackRange;
        }

        return fallbackAttackRange;
    }

    private float GetAttackIntervalSeconds()
    {
        float attackSpeed = runtimeStatus != null ? runtimeStatus.AttackSpeed : 0f;

        if (attackSpeed > 0f)
        {
            return 1f / attackSpeed;
        }

        return fallbackAttackIntervalSeconds;
    }

    private bool CanAttackTargetState()
    {
        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul == null || targetSoul.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fallbackAttackRange);
    }
}
