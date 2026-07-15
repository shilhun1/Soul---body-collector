using UnityEngine;

public class HSH_FallingTrap : MonoBehaviour
{
    [Header("낙석 함정 설정")]
    public float damage = 20f; // 데미지 수치
    public float fallGravity = 3f; // 떨어지는 중력 값
    
    [Header("아래쪽 감지 범위 설정")]
    [Tooltip("돌 아래쪽으로 플레이어를 감지할 거리 (숫자를 키우면 감지 범위가 길어집니다)")]
    public float detectionDistance = 10f; 

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 시작 시 공중에 떠있도록 중력 0 설정
            rb.gravityScale = 0f;
        }
    }

    private void Update()
    {
        // 아직 돌이 떨어지지 않은 상태(공중에 떠있는 상태)일 때만 감지
        if (rb != null && rb.gravityScale == 0f)
        {
            // RaycastAll을 사용하여 자신(돌)의 콜라이더에 막히지 않고 선 위의 모든 것을 검사합니다.
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, detectionDistance);

            foreach (RaycastHit2D hit in hits)
            {
                // 맞은 것들 중에 플레이어(Player)가 있다면? (함정 발동은 플레이어에게만)
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    // 중력을 켜서 돌을 떨어뜨립니다!
                    rb.gravityScale = fallGravity;
                    break; // 찾았으니 검사 종료
                }
            }
        }
    }

    // 감지 센서(Trigger)에 플레이어나 적이 들어왔을 때 (기존 방식 유지)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
        {
            // 중력을 활성화하여 돌을 떨어뜨림 (플레이어만 발동시킴)
            if (rb != null && rb.gravityScale == 0f)
            {
                if (collision.CompareTag("Player"))
                {
                    rb.gravityScale = fallGravity;
                }
            }
            else
            {
                // 이미 떨어지는 중이거나 땅에 닿은 상태에서 부딪혔다면 데미지와 넉백
                ApplyDamageAndKnockback(collision.gameObject);
            }
        }
    }

    // 돌이 실제로 부딪혔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("hit");
            ApplyDamageAndKnockback(collision.gameObject);
        }
    }

    private void ApplyDamageAndKnockback(GameObject target)
    {
        // 영혼 상태일 경우 데미지 및 넉백 무시
        HWJ_SoulSystem soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();
        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return;
        }

        // 1. 데미지 처리
        HWJ_RuntimeStatusSystem statusSystem = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        if (statusSystem != null)
        {
            statusSystem.ApplyDamage(damage);
            Debug.Log($"[{gameObject.name}] {target.name}에게 데미지: {damage}");
        }

        // 2. 넉백 처리
        float knockbackPower = 10f; 
        Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
        knockbackDir.y += 0.5f; // 약간 위로 뜨게 설정
        knockbackDir = knockbackDir.normalized;

        hys_Player_Hit playerHit = target.GetComponentInParent<hys_Player_Hit>();
        if (playerHit != null)
        {
            playerHit.ApplyKnockback(knockbackDir * knockbackPower);
        }
        else
        {
            HWJ_KnockbackSystem knockbackSystem = target.GetComponentInParent<HWJ_KnockbackSystem>();
            if (knockbackSystem == null)
            {
                Rigidbody2D parentRb = target.GetComponentInParent<Rigidbody2D>();
                knockbackSystem = (parentRb != null) ? parentRb.gameObject.AddComponent<HWJ_KnockbackSystem>() : target.AddComponent<HWJ_KnockbackSystem>();
            }
            knockbackSystem.PlayKnockback(knockbackDir, knockbackPower, 0.25f);
        }
    }

    // 유니티 씬(Scene) 화면에서 감지 범위를 빨간색 선으로 눈에 보이게 그려주는 편의 기능입니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // 돌 위치에서 아래쪽으로 설정한 거리만큼 선을 긋습니다.
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * detectionDistance);
    }
}
