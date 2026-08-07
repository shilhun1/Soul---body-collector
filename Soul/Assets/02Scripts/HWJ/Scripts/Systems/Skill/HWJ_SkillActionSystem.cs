using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SkillActionDataSO를 실제 전투 행동으로 실행하는 공통 스킬 시스템입니다.
/// 플레이어, 빙의 상태, 몬스터 AI가 같은 스킬 데이터를 사용하도록 ID 조회, 쿨타임, 모션, 판정을 한 곳에서 처리합니다.
/// </summary>
public partial class HWJ_SkillActionSystem : MonoBehaviour
{
    private const float MinimumProjectileDistance = 30f;
    private const float DefaultProjectileSpeed = 24f;
    private const float MinimumDashSpeed = 15f;

    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CombatExecutionSystem combatExecutionSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_SkillUnlockSystem playerSkillUnlock;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private HWJ_GameplayDatabaseSO database;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HWJ_SkillActionDataSO[] localSkillActions;
    [SerializeField] private Vector2 actionEffectSpawnOffset = new Vector2(0.8f, 0.15f);
    [SerializeField] private bool mirrorActionEffectByFacing = true;
    [SerializeField] private bool useGameplaySkillRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO skillUseExecutionCore;
    [SerializeField] private string skillUseExecutionCoreId = "skill_use_execution";
    [SerializeField] private HWJ_GameplayRuleSO skillUseRule;
    [SerializeField] private string skillUseRuleId = "skill_can_use";
    [SerializeField] private string lastSkillResult;
    [SerializeField] private float lastDamageApplied;

    private readonly Dictionary<string, float> nextUseTimes = new Dictionary<string, float>();
    private Coroutine movementRoutine;
    private Coroutine motionRoutine;
    private Coroutine projectileRoutine;
    private Coroutine actionRoutine;
    private float navigationBlockEndTime;
    private bool skillMovementControlsVelocity;

    public string LastSkillResult => lastSkillResult;
    public float LastDamageApplied => lastDamageApplied;
    public bool IsNavigationBlocked => Time.time < navigationBlockEndTime;
    public bool ShouldStopNavigationMovement => IsNavigationBlocked && !skillMovementControlsVelocity;

    private void Awake()
    {
        CacheReferences();
    }

    /// <summary>
    /// 스킬 ID로 SkillActionDataSO를 찾아 실행합니다.
    /// 플레이어 기본 공격이나 외부 시스템이 스킬 ID만 알고 있을 때 사용합니다.
    /// </summary>
    public bool TryUseSkill(string skillActionId)
    {
        return TryUseSkill(skillActionId, null);
    }

    /// <summary>
    /// 스킬 ID와 대상 위치를 함께 넘겨 실행합니다.
    /// 몬스터 AI는 대상 방향 돌진과 사거리 판정을 위해 target을 전달합니다.
    /// </summary>
    public bool TryUseSkill(string skillActionId, Transform target)
    {
        if (!TryGetSkillAction(skillActionId, out HWJ_SkillActionDataSO skillAction))
        {
            lastSkillResult = $"Skill failed: missing action data for {skillActionId}.";
            return false;
        }

        return TryUseSkill(skillAction, target);
    }

    /// <summary>
    /// EnemyTypeDataSO의 SkillCycle에 들어간 스킬 항목을 실행합니다.
    /// 스킬 정의 엔트리의 쿨타임 값이 0보다 크면 SO 기본 쿨타임보다 우선합니다.
    /// </summary>
    public bool TryUseSkillEntry(HWJ_SkillEntryData definedSkillEntry, Transform target)
    {
        if (definedSkillEntry == null || string.IsNullOrEmpty(definedSkillEntry.skillId))
        {
            lastSkillResult = "Skill failed: missing skill entry.";
            return false;
        }

        if (!IsSkillEntryWeaponMatched(definedSkillEntry))
        {
            lastSkillResult = $"Skill failed: {definedSkillEntry.skillId} weapon mismatch.";
            return false;
        }

        if (!TryGetSkillAction(definedSkillEntry.skillId, out HWJ_SkillActionDataSO skillAction))
        {
            lastSkillResult = $"Skill failed: missing action data for {definedSkillEntry.skillId}.";
            return false;
        }

        float cooldownOverride = definedSkillEntry.cooldownSeconds > 0f
            ? definedSkillEntry.cooldownSeconds
            : -1f;

        return TryUseSkill(skillAction, target, cooldownOverride);
    }

    /// <summary>
    /// 이미 참조된 SkillActionDataSO를 직접 실행합니다.
    /// 보스 패턴처럼 SO 배열을 직접 들고 있는 시스템에서 사용합니다.
    /// </summary>
    public bool TryUseSkill(HWJ_SkillActionDataSO skillAction)
    {
        return TryUseSkill(skillAction, null);
    }

    public bool TryUseSkill(HWJ_SkillActionDataSO skillAction, Transform target)
    {
        return TryUseSkill(skillAction, target, -1f);
    }

    public bool TryUseSkill(HWJ_SkillActionDataSO skillAction, Transform target, float cooldownOverride)
    {
        return TryUseSkillInternal(skillAction, target, cooldownOverride, false, Vector2.zero);
    }

    public bool TryUseSkill(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        float cooldownOverride,
        Vector2 lockedDirection)
    {
        return TryUseSkillInternal(skillAction, target, cooldownOverride, true, lockedDirection);
    }

    private bool TryUseSkillInternal(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        float cooldownOverride,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        CacheReferences();
        lastDamageApplied = 0f;

        if (skillAction == null || dataResolver == null)
        {
            lastSkillResult = "Skill failed: missing skill action or data resolver.";
            return false;
        }

        if (skillAction.ActionType != HWJ_SkillActionType.Dash && !IsSkillRuleSatisfied(skillAction))
        {
            if (string.IsNullOrEmpty(lastSkillResult))
            {
                lastSkillResult = $"Skill failed: {skillAction.SkillActionId} gameplay rule.";
            }

            return false;
        }

        if (runtimeStatus != null
            && skillAction.ActionType != HWJ_SkillActionType.Dash
            && (!runtimeStatus.CanAttack || runtimeStatus.IsHitStunned))
        {
            lastSkillResult = $"Skill failed: {skillAction.SkillActionId} action locked.";
            return false;
        }

        if (!skillAction.CanUseWithWeapon(GetCurrentWeaponType()))
        {
            lastSkillResult = $"Skill failed: {skillAction.SkillActionId} weapon mismatch.";
            return false;
        }

        if (!IsTrackedPlayerSkillUnlocked(skillAction.SkillActionId))
        {
            return false;
        }

        if (!IsSkillReady(skillAction.SkillActionId))
        {
            lastSkillResult = $"Skill failed: {skillAction.SkillActionId} cooldown.";
            return false;
        }

        float cooldownSeconds = cooldownOverride > 0f
            ? cooldownOverride
            : skillAction.CooldownSeconds;

        if (!string.IsNullOrEmpty(skillAction.SkillActionId))
        {
            nextUseTimes[skillAction.SkillActionId] = Time.time + Mathf.Max(0f, cooldownSeconds);
        }

        ExecuteSkill(skillAction, target, useLockedDirection, lockedDirection);
        return true;
    }

    private bool IsSkillRuleSatisfied(HWJ_SkillActionDataSO skillAction)
    {
        if (!useGameplaySkillRule)
        {
            return true;
        }

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(dataResolver, null)
            .WithSource(this)
            .WithSkill(skillAction);

        if (ResolveSkillExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool corePassed = executionCore.TryExecute(context, out HWJ_RuleExecutionResult executionResult);
            lastSkillResult = executionResult.Message;
            return corePassed;
        }

        HWJ_GameplayRuleSO rule = skillUseRule;

        if (rule == null
            && !string.IsNullOrEmpty(skillUseRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(skillUseRuleId, out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool passed = rule.TryEvaluate(context, out HWJ_RuleEvaluationResult result);
        lastSkillResult = result.Message;
        return passed;
    }

    private bool ResolveSkillExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (skillUseExecutionCore != null)
        {
            executionCore = skillUseExecutionCore;
            return true;
        }

        if (string.IsNullOrEmpty(skillUseExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(skillUseExecutionCoreId, out executionCore);
    }

    /// <summary>
    /// 외부 AI가 스킬을 고르기 전에 쿨타임만 확인할 수 있게 해주는 조회 함수입니다.
    /// </summary>
    public bool IsSkillReady(string skillActionId)
    {
        if (string.IsNullOrEmpty(skillActionId))
        {
            return true;
        }

        return !nextUseTimes.TryGetValue(skillActionId, out float nextUseTime)
            || Time.time >= nextUseTime;
    }

    /// <summary>
    /// Keeps enemy navigation from overwriting charge-up or dash movement for a short skill window.
    /// skillControlsVelocity should be true only while this system is directly setting Rigidbody2D.linearVelocity.
    /// </summary>
    public void BlockNavigationForSkill(float seconds, bool skillControlsVelocity)
    {
        float duration = Mathf.Max(0f, seconds);

        if (duration <= 0f)
        {
            return;
        }

        navigationBlockEndTime = Mathf.Max(navigationBlockEndTime, Time.time + duration);
        skillMovementControlsVelocity = skillControlsVelocity;

        if (!skillControlsVelocity && body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
        }
    }

    public void ReleaseNavigationBlock()
    {
        navigationBlockEndTime = 0f;
        skillMovementControlsVelocity = false;
    }

    public void CancelCurrentAction()
    {
        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
            actionRoutine = null;
        }

        if (projectileRoutine != null)
        {
            StopCoroutine(projectileRoutine);
            projectileRoutine = null;
        }

        runtimeStatus?.CancelAttackAction();
        ReleaseNavigationBlock();
    }

    /// <summary>
    /// 로컬 목록, 직접 연결된 데이터베이스, GameManager 데이터베이스 순서로 스킬 데이터를 찾습니다.
    /// </summary>
    public bool TryGetSkillAction(string skillActionId, out HWJ_SkillActionDataSO skillAction)
    {
        skillAction = null;

        if (string.IsNullOrEmpty(skillActionId))
        {
            return false;
        }

        if (localSkillActions != null)
        {
            for (int i = 0; i < localSkillActions.Length; i++)
            {
                if (localSkillActions[i] != null && localSkillActions[i].SkillActionId == skillActionId)
                {
                    skillAction = localSkillActions[i];
                    return true;
                }
            }
        }

        if (database != null && database.TryGetSkillAction(skillActionId, out skillAction))
        {
            return true;
        }

        return HWJ_GameAccess.TryGetSkillAction(skillActionId, out skillAction);
    }

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (combatExecutionSystem == null)
        {
            combatExecutionSystem = GetComponent<HWJ_CombatExecutionSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (playerSkillUnlock == null)
        {
            playerSkillUnlock = GetComponent<HWJ_SkillUnlockSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }
}
