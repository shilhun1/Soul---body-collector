using UnityEngine;

public class HSH_FireSparkTrap : MonoBehaviour
{
    [Header("불똥(투사체) 설정")]
    public float damage = 15f; // 데미지 수치
    public float speed = 5f; // 이동 속도
    public float lifeTime = 5f; // 유지 시간 (5초 뒤 삭제)
    public Vector2 direction = Vector2.right; // 날아갈 방향 (위, 아래, 양옆 설정 가능)

    [Header("발사기(Spawner) 설정")]
    [Tooltip("체크하면 제자리에서 5초에 한 번씩 불똥을 발사합니다.")]
    public bool isSpawner = false; // 발사기 여부
    public GameObject sparkPrefab; // 발사할 불똥 프리팹
    public float fireInterval = 5f; // 발사 간격 (5초)

    private float timer = 0f;

    private void Start()
    {
        if (!isSpawner)
        {
            // 투사체는 일정 시간 뒤 자동 삭제
            Destroy(gameObject, lifeTime);
        }
        else
        {
            // 발사기는 타이머 초기화 (시작 시점에 바로 쏠 수 있도록)
            timer = fireInterval;
        }
    }

    private void Update()
    {
        if (isSpawner)
        {
            // 주기적으로 무조건 발사
            timer += Time.deltaTime;
            if (timer >= fireInterval)
            {
                timer = 0f;
                if (sparkPrefab != null)
                {
                    Instantiate(sparkPrefab, transform.position, Quaternion.identity);
                }
            }
        }
        else
        {
            // 투사체 이동
            transform.Translate(direction.normalized * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 발사기가 아닌 투사체일 때만 데미지
        if (!isSpawner && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            ApplyDamageAndKnockback(collision.gameObject);
            Destroy(gameObject); // 맞으면 불똥 삭제
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
        knockbackDir.y += 0.5f; // 약간 위로
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

    private void OnDrawGizmos()
    {
        if (isSpawner)
        {
#if UNITY_EDITOR
            if (Camera.current != null && Camera.current.name != "SceneCamera") return;
#endif
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Gizmos.DrawRay(transform.position, direction.normalized * 2f); // 씬 뷰에서 날아갈 방향 표시
        }
    }
}
