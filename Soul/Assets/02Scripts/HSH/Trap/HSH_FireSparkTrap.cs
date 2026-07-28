using UnityEngine;

public class HSH_FireSparkTrap : MonoBehaviour
{
    [Header("불똥(투사체) 설정")]
    public float damage = 15f; // 데미지 수치
    public float speed = 5f; // 이동 속도
    public float lifeTime = 5f; // 유지 시간 (5초 뒤 삭제)
    public Vector2 direction = Vector2.right; // 날아갈 방향 (위, 아래, 양옆 설정 가능)

    [Header("발사기(Spawner) 설정")]
    [Tooltip("체크하면 제자리에서 주기적/감지 시 불똥을 발사합니다.")]
    public bool isSpawner = false; // 발사기 여부
    public GameObject sparkPrefab; // 발사할 불똥 프리팹
    public float fireInterval = 5f; // 발사 간격 (5초)

    [Header("플레이어 감지 설정 (발사기 전용)")]
    public bool detectPlayer = true; // 체크하면 전방 플레이어 감지 시에만 발사
    public float detectDistance = 10f; // 감지 거리

    [Header("애니메이션 설정 (발사기 전용)")]
    public Animator animator;
    public string fireTriggerName = "Fire"; // 발사 시 실행할 애니메이션 트리거

    private float timer = 0f;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (!isSpawner)
        {
            // 투사체는 일정 시간 뒤 자동 삭제
            Destroy(gameObject, lifeTime);
        }
        else
        {
            // 발사기는 타이머 초기화 (감지되자마자 쏠 수 있게)
            timer = fireInterval;
        }
    }

    private void Update()
    {
        if (isSpawner)
        {
            bool shouldFire = false;

            if (detectPlayer)
            {
                // 전방 레이캐스트로 플레이어 감지
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction.normalized, detectDistance);
                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.CompareTag("Player"))
                    {
                        shouldFire = true;
                        break;
                    }
                }
            }
            else
            {
                shouldFire = true; // 무조건 주기적 발사
            }

            if (shouldFire)
            {
                timer += Time.deltaTime;
                if (timer >= fireInterval)
                {
                    timer = 0f;
                    FireSpark();
                }
            }
        }
        else
        {
            // 투사체 모드: 지정된 방향으로 날아감
            transform.Translate(direction.normalized * speed * Time.deltaTime);
        }
    }

    private void FireSpark()
    {
        // 1. 발사기 애니메이션 연출
        if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
        {
            animator.SetTrigger(fireTriggerName);
        }

        // 2. 불똥 프로젝타일 생성
        if (sparkPrefab != null)
        {
            Instantiate(sparkPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 발사할 sparkPrefab이 지정되지 않았습니다!");
        }
    }

    // 투사체(프로젝타일)가 플레이어 또는 적의 콜라이더에 닿았을 때 데미지 적용
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isSpawner && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            ApplyDamageAndKnockback(collision.gameObject);
            Destroy(gameObject); // 맞으면 불똥 프로젝타일 삭제
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
        knockbackDir.y += 0.5f;
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
        if (isSpawner)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            float drawDist = detectPlayer ? detectDistance : 2f;
            Gizmos.DrawRay(transform.position, direction.normalized * drawDist);
        }
    }
}
