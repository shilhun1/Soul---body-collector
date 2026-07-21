using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 물리형 중간보스가 사용하는 패턴 실행 시스템입니다.
/// 애니메이션과 아트가 없어도 이동, 예고 표시, 소환, 데미지 판정, 그로기 흐름을 먼저 테스트할 수 있습니다.
/// </summary>
public class HWJ_MidBossPatternSystem : MonoBehaviour, HWJ_IBossSpecialPatternExecutor
{
    [Header("패턴 1 - 기본 소환 프리팹")]
    [Tooltip("패턴 1에서 소환할 기본 몬스터 프리팹입니다. 아래 소환 몬스터 프리팹 목록이 비어 있거나 해당 칸이 비어 있으면 이 프리팹을 사용합니다.")]
    [InspectorName("기본 소환 몬스터 프리팹")]
    [FormerlySerializedAs("summonMonsterPerfabs")]
    [SerializeField] private GameObject summonMonsterPrefab;

    [Header("참조")]
    [Tooltip("보스의 상태 머신을 관리하는 시스템입니다.")]
    [InspectorName("보스 상태 시스템")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [Tooltip("보스의 현재 체력, 사망 여부, 방어력 같은 런타임 상태를 관리합니다.")]
    [InspectorName("런타임 상태 시스템")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [Tooltip("보스가 플레이어에게 데미지를 줄 때 사용하는 전투 계산 시스템입니다.")]
    [InspectorName("전투 시스템")]
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [Tooltip("보스 패턴 중 이동 잠금, 스킬 취소 같은 조작 제한을 처리합니다.")]
    [InspectorName("스킬 실행 시스템")]
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [Tooltip("보스 이동과 정지 처리에 사용하는 Rigidbody2D입니다.")]
    [InspectorName("보스 Rigidbody2D")]
    [SerializeField] private Rigidbody2D body;
    [Tooltip("은신, 등장 같은 연출에서 보스 스프라이트 표시 여부를 제어합니다.")]
    [InspectorName("보스 스프라이트 목록")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("패턴 실행 키")]
    [Tooltip("BossPatternData의 Custom Executor Key와 같은 값이어야 이 시스템이 해당 패턴을 실행합니다.")]
    [InspectorName("실행 키")]
    [SerializeField] private string executorKey = "mid_boss_physical";

    [Header("공통 타이밍")]
    [Tooltip("보스가 대쉬 이동에 사용하는 시간입니다.")]
    [InspectorName("대쉬 시간")]
    [SerializeField] private float dashSeconds = 0.18f;
    [Tooltip("공격 판정이 나오기 전 예고 표시가 유지되는 시간입니다.")]
    [InspectorName("베기 예고 시간")]
    [SerializeField] private float slashWarningSeconds = 0.35f;
    [Tooltip("패턴이 끝난 뒤 다음 행동으로 넘어가기 전 대기 시간입니다.")]
    [InspectorName("후딜 시간")]
    [SerializeField] private float recoverySeconds = 0.25f;
    [Tooltip("가로 베기와 전방 공격의 기본 사거리입니다.")]
    [InspectorName("가로 베기 사거리")]
    [SerializeField] private float horizontalSlashRange = 3f;
    [Tooltip("세로 베기나 원형 충격 판정의 반지름입니다.")]
    [InspectorName("세로 베기 반지름")]
    [SerializeField] private float verticalSlashRadius = 1.35f;
    [Tooltip("충격파가 위아래로 판정되는 높이입니다.")]
    [InspectorName("충격파 높이")]
    [SerializeField] private float shockwaveHeight = 1.2f;
    [Tooltip("임시 예고 선의 두께입니다.")]
    [InspectorName("예고 선 두께")]
    [SerializeField] private float warningLineWidth = 0.06f;
    [Tooltip("임시 예고 표시의 색상입니다.")]
    [InspectorName("예고 색상")]
    [SerializeField] private Color warningColor = new Color(1f, 0.1f, 0.05f, 0.85f);

    [Header("패턴 1 - 몬스터 소환")]
    [Tooltip("보스 체력이 이 비율만큼 깎일 때마다 패턴 1 사용 기회를 1회 쌓습니다. 0.15는 15%입니다.")]
    [InspectorName("체력 감소 사용 간격")]
    [SerializeField] private float pattern1HpLossIntervalRatio = 0.15f;
    [Tooltip("패턴 1에서 소환할 몬스터 수입니다.")]
    [InspectorName("소환 몬스터 수")]
    [SerializeField] private int pattern1SummonCount = 4;
    [Tooltip("소환 전에 보스가 호루라기 연출로 대기하는 시간입니다.")]
    [InspectorName("호루라기 대기 시간")]
    [SerializeField] private float pattern1WhistleSeconds = 2f;
    [Tooltip("몬스터를 한 마리씩 소환할 때 다음 몬스터가 나오기까지의 간격입니다.")]
    [InspectorName("소환 간격")]
    [SerializeField] private float pattern1SummonIntervalSeconds = 0.15f;
    [Tooltip("보스가 맵 끝으로 이동할 때 벽에서 떨어질 거리입니다.")]
    [InspectorName("맵 끝 여유 거리")]
    [SerializeField] private float pattern1EdgePadding = 1f;
    [Tooltip("소환된 몬스터의 저장용 ID를 만들 때 사용하는 접두어입니다.")]
    [InspectorName("소환 몬스터 저장 ID 접두어")]
    [SerializeField] private string pattern1SummonSaveIdPrefix = "mid_boss_summon";
    [Tooltip("보스 위치 기준으로 소환 위치를 조금씩 벌리고 싶을 때 사용하는 간격입니다. 0이면 모든 몬스터가 보스 위치에서 소환됩니다.")]
    [InspectorName("소환 위치 추가 간격")]
    [SerializeField] private Vector2 pattern1SummonStepOffset = Vector2.zero;
    [Tooltip("패턴 1에서 순서대로 사용할 몬스터 프리팹 목록입니다. 비어 있으면 기본 소환 몬스터 프리팹 또는 RootObjectData의 모델 프리팹을 사용합니다.")]
    [InspectorName("소환 몬스터 프리팹 목록")]
    [SerializeField] private GameObject[] possessableMonsterPrefabs;
    [Tooltip("소환 몬스터에게 적용할 RootObjectData 목록입니다. 프리팹에 데이터가 없어도 이 값을 통해 능력치와 빙의 데이터를 넣을 수 있습니다.")]
    [InspectorName("소환 몬스터 RootObjectData 목록")]
    [SerializeField] private HWJ_RootObjectDataSO[] possessableMonsterRootObjects;

    [Header("패턴 3 - 검기")]
    [Tooltip("패턴 3에서 전범위 검기를 날리기 전 차징하는 시간입니다.")]
    [InspectorName("검기 차징 시간")]
    [SerializeField] private float pattern3ChargeSeconds = 1.5f;
    [Tooltip("패턴 3 검기가 위아래로 판정되는 높이입니다.")]
    [InspectorName("검기 판정 높이")]
    [SerializeField] private float pattern3WaveHeight = 1.5f;
    [Tooltip("패턴 3 검기에 적용되는 데미지 배율입니다.")]
    [InspectorName("검기 데미지 배율")]
    [SerializeField] private float pattern3DamageMultiplier = 1.25f;

    [Header("패턴 4 - 고정 피해 돌진")]
    [Tooltip("패턴 4에서 플레이어를 조준하고 돌진하기 전 대기하는 시간입니다.")]
    [InspectorName("조준 시간")]
    [SerializeField] private float pattern4AimSeconds = 0.65f;
    [Tooltip("패턴 4가 플레이어 최대 체력 기준으로 깎는 비율입니다. 0.4는 최대 체력의 40%입니다.")]
    [InspectorName("최대 체력 고정 피해 비율")]
    [SerializeField] private float pattern4FixedMaxHpDamageRatio = 0.4f;
    [Tooltip("패턴 4가 끝난 뒤 보스가 그로기 상태로 멈춰 있는 시간입니다.")]
    [InspectorName("그로기 시간")]
    [SerializeField] private float pattern4GroggySeconds = 3f;

    [Header("패턴 5 - 2페이즈 연속 공격")]
    [Tooltip("패턴 5 시작 시 검에 붉은 이펙트를 두른 상태로 대기하는 시간입니다.")]
    [InspectorName("붉은 검 연출 시간")]
    [SerializeField] private float pattern5BuffHoldSeconds = 0.6f;
    [Tooltip("패턴 5 연속 공격 사이의 시간 간격입니다.")]
    [InspectorName("연속 공격 간격")]
    [SerializeField] private float pattern5ComboIntervalSeconds = 0.35f;
    [Tooltip("패턴 5 마지막 충격파가 보스방 가로 폭에서 차지하는 비율입니다.")]
    [InspectorName("충격파 가로 비율")]
    [SerializeField] private float pattern5ShockwaveWidthRatio = 0.5f;

    [Header("패턴 6 - 은신 기습")]
    [Tooltip("패턴 6에서 은신하기 전 대기하는 시간입니다.")]
    [InspectorName("은신 전 대기 시간")]
    [SerializeField] private float pattern6VanishDelaySeconds = 2f;
    [Tooltip("플레이어 뒤로 순간이동할 때 플레이어와 떨어지는 거리입니다.")]
    [InspectorName("후방 순간이동 거리")]
    [SerializeField] private float pattern6BehindOffset = 1.2f;
    [Tooltip("기습 후 원래 위치로 돌아오기 전 대기하는 시간입니다.")]
    [InspectorName("복귀 전 대기 시간")]
    [SerializeField] private float pattern6ReturnDelaySeconds = 0.25f;

    [Header("패턴 7 - 점프 내려찍기")]
    [Tooltip("패턴 7에서 보스가 위로 올라가는 높이입니다.")]
    [InspectorName("점프 높이")]
    [SerializeField] private float pattern7JumpHeight = 4f;
    [Tooltip("공중에 떠서 플레이어 위치를 조준하는 시간입니다.")]
    [InspectorName("공중 대기 시간")]
    [SerializeField] private float pattern7AirHoldSeconds = 0.65f;
    [Tooltip("내려찍기 착지 지점의 원형 데미지 반지름입니다.")]
    [InspectorName("내려찍기 반지름")]
    [SerializeField] private float pattern7ImpactRadius = 1.6f;
    [Tooltip("패턴 7 착지 충격파가 보스방 가로 폭에서 차지하는 비율입니다.")]
    [InspectorName("착지 충격파 가로 비율")]
    [SerializeField] private float pattern7ShockwaveWidthRatio = 0.5f;

    private Coroutine activePatternRoutine;
    private int pendingPattern1Charges;
    private float nextPattern1HpRatioThreshold;
    private float trackedMaxHp;
    private int summonedMonsterSequence;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => activePatternRoutine != null;

    private void Awake()
    {
        CacheReferences();
        ResetPattern1Tracking();
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        CacheReferences();

        if (!MatchesPatternExecutor(pattern)
            || activePatternRoutine != null
            || runtimeStatus != null && runtimeStatus.IsDead)
        {
            return false;
        }

        RefreshPattern1Charges();

        if (pattern.PatternNumber == 1)
        {
            return pendingPattern1Charges > 0 && !HasPossessableCorpseInRoom();
        }

        return pattern.PatternNumber >= 2 && pattern.PatternNumber <= 7;
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (!CanUsePattern(pattern, target))
        {
            return false;
        }

        switch (pattern.PatternNumber)
        {
            case 1:
                pendingPattern1Charges = Mathf.Max(0, pendingPattern1Charges - 1);
                activePatternRoutine = StartCoroutine(Pattern1SummonRoutine(target));
                return true;
            case 2:
                activePatternRoutine = StartCoroutine(Pattern2DashDoubleSlashRoutine(target));
                return true;
            case 3:
                activePatternRoutine = StartCoroutine(Pattern3SlashAndFullWaveRoutine(target));
                return true;
            case 4:
                activePatternRoutine = StartCoroutine(Pattern4FixedDamageDashRoutine(target));
                return true;
            case 5:
                activePatternRoutine = StartCoroutine(Pattern5RedSwordComboRoutine(target));
                return true;
            case 6:
                activePatternRoutine = StartCoroutine(Pattern6VanishBackstabRoutine(target));
                return true;
            case 7:
                activePatternRoutine = StartCoroutine(Pattern7JumpSlamRoutine(target));
                return true;
            default:
                return false;
        }
    }

    public void CancelActivePattern()
    {
        if (activePatternRoutine != null)
        {
            StopCoroutine(activePatternRoutine);
            activePatternRoutine = null;
        }

        SetSpriteVisible(true);
        skillActionSystem?.CancelCurrentAction();
        StopMovement();
    }

    private IEnumerator Pattern1SummonRoutine(Transform target)
    {
        float totalSeconds = Mathf.Max(0f, dashSeconds)
            + Mathf.Max(0f, pattern1WhistleSeconds)
            + Mathf.Max(0, pattern1SummonCount) * Mathf.Max(0f, pattern1SummonIntervalSeconds)
            + Mathf.Max(0f, recoverySeconds);
        skillActionSystem?.BlockNavigationForSkill(totalSeconds, true);

        Vector3 edgePosition = ResolveFarEdgePosition(target, pattern1EdgePadding);
        yield return DashToPosition(edgePosition, dashSeconds);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern1WhistleSeconds));

        int spawnCount = Mathf.Max(0, pattern1SummonCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPossessableMonster(i);

            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, pattern1SummonIntervalSeconds));
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern2DashDoubleSlashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds + slashWarningSeconds * 2f + 0.5f + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.9f);
        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return new WaitForSeconds(0.5f);
        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern3SlashAndFullWaveRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds * 2f + slashWarningSeconds + pattern3ChargeSeconds + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.75f);
        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return DashToPosition(ResolveFarEdgePosition(target, pattern1EdgePadding), dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, pattern3ChargeSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern3ChargeSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, pattern3DamageMultiplier);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern4FixedDamageDashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(pattern4AimSeconds + dashSeconds + pattern4GroggySeconds, true);

        float direction = GetDirectionToTarget(target);
        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            direction,
            Mathf.Max(1f, horizontalSlashRange * 1.5f),
            0.8f,
            pattern4AimSeconds,
            warningColor,
            warningLineWidth);

        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4AimSeconds));
        yield return DashToTargetSide(target, 0.35f);
        DamageTargetByFixedMaxHpRatio(target, pattern4FixedMaxHpDamageRatio);
        bossBrain?.ForceGroggy(pattern4GroggySeconds);
        activePatternRoutine = null;
    }

    private IEnumerator Pattern5RedSwordComboRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern5BuffHoldSeconds + pattern5ComboIntervalSeconds * 4f + recoverySeconds,
            true);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern5BuffHoldSeconds));

        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, pattern7ImpactRadius, 1f);

        ShowHalfRoomShockwaveWarning(pattern5ShockwaveWidthRatio, shockwaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(target, pattern5ShockwaveWidthRatio, shockwaveHeight, 1.15f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern6VanishBackstabRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern6VanishDelaySeconds + dashSeconds + slashWarningSeconds + pattern6ReturnDelaySeconds + recoverySeconds,
            true);

        Vector3 returnPosition = transform.position;
        yield return new WaitForSeconds(Mathf.Max(0f, pattern6VanishDelaySeconds));
        SetSpriteVisible(false);
        SetPosition(ResolveBehindTargetPosition(target));
        SetSpriteVisible(true);

        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern6ReturnDelaySeconds));
        yield return DashToPosition(returnPosition, dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern7JumpSlamRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(pattern7AirHoldSeconds + slashWarningSeconds + recoverySeconds, true);

        Vector3 startPosition = transform.position;
        Vector3 airPosition = startPosition + Vector3.up * Mathf.Max(0f, pattern7JumpHeight);
        SetPosition(airPosition);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern7AirHoldSeconds));

        Vector3 landingPosition = target != null
            ? new Vector3(target.position.x, startPosition.y, startPosition.z)
            : startPosition;
        ShowCircleWarning(landingPosition, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        SetPosition(landingPosition);
        DamageTargetIfInsideCircle(target, landingPosition, pattern7ImpactRadius, 1.2f);

        ShowHalfRoomShockwaveWarning(pattern7ShockwaveWidthRatio, shockwaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(target, pattern7ShockwaveWidthRatio, shockwaveHeight, 1.1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private void RefreshPattern1Charges()
    {
        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return;
        }

        if (!Mathf.Approximately(trackedMaxHp, runtimeStatus.MaxHp) || nextPattern1HpRatioThreshold <= 0f)
        {
            ResetPattern1Tracking();
        }

        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        float currentRatio = Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp);

        while (nextPattern1HpRatioThreshold > 0f && currentRatio <= nextPattern1HpRatioThreshold)
        {
            pendingPattern1Charges++;
            nextPattern1HpRatioThreshold -= interval;
        }
    }

    private void ResetPattern1Tracking()
    {
        trackedMaxHp = runtimeStatus != null ? runtimeStatus.MaxHp : 0f;
        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        nextPattern1HpRatioThreshold = 1f - interval;
        pendingPattern1Charges = 0;
    }

    private bool MatchesPatternExecutor(HWJ_BossPatternDataSO pattern)
    {
        if (pattern == null || !pattern.UseCustomPatternExecutor)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(pattern.CustomPatternExecutorKey)
            || string.IsNullOrWhiteSpace(executorKey)
            || pattern.CustomPatternExecutorKey == executorKey;
    }

    private bool HasPossessableCorpseInRoom()
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (IsPossessableCorpse(resolvers[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPossessableCorpse(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.gameObject == gameObject)
        {
            return false;
        }

        if (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss)
        {
            return false;
        }

        if (!IsInsideBossRoom(resolver.transform.position))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(resolver, out HWJ_PossessionData possessionData)
            || !possessionData.canBePossessed)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState = resolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null && bodyState.IsConsumed)
        {
            return false;
        }

        if (!possessionData.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver resolver,
        out HWJ_PossessionData possessionData)
    {
        possessionData = null;

        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            possessionData = enemyData.PossessionBody;
        }
        else if (resolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionData = bossData.PossessionBody;
        }

        return possessionData != null;
    }

    private void SpawnPossessableMonster(int spawnIndex)
    {
        HWJ_RootObjectDataSO rootData = GetPossessableMonsterRootObject(spawnIndex);
        GameObject prefab = GetPossessableMonsterPrefab(spawnIndex, rootData);

        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = ResolvePattern1SummonPosition(spawnIndex);
        GameObject instance = HWJ_GameAccess.HasManager
            ? HWJ_GameAccess.Spawn(prefab, spawnPosition, Quaternion.identity)
            : Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        EnsureSummonedMonsterRuntimeComponents(instance, rootData, spawnIndex);

        HWJ_PossessionBodyState bodyState = instance.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null)
        {
            bodyState.ResetConsumed();
        }

        HWJ_RuntimeStatusSystem spawnedStatus = instance.GetComponent<HWJ_RuntimeStatusSystem>();

        if (spawnedStatus != null)
        {
            spawnedStatus.RefreshCurrentHpFromData(true);
        }

        AssignSummonedMonsterSaveIdentity(instance, resolver, rootData, spawnIndex);

        HWJ_MonsterAISystem monsterAI = instance.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.RefreshData();
            monsterAI.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyNavigationSystem navigation = instance.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyAttackSystem attackSystem = instance.GetComponent<HWJ_EnemyAttackSystem>();

        if (attackSystem != null)
        {
            attackSystem.SetTarget(FindPlayerTarget());
        }
    }

    private GameObject GetPossessableMonsterPrefab(int index, HWJ_RootObjectDataSO rootData)
    {
        if (possessableMonsterPrefabs != null && possessableMonsterPrefabs.Length > 0)
        {
            int safeIndex = Mathf.Abs(index) % possessableMonsterPrefabs.Length;
            GameObject indexedPrefab = possessableMonsterPrefabs[safeIndex];

            if (indexedPrefab != null)
            {
                return indexedPrefab;
            }
        }

        if (summonMonsterPrefab != null)
        {
            return summonMonsterPrefab;
        }

        return rootData != null && rootData.Model != null
            ? rootData.Model.modelPrefab
            : null;
    }

    private Vector3 ResolvePattern1SummonPosition(int spawnIndex)
    {
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += pattern1SummonStepOffset.x * Mathf.Max(0, spawnIndex);
        spawnPosition.y += pattern1SummonStepOffset.y * Mathf.Max(0, spawnIndex);
        return spawnPosition;
    }

    private HWJ_RootObjectDataSO GetPossessableMonsterRootObject(int index)
    {
        if (possessableMonsterRootObjects == null || possessableMonsterRootObjects.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Abs(index) % possessableMonsterRootObjects.Length;
        return possessableMonsterRootObjects[safeIndex];
    }

    private void EnsureSummonedMonsterRuntimeComponents(
        GameObject instance,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = EnsureComponent<HWJ_RootObjectDataResolver>(instance);

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        Rigidbody2D summonedBody = EnsureComponent<Rigidbody2D>(instance);

        if (summonedBody != null)
        {
            summonedBody.freezeRotation = true;
            summonedBody.gravityScale = Mathf.Approximately(summonedBody.gravityScale, 0f)
                ? 1f
                : summonedBody.gravityScale;
        }

        if (instance.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D boxCollider = instance.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 1f);
        }

        EnsureComponent<HWJ_PossessionBodyState>(instance);
        EnsureComponent<HWJ_KnockbackSystem>(instance);
        EnsureComponent<HWJ_RuntimeStatusSystem>(instance);
        EnsureComponent<HWJ_CombatSystem>(instance);
        EnsureComponent<HWJ_CombatExecutionSystem>(instance);
        EnsureComponent<HWJ_SkillActionSystem>(instance);
        EnsureComponent<HWJ_CharacterMotionSystem>(instance);
        EnsureComponent<HWJ_EnemyAttackSystem>(instance);
        EnsureComponent<HWJ_EnemyNavigationSystem>(instance);
        EnsureComponent<HWJ_MonsterAISystem>(instance);
    }

    private void AssignSummonedMonsterSaveIdentity(
        GameObject instance,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        string rootObjectId = ResolveRootObjectId(rootData, resolver);

        if (string.IsNullOrEmpty(rootObjectId))
        {
            return;
        }

        HWJ_RuntimeSaveIdentity saveIdentity = EnsureComponent<HWJ_RuntimeSaveIdentity>(instance);

        if (saveIdentity == null || saveIdentity.HasStableInstanceId)
        {
            return;
        }

        string bossId = ResolveRootObjectId(
            null,
            bossBrain != null ? bossBrain.GetComponent<HWJ_RootObjectDataResolver>() : GetComponent<HWJ_RootObjectDataResolver>());
        string sourceId = string.IsNullOrEmpty(bossId) ? pattern1SummonSaveIdPrefix : bossId + "_" + pattern1SummonSaveIdPrefix;
        string stableId = $"{sourceId}_{summonedMonsterSequence:000}_{Mathf.Max(0, spawnIndex):00}";
        summonedMonsterSequence++;
        saveIdentity.SetManualIdentity(stableId, rootObjectId);
    }

    private static string ResolveRootObjectId(
        HWJ_RootObjectDataSO rootData,
        HWJ_RootObjectDataResolver resolver)
    {
        if (rootData != null && rootData.Identity != null && !string.IsNullOrEmpty(rootData.Identity.objectId))
        {
            return rootData.Identity.objectId;
        }

        return resolver != null && resolver.Identity != null
            ? resolver.Identity.objectId
            : null;
    }

    private static T EnsureComponent<T>(GameObject instance) where T : Component
    {
        if (instance == null)
        {
            return null;
        }

        T component = instance.GetComponent<T>();
        return component != null ? component : instance.AddComponent<T>();
    }

    private IEnumerator DashToTargetSide(Transform target, float stopOffset)
    {
        if (target == null)
        {
            yield break;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 destination = target.position - Vector3.right * direction * Mathf.Max(0f, stopOffset);
        destination.y = transform.position.y;
        destination.z = transform.position.z;
        yield return DashToPosition(destination, dashSeconds);
    }

    private IEnumerator DashToPosition(Vector3 destination, float seconds)
    {
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.01f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetPosition(Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetPosition(destination);
        StopMovement();
    }

    private Vector3 ResolveFarEdgePosition(Transform target, float padding)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float leftX = roomCenter.x - roomSize.x * 0.5f + Mathf.Max(0f, padding);
        float rightX = roomCenter.x + roomSize.x * 0.5f - Mathf.Max(0f, padding);
        float targetX = target != null ? target.position.x : roomCenter.x;
        float destinationX = Mathf.Abs(targetX - leftX) > Mathf.Abs(targetX - rightX) ? leftX : rightX;
        return new Vector3(destinationX, transform.position.y, transform.position.z);
    }

    private Vector3 ResolveBehindTargetPosition(Transform target)
    {
        if (target == null)
        {
            return transform.position;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 position = target.position + Vector3.right * direction * Mathf.Max(0f, pattern6BehindOffset);
        position.y = transform.position.y;
        position.z = transform.position.z;
        return position;
    }

    private void ShowForwardSlashWarning(Transform target, float range, float seconds)
    {
        float direction = GetDirectionToTarget(target);
        HWJ_SkillWarningIndicator.ShowForwardArc(
            transform.position,
            direction,
            Mathf.Max(0.1f, range),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowCircleWarning(Vector3 center, float radius, float seconds)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            center,
            Mathf.Max(0.1f, radius),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowRoomWideHorizontalWarning(float height, float seconds)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        HWJ_SkillWarningIndicator.ShowRectangle(
            new Vector3(roomCenter.x, transform.position.y, transform.position.z),
            new Vector2(Mathf.Max(1f, roomSize.x), Mathf.Max(0.1f, height)),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowHalfRoomShockwaveWarning(float widthRatio, float height, float seconds)
    {
        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height);
        HWJ_SkillWarningIndicator.ShowRectangle(
            rect.center,
            rect.size,
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void DamageTargetIfInsideForwardRange(Transform target, float range, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 delta = target.position - transform.position;

        if (Mathf.Sign(delta.x) != Mathf.Sign(direction)
            || Mathf.Abs(delta.x) > Mathf.Max(0.1f, range)
            || Mathf.Abs(delta.y) > Mathf.Max(0.1f, verticalSlashRadius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideCircle(Transform target, Vector3 center, float radius, float damageMultiplier)
    {
        if (target == null || Vector2.Distance(target.position, center) > Mathf.Max(0.1f, radius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideRoomHorizontalBand(Transform target, float height, float damageMultiplier)
    {
        if (target == null || Mathf.Abs(target.position.y - transform.position.y) > Mathf.Max(0.1f, height) * 0.5f)
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideHalfRoomShockwave(Transform target, float widthRatio, float height, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height);

        if (!rect.Contains((Vector2)target.position))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetByFixedMaxHpRatio(Transform target, float damageRatio)
    {
        HWJ_RuntimeStatusSystem targetStatus = target != null
            ? target.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;

        if (targetStatus == null || targetStatus.IsDead || targetStatus.MaxHp <= 0f)
        {
            return;
        }

        targetStatus.ApplyDamage(targetStatus.MaxHp * Mathf.Clamp01(damageRatio), combatSystem, null);
    }

    private void DamageTarget(Transform target, float damageMultiplier)
    {
        HWJ_RootObjectDataResolver targetResolver = target != null
            ? target.GetComponentInParent<HWJ_RootObjectDataResolver>()
            : null;

        combatSystem?.TryDealDamageTo(targetResolver, damageMultiplier, out _);
    }

    private Rect ResolveHalfRoomShockwaveRect(float widthRatio, float height)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float direction = Mathf.Sign(GetDirectionToTarget(bossBrain != null ? bossBrain.Target : null));
        float width = Mathf.Max(1f, roomSize.x * Mathf.Clamp01(widthRatio));
        float centerX = transform.position.x + direction * width * 0.5f;
        return new Rect(
            centerX - width * 0.5f,
            transform.position.y - Mathf.Max(0.1f, height) * 0.5f,
            width,
            Mathf.Max(0.1f, height));
    }

    private bool IsInsideBossRoom(Vector3 position)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        Vector2 halfSize = roomSize * 0.5f;
        return Mathf.Abs(position.x - roomCenter.x) <= halfSize.x
            && Mathf.Abs(position.y - roomCenter.y) <= halfSize.y;
    }

    private Vector2 GetRoomCenter()
    {
        return bossBrain != null ? bossBrain.BossRoomCenter : (Vector2)transform.position;
    }

    private Vector2 GetRoomSize()
    {
        return bossBrain != null ? bossBrain.BossRoomSize : new Vector2(28f, 14f);
    }

    private float GetDirectionToTarget(Transform target)
    {
        if (target == null)
        {
            return transform.localScale.x < 0f ? -1f : 1f;
        }

        float deltaX = target.position.x - transform.position.x;
        return Mathf.Abs(deltaX) <= 0.001f ? 1f : Mathf.Sign(deltaX);
    }

    private Transform FindPlayerTarget()
    {
        if (bossBrain != null && bossBrain.Target != null)
        {
            return bossBrain.Target;
        }

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

    private void SetPosition(Vector3 position)
    {
        if (body != null)
        {
            body.position = (Vector2)position;
            return;
        }

        transform.position = position;
    }

    private void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = visible;
            }
        }
    }

    private void CacheReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }
}
