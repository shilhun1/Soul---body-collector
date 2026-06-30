using UnityEngine;

public class PlayerStatController : MonoBehaviour // 플레이어 능력치를 게산하고 관리
{
    public StatData baseStats; // 플레이어 기본 능력치
    public StatData CurrentStats { get; private set;  } // 현재 최종 능력치

    [SerializeField] private PlayerLevelRuntime levelRuntime; // 레벨 정보를 가져올 컴포넌트
    private StatData possessionBonus; // 빙의로 얻는 능력치 보너스
    private StatData skillTreeBonus; // 스킬 트리로 얻는 능력치 보너스

    private void Awake() // 오브젝트 생성 시 호출
    {
        if (levelRuntime == null) // 레벨 런타임이 연결되지 않았는지 확인
        {
            levelRuntime = GetComponent<PlayerLevelRuntime>(); // 같은 오브젝트에서 찾음
        }

        RecalculateStats();
    }

    public void AddExp(int exp) // 경험치를 추가학 능력치 갱신
    {
        if(levelRuntime == null) // 레벨 런타임이 없는지 확인
        {
            return; // 없으면 종료
        }

        levelRuntime.AddExp(exp); // 레벨 런타임에 경험치 추가
        RecalculateStats(); // 레벨업 결과를 반영해 능력치 계산
    }

    public void SetPossessionBonus(StatData bouns) // 빙의 보너스 설정
    {
        possessionBonus = bouns; // 빙의 보너스 저장
        RecalculateStats(); // 능력치 계산
    }
    public void SetSkillTreeBonus(StatData bonus) // 스킬 트리 보너스를 설정
    {
        skillTreeBonus = bonus; // 스킬 트리 보너스를 저장
        RecalculateStats(); // 능력치를 다시 계산
    }

    public void RecalculateStats() // 최종 능력치 게산
    {
        CurrentStats = baseStats != null ? baseStats.Clone() : new StatData(); // 기본 능력치를 복사해서 시작

        if (levelRuntime != null) // 레벨 런타임이 있는지 확인
        {
            for(int level = 2; level <= levelRuntime.CurrentLevel; level++) // 2레벨부터 현재 레벨까지 반복
            {
                LevelUpJsonData levelData = GameDataManager.Instance.GetLevelUp(level); //해당 레벨 보너스 데이터를 가져옴

                if (levelData != null) // 레벨 데이터가 있는지 확인
                {
                    CurrentStats.Add(levelData.statBouns); // 레벨 보너스를 더함
                }
            }
        }

        CurrentStats.Add(skillTreeBonus); // 스킬 트리 보너스 더함
        CurrentStats.Add(possessionBonus); // 빙의 보너스 더함
    }
}
