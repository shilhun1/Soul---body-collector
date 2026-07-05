using UnityEngine;

/// <summary>
/// 모든 유형 데이터 ScriptableObject의 공통 부모입니다.
/// Player, Enemy, NPC, Boss TypeData가 공통으로 타입 ID, 표시 이름, 기본 무기 정보를 가지게 합니다.
/// </summary>
public abstract class HWJ_ObjectTypeDataSO : ScriptableObject
{
    [Header("Type Identity")]
    [SerializeField] private HWJ_ObjectType objectType;
    [SerializeField] private string typeId;
    [SerializeField] private string displayName;
    [SerializeField] private HWJ_WeaponType defaultWeaponType;
    [TextArea]
    [SerializeField] private string description;

    public HWJ_ObjectType ObjectType => objectType;
    public string TypeId => typeId;
    public string DisplayName => displayName;
    public HWJ_WeaponType DefaultWeaponType => defaultWeaponType;
    public string Description => description;

    /// <summary>
    /// 이 TypeData가 요청한 오브젝트 유형인지 확인합니다.
    /// Resolver를 통해 받은 TypeData를 필터링할 때 사용합니다.
    /// </summary>
    public bool IsObjectType(HWJ_ObjectType targetType)
    {
        return objectType == targetType;
    }
}
