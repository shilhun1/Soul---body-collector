using System.Collections;
using UnityEngine;

/// <summary>
/// 풀에서 생성된 오브젝트가 자기 풀로 돌아갈 수 있게 해주는 컴포넌트입니다.
/// 투사체, 이펙트, 스폰 오브젝트 프리팹에 붙이면 Destroy 대신 ReturnToPool을 사용할 수 있습니다.
/// </summary>
public class HWJ_PoolableObject : MonoBehaviour
{
    private HWJ_ObjectPoolSystem ownerPool;
    private GameObject prefabKey;

    public GameObject PrefabKey => prefabKey;

    /// <summary>
    /// ObjectPoolSystem이 생성 직후 이 오브젝트의 원본 프리팹과 소유 풀을 기록합니다.
    /// 외부 시스템에서 직접 호출할 필요는 없습니다.
    /// </summary>
    public void SetPoolInfo(HWJ_ObjectPoolSystem ownerPool, GameObject prefabKey)
    {
        this.ownerPool = ownerPool;
        this.prefabKey = prefabKey;
    }

    /// <summary>
    /// 풀에서 꺼냈을 때 호출됩니다.
    /// 필요한 경우 파생 컴포넌트나 다른 스크립트가 이 타이밍에 상태를 초기화하면 됩니다.
    /// </summary>
    public void OnSpawnedFromPool()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 풀에 들어가기 직전에 호출됩니다.
    /// Rigidbody나 이펙트 잔여 상태 정리는 필요한 시스템에서 이 타이밍에 확장합니다.
    /// </summary>
    public void OnDespawnedToPool()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 이 오브젝트를 소유 풀로 되돌립니다.
    /// 풀 정보가 없으면 안전하게 비활성화만 합니다.
    /// </summary>
    public void ReturnToPool()
    {
        if (ownerPool == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ownerPool.Despawn(gameObject);
    }
}

