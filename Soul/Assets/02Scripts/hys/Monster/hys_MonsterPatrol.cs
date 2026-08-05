using UnityEngine;

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody2D))]
public class hys_MonsterPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_MonsterAISystem monsterAI;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Rigidbody2D rb;

    [Header("Patrol Area")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private float patrolRange = 2f;
    [SerializeField] private bool useSpawnPositionAsCenter = true;

    [Header("Patrol Move")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtEdgeSeconds = 0.4f;
    [SerializeField] private bool useRuntimeMoveSpeed = true;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool avoidWalls = true;
    [SerializeField] private bool avoidLedges = true;
    [SerializeField] private float wallCheckDistance = 0.2f;
    [SerializeField] private float ledgeCheckForwardDistance = 0.45f;
    [SerializeField] private float ledgeCheckDownDistance = 1.2f;

    [Header("AI Link")]
    [SerializeField] private bool patrolOnlyWhenMonsterAIIdle = true;
    [SerializeField] private bool stopWhenActionLocked = true;

    private Vector3 startPosition;
    private Vector3 spawnPosition;
    private float leftBoundX;
    private float rightBoundX;
    private float directionX = 1f;
    private float waitEndTime;

    private void Awake()
    {
        // 시작할 때 필요한 컴포넌트와 순찰 범위를 준비합니다.
        CacheReferences();
        CacheSpawnPosition();
        CachePatrolBounds();
    }

    private void OnValidate()
    {
        // 인스펙터 값이 바뀌면 순찰 범위를 다시 계산합니다.
        CacheReferences();
        CachePatrolBounds();
    }

    private void Update()
    {
        CacheReferences();

        if (!CanPatrol())
        {
            StopHorizontalMovement();
            return;
        }

        if (Time.time < waitEndTime)
        {
            StopHorizontalMovement();
            return;
        }

        PatrolMove();
    }

    private void CacheReferences()
    {
        // 같은 오브젝트에서 HWJ 몬스터 관련 컴포넌트를 자동으로 찾습니다.
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (monsterAI == null)
        {
            monsterAI = GetComponent<HWJ_MonsterAISystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void CachePatrolBounds()
    {
        // 지정된 포인트가 있으면 포인트 기준, 없으면 스폰 위치 기준으로 순찰 범위를 잡습니다.
        if (useSpawnPositionAsCenter)
        {
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            leftBoundX = center.x - Mathf.Max(0.1f, patrolRange);
            rightBoundX = center.x + Mathf.Max(0.1f, patrolRange);
            return;
        }

        if (leftPoint != null && rightPoint != null)
        {
            leftBoundX = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            rightBoundX = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
            return;
        }

        if (startPosition == Vector3.zero)
        {
            startPosition = transform.position;
        }

        float halfDistance = Mathf.Max(0.1f, patrolRange);
        leftBoundX = startPosition.x - halfDistance;
        rightBoundX = startPosition.x + halfDistance;
    }

    private void CacheSpawnPosition()
    {
        // 플레이 시작 시 몬스터가 놓인 위치를 스폰 중심점으로 저장합니다.
        spawnPosition = transform.position;

        if (startPosition == Vector3.zero)
        {
            startPosition = spawnPosition;
        }
    }

    private bool CanPatrol()
    {
        if (rb == null)
        {
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return false;
        }

        if (stopWhenActionLocked && runtimeStatus != null && !runtimeStatus.CanMove)
        {
            return false;
        }

        if (!patrolOnlyWhenMonsterAIIdle || monsterAI == null)
        {
            return true;
        }

        return monsterAI.CurrentState == HWJ_MonsterAIState.Idle;
    }

    private void PatrolMove()
    {
        CachePatrolBounds();

        if (ShouldTurnAround())
        {
            TurnAround();
            StopHorizontalMovement();
            return;
        }

        float moveSpeed = GetMoveSpeed();
        Vector2 velocity = rb.linearVelocity;
        velocity.x = directionX * moveSpeed;
        rb.linearVelocity = velocity;

        runtimeStatus?.SetState(HWJ_RuntimeState.Move);
        motionSystem?.FaceDirection(directionX);
    }

    private bool ShouldTurnAround()
    {
        if (directionX > 0f && transform.position.x >= rightBoundX)
        {
            return true;
        }

        if (directionX < 0f && transform.position.x <= leftBoundX)
        {
            return true;
        }

        if (avoidWalls && IsWallAhead())
        {
            return true;
        }

        return avoidLedges && IsLedgeAhead();
    }

    private void TurnAround()
    {
        // 끝 지점에 도착하면 잠깐 멈춘 뒤 반대 방향으로 순찰합니다.
        directionX *= -1f;
        waitEndTime = Time.time + Mathf.Max(0f, waitAtEdgeSeconds);
        motionSystem?.FaceDirection(directionX);
    }

    private float GetMoveSpeed()
    {
        if (useRuntimeMoveSpeed && runtimeStatus != null && runtimeStatus.MoveSpeed > 0f)
        {
            return runtimeStatus.MoveSpeed;
        }

        if (useRuntimeMoveSpeed && dataResolver != null && dataResolver.Status != null && dataResolver.Status.moveSpeed > 0f)
        {
            return dataResolver.Status.moveSpeed;
        }

        return Mathf.Max(0f, patrolSpeed);
    }

    private bool IsWallAhead()
    {
        if (wallCheckDistance <= 0f)
        {
            return false;
        }

        int layerMask = groundLayer.value != 0 ? groundLayer.value : Physics2D.AllLayers;
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.right * Mathf.Sign(directionX);

        return Physics2D.Raycast(origin + Vector2.up * 0.25f, direction, wallCheckDistance, layerMask)
            || Physics2D.Raycast(origin, direction, wallCheckDistance, layerMask)
            || Physics2D.Raycast(origin + Vector2.down * 0.25f, direction, wallCheckDistance, layerMask);
    }

    private bool IsLedgeAhead()
    {
        int layerMask = groundLayer.value != 0 ? groundLayer.value : Physics2D.AllLayers;
        Vector2 direction = Vector2.right * Mathf.Sign(directionX);
        Vector2 origin = (Vector2)transform.position
            + direction * Mathf.Max(0.05f, ledgeCheckForwardDistance)
            + Vector2.down * 0.05f;

        return !Physics2D.Raycast(origin, Vector2.down, Mathf.Max(0.1f, ledgeCheckDownDistance), layerMask);
    }

    private void StopHorizontalMovement()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        CachePatrolBounds();

        Gizmos.color = Color.cyan;
        Vector3 left = new Vector3(leftBoundX, transform.position.y, transform.position.z);
        Vector3 right = new Vector3(rightBoundX, transform.position.y, transform.position.z);
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.12f);
        Gizmos.DrawWireSphere(right, 0.12f);

        Gizmos.color = Color.yellow;
        Vector3 wallStart = transform.position;
        Vector3 wallEnd = wallStart + Vector3.right * directionX * wallCheckDistance;
        Gizmos.DrawLine(wallStart, wallEnd);

        Gizmos.color = Color.magenta;
        Vector3 ledgeStart = transform.position
            + Vector3.right * directionX * Mathf.Max(0.05f, ledgeCheckForwardDistance)
            + Vector3.down * 0.05f;
        Gizmos.DrawLine(ledgeStart, ledgeStart + Vector3.down * Mathf.Max(0.1f, ledgeCheckDownDistance));
    }
}
