using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 격투가 보스의 현재 타격 프레임만 활성화되는 공격 판정입니다.
/// 한 타격 단계에서는 같은 대상을 한 번만 공격하고, 다음 타격이 시작될 때 적중 기록을 초기화합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class HWJ_FighterBossHitboxSystem : MonoBehaviour
{
    [SerializeField] private HWJ_CombatSystem ownerCombat;
    [SerializeField] private BoxCollider2D hitboxCollider;
    [SerializeField] private HWJ_ObjectType targetObjectType = HWJ_ObjectType.Player;
    [SerializeField] private bool isArmed;
    [SerializeField] private int currentStrikeNumber;
    [SerializeField] private float currentDamageMultiplier = 1f;
    [SerializeField] private float currentExtraKnockbackPower;

    private readonly HashSet<int> hitTargetIds = new HashSet<int>();
    private int totalSuccessfulHits;
    private int totalArmCount;

    public bool IsArmed => isArmed;
    public bool ColliderEnabled => hitboxCollider != null && hitboxCollider.enabled;
    public int CurrentStrikeNumber => currentStrikeNumber;
    public int TotalSuccessfulHits => totalSuccessfulHits;
    public int TotalArmCount => totalArmCount;

    /// <summary>
    /// Configures a runtime-created pooled hitbox without exposing serialized fields.
    /// Prefab hitboxes continue to use their Inspector references.
    /// </summary>
    public void Configure(
        HWJ_CombatSystem combatOwner,
        BoxCollider2D collider,
        HWJ_ObjectType targetType = HWJ_ObjectType.Player)
    {
        ownerCombat = combatOwner;
        hitboxCollider = collider != null ? collider : GetComponent<BoxCollider2D>();
        targetObjectType = targetType;

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }

        Disarm();
    }

    private void Awake()
    {
        CacheReferences();
        Disarm();
    }

    private void FixedUpdate()
    {
        if (isArmed)
        {
            ScanForTargets();
        }
    }

    private void OnDisable()
    {
        Disarm();
    }

    /// <summary>
    /// 지정한 Combo 타격 단계의 판정을 시작합니다.
    /// damageMultiplier는 공통 CombatSystem의 공격력 계산에 곱해집니다.
    /// </summary>
    public void ArmStrike(int strikeNumber, float damageMultiplier, float extraKnockbackPower)
    {
        CacheReferences();
        hitTargetIds.Clear();
        currentStrikeNumber = Mathf.Max(1, strikeNumber);
        currentDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        currentExtraKnockbackPower = Mathf.Max(0f, extraKnockbackPower);
        isArmed = true;
        totalArmCount++;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        Physics2D.SyncTransforms();
        ScanForTargets();
    }

    /// <summary>
    /// 현재 타격 판정을 즉시 종료합니다. 공격 종료와 취소 경로에서 항상 호출됩니다.
    /// </summary>
    public void Disarm()
    {
        isArmed = false;
        currentStrikeNumber = 0;
        currentDamageMultiplier = 1f;
        currentExtraKnockbackPower = 0f;
        hitTargetIds.Clear();

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private void ScanForTargets()
    {
        if (!isArmed || hitboxCollider == null || ownerCombat == null)
        {
            return;
        }

        Vector2 center = transform.TransformPoint(hitboxCollider.offset);
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(
            Mathf.Abs(hitboxCollider.size.x * scale.x),
            Mathf.Abs(hitboxCollider.size.y * scale.y));
        float angle = transform.eulerAngles.z;
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, angle);

        for (int i = 0; i < overlaps.Length; i++)
        {
            TryHit(overlaps[i]);
        }
    }

    private void TryHit(Collider2D overlap)
    {
        if (overlap == null || overlap == hitboxCollider)
        {
            return;
        }

        HWJ_RootObjectDataResolver targetResolver = overlap.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null
            || targetResolver == ownerCombat.DataResolver
            || targetResolver.ObjectType != targetObjectType)
        {
            return;
        }

        int targetId = targetResolver.GetInstanceID();

        if (!hitTargetIds.Add(targetId))
        {
            return;
        }

        if (!ownerCombat.TryDealDamageTo(targetResolver, currentDamageMultiplier, out _))
        {
            hitTargetIds.Remove(targetId);
            return;
        }

        totalSuccessfulHits++;

        if (currentExtraKnockbackPower > 0f)
        {
            ApplyExtraKnockback(targetResolver, currentExtraKnockbackPower);
        }
    }

    private void ApplyExtraKnockback(HWJ_RootObjectDataResolver targetResolver, float power)
    {
        HWJ_KnockbackSystem knockback = targetResolver.GetComponent<HWJ_KnockbackSystem>();

        if (knockback == null)
        {
            knockback = targetResolver.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = targetResolver.transform.position - ownerCombat.transform.position;
        direction.y = 0f;
        knockback.PlayKnockback(direction, power, 0.25f);
    }

    private void CacheReferences()
    {
        if (ownerCombat == null)
        {
            ownerCombat = GetComponentInParent<HWJ_CombatSystem>();
        }

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
        }

        if (hitboxCollider == null)
        {
            return;
        }

        Gizmos.color = isArmed ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(hitboxCollider.offset, hitboxCollider.size);
        Gizmos.matrix = previousMatrix;
    }
}
