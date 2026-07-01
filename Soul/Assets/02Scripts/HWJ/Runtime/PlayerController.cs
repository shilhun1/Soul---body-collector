using UnityEngine; // Debug와 GameObject 기능을 사용하기 위해 필요합니다.

public class PlayerController : CharacterBase // 플레이어가 CharacterBase를 실제로 상속하기 위한 기본 컨트롤러입니다.
{
    private void Start() // 첫 프레임 전에 실행됩니다.
    {
        PlayerStatController statController = GetComponent<PlayerStatController>(); // 플레이어 능력치 컨트롤러를 찾습니다.

        if (statController != null && statController.CurrentStats != null) // 계산된 능력치가 있으면
        {
            Init(statController.CurrentStats); // 플레이어 체력과 능력치를 초기화합니다.
        }
    }

    public override void Die() // 플레이어가 죽었을 때 실행됩니다.
    {
        Debug.Log("Player Die"); // 임시 사망 로그를 출력합니다.
        gameObject.SetActive(false); // 임시로 플레이어 오브젝트를 비활성화합니다.
    }
}
