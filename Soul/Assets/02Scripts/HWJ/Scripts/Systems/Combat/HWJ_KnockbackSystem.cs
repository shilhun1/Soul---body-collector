using UnityEngine;

public class HWJ_KnockbackSystem : MonoBehaviour
{
    private const float MinimumDuration = 0.05f;
    private const float VelocityScale = 1.6f;

    [SerializeField] private Rigidbody2D body;
    [SerializeField] private bool preserveVerticalVelocity = true;
    [SerializeField] private float remainingTime;
    [SerializeField] private float duration;
    [SerializeField] private Vector2 startVelocity;

    public bool IsActive => remainingTime > 0f;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    public void PlayKnockback(Vector2 direction, float power, float durationSeconds)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (body == null || power <= 0f)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        duration = Mathf.Max(MinimumDuration, durationSeconds);
        remainingTime = duration;
        startVelocity = normalizedDirection * power * VelocityScale;
    }

    public void StopKnockback()
    {
        remainingTime = 0f;
        duration = 0f;
        startVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (remainingTime <= 0f || body == null)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.fixedDeltaTime);
        float normalizedTime = duration > 0f ? Mathf.Clamp01(remainingTime / duration) : 0f;
        float easedStrength = normalizedTime * normalizedTime;

        Vector2 velocity = body.linearVelocity;
        velocity.x = startVelocity.x * easedStrength;

        if (!preserveVerticalVelocity)
        {
            velocity.y = startVelocity.y * easedStrength;
        }

        body.linearVelocity = velocity;

        if (remainingTime <= 0f)
        {
            startVelocity = Vector2.zero;
        }
    }
}
