using UnityEngine;

public class HWJ_CombatExecutionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;

    [Header("Target Filter")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private bool useObjectTypeDefaultTargetFilter = true;
    [SerializeField] private bool canDamagePlayer;
    [SerializeField] private bool canDamageEnemy = true;
    [SerializeField] private bool canDamageBoss = true;
    [SerializeField] private bool canDamageNpc;

    [Header("Area Attack")]
    [SerializeField] private Vector2 attackOffset;
    [SerializeField] private float fallbackAttackRange = 1.5f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private bool playMotionOnExecute = true;
    [SerializeField] private string motionKey;
    [SerializeField] private string lastExecutionResult;
    [SerializeField] private float lastDamageApplied;

    private readonly Collider2D[] hits = new Collider2D[16];

    public string LastExecutionResult => lastExecutionResult;
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

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        ApplyDefaultTargetFilter();
    }

    public bool TryExecuteAreaAttack()
    {
        return TryExecuteAreaAttack(GetDefaultAttackRange(), damageMultiplier, motionKey);
    }

    public bool TryExecuteAreaAttack(float attackRange)
    {
        return TryExecuteAreaAttack(attackRange, damageMultiplier, motionKey);
    }

    public bool TryExecuteAreaAttack(
        float attackRange,
        float attackDamageMultiplier,
        string attackMotionKey = null,
        bool shouldPlayMotion = true)
    {
        lastDamageApplied = 0f;

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastExecutionResult = "Attack failed: owner is dead.";
            return false;
        }

        if (combatSystem == null)
        {
            lastExecutionResult = "Attack failed: missing combat system.";
            return false;
        }

        if (playMotionOnExecute && shouldPlayMotion)
        {
            PlayAttackMotion(attackMotionKey);
        }

        Vector2 center = GetAttackCenter();
        float range = Mathf.Max(0.1f, attackRange);
        int layerMask = targetLayer.value != 0 ? targetLayer.value : Physics2D.AllLayers;
        ContactFilter2D filter = CreateOverlapFilter(layerMask);
        int hitCount = Physics2D.OverlapCircle(center, range, filter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            HWJ_RootObjectDataResolver targetResolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (!CanDamageTarget(targetResolver))
            {
                continue;
            }

            if (combatSystem.TryDealDamageTo(targetResolver, attackDamageMultiplier, out float finalDamage))
            {
                lastDamageApplied = finalDamage;
                lastExecutionResult = $"Damaged {targetResolver.name}.";
                return true;
            }
        }

        lastExecutionResult = $"Attack checked {hitCount} colliders, no damageable target.";
        return false;
    }

    public bool TryExecuteAttackTo(HWJ_RootObjectDataResolver targetResolver)
    {
        return TryExecuteAttackTo(targetResolver, damageMultiplier, motionKey);
    }

    public bool TryExecuteAttackTo(
        HWJ_RootObjectDataResolver targetResolver,
        float attackDamageMultiplier,
        string attackMotionKey = null,
        bool shouldPlayMotion = true)
    {
        lastDamageApplied = 0f;

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastExecutionResult = "Attack failed: owner is dead.";
            return false;
        }

        if (!CanDamageTarget(targetResolver))
        {
            lastExecutionResult = "Attack failed: invalid target.";
            return false;
        }

        if (playMotionOnExecute && shouldPlayMotion)
        {
            PlayAttackMotion(attackMotionKey);
        }

        if (combatSystem != null
            && combatSystem.TryDealDamageTo(targetResolver, attackDamageMultiplier, out float finalDamage))
        {
            lastDamageApplied = finalDamage;
            lastExecutionResult = $"Damaged {targetResolver.name}.";
            return true;
        }

        lastExecutionResult = "Attack failed: damage was blocked or combat system is missing.";
        return false;
    }

    private void PlayAttackMotion(string attackMotionKey)
    {
        if (motionSystem == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(attackMotionKey))
        {
            motionSystem.PlayMotionKey(attackMotionKey);
            return;
        }

        motionSystem.PlayAttack(null);
    }

    private Vector2 GetAttackCenter()
    {
        float facing = Mathf.Sign(transform.localScale.x);

        if (facing == 0f)
        {
            facing = 1f;
        }

        Vector2 offset = attackOffset;
        offset.x *= facing;
        return (Vector2)transform.position + offset;
    }

    private float GetDefaultAttackRange()
    {
        if (dataResolver != null && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return Mathf.Max(fallbackAttackRange, playerData.Attack.attackRange);
        }

        if (dataResolver != null && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return Mathf.Max(fallbackAttackRange, enemyData.State.attackRange);
        }

        return fallbackAttackRange;
    }

    private bool CanDamageTarget(HWJ_RootObjectDataResolver targetResolver)
    {
        if (targetResolver == null || targetResolver == dataResolver)
        {
            return false;
        }

        switch (targetResolver.ObjectType)
        {
            case HWJ_ObjectType.Player:
                return canDamagePlayer;
            case HWJ_ObjectType.Enemy:
                return canDamageEnemy;
            case HWJ_ObjectType.Boss:
                return canDamageBoss;
            case HWJ_ObjectType.NPC:
                return canDamageNpc;
            default:
                return false;
        }
    }

    private static ContactFilter2D CreateOverlapFilter(int layerMask)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useTriggers = Physics2D.queriesHitTriggers;
        return filter;
    }

    private void ApplyDefaultTargetFilter()
    {
        if (!useObjectTypeDefaultTargetFilter || dataResolver == null)
        {
            return;
        }

        switch (dataResolver.ObjectType)
        {
            case HWJ_ObjectType.Player:
                canDamagePlayer = false;
                canDamageEnemy = true;
                canDamageBoss = true;
                canDamageNpc = false;
                break;
            case HWJ_ObjectType.Enemy:
            case HWJ_ObjectType.Boss:
                canDamagePlayer = true;
                canDamageEnemy = false;
                canDamageBoss = false;
                canDamageNpc = false;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetAttackCenter(), Mathf.Max(0.1f, fallbackAttackRange));
    }
}
