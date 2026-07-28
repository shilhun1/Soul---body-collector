using UnityEngine;

public class HSH_WindTrap : MonoBehaviour
{
    [Header("바람 함정(투사체) 설정")]
    public float damage = 20f; // 데미지 수치
    public float windSpeed = 5f; // 이동 속도
    public float windLifeTime = 5f; // 유지 시간 (시간 경과 시 삭제)
    public Vector2 windDirection = Vector2.left; // 이동 방향 (-1, 0 이면 왼쪽)

    [Header("발사기(Spawner) 설정")]
    [Tooltip("체크하면 제자리에서 플레이어를 감지하고 바람을 주기적으로 발사합니다.")]
    public bool isSpawner = false; // 발사기 여부
    public GameObject windPrefab; // 발사할 바람 프리팹 (HSH_WindTrap이 붙은 프리팹)
    public float fireInterval = 2f; // 발사 간격(초)
    public float detectDistance = 15f; // 감지 거리

    [Header("애니메이션 설정 (발사기 전용)")]
    public Animator animator;
    public string fireTriggerName = "Fire"; // 바람 발사 시 실행할 애니메이션 트리거

    private float fireTimer = 0f; // 발사 타이머

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (!isSpawner)
        {
            // 투사체(바람)인 경우 시작 시부터 수명 카운트 시작
            Destroy(gameObject, windLifeTime);
        }
        else
        {
            // 발사기인 경우 타이머 초기화 (감지되자마자 쏠 수 있게)
            fireTimer = fireInterval;
        }
    }

    private void Update()
    {
        if (isSpawner)
        {
            // 1. 발사기 모드: 플레이어가 앞에 있는지 Ray로 감지
            bool isPlayerDetected = false;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, windDirection.normalized, detectDistance);

            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    isPlayerDetected = true;
                    break;
                }
            }

            // 2. 플레이어가 감지되면 주기적으로 발사
            if (isPlayerDetected)
            {
                fireTimer += Time.deltaTime;
                if (fireTimer >= fireInterval)
                {
                    fireTimer = 0f;
                    FireWind();
                }
            }
        }
        else
        {
            // 3. 투사체 모드: 지정된 방향으로 매 프레임 날아감
            transform.Translate(windDirection.normalized * windSpeed * Time.deltaTime);
        }
    }

    private void FireWind()
    {
        // 발사 애니메이션 연출
        if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
        {
            animator.SetTrigger(fireTriggerName);
        }

        // 바람 프로젝타일 생성
        if (windPrefab != null)
        {
            Instantiate(windPrefab, transform.position, Quaternion.identity);
            Debug.Log("[WindTrap] 바람 투사체 발사!");
        }
        else
        {
            Debug.LogWarning("[WindTrap] 발사할 바람 프리팹(windPrefab)이 연결되어 있지 않습니다!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 발사기가 아닌 '투사체'일 때만 데미지를 줌
        if (!isSpawner && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
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

    private void OnDrawGizmosSelected()
    {
        if (isSpawner)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, windDirection.normalized * detectDistance);
        }
    }
}
