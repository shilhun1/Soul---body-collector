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

public enum HWJ_PossessionKind
{
    None,
    Live,
    Corpse
}

/// <summary>
/// 빙의 기능의 중앙 조정자입니다.
/// 검증, 생체/시체 처리, 해제, 저장 복원, 육체와 외형 처리는 분리된 컴포넌트에 위임합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HWJ_PossessionTargetValidator))]
[RequireComponent(typeof(HWJ_LivePossessionSystem))]
[RequireComponent(typeof(HWJ_CorpsePossessionSystem))]
[RequireComponent(typeof(HWJ_PossessionExitSystem))]
[RequireComponent(typeof(HWJ_PossessionSnapshotSystem))]
[RequireComponent(typeof(HWJ_PossessionBodyController))]
[RequireComponent(typeof(HWJ_PossessionVisualController))]
[RequireComponent(typeof(HWJ_PossessedSkillProvider))]
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

    [Space(8f)]
    [Header("분리된 컴포넌트")]
    [SerializeField] private HWJ_PossessionTargetValidator targetValidator;
    [SerializeField] private HWJ_LivePossessionSystem livePossessionSystem;
    [SerializeField] private HWJ_CorpsePossessionSystem corpsePossessionSystem;
    [SerializeField] private HWJ_PossessionExitSystem exitSystem;
    [SerializeField] private HWJ_PossessionSnapshotSystem snapshotSystem;
    [SerializeField] private HWJ_PossessionBodyController bodyController;
    [SerializeField] private HWJ_PossessionVisualController visualController;
    [SerializeField] private HWJ_PossessedSkillProvider skillProvider;

    [Space(8f)]
    [Header("빙의 전환")]
    [Tooltip("빙의 성공 직후 빙의 애니메이션이 끝날 때까지 이동, 공격, 대쉬를 잠그는 시간입니다.")]
    [SerializeField] private float possessionControlLockSeconds = 0.75f;

    [Space(8f)]
    [Header("디버그")]
    [SerializeField] private HWJ_PossessionFailureCode lastPossessionFailureCode;
    [SerializeField] private string lastPossessionResult;
    [SerializeField] private float lastLivePossessionRoll;
    [SerializeField] private float lastLivePossessionSuccessChance;
    [SerializeField] private HWJ_PossessionKind currentPossessionKind = HWJ_PossessionKind.None;

    private HWJ_PossessionData activePossessionBodyData;
    private HWJ_LivePossessionMentalState activeLiveMentalState;

    public bool HasActivePossessedBody => possessedBodyResolver != null
        && (soulSystem == null
            || soulSystem.CurrentExistenceState == HWJ_PlayerExistenceState.Possessed);

    public HWJ_RootObjectDataResolver PossessedBodyResolver => possessedBodyResolver;
    public HWJ_PossessedBodySystem PossessedBodySystem => possessedBodySystem;
    public bool CanLoadPossessedBodyStats => CanLoadBodyStats();
    public HWJ_PossessionKind CurrentPossessionKind => currentPossessionKind;
    public bool IsLivePossessionActive => HasActivePossessedBody
        && currentPossessionKind == HWJ_PossessionKind.Live
        && activeLiveMentalState != null;
    public bool IsCorpsePossessionActive => HasActivePossessedBody
        && currentPossessionKind == HWJ_PossessionKind.Corpse;

    public HWJ_WeaponType CurrentWeaponType => HasActivePossessedBody
        ? possessedBodyResolver.WeaponType
        : HWJ_WeaponType.None;

    public HWJ_PossessionFailureCode LastPossessionFailureCode => lastPossessionFailureCode;
    public string LastPossessionResult => lastPossessionResult;
    public float LastLivePossessionRoll => lastLivePossessionRoll;
    public float LastLivePossessionSuccessChance => lastLivePossessionSuccessChance;

    internal HWJ_RootObjectDataResolver OwnerDataResolver => ownerDataResolver;
    internal HWJ_SoulSystem SoulSystem => soulSystem;
    internal HWJ_RuntimeStatusSystem RuntimeStatus => runtimeStatus;
    internal HWJ_PossessedBodySystem RuntimeBodySystem => possessedBodySystem;
    internal HWJ_PossessionData ActivePossessionBodyData => activePossessionBodyData;
    internal HWJ_LivePossessionMentalState ActiveLiveMentalState => activeLiveMentalState;
    internal bool HasPossessedBodyReference => possessedBodyResolver != null;
    internal HWJ_PossessionTargetValidator TargetValidator => targetValidator;
    internal HWJ_PossessionBodyController BodyController => bodyController;
    internal HWJ_PossessionVisualController VisualController => visualController;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        visualController?.CacheOwnerVisual();
    }

    private void Update()
    {
        ResolvePlayerInput();

        if (HasActivePossessedBody
            && runtimeStatus != null
            && runtimeStatus.UsesHp
            && runtimeStatus.CurrentHp <= 0f)
        {
            soulSystem?.EnterSoulState(false, HWJ_PossessedBodyExitReason.HpDepleted);
            return;
        }

        if (playerInput != null && playerInput.ExitPossessionPressedThisFrame)
        {
            TryExitPossessedBodyToSoul();
        }
    }

    public bool CanPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        return EvaluatePossession(targetDataResolver).Succeeded;
    }

    public HWJ_PossessionResult EvaluatePossession(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (targetValidator == null)
        {
            return StorePossessionResult(HWJ_PossessionResult.Fail(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Possession failed: missing HWJ_PossessionTargetValidator.",
                targetDataResolver));
        }

        return StorePossessionResult(targetValidator.Evaluate(targetDataResolver));
    }

    /// <summary>E 입력용 시체 즉시 빙의 API입니다.</summary>
    public bool TryPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (corpsePossessionSystem == null)
        {
            StoreFailure(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Corpse possession failed: missing HWJ_CorpsePossessionSystem.");
            return false;
        }

        return corpsePossessionSystem.TryPossessCorpse(targetDataResolver);
    }

    /// <summary>R 입력 후 생체 빙의 미니게임을 열기 전에 호출합니다.</summary>
    public bool CanStartLivePossessionMinigame(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();
        return livePossessionSystem != null
            && livePossessionSystem.CanStartMinigame(targetDataResolver);
    }

    /// <summary>생체 빙의 미니게임 성공 시 호출합니다.</summary>
    public bool CompleteLivePossessionFromMinigame(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();
        return livePossessionSystem != null
            && livePossessionSystem.CompleteFromMinigame(targetDataResolver);
    }

    public void ApplyLivePossessionMinigameFailure(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();
        livePossessionSystem?.ApplyMinigameFailure(targetDataResolver);
    }

    /// <summary>
    /// 생체/시체 전용 시스템이 검증을 끝낸 뒤 호출하는 공통 빙의 시작 처리입니다.
    /// </summary>
    internal bool BeginPossession(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody,
        HWJ_PossessionKind possessionKind,
        HWJ_LivePossessionMentalState liveMentalState)
    {
        ResolveReferences();

        if (targetDataResolver == null || possessionBody == null)
        {
            StoreFailure(
                HWJ_PossessionFailureCode.InvalidTarget,
                "Possession failed: missing target or possession data.");
            return false;
        }

        if (bodyController == null || visualController == null)
        {
            StoreFailure(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "Possession failed: missing body or visual controller.");
            return false;
        }

        HWJ_RuntimeStatusSystem targetStatus =
            targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        float liveBodyCurrentHp = targetStatus != null
            ? targetStatus.CurrentHp
            : targetDataResolver.Status != null
                ? targetDataResolver.Status.maxHp
                : 0f;

        RegisterPossessionState(
            targetDataResolver,
            possessionBody,
            possessionKind,
            liveMentalState);

        if (possessionKind == HWJ_PossessionKind.Corpse
            && targetDataResolver.TryGetComponent(out HWJ_LivePossessionMentalState corpseMentalState)
            && corpseMentalState.CanPossessAsCorpse())
        {
            corpseMentalState.MarkCorpsePossessionConsumed();
        }

        bodyController.CaptureBeforePossession(
            targetDataResolver,
            possessionKind == HWJ_PossessionKind.Live);
        CreateRuntimeBodyState(
            targetDataResolver,
            possessionKind == HWJ_PossessionKind.Corpse,
            true);

        // 생체 빙의는 대상이 빙의 직전에 보유하던 HP를 그대로 이어받습니다.
        // 그래야 정신력 0 해제 시 같은 남은 HP로 적대 몬스터가 복귀합니다.
        if (possessionKind == HWJ_PossessionKind.Live)
        {
            possessedBodySystem?.SetCurrentHp(liveBodyCurrentHp);
        }

        if (possessionBody.transfersControlToBody)
        {
            bodyController.TransferOwnerToPossessedBody(targetDataResolver, visualController);
        }

        soulSystem?.EnterBodyState();
        LockOwnerControlForPossessionTransition();

        if (possessionBody.loadsBodyStatsToPlayer && runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(
                possessionKind == HWJ_PossessionKind.Corpse);
        }

        lastPossessionResult = possessionKind == HWJ_PossessionKind.Live
            ? $"Live possessed {targetDataResolver.name}."
            : $"Corpse possessed {targetDataResolver.name}.";

        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(this, targetDataResolver, true, lastPossessionResult));
        SavePlayerRuntimeSnapshotIfOwner();
        return true;
    }

    private void LockOwnerControlForPossessionTransition()
    {
        if (runtimeStatus == null || possessionControlLockSeconds <= 0f)
        {
            return;
        }

        runtimeStatus.LockControl(possessionControlLockSeconds);
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
        ResolveReferences();
        return snapshotSystem != null
            && snapshotSystem.RestorePossessedBody(
                rootObjectData,
                visualSprite,
                visualColor,
                visualFlipX,
                visualFlipY,
                animatorController,
                hasVisualSnapshot,
                refreshStatus,
                refillToMax,
                saveSnapshot);
    }

    public bool TryExitPossessedBodyToSoul()
    {
        ResolveReferences();
        return exitSystem != null && exitSystem.TryManualExit();
    }

    public bool ReleasePossessedBodyByMentalDepletion()
    {
        ResolveReferences();
        return exitSystem != null && exitSystem.ReleaseByMentalDepletion();
    }

    public bool ReleasePossessedBodyByDecayDepletion()
    {
        ResolveReferences();
        return exitSystem != null && exitSystem.ReleaseByDecayDepletion();
    }

    public void ClearPossessedBody(
        bool refreshStatus = true,
        bool refillToMax = false,
        bool saveSnapshot = true,
        HWJ_PossessedBodyExitReason exitReason = HWJ_PossessedBodyExitReason.ManualExit)
    {
        ResolveReferences();
        exitSystem?.ClearPossessedBody(refreshStatus, refillToMax, saveSnapshot, exitReason);
    }

    public bool TryGetActiveLiveMentalState(
        out HWJ_LivePossessionMentalState mentalState)
    {
        mentalState = IsLivePossessionActive ? activeLiveMentalState : null;
        return mentalState != null;
    }

    public bool TryGetActivePossessedBodyDecayData(out HWJ_BodyDecayData bodyDecay)
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

        bodyDecay = activePossessionBodyData.possessedBodyDecayOverride;
        return true;
    }

    public bool TryGetPossessedRootObjectId(out string rootObjectId)
    {
        rootObjectId = null;

        if (!HasActivePossessedBody
            || possessedBodyResolver.RootObjectData == null
            || possessedBodyResolver.RootObjectData.Identity == null
            || string.IsNullOrEmpty(possessedBodyResolver.RootObjectData.Identity.objectId))
        {
            return false;
        }

        rootObjectId = possessedBodyResolver.RootObjectData.Identity.objectId;
        return true;
    }

    public bool TryGetPossessedBodyRuntimeState(
        out HWJ_PossessedBodyRuntimeState state)
    {
        ResolveReferences();
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
        ResolveReferences();

        if (!HasActivePossessedBody || visualController == null)
        {
            sprite = null;
            color = Color.white;
            flipX = false;
            flipY = false;
            animatorController = null;
            return false;
        }

        return visualController.TryGetCurrentSnapshot(
            out sprite,
            out color,
            out flipX,
            out flipY,
            out animatorController);
    }

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

    public bool TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet)
    {
        ResolveReferences();
        skillSet = null;
        return skillProvider != null && skillProvider.TryGetPossessedSkillSet(out skillSet);
    }

    public bool TryGetPrimaryPossessedSkillId(out string skillId)
    {
        ResolveReferences();
        skillId = null;
        return skillProvider != null && skillProvider.TryGetPrimaryPossessedSkillId(out skillId);
    }

    public bool TryGetPossessedSkillIdAt(int slotIndex, out string skillId)
    {
        ResolveReferences();
        skillId = null;
        return skillProvider != null
            && skillProvider.TryGetPossessedSkillIdAt(slotIndex, out skillId);
    }

    public HWJ_SkillNodeDataSO[] GetCurrentPossessedSkillNodes()
    {
        ResolveReferences();
        return skillProvider != null
            ? skillProvider.GetCurrentPossessedSkillNodes()
            : new HWJ_SkillNodeDataSO[0];
    }

    public bool TryGetCurrentPossessedSkillNodeAt(
        int slotIndex,
        out HWJ_SkillNodeDataSO skillNode)
    {
        ResolveReferences();
        skillNode = null;
        return skillProvider != null
            && skillProvider.TryGetCurrentPossessedSkillNodeAt(slotIndex, out skillNode);
    }

    internal void RegisterPossessionState(
        HWJ_RootObjectDataResolver resolver,
        HWJ_PossessionData possessionData,
        HWJ_PossessionKind kind,
        HWJ_LivePossessionMentalState mentalState)
    {
        possessedBodyResolver = resolver;
        activePossessionBodyData = possessionData;
        currentPossessionKind = kind;
        activeLiveMentalState = kind == HWJ_PossessionKind.Live ? mentalState : null;
    }

    internal void ClearRegisteredPossessionState()
    {
        possessedBodyResolver = null;
        activePossessionBodyData = null;
        activeLiveMentalState = null;
        currentPossessionKind = HWJ_PossessionKind.None;
    }

    internal void CreateRuntimeBodyState(
        HWJ_RootObjectDataResolver targetDataResolver,
        bool refillHpToMax,
        bool resetDecayToInitial)
    {
        if (possessedBodySystem == null)
        {
            return;
        }

        possessedBodySystem.CreateCurrentBodyState(
            targetDataResolver,
            ResolveCurrentBodyDecayData(),
            refillHpToMax,
            resetDecayToInitial);
    }

    internal void StoreFailure(HWJ_PossessionFailureCode code, string message)
    {
        lastPossessionFailureCode = code;
        lastPossessionResult = message;
    }

    internal void StoreResultMessage(string message)
    {
        lastPossessionResult = message;
    }

    internal void RecordLiveChallengeDebug(float roll, float successChance)
    {
        lastLivePossessionRoll = roll;
        lastLivePossessionSuccessChance = successChance;
    }

    internal void SaveRuntimeSnapshot()
    {
        SavePlayerRuntimeSnapshotIfOwner();
    }

    internal void RefreshReferences()
    {
        ResolveReferences();
    }

    private HWJ_PossessionResult StorePossessionResult(HWJ_PossessionResult result)
    {
        lastPossessionFailureCode = result.FailureCode;
        lastPossessionResult = result.Message;
        return result;
    }

    private bool CanLoadBodyStats()
    {
        return HasActivePossessedBody
            && activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer;
    }

    private HWJ_BodyDecayData ResolveCurrentBodyDecayData()
    {
        // 생체 빙의에서도 런타임 육체 상태 생성은 기존 데이터 구조를 유지합니다.
        // 실제 부패 증가는 HWJ_BodyDecaySystem에서 차단합니다.
        if (currentPossessionKind == HWJ_PossessionKind.Corpse
            && TryGetActivePossessedBodyDecayData(out HWJ_BodyDecayData bodyOverride))
        {
            return bodyOverride;
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return playerData.BodyDecay;
        }

        return null;
    }

    private void ResolveReferences()
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

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        if (skillUnlockSystem == null)
        {
            skillUnlockSystem = GetComponent<HWJ_SkillUnlockSystem>();
        }

        if (targetValidator == null)
        {
            targetValidator = GetComponent<HWJ_PossessionTargetValidator>();
        }

        if (livePossessionSystem == null)
        {
            livePossessionSystem = GetComponent<HWJ_LivePossessionSystem>();
        }

        if (corpsePossessionSystem == null)
        {
            corpsePossessionSystem = GetComponent<HWJ_CorpsePossessionSystem>();
        }

        if (exitSystem == null)
        {
            exitSystem = GetComponent<HWJ_PossessionExitSystem>();
        }

        if (snapshotSystem == null)
        {
            snapshotSystem = GetComponent<HWJ_PossessionSnapshotSystem>();
        }

        if (bodyController == null)
        {
            bodyController = GetComponent<HWJ_PossessionBodyController>();
        }

        if (visualController == null)
        {
            visualController = GetComponent<HWJ_PossessionVisualController>();
        }

        if (skillProvider == null)
        {
            skillProvider = GetComponent<HWJ_PossessedSkillProvider>();
        }

        ResolvePlayerInput();
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

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerPossession == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }
}
