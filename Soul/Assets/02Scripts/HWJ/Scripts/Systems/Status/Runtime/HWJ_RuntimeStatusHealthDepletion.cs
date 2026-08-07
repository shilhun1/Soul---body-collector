using UnityEngine;

/// <summary>
/// Routes zero-HP handling to possession, collapse, soul, or generic death systems.
/// It is called by HWJ_RuntimeStatusSystem after damage changes the active HP pool.
/// </summary>
public partial class HWJ_RuntimeStatusSystem
{
    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerStatus == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }

    private bool TryHandleHealthDepleted()
    {
        ResolveHealthDepletionHandler();
        return healthDepletionHandler != null
            && healthDepletionHandler.TryHandleHealthDepleted(this);
    }

    private bool IsHealthDepletionDeferred()
    {
        ResolveHealthDepletionHandler();
        return healthDepletionHandler != null
            && healthDepletionHandler.IsHealthDepletionHandled;
    }

    private void ResolveHealthDepletionHandler()
    {
        if (healthDepletionHandler is Object handlerObject && handlerObject != null)
        {
            return;
        }

        healthDepletionHandler = null;
        MonoBehaviour[] localBehaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < localBehaviours.Length; i++)
        {
            if (localBehaviours[i] is HWJ_IHealthDepletionHandler candidate)
            {
                healthDepletionHandler = candidate;
                return;
            }
        }
    }

    private void HandleEmptyHp()
    {
        if (soulSystem == null)
        {
            SetState(HWJ_RuntimeState.Dead);
            motionSystem?.PlayDead();
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            if (collapseSystem == null)
            {
                collapseSystem = GetComponent<HWJ_CollapseSystem>();
            }

            if (collapseSystem != null)
            {
                collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.HpDepleted);
                return;
            }

            possessedBodySystem?.MarkCurrentBodyCollapsed();
            soulSystem.EnterSoulState(false, HWJ_PossessedBodyExitReason.HpDepleted);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            soulSystem.EnterDeadState();
            return;
        }

        SetState(HWJ_RuntimeState.Dead);
        motionSystem?.PlayDead();
    }

    private bool TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
    {
        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        bodyState = null;
        return possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out bodyState)
            && bodyState != null;
    }
}
