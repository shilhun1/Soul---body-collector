using UnityEngine;

public class PlayerLevelRuntime : MonoBehaviour // 플레이어 레벨, 경험치, 스킬 포인트를 관리합니다.
{ 
    public int CurrentLevel { get; private set; } = 1; // 현재 레벨입니다.
    public int CurrentExp { get; private set; } = 0; // 현재 누적 경험치입니다.
    public int SkillPoint { get; private set; } = 0; // 레벨업으로 얻은 사용 가능 스킬 포인트입니다.

    public void AddExp(int amount) // 경험치를 추가합니다.
    {
        CurrentExp += amount; // 경험치를 누적합니다.
        TryLevelUp(); // 레벨업 가능한지 확인합니다.
    }

    private void TryLevelUp() // 경험치 기준으로 레벨업을 처리합니다.
    {
        while (true) // 여러 레벨을 한 번에 올릴 수 있도록 반복합니다.
        {
            LevelUpJsonData nextLevelData = GameDataManager.Instance.GetLevelUp(CurrentLevel + 1); // 다음 레벨 데이터를 가져옵니다.

            if (nextLevelData == null) // 다음 레벨 데이터가 없는지 확인합니다.
            {
                break; // 더 이상 레벨업할 수 없으므로 반복을 끝냅니다.
            }

            if (CurrentExp < nextLevelData.requiredExp) // 경험치가 부족한지 확인합니다.
            {
                break; // 경험치가 부족하면 반복을 끝냅니다.
            } 

            CurrentLevel++; // 레벨을 1 올립니다.
            SkillPoint++; // 레벨업할 때마다 스킬 포인트를 1개 지급합니다.
        }
    }

    public bool TryUseSkillPoint() // 스킬 포인트 1개 사용을 시도합니다.
    {
        if (SkillPoint <= 0) // 사용 가능한 스킬 포인트가 없는지 확인합니다.
        {
            return false; // 포인트가 없으면 실패를 반환합니다.
        }

        SkillPoint--; // 스킬 포인트를 1개 차감합니다.
        return true; // 사용 성공을 반환합니다.
    }
}