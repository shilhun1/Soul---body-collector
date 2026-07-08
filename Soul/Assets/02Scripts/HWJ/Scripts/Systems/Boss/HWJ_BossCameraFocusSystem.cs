using System.Collections;
using UnityEngine;

public class HWJ_BossCameraFocusSystem : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool disablePlayerFollowWhileFocusing = true;
    [SerializeField] private float focusLerpSpeed = 6f;
    [SerializeField] private Vector2 focusOffset = new Vector2(0f, 1f);

    private Coroutine focusRoutine;
    private HWJ_PlayerCameraFollowSystem disabledFollowSystem;

    public void FocusOnBoss(Transform boss, Transform player, float durationSeconds)
    {
        if (boss == null || durationSeconds <= 0f)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
        }

        focusRoutine = StartCoroutine(FocusRoutine(boss, durationSeconds));
    }

    private IEnumerator FocusRoutine(Transform boss, float durationSeconds)
    {
        if (disablePlayerFollowWhileFocusing)
        {
            disabledFollowSystem = targetCamera.GetComponent<HWJ_PlayerCameraFollowSystem>();

            if (disabledFollowSystem != null)
            {
                disabledFollowSystem.enabled = false;
            }
        }

        float endTime = Time.time + Mathf.Max(0.01f, durationSeconds);

        while (Time.time < endTime && boss != null && targetCamera != null)
        {
            Vector3 targetPosition = boss.position + (Vector3)focusOffset;
            targetPosition.z = targetCamera.transform.position.z;
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPosition,
                Time.deltaTime * Mathf.Max(0.01f, focusLerpSpeed));
            yield return null;
        }

        if (disabledFollowSystem != null)
        {
            disabledFollowSystem.enabled = true;
            disabledFollowSystem = null;
        }

        focusRoutine = null;
    }
}
