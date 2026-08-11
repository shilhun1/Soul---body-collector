using UnityEngine;

/// <summary>
/// Resolves body-collision filtering, hit stun, invincibility, and knockback reactions.
/// This keeps physical hit behavior separate from HP and spirit resource bookkeeping.
/// </summary>
public partial class HWJ_RuntimeStatusSystem
{
    private float GetBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (runtimeContext != null)
        {
            HWJ_StatusData effectiveStatus = runtimeContext.GetEffectiveStatusData();
            return effectiveStatus != null ? selector(effectiveStatus) : 0f;
        }

        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return selector(possessedStatus);
        }

        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetRuntimeBodyMaxHpOrBase()
    {
        if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
        {
            return bodyState.MaxHp;
        }

        return GetBaseStatusValue(status => status.maxHp);
    }

    private HWJ_ReceivedDamageData GetReceivedDamageData()
    {
        if (runtimeContext != null)
        {
            return runtimeContext.GetEffectiveReceivedDamageData();
        }

        if (possessionSystem != null
            && possessionSystem.TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData possessedReceivedDamage))
        {
            return possessedReceivedDamage;
        }

        return dataResolver != null ? dataResolver.ReceivedDamage : null;
    }

    private bool IsDataInvincible()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        return receivedDamage != null && receivedDamage.isInvincible;
    }

    private bool HasDataSuperArmor()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null && receivedDamage.hasSuperArmor)
        {
            return true;
        }

        return dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State != null
            && enemyData.State.hasSuperArmor;
    }

    private bool ShouldDataIgnoreKnockback()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        return receivedDamage != null && receivedDamage.ignoreKnockback;
    }

    private bool IsBossBody()
    {
        return dataResolver != null && dataResolver.ObjectType == HWJ_ObjectType.Boss;
    }

    private bool ShouldUseBodyCollisionFilter()
    {
        if (dataResolver == null)
        {
            return false;
        }

        return dataResolver.ObjectType == HWJ_ObjectType.Player
            || dataResolver.ObjectType == HWJ_ObjectType.Enemy
            || dataResolver.ObjectType == HWJ_ObjectType.Boss;
    }

    private void CacheBodyCollisionColliders()
    {
        bodyCollisionColliders = GetComponentsInChildren<Collider2D>();
    }

    private void UpdateBodyCollisionIgnores()
    {
        if (Time.time < nextBodyCollisionRefreshTime)
        {
            return;
        }

        nextBodyCollisionRefreshTime = Time.time + Mathf.Max(0.02f, bodyCollisionRefreshSeconds);

        if (!ShouldUseBodyCollisionFilter())
        {
            return;
        }

        if (bodyCollisionColliders == null || bodyCollisionColliders.Length == 0)
        {
            CacheBodyCollisionColliders();
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver otherResolver = resolvers[i];

            if (otherResolver == null
                || otherResolver == dataResolver
                || !ShouldIgnoreBodyCollisionWith(otherResolver))
            {
                continue;
            }

            IgnoreBodyCollisionWith(otherResolver);
        }
    }

    private bool ShouldIgnoreBodyCollisionWith(HWJ_RootObjectDataResolver otherResolver)
    {
        if (dataResolver == null || otherResolver == null)
        {
            return false;
        }

        HWJ_ObjectType selfType = dataResolver.ObjectType;
        HWJ_ObjectType otherType = otherResolver.ObjectType;

        if (selfType == HWJ_ObjectType.Player)
        {
            return ignorePlayerMonsterBodyCollision && IsMonsterType(otherType);
        }

        if (IsMonsterType(selfType))
        {
            return otherType == HWJ_ObjectType.Player && ignorePlayerMonsterBodyCollision
                || IsMonsterType(otherType) && ignoreMonsterBodyCollision;
        }

        return false;
    }

    private void IgnoreBodyCollisionWith(HWJ_RootObjectDataResolver otherResolver)
    {
        if (bodyCollisionColliders == null)
        {
            return;
        }

        Collider2D[] otherColliders = otherResolver.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < bodyCollisionColliders.Length; i++)
        {
            Collider2D ownedCollider = bodyCollisionColliders[i];

            if (!IsPhysicalBodyCollider(ownedCollider))
            {
                continue;
            }

            for (int j = 0; j < otherColliders.Length; j++)
            {
                Collider2D otherCollider = otherColliders[j];

                if (!IsPhysicalBodyCollider(otherCollider) || otherCollider == ownedCollider)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(ownedCollider, otherCollider, true);
            }
        }
    }

    private static bool IsPhysicalBodyCollider(Collider2D collider)
    {
        return collider != null && !collider.isTrigger;
    }

    private static bool IsMonsterType(HWJ_ObjectType objectType)
    {
        return objectType == HWJ_ObjectType.Enemy || objectType == HWJ_ObjectType.Boss;
    }

    private float GetKnockbackWeight()
    {
        HWJ_StatusData status = GetStatusDataForWeight();
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        float bodyWeight = status != null && status.bodyWeight > 0f ? status.bodyWeight : 1f;
        float reactionWeight = receivedDamage != null && receivedDamage.knockbackWeightMultiplier > 0f
            ? receivedDamage.knockbackWeightMultiplier
            : 1f;
        return bodyWeight * reactionWeight;
    }

    private HWJ_StatusData GetStatusDataForWeight()
    {
        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return possessedStatus;
        }

        return dataResolver != null ? dataResolver.Status : null;
    }

    private float GetHitStunSeconds(HWJ_DamageData sourceDamage)
    {
        if (IsHitReactionImmune)
        {
            return 0f;
        }

        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null && receivedDamage.ignoreHitStun)
        {
            return 0f;
        }

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State != null
            && enemyData.State.immuneToHitStun)
        {
            return 0f;
        }

        if (sourceDamage != null && sourceDamage.hitStunSeconds > 0f)
        {
            return sourceDamage.hitStunSeconds;
        }

        return receivedDamage != null ? Mathf.Max(0f, receivedDamage.hitStunSeconds) : 0f;
    }

    private void ApplyPostHitTimers(Component source, HWJ_DamageData sourceDamage)
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null)
        {
            GrantInvincibility(receivedDamage.invincibleSecondsAfterHit);
            hitReactionImmuneEndTime = Mathf.Max(
                hitReactionImmuneEndTime,
                Time.time + Mathf.Max(0f, receivedDamage.hitReactionImmuneSeconds));
        }

        int sourceId = source != null ? source.GetInstanceID() : 0;

        if (sourceId == 0)
        {
            return;
        }

        float cooldownSeconds = sourceDamage != null
            ? Mathf.Max(0f, sourceDamage.sameTargetHitCooldownSeconds)
            : 0f;

        if (cooldownSeconds > 0f)
        {
            nextDamageTimesBySource[sourceId] = Time.time + cooldownSeconds;
        }
    }

    private float GetOwnerBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetStoredHpForActiveState()
    {
        if (soulSystem == null)
        {
            return MaxHp;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            return possessedBodyHp;
        }

        return soulHp;
    }

    private void ApplyHitReaction(float damage, HWJ_DamageData sourceDamage)
    {
        bool reactionBlockedBySuperArmor = HasSuperArmor;
        bool reactionBlockedByLimit = IsHitReactionLimited;
        bossBrain?.NotifyDamageTaken(damage, reactionBlockedBySuperArmor, reactionBlockedByLimit);

        if (bossBrain != null && bossBrain.IsGroggy)
        {
            return;
        }

        if (reactionBlockedBySuperArmor)
        {
            return;
        }

        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (!TryConsumeHitReactionSlot(receivedDamage))
        {
            return;
        }

        float hitStunSeconds = GetHitStunSeconds(sourceDamage);

        if (hitStunSeconds > 0f)
        {
            hitStunEndTime = Mathf.Max(hitStunEndTime, Time.time + hitStunSeconds);
            LockControl(hitStunSeconds);
        }

        SetState(HWJ_RuntimeState.Hit);
        motionSystem?.PlayHit();
    }

    private bool TryConsumeHitReactionSlot(HWJ_ReceivedDamageData receivedDamage)
    {
        if (receivedDamage == null || receivedDamage.maxHitReactionsPerWindow <= 0)
        {
            return true;
        }

        if (IsHitReactionLimited)
        {
            return false;
        }

        float windowSeconds = Mathf.Max(0.01f, receivedDamage.hitReactionWindowSeconds);

        if (Time.time >= hitReactionWindowEndTime)
        {
            hitReactionWindowEndTime = Time.time + windowSeconds;
            hitReactionCountInWindow = 0;
        }

        if (hitReactionCountInWindow >= receivedDamage.maxHitReactionsPerWindow)
        {
            float immuneSeconds = Mathf.Max(0f, receivedDamage.hitReactionLimitImmuneSeconds);

            if (immuneSeconds > 0f)
            {
                hitReactionLimitEndTime = Mathf.Max(hitReactionLimitEndTime, Time.time + immuneSeconds);
            }

            return false;
        }

        hitReactionCountInWindow++;
        return true;
    }

}
