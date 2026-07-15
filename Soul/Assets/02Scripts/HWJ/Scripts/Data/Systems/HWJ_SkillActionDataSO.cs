using UnityEngine;

/// <summary>
/// 실제 스킬이 어떤 행동을 할지 정의하는 ScriptableObject입니다.
/// SkillActionSystem이 스킬 ID로 이 데이터를 찾아 데미지, 범위, 이펙트, 쿨타임을 적용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_SkillActionData", menuName = "HWJ/Data/System/Skill Action")]
public class HWJ_SkillActionDataSO : ScriptableObject
{
    [SerializeField] private string skillActionId;
    [SerializeField] private HWJ_SkillActionType actionType;
    [SerializeField] private HWJ_WeaponType requiredWeaponType;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float range;
    [SerializeField] private float hitRange;
    [SerializeField] private float durationSeconds;
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private float hitStartSeconds = 0.08f;
    [SerializeField] private float hitActiveSeconds = 0.08f;
    [SerializeField] private float recoverySeconds = 0.18f;
    [SerializeField] private float movementLockSeconds = 0.18f;
    [SerializeField] private float dashCancelStartSeconds = 0.12f;
    [SerializeField] private bool canDashCancel = true;
    [SerializeField] private float moveDistance;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float hitIntervalSeconds;
    [SerializeField] private bool usesCustomBodyDecayAmount;
    [SerializeField] private float customBodyDecayAmount;
    [SerializeField] private float additionalBodyDecayAmount;
    [SerializeField] private bool usesComboBodyDecayMultiplier;
    [SerializeField] private float comboBodyDecayMultiplier = 1f;
    [SerializeField] private float chargeBodyDecayPerSecond;
    [SerializeField] private float maxChargeBodyDecayAmount;
    [SerializeField] private string motionKey;
    [SerializeField] private string[] motionSequenceKeys;
    [SerializeField] private float motionStepIntervalSeconds = 0.12f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject actionEffectPrefab;

    public string SkillActionId => skillActionId;
    public HWJ_SkillActionType ActionType => actionType;
    public HWJ_WeaponType RequiredWeaponType => requiredWeaponType;
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
