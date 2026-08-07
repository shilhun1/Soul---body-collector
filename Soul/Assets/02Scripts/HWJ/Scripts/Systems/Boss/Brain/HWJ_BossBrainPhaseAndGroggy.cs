using UnityEngine;

/// <summary>
/// Controls groggy entry, phase identifiers, and super-armor state.
/// This partial belongs to the single HWJ_BossBrainSystem component.
/// </summary>
public partial class HWJ_BossBrainSystem
{
    private void EnterGroggy(HWJ_BossFSMData fsm)
    {
        groggyHitCount = 0;
        groggyHitWindowTimer = 0f;
        groggyTimer = Mathf.Max(0.01f, fsm.groggyDurationSeconds);
        ApplyGroggyEntryEffects(fsm);
        SetBossState(HWJ_BossFSMState.Groggy);
        StopHorizontalMovement();
    }

    private void ApplyGroggyEntryEffects(HWJ_BossFSMData fsm)
    {
        if (fsm == null || fsm.cancelActionsOnGroggy)
        {
            CancelCurrentBossActions();
        }

        if (fsm == null || fsm.clearHitReactionLimitOnGroggy)
        {
            runtimeStatus?.ClearHitReactionLimit();
        }
    }

    private bool UpdateGroggy()
    {
        if (groggyHitWindowTimer > 0f)
        {
            groggyHitWindowTimer = Mathf.Max(0f, groggyHitWindowTimer - Time.deltaTime);

            if (groggyHitWindowTimer <= 0f)
            {
                groggyHitCount = 0;
            }
        }

        if (currentState != HWJ_BossFSMState.Groggy)
        {
            return false;
        }

        StopHorizontalMovement();
        groggyTimer -= Time.deltaTime;

        if (groggyTimer <= 0f)
        {
            if (TryGetBossData(out HWJ_BossTypeDataSO bossData) && bossData.FSM.knockbackOnGroggyEnd)
            {
                KnockbackTarget(bossData.FSM.groggyEndKnockbackPower);
            }

            SetBossState(HWJ_BossFSMState.Idle);
        }

        return true;
    }

    private int ResolvePhaseIndex()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData)
            || bossData.Phases == null
            || bossData.Phases.Length == 0
            || runtimeStatus == null
            || runtimeStatus.MaxHp <= 0f)
        {
            return -1;
        }

        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;
        int bestIndex = -1;
        float bestThreshold = float.MaxValue;

        for (int i = 0; i < bossData.Phases.Length; i++)
        {
            HWJ_BossPhaseData phase = bossData.Phases[i];

            if (phase == null || hpRatio > phase.startHpRatio)
            {
                continue;
            }

            if (phase.startHpRatio < bestThreshold)
            {
                bestThreshold = phase.startHpRatio;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private string GetPhaseId(int phaseIndex)
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData)
            || bossData.Phases == null
            || phaseIndex < 0
            || phaseIndex >= bossData.Phases.Length
            || bossData.Phases[phaseIndex] == null)
        {
            return null;
        }

        return bossData.Phases[phaseIndex].phaseId;
    }

    private bool IsSuperArmorActive()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return false;
        }

        return (currentState == HWJ_BossFSMState.Attack && bossData.FSM.superArmorDuringAttack)
            || (currentState == HWJ_BossFSMState.PhaseTransition && bossData.FSM.superArmorDuringPhaseTransition);
    }

}
