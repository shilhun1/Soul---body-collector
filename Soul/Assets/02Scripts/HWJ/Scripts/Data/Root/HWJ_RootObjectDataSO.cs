using UnityEngine;

/// <summary>
/// 플레이어, 적, NPC, 보스가 공통으로 사용하는 최상위 데이터 에셋입니다.
/// 각 오브젝트 프리팹의 HWJ_RootObjectDataResolver에 할당해서 공통 데이터와 선택된 유형 데이터를 함께 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_RootObjectData", menuName = "HWJ/Data/Root Object Data")]
public class HWJ_RootObjectDataSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private HWJ_IdentityData identity = new HWJ_IdentityData();

    [Header("Common Data")]
    [SerializeField] private HWJ_StatusData status = new HWJ_StatusData();
    [SerializeField] private HWJ_ModelData model = new HWJ_ModelData();
    [SerializeField] private HWJ_DamageData damage = new HWJ_DamageData();
    [SerializeField] private HWJ_ReceivedDamageData receivedDamage = new HWJ_ReceivedDamageData();
    [SerializeField] private HWJ_InteractionData interaction = new HWJ_InteractionData();
    [SerializeField] private HWJ_RewardData reward = new HWJ_RewardData();

    [Header("Selected Type")]
    [SerializeField] private HWJ_ObjectTypeDataSO selectedTypeData;

    public HWJ_IdentityData Identity => identity;
    public HWJ_StatusData Status => status;
    public HWJ_ModelData Model => model;
    public HWJ_DamageData Damage => damage;
    public HWJ_ReceivedDamageData ReceivedDamage => receivedDamage;
    public HWJ_InteractionData Interaction => interaction;
    public HWJ_RewardData Reward => reward;
    public HWJ_ObjectTypeDataSO SelectedTypeData => selectedTypeData;

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
