using System;

[Serializable]
public struct HWJ_RuntimeStatSnapshot
{
    public HWJ_RuntimeState runtimeState;
    public float currentHp;
    public float maxHp;
    public float soulHp;
    public float soulMaxHp;
    public float possessedBodyHp;
    public float moveSpeed;
    public float attackPower;
    public float defense;
    public float attackSpeed;
    public bool usesHp;
    public bool isDead;
    public bool canMove;
    public bool canAttack;
    public bool canDash;
    public bool isHitStunned;
    public bool isInvincible;
    public bool isHitReactionLimited;
    public bool hasSuperArmor;
    public bool shouldIgnoreKnockback;

    public static HWJ_RuntimeStatSnapshot FromStatus(HWJ_RuntimeStatusSystem status)
    {
        if (status == null)
        {
            return default;
        }

        return new HWJ_RuntimeStatSnapshot
        {
            runtimeState = status.CurrentState,
            currentHp = status.CurrentHp,
            maxHp = status.MaxHp,
            soulHp = status.SoulHp,
            soulMaxHp = status.SoulMaxHp,
            possessedBodyHp = status.PossessedBodyHp,
            moveSpeed = status.MoveSpeed,
            attackPower = status.AttackPower,
            defense = status.Defense,
            attackSpeed = status.AttackSpeed,
            usesHp = status.UsesHp,
            isDead = status.IsDead,
            canMove = status.CanMove,
            canAttack = status.CanAttack,
            canDash = status.CanDash,
            isHitStunned = status.IsHitStunned,
            isInvincible = status.IsTemporarilyInvincible,
            isHitReactionLimited = status.IsHitReactionLimited,
            hasSuperArmor = status.HasSuperArmor,
            shouldIgnoreKnockback = status.ShouldIgnoreKnockback
        };
    }
}

[Serializable]
public struct HWJ_RuntimeBodySnapshot
{
    public HWJ_SoulRuntimeState soulState;
    public HWJ_PlayerExistenceState playerExistenceState;
    public float soulDeadlineTimer;
    public float bodyToSoulTransitionTimer;
    public bool hasActivePossessedBody;
    public bool hasPossessedBodyRuntimeState;
    public string possessedBodyRuntimeInstanceId;
    public string possessedBodyDefinitionDataId;
    public float possessedBodyCurrentHp;
    public float possessedBodyMaxHp;
    public bool possessedBodyCollapsed;
    public HWJ_WeaponType possessedWeaponType;
    public float currentDecayValue;
    public float maxDecayValue;
    public float currentDecayRatio;
    public float remainingDecayValue;
    public float remainingDecayRatio;
    public HWJ_DecayDangerLevel decayDangerLevel;
    public bool isDecaying;

    public static HWJ_RuntimeBodySnapshot FromSystems(
        HWJ_SoulSystem soul,
        HWJ_PossessionSystem possession,
        HWJ_BodyDecaySystem bodyDecay,
        HWJ_PossessedBodySystem possessedBodySystem)
    {
        HWJ_PossessedBodyRuntimeState bodyState = null;
        bool hasBodyState = possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out bodyState)
            && bodyState != null;

        return new HWJ_RuntimeBodySnapshot
        {
            soulState = soul != null ? soul.CurrentState : HWJ_SoulRuntimeState.Body,
            playerExistenceState = soul != null ? soul.CurrentExistenceState : HWJ_PlayerExistenceState.None,
            soulDeadlineTimer = soul != null ? soul.SoulDeadlineTimer : 0f,
            bodyToSoulTransitionTimer = soul != null ? soul.BodyToSoulTransitionTimer : 0f,
            hasActivePossessedBody = possession != null && possession.HasActivePossessedBody,
            hasPossessedBodyRuntimeState = hasBodyState,
            possessedBodyRuntimeInstanceId = hasBodyState ? bodyState.RuntimeInstanceId : null,
            possessedBodyDefinitionDataId = hasBodyState ? bodyState.DefinitionDataId : null,
            possessedBodyCurrentHp = hasBodyState ? bodyState.CurrentHp : 0f,
            possessedBodyMaxHp = hasBodyState ? bodyState.MaxHp : 0f,
            possessedBodyCollapsed = hasBodyState && bodyState.IsCollapsed,
            possessedWeaponType = possession != null ? possession.CurrentWeaponType : HWJ_WeaponType.None,
            currentDecayValue = bodyDecay != null ? bodyDecay.CurrentDecayValue : 0f,
            maxDecayValue = bodyDecay != null ? bodyDecay.MaxDecayValue : 0f,
            currentDecayRatio = bodyDecay != null ? bodyDecay.CurrentDecayRatio : 0f,
            remainingDecayValue = bodyDecay != null ? bodyDecay.RemainingDecayValue : 0f,
            remainingDecayRatio = bodyDecay != null ? bodyDecay.RemainingDecayRatio : 0f,
            decayDangerLevel = bodyDecay != null ? bodyDecay.CurrentDangerLevel : HWJ_DecayDangerLevel.Stable,
            isDecaying = bodyDecay != null && bodyDecay.IsDecaying
        };
    }
}

[Serializable]
public struct HWJ_RuntimeStageFlowSnapshot
{
    public string stageId;
    public string nextRegionId;
    public HWJ_StageFlowState currentState;
    public bool objectiveComplete;
    public bool bossUnlocked;
    public bool bossBattleStarted;
    public bool bossDefeated;
    public bool regionUnlocked;
    public bool transitionLocked;

    public static HWJ_RuntimeStageFlowSnapshot FromStage(HWJ_StageProgressionSystem stage)
    {
        if (stage == null)
        {
            return default;
        }

        return new HWJ_RuntimeStageFlowSnapshot
        {
            stageId = stage.StageId,
            nextRegionId = stage.NextRegionId,
            currentState = stage.CurrentState,
            objectiveComplete = stage.ObjectiveComplete,
            bossUnlocked = stage.BossUnlocked,
            bossBattleStarted = stage.BossBattleStarted,
            bossDefeated = stage.BossDefeated,
            regionUnlocked = stage.RegionUnlocked,
            transitionLocked = stage.TransitionLocked
        };
    }
}

[Serializable]
public struct HWJ_RuntimeGrowthSnapshot
{
    public int currentLevel;
    public int currentExperience;
    public int skillPoint;
    public string[] unlockedSkillIds;
    public string[] unlockedSkillNodeIds;

    /// <summary>
    /// 현재 성장 런타임 값을 저장 DTO로 옮기기 쉬운 값 타입으로 복사합니다.
    /// </summary>
    public static HWJ_RuntimeGrowthSnapshot FromSystems(
        HWJ_LevelUpSystem level,
        HWJ_SkillUnlockSystem unlockStateSource)
    {
        return new HWJ_RuntimeGrowthSnapshot
        {
            currentLevel = level != null ? level.CurrentLevel : 1,
            currentExperience = level != null ? level.CurrentExperience : 0,
            skillPoint = level != null ? level.SkillPoint : 0,
            unlockedSkillIds = unlockStateSource != null ? unlockStateSource.GetUnlockedSkillIds() : new string[0],
            unlockedSkillNodeIds = unlockStateSource != null ? unlockStateSource.GetUnlockedSkillNodeIds() : new string[0]
        };
    }
}

[Serializable]
public struct HWJ_RuntimeObjectSnapshot
{
    public HWJ_RootObjectDataSO rootObjectData;
    public HWJ_ObjectType objectType;
    public HWJ_Faction faction;
    public HWJ_WeaponType weaponType;
    public HWJ_StatusData sourceStatusData;
    public HWJ_DamageData sourceDamageData;
    public HWJ_ReceivedDamageData sourceReceivedDamageData;
    public HWJ_RuntimeStatSnapshot runtimeStats;
    public HWJ_RuntimeBodySnapshot runtimeBody;
    public HWJ_RuntimeGrowthSnapshot runtimeGrowth;
}
