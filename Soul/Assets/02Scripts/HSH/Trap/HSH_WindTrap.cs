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

    private float fireTimer = 0f; // 발사 타이머

    private void Start()
    {
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
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, windDirection, detectDistance);

            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Player"))
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
            }
            // else
            // {
            //     // 플레이어가 벗어나면 다음에 들어올 때 즉시 쏘도록 타이머를 채워둠
            //     fireTimer = fireInterval;
            // }
        }
        else
        {
            // 3. 투사체 모드: 지정된 방향으로 매 프레임 날아감
            transform.Translate(windDirection.normalized * windSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 발사기가 아닌 '투사체'일 때만 데미지를 줌
        if (!isSpawner && collision.CompareTag("Player"))
        {
            ApplyDamage(collision.gameObject);
            // 원한다면 맞춘 후 바람 삭제 가능
            // Destroy(gameObject);
        }
    }

    private void ApplyDamage(GameObject player)
    {
        HSH_BarUI[] barUIs = FindObjectsByType<HSH_BarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            Debug.Log($"[WindTrap] 플레이어가 바람에 맞았습니다! 데미지: {damage}");
        }
    }

    private void OnDrawGizmos()
    {
        // 발사기일 때만 씬 뷰에서 감지 거리를 빨간 선으로 항시 확인
        if (isSpawner)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, windDirection.normalized * detectDistance);
        }
    }
}
