using UnityEngine;

/// <summary>
/// 프로젝트에서 사용할 풀 목록을 관리하는 ScriptableObject입니다.
/// GameManager 또는 ObjectPoolSystem에 연결해서 이펙트, 투사체, 몬스터, 구슬 프리팹을 미리 생성합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_ObjectPoolData", menuName = "HWJ/Data/System/Object Pool")]
public class HWJ_ObjectPoolDataSO : ScriptableObject
{
    [Header("오브젝트 풀")]
    [Tooltip("미리 생성하거나 재사용할 프리팹 풀 목록입니다.")]
    [InspectorName("풀 항목 목록")]
    [SerializeField] private HWJ_PoolEntryData[] entries;

    public HWJ_PoolEntryData[] Entries => entries;

    /// <summary>
    /// 프리팹 기준으로 풀 설정을 찾습니다.
    /// 시스템이 poolId를 몰라도 프리팹 참조만으로 풀링할 수 있게 해줍니다.
    /// </summary>
    public bool TryGetEntry(GameObject prefab, out HWJ_PoolEntryData entry)
    {
        entry = null;

        if (prefab == null || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].prefab == prefab)
            {
                entry = entries[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 풀 ID 기준으로 풀 설정을 찾습니다.
    /// 데이터베이스나 이벤트에서 문자열 ID로 풀을 선택할 때 사용합니다.
    /// </summary>
    public bool TryGetEntry(string poolId, out HWJ_PoolEntryData entry)
    {
        entry = null;

        if (string.IsNullOrEmpty(poolId) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].poolId == poolId)
            {
                entry = entries[i];
                return true;
            }
        }

        return false;
    }
}
