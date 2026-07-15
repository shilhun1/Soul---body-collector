public static class HWJ_FSMStateUtility
{
    public static HWJ_RuntimeState ToRuntimeState(HWJ_MonsterAIState state)
    {
        switch (state)
        {
            case HWJ_MonsterAIState.Approach:
                return HWJ_RuntimeState.Move;
            case HWJ_MonsterAIState.AttackPrepare:
            case HWJ_MonsterAIState.Attack:
                return HWJ_RuntimeState.Attack;
            case HWJ_MonsterAIState.HitStun:
                return HWJ_RuntimeState.Hit;
            case HWJ_MonsterAIState.Dead:
                return HWJ_RuntimeState.Dead;
            default:
                return HWJ_RuntimeState.Idle;
        }
    }

    public static HWJ_RuntimeState ToRuntimeState(HWJ_BossFSMState state)
    {
        switch (state)
        {
            case HWJ_BossFSMState.Chase:
                return HWJ_RuntimeState.Move;
            case HWJ_BossFSMState.Attack:
                return HWJ_RuntimeState.Attack;
            case HWJ_BossFSMState.PhaseTransition:
            case HWJ_BossFSMState.Groggy:
                return HWJ_RuntimeState.Hit;
            case HWJ_BossFSMState.Dead:
                return HWJ_RuntimeState.Dead;
            default:
                return HWJ_RuntimeState.Idle;
        }
    }

    public static float GetDefaultMonsterStateDuration(
        HWJ_MonsterAIState state,
        HWJ_EnemyTypeDataSO enemyData)
    {
        if (enemyData == null || enemyData.AI == null)
        {
            return 0f;
        }

        switch (state)
        {
            case HWJ_MonsterAIState.Idle:
                return enemyData.AI.idleSeconds;
            case HWJ_MonsterAIState.Detect:
                return enemyData.AI.detectSeconds;
            case HWJ_MonsterAIState.AttackPrepare:
                return enemyData.AI.attackPrepareSeconds;
            case HWJ_MonsterAIState.Recovery:
                return enemyData.AI.attackRecoverySeconds;
            case HWJ_MonsterAIState.Repath:
                return enemyData.AI.repathSeconds;
            default:
                return 0f;
        }
    }
}
