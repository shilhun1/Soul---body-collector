using UnityEngine;

public class HSH_WindTrap : MonoBehaviour
{
    [Header("바람 함정 설정")]
    public float damage = 20f; // 데미지 수치
    public float windSpeed = 5f; // 이동 속도
    public float windLifeTime = 5f; // 유지 시간 (시간 경과 시 삭제)
    public Vector2 windDirection = Vector2.left; // 이동 방향 (-1, 0 이면 왼쪽)

    private void Start()
    {
        // 일정 시간 뒤에 맵 밖으로 사라진 바람 제거
        Destroy(gameObject, windLifeTime);
    }

    private void Update()
    {
        // 지정된 방향으로 매 프레임 이동
        transform.Translate(windDirection.normalized * windSpeed * Time.deltaTime);
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
}
