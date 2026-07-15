using System;
using UnityEngine;

/// <summary>
/// 오브젝트 풀 하나의 설정 데이터입니다.
/// 풀링할 프리팹, 초기 생성 개수, 최대 개수, 부족할 때 확장 가능 여부를 관리합니다.
/// </summary>
[Serializable]
public class HWJ_PoolEntryData
{
    [Header("풀 항목")]
    [InspectorName("풀 ID")]
    [Tooltip("이 풀을 구분하는 고정 ID입니다.")]
    public string poolId;
    [InspectorName("프리팹")]
    [Tooltip("풀링해서 재사용할 프리팹입니다.")]
    public GameObject prefab;
    [InspectorName("초기 생성 수")]
    [Tooltip("처음에 미리 만들어둘 개수입니다.")]
    public int initialSize = 5;
    [InspectorName("최대 생성 수")]
    [Tooltip("풀에서 만들 수 있는 최대 개수입니다.")]
    public int maxSize = 30;
    [InspectorName("부족하면 확장")]
    [Tooltip("켜면 풀이 부족할 때 추가로 생성할 수 있습니다.")]
    public bool canExpand = true;
}
