using System; // Serializable을 사용하기 위해 필요합니다.

[Serializable] // JSON에서 능력치 구슬 데이터를 읽을 수 있게 합니다.
public class StatOrbJsonData // 강화체 처치 후 얻는 능력치 구슬 데이터입니다.
{
    public string id; // 구슬 고유 ID입니다.
    public string name; // 구슬 이름입니다.
    public StatData statBonus; // 획득 시 플레이어에게 영구 적용할 능력치 보너스입니다.
}
