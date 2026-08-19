using UnityEngine;

/// <summary>
/// 일반 몬스터의 플레이어 발견, 동일 발판, 피격 어그로, 영혼 비인식 판정을 담당합니다.
/// 이동과 공격은 실행하지 않고 MonsterAISystem에 판정 결과만 제공합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_EnemyPerceptionSystem : MonoBehaviour
{
    private const int GroundProbeHitCapacity = 8;

    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private Transform candidateTarget;
    [SerializeField] private LayerMask groundLayer;

    [Header("대상 검색")]
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField, Min(0.05f)] private float targetSearchIntervalSeconds = 0.5f;

    [Header("런타임 확인")]
    [SerializeField] private bool hasAggro;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private string lastPerceptionResult;

    private float nextTargetSearchTime;
    private readonly RaycastHit2D[] groundProbeHits = new RaycastHit2D[GroundProbeHitCapacity];

    public Transform CurrentTarget => candidateTarget;
    public bool HasAggro => hasAggro;
    public Vector3 SpawnPosition => spawnPosition;
    public string LastPerceptionResult => lastPerceptionResult;

    private HWJ_EnemyTypeDataSO EnemyData
    {
        get
        {
            if (dataResolver != null
                && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
            {
                return enemyData;
            }

            return null;
        }
    }

    private void Awake()
    {
        CacheReferences();
        CaptureSpawnOrigin();
    }

    private void OnEnable()
    {
        HWJ_GameplayEvents.DamageApplied += HandleDamageApplied;
        CacheReferences();
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.DamageApplied -= HandleDamageApplied;
    }

    private void Update()
    {
        RefreshPerception();
    }

    public void CaptureSpawnOrigin()
    {
        spawnPosition = transform.position;
        hasAggro = false;
        lastPerceptionResult = "스폰 위치를 기준으로 순찰을 준비했습니다.";
    }

    public void SetCandidateTarget(Transform target, bool acquireAggro)
    {
        candidateTarget = target;

        if (acquireAggro && IsValidBodyTarget(candidateTarget))
        {
            hasAggro = true;
            lastPerceptionResult = "외부 시스템이 대상을 지정해 어그로를 획득했습니다.";
        }
    }

    public void RefreshPerception()
    {
        CacheReferences();

        if (candidateTarget == null
            && autoFindPlayerTarget
            && Time.time >= nextTargetSearchTime)
        {
            candidateTarget = FindPlayerTarget();
            nextTargetSearchTime = Time.time + Mathf.Max(0.05f, targetSearchIntervalSeconds);
        }

        if (!IsValidBodyTarget(candidateTarget))
        {
            if (hasAggro)
            {
                lastPerceptionResult = "대상이 영혼 상태이거나 유효하지 않아 어그로를 해제했습니다.";
            }

            hasAggro = false;
            return;
        }

        if (hasAggro)
        {
            if (ShouldLoseAggro())
            {
                hasAggro = false;
                lastPerceptionResult = "최대 추적 범위를 벗어나 어그로를 해제했습니다.";
            }

            return;
        }

        if (CanSeeCandidateTarget())
        {
            hasAggro = true;
            lastPerceptionResult = "전방 시야에서 빙의 상태 플레이어를 발견했습니다.";
        }
    }

    public void ClearAggro(string reason)
    {
        hasAggro = false;
        lastPerceptionResult = string.IsNullOrWhiteSpace(reason)
            ? "어그로를 해제했습니다."
            : reason;
    }

    public bool CanSeeCandidateTarget()
    {
        HWJ_EnemyTypeDataSO enemyData = EnemyData;

        if (enemyData == null || enemyData.Vision == null)
        {
            return FailPerception("적 시야 데이터가 없어 플레이어를 인식할 수 없습니다.");
        }

        if (!IsValidBodyTarget(candidateTarget))
        {
            return FailPerception("대상이 없거나 육신 상태가 아니어서 인식하지 않습니다.");
        }

        Vector2 toTarget = candidateTarget.position - transform.position;
        float viewDistance = enemyData.Vision.viewDistance > 0f
            ? enemyData.Vision.viewDistance
            : enemyData.Tracking.trackingRange;

        if (viewDistance <= 0f)
        {
            return FailPerception("시야 거리가 0 이하라 플레이어를 인식할 수 없습니다.");
        }

        if (toTarget.magnitude > viewDistance)
        {
            return FailPerception(
                $"플레이어가 시야 거리 밖에 있습니다. 현재 {toTarget.magnitude:F2} / 허용 {viewDistance:F2}");
        }

        float facingDirection = motionSystem != null
            ? motionSystem.CurrentFacingDirection
            : Mathf.Sign(transform.localScale.x);
        facingDirection = Mathf.Approximately(facingDirection, 0f) ? 1f : facingDirection;
        float halfAngle = Mathf.Clamp(enemyData.Vision.viewAngle, 0f, 360f) * 0.5f;

        float targetAngle = Vector2.Angle(Vector2.right * facingDirection, toTarget);

        if (targetAngle > halfAngle)
        {
            return FailPerception(
                $"플레이어가 시야각 밖에 있습니다. 현재 {targetAngle:F1}도 / 허용 {halfAngle:F1}도");
        }

        if (enemyData.Vision.requireSamePlatform && !IsTargetOnSamePlatform())
        {
            return FailPerception("플레이어가 몬스터와 같은 발판에 있지 않아 인식하지 않습니다.");
        }

        if (enemyData.Vision.requireClearLineOfSight && !HasClearLineOfSight(toTarget))
        {
            return FailPerception("몬스터와 플레이어 사이의 지형이 시야를 막고 있습니다.");
        }

        return true;
    }

    public bool IsTargetOnSamePlatform()
    {
        HWJ_EnemyTypeDataSO enemyData = EnemyData;

        if (enemyData == null || enemyData.Vision == null || candidateTarget == null)
        {
            return false;
        }

        float probeDistance = Mathf.Max(0.1f, enemyData.Vision.groundProbeDistance);
        int mask = HWJ_PhysicsLayerUtility.ResolveGroundMask(groundLayer);
        float tolerance = Mathf.Max(0f, enemyData.Vision.samePlatformHeightTolerance);

        bool hasSelfGround = TryGetGroundPoint(transform, probeDistance, mask, out float selfGroundY);
        bool hasTargetGround = TryGetGroundPoint(candidateTarget, probeDistance, mask, out float targetGroundY);

        if (hasSelfGround && hasTargetGround)
        {
            return Mathf.Abs(selfGroundY - targetGroundY) <= tolerance;
        }

        return Mathf.Abs(candidateTarget.position.y - transform.position.y) <= tolerance;
    }

    /// <summary>
    /// Default 레이어를 지면으로 사용하는 씬에서도 캐릭터 자신의 콜라이더를 제외하고
    /// 실제 발판의 높이를 찾습니다.
    /// </summary>
    private bool TryGetGroundPoint(
        Transform actor,
        float probeDistance,
        int layerMask,
        out float groundY)
    {
        groundY = 0f;

        if (actor == null)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useTriggers = false;

        int hitCount = Physics2D.Raycast(
            (Vector2)actor.position + Vector2.up * 0.1f,
            Vector2.down,
            filter,
            groundProbeHits,
            Mathf.Max(0.1f, probeDistance));
        float nearestDistance = float.MaxValue;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundProbeHits[i];

            if (hit.collider == null
                || hit.transform == actor
                || hit.transform.IsChildOf(actor))
            {
                continue;
            }

            if (hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            groundY = hit.point.y;
            foundGround = true;
        }

        return foundGround;
    }

    private bool FailPerception(string reason)
    {
        lastPerceptionResult = reason;
        return false;
    }

    public bool IsTargetAboveDifferentPlatform()
    {
        if (candidateTarget == null)
        {
            return false;
        }

        HWJ_EnemyTypeDataSO enemyData = EnemyData;
        float heightTolerance = enemyData != null && enemyData.Vision != null
            ? Mathf.Max(0.05f, enemyData.Vision.samePlatformHeightTolerance)
            : 0.4f;
        float targetHeightDelta = candidateTarget.position.y - transform.position.y;

        if (targetHeightDelta <= heightTolerance)
        {
            return false;
        }

        // Transform 피벗 높이만으로 다른 발판을 판정하면 크기가 다른 캐릭터가
        // 같은 바닥에 서 있어도 WaitBelowPlatform 상태에 고정될 수 있습니다.
        // 양쪽 지면을 실제로 찾은 경우에만 상단 발판 대기 상태를 허용합니다.
        float probeDistance = enemyData != null && enemyData.Vision != null
            ? Mathf.Max(0.1f, enemyData.Vision.groundProbeDistance)
            : 3f;
        int groundMask = HWJ_PhysicsLayerUtility.ResolveGroundMask(groundLayer);
        bool hasMonsterGround = TryGetGroundPoint(
            transform,
            probeDistance,
            groundMask,
            out float monsterGroundY);
        bool hasTargetGround = TryGetGroundPoint(
            candidateTarget,
            probeDistance,
            groundMask,
            out float targetGroundY);

        if (!hasMonsterGround || !hasTargetGround)
        {
            return false;
        }

        return targetGroundY - monsterGroundY > heightTolerance;
    }

    public bool ShouldLoseAggro()
    {
        HWJ_EnemyTypeDataSO enemyData = EnemyData;

        if (!hasAggro || enemyData == null || !IsValidBodyTarget(candidateTarget))
        {
            return true;
        }

        float loseRange = enemyData.Tracking.loseTargetRange > 0f
            ? enemyData.Tracking.loseTargetRange
            : Mathf.Max(enemyData.Tracking.trackingRange, enemyData.Vision.viewDistance);

        if (loseRange > 0f
            && Vector2.Distance(transform.position, candidateTarget.position) > loseRange)
        {
            return true;
        }

        float maxDistanceFromSpawn = enemyData.ReturnBehavior != null
            ? enemyData.ReturnBehavior.maxChaseDistanceFromSpawn
            : 0f;
        return maxDistanceFromSpawn > 0f
            && Mathf.Abs(transform.position.x - spawnPosition.x) > maxDistanceFromSpawn;
    }

    private bool HasClearLineOfSight(Vector2 toTarget)
    {
        int mask = HWJ_PhysicsLayerUtility.ResolveGroundMask(groundLayer);
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            toTarget.normalized,
            toTarget.magnitude,
            mask);
        return hit.collider == null
            || hit.transform == candidateTarget
            || hit.transform.IsChildOf(candidateTarget);
    }

    private bool IsValidBodyTarget(Transform checkedTarget)
    {
        if (checkedTarget == null || !checkedTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        HWJ_SoulSystem soulSystem = checkedTarget.GetComponent<HWJ_SoulSystem>();

        if (soulSystem == null)
        {
            soulSystem = checkedTarget.GetComponentInParent<HWJ_SoulSystem>();
        }

        return soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private void HandleDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (damageEvent.TargetStatus == null
            || damageEvent.TargetStatus.gameObject != gameObject
            || damageEvent.Source == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver attacker =
            damageEvent.Source.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (attacker == null
            || attacker.ObjectType != HWJ_ObjectType.Player
            || !IsValidBodyTarget(attacker.transform))
        {
            return;
        }

        candidateTarget = attacker.transform;
        hasAggro = true;
        motionSystem?.FaceDirection(candidateTarget.position.x - transform.position.x);
        lastPerceptionResult = "시야 밖에서 피격되어 공격자를 즉시 인식했습니다.";
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

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }
    }
}
