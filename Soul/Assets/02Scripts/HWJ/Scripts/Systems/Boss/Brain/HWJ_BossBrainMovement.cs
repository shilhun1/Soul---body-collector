using UnityEngine;

/// <summary>
/// Moves, faces, teleports, and applies boss knockback inside the arena.
/// This partial belongs to the single HWJ_BossBrainSystem component.
/// </summary>
public partial class HWJ_BossBrainSystem
{
    private float GetAttackRange(HWJ_BossTypeDataSO bossData)
    {
        if (bossData.FSM.attackStartRange > 0f)
        {
            return bossData.FSM.attackStartRange;
        }

        return Mathf.Max(1f, bossData.Navigation.stoppingDistance);
    }

    private float GetOptimalDistance(HWJ_BossTypeDataSO bossData)
    {
        if (bossData.FSM.optimalAttackDistance > 0f)
        {
            return bossData.FSM.optimalAttackDistance;
        }

        return GetAttackRange(bossData);
    }

    private void MoveTowardTarget(HWJ_BossTypeDataSO bossData)
    {
        if (target == null)
        {
            StopHorizontalMovement();
            return;
        }

        float directionX = Mathf.Sign(target.position.x - transform.position.x);
        float moveSpeed = runtimeStatus != null
            ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        moveSpeed *= Mathf.Max(0f, bossData.FSM.chaseMoveSpeedMultiplier);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return;
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
    }

    private void MoveTowardPosition(Vector2 destination, HWJ_BossTypeDataSO bossData, float stoppingDistance)
    {
        float distanceX = Mathf.Abs(destination.x - transform.position.x);

        if (distanceX <= Mathf.Max(0.01f, stoppingDistance))
        {
            StopHorizontalMovement();
            return;
        }

        float directionX = Mathf.Sign(destination.x - transform.position.x);
        float moveSpeed = runtimeStatus != null
            ? runtimeStatus.MoveSpeed
            : dataResolver != null && dataResolver.Status != null ? dataResolver.Status.moveSpeed : 0f;
        moveSpeed *= Mathf.Max(0f, bossData.FSM.chaseMoveSpeedMultiplier);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = directionX * moveSpeed;
            body.linearVelocity = velocity;
            return;
        }

        transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
    }

    private void TeleportToRoomCenter()
    {
        Vector2 center = GetBossRoomCenter();
        Vector3 targetPosition = new Vector3(center.x, center.y, transform.position.z);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = (Vector2)targetPosition;
        }

        transform.position = targetPosition;
    }

    private Vector2 GetBossRoomCenter()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return roomAnchorPosition;
        }

        return (Vector2)roomAnchorPosition + bossData.FSM.bossRoomOffset;
    }

    private Vector2 GetBossRoomSize()
    {
        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData))
        {
            return new Vector2(28f, 14f);
        }

        return bossData.FSM.bossRoomSize;
    }

    private void FaceTarget()
    {
        if (target == null || motionSystem == null)
        {
            return;
        }

        motionSystem.FaceDirection(target.position.x - transform.position.x);
    }

    private void StopHorizontalMovement()
    {
        if (body == null)
        {
            return;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = 0f;
        body.linearVelocity = velocity;
    }

    private void KnockbackTarget(float power)
    {
        if (target == null || power <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = target.GetComponent<HWJ_KnockbackSystem>();

        if (knockbackSystem == null)
        {
            knockbackSystem = target.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 direction = target.position - transform.position;
        direction.y = 0f;
        knockbackSystem.PlayKnockback(direction, power, 0.25f);
    }

}
