using UnityEngine;

/// <summary>
/// EnemyTypeDataSO의 Tracking/Navigation/State 값을 읽어 대상에게 접근하는 기본 적 이동 시스템입니다.
/// 실제 길찾기나 플랫폼 AI는 이 시스템을 교체하더라도 같은 EnemyTypeData를 계속 사용할 수 있습니다.
/// </summary>
public class HWJ_EnemyNavigationSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Transform target;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }

    private void Update()
    {
        if (target == null || dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > enemyData.Tracking.trackingRange || distance <= enemyData.Navigation.stoppingDistance)
        {
            if (runtimeStatus != null)
            {
                runtimeStatus.SetState(HWJ_RuntimeState.Idle);
            }

            return;
        }

        float moveSpeed = runtimeStatus != null ? runtimeStatus.MoveSpeed : dataResolver.Status.moveSpeed;
        transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(distance <= enemyData.State.attackRange ? HWJ_RuntimeState.Attack : HWJ_RuntimeState.Move);
        }
    }

    /// <summary>
    /// 추적할 대상을 외부에서 지정합니다.
    /// 감지 시스템이나 스테이지 매니저가 플레이어 Transform을 넘겨줄 때 사용합니다.
    /// </summary>
    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}
