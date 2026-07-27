using UnityEngine;

/// <summary>
/// 실제 스킬이 어떤 행동을 할지 정의하는 ScriptableObject입니다.
/// SkillActionSystem이 스킬 ID로 이 데이터를 찾아 데미지, 범위, 이펙트, 쿨타임을 적용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_SkillActionData", menuName = "HWJ/Data/System/Skill Action")]
public class HWJ_SkillActionDataSO : ScriptableObject
{
    [Header("스킬 기본 정보")]
    [Tooltip("스킬 실행에 사용하는 고정 ID입니다. SkillSet의 skillId와 맞아야 합니다.")]
    [InspectorName("스킬 실행 ID")]
    [SerializeField] private string skillActionId;
    [Tooltip("원본 기획 시트나 외부 데이터의 스킬 ID입니다.")]
    [InspectorName("원본 스킬 ID")]
    [SerializeField] private string sourceSkillId;
    [Tooltip("인스펙터와 UI에서 볼 스킬 이름입니다.")]
    [InspectorName("표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("스킬 설명입니다.")]
    [InspectorName("스킬 설명")]
    [SerializeField] private string skillDescription;
    [Tooltip("이펙트 테이블을 사용할 때 연결할 이펙트 ID입니다.")]
    [InspectorName("이펙트 ID")]
    [SerializeField] private string effectId;
    [Tooltip("사운드 테이블을 사용할 때 연결할 사운드 ID입니다.")]
    [InspectorName("사운드 ID")]
    [SerializeField] private string soundId;

    [Header("실행 방식")]
    [Tooltip("근접, 투사체, 대쉬, 버프 등 실제 스킬 행동 타입입니다.")]
    [InspectorName("스킬 행동 타입")]
    [SerializeField] private HWJ_SkillActionType actionType;
    [Tooltip("이 무기일 때만 사용할 수 있습니다. None이면 제한 없음입니다.")]
    [InspectorName("필요 무기")]
    [SerializeField] private HWJ_WeaponType requiredWeaponType;
    [Tooltip("시트에서 입력한 기준 공격력입니다. 데미지 계산 기준으로 사용할 수 있습니다.")]
    [InspectorName("기획 공격력")]
    [SerializeField] private float authoredAttackPower;
    [Tooltip("최종 데미지에 곱할 배율입니다.")]
    [InspectorName("데미지 배율")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("스킬을 사용할 수 있는 사거리입니다.")]
    [InspectorName("사거리")]
    [SerializeField] private float range;
    [Tooltip("실제 데미지 판정 범위입니다.")]
    [InspectorName("피격 판정 범위")]
    [SerializeField] private float hitRange;
    [Tooltip("차징, 지속 장판, 투사체 준비 등에 사용할 지속 시간입니다.")]
    [InspectorName("지속 시간")]
    [SerializeField] private float durationSeconds;
    [Tooltip("같은 스킬을 다시 사용하기까지 기다리는 시간입니다.")]
    [InspectorName("쿨타임")]
    [SerializeField] private float cooldownSeconds;
    [Header("공격 판정 타이밍")]
    [Tooltip("스킬 시작 후 판정이 켜지는 시간입니다.")]
    [InspectorName("판정 시작 시간")]
    [SerializeField] private float hitStartSeconds = 0.08f;
    [Tooltip("판정이 유지되는 시간입니다.")]
    [InspectorName("판정 지속 시간")]
    [SerializeField] private float hitActiveSeconds = 0.08f;
    [Tooltip("스킬 사용 후 행동이 회복되기까지 걸리는 시간입니다.")]
    [InspectorName("후딜 시간")]
    [SerializeField] private float recoverySeconds = 0.18f;
    [Tooltip("스킬 사용 중 이동을 잠그는 시간입니다.")]
    [InspectorName("이동 잠금 시간")]
    [SerializeField] private float movementLockSeconds = 0.18f;
    [Tooltip("스킬 시작 후 대쉬 캔슬이 가능해지는 시간입니다.")]
    [InspectorName("대쉬 캔슬 가능 시점")]
    [SerializeField] private float dashCancelStartSeconds = 0.12f;
    [Tooltip("켜면 이 스킬은 대쉬 캔슬을 허용합니다.")]
    [InspectorName("대쉬 캔슬 가능")]
    [SerializeField] private bool canDashCancel = true;
    [Header("이동/다단히트")]
    [Tooltip("대쉬나 돌진 스킬에서 이동할 거리입니다.")]
    [InspectorName("이동 거리")]
    [SerializeField] private float moveDistance;
    [Tooltip("대쉬나 투사체가 이동하는 속도입니다.")]
    [InspectorName("이동 속도")]
    [SerializeField] private float moveSpeed;
    [Tooltip("한 번의 스킬에서 데미지 판정이 발생하는 횟수입니다.")]
    [InspectorName("히트 횟수")]
    [SerializeField] private int hitCount = 1;
    [Tooltip("다단히트일 때 히트 사이 간격입니다.")]
    [InspectorName("히트 간격")]
    [SerializeField] private float hitIntervalSeconds;

    [Header("피격 반응")]
    [Tooltip("켜면 대상에게 넉백을 줍니다.")]
    [InspectorName("넉백 발생")]
    [SerializeField] private bool causesKnockback;
    [Tooltip("넉백 거리 또는 힘으로 사용할 값입니다.")]
    [InspectorName("넉백 거리")]
    [SerializeField] private float knockbackDistance;
    [Tooltip("켜면 대상에게 경직을 줍니다.")]
    [InspectorName("경직 발생")]
    [SerializeField] private bool causesStagger;
    [Tooltip("경직이 유지되는 시간입니다.")]
    [InspectorName("경직 시간")]
    [SerializeField] private float staggerSeconds;
    [Tooltip("켜면 대상을 위로 띄웁니다.")]
    [InspectorName("띄우기 발생")]
    [SerializeField] private bool launchesTarget;
    [Tooltip("띄우기 높이 또는 힘입니다.")]
    [InspectorName("띄우기 높이")]
    [SerializeField] private float launchHeight;
    [Tooltip("켜면 스킬 사용자에게 무적을 부여합니다.")]
    [InspectorName("무적 부여")]
    [SerializeField] private bool grantsInvincibility;
    [Tooltip("스킬 사용자가 무적이 되는 시간입니다.")]
    [InspectorName("무적 시간")]
    [SerializeField] private float invincibilitySeconds;

    [Header("빙의체 정신력 소모")]
    [Tooltip("켜면 PlayerTypeData의 기본 스킬 정신력 소모량 대신 이 스킬 전용 정신력 소모량을 사용합니다.")]
    [InspectorName("전용 정신력 소모 사용")]
    [SerializeField] private bool usesCustomBodyDecayAmount;
    [Tooltip("전용으로 사용할 스킬 정신력 소모량입니다.")]
    [InspectorName("전용 정신력 소모량")]
    [SerializeField] private float customBodyDecayAmount;
    [Tooltip("기본 스킬 정신력 소모량에 추가로 더할 값입니다.")]
    [InspectorName("추가 정신력 소모량")]
    [SerializeField] private float additionalBodyDecayAmount;
    [Tooltip("켜면 콤보 단계에 따라 정신력 소모 배율을 적용합니다.")]
    [InspectorName("콤보 정신력 배율 사용")]
    [SerializeField] private bool usesComboBodyDecayMultiplier;
    [Tooltip("콤보 단계에 적용할 정신력 소모 배율입니다.")]
    [InspectorName("콤보 정신력 배율")]
    [SerializeField] private float comboBodyDecayMultiplier = 1f;
    [Tooltip("차징 시간 1초당 추가되는 정신력 소모량입니다.")]
    [InspectorName("차징 초당 정신력 소모량")]
    [SerializeField] private float chargeBodyDecayPerSecond;
    [Tooltip("차징으로 추가될 수 있는 최대 정신력 소모량입니다.")]
    [InspectorName("차징 최대 정신력 소모량")]
    [SerializeField] private float maxChargeBodyDecayAmount;

    [Header("모션과 에셋")]
    [Tooltip("모션 프로필에서 찾을 단일 모션 키입니다.")]
    [InspectorName("모션 키")]
    [SerializeField] private string motionKey;
    [Tooltip("연속 모션을 사용할 때 순서대로 재생할 모션 키 목록입니다.")]
    [InspectorName("모션 시퀀스 키")]
    [SerializeField] private string[] motionSequenceKeys;
    [Tooltip("모션 시퀀스의 각 단계 사이 간격입니다.")]
    [InspectorName("모션 단계 간격")]
    [SerializeField] private float motionStepIntervalSeconds = 0.12f;
    [Tooltip("투사체 스킬에서 발사할 프리팹입니다.")]
    [InspectorName("투사체 프리팹")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("스킬 사용 위치에 생성할 이펙트 프리팹입니다.")]
    [InspectorName("액션 이펙트 프리팹")]
    [SerializeField] private GameObject actionEffectPrefab;

    public string SkillActionId => skillActionId;
    public string SourceSkillId => sourceSkillId;
    public string DisplayName => displayName;
    public string SkillDescription => skillDescription;
    public string EffectId => effectId;
    public string SoundId => soundId;
    public HWJ_SkillActionType ActionType => actionType;
    public HWJ_WeaponType RequiredWeaponType => requiredWeaponType;
    public float AuthoredAttackPower => authoredAttackPower;
    public float DamageMultiplier => damageMultiplier;
    public float Range => range;
    public float HitRange => hitRange;
    public float DurationSeconds => durationSeconds;
    public float CooldownSeconds => cooldownSeconds;
    public float HitStartSeconds => hitStartSeconds;
    public float HitActiveSeconds => hitActiveSeconds;
    public float RecoverySeconds => recoverySeconds;
    public float MovementLockSeconds => movementLockSeconds;
    public float DashCancelStartSeconds => dashCancelStartSeconds;
    public bool CanDashCancel => canDashCancel;
    public float MoveDistance => moveDistance;
    public float MoveSpeed => moveSpeed;
    public int HitCount => hitCount;
    public float HitIntervalSeconds => hitIntervalSeconds;
    public bool CausesKnockback => causesKnockback;
    public float KnockbackDistance => knockbackDistance;
    public bool CausesStagger => causesStagger;
    public float StaggerSeconds => staggerSeconds;
    public bool LaunchesTarget => launchesTarget;
    public float LaunchHeight => launchHeight;
    public bool GrantsInvincibility => grantsInvincibility;
    public float InvincibilitySeconds => invincibilitySeconds;
    public bool UsesCustomBodyDecayAmount => usesCustomBodyDecayAmount;
    public float CustomBodyDecayAmount => customBodyDecayAmount;
    public float AdditionalBodyDecayAmount => additionalBodyDecayAmount;
    public bool UsesComboBodyDecayMultiplier => usesComboBodyDecayMultiplier;
    public float ComboBodyDecayMultiplier => comboBodyDecayMultiplier;
    public float ChargeBodyDecayPerSecond => chargeBodyDecayPerSecond;
    public float MaxChargeBodyDecayAmount => maxChargeBodyDecayAmount;
    public string MotionKey => motionKey;
    public string[] MotionSequenceKeys => motionSequenceKeys;
    public float MotionStepIntervalSeconds => motionStepIntervalSeconds;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject ActionEffectPrefab => actionEffectPrefab;
    public bool HasMotionSequence => motionSequenceKeys != null && motionSequenceKeys.Length > 0;

    /// <summary>
    /// 현재 무기로 이 스킬을 사용할 수 있는지 확인합니다.
    /// RequiredWeaponType이 None이면 모든 무기에서 사용 가능합니다.
    /// </summary>
    public bool CanUseWithWeapon(HWJ_WeaponType weaponType)
    {
        return requiredWeaponType == HWJ_WeaponType.None || requiredWeaponType == weaponType;
    }
}
