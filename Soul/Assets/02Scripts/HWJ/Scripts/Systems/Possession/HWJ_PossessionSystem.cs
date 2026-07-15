using UnityEngine;

/// <summary>
/// 플레이어가 적 또는 보스의 육신에 빙의할 수 있는지 판단하는 컴포넌트입니다.
/// 플레이어 오브젝트에 붙이고, 대상 오브젝트의 HWJ_RootObjectDataResolver를 받아 빙의 가능 데이터를 확인합니다.
/// </summary>
public class HWJ_PossessionSystem : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [SerializeField] private HWJ_RootObjectDataResolver possessedBodyResolver;
    [SerializeField] private HWJ_RootObjectDataResolver runtimePossessedBodyResolver;

    [Space(8f)]
    [Header("Possession Runtime")]
    [SerializeField] private bool moveOwnerToPossessedBody = true;
    [SerializeField] private bool copyPossessedBodyVisual = true;
    [SerializeField] private bool consumePossessedCorpse = true;
    [SerializeField] private bool deactivateConsumedCorpse = true;
    [SerializeField] private bool allowManualSoulExit = true;

    [Space(8f)]
    [Header("Rules")]
    [SerializeField] private bool useGameplayPossessionRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO possessionExecutionCore;
    [SerializeField] private string possessionExecutionCoreId = "possession_execution";
    [SerializeField] private HWJ_GameplayRuleSO possessionRule;
    [SerializeField] private string possessionRuleId = "possession_can_start";

    [Space(8f)]
    [Header("Debug")]
    [SerializeField] private HWJ_PossessionFailureCode lastPossessionFailureCode;
    [SerializeField] private string lastPossessionResult;

    private HWJ_PossessionData activePossessionBodyData;
    private SpriteRenderer ownerSpriteRenderer;
    private Sprite ownerOriginalSprite;
    private Color ownerOriginalColor;
    private bool ownerOriginalFlipX;
    private bool ownerOriginalFlipY;
    private bool hasOwnerSpriteCache;
    private Animator ownerAnimator;
    private RuntimeAnimatorController ownerOriginalAnimatorController;
    private bool hasOwnerAnimatorCache;
    private HWJ_CharacterMotionSystem ownerMotionSystem;

    public bool HasActivePossessedBody => possessedBodyResolver != null
        && (soulSystem == null || soulSystem.CurrentExistenceState == HWJ_PlayerExistenceState.Possessed);

    public HWJ_RootObjectDataResolver PossessedBodyResolver => possessedBodyResolver;
    public HWJ_PossessedBodySystem PossessedBodySystem => possessedBodySystem;
    public bool CanLoadPossessedBodyStats => CanLoadBodyStats();
    public HWJ_WeaponType CurrentWeaponType => HasActivePossessedBody
        ? possessedBodyResolver.WeaponType
        : HWJ_WeaponType.None;
    public HWJ_PossessionFailureCode LastPossessionFailureCode => lastPossessionFailureCode;
    public string LastPossessionResult => lastPossessionResult;

    private void Awake()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        CacheOwnerVisual();
    }

    private void Update()
    {
        ResolvePlayerInput();

        if (playerInput != null && playerInput.ExitPossessionPressedThisFrame)
        {
            TryExitPossessedBodyToSoul();
        }
    }

    /// <summary>
    /// 현재 플레이어 상태와 대상의 PossessionData를 기준으로 빙의 가능 여부를 반환합니다.
    /// 대상은 EnemyTypeDataSO 또는 BossTypeDataSO를 가진 RootObjectData여야 합니다.
    /// </summary>
    public bool CanPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        return EvaluatePossession(targetDataResolver).Succeeded;
    }

    public HWJ_PossessionResult EvaluatePossession(HWJ_RootObjectDataResolver targetDataResolver)
    {
        CacheRuntimeReferences();

        if (targetDataResolver == null)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.InvalidTarget,
                "Possession failed: missing target."));
        }

        if (soulSystem != null && soulSystem.IsTransitioningExistence)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TransitionInProgress,
                "Possession failed: player existence state is transitioning.",
                targetDataResolver));
        }

        if (soulSystem != null && soulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Spirit)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.NotInSpiritState,
                "Possession failed: player is not in Spirit existence state.",
                targetDataResolver));
        }

        if (possessedBodyResolver != null || HasActivePossessedBody)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.AlreadyPossessingBody,
                "Possession failed: player already has an occupied body.",
                targetDataResolver));
        }

        if (!targetDataResolver.gameObject.activeInHierarchy)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target body is inactive.",
                targetDataResolver));
        }

        if (IsConsumedPossessionBody(targetDataResolver))
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetAlreadyPossessed,
                "Possession failed: target body was already consumed.",
                targetDataResolver));
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null
            && !playerData.Possession.canPossess)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Possession failed: player possession is disabled.",
                targetDataResolver));
        }

        if (!TryGetPossessionBodyData(targetDataResolver, out HWJ_PossessionData possessionBody))
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.InvalidTarget,
                "Possession failed: target has no possessable body data.",
                targetDataResolver));
        }

        if (!possessionBody.canBePossessed)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target cannot be possessed.",
                targetDataResolver));
        }

        if (IsEnemyOrBoss(targetDataResolver) && !IsDefeatedTarget(targetDataResolver))
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target monster is still alive.",
                targetDataResolver));
        }

        if (!IsDefeatedIfRequired(targetDataResolver, possessionBody))
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "Possession failed: target is not defeated.",
                targetDataResolver));
        }

        if (!IsPossessionRuleSatisfied(targetDataResolver))
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                ResolveRuleFailureCode(lastPossessionResult),
                lastPossessionResult,
                targetDataResolver));
        }

        return StorePossessionResult(HWJ_PossessionResult.Success(
            targetDataResolver,
            "Possession target is valid."));
    }

    private bool IsPossessionRuleSatisfied(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!useGameplayPossessionRule)
        {
            return true;
        }

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(ownerDataResolver, targetDataResolver)
            .WithSource(this)
            .WithTarget(targetDataResolver);

        if (ResolvePossessionExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool corePassed = executionCore.TryExecute(context, out HWJ_RuleExecutionResult executionResult);
            lastPossessionResult = executionResult.Message;
            return corePassed;
        }

        HWJ_GameplayRuleSO rule = possessionRule;

        if (rule == null
            && !string.IsNullOrEmpty(possessionRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(possessionRuleId, out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool passed = rule.TryEvaluate(context, out HWJ_RuleEvaluationResult result);
        lastPossessionResult = result.Message;
        return passed;
    }

    private bool ResolvePossessionExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (possessionExecutionCore != null)
        {
            executionCore = possessionExecutionCore;
            return true;
        }

        if (string.IsNullOrEmpty(possessionExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(possessionExecutionCoreId, out executionCore);
    }

    /// <summary>
    /// 빙의 가능하면 대상 시체를 현재 육신으로 등록하고 플레이어를 육신 상태로 전환합니다.
    /// 등록된 육신의 무기, 스탯, 스킬은 RuntimeStatus/Combat/SkillActionSystem에서 읽어 사용합니다.
    /// </summary>
    public bool TryPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        if (IsEnemyOrBoss(targetDataResolver) && !IsDefeatedTarget(targetDataResolver))
        {
            lastPossessionResult = "Possession failed: target monster is still alive.";
            return false;
        }

        TryGetPossessionBodyData(targetDataResolver, out activePossessionBodyData);
        possessedBodyResolver = targetDataResolver;
        MarkPossessionBodyConsumed(targetDataResolver);
        CreateRuntimeBodyState(targetDataResolver, true, true);

        if (activePossessionBodyData == null || activePossessionBodyData.transfersControlToBody)
        {
            TransferOwnerToPossessedBody(targetDataResolver);
        }

        soulSystem?.EnterBodyState();

        if (activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer
            && runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(true);
        }

        lastPossessionResult = $"Possessed {targetDataResolver.name}.";
        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(this, targetDataResolver, true, lastPossessionResult));
        SavePlayerRuntimeSnapshotIfOwner();
        return true;
    }

    public bool RestorePossessedBody(
        HWJ_RootObjectDataSO rootObjectData,
        Sprite visualSprite,
        Color visualColor,
        bool visualFlipX,
        bool visualFlipY,
        RuntimeAnimatorController animatorController,
        bool hasVisualSnapshot,
        bool refreshStatus = true,
        bool refillToMax = false,
        bool saveSnapshot = true)
    {
        CacheOwnerVisual();

        if (rootObjectData == null)
        {
            lastPossessionResult = "Restore possession failed: missing root object data.";
            return false;
        }

        HWJ_RootObjectDataResolver restoredResolver = GetOrCreateRuntimePossessedBodyResolver();

        if (restoredResolver == null)
        {
            lastPossessionResult = "Restore possession failed: missing runtime resolver.";
            return false;
        }

        restoredResolver.SetRootObjectData(rootObjectData);

        if (!TryGetPossessionBodyData(restoredResolver, out activePossessionBodyData))
        {
            possessedBodyResolver = null;
            activePossessionBodyData = null;
            lastPossessionResult = $"Restore possession failed: {rootObjectData.name} has no body data.";
            return false;
        }

        possessedBodyResolver = restoredResolver;
        CreateRuntimeBodyState(restoredResolver, refillToMax, true);
        soulSystem?.EnterBodyState();

        if (hasVisualSnapshot)
        {
            ApplyPossessedBodyVisualSnapshot(
                visualSprite,
                visualColor,
                visualFlipX,
                visualFlipY,
                animatorController);
        }
        else
        {
            ApplyPossessedBodyModelData(rootObjectData);
        }

        if (refreshStatus
            && activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer
            && runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(refillToMax);
        }

        lastPossessionResult = $"Restored possessed body from {rootObjectData.name}.";
        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(this, possessedBodyResolver, true, lastPossessionResult));
        if (saveSnapshot)
        {
            SavePlayerRuntimeSnapshotIfOwner();
        }

        return true;
    }

    public bool TryExitPossessedBodyToSoul()
    {
        if (!CanExitPossessedBodyToSoul())
        {
            return false;
        }

        lastPossessionResult = "Exited possessed body to Soul state.";
        soulSystem.EnterSoulState(false);
        return true;
    }

    /// <summary>
    /// 현재 빙의 중인 육신을 해제합니다.
    /// 다시 유령 상태로 돌아가거나 육신이 소멸될 때 호출합니다.
    /// </summary>
    public void ClearPossessedBody(bool refreshStatus = true, bool refillToMax = false, bool saveSnapshot = true)
    {
        HWJ_RootObjectDataResolver previousBodyResolver = possessedBodyResolver;
        bool hadActiveBody = HasActivePossessedBody;
        possessedBodyResolver = null;
        activePossessionBodyData = null;
        possessedBodySystem?.ClearCurrentBodyState(false);
        RestoreOwnerVisual();

        if (refreshStatus)
        {
            runtimeStatus?.RefreshCurrentHpFromData(refillToMax);
        }

        if (saveSnapshot)
        {
            SavePlayerRuntimeSnapshotIfOwner();
        }

        if (hadActiveBody)
        {
            HWJ_GameplayEvents.RaisePossessionChanged(
                new HWJ_PossessionEvent(this, previousBodyResolver, false, "Possession body cleared."));
        }
    }

    private void CacheRuntimeReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput = GetComponent<HWJ_PlayerInputSystem>();

        if (playerInput == null)
        {
            playerInput = HWJ_GameAccess.PlayerInput;
        }
    }

    private HWJ_PossessionResult StorePossessionResult(HWJ_PossessionResult result)
    {
        lastPossessionFailureCode = result.FailureCode;
        lastPossessionResult = result.Message;
        return result;
    }

    private static HWJ_PossessionFailureCode ResolveRuleFailureCode(string ruleMessage)
    {
        if (string.IsNullOrEmpty(ruleMessage))
        {
            return HWJ_PossessionFailureCode.PossessionBlocked;
        }

        if (ruleMessage.Contains("within_possession_range"))
        {
            return HWJ_PossessionFailureCode.TargetOutOfRange;
        }

        if (ruleMessage.Contains("target_corpse_available"))
        {
            return HWJ_PossessionFailureCode.TargetAlreadyPossessed;
        }

        if (ruleMessage.Contains("source_soul_state"))
        {
            return HWJ_PossessionFailureCode.NotInSpiritState;
        }

        if (ruleMessage.Contains("source_can_possess"))
        {
            return HWJ_PossessionFailureCode.PossessionBlocked;
        }

        if (ruleMessage.Contains("target_can_be_possessed")
            || ruleMessage.Contains("target_defeated")
            || ruleMessage.Contains("target_object"))
        {
            return HWJ_PossessionFailureCode.TargetUnavailable;
        }

        return HWJ_PossessionFailureCode.PossessionBlocked;
    }

    public bool TryGetPossessedRootObjectId(out string rootObjectId)
    {
        rootObjectId = null;

        if (!HasActivePossessedBody
            || possessedBodyResolver == null
            || possessedBodyResolver.RootObjectData == null
            || possessedBodyResolver.RootObjectData.Identity == null
            || string.IsNullOrEmpty(possessedBodyResolver.RootObjectData.Identity.objectId))
        {
            return false;
        }

        rootObjectId = possessedBodyResolver.RootObjectData.Identity.objectId;
        return true;
    }

    public bool TryGetPossessedBodyRuntimeState(out HWJ_PossessedBodyRuntimeState state)
    {
        CacheRuntimeReferences();

        state = null;
        return possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out state)
            && state != null;
    }

    public bool TryGetPossessedVisualSnapshot(
        out Sprite sprite,
        out Color color,
        out bool flipX,
        out bool flipY,
        out RuntimeAnimatorController animatorController)
    {
        CacheOwnerVisual();

        sprite = null;
        color = Color.white;
        flipX = false;
        flipY = false;
        animatorController = null;

        if (!HasActivePossessedBody)
        {
            return false;
        }

        bool hasVisual = false;

        if (ownerSpriteRenderer != null)
        {
            sprite = ownerSpriteRenderer.sprite;
            color = ownerSpriteRenderer.color;
            flipX = ownerSpriteRenderer.flipX;
            flipY = ownerSpriteRenderer.flipY;
            hasVisual = sprite != null;
        }

        if (ownerAnimator != null)
        {
            animatorController = ownerAnimator.runtimeAnimatorController;
            hasVisual = hasVisual || animatorController != null;
        }

        return hasVisual;
    }

    /// <summary>
    /// 빙의한 육신의 공통 스탯을 가져옵니다.
    /// 플레이어 자체 SO를 바꾸지 않고 런타임 계산에서만 육신 스탯을 사용하기 위한 통로입니다.
    /// </summary>
    public bool TryGetPossessedStatus(out HWJ_StatusData status)
    {
        status = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.Status == null)
        {
            return false;
        }

        status = possessedBodyResolver.Status;
        return true;
    }

    /// <summary>
    /// 빙의한 육신의 공격 데이터를 가져옵니다.
    /// 무기별 기본 피해나 속성은 시체의 RootObjectData를 기준으로 계산합니다.
    /// </summary>
    public bool TryGetPossessedDamage(out HWJ_DamageData damage)
    {
        damage = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.Damage == null)
        {
            return false;
        }

        damage = possessedBodyResolver.Damage;
        return true;
    }

    /// <summary>
    /// 빙의한 육신의 피격 데이터를 가져옵니다.
    /// 방어 보정, 무적 여부, 피해 배율도 시체 데이터 기준으로 바꿀 수 있습니다.
    /// </summary>
    public bool TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData receivedDamage)
    {
        receivedDamage = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.ReceivedDamage == null)
        {
            return false;
        }

        receivedDamage = possessedBodyResolver.ReceivedDamage;
        return true;
    }

    /// <summary>
    /// 빙의한 적/보스가 가진 스킬 세트를 가져옵니다.
    /// 플레이어가 시체의 무기 스킬을 사용할 때 이 데이터를 우선합니다.
    /// </summary>
    public bool TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet)
    {
        skillSet = null;

        if (!HasActivePossessedBody)
        {
            return false;
        }

        if (possessedBodyResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            if (HasAnySkillEntry(enemyData.PlayerPossessionSkillSet))
            {
                skillSet = enemyData.PlayerPossessionSkillSet;
                return true;
            }

            skillSet = enemyData.SkillCycle;
            return HasAnySkillEntry(skillSet);
        }

        if (possessedBodyResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            skillSet = bossData.SkillCycle;
            return skillSet != null;
        }

        return false;
    }

    private static bool HasAnySkillEntry(HWJ_SkillSetData skillSet)
    {
        return skillSet != null && skillSet.skills != null && skillSet.skills.Length > 0;
    }

    /// <summary>
    /// 현재 빙의한 무기 타입으로 사용할 수 있는 첫 번째 스킬 ID를 가져옵니다.
    /// 기본 공격 입력이 들어왔을 때 시체 무기 스킬을 우선 실행하기 위해 사용합니다.
    /// </summary>
    public bool TryGetPrimaryPossessedSkillId(out string skillId)
    {
        return TryGetPossessedSkillIdAt(0, out skillId);
    }

    /// <summary>
    /// 현재 빙의한 몬스터의 스킬 목록에서 슬롯 번호에 맞는 스킬 ID를 가져옵니다.
    /// PlayerAttackSystem은 숫자키 1/2/3 입력을 이 메서드와 연결해 빙의 스킬을 실행합니다.
    /// </summary>
    public bool TryGetPossessedSkillIdAt(int slotIndex, out string skillId)
    {
        skillId = null;

        if (slotIndex < 0 || !TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet) || skillSet.skills == null)
        {
            return false;
        }

        HWJ_WeaponType weaponType = CurrentWeaponType;
        int matchedSlotIndex = 0;

        for (int i = 0; i < skillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData skill = skillSet.skills[i];

            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                continue;
            }

            bool weaponMatched = skill.requiredWeaponType == HWJ_WeaponType.None
                || skill.requiredWeaponType == weaponType;

            if (!weaponMatched)
            {
                continue;
            }

            if (!skill.startsUnlocked)
            {
                continue;
            }

            if (matchedSlotIndex == slotIndex)
            {
                skillId = skill.skillId;
                return true;
            }

            matchedSlotIndex++;
        }

        return false;
    }

    /// <summary>
    /// 대상 오브젝트가 적 또는 보스일 때 빙의당하는 몸 데이터를 꺼냅니다.
    /// 적 역할 데이터에서 빙의 불가 몸으로 설정되어 있으면 false를 반환합니다.
    /// </summary>
    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver targetDataResolver,
        out HWJ_PossessionData possessionBody)
    {
        possessionBody = null;

        if (targetDataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            if (enemyData.Role != null
                && (!enemyData.Role.leavesCorpseOnDeath || !enemyData.Role.isPossessableBody))
            {
                return false;
            }

            possessionBody = enemyData.PossessionBody;
            return possessionBody != null;
        }

        if (targetDataResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionBody = bossData.PossessionBody;
            return possessionBody != null;
        }

        return false;
    }

    private bool CanLoadBodyStats()
    {
        return HasActivePossessedBody
            && activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer;
    }

    private void CreateRuntimeBodyState(
        HWJ_RootObjectDataResolver targetDataResolver,
        bool refillHpToMax,
        bool resetDecayToInitial)
    {
        CacheRuntimeReferences();

        if (possessedBodySystem == null)
        {
            return;
        }

        possessedBodySystem.CreateCurrentBodyState(
            targetDataResolver,
            ResolveOwnerBodyDecayData(),
            refillHpToMax,
            resetDecayToInitial);
    }

    private HWJ_BodyDecayData ResolveOwnerBodyDecayData()
    {
        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return playerData.BodyDecay;
        }

        return null;
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerPossession == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }

    private bool IsConsumedPossessionBody(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState = targetDataResolver.GetComponent<HWJ_PossessionBodyState>();
        return bodyState != null && bodyState.IsConsumed;
    }

    private void MarkPossessionBodyConsumed(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        HWJ_PossessionBodyState bodyState = targetDataResolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState == null)
        {
            bodyState = targetDataResolver.gameObject.AddComponent<HWJ_PossessionBodyState>();
        }

        bodyState.MarkConsumed();
    }

    private bool CanExitPossessedBodyToSoul()
    {
        if (!allowManualSoulExit)
        {
            lastPossessionResult = "Exit possession failed: manual soul exit is disabled.";
            return false;
        }

        if (!HasActivePossessedBody)
        {
            lastPossessionResult = "Exit possession failed: no active possessed body.";
            return false;
        }

        if (soulSystem == null || soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            lastPossessionResult = "Exit possession failed: player is not in Body state.";
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastPossessionResult = "Exit possession failed: player is dead.";
            return false;
        }

        return true;
    }

    private bool IsEnemyOrBoss(HWJ_RootObjectDataResolver targetDataResolver)
    {
        return targetDataResolver != null
            && (targetDataResolver.ObjectType == HWJ_ObjectType.Enemy
                || targetDataResolver.ObjectType == HWJ_ObjectType.Boss);
    }

    private bool IsDefeatedTarget(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null
            && (targetStatus.CurrentState == HWJ_RuntimeState.Dead
                || (targetStatus.UsesHp && targetStatus.CurrentHp <= 0f));
    }

    private bool IsDefeatedIfRequired(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (possessionBody == null || !possessionBody.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private void TransferOwnerToPossessedBody(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        if (moveOwnerToPossessedBody)
        {
            Vector3 targetPosition = targetDataResolver.transform.position;
            targetPosition.z = transform.position.z;
            transform.SetPositionAndRotation(targetPosition, targetDataResolver.transform.rotation);

            if (TryGetComponent(out Rigidbody2D ownerBody))
            {
                ownerBody.linearVelocity = Vector2.zero;
                ownerBody.angularVelocity = 0f;
            }
        }

        if (copyPossessedBodyVisual)
        {
            ApplyPossessedBodyVisual(targetDataResolver.gameObject);
        }

        if (consumePossessedCorpse)
        {
            DisablePossessedCorpseObject(targetDataResolver.gameObject);
        }
    }

    private void CacheOwnerVisual()
    {
        if (ownerSpriteRenderer == null)
        {
            ownerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (ownerSpriteRenderer != null && !hasOwnerSpriteCache)
        {
            ownerOriginalSprite = ownerSpriteRenderer.sprite;
            ownerOriginalColor = ownerSpriteRenderer.color;
            ownerOriginalFlipX = ownerSpriteRenderer.flipX;
            ownerOriginalFlipY = ownerSpriteRenderer.flipY;
            hasOwnerSpriteCache = true;
        }

        if (ownerAnimator == null)
        {
            ownerAnimator = GetComponentInChildren<Animator>();
        }

        if (ownerAnimator != null && !hasOwnerAnimatorCache)
        {
            ownerOriginalAnimatorController = ownerAnimator.runtimeAnimatorController;
            hasOwnerAnimatorCache = true;
        }

        if (ownerMotionSystem == null)
        {
            ownerMotionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }
    }

    private HWJ_RootObjectDataResolver GetOrCreateRuntimePossessedBodyResolver()
    {
        if (runtimePossessedBodyResolver != null)
        {
            return runtimePossessedBodyResolver;
        }

        Transform runtimeBodyTransform = transform.Find("HWJ_RuntimePossessedBodyData");

        if (runtimeBodyTransform != null)
        {
            runtimePossessedBodyResolver = runtimeBodyTransform.GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimePossessedBodyResolver == null)
        {
            GameObject runtimeBody = new GameObject("HWJ_RuntimePossessedBodyData");
            runtimeBody.transform.SetParent(transform, false);
            runtimePossessedBodyResolver = runtimeBody.AddComponent<HWJ_RootObjectDataResolver>();
            runtimeBody.SetActive(false);
        }

        return runtimePossessedBodyResolver;
    }

    private void ApplyPossessedBodyVisual(GameObject possessedBody)
    {
        CacheOwnerVisual();

        if (possessedBody == null)
        {
            return;
        }

        SpriteRenderer possessedRenderer = possessedBody.GetComponentInChildren<SpriteRenderer>();

        if (ownerSpriteRenderer != null && possessedRenderer != null)
        {
            ownerSpriteRenderer.sprite = possessedRenderer.sprite;
            ownerSpriteRenderer.color = possessedRenderer.color;
            ownerSpriteRenderer.flipX = possessedRenderer.flipX;
            ownerSpriteRenderer.flipY = possessedRenderer.flipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        Animator possessedAnimator = possessedBody.GetComponentInChildren<Animator>();

        if (ownerAnimator != null && possessedAnimator != null)
        {
            ownerAnimator.runtimeAnimatorController = possessedAnimator.runtimeAnimatorController;
        }
    }

    private void ApplyPossessedBodyVisualSnapshot(
        Sprite sprite,
        Color color,
        bool flipX,
        bool flipY,
        RuntimeAnimatorController animatorController)
    {
        CacheOwnerVisual();

        if (ownerSpriteRenderer != null && sprite != null)
        {
            ownerSpriteRenderer.sprite = sprite;
            ownerSpriteRenderer.color = color;
            ownerSpriteRenderer.flipX = flipX;
            ownerSpriteRenderer.flipY = flipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        if (ownerAnimator != null && animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController = animatorController;
        }
    }

    private void ApplyPossessedBodyModelData(HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null || rootObjectData.Model == null)
        {
            return;
        }

        if (rootObjectData.Model.modelPrefab != null)
        {
            ApplyPossessedBodyVisual(rootObjectData.Model.modelPrefab);
        }

        if (ownerAnimator != null && rootObjectData.Model.animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController = rootObjectData.Model.animatorController;
        }
    }

    private void RestoreOwnerVisual()
    {
        if (ownerSpriteRenderer != null && hasOwnerSpriteCache)
        {
            ownerSpriteRenderer.sprite = ownerOriginalSprite;
            ownerSpriteRenderer.color = ownerOriginalColor;
            ownerSpriteRenderer.flipX = ownerOriginalFlipX;
            ownerSpriteRenderer.flipY = ownerOriginalFlipY;
            ownerMotionSystem?.RefreshFacingBaseline();
        }

        if (ownerAnimator != null && hasOwnerAnimatorCache)
        {
            ownerAnimator.runtimeAnimatorController = ownerOriginalAnimatorController;
        }
    }

    private void DisablePossessedCorpseObject(GameObject possessedBody)
    {
        if (possessedBody == null)
        {
            return;
        }

        HWJ_EnemyNavigationSystem navigation = possessedBody.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.enabled = false;
        }

        HWJ_MonsterAISystem monsterAI = possessedBody.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.enabled = false;
        }

        Collider2D[] colliders = possessedBody.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        SpriteRenderer[] renderers = possessedBody.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        Animator animator = possessedBody.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        Rigidbody2D body = possessedBody.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        if (deactivateConsumedCorpse)
        {
            possessedBody.SetActive(false);
        }
    }
}
