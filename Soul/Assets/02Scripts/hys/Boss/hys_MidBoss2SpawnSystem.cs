using UnityEngine;

// 씬 시작 시 보스 스폰포인트를 찾아 HYS 중간보스2 프리팹을 정확히 한 번 생성합니다.
[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
public class hys_MidBoss2SpawnSystem : MonoBehaviour
{
    [Header("생성 설정")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private hys_MidBoss2SpawnPoint spawnPoint;
    [SerializeField] private string spawnPointId = "hys_midboss2_spawn";
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool preventDuplicate = true;

    [Header("플레이어 진입 조건")]
    [SerializeField] private bool spawnWhenPlayerEntersRange = true;
    [SerializeField, Min(0.5f)] private float playerEnterRange = 10f;
    [SerializeField, Min(0.5f)] private float cinematicStopRange = 4.2f;
    [SerializeField] private bool requirePlayerBodyState = true;
    [SerializeField] private bool requirePlayerGrounded = true;
    [SerializeField] private Transform activationTarget;

    [Header("실행 확인")]
    [SerializeField] private GameObject spawnedBoss;
    [SerializeField] private int spawnCount;
    [SerializeField] private bool usedExistingBoss;
    [SerializeField] private string lastSpawnResult;
    [SerializeField] private Vector3 lastSpawnPosition;
    [SerializeField] private Quaternion lastSpawnRotation = Quaternion.identity;
    [SerializeField] private bool waitingForPlayer;
    [SerializeField] private float lastPlayerDistance = float.PositiveInfinity;
    [SerializeField] private bool spawnCommitted;
    [SerializeField] private bool encounterCompleted;
    [SerializeField] private bool waitingForPlayerLanding;

    private float nextTargetSearchTime;

    public GameObject SpawnedBoss => spawnedBoss;
    public GameObject BossPrefab => bossPrefab;
    public hys_MidBoss2SpawnPoint SpawnPoint => spawnPoint;
    public string SpawnPointId => spawnPointId;
    public int SpawnCount => spawnCount;
    public bool UsedExistingBoss => usedExistingBoss;
    public string LastSpawnResult => lastSpawnResult;
    public Vector3 LastSpawnPosition => lastSpawnPosition;
    public Quaternion LastSpawnRotation => lastSpawnRotation;
    public bool SpawnWhenPlayerEntersRange => spawnWhenPlayerEntersRange;
    public float PlayerEnterRange => EffectivePlayerEnterRange;
    public float CinematicStopRange => cinematicStopRange;
    public bool WaitingForPlayer => waitingForPlayer;
    public float LastPlayerDistance => lastPlayerDistance;
    public Transform ActivationTarget => activationTarget;
    public bool SpawnCommitted => spawnCommitted;
    public bool EncounterCompleted => encounterCompleted;
    public bool WaitingForPlayerLanding => waitingForPlayerLanding;

    public void Initialize(GameObject prefab, hys_MidBoss2SpawnPoint point)
    {
        bossPrefab = prefab;
        spawnPoint = point;
        if (point != null && !string.IsNullOrWhiteSpace(point.SpawnId))
            spawnPointId = point.SpawnId;
    }

    public void ConfigurePlayerEntry(float range, bool requireBodyState)
    {
        spawnWhenPlayerEntersRange = true;
        playerEnterRange = Mathf.Max(0.5f, range);
        requirePlayerBodyState = requireBodyState;
    }

    private void Start()
    {
        if (!spawnOnStart) return;
        if (hys_MidBoss2EncounterSession.IsDefeated(this))
        {
            encounterCompleted = true;
            waitingForPlayer = false;
            lastSpawnResult = "이미 처치한 중간보스2는 재입장 시 다시 생성하지 않습니다.";
            return;
        }
        if (spawnWhenPlayerEntersRange)
        {
            waitingForPlayer = true;
            return;
        }

        TrySpawnBoss();
    }

    private void Update()
    {
        UpdateCommittedBossState();
        if (!spawnOnStart || !spawnWhenPlayerEntersRange || encounterCompleted
            || spawnCommitted || spawnedBoss != null) return;

        waitingForPlayer = true;
        ResolveSpawnPoint();
        ResolveActivationTarget();
        if (spawnPoint == null || activationTarget == null) return;

        lastPlayerDistance = Vector2.Distance(activationTarget.position, spawnPoint.Position);
        // 넓은 감지 반경이 직렬화돼 있어도 연출 지점까지 직접 걸어온 뒤에만 보스를 생성합니다.
        if (lastPlayerDistance > EffectivePlayerEnterRange)
        {
            waitingForPlayerLanding = false;
            return;
        }
        if (!IsTargetReady()) return;

        // 점프 중 범위에 들어오면 공중에서 고정하지 않고 실제 착지 프레임까지 기다립니다.
        waitingForPlayerLanding = requirePlayerGrounded && !IsTargetGrounded();
        if (waitingForPlayerLanding) return;

        TrySpawnBoss();
    }

    public bool TrySpawnBoss()
    {
        UpdateCommittedBossState();
        if (encounterCompleted || hys_MidBoss2EncounterSession.IsDefeated(this))
        {
            encounterCompleted = true;
            waitingForPlayer = false;
            lastSpawnResult = "처치 완료 상태이므로 중간보스2를 생성하지 않았습니다.";
            return false;
        }
        if (spawnCommitted)
        {
            lastSpawnResult = spawnedBoss != null
                ? "이미 생성한 중간보스2를 유지합니다."
                : "이 씬에서 이미 생성한 보스는 중복 생성하지 않습니다.";
            return spawnedBoss != null;
        }

        ResolveSpawnPoint();
        if (bossPrefab == null || spawnPoint == null)
        {
            lastSpawnResult = "보스 프리팹 또는 스폰포인트가 없습니다.";
            Debug.LogError(lastSpawnResult, this);
            return false;
        }

        if (preventDuplicate)
        {
            hys_SecondBossLogic existingBoss = FindFirstObjectByType<hys_SecondBossLogic>(
                FindObjectsInactive.Exclude);
            if (existingBoss != null)
            {
                spawnedBoss = existingBoss.gameObject;
                spawnCommitted = true;
                usedExistingBoss = true;
                waitingForPlayer = false;
                lastSpawnResult = "이미 존재하는 중간보스2를 사용했습니다.";
                return true;
            }
        }

        HWJ_SpawnPoint sharedPoint = spawnPoint.SharedSpawnPoint;
        Transform spawnParent = sharedPoint != null ? sharedPoint.SpawnParent : null;
        // AI나 물리가 움직이기 전, 실제 생성에 사용한 좌표와 방향을 기록합니다.
        lastSpawnPosition = spawnPoint.Position;
        lastSpawnRotation = spawnPoint.Rotation;
        spawnedBoss = Instantiate(
            bossPrefab,
            lastSpawnPosition,
            lastSpawnRotation,
            spawnParent);
        if (spawnedBoss == null)
        {
            lastSpawnResult = "중간보스2 생성에 실패했습니다.";
            Debug.LogError(lastSpawnResult, this);
            return false;
        }

        spawnedBoss.name = "hys_MidBoss2_SceneBoss";
        spawnCommitted = true;
        spawnCount++;
        usedExistingBoss = false;
        waitingForPlayer = false;
        lastSpawnResult = $"중간보스2 생성 완료: {spawnPoint.SpawnId}";
        return true;
    }

    private void UpdateCommittedBossState()
    {
        if (encounterCompleted) return;
        if (spawnedBoss != null)
        {
            HWJ_RuntimeStatusSystem status = spawnedBoss.GetComponent<HWJ_RuntimeStatusSystem>();
            if (status == null || !status.IsDead) return;
            encounterCompleted = true;
            waitingForPlayer = false;
            hys_MidBoss2EncounterSession.MarkDefeated(this);
            lastSpawnResult = "중간보스2 처치 완료 - 재소환을 차단했습니다.";
            return;
        }

        // 한 번 생성한 인스턴스가 제거되어도 같은 씬에서 새 보스를 중복 생성하지 않습니다.
        if (spawnCommitted)
        {
            waitingForPlayer = false;
            lastSpawnResult = "생성 완료 인스턴스가 제거되어 재소환을 차단했습니다.";
        }
    }

    private void ResolveActivationTarget()
    {
        if (activationTarget != null || Time.time < nextTargetSearchTime) return;
        nextTargetSearchTime = Time.time + 0.25f;

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            activationTarget = HWJ_GameAccess.Manager.PlayerResolver.transform;
            return;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsSortMode.None);
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                activationTarget = resolvers[i].transform;
                return;
            }
        }
    }

    private bool IsTargetReady()
    {
        if (!requirePlayerBodyState || activationTarget == null) return activationTarget != null;
        HWJ_SoulSystem soulSystem = activationTarget.GetComponentInParent<HWJ_SoulSystem>();
        return soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body;
    }

    private bool IsTargetGrounded()
    {
        if (activationTarget == null) return false;
        hys_Player_Movement hysMovement = activationTarget.GetComponentInParent<hys_Player_Movement>();
        if (hysMovement != null) return hysMovement.Is_Grounded;

        HWJ_PlayerMovementSystem hwjMovement =
            activationTarget.GetComponentInParent<HWJ_PlayerMovementSystem>();
        if (hwjMovement != null) return hwjMovement.IsGrounded;

        Rigidbody2D playerBody = activationTarget.GetComponentInParent<Rigidbody2D>();
        return playerBody == null || Mathf.Abs(playerBody.linearVelocity.y) <= 0.05f;
    }

    private void ResolveSpawnPoint()
    {
        if (spawnPoint != null) return;
        hys_MidBoss2SpawnPoint[] points = FindObjectsByType<hys_MidBoss2SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null
                && string.Equals(points[i].SpawnId, spawnPointId, System.StringComparison.OrdinalIgnoreCase))
            {
                spawnPoint = points[i];
                return;
            }
        }

        if (points.Length > 0) spawnPoint = points[0];
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = spawnPoint != null ? spawnPoint.transform : transform;
        Gizmos.color = new Color(0.75f, 0.12f, 0.85f, 0.8f);
        Gizmos.DrawWireSphere(center.position, EffectivePlayerEnterRange);
    }

    private float EffectivePlayerEnterRange => Mathf.Min(
        Mathf.Max(0.5f, playerEnterRange),
        Mathf.Max(0.5f, cinematicStopRange));
}
