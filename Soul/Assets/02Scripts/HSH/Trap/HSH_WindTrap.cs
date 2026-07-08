using UnityEngine;

public class HSH_WindTrap : MonoBehaviour
{
    [Header("바람 함정 설정")]
    public float damage = 20f; // 데미지 수치
    public float windSpeed = 5f; // 이동 속도
    public float windLifeTime = 5f; // 유지 시간 (시간 경과 시 삭제)
    public Vector2 windDirection = Vector2.left; // 이동 방향 (-1, 0 이면 왼쪽)
    public float detectDistance = 15f; // 감지 거리

    private bool isActivated = false; // 발동 여부

    private void Start()
    {
        // 시작 시에는 삭제하지 않고, 발동될 때 삭제 예약을 합니다.
    }

    private void Update()
    {
        if (!isActivated)
        {
            // 발동 전: 설정된 방향으로 Ray를 쏴서 플레이어 감지
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, windDirection, detectDistance);

            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    isActivated = true;
                    // 발동된 시점부터 일정 시간 뒤에 사라지도록 함
                    Destroy(gameObject, windLifeTime);
                    break;
                }
            }
        }
        else
        {
            // 발동 후: 지정된 방향으로 매 프레임 이동
            transform.Translate(windDirection.normalized * windSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ApplyDamage(collision.gameObject);
            // 플레이어에게 맞으면 바람 투사체 파괴
            // Destroy(gameObject);
        }
    }

    private void ApplyDamage(GameObject player)
    {


        HSH_BarUI[] barUIs = FindObjectsOfType<HSH_BarUI>(true);
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
        // 게임 실행 전에도 씬 뷰에서 감지 거리를 빨간 선으로 항시 확인할 수 있습니다.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, windDirection.normalized * detectDistance);
    }
}
