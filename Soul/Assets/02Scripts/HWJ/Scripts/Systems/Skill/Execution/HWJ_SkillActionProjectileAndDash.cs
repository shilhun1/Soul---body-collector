using System.Collections;
using UnityEngine;

/// <summary>
/// Executes projectile spawning, projectile charge timing, dash movement, and dash contact damage.
/// This partial belongs to the single HWJ_SkillActionSystem component.
/// </summary>
public partial class HWJ_SkillActionSystem
{
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
            SpawnActionEffect(skillAction.ActionEffectPrefab);
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
            false,
            skillAction,
            HWJ_GimmickHitSource.DashContact);

        if (damaged)
        {
            lastDamageApplied = combatExecutionSystem.LastDamageApplied;
            lastSkillResult = combatExecutionSystem.LastExecutionResult;
        }

        return damaged;
    }

}
