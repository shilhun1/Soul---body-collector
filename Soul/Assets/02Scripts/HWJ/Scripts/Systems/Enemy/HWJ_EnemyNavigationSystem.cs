using UnityEngine;

/// <summary>
/// EnemyTypeDataSO의 Tracking/Navigation/State 값을 읽어 대상에게 접근하는 기본 적 이동 시스템입니다.
/// 실제 길찾기나 플랫폼 AI는 이 시스템을 교체하더라도 같은 EnemyTypeData를 계속 사용할 수 있습니다.
/// </summary>
public class HWJ_EnemyNavigationSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_MonsterAISystem monsterAI;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private bool chaseOnlyBodyState = true;
    [SerializeField] private bool faceOnlyBodyState = true;
    [SerializeField] private bool horizontalMoveOnly = true;
    [SerializeField] private float fallbackTrackingRange = 8f;
    [SerializeField] private float fallbackStoppingDistance = 1f;
    [SerializeField] private float targetSearchIntervalSeconds = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private float nextTargetSearchTime;
    private float hitPauseEndTime;
    private HWJ_RuntimeState previousRuntimeState = HWJ_RuntimeState.None;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (monsterAI == null)
        {
            monsterAI = GetComponent<HWJ_MonsterAISystem>();
        }
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

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (target == null && autoFindPlayerTarget && Time.time >= nextTargetSearchTime)
        {
            target = FindPlayerTarget();
            nextTargetSearchTime = Time.time + targetSearchIntervalSeconds;
        }

        if (target == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            StopHorizontalMovement();
            return;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            StopHorizontalMovement();
            return;
        }

        UpdateHitPause(enemyData);

        if (runtimeStatus != null
            && runtimeStatus.CurrentState == HWJ_RuntimeState.Hit
            && enemyData.State.stopWhenHit
            && Time.time < hitPauseEndTime)
        {
            return;
        }

        if (skillActionSystem != null && skillActionSystem.IsNavigationBlocked)
        {
            if (!faceOnlyBodyState || CanChaseTargetState())
            {
                FaceTarget();
            }

            if (skillActionSystem.ShouldStopNavigationMovement)
            {
                StopHorizontalMovement();
            }

            return;
        }

        if (chaseOnlyBodyState && !CanChaseTargetState())
        {
            SetIdle();
            return;
        }

        FaceTarget();

        float distance = horizontalMoveOnly
            ? Mathf.Abs(target.position.x - transform.position.x)
            : Vector2.Distance(transform.position, target.position);
        float trackingRange = enemyData.Tracking.trackingRange > 0f
            ? enemyData.Tracking.trackingRange
            : fallbackTrackingRange;
        float stoppingDistance = enemyData.Navigation.stoppingDistance > 0f
            ? enemyData.Navigation.stoppingDistance
            : fallbackStoppingDistance;

        if (distance > trackingRange || distance <= stoppingDistance)
        {
            SetIdle();
            return;
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;

        if (!CanMoveForward(Mathf.Sign(target.position.x - transform.position.x), enemyData))
        {
            SetIdle();
            return;
        }

        MoveTowardTarget(moveSpeed);

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(distance <= enemyData.State.attackRange ? HWJ_RuntimeState.Attack : HWJ_RuntimeState.Move);
        }
    }

    private void UpdateHitPause(HWJ_EnemyTypeDataSO enemyData)
    {
        if (runtimeStatus == null)
        {
            return;
        }

        HWJ_RuntimeState currentState = runtimeStatus.CurrentState;

        if (currentState == HWJ_RuntimeState.Hit && previousRuntimeState != HWJ_RuntimeState.Hit)
        {
            float pauseSeconds = enemyData != null && enemyData.State.returnToIdleDelaySeconds > 0f
                ? enemyData.State.returnToIdleDelaySeconds
                : 0.2f;
            hitPauseEndTime = Time.time + pauseSeconds;
        }

        previousRuntimeState = currentState;
    }

    /// <summary>
    /// 추적할 대상을 외부에서 지정합니다.
    /// 감지 시스템이나 스테이지 매니저가 플레이어 Transform을 넘겨줄 때 사용합니다.
    /// </summary>
    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    private void FaceTarget()
    {
        if (target == null || motionSystem == null)
        {
            return;
        }

        motionSystem.FaceDirection(target.position.x - transform.position.x);
    }

    private void MoveTowardTarget(float moveSpeed)
    {
        float directionX = Mathf.Sign(target.position.x - transform.position.x);

        if (horizontalMoveOnly)
        {
            if (body != null)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x = directionX * moveSpeed;
                body.linearVelocity = velocity;
                return;
            }

            transform.position += Vector3.right * directionX * moveSpeed * Time.deltaTime;
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (body != null)
        {
            body.MovePosition(nextPosition);
            return;
        }

        transform.position = nextPosition;
    }

    private void SetIdle()
    {
        StopHorizontalMovement();

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(HWJ_RuntimeState.Idle);
        }
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

    private bool CanMoveForward(float directionX, HWJ_EnemyTypeDataSO enemyData)
    {
        if (enemyData == null || Mathf.Abs(directionX) <= 0.01f)
        {
            return false;
        }

        if (!enemyData.Navigation.avoidLedges && enemyData.Navigation.wallCheckDistance <= 0f)
        {
            return true;
        }

        int layerMask = HWJ_PhysicsLayerUtility.ResolveGroundMask(groundLayer);
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.right * Mathf.Sign(directionX);

        if (enemyData.Navigation.wallCheckDistance > 0f
            && Physics2D.Raycast(origin, direction, enemyData.Navigation.wallCheckDistance, layerMask))
        {
            return false;
        }

        if (!enemyData.Navigation.avoidLedges)
        {
            return true;
        }

        Vector2 ledgeOrigin = origin
            + direction * Mathf.Max(0.05f, enemyData.Navigation.ledgeCheckForwardDistance)
            + Vector2.down * 0.05f;
        float ledgeDistance = Mathf.Max(0.1f, enemyData.Navigation.ledgeCheckDownDistance);
        return Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeDistance, layerMask);
    }

    private bool CanChaseTargetState()
    {
        HWJ_SoulSystem targetSoul = target.GetComponent<HWJ_SoulSystem>();

        if (targetSoul == null)
        {
            targetSoul = target.GetComponentInParent<HWJ_SoulSystem>();
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
}
