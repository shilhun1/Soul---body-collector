using UnityEngine;

public enum HWJ_GameplayActorSlot
{
    Source,
    Target
}

public enum HWJ_FloatCompareMode
{
    Less,
    LessOrEqual,
    Equal,
    GreaterOrEqual,
    Greater
}

public enum HWJ_ConditionGroupMode
{
    All,
    Any
}

public enum HWJ_StatusFlag
{
    Alive,
    Dead,
    CanMove,
    CanAttack,
    CanDash,
    HitStunned,
    TemporarilyInvincible,
    HitReactionImmune,
    SuperArmor,
    UsesHp,
    HasHpRemaining,
    IgnoreKnockback,
    HitReactionLimited
}

public enum HWJ_PossessionRequirement
{
    SourceCanPossess,
    SourceIsSoulState,
    TargetIsEnemyOrBoss,
    TargetCanBePossessed,
    TargetDefeatedIfRequired,
    WithinPossessionRange,
    TargetCorpseAvailable
}

public enum HWJ_SkillRequirement
{
    HasSkillAction,
    RequiredWeaponMatchesSource,
    SourceCanAttack,
    SourceCanDash,
    SkillCooldownReady,
    SkillUnlockedBySource
}

public enum HWJ_PossessionStateRequirement
{
    HasActivePossessedBody,
    DoesNotHaveActivePossessedBody,
    PossessedWeaponMatches,
    PossessedObjectTypeMatches,
    CanLoadBodyStats
}

public enum HWJ_BodyDecayRequirement
{
    IsDecaying,
    IsNotDecaying,
    HasDecayRemaining,
    DecayValueCompare,
    DecayRatioCompare,
    RemainingDecayValueCompare,
    RemainingDecayRatioCompare,
    DangerLevelAtLeast,
    DangerLevelEquals
}

public enum HWJ_AIRequirement
{
    HasTarget,
    StateMatches,
    TargetInTrackingRange,
    TargetInAttackRange,
    TargetDistanceCompare,
    TargetBodyState
}

public enum HWJ_BossRequirement
{
    EncounterStarted,
    EncounterNotStarted,
    StateMatches,
    PhaseNumberCompare,
    IsGroggy,
    NotGroggy,
    TargetInsideBossRoom
}

public enum HWJ_ProgressionRequirement
{
    LevelCompare,
    ExperienceCompare,
    SkillPointCompare,
    HasSkillPoint
}

public enum HWJ_AttackHitRequirement
{
    HasHitConfirmed,
    HasHitboxId,
    HitboxIdMatches,
    HasHitCollider,
    DamageTypeMatches
}

public enum HWJ_EnvironmentRequirement
{
    ActorGrounded,
    GroundBelow,
    WallInDirection,
    LineOfSightToTarget,
    NoLineOfSightToTarget,
    PointOverlapsLayer
}

public enum HWJ_RuntimeStatField
{
    CurrentHp,
    MaxHp,
    MoveSpeed,
    AttackPower,
    Defense,
    AttackSpeed,
    BodyDecayValue,
    BodyDecayRatio
}

public class HWJ_GameplayContext
{
    public HWJ_RootObjectDataResolver SourceResolver { get; set; }
    public HWJ_RootObjectDataResolver TargetResolver { get; set; }
    public Component SourceComponent { get; set; }
    public Component TargetComponent { get; set; }
    public HWJ_DamageData DamageData { get; set; }
    public HWJ_SkillActionDataSO SkillAction { get; set; }
    public string ActionId { get; set; }
    public string HitboxId { get; set; }
    public Collider2D HitCollider { get; set; }
    public Vector2 WorldPoint { get; set; }
    public bool HasWorldPoint { get; set; }
    public LayerMask EnvironmentLayerMask { get; set; }
    public Vector2 CheckDirection { get; set; } = Vector2.down;
    public float CheckDistance { get; set; } = 1f;
    public bool HasHitConfirmed { get; set; }
    public float DamageMultiplier { get; set; } = 1f;
    public float DistanceOverride { get; set; } = -1f;

    public static HWJ_GameplayContext Create(
        HWJ_RootObjectDataResolver source,
        HWJ_RootObjectDataResolver target)
    {
        return new HWJ_GameplayContext
        {
            SourceResolver = source,
            TargetResolver = target
        };
    }

    public HWJ_GameplayContext WithSource(Component source)
    {
        SourceComponent = source;
        if (SourceResolver == null && source != null)
        {
            SourceResolver = source.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        return this;
    }

    public HWJ_GameplayContext WithTarget(Component target)
    {
        TargetComponent = target;
        if (TargetResolver == null && target != null)
        {
            TargetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        return this;
    }

    public HWJ_GameplayContext WithDamage(HWJ_DamageData damageData, float damageMultiplier = 1f)
    {
        DamageData = damageData;
        DamageMultiplier = damageMultiplier;
        return this;
    }

    public HWJ_GameplayContext WithSkill(HWJ_SkillActionDataSO skillAction)
    {
        SkillAction = skillAction;
        ActionId = skillAction != null ? skillAction.SkillActionId : ActionId;
        return this;
    }

    public HWJ_GameplayContext WithHitConfirmed(bool hasHitConfirmed)
    {
        HasHitConfirmed = hasHitConfirmed;
        return this;
    }

    public HWJ_GameplayContext WithActionId(string actionId)
    {
        ActionId = actionId;
        return this;
    }

    public HWJ_GameplayContext WithHitbox(string hitboxId, Collider2D hitCollider = null)
    {
        HitboxId = hitboxId;
        HitCollider = hitCollider;
        return this;
    }

    public HWJ_GameplayContext WithWorldPoint(Vector2 worldPoint)
    {
        WorldPoint = worldPoint;
        HasWorldPoint = true;
        return this;
    }

    public HWJ_GameplayContext WithEnvironment(
        LayerMask layerMask,
        Vector2 direction,
        float distance)
    {
        EnvironmentLayerMask = layerMask;
        CheckDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        CheckDistance = Mathf.Max(0f, distance);
        return this;
    }

    public HWJ_GameplayContext WithDistanceOverride(float distance)
    {
        DistanceOverride = distance;
        return this;
    }

    public HWJ_RootObjectDataResolver GetResolver(HWJ_GameplayActorSlot actor)
    {
        if (actor == HWJ_GameplayActorSlot.Source)
        {
            if (SourceResolver == null && SourceComponent != null)
            {
                SourceResolver = SourceComponent.GetComponentInParent<HWJ_RootObjectDataResolver>();
            }

            return SourceResolver;
        }

        if (TargetResolver == null && TargetComponent != null)
        {
            TargetResolver = TargetComponent.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        return TargetResolver;
    }

    public Transform GetTransform(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);

        if (resolver != null)
        {
            return resolver.transform;
        }

        Component component = actor == HWJ_GameplayActorSlot.Source ? SourceComponent : TargetComponent;
        return component != null ? component.transform : null;
    }

    public HWJ_RuntimeStatusSystem GetStatus(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_RuntimeStatusSystem>() : null;
    }

    public HWJ_SoulSystem GetSoul(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_SoulSystem>() : null;
    }

    public HWJ_SkillActionSystem GetSkillActionSystem(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_SkillActionSystem>() : null;
    }

    public HWJ_SkillUnlockSystem GetSkillUnlockSystem(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_SkillUnlockSystem>() : null;
    }

    public HWJ_PossessionSystem GetPossessionSystem(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_PossessionSystem>() : null;
    }

    public HWJ_BodyDecaySystem GetBodyDecay(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_BodyDecaySystem>() : null;
    }

    public HWJ_MonsterAISystem GetMonsterAI(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_MonsterAISystem>() : null;
    }

    public HWJ_BossBrainSystem GetBossBrain(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_BossBrainSystem>() : null;
    }

    public HWJ_LevelUpSystem GetLevel(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_LevelUpSystem>() : null;
    }

    public HWJ_PlayerMovementSystem GetMovement(HWJ_GameplayActorSlot actor)
    {
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);
        return resolver != null ? resolver.GetComponent<HWJ_PlayerMovementSystem>() : null;
    }

    public float GetDistance()
    {
        if (DistanceOverride >= 0f)
        {
            return DistanceOverride;
        }

        Transform source = GetTransform(HWJ_GameplayActorSlot.Source);
        Transform target = GetTransform(HWJ_GameplayActorSlot.Target);

        if (source == null || target == null)
        {
            return float.MaxValue;
        }

        return Vector2.Distance(source.position, target.position);
    }

    public bool TryGetPossessionData(HWJ_GameplayActorSlot actor, out HWJ_PossessionData possessionData)
    {
        possessionData = null;
        HWJ_RootObjectDataResolver resolver = GetResolver(actor);

        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            possessionData = playerData.Possession;
            return possessionData != null;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            possessionData = enemyData.PossessionBody;
            return possessionData != null;
        }

        if (resolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionData = bossData.PossessionBody;
            return possessionData != null;
        }

        return false;
    }
}

public static class HWJ_ConditionUtility
{
    public static bool Compare(float left, HWJ_FloatCompareMode compareMode, float right, float tolerance = 0.001f)
    {
        switch (compareMode)
        {
            case HWJ_FloatCompareMode.Less:
                return left < right;
            case HWJ_FloatCompareMode.LessOrEqual:
                return left <= right;
            case HWJ_FloatCompareMode.Equal:
                return Mathf.Abs(left - right) <= tolerance;
            case HWJ_FloatCompareMode.GreaterOrEqual:
                return left >= right;
            case HWJ_FloatCompareMode.Greater:
                return left > right;
            default:
                return false;
        }
    }
}
