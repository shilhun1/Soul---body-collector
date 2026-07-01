using UnityEngine; // MonoBehaviour와 Collider2D를 사용하기 위해 필요합니다.

public class PossessionBodyObject : MonoBehaviour // 맵에 떨어져 있는 빙의체 오브젝트입니다.
{
    public PossessionBodyJsonData Data { get; private set; } // 이 오브젝트가 가진 빙의체 데이터입니다.

    public void Init(PossessionBodyJsonData data) // 빙의체 데이터를 초기화합니다.
    {
        Data = data; // 데이터를 저장합니다.
        gameObject.name = "PossessionBody_" + data.id; // 오브젝트 이름을 보기 좋게 바꿉니다.
    }

    public void Possess(GameObject player) // 플레이어에게 빙의를 적용합니다.
    {
        if (player == null || Data == null) return; // 플레이어나 데이터가 없으면 종료합니다.

        PossessionController controller = player.GetComponent<PossessionController>(); // 플레이어의 빙의 컨트롤러를 찾습니다.

        if (controller == null) return; // 빙의 컨트롤러가 없으면 종료합니다.

        controller.Possess(Data.id); // 플레이어에게 빙의를 적용합니다.
        Destroy(gameObject); // 사용한 빙의체 오브젝트를 제거합니다.
    }

    private void OnTriggerEnter2D(Collider2D other) // 다른 콜라이더와 닿았을 때 실행됩니다.
    {
        if (other.CompareTag("Player")) // 닿은 대상이 플레이어라면
        {
            Possess(other.gameObject); // 플레이어에게 빙의를 적용합니다.
        }
    }
}
