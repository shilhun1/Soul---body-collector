using UnityEngine;

/// <summary>
/// 수동 해제, 정신력 0, 부패 최대, HP 0에 따른 빙의 종료를 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionExitSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessionBodyController bodyController;
    [SerializeField] private HWJ_PossessionVisualController visualController;
    [SerializeField] private bool allowManualSoulExit = true;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public bool TryManualExit()
    {
        ResolveReferences();

        if (!CanExitManually())
        {
            return false;
        }

        possessionSystem.StoreResultMessage("Exited possessed body to Soul state.");
        possessionSystem.SoulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.ManualExit);
        return true;
    }

    public bool ReleaseByMentalDepletion()
    {
        ResolveReferences();

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            possessionSystem?.StoreResultMessage(
                "Possession mental release failed: no active possessed body.");
            return false;
        }

        if (possessionSystem.SoulSystem == null
            || possessionSystem.SoulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            possessionSystem.StoreResultMessage(
                "Possession mental release failed: player is not in Body state.");
            return false;
        }

        if (possessionSystem.CurrentPossessionKind == HWJ_PossessionKind.Live)
        {
            possessionSystem.ActiveLiveMentalState?.BlockPossessionPermanently();
            possessionSystem.StoreResultMessage(
                "몬스터 정신력이 0이 되어 빙의가 해제됩니다. 해당 몬스터는 다시 빙의할 수 없습니다.");
        }
        else
        {
            possessionSystem.StoreResultMessage(
                "Possession mental depleted. Returning to Soul state.");
        }

        possessionSystem.SoulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.MentalDepleted);
        return true;
    }

    public bool ReleaseByDecayDepletion()
    {
        ResolveReferences();

        if (possessionSystem == null
            || !possessionSystem.HasActivePossessedBody
            || possessionSystem.CurrentPossessionKind != HWJ_PossessionKind.Corpse)
        {
            possessionSystem?.StoreResultMessage(
                "Decay release failed: no active corpse possession.");
            return false;
        }

        if (possessionSystem.SoulSystem == null
            || possessionSystem.SoulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            possessionSystem.StoreResultMessage(
                "Decay release failed: player is not in Body state.");
            return false;
        }

        possessionSystem.StoreResultMessage(
            "시체 부패가 최대치에 도달하여 육체가 붕괴합니다.");
        possessionSystem.SoulSystem.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.DecayDepleted);
        return true;
    }

    public void ClearPossessedBody(
        bool refreshStatus,
        bool refillToMax,
        bool saveSnapshot,
        HWJ_PossessedBodyExitReason exitReason)
    {
        ResolveReferences();

        if (possessionSystem == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver previousBodyResolver =
            possessionSystem.PossessedBodyResolver;
        HWJ_PossessionKind previousKind =
            possessionSystem.CurrentPossessionKind;
        HWJ_LivePossessionMentalState previousMentalState =
            possessionSystem.ActiveLiveMentalState;
        bool hadActiveBody = possessionSystem.HasActivePossessedBody;

        HWJ_PossessedBodyRuntimeState previousBodyState =
            possessionSystem.RuntimeBodySystem != null
            && possessionSystem.RuntimeBodySystem.TryGetCurrentBodyState(
                out HWJ_PossessedBodyRuntimeState bodyState)
                ? bodyState
                : null;

        bool restoredOriginalBody = false;
        bool removedCollapsedBody = false;

        if (previousKind == HWJ_PossessionKind.Live
            && (exitReason == HWJ_PossessedBodyExitReason.ManualExit
                || exitReason == HWJ_PossessedBodyExitReason.MentalDepleted))
        {
            restoredOriginalBody = bodyController != null
                && bodyController.RestoreOriginalBodyAfterPossession(
                    previousBodyResolver,
                    previousBodyState);
        }

        if (previousKind == HWJ_PossessionKind.Corpse
            && (exitReason == HWJ_PossessedBodyExitReason.HpDepleted
                || exitReason == HWJ_PossessedBodyExitReason.DecayDepleted))
        {
            removedCollapsedBody = bodyController != null
                && bodyController.RemovePossessedBody(previousBodyResolver);
        }
        else if (previousKind == HWJ_PossessionKind.Live
            && exitReason == HWJ_PossessedBodyExitReason.HpDepleted)
        {
            previousMentalState?.BlockPossessionPermanently();
            removedCollapsedBody = bodyController != null
                && bodyController.RemovePossessedBody(previousBodyResolver);
        }

        possessionSystem.ClearRegisteredPossessionState();
        possessionSystem.RuntimeBodySystem?.ClearCurrentBodyState(
            exitReason == HWJ_PossessedBodyExitReason.HpDepleted
            || exitReason == HWJ_PossessedBodyExitReason.DecayDepleted);
        bodyController?.RestoreOwnerCollider();
        visualController?.RestoreOwnerVisual();

        if (refreshStatus)
        {
            possessionSystem.RuntimeStatus?.RefreshCurrentHpFromData(refillToMax);
        }

        if (saveSnapshot)
        {
            possessionSystem.SaveRuntimeSnapshot();
        }

        if (!hadActiveBody)
        {
            return;
        }

        string endMessage = ResolveEndMessage(
            previousKind,
            exitReason,
            restoredOriginalBody,
            removedCollapsedBody);

        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(
                possessionSystem,
                previousBodyResolver,
                false,
                endMessage));
    }

    private bool CanExitManually()
    {
        if (possessionSystem == null)
        {
            return false;
        }

        if (!allowManualSoulExit)
        {
            possessionSystem.StoreResultMessage(
                "Exit possession failed: manual soul exit is disabled.");
            return false;
        }

        if (!possessionSystem.HasActivePossessedBody)
        {
            possessionSystem.StoreResultMessage(
                "Exit possession failed: no active possessed body.");
            return false;
        }

        if (possessionSystem.SoulSystem == null
            || possessionSystem.SoulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            possessionSystem.StoreResultMessage(
                "Exit possession failed: player is not in Body state.");
            return false;
        }

        if (possessionSystem.RuntimeStatus != null
            && possessionSystem.RuntimeStatus.IsDead)
        {
            possessionSystem.StoreResultMessage(
                "Exit possession failed: player is dead.");
            return false;
        }

        return true;
    }

    private static string ResolveEndMessage(
        HWJ_PossessionKind previousKind,
        HWJ_PossessedBodyExitReason exitReason,
        bool restoredOriginalBody,
        bool removedCollapsedBody)
    {
        switch (exitReason)
        {
            case HWJ_PossessedBodyExitReason.MentalDepleted:
                return previousKind == HWJ_PossessionKind.Live && restoredOriginalBody
                    ? "몬스터 정신력이 0이 되어 적대 상태로 복귀했습니다. 해당 몬스터는 다시 빙의할 수 없습니다."
                    : "빙의체 정신력이 고갈되어 유령 상태로 복귀했습니다.";

            case HWJ_PossessedBodyExitReason.DecayDepleted:
                return removedCollapsedBody
                    ? "시체 부패가 최대치에 도달하여 육체가 붕괴했습니다."
                    : "시체 부패가 최대치에 도달하여 빙의가 해제되었습니다.";

            case HWJ_PossessedBodyExitReason.HpDepleted:
                return removedCollapsedBody
                    ? "빙의체 체력이 0이 되어 육체가 제거되었으며 다시 빙의할 수 없습니다."
                    : "빙의체 체력이 0이 되어 빙의가 해제되었습니다.";

            case HWJ_PossessedBodyExitReason.ManualExit:
                return previousKind == HWJ_PossessionKind.Live && restoredOriginalBody
                    ? "빙의를 수동 해제하여 몬스터가 적대 상태로 복귀했습니다."
                    : "빙의를 수동 해제했습니다.";

            default:
                return "빙의체가 해제되었습니다.";
        }
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (bodyController == null)
        {
            bodyController = GetComponent<HWJ_PossessionBodyController>();
        }

        if (visualController == null)
        {
            visualController = GetComponent<HWJ_PossessionVisualController>();
        }
    }
}
