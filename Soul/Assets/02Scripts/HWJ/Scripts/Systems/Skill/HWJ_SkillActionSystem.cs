using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SkillActionDataSO를 실제 전투 행동으로 실행하는 공통 스킬 시스템입니다.
/// 플레이어, 빙의 상태, 몬스터 AI가 같은 스킬 데이터를 사용하도록 ID 조회, 쿨타임, 모션, 판정을 한 곳에서 처리합니다.
/// </summary>
public class HWJ_SkillActionSystem : MonoBehaviour
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

    private void ExecuteSkill(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        if (skillAction == null)
        {
            return;
        }

        if (skillAction.GrantsInvincibility && runtimeStatus != null)
        {
            runtimeStatus.GrantInvincibility(skillAction.InvincibilitySeconds);
        }

        if (skillAction.ActionEffectPrefab != null)
        {
            SpawnPooled(skillAction.ActionEffectPrefab);
        }

        switch (skillAction.ActionType)
        {
            case HWJ_SkillActionType.Projectile:
                ExecuteProjectileSkill(skillAction, target, useLockedDirection, lockedDirection);
                break;
            case HWJ_SkillActionType.Dash:
                ExecuteDashSkill(skillAction, target, useLockedDirection, lockedDirection);
                break;
            case HWJ_SkillActionType.Melee:
            case HWJ_SkillActionType.Area:
                FaceLockedDirection(useLockedDirection, lockedDirection);
                ExecuteAreaDamage(skillAction, true);
                break;
            case HWJ_SkillActionType.Buff:
                PlaySkillMotion(skillAction);
                lastSkillResult = $"Used buff skill {skillAction.SkillActionId}.";
                break;
            default:
                PlaySkillMotion(skillAction);
                lastSkillResult = $"Used skill {skillAction.SkillActionId}.";
                break;
        }
    }

    private void ExecuteProjectileSkill(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        if (projectileRoutine != null)
        {
            StopCoroutine(projectileRoutine);
        }

        projectileRoutine = StartCoroutine(ProjectileSkillRoutine(skillAction, target, useLockedDirection, lockedDirection));
    }

    private IEnumerator ProjectileSkillRoutine(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        PlaySkillMotion(skillAction);
        Vector2 resolvedDirection = ResolveSkillDirectionWithLock(target, useLockedDirection, lockedDirection);
        float chargeSeconds = Mathf.Max(0f, skillAction.DurationSeconds);

        int projectileCount = Mathf.Max(1, skillAction.HitCount);
        float shotIntervalSeconds = Mathf.Max(0f, skillAction.HitIntervalSeconds);
        float totalNavigationBlockSeconds = chargeSeconds + shotIntervalSeconds * Mathf.Max(0, projectileCount - 1);

        if (totalNavigationBlockSeconds > 0f)
        {
            BlockNavigationForSkill(totalNavigationBlockSeconds + 0.05f, false);
        }

        if (chargeSeconds > 0f)
        {
            yield return new WaitForSeconds(chargeSeconds);
        }

        for (int i = 0; i < projectileCount; i++)
        {
            FireProjectile(skillAction, resolvedDirection);

            if (i < projectileCount - 1 && shotIntervalSeconds > 0f)
            {
                yield return new WaitForSeconds(shotIntervalSeconds);
            }
        }

        projectileRoutine = null;
    }

    private void FireProjectile(HWJ_SkillActionDataSO skillAction, Vector2 direction)
    {
        if (skillAction.ProjectilePrefab != null)
        {
            GameObject projectile = SpawnPooled(skillAction.ProjectilePrefab);
            SetupProjectile(projectile, skillAction, direction);
            lastSkillResult = $"Fired pooled projectile skill {skillAction.SkillActionId}.";
            return;
        }

        // 임시 투사체 프리팹이 없을 때도 데이터 테스트가 가능하도록 범위 판정을 사용합니다.
        GameObject fallbackProjectile = CreateFallbackProjectileObject(skillAction, direction);
        SetupProjectile(fallbackProjectile, skillAction, direction);
        lastSkillResult = $"Fired fallback projectile skill {skillAction.SkillActionId}.";
    }

    private void ExecuteDashSkill(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        PlaySkillMotion(skillAction);

        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
        }

        movementRoutine = StartCoroutine(DashSkillRoutine(skillAction, target, useLockedDirection, lockedDirection));
    }

    private IEnumerator DashSkillRoutine(
        HWJ_SkillActionDataSO skillAction,
        Transform target,
        bool useLockedDirection,
        Vector2 lockedDirection)
    {
        float moveDistance = Mathf.Max(0f, skillAction.MoveDistance);
        float moveSpeed = Mathf.Max(MinimumDashSpeed, skillAction.MoveSpeed);

        if (moveDistance <= 0f || moveSpeed <= 0f)
        {
            ExecuteAreaDamage(skillAction, false);
            movementRoutine = null;
            yield break;
        }

        Vector2 direction = ResolveSkillDirectionWithLock(target, useLockedDirection, lockedDirection);
        float duration = Mathf.Max(0.01f, moveDistance / moveSpeed);
        float endTime = Time.time + duration;
        float movedDistance = 0f;
        bool hasDamagedDuringDash = false;

        BlockNavigationForSkill(duration + 0.05f, true);

        while (Time.time < endTime && movedDistance < moveDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            movedDistance += step;
            MoveOwnerByVelocity(direction, moveSpeed);

            if (!hasDamagedDuringDash)
            {
                hasDamagedDuringDash = TryExecuteDashContactDamage(skillAction);
            }

            yield return null;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (!hasDamagedDuringDash)
        {
            ExecuteAreaDamage(skillAction, false);
        }

        ReleaseNavigationBlock();
        movementRoutine = null;
    }

    private void SetupProjectile(GameObject projectile, HWJ_SkillActionDataSO skillAction, Vector2 direction)
    {
        if (projectile == null)
        {
            ExecuteAreaDamage(skillAction, false);
            return;
        }

        HWJ_RuntimeSkillProjectile runtimeProjectile = projectile.GetComponent<HWJ_RuntimeSkillProjectile>();

        if (runtimeProjectile == null)
        {
            runtimeProjectile = projectile.AddComponent<HWJ_RuntimeSkillProjectile>();
        }

        float distance = Mathf.Max(MinimumProjectileDistance, skillAction.Range, skillAction.MoveDistance);
        float speed = skillAction.MoveSpeed > 0f ? skillAction.MoveSpeed : DefaultProjectileSpeed;
        float hitHeight = Mathf.Max(0.25f, skillAction.HitRange);

        runtimeProjectile.Initialize(
            transform,
            combatExecutionSystem,
            skillAction,
            direction,
            distance,
            speed,
            hitHeight);
    }

    private GameObject CreateFallbackProjectileObject(HWJ_SkillActionDataSO skillAction, Vector2 direction)
    {
        GameObject projectile = new GameObject($"HWJ_RuntimeProjectile_{skillAction.SkillActionId}");
        projectile.transform.position = transform.position;

        // Temporary visual so the sword wave works before a real projectile sprite prefab is added.
        LineRenderer lineRenderer = projectile.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = Mathf.Max(0.08f, skillAction.HitRange * 0.08f);
        lineRenderer.startColor = new Color(0.35f, 0.85f, 1f, 0.95f);
        lineRenderer.endColor = new Color(1f, 1f, 1f, 0.95f);
        lineRenderer.material = GetRuntimeLineMaterial();
        lineRenderer.sortingOrder = 110;
        lineRenderer.SetPosition(0, new Vector3(-0.15f * direction.x, -0.5f, 0f));
        lineRenderer.SetPosition(1, new Vector3(0.15f * direction.x, 0.5f, 0f));

        return projectile;
    }

    private bool TryExecuteDashContactDamage(HWJ_SkillActionDataSO skillAction)
    {
        if (combatExecutionSystem == null)
        {
            return false;
        }

        float range = Mathf.Max(0.1f, skillAction.HitRange > 0f ? skillAction.HitRange : skillAction.Range);
        bool damaged = combatExecutionSystem.TryExecuteAreaAttack(
            range,
            skillAction.DamageMultiplier,
            skillAction.MotionKey,
            false);

        if (damaged)
        {
            lastDamageApplied = combatExecutionSystem.LastDamageApplied;
            lastSkillResult = combatExecutionSystem.LastExecutionResult;
        }

        return damaged;
    }

    private void ExecuteAreaDamage(HWJ_SkillActionDataSO skillAction, bool playMotion)
    {
        if (combatExecutionSystem == null)
        {
            PlaySkillMotion(skillAction);
            lastSkillResult = $"Skill {skillAction.SkillActionId} failed: missing combat execution system.";
            return;
        }

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
        }

        actionRoutine = StartCoroutine(TimedAreaDamageRoutine(skillAction, playMotion));
    }

    private IEnumerator TimedAreaDamageRoutine(HWJ_SkillActionDataSO skillAction, bool playMotion)
    {
        float hitStart = Mathf.Max(0f, skillAction.HitStartSeconds);
        float activeSeconds = Mathf.Max(0.01f, skillAction.HitActiveSeconds);
        float recoverySeconds = Mathf.Max(0f, skillAction.RecoverySeconds);
        float totalSeconds = Mathf.Max(skillAction.DurationSeconds, hitStart + activeSeconds + recoverySeconds);
        float movementLockSeconds = skillAction.MovementLockSeconds > 0f
            ? skillAction.MovementLockSeconds
            : hitStart + activeSeconds;

        runtimeStatus?.BeginTimedAttackAction(
            totalSeconds,
            movementLockSeconds,
            skillAction.DashCancelStartSeconds,
            skillAction.CanDashCancel);
        BlockNavigationForSkill(movementLockSeconds, false);

        if (playMotion)
        {
            PlaySkillMotion(skillAction);
        }

        if (hitStart > 0f)
        {
            yield return new WaitForSeconds(hitStart);
        }

        int hitCount = Mathf.Max(1, skillAction.HitCount);
        float interval = Mathf.Max(0.01f, skillAction.HitIntervalSeconds);
        float activeEndTime = Time.time + activeSeconds;

        for (int i = 0; i < hitCount; i++)
        {
            ExecuteSingleAreaDamage(skillAction, false);

            if (i >= hitCount - 1 || Time.time + interval > activeEndTime)
            {
                break;
            }

            yield return new WaitForSeconds(interval);
        }

        float remainingActiveSeconds = activeEndTime - Time.time;

        if (remainingActiveSeconds > 0f)
        {
            yield return new WaitForSeconds(remainingActiveSeconds);
        }

        if (recoverySeconds > 0f)
        {
            yield return new WaitForSeconds(recoverySeconds);
        }

        actionRoutine = null;
    }

    private void ExecuteSingleAreaDamage(HWJ_SkillActionDataSO skillAction, bool playMotion)
    {
        float range = Mathf.Max(0.1f, skillAction.HitRange > 0f ? skillAction.HitRange : skillAction.Range);
        bool damaged = combatExecutionSystem.TryExecuteAreaAttack(
            range,
            skillAction.DamageMultiplier,
            skillAction.MotionKey,
            playMotion);

        lastDamageApplied = combatExecutionSystem.LastDamageApplied;
        lastSkillResult = damaged
            ? combatExecutionSystem.LastExecutionResult
            : $"Skill {skillAction.SkillActionId}: {combatExecutionSystem.LastExecutionResult}";
    }

    private void PlaySkillMotion(HWJ_SkillActionDataSO skillAction)
    {
        if (motionSystem == null || skillAction == null)
        {
            return;
        }

        if (skillAction.HasMotionSequence)
        {
            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
            }

            motionRoutine = StartCoroutine(MotionSequenceRoutine(skillAction));
            return;
        }

        if (!string.IsNullOrEmpty(skillAction.MotionKey))
        {
            motionSystem.PlayMotionKey(skillAction.MotionKey);
            return;
        }

        switch (skillAction.ActionType)
        {
            case HWJ_SkillActionType.Melee:
            case HWJ_SkillActionType.Projectile:
            case HWJ_SkillActionType.Area:
                motionSystem.PlayAttack(null);
                break;
            case HWJ_SkillActionType.Dash:
                motionSystem.PlayDash();
                break;
        }
    }

    private IEnumerator MotionSequenceRoutine(HWJ_SkillActionDataSO skillAction)
    {
        string[] motionKeys = skillAction.MotionSequenceKeys;
        float interval = Mathf.Max(0.01f, skillAction.MotionStepIntervalSeconds);

        for (int i = 0; i < motionKeys.Length; i++)
        {
            if (!string.IsNullOrEmpty(motionKeys[i]))
            {
                motionSystem.PlayMotionKey(motionKeys[i]);
            }

            if (i < motionKeys.Length - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        motionRoutine = null;
    }

    private GameObject SpawnPooled(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        if (objectPool != null)
        {
            return objectPool.Spawn(prefab, transform.position, transform.rotation);
        }

        GameObject spawned = HWJ_GameAccess.Spawn(prefab, transform.position, transform.rotation);
        return spawned != null ? spawned : Instantiate(prefab, transform.position, transform.rotation);
    }

    private Vector2 ResolveSkillDirection(Transform target)
    {
        return ResolveSkillDirectionWithLock(target, false, Vector2.zero);
    }

    private Vector2 ResolveSkillDirectionWithLock(Transform target, bool useLockedDirection, Vector2 lockedDirection)
    {
        if (useLockedDirection && lockedDirection.sqrMagnitude > 0.0001f)
        {
            Vector2 normalizedDirection = lockedDirection.normalized;
            return new Vector2(normalizedDirection.x == 0f ? GetFacingDirection() : Mathf.Sign(normalizedDirection.x), 0f);
        }

        if (target != null)
        {
            float xDirection = Mathf.Sign(target.position.x - transform.position.x);
            return new Vector2(xDirection == 0f ? GetFacingDirection() : xDirection, 0f);
        }

        return new Vector2(GetFacingDirection(), 0f);
    }

    private void FaceLockedDirection(bool useLockedDirection, Vector2 lockedDirection)
    {
        if (!useLockedDirection || lockedDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float directionX = Mathf.Sign(lockedDirection.x);

        if (directionX == 0f)
        {
            return;
        }

        if (motionSystem != null)
        {
            motionSystem.FaceDirection(directionX);
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * directionX;
        transform.localScale = scale;
    }

    private float GetFacingDirection()
    {
        if (motionSystem != null)
        {
            // 스프라이트 플립과 스케일 플립 중 실제 캐릭터가 사용하는 방향 기준을 모션 시스템에서 가져옵니다.
            float motionFacing = motionSystem.ResolveCurrentFacingDirection();
            return motionFacing < 0f ? -1f : 1f;
        }

        float facing = Mathf.Sign(transform.localScale.x);
        return facing == 0f ? 1f : facing;
    }

    private void MoveOwner(Vector2 delta)
    {
        if (body != null)
        {
            body.MovePosition(body.position + delta);
            return;
        }

        transform.position += (Vector3)delta;
    }

    private void MoveOwnerByVelocity(Vector2 direction, float speed)
    {
        if (body != null)
        {
            body.linearVelocity = direction * speed;
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private static Material GetRuntimeLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        return shader != null ? new Material(shader) : null;
    }

    private bool IsSkillEntryWeaponMatched(HWJ_SkillEntryData definedSkillEntry)
    {
        if (definedSkillEntry == null)
        {
            return false;
        }

        HWJ_WeaponType currentWeaponType = GetCurrentWeaponType();
        return definedSkillEntry.requiredWeaponType == HWJ_WeaponType.None
            || definedSkillEntry.requiredWeaponType == currentWeaponType;
    }

    private HWJ_WeaponType GetCurrentWeaponType()
    {
        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        return dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
    }

    /// <summary>
    /// 플레이어 스킬트리에 등록된 스킬만 해금 상태를 검사합니다.
    /// 몬스터/보스 전용 스킬은 기존 스킬 사이클 규칙을 그대로 사용합니다.
    /// </summary>
    private bool IsTrackedPlayerSkillUnlocked(string skillActionId)
    {
        if (playerSkillUnlock == null)
        {
            playerSkillUnlock = GetComponent<HWJ_SkillUnlockSystem>();
        }

        if (playerSkillUnlock == null)
        {
            return true;
        }

        if (playerSkillUnlock.HasSkillNodeDefinitionForSkillAction(skillActionId))
        {
            if (playerSkillUnlock.IsSkillActionUnlockedBySkillNodeProgress(skillActionId))
            {
                return true;
            }

            lastSkillResult = $"Skill failed: {skillActionId} skill node is locked.";
            return false;
        }

        if (!playerSkillUnlock.HasSkillDefinition(skillActionId))
        {
            return true;
        }

        if (playerSkillUnlock.IsSkillUnlocked(skillActionId))
        {
            return true;
        }

        lastSkillResult = $"Skill failed: {skillActionId} is locked.";
        return false;
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

internal class HWJ_RuntimeSkillProjectile : MonoBehaviour
{
    private readonly Collider2D[] hits = new Collider2D[8];

    private Transform owner;
    private HWJ_CombatExecutionSystem combatExecutionSystem;
    private HWJ_SkillActionDataSO skillAction;
    private Vector2 direction;
    private float maxDistance;
    private float speed;
    private float hitHeight;
    private float traveledDistance;

    public void Initialize(
        Transform owner,
        HWJ_CombatExecutionSystem combatExecutionSystem,
        HWJ_SkillActionDataSO skillAction,
        Vector2 direction,
        float maxDistance,
        float speed,
        float hitHeight)
    {
        this.owner = owner;
        this.combatExecutionSystem = combatExecutionSystem;
        this.skillAction = skillAction;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.maxDistance = Mathf.Max(0.1f, maxDistance);
        this.speed = Mathf.Max(0.1f, speed);
        this.hitHeight = Mathf.Max(0.25f, hitHeight);
        traveledDistance = 0f;

        transform.position = owner != null ? owner.position : transform.position;
        transform.right = this.direction;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        traveledDistance += step;

        if (TryHitTarget() || traveledDistance >= maxDistance)
        {
            DespawnOrDestroy();
        }
    }

    private bool TryHitTarget()
    {
        if (combatExecutionSystem == null || skillAction == null)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = Physics2D.queriesHitTriggers;

        Vector2 hitBoxSize = new Vector2(0.8f, hitHeight);
        int hitCount = Physics2D.OverlapBox(transform.position, hitBoxSize, 0f, filter, hits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || owner != null && hit.transform.IsChildOf(owner))
            {
                continue;
            }

            HWJ_RootObjectDataResolver targetResolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (targetResolver == null)
            {
                continue;
            }

            if (combatExecutionSystem.TryExecuteAttackTo(
                targetResolver,
                skillAction.DamageMultiplier,
                skillAction.MotionKey,
                false))
            {
                return true;
            }
        }

        return false;
    }

    private void DespawnOrDestroy()
    {
        HWJ_PoolableObject poolableObject = GetComponent<HWJ_PoolableObject>();

        if (poolableObject != null && poolableObject.PrefabKey != null)
        {
            poolableObject.ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }
}
