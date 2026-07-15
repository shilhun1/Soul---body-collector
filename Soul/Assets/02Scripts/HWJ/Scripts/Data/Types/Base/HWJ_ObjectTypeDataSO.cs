using UnityEngine;

/// <summary>
/// 모든 유형 데이터 ScriptableObject의 공통 부모입니다.
/// Player, Enemy, NPC, Boss TypeData가 공통으로 타입 ID, 표시 이름, 기본 무기 정보를 가지게 합니다.
/// </summary>
public abstract class HWJ_ObjectTypeDataSO : ScriptableObject
{
    [Header("유형 기본 정보")]
    [Tooltip("이 데이터가 플레이어, 적, NPC, 보스 중 어느 유형인지 정합니다.")]
    [InspectorName("오브젝트 유형")]
    [SerializeField] private HWJ_ObjectType objectType;
    [Tooltip("저장 데이터와 데이터베이스 조회에 쓰는 고정 ID입니다. 파일명을 바꿔도 이 값은 유지해야 합니다.")]
    [InspectorName("유형 ID")]
    [SerializeField] private string typeId;
    [Tooltip("기획자와 개발자가 구분하기 쉽게 보는 이름입니다.")]
    [InspectorName("표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("이 유형이 기본으로 사용하는 무기입니다. None이면 RootObjectData의 기본 무기를 사용합니다.")]
    [InspectorName("기본 무기")]
    [SerializeField] private HWJ_WeaponType defaultWeaponType;
    [Header("설명")]
    [TextArea]
    [Tooltip("이 유형의 목적, 특징, 밸런스 메모를 적습니다.")]
    [InspectorName("설명")]
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
