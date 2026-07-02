using UnityEngine;

/// <summary>
/// GameObject와 RootObjectDataSO를 연결하는 런타임 접근 컴포넌트입니다.
/// 플레이어, 적, NPC, 보스 프리팹에 붙이고 시스템 스크립트들이 이 컴포넌트를 통해 데이터를 읽습니다.
/// </summary>
public class HWJ_RootObjectDataResolver : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataSO rootObjectData;

    public HWJ_RootObjectDataSO RootObjectData => rootObjectData;
    public HWJ_IdentityData Identity => rootObjectData != null ? rootObjectData.Identity : null;
    public HWJ_StatusData Status => rootObjectData != null ? rootObjectData.Status : null;
    public HWJ_ModelData Model => rootObjectData != null ? rootObjectData.Model : null;
    public HWJ_DamageData Damage => rootObjectData != null ? rootObjectData.Damage : null;
    public HWJ_ReceivedDamageData ReceivedDamage => rootObjectData != null ? rootObjectData.ReceivedDamage : null;
    public HWJ_InteractionData Interaction => rootObjectData != null ? rootObjectData.Interaction : null;
    public HWJ_RewardData Reward => rootObjectData != null ? rootObjectData.Reward : null;
    public HWJ_ObjectTypeDataSO TypeData => rootObjectData != null ? rootObjectData.SelectedTypeData : null;
    public HWJ_ObjectType ObjectType => rootObjectData != null ? rootObjectData.ObjectType : HWJ_ObjectType.Player;
    public HWJ_WeaponType WeaponType => GetWeaponType();

    /// <summary>
    /// 런타임에 생성된 오브젝트에 RootObjectData를 주입합니다.
    /// SpawnerSystem이 프리팹을 생성한 뒤 어떤 데이터로 동작해야 하는지 연결할 때 사용합니다.
    /// </summary>
    public void SetRootObjectData(HWJ_RootObjectDataSO rootObjectData)
    {
        this.rootObjectData = rootObjectData;
    }

    /// <summary>
    /// 이 오브젝트에 연결된 TypeData를 필요한 유형으로 꺼냅니다.
    /// 시스템에서 캐스팅 실패를 직접 처리하지 않도록 bool 결과로 성공 여부를 알려줍니다.
    /// </summary>
    public bool TryGetTypeData<T>(out T typedData) where T : HWJ_ObjectTypeDataSO
    {
        if (rootObjectData == null)
        {
            typedData = null;
            return false;
        }

        return rootObjectData.TryGetTypeData(out typedData);
    }

    /// <summary>
    /// 현재 오브젝트가 특정 유형인지 확인합니다.
    /// 충돌, 상호작용, 타겟팅 로직에서 타입 필터로 사용합니다.
    /// </summary>
    public bool IsObjectType(HWJ_ObjectType objectType)
    {
        return rootObjectData != null && rootObjectData.ObjectType == objectType;
    }

    private HWJ_WeaponType GetWeaponType()
    {
        if (TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.Role != null
            && enemyData.Role.weaponType != HWJ_WeaponType.None)
        {
            return enemyData.Role.weaponType;
        }

        return rootObjectData != null ? rootObjectData.WeaponType : HWJ_WeaponType.None;
    }
}
