using UnityEngine;

public class HSH_trap : MonoBehaviour
{
    public enum TrapType
    {
        FallingRock, // 위에서 떨어지는 함정 (낙석)
        WindX,       // X축으로 이동하는 바람 함정
        Spike        // 고정된 장소에서 밟으면 데미지를 주는 함정
    }

    [Header("함정 기본 설정")]
    public TrapType trapType = TrapType.Spike; // 인스펙터에서 원하는 함정 종류 선택
    public float damage = 20f; // 함정에 맞았을 때 깎이는 체력(데미지)
    
    [Header("낙석 함정(FallingRock) 전용 설정")]
    [Tooltip("플레이어가 감지되면 돌에 적용될 중력값 (떨어지는 속도)")]
    public float fallGravity = 3f; 
    private Rigidbody2D rb;

    [Header("바람 함정(WindX) 전용 설정")]
    public bool isWindSpawner = false; // 체크하면 제자리에서 바람을 주기적으로 발사함
    public GameObject windPrefab; // 발사할 프리팹 (HSH_trap이 붙은 바람 프리팹)
    public float windFireInterval = 2f; // 발사 간격(초)
    public float windSpeed = 5f; // 바람 이동 속도
    public float windLifeTime = 5f; // 생성 후 스스로 사라지는 시간
    public Vector2 windDirection = Vector2.left; // 바람이 부는 방향 (-1, 0 이면 왼쪽)
    public float windDetectDistance = 15f; // 바람 감지 거리

    private float windFireTimer = 0f; // 바람 발사 타이머

    private void Start()
    {
        if (trapType == TrapType.FallingRock)
        {
            // 낙석 함정은 처음엔 공중에 매달려 있어야 하므로 중력을 0으로 만듭니다.
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
            }
        }
        else if (trapType == TrapType.WindX)
        {
            if (!isWindSpawner)
            {
                // 투사체인 경우 시작 시 수명 카운트 시작
                Destroy(gameObject, windLifeTime);
            }
            else
            {
                // 발사기인 경우 초기 타이머 셋업 (바로 쏘도록)
                windFireTimer = windFireInterval;
            }
        }
    }

    private void Update()
    {
        if (trapType == TrapType.WindX)
        {
            if (isWindSpawner)
            {
                // 1. 플레이어 감지
                bool isPlayerDetected = false;
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, windDirection, windDetectDistance);

                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        isPlayerDetected = true;
                        break;
                    }
                }

                // 2. 플레이어 감지 시 주기적으로 프리팹 발사
                if (isPlayerDetected)
                {
                    windFireTimer += Time.deltaTime;
                    if (windFireTimer >= windFireInterval)
                    {
                        windFireTimer = 0f;
                        if (windPrefab != null)
                        {
                            Instantiate(windPrefab, transform.position, Quaternion.identity);
                        }
                    }
                }
                else
                {
                    windFireTimer = windFireInterval; // 범위 밖이면 즉시 쏠 수 있도록 충전
                }
            }
            else
            {
                // 투사체 모드: 매 프레임 날아감
                transform.Translate(windDirection.normalized * windSpeed * Time.deltaTime);
            }
        }
    }

    // Trigger(영역)에 플레이어가 닿았을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 대상이 플레이어인지 태그로 확인합니다. (플레이어 오브젝트 Tag가 Player여야 합니다)
        if (collision.CompareTag("Player"))
        {
            // 1. 낙석 함정: 아래쪽 감지 구역(Trigger)에 플레이어가 오면 중력을 줘서 떨어뜨림
            if (trapType == TrapType.FallingRock)
            {
                if (rb != null && rb.gravityScale == 0f)
                {
                    rb.gravityScale = fallGravity;
                }
            }
            // 2. 가시 함정 & 바람 함정: 닿는 즉시 데미지를 줍니다.
            else if (trapType == TrapType.Spike || trapType == TrapType.WindX)
            {
                ApplyDamage();

                // 바람 함정은 플레이어를 때리고 나면 보통 사라지게 합니다.
                if (trapType == TrapType.WindX)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    // 실제 물리적인 물체(Collider)끼리 꽝 부딪혔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 낙석이 떨어져서 플레이어 머리를 쳤을 때 데미지를 줍니다.
            if (trapType == TrapType.FallingRock || trapType == TrapType.Spike)
            {
                ApplyDamage();
            }
        }
    }

    /// <summary>
    /// 플레이어에게 데미지를 주는 함수입니다.
    /// </summary>
    private void ApplyDamage()
    {
        // 아까 만들어둔 HSH_BarUI를 찾아서 데미지 수치만큼 체력을 깎습니다.
        // 체력이 0이 되면 HSH_BarUI에서 알아서 게임오버 창을 띄워줄 것입니다!
        HSH_BarUI barUI = FindObjectOfType<HSH_BarUI>();
        if (barUI != null)
        {
            barUI.DecreaseValue(damage);
            Debug.Log($"[함정] 앗! 함정에 당했습니다! 데미지: {damage}");
        }
        else
        {
            Debug.LogWarning("[함정] 씬에 HSH_BarUI가 없어서 데미지가 안 들어갑니다.");
        }
    }

    private void OnDrawGizmos()
    {
        if (trapType == TrapType.WindX)
        {
            // 게임 실행 전에도 씬 뷰에서 감지 거리를 빨간 선으로 항시 확인할 수 있습니다.
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, windDirection.normalized * windDetectDistance);
        }
    }
}
