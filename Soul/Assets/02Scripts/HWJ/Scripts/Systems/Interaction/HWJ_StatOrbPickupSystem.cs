using UnityEngine;

/// <summary>
/// 씬에 배치된 능력치 구슬의 상호작용 컴포넌트입니다.
/// StatOrbDataSO를 RuntimeStatusSystem에 적용하고, 수집 후 오브젝트를 제거합니다.
/// </summary>
public class HWJ_StatOrbPickupSystem : MonoBehaviour
{
    [SerializeField] private HWJ_StatOrbDataSO statOrbData;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private bool destroyOnCollect = true;

    private bool isCollected;

    /// <summary>
    /// 대상 런타임 상태에 구슬 효과를 적용합니다.
    /// 플레이어뿐 아니라 버프를 받을 수 있는 다른 오브젝트에도 재사용할 수 있습니다.
    /// </summary>
    public bool TryCollect(HWJ_RuntimeStatusSystem collectorStatus)
    {
        if (isCollected || statOrbData == null || collectorStatus == null)
        {
            return false;
        }

        isCollected = true;
        collectorStatus.ApplyStatOrb(statOrbData);

        if (statOrbData.CollectEffectPrefab != null)
        {
            if (objectPool != null)
            {
                objectPool.Spawn(statOrbData.CollectEffectPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                HWJ_GameAccess.Spawn(statOrbData.CollectEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        if (destroyOnCollect)
        {
            HWJ_PoolableObject poolableObject = GetComponent<HWJ_PoolableObject>();

            if (poolableObject != null)
            {
                poolableObject.ReturnToPool();
            }
            else if (objectPool != null)
            {
                objectPool.Despawn(gameObject);
            }
            else if (HWJ_GameAccess.HasManager)
            {
                HWJ_GameAccess.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        return true;
    }
}
