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
    [SerializeField] private bool requirePlayerBodyState = true;
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
    public float PlayerEnterRange => playerEnterRange;
    public bool WaitingForPlayer => waitingForPlayer;
    public float LastPlayerDistance => lastPlayerDistance;
    public Transform ActivationTarget => activationTarget;

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
        if (spawnWhenPlayerEntersRange)
        {
            waitingForPlayer = true;
            return;
        }

        TrySpawnBoss();
    }

    private void Update()
    {
        if (!spawnOnStart || !spawnWhenPlayerEntersRange || spawnedBoss != null) return;

        waitingForPlayer = true;
        ResolveSpawnPoint();
        ResolveActivationTarget();
        if (spawnPoint == null || activationTarget == null) return;

        lastPlayerDistance = Vector2.Distance(activationTarget.position, spawnPoint.Position);
        if (lastPlayerDistance > Mathf.Max(0.5f, playerEnterRange) || !IsTargetReady()) return;

        TrySpawnBoss();
    }

    public bool TrySpawnBoss()
    {
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
        spawnCount++;
        usedExistingBoss = false;
        waitingForPlayer = false;
        lastSpawnResult = $"중간보스2 생성 완료: {spawnPoint.SpawnId}";
        return true;
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
        Gizmos.DrawWireSphere(center.position, Mathf.Max(0.5f, playerEnterRange));
    }
}
