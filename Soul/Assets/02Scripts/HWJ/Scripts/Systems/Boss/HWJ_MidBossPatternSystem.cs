using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 물리형 중간보스가 사용하는 특수 패턴 실행 시스템입니다.
/// 애니메이션과 이펙트가 없어도 이동, 예고 표시, 소환, 데미지 판정, 그로기 흐름을 먼저 테스트할 수 있습니다.
/// </summary>
public partial class HWJ_MidBossPatternSystem : MonoBehaviour, HWJ_IBossSpecialPatternExecutor
{
    [Header("패턴 1 - 기본 소환 프리팹")]
    [Tooltip("패턴 1에서 사용할 기본 소환 몬스터 프리팹입니다. 아래 소환 몬스터 프리팹 목록이 비어 있거나 해당 칸이 비어 있으면 이 프리팹을 사용합니다.")]
    [InspectorName("기본 소환 몬스터 프리팹")]
    [FormerlySerializedAs("summonMonsterPerfabs")]
    [SerializeField] private GameObject summonMonsterPrefab;

    [Header("참조")]
    [Tooltip("보스의 상태 머신과 보스방 정보를 관리하는 시스템입니다.")]
    [InspectorName("보스 상태 시스템")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;

    [Tooltip("보스의 현재 체력, 사망 여부, 방어력 같은 런타임 상태를 관리합니다.")]
    [InspectorName("런타임 상태 시스템")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;

    [Tooltip("보스가 플레이어에게 데미지를 줄 때 사용하는 공통 전투 계산 시스템입니다.")]
    [InspectorName("전투 시스템")]
    [SerializeField] private HWJ_CombatSystem combatSystem;

    [Tooltip("보스 패턴 중 이동 잠금, 대쉬 취소 같은 조작 제한을 처리합니다.")]
    [InspectorName("스킬 실행 시스템")]
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;

    [Tooltip("보스 이동과 정지 처리에 사용하는 Rigidbody2D입니다.")]
    [InspectorName("보스 Rigidbody2D")]
    [SerializeField] private Rigidbody2D body;

    [Tooltip("대기, 은신 같은 연출에서 보스 스프라이트 표시 여부를 제어합니다.")]
    [InspectorName("보스 스프라이트 목록")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("패턴 실행 키")]
    [Tooltip("BossPatternData의 Custom Executor Key와 같은 값일 때 이 시스템이 해당 패턴을 실행합니다.")]
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

    [Tooltip("세로 베기와 원형 충격 판정의 반지름입니다.")]
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
    [Tooltip("보스 체력이 이 비율만큼 깎일 때마다 패턴 1 사용 기회를 1개 얻습니다. 0.15는 15%입니다.")]
    [InspectorName("체력 감소 사용 간격")]
    [SerializeField] private float pattern1HpLossIntervalRatio = 0.15f;

    [Tooltip("패턴 1에서 소환할 몬스터 수입니다.")]
    [InspectorName("소환 몬스터 수")]
    [SerializeField] private int pattern1SummonCount = 4;

    [Tooltip("소환 전에 보스가 호루라기 연출로 대기하는 시간입니다.")]
    [InspectorName("호루라기 대기 시간")]
    [SerializeField] private float pattern1WhistleSeconds = 2f;

    [Tooltip("몬스터를 한 마리 소환한 뒤 다음 몬스터가 나오기까지의 간격입니다.")]
    [InspectorName("소환 간격")]
    [SerializeField] private float pattern1SummonIntervalSeconds = 0.15f;

    [Tooltip("보스가 맵 끝으로 이동할 때 벽에서 떨어지는 여유 거리입니다.")]
    [InspectorName("맵 끝 여유 거리")]
    [SerializeField] private float pattern1EdgePadding = 1f;

    [Tooltip("소환 몬스터의 저장용 ID를 만들 때 사용하는 앞부분입니다.")]
    [InspectorName("소환 몬스터 저장 ID 접두어")]
    [SerializeField] private string pattern1SummonSaveIdPrefix = "mid_boss_summon";

    [Tooltip("보스 위치 기준으로 소환 위치를 조금씩 벌릴 때 사용하는 간격입니다. 0이면 모든 몬스터가 보스 위치에서 소환됩니다.")]
    [InspectorName("소환 위치 추가 간격")]
    [SerializeField] private Vector2 pattern1SummonStepOffset = Vector2.zero;

    [Tooltip("패턴 1에서 순서대로 사용할 몬스터 프리팹 목록입니다. 비어 있으면 기본 소환 프리팹 또는 RootObjectData의 모델 프리팹을 사용합니다.")]
    [InspectorName("소환 몬스터 프리팹 목록")]
    [SerializeField] private GameObject[] possessableMonsterPrefabs;

    [Tooltip("소환 몬스터에 적용할 RootObjectData 목록입니다. 프리팹에 데이터가 없어도 이 값으로 능력치와 빙의 데이터를 넣을 수 있습니다.")]
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
    [Tooltip("패턴 5 시작 시 검의 붉은 이펙트를 보여줄 예정인 대기 시간입니다.")]
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

    [Tooltip("기습 후 원래 위치로 돌아가기 전 대기하는 시간입니다.")]
    [InspectorName("복귀 전 대기 시간")]
    [SerializeField] private float pattern6ReturnDelaySeconds = 0.25f;

    [Header("패턴 7 - 점프 내려찍기")]
    [Tooltip("패턴 7에서 보스가 위로 올라가는 높이입니다.")]
    [InspectorName("점프 높이")]
    [SerializeField] private float pattern7JumpHeight = 4f;

    [Tooltip("공중에서 플레이어 위치를 조준하는 시간입니다.")]
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
    private bool isPatternPhysicsOverridden;
    private float cachedPatternGravityScale;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => activePatternRoutine != null;
    public bool IsPatternPhysicsOverridden => isPatternPhysicsOverridden;
    public float CurrentPatternGravityScale => body != null ? body.gravityScale : 0f;
    public float CachedPatternGravityScale => cachedPatternGravityScale;

    public int PendingPattern1Charges
    {
        get
        {
            RefreshPattern1Charges();
            return pendingPattern1Charges;
        }
    }

    public float NextPattern1HpRatioThreshold
    {
        get
        {
            RefreshPattern1Charges();
            return nextPattern1HpRatioThreshold;
        }
    }

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
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern1SummonRoutine(target));
                return true;
            case 2:
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern2DashDoubleSlashRoutine(target));
                return true;
            case 3:
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern3SlashAndFullWaveRoutine(target));
                return true;
            case 4:
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern4FixedDamageDashRoutine(target));
                return true;
            case 5:
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern5RedSwordComboRoutine(target));
                return true;
            case 6:
                BeginPatternPhysicsOverride();
                activePatternRoutine = StartCoroutine(Pattern6VanishBackstabRoutine(target));
                return true;
            case 7:
                BeginPatternPhysicsOverride();
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
        EndPatternPhysicsOverride();
    }

    private void FinishPatternRoutine()
    {
        EndPatternPhysicsOverride();
        activePatternRoutine = null;
    }
}
