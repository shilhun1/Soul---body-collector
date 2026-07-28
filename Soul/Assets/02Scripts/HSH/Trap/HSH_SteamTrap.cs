using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_SteamTrap : MonoBehaviour
{
    [Header("수증기 함정 설정")]
    public float damage = 15f; 
    public float activeDuration = 2f; // 수증기가 뿜어져 나오는 시간
    public float inactiveDuration = 3f; // 수증기가 쉬는 시간 (간헐적)
    public float damageCooldown = 1f; // 같은 대상이 다시 데미지를 입기까지 걸리는 시간
    [Tooltip("수증기에 맞았을 때 위로 튕겨 올라가는 힘입니다. 숫자가 클수록 높이 뜹니다.")]
    public float knockbackPower = 25f; 

    [Header("플레이어 감지 설정")]
    public bool detectPlayer = false; // 체크하면 감지할 때만 분출, 해제 시 주기적 자동 분출
    public float detectionDistance = 4f; // 플레이어를 감지할 거리
    public Vector2 detectDirection = Vector2.up; // 감지 레이 방향

    [Header("애니메이션 설정")]
    public Animator animator;
    public string attackTriggerName = "Attack"; // 수증기 분출 시 실행할 애니메이션 트리거
    public string activeBoolName = "IsActive"; // 분출 상태 지속 여부를 나타낼 애니메이션 파라미터 (선택 사항)

    [Header("콜라이더 Offset 설정")]
    public Collider2D steamCollider; // 데미지 판정을 담당할 콜라이더
    public Vector2 activeOffset = new Vector2(0f, 1.5f); // 수증기가 뿜어져 나올 때의 콜라이더 Offset
    private Vector2 originalOffset; // 평소 기본 콜라이더 Offset

    private SpriteRenderer spriteRenderer; 
    
    [Header("시각 효과 (파티클)")]
    [Tooltip("수증기가 뿜어져 나오는 시각 효과(Particle System)를 연결하세요.")]
    public ParticleSystem steamParticles;

    private bool isActive = false;
    private bool isAttacking = false;
    private Dictionary<GameObject, float> nextDamageTime = new Dictionary<GameObject, float>();

    private void Start()
    {
        if (steamCollider == null) steamCollider = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (steamCollider != null)
        {
            originalOffset = steamCollider.offset;
            steamCollider.enabled = false;
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.2f); 
        
        if (steamParticles != null)
        {
            steamParticles.Stop(); // 시작 시 파티클 중지
        }

        if (!detectPlayer)
        {
            StartCoroutine(SteamRoutine());
        }
    }

    private void Update()
    {
        // 플레이어 감지 모드일 때
        if (detectPlayer && !isAttacking)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, detectDirection.normalized, detectionDistance);
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    StartCoroutine(TriggerSteamAttack());
                    break;
                }
            }
        }
    }

    private IEnumerator SteamRoutine()
    {
        while (true)
        {
            // 쉬는 시간
            yield return new WaitForSeconds(inactiveDuration);
            yield return StartCoroutine(TriggerSteamAttack());
        }
    }

    private IEnumerator TriggerSteamAttack()
    {
        isAttacking = true;
        isActive = true;

        // 1. 애니메이션 재생 및 Collider Offset 이동
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(attackTriggerName)) animator.SetTrigger(attackTriggerName);
            if (!string.IsNullOrEmpty(activeBoolName)) animator.SetBool(activeBoolName, true);
        }

        if (steamCollider != null)
        {
            steamCollider.offset = activeOffset;
            steamCollider.enabled = true;
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.8f);
        if (steamParticles != null) steamParticles.Play();

        // 2. 분출 유지 시간 대기
        yield return new WaitForSeconds(activeDuration);

        // 3. 분출 종료 및 Collider Offset 원복
        isActive = false;
        if (steamCollider != null)
        {
            steamCollider.offset = originalOffset;
            steamCollider.enabled = false;
        }

        if (animator != null && !string.IsNullOrEmpty(activeBoolName))
        {
            animator.SetBool(activeBoolName, false);
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.2f);
        if (steamParticles != null) steamParticles.Stop();

        // 감지 모드일 경우 감지 쿨타임 대기
        if (detectPlayer)
        {
            yield return new WaitForSeconds(inactiveDuration);
        }

        isAttacking = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isActive && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            GameObject target = collision.gameObject;

            // 쿨타임 체크
            if (!nextDamageTime.ContainsKey(target) || Time.time >= nextDamageTime[target])
            {
                ApplyDamageAndKnockback(target);
                nextDamageTime[target] = Time.time + damageCooldown;
            }
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
        Vector2 knockbackDir = Vector2.up; 

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
            
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, knockbackPower);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (detectPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, detectDirection.normalized * detectionDistance);
        }

        Gizmos.color = Color.yellow;
        Vector3 activeOffsetWorldPos = transform.TransformPoint(activeOffset);
        Gizmos.DrawWireSphere(activeOffsetWorldPos, 0.2f);
    }
}
