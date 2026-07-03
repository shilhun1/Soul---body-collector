using UnityEngine;

/// <summary>
/// 적 오브젝트에서 EnemyTypeDataSO를 가져오는 AI 연결 컴포넌트입니다.
/// 현재는 데이터 참조 단계이며, 실제 추적/공격 행동은 EnemyData의 Tracking, AI, Navigation 값을 읽는 후속 AI 로직에서 구현합니다.
/// </summary>
public class HWJ_MonsterAISystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;

    public HWJ_EnemyTypeDataSO EnemyData { get; private set; }

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        RefreshData();
    }

    public void RefreshData()
    {
        if (dataResolver != null)
        {
            dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData);
            EnemyData = enemyData;
        }
    }
}
