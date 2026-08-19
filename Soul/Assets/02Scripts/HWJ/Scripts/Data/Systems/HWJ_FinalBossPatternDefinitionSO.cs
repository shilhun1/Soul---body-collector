using UnityEngine;

public enum HWJ_FinalBossPatternKind
{
    Phase1BlackOrbVolley = 1,
    Phase1SpearLine = 2,
    Phase1Barrier = 3,
    Phase1PortalSummon = 4,
    Phase1FloorFireLightning = 5,
    Phase2WeaponBarrage = 6,
    Phase2BarrierOrbVolley = 7,
    Phase2BlackFlameCharge = 8,
    Phase2DoublePortalSummon = 9,
    Phase2EightWayLightning = 10
}

/// <summary>
/// 최종보스 패턴의 고정 설정을 보관합니다.
/// 런타임 진행 시간, 현재 대상, 생성된 공격체는 이 원본 SO에 기록하지 않습니다.
/// </summary>
[CreateAssetMenu(
    fileName = "HWJ_FinalBossPatternDefinition",
    menuName = "HWJ/Data/System/Final Boss Pattern Definition")]
public sealed class HWJ_FinalBossPatternDefinitionSO : ScriptableObject
{
    [Header("패턴 식별")]
    [Tooltip("HWJ_BossPatternDataSO의 패턴 ID와 일치해야 합니다.")]
    [SerializeField] private string patternId;

    [Tooltip("실행할 최종보스 패턴 종류입니다.")]
    [SerializeField] private HWJ_FinalBossPatternKind patternKind;

    [Header("손짓 및 시간")]
    [Tooltip("Animator에서 재생할 손짓 Trigger 이름입니다.")]
    [SerializeField] private string gestureTrigger;

    [Tooltip("실제 공격 판정이 시작되기 전 예고 시간입니다.")]
    [SerializeField, Min(0f)] private float preparationSeconds = 1f;

    [Tooltip("애니메이션 이벤트가 오지 않을 때 이 시간이 지나면 공격을 강제로 시작합니다.")]
    [SerializeField, Min(0.01f)] private float animationEventTimeoutSeconds = 2f;

    [Tooltip("켜면 애니메이션 이벤트를 우선 사용하고, 이벤트가 없으면 제한 시간 뒤 실행합니다.")]
    [SerializeField] private bool preferAnimationCastEvent = true;

    [Tooltip("패턴 판정 종료 후 다음 행동까지의 후딜입니다.")]
    [SerializeField, Min(0f)] private float recoverySeconds = 1f;

    [Header("공격 판정")]
    [Tooltip("보스 공통 공격력에 곱하는 배율입니다.")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    [Tooltip("현재 빙의 정신력 최대치에서 차감할 비율입니다. 0.1은 10%입니다.")]
    [SerializeField, Range(0f, 1f)] private float possessionMentalDamageRatio;

    [Tooltip("공격 적중 시 플레이어의 이동, 공격, 대쉬를 막는 시간입니다.")]
    [SerializeField, Min(0f)] private float stunSeconds;

    [Tooltip("장판이 같은 대상을 다시 공격할 수 있는 최소 간격입니다.")]
    [SerializeField, Min(0.01f)] private float repeatHitIntervalSeconds = 0.5f;

    [Header("투사체")]
    [Tooltip("한 패턴에서 생성하는 투사체 또는 순차 공격 개수입니다.")]
    [SerializeField, Min(1)] private int projectileCount = 1;

    [Tooltip("순차 투사체 사이의 발사 간격입니다.")]
    [SerializeField, Min(0f)] private float projectileIntervalSeconds = 0.3f;

    [Tooltip("투사체 이동 속도입니다.")]
    [SerializeField, Min(0f)] private float projectileSpeed = 10f;

    [Tooltip("투사체가 이동할 수 있는 최대 거리입니다.")]
    [SerializeField, Min(0.1f)] private float projectileMaximumDistance = 30f;

    [Tooltip("투사체 충돌 판정 크기입니다.")]
    [SerializeField] private Vector2 projectileSize = new Vector2(1f, 0.4f);

    [Tooltip("아트가 준비되면 사용할 투사체 프리팹입니다. 비어 있으면 런타임 표시용 도형을 사용합니다.")]
    [SerializeField] private GameObject projectileVisualPrefab;

    [Tooltip("한 패턴에서 두 번째 종류의 투사체를 사용할 때 연결합니다. 2-1에서는 창에 사용합니다.")]
    [SerializeField] private GameObject secondaryProjectileVisualPrefab;

    [Tooltip("한 패턴에서 세 번째 종류의 투사체를 사용할 때 연결합니다. 2-1에서는 도끼에 사용합니다.")]
    [SerializeField] private GameObject tertiaryProjectileVisualPrefab;

    [Header("장판 및 낙뢰")]
    [Tooltip("창 경로, 바닥 불, 낙뢰 판정이 유지되는 시간입니다.")]
    [SerializeField, Min(0f)] private float hazardDurationSeconds = 3f;

    [Tooltip("장판 또는 직선 공격 한 개의 판정 크기입니다.")]
    [SerializeField] private Vector2 hazardSize = new Vector2(8f, 1f);

    [Tooltip("8방향 낙뢰처럼 방사형으로 생성할 공격 수입니다.")]
    [SerializeField, Min(1)] private int radialAttackCount = 8;

    [Tooltip("아트가 준비되면 사용할 예고 표시 프리팹입니다.")]
    [SerializeField] private GameObject telegraphVisualPrefab;

    [Tooltip("창 경로, 바닥 불, 충격파처럼 유지되는 첫 번째 판정의 시각 프리팹입니다.")]
    [SerializeField] private GameObject hazardVisualPrefab;

    [Tooltip("한 패턴에 서로 다른 두 번째 장판 표현이 필요할 때 연결합니다. 1-5에서는 낙뢰에 사용합니다.")]
    [SerializeField] private GameObject secondaryHazardVisualPrefab;

    [Header("방어막")]
    [Tooltip("방어막 체력을 보스 최대 체력의 비율로 계산합니다.")]
    [SerializeField, Range(0f, 1f)] private float barrierHpRatio = 0.05f;

    [Tooltip("방어막이 자동으로 사라지는 시간입니다. 0이면 파괴될 때까지 유지됩니다.")]
    [SerializeField, Min(0f)] private float barrierDurationSeconds;

    [Tooltip("아트가 준비되면 사용할 방어막 프리팹입니다.")]
    [SerializeField] private GameObject barrierVisualPrefab;

    [Header("포탈 소환")]
    [Tooltip("생성할 붉은 포탈 수입니다. 2페이즈 패턴은 2개입니다.")]
    [SerializeField, Min(1)] private int portalCount = 1;

    [Tooltip("각 포탈에서 생성할 몬스터 수입니다. 2페이즈는 포탈당 4마리, 총 8마리입니다.")]
    [SerializeField, Min(1)] private int monstersPerPortal = 3;

    [Tooltip("같은 포탈에서 몬스터가 연속 생성되는 간격입니다.")]
    [SerializeField, Min(0f)] private float summonIntervalSeconds = 0.35f;

    [Tooltip("소환 위치가 겹치지 않도록 적용하는 간격입니다.")]
    [SerializeField] private Vector2 summonStepOffset = new Vector2(0.45f, 0f);

    [Tooltip("소환할 빙의 가능 몬스터 프리팹 목록입니다.")]
    [SerializeField] private GameObject[] summonMonsterPrefabs;

    [Tooltip("프리팹 대신 모델 프리팹을 찾을 빙의 가능 몬스터 RootObjectData 목록입니다.")]
    [SerializeField] private HWJ_RootObjectDataSO[] summonMonsterRootObjects;

    [Tooltip("아트가 준비되면 사용할 붉은 포탈 프리팹입니다.")]
    [SerializeField] private GameObject portalVisualPrefab;

    [Header("2페이즈 실제 돌진")]
    [Tooltip("검은 불꽃 돌진 속도입니다.")]
    [SerializeField, Min(0.1f)] private float chargeSpeed = 18f;

    [Tooltip("돌진이 추적할 수 있는 최대 거리입니다.")]
    [SerializeField, Min(0.1f)] private float chargeMaximumDistance = 24f;

    [Tooltip("돌진 종료 후 고정 위치로 복귀하는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float returnToAnchorSeconds = 0.35f;

    public string PatternId => patternId;
    public HWJ_FinalBossPatternKind PatternKind => patternKind;
    public string GestureTrigger => gestureTrigger;
    public float PreparationSeconds => Mathf.Max(0f, preparationSeconds);
    public float AnimationEventTimeoutSeconds => Mathf.Max(0.01f, animationEventTimeoutSeconds);
    public bool PreferAnimationCastEvent => preferAnimationCastEvent;
    public float RecoverySeconds => Mathf.Max(0f, recoverySeconds);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float PossessionMentalDamageRatio => Mathf.Clamp01(possessionMentalDamageRatio);
    public float StunSeconds => Mathf.Max(0f, stunSeconds);
    public float RepeatHitIntervalSeconds => Mathf.Max(0.01f, repeatHitIntervalSeconds);
    public int ProjectileCount => Mathf.Max(1, projectileCount);
    public float ProjectileIntervalSeconds => Mathf.Max(0f, projectileIntervalSeconds);
    public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
    public float ProjectileMaximumDistance => Mathf.Max(0.1f, projectileMaximumDistance);
    public Vector2 ProjectileSize => new Vector2(Mathf.Max(0.05f, projectileSize.x), Mathf.Max(0.05f, projectileSize.y));
    public GameObject ProjectileVisualPrefab => projectileVisualPrefab;
    public GameObject SecondaryProjectileVisualPrefab => secondaryProjectileVisualPrefab;
    public GameObject TertiaryProjectileVisualPrefab => tertiaryProjectileVisualPrefab;
    public float HazardDurationSeconds => Mathf.Max(0f, hazardDurationSeconds);
    public Vector2 HazardSize => new Vector2(Mathf.Max(0.05f, hazardSize.x), Mathf.Max(0.05f, hazardSize.y));
    public int RadialAttackCount => Mathf.Max(1, radialAttackCount);
    public GameObject TelegraphVisualPrefab => telegraphVisualPrefab;
    public GameObject HazardVisualPrefab => hazardVisualPrefab;
    public GameObject SecondaryHazardVisualPrefab => secondaryHazardVisualPrefab;
    public float BarrierHpRatio => Mathf.Clamp01(barrierHpRatio);
    public float BarrierDurationSeconds => Mathf.Max(0f, barrierDurationSeconds);
    public GameObject BarrierVisualPrefab => barrierVisualPrefab;
    public int PortalCount => Mathf.Max(1, portalCount);
    public int MonstersPerPortal => Mathf.Max(1, monstersPerPortal);
    public float SummonIntervalSeconds => Mathf.Max(0f, summonIntervalSeconds);
    public Vector2 SummonStepOffset => summonStepOffset;
    public GameObject[] SummonMonsterPrefabs => summonMonsterPrefabs;
    public HWJ_RootObjectDataSO[] SummonMonsterRootObjects => summonMonsterRootObjects;
    public GameObject PortalVisualPrefab => portalVisualPrefab;
    public float ChargeSpeed => Mathf.Max(0.1f, chargeSpeed);
    public float ChargeMaximumDistance => Mathf.Max(0.1f, chargeMaximumDistance);
    public float ReturnToAnchorSeconds => Mathf.Max(0.01f, returnToAnchorSeconds);

    private void OnValidate()
    {
        patternId = patternId != null ? patternId.Trim() : string.Empty;
        preparationSeconds = Mathf.Max(0f, preparationSeconds);
        animationEventTimeoutSeconds = Mathf.Max(0.01f, animationEventTimeoutSeconds);
        recoverySeconds = Mathf.Max(0f, recoverySeconds);
        projectileCount = Mathf.Max(1, projectileCount);
        portalCount = Mathf.Max(1, portalCount);
        monstersPerPortal = Mathf.Max(1, monstersPerPortal);
        radialAttackCount = Mathf.Max(1, radialAttackCount);
    }
}
