using UnityEngine;

/// <summary>
/// Moves one runtime skill projectile, checks its hit box, and returns pooled projectiles.
/// HWJ_SkillActionSystem creates and initializes this component when a projectile skill fires.
/// </summary>
internal class HWJ_RuntimeSkillProjectile : MonoBehaviour
{
    private readonly Collider2D[] hits = new Collider2D[8];

    private Transform owner;
    private HWJ_CombatExecutionSystem combatExecutionSystem;
    private HWJ_SkillActionDataSO skillAction;
    private Vector2 direction;
    private float maxDistance;
    private float speed;
    private float hitHeight;
    private float traveledDistance;

    public void Initialize(
        Transform owner,
        HWJ_CombatExecutionSystem combatExecutionSystem,
        HWJ_SkillActionDataSO skillAction,
        Vector2 direction,
        float maxDistance,
        float speed,
        float hitHeight)
    {
        this.owner = owner;
        this.combatExecutionSystem = combatExecutionSystem;
        this.skillAction = skillAction;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.maxDistance = Mathf.Max(0.1f, maxDistance);
        this.speed = Mathf.Max(0.1f, speed);
        this.hitHeight = Mathf.Max(0.25f, hitHeight);
        traveledDistance = 0f;

        transform.position = owner != null ? owner.position : transform.position;
        transform.right = this.direction;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        traveledDistance += step;

        if (TryHitTarget() || traveledDistance >= maxDistance)
        {
            DespawnOrDestroy();
        }
    }

    private bool TryHitTarget()
    {
        if (combatExecutionSystem == null || skillAction == null)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = Physics2D.queriesHitTriggers;

        Vector2 hitBoxSize = new Vector2(0.8f, hitHeight);
        int hitCount = Physics2D.OverlapBox(transform.position, hitBoxSize, 0f, filter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || owner != null && hit.transform.IsChildOf(owner))
            {
                continue;
            }

            if (HWJ_WeaponGimmickActivatorUtility.TryActivateFromHit(
                hit,
                owner,
                skillAction,
                HWJ_GimmickHitSource.Projectile,
                out bool consumeProjectile,
                out _)
                && consumeProjectile)
            {
                return true;
            }

            HWJ_RootObjectDataResolver targetResolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (targetResolver == null)
            {
                continue;
            }

            if (combatExecutionSystem.TryExecuteAttackTo(
                targetResolver,
                skillAction.DamageMultiplier,
                skillAction.MotionKey,
                false))
            {
                return true;
            }
        }

        return false;
    }

    private void DespawnOrDestroy()
    {
        HWJ_PoolableObject poolableObject = GetComponent<HWJ_PoolableObject>();

        if (poolableObject != null && poolableObject.PrefabKey != null)
        {
            poolableObject.ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }
}
