using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HWJ_Stage1BossPatternSystem : MonoBehaviour, HWJ_IBossSpecialPatternExecutor

{
    [Header("References")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private Rigidbody2D body;

    [Header("Common")]
    [SerializeField] private Color warningColor = new Color(1f, 0.1f, 0.05f, 0.85f);
    [SerializeField] private float warningLineWidth = 0.06f;
    [SerializeField] private float warningSeconds = 0.85f;
    [SerializeField] private float arrowDamageMultiplier = 1f;
    [SerializeField] private float arrowHitRadius = 0.75f;
    [SerializeField] private float aerialHeight = 3f;

    [Header("Pattern 2")]
    [SerializeField] private int trackingArrowCount = 5;
    [SerializeField] private float trackingArrowIntervalSeconds = 0.35f;

    [Header("Pattern 3")]
    [SerializeField] private float rainZoneRadius = 2.2f;
    [SerializeField] private float rainZoneDurationSeconds = 3f;
    [SerializeField] private float rainZoneTickSeconds = 0.35f;

    [Header("Pattern 4")]
    [SerializeField] private GameObject[] possessableMonsterPrefabs;
    [SerializeField] private HWJ_RootObjectDataSO[] possessableMonsterRootObjects;
    [SerializeField] private int minPossessableMonsterSpawnCount = 2;
    [SerializeField] private int maxPossessableMonsterSpawnCount = 3;
    [InspectorName("패턴4 정신력 잔여 시간 조건")]
    [Tooltip("플레이어의 빙의체 정신력으로 계산한 남은 시간이 이 값 이하일 때 패턴4 조건을 만족합니다.")]
    [SerializeField] private float pattern4BodyTimeThresholdSeconds = 30f;
    [SerializeField] private float pattern4BossHpRatioThreshold = 0.9f;
    [SerializeField] private float pattern4ArrowHeightAboveFloor = 2.6f;
    [SerializeField] private float pattern4ChargeSeconds = 1.15f;
    [SerializeField] private float pattern4ArrowThickness = 0.65f;

    [Header("Pattern 5")]
    [SerializeField] private float pattern5HpRatioThreshold = 0.3f;
    [SerializeField] private float pattern5RainDurationSeconds = 10f;
    [SerializeField] private float pattern5RainIntervalSeconds = 0.45f;
    [SerializeField] private float pattern5WarningSeconds = 0.45f;
    [SerializeField] private float pattern5SafeGapWidth = 2.2f;
    [SerializeField] private float pattern5GroggySeconds = 3f;

    [Header("Phase 2 Transition Laser")]
    [SerializeField] private bool usePhase2TransitionLaser = true;
    [SerializeField] private int phase2LaserLaneCount = 7;
    [SerializeField] private int phase2LaserSafeLaneCount = 1;
    [SerializeField] private float phase2LaserRoomEdgePadding = 0.4f;
    [SerializeField] private float phase2LaserDialogueHoldSeconds = 1f;
    [SerializeField] private float phase2LaserWarningSeconds = 1.15f;
    [SerializeField] private float phase2LaserActiveSeconds = 0.8f;
    [SerializeField] private float phase2LaserDamageCheckSeconds = 0.05f;
    [SerializeField] private float phase2LaserRecoverySeconds = 0.35f;
    [SerializeField] private float phase2LaserDamageMultiplier = 2.4f;
    [SerializeField] private float phase2LaserActiveLineWidth = 0.14f;
    [SerializeField] private Color phase2LaserActiveColor = new Color(1f, 0.02f, 0.02f, 0.95f);

    private readonly List<GameObject> spawnedHelpers = new List<GameObject>();
    private Coroutine activePatternRoutine;
    private bool pattern5Used;
    private bool phase2TransitionLaserUsed;

    public string ExecutorKey => "stage1_boss";
    public bool IsPatternRunning => activePatternRoutine != null;

    private void Awake()
    {
        CacheReferences();
    }

    public bool CanUsePattern(int patternNumber, Transform target)
    {
        CacheReferences();

        if (activePatternRoutine != null || runtimeStatus != null && runtimeStatus.IsDead)
        {
            return false;
        }

        switch (patternNumber)
        {
            case 4:
                return CanUsePattern4(target);
            case 5:
                return CanUsePattern5();
            default:
                return patternNumber >= 1 && patternNumber <= 5;
        }
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        return pattern != null
            && pattern.UseStageOneSpecialExecution
            && pattern.PatternNumber > 0
            && CanUsePattern(pattern.PatternNumber, target);
    }

    public bool TryExecutePattern(int patternNumber, Transform target)
    {
        if (!CanUsePattern(patternNumber, target))
        {
            return false;
        }

        switch (patternNumber)
        {
            case 1:
                activePatternRoutine = StartCoroutine(Pattern1Routine(target));
                return true;
            case 2:
                activePatternRoutine = StartCoroutine(Pattern2Routine(target));
                return true;
            case 3:
                activePatternRoutine = StartCoroutine(Pattern3Routine(target));
                return true;
            case 4:
                activePatternRoutine = StartCoroutine(Pattern4Routine(target));
                return true;
            case 5:
                pattern5Used = true;
                activePatternRoutine = StartCoroutine(Pattern5Routine(target));
                return true;
            default:
                return false;
        }
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        return pattern != null
            && pattern.UseStageOneSpecialExecution
            && TryExecutePattern(pattern.PatternNumber, target);
    }

    public bool TryExecutePhaseTwoTransitionLaser(Transform target, out float totalSeconds)
    {
        CacheReferences();
        totalSeconds = GetPhaseTwoTransitionLaserTotalSeconds();

        if (!usePhase2TransitionLaser
            || phase2TransitionLaserUsed
            || activePatternRoutine != null
            || runtimeStatus != null && runtimeStatus.IsDead)
        {
            totalSeconds = 0f;
            return false;
        }

        phase2TransitionLaserUsed = true;
        activePatternRoutine = StartCoroutine(PhaseTwoTransitionLaserRoutine(target));
        return true;
    }

    public void CancelActivePattern()
    {
        if (activePatternRoutine != null)
        {
            StopCoroutine(activePatternRoutine);
            activePatternRoutine = null;
        }

        ClearHelpers();
        skillActionSystem?.CancelCurrentAction();
    }

    private IEnumerator PhaseTwoTransitionLaserRoutine(Transform target)
    {
        float totalSeconds = GetPhaseTwoTransitionLaserTotalSeconds();
        skillActionSystem?.BlockNavigationForSkill(totalSeconds, false);

        yield return new WaitForSeconds(Mathf.Max(0f, phase2LaserDialogueHoldSeconds));

        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        List<Rect> dangerLanes = BuildPhaseTwoTransitionDangerLanes(target, roomCenter, roomSize);

        for (int i = 0; i < dangerLanes.Count; i++)
        {
            ShowRectangleWarning(dangerLanes[i], phase2LaserWarningSeconds, warningColor, warningLineWidth);
        }

        yield return new WaitForSeconds(Mathf.Max(0.01f, phase2LaserWarningSeconds));

        for (int i = 0; i < dangerLanes.Count; i++)
        {
            ShowRectangleWarning(dangerLanes[i], phase2LaserActiveSeconds, phase2LaserActiveColor, phase2LaserActiveLineWidth);
        }

        float endTime = Time.time + Mathf.Max(0.01f, phase2LaserActiveSeconds);
        float checkSeconds = Mathf.Max(0.01f, phase2LaserDamageCheckSeconds);
        HashSet<int> hitTargets = new HashSet<int>();

        while (Time.time < endTime)
        {
            DamageTargetIfInsideLanes(target, dangerLanes, phase2LaserDamageMultiplier, hitTargets);
            yield return new WaitForSeconds(checkSeconds);
        }

        if (phase2LaserRecoverySeconds > 0f)
        {
            yield return new WaitForSeconds(phase2LaserRecoverySeconds);
        }

        activePatternRoutine = null;
    }

    private IEnumerator Pattern1Routine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(warningSeconds + 0.5f, false);
        Vector3 startPosition = transform.position;
        Vector3 airPosition = startPosition + Vector3.up * Mathf.Max(0f, aerialHeight);
        SetPosition(airPosition);

        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float leftX = roomCenter.x - roomSize.x * 0.5f + 1f;
        float rightX = roomCenter.x + roomSize.x * 0.5f - 1f;
        GameObject leftClone = CreateClone(new Vector3(leftX, airPosition.y, airPosition.z));
        GameObject rightClone = CreateClone(new Vector3(rightX, airPosition.y, airPosition.z));

        Vector3 bossStrike = target != null ? target.position : startPosition;
        Vector3 leftStrike = new Vector3(leftX, bossStrike.y, bossStrike.z);
        Vector3 rightStrike = new Vector3(rightX, bossStrike.y, bossStrike.z);
        ShowCircleWarning(bossStrike, arrowHitRadius, warningSeconds);
        ShowCircleWarning(leftStrike, arrowHitRadius, warningSeconds);
        ShowCircleWarning(rightStrike, arrowHitRadius, warningSeconds);

        yield return new WaitForSeconds(warningSeconds);

        DamageTargetIfInside(target, bossStrike, arrowHitRadius, arrowDamageMultiplier);
        DamageTargetIfInside(target, leftStrike, arrowHitRadius, arrowDamageMultiplier);
        DamageTargetIfInside(target, rightStrike, arrowHitRadius, arrowDamageMultiplier);

        if (leftClone != null)
        {
            Destroy(leftClone);
        }

        if (rightClone != null)
        {
            Destroy(rightClone);
        }

        SetPosition(startPosition);
        activePatternRoutine = null;
    }

    private IEnumerator Pattern2Routine(Transform target)
    {
        int count = Mathf.Max(1, trackingArrowCount);

        for (int i = 0; i < count; i++)
        {
            Vector3 strikePosition = target != null ? target.position : transform.position;
            ShowCircleWarning(strikePosition, arrowHitRadius, warningSeconds);
            yield return new WaitForSeconds(warningSeconds);
            DamageTargetIfInside(target, strikePosition, arrowHitRadius, arrowDamageMultiplier);

            if (i < count - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, trackingArrowIntervalSeconds));
            }
        }

        activePatternRoutine = null;
    }

    private IEnumerator Pattern3Routine(Transform target)
    {
        Vector3 zoneCenter = target != null ? target.position : transform.position;
        ShowCircleWarning(zoneCenter, rainZoneRadius, warningSeconds);
        yield return new WaitForSeconds(warningSeconds);

        float endTime = Time.time + Mathf.Max(0.01f, rainZoneDurationSeconds);
        float tickSeconds = Mathf.Max(0.05f, rainZoneTickSeconds);

        while (Time.time < endTime)
        {
            ShowCircleWarning(zoneCenter, rainZoneRadius, tickSeconds);
            DamageTargetIfInside(target, zoneCenter, rainZoneRadius, arrowDamageMultiplier);
            yield return new WaitForSeconds(tickSeconds);
        }

        activePatternRoutine = null;
    }

    private IEnumerator Pattern4Routine(Transform target)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float leftX = roomCenter.x - roomSize.x * 0.5f + 1f;
        float rightX = roomCenter.x + roomSize.x * 0.5f - 1f;
        float targetX = target != null ? target.position.x : roomCenter.x;
        float teleportX = Mathf.Abs(targetX - leftX) > Mathf.Abs(targetX - rightX) ? leftX : rightX;
        SetPosition(new Vector3(teleportX, transform.position.y, transform.position.z));
        SpawnPossessableMonsters();

        float floorY = roomCenter.y - roomSize.y * 0.5f;
        float arrowY = floorY + Mathf.Max(0.1f, pattern4ArrowHeightAboveFloor);
        float direction = teleportX < roomCenter.x ? 1f : -1f;
        Vector3 warningStart = new Vector3(direction > 0f ? leftX : rightX, arrowY, transform.position.z);
        float length = Mathf.Max(1f, roomSize.x - 2f);
        HWJ_SkillWarningIndicator.ShowArrowPath(
            warningStart,
            direction,
            length,
            pattern4ArrowThickness,
            pattern4ChargeSeconds,
            warningColor,
            warningLineWidth);

        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4ChargeSeconds));
        DamageTargetIfInsideHorizontalLine(target, arrowY, pattern4ArrowThickness, arrowDamageMultiplier * 1.2f);
        activePatternRoutine = null;
    }

    private IEnumerator Pattern5Routine(Transform target)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        Vector3 airCenter = new Vector3(roomCenter.x, roomCenter.y + roomSize.y * 0.25f, transform.position.z);
        SetPosition(airCenter);

        float endTime = Time.time + Mathf.Max(0.1f, pattern5RainDurationSeconds);
        float interval = Mathf.Max(0.05f, pattern5RainIntervalSeconds);
        float leftX = roomCenter.x - roomSize.x * 0.5f + 1f;
        float rightX = roomCenter.x + roomSize.x * 0.5f - 1f;
        float floorY = roomCenter.y - roomSize.y * 0.5f + 0.8f;

        while (Time.time < endTime)
        {
            float safeX = Random.Range(leftX, rightX);
            for (float x = leftX; x <= rightX; x += Mathf.Max(1f, arrowHitRadius * 2.2f))
            {
                if (Mathf.Abs(x - safeX) <= pattern5SafeGapWidth * 0.5f)
                {
                    continue;
                }

                Vector3 strikePosition = new Vector3(x, floorY, transform.position.z);
                ShowCircleWarning(strikePosition, arrowHitRadius, pattern5WarningSeconds);
                StartCoroutine(DelayedDamageCircle(target, strikePosition, arrowHitRadius, pattern5WarningSeconds));
            }

            yield return new WaitForSeconds(interval);
        }

        bossBrain?.ForceGroggy(pattern5GroggySeconds);
        activePatternRoutine = null;
    }

    private IEnumerator DelayedDamageCircle(Transform target, Vector3 center, float radius, float delaySeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delaySeconds));
        DamageTargetIfInside(target, center, radius, arrowDamageMultiplier);
    }

    private bool CanUsePattern4(Transform target)
    {
        if (target == null || runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return false;
        }

        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;
        return hpRatio <= pattern4BossHpRatioThreshold
            && GetPlayerPossessionMentalRemainingSeconds(target) <= pattern4BodyTimeThresholdSeconds;
    }

    private bool CanUsePattern5()
    {
        if (pattern5Used || runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return false;
        }

        return runtimeStatus.CurrentHp / runtimeStatus.MaxHp <= pattern5HpRatioThreshold;
    }

    private float GetPhaseTwoTransitionLaserTotalSeconds()
    {
        if (!usePhase2TransitionLaser)
        {
            return 0f;
        }

        return Mathf.Max(0f, phase2LaserDialogueHoldSeconds)
            + Mathf.Max(0.01f, phase2LaserWarningSeconds)
            + Mathf.Max(0.01f, phase2LaserActiveSeconds)
            + Mathf.Max(0f, phase2LaserRecoverySeconds);
    }

    private List<Rect> BuildPhaseTwoTransitionDangerLanes(Transform target, Vector2 roomCenter, Vector2 roomSize)
    {
        int laneCount = Mathf.Max(2, phase2LaserLaneCount);
        int safeLaneCount = Mathf.Clamp(phase2LaserSafeLaneCount, 1, laneCount - 1);
        float padding = Mathf.Max(0f, phase2LaserRoomEdgePadding);
        float left = roomCenter.x - roomSize.x * 0.5f + padding;
        float right = roomCenter.x + roomSize.x * 0.5f - padding;
        float bottom = roomCenter.y - roomSize.y * 0.5f + padding;
        float top = roomCenter.y + roomSize.y * 0.5f - padding;
        float totalWidth = Mathf.Max(1f, right - left);
        float totalHeight = Mathf.Max(1f, top - bottom);
        float laneWidth = totalWidth / laneCount;
        List<int> safeLanes = new List<int>();

        if (target != null)
        {
            int targetLane = Mathf.Clamp(Mathf.FloorToInt((target.position.x - left) / laneWidth), 0, laneCount - 1);
            safeLanes.Add(targetLane);
        }

        while (safeLanes.Count < safeLaneCount)
        {
            int lane = Random.Range(0, laneCount);

            if (!safeLanes.Contains(lane))
            {
                safeLanes.Add(lane);
            }
        }

        List<Rect> dangerLanes = new List<Rect>();

        for (int i = 0; i < laneCount; i++)
        {
            if (safeLanes.Contains(i))
            {
                continue;
            }

            dangerLanes.Add(new Rect(left + laneWidth * i, bottom, laneWidth, totalHeight));
        }

        return dangerLanes;
    }

    private float GetPlayerPossessionMentalRemainingSeconds(Transform target)
    {
        HWJ_BodyDecaySystem possessionMental = target.GetComponent<HWJ_BodyDecaySystem>();

        if (possessionMental == null)
        {
            possessionMental = target.GetComponentInParent<HWJ_BodyDecaySystem>();
        }

        if (possessionMental == null)
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
            return possessionMental.RemainingPossessionMentalValue;
        }

        float tickAmount = Mathf.Max(0.01f, playerData.BodyDecay.decayAmountPerTick);
        float tickSeconds = Mathf.Max(0.01f, playerData.BodyDecay.decayTickSeconds);
        return possessionMental.RemainingPossessionMentalValue / tickAmount * tickSeconds;
    }

    private void DamageTargetIfInside(Transform target, Vector3 center, float radius, float damageMultiplier)
    {
        if (target == null || combatSystem == null)
        {
            return;
        }

        if (Vector2.Distance(target.position, center) > radius)
        {
            return;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (targetResolver != null)
        {
            combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _);
        }
    }

    private void DamageTargetIfInsideHorizontalLine(Transform target, float lineY, float thickness, float damageMultiplier)
    {
        if (target == null || combatSystem == null)
        {
            return;
        }

        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        bool insideX = target.position.x >= roomCenter.x - roomSize.x * 0.5f
            && target.position.x <= roomCenter.x + roomSize.x * 0.5f;
        bool insideY = Mathf.Abs(target.position.y - lineY) <= Mathf.Max(0.1f, thickness) * 0.5f;

        if (!insideX || !insideY)
        {
            return;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (targetResolver != null)
        {
            combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _);
        }
    }

    private void DamageTargetIfInsideLanes(
        Transform target,
        List<Rect> dangerLanes,
        float damageMultiplier,
        HashSet<int> hitTargets)
    {
        if (target == null || combatSystem == null || dangerLanes == null || dangerLanes.Count == 0)
        {
            return;
        }

        int targetId = target.GetInstanceID();

        if (hitTargets != null && hitTargets.Contains(targetId))
        {
            return;
        }

        Vector2 targetPosition = target.position;
        bool isInsideDangerLane = false;

        for (int i = 0; i < dangerLanes.Count; i++)
        {
            if (dangerLanes[i].Contains(targetPosition))
            {
                isInsideDangerLane = true;
                break;
            }
        }

        if (!isInsideDangerLane)
        {
            return;
        }

        HWJ_RootObjectDataResolver targetResolver = target.GetComponent<HWJ_RootObjectDataResolver>();

        if (targetResolver == null)
        {
            targetResolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (targetResolver != null && combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _) && hitTargets != null)
        {
            hitTargets.Add(targetId);
        }
    }

    private void SpawnPossessableMonsters()
    {
        if (possessableMonsterPrefabs == null || possessableMonsterPrefabs.Length == 0)
        {
            return;
        }

        int count = Random.Range(
            Mathf.Max(1, minPossessableMonsterSpawnCount),
            Mathf.Max(minPossessableMonsterSpawnCount, maxPossessableMonsterSpawnCount) + 1);
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float floorY = roomCenter.y - roomSize.y * 0.5f + 0.6f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = possessableMonsterPrefabs[Random.Range(0, possessableMonsterPrefabs.Length)];

            if (prefab == null)
            {
                continue;
            }

            float x = Mathf.Lerp(roomCenter.x - roomSize.x * 0.35f, roomCenter.x + roomSize.x * 0.35f, (i + 1f) / (count + 1f));
            GameObject spawned = HWJ_GameAccess.Spawn(prefab, new Vector3(x, floorY, transform.position.z), Quaternion.identity);

            if (spawned == null)
            {
                spawned = Instantiate(prefab, new Vector3(x, floorY, transform.position.z), Quaternion.identity);
            }

            HWJ_RootObjectDataResolver resolver = spawned.GetComponent<HWJ_RootObjectDataResolver>();

            if (resolver != null && possessableMonsterRootObjects != null && possessableMonsterRootObjects.Length > 0)
            {
                HWJ_RootObjectDataSO rootData = possessableMonsterRootObjects[Random.Range(0, possessableMonsterRootObjects.Length)];

                if (rootData != null)
                {
                    resolver.SetRootObjectData(rootData);
                }
            }
        }
    }

    private GameObject CreateClone(Vector3 position)
    {
        GameObject clone = new GameObject("HWJ_BossArrowClone");
        clone.transform.position = position;
        SpriteRenderer sourceRenderer = GetComponentInChildren<SpriteRenderer>();

        if (sourceRenderer != null)
        {
            SpriteRenderer cloneRenderer = clone.AddComponent<SpriteRenderer>();
            cloneRenderer.sprite = sourceRenderer.sprite;
            cloneRenderer.color = new Color(1f, 1f, 1f, 0.55f);
            cloneRenderer.flipX = sourceRenderer.flipX;
            cloneRenderer.flipY = sourceRenderer.flipY;
            cloneRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            cloneRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        }

        spawnedHelpers.Add(clone);
        return clone;
    }

    private void ClearHelpers()
    {
        for (int i = spawnedHelpers.Count - 1; i >= 0; i--)
        {
            if (spawnedHelpers[i] != null)
            {
                Destroy(spawnedHelpers[i]);
            }
        }

        spawnedHelpers.Clear();
    }

    private void ShowCircleWarning(Vector3 center, float radius, float durationSeconds)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            center,
            radius,
            Mathf.Max(0.01f, durationSeconds),
            warningColor,
            warningLineWidth);
    }

    private void ShowRectangleWarning(Rect rect, float durationSeconds, Color color, float lineWidth)
    {
        Vector3 center = new Vector3(rect.center.x, rect.center.y, transform.position.z);
        Vector2 size = new Vector2(rect.width, rect.height);
        HWJ_SkillWarningIndicator.ShowRectangle(
            center,
            size,
            Mathf.Max(0.01f, durationSeconds),
            color,
            lineWidth);
    }

    private Vector2 GetRoomCenter()
    {
        return bossBrain != null ? bossBrain.BossRoomCenter : (Vector2)transform.position;
    }

    private Vector2 GetRoomSize()
    {
        return bossBrain != null ? bossBrain.BossRoomSize : new Vector2(28f, 14f);
    }

    private void SetPosition(Vector3 position)
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = (Vector2)position;
        }

        transform.position = position;
    }

    private void CacheReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }
}
