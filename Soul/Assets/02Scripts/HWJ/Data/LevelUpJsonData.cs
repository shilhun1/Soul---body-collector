using System; // Serializable을 사용하기 위해 필요합니다.

[Serializable] // JSON에서 레벨업 데이터를 읽을 수 있게 합니다.
public class LevelUpJsonData // 레벨업 필요 경험치와 보너스를 담습니다.
{
    public int level; // 적용 대상 레벨입니다.
    public int requiredExp; // 해당 레벨이 되기 위한 누적 경험치입니다.
    public StatData statBonus; // 해당 레벨이 되었을 때 적용할 능력치 보너스입니다.
}
