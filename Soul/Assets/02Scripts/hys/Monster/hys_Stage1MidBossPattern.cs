using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hys_Stage1MidBossPattern : MonoBehaviour
{
    // 중간보스의 공격 패턴만 관리합니다.
    // 기본 이동, 추격, 보스전 시작 조건은 hys_Stage1MidBossLogic에서 처리합니다.

    [Header("References")]
    // HWJ 전투 시스템을 사용해서 데미지 계산과 피격 처리를 기존 구조에 맞춥니다.
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private Rigidbody2D rb;

    [Header("Common")]
    // 경고 범위와 공통 데미지 배율입니다. 실제 화살 프리팹은 나중에 추가할 수 있게 범위 판정으로 먼저 구성했습니다.
    [SerializeField] private Color warningColor = new Color(1f, 0.12f, 0.05f, 0.85f);
    [SerializeField] private Color activeLaserColor = new Color(1f, 0.02f, 0.02f, 0.95f);
    [SerializeField] private float warningLineWidth = 0.06f;
    [SerializeField] private float warningSeconds = 0.85f;
    [SerializeField] private float arrowHitRadius = 0.75f;
    [SerializeField] private float arrowDamageMultiplier = 1f;
    [SerializeField] private float patternCooldownSeconds = 1f;

    [Header("Pattern 1")]
    // 보스가 공중으로 올라가고, 양쪽 분신과 함께 아래 방향 화살을 동시에 쏩니다.
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float aerialHeight = 3f;
    [SerializeField] private float cloneEdgePadding = 1f;

    [Header("Pattern 2")]
    // 플레이어 위치를 따라가며 낙하 화살을 여러 번 떨어뜨립니다.
    [SerializeField] private int trackingArrowCount = 5;
    [SerializeField] private float trackingArrowIntervalSeconds = 0.35f;

    [Header("Pattern 3")]
    // 플레이어 위치에 예고 범위를 만들고 일정 시간 장판 데미지를 줍니다.
    [SerializeField] private float rainZoneRadius = 2.2f;
    [SerializeField] private float rainZoneDurationSeconds = 3f;
    [SerializeField] private float rainZoneTickSeconds = 0.35f;

    [Header("Pattern 4")]
    // 육신 시간이 얼마 남지 않았을 때 먼 사이드로 이동하고 소환 + 가로 화살을 사용합니다.
    [SerializeField] private GameObject[] possessableMonsterPrefabs;
    [SerializeField] private int minPossessableMonsterSpawnCount = 2;
    [SerializeField] private int maxPossessableMonsterSpawnCount = 3;
    [SerializeField] private float pattern4BodyTimeThresholdSeconds = 30f;
    [SerializeField] private float pattern4BossHpRatioThreshold = 0.9f;
    [SerializeField] private float playerBodyRemainingSeconds = 999f;
    [SerializeField] private bool useManualBodyRemainingSeconds;
    [SerializeField] private float horizontalArrowHeightAboveFloor = 2.6f;
    [SerializeField] private float horizontalArrowChargeSeconds = 1.15f;
    [SerializeField] private float horizontalArrowThickness = 0.65f;

    [Header("Pattern 5")]
    // 체력 30% 조건에서 사용하는 전체 화살비와 그로기 시간입니다.
    [SerializeField] private float pattern5RainDurationSeconds = 10f;
    [SerializeField] private float pattern5RainIntervalSeconds = 0.45f;
    [SerializeField] private float pattern5WarningSeconds = 0.45f;
    [SerializeField] private float pattern5SafeGapWidth = 2.2f;
    [SerializeField] private float pattern5GroggySeconds = 3f;

    [Header("Phase 2 Laser")]
    // 페이즈2 진입 시 한 번만 쓰는 레이저 화살 패턴입니다.
    [SerializeField] private int phase2LaserLaneCount = 7;
    [SerializeField] private int phase2LaserSafeLaneCount = 1;
    [SerializeField] private float phase2LaserWarningSeconds = 1.15f;
    [SerializeField] private float phase2LaserActiveSeconds = 0.8f;
    [SerializeField] private float phase2LaserDamageMultiplier = 2.4f;
    [SerializeField] private float phase2LaserLineWidth = 0.14f;

    [Header("Debug")]
    [SerializeField] private string lastPatternAction;

    private readonly List<GameObject> spawnedHelpers = new List<GameObject>();
    private readonly List<Coroutine> delayedDamageRoutines = new List<Coroutine>();
    private Coroutine activeRoutine;
    private Transform currentTarget;
    private Vector2 currentRoomCenter;
    private Vector2 currentRoomSize;
    private Action onPatternFinished;
    private Action<float> onGroggyStarted;

    public bool IsPatternRunning => activeRoutine != null;
    public float PatternCooldownSeconds => patternCooldownSeconds;
    public string LastPatternAction => lastPatternAction;

    private void Awake()
    {
        CacheReferences();
    }

    public bool CanUsePattern4(Transform target, float bossHpRatio)
    {
        if (target == null || bossHpRatio > pattern4BossHpRatioThreshold)
        {
            return false;
        }

        return GetPlayerBodyRemainingSeconds(target) <= pattern4BodyTimeThresholdSeconds;
    }

    public void SetPlayerBodyRemainingSeconds(float seconds)
    {
        // 외부 시스템에서 육신 남은 시간을 계산해 넘기고 싶을 때 사용합니다.
        playerBodyRemainingSeconds = Mathf.Max(0f, seconds);
        useManualBodyRemainingSeconds = true;
    }

    public bool TryStartPattern(int patternNumber, Transform target, Vector2 roomCenter, Vector2 roomSize, Action finishedCallback)
    {
        if (activeRoutine != null)
        {
            return false;
        }

        PreparePattern(target, roomCenter, roomSize, finishedCallback, null);

        switch (patternNumber)
        {
            case 1:
                activeRoutine = StartCoroutine(Pattern1Routine());
                return true;
            case 2:
                activeRoutine = StartCoroutine(Pattern2Routine());
                return true;
            case 3:
                activeRoutine = StartCoroutine(Pattern3Routine());
                return true;
            case 4:
                activeRoutine = StartCoroutine(Pattern4Routine());
                return true;
            default:
                FinishPattern();
                return false;
        }
    }

    public bool TryStartPattern5(
        Transform target,
        Vector2 roomCenter,
        Vector2 roomSize,
        Action<float> groggyStartedCallback,
        Action finishedCallback)
    {
        if (activeRoutine != null)
        {
            return false;
        }

        PreparePattern(target, roomCenter, roomSize, finishedCallback, groggyStartedCallback);
        activeRoutine = StartCoroutine(Pattern5Routine());
        return true;
    }

    public bool TryStartPhaseTwoLaser(Transform target, Vector2 roomCenter, Vector2 roomSize, Action finishedCallback)
    {
        if (activeRoutine != null)
        {
            return false;
        }

        PreparePattern(target, roomCenter, roomSize, finishedCallback, null);
        activeRoutine = StartCoroutine(PhaseTwoLaserRoutine());
        return true;
    }

    public void CancelCurrentPattern()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        for (int i = delayedDamageRoutines.Count - 1; i >= 0; i--)
        {
            if (delayedDamageRoutines[i] != null)
            {
                StopCoroutine(delayedDamageRoutines[i]);
            }
        }

        delayedDamageRoutines.Clear();

        ClearHelpers();
        StopBodyMovement();
        currentTarget = null;
        onPatternFinished = null;
        onGroggyStarted = null;
    }

    public void ResetForEncounter()
    {
        // 재도전 전에 이전 전투에서 생성된 공격과 보조 오브젝트를 모두 제거합니다.
        CancelCurrentPattern();
        lastPatternAction = "전투 초기화";
    }

    private void PreparePattern(
        Transform target,
        Vector2 roomCenter,
        Vector2 roomSize,
        Action finishedCallback,
        Action<float> groggyStartedCallback)
    {
        CacheReferences();
        currentTarget = target;
        currentRoomCenter = roomCenter;
        currentRoomSize = new Vector2(Mathf.Max(1f, roomSize.x), Mathf.Max(1f, roomSize.y));
        onPatternFinished = finishedCallback;
        onGroggyStarted = groggyStartedCallback;
        StopBodyMovement();
    }

    private IEnumerator Pattern1Routine()
    {
        lastPatternAction = "패턴 1: 분신 동시 화살";

        Vector3 startPosition = transform.position;
        Vector3 airPosition = startPosition + Vector3.up * Mathf.Max(0f, aerialHeight);
        SetPosition(airPosition);

        float leftX = currentRoomCenter.x - currentRoomSize.x * 0.5f + cloneEdgePadding;
        float rightX = currentRoomCenter.x + currentRoomSize.x * 0.5f - cloneEdgePadding;
        GameObject leftClone = CreateClone(new Vector3(leftX, airPosition.y, airPosition.z));
        GameObject rightClone = CreateClone(new Vector3(rightX, airPosition.y, airPosition.z));

        Vector3 bossStrike = currentTarget != null ? currentTarget.position : startPosition;
        Vector3 leftStrike = new Vector3(leftX, bossStrike.y, bossStrike.z);
        Vector3 rightStrike = new Vector3(rightX, bossStrike.y, bossStrike.z);

        ShowCircleWarning(bossStrike, arrowHitRadius, warningSeconds);
        ShowCircleWarning(leftStrike, arrowHitRadius, warningSeconds);
        ShowCircleWarning(rightStrike, arrowHitRadius, warningSeconds);

        yield return new WaitForSeconds(Mathf.Max(0.01f, warningSeconds));

        DamageTargetIfInsideCircle(bossStrike, arrowHitRadius, arrowDamageMultiplier);
        DamageTargetIfInsideCircle(leftStrike, arrowHitRadius, arrowDamageMultiplier);
        DamageTargetIfInsideCircle(rightStrike, arrowHitRadius, arrowDamageMultiplier);

        DestroyHelper(leftClone);
        DestroyHelper(rightClone);
        SetPosition(startPosition);
        FinishPattern();
    }

    private IEnumerator Pattern2Routine()
    {
        lastPatternAction = "패턴 2: 추적 낙하 화살";
        int count = Mathf.Max(1, trackingArrowCount);

        for (int i = 0; i < count; i++)
        {
            Vector3 strikePosition = currentTarget != null ? currentTarget.position : transform.position;
            ShowCircleWarning(strikePosition, arrowHitRadius, warningSeconds);
            yield return new WaitForSeconds(Mathf.Max(0.01f, warningSeconds));
            DamageTargetIfInsideCircle(strikePosition, arrowHitRadius, arrowDamageMultiplier);

            if (i < count - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, trackingArrowIntervalSeconds));
            }
        }

        FinishPattern();
    }

    private IEnumerator Pattern3Routine()
    {
        lastPatternAction = "패턴 3: 화살비 장판";

        Vector3 zoneCenter = currentTarget != null ? currentTarget.position : transform.position;
        ShowCircleWarning(zoneCenter, rainZoneRadius, warningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, warningSeconds));

        float endTime = Time.time + Mathf.Max(0.01f, rainZoneDurationSeconds);
        float tickSeconds = Mathf.Max(0.05f, rainZoneTickSeconds);

        while (Time.time < endTime)
        {
            ShowCircleWarning(zoneCenter, rainZoneRadius, tickSeconds);
            DamageTargetIfInsideCircle(zoneCenter, rainZoneRadius, arrowDamageMultiplier);
            yield return new WaitForSeconds(tickSeconds);
        }

        FinishPattern();
    }

    private IEnumerator Pattern4Routine()
    {
        lastPatternAction = "패턴 4: 순간이동 가로 화살";

        float leftX = currentRoomCenter.x - currentRoomSize.x * 0.5f + cloneEdgePadding;
        float rightX = currentRoomCenter.x + currentRoomSize.x * 0.5f - cloneEdgePadding;
        float targetX = currentTarget != null ? currentTarget.position.x : currentRoomCenter.x;
        float teleportX = Mathf.Abs(targetX - leftX) > Mathf.Abs(targetX - rightX) ? leftX : rightX;

        SetPosition(new Vector3(teleportX, transform.position.y, transform.position.z));
        SpawnPossessableMonsters();

        float floorY = currentRoomCenter.y - currentRoomSize.y * 0.5f;
        float arrowY = floorY + Mathf.Max(0.1f, horizontalArrowHeightAboveFloor);
        float direction = teleportX < currentRoomCenter.x ? 1f : -1f;
        float length = Mathf.Max(1f, currentRoomSize.x - cloneEdgePadding * 2f);
        Vector3 start = new Vector3(direction > 0f ? leftX : rightX, arrowY, transform.position.z);

        HWJ_SkillWarningIndicator.ShowArrowPath(
            start,
            direction,
            length,
            horizontalArrowThickness,
            horizontalArrowChargeSeconds,
            warningColor,
            warningLineWidth);

        yield return new WaitForSeconds(Mathf.Max(0.01f, horizontalArrowChargeSeconds));
        DamageTargetIfInsideHorizontalLine(arrowY, horizontalArrowThickness, arrowDamageMultiplier * 1.2f);
        FinishPattern();
    }

    private IEnumerator Pattern5Routine()
    {
        lastPatternAction = "패턴 5: 전체 화살비 후 그로기";

        Vector3 airCenter = new Vector3(
            currentRoomCenter.x,
            currentRoomCenter.y + currentRoomSize.y * 0.25f,
            transform.position.z);
        SetPosition(airCenter);

        float endTime = Time.time + Mathf.Max(0.1f, pattern5RainDurationSeconds);
        float interval = Mathf.Max(0.05f, pattern5RainIntervalSeconds);
        float leftX = currentRoomCenter.x - currentRoomSize.x * 0.5f + cloneEdgePadding;
        float rightX = currentRoomCenter.x + currentRoomSize.x * 0.5f - cloneEdgePadding;
        float floorY = currentRoomCenter.y - currentRoomSize.y * 0.5f + 0.8f;

        while (Time.time < endTime)
        {
            float safeX = UnityEngine.Random.Range(leftX, rightX);

            for (float x = leftX; x <= rightX; x += Mathf.Max(1f, arrowHitRadius * 2.2f))
            {
                if (Mathf.Abs(x - safeX) <= pattern5SafeGapWidth * 0.5f)
                {
                    continue;
                }

                Vector3 strikePosition = new Vector3(x, floorY, transform.position.z);
                ShowCircleWarning(strikePosition, arrowHitRadius, pattern5WarningSeconds);
                Coroutine delayedRoutine = StartCoroutine(
                    DelayedDamageCircle(strikePosition, arrowHitRadius, pattern5WarningSeconds));
                delayedDamageRoutines.Add(delayedRoutine);
            }

            yield return new WaitForSeconds(interval);
        }

        onGroggyStarted?.Invoke(pattern5GroggySeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern5GroggySeconds));
        FinishPattern();
    }

    private IEnumerator PhaseTwoLaserRoutine()
    {
        lastPatternAction = "페이즈 2 전환: 레이저 화살";

        List<Rect> dangerLanes = BuildPhaseTwoDangerLanes();

        for (int i = 0; i < dangerLanes.Count; i++)
        {
            ShowRectangleWarning(dangerLanes[i], phase2LaserWarningSeconds, warningColor, warningLineWidth);
        }

        yield return new WaitForSeconds(Mathf.Max(0.01f, phase2LaserWarningSeconds));

        for (int i = 0; i < dangerLanes.Count; i++)
        {
            ShowRectangleWarning(dangerLanes[i], phase2LaserActiveSeconds, activeLaserColor, phase2LaserLineWidth);
        }

        DamageTargetIfInsideLanes(dangerLanes, phase2LaserDamageMultiplier);
        yield return new WaitForSeconds(Mathf.Max(0.01f, phase2LaserActiveSeconds));
        FinishPattern();
    }

    private IEnumerator DelayedDamageCircle(Vector3 center, float radius, float delaySeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delaySeconds));
        DamageTargetIfInsideCircle(center, radius, arrowDamageMultiplier);
    }

    private void FinishPattern()
    {
        activeRoutine = null;
        delayedDamageRoutines.Clear();
        Action finishedCallback = onPatternFinished;
        onPatternFinished = null;
        onGroggyStarted = null;
        finishedCallback?.Invoke();
    }

    private void CacheReferences()
    {
        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private float GetPlayerBodyRemainingSeconds(Transform target)
    {
        // HWJ 육신 부패 값을 초 단위로 바꿔서 패턴 4 조건에 사용합니다.
        if (useManualBodyRemainingSeconds)
        {
            return playerBodyRemainingSeconds;
        }

        if (target == null)
        {
            return float.MaxValue;
        }

        HWJ_BodyDecaySystem bodyDecay = target.GetComponent<HWJ_BodyDecaySystem>();

        if (bodyDecay == null)
        {
            bodyDecay = target.GetComponentInParent<HWJ_BodyDecaySystem>();
        }

        if (bodyDecay == null)
        {
            return float.MaxValue;
        }

        HWJ_RootObjectDataResolver playerResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (playerResolver == null)
        {
            playerResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (playerResolver == null || !playerResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData) || playerData.BodyDecay == null)
        {
            return bodyDecay.CurrentDecayValue;
        }

        float tickAmount = Mathf.Max(0.01f, playerData.BodyDecay.decayAmountPerTick);
        float tickSeconds = Mathf.Max(0.01f, playerData.BodyDecay.decayTickSeconds);
        return bodyDecay.CurrentDecayValue / tickAmount * tickSeconds;
    }

    private void SetPosition(Vector3 position)
    {
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        transform.position = position;
    }

    private void StopBodyMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private GameObject CreateClone(Vector3 position)
    {
        if (clonePrefab == null)
        {
            // 보스 전체가 복제되는 사고를 막기 위해 분신 프리팹이 없으면 생성하지 않습니다.
            Debug.LogWarning("중간보스 분신 프리팹이 연결되지 않았습니다.", this);
            return null;
        }

        GameObject helper = Instantiate(clonePrefab, position, transform.rotation);

        hys_Stage1MidBossLogic cloneLogic = helper.GetComponent<hys_Stage1MidBossLogic>();
        hys_Stage1MidBossPattern clonePattern = helper.GetComponent<hys_Stage1MidBossPattern>();

        if (cloneLogic != null)
        {
            cloneLogic.enabled = false;
        }

        if (clonePattern != null)
        {
            clonePattern.enabled = false;
        }

        spawnedHelpers.Add(helper);
        return helper;
    }

    private void SpawnPossessableMonsters()
    {
        if (possessableMonsterPrefabs == null || possessableMonsterPrefabs.Length == 0)
        {
            return;
        }

        int count = UnityEngine.Random.Range(
            Mathf.Max(0, minPossessableMonsterSpawnCount),
            Mathf.Max(minPossessableMonsterSpawnCount, maxPossessableMonsterSpawnCount) + 1);

        float leftX = currentRoomCenter.x - currentRoomSize.x * 0.35f;
        float rightX = currentRoomCenter.x + currentRoomSize.x * 0.35f;
        float floorY = currentRoomCenter.y - currentRoomSize.y * 0.5f + 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = possessableMonsterPrefabs[UnityEngine.Random.Range(0, possessableMonsterPrefabs.Length)];

            if (prefab == null)
            {
                continue;
            }

            Vector3 position = new Vector3(UnityEngine.Random.Range(leftX, rightX), floorY, transform.position.z);
            spawnedHelpers.Add(Instantiate(prefab, position, Quaternion.identity));
        }
    }

    private void ClearHelpers()
    {
        for (int i = spawnedHelpers.Count - 1; i >= 0; i--)
        {
            DestroyHelper(spawnedHelpers[i]);
        }

        spawnedHelpers.Clear();
    }

    private void DestroyHelper(GameObject helper)
    {
        if (helper != null)
        {
            spawnedHelpers.Remove(helper);
            Destroy(helper);
        }
    }

    private void DamageTargetIfInsideCircle(Vector3 center, float radius, float damageMultiplier)
    {
        if (currentTarget == null || Vector2.Distance(currentTarget.position, center) > radius)
        {
            return;
        }

        DealDamageToTarget(damageMultiplier);
    }

    private void DamageTargetIfInsideHorizontalLine(float lineY, float thickness, float damageMultiplier)
    {
        if (currentTarget == null)
        {
            return;
        }

        float halfHeight = Mathf.Max(0.05f, thickness * 0.5f);
        float halfWidth = currentRoomSize.x * 0.5f;
        Vector3 targetPosition = currentTarget.position;

        bool insideX = targetPosition.x >= currentRoomCenter.x - halfWidth && targetPosition.x <= currentRoomCenter.x + halfWidth;
        bool insideY = Mathf.Abs(targetPosition.y - lineY) <= halfHeight;

        if (insideX && insideY)
        {
            DealDamageToTarget(damageMultiplier);
        }
    }

    private void DamageTargetIfInsideLanes(List<Rect> lanes, float damageMultiplier)
    {
        if (currentTarget == null || lanes == null)
        {
            return;
        }

        Vector2 targetPosition = currentTarget.position;

        for (int i = 0; i < lanes.Count; i++)
        {
            if (lanes[i].Contains(targetPosition))
            {
                DealDamageToTarget(damageMultiplier);
                return;
            }
        }
    }

    private void DealDamageToTarget(float damageMultiplier)
    {
        HWJ_RootObjectDataResolver targetResolver = currentTarget != null
            ? currentTarget.GetComponentInParent<HWJ_RootObjectDataResolver>()
            : null;

        if (targetResolver != null && combatSystem != null)
        {
            combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _);
        }
    }

    private List<Rect> BuildPhaseTwoDangerLanes()
    {
        List<Rect> dangerLanes = new List<Rect>();
        int laneCount = Mathf.Max(1, phase2LaserLaneCount);
        int safeCount = Mathf.Clamp(phase2LaserSafeLaneCount, 0, laneCount);
        int safeStart = safeCount > 0 ? UnityEngine.Random.Range(0, laneCount - safeCount + 1) : -1;
        float laneWidth = currentRoomSize.x / laneCount;
        float left = currentRoomCenter.x - currentRoomSize.x * 0.5f;
        float bottom = currentRoomCenter.y - currentRoomSize.y * 0.5f;

        for (int i = 0; i < laneCount; i++)
        {
            bool isSafeLane = safeCount > 0 && i >= safeStart && i < safeStart + safeCount;

            if (isSafeLane)
            {
                continue;
            }

            dangerLanes.Add(new Rect(left + laneWidth * i, bottom, laneWidth, currentRoomSize.y));
        }

        return dangerLanes;
    }

    private void ShowCircleWarning(Vector3 center, float radius, float durationSeconds)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            center,
            radius,
            durationSeconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowRectangleWarning(Rect rect, float durationSeconds, Color color, float lineWidth)
    {
        Vector3 center = new Vector3(rect.center.x, rect.center.y, transform.position.z);
        Vector2 size = new Vector2(rect.width, rect.height);
        HWJ_SkillWarningIndicator.ShowRectangle(center, size, durationSeconds, color, lineWidth);
    }

    private void OnDisable()
    {
        CancelCurrentPattern();
    }
}
