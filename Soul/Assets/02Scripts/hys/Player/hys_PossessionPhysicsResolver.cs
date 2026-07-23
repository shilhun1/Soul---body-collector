using System.Collections;
using UnityEngine;

// 빙의 중 물리를 잠시 멈추고 안전한 위치와 Collider 프로필을 적용한 뒤 다음 물리 프레임에 복구합니다.
[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class hys_PossessionPhysicsResolver : MonoBehaviour
{
    [Header("플레이어 물리")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private BoxCollider2D bodyCollider;

    [Header("겹침 해소")]
    [SerializeField] private LayerMask solidLayerMask = ~0;
    [SerializeField, Min(1)] private int maxUpwardResolveSteps = 30;
    [SerializeField, Min(0.01f)] private float upwardResolveStep = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool isResolving;
    [SerializeField] private string lastResolveResult;

    private Coroutine enableRoutine;
    private bool originalColliderEnabled;
    private bool originalColliderTrigger;
    private bool originalBodySimulated;

    public bool IsResolving => isResolving;

    private void Awake()
    {
        CacheReferences();
    }

    private void LateUpdate()
    {
        if (!isResolving)
        {
            return;
        }

        // 다른 상태 스크립트가 같은 프레임에 Collider를 켜도 물리 준비가 끝날 때까지 다시 잠급니다.
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        if (body != null)
        {
            body.simulated = false;
        }
    }

    public bool BeginPossessionTransition()
    {
        CacheReferences();

        if (body == null || bodyCollider == null)
        {
            lastResolveResult = "플레이어 Rigidbody2D 또는 BoxCollider2D가 없습니다.";
            return false;
        }

        if (enableRoutine != null)
        {
            StopCoroutine(enableRoutine);
            enableRoutine = null;
        }

        originalColliderEnabled = bodyCollider.enabled;
        originalColliderTrigger = bodyCollider.isTrigger;
        originalBodySimulated = body.simulated;
        isResolving = true;
        bodyCollider.enabled = false;
        body.simulated = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        lastResolveResult = "빙의 물리 잠금";
        return true;
    }

    public void CompletePossessionTransition(
        hys_BodyCollisionSettings settings,
        Vector2 feetWorldPosition)
    {
        CacheReferences();

        if (body == null || bodyCollider == null)
        {
            CancelPossessionTransition();
            return;
        }

        bodyCollider.size = settings.size;
        bodyCollider.offset = settings.offset;
        AlignColliderFeet(feetWorldPosition + Vector2.up * settings.groundClearance);
        ResolveSolidOverlapUpward();

        if (enableRoutine != null)
        {
            StopCoroutine(enableRoutine);
        }

        enableRoutine = StartCoroutine(EnablePhysicsOnNextFixedUpdate());
    }

    public void CancelPossessionTransition()
    {
        if (enableRoutine != null)
        {
            StopCoroutine(enableRoutine);
            enableRoutine = null;
        }

        isResolving = false;

        if (body != null)
        {
            body.simulated = originalBodySimulated;
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = originalColliderTrigger;
            bodyCollider.enabled = originalColliderEnabled;
        }

        lastResolveResult = "빙의 실패로 기존 물리 복구";
    }

    private void CacheReferences()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }
    }

    private void AlignColliderFeet(Vector2 desiredFeetWorldPosition)
    {
        Vector2 localFeet = bodyCollider.offset + Vector2.down * (bodyCollider.size.y * 0.5f);
        Vector2 currentFeetWorld = transform.TransformPoint(localFeet);
        Vector2 correction = desiredFeetWorldPosition - currentFeetWorld;
        transform.position += (Vector3)correction;
        Physics2D.SyncTransforms();
    }

    private void ResolveSolidOverlapUpward()
    {
        for (int step = 0; step < maxUpwardResolveSteps; step++)
        {
            if (!HasBlockingOverlap())
            {
                lastResolveResult = step == 0
                    ? "발 위치 배치 완료"
                    : $"위쪽으로 {step * upwardResolveStep:0.00}만큼 겹침 해소";
                return;
            }

            transform.position += Vector3.up * upwardResolveStep;
            Physics2D.SyncTransforms();
        }

        lastResolveResult = "최대 높이까지 올렸지만 겹침이 남아 있습니다.";
    }

    private bool HasBlockingOverlap()
    {
        Vector2 center = transform.TransformPoint(bodyCollider.offset);
        Vector3 lossyScale = transform.lossyScale;
        Vector2 size = new Vector2(
            bodyCollider.size.x * Mathf.Abs(lossyScale.x),
            bodyCollider.size.y * Mathf.Abs(lossyScale.y));
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            center,
            size * 0.98f,
            transform.eulerAngles.z,
            solidLayerMask);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null || overlap == bodyCollider || overlap.isTrigger)
            {
                continue;
            }

            Transform overlapTransform = overlap.transform;
            if (overlapTransform == transform || overlapTransform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private IEnumerator EnablePhysicsOnNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();

        isResolving = false;
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        bodyCollider.isTrigger = false;
        bodyCollider.enabled = true;
        Physics2D.SyncTransforms();
        enableRoutine = null;
        lastResolveResult += " / 다음 물리 프레임 활성화";
    }
}
