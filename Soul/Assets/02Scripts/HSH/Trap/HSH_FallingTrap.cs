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
                // 맞은 것들 중에 플레이어(Player)가 있다면?
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    // 중력을 켜서 돌을 떨어뜨립니다!
                    rb.gravityScale = fallGravity;
                    break; // 찾았으니 검사 종료
                }
            }
        }
    }

    // 감지 센서(Trigger)에 플레이어가 들어왔을 때 (기존 방식 유지)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 중력을 활성화하여 돌을 떨어뜨림
            if (rb != null && rb.gravityScale == 0f)
            {
                rb.gravityScale = fallGravity;
            }
            else
            {
                // 이미 떨어지는 중이거나 땅에 닿은 상태에서 플레이어(유령 등)와 겹쳤다면 데미지
                ApplyDamage(collision.gameObject);
            }
        }
    }

    // 돌이 플레이어와 실제로 부딪혔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("hit");
            ApplyDamage(collision.gameObject);
        }
    }

    private void ApplyDamage(GameObject player)
    {


        // 2. UI 체력바도 깎습니다.
        HSH_BarUI[] barUIs = FindObjectsByType<HSH_BarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 꺼져있는 UI도 찾음
        bool isDamaged = false;

        foreach (var barUI in barUIs)
        {
            if (barUI.currentType == HSH_BarUI.BarType.HP || barUI.currentType == HSH_BarUI.BarType.GhostHP)
            {
                barUI.DecreaseValue(damage);
                isDamaged = true;
            }
        }

        if (isDamaged)
        {
            Debug.Log($"[FallingTrap] 플레이어가 낙석에 맞았습니다! 데미지: {damage}");
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
