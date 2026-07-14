using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_BossFlowSystem : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;

    [Header("Boss")]
    [SerializeField] private HWJ_BossBrainSystem bossBrainSystem;
    [SerializeField] private HWJ_RootObjectDataResolver bossResolver;
    [SerializeField] private bool bossEntryTriggerActive = true;

    [Header("Player")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_SoulSystem playerSoulSystem;
    [SerializeField] private HWJ_PossessionSystem playerPossessionSystem;
    [SerializeField] private HWJ_CollapseSystem playerCollapseSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem playerStatusSystem;
    [SerializeField] private HWJ_BodyDecaySystem playerBodyDecaySystem;
    [SerializeField] private HWJ_SkillUnlockSystem playerSkillUnlockSystem;

    [Header("Runtime Result")]
    [SerializeField] private HWJ_BossFlowOperationType lastOperationType;
    [SerializeField] private HWJ_BossFlowFailureCode lastFailureCode;
    [SerializeField] private string lastFlowMessage;

    private HWJ_BossFlowResult lastFlowResult;

    public bool BossEntryTriggerActive => bossEntryTriggerActive;
    public HWJ_BossFlowResult LastFlowResult => lastFlowResult;
    public HWJ_BossFlowOperationType LastOperationType => lastOperationType;
    public HWJ_BossFlowFailureCode LastFailureCode => lastFailureCode;
    public string LastFlowMessage => lastFlowMessage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public void SetBossEntryTriggerActive(bool active)
    {
        bossEntryTriggerActive = active;
    }

    public HWJ_BossFlowResult EvaluateBossBattleStart()
    {
        ResolveReferences();
        return StoreResult(EvaluateBossBattleStartInternal(HWJ_BossFlowOperationType.EvaluateBossBattleStart));
    }

    // This is the scene trigger entry point. Validation runs first, then stage and boss state are changed.
    public HWJ_BossFlowResult TryStartBossBattle(string reason = null)
    {
        ResolveReferences();

        HWJ_BossFlowResult validationResult = EvaluateBossBattleStartInternal(
            HWJ_BossFlowOperationType.StartBossBattle);

        if (!validationResult.Succeeded)
        {
            return StoreResult(validationResult);
        }

        HWJ_StageFlowTransitionResult stageResult = stageProgressionSystem.TryStartBossBattle(
            reason ?? "Boss battle started by boss flow.");

        if (!stageResult.Succeeded)
        {
            return StoreResult(HWJ_BossFlowResult.Fail(
                HWJ_BossFlowOperationType.StartBossBattle,
                HWJ_BossFlowFailureCode.StageTransitionFailed,
                stageResult.Message,
                stageResult,
                validationResult.RuleExecutionResult));
        }

        if (bossBrainSystem != null && !bossBrainSystem.EncounterStarted)
        {
            bossBrainSystem.StartBossEncounter();
        }

        return StoreResult(HWJ_BossFlowResult.Success(
            HWJ_BossFlowOperationType.StartBossBattle,
            reason ?? "Boss battle started by boss flow.",
            stageResult,
            validationResult.RuleExecutionResult));
    }

    public HWJ_BossFlowResult TryMarkBossDefeated(
        string reason = null,
        bool unlockNextRegion = false,
        string nextRegionId = null)
    {
        ResolveReferences();

        if (stageProgressionSystem == null)
        {
            return StoreResult(HWJ_BossFlowResult.Fail(
                HWJ_BossFlowOperationType.MarkBossDefeated,
                HWJ_BossFlowFailureCode.MissingStageProgressionSystem,
                "Boss flow failed: missing HWJ_StageProgressionSystem."));
        }

        if (stageProgressionSystem.BossDefeated)
        {
            return StoreResult(HWJ_BossFlowResult.Fail(
                HWJ_BossFlowOperationType.MarkBossDefeated,
                HWJ_BossFlowFailureCode.BossAlreadyDefeated,
                "Boss flow failed: boss is already defeated."));
        }

        HWJ_StageFlowTransitionResult stageResult = stageProgressionSystem.TryMarkBossDefeated(
            reason ?? "Boss defeated by boss flow.");

        if (!stageResult.Succeeded)
        {
            return StoreResult(HWJ_BossFlowResult.Fail(
                HWJ_BossFlowOperationType.MarkBossDefeated,
                HWJ_BossFlowFailureCode.StageTransitionFailed,
                stageResult.Message,
                stageResult));
        }

        if (bossBrainSystem != null && bossBrainSystem.EncounterStarted)
        {
            bossBrainSystem.StopBossEncounter();
        }

        if (!unlockNextRegion)
        {
            return StoreResult(HWJ_BossFlowResult.Success(
                HWJ_BossFlowOperationType.MarkBossDefeated,
                reason ?? "Boss defeated by boss flow.",
                stageResult));
        }

        HWJ_StageFlowTransitionResult regionResult = stageProgressionSystem.TryUnlockRegion(
            nextRegionId,
            "Region unlocked after boss defeat.");

        if (!regionResult.Succeeded)
        {
            return StoreResult(HWJ_BossFlowResult.Fail(
                HWJ_BossFlowOperationType.UnlockRegionAfterBoss,
                HWJ_BossFlowFailureCode.StageTransitionFailed,
                regionResult.Message,
                regionResult));
        }

        return StoreResult(HWJ_BossFlowResult.Success(
            HWJ_BossFlowOperationType.UnlockRegionAfterBoss,
            "Boss defeated and next region unlocked.",
            regionResult));
    }

    // Evaluation must not mutate stage or boss state because UI and trigger scripts can call it for previews.
    private HWJ_BossFlowResult EvaluateBossBattleStartInternal(HWJ_BossFlowOperationType operationType)
    {
        if (stageProgressionSystem == null)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingStageProgressionSystem,
                "Boss flow failed: missing HWJ_StageProgressionSystem.");
        }

        if (bossBrainSystem == null)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingBossBrainSystem,
                "Boss flow failed: missing HWJ_BossBrainSystem.");
        }

        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingBossData,
                "Boss flow failed: boss has no HWJ_BossTypeDataSO.");
        }

        HWJ_BossEntryRequirementData requirements = bossData.EntryRequirements;

        if (requirements == null)
        {
            requirements = new HWJ_BossEntryRequirementData();
        }

        if (stageProgressionSystem.BossBattleStarted
            || stageProgressionSystem.CurrentState == HWJ_StageFlowState.BossBattle)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BossBattleAlreadyStarted,
                "Boss flow failed: boss battle is already started.");
        }

        if (requirements.requireObjectiveComplete && !stageProgressionSystem.ObjectiveComplete)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.ObjectiveNotComplete,
                "Boss flow failed: stage objective is not complete.");
        }

        if (requirements.requireBossUnlocked && !stageProgressionSystem.BossUnlocked)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BossNotReady,
                "Boss flow failed: boss is not unlocked.");
        }

        if (requirements.requireBossUnlocked
            && stageProgressionSystem.CurrentState != HWJ_StageFlowState.BossReady)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BossNotReady,
                "Boss flow failed: stage flow state is not BossReady.");
        }

        if (requirements.requireBossNotDefeated && stageProgressionSystem.BossDefeated)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BossAlreadyDefeated,
                "Boss flow failed: boss is already defeated.");
        }

        if (requirements.requireEntryTriggerActive && !bossEntryTriggerActive)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BossEntryTriggerInactive,
                "Boss flow failed: boss entry trigger is inactive.");
        }

        if (requirements.requirePossessedBody)
        {
            HWJ_BossFlowResult bodyResult = EvaluatePossessedBodyRequirements(operationType, requirements);

            if (!bodyResult.Succeeded)
            {
                return bodyResult;
            }
        }

        if (!EvaluateAdditionalRule(requirements, out HWJ_RuleExecutionResult ruleResult))
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.AdditionalRuleFailed,
                ruleResult.Message,
                default(HWJ_StageFlowTransitionResult),
                ruleResult);
        }

        HWJ_StageFlowTransitionResult stagePreview = HWJ_StageFlowTransitionResult.Success(
            stageProgressionSystem.CurrentState,
            HWJ_StageFlowState.BossBattle,
            "Boss battle start request is valid.");

        return HWJ_BossFlowResult.Success(
            operationType,
            "Boss battle start request is valid.",
            stagePreview,
            ruleResult);
    }

    private HWJ_BossFlowResult EvaluatePossessedBodyRequirements(
        HWJ_BossFlowOperationType operationType,
        HWJ_BossEntryRequirementData requirements)
    {
        if (playerSoulSystem == null)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingPlayerSoulSystem,
                "Boss flow failed: missing player soul system.");
        }

        if (playerPossessionSystem == null)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingPlayerPossessionSystem,
                "Boss flow failed: missing player possession system.");
        }

        if (playerSoulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Possessed
            || !playerPossessionSystem.HasActivePossessedBody)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.PlayerNotPossessed,
                "Boss flow failed: player is not possessing a valid body.");
        }

        if (requirements.requireBodyNotCollapsing
            && playerCollapseSystem != null
            && playerCollapseSystem.IsCollapsing)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.PlayerBodyCollapsing,
                "Boss flow failed: possessed body is collapsing.");
        }

        if (!EvaluateBodyObjectType(operationType, requirements, out HWJ_BossFlowResult bodyTypeResult))
        {
            return bodyTypeResult;
        }

        if (!EvaluateBodyIdFilter(operationType, requirements, out HWJ_BossFlowResult bodyIdResult))
        {
            return bodyIdResult;
        }

        if (requirements.requiredWeaponType != HWJ_WeaponType.None
            && playerPossessionSystem.CurrentWeaponType != requirements.requiredWeaponType)
        {
            return HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.RequiredWeaponMismatch,
                $"Boss flow failed: possessed weapon must be {requirements.requiredWeaponType}.");
        }

        if (requirements.minimumCurrentHp > 0f)
        {
            if (playerStatusSystem == null)
            {
                return HWJ_BossFlowResult.Fail(
                    operationType,
                    HWJ_BossFlowFailureCode.MissingPlayerStatusSystem,
                    "Boss flow failed: missing player runtime status system.");
            }

            if (playerStatusSystem.CurrentHp < requirements.minimumCurrentHp)
            {
                return HWJ_BossFlowResult.Fail(
                    operationType,
                    HWJ_BossFlowFailureCode.PossessedBodyHpTooLow,
                    $"Boss flow failed: possessed body HP must be at least {requirements.minimumCurrentHp}.");
            }
        }

        if (requirements.maximumCurrentDecayRatio < 1f)
        {
            if (playerBodyDecaySystem == null)
            {
                return HWJ_BossFlowResult.Fail(
                    operationType,
                    HWJ_BossFlowFailureCode.MissingBodyDecaySystem,
                    "Boss flow failed: missing player body decay system.");
            }

            if (playerBodyDecaySystem.CurrentDecayRatio > requirements.maximumCurrentDecayRatio)
            {
                return HWJ_BossFlowResult.Fail(
                    operationType,
                    HWJ_BossFlowFailureCode.PossessedBodyDecayTooHigh,
                    $"Boss flow failed: possessed body decay ratio must be {requirements.maximumCurrentDecayRatio} or lower.");
            }
        }

        if (!EvaluateRequiredSkills(operationType, requirements, out HWJ_BossFlowResult skillResult))
        {
            return skillResult;
        }

        return HWJ_BossFlowResult.Success(
            operationType,
            "Possessed body requirements passed.");
    }

    private bool EvaluateBodyObjectType(
        HWJ_BossFlowOperationType operationType,
        HWJ_BossEntryRequirementData requirements,
        out HWJ_BossFlowResult result)
    {
        result = default(HWJ_BossFlowResult);

        if (!requirements.requireBodyObjectType)
        {
            return true;
        }

        HWJ_RootObjectDataResolver bodyResolver = playerPossessionSystem.PossessedBodyResolver;

        if (bodyResolver != null && bodyResolver.ObjectType == requirements.requiredBodyObjectType)
        {
            return true;
        }

        result = HWJ_BossFlowResult.Fail(
            operationType,
            HWJ_BossFlowFailureCode.RequiredBodyTypeMismatch,
            $"Boss flow failed: possessed body type must be {requirements.requiredBodyObjectType}.");
        return false;
    }

    private bool EvaluateBodyIdFilter(
        HWJ_BossFlowOperationType operationType,
        HWJ_BossEntryRequirementData requirements,
        out HWJ_BossFlowResult result)
    {
        result = default(HWJ_BossFlowResult);
        HWJ_RootObjectDataResolver bodyResolver = playerPossessionSystem.PossessedBodyResolver;
        string bodyObjectId = GetObjectId(bodyResolver);

        if (HasConfiguredIds(requirements.allowedBodyObjectIds)
            && !ContainsId(requirements.allowedBodyObjectIds, bodyObjectId))
        {
            result = HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BodyIdNotAllowed,
                $"Boss flow failed: possessed body id {bodyObjectId} is not allowed.");
            return false;
        }

        if (ContainsId(requirements.blockedBodyObjectIds, bodyObjectId))
        {
            result = HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.BodyIdBlocked,
                $"Boss flow failed: possessed body id {bodyObjectId} is blocked.");
            return false;
        }

        return true;
    }

    private bool EvaluateRequiredSkills(
        HWJ_BossFlowOperationType operationType,
        HWJ_BossEntryRequirementData requirements,
        out HWJ_BossFlowResult result)
    {
        result = default(HWJ_BossFlowResult);

        if (!HasConfiguredIds(requirements.requiredUnlockedSkillIds))
        {
            return true;
        }

        if (playerSkillUnlockSystem == null)
        {
            result = HWJ_BossFlowResult.Fail(
                operationType,
                HWJ_BossFlowFailureCode.MissingSkillUnlockSystem,
                "Boss flow failed: missing player skill unlock system.");
            return false;
        }

        for (int i = 0; i < requirements.requiredUnlockedSkillIds.Length; i++)
        {
            string skillId = requirements.requiredUnlockedSkillIds[i];

            if (string.IsNullOrEmpty(skillId))
            {
                continue;
            }

            if (!playerSkillUnlockSystem.IsSkillUnlocked(skillId))
            {
                result = HWJ_BossFlowResult.Fail(
                    operationType,
                    HWJ_BossFlowFailureCode.RequiredSkillLocked,
                    $"Boss flow failed: required skill {skillId} is locked.");
                return false;
            }
        }

        return true;
    }

    private bool EvaluateAdditionalRule(
        HWJ_BossEntryRequirementData requirements,
        out HWJ_RuleExecutionResult ruleResult)
    {
        ruleResult = default(HWJ_RuleExecutionResult);

        if (!ResolveAdditionalRuleExecutionCore(requirements, out HWJ_RuleExecutionCoreSO executionCore))
        {
            return true;
        }

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(playerResolver, bossResolver)
            .WithSource(playerResolver)
            .WithTarget(bossBrainSystem);

        return executionCore.TryExecute(context, out ruleResult);
    }

    private bool ResolveAdditionalRuleExecutionCore(
        HWJ_BossEntryRequirementData requirements,
        out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (requirements == null)
        {
            return false;
        }

        if (requirements.additionalRuleExecutionCore != null)
        {
            executionCore = requirements.additionalRuleExecutionCore;
            return true;
        }

        return !string.IsNullOrEmpty(requirements.additionalRuleExecutionCoreId)
            && HWJ_GameAccess.TryGetRuleExecutionCore(requirements.additionalRuleExecutionCoreId, out executionCore);
    }

    private bool TryGetBossData(out HWJ_BossTypeDataSO bossData)
    {
        bossData = null;

        if (bossResolver == null && bossBrainSystem != null)
        {
            bossResolver = bossBrainSystem.GetComponent<HWJ_RootObjectDataResolver>();
        }

        return bossResolver != null && bossResolver.TryGetTypeData(out bossData);
    }

    private HWJ_BossFlowResult StoreResult(HWJ_BossFlowResult result)
    {
        lastFlowResult = result;
        lastOperationType = result.OperationType;
        lastFailureCode = result.FailureCode;
        lastFlowMessage = result.Message;
        return result;
    }

    // Explicit scene references win. GameManager and scene search are only fallbacks for simple test scenes.
    private void ResolveReferences()
    {
        if (bossBrainSystem == null)
        {
            bossBrainSystem = GetComponent<HWJ_BossBrainSystem>();
        }

        if (bossResolver == null)
        {
            bossResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponentInParent<HWJ_StageProgressionSystem>();
        }

        if (stageProgressionSystem == null)
        {
            HWJ_StageProgressionSystem[] stages = FindObjectsByType<HWJ_StageProgressionSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (stages.Length > 0)
            {
                stageProgressionSystem = stages[0];
            }
        }

        ResolvePlayerReferences();
    }

    private void ResolvePlayerReferences()
    {
        if (playerResolver == null && HWJ_GameAccess.HasManager)
        {
            playerResolver = HWJ_GameAccess.Manager.PlayerResolver;
        }

        if (playerSoulSystem == null && HWJ_GameAccess.HasManager)
        {
            playerSoulSystem = HWJ_GameAccess.Manager.PlayerSoul;
        }

        if (playerPossessionSystem == null && HWJ_GameAccess.HasManager)
        {
            playerPossessionSystem = HWJ_GameAccess.Manager.PlayerPossession;
        }

        if (playerStatusSystem == null && HWJ_GameAccess.HasManager)
        {
            playerStatusSystem = HWJ_GameAccess.Manager.PlayerStatus;
        }

        if (playerBodyDecaySystem == null && HWJ_GameAccess.HasManager)
        {
            playerBodyDecaySystem = HWJ_GameAccess.Manager.PlayerBodyDecay;
        }

        if (playerResolver == null && playerSoulSystem != null)
        {
            playerResolver = playerSoulSystem.GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (playerResolver == null)
        {
            playerResolver = FindPlayerResolver();
        }

        if (playerResolver == null)
        {
            return;
        }

        if (playerSoulSystem == null)
        {
            playerSoulSystem = playerResolver.GetComponent<HWJ_SoulSystem>();
        }

        if (playerPossessionSystem == null)
        {
            playerPossessionSystem = playerResolver.GetComponent<HWJ_PossessionSystem>();
        }

        if (playerCollapseSystem == null)
        {
            playerCollapseSystem = playerResolver.GetComponent<HWJ_CollapseSystem>();
        }

        if (playerStatusSystem == null)
        {
            playerStatusSystem = playerResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerBodyDecaySystem == null)
        {
            playerBodyDecaySystem = playerResolver.GetComponent<HWJ_BodyDecaySystem>();
        }

        if (playerSkillUnlockSystem == null)
        {
            playerSkillUnlockSystem = playerResolver.GetComponent<HWJ_SkillUnlockSystem>();
        }
    }

    private static HWJ_RootObjectDataResolver FindPlayerResolver()
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i];
            }
        }

        return null;
    }

    private static bool HasConfiguredIds(string[] ids)
    {
        if (ids == null)
        {
            return false;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsId(string[] ids, string id)
    {
        if (ids == null || string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == id)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetObjectId(HWJ_RootObjectDataResolver resolver)
    {
        return resolver != null && resolver.Identity != null
            ? resolver.Identity.objectId
            : null;
    }
}
