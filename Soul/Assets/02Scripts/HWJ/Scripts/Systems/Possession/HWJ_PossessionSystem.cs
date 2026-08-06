using UnityEngine;

public enum HWJ_PossessedBodyExitReason
{
    Unknown,
    ManualExit,
    MentalDepleted,
    DecayDepleted,
    HpDepleted,
    SaveRestore,
    ForcedClear
}

/// <summary>
/// 플레이어가 적의 육신에 빙의할 수 있는지 판단하는 컴포넌트입니다.
/// 플레이어 오브젝트에 붙이고, 대상 오브젝트의
/// HWJ_RootObjectDataResolver를 받아 빙의 가능 데이터를 확인합니다.
/// </summary>
public class HWJ_PossessionSystem : MonoBehaviour
{
    [Header("핵심 참조")]
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [SerializeField] private HWJ_SkillUnlockSystem skillUnlockSystem;
    [SerializeField] private HWJ_RootObjectDataResolver possessedBodyResolver;
    [SerializeField] private HWJ_RootObjectDataResolver runtimePossessedBodyResolver;

    [Space(8f)]
    [Header("빙의 런타임")]
    [SerializeField] private bool moveOwnerToPossessedBody = true;
    [SerializeField] private bool copyPossessedBodyVisual = true;
    [SerializeField] private bool consumePossessedCorpse = true;
    [SerializeField] private bool deactivateConsumedCorpse = true;
    [SerializeField] private bool allowManualSoulExit = true;

    [Space(8f)]
    [Header("생체 빙의 정신력")]
    [Tooltip("살아 있는 몬스터에게 빙의할 때 성공 시 차감하는 정신력입니다.")]
    [SerializeField, Min(0f)] private float livePossessionMentalCost = 10f;

    [Space(8f)]
    [Header("빙의 규칙")]
    [SerializeField] private bool useGameplayPossessionRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO possessionExecutionCore;
    [SerializeField]
    private string possessionExecutionCoreId =
        "possession_execution";
    [SerializeField] private HWJ_GameplayRuleSO possessionRule;
    [SerializeField]
    private string possessionRuleId =
        "possession_can_start";

    [Space(8f)]
    [Header("디버그")]
    [SerializeField] private HWJ_PossessionFailureCode lastPossessionFailureCode;
    [SerializeField] private string lastPossessionResult;
    [SerializeField] private float lastLivePossessionRoll;
    [SerializeField] private float lastLivePossessionSuccessChance;

    private HWJ_PossessionData activePossessionBodyData;
    private bool activePossessionWasLive;
    private HWJ_LivePossessionMentalState activeLiveMentalState;

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

    public bool HasActivePossessedBody =>
        possessedBodyResolver != null
        && (soulSystem == null
            || soulSystem.CurrentExistenceState
            == HWJ_PlayerExistenceState.Possessed);

    public HWJ_RootObjectDataResolver PossessedBodyResolver =>
        possessedBodyResolver;

    public HWJ_PossessedBodySystem PossessedBodySystem =>
        possessedBodySystem;

    public bool CanLoadPossessedBodyStats =>
        CanLoadBodyStats();

    public HWJ_WeaponType CurrentWeaponType =>
        HasActivePossessedBody
            ? possessedBodyResolver.WeaponType
            : HWJ_WeaponType.None;

    public HWJ_PossessionFailureCode LastPossessionFailureCode =>
        lastPossessionFailureCode;

    public string LastPossessionResult =>
        lastPossessionResult;

    public float LastLivePossessionRoll =>
        lastLivePossessionRoll;

    public float LastLivePossessionSuccessChance =>
        lastLivePossessionSuccessChance;

    public bool IsLivePossessionActive =>
        HasActivePossessedBody
        && activePossessionWasLive
        && activeLiveMentalState != null;

    public bool TryGetActiveLiveMentalState(
        out HWJ_LivePossessionMentalState mentalState)
    {
        mentalState = IsLivePossessionActive
            ? activeLiveMentalState
            : null;

        return mentalState != null;
    }

    public bool TryGetActivePossessedBodyDecayData(
        out HWJ_BodyDecayData bodyDecay)
    {
        bodyDecay = null;

        if (IsLivePossessionActive
            || possessedBodyResolver == null
            || activePossessionBodyData == null
            || !activePossessionBodyData.overrideBodyDecayOnPossession
            || activePossessionBodyData.possessedBodyDecayOverride == null)
        {
            return false;
        }

        bodyDecay =
            activePossessionBodyData.possessedBodyDecayOverride;

        return true;
    }

    private void Awake()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver =
                GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus =
                GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem =
                GetComponent<HWJ_PossessedBodySystem>();
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem =
                GetComponent<HWJ_SkillUnlockSystem>();
        }

        CacheOwnerVisual();
    }

    private void Update()
    {
        ResolvePlayerInput();

        /*
         * 빙의 상태에서 사용되는 RuntimeStatus의 HP가 0이 되면
         * 현재 빙의가 생체 빙의인지 시체 빙의인지와 관계없이
         * HP 고갈 사유로 영혼 상태 전환을 요청합니다.
         *
         * 생체 빙의였다면 ClearPossessedBody에서 원래 몬스터를
         * 시체 상태로 복구합니다.
         */
        if (HasActivePossessedBody
            && runtimeStatus != null
            && runtimeStatus.UsesHp
            && runtimeStatus.CurrentHp <= 0f)
        {
            soulSystem?.EnterSoulState(
                false,
                HWJ_PossessedBodyExitReason.HpDepleted);

            return;
        }

        if (playerInput != null
            && playerInput.ExitPossessionPressedThisFrame)
        {
            TryExitPossessedBodyToSoul();
        }
    }

    /// <summary>
    /// 현재 플레이어 상태와 대상의 데이터를 기준으로
    /// 빙의 가능 여부를 반환합니다.
    /// </summary>
    public bool CanPossess(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        return EvaluatePossession(targetDataResolver).Succeeded;
    }

    public HWJ_PossessionResult EvaluatePossession(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        CacheRuntimeReferences();

        if (targetDataResolver == null)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.InvalidTarget,
                    "Possession failed: missing target."));
        }

        if (targetDataResolver.ObjectType
            == HWJ_ObjectType.Boss)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.BossPossessionBlocked,
                    "Possession failed: bosses cannot be possessed.",
                    targetDataResolver));
        }

        if (soulSystem != null
            && soulSystem.IsTransitioningExistence)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TransitionInProgress,
                    "Possession failed: player existence state is transitioning.",
                    targetDataResolver));
        }

        if (soulSystem != null
            && soulSystem.CurrentExistenceState
            != HWJ_PlayerExistenceState.Spirit)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.NotInSpiritState,
                    "Possession failed: player is not in Spirit existence state.",
                    targetDataResolver));
        }

        if (possessedBodyResolver != null
            || HasActivePossessedBody)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.AlreadyPossessingBody,
                    "Possession failed: player already has an occupied body.",
                    targetDataResolver));
        }

        if (!targetDataResolver.gameObject.activeInHierarchy)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetUnavailable,
                    "Possession failed: target body is inactive.",
                    targetDataResolver));
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(
                out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null
            && !playerData.Possession.canPossess)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.PossessionBlocked,
                    "Possession failed: player possession is disabled.",
                    targetDataResolver));
        }

        if (!TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody))
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.InvalidTarget,
                    "Possession failed: target has no possessable body data.",
                    targetDataResolver));
        }

        if (!possessionBody.canBePossessed)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetUnavailable,
                    "Possession failed: target cannot be possessed.",
                    targetDataResolver));
        }

        if (!IsDefeatedIfRequired(
                targetDataResolver,
                possessionBody))
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetUnavailable,
                    "Possession failed: target is not defeated.",
                    targetDataResolver));
        }

        bool isLiveTarget = IsLivePossessionTarget(
            targetDataResolver,
            possessionBody);

        /*
         * 생체 빙의 중 HP가 0이 되어 시체가 된 몬스터는
         * 생체 빙의 때 이미 Consumed 표시가 남아 있을 수 있습니다.
         *
         * CanPossessAsCorpse가 true인 경우에는 시체 빙의를
         * 한 번 허용합니다.
         */
        bool canUseConsumedCorpse =
            !isLiveTarget
            && targetDataResolver.TryGetComponent(
                out HWJ_LivePossessionMentalState corpseMentalState)
            && corpseMentalState.CanPossessAsCorpse();

        /*
         * 살아 있는 몬스터는 수동 빙의 해제 후 다시 빙의할 수 있으므로
         * IsConsumed만으로 차단하지 않습니다.
         *
         * 정신력이 10 이하인지 또는 영구 재빙의 불가인지 여부는
         * CanAttemptLivePossession에서 따로 검사합니다.
         */
        if (IsConsumedPossessionBody(targetDataResolver)
            && !isLiveTarget
            && !canUseConsumedCorpse)
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetAlreadyPossessed,
                    "Possession failed: target body was already consumed.",
                    targetDataResolver));
        }

        if (isLiveTarget
            && !CanAttemptLivePossession(
                targetDataResolver,
                out string livePossessionMessage))
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    HWJ_PossessionFailureCode.TargetUnavailable,
                    livePossessionMessage,
                    targetDataResolver));
        }

        /*
         * 생체 빙의 후 사망하여 시체가 된 대상은 기존의
         * corpse_available 규칙이 Consumed 상태 때문에 막을 수 있으므로
         * 이 특수한 경우에는 위의 직접 검사 결과를 사용합니다.
         */
        if (!canUseConsumedCorpse
            && !IsPossessionRuleSatisfied(targetDataResolver))
        {
            return StorePossessionResult(
                HWJ_PossessionResult.Fail(
                    ResolveRuleFailureCode(lastPossessionResult),
                    lastPossessionResult,
                    targetDataResolver));
        }

        return StorePossessionResult(
            HWJ_PossessionResult.Success(
                targetDataResolver,
                isLiveTarget
                    ? "Live possession target is valid."
                    : "Corpse possession target is valid."));
    }

    private bool IsPossessionRuleSatisfied(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!useGameplayPossessionRule)
        {
            return true;
        }

        HWJ_GameplayContext context =
            HWJ_GameplayContext
                .Create(ownerDataResolver, targetDataResolver)
                .WithSource(this)
                .WithTarget(targetDataResolver);

        if (ResolvePossessionExecutionCore(
                out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool corePassed = executionCore.TryExecute(
                context,
                out HWJ_RuleExecutionResult executionResult);

            lastPossessionResult = executionResult.Message;
            return corePassed;
        }

        HWJ_GameplayRuleSO rule = possessionRule;

        if (rule == null
            && !string.IsNullOrEmpty(possessionRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(
                possessionRuleId,
                out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool passed = rule.TryEvaluate(
            context,
            out HWJ_RuleEvaluationResult result);

        lastPossessionResult = result.Message;
        return passed;
    }

    private bool ResolvePossessionExecutionCore(
        out HWJ_RuleExecutionCoreSO executionCore)
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

        return HWJ_GameAccess.TryGetRuleExecutionCore(
            possessionExecutionCoreId,
            out executionCore);
    }

    /// <summary>
    /// 죽은 몬스터를 E 입력으로 즉시 빙의할 때 사용합니다.
    /// 살아 있는 몬스터는 이 메서드로 빙의할 수 없습니다.
    /// </summary>
    public bool TryPossess(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody))
        {
            return false;
        }

        if (IsLivePossessionTarget(
                targetDataResolver,
                possessionBody))
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.PossessionBlocked;

            lastPossessionResult =
                "살아 있는 몬스터는 R 빙의 미니게임에 성공한 뒤 빙의해야 합니다.";

            return false;
        }

        return StartPossessionInternal(
            targetDataResolver,
            possessionBody,
            false,
            null);
    }

    /// <summary>
    /// R 입력 후 생체 빙의 미니게임을 시작하기 전에 호출합니다.
    /// 대상 정신력이 10 이하이면 false를 반환합니다.
    /// </summary>
    public bool CanStartLivePossessionMinigame(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody)
            || !IsLivePossessionTarget(
                targetDataResolver,
                possessionBody))
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.TargetUnavailable;

            lastPossessionResult =
                "대상이 살아 있는 빙의 가능 몬스터가 아닙니다.";

            return false;
        }

        return true;
    }

    /// <summary>
    /// R 생체 빙의 미니게임 성공 시 호출합니다.
    /// 성공한 시점에 몬스터 정신력을 10 차감합니다.
    /// </summary>
    public bool CompleteLivePossessionFromMinigame(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        /*
         * 미니게임이 진행되는 동안 대상이 죽었거나,
         * 플레이어 상태가 변경될 수 있으므로 다시 검사합니다.
         */
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody)
            || !IsLivePossessionTarget(
                targetDataResolver,
                possessionBody))
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.TargetUnavailable;

            lastPossessionResult =
                "대상이 살아 있는 빙의 가능 몬스터가 아닙니다.";

            return false;
        }

        HWJ_LivePossessionMentalState mentalState =
            targetDataResolver.GetComponent<
                HWJ_LivePossessionMentalState>();

        if (mentalState == null)
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.TargetUnavailable;

            lastPossessionResult =
                "대상에게 HWJ_LivePossessionMentalState가 없습니다.";

            return false;
        }

        /*
         * 현재 정신력이 10 이하인 대상은 TrySpendPossessionCost에서
         * 차단됩니다.
         *
         * 10보다 큰 대상만 정신력 10을 차감하고 빙의를 시작합니다.
         */
        if (!mentalState.TrySpendPossessionCost(
                livePossessionMentalCost,
                out string resultMessage))
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.TargetUnavailable;

            lastPossessionResult = resultMessage;
            return false;
        }

        return StartPossessionInternal(
            targetDataResolver,
            possessionBody,
            true,
            mentalState);
    }

    private bool StartPossessionInternal(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody,
        bool isLivePossession,
        HWJ_LivePossessionMentalState liveMentalState)
    {
        activePossessionBodyData = possessionBody;
        activePossessionWasLive = isLivePossession;
        activeLiveMentalState = liveMentalState;

        /*
         * 생체 빙의체가 HP 0으로 시체가 된 뒤
         * E를 눌러 시체 빙의를 시작하는 경우입니다.
         */
        if (!isLivePossession
            && targetDataResolver.TryGetComponent(
                out HWJ_LivePossessionMentalState corpseMentalState))
        {
            corpseMentalState.MarkCorpsePossessionConsumed();
        }

        possessedBodyResolver = targetDataResolver;

        MarkPossessionBodyConsumed(
            targetDataResolver,
            ShouldRestoreOriginalBodyOnPossessionExit(
                targetDataResolver,
                possessionBody));

        /*
         * 생체 빙의에서는 ResolveOwnerBodyDecayData가 null을 반환하므로
         * 부패 데이터를 사용하지 않습니다.
         *
         * 시체 빙의에서는 기존 BodyDecay 데이터를 사용합니다.
         */
        CreateRuntimeBodyState(
            targetDataResolver,
            true,
            true);

        if (possessionBody == null
            || possessionBody.transfersControlToBody)
        {
            TransferOwnerToPossessedBody(targetDataResolver);
        }

        soulSystem?.EnterBodyState();

        if (possessionBody != null
            && possessionBody.loadsBodyStatsToPlayer
            && runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(true);
        }

        lastPossessionResult = isLivePossession
            ? $"Live possessed {targetDataResolver.name}."
            : $"Corpse possessed {targetDataResolver.name}.";

        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(
                this,
                targetDataResolver,
                true,
                lastPossessionResult));

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
            lastPossessionResult =
                "Restore possession failed: missing root object data.";

            return false;
        }

        HWJ_RootObjectDataResolver restoredResolver =
            GetOrCreateRuntimePossessedBodyResolver();

        if (restoredResolver == null)
        {
            lastPossessionResult =
                "Restore possession failed: missing runtime resolver.";

            return false;
        }

        restoredResolver.SetRootObjectData(rootObjectData);

        if (!TryGetPossessionBodyData(
                restoredResolver,
                out activePossessionBodyData))
        {
            possessedBodyResolver = null;
            activePossessionBodyData = null;
            activePossessionWasLive = false;
            activeLiveMentalState = null;

            lastPossessionResult =
                $"Restore possession failed: {rootObjectData.name} has no body data.";

            return false;
        }

        possessedBodyResolver = restoredResolver;

        /*
         * 기존 저장 데이터에는 생체/시체 빙의 구분값이 없으므로
         * 현재 복원은 기본적으로 시체 빙의로 처리합니다.
         */
        activePossessionWasLive = false;
        activeLiveMentalState = null;

        CreateRuntimeBodyState(
            restoredResolver,
            refillToMax,
            true);

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

        lastPossessionResult =
            $"Restored possessed body from {rootObjectData.name}.";

        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(
                this,
                possessedBodyResolver,
                true,
                lastPossessionResult));

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

        lastPossessionResult =
            "Exited possessed body to Soul state.";

        soulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.ManualExit);

        return true;
    }

    /// <summary>
    /// 살아 있는 빙의체의 정신력이 0이 되었을 때 호출합니다.
    /// 몬스터를 영구 재빙의 불가 상태로 만들고 적대 상태로 복귀시킵니다.
    /// </summary>
    public bool ReleasePossessedBodyByMentalDepletion()
    {
        if (!HasActivePossessedBody)
        {
            lastPossessionResult =
                "Possession mental release failed: no active possessed body.";

            return false;
        }

        if (soulSystem == null
            || soulSystem.CurrentState
            != HWJ_SoulRuntimeState.Body)
        {
            lastPossessionResult =
                "Possession mental release failed: player is not in Body state.";

            return false;
        }

        if (activePossessionWasLive)
        {
            activeLiveMentalState?
                .BlockPossessionPermanently();

            lastPossessionResult =
                "몬스터 정신력이 0이 되어 빙의가 해제됩니다. "
                + "해당 몬스터는 다시 빙의할 수 없습니다.";
        }
        else
        {
            lastPossessionResult =
                "빙의체 정신력이 고갈되어 영혼 상태로 복귀합니다.";
        }

        soulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.MentalDepleted);

        return true;
    }

    /// <summary>
    /// 시체 부패가 최대치에 도달했을 때 호출합니다.
    /// </summary>
    public bool ReleasePossessedBodyByDecayDepletion()
    {
        if (!HasActivePossessedBody)
        {
            lastPossessionResult =
                "Decay release failed: no active possessed body.";

            return false;
        }

        if (IsLivePossessionActive)
        {
            lastPossessionResult =
                "Decay release failed: live possession does not use decay.";

            return false;
        }

        if (soulSystem == null
            || soulSystem.CurrentState
            != HWJ_SoulRuntimeState.Body)
        {
            lastPossessionResult =
                "Decay release failed: player is not in Body state.";

            return false;
        }

        lastPossessionResult =
            "시체 부패가 최대치에 도달하여 육체가 붕괴하고 "
            + "영혼 상태로 복귀합니다.";

        soulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.DecayDepleted);

        return true;
    }

    /// <summary>
    /// 현재 빙의 중인 육신을 해제합니다.
    /// </summary>
    public void ClearPossessedBody(
        bool refreshStatus = true,
        bool refillToMax = false,
        bool saveSnapshot = true,
        HWJ_PossessedBodyExitReason exitReason =
            HWJ_PossessedBodyExitReason.ManualExit)
    {
        HWJ_RootObjectDataResolver previousBodyResolver =
            possessedBodyResolver;

        bool hadActiveBody = HasActivePossessedBody;
        bool previousPossessionWasLive =
            activePossessionWasLive;

        HWJ_LivePossessionMentalState previousMentalState =
            activeLiveMentalState;

        HWJ_PossessedBodyRuntimeState previousBodyState =
            possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(
                out HWJ_PossessedBodyRuntimeState bodyState)
                ? bodyState
                : null;

        bool shouldRestoreOriginalBody =
            ShouldRestoreOriginalBodyForExit(
                exitReason,
                previousPossessionWasLive);

        /*
         * 생체 빙의의 수동 해제, 정신력 고갈, HP 고갈에서는
         * 원래 몬스터 오브젝트를 다시 활성화합니다.
         */
        bool restoredOriginalBody =
            shouldRestoreOriginalBody
            && RestoreOriginalBodyAfterPossessionIfNeeded(
                previousBodyResolver,
                previousBodyState);

        bool restoredAsCorpse = false;

        /*
         * 생체 빙의 중 HP가 0이 되었다면 원래 몬스터를
         * 살아 있는 적으로 되돌리지 않고 시체 상태로 변경합니다.
         */
        if (previousPossessionWasLive
            && exitReason
            == HWJ_PossessedBodyExitReason.HpDepleted
            && restoredOriginalBody)
        {
            restoredAsCorpse =
                RestoreLiveBodyAsCorpseAfterHpDepleted(
                    previousBodyResolver,
                    previousMentalState);
        }

        /*
         * 시체 빙의 상태에서 HP가 0이 되거나 부패가 최대가 되면
         * 현재 시체를 제거합니다.
         *
         * 생체 빙의 HP 0인데 원본 복원이 실패한 경우에도
         * 남은 비활성 육체를 제거합니다.
         */
        bool shouldRemoveCollapsedBody =
            (!previousPossessionWasLive
                && (exitReason
                        == HWJ_PossessedBodyExitReason.HpDepleted
                    || exitReason
                        == HWJ_PossessedBodyExitReason.DecayDepleted))
            || (previousPossessionWasLive
                && exitReason
                    == HWJ_PossessedBodyExitReason.HpDepleted
                && !restoredAsCorpse);

        bool removedCollapsedBody =
            shouldRemoveCollapsedBody
            && RemovePossessedBodyAfterHpDepleted(
                previousBodyResolver);

        possessedBodyResolver = null;
        activePossessionBodyData = null;
        activePossessionWasLive = false;
        activeLiveMentalState = null;

        possessedBodySystem?.ClearCurrentBodyState(
            exitReason == HWJ_PossessedBodyExitReason.HpDepleted
            || exitReason
                == HWJ_PossessedBodyExitReason.DecayDepleted);

        RestoreOwnerVisual();

        if (refreshStatus)
        {
            runtimeStatus?.RefreshCurrentHpFromData(
                refillToMax);
        }

        if (saveSnapshot)
        {
            SavePlayerRuntimeSnapshotIfOwner();
        }

        if (hadActiveBody)
        {
            string possessionEndMessage =
                ResolvePossessionEndMessage(
                    exitReason,
                    restoredOriginalBody,
                    restoredAsCorpse,
                    removedCollapsedBody);

            HWJ_GameplayEvents.RaisePossessionChanged(
                new HWJ_PossessionEvent(
                    this,
                    previousBodyResolver,
                    false,
                    possessionEndMessage));
        }
    }

    private void CacheRuntimeReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver =
                GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem =
                GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus =
                GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem =
                GetComponent<HWJ_PossessedBodySystem>();
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem =
                GetComponent<HWJ_SkillUnlockSystem>();
        }
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput =
            GetComponent<HWJ_PlayerInputSystem>();

        if (playerInput == null)
        {
            playerInput = HWJ_GameAccess.PlayerInput;
        }
    }

    private HWJ_PossessionResult StorePossessionResult(
        HWJ_PossessionResult result)
    {
        lastPossessionFailureCode =
            result.FailureCode;

        lastPossessionResult =
            result.Message;

        return result;
    }

    private static HWJ_PossessionFailureCode ResolveRuleFailureCode(
        string ruleMessage)
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

        if (ruleMessage.Contains(
                "source_can_pay_possession_spirit_mental_cost"))
        {
            return HWJ_PossessionFailureCode.InsufficientSpiritMental;
        }

        if (ruleMessage.Contains("target_can_be_possessed")
            || ruleMessage.Contains("target_defeated")
            || ruleMessage.Contains("target_object"))
        {
            return HWJ_PossessionFailureCode.TargetUnavailable;
        }

        return HWJ_PossessionFailureCode.PossessionBlocked;
    }

    /*
     * 아래 플레이어 정신력 비용 메서드는 기존 외부 코드와의
     * 호환을 위해 유지합니다.
     *
     * 현재 TryPossess와 CompleteLivePossessionFromMinigame에서는
     * 사용하지 않습니다.
     */
    private bool CanSpendSpiritMentalForPossession(
        HWJ_PossessionData possessionBody,
        out string message)
    {
        float mentalCost =
            ResolveSpiritMentalCostOnPossession(
                possessionBody);

        if (mentalCost <= 0f)
        {
            message = "Possession mental cost is free.";
            return true;
        }

        if (runtimeStatus == null)
        {
            message =
                "Possession failed: missing runtime status for spirit mental cost.";

            return false;
        }

        if (runtimeStatus.CurrentSpiritMentalValue
            - mentalCost <= 0f)
        {
            message =
                $"Possession failed: not enough spirit mental. "
                + $"Need {mentalCost:0.###}, "
                + $"current {runtimeStatus.CurrentSpiritMentalValue:0.###}.";

            return false;
        }

        message =
            $"Possession mental cost is available. "
            + $"Cost {mentalCost:0.###}.";

        return true;
    }

    private bool TrySpendSpiritMentalForPossession(
        HWJ_PossessionData possessionBody)
    {
        if (!CanSpendSpiritMentalForPossession(
                possessionBody,
                out string message))
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.InsufficientSpiritMental;

            lastPossessionResult = message;
            return false;
        }

        float mentalCost =
            ResolveSpiritMentalCostOnPossession(
                possessionBody);

        if (mentalCost <= 0f)
        {
            return true;
        }

        bool spent =
            runtimeStatus != null
            && runtimeStatus.TryApplySpiritMentalCost(
                mentalCost,
                "possession_success");

        if (!spent)
        {
            lastPossessionFailureCode =
                HWJ_PossessionFailureCode.InsufficientSpiritMental;

            lastPossessionResult =
                "Possession failed: spirit mental cost could not be spent.";
        }

        return spent;
    }

    private static float ResolveSpiritMentalCostOnPossession(
        HWJ_PossessionData possessionBody)
    {
        return possessionBody != null
            ? Mathf.Max(
                0f,
                possessionBody.spiritMentalCostOnPossession)
            : 0f;
    }

    /*
     * 기존 무작위 생체 빙의 판정 메서드입니다.
     * 현재는 실제 미니게임 성공 콜백을 사용하므로
     * TryPossess에서는 호출하지 않습니다.
     */
    private bool TryResolveLivePossessionChallenge(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (!ShouldRunLivePossessionChallenge(
                targetDataResolver,
                possessionBody))
        {
            lastLivePossessionRoll = 0f;
            lastLivePossessionSuccessChance = 1f;
            return true;
        }

        float successChance =
            Mathf.Clamp01(
                possessionBody.livePossessionSuccessChance);

        float roll = Random.value;

        lastLivePossessionRoll = roll;
        lastLivePossessionSuccessChance = successChance;

        if (roll <= successChance)
        {
            lastPossessionResult =
                $"Live possession challenge succeeded. "
                + $"Roll {roll:0.###}, "
                + $"chance {successChance:0.###}.";

            return true;
        }

        ApplyLivePossessionFailurePenalty(
            targetDataResolver,
            possessionBody);

        lastPossessionFailureCode =
            HWJ_PossessionFailureCode.PossessionResisted;

        lastPossessionResult =
            string.IsNullOrWhiteSpace(
                possessionBody.livePossessionFailureMessage)
                ? $"Live possession resisted. "
                  + $"Roll {roll:0.###}, "
                  + $"chance {successChance:0.###}."
                : possessionBody.livePossessionFailureMessage;

        return false;
    }

    private bool ShouldRunLivePossessionChallenge(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        return targetDataResolver != null
            && possessionBody != null
            && !possessionBody.requiresDefeatedState
            && IsEnemyTarget(targetDataResolver)
            && !IsDefeatedTarget(targetDataResolver);
    }

    private bool IsLivePossessionTarget(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        return targetDataResolver != null
            && possessionBody != null
            && !possessionBody.requiresDefeatedState
            && IsEnemyTarget(targetDataResolver)
            && !IsDefeatedTarget(targetDataResolver);
    }

    /// <summary>
    /// 살아 있는 몬스터의 현재 정신력이 10을 초과하는지 검사합니다.
    /// 정확히 10이거나 10보다 낮으면 빙의할 수 없습니다.
    /// </summary>
    private bool CanAttemptLivePossession(
        HWJ_RootObjectDataResolver targetDataResolver,
        out string resultMessage)
    {
        resultMessage = null;

        if (targetDataResolver == null)
        {
            resultMessage = "빙의 대상이 없습니다.";
            return false;
        }

        HWJ_LivePossessionMentalState mentalState =
            targetDataResolver.GetComponent<
                HWJ_LivePossessionMentalState>();

        if (mentalState == null)
        {
            resultMessage =
                "대상에게 HWJ_LivePossessionMentalState가 없습니다.";

            return false;
        }

        if (mentalState.IsPermanentlyBlocked)
        {
            resultMessage =
                "이 몬스터는 다시 빙의할 수 없습니다.";

            return false;
        }

        /*
         * livePossessionMentalCost 기본값이 10이므로
         * CurrentMentalValue > 10일 때만 true입니다.
         */
        if (!mentalState.CanAttemptLivePossession(
                livePossessionMentalCost))
        {
            resultMessage =
                $"빙의 불가능: 대상 정신력이 "
                + $"{livePossessionMentalCost:0.##}을 초과해야 합니다. "
                + $"현재 정신력: "
                + $"{mentalState.CurrentMentalValue:0.##}";

            return false;
        }

        resultMessage = "생체 빙의 가능";
        return true;
    }

    public void ApplyLivePossessionMinigameFailure(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        if (!TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody))
        {
            return;
        }

        if (!IsLivePossessionTarget(
                targetDataResolver,
                possessionBody))
        {
            return;
        }

        ApplyLivePossessionFailurePenalty(
            targetDataResolver,
            possessionBody);
    }

    private void ApplyLivePossessionFailurePenalty(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (runtimeStatus != null)
        {
            runtimeStatus.TryApplySpiritMentalCost(
                Mathf.Max(
                    0f,
                    possessionBody
                        .livePossessionFailureSpiritMentalCost),
                "live_possession_failed");

            runtimeStatus.LockControl(
                possessionBody
                    .livePossessionFailureControlLockSeconds);
        }

        float knockbackPower =
            Mathf.Max(
                0f,
                possessionBody
                    .livePossessionFailureKnockbackPower);

        if (knockbackPower <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem =
            GetComponent<HWJ_KnockbackSystem>();

        if (knockbackSystem == null)
        {
            knockbackSystem =
                gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector3 rawDirection =
            targetDataResolver != null
                ? transform.position
                  - targetDataResolver.transform.position
                : Vector3.right;

        Vector2 direction = rawDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        knockbackSystem.PlayKnockback(
            direction,
            knockbackPower,
            Mathf.Max(
                0f,
                possessionBody
                    .livePossessionFailureKnockbackSeconds));
    }

    public bool TryGetPossessedRootObjectId(
        out string rootObjectId)
    {
        rootObjectId = null;

        if (!HasActivePossessedBody
            || possessedBodyResolver == null
            || possessedBodyResolver.RootObjectData == null
            || possessedBodyResolver.RootObjectData.Identity == null
            || string.IsNullOrEmpty(
                possessedBodyResolver
                    .RootObjectData
                    .Identity
                    .objectId))
        {
            return false;
        }

        rootObjectId =
            possessedBodyResolver
                .RootObjectData
                .Identity
                .objectId;

        return true;
    }

    public bool TryGetPossessedBodyRuntimeState(
        out HWJ_PossessedBodyRuntimeState state)
    {
        CacheRuntimeReferences();

        state = null;

        return possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(
                out state)
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
            animatorController =
                ownerAnimator.runtimeAnimatorController;

            hasVisual =
                hasVisual || animatorController != null;
        }

        return hasVisual;
    }

    public bool TryGetPossessedStatus(
        out HWJ_StatusData status)
    {
        status = null;

        if (!CanLoadBodyStats()
            || possessedBodyResolver.Status == null)
        {
            return false;
        }

        status = possessedBodyResolver.Status;
        return true;
    }

    public bool TryGetPossessedDamage(
        out HWJ_DamageData damage)
    {
        damage = null;

        if (!CanLoadBodyStats()
            || possessedBodyResolver.Damage == null)
        {
            return false;
        }

        damage = possessedBodyResolver.Damage;
        return true;
    }

    public bool TryGetPossessedReceivedDamage(
        out HWJ_ReceivedDamageData receivedDamage)
    {
        receivedDamage = null;

        if (!CanLoadBodyStats()
            || possessedBodyResolver.ReceivedDamage == null)
        {
            return false;
        }

        receivedDamage =
            possessedBodyResolver.ReceivedDamage;

        return true;
    }

    public bool TryGetPossessedSkillSet(
        out HWJ_SkillSetData skillSet)
    {
        skillSet = null;

        if (!HasActivePossessedBody)
        {
            return false;
        }

        if (possessedBodyResolver.TryGetTypeData(
                out HWJ_EnemyTypeDataSO enemyData))
        {
            if (HasAnySkillEntry(
                    enemyData.PlayerPossessionSkillSet))
            {
                skillSet =
                    enemyData.PlayerPossessionSkillSet;

                return true;
            }

            skillSet = enemyData.SkillCycle;
            return HasAnySkillEntry(skillSet);
        }

        if (possessedBodyResolver.TryGetTypeData(
                out HWJ_BossTypeDataSO bossData))
        {
            skillSet = bossData.SkillCycle;
            return skillSet != null;
        }

        return false;
    }

    private static bool HasAnySkillEntry(
        HWJ_SkillSetData skillSet)
    {
        return skillSet != null
            && skillSet.skills != null
            && skillSet.skills.Length > 0;
    }

    public bool TryGetPrimaryPossessedSkillId(
        out string skillId)
    {
        return TryGetPossessedSkillIdAt(
            0,
            out skillId);
    }

    public bool TryGetPossessedSkillIdAt(
        int slotIndex,
        out string skillId)
    {
        skillId = null;

        if (slotIndex < 0
            || !TryGetPossessedSkillSet(
                out HWJ_SkillSetData skillSet)
            || skillSet.skills == null)
        {
            return false;
        }

        HWJ_WeaponType weaponType =
            CurrentWeaponType;

        int matchedSlotIndex = 0;

        for (int i = 0; i < skillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData skill =
                skillSet.skills[i];

            if (skill == null
                || string.IsNullOrEmpty(skill.skillId))
            {
                continue;
            }

            bool weaponMatched =
                skill.requiredWeaponType
                    == HWJ_WeaponType.None
                || skill.requiredWeaponType
                    == weaponType;

            if (!weaponMatched)
            {
                continue;
            }

            if (!IsPossessedSkillEntryUnlocked(skill))
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

    public HWJ_SkillNodeDataSO[]
        GetCurrentPossessedSkillNodes()
    {
        if (!HasActivePossessedBody)
        {
            return new HWJ_SkillNodeDataSO[0];
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem =
                GetComponent<HWJ_SkillUnlockSystem>();
        }

        return skillUnlockSystem != null
            ? skillUnlockSystem.GetSkillNodesForWeapon(
                CurrentWeaponType)
            : new HWJ_SkillNodeDataSO[0];
    }

    public bool TryGetCurrentPossessedSkillNodeAt(
        int slotIndex,
        out HWJ_SkillNodeDataSO skillNode)
    {
        skillNode = null;

        if (slotIndex < 0)
        {
            return false;
        }

        HWJ_SkillNodeDataSO[] skillNodes =
            GetCurrentPossessedSkillNodes();

        if (slotIndex >= skillNodes.Length)
        {
            return false;
        }

        skillNode = skillNodes[slotIndex];
        return skillNode != null;
    }

    private bool IsPossessedSkillEntryUnlocked(
        HWJ_SkillEntryData skill)
    {
        if (skill == null
            || string.IsNullOrEmpty(skill.skillId))
        {
            return false;
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem =
                GetComponent<HWJ_SkillUnlockSystem>();
        }

        return skillUnlockSystem != null
            ? skillUnlockSystem.IsSkillEntryUnlocked(skill)
            : skill.startsUnlocked;
    }

    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver targetDataResolver,
        out HWJ_PossessionData possessionBody)
    {
        possessionBody = null;

        if (targetDataResolver == null)
        {
            return false;
        }

        if (targetDataResolver.TryGetTypeData(
                out HWJ_EnemyTypeDataSO enemyData))
        {
            if (enemyData.Role != null
                && (!enemyData.Role.leavesCorpseOnDeath
                    || !enemyData.Role.isPossessableBody))
            {
                return false;
            }

            possessionBody =
                enemyData.PossessionBody;

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
        /*
         * 살아 있는 몬스터 빙의에서는 부패 데이터를 사용하지 않습니다.
         */
        if (activePossessionWasLive)
        {
            return null;
        }

        if (TryGetActivePossessedBodyDecayData(
                out HWJ_BodyDecayData possessedBodyDecay))
        {
            return possessedBodyDecay;
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(
                out HWJ_PlayerTypeDataSO playerData))
        {
            return playerData.BodyDecay;
        }

        return null;
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager
            && HWJ_GameAccess.Manager.PlayerPossession
            == this)
        {
            HWJ_GameAccess.Manager
                .SavePlayerRuntimeSnapshot();
        }
    }

    private bool IsConsumedPossessionBody(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            targetDataResolver.GetComponent<
                HWJ_PossessionBodyState>();

        return bodyState != null
            && bodyState.IsConsumed;
    }

    private void MarkPossessionBodyConsumed(
        HWJ_RootObjectDataResolver targetDataResolver,
        bool restoreOriginalBodyOnExit)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        HWJ_PossessionBodyState bodyState =
            targetDataResolver.GetComponent<
                HWJ_PossessionBodyState>();

        if (bodyState == null)
        {
            bodyState =
                targetDataResolver.gameObject.AddComponent<
                    HWJ_PossessionBodyState>();
        }

        bool wasAliveWhenPossessed =
            IsEnemyTarget(targetDataResolver)
            && !IsDefeatedTarget(targetDataResolver);

        bodyState.CaptureBeforePossession(
            wasAliveWhenPossessed,
            restoreOriginalBodyOnExit);

        bodyState.MarkConsumed();
    }

    private bool ShouldRestoreOriginalBodyOnPossessionExit(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        return targetDataResolver != null
            && possessionBody != null
            && !possessionBody.requiresDefeatedState
            && IsEnemyTarget(targetDataResolver)
            && !IsDefeatedTarget(targetDataResolver);
    }

    private static bool ShouldRestoreOriginalBodyForExit(
        HWJ_PossessedBodyExitReason exitReason,
        bool wasLivePossession)
    {
        if (!wasLivePossession)
        {
            return false;
        }

        return exitReason
                == HWJ_PossessedBodyExitReason.ManualExit
            || exitReason
                == HWJ_PossessedBodyExitReason.MentalDepleted
            || exitReason
                == HWJ_PossessedBodyExitReason.HpDepleted;
    }

    private bool RestoreOriginalBodyAfterPossessionIfNeeded(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_PossessedBodyRuntimeState previousBodyState)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            previousBodyResolver.GetComponent<
                HWJ_PossessionBodyState>();

        if (bodyState == null
            || !bodyState.ShouldRestoreObjectOnPossessionExit)
        {
            return false;
        }

        bodyState.RestoreCapturedObjectState(transform);

        RestoreReleasedBodyHp(
            previousBodyResolver,
            previousBodyState);

        return true;
    }

    private static void RestoreReleasedBodyHp(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_PossessedBodyRuntimeState previousBodyState)
    {
        if (previousBodyResolver == null
            || previousBodyState == null)
        {
            return;
        }

        HWJ_RuntimeStatusSystem restoredStatus =
            previousBodyResolver.GetComponent<
                HWJ_RuntimeStatusSystem>();

        if (restoredStatus == null)
        {
            return;
        }

        float restoredHp =
            Mathf.Max(
                0f,
                previousBodyState.CurrentHp);

        restoredStatus.RestoreHpSnapshot(
            restoredHp,
            restoredHp,
            restoredHp);

        if (restoredHp > 0f)
        {
            restoredStatus.SetState(
                HWJ_RuntimeState.Idle);
        }
    }

    /// <summary>
    /// 생체 빙의체의 HP가 0이 되었을 때
    /// 원래 몬스터를 시체 상태로 전환합니다.
    /// </summary>
    private static bool RestoreLiveBodyAsCorpseAfterHpDepleted(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_LivePossessionMentalState mentalState)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem enemyStatus =
            previousBodyResolver.GetComponent<
                HWJ_RuntimeStatusSystem>();

        if (enemyStatus == null)
        {
            return false;
        }

        enemyStatus.RestoreHpSnapshot(
            0f,
            0f,
            0f);

        enemyStatus.SetState(
            HWJ_RuntimeState.Dead);

        mentalState?
            .MarkBecameCorpseAfterLivePossession();

        return true;
    }

    private bool RemovePossessedBodyAfterHpDepleted(
        HWJ_RootObjectDataResolver previousBodyResolver)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            previousBodyResolver.GetComponent<
                HWJ_PossessionBodyState>();

        bodyState?.MarkRemovedAfterPossession();

        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.Despawn(
                previousBodyResolver.gameObject);
        }
        else
        {
            previousBodyResolver.gameObject.SetActive(false);
        }

        return true;
    }

    private static string ResolvePossessionEndMessage(
        HWJ_PossessedBodyExitReason exitReason,
        bool restoredOriginalBody,
        bool restoredAsCorpse,
        bool removedCollapsedBody)
    {
        switch (exitReason)
        {
            case HWJ_PossessedBodyExitReason.MentalDepleted:
                return restoredOriginalBody
                    ? "몬스터 정신력이 0이 되어 빙의가 해제되고 "
                      + "적대 몬스터로 복귀했습니다. "
                      + "다시 빙의할 수 없습니다."
                    : "빙의체 정신력이 0이 되어 "
                      + "영혼 상태로 복귀했습니다.";

            case HWJ_PossessedBodyExitReason.DecayDepleted:
                return removedCollapsedBody
                    ? "시체 부패가 최대치에 도달하여 "
                      + "육체가 붕괴했습니다."
                    : "시체 부패가 최대치에 도달하여 "
                      + "빙의가 해제되었습니다.";

            case HWJ_PossessedBodyExitReason.HpDepleted:
                if (restoredAsCorpse)
                {
                    return "살아 있는 빙의체의 체력이 0이 되어 "
                           + "플레이어가 유령으로 이탈하고 "
                           + "몬스터가 시체가 되었습니다.";
                }

                return removedCollapsedBody
                    ? "빙의체 체력이 0이 되어 "
                      + "육체가 제거되었습니다."
                    : "빙의체 체력이 0이 되어 "
                      + "빙의가 해제되었습니다.";

            case HWJ_PossessedBodyExitReason.ManualExit:
                return restoredOriginalBody
                    ? "빙의를 수동 해제하여 몬스터가 "
                      + "적대 상태로 복귀했습니다."
                    : "빙의를 수동 해제했습니다.";

            default:
                return "빙의체가 해제되었습니다.";
        }
    }

    private bool CanExitPossessedBodyToSoul()
    {
        if (!allowManualSoulExit)
        {
            lastPossessionResult =
                "Exit possession failed: manual soul exit is disabled.";

            return false;
        }

        if (!HasActivePossessedBody)
        {
            lastPossessionResult =
                "Exit possession failed: no active possessed body.";

            return false;
        }

        if (soulSystem == null
            || soulSystem.CurrentState
            != HWJ_SoulRuntimeState.Body)
        {
            lastPossessionResult =
                "Exit possession failed: player is not in Body state.";

            return false;
        }

        if (runtimeStatus != null
            && runtimeStatus.IsDead)
        {
            lastPossessionResult =
                "Exit possession failed: player is dead.";

            return false;
        }

        return true;
    }

    private bool IsEnemyTarget(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        return targetDataResolver != null
            && targetDataResolver.ObjectType
            == HWJ_ObjectType.Enemy;
    }

    private bool IsDefeatedTarget(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            targetDataResolver.GetComponent<
                HWJ_RuntimeStatusSystem>();

        return targetStatus != null
            && (targetStatus.CurrentState
                    == HWJ_RuntimeState.Dead
                || (targetStatus.UsesHp
                    && targetStatus.CurrentHp <= 0f));
    }

    private bool IsDefeatedIfRequired(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (possessionBody == null
            || !possessionBody.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            targetDataResolver.GetComponent<
                HWJ_RuntimeStatusSystem>();

        return targetStatus != null
            && targetStatus.IsDead;
    }

    private void TransferOwnerToPossessedBody(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        if (moveOwnerToPossessedBody)
        {
            Vector3 targetPosition =
                targetDataResolver.transform.position;

            targetPosition.z =
                transform.position.z;

            transform.SetPositionAndRotation(
                targetPosition,
                targetDataResolver.transform.rotation);

            if (TryGetComponent(
                    out Rigidbody2D ownerBody))
            {
                ownerBody.linearVelocity =
                    Vector2.zero;

                ownerBody.angularVelocity = 0f;
            }
        }

        if (copyPossessedBodyVisual)
        {
            ApplyPossessedBodyVisual(
                targetDataResolver.gameObject);
        }

        if (consumePossessedCorpse)
        {
            DisablePossessedCorpseObject(
                targetDataResolver.gameObject);
        }
    }

    private void CacheOwnerVisual()
    {
        if (ownerSpriteRenderer == null)
        {
            ownerSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (ownerSpriteRenderer != null
            && !hasOwnerSpriteCache)
        {
            ownerOriginalSprite =
                ownerSpriteRenderer.sprite;

            ownerOriginalColor =
                ownerSpriteRenderer.color;

            ownerOriginalFlipX =
                ownerSpriteRenderer.flipX;

            ownerOriginalFlipY =
                ownerSpriteRenderer.flipY;

            hasOwnerSpriteCache = true;
        }

        if (ownerAnimator == null)
        {
            ownerAnimator =
                GetComponentInChildren<Animator>();
        }

        if (ownerAnimator != null
            && !hasOwnerAnimatorCache)
        {
            ownerOriginalAnimatorController =
                ownerAnimator.runtimeAnimatorController;

            hasOwnerAnimatorCache = true;
        }

        if (ownerMotionSystem == null)
        {
            ownerMotionSystem =
                GetComponent<HWJ_CharacterMotionSystem>();
        }
    }

    private HWJ_RootObjectDataResolver
        GetOrCreateRuntimePossessedBodyResolver()
    {
        if (runtimePossessedBodyResolver != null)
        {
            return runtimePossessedBodyResolver;
        }

        Transform runtimeBodyTransform =
            transform.Find("HWJ_RuntimePossessedBodyData");

        if (runtimeBodyTransform != null)
        {
            runtimePossessedBodyResolver =
                runtimeBodyTransform.GetComponent<
                    HWJ_RootObjectDataResolver>();
        }

        if (runtimePossessedBodyResolver == null)
        {
            GameObject runtimeBody =
                new GameObject(
                    "HWJ_RuntimePossessedBodyData");

            runtimeBody.transform.SetParent(
                transform,
                false);

            runtimePossessedBodyResolver =
                runtimeBody.AddComponent<
                    HWJ_RootObjectDataResolver>();

            runtimeBody.SetActive(false);
        }

        return runtimePossessedBodyResolver;
    }

    private void ApplyPossessedBodyVisual(
        GameObject possessedBody)
    {
        CacheOwnerVisual();

        if (possessedBody == null)
        {
            return;
        }

        SpriteRenderer possessedRenderer =
            possessedBody.GetComponentInChildren<
                SpriteRenderer>();

        if (ownerSpriteRenderer != null
            && possessedRenderer != null)
        {
            ownerSpriteRenderer.sprite =
                possessedRenderer.sprite;

            ownerSpriteRenderer.color =
                possessedRenderer.color;

            ownerSpriteRenderer.flipX =
                possessedRenderer.flipX;

            ownerSpriteRenderer.flipY =
                possessedRenderer.flipY;

            ownerMotionSystem?
                .RefreshFacingBaseline();
        }

        Animator possessedAnimator =
            possessedBody.GetComponentInChildren<
                Animator>();

        if (ownerAnimator != null
            && possessedAnimator != null)
        {
            ownerAnimator.runtimeAnimatorController =
                possessedAnimator.runtimeAnimatorController;
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

        if (ownerSpriteRenderer != null
            && sprite != null)
        {
            ownerSpriteRenderer.sprite = sprite;
            ownerSpriteRenderer.color = color;
            ownerSpriteRenderer.flipX = flipX;
            ownerSpriteRenderer.flipY = flipY;

            ownerMotionSystem?
                .RefreshFacingBaseline();
        }

        if (ownerAnimator != null
            && animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController =
                animatorController;
        }
    }

    private void ApplyPossessedBodyModelData(
        HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null
            || rootObjectData.Model == null)
        {
            return;
        }

        if (rootObjectData.Model.modelPrefab != null)
        {
            ApplyPossessedBodyVisual(
                rootObjectData.Model.modelPrefab);
        }

        if (ownerAnimator != null
            && rootObjectData.Model.animatorController != null)
        {
            ownerAnimator.runtimeAnimatorController =
                rootObjectData.Model.animatorController;
        }
    }

    private void RestoreOwnerVisual()
    {
        if (ownerSpriteRenderer != null
            && hasOwnerSpriteCache)
        {
            ownerSpriteRenderer.sprite =
                ownerOriginalSprite;

            ownerSpriteRenderer.color =
                ownerOriginalColor;

            ownerSpriteRenderer.flipX =
                ownerOriginalFlipX;

            ownerSpriteRenderer.flipY =
                ownerOriginalFlipY;

            ownerMotionSystem?
                .RefreshFacingBaseline();
        }

        if (ownerAnimator != null
            && hasOwnerAnimatorCache)
        {
            ownerAnimator.runtimeAnimatorController =
                ownerOriginalAnimatorController;
        }
    }

    private void DisablePossessedCorpseObject(
        GameObject possessedBody)
    {
        if (possessedBody == null)
        {
            return;
        }

        HWJ_EnemyNavigationSystem navigation =
            possessedBody.GetComponent<
                HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.enabled = false;
        }

        HWJ_MonsterAISystem monsterAI =
            possessedBody.GetComponent<
                HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.enabled = false;
        }

        Collider2D[] colliders =
            possessedBody.GetComponentsInChildren<
                Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        SpriteRenderer[] renderers =
            possessedBody.GetComponentsInChildren<
                SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        Animator animator =
            possessedBody.GetComponentInChildren<
                Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        Rigidbody2D body =
            possessedBody.GetComponent<Rigidbody2D>();

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