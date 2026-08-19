using UnityEngine;

/// <summary>
/// Resolves aim/facing and validates weapon type and player skill unlock state.
/// This partial belongs to the single HWJ_SkillActionSystem component.
/// </summary>
public partial class HWJ_SkillActionSystem
{
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

}
