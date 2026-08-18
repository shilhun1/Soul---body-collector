using System.Collections;
using UnityEngine;

/// <summary>
/// 적 몬스터의 자동 공격과 스킬 사용을 담당합니다.
/// EnemyTypeDataSO의 SkillCycle을 기준으로 사거리와 쿨타임을 검사하고, 사용할 스킬이 없으면 기본 공격으로 fallback합니다.
/// </summary>
public class HWJ_EnemyAttackSystem : MonoBehaviour
{
    private const int DefaultMonsterSkillCycleCount = 3;
    private const float DefaultMonsterSkillCooldownSeconds = 3f;
    private const float DefaultMonsterSkillCycleDelaySeconds = 5f;
    private const float DefaultMonsterBasicAttackIntervalSeconds = 1.5f;

    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatExecutionSystem combatExecutionSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_MonsterAISystem monsterAI;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Animator animator;
    [Header("타겟")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool autoAttackWhenNoBehaviorDriver = true;
    [SerializeField] private bool attackOnlyBodyState = true;
    [Header("스킬 예고")]
    [SerializeField] private bool useSkillCycle = true;
    [SerializeField] private bool showSkillWarning = true;
    [SerializeField] private float skillWarningDelaySeconds = 1f;
    [SerializeField] private Color skillWarningColor = new Color(1f, 0.15f, 0.05f, 0.85f);
    [SerializeField] private float skillWarningLineWidth = 0.06f;
    [Header("기본 공격과 스킬 순서")]
    [SerializeField] private float fallbackAttackRange = 1.2f;
    [SerializeField] private float fallbackAttackIntervalSeconds = DefaultMonsterBasicAttackIntervalSeconds;
    [SerializeField] private int fallbackSkillCycleCount = DefaultMonsterSkillCycleCount;
    [SerializeField] private float fallbackMonsterSkillCooldownSeconds = DefaultMonsterSkillCooldownSeconds;
    [SerializeField] private float fallbackSkillCycleDelaySeconds = DefaultMonsterSkillCycleDelaySeconds;
    [SerializeField] private float targetSearchIntervalSeconds = 0.5f;
    [Header("디버그")]
    [SerializeField] private string lastAttackResult;
    [SerializeField] private float lastDamageApplied;

    private float nextBasicAttackTime;
    private float nextSkillTime;
    private float nextTargetSearchTime;
    private int nextSkillCycleIndex;
    private bool isPreparingSkill;
    private Coroutine skillPrepareRoutine;
    private HWJ_RootObjectDataResolver pendingBasicAttackTarget;
    private Coroutine basicAttackFallbackRoutine;
    private bool pendingBasicAttackHitApplied;
    private float nextContactDamageTime;

    public string LastAttackResult => lastAttackResult;
    public float LastDamageApplied => lastDamageApplied;
    public bool HasPendingBasicAttack => pendingBasicAttackTarget != null;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnDisable()
    {
        CancelPreparedBasicAttack();

        if (skillPrepareRoutine != null)
        {
            StopCoroutine(skillPrepareRoutine);
            skillPrepareRoutine = null;
        }

        isPreparingSkill = false;
    }

    private void Update()
    {
        if (monsterAI == null)
        {
            monsterAI = GetComponent<HWJ_MonsterAISystem>();
        }

        if (monsterAI != null && monsterAI.DrivesBehavior)
        {
            return;
        }

        if (target == null && autoFindPlayerTarget && Time.time >= nextTargetSearchTime)
        {
            target = FindPlayerTarget();
            nextTargetSearchTime = Time.time + targetSearchIntervalSeconds;
        }

        if (autoAttackWhenNoBehaviorDriver)
        {
            TryAutoAttack();
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public bool TryAutoAttack()
    {
        CacheReferences();
        lastDamageApplied = 0f;

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastAttackResult = "Attack failed: enemy is dead.";
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.CurrentState == HWJ_RuntimeState.Hit)
        {
            lastAttackResult = "Attack failed: enemy is hit.";
            return false;
        }

        if (runtimeStatus != null && !runtimeStatus.CanAttack)
        {
            lastAttackResult = "Attack failed: action locked.";
            return false;
        }

        if (isPreparingSkill)
        {
            lastAttackResult = "Attack waiting: skill warning is active.";
            return false;
        }

        if (skillActionSystem != null && skillActionSystem.IsNavigationBlocked)
        {
            lastAttackResult = "Attack waiting: skill action is active.";
            return false;
        }

        if (target == null)
        {
            lastAttackResult = "Attack failed: missing target.";
            return false;
        }

        if (attackOnlyBodyState && !CanAttackTargetState())
        {
            lastAttackResult = "Attack failed: target is not in body state.";
            return false;
        }

        HWJ_RootObjectDataResolver targetResolver = FindTargetResolver();

        if (targetResolver == null)
        {
            lastAttackResult = "Attack failed: target has no data resolver.";
            return false;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (TryUseSkillCycle(distance))
        {
            runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
            return true;
        }

        string skillCycleFailureResult = lastAttackResult;

        if (Time.time < nextBasicAttackTime)
        {
            lastAttackResult = "Basic attack failed: attack interval.";
            return false;
        }

        float attackRange = GetAttackRange();

        if (distance > attackRange)
        {
            lastAttackResult = !string.IsNullOrEmpty(skillCycleFailureResult)
                && skillCycleFailureResult.StartsWith("Skill", System.StringComparison.Ordinal)
                ? $"{skillCycleFailureResult} Basic fallback failed: target out of range."
                : "Attack failed: target out of range.";
            return false;
        }

        nextBasicAttackTime = Time.time + GetAttackIntervalSeconds();
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        return TryExecuteBasicAttack(targetResolver);
    }

    private bool TryExecuteBasicAttack(HWJ_RootObjectDataResolver targetResolver)
    {
        HWJ_EnemyBasicAttackData attackData = GetBasicAttackData();
        HWJ_EnemyBasicAttackMode attackMode = attackData != null
            ? attackData.mode
            : HWJ_EnemyBasicAttackMode.Immediate;

        switch (attackMode)
        {
            case HWJ_EnemyBasicAttackMode.Contact:
                lastAttackResult = "Contact attack is armed and applies damage only while touching the player.";
                return true;
            case HWJ_EnemyBasicAttackMode.AnimationEvent:
                return BeginPreparedBasicAttack(targetResolver, attackData);
            default:
                return ExecuteBasicAttackDamage(targetResolver, attackData, true);
        }
    }

    private bool BeginPreparedBasicAttack(
        HWJ_RootObjectDataResolver targetResolver,
        HWJ_EnemyBasicAttackData attackData)
    {
        if (pendingBasicAttackTarget != null)
        {
            lastAttackResult = "Basic attack failed: an animation-timed attack is already pending.";
            return false;
        }

        pendingBasicAttackTarget = targetResolver;
        pendingBasicAttackHitApplied = false;
        string motionKey = attackData != null ? attackData.motionKey : null;

        if (motionSystem != null)
        {
            if (string.IsNullOrWhiteSpace(motionKey))
            {
                motionSystem.PlayAttack(null);
            }
            else
            {
                motionSystem.PlayMotionKey(motionKey);
            }
        }

        bool hasAnimationController = animator != null && animator.runtimeAnimatorController != null;

        if (!hasAnimationController)
        {
            float delaySeconds = attackData != null
                ? Mathf.Max(0f, attackData.fallbackHitDelaySeconds)
                : 0f;
            basicAttackFallbackRoutine = StartCoroutine(
                TimedBasicAttackFallbackRoutine(delaySeconds));
        }

        lastAttackResult = hasAnimationController
            ? "Animation-timed basic attack started. Waiting for the animation hit event."
            : "Basic attack started with the no-animation fallback timer.";
        return true;
    }

    private IEnumerator TimedBasicAttackFallbackRoutine(float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        ApplyPreparedBasicAttackHit();
        pendingBasicAttackTarget = null;
        pendingBasicAttackHitApplied = false;
        basicAttackFallbackRoutine = null;
    }

    /// <summary>
    /// 공격 애니메이션의 실제 타격 프레임에서 Animation Event로 호출합니다.
    /// 한 공격 요청에서는 한 번만 피해가 적용됩니다.
    /// </summary>
    public bool ApplyPreparedBasicAttackHit()
    {
        if (pendingBasicAttackTarget == null || pendingBasicAttackHitApplied)
        {
            lastAttackResult = "Animation hit ignored: no pending basic attack.";
            return false;
        }

        HWJ_EnemyBasicAttackData attackData = GetBasicAttackData();
        float tolerance = attackData != null ? Mathf.Max(0f, attackData.hitRangeTolerance) : 0f;
        float distance = Vector2.Distance(transform.position, pendingBasicAttackTarget.transform.position);

        if (runtimeStatus != null && (runtimeStatus.IsDead || !runtimeStatus.CanAttack))
        {
            lastAttackResult = "Animation hit canceled: attacker cannot attack.";
            return false;
        }

        if (!CanAttackTargetState(pendingBasicAttackTarget.transform)
            || distance > GetAttackRange() + tolerance)
        {
            lastAttackResult = "Animation hit missed: target left the valid range or state.";
            pendingBasicAttackHitApplied = true;
            return false;
        }

        pendingBasicAttackHitApplied = true;
        return ExecuteBasicAttackDamage(pendingBasicAttackTarget, attackData, false);
    }

    /// <summary>
    /// 공격 애니메이션의 종료 프레임에서 Animation Event로 호출합니다.
    /// </summary>
    public void CompletePreparedBasicAttack()
    {
        if (basicAttackFallbackRoutine != null)
        {
            StopCoroutine(basicAttackFallbackRoutine);
            basicAttackFallbackRoutine = null;
        }

        pendingBasicAttackTarget = null;
        pendingBasicAttackHitApplied = false;
    }

    public void CancelPreparedBasicAttack()
    {
        CompletePreparedBasicAttack();
        lastAttackResult = "Prepared basic attack was canceled.";
    }

    private bool ExecuteBasicAttackDamage(
        HWJ_RootObjectDataResolver targetResolver,
        HWJ_EnemyBasicAttackData attackData,
        bool shouldPlayMotion)
    {
        float multiplier = attackData != null ? Mathf.Max(0f, attackData.damageMultiplier) : 1f;

        if (combatExecutionSystem != null
            && combatExecutionSystem.TryExecuteAttackTo(
                targetResolver,
                multiplier,
                attackData != null ? attackData.motionKey : null,
                shouldPlayMotion))
        {
            lastDamageApplied = combatExecutionSystem.LastDamageApplied;
            lastAttackResult = combatExecutionSystem.LastExecutionResult;
            return true;
        }

        lastAttackResult = combatExecutionSystem != null
            ? combatExecutionSystem.LastExecutionResult
            : "Attack failed: missing combat execution system.";
        return false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyContactDamage(collision != null ? collision.collider : null);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    private void TryApplyContactDamage(Collider2D other)
    {
        HWJ_EnemyBasicAttackData attackData = GetBasicAttackData();

        if (attackData == null
            || attackData.mode != HWJ_EnemyBasicAttackMode.Contact
            || other == null
            || Time.time < nextContactDamageTime
            || (runtimeStatus != null && (runtimeStatus.IsDead || !runtimeStatus.CanAttack)))
        {
            return;
        }

        HWJ_RootObjectDataResolver targetResolver =
            other.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null
            || !CanAttackTargetState(targetResolver.transform))
        {
            return;
        }

        nextContactDamageTime = Time.time
            + Mathf.Max(0.01f, attackData.contactDamageIntervalSeconds);
        ExecuteBasicAttackDamage(targetResolver, attackData, false);
    }

    private bool TryUseSkillCycle(float distanceToTarget)
    {
        if (!useSkillCycle)
        {
            lastAttackResult = "Skill skipped: fixed skill cycle disabled.";
            return false;
        }

        if (skillActionSystem == null)
        {
            lastAttackResult = "Skill skipped: missing skill action system.";
            return false;
        }

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            lastAttackResult = "Skill skipped: missing enemy type data.";
            return false;
        }

        HWJ_SkillSetData skillCycle = enemyData.SkillCycle;

        if (skillCycle == null || skillCycle.skills == null)
        {
            lastAttackResult = "Skill skipped: missing fixed skill cycle data.";
            return false;
        }

        int skillCycleCount = GetSkillCycleCount(skillCycle);

        if (skillCycleCount <= 0)
        {
            lastAttackResult = "Skill skipped: fixed skill cycle is empty.";
            return false;
        }

        if (Time.time < nextSkillTime)
        {
            lastAttackResult = "Skill waiting: fixed skill sequence cooldown.";
            return false;
        }

        nextSkillCycleIndex = Mathf.Clamp(nextSkillCycleIndex, 0, skillCycleCount - 1);
        HWJ_SkillEntryData skillEntry = skillCycle.skills[nextSkillCycleIndex];

        if (!CanTrySkillEntry(skillEntry))
        {
            lastAttackResult = $"Skill failed: invalid fixed skill slot {nextSkillCycleIndex + 1}.";
            return false;
        }

        if (!skillActionSystem.TryGetSkillAction(skillEntry.skillId, out HWJ_SkillActionDataSO skillAction))
        {
            lastAttackResult = $"Skill failed: missing action data for fixed skill slot {nextSkillCycleIndex + 1}.";
            return false;
        }

        float skillRange = skillAction.Range > 0f ? skillAction.Range : GetAttackRange();

        if (distanceToTarget > skillRange)
        {
            lastAttackResult = $"Skill failed: fixed skill slot {nextSkillCycleIndex + 1} target out of range.";
            return false;
        }

        if (!skillActionSystem.IsSkillReady(skillEntry.skillId))
        {
            lastAttackResult = $"Skill failed: fixed skill slot {nextSkillCycleIndex + 1} cooldown.";
            return false;
        }

        StartPreparedSkill(skillEntry, skillAction, target, nextSkillCycleIndex, skillCycleCount);
        lastAttackResult = $"Preparing fixed skill {nextSkillCycleIndex + 1}: {skillEntry.skillId}.";
        return true;
    }

    private void StartPreparedSkill(
        HWJ_SkillEntryData skillEntry,
        HWJ_SkillActionDataSO skillAction,
        Transform skillTarget,
        int skillCycleIndex,
        int skillCycleCount)
    {
        if (skillPrepareRoutine != null)
        {
            StopCoroutine(skillPrepareRoutine);
        }

        skillPrepareRoutine = StartCoroutine(PreparedSkillRoutine(
            skillEntry,
            skillAction,
            skillTarget,
            skillCycleIndex,
            skillCycleCount));
    }

    /// <summary>
    /// 몬스터 전용 스킬 예고 흐름입니다.
    /// 먼저 경고 범위를 표시하고, 1초 뒤에 대상과 사거리를 다시 확인한 다음 실제 스킬을 실행합니다.
    /// </summary>
    private IEnumerator PreparedSkillRoutine(
        HWJ_SkillEntryData skillEntry,
        HWJ_SkillActionDataSO skillAction,
        Transform skillTarget,
        int skillCycleIndex,
        int skillCycleCount)
    {
        isPreparingSkill = true;
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        skillActionSystem?.BlockNavigationForSkill(Mathf.Max(0f, skillWarningDelaySeconds), false);
        float lockedDirectionX = ResolveTargetDirection(skillTarget);
        Vector2 lockedDirection = new Vector2(lockedDirectionX, 0f);
        ShowSkillWarning(skillAction, lockedDirectionX);

        float delaySeconds = Mathf.Max(0f, skillWarningDelaySeconds);

        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        if (!CanCompletePreparedSkill(skillAction, skillTarget))
        {
            skillActionSystem?.ReleaseNavigationBlock();
            isPreparingSkill = false;
            skillPrepareRoutine = null;
            yield break;
        }

        float cooldownSeconds = ResolveMonsterSkillCooldownSeconds(skillEntry, skillAction);

        if (skillActionSystem != null && skillActionSystem.TryUseSkill(skillAction, skillTarget, cooldownSeconds, lockedDirection))
        {
            lastDamageApplied = skillActionSystem.LastDamageApplied;
            lastAttackResult = skillActionSystem.LastSkillResult;
            RegisterCompletedSkillCycleStep(skillCycleIndex, skillCycleCount, cooldownSeconds);
        }
        else
        {
            lastAttackResult = skillActionSystem != null
                ? skillActionSystem.LastSkillResult
                : "Skill failed: missing skill action system.";
        }

        isPreparingSkill = false;
        skillPrepareRoutine = null;
    }

    private float ResolveMonsterSkillCooldownSeconds(HWJ_SkillEntryData skillEntry, HWJ_SkillActionDataSO skillAction)
    {
        if (skillEntry != null && skillEntry.cooldownSeconds > 0f)
        {
            return skillEntry.cooldownSeconds;
        }

        if (skillAction != null && skillAction.CooldownSeconds > 0f)
        {
            return skillAction.CooldownSeconds;
        }

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.AI != null
            && enemyData.AI.defaultSkillCooldownSeconds > 0f)
        {
            return enemyData.AI.defaultSkillCooldownSeconds;
        }

        return Mathf.Max(0f, fallbackMonsterSkillCooldownSeconds);
    }

    private float ResolveSkillCycleDelaySeconds()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.AI != null
            && enemyData.AI.skillCycleResetDelaySeconds > 0f)
        {
            return enemyData.AI.skillCycleResetDelaySeconds;
        }

        return Mathf.Max(0f, fallbackSkillCycleDelaySeconds);
    }

    private int GetSkillCycleCount(HWJ_SkillSetData skillCycle)
    {
        if (skillCycle == null || skillCycle.skills == null)
        {
            return 0;
        }

        int requestedCount = DefaultMonsterSkillCycleCount;

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.AI != null
            && enemyData.AI.skillCycleCount > 0)
        {
            requestedCount = enemyData.AI.skillCycleCount;
        }
        else if (fallbackSkillCycleCount > 0)
        {
            requestedCount = fallbackSkillCycleCount;
        }

        return Mathf.Clamp(requestedCount, 0, skillCycle.skills.Length);
    }

    private void RegisterCompletedSkillCycleStep(int skillCycleIndex, int skillCycleCount, float cooldownSeconds)
    {
        if (skillCycleCount <= 0)
        {
            nextSkillCycleIndex = 0;
            nextSkillTime = Time.time;
            return;
        }

        bool isLastSkillInCycle = skillCycleIndex >= skillCycleCount - 1;

        if (isLastSkillInCycle)
        {
            nextSkillCycleIndex = 0;
            nextSkillTime = Time.time + ResolveSkillCycleDelaySeconds();
            return;
        }

        nextSkillCycleIndex = Mathf.Clamp(skillCycleIndex + 1, 0, skillCycleCount - 1);
        nextSkillTime = Time.time + Mathf.Max(0f, cooldownSeconds);
    }

    private bool CanCompletePreparedSkill(HWJ_SkillActionDataSO skillAction, Transform skillTarget)
    {
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastAttackResult = "Skill canceled: enemy is dead.";
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.CurrentState == HWJ_RuntimeState.Hit)
        {
            lastAttackResult = "Skill canceled: enemy is hit.";
            return false;
        }

        if (skillTarget == null)
        {
            lastAttackResult = "Skill canceled: missing target.";
            return false;
        }

        if (attackOnlyBodyState && !CanAttackTargetState(skillTarget))
        {
            lastAttackResult = "Skill canceled: target is not in body state.";
            return false;
        }

        if (skillActionSystem != null
            && skillAction != null
            && !skillActionSystem.IsSkillReady(skillAction.SkillActionId))
        {
            lastAttackResult = "Skill canceled: cooldown.";
            return false;
        }

        return true;
    }

    private void ShowSkillWarning(HWJ_SkillActionDataSO skillAction, float lockedDirectionX)
    {
        if (!showSkillWarning || skillAction == null || skillWarningDelaySeconds <= 0f)
        {
            return;
        }

        float direction = lockedDirectionX == 0f ? ResolveTargetDirection(null) : lockedDirectionX;
        Color warningColor = GetWarningColor(skillAction);

        switch (skillAction.ActionType)
        {
            case HWJ_SkillActionType.Dash:
                ShowDashSkillWarning(skillAction, direction, warningColor);
                break;
            case HWJ_SkillActionType.Projectile:
                ShowProjectileSkillWarning(skillAction, direction, warningColor);
                break;
            case HWJ_SkillActionType.Melee:
                ShowMeleeSkillWarning(skillAction, direction, warningColor);
                break;
            default:
                ShowCircleSkillWarning(skillAction, warningColor);
                break;
        }
    }

    private void ShowDashSkillWarning(HWJ_SkillActionDataSO skillAction, float direction, Color warningColor)
    {
        // Dash skills are shown as a wide arrow so the movement path is readable before the monster moves.
        float length = Mathf.Max(0.1f, skillAction.MoveDistance > 0f ? skillAction.MoveDistance : skillAction.Range);
        float width = Mathf.Max(0.6f, GetWarningRadius(skillAction));

        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            direction,
            length,
            width,
            skillWarningDelaySeconds,
            warningColor,
            skillWarningLineWidth);
    }

    private void ShowProjectileSkillWarning(HWJ_SkillActionDataSO skillAction, float direction, Color warningColor)
    {
        // Projectile skills use a narrow arrow path to separate them from circle-based area attacks.
        float length = Mathf.Max(0.1f, skillAction.Range);
        float width = Mathf.Clamp(GetWarningRadius(skillAction) * 0.4f, 0.25f, 0.9f);

        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            direction,
            length,
            width,
            skillWarningDelaySeconds,
            warningColor,
            skillWarningLineWidth);
    }

    private void ShowMeleeSkillWarning(HWJ_SkillActionDataSO skillAction, float direction, Color warningColor)
    {
        // Melee skills use a forward arc, making slash/thrust attacks distinct from dash and full area attacks.
        HWJ_SkillWarningIndicator.ShowForwardArc(
            transform.position,
            direction,
            GetWarningRadius(skillAction),
            skillWarningDelaySeconds,
            warningColor,
            skillWarningLineWidth);
    }

    private void ShowCircleSkillWarning(HWJ_SkillActionDataSO skillAction, Color warningColor)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            transform.position,
            GetWarningRadius(skillAction),
            skillWarningDelaySeconds,
            warningColor,
            skillWarningLineWidth);
    }

    private float GetWarningRadius(HWJ_SkillActionDataSO skillAction)
    {
        return skillAction.HitRange > 0f
            ? skillAction.HitRange
            : Mathf.Max(0.1f, skillAction.Range);
    }

    private Color GetWarningColor(HWJ_SkillActionDataSO skillAction)
    {
        switch (skillAction.ActionType)
        {
            case HWJ_SkillActionType.Dash:
                return new Color(1f, 0.55f, 0.05f, 0.9f);
            case HWJ_SkillActionType.Projectile:
                return new Color(0.15f, 0.75f, 1f, 0.9f);
            case HWJ_SkillActionType.Melee:
                return new Color(1f, 0.2f, 0.15f, 0.9f);
            default:
                return skillWarningColor;
        }
    }

    private float ResolveTargetDirection(Transform skillTarget)
    {
        if (skillTarget != null)
        {
            float direction = Mathf.Sign(skillTarget.position.x - transform.position.x);

            if (direction != 0f)
            {
                return direction;
            }
        }

        float facing = Mathf.Sign(transform.localScale.x);
        return facing == 0f ? 1f : facing;
    }

    private bool CanTrySkillEntry(HWJ_SkillEntryData skillEntry)
    {
        if (skillEntry == null || string.IsNullOrEmpty(skillEntry.skillId))
        {
            return false;
        }

        if (!skillEntry.startsUnlocked)
        {
            return false;
        }

        HWJ_WeaponType weaponType = dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
        return skillEntry.requiredWeaponType == HWJ_WeaponType.None
            || skillEntry.requiredWeaponType == weaponType;
    }

    private HWJ_RootObjectDataResolver FindTargetResolver()
    {
        if (target == null)
        {
            return null;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        return targetResolver;
    }

    private float GetAttackRange()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State.attackRange > 0f)
        {
            return enemyData.State.attackRange;
        }

        return fallbackAttackRange;
    }

    private float GetAttackIntervalSeconds()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.AI != null
            && enemyData.AI.basicAttackIntervalSeconds > 0f)
        {
            return enemyData.AI.basicAttackIntervalSeconds;
        }

        return Mathf.Max(0f, fallbackAttackIntervalSeconds);
    }

    private HWJ_EnemyBasicAttackData GetBasicAttackData()
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return enemyData.BasicAttack;
        }

        return null;
    }

    private bool CanAttackTargetState()
    {
        return CanAttackTargetState(target);
    }

    private bool CanAttackTargetState(Transform checkedTarget)
    {
        if (checkedTarget == null)
        {
            return false;
        }

        HWJ_SoulSystem targetSoul = checkedTarget.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = checkedTarget.GetComponentInParent<HWJ_SoulSystem>();
        }

        return targetSoul == null || targetSoul.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
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

        if (combatExecutionSystem == null)
        {
            combatExecutionSystem = GetComponent<HWJ_CombatExecutionSystem>();

            if (combatExecutionSystem == null)
            {
                combatExecutionSystem = gameObject.AddComponent<HWJ_CombatExecutionSystem>();
            }
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();

            if (skillActionSystem == null)
            {
                skillActionSystem = gameObject.AddComponent<HWJ_SkillActionSystem>();
            }
        }

        if (monsterAI == null)
        {
            monsterAI = GetComponent<HWJ_MonsterAISystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fallbackAttackRange);
    }
}

/// <summary>
/// 몬스터가 스킬을 쓰기 전에 공격 범위를 잠깐 보여주는 런타임 예고 표시입니다.
/// 별도 프리팹 없이 LineRenderer로 원형 경고선을 만들어 스킬 테스트 단계에서도 바로 확인할 수 있게 합니다.
/// </summary>
internal class HWJ_SkillWarningIndicator : MonoBehaviour
{
    private const int DefaultSegmentCount = 48;
    private const int ArcSegmentCount = 24;
    private const string WarningObjectName = "HWJ_SkillWarningIndicator";

    private static Material sharedLineMaterial;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float durationSeconds = 1f;
    [SerializeField] private Color warningColor = new Color(1f, 0.15f, 0.05f, 0.85f);
    [SerializeField] private float lineWidth = 0.06f;

    private float endTime;

    public static HWJ_SkillWarningIndicator ShowCircle(
        Vector3 center,
        float radius,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        GameObject indicatorObject = new GameObject(WarningObjectName);
        indicatorObject.transform.position = center;

        HWJ_SkillWarningIndicator indicator = indicatorObject.AddComponent<HWJ_SkillWarningIndicator>();
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawCircle(radius);
        return indicator;
    }

    public static HWJ_SkillWarningIndicator ShowArrowPath(
        Vector3 start,
        float direction,
        float length,
        float width,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        GameObject indicatorObject = new GameObject(WarningObjectName);
        indicatorObject.transform.position = start;

        HWJ_SkillWarningIndicator indicator = indicatorObject.AddComponent<HWJ_SkillWarningIndicator>();
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawArrowPath(direction, length, width);
        return indicator;
    }

    public static HWJ_SkillWarningIndicator ShowRectangle(
        Vector3 center,
        Vector2 size,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        GameObject indicatorObject = new GameObject(WarningObjectName);
        indicatorObject.transform.position = center;

        HWJ_SkillWarningIndicator indicator = indicatorObject.AddComponent<HWJ_SkillWarningIndicator>();
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawRectangle(size);
        return indicator;
    }

    public static HWJ_SkillWarningIndicator ShowForwardArc(
        Vector3 center,
        float direction,
        float radius,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        GameObject indicatorObject = new GameObject(WarningObjectName);
        indicatorObject.transform.position = center;

        HWJ_SkillWarningIndicator indicator = indicatorObject.AddComponent<HWJ_SkillWarningIndicator>();
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawForwardArc(direction, radius);
        return indicator;
    }

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize(float durationSeconds, Color warningColor, float lineWidth)
    {
        this.durationSeconds = Mathf.Max(0.01f, durationSeconds);
        this.warningColor = warningColor;
        this.lineWidth = Mathf.Max(0.01f, lineWidth);
        endTime = Time.time + this.durationSeconds;

        EnsureLineRenderer();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.startColor = warningColor;
        lineRenderer.endColor = warningColor;
        lineRenderer.material = GetSharedLineMaterial();
        lineRenderer.sortingOrder = 100;
    }

    private void DrawRectangle(Vector2 size)
    {
        float halfWidth = Mathf.Max(0.1f, size.x * 0.5f);
        float halfHeight = Mathf.Max(0.1f, size.y * 0.5f);

        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0f));
        lineRenderer.SetPosition(1, new Vector3(-halfWidth, halfHeight, 0f));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, halfHeight, 0f));
        lineRenderer.SetPosition(3, new Vector3(halfWidth, -halfHeight, 0f));
    }

    private void DrawCircle(float radius)
    {
        float safeRadius = Mathf.Max(0.1f, radius);
        lineRenderer.loop = true;
        lineRenderer.positionCount = DefaultSegmentCount;

        for (int i = 0; i < DefaultSegmentCount; i++)
        {
            float angle = i / (float)DefaultSegmentCount * Mathf.PI * 2f;
            Vector3 point = new Vector3(Mathf.Cos(angle) * safeRadius, Mathf.Sin(angle) * safeRadius, 0f);
            lineRenderer.SetPosition(i, point);
        }
    }

    private void DrawArrowPath(float direction, float length, float width)
    {
        float safeDirection = direction < 0f ? -1f : 1f;
        float safeLength = Mathf.Max(0.1f, length);
        float halfWidth = Mathf.Max(0.1f, width * 0.5f);
        float arrowLength = Mathf.Clamp(safeLength * 0.25f, 0.25f, 0.75f);
        Vector3 forward = Vector3.right * safeDirection;
        Vector3 side = Vector3.up;

        lineRenderer.loop = true;
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, side * halfWidth);
        lineRenderer.SetPosition(1, forward * safeLength + side * halfWidth);
        lineRenderer.SetPosition(2, forward * (safeLength + arrowLength));
        lineRenderer.SetPosition(3, forward * safeLength - side * halfWidth);
        lineRenderer.SetPosition(4, -side * halfWidth);
    }

    private void DrawForwardArc(float direction, float radius)
    {
        float safeDirection = direction < 0f ? -1f : 1f;
        float safeRadius = Mathf.Max(0.1f, radius);
        float startAngle = safeDirection > 0f ? -55f : 125f;
        float endAngle = safeDirection > 0f ? 55f : 235f;

        lineRenderer.loop = true;
        lineRenderer.positionCount = ArcSegmentCount + 2;
        lineRenderer.SetPosition(0, Vector3.zero);

        for (int i = 0; i <= ArcSegmentCount; i++)
        {
            float t = i / (float)ArcSegmentCount;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            Vector3 point = new Vector3(Mathf.Cos(angle) * safeRadius, Mathf.Sin(angle) * safeRadius, 0f);
            lineRenderer.SetPosition(i + 1, point);
        }
    }

    private static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial != null)
        {
            return sharedLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Hidden/Internal-Colored");
        }

        if (shader == null)
        {
            return null;
        }

        sharedLineMaterial = new Material(shader);
        return sharedLineMaterial;
    }
}
