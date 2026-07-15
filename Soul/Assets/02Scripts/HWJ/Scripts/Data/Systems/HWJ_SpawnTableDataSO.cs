using UnityEngine;

/// <summary>
/// 스테이지나 구간별 스폰 목록을 관리하는 ScriptableObject입니다.
/// 데이터베이스에 등록하거나 SpawnerSystem에 직접 할당해서 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_SpawnTableData", menuName = "HWJ/Data/System/Spawn Table")]
public class HWJ_SpawnTableDataSO : ScriptableObject
{
    [Header("스폰 테이블")]
    [Tooltip("이 스폰 테이블을 구분하는 고정 ID입니다.")]
    [InspectorName("스폰 테이블 ID")]
    [SerializeField] private string tableId;
    [Tooltip("이 테이블에서 생성할 스폰 항목 목록입니다.")]
    [InspectorName("스폰 항목 목록")]
    [SerializeField] private HWJ_SpawnEntryData[] entries;

    public string TableId => tableId;
    public HWJ_SpawnEntryData[] Entries => entries;

    /// <summary>
    /// 스폰 ID로 특정 스폰 항목을 찾습니다.
    /// 스테이지 이벤트나 보스 페이즈에서 특정 스폰만 실행할 때 사용합니다.
    /// </summary>
    public bool TryGetEntry(string spawnId, out HWJ_SpawnEntryData entry)
    {
        entry = null;

        if (entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].spawnId == spawnId)
            {
                entry = entries[i];
                return true;
            }
        }

        return false;
    }
}
