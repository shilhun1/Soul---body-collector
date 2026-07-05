using System;
using UnityEngine;

/// <summary>
/// 오브젝트 풀 하나의 설정 데이터입니다.
/// 풀링할 프리팹, 초기 생성 개수, 최대 개수, 부족할 때 확장 가능 여부를 관리합니다.
/// </summary>
[Serializable]
public class HWJ_PoolEntryData
{
    public string poolId;
    public GameObject prefab;
    public int initialSize = 5;
    public int maxSize = 30;
    public bool canExpand = true;
}
