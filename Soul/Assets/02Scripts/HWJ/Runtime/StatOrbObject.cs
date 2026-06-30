using UnityEngine;

public class StatOrbObject : MonoBehaviour
{
    public StatOrbJsonData Data { get; private set; } // 이 구슬이 가진 데이터

    public void Init(StatOrbJsonData data) // 구슬 데이터 초기화
    {
        Data = data;
        gameObject.name = "StatOrb_" + data.id; // 오브젝트 이름을 보기 좋게 바꾸기
    }

    public void Collect(GameObject player) // 플레이어가 구슬 획득
    {
        if (player == null || Data == null) // 플레이어나 데이터가 없는지 확인
        {
            return; // 없으면 종료
        }
    }
}
