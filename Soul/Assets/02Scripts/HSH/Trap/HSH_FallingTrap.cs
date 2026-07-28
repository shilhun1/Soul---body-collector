using System.Collections;
using UnityEngine;

public class HSH_FallingTrap : MonoBehaviour
{
    [Header("낙석 함정 설정")]
    public float damage = 20f; // 데미지 수치
    public float fallGravity = 3f; // 떨어지는 중력 값 (0일 경우 물리 이동 대신 애니메이션만 사용)
    public bool usePhysicsFall = true; // 물리(Gravity)로 떨어뜨릴지 여부
    public float activeDuration = 1.5f; // 공격 판정 유지 시간

    [Header("아래쪽 감지 범위 설정")]
    [Tooltip("돌 아래쪽으로 플레이어를 감지할 거리 (숫자를 키우면 감지 범위가 길어집니다)")]
    public float detectionDistance = 10f;

    [Header("애니메이션 설정")]
    public Animator animator;
    public string attackTriggerName = "Attack"; // 플레이어 감지 시 재생할 애니메이션 트리거

    [Header("콜라이더 Offset 설정")]
    public Collider2D damageCollider; // 데미지 판정을 담당할 콜라이더
    public Vector2 activeOffset = new Vector2(0f, -2f); // 발동 시 이동시킬 콜라이더 Offset
    private Vector2 originalOffset; // 기본 콜라이더 Offset

    private Rigidbody2D rb;
    private bool hasTriggered = false; // 1회 발동 여부
    private bool isProtruding = false;
    private bool hasDamagedThisAttack = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 시작 시 공중에 떠있도록 중력 0 설정
            rb.gravityScale = 0f;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }

        if (damageCollider != null)
        {
            originalOffset = damageCollider.offset;
        }
    }

    private void Update()
    {
        // 아직 한 번도 발동되지 않은 상태일 때만 플레이어 감지
        if (!hasTriggered)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, detectionDistance);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    StartCoroutine(AttackRoutine());
                    break;
                }
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        hasTriggered = true; // 1회만 발동되도록 설정
        hasDamagedThisAttack = false;

        // 1. 애니메이션 실행
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }

        // 2. 물리 낙하 설정 시 중력 적용
        if (usePhysicsFall && rb != null)
        {
            rb.gravityScale = fallGravity;
        }

        // 3. 콜라이더 Offset 이동 (데미지 영역 변경)
        isProtruding = true;
        if (damageCollider != null)
        {
            damageCollider.offset = activeOffset;
        }

        // 4. 유지 시간 대기
        yield return new WaitForSeconds(activeDuration);

        // 5. 콜라이더 Offset 원복 (데미지 비활성화)
        isProtruding = false;
        if (damageCollider != null)
        {
            damageCollider.offset = originalOffset;
        }
    }

    // 센서(Trigger)에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
        {
            if (!hasTriggered)
            {
                if (collision.CompareTag("Player"))
                {
                    StartCoroutine(AttackRoutine());
                }
            }
            else if (isProtruding && !hasDamagedThisAttack)
            {
                ApplyDamageAndKnockback(collision.gameObject);
                hasDamagedThisAttack = true;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isProtruding && !hasDamagedThisAttack && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            ApplyDamageAndKnockback(collision.gameObject);
            hasDamagedThisAttack = true;
        }
    }

    // 실제 충돌 시 (물리 충돌)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((isProtruding || usePhysicsFall) && !hasDamagedThisAttack && (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy")))
        {
            ApplyDamageAndKnockback(collision.gameObject);
            hasDamagedThisAttack = true;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * detectionDistance);

        Gizmos.color = Color.yellow;
        Vector3 activeOffsetWorldPos = transform.TransformPoint(activeOffset);
        Gizmos.DrawWireSphere(activeOffsetWorldPos, 0.2f);
    }
}
