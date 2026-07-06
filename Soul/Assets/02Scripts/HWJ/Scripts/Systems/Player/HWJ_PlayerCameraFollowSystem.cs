using UnityEngine;

/// <summary>
/// PlayerTypeDataSO.Camera 값을 사용해 카메라가 플레이어를 따라가게 하는 기본 시스템입니다.
/// 카메라 오브젝트에 붙이고 target에는 플레이어 Transform을 연결해서 사용합니다.
/// </summary>
public class HWJ_PlayerCameraFollowSystem : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private HWJ_RootObjectDataResolver targetDataResolver;

    private void LateUpdate()
    {
        if (target == null || targetDataResolver == null || !targetDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        Vector3 targetPosition = target.position;
        float facing = Mathf.Sign(target.localScale.x);
        targetPosition.x += facing * playerData.Camera.lookAheadDistance;
        targetPosition.y += playerData.Camera.verticalOffset;
        targetPosition.z = transform.position.z;

        if (playerData.Camera.clampToBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, playerData.Camera.minBounds.x, playerData.Camera.maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, playerData.Camera.minBounds.y, playerData.Camera.maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * playerData.Camera.followSpeed);
    }
}
