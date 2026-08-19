using System.Collections;
using UnityEngine;

/// <summary>
/// Executes timed area hits, character motion sequences, and pooled action effects.
/// This partial belongs to the single HWJ_SkillActionSystem component.
/// </summary>
public partial class HWJ_SkillActionSystem
{
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
            playMotion,
            skillAction,
            HWJ_GimmickHitSource.AreaAttack);

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
        return SpawnPooled(prefab, transform.position, transform.rotation);
    }

    private GameObject SpawnPooled(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        if (objectPool != null)
        {
            return objectPool.Spawn(prefab, position, rotation);
        }

        GameObject spawned = HWJ_GameAccess.Spawn(prefab, position, rotation);
        return spawned != null ? spawned : Instantiate(prefab, position, rotation);
    }

    private GameObject SpawnActionEffect(HWJ_SkillActionDataSO skillAction)
    {
        if (skillAction == null || skillAction.ActionEffectPrefab == null)
        {
            return null;
        }

        Vector2 offset = skillAction.UseCustomActionEffectOffset
            ? skillAction.ActionEffectOffset
            : actionEffectSpawnOffset;
        float rotationZ = skillAction.ActionEffectRotationZ;
        float scaleMultiplier = skillAction.ActionEffectScaleMultiplier;

        return SpawnActionEffect(skillAction.ActionEffectPrefab, offset, rotationZ, scaleMultiplier);
    }

    private GameObject SpawnActionEffect(GameObject prefab)
    {
        return SpawnActionEffect(prefab, actionEffectSpawnOffset, 0f, 1f);
    }

    private GameObject SpawnActionEffect(
        GameObject prefab,
        Vector2 spawnOffset,
        float rotationZ = 0f,
        float scaleMultiplier = 1f)
    {
        if (prefab == null)
        {
            return null;
        }

        float facingDirection = GetFacingDirection();
        Vector3 offset = new Vector3(spawnOffset.x * facingDirection, spawnOffset.y, 0f);

        Quaternion spawnRotation = transform.rotation;
        if (!Mathf.Approximately(rotationZ, 0f))
        {
            float appliedRotationZ = facingDirection < 0f ? -rotationZ : rotationZ;
            spawnRotation = Quaternion.Euler(0f, 0f, appliedRotationZ) * spawnRotation;
        }

        GameObject spawned = SpawnPooled(prefab, transform.position + offset, spawnRotation);

        if (spawned == null)
        {
            return null;
        }

        if (mirrorActionEffectByFacing || !Mathf.Approximately(scaleMultiplier, 1f))
        {
            Vector3 scale = spawned.transform.localScale;
            float targetScaleMultiplier = scaleMultiplier <= 0f ? 1f : scaleMultiplier;
            float signX = (facingDirection < 0f && mirrorActionEffectByFacing) ? -1f : 1f;
            scale.x = Mathf.Abs(scale.x) * signX * targetScaleMultiplier;
            scale.y = Mathf.Abs(scale.y) * targetScaleMultiplier;
            scale.z = Mathf.Abs(scale.z) * targetScaleMultiplier;
            spawned.transform.localScale = scale;
        }

        return spawned;
    }

}
