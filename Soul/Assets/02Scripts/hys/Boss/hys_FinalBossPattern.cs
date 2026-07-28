using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum hys_FinalBossPatternId
{
    None,
    BlackOrbs,
    LingeringSpear,
    BloodShield,
    RedPortalSummon,
    FireAndBlackLightning,
    WeaponBarrage,
    PhaseTwoShieldOrbs,
    CorruptionFlameDash,
    DoublePortalSummon,
    RadialBlackLightning
}

/// <summary>
/// 최종보스 1페이즈의 다섯 패턴과 쿨타임을 한 곳에서 실행합니다.
/// </summary>
public class hys_FinalBossPattern : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private hys_FinalBossShield shieldSystem;
    [SerializeField] private HWJ_BossPatternDataSO[] patternData = new HWJ_BossPatternDataSO[10];
    [SerializeField] private GameObject[] summonMonsterPrefabs = new GameObject[5];

    [Header("공통")]
    [SerializeField] private bool preventSamePatternConsecutively = true;
    // 보호막 패턴 3은 HP 조건이 있으므로 제외하고, 1, 2, 4, 5를 순서대로 실행합니다.
    [SerializeField] private bool useOrderedOpeningSequence = true;
    // 2페이즈 보호막을 제외하고 1, 3, 4, 5번 패턴을 순서대로 실행합니다.
    [SerializeField] private bool useOrderedPhaseTwoOpeningSequence = true;
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHpRatio = 0.5f;
    // 테스트 중에는 1페이즈를 건너뛰고 2페이즈 패턴만 확인합니다.
    [SerializeField] private bool forcePhaseTwoForTesting;
    [SerializeField, Min(0.1f)] private float phaseTransitionSeconds = 1.5f;
    [SerializeField] private Color blackMagicColor = new Color(0.025f, 0.01f, 0.04f, 0.95f);
    [SerializeField] private Color purpleMagicColor = new Color(0.48f, 0.08f, 0.75f, 0.9f);
    [SerializeField] private Color redPortalColor = new Color(0.75f, 0.02f, 0.04f, 0.9f);

    [Header("패턴 1 - 검은 구체")]
    [SerializeField, Min(0.1f)] private float orbPrepareSeconds = 0.6f;
    [SerializeField, Min(0.1f)] private float orbRadius = 0.55f;
    [SerializeField, Min(0.1f)] private float orbOrbitRadius = 2.2f;
    [SerializeField, Min(0.1f)] private float orbSpeed = 15f;
    [SerializeField, Min(0f)] private float orbLaunchIntervalSeconds = 0.12f;
    [SerializeField, Min(0f)] private float orbDamageMultiplier = 1f;

    [Header("패턴 2 - 잔류 창")]
    [SerializeField, Min(0.1f)] private float spearPrepareSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float spearSpeed = 23f;
    [SerializeField, Min(1f)] private float spearTravelDistance = 30f;
    [SerializeField, Min(0.1f)] private float spearHitRadius = 0.75f;
    [SerializeField, Min(0f)] private float spearDamageMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float spearTrailSeconds = 3f;
    [SerializeField, Min(0.1f)] private float spearTrailWidth = 0.75f;
    [SerializeField, Min(0f)] private float spearTrailDamageMultiplier = 0.55f;

    [Header("패턴 3 - 보호막")]
    [SerializeField, Range(0f, 1f)] private float shieldActivationHpRatio = 0.8f;
    [SerializeField, Min(0f)] private float shieldCastSeconds = 0.6f;

    [Header("패턴 4 - 붉은 포탈")]
    [SerializeField, Range(1, 5)] private int summonCount = 3;
    [SerializeField, Min(0.1f)] private float portalOpenSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float summonSpacing = 2.2f;

    [Header("패턴 5 - 불길과 검은 번개")]
    [SerializeField, Min(0.1f)] private float ultimateChargeSeconds = 3f;
    [SerializeField] private float roomLeftX = -22f;
    [SerializeField] private float roomRightX = 22f;
    [SerializeField] private float groundY = 0f;
    [SerializeField, Min(0.1f)] private float fireHeight = 1.1f;
    [SerializeField, Min(0.1f)] private float fireDurationSeconds = 5f;
    [SerializeField, Min(0f)] private float fireDamageMultiplier = 0.45f;
    [SerializeField, Range(3, 20)] private int lightningCount = 11;
    [SerializeField, Min(0.01f)] private float lightningIntervalSeconds = 0.16f;
    [SerializeField, Min(0.01f)] private float lightningWarningSeconds = 0.18f;
    [SerializeField, Min(0.1f)] private float lightningWidth = 1.35f;
    [SerializeField, Min(0f)] private float lightningDamageMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float lightningStunSeconds = 1f;
    [SerializeField, Min(0f)] private float ultimateGroggySeconds = 4f;

    [Header("2페이즈 패턴 6 - 무기 연속 투척")]
    [SerializeField, Min(0.1f)] private float weaponBarragePrepareSeconds = 0.65f;
    [SerializeField, Min(0.1f)] private float weaponFormationAimSeconds = 0.45f;
    [SerializeField, Min(0f)] private float weaponLaunchIntervalSeconds = 0.28f;
    [SerializeField, Min(0f)] private float weaponGroupIntervalSeconds = 0.4f;
    [SerializeField, Min(0.1f)] private float weaponProjectileSpeed = 21f;
    [SerializeField, Min(0.1f)] private float weaponHitRadius = 0.8f;
    [SerializeField, Min(0f)] private float weaponDamageMultiplier = 1.15f;
    [SerializeField] private LayerMask axeShockwaveSurfaceLayers = ~0;
    [SerializeField, Min(0.1f)] private float lastAxeShockwaveRadius = 3.5f;
    [SerializeField, Min(0.1f)] private float lastAxeShockwaveExpandSeconds = 0.55f;
    [SerializeField, Range(0.2f, 0.95f)] private float lastAxeShockwaveEchoRadiusRatio = 0.72f;
    [SerializeField, Min(0f)] private float lastAxeShockwaveDamageMultiplier = 1.45f;

    [Header("2페이즈 패턴 7 - 강화 보호막과 구체")]
    [SerializeField, Range(0.01f, 0.5f)] private float phaseTwoShieldHpRatio = 0.1f;
    [SerializeField, Range(1, 8)] private int phaseTwoShieldOrbCount = 5;
    [SerializeField, Min(0.1f)] private float phaseTwoShieldOrbDuration = 4f;
    [SerializeField, Min(0.1f)] private float phaseTwoShieldOrbSpeed = 16f;
    [SerializeField, Min(0f)] private float phaseTwoShieldOrbDamageMultiplier = 0.85f;

    [Header("2페이즈 패턴 8 - 검은 불길 부패 돌진")]
    [SerializeField, Min(0.1f)] private float flameDashRiseHeight = 4f;
    [SerializeField, Min(0.1f)] private float flameDashRiseSeconds = 0.55f;
    [SerializeField, Min(0.1f)] private float flameDashSpeed = 32f;
    [SerializeField, Min(0.1f)] private float flameDashHitRadius = 1.5f;
    [SerializeField, Min(0f)] private float flameDashDecayPenalty = 35f;
    [SerializeField, Min(0f)] private float flameDashDamageMultiplier = 0.6f;

    [Header("2페이즈 패턴 9 - 이중 포탈")]
    [SerializeField, Range(1, 8)] private int phaseTwoSummonCount = 4;
    [SerializeField, Min(0.1f)] private float doublePortalDistance = 5f;

    [Header("2페이즈 패턴 10 - 8방향 검은 번개")]
    [SerializeField, Min(0.1f)] private float radialLightningChargeSeconds = 2f;
    [SerializeField, Min(1f)] private float radialLightningDistance = 22f;
    [SerializeField, Min(0.1f)] private float radialLightningWidth = 1.1f;
    [SerializeField, Min(0f)] private float radialLightningDamageMultiplier = 1.35f;
    [SerializeField, Min(0f)] private float radialLightningStunSeconds = 0.7f;

    [Header("그로기 공통 처리")]
    [SerializeField] private bool enableHitCountGroggy = true;
    [SerializeField, Min(1)] private int groggyHitCountThreshold = 5;
    [SerializeField, Min(0.1f)] private float groggyHitWindowSeconds = 3f;
    [SerializeField, Min(0.1f)] private float commonGroggyDurationSeconds = 2.5f;
    [SerializeField, Min(1f)] private float groggyReceivedDamageMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float groggyEndKnockbackRadius = 6f;
    [SerializeField, Min(0f)] private float groggyEndKnockbackPower = 8f;
    [SerializeField, Min(0.05f)] private float groggyEndKnockbackSeconds = 0.25f;

    [Header("디버그")]
    [SerializeField] private hys_FinalBossPatternId activePattern;
    [SerializeField] private bool isGroggy;
    [SerializeField] private bool isPhaseTransitioning;
    [SerializeField] private int currentPhaseNumber = 1;
    [SerializeField] private string lastPatternAction;

    private readonly List<hys_FinalBossPatternId> selectable = new List<hys_FinalBossPatternId>(5);
    private static readonly hys_FinalBossPatternId[] PhaseOneOpeningOrder =
    {
        hys_FinalBossPatternId.BlackOrbs,
        hys_FinalBossPatternId.LingeringSpear,
        hys_FinalBossPatternId.RedPortalSummon,
        hys_FinalBossPatternId.FireAndBlackLightning
    };
    private static readonly hys_FinalBossPatternId[] PhaseTwoOpeningOrder =
    {
        hys_FinalBossPatternId.WeaponBarrage,
        hys_FinalBossPatternId.CorruptionFlameDash,
        hys_FinalBossPatternId.DoublePortalSummon,
        hys_FinalBossPatternId.RadialBlackLightning
    };
    private Coroutine activeRoutine;
    private Transform currentTarget;
    private Action<hys_FinalBossPatternId> finishedCallback;
    private hys_FinalBossPatternId lastCompleted;
    private int openingPatternStep;
    private int phaseTwoOpeningPatternStep;
    private bool handlingGroggyBonusDamage;
    private float previousObservedBossHp;
    private int groggyHitCount;
    private float groggyHitWindowEndTime;
    private readonly float[] readyTimes = new float[11];

    public bool IsPatternRunning => activeRoutine != null;
    public bool IsGroggy => isGroggy;
    public bool IsPhaseTransitioning => isPhaseTransitioning;
    public hys_FinalBossPatternId ActivePattern => activePattern;
    public string LastPatternAction => lastPatternAction;
    public int CurrentPhaseNumber => currentPhaseNumber;

    private void Awake()
    {
        CacheReferences();
        currentPhaseNumber = ResolveCurrentPhaseNumber();
    }

    private void OnEnable()
    {
        CacheReferences();
        previousObservedBossHp = runtimeStatus != null ? runtimeStatus.CurrentHp : 0f;
    }

    private void Update()
    {
        ObserveBossDamage();
        int resolvedPhase = ResolveCurrentPhaseNumber();
        if (resolvedPhase == currentPhaseNumber) return;

        currentPhaseNumber = resolvedPhase;
        if (currentPhaseNumber == 2)
        {
            // 2페이즈 진입 즉시 진행 중이던 1페이즈 패턴을 끊습니다.
            StopAllCoroutines();
            activeRoutine = null;
            activePattern = hys_FinalBossPatternId.None;
            isGroggy = false;
            finishedCallback = null;
            StopHorizontalMovement();
            groggyHitCount = 0;
            groggyHitWindowEndTime = 0f;
            phaseTwoOpeningPatternStep = 0;
            isPhaseTransitioning = true;
            activeRoutine = StartCoroutine(PhaseTransitionRoutine());
        }
    }

    public bool TryStartNextPattern(Transform target, Action<hys_FinalBossPatternId> onFinished,
        out hys_FinalBossPatternId selected)
    {
        selected = hys_FinalBossPatternId.None;
        if (activeRoutine != null || target == null) return false;

        selectable.Clear();
        currentPhaseNumber = ResolveCurrentPhaseNumber();

        // 체력 조건 보호막은 일반 순서와 랜덤 추첨보다 우선하며 로테이션 단계는 소비하지 않습니다.
        hys_FinalBossPatternId conditionalPattern = currentPhaseNumber == 1
            ? hys_FinalBossPatternId.BloodShield
            : hys_FinalBossPatternId.PhaseTwoShieldOrbs;
        if (CanUsePattern(conditionalPattern, target))
        {
            if (!StartPattern(conditionalPattern, target, onFinished)) return false;
            selected = conditionalPattern;
            return true;
        }

        if (currentPhaseNumber == 1 && useOrderedOpeningSequence
            && openingPatternStep < PhaseOneOpeningOrder.Length)
        {
            hys_FinalBossPatternId openingPattern = PhaseOneOpeningOrder[openingPatternStep];
            if (!CanUsePattern(openingPattern, target)) return false;

            Action<hys_FinalBossPatternId> orderedFinished = completed =>
            {
                if (completed == openingPattern) openingPatternStep++;
                onFinished?.Invoke(completed);
            };
            if (!StartPattern(openingPattern, target, orderedFinished)) return false;
            selected = openingPattern;
            return true;
        }

        if (currentPhaseNumber == 2 && useOrderedPhaseTwoOpeningSequence
            && phaseTwoOpeningPatternStep < PhaseTwoOpeningOrder.Length)
        {
            hys_FinalBossPatternId openingPattern = PhaseTwoOpeningOrder[phaseTwoOpeningPatternStep];
            if (!CanUsePattern(openingPattern, target)) return false;

            Action<hys_FinalBossPatternId> orderedFinished = completed =>
            {
                if (completed == openingPattern) phaseTwoOpeningPatternStep++;
                onFinished?.Invoke(completed);
            };
            if (!StartPattern(openingPattern, target, orderedFinished)) return false;
            selected = openingPattern;
            return true;
        }

        int firstPattern = currentPhaseNumber == 1 ? 1 : 6;
        int lastPattern = currentPhaseNumber == 1 ? 5 : 10;
        for (int i = firstPattern; i <= lastPattern; i++)
        {
            hys_FinalBossPatternId id = (hys_FinalBossPatternId)i;
            // 체력 조건 패턴은 일반 랜덤 후보에서 제외합니다.
            if (id == hys_FinalBossPatternId.BloodShield
                || id == hys_FinalBossPatternId.PhaseTwoShieldOrbs) continue;
            if (CanUsePattern(id, target)) selectable.Add(id);
        }
        if (selectable.Count == 0) return false;

        selected = SelectWeightedPattern();
        return StartPattern(selected, target, onFinished);
    }

    public void ResetPatternSelection()
    {
        lastCompleted = hys_FinalBossPatternId.None;
        openingPatternStep = 0;
        phaseTwoOpeningPatternStep = 0;
        groggyHitCount = 0;
        groggyHitWindowEndTime = 0f;
        for (int i = 0; i < readyTimes.Length; i++) readyTimes[i] = 0f;
    }

    public void CancelActivePattern()
    {
        StopAllCoroutines();
        activeRoutine = null;
        currentTarget = null;
        finishedCallback = null;
        activePattern = hys_FinalBossPatternId.None;
        isGroggy = false;
        isPhaseTransitioning = false;
        groggyHitCount = 0;
        groggyHitWindowEndTime = 0f;
        StopHorizontalMovement();
    }

    private bool CanUsePattern(hys_FinalBossPatternId id, Transform target)
    {
        int index = (int)id;
        if (index <= 0 || index >= readyTimes.Length || Time.time < readyTimes[index]) return false;
        if (preventSamePatternConsecutively && id == lastCompleted) return false;
        if (id == hys_FinalBossPatternId.BloodShield)
        {
            float ratio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
                ? runtimeStatus.CurrentHp / runtimeStatus.MaxHp : 1f;
            if (ratio > shieldActivationHpRatio || shieldSystem != null && shieldSystem.IsActive) return false;
        }
        if (id == hys_FinalBossPatternId.PhaseTwoShieldOrbs && shieldSystem != null && shieldSystem.IsActive)
            return false;

        HWJ_BossPatternDataSO data = GetPatternData(id);
        if (data == null) return true;
        float hpRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? runtimeStatus.CurrentHp / runtimeStatus.MaxHp : 1f;
        return data.IsHpConditionMatched(hpRatio) && data.IsPhaseAllowed(currentPhaseNumber);
    }

    private bool StartPattern(hys_FinalBossPatternId id, Transform target,
        Action<hys_FinalBossPatternId> onFinished)
    {
        currentTarget = target;
        finishedCallback = onFinished;
        activePattern = id;
        SetCooldown(id);

        switch (id)
        {
            case hys_FinalBossPatternId.BlackOrbs: activeRoutine = StartCoroutine(BlackOrbRoutine()); break;
            case hys_FinalBossPatternId.LingeringSpear: activeRoutine = StartCoroutine(LingeringSpearRoutine()); break;
            case hys_FinalBossPatternId.BloodShield: activeRoutine = StartCoroutine(ShieldRoutine()); break;
            case hys_FinalBossPatternId.RedPortalSummon: activeRoutine = StartCoroutine(PortalRoutine()); break;
            case hys_FinalBossPatternId.FireAndBlackLightning: activeRoutine = StartCoroutine(UltimateRoutine()); break;
            case hys_FinalBossPatternId.WeaponBarrage: activeRoutine = StartCoroutine(WeaponBarrageRoutine()); break;
            case hys_FinalBossPatternId.PhaseTwoShieldOrbs: activeRoutine = StartCoroutine(PhaseTwoShieldRoutine()); break;
            case hys_FinalBossPatternId.CorruptionFlameDash: activeRoutine = StartCoroutine(CorruptionFlameDashRoutine()); break;
            case hys_FinalBossPatternId.DoublePortalSummon: activeRoutine = StartCoroutine(DoublePortalRoutine()); break;
            case hys_FinalBossPatternId.RadialBlackLightning: activeRoutine = StartCoroutine(RadialLightningRoutine()); break;
            default: return false;
        }
        return true;
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        lastPatternAction = "2페이즈 전환 연출";
        if (runtimeStatus != null && !runtimeStatus.IsDead)
            runtimeStatus.SetState(HWJ_RuntimeState.Attack);

        GameObject transitionEffect = hys_FinalBossMagicVisual.SpawnRing(
            transform, 3.2f, purpleMagicColor, phaseTransitionSeconds,
            "hys_FinalBossPhaseTransition");
        yield return new WaitForSeconds(phaseTransitionSeconds);
        if (transitionEffect != null) Destroy(transitionEffect);

        isPhaseTransitioning = false;
        activeRoutine = null;
        if (runtimeStatus != null && !runtimeStatus.IsDead)
            runtimeStatus.SetState(HWJ_RuntimeState.Idle);
    }

    private IEnumerator BlackOrbRoutine()
    {
        lastPatternAction = "검은 구체 3개 생성";
        GameObject[] orbs = new GameObject[3];
        for (int i = 0; i < orbs.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / orbs.Length;
            Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * orbOrbitRadius;
            orbs[i] = hys_FinalBossMagicVisual.SpawnOrb(position, orbRadius, blackMagicColor, 5f);
        }
        yield return new WaitForSeconds(orbPrepareSeconds);

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;
            Vector2 direction = currentTarget != null
                ? (Vector2)(currentTarget.position - orbs[i].transform.position) : Vector2.right;
            StartCoroutine(LaunchProjectile(
                orbs[i], direction.normalized, orbSpeed, orbRadius, orbDamageMultiplier, 3f));
            yield return new WaitForSeconds(orbLaunchIntervalSeconds);
        }
        yield return new WaitForSeconds(3f);
        FinishPattern();
    }

    private IEnumerator LingeringSpearRoutine()
    {
        Vector3 start = transform.position + Vector3.up * 1.2f;
        Vector2 aim = currentTarget != null ? (Vector2)(currentTarget.position - start) : Vector2.right;
        GameObject spear = hys_FinalBossMagicVisual.SpawnSpear(start, aim, 3.8f, blackMagicColor, 6f);
        lastPatternAction = "검은 창 1.5초 조준";
        yield return new WaitForSeconds(spearPrepareSeconds);

        aim = currentTarget != null ? (Vector2)(currentTarget.position - start) : aim;
        aim = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.right;
        Vector3 end = start + (Vector3)aim * spearTravelDistance;
        if (spear != null) spear.transform.rotation = Quaternion.FromToRotation(Vector3.up, aim);
        yield return LaunchProjectile(spear, aim, spearSpeed, spearHitRadius, spearDamageMultiplier,
            spearTravelDistance / Mathf.Max(0.1f, spearSpeed));

        hys_FinalBossMagicVisual.SpawnLine(start, end, spearTrailWidth, blackMagicColor,
            spearTrailSeconds, "hys_FinalBossLingeringSpearTrail");
        StartCoroutine(LingeringLineDamageRoutine(start, end, spearTrailSeconds));
        FinishPattern();
    }

    private IEnumerator ShieldRoutine()
    {
        lastPatternAction = "손에 보라색 마력 집중";
        GameObject charge = hys_FinalBossMagicVisual.SpawnOrb(
            transform.position + Vector3.up * 1.1f, 0.45f, purpleMagicColor, shieldCastSeconds);
        yield return new WaitForSeconds(shieldCastSeconds);
        shieldSystem?.ActivateShield();
        if (charge != null) Destroy(charge);
        FinishPattern();
    }

    private IEnumerator PortalRoutine()
    {
        Vector3 portalPosition = transform.position + Vector3.right * 3f;
        GameObject portal = hys_FinalBossMagicVisual.SpawnRing(null, 1.7f, redPortalColor,
            portalOpenSeconds + 1f, "hys_FinalBossRedPortal");
        portal.transform.position = portalPosition;
        lastPatternAction = "붉은 포탈 개방";
        yield return new WaitForSeconds(portalOpenSeconds);

        for (int i = 0; i < summonCount; i++)
        {
            GameObject prefab = GetRandomSummonPrefab();
            if (prefab == null) continue;
            float centered = i - (summonCount - 1) * 0.5f;
            Vector3 spawnPosition = portalPosition + Vector3.right * centered * summonSpacing;
            GameObject spawned = HWJ_GameAccess.Spawn(prefab, spawnPosition, Quaternion.identity);
            if (spawned == null) spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);
            IgnoreCollisionWithSummon(spawned);
        }
        FinishPattern();
    }

    private IEnumerator UltimateRoutine()
    {
        lastPatternAction = "보라색 마력 3초 충전";
        GameObject charge = hys_FinalBossMagicVisual.SpawnRing(transform, 2.2f, purpleMagicColor,
            ultimateChargeSeconds, "hys_FinalBossUltimateCharge");
        yield return new WaitForSeconds(ultimateChargeSeconds);
        if (charge != null) Destroy(charge);

        float width = Mathf.Abs(roomRightX - roomLeftX);
        Vector3 fireCenter = new Vector3((roomLeftX + roomRightX) * 0.5f, groundY + fireHeight * 0.5f, 0f);
        hys_FinalBossMagicVisual.SpawnRectangle(fireCenter, new Vector2(width, fireHeight),
            new Color(0.55f, 0.03f, 0.01f, 0.65f), fireDurationSeconds, "hys_FinalBossGroundFire");
        StartCoroutine(GroundFireDamageRoutine(fireDurationSeconds));

        for (int i = 0; i < lightningCount; i++)
        {
            float t = lightningCount <= 1 ? 0.5f : i / (float)(lightningCount - 1);
            float x = Mathf.Lerp(roomLeftX, roomRightX, t);
            hys_FinalBossMagicVisual.SpawnRectangle(new Vector3(x, groundY + 5f, 0f),
                new Vector2(lightningWidth, 10f), new Color(0.25f, 0f, 0.35f, 0.25f),
                lightningWarningSeconds, "hys_FinalBossLightningWarning");
            yield return new WaitForSeconds(lightningWarningSeconds);
            hys_FinalBossMagicVisual.SpawnLine(new Vector3(x, groundY, 0f), new Vector3(x, groundY + 12f, 0f),
                lightningWidth, blackMagicColor, 0.3f, "hys_FinalBossBlackLightning");
            if (currentTarget != null && Mathf.Abs(currentTarget.position.x - x) <= lightningWidth * 0.5f)
            {
                if (TryDamageCurrentTarget(lightningDamageMultiplier, out HWJ_RuntimeStatusSystem targetStatus))
                    targetStatus?.LockControl(lightningStunSeconds);
            }
            yield return new WaitForSeconds(lightningIntervalSeconds);
        }

        isGroggy = true;
        lastPatternAction = "궁극기 종료 - 4초 그로기";
        if (runtimeStatus != null && !runtimeStatus.IsDead) runtimeStatus.SetState(HWJ_RuntimeState.Hit);
        yield return new WaitForSeconds(ultimateGroggySeconds);
        isGroggy = false;
        ApplyGroggyEndKnockback();
        FinishPattern();
    }

    private IEnumerator WeaponBarrageRoutine()
    {
        lastPatternAction = "검, 창, 도끼 각 3개 생성";
        GameObject charge = hys_FinalBossMagicVisual.SpawnRing(
            transform, 2.4f, purpleMagicColor, weaponBarragePrepareSeconds, "hys_FinalBossWeaponBarrageCharge");
        yield return new WaitForSeconds(weaponBarragePrepareSeconds);
        if (charge != null) Destroy(charge);

        hys_FinalBossWeaponVisualType[] weaponOrder =
        {
            hys_FinalBossWeaponVisualType.Sword,
            hys_FinalBossWeaponVisualType.Spear,
            hys_FinalBossWeaponVisualType.Axe
        };

        for (int group = 0; group < weaponOrder.Length; group++)
        {
            // 같은 종류 3개를 먼저 보여준 뒤 조준하고 한 발씩 발사합니다.
            GameObject[] preparedWeapons = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float side = i - 1f;
                Vector3 spawn = transform.position
                    + new Vector3(side * 0.95f, 2.15f + Mathf.Abs(side) * 0.35f, 0f);
                Vector2 direction = currentTarget != null
                    ? (Vector2)(currentTarget.position - spawn).normalized : Vector2.right;
                preparedWeapons[i] = hys_FinalBossMagicVisual.SpawnWeapon(
                    spawn, direction, weaponOrder[group], blackMagicColor, 4f);
            }

            lastPatternAction = $"{weaponOrder[group]} 3개 생성 및 조준";
            yield return new WaitForSeconds(weaponFormationAimSeconds);

            for (int i = 0; i < preparedWeapons.Length; i++)
            {
                GameObject weapon = preparedWeapons[i];
                if (weapon == null) continue;
                Vector2 direction = currentTarget != null
                    ? (Vector2)(currentTarget.position - weapon.transform.position).normalized
                    : Vector2.right;
                weapon.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                bool isLastAxe = group == 2 && i == 2;
                StartCoroutine(LaunchProjectile(
                    weapon, direction, weaponProjectileSpeed, weaponHitRadius,
                    weaponDamageMultiplier, 3f, isLastAxe));
                yield return new WaitForSeconds(weaponLaunchIntervalSeconds);
            }

            if (group < weaponOrder.Length - 1)
                yield return new WaitForSeconds(weaponGroupIntervalSeconds);
        }

        yield return new WaitForSeconds(3f);
        FinishPattern();
    }

    private IEnumerator PhaseTwoShieldRoutine()
    {
        lastPatternAction = "강화 보호막과 추적 구체 5개 생성";
        GameObject handEffect = hys_FinalBossMagicVisual.SpawnOrb(
            transform.position + Vector3.up * 1.2f, 0.5f, purpleMagicColor, shieldCastSeconds);
        yield return new WaitForSeconds(shieldCastSeconds);
        shieldSystem?.ActivateShield(phaseTwoShieldHpRatio);
        if (handEffect != null) Destroy(handEffect);

        GameObject[] orbs = new GameObject[phaseTwoShieldOrbCount];
        for (int i = 0; i < orbs.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / orbs.Length;
            Vector3 position = transform.position
                + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (orbOrbitRadius + 0.5f);
            orbs[i] = hys_FinalBossMagicVisual.SpawnOrb(position, orbRadius * 0.75f,
                purpleMagicColor, phaseTwoShieldOrbDuration + 4f);
        }

        float interval = phaseTwoShieldOrbDuration / Mathf.Max(1, orbs.Length);
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] != null)
            {
                Vector2 direction = currentTarget != null
                    ? (Vector2)(currentTarget.position - orbs[i].transform.position).normalized : Vector2.right;
                StartCoroutine(LaunchProjectile(orbs[i], direction, phaseTwoShieldOrbSpeed,
                    orbRadius, phaseTwoShieldOrbDamageMultiplier, 3f));
            }
            yield return new WaitForSeconds(interval);
        }
        FinishPattern();
    }

    private IEnumerator CorruptionFlameDashRoutine()
    {
        lastPatternAction = "공중 상승 후 검은 불길 돌진";
        Vector3 groundPosition = transform.position;
        Vector3 airPosition = groundPosition + Vector3.up * flameDashRiseHeight;
        float riseEnd = Time.time + flameDashRiseSeconds;
        while (Time.time < riseEnd)
        {
            float t = 1f - Mathf.Clamp01((riseEnd - Time.time) / flameDashRiseSeconds);
            transform.position = Vector3.Lerp(groundPosition, airPosition, t);
            yield return null;
        }
        transform.position = airPosition;

        GameObject flame = hys_FinalBossMagicVisual.SpawnRing(
            transform, 1.8f, blackMagicColor, 2f, "hys_FinalBossCorruptionFlame");
        Vector3 targetPosition = currentTarget != null ? currentTarget.position : transform.position + Vector3.right * 8f;
        targetPosition.x = Mathf.Clamp(targetPosition.x, roomLeftX, roomRightX);
        float dashDirection = Mathf.Sign(targetPosition.x - transform.position.x);
        if (Mathf.Approximately(dashDirection, 0f)) dashDirection = 1f;
        Vector3 roomEndPosition = new Vector3(
            dashDirection > 0f ? roomRightX : roomLeftX,
            targetPosition.y,
            transform.position.z);
        Vector3[] dashWaypoints = { targetPosition, roomEndPosition };
        bool hit = false;
        for (int waypointIndex = 0; waypointIndex < dashWaypoints.Length; waypointIndex++)
        {
            Vector3 waypoint = dashWaypoints[waypointIndex];
            while ((transform.position - waypoint).sqrMagnitude > 0.0025f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    waypoint,
                    flameDashSpeed * Time.deltaTime);
                if (!hit && currentTarget != null
                    && Vector2.Distance(transform.position, currentTarget.position) <= flameDashHitRadius
                    && TryDamageCurrentTarget(flameDashDamageMultiplier, out _))
                {
                    hit = true;
                    HWJ_BodyDecaySystem decay = currentTarget.GetComponentInParent<HWJ_BodyDecaySystem>();
                    decay?.ApplyHitDecayPenalty(flameDashDecayPenalty);
                }
                yield return null;
            }
            transform.position = waypoint;
        }
        if (flame != null) Destroy(flame);

        Vector3 landingPosition = new Vector3(transform.position.x, groundPosition.y, transform.position.z);
        float landEnd = Time.time + flameDashRiseSeconds;
        Vector3 landStart = transform.position;
        while (Time.time < landEnd)
        {
            float t = 1f - Mathf.Clamp01((landEnd - Time.time) / flameDashRiseSeconds);
            transform.position = Vector3.Lerp(landStart, landingPosition, t);
            yield return null;
        }
        transform.position = landingPosition;
        FinishPattern();
    }

    private IEnumerator DoublePortalRoutine()
    {
        lastPatternAction = "붉은 포탈 2개 개방";
        Vector3 leftPortal = transform.position + Vector3.left * doublePortalDistance;
        Vector3 rightPortal = transform.position + Vector3.right * doublePortalDistance;
        GameObject left = hys_FinalBossMagicVisual.SpawnRing(null, 1.7f, redPortalColor,
            portalOpenSeconds + 1.5f, "hys_FinalBossRedPortalLeft");
        GameObject right = hys_FinalBossMagicVisual.SpawnRing(null, 1.7f, redPortalColor,
            portalOpenSeconds + 1.5f, "hys_FinalBossRedPortalRight");
        left.transform.position = leftPortal;
        right.transform.position = rightPortal;
        yield return new WaitForSeconds(portalOpenSeconds);

        for (int i = 0; i < phaseTwoSummonCount; i++)
        {
            GameObject prefab = GetRandomSummonPrefab();
            if (prefab == null) continue;
            Vector3 portal = i % 2 == 0 ? leftPortal : rightPortal;
            Vector3 spawnPosition = portal + Vector3.right * ((i / 2) - 0.5f) * summonSpacing;
            GameObject spawned = HWJ_GameAccess.Spawn(prefab, spawnPosition, Quaternion.identity);
            if (spawned == null) spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);
            IgnoreCollisionWithSummon(spawned);
        }
        FinishPattern();
    }

    private IEnumerator RadialLightningRoutine()
    {
        lastPatternAction = "8방향 검은 번개 충전";
        GameObject charge = hys_FinalBossMagicVisual.SpawnRing(
            transform, 2.6f, purpleMagicColor, radialLightningChargeSeconds,
            "hys_FinalBossRadialLightningCharge");
        yield return new WaitForSeconds(radialLightningChargeSeconds);
        if (charge != null) Destroy(charge);

        Vector2 center = transform.position;
        for (int i = 0; i < 8; i++)
        {
            float angle = Mathf.PI * 2f * i / 8f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 end = center + direction * radialLightningDistance;
            hys_FinalBossMagicVisual.SpawnLine(center, end, radialLightningWidth,
                blackMagicColor, 0.55f, "hys_FinalBossRadialBlackLightning");

            if (currentTarget != null
                && DistancePointToSegment(currentTarget.position, center, end) <= radialLightningWidth * 0.5f)
            {
                if (TryDamageCurrentTarget(radialLightningDamageMultiplier, out HWJ_RuntimeStatusSystem targetStatus))
                    targetStatus?.LockControl(radialLightningStunSeconds);
            }
        }
        yield return new WaitForSeconds(0.55f);
        FinishPattern();
    }

    private IEnumerator LaunchProjectile(GameObject projectile, Vector2 direction, float speed,
        float hitRadius, float damageMultiplier, float lifetime, bool createAxeShockwave = false)
    {
        if (projectile == null) yield break;
        float endTime = Time.time + Mathf.Max(0.05f, lifetime);
        bool damaged = false;
        bool shockwaveCreated = false;
        Vector3 lastProjectilePosition = projectile.transform.position;
        while (projectile != null && Time.time < endTime)
        {
            Vector3 previousPosition = projectile.transform.position;
            Vector3 nextPosition = previousPosition + (Vector3)(direction * speed * Time.deltaTime);
            if (createAxeShockwave
                && TryFindAxeSurfaceImpact(projectile, previousPosition, nextPosition, out Vector3 impactPosition))
            {
                lastProjectilePosition = impactPosition;
                projectile.transform.position = impactPosition;
                CreateLastAxeShockwave(impactPosition);
                shockwaveCreated = true;
                Destroy(projectile);
                yield break;
            }

            projectile.transform.position = nextPosition;
            lastProjectilePosition = projectile.transform.position;
            if (!damaged && currentTarget != null
                && Vector2.Distance(projectile.transform.position, currentTarget.position) <= hitRadius)
            {
                damaged = TryDamageCurrentTarget(damageMultiplier, out _);
                if (createAxeShockwave)
                {
                    CreateLastAxeShockwave(projectile.transform.position);
                    shockwaveCreated = true;
                }
            }
            yield return null;
        }
        // 시각 오브젝트가 수명으로 먼저 삭제돼도 마지막 도끼 충격파는 반드시 생성합니다.
        if (createAxeShockwave && !shockwaveCreated)
            CreateLastAxeShockwave(lastProjectilePosition);
        if (projectile != null) Destroy(projectile);
    }

    private bool TryFindAxeSurfaceImpact(
        GameObject projectile,
        Vector2 start,
        Vector2 end,
        out Vector3 impactPosition)
    {
        impactPosition = end;
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end, axeShockwaveSurfaceLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.isTrigger) continue;
            Transform hitTransform = hitCollider.transform;
            if (hitTransform == projectile.transform || hitTransform.IsChildOf(projectile.transform)) continue;
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;

            // 전투 캐릭터는 지형으로 취급하지 않고 기존 거리 판정으로 처리합니다.
            if (hitCollider.GetComponentInParent<HWJ_RootObjectDataResolver>() != null) continue;
            impactPosition = hits[i].point;
            return true;
        }

        return false;
    }

    private void CreateLastAxeShockwave(Vector3 position)
    {
        // 작은 중심에서 외곽으로 커지는 본 파동과 느린 후속 파동을 함께 표시합니다.
        hys_FinalBossMagicVisual.SpawnExpandingRing(
            position,
            lastAxeShockwaveRadius,
            new Color(0.2f, 0f, 0.28f, 0.72f),
            lastAxeShockwaveExpandSeconds,
            "hys_FinalBossLastAxeShockwave");
        hys_FinalBossMagicVisual.SpawnExpandingRing(
            position,
            lastAxeShockwaveRadius * lastAxeShockwaveEchoRadiusRatio,
            new Color(0.48f, 0.08f, 0.62f, 0.42f),
            lastAxeShockwaveExpandSeconds * 1.35f,
            "hys_FinalBossLastAxeShockwaveEcho",
            0.04f);
        if (currentTarget != null
            && Vector2.Distance(currentTarget.position, position) <= lastAxeShockwaveRadius)
            TryDamageCurrentTarget(lastAxeShockwaveDamageMultiplier, out _);
    }

    private IEnumerator LingeringLineDamageRoutine(Vector2 start, Vector2 end, float seconds)
    {
        float endTime = Time.time + seconds;
        while (Time.time < endTime)
        {
            if (currentTarget != null
                && DistancePointToSegment(currentTarget.position, start, end) <= spearTrailWidth * 0.5f)
                TryDamageCurrentTarget(spearTrailDamageMultiplier, out _);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator GroundFireDamageRoutine(float seconds)
    {
        float endTime = Time.time + seconds;
        while (Time.time < endTime)
        {
            if (currentTarget != null && currentTarget.position.x >= roomLeftX && currentTarget.position.x <= roomRightX
                && currentTarget.position.y <= groundY + fireHeight + 0.5f)
                TryDamageCurrentTarget(fireDamageMultiplier, out _);
            yield return new WaitForSeconds(0.35f);
        }
    }

    private bool TryDamageCurrentTarget(float multiplier, out HWJ_RuntimeStatusSystem targetStatus)
    {
        targetStatus = null;
        // 모든 최종보스 공격의 마지막 관문에서 유령/사망 상태를 차단합니다.
        if (!IsCurrentTargetCombatBody() || combatSystem == null) return false;
        HWJ_RootObjectDataResolver resolver = currentTarget.GetComponentInParent<HWJ_RootObjectDataResolver>();
        if (resolver == null) return false;
        targetStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return combatSystem.TryDealDamageTo(resolver, multiplier, out _);
    }

    private bool IsCurrentTargetCombatBody()
    {
        if (currentTarget == null) return false;
        HWJ_SoulSystem soul = currentTarget.GetComponentInParent<HWJ_SoulSystem>();
        if (soul != null && soul.CurrentState != HWJ_SoulRuntimeState.Body) return false;
        HWJ_RuntimeStatusSystem targetStatus = currentTarget.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        return targetStatus == null || !targetStatus.IsDead;
    }

#if HYS_USE_HWJ_GAMEPLAY_EVENTS
    private void HandleBossDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (handlingGroggyBonusDamage || runtimeStatus == null || runtimeStatus.IsDead
            || damageEvent.TargetStatus != runtimeStatus || damageEvent.Damage <= 0f)
        {
            return;
        }

        bool shieldActive = shieldSystem != null && shieldSystem.IsActive;
        if (!isGroggy && !isPhaseTransitioning && !shieldActive && enableHitCountGroggy)
        {
            if (Time.time > groggyHitWindowEndTime) groggyHitCount = 0;
            groggyHitCount++;
            groggyHitWindowEndTime = Time.time + groggyHitWindowSeconds;
            if (groggyHitCount >= groggyHitCountThreshold) BeginCommonGroggy();
        }

        if (!isGroggy || shieldActive || groggyReceivedDamageMultiplier <= 1f) return;

        // 그로기 중 방어력 감소는 방어 계산이 끝난 최종 피해에 추가 배율로 반영합니다.
        float bonusDamage = damageEvent.Damage * (groggyReceivedDamageMultiplier - 1f);
        float remainingHp = Mathf.Max(0f, runtimeStatus.CurrentHp - bonusDamage);
        handlingGroggyBonusDamage = true;
        runtimeStatus.RestoreHpSnapshot(remainingHp, remainingHp, remainingHp);
        bool died = remainingHp <= 0f;
        if (died) runtimeStatus.SetState(HWJ_RuntimeState.Dead);
        HWJ_GameplayEvents.RaiseDamageApplied(new HWJ_DamageEvent(
            runtimeStatus,
            damageEvent.Source,
            damageEvent.SourceDamage,
            bonusDamage,
            remainingHp,
            died));
        handlingGroggyBonusDamage = false;
    }

#endif

    private void ObserveBossDamage()
    {
        if (runtimeStatus == null) return;

        float currentHp = runtimeStatus.CurrentHp;
        float receivedDamage = previousObservedBossHp - currentHp;
        previousObservedBossHp = currentHp;
        if (receivedDamage <= 0f || handlingGroggyBonusDamage || runtimeStatus.IsDead) return;

        bool shieldActive = shieldSystem != null && shieldSystem.IsActive;
        if (!isGroggy && !isPhaseTransitioning && !shieldActive && enableHitCountGroggy)
        {
            if (Time.time > groggyHitWindowEndTime) groggyHitCount = 0;
            groggyHitCount++;
            groggyHitWindowEndTime = Time.time + groggyHitWindowSeconds;
            if (groggyHitCount >= groggyHitCountThreshold) BeginCommonGroggy();
        }

        if (!isGroggy || shieldActive || groggyReceivedDamageMultiplier <= 1f) return;

        // HWJ 이벤트 타입 없이도 그로기 추가 피해를 실제 HP 감소량 기준으로 계산합니다.
        float bonusDamage = receivedDamage * (groggyReceivedDamageMultiplier - 1f);
        float remainingHp = Mathf.Max(0f, runtimeStatus.CurrentHp - bonusDamage);
        handlingGroggyBonusDamage = true;
        runtimeStatus.RestoreHpSnapshot(remainingHp, remainingHp, remainingHp);
        if (remainingHp <= 0f) runtimeStatus.SetState(HWJ_RuntimeState.Dead);
        previousObservedBossHp = remainingHp;
        handlingGroggyBonusDamage = false;
    }

    private void BeginCommonGroggy()
    {
        groggyHitCount = 0;
        groggyHitWindowEndTime = 0f;
        StopAllCoroutines();
        activeRoutine = null;
        activePattern = hys_FinalBossPatternId.None;
        finishedCallback = null;
        isPhaseTransitioning = false;
        isGroggy = true;
        StopHorizontalMovement();
        activeRoutine = StartCoroutine(CommonGroggyRoutine());
    }

    private IEnumerator CommonGroggyRoutine()
    {
        lastPatternAction = "연속 피격 - 공통 그로기";
        if (runtimeStatus != null && !runtimeStatus.IsDead)
            runtimeStatus.SetState(HWJ_RuntimeState.Hit);
        yield return new WaitForSeconds(commonGroggyDurationSeconds);
        isGroggy = false;
        ApplyGroggyEndKnockback();
        activeRoutine = null;
        if (runtimeStatus != null && !runtimeStatus.IsDead)
            runtimeStatus.SetState(HWJ_RuntimeState.Idle);
    }

    private void ApplyGroggyEndKnockback()
    {
        if (!IsCurrentTargetCombatBody() || groggyEndKnockbackPower <= 0f) return;
        Vector2 offset = currentTarget.position - transform.position;
        if (offset.sqrMagnitude > groggyEndKnockbackRadius * groggyEndKnockbackRadius) return;

        HWJ_RootObjectDataResolver resolver = currentTarget.GetComponentInParent<HWJ_RootObjectDataResolver>();
        if (resolver == null) return;
        HWJ_KnockbackSystem knockbackSystem = resolver.GetComponent<HWJ_KnockbackSystem>();
        if (knockbackSystem == null) knockbackSystem = resolver.gameObject.AddComponent<HWJ_KnockbackSystem>();

        Vector2 direction = new Vector2(offset.x, 0f);
        if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.right;
        knockbackSystem.PlayKnockback(direction, groggyEndKnockbackPower, groggyEndKnockbackSeconds);
        lastPatternAction = "그로기 종료 - 주변 플레이어 넉백";
    }

    private hys_FinalBossPatternId SelectWeightedPattern()
    {
        int total = 0;
        for (int i = 0; i < selectable.Count; i++)
            total += Mathf.Max(1, GetPatternData(selectable[i]) != null ? GetPatternData(selectable[i]).Weight : 1);
        int roll = UnityEngine.Random.Range(0, Mathf.Max(1, total));
        for (int i = 0; i < selectable.Count; i++)
        {
            roll -= Mathf.Max(1, GetPatternData(selectable[i]) != null ? GetPatternData(selectable[i]).Weight : 1);
            if (roll < 0) return selectable[i];
        }
        return selectable[0];
    }

    private void SetCooldown(hys_FinalBossPatternId id)
    {
        HWJ_BossPatternDataSO data = GetPatternData(id);
        float fallback;
        switch (id)
        {
            case hys_FinalBossPatternId.BlackOrbs: fallback = 10f; break;
            case hys_FinalBossPatternId.LingeringSpear: fallback = 15f; break;
            case hys_FinalBossPatternId.BloodShield: fallback = 30f; break;
            case hys_FinalBossPatternId.RedPortalSummon: fallback = 60f; break;
            case hys_FinalBossPatternId.FireAndBlackLightning: fallback = 90f; break;
            case hys_FinalBossPatternId.WeaponBarrage: fallback = 18f; break;
            case hys_FinalBossPatternId.PhaseTwoShieldOrbs: fallback = 30f; break;
            case hys_FinalBossPatternId.CorruptionFlameDash: fallback = 20f; break;
            case hys_FinalBossPatternId.DoublePortalSummon: fallback = 60f; break;
            case hys_FinalBossPatternId.RadialBlackLightning: fallback = 100f; break;
            default: fallback = 1f; break;
        }
        readyTimes[(int)id] = Time.time + (data != null ? data.EffectiveCooldownSeconds : fallback);
    }

    private int ResolveCurrentPhaseNumber()
    {
        if (forcePhaseTwoForTesting) return 2;
        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f) return 1;
        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;
        return hpRatio <= phaseTwoHpRatio ? 2 : 1;
    }

    private HWJ_BossPatternDataSO GetPatternData(hys_FinalBossPatternId id)
    {
        int number = (int)id;
        if (patternData == null) return null;
        for (int i = 0; i < patternData.Length; i++)
            if (patternData[i] != null && patternData[i].PatternNumber == number) return patternData[i];
        return null;
    }

    private GameObject GetRandomSummonPrefab()
    {
        if (summonMonsterPrefabs == null || summonMonsterPrefabs.Length == 0) return null;
        int start = UnityEngine.Random.Range(0, summonMonsterPrefabs.Length);
        for (int i = 0; i < summonMonsterPrefabs.Length; i++)
        {
            GameObject candidate = summonMonsterPrefabs[(start + i) % summonMonsterPrefabs.Length];
            if (candidate != null) return candidate;
        }
        return null;
    }

    private void IgnoreCollisionWithSummon(GameObject summon)
    {
        if (summon == null) return;
        Collider2D[] bossColliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] summonColliders = summon.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < bossColliders.Length; i++)
            for (int j = 0; j < summonColliders.Length; j++)
                if (bossColliders[i] != null && summonColliders[j] != null)
                    Physics2D.IgnoreCollision(bossColliders[i], summonColliders[j], true);
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        if (segment.sqrMagnitude <= 0.0001f) return Vector2.Distance(point, start);
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, start + segment * t);
    }

    private void FinishPattern()
    {
        hys_FinalBossPatternId completed = activePattern;
        activeRoutine = null;
        activePattern = hys_FinalBossPatternId.None;
        isGroggy = false;
        lastCompleted = completed;
        if (runtimeStatus != null && !runtimeStatus.IsDead) runtimeStatus.SetState(HWJ_RuntimeState.Idle);
        Action<hys_FinalBossPatternId> callback = finishedCallback;
        finishedCallback = null;
        callback?.Invoke(completed);
    }

    private void StopHorizontalMovement()
    {
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void CacheReferences()
    {
        if (dataResolver == null) dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        if (combatSystem == null) combatSystem = GetComponent<HWJ_CombatSystem>();
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (shieldSystem == null) shieldSystem = GetComponent<hys_FinalBossShield>();
    }
}
