using UnityEngine;

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

        if (dataResolver != null)
        {
            dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData);
            EnemyData = enemyData;
        }
    }
}
