using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 보스 대사 동안 현재 활성 CinemachineCamera의 추적 대상과 렌즈를 임시로 변경합니다.
/// 대사가 끝나면 기존 플레이어 추적 대상과 렌즈 값을 그대로 복원합니다.
/// </summary>
public class HWJ_BossCameraFocusSystem : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("비워두면 Main Camera의 CinemachineBrain 또는 씬의 활성 CinemachineCamera를 자동 탐색합니다.")]
    [SerializeField] private CinemachineCamera targetCinemachineCamera;
    [SerializeField] private Camera targetCamera;

    [Header("대사 줌")]
    [SerializeField, Min(1f)] private float dialogueFocusFieldOfView = 28f;
    [SerializeField, Min(0.1f)] private float dialogueFocusOrthographicSize = 3.2f;
    [SerializeField, Min(0.01f)] private float zoomLerpSpeed = 4f;
    [SerializeField, Min(0f)] private float returnBlendSeconds = 0.45f;
    [SerializeField] private Vector2 focusOffset = new Vector2(0f, 0.8f);

    [Header("일반 카메라 대체 동작")]
    [SerializeField] private bool disablePlayerFollowWhileFocusing = true;
    [SerializeField, Min(0.01f)] private float fallbackPositionLerpSpeed = 6f;

    private Coroutine focusRoutine;
    private CinemachineCamera activeCinemachineCamera;
    private Transform previousTrackingTarget;
    private LensSettings previousLens;
    private LensSettings focusedLens;
    private Transform focusAnchor;
    private HWJ_PlayerCameraFollowSystem disabledFollowSystem;
    private bool hasStoredCinemachineState;
    private Camera activeFallbackCamera;
    private float previousFallbackFieldOfView;
    private float previousFallbackOrthographicSize;
    private bool hasStoredFallbackState;

    public bool IsFocusing => focusRoutine != null;
    public bool UsesCinemachineDuringFocus => activeCinemachineCamera != null;
    public float DialogueFocusFieldOfView => dialogueFocusFieldOfView;
    public float DialogueFocusOrthographicSize => dialogueFocusOrthographicSize;

    /// <summary>
    /// 통합 빌더가 생성 프리팹에 읽기 쉬운 대사 줌 기본값을 적용할 때 사용합니다.
    /// </summary>
    public void ConfigureFighterBossDialogueDefaults()
    {
        dialogueFocusFieldOfView = 28f;
        dialogueFocusOrthographicSize = 3.2f;
        zoomLerpSpeed = 4f;
        returnBlendSeconds = 0.45f;
        focusOffset = new Vector2(0f, 0.8f);
        disablePlayerFollowWhileFocusing = true;
        fallbackPositionLerpSpeed = 6f;
    }

    public void FocusOnBoss(Transform boss, Transform player, float durationSeconds)
    {
        if (boss == null || durationSeconds <= 0f)
        {
            return;
        }

        StopFocus();
        CinemachineCamera cinemachineCamera = ResolveCinemachineCamera();

        focusRoutine = cinemachineCamera != null
            ? StartCoroutine(CinemachineFocusRoutine(cinemachineCamera, boss, player, durationSeconds))
            : StartCoroutine(FallbackCameraFocusRoutine(boss, player, durationSeconds));
    }

    /// <summary>
    /// 전투가 중단되거나 보스가 비활성화될 때 카메라를 즉시 원래 상태로 되돌립니다.
    /// </summary>
    public void StopFocus()
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        RestoreCinemachineImmediately();
        RestoreFallbackImmediately();
    }

    private void OnDisable()
    {
        StopFocus();
    }

    private IEnumerator CinemachineFocusRoutine(
        CinemachineCamera cinemachineCamera,
        Transform boss,
        Transform player,
        float durationSeconds)
    {
        activeCinemachineCamera = cinemachineCamera;
        previousTrackingTarget = cinemachineCamera.Target.TrackingTarget;
        previousLens = cinemachineCamera.Lens;
        focusedLens = previousLens;
        // 씬의 기존 카메라보다 멀어지는 값은 사용하지 않아 대사 중에는 항상 줌인만 합니다.
        focusedLens.FieldOfView = Mathf.Min(previousLens.FieldOfView, dialogueFocusFieldOfView);
        focusedLens.OrthographicSize = Mathf.Min(
            previousLens.OrthographicSize,
            dialogueFocusOrthographicSize);
        hasStoredCinemachineState = true;

        Transform dialogueTarget = GetOrCreateFocusAnchor(boss);
        cinemachineCamera.Target.TrackingTarget = dialogueTarget;
        float endTime = Time.time + Mathf.Max(0.01f, durationSeconds);

        while (Time.time < endTime && boss != null && cinemachineCamera != null)
        {
            ApplyDialogueZoom(cinemachineCamera, Time.deltaTime * zoomLerpSpeed);
            yield return null;
        }

        if (cinemachineCamera == null)
        {
            ClearCinemachineState();
            focusRoutine = null;
            yield break;
        }

        cinemachineCamera.Target.TrackingTarget = previousTrackingTarget != null
            ? previousTrackingTarget
            : player;

        float returnEndTime = Time.time + Mathf.Max(0f, returnBlendSeconds);

        while (Time.time < returnEndTime && cinemachineCamera != null)
        {
            ApplyLens(cinemachineCamera, previousLens, Time.deltaTime * zoomLerpSpeed);
            yield return null;
        }

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens = previousLens;
        }

        ClearCinemachineState();
        focusRoutine = null;
    }

    private IEnumerator FallbackCameraFocusRoutine(
        Transform boss,
        Transform player,
        float durationSeconds)
    {
        Camera camera = ResolveTargetCamera();

        if (camera == null)
        {
            focusRoutine = null;
            yield break;
        }

        activeFallbackCamera = camera;
        previousFallbackFieldOfView = camera.fieldOfView;
        previousFallbackOrthographicSize = camera.orthographicSize;
        float focusedFieldOfView = Mathf.Min(previousFallbackFieldOfView, dialogueFocusFieldOfView);
        float focusedOrthographicSize = Mathf.Min(
            previousFallbackOrthographicSize,
            dialogueFocusOrthographicSize);
        hasStoredFallbackState = true;

        if (disablePlayerFollowWhileFocusing)
        {
            disabledFollowSystem = camera.GetComponent<HWJ_PlayerCameraFollowSystem>();

            if (disabledFollowSystem != null)
            {
                disabledFollowSystem.enabled = false;
            }
        }

        float endTime = Time.time + Mathf.Max(0.01f, durationSeconds);

        while (Time.time < endTime && boss != null && camera != null)
        {
            Vector3 targetPosition = boss.position + (Vector3)focusOffset;
            targetPosition.z = camera.transform.position.z;
            camera.transform.position = Vector3.Lerp(
                camera.transform.position,
                targetPosition,
                Time.deltaTime * fallbackPositionLerpSpeed);
            camera.fieldOfView = Mathf.Lerp(
                camera.fieldOfView,
                focusedFieldOfView,
                Time.deltaTime * zoomLerpSpeed);
            camera.orthographicSize = Mathf.Lerp(
                camera.orthographicSize,
                focusedOrthographicSize,
                Time.deltaTime * zoomLerpSpeed);
            yield return null;
        }

        RestoreFallbackFollow();
        float returnEndTime = Time.time + Mathf.Max(0f, returnBlendSeconds);

        while (Time.time < returnEndTime && camera != null)
        {
            camera.fieldOfView = Mathf.Lerp(
                camera.fieldOfView,
                previousFallbackFieldOfView,
                Time.deltaTime * zoomLerpSpeed);
            camera.orthographicSize = Mathf.Lerp(
                camera.orthographicSize,
                previousFallbackOrthographicSize,
                Time.deltaTime * zoomLerpSpeed);
            yield return null;
        }

        if (camera != null)
        {
            camera.fieldOfView = previousFallbackFieldOfView;
            camera.orthographicSize = previousFallbackOrthographicSize;
        }

        ClearFallbackState();
        focusRoutine = null;
    }

    private CinemachineCamera ResolveCinemachineCamera()
    {
        if (targetCinemachineCamera != null && targetCinemachineCamera.isActiveAndEnabled)
        {
            return targetCinemachineCamera;
        }

        Camera camera = ResolveTargetCamera();
        CinemachineBrain brain = camera != null ? camera.GetComponent<CinemachineBrain>() : null;

        if (brain != null && brain.ActiveVirtualCamera is CinemachineCamera activeCamera)
        {
            targetCinemachineCamera = activeCamera;
            return targetCinemachineCamera;
        }

        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        int highestPriority = int.MinValue;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null || !cameras[i].isActiveAndEnabled)
            {
                continue;
            }

            int priority = cameras[i].Priority.Value;

            if (targetCinemachineCamera == null || priority > highestPriority)
            {
                targetCinemachineCamera = cameras[i];
                highestPriority = priority;
            }
        }

        return targetCinemachineCamera;
    }

    private Camera ResolveTargetCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        return targetCamera;
    }

    private Transform GetOrCreateFocusAnchor(Transform boss)
    {
        if (focusAnchor == null)
        {
            GameObject anchorObject = new GameObject("HWJ_BossDialogueCameraAnchor");
            focusAnchor = anchorObject.transform;
        }

        focusAnchor.SetParent(boss, false);
        focusAnchor.localPosition = new Vector3(focusOffset.x, focusOffset.y, 0f);
        return focusAnchor;
    }

    private void ApplyDialogueZoom(CinemachineCamera cinemachineCamera, float t)
    {
        LensSettings lens = cinemachineCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(
            lens.FieldOfView,
            focusedLens.FieldOfView,
            Mathf.Clamp01(t));
        lens.OrthographicSize = Mathf.Lerp(
            lens.OrthographicSize,
            focusedLens.OrthographicSize,
            Mathf.Clamp01(t));
        cinemachineCamera.Lens = lens;
    }

    private static void ApplyLens(
        CinemachineCamera cinemachineCamera,
        LensSettings destination,
        float t)
    {
        LensSettings lens = cinemachineCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, destination.FieldOfView, Mathf.Clamp01(t));
        lens.OrthographicSize = Mathf.Lerp(
            lens.OrthographicSize,
            destination.OrthographicSize,
            Mathf.Clamp01(t));
        cinemachineCamera.Lens = lens;
    }

    private void RestoreCinemachineImmediately()
    {
        if (hasStoredCinemachineState && activeCinemachineCamera != null)
        {
            activeCinemachineCamera.Target.TrackingTarget = previousTrackingTarget;
            activeCinemachineCamera.Lens = previousLens;
        }

        ClearCinemachineState();
    }

    private void ClearCinemachineState()
    {
        activeCinemachineCamera = null;
        previousTrackingTarget = null;
        hasStoredCinemachineState = false;
    }

    private void RestoreFallbackFollow()
    {
        if (disabledFollowSystem != null)
        {
            disabledFollowSystem.enabled = true;
            disabledFollowSystem = null;
        }
    }

    private void RestoreFallbackImmediately()
    {
        if (hasStoredFallbackState && activeFallbackCamera != null)
        {
            activeFallbackCamera.fieldOfView = previousFallbackFieldOfView;
            activeFallbackCamera.orthographicSize = previousFallbackOrthographicSize;
        }

        RestoreFallbackFollow();
        ClearFallbackState();
    }

    private void ClearFallbackState()
    {
        activeFallbackCamera = null;
        hasStoredFallbackState = false;
    }
}
