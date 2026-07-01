using UnityEngine; // MonoBehaviour와 Collider2D를 사용하기 위해 필요합니다.

public class StatOrbObject : MonoBehaviour // 맵에 떨어지는 능력치 구슬 오브젝트입니다.
{
    public StatOrbJsonData Data { get; private set; } // 이 구슬이 가진 데이터입니다.

    public void Init(StatOrbJsonData data) // 구슬 데이터를 초기화합니다.
    {
        Data = data; // 데이터를 저장합니다.
        gameObject.name = "StatOrb_" + data.id; // 오브젝트 이름을 보기 좋게 바꿉니다.
    }

    public void Collect(GameObject player) // 플레이어가 구슬을 획득합니다.
    {
        if (player == null || Data == null) return; // 플레이어나 데이터가 없으면 종료합니다.

        PlayerStatController statController = player.GetComponent<PlayerStatController>(); // 플레이어의 능력치 컨트롤러를 찾습니다.

        if (statController == null) return; // 능력치 컨트롤러가 없으면 종료합니다.

        statController.AddPermanentStatBonus(Data.statBonus); // 구슬 능력치를 영구 보너스로 적용합니다.
        Destroy(gameObject); // 획득한 구슬 오브젝트를 제거합니다.
    }

    private void OnTriggerEnter2D(Collider2D other) // 다른 콜라이더와 닿았을 때 실행됩니다.
    {
        if (other.CompareTag("Player")) // 닿은 대상이 플레이어라면
        {
            Collect(other.gameObject); // 플레이어에게 구슬 효과를 적용합니다.
        }
    }
}
