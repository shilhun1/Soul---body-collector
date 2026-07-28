using UnityEngine;

/// <summary>
/// 플레이어, 적, NPC, 보스가 공통으로 사용하는 최상위 데이터 에셋입니다.
/// 각 오브젝트 프리팹의 HWJ_RootObjectDataResolver에 할당해서 공통 데이터와 선택된 유형 데이터를 함께 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_RootObjectData", menuName = "HWJ/Data/Root Object Data")]
public class HWJ_RootObjectDataSO : ScriptableObject
{
    [Header("공통 식별 정보")]
    [Tooltip("오브젝트 ID, 표시 이름, 진영, 기본 무기를 설정합니다.")]
    [InspectorName("식별 정보")]
    [SerializeField] private HWJ_IdentityData identity = new HWJ_IdentityData();

    [Header("공통 능력치")]
    [Tooltip("플레이어, 적, NPC, 보스가 공통으로 읽는 기본 능력치입니다.")]
    [InspectorName("상태/능력치")]
    [SerializeField] private HWJ_StatusData status = new HWJ_StatusData();
    [Tooltip("모델, 애니메이터, 이펙트 위치 같은 표현 데이터를 설정합니다.")]
    [InspectorName("모델/표현")]
    [SerializeField] private HWJ_ModelData model = new HWJ_ModelData();
    [Tooltip("이 오브젝트가 공격할 때 사용하는 기본 데미지 데이터입니다.")]
    [InspectorName("주는 데미지")]
    [SerializeField] private HWJ_DamageData damage = new HWJ_DamageData();
    [Tooltip("이 오브젝트가 피해를 받을 때 사용하는 방어, 무적, 피격 반응 데이터입니다.")]
    [InspectorName("받는 데미지")]
    [SerializeField] private HWJ_ReceivedDamageData receivedDamage = new HWJ_ReceivedDamageData();
    [Tooltip("상호작용 가능 여부와 범위를 설정합니다.")]
    [InspectorName("상호작용")]
    [SerializeField] private HWJ_InteractionData interaction = new HWJ_InteractionData();
    [Tooltip("처치 또는 완료 시 지급할 보상 데이터입니다.")]
    [InspectorName("보상")]
    [SerializeField] private HWJ_RewardData reward = new HWJ_RewardData();

    [Header("유형별 추가 데이터")]
    [Tooltip("플레이어/적/NPC/보스 전용 데이터 SO를 연결합니다. 이 값에 따라 전용 능력, AI, 빙의 데이터가 추가됩니다.")]
    [InspectorName("선택된 유형 데이터")]
    [SerializeField] private HWJ_ObjectTypeDataSO selectedTypeData;

    public HWJ_IdentityData Identity => identity;
    public HWJ_StatusData Status => status;
    public HWJ_ModelData Model => model;
    public HWJ_DamageData Damage => damage;
    public HWJ_ReceivedDamageData ReceivedDamage => receivedDamage;
    public HWJ_InteractionData Interaction => interaction;
    public HWJ_RewardData Reward => reward;
    public HWJ_ObjectTypeDataSO SelectedTypeData => selectedTypeData;
    public HWJ_AbilityTag[] AbilityTags => identity != null ? identity.abilityTags : null;

    /// <summary>
    /// 선택된 TypeData가 있으면 그 타입을 우선 사용하고, 없으면 Identity의 타입을 사용합니다.
    /// 시스템에서 이 오브젝트가 플레이어/적/NPC/보스 중 무엇인지 빠르게 확인할 때 씁니다.
    /// </summary>
    public HWJ_ObjectType ObjectType => selectedTypeData != null ? selectedTypeData.ObjectType : identity.objectType;

    /// <summary>
    /// 선택된 TypeData의 기본 무기가 있으면 우선 사용하고, 없으면 Identity의 무기를 사용합니다.
    /// 빙의 후 무기별 스킬이나 적 역할을 판단할 때 씁니다.
    /// </summary>
    public HWJ_WeaponType WeaponType => GetWeaponType();

    /// <summary>
    /// Returns true when this root object grants a specific map gimmick ability.
    /// This keeps stage gimmicks connected to shared SO data instead of scene-only rules.
    /// </summary>
    public bool HasAbilityTag(HWJ_AbilityTag abilityTag)
    {
        if (abilityTag == HWJ_AbilityTag.None || identity == null || identity.abilityTags == null)
        {
            return false;
        }

        for (int i = 0; i < identity.abilityTags.Length; i++)
        {
            if (identity.abilityTags[i] == abilityTag)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 선택된 유형 데이터를 원하는 타입으로 안전하게 꺼냅니다.
    /// 예: 플레이어 시스템은 HWJ_PlayerTypeDataSO, 적 AI는 HWJ_EnemyTypeDataSO로 요청합니다.
    /// </summary>
    public bool TryGetTypeData<T>(out T typedData) where T : HWJ_ObjectTypeDataSO
    {
        typedData = selectedTypeData as T;
        return typedData != null;
    }

    private HWJ_WeaponType GetWeaponType()
    {
        if (selectedTypeData is HWJ_EnemyTypeDataSO enemyData
            && enemyData.Role != null
            && enemyData.Role.weaponType != HWJ_WeaponType.None)
        {
            return enemyData.Role.weaponType;
        }

        if (selectedTypeData != null && selectedTypeData.DefaultWeaponType != HWJ_WeaponType.None)
        {
            return selectedTypeData.DefaultWeaponType;
        }

        return identity.weaponType;
    }
}
