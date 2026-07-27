using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_CollapseSystem : MonoBehaviour
{
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PlayerAttackSystem playerAttackSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private bool isCollapsing;
    [SerializeField] private bool hasCollapsedCurrentBody;
    [SerializeField] private HWJ_BodyCollapseReason lastCollapseReason = HWJ_BodyCollapseReason.Unknown;
    [SerializeField] private HWJ_BodyCollapseFailureCode lastFailureCode = HWJ_BodyCollapseFailureCode.None;
    [SerializeField] private string lastCollapseMessage;

    public bool IsCollapsing => isCollapsing;
    public bool HasCollapsedCurrentBody => hasCollapsedCurrentBody;
    public HWJ_BodyCollapseReason LastCollapseReason => lastCollapseReason;
    public HWJ_BodyCollapseFailureCode LastFailureCode => lastFailureCode;
    public string LastCollapseMessage => lastCollapseMessage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Body
            && possessionSystem != null
            && possessionSystem.HasActivePossessedBody
            && possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out HWJ_PossessedBodyRuntimeState activeState)
            && activeState != null
            && !activeState.IsCollapsed)
        {
            isCollapsing = false;
            hasCollapsedCurrentBody = false;
        }

        if (!isCollapsing || soulSystem == null)
        {
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
            || soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        {
            isCollapsing = false;
        }
    }

    public HWJ_BodyCollapseResult TryCollapseCurrentBody(
        HWJ_BodyCollapseReason reason,
        bool refillSoulHp = false)
    {
        ResolveReferences();

        HWJ_BodyCollapseResult validationResult = ValidateCollapseRequest(reason);

        if (!validationResult.Succeeded)
        {
            StoreResult(validationResult);
            return validationResult;
        }

        isCollapsing = true;
        hasCollapsedCurrentBody = true;
        lastCollapseReason = reason;

        HWJ_RootObjectDataResolver collapsedBodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : null;
        HWJ_PossessedBodyRuntimeState collapsedState = possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out HWJ_PossessedBodyRuntimeState state)
                ? state
                : null;

        HWJ_GameplayEvents.RaiseBodyCollapseStarted(
            new HWJ_BodyCollapseEvent(this, collapsedBodyResolver, collapsedState, reason, "Body collapse started."));

        CancelCurrentActions();
        FreezeBodyMotion();
        runtimeStatus?.CacheCurrentHpForActiveState();
        runtimeStatus?.LockControl(0.1f);
        possessedBodySystem?.MarkCurrentBodyCollapsed();

        soulSystem.EnterSoulState(refillSoulHp, ResolvePossessedBodyExitReason(reason));

        HWJ_BodyCollapseResult result = HWJ_BodyCollapseResult.Success(
            reason,
            "Body collapse completed.");
        StoreResult(result);

        HWJ_GameplayEvents.RaiseBodyCollapsed(
            new HWJ_BodyCollapseEvent(this, collapsedBodyResolver, collapsedState, reason, result.Message));

        return result;
    }

    public void ResetCollapseGate()
    {
        isCollapsing = false;
        hasCollapsedCurrentBody = false;
        lastFailureCode = HWJ_BodyCollapseFailureCode.None;
        lastCollapseReason = HWJ_BodyCollapseReason.Unknown;
        lastCollapseMessage = null;
    }

    private HWJ_BodyCollapseResult ValidateCollapseRequest(HWJ_BodyCollapseReason reason)
    {
        if (soulSystem == null)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.MissingSoulSystem,
                reason,
                "Body collapse failed: missing HWJ_SoulSystem.");
        }

        if (isCollapsing || soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.AlreadyCollapsing,
                reason,
                "Body collapse failed: collapse is already running.");
        }

        if (soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.NotPossessedState,
                reason,
                "Body collapse failed: player is not in possessed body state.");
        }

        if (possessionSystem == null)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.MissingPossessionSystem,
                reason,
                "Body collapse failed: missing HWJ_PossessionSystem.");
        }

        if (!possessionSystem.HasActivePossessedBody)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.MissingPossessedBody,
                reason,
                "Body collapse failed: no active possessed body.");
        }

        if (possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
            && bodyState != null
            && bodyState.IsCollapsed
            && hasCollapsedCurrentBody)
        {
            return HWJ_BodyCollapseResult.Fail(
                HWJ_BodyCollapseFailureCode.AlreadyCollapsed,
                reason,
                "Body collapse failed: current body is already collapsed.");
        }

        return HWJ_BodyCollapseResult.Success(reason, "Body collapse request is valid.");
    }

    private void CancelCurrentActions()
    {
        playerAttackSystem?.CancelCurrentAttack();
        skillActionSystem?.CancelCurrentAction();
        runtimeStatus?.CancelAttackAction();
    }

    private void FreezeBodyMotion()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void StoreResult(HWJ_BodyCollapseResult result)
    {
        lastFailureCode = result.FailureCode;
        lastCollapseReason = result.Reason;
        lastCollapseMessage = result.Message;
    }

    private static HWJ_PossessedBodyExitReason ResolvePossessedBodyExitReason(HWJ_BodyCollapseReason reason)
    {
        return reason == HWJ_BodyCollapseReason.HpDepleted
            ? HWJ_PossessedBodyExitReason.HpDepleted
            : HWJ_PossessedBodyExitReason.ForcedClear;
    }

    private void ResolveReferences()
    {
        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerAttackSystem == null)
        {
            playerAttackSystem = GetComponent<HWJ_PlayerAttackSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }
}
