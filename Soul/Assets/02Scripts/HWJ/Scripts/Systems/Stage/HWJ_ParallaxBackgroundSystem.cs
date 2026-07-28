using UnityEngine;

/// <summary>
/// 2D 배경 레이어가 카메라 이동량보다 느리거나 빠르게 움직이도록 만드는 간단한 패럴랙스 시스템입니다.
/// 스테이지 배경에만 붙이며, 플레이어 조작/전투/빙의 로직과는 분리되어 있습니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Stage/Parallax Background System")]
public sealed class HWJ_ParallaxBackgroundSystem : MonoBehaviour
{
    [Header("카메라 추적")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private bool autoFindMainCamera = true;

    [Header("패럴랙스 값")]
    [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.18f, 0.04f);
    [SerializeField] private bool keepInitialZPosition = true;
    [SerializeField] private bool useLateUpdate = true;

    [Header("확인용")]
    [SerializeField] private Vector3 initialLayerPosition;
    [SerializeField] private Vector3 previousCameraPosition;
    [SerializeField] private bool cameraResolved;

    public Vector2 ParallaxMultiplier => parallaxMultiplier;
    public bool CameraResolved => cameraResolved;

    private void OnEnable()
    {
        ResolveCameraTarget();
        initialLayerPosition = transform.position;
        previousCameraPosition = cameraTarget != null ? cameraTarget.position : Vector3.zero;
    }

    private void Update()
    {
        if (!useLateUpdate)
        {
            ApplyParallax();
        }
    }

    private void LateUpdate()
    {
        if (useLateUpdate)
        {
            ApplyParallax();
        }
    }

    public void SetParallaxMultiplier(Vector2 newMultiplier)
    {
        parallaxMultiplier = newMultiplier;
    }

    public void SetCameraTarget(Transform newCameraTarget)
    {
        cameraTarget = newCameraTarget;
        cameraResolved = cameraTarget != null;
        previousCameraPosition = cameraResolved ? cameraTarget.position : Vector3.zero;
    }

    private void ResolveCameraTarget()
    {
        if (cameraTarget != null || !autoFindMainCamera)
        {
            cameraResolved = cameraTarget != null;
            return;
        }

        Camera mainCamera = Camera.main;
        cameraTarget = mainCamera != null ? mainCamera.transform : null;
        cameraResolved = cameraTarget != null;
    }

    private void ApplyParallax()
    {
        if (cameraTarget == null)
        {
            ResolveCameraTarget();

            if (cameraTarget == null)
            {
                return;
            }
        }

        Vector3 cameraDelta = cameraTarget.position - previousCameraPosition;
        Vector3 nextPosition = transform.position;
        nextPosition.x += cameraDelta.x * parallaxMultiplier.x;
        nextPosition.y += cameraDelta.y * parallaxMultiplier.y;

        if (keepInitialZPosition)
        {
            nextPosition.z = initialLayerPosition.z;
        }

        transform.position = nextPosition;
        previousCameraPosition = cameraTarget.position;
        cameraResolved = true;
    }
}
