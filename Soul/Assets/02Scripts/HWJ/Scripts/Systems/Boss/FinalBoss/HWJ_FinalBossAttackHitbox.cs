using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 최종보스가 런타임에 생성한 투사체와 장판의 판정을 처리합니다.
/// HP 피해와 현재 빙의 정신력 피해를 분리해 한 공격이 잘못된 자원을 차감하지 않게 합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class HWJ_FinalBossAttackHitbox : MonoBehaviour
{
    [SerializeField] private HWJ_CombatSystem ownerCombat;
    [SerializeField] private BoxCollider2D hitboxCollider;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float possessionMentalDamageRatio;
    [SerializeField] private float stunSeconds;
    [SerializeField] private float repeatHitIntervalSeconds = 0.5f;
    [SerializeField] private bool repeatHits;
    [SerializeField] private bool armed;
    [SerializeField] private int totalSuccessfulHits;

    private readonly Dictionary<int, float> nextHitTimes = new Dictionary<int, float>();

    public int TotalSuccessfulHits => totalSuccessfulHits;
    public bool IsArmed => armed;

    private void Awake()
    {
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
        }

        hitboxCollider.isTrigger = true;
    }

    private void FixedUpdate()
    {
        if (armed)
        {
            ScanForPlayer();
        }
    }

    private void OnDisable()
    {
        Disarm();
    }

    public void Configure(
        HWJ_CombatSystem combatOwner,
        BoxCollider2D collider,
        float configuredDamageMultiplier,
        float configuredMentalDamageRatio,
        float configuredStunSeconds,
        bool allowRepeatHits,
        float configuredRepeatHitInterval)
    {
        ownerCombat = combatOwner;
        hitboxCollider = collider != null ? collider : GetComponent<BoxCollider2D>();
        damageMultiplier = Mathf.Max(0f, configuredDamageMultiplier);
        possessionMentalDamageRatio = Mathf.Clamp01(configuredMentalDamageRatio);
        stunSeconds = Mathf.Max(0f, configuredStunSeconds);
        repeatHits = allowRepeatHits;
        repeatHitIntervalSeconds = Mathf.Max(0.01f, configuredRepeatHitInterval);
        totalSuccessfulHits = 0;
        nextHitTimes.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
    }

    public void Arm()
    {
        armed = true;
        nextHitTimes.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        Physics2D.SyncTransforms();
        ScanForPlayer();
    }

    public void Disarm()
    {
        armed = false;
        nextHitTimes.Clear();

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private void ScanForPlayer()
    {
        if (!armed || hitboxCollider == null || ownerCombat == null)
        {
            return;
        }

        Vector2 center = transform.TransformPoint(hitboxCollider.offset);
        Vector3 lossyScale = transform.lossyScale;
        Vector2 size = new Vector2(
            Mathf.Abs(hitboxCollider.size.x * lossyScale.x),
            Mathf.Abs(hitboxCollider.size.y * lossyScale.y));
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, transform.eulerAngles.z);

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

        HWJ_RootObjectDataResolver targetResolver =
            overlap.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null
            || targetResolver == ownerCombat.DataResolver
            || targetResolver.ObjectType != HWJ_ObjectType.Player)
        {
            return;
        }

        int targetId = targetResolver.GetInstanceID();

        if (nextHitTimes.TryGetValue(targetId, out float nextHitTime)
            && Time.time < nextHitTime)
        {
            return;
        }

        bool applied = possessionMentalDamageRatio > 0f
            ? TryApplyPossessionMentalDamage(targetResolver)
            : ownerCombat.TryDealDamageTo(targetResolver, damageMultiplier, out _);

        if (!applied)
        {
            return;
        }

        totalSuccessfulHits++;
        nextHitTimes[targetId] = repeatHits
            ? Time.time + repeatHitIntervalSeconds
            : float.PositiveInfinity;

        if (stunSeconds > 0f)
        {
            targetResolver.GetComponent<HWJ_RuntimeStatusSystem>()?.LockControl(stunSeconds);
        }
    }

    private bool TryApplyPossessionMentalDamage(HWJ_RootObjectDataResolver targetResolver)
    {
        HWJ_PossessionSystem possession = targetResolver != null
            ? targetResolver.GetComponent<HWJ_PossessionSystem>()
            : null;

        if (possession == null || !possession.HasActivePossessedBody)
        {
            return false;
        }

        if (possession.TryGetActiveLiveMentalState(out HWJ_LivePossessionMentalState liveMental))
        {
            float amount = liveMental.MaxMentalValue * possessionMentalDamageRatio;
            bool depleted = liveMental.ApplyMentalDrain(amount);

            if (depleted)
            {
                possession.ReleasePossessedBodyByMentalDepletion();
            }

            return amount > 0f;
        }

        HWJ_BodyDecaySystem corpseMental = targetResolver.GetComponent<HWJ_BodyDecaySystem>();

        if (corpseMental == null || !corpseMental.HasPossessionMentalRemaining)
        {
            return false;
        }

        float corpseMentalCost = corpseMental.MaxPossessionMentalValue
            * possessionMentalDamageRatio;
        corpseMental.ApplyPossessionMentalCost(
            corpseMentalCost,
            "final_boss_black_flame_charge");
        return corpseMentalCost > 0f;
    }
}
