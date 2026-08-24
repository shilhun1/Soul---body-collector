using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum hys_SecondBossPatternId
{
    None,
    DashReverseSlash,
    SummonCommand,
    HighSpeedPiercingSlash,
    DarkMagicSummonAssault,
    MagicSwordEncirclement,
    GroundSwordEruption
}

// Animator가 문자열 디버그 문구 대신 안정적인 번호로 현재 동작 구간을 읽습니다.
public enum hys_SecondBossAnimationAction
{
    None,
    Dash,
    ReverseSlash,
    SummonCommand,
    TwoHandPrepare,
    WideSlash,
    PlantSword,
    ShockwaveChannel,
    DashSwordWave,
    MagicCast,
    MagicRelease
}

public class hys_SecondBossPattern : MonoBehaviour
{
    // 두 번째 보스의 세 가지 전용 패턴과 각 패턴 쿨타임을 실행합니다.
    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private hys_SecondBossSummonSpawner summonSpawner;

    [Header("HWJ 패턴 데이터")]
    // 패턴 번호, 쿨타임, 가중치, HP/페이즈/거리 조건은 HWJ 공통 데이터에서 읽습니다.
    [SerializeField] private HWJ_BossPatternDataSO[] patternData = new HWJ_BossPatternDataSO[6];
    [SerializeField] private bool useOrderedOpeningSequence = true;
    [SerializeField] private bool preventSamePatternConsecutively = true;
    // 테스트 중에는 체력과 관계없이 페이즈 2 패턴 4, 5, 6만 선택합니다.
    [SerializeField] private bool forcePhaseTwoForTesting;

    [Header("공통 표시")]
    [SerializeField] private Color warningColor = new Color(0.85f, 0.12f, 0.08f, 0.9f);
    [SerializeField, Min(0.01f)] private float warningLineWidth = 0.08f;
    [SerializeField] private Color phaseTwoMagicColor = new Color(0.45f, 0.12f, 0.85f, 0.95f);

    [Header("패턴 1 - 대시 후 역방향 베기")]
    // 대시에는 피해가 없고, 플레이어를 지나친 뒤 사용하는 역방향 베기에만 피해가 있습니다.
    [SerializeField, Min(0f)] private float pattern1CooldownSeconds = 8f;
    // 보스 패턴답게 짧은 예고 뒤 빠르게 통과하고, 뒤쪽에서 넓은 역방향 베기를 사용합니다.
    [SerializeField, Min(0.01f)] private float pattern1PrepareSeconds = 0.15f;
    [SerializeField, Min(0.1f)] private float pattern1DashSpeed = 38f;
    [SerializeField, Min(0f)] private float pattern1OvershootDistance = 3f;
    [SerializeField, Min(0.1f)] private float pattern1MaxDashSeconds = 1.2f;
    [SerializeField, Min(0f)] private float pattern1LandingWaitSeconds = 0.15f;
    [SerializeField, Min(0.01f)] private float pattern1SlashWarningSeconds = 0.12f;
    [SerializeField] private Vector2 pattern1SlashBoxSize = new Vector2(5.2f, 3.6f);
    [SerializeField] private Vector2 pattern1SlashBoxOffset = new Vector2(2.3f, 0.25f);
    [SerializeField, Min(0f)] private float pattern1DamageMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float pattern1RecoverySeconds = 0.45f;

    [Header("패턴 2 - 공격 명령과 몬스터 소환")]
    [SerializeField, Min(0f)] private float pattern2CooldownSeconds = 60f;
    [SerializeField, Min(0f)] private float pattern2CommandSeconds = 0.8f;
    [SerializeField, Range(1, 5)] private int summonCount = 3;

    [Header("패턴 3 - 전방 접근 베기와 그로기")]
    [SerializeField, Min(0f)] private float pattern3CooldownSeconds = 20f;
    [SerializeField, Min(0.01f)] private float pattern3PrepareSeconds = 0.7f;
    [SerializeField, Min(0.1f)] private float pattern3DashSpeed = 34f;
    [SerializeField, Min(0f)] private float pattern3StopDistance = 2.4f;
    [SerializeField, Min(0.1f)] private float pattern3MaxDashSeconds = 1.1f;
    [SerializeField, Min(0.01f)] private float pattern3SlashWarningSeconds = 0.14f;
    [SerializeField] private Vector2 pattern3HitBoxSize = new Vector2(6.5f, 4.4f);
    // 플레이어 앞에서 멈춘 뒤 진행 방향의 넓은 대검 범위에만 피해가 발생합니다.
    [SerializeField] private Vector2 pattern3SlashBoxOffset = new Vector2(2.8f, 0.3f);
    [SerializeField, Min(0f)] private float pattern3DamageMultiplier = 1.8f;
    [SerializeField, Min(0f)] private float pattern3GroggySeconds = 3f;
    // 그로기가 끝날 때 가까이 있는 플레이어를 보스 바깥 방향으로 밀어냅니다.
    [SerializeField, Min(0f)] private float pattern3GroggyEndKnockbackRadius = 6f;
    [SerializeField, Min(0f)] private float pattern3GroggyEndKnockbackPower = 8f;
    [SerializeField, Min(0.05f)] private float pattern3GroggyEndKnockbackSeconds = 0.25f;

    [Header("페이즈 2 패턴 4 - 충격파, 소환, 검기")]
    [SerializeField, Min(0f)] private float pattern4CooldownSeconds = 60f;
    [SerializeField, Min(0.01f)] private float pattern4PlantSwordSeconds = 0.65f;
    [SerializeField, Range(2, 10)] private int pattern4ShockwaveCount = 6;
    [SerializeField, Min(0.1f)] private float pattern4ShockwaveSpacing = 1.5f;
    [SerializeField, Min(0.01f)] private float pattern4ShockwaveWarningSeconds = 0.18f;
    [SerializeField, Min(0.01f)] private float pattern4ShockwaveIntervalSeconds = 0.09f;
    [SerializeField] private Vector2 pattern4ShockwaveSize = new Vector2(1.25f, 4.2f);
    [SerializeField, Min(0f)] private float pattern4ShockwaveDamageMultiplier = 1.25f;
    [SerializeField, Range(1, 5)] private int pattern4SummonCount = 2;
    [SerializeField, Range(1, 5)] private int pattern4AssaultCount = 2;
    [SerializeField, Min(0.1f)] private float pattern4FirstSummonDistance = 4f;
    [SerializeField, Min(0.1f)] private float pattern4SummonDistanceSpacing = 4f;
    [SerializeField, Min(0.1f)] private float pattern4DashSpeed = 24f;
    [SerializeField, Min(0.1f)] private float pattern4DashMaxSeconds = 1.2f;
    [SerializeField, Min(0f)] private float pattern4SwordFireDelaySeconds = 0.08f;
    [SerializeField, Min(0.01f)] private float pattern4SwordWaveWarningSeconds = 0.12f;
    [SerializeField, Min(0.1f)] private float pattern4SwordWaveSpeed = 22f;
    [SerializeField, Min(0.1f)] private float pattern4SwordWaveHitRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float pattern4SwordVisualLength = 4.2f;
    [SerializeField, Min(0.05f)] private float pattern4SwordVisualWidth = 0.55f;
    [SerializeField, Min(0f)] private float pattern4SwordWaveDamageMultiplier = 1.35f;

    [Header("페이즈 2 패턴 5 - 마력 검 포위 찌르기")]
    [SerializeField, Min(0f)] private float pattern5CooldownSeconds = 15f;
    [SerializeField, Min(0.1f)] private float pattern5MarkSeconds = 2f;
    [SerializeField, Min(0.1f)] private float pattern5MarkRadius = 1.4f;
    [SerializeField, Range(4, 8)] private int pattern5SwordCount = 4;
    [SerializeField, Min(0.1f)] private float pattern5SwordSpawnRadius = 9f;
    [SerializeField, Min(0.1f)] private float pattern5SwordTravelSpeed = 18f;
    [SerializeField, Min(0.1f)] private float pattern5SwordHitRadius = 0.75f;
    [SerializeField, Min(0.1f)] private float pattern5SwordVisualLength = 3.2f;
    [SerializeField, Min(0.05f)] private float pattern5SwordVisualWidth = 0.38f;
    [SerializeField, Min(0f)] private float pattern5DamageMultiplier = 1.6f;

    [Header("페이즈 2 패턴 6 - 바닥 검 솟구치기")]
    [SerializeField, Min(0f)] private float pattern6CooldownSeconds = 10f;
    [SerializeField, Min(0.1f)] private float pattern6MarkSeconds = 2f;
    [SerializeField] private Vector2 pattern6EruptionSize = new Vector2(8f, 12f);
    [SerializeField] private LayerMask pattern6GroundLayerMask = ~0;
    [SerializeField, Min(1f)] private float pattern6GroundSearchDistance = 30f;
    [SerializeField, Min(0.1f)] private float pattern6SwordRiseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float pattern6DamageMultiplier = 1.75f;
    [SerializeField, Min(0.1f)] private float pattern6SwordRemainSeconds = 2f;

    [Header("디버그")]
    [SerializeField] private hys_SecondBossPatternId activePattern = hys_SecondBossPatternId.None;
    [SerializeField] private hys_SecondBossAnimationAction currentAnimationAction;
    [SerializeField] private bool isGroggy;
    [SerializeField] private int currentPhaseNumber = 1;
    [SerializeField] private string lastPatternAction;

    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];
    private Coroutine activeRoutine;
    private Action<hys_SecondBossPatternId> finishedCallback;
    private Transform currentTarget;
    private Vector3 lockedTargetPosition;
    private float lockedDirection = 1f;
    private float pattern1ReadyTime;
    private float pattern2ReadyTime;
    private float pattern3ReadyTime;
    private float pattern4ReadyTime;
    private float pattern5ReadyTime;
    private float pattern6ReadyTime;
    private Vector3 initialBossPosition;
    private int openingPatternStep;
    private hys_SecondBossPatternId lastCompletedPattern = hys_SecondBossPatternId.None;
    private readonly List<hys_SecondBossPatternId> selectablePatterns = new List<hys_SecondBossPatternId>(3);

    public bool IsPatternRunning => activeRoutine != null;
    public bool IsGroggy => isGroggy;
    public hys_SecondBossPatternId ActivePattern => activePattern;
    public hys_SecondBossAnimationAction CurrentAnimationAction => currentAnimationAction;
    public string LastPatternAction => lastPatternAction;
    public int CurrentPhaseNumber => currentPhaseNumber;
    public hys_SecondBossSummonSpawner SummonSpawner => summonSpawner;
    // 강한 타격 순간을 Cinemachine Impulse 연출에 전달합니다.
    public event Action<float> ImpactRequested;

    private void Awake()
    {
        initialBossPosition = transform.position;
        CacheReferences();
    }

    public void ResetPatternSelection()
    {
        openingPatternStep = 0;
        lastCompletedPattern = hys_SecondBossPatternId.None;
        pattern1ReadyTime = 0f;
        pattern2ReadyTime = 0f;
        pattern3ReadyTime = 0f;
        pattern4ReadyTime = 0f;
        pattern5ReadyTime = 0f;
        pattern6ReadyTime = 0f;
    }

    public bool TryStartNextPattern(
        Transform target,
        float attackDirection,
        Action<hys_SecondBossPatternId> onFinished,
        out hys_SecondBossPatternId selectedPattern)
    {
        selectedPattern = hys_SecondBossPatternId.None;
        if (target == null || activeRoutine != null) return false;
        currentPhaseNumber = ResolveCurrentPhaseNumber();

        // 전투 시작 시에는 데이터 번호 1, 2, 3 순서로 한 번씩 실행합니다.
        if (currentPhaseNumber == 1 && useOrderedOpeningSequence && openingPatternStep < 3)
        {
            hys_SecondBossPatternId openingPattern = GetPatternIdByNumber(openingPatternStep + 1);
            if (!CanUsePattern(openingPattern, target)) return false;

            Action<hys_SecondBossPatternId> openingFinished = completed =>
            {
                if (completed == openingPattern) openingPatternStep++;
                lastCompletedPattern = completed;
                onFinished?.Invoke(completed);
            };

            if (!TryStartPattern(openingPattern, target, attackDirection, openingFinished)) return false;
            selectedPattern = openingPattern;
            return true;
        }

        selectablePatterns.Clear();
        AddSelectablePattern(hys_SecondBossPatternId.DashReverseSlash, target);
        AddSelectablePattern(hys_SecondBossPatternId.SummonCommand, target);
        AddSelectablePattern(hys_SecondBossPatternId.HighSpeedPiercingSlash, target);
        AddSelectablePattern(hys_SecondBossPatternId.DarkMagicSummonAssault, target);
        AddSelectablePattern(hys_SecondBossPatternId.MagicSwordEncirclement, target);
        AddSelectablePattern(hys_SecondBossPatternId.GroundSwordEruption, target);
        if (selectablePatterns.Count == 0) return false;

        selectedPattern = SelectWeightedPattern();
        Action<hys_SecondBossPatternId> randomFinished = completed =>
        {
            lastCompletedPattern = completed;
            onFinished?.Invoke(completed);
        };

        return TryStartPattern(selectedPattern, target, attackDirection, randomFinished);
    }

    public bool CanUsePattern(hys_SecondBossPatternId patternId, Transform target = null)
    {
        if (activeRoutine != null || runtimeStatus != null && runtimeStatus.IsDead) return false;

        HWJ_BossPatternDataSO data = GetPatternData(patternId);
        if (data != null && !IsPatternDataConditionMatched(data, target)) return false;

        switch (patternId)
        {
            case hys_SecondBossPatternId.DashReverseSlash: return Time.time >= pattern1ReadyTime;
            case hys_SecondBossPatternId.SummonCommand: return Time.time >= pattern2ReadyTime;
            case hys_SecondBossPatternId.HighSpeedPiercingSlash: return Time.time >= pattern3ReadyTime;
            case hys_SecondBossPatternId.DarkMagicSummonAssault: return Time.time >= pattern4ReadyTime;
            case hys_SecondBossPatternId.MagicSwordEncirclement: return Time.time >= pattern5ReadyTime;
            case hys_SecondBossPatternId.GroundSwordEruption: return Time.time >= pattern6ReadyTime;
            default: return false;
        }
    }

    public bool TryStartPattern(hys_SecondBossPatternId patternId, Transform target, float attackDirection,
        Action<hys_SecondBossPatternId> onFinished)
    {
        CacheReferences();
        if (target == null || !CanUsePattern(patternId, target)) return false;

        currentTarget = target;
        lockedTargetPosition = target.position;
        lockedDirection = attackDirection == 0f ? 1f : Mathf.Sign(attackDirection);
        finishedCallback = onFinished;
        activePattern = patternId;
        isGroggy = false;
        SetPatternCooldown(patternId);

        switch (patternId)
        {
            case hys_SecondBossPatternId.DashReverseSlash:
                activeRoutine = StartCoroutine(DashReverseSlashRoutine());
                break;
            case hys_SecondBossPatternId.SummonCommand:
                activeRoutine = StartCoroutine(SummonCommandRoutine());
                break;
            case hys_SecondBossPatternId.HighSpeedPiercingSlash:
                activeRoutine = StartCoroutine(HighSpeedPiercingSlashRoutine());
                break;
            case hys_SecondBossPatternId.DarkMagicSummonAssault:
                activeRoutine = StartCoroutine(DarkMagicSummonAssaultRoutine());
                break;
            case hys_SecondBossPatternId.MagicSwordEncirclement:
                activeRoutine = StartCoroutine(MagicSwordEncirclementRoutine());
                break;
            case hys_SecondBossPatternId.GroundSwordEruption:
                activeRoutine = StartCoroutine(GroundSwordEruptionRoutine());
                break;
            default:
                activePattern = hys_SecondBossPatternId.None;
                finishedCallback = null;
                return false;
        }

        return true;
    }

    public void CancelActivePattern()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = null;
        activePattern = hys_SecondBossPatternId.None;
        currentAnimationAction = hys_SecondBossAnimationAction.None;
        isGroggy = false;
        currentTarget = null;
        finishedCallback = null;
        StopHorizontalMovement();
    }

    private IEnumerator DashReverseSlashRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.Dash);
        lastPatternAction = "패턴 1 준비";
        FaceDirection(lockedDirection);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern1PrepareSeconds));

        // 준비가 끝난 시점의 플레이어 방향과 위치를 다시 잡아 실제 뒤쪽까지 통과합니다.
        float dashDirection = ResolveDirectionToCurrentTarget(lockedDirection);
        FaceDirection(dashDirection);
        lastPatternAction = "피해 없는 대시";
        yield return MoveHorizontallyBehindTarget(
            dashDirection,
            pattern1DashSpeed,
            pattern1MaxDashSeconds);
        yield return WaitForLanding(pattern1LandingWaitSeconds);

        float reverseDirection = -dashDirection;
        FaceDirection(reverseDirection);
        Vector3 slashCenter = transform.position
            + Vector3.right * reverseDirection * Mathf.Abs(pattern1SlashBoxOffset.x)
            + Vector3.up * pattern1SlashBoxOffset.y;
        ShowRectangleWarning(slashCenter, pattern1SlashBoxSize, pattern1SlashWarningSeconds);
        SetAnimationAction(hys_SecondBossAnimationAction.ReverseSlash);
        lastPatternAction = "역방향 베기";
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern1SlashWarningSeconds));
        TryDamageTargetInBox(slashCenter, pattern1SlashBoxSize, pattern1DamageMultiplier);
        RequestImpact(0.3f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern1RecoverySeconds));
        FinishPattern();
    }

    private IEnumerator MoveHorizontallyBehindTarget(float direction, float speed, float maxSeconds)
    {
        float safeSpeed = Mathf.Max(0.1f, speed);
        float initialTargetX = currentTarget != null ? currentTarget.position.x : lockedTargetPosition.x;
        float initialDestinationX = initialTargetX + direction * pattern1OvershootDistance;
        float initialDistance = Mathf.Abs(initialDestinationX - transform.position.x);

        // 인식 범위 끝에서 시작해도 플레이어 뒤까지 도달할 시간을 확보하되 무한 추적은 막습니다.
        float safetySeconds = initialDistance / safeSpeed + 0.75f;
        float endTime = Time.time + Mathf.Max(Mathf.Max(0.1f, maxSeconds), safetySeconds);

        while (Time.time < endTime)
        {
            float targetX = currentTarget != null ? currentTarget.position.x : lockedTargetPosition.x;
            float destinationX = targetX + direction * pattern1OvershootDistance;

            if (HasPassedDestination(destinationX, direction))
            {
                break;
            }

            if (body != null)
            {
                body.linearVelocity = new Vector2(direction * safeSpeed, body.linearVelocity.y);
            }
            else
            {
                transform.position += Vector3.right * direction * safeSpeed * Time.fixedDeltaTime;
            }

            yield return new WaitForFixedUpdate();
        }

        StopHorizontalMovement();
    }

    private float ResolveDirectionToCurrentTarget(float fallbackDirection)
    {
        if (currentTarget == null)
        {
            return fallbackDirection == 0f ? 1f : Mathf.Sign(fallbackDirection);
        }

        float deltaX = currentTarget.position.x - transform.position.x;
        return Mathf.Abs(deltaX) <= 0.01f
            ? (fallbackDirection == 0f ? 1f : Mathf.Sign(fallbackDirection))
            : Mathf.Sign(deltaX);
    }

    private IEnumerator SummonCommandRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.SummonCommand);
        lastPatternAction = "패턴 2 공격 명령";
        FaceDirection(lockedDirection);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern2CommandSeconds));
        if (summonSpawner != null)
            yield return summonSpawner.SpawnFormationRoutine(summonCount);
        lastPatternAction = "빙의 몬스터 소환 완료";
        FinishPattern();
    }

    private IEnumerator HighSpeedPiercingSlashRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.TwoHandPrepare);
        lastPatternAction = "패턴 3 양손 준비 자세";
        FaceDirection(lockedDirection);
        float targetX = currentTarget != null ? currentTarget.position.x : lockedTargetPosition.x;
        float destinationX = targetX - lockedDirection * pattern3StopDistance;
        Vector3 warningCenter = new Vector3(
            (transform.position.x + destinationX) * 0.5f,
            transform.position.y,
            transform.position.z);
        Vector2 warningSize = new Vector2(
            Mathf.Abs(destinationX - transform.position.x),
            pattern3HitBoxSize.y);
        ShowRectangleWarning(warningCenter, warningSize, pattern3PrepareSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern3PrepareSeconds));

        // 준비가 끝난 시점의 플레이어 앞쪽 좌표를 다시 계산하되 최초 공격 방향은 유지합니다.
        targetX = currentTarget != null ? currentTarget.position.x : lockedTargetPosition.x;
        destinationX = targetX - lockedDirection * pattern3StopDistance;
        SetAnimationAction(hys_SecondBossAnimationAction.Dash);
        lastPatternAction = "플레이어 앞까지 무피해 대시";
        yield return MoveHorizontallyTo(
            destinationX,
            lockedDirection,
            pattern3DashSpeed,
            pattern3MaxDashSeconds);

        StopHorizontalMovement();
        FaceDirection(lockedDirection);
        Vector3 slashCenter = transform.position
            + Vector3.right * lockedDirection * Mathf.Abs(pattern3SlashBoxOffset.x)
            + Vector3.up * pattern3SlashBoxOffset.y;
        SetAnimationAction(hys_SecondBossAnimationAction.WideSlash);
        lastPatternAction = "넓은 전방 대검 베기";
        ShowRectangleWarning(slashCenter, pattern3HitBoxSize, pattern3SlashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern3SlashWarningSeconds));
        TryDamageTargetInBox(slashCenter, pattern3HitBoxSize, pattern3DamageMultiplier);
        RequestImpact(0.45f);

        SetAnimationAction(hys_SecondBossAnimationAction.None);
        isGroggy = true;
        lastPatternAction = "갑옷 무게로 넘어짐 - 3초 그로기";
        if (runtimeStatus != null && !runtimeStatus.IsDead) runtimeStatus.SetState(HWJ_RuntimeState.Hit);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern3GroggySeconds));
        isGroggy = false;
        ApplyPattern3GroggyEndKnockback();
        FinishPattern();
    }

    private void ApplyPattern3GroggyEndKnockback()
    {
        if (currentTarget == null || pattern3GroggyEndKnockbackPower <= 0f)
        {
            return;
        }

        Vector2 offset = currentTarget.position - transform.position;
        if (offset.sqrMagnitude > pattern3GroggyEndKnockbackRadius * pattern3GroggyEndKnockbackRadius)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem = currentTarget.GetComponent<HWJ_KnockbackSystem>();
        if (knockbackSystem == null)
        {
            knockbackSystem = currentTarget.gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector2 knockbackDirection = new Vector2(offset.x, 0f);
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = Vector2.right * (lockedDirection == 0f ? 1f : Mathf.Sign(lockedDirection));
        }

        knockbackSystem.PlayKnockback(
            knockbackDirection,
            pattern3GroggyEndKnockbackPower,
            pattern3GroggyEndKnockbackSeconds);
        lastPatternAction = "그로기 종료 - 주변 플레이어 넉백";
    }

    private IEnumerator MoveHorizontallyTo(
        float destinationX,
        float direction,
        float speed,
        float maxSeconds)
    {
        float endTime = Time.time + Mathf.Max(0.1f, maxSeconds);
        while (Time.time < endTime && !HasPassedDestination(destinationX, direction))
        {
            if (body != null)
                body.linearVelocity = new Vector2(direction * Mathf.Max(0.1f, speed), body.linearVelocity.y);
            else
                transform.position += Vector3.right * direction * Mathf.Max(0.1f, speed) * Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }
        StopHorizontalMovement();
    }

    private IEnumerator DarkMagicSummonAssaultRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.PlantSword);
        lastPatternAction = "패턴 4 검을 꽂아 충격파 준비";
        StopHorizontalMovement();
        float patternDirection = lockedDirection == 0f ? 1f : Mathf.Sign(lockedDirection);
        hys_SecondBossMagicVisual.SpawnSword(
            transform.position,
            Vector2.up,
            pattern4PlantSwordSeconds + 0.35f,
            3.8f,
            0.45f,
            0f,
            phaseTwoMagicColor);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4PlantSwordSeconds));

        SetAnimationAction(hys_SecondBossAnimationAction.ShockwaveChannel);
        lastPatternAction = "지면을 따라 세로 충격파 연속 발생";
        yield return SequentialGroundShockwaveRoutine();

        List<Vector3> assaultAnchors = new List<Vector3>(pattern4SummonCount);
        for (int i = 0; i < Mathf.Max(1, pattern4SummonCount); i++)
        {
            assaultAnchors.Add(transform.position
                + Vector3.right * patternDirection
                * (pattern4FirstSummonDistance + pattern4SummonDistanceSpacing * i)
                + Vector3.up * (summonSpawner != null ? summonSpawner.VerticalOffset : 0.2f));
        }

        SetAnimationAction(hys_SecondBossAnimationAction.SummonCommand);
        lastPatternAction = "보스 전방에 빙의 몬스터 2마리 소환";
        if (summonSpawner != null)
            yield return summonSpawner.SpawnAtPositionsRoutine(assaultAnchors);

        if (assaultAnchors.Count > 0)
        {
            SetAnimationAction(hys_SecondBossAnimationAction.DashSwordWave);
            float enterDirection = Mathf.Sign(assaultAnchors[0].x - transform.position.x);
            if (enterDirection == 0f) enterDirection = patternDirection;
            lastPatternAction = "첫 번째 몬스터 위치로 진입";
            yield return MoveHorizontallyTo(
                assaultAnchors[0].x,
                enterDirection,
                pattern4DashSpeed,
                pattern4DashMaxSeconds);
        }

        int swordCount = Mathf.Max(1, pattern4AssaultCount);
        for (int i = 0; i < swordCount && assaultAnchors.Count > 0; i++)
        {
            int anchorIndex = assaultAnchors.Count > 1 ? (i + 1) % assaultAnchors.Count : 0;
            lastPatternAction = "몬스터 고정 지점 사이 돌진하며 검기 발사";
            yield return DashAndFireSwordWaveRoutine(assaultAnchors[anchorIndex].x);
        }

        FinishPattern();
    }

    private IEnumerator SequentialGroundShockwaveRoutine()
    {
        bool dealtDamage = false;
        ResolvePattern4RoomHorizontalBounds(out float leftBoundary, out float rightBoundary);
        float originX = transform.position.x;
        int safetyStepCount = 64;

        for (int step = 1; step <= safetyStepCount; step++)
        {
            bool hasLeftShockwave = false;
            bool hasRightShockwave = false;
            float distance = pattern4ShockwaveSpacing * step;
            float leftX = originX - distance;
            float rightX = originX + distance;

            if (leftX >= leftBoundary)
            {
                hasLeftShockwave = true;
                ShowPattern4ShockwaveWarning(leftX);
            }

            if (rightX <= rightBoundary)
            {
                hasRightShockwave = true;
                ShowPattern4ShockwaveWarning(rightX);
            }

            if (!hasLeftShockwave && !hasRightShockwave) break;

            yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4ShockwaveWarningSeconds));
            if (hasLeftShockwave && SpawnPattern4GroundShockwave(leftX) && !dealtDamage)
                dealtDamage = true;
            if (hasRightShockwave && SpawnPattern4GroundShockwave(rightX) && !dealtDamage)
                dealtDamage = true;
            RequestImpact(0.12f);

            yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4ShockwaveIntervalSeconds));
        }
    }

    private void ShowPattern4ShockwaveWarning(float xPosition)
    {
        Vector3 center = new Vector3(xPosition, transform.position.y, transform.position.z);
        ShowRectangleWarning(center, pattern4ShockwaveSize, pattern4ShockwaveWarningSeconds);
    }

    private bool SpawnPattern4GroundShockwave(float xPosition)
    {
        Vector3 shockwaveCenter = new Vector3(xPosition, transform.position.y, transform.position.z);
        // 경고선이 사라지는 순간 실제 충격파와 피해를 발생시킵니다.
        hys_SecondBossMagicVisual.SpawnShockwave(
            shockwaveCenter - Vector3.up * pattern4ShockwaveSize.y * 0.5f,
            pattern4ShockwaveIntervalSeconds + 0.32f,
            pattern4ShockwaveSize.x,
            pattern4ShockwaveSize.y,
            phaseTwoMagicColor);
        return TryDamageTargetInBox(
            shockwaveCenter,
            pattern4ShockwaveSize,
            pattern4ShockwaveDamageMultiplier);
    }

    private void ResolvePattern4RoomHorizontalBounds(out float leftBoundary, out float rightBoundary)
    {
        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData)
            && bossData.FSM.useBossRoomBounds)
        {
            float roomCenterX = initialBossPosition.x + bossData.FSM.bossRoomOffset.x;
            float halfRoomWidth = Mathf.Max(1f, bossData.FSM.bossRoomSize.x * 0.5f);
            leftBoundary = roomCenterX - halfRoomWidth;
            rightBoundary = roomCenterX + halfRoomWidth;
            return;
        }

        float fallbackDistance = Mathf.Max(1, pattern4ShockwaveCount) * pattern4ShockwaveSpacing;
        leftBoundary = transform.position.x - fallbackDistance;
        rightBoundary = transform.position.x + fallbackDistance;
    }

    private IEnumerator DashAndFireSwordWaveRoutine(float destinationX)
    {
        float dashDirection = Mathf.Sign(destinationX - transform.position.x);
        if (dashDirection == 0f) dashDirection = lockedDirection == 0f ? 1f : lockedDirection;
        FaceDirection(dashDirection);

        float dashEndTime = Time.time + Mathf.Max(0.1f, pattern4DashMaxSeconds);
        float fireTime = Time.time + Mathf.Max(0f, pattern4SwordFireDelaySeconds);
        bool fired = false;
        Vector3 swordTarget = Vector3.zero;
        float swordArrivalTime = 0f;

        while (Time.time < dashEndTime && !HasPassedDestination(destinationX, dashDirection))
        {
            if (body != null)
                body.linearVelocity = new Vector2(dashDirection * pattern4DashSpeed, body.linearVelocity.y);
            else
                transform.position += Vector3.right * dashDirection * pattern4DashSpeed * Time.fixedDeltaTime;

            if (!fired && Time.time >= fireTime)
                FirePattern4SwordWave(out swordTarget, out swordArrivalTime);

            fired = swordArrivalTime > 0f;
            yield return new WaitForFixedUpdate();
        }

        StopHorizontalMovement();
        if (!fired)
        {
            FirePattern4SwordWave(out swordTarget, out swordArrivalTime);
            fired = true;
        }

        if (fired)
        {
            float remainingTravelSeconds = swordArrivalTime - Time.time;
            if (remainingTravelSeconds > 0f)
                yield return new WaitForSeconds(remainingTravelSeconds);

            TryDamageTargetInCircle(
                swordTarget,
                pattern4SwordWaveHitRadius,
                pattern4SwordWaveDamageMultiplier);
            RequestImpact(0.32f);
        }
    }

    private void FirePattern4SwordWave(out Vector3 swordTarget, out float arrivalTime)
    {
        Vector3 swordStart = transform.position + Vector3.up * 0.5f;
        swordTarget = currentTarget != null ? currentTarget.position : lockedTargetPosition;
        Vector2 swordPath = swordTarget - swordStart;
        if (swordPath.sqrMagnitude <= 0.001f)
            swordPath = Vector2.right * (lockedDirection == 0f ? 1f : lockedDirection);

        float swordSpeed = Mathf.Max(0.1f, pattern4SwordWaveSpeed);
        float travelSeconds = swordPath.magnitude / swordSpeed;
        arrivalTime = Time.time + travelSeconds;
        ShowCircleWarning(
            swordTarget,
            pattern4SwordWaveHitRadius,
            Mathf.Max(pattern4SwordWaveWarningSeconds, travelSeconds));
        hys_SecondBossMagicVisual.SpawnSword(
            swordStart,
            swordPath.normalized,
            travelSeconds + 0.15f,
            pattern4SwordVisualLength,
            pattern4SwordVisualWidth,
            swordSpeed,
            phaseTwoMagicColor);
    }

    private IEnumerator MagicSwordEncirclementRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.MagicCast);
        lastPatternAction = "패턴 5 플레이어 추적 마력 표식";
        if (currentTarget != null)
        {
            hys_SecondBossMagicVisual.SpawnTrackingMark(
                currentTarget,
                pattern5MarkSeconds,
                pattern5MarkRadius,
                phaseTwoMagicColor);
        }
        yield return new WaitForSeconds(Mathf.Max(0.1f, pattern5MarkSeconds));

        if (currentTarget == null)
        {
            FinishPattern();
            yield break;
        }

        // 표식 종료 순간의 위치만 고정하며, 발사 후에는 플레이어를 유도 추적하지 않습니다.
        SetAnimationAction(hys_SecondBossAnimationAction.MagicRelease);
        Vector3 swordCenter = currentTarget.position;
        int swordCount = Mathf.Max(4, pattern5SwordCount);
        float swordSpeed = Mathf.Max(0.1f, pattern5SwordTravelSpeed);
        float longestTravelSeconds = 0f;
        float expectedTravelSeconds = pattern5SwordSpawnRadius / swordSpeed;
        ShowCircleWarning(swordCenter, pattern5SwordHitRadius, expectedTravelSeconds);
        lastPatternAction = "고정된 플레이어 위치 주변에 마력 검 소환";
        for (int i = 0; i < swordCount; i++)
        {
            float angle = Mathf.PI * 2f * i / swordCount;
            Vector3 swordPosition = swordCenter + new Vector3(
                Mathf.Cos(angle) * pattern5SwordSpawnRadius,
                Mathf.Sin(angle) * pattern5SwordSpawnRadius,
                0f);
            Vector2 straightDirection = swordCenter - swordPosition;
            float travelSeconds = straightDirection.magnitude / swordSpeed;
            longestTravelSeconds = Mathf.Max(longestTravelSeconds, travelSeconds);
            hys_SecondBossMagicVisual.SpawnSword(
                swordPosition,
                straightDirection,
                travelSeconds + 0.15f,
                pattern5SwordVisualLength,
                pattern5SwordVisualWidth,
                swordSpeed,
                phaseTwoMagicColor);
        }

        lastPatternAction = "마력 검들이 표식 종료 좌표로 직선 돌진";
        yield return new WaitForSeconds(Mathf.Max(0.01f, longestTravelSeconds));
        TryDamageTargetInCircle(
            swordCenter,
            pattern5SwordHitRadius,
            pattern5DamageMultiplier);
        RequestImpact(0.5f);

        FinishPattern();
    }

    private IEnumerator GroundSwordEruptionRoutine()
    {
        SetAnimationAction(hys_SecondBossAnimationAction.MagicCast);
        Vector3 groundPosition = ResolvePattern6GroundPosition();
        lastPatternAction = "패턴 6 플레이어 아래 실제 지면에 거대 검 표식";
        ShowCircleWarning(groundPosition, pattern6EruptionSize.x * 0.5f, pattern6MarkSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.1f, pattern6MarkSeconds));

        SetAnimationAction(hys_SecondBossAnimationAction.MagicRelease);
        Vector3 eruptionCenter = groundPosition + Vector3.up * pattern6EruptionSize.y * 0.5f;
        lastPatternAction = "바닥에서 거대한 마력 검 솟구침";
        ShowRectangleWarning(eruptionCenter, pattern6EruptionSize, pattern6SwordRemainSeconds);
        hys_SecondBossMagicVisual.SpawnSword(
            groundPosition - Vector3.up * pattern6EruptionSize.y * 0.55f,
            Vector2.up,
            pattern6SwordRemainSeconds,
            pattern6EruptionSize.y,
            pattern6EruptionSize.x * 0.32f,
            pattern6SwordRiseSpeed,
            phaseTwoMagicColor);
        TryDamageTargetInBox(eruptionCenter, pattern6EruptionSize, pattern6DamageMultiplier);
        RequestImpact(0.75f);
        yield return new WaitForSeconds(Mathf.Max(0.1f, pattern6SwordRemainSeconds));
        FinishPattern();
    }

    private Vector3 ResolvePattern6GroundPosition()
    {
        Vector3 targetPosition = currentTarget != null ? currentTarget.position : lockedTargetPosition;
        Vector2 rayOrigin = (Vector2)targetPosition + Vector2.up * 2f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            rayOrigin,
            Vector2.down,
            Mathf.Max(1f, pattern6GroundSearchDistance),
            pattern6GroundLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.isTrigger || hits[i].normal.y < 0.25f) continue;
            // 플레이어, 보스, 소환 몬스터의 몸체는 바닥 후보에서 제외합니다.
            if (hitCollider.GetComponentInParent<HWJ_RootObjectDataResolver>() != null) continue;
            return new Vector3(targetPosition.x, hits[i].point.y, transform.position.z);
        }

        // 레이어 설정이 비어 있더라도 표식이 공중에 생기지 않도록 발밑으로 내리는 예비 위치입니다.
        return new Vector3(targetPosition.x, targetPosition.y - 2f, transform.position.z);
    }

    private IEnumerator WaitForLanding(float maxSeconds)
    {
        float endTime = Time.time + Mathf.Max(0f, maxSeconds);
        while (Time.time < endTime && !HasGroundContact()) yield return new WaitForFixedUpdate();
    }

    private bool HasPassedDestination(float destinationX, float direction)
    {
        float currentX = body != null ? body.position.x : transform.position.x;
        return direction > 0f ? currentX >= destinationX : currentX <= destinationX;
    }

    private bool HasGroundContact()
    {
        if (bodyCollider == null) return true;
        int count = bodyCollider.GetContacts(groundContacts);
        for (int i = 0; i < count; i++)
        {
            if (groundContacts[i].normal.y >= 0.5f) return true;
        }
        return false;
    }

    private bool TryDamageTargetInBox(Vector3 center, Vector2 size, float damageMultiplier)
    {
        if (currentTarget == null || combatSystem == null) return false;
        Vector2 delta = currentTarget.position - center;
        if (Mathf.Abs(delta.x) > Mathf.Max(0.1f, size.x) * 0.5f
            || Mathf.Abs(delta.y) > Mathf.Max(0.1f, size.y) * 0.5f) return false;
        HWJ_RootObjectDataResolver targetResolver = currentTarget.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return targetResolver != null && combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _);
    }

    private bool TryDamageTargetInCircle(Vector3 center, float radius, float damageMultiplier)
    {
        if (currentTarget == null || combatSystem == null) return false;
        if (Vector2.Distance(currentTarget.position, center) > Mathf.Max(0.1f, radius)) return false;

        HWJ_RootObjectDataResolver targetResolver =
            currentTarget.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return targetResolver != null
            && combatSystem.TryDealDamageTo(targetResolver, damageMultiplier, out _);
    }

    private void AddSelectablePattern(hys_SecondBossPatternId patternId, Transform target)
    {
        if (preventSamePatternConsecutively && patternId == lastCompletedPattern) return;
        if (CanUsePattern(patternId, target)) selectablePatterns.Add(patternId);
    }

    private hys_SecondBossPatternId SelectWeightedPattern()
    {
        int totalWeight = 0;
        for (int i = 0; i < selectablePatterns.Count; i++)
        {
            HWJ_BossPatternDataSO data = GetPatternData(selectablePatterns[i]);
            totalWeight += Mathf.Max(1, data != null ? data.Weight : 1);
        }

        int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
        for (int i = 0; i < selectablePatterns.Count; i++)
        {
            HWJ_BossPatternDataSO data = GetPatternData(selectablePatterns[i]);
            roll -= Mathf.Max(1, data != null ? data.Weight : 1);
            if (roll < 0) return selectablePatterns[i];
        }

        return selectablePatterns[0];
    }

    private bool IsPatternDataConditionMatched(HWJ_BossPatternDataSO data, Transform target)
    {
        if (data == null) return true;

        float hpRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? runtimeStatus.CurrentHp / runtimeStatus.MaxHp
            : 1f;
        if (!data.IsHpConditionMatched(hpRatio)) return false;

        int phaseNumber = ResolveCurrentPhaseNumber();
        bool isCloseRange = false;
        if (dataResolver != null && dataResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            if (target != null)
            {
                float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
                isCloseRange = horizontalDistance <= Mathf.Max(0f, bossData.FSM.closeSkillRange);
            }
        }

        return data.IsPhaseAllowed(phaseNumber) && data.IsRangeMatched(isCloseRange);
    }

    private int ResolveCurrentPhaseNumber()
    {
        if (forcePhaseTwoForTesting) return 2;

        float hpRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? runtimeStatus.CurrentHp / runtimeStatus.MaxHp
            : 1f;

        if (dataResolver != null && dataResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
            return hpRatio <= bossData.FSM.phaseTwoHpRatio ? 2 : 1;

        return hpRatio <= 0.5f ? 2 : 1;
    }

    private HWJ_BossPatternDataSO GetPatternData(hys_SecondBossPatternId patternId)
    {
        int patternNumber = GetPatternNumber(patternId);
        if (patternData == null || patternNumber <= 0) return null;

        for (int i = 0; i < patternData.Length; i++)
        {
            if (patternData[i] != null && patternData[i].PatternNumber == patternNumber)
                return patternData[i];
        }

        return null;
    }

    private static int GetPatternNumber(hys_SecondBossPatternId patternId)
    {
        switch (patternId)
        {
            case hys_SecondBossPatternId.DashReverseSlash: return 1;
            case hys_SecondBossPatternId.SummonCommand: return 2;
            case hys_SecondBossPatternId.HighSpeedPiercingSlash: return 3;
            case hys_SecondBossPatternId.DarkMagicSummonAssault: return 4;
            case hys_SecondBossPatternId.MagicSwordEncirclement: return 5;
            case hys_SecondBossPatternId.GroundSwordEruption: return 6;
            default: return 0;
        }
    }

    private static hys_SecondBossPatternId GetPatternIdByNumber(int patternNumber)
    {
        switch (patternNumber)
        {
            case 1: return hys_SecondBossPatternId.DashReverseSlash;
            case 2: return hys_SecondBossPatternId.SummonCommand;
            case 3: return hys_SecondBossPatternId.HighSpeedPiercingSlash;
            case 4: return hys_SecondBossPatternId.DarkMagicSummonAssault;
            case 5: return hys_SecondBossPatternId.MagicSwordEncirclement;
            case 6: return hys_SecondBossPatternId.GroundSwordEruption;
            default: return hys_SecondBossPatternId.None;
        }
    }

    private void SetPatternCooldown(hys_SecondBossPatternId patternId)
    {
        HWJ_BossPatternDataSO data = GetPatternData(patternId);
        float cooldownSeconds = data != null ? data.EffectiveCooldownSeconds : -1f;

        switch (patternId)
        {
            case hys_SecondBossPatternId.DashReverseSlash:
                pattern1ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern1CooldownSeconds); break;
            case hys_SecondBossPatternId.SummonCommand:
                pattern2ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern2CooldownSeconds); break;
            case hys_SecondBossPatternId.HighSpeedPiercingSlash:
                pattern3ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern3CooldownSeconds); break;
            case hys_SecondBossPatternId.DarkMagicSummonAssault:
                pattern4ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern4CooldownSeconds); break;
            case hys_SecondBossPatternId.MagicSwordEncirclement:
                pattern5ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern5CooldownSeconds); break;
            case hys_SecondBossPatternId.GroundSwordEruption:
                pattern6ReadyTime = Time.time + Mathf.Max(0f,
                    cooldownSeconds >= 0f ? cooldownSeconds : pattern6CooldownSeconds); break;
        }
    }

    private void FinishPattern()
    {
        hys_SecondBossPatternId completed = activePattern;
        activeRoutine = null;
        activePattern = hys_SecondBossPatternId.None;
        currentAnimationAction = hys_SecondBossAnimationAction.None;
        isGroggy = false;
        currentTarget = null;
        if (runtimeStatus != null && !runtimeStatus.IsDead) runtimeStatus.SetState(HWJ_RuntimeState.Idle);
        Action<hys_SecondBossPatternId> callback = finishedCallback;
        finishedCallback = null;
        callback?.Invoke(completed);
    }

    private void SetAnimationAction(hys_SecondBossAnimationAction action)
    {
        currentAnimationAction = action;
    }

    private void RequestImpact(float force)
    {
        ImpactRequested?.Invoke(Mathf.Max(0f, force));
    }

    private void ShowRectangleWarning(Vector3 center, Vector2 size, float duration)
    {
        hys_SkillWarningIndicator.ShowRectangle(center,
            new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y)),
            Mathf.Max(0.01f, duration), warningColor, warningLineWidth);
    }

    private void ShowCircleWarning(Vector3 center, float radius, float duration)
    {
        hys_SkillWarningIndicator.ShowCircle(
            center,
            Mathf.Max(0.1f, radius),
            Mathf.Max(0.01f, duration),
            warningColor,
            warningLineWidth);
    }

    private void FaceDirection(float direction)
    {
        if (spriteRenderer != null && direction != 0f) spriteRenderer.flipX = direction < 0f;
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
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (summonSpawner == null) summonSpawner = GetComponent<hys_SecondBossSummonSpawner>();
    }

    private void OnDisable() { CancelActivePattern(); }
}
