using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(hys_Player_State))]
public class hys_Player_Hit : MonoBehaviour
{
    [Header("Knockback")]
    // 넉백 속도가 유지되는 시간입니다.
    [SerializeField] private float knockbackTime = 0.15f;

    // 넉백 중에는 기존 속도를 덮어써서 확실히 밀리게 할지 정합니다.
    [SerializeField] private bool overrideVelocity = true;

    [Header("Debug")]
    // 넉백 로그를 출력할지 정합니다.
    [SerializeField] private bool useDebugLog = true;

    private Rigidbody2D rb;
    private hys_Player_State playerState;
    private Coroutine knockbackRoutine;

    public bool IsKnockbacking { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<hys_Player_State>();
    }

    // 적 공격이 플레이어를 밀어낼 때 호출하는 함수입니다.
    public void ApplyKnockback(Vector2 knockbackVelocity)
    {
        if (playerState != null && playerState.CurrentState == hys_PlayerState.Dead)
        {
            return;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackRoutine(knockbackVelocity));
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackVelocity)
    {
        IsKnockbacking = true;

        if (playerState != null)
        {
            playerState.SetState(hys_PlayerState.Hit);
        }

        if (overrideVelocity)
        {
            rb.linearVelocity = knockbackVelocity;
        }
        else
        {
            rb.linearVelocity += knockbackVelocity;
        }

        if (useDebugLog)
        {
            Debug.Log($"[Player Hit] Knockback: {knockbackVelocity}", this);
        }

        yield return new WaitForSeconds(knockbackTime);

        IsKnockbacking = false;
        knockbackRoutine = null;
    }
}
