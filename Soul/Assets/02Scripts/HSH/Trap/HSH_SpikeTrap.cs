using System.Collections;
using UnityEngine;

public class HSH_SpikeTrap : MonoBehaviour
{
    [Header("고정형(가시) 함정 설정")]
    public float damage = 20f; // 데미지 수치

    [Header("감지 및 발동 설정")]
    public float detectionDistance = 3f; // 위로 플레이어를 감지할 거리
    public float delayTime = 0.7f; // 감지 후 애니메이션/공격 발동까지 걸리는 딜레이 시간
    public float activeDuration = 1.5f; // 가시가 나와있는(데미지 판정) 유지 시간
    public float coolTime = 0.5f; // 가시가 들어간 후 재감지까지의 쿨타임

    [Header("애니메이션 설정")]
    public Animator animator;
    public string attackTriggerName = "Attack"; // 감지 시 실행할 애니메이션 트리거 이름

    [Header("콜라이더 Offset 설정")]
    public Collider2D damageCollider; // 데미지 판정을 담당할 콜라이더
    public Vector2 activeOffset = new Vector2(0f, 1f); // 가시가 나와서 공격할 때의 콜라이더 Offset
    private Vector2 originalOffset; // 평소 기본 콜라이더 Offset

    private bool isAttacking = false;
    private bool isProtruding = false; // 현재 가시가 나와있어 데미지를 줄 수 있는 상태인지
    private bool hasDamagedThisAttack = false; // 한 번의 공격당 중복 데미지 방지

    private void Start()
    {
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
        // 공격 중이 아닐 때만 플레이어를 감지합니다.
        if (!isAttacking)
        {
            // 위쪽으로 레이캐스트를 쏴서 플레이어 감지 (자신 콜라이더 무시 위해 RaycastAll 사용)
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.up, detectionDistance);

            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    // 플레이어를 감지하면 공격 루틴(코루틴) 시작!
                    StartCoroutine(AttackRoutine());
                    break;
                }
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hasDamagedThisAttack = false; // 데미지 판정 초기화

        // 1. 플레이어 감지 후 설정한 딜레이 대기
        yield return new WaitForSeconds(delayTime);

        // 2. 애니메이션 실행 & 콜라이더 Offset 이동 (데미지 영역 활성화)
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }

        isProtruding = true;
        if (damageCollider != null)
        {
            damageCollider.offset = activeOffset;
        }

        // 3. 솟아오른 상태(공격 위험 상태) 유지
        yield return new WaitForSeconds(activeDuration);

        // 4. 다시 가시 들어감 (콜라이더 Offset 원복 & 데미지 영역 비활성화)
        isProtruding = false;
        if (damageCollider != null)
        {
            damageCollider.offset = originalOffset;
        }

        // 5. 쿨타임 (함정이 들어가고 나서 다시 감지하기까지의 시간)
        yield return new WaitForSeconds(coolTime);
        isAttacking = false;
    }

    // Trigger 영역에 닿아있을 때
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 가시가 나와 있는 상태이고, 이번 공격에 아직 데미지를 주지 않았다면!
        if (isProtruding && !hasDamagedThisAttack && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            ApplyDamageAndKnockback(collision.gameObject);
            hasDamagedThisAttack = true;
        }
    }

    // 물리적 충돌체에 부딪혔을 때
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isProtruding && !hasDamagedThisAttack && (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy")))
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

    // 에디터 씬 뷰에서 감지 범위 및 콜라이더 Offset 위치를 시각적으로 확인하기 위한 Gizmos
    private void OnDrawGizmosSelected()
    {
        // 1. 감지 범위 레이 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * detectionDistance);

        // 2. 공격 시 활성화되는 Collider Offset 위치 표시 (노란색)
        Gizmos.color = Color.yellow;
        Vector3 activeOffsetWorldPos = transform.TransformPoint(activeOffset);
        Gizmos.DrawWireSphere(activeOffsetWorldPos, 0.2f);
    }
}
