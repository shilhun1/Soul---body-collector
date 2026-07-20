using System;
using UnityEngine;

public readonly struct HWJ_DamageEvent
{
    public readonly HWJ_RuntimeStatusSystem TargetStatus;
    public readonly Component Source;
    public readonly HWJ_DamageData SourceDamage;
    public readonly float Damage;
    public readonly float RemainingHp;
    public readonly bool TargetDied;

    public HWJ_DamageEvent(
        HWJ_RuntimeStatusSystem targetStatus,
        Component source,
        HWJ_DamageData sourceDamage,
        float damage,
        float remainingHp,
        bool targetDied)
    {
        TargetStatus = targetStatus;
        Source = source;
        SourceDamage = sourceDamage;
        Damage = damage;
        RemainingHp = remainingHp;
        TargetDied = targetDied;
    }
}

public readonly struct HWJ_RuntimeStateChangedEvent
{
    public readonly HWJ_RuntimeStatusSystem Status;
    public readonly HWJ_RuntimeState PreviousState;
    public readonly HWJ_RuntimeState CurrentState;

    public HWJ_RuntimeStateChangedEvent(
        HWJ_RuntimeStatusSystem status,
        HWJ_RuntimeState previousState,
        HWJ_RuntimeState currentState)
    {
        Status = status;
        PreviousState = previousState;
        CurrentState = currentState;
    }
}

public readonly struct HWJ_SoulStateChangedEvent
{
    public readonly HWJ_SoulSystem SoulSystem;
    public readonly HWJ_SoulRuntimeState PreviousState;
    public readonly HWJ_SoulRuntimeState CurrentState;

    public HWJ_SoulStateChangedEvent(
        HWJ_SoulSystem soulSystem,
        HWJ_SoulRuntimeState previousState,
        HWJ_SoulRuntimeState currentState)
    {
        SoulSystem = soulSystem;
        PreviousState = previousState;
        CurrentState = currentState;
    }
}

public readonly struct HWJ_PlayerExistenceStateChangedEvent
{
    public readonly HWJ_SoulSystem SoulSystem;
    public readonly HWJ_PlayerExistenceState PreviousState;
    public readonly HWJ_PlayerExistenceState CurrentState;

    public HWJ_PlayerExistenceStateChangedEvent(
        HWJ_SoulSystem soulSystem,
        HWJ_PlayerExistenceState previousState,
        HWJ_PlayerExistenceState currentState)
    {
        SoulSystem = soulSystem;
        PreviousState = previousState;
        CurrentState = currentState;
    }
}

public readonly struct HWJ_PossessionEvent
{
    public readonly HWJ_PossessionSystem PossessionSystem;
    public readonly HWJ_RootObjectDataResolver BodyResolver;
    public readonly bool Possessed;
    public readonly string Message;

    public HWJ_PossessionEvent(
        HWJ_PossessionSystem possessionSystem,
        HWJ_RootObjectDataResolver bodyResolver,
        bool possessed,
        string message)
    {
        PossessionSystem = possessionSystem;
        BodyResolver = bodyResolver;
        Possessed = possessed;
        Message = message;
    }
}

public readonly struct HWJ_PossessionTargetChangedEvent
{
    public readonly HWJ_BodyDiscoverySystem DiscoverySystem;
    public readonly HWJ_RootObjectDataResolver PreviousTarget;
    public readonly HWJ_RootObjectDataResolver CurrentTarget;
    public readonly HWJ_BodyDiscoveryResult DiscoveryResult;

    public HWJ_PossessionTargetChangedEvent(
        HWJ_BodyDiscoverySystem discoverySystem,
        HWJ_RootObjectDataResolver previousTarget,
        HWJ_RootObjectDataResolver currentTarget,
        HWJ_BodyDiscoveryResult discoveryResult)
    {
        DiscoverySystem = discoverySystem;
        PreviousTarget = previousTarget;
        CurrentTarget = currentTarget;
        DiscoveryResult = discoveryResult;
    }
}

public readonly struct HWJ_PossessedBodyRuntimeStateChangedEvent
{
    public readonly HWJ_PossessedBodySystem BodySystem;
    public readonly HWJ_PossessedBodyRuntimeState RuntimeState;
    public readonly string Message;

    public HWJ_PossessedBodyRuntimeStateChangedEvent(
        HWJ_PossessedBodySystem bodySystem,
        HWJ_PossessedBodyRuntimeState runtimeState,
        string message)
    {
        BodySystem = bodySystem;
        RuntimeState = runtimeState;
        Message = message;
    }
}

public readonly struct HWJ_BodyDecayChangedEvent
{
    public readonly HWJ_BodyDecaySystem BodyDecaySystem;
    public readonly float PreviousValue;
    public readonly float CurrentValue;
    public readonly float MaxValue;
    public readonly HWJ_DecayDangerLevel DangerLevel;

    public HWJ_BodyDecayChangedEvent(
        HWJ_BodyDecaySystem bodyDecaySystem,
        float previousValue,
        float currentValue,
        float maxValue,
        HWJ_DecayDangerLevel dangerLevel)
    {
        BodyDecaySystem = bodyDecaySystem;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        MaxValue = maxValue;
        DangerLevel = dangerLevel;
    }
}

public readonly struct HWJ_DecayDangerLevelChangedEvent
{
    public readonly HWJ_BodyDecaySystem BodyDecaySystem;
    public readonly HWJ_DecayDangerLevel PreviousLevel;
    public readonly HWJ_DecayDangerLevel CurrentLevel;

    public HWJ_DecayDangerLevelChangedEvent(
        HWJ_BodyDecaySystem bodyDecaySystem,
        HWJ_DecayDangerLevel previousLevel,
        HWJ_DecayDangerLevel currentLevel)
    {
        BodyDecaySystem = bodyDecaySystem;
        PreviousLevel = previousLevel;
        CurrentLevel = currentLevel;
    }
}

public readonly struct HWJ_BodyCollapseEvent
{
    public readonly HWJ_CollapseSystem CollapseSystem;
    public readonly HWJ_RootObjectDataResolver BodyResolver;
    public readonly HWJ_PossessedBodyRuntimeState RuntimeState;
    public readonly HWJ_BodyCollapseReason Reason;
    public readonly string Message;

    public HWJ_BodyCollapseEvent(
        HWJ_CollapseSystem collapseSystem,
        HWJ_RootObjectDataResolver bodyResolver,
        HWJ_PossessedBodyRuntimeState runtimeState,
        HWJ_BodyCollapseReason reason,
        string message)
    {
        CollapseSystem = collapseSystem;
        BodyResolver = bodyResolver;
        RuntimeState = runtimeState;
        Reason = reason;
        Message = message;
    }
}

public readonly struct HWJ_AbilityUsedEvent
{
    public readonly Component User;
    public readonly HWJ_RootObjectDataResolver UserResolver;
    public readonly string AbilityId;
    public readonly bool IsBasicAttack;
    public readonly float DecayValueAfterUse;
    public readonly int ComboStep;
    public readonly float ChargeSeconds;
    public readonly string Message;

    public HWJ_AbilityUsedEvent(
        Component user,
        HWJ_RootObjectDataResolver userResolver,
        string abilityId,
        bool isBasicAttack,
        float decayValueAfterUse,
        string message,
        int comboStep = 0,
        float chargeSeconds = 0f)
    {
        User = user;
        UserResolver = userResolver;
        AbilityId = abilityId;
        IsBasicAttack = isBasicAttack;
        DecayValueAfterUse = decayValueAfterUse;
        ComboStep = comboStep > 0 ? comboStep : 0;
        ChargeSeconds = chargeSeconds > 0f ? chargeSeconds : 0f;
        Message = message;
    }
}

public readonly struct HWJ_BasicAttackChargeEvent
{
    public readonly HWJ_PlayerAttackSystem AttackSystem;
    public readonly bool IsCharging;
    public readonly float ChargeSeconds;
    public readonly float MaxChargeSeconds;
    public readonly float ChargeRatio;
    public readonly string Message;

    public HWJ_BasicAttackChargeEvent(
        HWJ_PlayerAttackSystem attackSystem,
        bool isCharging,
        float chargeSeconds,
        float maxChargeSeconds,
        string message)
    {
        AttackSystem = attackSystem;
        IsCharging = isCharging;
        ChargeSeconds = Mathf.Max(0f, chargeSeconds);
        MaxChargeSeconds = Mathf.Max(0f, maxChargeSeconds);
        ChargeRatio = MaxChargeSeconds > 0f ? Mathf.Clamp01(ChargeSeconds / MaxChargeSeconds) : 0f;
        Message = message;
    }
}

public readonly struct HWJ_EnemyDefeatedEvent
{
    public readonly HWJ_RootObjectDataResolver DefeatedResolver;
    public readonly HWJ_RootObjectDataResolver DefeatedByResolver;
    public readonly Vector3 DefeatedPosition;
    public readonly bool IsBoss;

    public HWJ_EnemyDefeatedEvent(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver defeatedByResolver,
        Vector3 defeatedPosition,
        bool isBoss)
    {
        DefeatedResolver = defeatedResolver;
        DefeatedByResolver = defeatedByResolver;
        DefeatedPosition = defeatedPosition;
        IsBoss = isBoss;
    }
}

public readonly struct HWJ_RewardGrantedEvent
{
    public readonly HWJ_RewardGrantResult RewardResult;

    public HWJ_RewardGrantedEvent(HWJ_RewardGrantResult rewardResult)
    {
        RewardResult = rewardResult;
    }
}

public readonly struct HWJ_ExperienceChangedEvent
{
    public readonly HWJ_LevelUpSystem LevelSystem;
    public readonly int PreviousExperience;
    public readonly int CurrentExperience;
    public readonly int ExperienceAdded;

    public HWJ_ExperienceChangedEvent(
        HWJ_LevelUpSystem levelSystem,
        int previousExperience,
        int currentExperience,
        int experienceAdded)
    {
        LevelSystem = levelSystem;
        PreviousExperience = previousExperience;
        CurrentExperience = currentExperience;
        ExperienceAdded = experienceAdded;
    }
}

public readonly struct HWJ_PlayerLevelChangedEvent
{
    public readonly HWJ_LevelUpSystem LevelSystem;
    public readonly int PreviousLevel;
    public readonly int CurrentLevel;
    public readonly int CurrentSkillPoint;

    public HWJ_PlayerLevelChangedEvent(
        HWJ_LevelUpSystem levelSystem,
        int previousLevel,
        int currentLevel,
        int currentSkillPoint)
    {
        LevelSystem = levelSystem;
        PreviousLevel = previousLevel;
        CurrentLevel = currentLevel;
        CurrentSkillPoint = currentSkillPoint;
    }
}

public enum HWJ_SkillPointChangeReason
{
    None,
    LevelUpReward,
    DirectReward,
    SkillUnlockSpend,
    Restore
}

public readonly struct HWJ_SkillPointChangedEvent
{
    public readonly HWJ_LevelUpSystem LevelSystem;
    public readonly int PreviousSkillPoint;
    public readonly int CurrentSkillPoint;
    public readonly int DeltaSkillPoint;
    public readonly HWJ_SkillPointChangeReason Reason;

    public HWJ_SkillPointChangedEvent(
        HWJ_LevelUpSystem levelSystem,
        int previousSkillPoint,
        int currentSkillPoint,
        HWJ_SkillPointChangeReason reason)
    {
        LevelSystem = levelSystem;
        PreviousSkillPoint = previousSkillPoint;
        CurrentSkillPoint = currentSkillPoint;
        DeltaSkillPoint = currentSkillPoint - previousSkillPoint;
        Reason = reason;
    }
}

public readonly struct HWJ_SkillUnlockedEvent
{
    public readonly HWJ_SkillUnlockSystem UnlockSource;
    public readonly string SkillId;
    public readonly int CurrentLevel;
    public readonly int RemainingSkillPoint;
    public readonly bool SpentSkillPoint;
    public readonly string Message;

    public HWJ_SkillUnlockedEvent(
        HWJ_SkillUnlockSystem unlockSource,
        string skillId,
        int currentLevel,
        int remainingSkillPoint,
        bool spentSkillPoint,
        string message)
    {
        UnlockSource = unlockSource;
        SkillId = skillId;
        CurrentLevel = currentLevel;
        RemainingSkillPoint = remainingSkillPoint;
        SpentSkillPoint = spentSkillPoint;
        Message = message;
    }
}

public readonly struct HWJ_StatOrbStackChangedEvent
{
    public readonly HWJ_StatOrbProgressSystem ProgressSystem;
    public readonly HWJ_StatOrbDataSO StatOrbData;
    public readonly string StatOrbId;
    public readonly int PreviousStackCount;
    public readonly int CurrentStackCount;
    public readonly int MaxStackCount;
    public readonly bool RestoredFromSave;
    public readonly string Message;

    public HWJ_StatOrbStackChangedEvent(
        HWJ_StatOrbProgressSystem progressSystem,
        HWJ_StatOrbDataSO statOrbData,
        string statOrbId,
        int previousStackCount,
        int currentStackCount,
        int maxStackCount,
        bool restoredFromSave,
        string message)
    {
        ProgressSystem = progressSystem;
        StatOrbData = statOrbData;
        StatOrbId = statOrbId;
        PreviousStackCount = previousStackCount;
        CurrentStackCount = currentStackCount;
        MaxStackCount = maxStackCount;
        RestoredFromSave = restoredFromSave;
        Message = message;
    }
}

public readonly struct HWJ_StageFlowStateChangedEvent
{
    public readonly HWJ_StageProgressionSystem StageSystem;
    public readonly string StageId;
    public readonly HWJ_StageFlowState PreviousState;
    public readonly HWJ_StageFlowState CurrentState;
    public readonly string Message;

    public HWJ_StageFlowStateChangedEvent(
        HWJ_StageProgressionSystem stageSystem,
        string stageId,
        HWJ_StageFlowState previousState,
        HWJ_StageFlowState currentState,
        string message)
    {
        StageSystem = stageSystem;
        StageId = stageId;
        PreviousState = previousState;
        CurrentState = currentState;
        Message = message;
    }
}

public readonly struct HWJ_StageProgressionEvent
{
    public readonly HWJ_StageProgressionSystem StageSystem;
    public readonly string StageId;
    public readonly string NextRegionId;
    public readonly string BossId;
    public readonly string UnlockRegionId;
    public readonly HWJ_StageFlowState CurrentState;
    public readonly bool ObjectiveComplete;
    public readonly bool BossUnlocked;
    public readonly bool BossBattleStarted;
    public readonly bool BossDefeated;
    public readonly bool RegionUnlocked;
    public readonly string Message;

    public HWJ_StageProgressionEvent(
        HWJ_StageProgressionSystem stageSystem,
        string stageId,
        string nextRegionId,
        HWJ_StageFlowState currentState,
        bool objectiveComplete,
        bool bossUnlocked,
        bool bossBattleStarted,
        bool bossDefeated,
        bool regionUnlocked,
        string message)
        : this(
            stageSystem,
            stageId,
            nextRegionId,
            null,
            nextRegionId,
            currentState,
            objectiveComplete,
            bossUnlocked,
            bossBattleStarted,
            bossDefeated,
            regionUnlocked,
            message)
    {
    }

    public HWJ_StageProgressionEvent(
        HWJ_StageProgressionSystem stageSystem,
        string stageId,
        string nextRegionId,
        string bossId,
        string unlockRegionId,
        HWJ_StageFlowState currentState,
        bool objectiveComplete,
        bool bossUnlocked,
        bool bossBattleStarted,
        bool bossDefeated,
        bool regionUnlocked,
        string message)
    {
        StageSystem = stageSystem;
        StageId = stageId;
        NextRegionId = nextRegionId;
        BossId = bossId;
        UnlockRegionId = unlockRegionId;
        CurrentState = currentState;
        ObjectiveComplete = objectiveComplete;
        BossUnlocked = bossUnlocked;
        BossBattleStarted = bossBattleStarted;
        BossDefeated = bossDefeated;
        RegionUnlocked = regionUnlocked;
        Message = message;
    }
}

public readonly struct HWJ_BossPhaseChangedEvent
{
    public readonly HWJ_BossBrainSystem BossBrain;
    public readonly int PreviousPhase;
    public readonly int CurrentPhase;

    public HWJ_BossPhaseChangedEvent(
        HWJ_BossBrainSystem bossBrain,
        int previousPhase,
        int currentPhase)
    {
        BossBrain = bossBrain;
        PreviousPhase = previousPhase;
        CurrentPhase = currentPhase;
    }
}

public readonly struct HWJ_SaveCompletedEvent
{
    public readonly HWJ_SaveOperationResult OperationResult;

    public HWJ_SaveCompletedEvent(HWJ_SaveOperationResult operationResult)
    {
        OperationResult = operationResult;
    }
}

public readonly struct HWJ_LoadCompletedEvent
{
    public readonly HWJ_SaveOperationResult OperationResult;

    public HWJ_LoadCompletedEvent(HWJ_SaveOperationResult operationResult)
    {
        OperationResult = operationResult;
    }
}

public readonly struct HWJ_CoreLoopOperationEvent
{
    public readonly HWJ_CoreLoopCoordinator Coordinator;
    public readonly HWJ_CoreLoopOperationResult OperationResult;

    public HWJ_CoreLoopOperationEvent(
        HWJ_CoreLoopCoordinator coordinator,
        HWJ_CoreLoopOperationResult operationResult)
    {
        Coordinator = coordinator;
        OperationResult = operationResult;
    }
}

public readonly struct HWJ_EnemyAITransitionEvent
{
    public readonly HWJ_MonsterAISystem MonsterAI;
    public readonly HWJ_EnemyAITransitionResult TransitionResult;

    public HWJ_EnemyAITransitionEvent(
        HWJ_MonsterAISystem monsterAI,
        HWJ_EnemyAITransitionResult transitionResult)
    {
        MonsterAI = monsterAI;
        TransitionResult = transitionResult;
    }
}

public readonly struct HWJ_EnemyAIActionEvent
{
    public readonly HWJ_MonsterAISystem MonsterAI;
    public readonly HWJ_EnemyAIActionResult ActionResult;

    public HWJ_EnemyAIActionEvent(
        HWJ_MonsterAISystem monsterAI,
        HWJ_EnemyAIActionResult actionResult)
    {
        MonsterAI = monsterAI;
        ActionResult = actionResult;
    }
}

public static class HWJ_GameplayEvents
{
    public static event Action<HWJ_DamageEvent> DamageApplied;
    public static event Action<HWJ_DamageEvent> ActorDied;
    public static event Action<HWJ_RuntimeStateChangedEvent> RuntimeStateChanged;
    public static event Action<HWJ_SoulStateChangedEvent> SoulStateChanged;
    public static event Action<HWJ_PlayerExistenceStateChangedEvent> PlayerExistenceStateChanged;
    public static event Action<HWJ_PossessionEvent> PossessionChanged;
    public static event Action<HWJ_PossessionTargetChangedEvent> PossessionTargetChanged;
    public static event Action<HWJ_PossessedBodyRuntimeStateChangedEvent> PossessedBodyRuntimeStateChanged;
    public static event Action<HWJ_BodyDecayChangedEvent> BodyDecayChanged;
    public static event Action<HWJ_DecayDangerLevelChangedEvent> DecayDangerLevelChanged;
    public static event Action<HWJ_BodyCollapseEvent> BodyCollapseStarted;
    public static event Action<HWJ_BodyCollapseEvent> BodyCollapsed;
    public static event Action<HWJ_AbilityUsedEvent> AbilityUsed;
    public static event Action<HWJ_BasicAttackChargeEvent> BasicAttackChargeChanged;
    public static event Action<HWJ_EnemyDefeatedEvent> EnemyDefeated;
    public static event Action<HWJ_RewardGrantedEvent> RewardGranted;
    public static event Action<HWJ_ExperienceChangedEvent> ExperienceChanged;
    public static event Action<HWJ_PlayerLevelChangedEvent> PlayerLevelChanged;
    public static event Action<HWJ_SkillPointChangedEvent> SkillPointChanged;
    public static event Action<HWJ_SkillUnlockedEvent> SkillUnlocked;
    public static event Action<HWJ_StatOrbStackChangedEvent> StatOrbStackChanged;
    public static event Action<HWJ_StageFlowStateChangedEvent> StageFlowStateChanged;
    public static event Action<HWJ_StageProgressionEvent> StageObjectiveChanged;
    public static event Action<HWJ_StageProgressionEvent> BossUnlocked;
    public static event Action<HWJ_StageProgressionEvent> BossBattleStarted;
    public static event Action<HWJ_StageProgressionEvent> BossDefeated;
    public static event Action<HWJ_StageProgressionEvent> StageCleared;
    public static event Action<HWJ_StageProgressionEvent> RegionUnlocked;
    public static event Action<HWJ_BossPhaseChangedEvent> BossPhaseChanged;
    public static event Action<HWJ_SaveCompletedEvent> SaveCompleted;
    public static event Action<HWJ_LoadCompletedEvent> LoadCompleted;
    public static event Action<HWJ_CoreLoopOperationEvent> CoreLoopOperationCompleted;
    public static event Action<HWJ_EnemyAITransitionEvent> EnemyAIStateTransitioned;
    public static event Action<HWJ_EnemyAIActionEvent> EnemyAIActionResolved;

    public static void RaiseDamageApplied(HWJ_DamageEvent damageEvent)
    {
        DamageApplied?.Invoke(damageEvent);

        if (damageEvent.TargetDied)
        {
            ActorDied?.Invoke(damageEvent);
        }
    }

    public static void RaiseRuntimeStateChanged(HWJ_RuntimeStateChangedEvent stateEvent)
    {
        RuntimeStateChanged?.Invoke(stateEvent);
    }

    public static void RaiseSoulStateChanged(HWJ_SoulStateChangedEvent stateEvent)
    {
        SoulStateChanged?.Invoke(stateEvent);
    }

    public static void RaisePlayerExistenceStateChanged(HWJ_PlayerExistenceStateChangedEvent stateEvent)
    {
        PlayerExistenceStateChanged?.Invoke(stateEvent);
    }

    public static void RaisePossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        PossessionChanged?.Invoke(possessionEvent);
    }

    public static void RaisePossessionTargetChanged(HWJ_PossessionTargetChangedEvent targetChangedEvent)
    {
        PossessionTargetChanged?.Invoke(targetChangedEvent);
    }

    public static void RaisePossessedBodyRuntimeStateChanged(HWJ_PossessedBodyRuntimeStateChangedEvent stateEvent)
    {
        PossessedBodyRuntimeStateChanged?.Invoke(stateEvent);
    }

    public static void RaiseBodyDecayChanged(HWJ_BodyDecayChangedEvent decayEvent)
    {
        BodyDecayChanged?.Invoke(decayEvent);
    }

    public static void RaiseDecayDangerLevelChanged(HWJ_DecayDangerLevelChangedEvent dangerEvent)
    {
        DecayDangerLevelChanged?.Invoke(dangerEvent);
    }

    public static void RaiseBodyCollapseStarted(HWJ_BodyCollapseEvent collapseEvent)
    {
        BodyCollapseStarted?.Invoke(collapseEvent);
    }

    public static void RaiseBodyCollapsed(HWJ_BodyCollapseEvent collapseEvent)
    {
        BodyCollapsed?.Invoke(collapseEvent);
    }

    public static void RaiseAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
    {
        AbilityUsed?.Invoke(abilityEvent);
    }

    public static void RaiseBasicAttackChargeChanged(HWJ_BasicAttackChargeEvent chargeEvent)
    {
        BasicAttackChargeChanged?.Invoke(chargeEvent);
    }

    public static void RaiseEnemyDefeated(HWJ_EnemyDefeatedEvent defeatedEvent)
    {
        EnemyDefeated?.Invoke(defeatedEvent);
    }

    public static void RaiseRewardGranted(HWJ_RewardGrantedEvent rewardEvent)
    {
        RewardGranted?.Invoke(rewardEvent);
    }

    public static void RaiseExperienceChanged(HWJ_ExperienceChangedEvent experienceEvent)
    {
        ExperienceChanged?.Invoke(experienceEvent);
    }

    public static void RaisePlayerLevelChanged(HWJ_PlayerLevelChangedEvent levelEvent)
    {
        PlayerLevelChanged?.Invoke(levelEvent);
    }

    public static void RaiseSkillPointChanged(HWJ_SkillPointChangedEvent skillPointEvent)
    {
        SkillPointChanged?.Invoke(skillPointEvent);
    }

    public static void RaiseSkillUnlocked(HWJ_SkillUnlockedEvent skillEvent)
    {
        SkillUnlocked?.Invoke(skillEvent);
    }

    public static void RaiseStatOrbStackChanged(HWJ_StatOrbStackChangedEvent statOrbEvent)
    {
        StatOrbStackChanged?.Invoke(statOrbEvent);
    }

    public static void RaiseStageFlowStateChanged(HWJ_StageFlowStateChangedEvent stateEvent)
    {
        StageFlowStateChanged?.Invoke(stateEvent);
    }

    public static void RaiseStageObjectiveChanged(HWJ_StageProgressionEvent progressionEvent)
    {
        StageObjectiveChanged?.Invoke(progressionEvent);
    }

    public static void RaiseBossUnlocked(HWJ_StageProgressionEvent progressionEvent)
    {
        BossUnlocked?.Invoke(progressionEvent);
    }

    public static void RaiseBossBattleStarted(HWJ_StageProgressionEvent progressionEvent)
    {
        BossBattleStarted?.Invoke(progressionEvent);
    }

    public static void RaiseBossDefeated(HWJ_StageProgressionEvent progressionEvent)
    {
        BossDefeated?.Invoke(progressionEvent);
    }

    public static void RaiseStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        StageCleared?.Invoke(progressionEvent);
    }

    public static void RaiseRegionUnlocked(HWJ_StageProgressionEvent progressionEvent)
    {
        RegionUnlocked?.Invoke(progressionEvent);
    }

    public static void RaiseBossPhaseChanged(HWJ_BossPhaseChangedEvent phaseEvent)
    {
        BossPhaseChanged?.Invoke(phaseEvent);
    }

    public static void RaiseSaveCompleted(HWJ_SaveCompletedEvent saveEvent)
    {
        SaveCompleted?.Invoke(saveEvent);
    }

    public static void RaiseLoadCompleted(HWJ_LoadCompletedEvent loadEvent)
    {
        LoadCompleted?.Invoke(loadEvent);
    }

    public static void RaiseCoreLoopOperationCompleted(HWJ_CoreLoopOperationEvent coreLoopEvent)
    {
        CoreLoopOperationCompleted?.Invoke(coreLoopEvent);
    }

    public static void RaiseEnemyAIStateTransitioned(HWJ_EnemyAITransitionEvent transitionEvent)
    {
        EnemyAIStateTransitioned?.Invoke(transitionEvent);
    }

    public static void RaiseEnemyAIActionResolved(HWJ_EnemyAIActionEvent actionEvent)
    {
        EnemyAIActionResolved?.Invoke(actionEvent);
    }

    public static void ClearAllSubscribers()
    {
        DamageApplied = null;
        ActorDied = null;
        RuntimeStateChanged = null;
        SoulStateChanged = null;
        PlayerExistenceStateChanged = null;
        PossessionChanged = null;
        PossessionTargetChanged = null;
        PossessedBodyRuntimeStateChanged = null;
        BodyDecayChanged = null;
        DecayDangerLevelChanged = null;
        BodyCollapseStarted = null;
        BodyCollapsed = null;
        AbilityUsed = null;
        BasicAttackChargeChanged = null;
        EnemyDefeated = null;
        RewardGranted = null;
        ExperienceChanged = null;
        PlayerLevelChanged = null;
        SkillPointChanged = null;
        SkillUnlocked = null;
        StatOrbStackChanged = null;
        StageFlowStateChanged = null;
        StageObjectiveChanged = null;
        BossUnlocked = null;
        BossBattleStarted = null;
        BossDefeated = null;
        StageCleared = null;
        RegionUnlocked = null;
        BossPhaseChanged = null;
        SaveCompleted = null;
        LoadCompleted = null;
        CoreLoopOperationCompleted = null;
        EnemyAIStateTransitioned = null;
        EnemyAIActionResolved = null;
    }
}
